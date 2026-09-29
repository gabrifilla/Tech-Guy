/// <summary>
/// Contract for a component that intercepts incoming damage before it reaches an
/// <see cref="Actor"/>'s health. <see cref="Actor.TakeDamage"/> offers the incoming amount to
/// an attached absorber first; the absorber consumes what it can and returns the leftover, which
/// is the only portion that reduces health.
/// </summary>
/// <remarks>
/// The <c>Shield</c> companion component (feature enemy-swarm-core-archetypes, task 2.2)
/// implements this interface. Defining the hook as an interface keeps <see cref="Actor"/>
/// decoupled from any concrete absorber and lets the shield remain invisible to every other
/// system.
/// </remarks>
public interface IDamageAbsorber
{
    /// <summary>
    /// Absorbs as much of <paramref name="amount"/> as the buffer allows and returns the
    /// leftover damage that should still be applied to health. Implementations must return a
    /// value in the range <c>[0, amount]</c> and never increase the incoming damage.
    /// </summary>
    float Absorb(float amount);
}
