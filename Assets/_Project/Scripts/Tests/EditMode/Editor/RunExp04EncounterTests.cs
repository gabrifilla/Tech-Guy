using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp04EncounterTests
    {
        [Test] public void SpatialCompositionPreservesEveryEnemyRankAndSafeSlotWithoutTouchingRandom()
        {
            var names=new[]{"Rush","Sniper","Healer","Grunt","Hooker","ShieldSupport"};
            var roster=names.Select((name,i)=>new ManualEnemySpawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+name+".prefab"),new Vector3(i%2==0?-7:7,0,(i/2-1)*5),EnemyRank.Normal)).ToList();
            var modes=new HashSet<FirstSectorEncounterLayout.Formation>();
            var random=Random.state;
            for(ulong seed=0;seed<256;seed++)
            {
                modes.Add(FirstSectorEncounterLayout.For(seed,1));
                var result=FirstSectorEncounterLayout.Arrange(roster,seed,1);
                Assert.AreEqual(roster.Count,result.Count);
                for(int i=0;i<roster.Count;i++){Assert.AreSame(roster[i].Prefab,result[i].Prefab);Assert.AreEqual(roster[i].Rank,result[i].Rank);}
                CollectionAssert.AreEquivalent(roster.Select(s=>s.Offset),result.Select(s=>s.Offset));
                var again=FirstSectorEncounterLayout.Arrange(roster,seed,1);CollectionAssert.AreEqual(result.Select(s=>s.Offset),again.Select(s=>s.Offset));
                if(FirstSectorEncounterLayout.For(seed,1)==FirstSectorEncounterLayout.Formation.PressureAndBackline)
                    Assert.LessOrEqual(result.Where(s=>!FirstSectorEncounterLayout.IsBackline(s.Prefab)).Max(s=>s.Offset.z),result.Where(s=>FirstSectorEncounterLayout.IsBackline(s.Prefab)).Min(s=>s.Offset.z));
            }
            Assert.AreEqual(3,modes.Count);Assert.AreEqual(JsonUtility.ToJson(random),JsonUtility.ToJson(Random.state));
            var solo=new[]{roster[0]};Assert.AreEqual(solo[0].Offset,FirstSectorEncounterLayout.Arrange(solo,1,1)[0].Offset);
        }
    }
}
