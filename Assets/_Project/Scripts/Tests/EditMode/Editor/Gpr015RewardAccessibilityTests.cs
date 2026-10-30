using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class Gpr015RewardAccessibilityTests
    {
        private static string Csv(params object[] values) => string.Join(",", values.Select(x => "\"" + Convert.ToString(x, CultureInfo.InvariantCulture).Replace("\"", "\"\"") + "\""));
        [UnityTest] public IEnumerator ProductionRewardsTargetConductorThenDetonation() => Sample("B-first");
        [UnityTest] public IEnumerator ProductionRewardsTargetDetonationThenConductor() => Sample("C-first");
        [UnityTest] public IEnumerator ProductionRewardsUniformChoiceUsesUnchangedOfferWeights() => Sample("uniform-choice");

        private static IEnumerator Sample(string strategy)
        {
            var authoring = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var protocolAsset = AssetDatabase.LoadAssetAtPath<ProtocolCatalogDefinition>("Assets/_Project/ScriptableObjects/Protocols/DefaultProtocolCatalog.asset");
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(authoring, weapon, out var source, out string error), error);
            Assert.IsTrue(protocolAsset.TryCreateSnapshot(out var protocols, out error), error);
            string before = JsonUtility.ToJson(authoring) + JsonUtility.ToJson(protocolAsset) + JsonUtility.ToJson(weapon);
            var paths = source.Paths.OrderBy(p => string.Join("/", p.Select(x => x.ToString("D3"))), StringComparer.Ordinal).ToArray();
            var rows = new List<string> { Csv("seed", "path", "strategy", "nodes", "mutation_opportunities", "protocol_opportunities", "technique_opportunities", "A", "B", "C", "AB", "AC", "ABC", "first_A_node", "first_B_node", "first_C_node", "first_ABC_node", "acquisition_order") };
            var trace = new List<string> { Csv("seed", "path", "strategy", "node", "ordinal", "kind", "eligible", "weights", "offered", "chosen") };
            var opportunities = new List<string> { Csv("seed", "path", "node", "kind", "ordinal") };
            for (ulong seed = 0; seed < 64; seed++)
            {
                // Rewards have their own seeded stream. The authored enemy fallback is used
                // only to satisfy generation; no enemy/room/reward data is changed.
                var generated = ManualRunRandomizer.Generate(source, seed, Array.Empty<GameObject>());
                for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
                {
                    int[] path = paths[pathIndex];
                    var route = new ManualRoutePolicy(generated); var policy = new RunRewardPolicy(route);
                    var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                    var inventory = new ProtocolInventory(route.Session.Id, protocols);
                    var context = new RunActionContext(true); var random = new System.Random(unchecked((int)seed * 104729 + pathIndex * 7919));
                    int mutation = 0, protocol = 0, technique = 0, aNode = -1, bNode = -1, cNode = -1, abcNode = -1;
                    var order = new List<string>();
                    foreach (int id in path.Skip(1))
                    {
                        Assert.IsTrue(route.TryCommitDestination(route.Session.Id, id, context));
                        Assert.IsTrue(route.TryEnterNode(route.Session.Id, id, context));
                        if (route.Session.Phase == RunPhase.RepairPending)
                        { Assert.IsTrue(route.TryCompleteRepair(route.Session.Id, id, context)); continue; }
                        Assert.IsTrue(route.TryResolveEncounter(route.Session.Id, id, true, context));
                        while (route.Session.Phase == RunPhase.RewardPending)
                        {
                            int ordinal = route.Session.RewardOrdinal; var kind = generated.Node(id).RewardAt(ordinal);
                            if (kind == RunRewardKind.Mutation) mutation++; else if (kind == RunRewardKind.Protocol) protocol++; else if (kind == RunRewardKind.Technique) technique++;
                            var eligible = ManualRewardCatalog.Eligible(kind, modifiers, weapon, inventory, route.Session.ProtocolAcquisitions + 1);
                            var alternatives = ManualRewardCatalog.Alternatives(kind, modifiers, weapon, inventory);
                            var request = new RunRewardRequest(route.Session.Id, id, ordinal);
                            var ticket = policy.Open(request, context, route.Session.InventoryRevision + ":" + inventory.Revision, eligible, alternatives);
                            Assert.IsNotNull(ticket); Assert.Greater(ticket.Choices.Count, 0);
                            var chosen = ticket.Choices[random.Next(ticket.Choices.Count)];
                            if (strategy != "uniform-choice")
                            {
                                string[] targets = strategy == "B-first" ? new[] { TitanicFistsDefinition.Id, "conductor", "detonation" } : new[] { TitanicFistsDefinition.Id, "detonation", "conductor" };
                                foreach (string target in targets)
                                {
                                    bool owned = target == TitanicFistsDefinition.Id ? modifiers.Expansion.MutationId(BoonSlot.Basic) == target : ProtocolInventory.TryKind(target, out var k) && inventory.Rank(k) > 0;
                                    var desired = ticket.Choices.FirstOrDefault(x => x.Id == target);
                                    if (!owned && desired != null) { chosen = desired; break; }
                                }
                            }
                            var actualPool = ticket.IsAlternative ? alternatives : eligible;
                            Assert.IsTrue(policy.TryChoose(request, ticket.Revision, chosen.Id, context,
                                () => actualPool.Any(x => x.Id == chosen.Id),
                                () => Apply(chosen.Id, modifiers, inventory, weapon), null), chosen.Id);
                            Assert.IsFalse(policy.TryChoose(request, ticket.Revision, chosen.Id, context, () => true, () => true, null));
                            if (chosen.Id == TitanicFistsDefinition.Id && aNode < 0) { aNode = id; order.Add("A"); }
                            if (chosen.Id == "conductor" && bNode < 0) { bNode = id; order.Add("B"); }
                            if (chosen.Id == "detonation" && cNode < 0) { cNode = id; order.Add("C"); }
                            if (aNode >= 0 && bNode >= 0 && cNode >= 0 && abcNode < 0) abcNode = id;
                            trace.Add(Csv(seed, pathIndex, strategy, id, ordinal, kind, string.Join("|", eligible.Select(x => x.Id)), string.Join("|", actualPool.Select(x => x.Id + ":" + x.SelectionWeight)), string.Join("|", ticket.Choices.Select(x => x.Id)), chosen.Id));
                            opportunities.Add(Csv(seed, pathIndex, id, kind, ordinal));
                        }
                    }
                    Assert.AreEqual(RunPhase.FinalResolution, route.Session.Phase);
                    Assert.AreEqual(1, mutation); Assert.AreEqual(1, route.Session.MutationAcquisitions);
                    rows.Add(Csv(seed, pathIndex, strategy, string.Join("/", path), mutation, protocol, technique,
                        aNode >= 0, bNode >= 0, cNode >= 0, aNode >= 0 && bNode >= 0, aNode >= 0 && cNode >= 0,
                        abcNode >= 0, aNode, bNode, cNode, abcNode, string.Join("", order)));
                }
                yield return null; // Keep the Editor/MCP responsive between deterministic seeds.
            }
            Assert.AreEqual(before, JsonUtility.ToJson(authoring) + JsonUtility.ToJson(protocolAsset) + JsonUtility.ToJson(weapon));
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/gpr-01-5/b/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")));
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, strategy + "-runs.csv"), rows);
            File.WriteAllLines(Path.Combine(directory, strategy + "-tickets.csv"), trace);
            File.WriteAllLines(Path.Combine(directory, strategy + "-opportunities.csv"), opportunities);
            TestContext.WriteLine("Reward audit: " + directory + "; 64 seeds x 4 paths; encounter victories assumed, no grant outside tickets; no physical playtest.");
        }

        private static bool Apply(string id, WeaponRunModifiers modifiers, ProtocolInventory inventory, WeaponScript weapon)
        {
            if (id == RunBoonOfferCatalog.ExhaustedId) return true;
            if (id == BoonReworkCatalog.ViolentFallId) return modifiers.TryAcquireViolentFall();
            if (BoonExpansionCatalog.TryDefinition(id, out _)) return BoonExpansionCatalog.TryAcquire(id, modifiers, weapon, inventory, out _);
            if (ProtocolInventory.TryKind(id, out var protocol)) return inventory.TryAcquire(inventory.SessionId, protocol, inventory.Revision, _ => true);
            if (WeaponRunModifiers.TryDefinition(id, out var definition)) return modifiers.Add(definition);
            return GauntletContentAuthoring.TryMutation(id, out var mutation) && modifiers.GauntletMutations.Select(mutation);
        }
    }
}
