using UnityEngine;

/// <summary>
/// Volátil (Etapa 3, 11/11/2026; replaces the Fragile's role): when it dies it leaves a time bomb on the floor —
/// a red ring that blinks faster and faster for <see cref="VolatileBlastModel.Fuse"/> seconds — then explodes,
/// hurting only the player inside <see cref="VolatileBlastModel.Radius"/>. Punishes careless AoE in the middle
/// of the group. Added at runtime by <see cref="EnemyVariant"/> for <see cref="ArchetypeId.Fragile"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Actor))]
public sealed class VolatileBehavior : MonoBehaviour
{
    private Actor _owner;
    private EnemyAI _ai;

    private void Awake()
    {
        _owner = GetComponent<Actor>();
        _ai = GetComponent<EnemyAI>();
    }

    private void OnEnable() { if (_owner) _owner.Died += OnDied; }
    private void OnDisable() { if (_owner) _owner.Died -= OnDied; }

    private void OnDied(Actor actor)
    {
        if (!_ai || !_ai.player) return;
        var player = _ai.player.GetComponent<PlayerActor>();
        if (!player) return;
        // Detached from the dying enemy (which is destroyed this frame); parented to the encounter root so
        // an encounter teardown removes a pending bomb with the rest of the room.
        VolatileBlast.Spawn(transform.position, player, transform.parent);
    }
}

/// <summary>Pure timing/shape of the Volátil bomb, testable without a scene.</summary>
public static class VolatileBlastModel
{
    public const float Fuse = .8f;
    public const float Radius = 2.5f;
    public const float Damage = 30f;
    /// <summary>Blink rate at the start and at the end of the fuse (blinks per second).</summary>
    public const float StartBlinkRate = 4f, EndBlinkRate = 18f;

    /// <summary>True when the ring is in its bright half-cycle at <paramref name="elapsed"/>; the rate ramps up.</summary>
    public static bool IsLit(float elapsed)
    {
        float t = Mathf.Clamp(elapsed, 0f, Fuse);
        // Phase = integral of a linearly ramping rate: rate(t) = a + (b-a)·t/F  =>  phase = a·t + (b-a)·t²/(2F).
        float phase = StartBlinkRate * t + (EndBlinkRate - StartBlinkRate) * t * t / (2f * Fuse);
        return phase - Mathf.Floor(phase) < .5f;
    }

    public static bool Hits(Vector3 center, Vector3 target)
    {
        Vector3 delta = target - center;
        if (Mathf.Abs(delta.y) > 2.5f) return false;
        delta.y = 0f;
        return delta.sqrMagnitude <= Radius * Radius;
    }
}

/// <summary>The bomb left by a dead Volátil. Uses scaled time, so pause and hitstop freeze the fuse.</summary>
public sealed class VolatileBlast : MonoBehaviour
{
    private static readonly Color Bright = new Color(1f, .12f, .08f), Dim = new Color(.35f, .03f, .02f);
    private CombatGroundRing _ring, _core;
    private PlayerActor _player;
    private float _elapsed;
    private bool _done;

    public bool Exploded => _done;

    public static VolatileBlast Spawn(Vector3 position, PlayerActor player, Transform parent)
    {
        var go = new GameObject("Volátil — bomba");
        if (parent) go.transform.SetParent(parent, true);
        go.transform.position = position;
        var blast = go.AddComponent<VolatileBlast>();
        blast._player = player;
        blast._ring = CombatGroundRing.Create(go.transform, "Volátil raio", Bright);
        blast._core = CombatGroundRing.Create(go.transform, "Volátil núcleo", Bright);
        blast.Draw();
        return blast;
    }

    private void Draw()
    {
        Vector3 center = transform.position + Vector3.up * .05f;
        bool lit = VolatileBlastModel.IsLit(_elapsed);
        float fraction = Mathf.Clamp01(_elapsed / VolatileBlastModel.Fuse);
        if (_ring) { _ring.Draw(center, VolatileBlastModel.Radius, lit ? .16f : .1f); _ring.SetColor(lit ? Bright : Dim); }
        // Inner ring grows toward the edge: how close the bomb is to going off.
        if (_core) { _core.Draw(center, Mathf.Max(.15f, VolatileBlastModel.Radius * fraction), .08f); _core.SetColor(lit ? Bright : Dim); }
    }

    private void Update()
    {
        if (_done) return;
        _elapsed += Time.deltaTime;
        if (_elapsed < VolatileBlastModel.Fuse) { Draw(); return; }
        _done = true;
        if (_player && !_player.IsDead && _player.isActiveAndEnabled && VolatileBlastModel.Hits(transform.position, _player.transform.position))
            _player.TakeDamage(VolatileBlastModel.Damage);
        if (_ring) { _ring.Draw(transform.position + Vector3.up * .05f, VolatileBlastModel.Radius, .35f); _ring.SetColor(Color.white); }
        if (_core) _core.gameObject.SetActive(false);
        Destroy(gameObject, .15f);
    }
}
