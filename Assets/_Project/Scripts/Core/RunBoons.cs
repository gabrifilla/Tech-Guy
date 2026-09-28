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
    // Seedable seam for the Rewrite appearance roll (R9.3): null in production, so the gate falls back
    // to the shared RNG (_random.NextDouble); tests assign it (by reflection) to a deterministic
    // Func<double> so the <= 20% gate can be driven reproducibly. Never touches gameplay behavior
    // beyond which value the roll reads. Used only by OfferReward's Rewrite gate.
    private Func<double> _rewriteRoll;
    private PlayerActor _player;
    private AbilityHolder _holder;
    private WeaponScript _weapon;
    private NavMeshAgent _agent;
    public bool IsChoosing { get; private set; }
    public int RewardRoom { get; private set; }
    public IReadOnlyList<Offer> Choices => _choices;
    public IReadOnlyList<Offer> Acquired => _acquired;
    public event Action RewardChosen;
    public WeaponRunModifiers WeaponModifiers { get; private set; }
    public WeaponScript RunWeapon => _weapon;
    // Escalating "system breaking" feedback state (R10); owned and driven per run.
    private SystemBreakState _systemBreak;
    // Per-run combat-event bus (R8/R11); created per run and cleared on run end so a previous
    // run's subscribers never fire later. Reached by collaborators through this getter (RunBoons
    // ownership), never via scene lookups.
    public HookBus Hooks { get; private set; }

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
        WeaponModifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(_weapon));
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
        // A fresh escalation state per run; recomputed from Acquired whenever a reward is chosen (R10.5/R11.4).
        _systemBreak = new SystemBreakState();
        // A fresh event bus per run; collaborators (raise-sites) reach it via the Hooks getter.
        Hooks = new HookBus();
        RewardChosen += EvaluateSystemBreak;
    }

    // Recomputes the interacting-modifier count from the acquired set and drives the escalation tiers (R10.1/R10.2).
    private void EvaluateSystemBreak() => _systemBreak?.Evaluate(_acquired.Count);

    public void OfferReward(int room)
    {
        if (IsChoosing || !_weapon || !_player || _player.IsDead) return;
        RewardRoom = room;
        var pool = new List<Offer>
        {
            new Offer("conductor", "Bobina encadeada", "Acertos saltam com 45% do dano e carregam fogo/gelo. Cada cópia: +1 alvo por salto e +0,5m de alcance (inicial: 3,5m)."),
            new Offer("detonation", "Reator de sucata", "Mortes por impacto explodem: 75% do dano em 3,5m. Cópias: +25 pontos percentuais e +0,5m. Explosões continuam a cadeia."),
            new Offer("reactor", "Combustível instável", "Com fogo adquirido, sua queimadura ganha por segundo +12% do dano do acerto por cópia. Combine com críticos e descargas."),
            new Offer("resonance", "Ressonância térmica", "Descargas e explosões: +40% de dano por efeito de fogo/gelo já presente no alvo, por cópia. Prepare a horda com elementos!"),
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
        foreach (var definition in WeaponRunModifiers.Catalog)
            if (definition.Family == WeaponModifiers.Family && WeaponModifiers.Rank(definition.Kind) < definition.MaxRank)
                pool.Add(new Offer(definition.Id, definition.Title, "Nível " + (WeaponModifiers.Rank(definition.Kind) + 1) + "/" + definition.MaxRank + "\n" + definition.Description));
        // Only single-use boons are removed once taken; repeatable ones (stats + elemental) stay in the pool.
        pool.RemoveAll(offer => IsSingleUse(offer.Id) && _acquired.Exists(owned => owned.Id == offer.Id));
        // Rewrite category (R9): exclude every Rewrite when no valid target ability exists (R9.5),
        // and gate its appearance behind a <= 20% roll per offer set (R9.3). At most one Rewrite is
        // ever added because only ability-replacing ids are classified as Rewrite and each is unique here.
        bool rewriteAllowed = HasRewriteTarget() && (_rewriteRoll ?? _random.NextDouble)() <= 0.2;
        if (!rewriteAllowed)
            pool.RemoveAll(offer => RunModifierPresentation.IsRewriteId(offer.Id));
        _choices.Clear();
        // Always include a skill-changing option while one remains, plus two different upgrades.
        // A Rewrite (when allowed) fills the skill slot; otherwise fall back to the focus upgrade.
        Offer skillOffer = pool.Find(offer => RunModifierPresentation.IsRewriteId(offer.Id)) ?? pool.Find(offer => offer.Id == "focus");
        if (skillOffer != null) { _choices.Add(skillOffer); pool.Remove(skillOffer); }
        var weaponOffers = pool.FindAll(offer => offer.Id.StartsWith("weapon_", StringComparison.Ordinal));
        if (weaponOffers.Count > 0)
        {
            Offer weaponOffer = weaponOffers[_random.Next(weaponOffers.Count)];
            _choices.Add(weaponOffer); pool.Remove(weaponOffer);
        }
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
        if (Time.timeScale <= 0f || !IsChoosing || !_player || _player.IsDead || index < 0 || index >= _choices.Count || _holder.IsCasting) return false;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return false;
        Offer offer = _choices[index];
        switch (offer.Id)
        {
            default:
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                break;
            case "conductor": _player.OnHitEffects.Synergies.Add(RunSynergy.Conductor); break;
            case "detonation": _player.OnHitEffects.Synergies.Add(RunSynergy.Detonation); break;
            case "reactor": _player.OnHitEffects.Synergies.Add(RunSynergy.Reactor); break;
            case "resonance": _player.OnHitEffects.Synergies.Add(RunSynergy.Resonance); break;
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
            case "swift": AddStat(PlayerStatType.MovementSpeedMultiplier, 20f, PlayerStatModifierMode.IncreasedPercent); break;
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

    // A Rewrite replaces the slot-1 (Q) ability via a runtime copy, so it needs a live ability to rewrite (R9.5).
    private bool HasRewriteTarget() => _weapon && _weapon.abilities != null && _weapon.abilities.Length > 0 && _weapon.abilities[0];

    private void OnDestroy()
    {
        // Lifecycle unbinding first: remove stat modifiers and detach from the holder / escalation
        // driver so nothing can re-populate run-scoped state while (or after) it is being cleared.
        if (_player) _player.Stats.RemoveModifiersFrom(this);
        if (_holder) _holder.BindRun(null);
        RewardChosen -= EvaluateSystemBreak;
        // R11.4: clear every piece of run-scoped state together, as one atomic operation.
        ClearRunState();
        // Runtime-copy cleanup last: the atomic clear no longer depends on these assets.
        foreach (ScriptableObject asset in _owned) if (asset) Destroy(asset);
    }

    // R11.4: atomically clear all run-scoped state as a single operation, so the element registry
    // (PlayerOnHitEffects), the cascade ranks (RunSynergyEffects, cleared via the element registry's
    // Clear), the combat-event bus (HookBus), and the escalation feedback (SystemBreakState) are all
    // reset together rather than independently or at different times. A previous run's state can never
    // leak into a later run.
    private void ClearRunState()
    {
        // PlayerOnHitEffects.Clear() cascades to RunSynergyEffects.Clear() (it calls _synergies.Clear()),
        // so this single call empties both the element registry and the cascade ranks.
        if (_player && _player.TryGetComponent(out PlayerOnHitEffects effects)) effects.Clear();
        // Drop every subscriber so a previous run's boons never fire in a later run (R8.9).
        Hooks?.Clear();
        // Reset the escalation tier (R10.5).
        _systemBreak?.Reset();
    }
}
