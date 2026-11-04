using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp09SpatialTests
    {
        [Test] public void FiftySeedsPreserveRewardsRostersAndConnectedRealFootprints()
        {
            var definition=AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
            var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition,WeaponLoadout.LoadSelected(),out var source,out var error),error);
            var families=new HashSet<FirstSectorRoomFamily>();
            for(ulong seed=0;seed<50;seed++)
            {
                var before=ManualRunRandomizer.Generate(source,seed,null,content);
                var after=ManualRunRandomizer.Generate(source,seed,null,content,true);
                var repeat=ManualRunRandomizer.Generate(source,seed,null,content,true);
                Assert.AreEqual(before.Paths.Count,after.Paths.Count);
                foreach(var n in after.Nodes)
                {
                    var old=before.Node(n.Id);var again=repeat.Node(n.Id);
                    CollectionAssert.AreEqual(old.RewardSteps.Select(s=>s.Kind+":"+s.Quantity),n.RewardSteps.Select(s=>s.Kind+":"+s.Quantity));
                    CollectionAssert.AreEqual(old.Spawns.Select(s=>s.Prefab.name+":"+s.Rank),n.Spawns.Select(s=>s.Prefab.name+":"+s.Rank));
                    CollectionAssert.AreEqual(n.Spawns.Select(s=>s.Offset),again.Spawns.Select(s=>s.Offset));
                    if(n.SpatialLayout==null)continue;
                    var plan=n.SpatialLayout;families.Add(plan.Family);
                    Assert.AreEqual(ManualNodeKind.Combat,n.Kind);Assert.Less(plan.Area,plan.Width*plan.Depth);
                    Assert.IsTrue(plan.Clear(Vector3.zero,.6f),"Native center remains a legal player anchor");
                    Assert.IsTrue(n.Spawns.All(s=>plan.Clear(s.Offset,1.2f)),"seed="+seed+" node="+n.Id);
                    foreach(var p in n.Spawns)Assert.Greater(p.Offset.sqrMagnitude,16);
                    Assert.AreEqual(plan.Area,plan.Floors.Sum(r=>r.width*r.height));
                    var visited=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();
                    for(int x=0;x<plan.Width&&queue.Count==0;x++)for(int z=0;z<plan.Depth;z++)if(plan.Cell(x,z)){var p=new Vector2Int(x,z);visited.Add(p);queue.Enqueue(p);break;}
                    var steps=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
                    while(queue.Count>0){var p=queue.Dequeue();foreach(var step in steps){var q=p+step;if(plan.Cell(q.x,q.y)&&visited.Add(q))queue.Enqueue(q);}}
                    Assert.AreEqual(plan.Area,visited.Count,"Disconnected footprint seed="+seed);
                    foreach(var requested in new[]{Vector3.left*100,Vector3.right*100,Vector3.forward*100,Vector3.back*100})
                    {
                        var port=plan.Port(requested);var inside=port-requested.normalized*1.5f;
                        Assert.IsTrue(plan.Clear(inside,.6f),"Door approach seed="+seed+" family="+plan.Family);
                    }
                }
                // Every new connector is still orthogonal and ends on real outer sockets.
                var paths=ProceduralRunGeometryPlan.Create(after);
                foreach(var line in paths.Values)for(int i=1;i<line.Length;i++)
                    Assert.IsTrue(Mathf.Abs(line[i].x-line[i-1].x)<.01f||Mathf.Abs(line[i].z-line[i-1].z)<.01f);
            }
            Assert.AreEqual(4,families.Count);
        }
    }
}
