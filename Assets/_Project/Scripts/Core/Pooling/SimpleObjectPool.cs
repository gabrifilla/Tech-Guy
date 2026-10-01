using System;
using System.Collections.Generic;

/// <summary>
/// A pure, framework-agnostic object pool. It knows nothing about Unity (no
/// <c>MonoBehaviour</c>, no scene, no <c>GameObject</c>), so it is 100% testable in
/// EditMode and forms the reusable core behind the gameplay pools
/// (<c>ComponentPool&lt;T&gt;</c> for projectiles and coins).
///
/// The pool hands out free instances when it has them and creates new ones through the
/// supplied <see cref="Func{T}"/> factory when it runs dry (expansion, never fails).
/// <see cref="CreatedCount"/> is a high-water-mark: it only ever grows, so a correctly
/// used pool whose <see cref="CreatedCount"/> stops growing proves instances are being
/// reused rather than leaked.
/// </summary>
/// <typeparam name="T">A reference type managed by the pool.</typeparam>
public sealed class SimpleObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Stack<T> _free = new Stack<T>();
    // Mirrors the contents of _free for O(1) membership checks so Release can cheaply
    // reject an item that is already parked as free (double-release guard).
    private readonly HashSet<T> _freeLookup = new HashSet<T>();

    /// <summary>
    /// Creates the pool.
    /// </summary>
    /// <param name="factory">
    /// Creates a brand-new instance. Required; must not return <c>null</c> if callers
    /// expect <see cref="Acquire"/> to never return <c>null</c>.
    /// </param>
    /// <param name="prewarm">Number of instances to create up front (optional).</param>
    public SimpleObjectPool(Func<T> factory, int prewarm = 0)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        for (int i = 0; i < prewarm; i++)
        {
            T item = _factory();
            if (item == null) continue;
            CreatedCount++;
            _free.Push(item);
            _freeLookup.Add(item);
        }
    }

    /// <summary>Total instances ever created by this pool (high-water-mark, never shrinks).</summary>
    public int CreatedCount { get; private set; }

    /// <summary>Instances currently parked and available for reuse.</summary>
    public int FreeCount => _free.Count;

    /// <summary>Instances currently handed out (derived: created minus free).</summary>
    public int LiveCount => CreatedCount - _free.Count;

    /// <summary>
    /// Returns a free instance if one is available, otherwise creates one via the factory
    /// (pool expansion). Never returns <c>null</c> as long as the factory does not.
    /// </summary>
    public T Acquire()
    {
        if (_free.Count > 0)
        {
            T reused = _free.Pop();
            _freeLookup.Remove(reused);
            return reused;
        }

        T created = _factory();
        if (created != null) CreatedCount++;
        return created;
    }

    /// <summary>
    /// Returns an instance to the free set for later reuse. Null items and items that are
    /// already parked as free (duplicates) are ignored, so a double release cannot corrupt
    /// the pool's counts.
    /// </summary>
    public void Release(T item)
    {
        if (item == null) return;
        if (!_freeLookup.Add(item)) return;
        _free.Push(item);
    }
}
