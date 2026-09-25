using UnityEngine;

/// <summary>
/// A coin dropped by an enemy. It spins and bobs in place until the player walks over it,
/// then banks its worth into the persistent <see cref="CurrencyWallet"/>.
/// Player detection mirrors WeaponPickup (tag + PlayerActor in parents).
/// </summary>
[DisallowMultipleComponent]
public sealed class CoinPickup : MonoBehaviour
{
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

    /// <summary>Sets the coin worth before it is picked up.</summary>
    public void SetValue(int coins) => value = Mathf.Max(1, coins);

    /// <summary>Injects the player at spawn time so the coin avoids a scene search.</summary>
    public void SetPlayer(Transform player) => _player = player;

    private void Start()
    {
        _spin = spinTarget ? spinTarget : transform;
        _basePosition = transform.position;
        _phase = Random.value * Mathf.PI * 2f;
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
        else Destroy(gameObject);
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
        Destroy(gameObject);
    }
}
