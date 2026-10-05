using System;

/// <summary>One session owns ranks. Claims are sealed before publication; reentry never reapplies an acquisition.</summary>
public sealed class ProtocolInventory
{
    private readonly int[] _ranks = new int[7];
    private bool _committing;
    private bool _ended;
    public Guid SessionId { get; private set; }
    public ProtocolCatalogSnapshot Catalog { get; }
    public int Revision { get; private set; }
    public bool IsFaulted { get; private set; }
    public bool IsEnded => _ended;
    public ProtocolInventory(Guid sessionId, ProtocolCatalogSnapshot catalog)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Missing session.", nameof(sessionId));
        SessionId = sessionId; Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }
    public int Rank(ProtocolKind kind) => (int)kind >= 0 && (int)kind < _ranks.Length ? _ranks[(int)kind] : 0;
    public bool TryBindFreshSession(Guid sessionId)
    {
        if (_ended || _committing || IsFaulted || Revision != 0 || sessionId == Guid.Empty) return false;
        SessionId = sessionId; return true;
    }
    public bool CanAcquire(ProtocolKind kind)
    {
        if (_ended || _committing || IsFaulted || !Enum.IsDefined(typeof(ProtocolKind), kind)) return false;
        var definition = Catalog.Get(kind);
        bool element = Rank(ProtocolKind.Ignite) > 0 || Rank(ProtocolKind.Frost) > 0;
        bool propagation = Rank(ProtocolKind.Conductor) > 0 || Rank(ProtocolKind.Detonation) > 0;
        return Rank(kind) < definition.MaxRank && (!definition.RequiresIgnite || Rank(ProtocolKind.Ignite) > 0) &&
            (!definition.RequiresElement || element) && (!definition.RequiresPropagation || propagation);
    }
    public bool TryAcquire(Guid sessionId, ProtocolKind kind, int expectedRevision, Func<int, bool> apply)
    {
        if (sessionId != SessionId || expectedRevision != Revision || apply == null || !CanAcquire(kind)) return false;
        _committing = true;
        try
        {
            int next = Rank(kind) + 1;
            if (!apply(next)) return false;
            if (_ended) { IsFaulted = true; return false; }
            _ranks[(int)kind] = next; Revision = checked(Revision + 1); return true;
        }
        catch { IsFaulted = true; throw; }
        finally { _committing = false; }
    }
    public void EndSession()
    { _ended = true; Array.Clear(_ranks, 0, _ranks.Length); }
    public static bool TryKind(string id, out ProtocolKind kind)
    {
        foreach (ProtocolKind candidate in Enum.GetValues(typeof(ProtocolKind)))
            if (ProtocolCatalogValidator.IdFor(candidate) == id) { kind = candidate; return true; }
        kind = default; return false;
    }
    public string InventoryKey()
        => SessionId.ToString("N") + ":" + Catalog.Revision + ":" + Revision + ":" + string.Join(",", _ranks);
}
