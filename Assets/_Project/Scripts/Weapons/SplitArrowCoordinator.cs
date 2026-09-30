using UnityEngine;

/// <summary>
/// Split Arrow boon (impactful-weapon-boons R2). A run-scoped coordinator, added to the player by
/// <c>RunBoons</c> when the Bow "Flecha estilhaçante" boon is chosen. When a bow arrow kills an
/// enemy it fans out <c>rank</c> fresh arrows from the victim's position, each carrying 50% of the
/// killing arrow's damage.
///
/// The MonoBehaviour stays thin: it only holds run-scoped references and enforces the two guards the
/// design calls out. Firing itself is delegated to <see cref="ArsenalProjectile.Fire"/>.
///
/// Guards:
/// - One generation only (R2.3): the <see cref="_spawning"/> re-entrancy flag makes <see cref="OnKill"/>
///   a no-op while splits are being fired. A split arrow that scores a kill re-enters through the same
///   <c>HookBus.OnKill</c> event, but the flag short-circuits it, so a split can never spawn more splits.
/// - Bounded per frame (R2.4): <see cref="_spawnedThisFrame"/> caps split spawns at the same 32 ceiling
///   the cascade engine uses (<c>RunSynergyEffects.MaxSecondaryHits</c>), so a mass kill cannot explode
///   the projectile count.
///
/// Lifecycle (R1.4): the <c>HookBus.OnKill</c> subscription is dropped by <c>HookBus.Clear()</c> at run
/// end, so no split arrow from an ended run fires in a later run.
/// </summary>
public sealed class SplitArrowCoordinator : MonoBehaviour
{
    /// <summary>Per-frame split ceiling, mirroring the cascade engine's MaxSecondaryHits budget (R2.4).</summary>
    private const int SpawnBudget = 32;

    /// <summary>Arc between adjacent split headings, in degrees, so each fired arrow leaves on a distinct heading (R2.1).</summary>
    private const float HeadingSpreadDeg = 40f;

    /// <summary>Fixed multiplier: each split carries 50% of the killing arrow's damage (R2.2).</summary>
    private const float SplitDamageMultiplier = .5f;

    /// <summary>Range and rendering constants for the spawned split arrows (cosmetic + reach).</summary>
    private const float SplitRange = 8f;

    private PlayerActor _owner;
    private HookBus _hooks;
    private int _rank;

    private bool _spawning;          // R2.3 re-entrancy guard: a split's kill cannot produce more splits.
    private int _spawnedThisFrame;   // R2.4 per-frame budget counter.
    private int _frame = -1;

    /// <summary>
    /// Binds this coordinator to the run. Safe to call again when the boon rank increases: the previous
    /// subscription is dropped first so the kill handler is never registered twice.
    /// </summary>
    public void Configure(PlayerActor owner, HookBus hooks, int rank)
    {
        if (_hooks != null) _hooks.OnKill -= OnKill;
        _owner = owner;
        _hooks = hooks;
        _rank = rank;
        if (_hooks != null) _hooks.OnKill += OnKill;
    }

    private void OnKill(Actor victim)
    {
        // One generation only (R2.3): ignore kills scored by a split arrow we are currently spawning.
        if (_spawning || _rank <= 0 || !victim) return;
        // No-op when the owner is dead or the equipped weapon is not a bow (R2.1 gate).
        if (!_owner || _owner.IsDead) return;
        WeaponScript weapon = _owner.CurrentWeapon;
        if (!weapon || !weapon.FiresArrows) return;

        // Per-frame budget bookkeeping (R2.4): reset the counter on a new frame, then refuse to spawn
        // if this kill would push the frame's split count past the cascade ceiling.
        if (Time.frameCount != _frame) { _frame = Time.frameCount; _spawnedThisFrame = 0; }
        if (_spawnedThisFrame + _rank > SpawnBudget) return;

        _spawning = true;
        try
        {
            Vector3 origin = victim.transform.position + Vector3.up;
            float damage = weapon.attackDamage;
            for (int i = 0; i < _rank; i++)
            {
                // Distinct headings (R2.1): fan the splits symmetrically around a random flat base heading.
                float angle = (i - (_rank - 1) * .5f) * HeadingSpreadDeg;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * RandomFlatDirection();
                // 50% of the killing arrow's damage, carried as the Fire multiplier (R2.2).
                ArsenalProjectile.Fire(_owner, origin, direction, damage, SplitDamageMultiplier,
                    SplitRange, false, Color.cyan);
                _spawnedThisFrame++;
            }
        }
        finally
        {
            _spawning = false;
        }
    }

    private static Vector3 RandomFlatDirection()
    {
        Vector2 flat = Random.insideUnitCircle;
        Vector3 direction = new Vector3(flat.x, 0f, flat.y);
        return direction.sqrMagnitude < .001f ? Vector3.forward : direction.normalized;
    }
}
