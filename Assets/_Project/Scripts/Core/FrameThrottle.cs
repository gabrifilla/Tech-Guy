/// <summary>
/// A pure, framework-agnostic frame throttle. It knows nothing about Unity (no
/// <c>MonoBehaviour</c>, no <c>Time</c>, no scene), so it is 100% testable in EditMode and
/// forms the reusable core behind rate-limited per-frame work such as the
/// <c>OutlineScript</c> raycast.
///
/// The caller advances the throttle once per frame with <see cref="Tick"/>. The throttle
/// fires (returns <c>true</c>) on the very first tick and then at most once every
/// <see cref="Interval"/> ticks. This guarantees two things the property test (P6) relies on:
/// the first tick is never missed, and no two fires happen closer than <see cref="Interval"/>
/// ticks apart.
///
/// An <see cref="Interval"/> of <c>1</c> (or less) means "fire every tick"; it is clamped to a
/// minimum of <c>1</c> so a misconfigured value can never stall the throttle forever.
/// </summary>
public sealed class FrameThrottle
{
    private readonly int _interval;

    // Ticks counted since the last fire. Starts high enough that the first Tick fires, so the
    // first tick is never missed regardless of the configured interval.
    private int _sinceLastFire;
    private bool _hasFired;

    /// <summary>
    /// Creates the throttle.
    /// </summary>
    /// <param name="interval">
    /// Minimum number of ticks between two fires. Values below <c>1</c> are clamped to <c>1</c>
    /// ("fire every tick"), so the throttle can never be configured into a permanent stall.
    /// </param>
    public FrameThrottle(int interval)
    {
        _interval = interval < 1 ? 1 : interval;
    }

    /// <summary>The clamped firing interval in ticks (always &gt;= 1).</summary>
    public int Interval => _interval;

    /// <summary>Number of ticks counted since the last fire (0 immediately after a fire).</summary>
    public int TicksSinceLastFire => _sinceLastFire;

    /// <summary>
    /// Advances the throttle by one frame. Returns <c>true</c> when the throttled work should run
    /// this frame: always on the first call, then only once at least <see cref="Interval"/> ticks
    /// have elapsed since the previous fire.
    /// </summary>
    public bool Tick()
    {
        if (!_hasFired)
        {
            _hasFired = true;
            _sinceLastFire = 0;
            return true;
        }

        _sinceLastFire++;
        if (_sinceLastFire >= _interval)
        {
            _sinceLastFire = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resets the throttle to its initial state so the next <see cref="Tick"/> fires again (useful
    /// when a reused instance should behave like a freshly created one).
    /// </summary>
    public void Reset()
    {
        _hasFired = false;
        _sinceLastFire = 0;
    }
}
