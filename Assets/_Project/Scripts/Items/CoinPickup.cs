using UnityEngine;

/// <summary>
/// A coin dropped by an enemy. It spins and bobs in place until the player walks over it,
/// then banks its worth into the persistent <see cref="CurrencyWallet"/>.
/// Player detection mirrors WeaponPickup (tag + PlayerActor in parents).
/// </summary>
[DisallowMultipleComponent]
public sealed class CoinPickup : MonoBehaviour
{
    private const string DefaultCoinResource = "Items/CoinPickup";

    // Shared coin prefab loaded once from Resources (Unity-managed asset: never moved/removed, R5.4).
    private static CoinPickup _sharedCoin;

    /// <summary>
    /// Static pool that replaces the per-coin <c>Instantiate</c>/<c>Destroy</c> pair. The factory
    /// instantiates the shared <c>Resources/Items/CoinPickup</c> prefab once per instance; the prefab
    /// stays Unity-managed and is never moved or removed (R5.4). Mirrors the lazy pool-root pattern
    /// used by <c>HitboxDamage.effectPools</c> (R3.5). Registered with <see cref="PoolResetRegistry"/>
    /// so static state does not leak between plays in the Editor.
    /// </summary>
    /// <remarks>Feature: project-cleanup-optimization, task 5.1. Requirements: 3.3, 3.5, 3.7.</remarks>
    public static ComponentPool<CoinPickup> Pool;

    static CoinPickup()
    {
        PoolResetRegistry.Register(() => Pool = null);
    }

    /// <summary>
    /// Returns a pooled coin, building the pool lazily on first use. The factory instantiates the
    /// shared <c>Resources/Items/CoinPickup</c> prefab once per instance (R3.7 expansion). Returns
    /// <c>null</c> when the prefab cannot be loaded; callers fall back to the current behavior.
    /// </summary>
    public static CoinPickup Acquire()
    {
        if (Pool == null)
        {
            CoinPickup prefab = SharedPrefab();
            if (!prefab) return null;
            Pool = new ComponentPool<CoinPickup>(() => Instantiate(prefab), reset: coin => coin.ResetForReuse(), rootName: "Coin");
        }

        return Pool.Acquire();
    }

    private static CoinPickup SharedPrefab()
    {
        if (!_sharedCoin) _sharedCoin = Resources.Load<CoinPickup>(DefaultCoinResource);
        return _sharedCoin;
    }

    [Header("Value")]
    [SerializeField, Min(1)] private int value = 1;

    [Header("Presentation")]
    [Tooltip("Visual that spins. When empty, the root transform spins instead.")]
    [SerializeField] private Transform spinTarget;
    [SerializeField, Min(0f)] private float spinSpeed = 220f;
    [SerializeField, Min(0f)] private float bobHeight = 0.18f;
    [SerializeField, Min(0f)] private float bobSpeed = 2.5f;

    [Header("Collection")]
    [Tooltip("Distance at which the player collects the coin. Backs up the trigger in case physics layers block it.")]
    [SerializeField, Min(0f)] private float collectRadius = 1.4f;
    [Tooltip("Distance at which the coin flies toward the player. 0 keeps the coin where the enemy died until the player walks over it.")]
    [SerializeField, Min(0f)] private float magnetRadius = 0f;
    [SerializeField, Min(0f)] private float magnetSpeed = 9f;

    [Header("Lifetime")]
    [Tooltip("Seconds before an uncollected coin despawns. 0 keeps it forever.")]
    [SerializeField, Min(0f)] private float lifetime = 30f;
    [Tooltip("Seconds spent fading/scaling out once picked up or expired.")]
    [SerializeField, Min(0f)] private float pickupFlourish = 0.12f;

    private Transform _spin;
    private Vector3 _basePosition;
    private float _phase;
    private bool _collected;
    private Transform _player;
    private float _playerSearchTimer;

    /// <summary>
    /// Sets the coin worth before it is picked up. Spawners call this after positioning the coin, so
    /// it also anchors the bob/magnet base position to the final spawn location — important for pooled
    /// reuse, where the pool reset runs before the spawner repositions the instance.
    /// </summary>
    public void SetValue(int coins)
    {
        value = Mathf.Max(1, coins);
        _basePosition = transform.position;
    }

    /// <summary>Injects the player at spawn time so the coin avoids a scene search.</summary>
    public void SetPlayer(Transform player) => _player = player;

    private void Start() => ResetForReuse();

    /// <summary>
    /// Returns a pooled (or freshly created) coin to its "just spawned" state so a reused instance
    /// behaves exactly like one that just ran <see cref="Start"/>: clears the collected flag, resets
    /// the lifetime timer, reinitializes the spin target, base position, scale and spin phase. Runs
    /// on every <see cref="Acquire"/> (as the pool reset action) and on first-time <see cref="Start"/>.
    /// </summary>
    /// <remarks>Feature: project-cleanup-optimization, task 5.2. Requirements: 3.3, 3.4, 3.6.</remarks>
    private void ResetForReuse()
    {
        _collected = false;
        _player = null;
        _playerSearchTimer = 0f;
        _spin = spinTarget ? spinTarget : transform;
        _spin.localScale = Vector3.one;
        _basePosition = transform.position;
        _phase = Random.value * Mathf.PI * 2f;
        CancelInvoke();
        if (lifetime > 0f) Invoke(nameof(Expire), lifetime);
    }

    private void Update()
    {
        if (_collected) return;

        if (spinSpeed > 0f) _spin.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        Transform player = ResolvePlayer();
        float distance = player ? Vector3.Distance(transform.position, player.position) : float.PositiveInfinity;

        // Proximity pickup backs up the physics trigger: the player uses a CharacterController,
        // so the coin needs a kinematic Rigidbody for OnTriggerEnter, and this guarantees collection either way.
        if (player && distance <= collectRadius)
        {
            CollectFor(player);
            return;
        }

        if (player && magnetRadius > 0f && distance <= magnetRadius)
        {
            // Optional magnet: only pulls the coin in when magnetRadius is enabled. Off by default so
            // coins stay where the enemy died and the player walks over them to collect.
            _basePosition = Vector3.MoveTowards(_basePosition, player.position, magnetSpeed * Time.deltaTime);
        }

        if (bobHeight > 0f)
        {
            Vector3 position = _basePosition;
            position.y += Mathf.Sin(Time.time * bobSpeed + _phase) * bobHeight;
            transform.position = position;
        }
        else
        {
            transform.position = _basePosition;
        }
    }

    private Transform ResolvePlayer()
    {
        if (_player) return _player;
        // Re-resolve occasionally rather than every frame when no player is cached yet.
        _playerSearchTimer -= Time.deltaTime;
        if (_playerSearchTimer > 0f) return null;
        _playerSearchTimer = 0.25f;
        var actor = FindPlayerActor();
        if (actor) _player = actor.transform;
        return _player;
    }

    private static PlayerActor FindPlayerActor()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<PlayerActor>();
#else
        return Object.FindObjectOfType<PlayerActor>();
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;
        PlayerActor actor = other.GetComponentInParent<PlayerActor>();
        if (!actor) return;
        CollectFor(actor.transform);
    }

    private void CollectFor(Transform player)
    {
        if (_collected) return;
        _player = player;
        CurrencyWallet.Add(value);
        Collect();
    }

    private void Expire()
    {
        if (!_collected) Collect();
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;
        CancelInvoke();
        if (pickupFlourish > 0f && isActiveAndEnabled) StartCoroutine(FlourishThenDestroy());
        else ReleaseToPool();
    }

    private System.Collections.IEnumerator FlourishThenDestroy()
    {
        Vector3 startScale = _spin.localScale;
        float elapsed = 0f;
        while (elapsed < pickupFlourish)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pickupFlourish);
            // Quick pop then shrink to nothing for a satisfying grab.
            float scale = t < 0.4f ? Mathf.Lerp(1f, 1.35f, t / 0.4f) : Mathf.Lerp(1.35f, 0f, (t - 0.4f) / 0.6f);
            _spin.localScale = startScale * scale;
            transform.position += Vector3.up * (2.5f * Time.deltaTime);
            _spin.Rotate(Vector3.up, spinSpeed * 2f * Time.deltaTime, Space.World);
            yield return null;
        }
        ReleaseToPool();
    }

    /// <summary>
    /// Returns the coin to the shared pool instead of destroying it (the terminal path for
    /// <see cref="Collect"/> and the end of <see cref="FlourishThenDestroy"/>). Falls back to
    /// <c>Destroy</c> for coins created outside the pool (per-enemy prefab override / failed load),
    /// matching the previous behavior.
    /// </summary>
    /// <remarks>Feature: project-cleanup-optimization, task 5.2. Requirements: 3.3, 3.6.</remarks>
    private void ReleaseToPool()
    {
        if (Pool != null) Pool.Release(this);
        else Destroy(gameObject);
    }
}
