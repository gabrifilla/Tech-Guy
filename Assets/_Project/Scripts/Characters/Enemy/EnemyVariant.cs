using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Actor), typeof(EnemyAI))]
public sealed class EnemyVariant : MonoBehaviour
{
    [SerializeField] private EnemyProfile _profile;
    [Tooltip("Optional abilities for this enemy variant, independent of its rarity profile.")]
    [SerializeField] private GameObject[] _additionalAffixPrefabs;
    private Actor _actor;
    private EnemyAI _ai;
    private NavMeshAgent _agent;
    private float _baseHealth, _baseDamage, _baseSpeed, _baseInterval;
    private bool _started;
    private Vector3 _baseScale;
    public float VisualScaleMultiplier => EnemyVisualStyle.SizeMultiplier(_profile ? _profile.Rarity : EnemyRarity.Normal, GetComponent<SectorBoss>());
    private readonly List<GameObject> _affixes = new List<GameObject>();

    public EnemyProfile Profile => _profile;
    public string AffixSummary { get; private set; } = "";

    private void Start()
    {
        _actor = GetComponent<Actor>();
        _baseScale = transform.localScale;
        _ai = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _baseHealth = _actor.maxHealth;
        _baseDamage = _ai.attackDamage;
        _baseInterval = _ai.timeBetweenAttacks;
        _baseSpeed = _agent ? _agent.speed : 0f;
        _started = true;
        ApplyProfile();
    }

    public void Configure(EnemyProfile profile)
    {
        _profile = profile;
        if (_started) ApplyProfile();
    }

    public void Configure(EnemyProfile profile, GameObject[] affixPrefabs)
    {
        _additionalAffixPrefabs = affixPrefabs;
        Configure(profile);
    }

    private void ApplyProfile()
    {
        transform.localScale = _baseScale * VisualScaleMultiplier;
        foreach (GameObject affix in _affixes)
        {
            if (!affix) continue;
            affix.SetActive(false);
            Destroy(affix);
        }
        _affixes.Clear();
        if (TryGetComponent(out SectorBoss boss))
        {
            AffixSummary = "";
            _actor.SetDamageTakenMultiplier(this,1f);
            _actor.SetMaxHealth(boss.MaximumHealth);
            return;
        }
        var selected = new HashSet<GameObject>();
        if (_profile && _profile.AffixPrefabs != null)
            foreach (var prefab in _profile.AffixPrefabs) if (prefab) selected.Add(prefab);
        if (_additionalAffixPrefabs != null)
            foreach (var prefab in _additionalAffixPrefabs) if (prefab) selected.Add(prefab);
        float movement = 1f, attackSpeed = 1f, damageTaken = 1f;
        var names = new List<string>();
        foreach (var prefab in selected)
        {
            if (prefab.GetComponentInChildren<EnemyFrostAura>(true)) names.Add("Gelo");
            foreach (var affix in prefab.GetComponentsInChildren<EnemyStatAffix>(true))
            {
                movement *= affix.MovementMultiplier;
                attackSpeed *= affix.AttackSpeedMultiplier;
                damageTaken *= affix.DamageTakenMultiplier;
                names.Add(affix.DisplayName);
            }
        }
        AffixSummary = string.Join(" · ", names);
        ApplyStanceProfile();
        _actor.SetDamageTakenMultiplier(this, damageTaken);
        _actor.SetMaxHealth(_baseHealth * (_profile ? _profile.HealthMultiplier : 1f));
        _ai.ConfigureAttack(_baseDamage * (_profile ? _profile.DamageMultiplier : 1f),
            _baseInterval / ((_profile ? _profile.AttackSpeedMultiplier : 1f) * attackSpeed));
        if (_agent) _agent.speed = _baseSpeed * (_profile ? _profile.MovementMultiplier : 1f) * movement;

        if (_actor.IsDead) return;
        var added = new HashSet<GameObject>();
        if (_profile) SpawnAffixes(_profile.AffixPrefabs, added);
        SpawnAffixes(_additionalAffixPrefabs, added);
    }

    /// <summary>
    /// Feeds the enemy's stance pool and crowd-control resistances into its CombatReactionController.
    /// The rank (already set by the spawner) stays authoritative for the enemy category; the profile
    /// supplies the concrete stance numbers and per-CC resistances on top of it.
    /// </summary>
    private void ApplyStanceProfile()
    {
        CombatReactionController reaction = GetComponentInChildren<CombatReactionController>();
        if (!reaction) return;

        EnemyRank resolvedRank = reaction.Rank;
        if (!_profile)
        {
            // No profile: keep whatever rank defaults the spawner applied.
            reaction.ConfigureRank(resolvedRank);
            return;
        }

        reaction.ConfigureStance(
            resolvedRank,
            _profile.MaxStance,
            _profile.StanceDamageMultiplier,
            _profile.StanceRecoveryPerSecond,
            _profile.StanceRecoveryDelay,
            _profile.StaggerResistance,
            _profile.StunResistance,
            _profile.KnockUpResistance,
            _profile.KnockbackResistance);
    }

    private void SpawnAffixes(IReadOnlyList<GameObject> prefabs, HashSet<GameObject> added)
    {
        if (prefabs == null) return;
        foreach (GameObject prefab in prefabs)
        {
            if (prefab && added.Add(prefab)) _affixes.Add(Instantiate(prefab, transform, false));
        }
    }
}
