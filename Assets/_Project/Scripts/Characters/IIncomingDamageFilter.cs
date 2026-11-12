/// <summary>
/// Optional per-actor hook that can reduce or cancel the HP damage of an incoming hit before modifiers and
/// absorbers (Etapa 3: the Guarda's immune front). Only HP is filtered; posture/stance is a separate channel.
/// </summary>
public interface IIncomingDamageFilter
{
    /// <summary>Returns the HP damage that should actually apply (0 = fully blocked).</summary>
    float FilterIncomingDamage(float amount);
}
