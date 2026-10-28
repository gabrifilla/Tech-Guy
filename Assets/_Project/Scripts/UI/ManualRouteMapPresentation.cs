using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Read-only route projection. It never opens a gate or changes the session.</summary>
public sealed class ManualRouteMapLayout
{
    private readonly Dictionary<int, Vector2> _points = new Dictionary<int, Vector2>();
    private readonly List<int> _ordered = new List<int>();
    public IReadOnlyList<int> OrderedNodes => _ordered;
    public Vector2 Point(int nodeId) => _points[nodeId];

    public ManualRouteMapLayout(ManualRouteSnapshot route)
    {
        if (route == null) throw new ArgumentNullException(nameof(route));
        var ranks = route.Nodes.ToDictionary(n => n.Id, _ => 0);
        // Snapshot validation guarantees a DAG. Longest-path ranks keep every edge moving forward,
        // including reconnection after a branch with an extra encounter.
        for (int pass = 0; pass < route.Nodes.Count; pass++)
        {
            bool changed = false;
            foreach (var edge in route.Connections)
            {
                int rank = ranks[edge.From] + 1;
                if (ranks[edge.To] >= rank) continue;
                ranks[edge.To] = rank;
                changed = true;
            }
            if (!changed) break;
        }
        var lanes = route.Nodes.ToDictionary(n => n.Id, _ => 0f);
        foreach (var group in route.Groups)
        {
            foreach (int id in group.FirstBranch) lanes[id] = -1f;
            foreach (int id in group.SecondBranch) lanes[id] = 1f;
        }
        int maximum = Mathf.Max(1, ranks.Values.Max());
        foreach (var layer in route.Nodes.GroupBy(n => ranks[n.Id]))
        {
            var siblings = layer.OrderBy(n => lanes[n.Id]).ThenBy(n => n.Id).ToList();
            for (int i = 0; i < siblings.Count; i++)
            {
                var node = siblings[i];
                // Unclassified parallel nodes still receive separate positions.
                float lane = siblings.Count > 1 ? (i - (siblings.Count - 1) * .5f) * 2f : lanes[node.Id];
                _points.Add(node.Id, new Vector2((float)ranks[node.Id] / maximum, .5f + lane * .24f));
            }
        }
        _ordered.AddRange(route.Nodes.OrderBy(n => ranks[n.Id]).ThenBy(n => _points[n.Id].y).ThenBy(n => n.Id).Select(n => n.Id));
    }
}

public static class ManualRouteMapPresentation
{
    public static bool CanSelectDestination(RunSessionState session, int nodeId)
    {
        if (session == null || session.IsClosed || session.Phase != RunPhase.RouteChoice ||
            session.NodeState(nodeId) != RunNodeState.Available ||
            nodeId == session.Route.BossNode && session.MutationAcquisitions == 0) return false;
        return session.Route.Connections.Any(e => e.From == session.CurrentNode && e.To == nodeId);
    }

    public static string RewardSummary(ManualNodeDefinition node)
    {
        if (node.Kind == ManualNodeKind.Repair) return "Repair";
        if (node.Kind == ManualNodeKind.Boss) return "Boss / extração";
        if (node.Kind == ManualNodeKind.Start) return "Início da rota";
        if (node.RewardSteps != null && node.RewardSteps.Count > 0)
            return string.Join(" + ", node.RewardSteps.Select(s => Quantity(s.Quantity, s.Kind)));
        return Quantity(node.Acquisitions, node.Reward);
    }

    public static string RewardAbbreviation(ManualNodeDefinition node)
    {
        if (node.Kind == ManualNodeKind.Repair) return "REPAIR";
        if (node.Kind == ManualNodeKind.Boss) return "BOSS";
        if (node.Kind == ManualNodeKind.Start) return "INÍCIO";
        if (node.RewardSteps != null && node.RewardSteps.Count > 0)
            return string.Join(" + ", node.RewardSteps.Select(s => (s.Quantity > 1 ? s.Quantity + " " : "") + Abbreviation(s.Kind)));
        return (node.Acquisitions > 1 ? node.Acquisitions + " " : "") + Abbreviation(node.Reward);
    }

    public static string StateLabel(RunSessionState session, int id)
    {
        switch (session.NodeState(id))
        {
            case RunNodeState.Excluded: return "ROTA DESCARTADA";
            case RunNodeState.Committed: return "CAMINHO ESCOLHIDO";
            case RunNodeState.InProgress: return session.Route.Node(id).Kind == ManualNodeKind.Repair ? "REPARO PENDENTE" : "EM COMBATE";
            case RunNodeState.RewardPending: return "RECOMPENSA PENDENTE";
            case RunNodeState.Completed: return id == session.CurrentNode ? "VOCÊ ESTÁ AQUI" : "CONCLUÍDA";
            case RunNodeState.Available: return "PRÓXIMO DESTINO";
            default: return "MAIS ADIANTE";
        }
    }

    public static string BlockingReason(RunSessionState session, int id)
    {
        if (session.NodeState(id) == RunNodeState.Excluded) return "Este ramo foi descartado pela sua escolha anterior.";
        if (session.NodeState(id) == RunNodeState.Completed) return "Sala concluída. Voltar não concede outra recompensa.";
        if (session.NodeState(id) == RunNodeState.Committed) return "Passagem escolhida. Feche o mapa e caminhe até esta sala.";
        if (session.Phase == RunPhase.RewardPending) return "Resolva a recompensa pendente para avançar.";
        if (session.Phase == RunPhase.RepairPending) return "Use ou dispense o Repair para avançar.";
        if (session.Phase == RunPhase.Encounter) return "Conclua o combate atual para escolher a próxima sala.";
        if (session.IsClosed) return "A run foi encerrada.";
        if (id == session.Route.BossNode && session.MutationAcquisitions == 0)
            return "Adquira uma Mutation nesta run antes de enfrentar o Boss.";
        if (!CanSelectDestination(session, id)) return "Destino futuro. Avance pelas conexões para chegar até ele.";
        return "Confirme para abrir a passagem. Depois, caminhe até a sala.";
    }

    public static IReadOnlyList<int> ExcludedByChoice(ManualRouteSnapshot route, int nodeId)
    {
        foreach (var group in route.Groups)
        {
            if (group.FirstBranch[0] == nodeId) return group.SecondBranch;
            if (group.SecondBranch[0] == nodeId) return group.FirstBranch;
        }
        return Array.Empty<int>();
    }

    public static string OpportunityCost(ManualRouteSnapshot route, int id)
    {
        var excluded = ExcludedByChoice(route, id);
        return excluded.Count == 0 ? "Este trecho continua a rota sem descartar outro ramo." :
            "Esta escolha descarta: " + string.Join(" → ", excluded.Select(n => route.Node(n).Label + " (" + RewardSummary(route.Node(n)) + ")")) + ".";
    }

    public static string EncounterSummary(ManualNodeDefinition node)
    {
        if (node.Kind == ManualNodeKind.Start) return "Planeje seu percurso até o Boss.";
        if (node.Kind == ManualNodeKind.Repair) return "Sem combate · um uso · até " + (node.RepairFraction * 100f).ToString("0") + "% da vida máxima.";
        int elites = node.Spawns.Count(s => s.Rank == EnemyRank.Elite);
        return node.Kind == ManualNodeKind.Boss ? "Confronto final. A vitória libera a extração." :
            node.Spawns.Count + " adversários" + (elites > 0 ? " · " + elites + " Elite · risco elevado" : " · combate normal") + ".";
    }

    public static string NextSteps(RunSessionState session, int id)
    {
        var next = session.Route.Connections.Where(e => e.From == id).Select(e => session.Route.Node(e.To));
        return string.Join("\n", next.Select(n => n.Label + " · " + RewardSummary(n) +
            (session.NodeState(n.Id) == RunNodeState.Excluded ? " (descartada)" : "")));
    }

    private static string Quantity(int count, RunRewardKind kind) => (count > 1 ? count + " " : "") + kind;
    private static string Abbreviation(RunRewardKind kind) => kind == RunRewardKind.Technique ? "TEC" :
        kind == RunRewardKind.Protocol ? "PRO" : kind == RunRewardKind.Mutation ? "MUT" : "—";
}
