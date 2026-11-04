using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp09BDistributionTests
    {
        private static ManualRouteSnapshot Source()
        {
            var d=AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(d,WeaponLoadout.LoadSelected(),out var s,out var error),error);return s;
        }
        private static FirstSectorRoomFamily Family(ManualNodeDefinition n)=>n.SpatialLayout?.Family??FirstSectorRoomFamily.Rectangle;
        [Test] public void ThousandSeedsPreserveOtherStreamsReplayEligibilityAndReduceLongStreaks()
        {
            var source=Source();var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
            var rows=new List<string>{"algorithm,seed,path,nodes,families,unique,repeats,max_streak,orientations,orientation_repeats,combined_repeats,unique_variants"};
            var nodes=new List<string>{"algorithm,seed,node,kind,family,eligible,orientation,orientation_mask,cover,roster,rewards"};
            int oldLong=0,newLong=0,oldRepeats=0,newRepeats=0,withRepeats=0,missingFamilies=0;var sequences=new HashSet<string>();
            for(ulong seed=0;seed<1024;seed++)
            {
                var old=ManualRunRandomizer.Generate(source,seed,null,content,true,false);
                var run=ManualRunRandomizer.Generate(source,seed,null,content,true,true,true);
                var repeat=ManualRunRandomizer.Generate(source,seed,null,content,true,true,true);
                foreach(var n in run.Nodes)
                {
                    var o=old.Node(n.Id);var r=repeat.Node(n.Id);
                    Assert.AreEqual(o.Kind,n.Kind);Assert.AreEqual(o.Center,n.Center);Assert.AreEqual(Family(n),Family(r));
                    Assert.AreEqual(n.SpatialLayout?.QuarterTurns??0,r.SpatialLayout?.QuarterTurns??0);
                    CollectionAssert.AreEqual(o.RewardSteps.Select(x=>x.Kind+":"+x.Quantity),n.RewardSteps.Select(x=>x.Kind+":"+x.Quantity));
                    CollectionAssert.AreEqual(o.Spawns.Select(x=>x.Prefab.GetEntityId()+":"+x.Rank),n.Spawns.Select(x=>x.Prefab.GetEntityId()+":"+x.Rank));
                    CollectionAssert.AreEqual(n.Spawns.Select(x=>x.Offset),r.Spawns.Select(x=>x.Offset));
                    Assert.AreEqual(content.HasCover(seed,o),content.HasCover(seed,n));
                    if(n.Kind!=ManualNodeKind.Combat){Assert.IsNull(n.SpatialLayout);Assert.AreEqual(o.ArenaSize,n.ArenaSize);CollectionAssert.AreEqual(o.Spawns.Select(x=>x.Offset),n.Spawns.Select(x=>x.Offset));}
                    if(n.SpatialLayout!=null)Assert.IsTrue(n.Spawns.All(x=>n.SpatialLayout.Clear(x.Offset,1.2f)));
                    foreach(var pair in new[]{Tuple.Create("legacy",o),Tuple.Create("history",n)})
                    {
                        var a=pair.Item2;nodes.Add(pair.Item1+","+seed+","+a.Id+","+a.Kind+","+Family(a)+","+(a.Kind==ManualNodeKind.Combat?31:0)+","+(a.SpatialLayout?.QuarterTurns??0)*90+","+(a.Kind!=ManualNodeKind.Combat?0:pair.Item1=="legacy"?1:FirstSectorRoomDistribution.EligibleOrientations(Family(a),a.SpatialLayout?.CanonicalSize??a.ArenaSize))+","+content.HasCover(seed,a)+","+string.Join("|",a.Spawns.Select(x=>x.Prefab.name+":"+x.Rank))+","+string.Join("|",a.RewardSteps.Select(x=>x.Kind+":"+x.Quantity)));
                    }
                }
                CollectionAssert.AreEqual(old.Connections.Select(x=>x.Id+":"+x.From+":"+x.To),run.Connections.Select(x=>x.Id+":"+x.From+":"+x.To));
                int index=0;
                foreach(var path in run.Paths)
                {
                    foreach(var pair in new[]{Tuple.Create("legacy",old),Tuple.Create("history",run)})
                    {
                        var fs=path.Select(id=>pair.Item2.Node(id)).Where(n=>n.Kind==ManualNodeKind.Combat).Select(Family).ToArray();
                        int streak=1,max=1,reps=0;for(int i=1;i<fs.Length;i++){if(fs[i]==fs[i-1]){streak++;reps++;}else streak=1;max=Math.Max(max,streak);}
                        if(pair.Item1=="legacy"){oldLong+=max>=3?1:0;oldRepeats+=reps;}
                        else{newLong+=max>=3?1:0;newRepeats+=reps;withRepeats+=reps>0?1:0;missingFamilies+=fs.Distinct().Count()<5?1:0;sequences.Add(string.Join("|",fs));}
                        var ns=path.Select(id=>pair.Item2.Node(id)).Where(n=>n.Kind==ManualNodeKind.Combat).ToArray();var turns=ns.Select(n=>(n.SpatialLayout?.QuarterTurns??0)*90).ToArray();
                        int orientationRepeats=0,combinedRepeats=0;for(int k=1;k<ns.Length;k++){if(turns[k]==turns[k-1])orientationRepeats++;if(turns[k]==turns[k-1]&&fs[k]==fs[k-1])combinedRepeats++;}
                        rows.Add(pair.Item1+","+seed+","+index+","+string.Join("|",ns.Select(n=>n.Id))+","+string.Join("|",fs)+","+fs.Distinct().Count()+","+reps+","+max+","+string.Join("|",turns)+","+orientationRepeats+","+combinedRepeats+","+ns.Select(n=>Family(n)+":"+(n.SpatialLayout?.QuarterTurns??0)).Distinct().Count());
                    }
                    index++;
                }
            }
            string dir="Docs/validation/artifacts/run-exp-09b/statistics";Directory.CreateDirectory(dir);File.WriteAllLines(dir+"/paths.csv",rows);File.WriteAllLines(dir+"/nodes.csv",nodes);
            Assert.Less(newLong,oldLong*.6,"Meaningful reduction of long streaks on held-out seeds");Assert.Less(newRepeats,oldRepeats);
            Assert.Greater(withRepeats,100,"Legitimate adjacent repeats remain possible");Assert.Greater(missingFamilies,100,"No full coverage guarantee");Assert.Greater(sequences.Count,500,"Different seeds generate diverse path sequences");
            UnityEngine.Debug.Log("09B old/new long="+oldLong+"/"+newLong+" repeats="+oldRepeats+"/"+newRepeats+" distinctSequences="+sequences.Count);
        }
        [Test] public void SingleTwoAndVaryingEligibilityShortRunsAndNodeOrderAreStable()
        {
            var source=Source();var w=new FirstSectorRoomVarietyWeights();
            for(ulong seed=0;seed<200;seed++)
            {
                var one=FirstSectorRoomDistribution.Plan(source,seed,w,n=>1<<3);Assert.IsTrue(one.Values.All(f=>f==FirstSectorRoomFamily.ElongatedGallery));
                var two=FirstSectorRoomDistribution.Plan(source,seed,w,n=>(1<<0)|(1<<4));Assert.IsTrue(two.Values.All(f=>f==FirstSectorRoomFamily.Rectangle||f==FirstSectorRoomFamily.FragmentedRing));
                var varied=FirstSectorRoomDistribution.Plan(source,seed,w,n=>n.Id%2==0?1<<2:(1<<0)|(1<<3));
                foreach(var n in source.Nodes.Where(n=>n.Kind==ManualNodeKind.Combat))Assert.IsTrue(n.Id%2==0?varied[n.Id]==FirstSectorRoomFamily.LChamber:varied[n.Id]==FirstSectorRoomFamily.Rectangle||varied[n.Id]==FirstSectorRoomFamily.ElongatedGallery);
                var reversed=new ManualRouteSnapshot(source,source.Nodes.Reverse());var a=FirstSectorRoomDistribution.Plan(source,seed,w);var b=FirstSectorRoomDistribution.Plan(reversed,seed,w);
                foreach(var kv in a)Assert.AreEqual(kv.Value,b[kv.Key],"Collection iteration order cannot change replay");
            }
            Assert.Throws<InvalidOperationException>(()=>FirstSectorRoomDistribution.Plan(source,5,w,n=>0));
            var first=source.Nodes.First(n=>n.Kind==ManualNodeKind.Combat).Id;
            var shortRun=new ManualRouteSnapshot(source,source.Nodes.Select(n=>new ManualNodeDefinition(n.Id,n.Label,n.Id==first?ManualNodeKind.Combat:ManualNodeKind.Repair,n.Reward,n.Acquisitions,n.Center,n.ArenaSize,n.RepairFraction,n.Spawns)));
            Assert.AreEqual(1,FirstSectorRoomDistribution.Plan(shortRun,5,w).Count);
        }
        [Test] public void DebugSeedIsOneShotAndNormalSeedsAreNotPinned()
        {
            string old=SessionState.GetString(RunSeedSource.NextEditorSeedKey,"");var go=new GameObject("09B transient seed source");
            try
            {
                var source=go.AddComponent<RunSeedSource>();var so=new SerializedObject(source);so.FindProperty("_randomizeEachRun").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,"5");Assert.IsTrue(source.TryGetSeed(out var seeded));Assert.AreEqual(5,seeded);Assert.AreEqual("",SessionState.GetString(RunSeedSource.NextEditorSeedKey,""));
                Assert.IsTrue(source.TryGetSeed(out var a));Assert.IsTrue(source.TryGetSeed(out var b));Assert.AreNotEqual(a,b);
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,"invalid");Assert.IsTrue(source.TryGetSeed(out _));SessionState.EraseString(RunSeedSource.NextEditorSeedKey);Assert.IsTrue(source.TryGetSeed(out _));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);if(old.Length==0)SessionState.EraseString(RunSeedSource.NextEditorSeedKey);else SessionState.SetString(RunSeedSource.NextEditorSeedKey,old);}
        }
        [Test] public void RotationsAreRigidFootprintTransformsWithMatchingCoversAndSafeSockets()
        {
            foreach(var family in new[]{FirstSectorRoomFamily.AsymmetricCourtyard,FirstSectorRoomFamily.LChamber})
            for(int w=22;w<=30;w+=2)for(int d=18;d<=24;d+=2)foreach(bool covered in new[]{false,true})
            {
                var canonical=new FirstSectorRoomLayout(family,new Vector2(w,d),covered);var distinct=new HashSet<string>();
                for(int q=0;q<4;q++)
                {
                    var oriented=new FirstSectorRoomLayout(family,new Vector2(w,d),covered,q);Assert.AreEqual(canonical.Area,oriented.Area);Assert.IsTrue(oriented.Clear(Vector3.zero,.6f));
                    for(int x=0;x<canonical.Width;x++)for(int z=0;z<canonical.Depth;z++)
                    {
                        var p=new Vector3(canonical.MinX+x+.5f,0,canonical.MinZ+z+.5f);Assert.AreEqual(canonical.Contains(p),oriented.Contains(oriented.ToOriented(p)));
                        Assert.AreEqual(p,oriented.ToCanonical(oriented.ToOriented(p)));
                    }
                    for(int i=0;i<canonical.Covers.Count;i++)
                    {var a=canonical.Covers[i];var b=oriented.Covers[i];var p=oriented.ToOriented(new Vector3(a.center.x,0,a.center.y));Assert.Less(Vector2.Distance(new Vector2(p.x,p.z),b.center),.001f);}
                    foreach(var side in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})Assert.IsTrue(oriented.Clear(oriented.Port(side*100)-side*1.5f,.6f));
                    distinct.Add(string.Join("|",oriented.Floors.Select(r=>r.ToString())));
                }
                Assert.AreEqual(4,distinct.Count,"Do not count equivalent rotations as variants");
            }
            foreach(var family in new[]{FirstSectorRoomFamily.Rectangle,FirstSectorRoomFamily.FragmentedRing,FirstSectorRoomFamily.ElongatedGallery})Assert.AreEqual(1,FirstSectorRoomDistribution.EligibleOrientations(family,new Vector2(28,26)));
        }
        [Test] public void PlanningCpuAndAllocationsAreMeasuredWithoutFrameWork()
        {
            var s=Source();var c=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
            long probeBefore=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[100000];long probeAfter=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(probe);bool counterValid=probeAfter-probeBefore>=100000;
            var rows=new List<string>{"scope,iteration,elapsed_ticks,bytes,heap_delta,gc_collections"};
            for(ulong seed=0;seed<16;seed++)FirstSectorRoomDistribution.Plan(s,seed,c.SpatialVariety);
            var timer=new Stopwatch();
            for(int i=0;i<256;i++)foreach(string mode in new[]{"plan","legacy_generate","history_generate","oriented_generate"})
            {
                long before=GC.GetAllocatedBytesForCurrentThread(),heap=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();int collections=GC.CollectionCount(0);timer.Restart();
                if(mode=="plan")FirstSectorRoomDistribution.Plan(s,(ulong)i,c.SpatialVariety);
                else ManualRunRandomizer.Generate(s,(ulong)i,null,c,true,mode!="legacy_generate",mode=="oriented_generate");
                timer.Stop();long bytes=counterValid?GC.GetAllocatedBytesForCurrentThread()-before:-1;rows.Add(mode+","+i+","+timer.ElapsedTicks+","+bytes+","+(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-heap)+","+(GC.CollectionCount(0)-collections));
            }
            var dir="Docs/validation/artifacts/run-exp-09b/performance";Directory.CreateDirectory(dir);File.WriteAllLines(dir+"/planning.csv",rows);File.WriteAllText(dir+"/conditions.txt","Editor, warmed schedule;256 seeds, alternating legacy/history/oriented complete data generation; current thread allocation counter valid="+counterValid+"; unsupported counter is -1, not zero allocation. Heap delta is net occupied managed heap with GC collection delta, NOT exact allocated bytes. Stopwatch ticks frequency="+Stopwatch.Frequency+". Full materialization/NavMesh not included. No standalone/GPU claim.");
        }
    }
}
