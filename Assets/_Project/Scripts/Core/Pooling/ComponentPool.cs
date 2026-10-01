using System;
using UnityEngine;

/// <summary>
/// Unity adapter around <see cref="SimpleObjectPool{T}"/> for pooling <see cref="Component"/>
/// instances (projectiles, coins, ...). It mirrors the lazy pool-root pattern already used by
/// <c>HitboxDamage</c> (<c>EnsureEffectPoolRoot</c>): a single <c>GameObject</c> named
/// "<c>&lt;Name&gt;Pool</c>" is created on demand and every pooled instance is parented under it.
///
/// The adapter replaces the <c>Instantiate</c>/<c>Destroy</c> pair with activate-on-acquire /
/// deactivate-on-release and exposes no new state to gameplay. The supplied factory builds the
/// <c>GameObject</c>+<typeparamref name="T"/> once per instance; the supplied reset action runs on
/// every acquire so a reused instance behaves like a freshly created one.
/// </summary>
/// <typeparam name="T">The pooled component type.</typeparam>
public sealed class ComponentPool<T> where T : Component
{
    private readonly Func<T> _factory;
    private readonly Action<T> _reset;
    private readonly string _rootName;
    private readonly SimpleObjectPool<T> _pool;

    private Transform _root;

    /// <summary>
    /// Creates the pool.
    /// </summary>
    /// <param name="factory">
    /// Creates a brand-new instance (GameObject + <typeparamref name="T"/>, including any owned
    /// Material/LineRenderer). Called once per instance; its result is parented under the lazy root
    /// and deactivated. Required.
    /// </param>
    /// <param name="reset">
    /// Called on every <see cref="Acquire"/> to return the instance to its "just created" state.
    /// Optional; may be <c>null</c> when no reset is needed.
    /// </param>
    /// <param name="rootName">
    /// Base name of the lazy pool root GameObject. The root is named "<c>&lt;rootName&gt;Pool</c>"
    /// to mirror the "HitEffectPool" convention. Falls back to the type name when null/empty.
    /// </param>
    public ComponentPool(Func<T> factory, Action<T> reset = null, string rootName = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;
        _rootName = string.IsNullOrEmpty(rootName) ? typeof(T).Name : rootName;
        _pool = new SimpleObjectPool<T>(CreateParented);
    }

    /// <summary>Total instances ever created by this pool (high-water-mark, never shrinks).</summary>
    public int CreatedCount => _pool.CreatedCount;

    /// <summary>Instances currently parked and available for reuse.</summary>
    public int FreeCount => _pool.FreeCount;

    /// <summary>Instances currently handed out (derived: created minus free).</summary>
    public int LiveCount => _pool.LiveCount;

    /// <summary>
    /// Returns a ready-to-use instance: reuses a parked one or creates a new one through the factory
    /// (expansion, never fails as long as the factory does not). The reset action runs and the
    /// GameObject is activated before the instance is handed out.
    /// </summary>
    public T Acquire()
    {
        T instance = _pool.Acquire();
        if (instance == null) return null;

        _reset?.Invoke(instance);
        instance.gameObject.SetActive(true);
        return instance;
    }

    /// <summary>
    /// Deactivates the instance and returns it to the pool for later reuse. Null items are ignored;
    /// the underlying pool guards against double release. The instance keeps its owned components
    /// (Material/LineRenderer) so nothing is destroyed per use.
    /// </summary>
    public void Release(T instance)
    {
        if (instance == null) return;

        if (instance.gameObject.activeSelf)
        {
            instance.gameObject.SetActive(false);
        }

        _pool.Release(instance);
    }

    private T CreateParented()
    {
        T instance = _factory();
        if (instance == null) return null;

        EnsureRoot();
        instance.transform.SetParent(_root, false);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void EnsureRoot()
    {
        if (_root != null) return;

        var root = new GameObject(_rootName + "Pool");
        _root = root.transform;
    }
}
