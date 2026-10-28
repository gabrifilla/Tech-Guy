using System.Linq;

/// <summary>
/// Read-only map discovery. Rewards remain announced; destination availability never reveals an unvisited encounter.
/// Secret rooms from the legacy graph are omitted until its discovery authority reveals them.
/// </summary>
public static class RunMapDiscoveryPresentation
{
    private const string Unknown = "?";

    public static bool IsDiscovered(RunSessionState session, int nodeId)
    {
        if (session == null || session.Route.Node(nodeId) == null) return false;
        if (nodeId == session.Route.StartNode) return true;
        switch (session.NodeState(nodeId))
        {
            case RunNodeState.InProgress:
            case RunNodeState.RewardPending:
            case RunNodeState.Completed: return true;
            default: return false;
        }
    }

    public static string NodeLabel(RunSessionState session, int nodeId) =>
        IsDiscovered(session, nodeId) ? session.Route.Node(nodeId).Label : Unknown;

    public static string Title(RunSessionState session, int nodeId) =>
        IsDiscovered(session, nodeId) ? session.Route.Node(nodeId).Label : "SALA NÃO EXPLORADA";

    public static string RewardSummary(RunSessionState session, int nodeId)
    {
        var node = session?.Route.Node(nodeId);
        if (node == null) return Unknown;
        if (node.Kind == ManualNodeKind.Repair)
            return "Recuperação · até " + (node.RepairFraction * 100f).ToString("0") + "% da vida máxima";
        if (node.Kind == ManualNodeKind.Boss) return "Extração";
        return ManualRouteMapPresentation.RewardSummary(node);
    }

    public static string RewardAbbreviation(RunSessionState session, int nodeId)
    {
        var node = session?.Route.Node(nodeId);
        if (node == null) return Unknown;
        if (node.Kind == ManualNodeKind.Repair) return "RECUP.";
        if (node.Kind == ManualNodeKind.Boss) return "EXTRAÇÃO";
        return ManualRouteMapPresentation.RewardAbbreviation(node);
    }

    public static string EncounterSummary(RunSessionState session, int nodeId) =>
        IsDiscovered(session, nodeId) ? ManualRouteMapPresentation.EncounterSummary(session.Route.Node(nodeId)) : Unknown;

    public static string Purpose(RunSessionState session, int nodeId)
    {
        if (!IsDiscovered(session, nodeId)) return "Explore esta sala para revelar seu conteúdo.";
        var node = session.Route.Node(nodeId);
        switch (node.Kind)
        {
            case ManualNodeKind.Repair: return "Recupere vida sem enfrentar inimigos. Esta oportunidade pode ser usada ou dispensada uma vez.";
            case ManualNodeKind.Boss: return "Vença este confronto e volte ao Nexus pelo portal de extração.";
            case ManualNodeKind.Start: return "Explore o setor. As salas são identificadas quando você entra nelas.";
        }
        if (node.Reward == RunRewardKind.Mutation) return "Transforme uma ação da Manopla com a recompensa desta sala.";
        if (node.Reward == RunRewardKind.Protocol) return "Construa sua combinação de efeitos universais.";
        return "Conclua o encontro para receber a recompensa desta sala.";
    }

    public static string OpportunityCost(RunSessionState session, int nodeId)
    {
        if (session == null || session.Route.Node(nodeId) == null) return Unknown;
        var excluded = ManualRouteMapPresentation.ExcludedByChoice(session.Route, nodeId);
        if (excluded.Count == 0) return "Este trecho continua a rota sem descartar outro ramo.";
        return "Esta escolha descarta: " + string.Join(" → ", excluded.Select(id =>
            NodeLabel(session, id) + " (" + RewardSummary(session, id) + ")")) + ".";
    }

    public static string NextSteps(RunSessionState session, int nodeId)
    {
        if (session == null || session.Route.Node(nodeId) == null) return Unknown;
        return string.Join("\n", session.Route.Connections.Where(edge => edge.From == nodeId).Select(edge =>
            NodeLabel(session, edge.To) + " · " + RewardSummary(session, edge.To)));
    }

    public static string StateLabel(RunSessionState session, int nodeId)
    {
        if (session == null || session.Route.Node(nodeId) == null) return "INDISPONÍVEL";
        if (IsDiscovered(session, nodeId)) return ManualRouteMapPresentation.StateLabel(session, nodeId);
        switch (session.NodeState(nodeId))
        {
            case RunNodeState.Available: return "PRÓXIMO DESTINO";
            case RunNodeState.Committed: return "CAMINHO ESCOLHIDO";
            case RunNodeState.Excluded: return "ROTA DESCARTADA";
            default: return "NÃO EXPLORADA";
        }
    }

    public static string BlockingReason(RunSessionState session, int nodeId)
    {
        if (session == null || session.Route.Node(nodeId) == null) return "Destino indisponível.";
        if (IsDiscovered(session, nodeId)) return ManualRouteMapPresentation.BlockingReason(session, nodeId);
        if (session.IsClosed) return "A run foi encerrada.";
        if (session.NodeState(nodeId) == RunNodeState.Excluded) return "Este caminho foi descartado pela sua escolha anterior.";
        if (session.NodeState(nodeId) == RunNodeState.Committed) return "Passagem escolhida. Feche o mapa e siga até a sala.";
        if (session.Phase == RunPhase.RewardPending) return "Resolva a recompensa pendente para avançar.";
        if (session.Phase == RunPhase.Encounter || session.Phase == RunPhase.RepairPending)
            return "Conclua a sala atual antes de escolher outro caminho.";
        if (!ManualRouteMapPresentation.CanSelectDestination(session, nodeId))
            return "Avance pelas conexões para explorar este destino.";
        return "Confirme para abrir a passagem. Explore a sala para revelar seus detalhes.";
    }

    public static bool IsVisible(Room room) => room != null &&
        (room.Type != RoomType.Secret || room.Revealed);

    public static bool IsDiscovered(Room room) => IsVisible(room) &&
        (room.Type == RoomType.Start || room.Visited || room.Cleared);

    public static bool IsConnectionVisible(Room first, Room second) => IsVisible(first) && IsVisible(second);

    public static string NodeLabel(Room room)
    {
        if (!IsVisible(room)) return string.Empty;
        if (!IsDiscovered(room)) return Unknown;
        switch (room.Type)
        {
            case RoomType.Start: return "INÍCIO";
            case RoomType.Boss: return "BOSS";
            case RoomType.Treasure: return "TESOURO";
            case RoomType.Secret: return "SECRETA";
            default: return "COMBATE";
        }
    }

    public static string StateLabel(Room room)
    {
        if (!IsVisible(room)) return string.Empty;
        if (!IsDiscovered(room)) return "NÃO EXPLORADA";
        if (room.Cleared) return "CONCLUÍDA";
        return room.Type == RoomType.Start ? "INÍCIO" : "VISITADA";
    }
}
