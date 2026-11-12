using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Enemy variety on top of the drawn rosters and room packs (Etapa 3, 11/11/2026). Runs after the packs, with its
/// own seeded stream (the draw no longer includes the SpreadShooter, Emissor or Fragmento as enemies of their own):
/// <list type="bullet">
/// <item>Shooter · Leque replaces the SpreadShooter as its own archetype: 1 in 3 drawn Shooters (and any leftover
/// SpreadShooter, converted to a Shooter) fire the fan; a non-Leque Shooter may become the "Rajada" placeholder.</item>
/// <item>Mira pack: its 3 Shooters are always Leque, Rajada and plain.</item>
/// <item>Emissor and Fragmento are looks, not enemies: a Sniper or Rush uses them with 50% chance.</item>
/// </list>
/// </summary>
public sealed partial class ProgressionDirector
{
    private const ulong VarietyStream = 0x5EED_E7A3_0000_0003UL;

    private void ApplyEnemyVariety(ManualRouteSnapshot route)
    {
        if (route == null) return;
        GameObject shooter = ArchetypePrefab(ArchetypeId.Shooter);
        GameObject emissor = SkinPrefab(ArchetypeId.Sniper), fragmento = SkinPrefab(ArchetypeId.Rush);
        ulong seed = route.RewardSeed ?? 0UL;
        foreach (var node in route.Nodes)
        {
            if (node.Kind != ManualNodeKind.Combat && node.Kind != ManualNodeKind.Elite) continue;
            if (node.Spawns == null || node.Spawns.Count == 0) continue;
            var rng = new SeededRng(SeedDerivation.SplitMix64(seed ^ VarietyStream ^ unchecked((ulong)node.Id)));
            bool mira = RoomPackFor(node.Id) == "Mira";
            var result = EnemyVarietyPlan.Apply(node.Spawns, mira, shooter, emissor, fragmento, rng);
            if (result != null) node.ConfigurePrototypeSpawns(result);
        }
    }

    private GameObject ArchetypePrefab(ArchetypeId id) =>
        _archetypePrefabs?.FirstOrDefault(v => v && v.ArchetypeId == id)?.gameObject;

    /// <summary>The look prefab (Emissor/Fragmento) kept in the First Sector content for <paramref name="id"/>.</summary>
    private GameObject SkinPrefab(ArchetypeId id)
    {
        if (!_firstSectorContent) return null;
        foreach (var prefab in _firstSectorContent.AdditionalEnemies)
            if (prefab && prefab.TryGetComponent(out EnemyVariant variant) && variant.ArchetypeId == id) return prefab;
        return null;
    }
}

/// <summary>Pure roster transform behind <see cref="ProgressionDirector"/>'s enemy variety (testable).</summary>
public static class EnemyVarietyPlan
{
    /// <summary>Returns the transformed roster, or null when nothing changed. Bosses and elites keep their prefab.</summary>
    public static List<ManualEnemySpawn> Apply(IReadOnlyList<ManualEnemySpawn> spawns, bool miraPack, GameObject shooter,
        GameObject emissor, GameObject fragmento, SeededRng rng)
    {
        var result = new List<ManualEnemySpawn>(spawns.Count);
        bool changed = false;
        int miraShooters = 0;
        foreach (var spawn in spawns)
        {
            if (spawn == null || !spawn.Prefab || spawn.Rank != EnemyRank.Normal) { result.Add(spawn); continue; }
            ArchetypeId? id = spawn.Prefab.TryGetComponent(out EnemyVariant variant) ? variant.ArchetypeId : null;
            GameObject prefab = spawn.Prefab;
            EnemyModifier modifier = spawn.Modifier;
            if (id == ArchetypeId.SpreadShooter && shooter)
            { prefab = shooter; id = ArchetypeId.Shooter; modifier = EnemyModifier.Leque; }
            else if (id == ArchetypeId.Shooter && modifier == EnemyModifier.None)
            {
                if (miraPack)
                    modifier = miraShooters++ switch { 0 => EnemyModifier.Leque, 1 => EnemyModifier.Rajada, _ => EnemyModifier.None };
                else if (rng.Roll(EnemyModifierRules.LequeChance)) modifier = EnemyModifier.Leque;
                else if (rng.Roll(EnemyModifierRules.RajadaChance)) modifier = EnemyModifier.Rajada;
            }
            else if (id == ArchetypeId.Sniper && emissor && rng.Roll(EnemyModifierRules.SkinChance)) prefab = emissor;
            else if (id == ArchetypeId.Rush && fragmento && rng.Roll(EnemyModifierRules.SkinChance)) prefab = fragmento;
            if (prefab != spawn.Prefab || modifier != spawn.Modifier) changed = true;
            result.Add(new ManualEnemySpawn(prefab, spawn.Offset, spawn.Rank, modifier));
        }
        return changed ? result : null;
    }
}
