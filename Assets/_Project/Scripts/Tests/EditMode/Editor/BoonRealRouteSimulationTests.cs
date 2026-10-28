using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Random = System.Random;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>
    /// V2 uses the published route/loadouts/protocol catalog and the production ticket policy.
    /// An equal-weight counterfactual changes supplied card weights only; it is not a runtime change.
    /// The choice stream is independent from world generation and the policy's seeded draw.
    /// </summary>
    public sealed class BoonRealRouteSimulationTests
    {
        private const int Seeds = 300;
        private const string RoutePath = "Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset";
        private const string ProtocolPath = "Assets/_Project/ScriptableObjects/Protocols/DefaultProtocolCatalog.asset";
        private static readonly ProtocolKind[] Protocols =
            { ProtocolKind.Ignite, ProtocolKind.Frost, ProtocolKind.Conductor, ProtocolKind.Detonation };

        private static readonly string[] WeaponPaths =
        {
            "Assets/_Project/Resources/Weapons/Melee/Gauntlet/Gauntlet.asset",
            "Assets/_Project/Resources/Weapons/Ranged/Bow_arrow/Bow.asset",
            "Assets/_Project/Resources/Weapons/Melee/Spear/Spear.asset"
        };

        [UnityTest]
        public IEnumerator GauntletPublishedRouteMeasuresThreeHundredSeedsWithProductionTickets() => MeasurePublishedRoute(0);

        [UnityTest]
        public IEnumerator BowPublishedRouteMeasuresThreeHundredSeedsWithProductionTickets() => MeasurePublishedRoute(1);

        [UnityTest]
        public IEnumerator SpearPublishedRouteMeasuresThreeHundredSeedsWithProductionTickets() => MeasurePublishedRoute(2);

        private static IEnumerator MeasurePublishedRoute(int weaponIndex)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(WeaponPaths[weaponIndex]);
            var authoring = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(RoutePath);
            var protocolAuthoring = AssetDatabase.LoadAssetAtPath<ProtocolCatalogDefinition>(ProtocolPath);
            Assert.IsNotNull(authoring, RoutePath);
            Assert.IsNotNull(protocolAuthoring, ProtocolPath);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(authoring, weapon, out var source, out var error), error);
            Assert.IsTrue(protocolAuthoring.TryCreateSnapshot(out var protocols, out error), error);
            var paths = source.Paths.OrderBy(p => string.Join("/", p.Select(id => id.ToString("D3"))), StringComparer.Ordinal).ToArray();
            Assert.AreEqual(4, paths.Length, "The measured published slice currently has four exclusive paths.");
            Assert.AreEqual(0, Seeds % paths.Length);
            string sourceBefore = AssetSignature(authoring, protocolAuthoring, weapon);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/validation/artifacts/boons/V2"));
            Directory.CreateDirectory(directory);
            string prefix = Path.Combine(directory, WeaponRunModifiers.Identify(weapon).ToString());
            File.WriteAllText(prefix + "-progress.json", JsonUtility.ToJson(new Progress
            { state = "running", weapon = WeaponRunModifiers.Identify(weapon).ToString(), totalRuns = Seeds * 2,
                totalSeedsPerMode = Seeds, utc = DateTime.UtcNow.ToString("O") }, true), new UTF8Encoding(false));
            var summaries = new List<RunResult>();
            var opportunities = new List<string> { Csv("mode", "weapon", "seed", "path", "opportunity", "node", "ordinal", "announced_kind", "alternative", "eligible_ids", "weights", "offered_ids", "chosen_id", "chosen_family", "chosen_targets", "remaining_techniques_after", "repeated_card_set", "build_after") };
            var prerequisites = new List<string> { Csv("mode", "weapon", "seed", "path", "opportunity", "duo", "required_ids", "missing_before", "eligible_now", "offered_now", "acquired_before", "acquired_now", "remaining_techniques_including_now", "missing_after") };
            var cardEvents = new List<string> { Csv("mode", "weapon", "seed", "path", "opportunity", "stage", "id", "family", "weight", "targets_if_acquired") };
            var prefixes = new List<string> { Csv("mode", "weapon", "seed", "path", "opportunities", "first_mutation", "mutation_count", "augment_count", "protocol_acquisitions", "duo_count", "build") };
            var definitions = BoonOfferCompatibility.ExecutableDefinitions().ToDictionary(d => d.Id, StringComparer.Ordinal);
            var duos = definitions.Values.Where(d => d.Family == BoonFamily.Resonance &&
                (d.Weapons & Mask(weapon)) != 0).OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();

            foreach (bool weighted in new[] { true, false })
            {
                for (int seed = 1; seed <= Seeds; seed++)
                {
                    // Exactly 75 distinct seeds follow each path. Neither choice nor draw RNG selects the path.
                    int pathIndex = (seed - 1) % paths.Length;
                    var generated = ManualRunRandomizer.Generate(source, (ulong)seed, Array.Empty<GameObject>());
                    var result = Simulate(weapon, generated, paths[pathIndex], pathIndex, seed, weighted,
                        protocols, definitions, duos, opportunities, prerequisites, cardEvents, prefixes);
                    summaries.Add(result);
                    if (seed <= paths.Length)
                    {
                        // Session GUIDs differ; RewardSeed and independent choice RNG must reproduce the trace.
                        var replay = Simulate(weapon, generated, paths[pathIndex], pathIndex, seed, weighted,
                            protocols, definitions, duos, null, null, null, null);
                        Assert.AreEqual(result.trace, replay.trace, "Replay must not depend on the new session GUID.");
                        Assert.AreEqual(result.build, replay.build);
                    }
                    File.WriteAllText(prefix + "-progress.json", JsonUtility.ToJson(new Progress
                    {
                        state = "running", weapon = result.weapon, mode = result.mode, completedSeedsInMode = seed,
                        totalSeedsPerMode = Seeds, completedRuns = summaries.Count, totalRuns = Seeds * 2,
                        lastSeed = seed, lastPath = pathIndex, lastOpportunities = result.opportunities,
                        utc = DateTime.UtcNow.ToString("O")
                    }, true), new UTF8Encoding(false));
                    // A seed's policies are purely synchronous. Yield between seeds so Editor updates
                    // and the MCP heartbeat remain live; this does not participate in any RNG stream.
                    yield return null;
                }
            }

            Write(prefix + "-opportunities.csv", opportunities);
            Write(prefix + "-prerequisites.csv", prerequisites);
            Write(prefix + "-cards.csv", cardEvents);
            Write(prefix + "-prefixes.csv", prefixes);
            Write(prefix + "-runs.csv", new[] { Csv("mode", "weapon", "seed", "path", "path_labels", "opportunities", "technique_opportunities", "protocol_opportunities", "mutation_opportunities", "first_mutation", "augment_picks", "duo_picks", "alternative_tickets", "continue_picks", "repeated_card_sets", "final_build", "trace") }
                .Concat(summaries.Select(r => Csv(r.mode, r.weapon, r.seed, r.path, r.pathLabels, r.opportunities, r.techniques, r.protocols,
                    r.mutations, r.firstMutation, r.augmentsPicked, r.duosPicked, r.alternatives, r.continues, r.repeatedSets, r.build, r.trace))));
            File.WriteAllText(prefix + "-summary.json", JsonUtility.ToJson(Summarize(summaries, duos), true), new UTF8Encoding(false));
            Assert.AreEqual(sourceBefore, AssetSignature(authoring, protocolAuthoring, weapon), "Simulation must not mutate authoring assets.");
            File.WriteAllText(prefix + "-progress.json", JsonUtility.ToJson(new Progress
            { state = "complete", weapon = WeaponRunModifiers.Identify(weapon).ToString(), completedRuns = summaries.Count,
                totalRuns = Seeds * 2, totalSeedsPerMode = Seeds, utc = DateTime.UtcNow.ToString("O") }, true), new UTF8Encoding(false));
            TestContext.Progress.WriteLine("V2 faithful route outputs: " + prefix + "-*; 300 seeds per mode, four paths, real generated opportunities.");
        }

        private static RunResult Simulate(WeaponScript weapon, ManualRouteSnapshot generated, int[] path, int pathIndex,
            int seed, bool weighted, ProtocolCatalogSnapshot protocols, Dictionary<string, BoonDefinition> definitions,
            BoonDefinition[] duos, List<string> opportunities, List<string> prerequisites, List<string> cards, List<string> prefixes)
        {
            var route = new ManualRoutePolicy(generated);
            var policy = new RunRewardPolicy(route);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            var inventory = new ProtocolInventory(route.Session.Id, protocols);
            var context = new RunActionContext(true);
            var choiceRandom = new Random(unchecked(seed * 104729 + 0x43484F49));
            var result = new RunResult { mode = weighted ? "production_2_to_1" : "counterfactual_uniform", weapon = modifiers.Family.ToString(), seed = seed,
                path = pathIndex, pathLabels = string.Join("/", path.Select(id => generated.Node(id).Label)), firstMutation = -1 };
            var trace = new List<string>();
            var seenSets = new HashSet<string>(StringComparer.Ordinal);
            var sequence = path.SelectMany(id => Enumerable.Range(0, generated.Node(id).TotalAcquisitions)
                .Select(ordinal => Tuple.Create(id, ordinal, generated.Node(id).RewardAt(ordinal)))).ToArray();
            int opportunity = 0;

            foreach (int nodeId in path.Skip(1))
            {
                Assert.IsTrue(route.TryCommitDestination(route.Session.Id, nodeId, context), "Commit " + nodeId);
                Assert.IsTrue(route.TryEnterNode(route.Session.Id, nodeId, context), "Enter " + nodeId);
                var node = generated.Node(nodeId);
                if (node.Kind == ManualNodeKind.Repair)
                { Assert.IsTrue(route.TryCompleteRepair(route.Session.Id, nodeId, context)); continue; }
                Assert.IsTrue(route.TryResolveEncounter(route.Session.Id, nodeId, true, context));
                if (node.Kind == ManualNodeKind.Boss) continue;
                for (int ordinal = 0; ordinal < node.TotalAcquisitions; ordinal++)
                {
                    var kind = node.RewardAt(ordinal);
                    int remainingTechniques = sequence.Skip(opportunity).Count(s => s.Item3 == RunRewardKind.Technique);
                    opportunity++;
                    var request = new RunRewardRequest(route.Session.Id, nodeId, ordinal);
                    var graph = Graph(modifiers, weapon, inventory);
                    var eligible = ManualRewardCatalog.Eligible(kind, modifiers, weapon, inventory);
                    var alternatives = ManualRewardCatalog.Alternatives(kind, modifiers, weapon, inventory);
                    string key = ManualRewardCatalog.InventoryKey(modifiers, weapon, route.Session.InventoryRevision) + inventory.InventoryKey();
                    foreach (var card in eligible) AssertValidCard(card.Id, graph, inventory);
                    var supplied = weighted ? eligible : Uniform(eligible);
                    var suppliedAlternatives = weighted ? alternatives : Uniform(alternatives);
                    var ticket = policy.Open(request, context, key, supplied, suppliedAlternatives);
                    Assert.IsNotNull(ticket, "A real route opportunity must authorize a ticket.");
                    Assert.That(ticket.Choices.Count, Is.InRange(1, 3), "A first Mutation cannot be silently skipped.");
                    Assert.AreEqual(ticket.Choices.Count, ticket.Choices.Select(c => c.Id).Distinct().Count());
                    string frozen = string.Join("|", ticket.Choices.Select(c => c.Id));
                    int ticketRevision = ticket.Revision;
                    Assert.AreSame(ticket, policy.Open(request, context, key, supplied.Reverse().ToArray(), suppliedAlternatives));
                    Assert.AreEqual(ticketRevision, ticket.Revision, "Reopening unchanged inventory cannot reroll.");
                    Assert.AreEqual(frozen, string.Join("|", ticket.Choices.Select(c => c.Id)));
                    var actualPool = ticket.IsAlternative ? suppliedAlternatives : supplied;
                    Assert.That(ticket.Choices.All(c => actualPool.Any(p => p.Id == c.Id)), Is.True);
                    foreach (var card in ticket.Choices) AssertValidCard(card.Id, graph, inventory);
                    bool repeated = !seenSets.Add(string.Join("|", ticket.Choices.Select(c => c.Id).OrderBy(id => id, StringComparer.Ordinal)));
                    var chosen = ticket.Choices[choiceRandom.Next(ticket.Choices.Count)];
                    var missingBefore = duos.ToDictionary(d => d.Id, d => Missing(d, graph));
                    var acquiredBefore = duos.ToDictionary(d => d.Id, d => graph.Rank(d.Id) > 0);
                    var eligibleDuos = eligible.Where(c => Family(c.Id, definitions) == BoonFamily.Resonance.ToString()).Select(c => c.Id).ToArray();
                    var offeredDuos = ticket.Choices.Where(c => Family(c.Id, definitions) == BoonFamily.Resonance.ToString()).Select(c => c.Id).ToArray();
                    foreach (string id in eligibleDuos) result.eligibleDuos.Add(id);
                    foreach (string id in offeredDuos) result.offeredDuos.Add(id);
                    Assert.IsTrue(policy.TryChoose(request, ticket.Revision, chosen.Id, context,
                        () => StillEligible(chosen.Id, kind, ticket.IsAlternative, modifiers, weapon, inventory),
                        () => Apply(chosen.Id, modifiers, inventory, weapon), null), "Failed real card commit: " + chosen.Id);
                    Assert.AreEqual(route.Session.ProtocolAcquisitions, inventory.Revision);
                    Assert.IsFalse(policy.TryChoose(request, ticket.Revision, chosen.Id, context, () => true, () => true, null), "A resolved ticket cannot grant twice.");
                    var after = Graph(modifiers, weapon, inventory);
                    var record = after.Acquisitions.SingleOrDefault(a => a.Id == chosen.Id);
                    string family = Family(chosen.Id, definitions);
                    string target = record == null ? "none" : record.Slots.ToString();
                    string build = Signature(after);
                    trace.Add(nodeId + ":" + ordinal + "[" + frozen + "]=" + chosen.Id + "@" + target);
                    if (kind == RunRewardKind.Mutation) result.mutations++;
                    if (kind == RunRewardKind.Technique) result.techniques++;
                    if (kind == RunRewardKind.Protocol) result.protocols++;
                    if (family == BoonFamily.Mutation.ToString() && result.firstMutation < 0) result.firstMutation = opportunity;
                    if (family == BoonFamily.Augment.ToString()) result.augmentsPicked++;
                    if (family == BoonFamily.Resonance.ToString()) result.duosPicked++;
                    if (ticket.IsAlternative) result.alternatives++;
                    if (chosen.Id == RunBoonOfferCatalog.ExhaustedId) result.continues++;
                    if (repeated) result.repeatedSets++;
                    int afterRemaining = remainingTechniques - (kind == RunRewardKind.Technique ? 1 : 0);
                    opportunities?.Add(Csv(result.mode, result.weapon, seed, pathIndex, opportunity, node.Label, ordinal, kind, ticket.IsAlternative,
                        string.Join("|", eligible.Select(c => c.Id)), string.Join("|", actualPool.Select(c => c.Id + ":" + c.SelectionWeight)), frozen,
                        chosen.Id, family, target, afterRemaining, repeated, build));
                    foreach (var card in supplied) cards?.Add(Csv(result.mode, result.weapon, seed, pathIndex, opportunity, "eligible", card.Id, Family(card.Id, definitions), card.SelectionWeight, ""));
                    foreach (var card in ticket.Choices) cards?.Add(Csv(result.mode, result.weapon, seed, pathIndex, opportunity, "offered", card.Id, Family(card.Id, definitions), card.SelectionWeight, ""));
                    cards?.Add(Csv(result.mode, result.weapon, seed, pathIndex, opportunity, "acquired", chosen.Id, family, chosen.SelectionWeight, target));
                    foreach (var duo in duos)
                    {
                        string missingAfter = Missing(duo, after);
                        if (missingAfter.Length == 0) result.completedPrerequisites.Add(duo.Id);
                        prerequisites?.Add(Csv(result.mode, result.weapon, seed, pathIndex, opportunity, duo.Id,
                            string.Join("|", duo.RequiredBoonIds), missingBefore[duo.Id], eligibleDuos.Contains(duo.Id), offeredDuos.Contains(duo.Id),
                            acquiredBefore[duo.Id], chosen.Id == duo.Id, remainingTechniques, missingAfter));
                    }
                    prefixes?.Add(Csv(result.mode, result.weapon, seed, pathIndex, opportunity, result.firstMutation,
                        after.Acquisitions.Count(a => Family(a.Id, definitions) == BoonFamily.Mutation.ToString()),
                        after.Acquisitions.Count(a => Family(a.Id, definitions) == BoonFamily.Augment.ToString()), inventory.Revision,
                        after.Acquisitions.Count(a => Family(a.Id, definitions) == BoonFamily.Resonance.ToString()), build));
                }
            }
            Assert.AreEqual(sequence.Length, opportunity, "Only the route's actual rewards are counted.");
            Assert.AreEqual(1, result.mutations, "The published route supplies one Mutation opportunity, not four.");
            Assert.That(route.Session.MutationAcquisitions, Is.GreaterThanOrEqualTo(1));
            Assert.IsTrue(route.TryFinalizeBoss(route.Session.Id, context));
            Assert.IsTrue(route.TryRequestExtraction(route.Session.Id, context));
            result.opportunities = opportunity;
            result.trace = string.Join(";", trace);
            var final = Graph(modifiers, weapon, inventory);
            result.build = Signature(final);
            result.acquiredDuos = final.Acquisitions.Where(a => Family(a.Id, definitions) == BoonFamily.Resonance.ToString()).Select(a => a.Id).ToList();
            return result;
        }

        private static bool StillEligible(string id, RunRewardKind kind, bool alternative, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory inventory)
            => (alternative ? ManualRewardCatalog.Alternatives(kind, modifiers, weapon, inventory) : ManualRewardCatalog.Eligible(kind, modifiers, weapon, inventory)).Any(c => c.Id == id);

        private static void AssertValidCard(string id, BoonCapabilityGraph graph, ProtocolInventory inventory)
        {
            if (id == RunBoonOfferCatalog.ExhaustedId) return;
            Assert.IsTrue(RunBoonOfferCatalog.IncludesOffer(id), "Retired/disabled card: " + id);
            Assert.IsTrue(BoonOfferCompatibility.CanAcquire(graph, id), "Dead/incompatible card: " + id);
            if (ProtocolInventory.TryKind(id, out var kind)) Assert.IsTrue(inventory.CanAcquire(kind));
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

        private static BoonCapabilityGraph Graph(WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory inventory)
        { Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, inventory, out var graph, out string error), error); return graph; }
        private static string Signature(BoonCapabilityGraph graph) => graph.Weapon + ":" + string.Join("|", graph.Acquisitions.Select(a => a.Id + "@" + a.Slots + ":" + a.Rank));
        private static string Missing(BoonDefinition definition, BoonCapabilityGraph graph) => string.Join("|", definition.RequiredBoonIds.Where(id => graph.Rank(id) == 0));
        private static string Family(string id, Dictionary<string, BoonDefinition> definitions) => definitions.TryGetValue(id, out var definition) ? definition.Family.ToString() : id == RunBoonOfferCatalog.ExhaustedId ? "Continue" : "UNKNOWN";
        private static BoonWeaponMask Mask(WeaponScript weapon) => WeaponRunModifiers.Identify(weapon) == RunWeaponFamily.Gauntlet ? BoonWeaponMask.Gauntlet : WeaponRunModifiers.Identify(weapon) == RunWeaponFamily.Bow ? BoonWeaponMask.Bow : BoonWeaponMask.Spear;
        private static IReadOnlyList<RunBoons.Offer> Uniform(IEnumerable<RunBoons.Offer> source) => source.Select(c => new RunBoons.Offer(c.Id, c.Title, c.Description, 1)).ToArray();
        private static string AssetSignature(ManualRunDefinition route, ProtocolCatalogDefinition protocols, WeaponScript weapon) => JsonUtility.ToJson(route) + JsonUtility.ToJson(protocols) + JsonUtility.ToJson(weapon) + string.Join("\n", weapon.abilities.Select(a => JsonUtility.ToJson(a)));
        private static string Csv(params object[] values) => string.Join(",", values.Select(v => "\"" + Convert.ToString(v, CultureInfo.InvariantCulture).Replace("\"", "\"\"") + "\""));
        private static void Write(string path, IEnumerable<string> lines) => File.WriteAllLines(path, lines, new UTF8Encoding(false));

        private static Summary Summarize(List<RunResult> runs, BoonDefinition[] duos)
        {
            var summary = new Summary { generatedUtc = DateTime.UtcNow.ToString("O"),
                routeAsset = RoutePath, protocolAsset = ProtocolPath, seedsPerModePerWeapon = Seeds,
                method = "Published asset -> ManualRouteValidator -> ManualRunRandomizer.Generate -> ManualRoutePolicy -> RunRewardPolicy.Open/TryChoose; seed1..300, path=(seed-1)%4; independent uniform card choice RNG(seed*104729+0x43484F49).",
                limitations = "Offer/economy model only: assumes all encounters are won; paths are equally stratified, not player frequencies; prefixes are reward counts, not elapsed seconds; uniform weights are a counterfactual, not shipped behavior; no human gameplay acceptance or performance benchmark." };
            summary.groups = runs.GroupBy(r => r.mode + ":" + r.path).Select(group => new GroupSummary
            {
                mode = group.First().mode, weapon = group.First().weapon, path = group.First().path, pathLabels = group.First().pathLabels,
                runs = group.Count(), uniqueBuilds = group.Select(r => r.build).Distinct().Count(),
                minOpportunities = group.Min(r => r.opportunities), maxOpportunities = group.Max(r => r.opportunities),
                meanOpportunities = group.Average(r => r.opportunities), minTechniques = group.Min(r => r.techniques),
                maxTechniques = group.Max(r => r.techniques), meanTechniques = group.Average(r => r.techniques),
                minFirstMutation = group.Min(r => r.firstMutation), maxFirstMutation = group.Max(r => r.firstMutation),
                runsDuoEligible = group.Count(r => r.eligibleDuos.Count > 0), runsDuoOffered = group.Count(r => r.offeredDuos.Count > 0),
                runsDuoAcquired = group.Count(r => r.acquiredDuos.Count > 0), totalAugmentsPicked = group.Sum(r => r.augmentsPicked),
                totalDuosPicked = group.Sum(r => r.duosPicked), alternativeTickets = group.Sum(r => r.alternatives),
                continuePicks = group.Sum(r => r.continues), repeatedCardSets = group.Sum(r => r.repeatedSets),
                duos = duos.Select(d => d.Id).OrderBy(id => id, StringComparer.Ordinal)
                    .Select(id => new DuoSummary { id = id, prerequisitesCompletedRuns = group.Count(r => r.completedPrerequisites.Contains(id)),
                        eligibleRuns = group.Count(r => r.eligibleDuos.Contains(id)), offeredRuns = group.Count(r => r.offeredDuos.Contains(id)),
                        acquiredRuns = group.Count(r => r.acquiredDuos.Contains(id)) }).ToArray()
            }).ToArray();
            return summary;
        }

        private sealed class RunResult
        {
            public string mode, weapon, pathLabels, build, trace;
            public int seed, path, opportunities, techniques, protocols, mutations, firstMutation, augmentsPicked, duosPicked, alternatives, continues, repeatedSets;
            public HashSet<string> completedPrerequisites = new HashSet<string>(StringComparer.Ordinal);
            public HashSet<string> eligibleDuos = new HashSet<string>(StringComparer.Ordinal);
            public HashSet<string> offeredDuos = new HashSet<string>(StringComparer.Ordinal);
            public List<string> acquiredDuos = new List<string>();
        }
        [Serializable] private sealed class Summary
        { public string generatedUtc, routeAsset, protocolAsset, method, limitations; public int seedsPerModePerWeapon; public GroupSummary[] groups; }
        [Serializable] private sealed class GroupSummary
        {
            public string mode, weapon, pathLabels;
            public int path, runs, uniqueBuilds, minOpportunities, maxOpportunities, minTechniques, maxTechniques, minFirstMutation, maxFirstMutation,
                runsDuoEligible, runsDuoOffered, runsDuoAcquired, totalAugmentsPicked, totalDuosPicked, alternativeTickets, continuePicks, repeatedCardSets;
            public double meanOpportunities, meanTechniques;
            public DuoSummary[] duos;
        }
        [Serializable] private sealed class DuoSummary
        { public string id; public int prerequisitesCompletedRuns, eligibleRuns, offeredRuns, acquiredRuns; }
        [Serializable] private sealed class Progress
        {
            public string state, weapon, mode, utc;
            public int completedSeedsInMode, totalSeedsPerMode, completedRuns, totalRuns, lastSeed, lastPath, lastOpportunities;
        }
    }
}
