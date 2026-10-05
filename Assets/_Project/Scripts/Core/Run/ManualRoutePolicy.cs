using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Local route authority. World owners validate observed facts before calling; no gameplay events run here.</summary>
public sealed class ManualRoutePolicy
{
    public RunSessionState Session { get; }
    public ManualRoutePolicy(ManualRouteSnapshot route) { Session = new RunSessionState(route); }
    public IReadOnlyList<int> AvailableDestinations => Session.Route.Connections
        .Where(e => e.From == Session.CurrentNode && Session.NodeState(e.To) == RunNodeState.Available)
        .Select(e => e.To).OrderBy(id => id).ToList().AsReadOnly();

    public bool TryCommitDestination(Guid sessionId, int nodeId, RunActionContext context)
    {
        if (!Authorize(sessionId, RunPhase.RouteChoice, context) || !AvailableDestinations.Contains(nodeId) ||
            (nodeId == Session.Route.BossNode && Session.MutationAcquisitions == 0)) return false;
        // All expected rejection checks precede mutation; branch identity never changes on revisit.
        foreach (var group in Session.Route.Groups)
        {
            if (group.DecisionNode != Session.CurrentNode) continue;
            if (Session.TryBranch(group.Id, out _)) return false;
        }
        foreach (var group in Session.Route.Groups)
        {
            if (group.DecisionNode != Session.CurrentNode) continue;
            Session.SetBranch(group.Id, nodeId);
            var excluded = group.FirstBranch[0] == nodeId ? group.SecondBranch : group.FirstBranch;
            foreach (int id in excluded) Session.SetNode(id, RunNodeState.Excluded);
        }
        Session.PendingDestination = nodeId; Session.SetNode(nodeId, RunNodeState.Committed);
        Session.Phase = RunPhase.Traversal;
        return true;
    }

    public bool TryEnterNode(Guid sessionId, int nodeId, RunActionContext context)
    {
        if (!Authorize(sessionId, RunPhase.Traversal, context) || Session.PendingDestination != nodeId ||
            Session.NodeState(nodeId) != RunNodeState.Committed) return false;
        var node = Session.Route.Node(nodeId);
        Session.CurrentNode = nodeId; Session.PendingDestination = -1; Session.RewardOrdinal = 0;
        Session.SetNode(nodeId, RunNodeState.InProgress);
        Session.Phase = node.Kind == ManualNodeKind.Repair ? RunPhase.RepairPending : RunPhase.Encounter;
        return true;
    }

    // The coordinator must supply a completed mechanical frontier, not just an enemy's Died event.
    public bool TryResolveEncounter(Guid sessionId, int nodeId, bool mechanicalFrontierComplete, RunActionContext context)
    {
        if (!Authorize(sessionId, RunPhase.Encounter, context) || !mechanicalFrontierComplete || Session.CurrentNode != nodeId) return false;
        var node = Session.Route.Node(nodeId);
        if (node.Kind == ManualNodeKind.Boss)
        { Session.SetNode(nodeId, RunNodeState.Completed); Session.Phase = RunPhase.FinalResolution; }
        else
        { Session.SetNode(nodeId, RunNodeState.RewardPending); Session.Phase = RunPhase.RewardPending; }
        return true;
    }

    // Called only after the contextual reward applicator has committed a real effect.
    public bool TryCompleteReward(Guid sessionId, int nodeId, int ordinal, bool acquiredMutation, RunActionContext context)
    {
        if (!Authorize(sessionId, RunPhase.RewardPending, context) || Session.CurrentNode != nodeId || Session.RewardOrdinal != ordinal) return false;
        var node = Session.Route.Node(nodeId);
        var kind = node.RewardAt(ordinal);
        if (kind == RunRewardKind.None || acquiredMutation && kind != RunRewardKind.Mutation) return false;
        if (kind == RunRewardKind.Mutation && !acquiredMutation && Session.MutationAcquisitions == 0) return false;
        if (kind == RunRewardKind.Protocol) Session.ProtocolAcquisitions++;
        if (acquiredMutation) Session.MutationAcquisitions++;
        Session.InventoryRevision++; Session.RewardOrdinal++;
        if (Session.RewardOrdinal == node.TotalAcquisitions) CompleteCurrentNode();
        return true;
    }

    // Healing/skip is performed by the Repair owner; policy records only the resolved opportunity.
    public bool TryCompleteRepair(Guid sessionId, int nodeId, RunActionContext context)
    {
        if (!Authorize(sessionId, RunPhase.RepairPending, context) || Session.CurrentNode != nodeId) return false;
        CompleteCurrentNode(); return true;
    }

    public bool TryFinalizeBoss(Guid sessionId, RunActionContext context)
    {
        if (sessionId != Session.Id || Session.Phase != RunPhase.FinalResolution) return false;
        if (!context.Alive) return TryEndRun(sessionId, RunOutcome.Dead, context);
        if (!context.CanConfirm) return false;
        Session.Phase = RunPhase.ExtractionAvailable; return true;
    }

    public bool TryRequestExtraction(Guid sessionId, RunActionContext context)
    {
        if (!Authorize(sessionId, RunPhase.ExtractionAvailable, context)) return false;
        return TryEndRun(sessionId, RunOutcome.Extracted, context);
    }

    public bool TryEndRun(Guid sessionId, RunOutcome outcome, RunActionContext context)
    {
        if (sessionId != Session.Id || Session.IsClosed || outcome == RunOutcome.None ||
            !Enum.IsDefined(typeof(RunOutcome), outcome) ||
            (outcome == RunOutcome.Dead && context.Alive) ||
            (outcome == RunOutcome.Extracted && (Session.Phase != RunPhase.ExtractionAvailable || !context.CanConfirm))) return false;
        Session.Outcome = outcome; Session.Phase = RunPhase.Returning; Session.PendingDestination = -1;
        return true;
    }

    public bool TryCompleteReturn(Guid sessionId)
    {
        if (sessionId != Session.Id || Session.Phase != RunPhase.Returning) return false;
        Session.Phase = RunPhase.Ended; return true;
    }

    private bool Authorize(Guid id, RunPhase phase, RunActionContext context) => id == Session.Id &&
        Session.Phase == phase && !Session.IsClosed && context.CanConfirm;
    private void CompleteCurrentNode()
    {
        Session.SetNode(Session.CurrentNode, RunNodeState.Completed);
        foreach (var edge in Session.Route.Connections)
            if (edge.From == Session.CurrentNode && Session.NodeState(edge.To) == RunNodeState.Locked)
                Session.SetNode(edge.To, RunNodeState.Available);
        Session.Phase = RunPhase.RouteChoice;
    }
}
