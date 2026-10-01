/// <summary>
/// A pure, framework-agnostic counter of consecutive basic hits. It knows nothing about Unity
/// (no <c>MonoBehaviour</c>, no <c>Time</c>, no scene), so it is 100% testable in EditMode and
/// forms the reusable core behind the "Guarda partida" (<c>GuardBreaker</c>, R5) boon.
///
/// The caller reports each landed basic hit with <see cref="RegisterBasicHit"/>. The counter
/// fires (returns <c>true</c>) on the hit that completes a run of <c>threshold</c> hits and then
/// resets itself so the next run starts clean. Any interruption — a skill cast or a frame with no
/// basic hit — is reported with <see cref="Reset"/>, which clears the running count.
///
/// A <c>threshold</c> of <c>1</c> (or less) is clamped to a minimum of <c>1</c>, so a
/// misconfigured value can never stall the counter forever.
/// </summary>
public sealed class ConsecutiveHitCounter
{
    private readonly int _threshold;

    /// <summary>Number of consecutive basic hits registered since the last fire or reset.</summary>
    public int Count { get; private set; }

    /// <summary>
    /// Creates the counter.
    /// </summary>
    /// <param name="threshold">
    /// Number of consecutive basic hits that completes a run. Values below <c>1</c> are clamped to
    /// <c>1</c>, so the counter can never be configured to never fire.
    /// </param>
    public ConsecutiveHitCounter(int threshold) => _threshold = System.Math.Max(1, threshold);

    /// <summary>
    /// Registers a landed basic hit. Returns <c>true</c> when THIS hit completes the run (R5.1),
    /// resetting the count so the next run starts from zero; otherwise returns <c>false</c>.
    /// </summary>
    public bool RegisterBasicHit()
    {
        Count++;
        if (Count >= _threshold)
        {
            Count = 0;
            return true;
        }

        return false;
    }

    /// <summary>Clears the running count. Any interruption (skill, frame without a basic) resets it (R5.3).</summary>
    public void Reset() => Count = 0;

    /// <summary>
    /// Stance-damage multiplier applied by the run-completing hit (R5.1): <c>1 + 0.5 * rank</c>.
    /// Negative ranks are clamped to <c>0</c>.
    /// </summary>
    public static float StanceMultiplier(int rank) => 1f + 0.5f * System.Math.Max(0, rank);
}
