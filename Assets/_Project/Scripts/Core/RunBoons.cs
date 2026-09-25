using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Player-owned upgrades. All modified weapons and skills are isolated runtime copies.</summary>
[RequireComponent(typeof(PlayerActor), typeof(AbilityHolder))]
public sealed class RunBoons : MonoBehaviour
{
    public sealed class Offer
    {
        public readonly string Id, Title, Description;
        public Offer(string id, string title, string description) { Id = id; Title = title; Description = description; }
    }
    private readonly List<Offer> _choices = new List<Offer>();
    private readonly List<Offer> _acquired = new List<Offer>();
    private readonly List<ScriptableObject> _owned = new List<ScriptableObject>();
    private readonly System.Random _random = new System.Random();
    private PlayerActor _player;
    private AbilityHolder _holder;
    private WeaponScript _weapon;
    private NavMeshAgent _agent;
    public bool IsChoosing { get; private set; }
    public int RewardRoom { get; private set; }
    public IReadOnlyList<Offer> Choices => _choices;
    public IReadOnlyList<Offer> Acquired => _acquired;
    public event Action RewardChosen;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _holder = GetComponent<AbilityHolder>();
        _agent = GetComponent<NavMeshAgent>();
        _holder.BindRun(this);
        gameObject.AddComponent<RunRewardUI>().Bind(this);
    }

    private void Start()
    {
        if (!_player.CurrentWeapon) { Debug.LogError("Run requires an equipped weapon.", this); enabled = false; return; }
        _weapon = Instantiate(_player.CurrentWeapon);
        _owned.Add(_weapon);
        Ability[] source = _weapon.abilities;
        _weapon.abilities = new Ability[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            if (!source[i]) continue;
            _weapon.abilities[i] = Instantiate(source[i]);
            _owned.Add(_weapon.abilities[i]);
        }
        _player.EquipWeapon(_weapon);
        _holder.RefreshLoadout();
    }

    public void OfferReward(int room)
    {
        if (IsChoosing || !_weapon || !_player || _player.IsDead) return;
        RewardRoom = room;
        var pool = new List<Offer>
        {
            new Offer("power", "Núcleo de força", "+25% de dano em ataques básicos e habilidades."),
            new Offer("haste", "Mãos velozes", "+25% de velocidade dos ataques básicos."),
            new Offer("recharge", "Fluxo arcano", "+15 pontos percentuais de redução de recarga."),
            new Offer("vitality", "Coração de ferro", "+100 de vida máxima e recuperação completa de vida."),
            new Offer("focus", "Foco eficiente", "Q custa 25% menos mana e recarrega 35% mais rápido."),
            // Repeatable stat scaling.
            new Offer("crit", "Olho preciso", "+15% de chance e +40% de dano crítico."),
            new Offer("brutal", "Golpe brutal", "+8 de dano fixo somado a cada golpe."),
            new Offer("bulwark", "Placa reforçada", "+40 de armadura, reduzindo o dano recebido."),
            new Offer("swift", "Passo veloz", "+20% de velocidade de movimento."),
            // Bizarre property-changing modifiers: they alter what basic attacks DO.
            new Offer("ignite", "Lâmina incandescente", "BIZARRO: seus ataques agora causam QUEIMADURA, dano contínuo por 4s. Acumula."),
            new Offer("frost", "Toque glacial", "BIZARRO: seus ataques agora CONGELAM — chance de imobilizar e lentidão pesada por 2.5s."),
            new Offer("transform", _weapon.FiresArrows ? "Disparo prismático" : "Nova de impacto",
                _weapon.FiresArrows ? "TRANSFORMA Q: troca o disparo duplo por cinco flechas perfurantes em leque." :
                "TRANSFORMA Q: troca o avanço/estocada por uma explosão circular de 4m. Não gera Asura.")
        };
        // Only single-use boons are removed once taken; repeatable ones (stats + elemental) stay in the pool.
        pool.RemoveAll(offer => IsSingleUse(offer.Id) && _acquired.Exists(owned => owned.Id == offer.Id));
        _choices.Clear();
        // Always include a skill-changing option while one remains, plus two different upgrades.
        Offer skillOffer = pool.Find(offer => offer.Id == "transform") ?? pool.Find(offer => offer.Id == "focus");
        if (skillOffer != null) { _choices.Add(skillOffer); pool.Remove(skillOffer); }
        while (_choices.Count < 3 && pool.Count > 0)
        {
            int index = _random.Next(pool.Count);
            _choices.Add(pool[index]); pool.RemoveAt(index);
        }
        IsChoosing = true;
        if (_agent && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath(); _agent.isStopped = true;
        }
        if (TryGetComponent(out CharControlScript controls)) controls.CancelCombo();
    }

    public bool Choose(int index)
    {
        if (!IsChoosing || !_player || _player.IsDead || index < 0 || index >= _choices.Count || _holder.IsCasting) return false;
        Offer offer = _choices[index];
        switch (offer.Id)
        {
            case "power": AddStat(PlayerStatType.IncreasedDamagePercent, 25); break;
            case "haste": AddStat(PlayerStatType.AttackSpeedMultiplier, .25f); break;
            case "recharge": AddStat(PlayerStatType.CooldownReductionPercent, 15); break;
            case "vitality":
                AddStat(PlayerStatType.MaxHealthBonus, 100);
                _player.RefreshResourceStats(); _player.RestoreHealthToMax(); break;
            case "crit":
                AddStat(PlayerStatType.CriticalChance, 15);
                AddStat(PlayerStatType.CriticalDamageMultiplier, 0.4f);
                break;
            case "brutal": AddStat(PlayerStatType.FlatDamageBonus, 8); break;
            case "bulwark": AddStat(PlayerStatType.Armor, 40); break;
            case "swift": AddStat(PlayerStatType.MovementSpeedMultiplier, 0.2f, PlayerStatModifierMode.IncreasedPercent); break;
            case "ignite":
                // Each pick makes the burn hit harder; duration stays at 4s.
                _player.OnHitEffects.EnableBurn(6f, 4f);
                break;
            case "frost":
                // Heavy slow with a chance to fully freeze; picking again raises the chance.
                _player.OnHitEffects.EnableChill(0.9f, 2.5f, 0.35f);
                break;
            case "focus":
                Ability q = _weapon.abilities[0];
                q.cooldownTime *= .65f;
                q.ConfigureRunPresentation(q.DisplayName, q.ManaCost * .75f, q.Glyph);
                break;
            case "transform":
                var skill = ScriptableObject.CreateInstance<ArsenalAbility>();
                skill.ConfigureRunTransformation(_weapon.FiresArrows);
                if (_acquired.Exists(owned => owned.Id == "focus"))
                {
                    skill.cooldownTime *= .65f;
                    skill.ConfigureRunPresentation(skill.DisplayName, skill.ManaCost * .75f, skill.Glyph);
                }
                _owned.Add(skill); _weapon.abilities[0] = skill;
                break;
        }
        _acquired.Add(offer);
        _holder.RefreshLoadout();
        IsChoosing = false;
        if (_agent && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        if (TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
        _choices.Clear();
        RewardChosen?.Invoke();
        return true;
    }

    private void AddStat(PlayerStatType stat, float value, PlayerStatModifierMode mode = PlayerStatModifierMode.Flat) =>
        _player.Stats.AddModifier(new PlayerStatModifier(stat, mode, value), this);

    // Skill transforms and one-off utility boons are single-use; stat and elemental boons repeat and stack.
    private static bool IsSingleUse(string id) => id == "transform" || id == "focus" || id == "recharge" || id == "vitality";

    private void OnDestroy()
    {
        if (_player)
        {
            _player.Stats.RemoveModifiersFrom(this);
            _player.OnHitEffects.Clear();
        }
        if (_holder) _holder.BindRun(null);
        foreach (ScriptableObject asset in _owned) if (asset) Destroy(asset);
    }
}
