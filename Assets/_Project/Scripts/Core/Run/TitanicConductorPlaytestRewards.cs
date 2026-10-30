using System.Collections.Generic;
using System.Linq;

/// <summary>Temporary Editor-only reward assistance, captured per run. Never edits authored assets.</summary>
public static class TitanicConductorPlaytestRewards
{
    public const string SessionKey = "TechGuy.Playtest.TitanicConductorRewards";
    private const string MenuPath = "Tools/Tech Guy/Testar Tornado + Conductor";
    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool(SessionKey, false);
#else
            return false;
#endif
        }
    }

    internal static ManualRouteSnapshot Apply(ManualRouteSnapshot source, WeaponScript weapon)
    {
        if (!Enabled || source == null || !weapon || WeaponRunModifiers.Identify(weapon) != RunWeaponFamily.Gauntlet) return source;
        var nodes = source.Nodes.Select(node =>
        {
            if (node.Kind != ManualNodeKind.Combat && node.Kind != ManualNodeKind.Elite) return node.Copy();
            var copy = new ManualNodeDefinition(node.Id, node.Label, node.Kind, RunRewardKind.Mutation, 2,
                node.Center, node.ArenaSize, node.RepairFraction, node.Spawns);
            copy.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Mutation, 1),
                new ManualRewardStep(RunRewardKind.Protocol, 1) });
            return copy;
        }).ToArray();
        return new ManualRouteSnapshot(source, nodes, titanicConductorPlaytest: true);
    }

    internal static IReadOnlyList<RunBoons.Offer> Prioritize(ManualRouteSnapshot route, RunRewardKind kind,
        IReadOnlyList<RunBoons.Offer> eligible, ProtocolInventory protocols)
    {
        if (!route.TitanicConductorPlaytest) return eligible;
        string wanted = kind == RunRewardKind.Mutation ? TitanicFistsDefinition.Id :
            kind == RunRewardKind.Protocol && protocols.Rank(ProtocolKind.Conductor) == 0 ? "conductor" : null;
        var offer = eligible.FirstOrDefault(candidate => candidate.Id == wanted);
        return offer == null ? eligible : new[] { offer };
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem(MenuPath)]
    private static void Toggle()
    {
        UnityEditor.SessionState.SetBool(SessionKey, !Enabled);
        UnityEngine.Debug.Log("Teste Tornado + Conductor: " + (Enabled ? "ATIVADO" : "DESATIVADO") +
            ". Vale para novas incursões da Manopla; uma Mutação e um Protocolo por sala de combate.");
    }
    [UnityEditor.MenuItem(MenuPath, true)]
    private static bool ValidateMenu() { UnityEditor.Menu.SetChecked(MenuPath, Enabled); return true; }
#endif
}
