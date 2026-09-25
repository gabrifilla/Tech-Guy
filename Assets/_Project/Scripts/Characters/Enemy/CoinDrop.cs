using UnityEngine;

/// <summary>
/// Drops coin pickups when the enemy dies. Auto-attached to every non-player Actor by
/// <see cref="Actor.Awake"/>, so no prefab rewiring is required.
///
/// Economy: coins are meant to be rare. A normal mob only sometimes drops a single coin;
/// rarer enemies drop more often and slightly more. A boss (<see cref="SectorBoss"/>) always
/// drops a small handful (2-3). Unlock prices in <see cref="WeaponLoadout"/> are tuned low to match.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Actor))]
public sealed class CoinDrop : MonoBehaviour
{
    private const string DefaultCoinResource = "Items/CoinPickup";

    [Tooltip("Optional coin prefab. When empty, the shared coin from Resources/Items is used.")]
    [SerializeField] private CoinPickup _coinPrefab;

    [Header("Normal mob (chance-based)")]
    [Tooltip("Chance a normal mob drops anything at all. Rarer enemies improve on this.")]
    [SerializeField, Range(0f, 1f)] private float _dropChance = 0.35f;
    [Tooltip("Coins dropped by a normal mob when it does drop.")]
    [SerializeField, Min(1)] private int _normalCoins = 1;

    [Header("Boss")]
    [SerializeField, Min(1)] private int _bossCoinsMin = 2;
    [SerializeField, Min(1)] private int _bossCoinsMax = 3;

    [Header("Scatter")]
    [SerializeField, Min(0f)] private float _scatterRadius = 0.6f;
    [SerializeField, Min(0f)] private float _dropHeight = 0.6f;

    private static CoinPickup _sharedCoin;
    private Actor _actor;

    private void Awake()
    {
        _actor = GetComponent<Actor>();
        if (_actor) _actor.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (_actor) _actor.Died -= OnDied;
    }

    private void OnDied(Actor actor)
    {
        if (actor) actor.Died -= OnDied;

        int total = RollCoinTotal();
        if (total <= 0) return;

        CoinPickup prefab = ResolvePrefab();
        if (!prefab)
        {
            Debug.LogWarning($"{nameof(CoinDrop)} could not load a coin prefab from Resources/{DefaultCoinResource}; no coins dropped.", this);
            return;
        }

        Vector3 origin = transform.position + Vector3.up * _dropHeight;
        // One coin per unit keeps each pickup worth 1, so a rare economy reads clearly on screen.
        for (int i = 0; i < total; i++) SpawnCoin(prefab, origin, 1);
    }

    /// <summary>Decides how many coins to drop based on rank/rarity. Normal mobs are chance-gated.</summary>
    private int RollCoinTotal()
    {
        if (GetComponent<SectorBoss>())
            return Random.Range(_bossCoinsMin, _bossCoinsMax + 1);

        EnemyVariant variant = GetComponent<EnemyVariant>();
        EnemyRarity rarity = variant && variant.Profile ? variant.Profile.Rarity : EnemyRarity.Normal;

        switch (rarity)
        {
            case EnemyRarity.Rare:
                // Rare mobs reliably give a couple coins.
                return Random.value < Mathf.Clamp01(_dropChance + 0.5f) ? 2 : 1;
            case EnemyRarity.Magic:
                // Magic mobs drop a bit more often.
                return Random.value < Mathf.Clamp01(_dropChance + 0.25f) ? 1 : 0;
            default:
                return Random.value < _dropChance ? _normalCoins : 0;
        }
    }

    private CoinPickup ResolvePrefab()
    {
        if (_coinPrefab) return _coinPrefab;
        if (!_sharedCoin) _sharedCoin = Resources.Load<CoinPickup>(DefaultCoinResource);
        return _sharedCoin;
    }

    private void SpawnCoin(CoinPickup prefab, Vector3 origin, int coins)
    {
        Vector3 offset = Random.insideUnitSphere * _scatterRadius;
        offset.y = 0f;
        CoinPickup pickup = Instantiate(prefab, origin + offset, Quaternion.Euler(0f, Random.value * 360f, 0f));
        pickup.SetValue(coins);
        Transform player = ResolvePlayer();
        if (player) pickup.SetPlayer(player);
    }

    private Transform ResolvePlayer()
    {
        // Reuse the reference the enemy AI already resolved instead of searching the scene.
        EnemyAI ai = GetComponent<EnemyAI>();
        return ai ? ai.player : null;
    }
}
