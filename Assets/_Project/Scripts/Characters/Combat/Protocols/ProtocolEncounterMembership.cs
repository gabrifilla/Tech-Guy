using System;
using UnityEngine;

/// <summary>Transient encounter identity injected by the manual spawner, never inferred from hierarchy names.</summary>
public sealed class ProtocolEncounterMembership : MonoBehaviour
{
    private Guid _sessionId;
    private int _encounterId;
    public void Configure(Guid sessionId, int encounterId)
    {
        if (sessionId == Guid.Empty || encounterId < 0) throw new ArgumentOutOfRangeException(nameof(encounterId));
        _sessionId = sessionId; _encounterId = encounterId;
    }
    public bool Matches(Guid sessionId, int encounterId) => _sessionId == sessionId && _encounterId == encounterId;
}
