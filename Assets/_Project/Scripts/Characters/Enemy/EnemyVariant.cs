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
    private readonly List<GameObject> _affixes = new List<GameObject>();

    public EnemyProfile Profile => _profile;

    private void Start()
    {
        _actor = GetComponent<Actor>();
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

    private void ApplyProfile()
    {
        foreach (GameObject affix in _affixes)
        {
            if (!affix) continue;
            affix.SetActive(false);
            Destroy(affix);
        }
        _affixes.Clear();
        _actor.SetMaxHealth(_baseHealth * (_profile ? _profile.HealthMultiplier : 1f));
        _ai.ConfigureAttack(_baseDamage * (_profile ? _profile.DamageMultiplier : 1f),
            _baseInterval / (_profile ? _profile.AttackSpeedMultiplier : 1f));
        if (_agent) _agent.speed = _baseSpeed * (_profile ? _profile.MovementMultiplier : 1f);

        if (_actor.IsDead) return;
        var added = new HashSet<GameObject>();
        if (_profile) SpawnAffixes(_profile.AffixPrefabs, added);
        SpawnAffixes(_additionalAffixPrefabs, added);
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
