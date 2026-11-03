using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Spatial composition only: assigns existing safe slots without changing roster or RNG.</summary>
public static class FirstSectorEncounterLayout
{
    public enum Formation { PressureAndBackline, Crossfire, SupportPocket }

    public static Formation For(ulong seed,int nodeId) => (Formation)(SeedDerivation.SplitMix64(seed ^ 0x454e434f554e5445UL ^ unchecked((ulong)nodeId)) % 3);

    public static bool IsBackline(GameObject prefab)
    {
        var variant=prefab ? prefab.GetComponent<EnemyVariant>() : null;
        if(!variant)return false;
        switch(variant.ArchetypeId)
        {
            case ArchetypeId.Shooter:case ArchetypeId.SpreadShooter:case ArchetypeId.Sniper:
            case ArchetypeId.Bomber:case ArchetypeId.HazardCaster:case ArchetypeId.Hooker:
            case ArchetypeId.Healer:case ArchetypeId.ShieldSupport:case ArchetypeId.Spawner:return true;
            default:return false;
        }
    }

    public static List<ManualEnemySpawn> Arrange(IReadOnlyList<ManualEnemySpawn> spawns,ulong seed,int nodeId)
    {
        var result=spawns.Select(s=>new ManualEnemySpawn(s.Prefab,s.Offset,s.Rank)).ToList();
        if(!spawns.Any(s=>IsBackline(s.Prefab)) || spawns.All(s=>IsBackline(s.Prefab)))return result;
        var formation=For(seed,nodeId);
        // Keep the exact validated position set. Pressure gets approach slots, backline gets depth;
        // crossfire separates those groups laterally rather than adding more simultaneous threats.
        System.Func<Vector3,float> score=p=>formation==Formation.Crossfire?p.x:p.z;
        var available=spawns.Select(s=>s.Offset).OrderBy(score).ThenBy(p=>p.x).ThenBy(p=>p.z).ToList();
        var order=Enumerable.Range(0,spawns.Count).OrderBy(i=>IsBackline(spawns[i].Prefab)?1:0).ToArray();
        if(formation==Formation.SupportPocket)
            order=order.OrderBy(i=>IsSupport(spawns[i].Prefab)?2:IsBackline(spawns[i].Prefab)?1:0).ToArray();
        for(int i=0;i<order.Length;i++)
        {int index=order[i];var s=spawns[index];result[index]=new ManualEnemySpawn(s.Prefab,available[i],s.Rank);}
        return result;
    }

    private static bool IsSupport(GameObject prefab)
    {
        var variant=prefab ? prefab.GetComponent<EnemyVariant>() : null;
        return variant && (variant.ArchetypeId==ArchetypeId.Healer || variant.ArchetypeId==ArchetypeId.ShieldSupport);
    }
}
