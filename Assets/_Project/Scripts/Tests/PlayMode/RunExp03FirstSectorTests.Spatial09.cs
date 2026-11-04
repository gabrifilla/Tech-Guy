using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        private const string SpatialReview="Assets/_Project/Scenes/Review/RunExp09SpatialReview.unity";
        [UnityTest] public IEnumerator Spatial09FiftyDecksHaveRealConnectedNavMeshAndUsableSockets()
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/structural/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            var rows=new List<string>{"seed,node,family,area,floor_rects,boundaries,spawns"};
            for(ulong seed=0;seed<50;seed++)
            {
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());
                EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int frame=0;frame<3;frame++)yield return null;
                var director=Object.FindAnyObjectByType<ProgressionDirector>();Assert.IsNotNull(director.ManualSession,"seed="+seed);
                var route=director.ManualSession.Route;Assert.IsTrue(director.ManualEnvironment.HasGeneratedLayout);
                foreach(var node in route.Nodes)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(node.Center,out var center,.3f,NavMesh.AllAreas));
                    foreach(var spawn in node.Spawns)
                    {
                        Assert.IsTrue(NavMesh.SamplePosition(node.Center+spawn.Offset,out var hit,.4f,NavMesh.AllAreas),"spawn "+seed+"/"+node.Id);
                        var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(center.position,hit.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);
                    }
                    var layout=node.SpatialLayout;if(layout==null)continue;
                    foreach(var floor in layout.Floors)
                    {
                        // Covers are deliberate solids on otherwise walkable floors; choose clear grid samples.
                        for(float x=floor.xMin+1;x<floor.xMax;x+=3)for(float z=floor.yMin+1;z<floor.yMax;z+=3)
                        {
                            var p=new Vector3(x,0,z);if(!layout.Clear(p,.8f))continue;
                            Assert.IsTrue(NavMesh.SamplePosition(node.Center+p,out var hit,.4f,NavMesh.AllAreas),"floor "+seed+"/"+node.Id+" "+p);
                            var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(center.position,hit.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);
                        }
                    }
                    for(int x=1;x<layout.Width-1;x+=2)for(int z=1;z<layout.Depth-1;z+=2)
                    {
                        if(layout.Cell(x,z))continue;var p=new Vector3(layout.MinX+x+.5f,0,layout.MinZ+z+.5f);
                        Assert.IsFalse(NavMesh.SamplePosition(node.Center+p,out var hit,.15f,NavMesh.AllAreas),"Forbidden floor must not have rectangular navigation "+seed+"/"+node.Id+" "+p);
                    }
                    rows.Add(seed+","+node.Id+","+layout.Family+","+layout.Area+","+layout.Floors.Count+","+layout.Boundaries.Count+","+node.Spawns.Count);
                }
                // Gates carve during play. For structural path verification use their native authored-open state.
                foreach(var binding in director.ManualEnvironment.Connections)binding.Barrier.GetComponent<EncounterGates>().SetAuthoredOpen(true);
                for(int frame=0;frame<3;frame++)yield return null;
                foreach(var edge in route.Connections)
                {
                    var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(route.Node(edge.From).Center,route.Node(edge.To).Center,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,"connection "+seed+"/"+edge.Id);
                }
                File.WriteAllLines(Path.Combine(_dir,"fifty-decks.csv"),rows);
                if(seed%10==0)Debug.Log("RUNEXP09 NavMesh seed="+seed+" verified");
            }
        }
    }
}
