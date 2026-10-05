using System;
using UnityEngine;

/// <summary>HP already confirmed. Derived listeners receive origin without masquerading as native OnHit.</summary>
public readonly struct ProtocolCombatEvent
{
    public Guid SessionId { get; }
    public long RootId { get; }
    public int EncounterId { get; }
    public ProtocolTargetHandle Target { get; }
    public ProtocolProducer Producer { get; }
    public int Depth { get; }
    public float Damage { get; }
    public Vector3 Position { get; }
    public bool IsDeath { get; }
    public ProtocolCombatEvent(ProtocolRootContext root, ProtocolHitSnapshot before, ProtocolProducer producer, int depth, float damage)
    {
        SessionId = root.SessionId; RootId = root.RootId; EncounterId = root.EncounterId; Target = before.Handle;
        Producer = producer; Depth = depth; Damage = damage; Position = before.Position; IsDeath = before.IsLethal(damage);
    }
}
