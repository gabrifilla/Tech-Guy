using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Central reset hook for the <c>static</c> object pools and caches introduced by the
/// project-cleanup-optimization feature. Because those pools (e.g. <c>ArsenalProjectile.Pool</c>,
/// <c>EnemyProjectile.Pool</c>, <c>CoinPickup.Pool</c>, the weapon cache) are <c>static</c>, their
/// contents survive between Play sessions in the Editor (where the domain is not reloaded between
/// plays). A stale pooled instance from a previous play would otherwise leak into the next one and
/// reference destroyed <c>GameObject</c>s.
///
/// This registry zeroes every registered pool/cache at the start of each play through
/// <see cref="RuntimeInitializeOnLoadMethodAttribute"/> (<c>BeforeSceneLoad</c>), and in the Editor on
/// domain reload — the same pattern <c>CombatBalance</c> uses for its cached config. It is a plain,
/// scene-free static type so the concrete pools can register their reset callbacks without this class
/// hardcoding references to types that are introduced by later tasks (3.1, 4.1, 5.1, 9.1).
///
/// Usage from a pooled system (type initializer or boot hook):
/// <code>
/// static ArsenalProjectile()
/// {
///     PoolResetRegistry.Register(() =>
///     {
///         Pool = null; // or Pool.Clear() — drop every pooled instance so the next play rebuilds it
///     });
/// }
/// </code>
/// </summary>
/// <remarks>Feature: project-cleanup-optimization, task 2.3. Requirements: 3.4, 10.2.</remarks>
public static class PoolResetRegistry
{
    // Ordered so resets run deterministically in registration order; a set-backed guard keeps a
    // callback from being registered (and therefore invoked) twice if a type initializer runs again.
    private static readonly List<Action> _resetCallbacks = new List<Action>();
    private static readonly HashSet<Action> _registered = new HashSet<Action>();

    /// <summary>Number of reset callbacks currently registered (exposed for tests/diagnostics).</summary>
    public static int CallbackCount => _resetCallbacks.Count;

    /// <summary>
    /// Registers a callback that clears a static pool/cache. Call this once from the owning type's
    /// static constructor (or a boot hook). Null callbacks and exact duplicates are ignored, so a
    /// double registration cannot cause a reset to run twice.
    /// </summary>
    public static void Register(Action resetCallback)
    {
        if (resetCallback == null) return;
        if (!_registered.Add(resetCallback)) return;
        _resetCallbacks.Add(resetCallback);
    }

    /// <summary>
    /// Removes a previously registered reset callback. Returns <c>true</c> when a callback was removed.
    /// Primarily useful for tests; gameplay pools register for the lifetime of the domain.
    /// </summary>
    public static bool Unregister(Action resetCallback)
    {
        if (resetCallback == null) return false;
        if (!_registered.Remove(resetCallback)) return false;
        _resetCallbacks.Remove(resetCallback);
        return true;
    }

    /// <summary>
    /// Invokes every registered reset callback. Each callback is isolated: a throw in one is logged and
    /// does not prevent the remaining pools from being reset, so one misbehaving pool cannot leave the
    /// others carrying stale state into the next play.
    /// </summary>
    public static void ResetAll()
    {
        // Iterate a snapshot so a callback that (re)registers during reset cannot mutate the list mid-loop.
        Action[] callbacks = _resetCallbacks.ToArray();
        for (int i = 0; i < callbacks.Length; i++)
        {
            try
            {
                callbacks[i]?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PoolResetRegistry] A pool reset callback threw and was skipped: {ex}");
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetAllOnPlay() => ResetAll();

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void ResetAllOnEditorReload() => ResetAll();
#endif
}
