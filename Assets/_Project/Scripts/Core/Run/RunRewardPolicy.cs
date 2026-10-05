using System;
using System.Collections.Generic;
using System.Linq;

public enum RunRewardTicketState { Pending, Committing, Resolved, Faulted }

public readonly struct RunRewardRequest : IEquatable<RunRewardRequest>
{
    public Guid SessionId { get; }
    public int NodeId { get; }
    public int Ordinal { get; }
    public RunRewardRequest(Guid sessionId, int nodeId, int ordinal)
    { SessionId = sessionId; NodeId = nodeId; Ordinal = ordinal; }
    public bool Equals(RunRewardRequest other) => SessionId == other.SessionId && NodeId == other.NodeId && Ordinal == other.Ordinal;
    public override bool Equals(object obj) => obj is RunRewardRequest other && Equals(other);
    public override int GetHashCode() => SessionId.GetHashCode() ^ NodeId * 397 ^ Ordinal;
}

public sealed class RunRewardTicket
{
    public RunRewardRequest Request { get; }
    public RunRewardKind AnnouncedKind { get; }
    public bool IsAlternative { get; internal set; }
    public RunRewardTicketState State { get; internal set; }
    public int Revision { get; internal set; }
    public IReadOnlyList<RunBoons.Offer> Choices { get; internal set; }
    public string ResultId { get; internal set; }
    public string Diagnostic { get; internal set; }
    internal string InventoryKey;
    internal RunRewardTicket(RunRewardRequest request, RunRewardKind kind)
    { Request = request; AnnouncedKind = kind; Choices = Array.Empty<RunBoons.Offer>(); }
}

/// <summary>Session-owned transactional tickets. Callbacks never run before the claim is recorded.</summary>
public sealed class RunRewardPolicy
{
    private readonly ManualRoutePolicy _route;
    private readonly Dictionary<RunRewardRequest, RunRewardTicket> _tickets = new Dictionary<RunRewardRequest, RunRewardTicket>();
    public RunRewardPolicy(ManualRoutePolicy route) { _route = route ?? throw new ArgumentNullException(nameof(route)); }
    public RunRewardTicket Find(RunRewardRequest request) => _tickets.TryGetValue(request, out var ticket) ? ticket : null;
    public bool Authorize(RunRewardRequest request, RunActionContext context) => context.CanConfirm &&
        !_route.Session.IsClosed && request.SessionId == _route.Session.Id && _route.Session.Phase == RunPhase.RewardPending &&
        request.NodeId == _route.Session.CurrentNode && request.Ordinal == _route.Session.RewardOrdinal;

    public RunRewardTicket Open(RunRewardRequest request, RunActionContext context, string inventoryKey,
        IReadOnlyList<RunBoons.Offer> eligible)
    {
        if (!Authorize(request, context) || eligible == null || inventoryKey == null) return null;
        var node = _route.Session.Route.Node(request.NodeId);
        var rewardKind = node.RewardAt(request.Ordinal);
        if (rewardKind == RunRewardKind.None || request.Ordinal < 0 || request.Ordinal >= node.TotalAcquisitions) return null;
        if (!_tickets.TryGetValue(request, out var ticket))
        { ticket = new RunRewardTicket(request, rewardKind); _tickets.Add(request, ticket); }
        if (ticket.State != RunRewardTicketState.Pending) return null;
        if (ticket.InventoryKey == inventoryKey) return ticket;
        if (ticket.InventoryKey != null)
        { ticket.Revision++; ticket.Diagnostic = "Inventory changed; pending offer rebuilt at revision " + ticket.Revision; }
        ticket.InventoryKey = inventoryKey;
        var ordered = eligible.GroupBy(o => o.Id).Select(g => g.First()).OrderBy(o => o.Id, StringComparer.Ordinal).ToList();
        ticket.IsAlternative = ordered.Count == 0 && (rewardKind != RunRewardKind.Mutation || _route.Session.MutationAcquisitions > 0);
        if (ticket.IsAlternative)
            ordered.Add(new RunBoons.Offer("bulwark", "ALTERNATIVA — DEFESA", "+40 Armor. Não cura e não conta como Mutation."));
        if (ordered.Count == 0) ticket.Diagnostic = "No eligible first Mutation; Boss remains blocked. Abort and return to Nexus.";
        // A derived seed, rather than shared RNG, makes reopening/revision independent of unrelated rolls.
        int seed = StableSeed(request.SessionId.ToString("N") + ":" + request.NodeId + ":" + request.Ordinal + ":" + ticket.Revision);
        var random = new Random(seed);
        for (int i = ordered.Count - 1; i > 0; i--)
        { int j = random.Next(i + 1); var swap = ordered[i]; ordered[i] = ordered[j]; ordered[j] = swap; }
        ticket.Choices = ordered.Take(3).ToList().AsReadOnly();
        return ticket;
    }

    public bool TryChoose(RunRewardRequest request, int revision, string id, RunActionContext context,
        Func<bool> stillEligible, Func<bool> apply, Action<RunRewardTicket> publish)
    {
        var ticket = Find(request);
        if (!Authorize(request, context) || ticket == null || ticket.State != RunRewardTicketState.Pending ||
            ticket.Revision != revision || !ticket.Choices.Any(o => o.Id == id) || stillEligible == null || apply == null || !stillEligible()) return false;
        ticket.State = RunRewardTicketState.Committing;
        try
        {
            if (!apply())
            { ticket.State = RunRewardTicketState.Pending; return false; }
            bool mutation = ticket.AnnouncedKind == RunRewardKind.Mutation && !ticket.IsAlternative;
            // Synchronous effect callbacks may kill/close the session; never force progression afterward.
            if (!_route.TryCompleteReward(request.SessionId, request.NodeId, request.Ordinal, mutation, context))
                throw new InvalidOperationException("Reward applied but session changed during commit.");
            ticket.ResultId = id; ticket.State = RunRewardTicketState.Resolved;
        }
        catch (Exception exception)
        { ticket.State = RunRewardTicketState.Faulted; ticket.Diagnostic = exception.ToString(); return false; }
        publish?.Invoke(ticket);
        return true;
    }

    private static int StableSeed(string value)
    { unchecked { int hash = 17; foreach (char c in value) hash = hash * 31 + c; return hash; } }
}
