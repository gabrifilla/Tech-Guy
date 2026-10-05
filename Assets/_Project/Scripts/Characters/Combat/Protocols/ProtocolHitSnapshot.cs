using UnityEngine;

/// <summary>Captured before HP and death cleanup; never read elements from a recycled target.</summary>
public readonly struct ProtocolHitSnapshot
{
    private readonly ProtocolImpactClaim _claim;
    internal bool TryClaim() => _claim != null && _claim.TryClaim();
    public ProtocolTargetHandle Handle { get; }
    public Vector3 Position { get; }
    public bool Burn { get; }
    public bool Chill { get; }
    public float Health { get; }
    public CombatAffiliation Affiliation { get; }
    public bool IsLethal(float confirmedDamage) => Health > 0 && confirmedDamage >= Health;
    public ProtocolHitSnapshot(Actor actor)
    {
        _claim = new ProtocolImpactClaim();
        Handle = new ProtocolTargetHandle(EntityId.ToULong(actor.GetEntityId()), actor.CombatGeneration);
        Position = actor.transform.position;
        Health = actor.health;
        Affiliation = actor.Affiliation;
        Burn = ProtocolElementQuery.HasBurn(actor); Chill = ProtocolElementQuery.HasChill(actor);
    }
}

/// <summary>Copies of the immutable receipt share one admission claim; roots retain no growing receipt history.</summary>
internal sealed class ProtocolImpactClaim
{
    private bool _claimed;
    internal bool TryClaim() { if (_claimed) return false; _claimed = true; return true; }
}
