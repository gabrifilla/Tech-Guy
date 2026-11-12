using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp03ContentTests
    {
        [Test] public void ContentUsesIndependentRoomStreamAndKeepsGraphRewardsCountsAndOldFamilies()
        {
            var definition=AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
            var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
            Assert.IsNotNull(content);Assert.AreEqual(2,content.AdditionalEnemies.Count);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition,Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]),out var source,out string error),error);
            var pool=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs/EnemyArchetypes"}).Select(id=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id))).OrderBy(p=>p.name).ToArray();
            var rows=new List<string>{"seed,node,kind,cover,roster"};int cover=0,open=0,fragmento=0,emissor=0,without=0;var old=new HashSet<string>();
            for(ulong seed=0;seed<128;seed++)
            {
                var before=ManualRunRandomizer.Generate(source,seed,pool);var after=ManualRunRandomizer.Generate(source,seed,pool,content);
                CollectionAssert.AreEqual(before.Paths.Select(p=>string.Join(",",p)),after.Paths.Select(p=>string.Join(",",p)));
                bool found=false;
                foreach(var node in after.Nodes.OrderBy(n=>n.Id))
                {
                    var original=before.Node(node.Id);Assert.AreEqual(original.Center,node.Center);Assert.AreEqual(original.ArenaSize,node.ArenaSize);Assert.AreEqual(original.Spawns.Count,node.Spawns.Count);
                    CollectionAssert.AreEqual(Enumerable.Range(0,original.TotalAcquisitions).Select(original.RewardAt),Enumerable.Range(0,node.TotalAcquisitions).Select(node.RewardAt));
                    bool covered=content.HasCover(seed,node);if(node.Kind==ManualNodeKind.Combat){if(covered)cover++;else open++;}else Assert.IsFalse(covered);
                    if(covered)Assert.IsTrue(node.Spawns.All(s=>FirstSectorContentDefinition.IsClear(s.Offset)));
                    foreach(var spawn in node.Spawns)
                    {if(spawn.Prefab.name=="Fragmento"){fragmento++;found=true;}else if(spawn.Prefab.name=="Emissor"){emissor++;found=true;}else old.Add(spawn.Prefab.name);}
                    rows.Add(seed+","+node.Id+","+node.Kind+","+covered+","+string.Join("|",node.Spawns.Select(s=>s.Prefab.name)));
                }
                if(!found)without++;
            }
            Assert.Greater(cover,0);Assert.Greater(open,0);Assert.Greater(old.Count,10);
            // Etapa 3 (11/11/2026): Emissor/Fragmento are looks applied after the draw, and the SpreadShooter became the
            // Shooter's "Leque" modifier, so none of them is drawn as an enemy of its own (one weight per archetype).
            Assert.AreEqual(0,fragmento,"Fragmento is a Rush look, not a drawn enemy.");Assert.AreEqual(0,emissor,"Emissor is a Sniper look, not a drawn enemy.");
            Assert.IsFalse(old.Contains("SpreadShooter"),"SpreadShooter leaves the draw.");
            Directory.CreateDirectory("Docs/validation/artifacts/run-exp-03");File.WriteAllLines("Docs/validation/artifacts/run-exp-03/distribution-128.csv",rows);
            File.WriteAllText("Docs/validation/artifacts/run-exp-03/distribution-summary.txt","Combat cover="+cover+" open="+open+" Fragmento spawns="+fragmento+" Emissor spawns="+emissor+" old families="+old.Count+" runs with neither="+without);
        }
    }
}
