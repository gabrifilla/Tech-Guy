using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Room packs (enemy review 10/11/2026; approved after playtest 11/11/2026 and made the default): normal combat rooms get
/// a role "pack" instead of the uniform draw. On in builds; in the Editor it can be switched off to compare
/// (menu: Tech Guy ▸ Protótipo ▸ Salas-pacote). Room ids rotate Fornalha (id % 3 == 1), Anzol (== 2) and Mira (== 0,
/// added 11/11/2026 so Shooter/SpreadShooter appear outside elite rooms). Elite and Boss rooms are untouched. Swarm and
/// Rush attack outside the melee token cap in pack rooms.
/// </summary>
public sealed partial class ProgressionDirector
{
    public const string RoomPacksPrefKey = "TechGuy.Prototype.RoomPacks";
    private static readonly ArchetypeId[] PackFodder = { ArchetypeId.Swarm, ArchetypeId.Rush };

    private readonly Dictionary<int, string> _roomPackByNode = new Dictionary<int, string>();

    private static bool RoomPacksEnabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(RoomPacksPrefKey, true);
#else
            return true;
#endif
        }
    }

    /// <summary>Name of the pack in <paramref name="nodeId"/> this run, or null.</summary>
    public string RoomPackFor(int nodeId) => _roomPackByNode.TryGetValue(nodeId, out var pack) ? pack : null;

    private void ApplyPrototypeRoomPacks(ManualRouteSnapshot route)
    {
        _roomPackByNode.Clear();
        if (!RoomPacksEnabled || route == null) return;
        foreach (var node in route.Nodes)
        {
            if (node.Kind != ManualNodeKind.Combat) continue;
            string packName = PackName(node.Id);
            var roster = packName == "Fornalha"
                ? Pack((ArchetypeId.Heavy, 1), (ArchetypeId.Swarm, 9), (ArchetypeId.HazardCaster, 1))
                : packName == "Anzol"
                    // Hooker added back (Etapa 2, 11/11/2026): its hook drags the player into the Rush and the bomb.
                    ? Pack((ArchetypeId.Charger, 1), (ArchetypeId.Hooker, 1), (ArchetypeId.Bomber, 1), (ArchetypeId.Rush, 4))
                    : Pack((ArchetypeId.Grunt, 3), (ArchetypeId.Shooter, 2), (ArchetypeId.SpreadShooter, 1));
            if (roster == null) { Debug.LogWarning("[Salas-pacote] Prefab de arquétipo ausente; sala " + node.Id + " mantida.", this); continue; }
            var placed = PlacePack(node, roster);
            if (placed == null) { Debug.LogWarning("[Salas-pacote] Sem posições seguras na sala " + node.Id + "; mantida.", this); continue; }
            node.ConfigurePrototypeSpawns(placed);
            _roomPackByNode[node.Id] = packName;
        }

    }

    /// <summary>Fornalha, Anzol and Mira in rotation by room id.</summary>
    public static string PackName(int nodeId) => nodeId % 3 == 1 ? "Fornalha" : nodeId % 3 == 2 ? "Anzol" : "Mira";

    private List<ManualEnemySpawn> Pack(params (ArchetypeId id, int count)[] entries)
    {
        var result = new List<ManualEnemySpawn>();
        foreach (var (id, count) in entries)
        {
            var variant = _archetypePrefabs?.FirstOrDefault(v => v && v.ArchetypeId == id);
            if (!variant) return null;
            for (int i = 0; i < count; i++) result.Add(new ManualEnemySpawn(variant.gameObject, Vector3.zero, EnemyRank.Normal));
        }
        return result;
    }

    /// <summary>Spatial rooms reuse their own safe-slot arrangement; rectangles use a 3×4 grid kept off the covers.</summary>
    private static List<ManualEnemySpawn> PlacePack(ManualNodeDefinition node, List<ManualEnemySpawn> roster)
    {
        if (node.SpatialLayout != null)
        {
            try { return node.SpatialLayout.Arrange(roster); }
            catch (System.InvalidOperationException) { return null; }
        }
        float halfX = node.ArenaSize.x * .5f - 3f, halfZ = node.ArenaSize.y * .5f - 3f;
        var slots = new List<Vector3>();
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 4; column++)
                slots.Add(new Vector3(-halfX + (column + .5f) * halfX * .5f, 0f, -halfZ + (row + .5f) * halfZ * (2f / 3f)));
        // Backline roles take the far rows, pressure roles the near ones.
        var ordered = roster.OrderBy(s => FirstSectorEncounterLayout.IsBackline(s.Prefab) ? 1 : 0).ToList();
        if (ordered.Count > slots.Count) return null;
        var result = new List<ManualEnemySpawn>();
        for (int i = 0; i < ordered.Count; i++)
        {
            int row = FirstSectorEncounterLayout.IsBackline(ordered[i].Prefab) ? 2 : i / 4;
            var slot = slots.Where(p => !result.Any(r => (r.Offset - p).sqrMagnitude < .01f))
                .OrderBy(p => Mathf.Abs(p.z - slots[row * 4].z)).First();
            result.Add(new ManualEnemySpawn(ordered[i].Prefab, slot, ordered[i].Rank));
        }
        return FirstSectorContentDefinition.SafeSpawns(result, node.ArenaSize);
    }

    private void ConfigurePackCoordinator(SwarmAttackCoordinator coordinator, int nodeId)
    {
        if (coordinator && _roomPackByNode.ContainsKey(nodeId)) coordinator.ConfigureUncapped(PackFodder);
    }
}
