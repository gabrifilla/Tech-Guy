using System.Collections.Generic;

/// <summary>
/// A pure, framework-agnostic dirty-tracker for a single rendered UI value. It knows nothing about
/// Unity (no <c>MonoBehaviour</c>, no <c>TMP_Text</c>), so it is 100% testable in EditMode and forms
/// the reusable core behind the HUD's per-frame allocation elimination (task 7.1,
/// Property 3 — "the cache only (re)emits when the source value differs from the previous one").
///
/// Each cache guards one logical value (health text, mana text, a slot's cooldown ratio, the tooltip
/// string, the feedback string, ...). The owner feeds the current value every frame via
/// <see cref="HasChanged"/>; the cache answers whether that value differs from the last one it
/// accepted and, if so, records it. Only when it returns <c>true</c> should the caller rebuild the
/// interpolated string and reassign the TMP, so an unchanged value never allocates.
/// </summary>
/// <typeparam name="T">The value type being tracked (string, int, float, bool, ...).</typeparam>
public sealed class UiValueCache<T>
{
    private readonly IEqualityComparer<T> _comparer;
    private T _last;
    private bool _hasValue;

    /// <summary>
    /// Creates the cache. The optional comparer controls equality; the default comparer for
    /// <typeparamref name="T"/> is used when none is supplied.
    /// </summary>
    public UiValueCache(IEqualityComparer<T> comparer = null)
    {
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>True once the cache has accepted at least one value.</summary>
    public bool HasValue => _hasValue;

    /// <summary>The last value accepted by the cache (default of <typeparamref name="T"/> before any).</summary>
    public T Current => _last;

    /// <summary>
    /// Returns <c>true</c> and records <paramref name="value"/> when it differs from the last
    /// accepted value (or when no value has been accepted yet). Returns <c>false</c> and leaves the
    /// cache untouched when the value is unchanged. A <c>true</c> result is the signal to rebuild and
    /// reassign the UI text.
    /// </summary>
    public bool HasChanged(T value)
    {
        if (_hasValue && _comparer.Equals(_last, value))
            return false;

        _last = value;
        _hasValue = true;
        return true;
    }

    /// <summary>
    /// Forgets the cached value so the next <see cref="HasChanged"/> is guaranteed to report a
    /// change. Used when the owning view is (re)bound and must repaint from scratch.
    /// </summary>
    public void Invalidate()
    {
        _hasValue = false;
        _last = default;
    }
}
