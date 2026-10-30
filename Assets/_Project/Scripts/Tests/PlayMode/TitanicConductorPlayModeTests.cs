using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private string _compositionEvidenceDirectory;
        private string CompositionEvidenceDirectory
        {
            get
            {
                if (_compositionEvidenceDirectory == null)
                {
                    string run = UnityEditor.SessionState.GetString("TechGuy.Tests.TitanicCompositionEvidence",
                        "gpr-01-3/replays/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
                    _compositionEvidenceDirectory = System.IO.Path.Combine(Application.dataPath,
                        "../Docs/validation/artifacts/gameplay-rework/" + run);
                    System.IO.Directory.CreateDirectory(_compositionEvidenceDirectory);
                }
                return _compositionEvidenceDirectory;
            }
        }
        private void GrantTitanicProtocol(ProtocolKind kind)
        {
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_titanRun, new object[] { new RunBoons.Offer(ProtocolCatalogValidator.IdFor(kind), "fixture", "Assisted, not natural reward evidence"), false }));
        }

        [UnityTest] public IEnumerator TitanicConductorWhiffLaterImpactCreatesNewElectricDamageFromVictim()
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor);
            var source = BasicVictim(Vector3.forward * 4.5f);
            var neighbor = BasicVictim(Vector3.forward * 4.5f + Vector3.right * 2);
            int hits = 0; neighbor.DamageReceived += (_, __) => hits++;
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            long roots = runtime.Budget.RootsCreated;
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                stats.Watch(source); stats.Watch(neighbor);
                yield return TornadoCombo(); Assert.AreEqual(1000, source.health); Assert.AreEqual(1000, neighbor.health);
                yield return TornadoEnd(runtime);
                Assert.That(source.health, Is.EqualTo(988).Within(.001));
                Assert.That(neighbor.health, Is.EqualTo(994.6f).Within(.001), "New branch: distant terminal whiff has no native Conductor impact.");
                Assert.AreEqual(1, hits); Assert.AreEqual(roots + 3, runtime.Budget.RootsCreated);
                var snapshot = stats.Snapshot(); Assert.AreEqual(2, snapshot.ConfirmedHpTransactions);
                Assert.That(snapshot.EffectiveDamage, Is.EqualTo(17.4f).Within(.001));
                Assert.IsTrue(snapshot.RecentDamage.All(x => x.Secondary && x.Ability == "Basic" && x.ProducerId == TitanicFistsDefinition.Id && x.Parents.Length > 0));
            }
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TitanicConductorBaseAOnlyAndBOnlyDoNotCreateCombinedBranch()
        {
            yield return PrepareTitanic(false);
            var source = BasicVictim(Vector3.forward * 4.5f); var neighbor = BasicVictim(Vector3.forward * 4.5f + Vector3.right * 2);
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo(); yield return new WaitForSeconds(1.7f);
            Assert.AreEqual(1000, source.health); Assert.AreEqual(1000, neighbor.health); Assert.AreEqual(0, runtime.TitanicWavesResolved);
            GrantTitanicProtocol(ProtocolKind.Conductor);
            yield return TornadoCombo(); yield return new WaitForSeconds(1.7f);
            Assert.AreEqual(1000, source.health); Assert.AreEqual(1000, neighbor.health); Assert.AreEqual(0, runtime.TitanicWavesResolved);
            // Native B still conducts from a real direct hit, independently of the new branch.
            _player.DealResolvedAttackDamage(source, 10);
            Assert.That(neighbor.health, Is.EqualTo(995.5f).Within(.001));
        }

        [UnityTest] public IEnumerator TitanicConductorNoImpactNeverEmitsElectricity() => TitanicNegative("miss");
        [UnityTest] public IEnumerator TitanicConductorImmuneWindTargetNeverEmitsElectricity() => TitanicNegative("immune");
        [UnityTest] public IEnumerator TitanicConductorImmuneArcTargetHasNoFalseElectricVfx() => TitanicNegative("arcimmune");
        [UnityTest] public IEnumerator TitanicConductorFullRootCannotDamageOrShowArcs() => TitanicNegative("full");
        [UnityTest] public IEnumerator TitanicConductorLastImpactSlotPaysWindButSkipsElectricity() => TitanicNegative("last");
        [UnityTest] public IEnumerator TitanicConductorWallBlocksArcFromActualImpact() => TitanicNegative("wall");
        [UnityTest] public IEnumerator TitanicConductorCancellationInsideWindReceiptRevokesArc() => TitanicNegative("cancel");

        private IEnumerator TitanicNegative(string mode)
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor);
            var source = BasicVictim(Vector3.forward * (mode == "miss" ? 12 : 4.5f));
            var neighbor = BasicVictim(source.transform.position + Vector3.right * 2);
            if (mode == "immune") source.AddDamageImmunity(_titanRun);
            if (mode == "arcimmune") neighbor.AddDamageImmunity(_titanRun);
            if (mode == "wall")
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(wall);
                wall.transform.position = source.transform.position + Vector3.right + Vector3.up;
                wall.transform.localScale = new Vector3(.1f, 2, 1.5f);
            }
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>(); int electricFrames = 0;
            if (mode == "cancel") source.DamageReceived += (_, __) => runtime.CloseEncounter();
            yield return TornadoCombo(2); Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
            var parent = (GauntletImpactContext)typeof(CharControlScript).GetField("_basicContext", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controls);
            var root = runtime.FindRoot(parent); long roots = runtime.Budget.RootsCreated;
            if (mode == "full" || mode == "last")
                for (int i = 0; i < (mode == "full" ? 32 : 31); i++) Assert.IsTrue(root.ReserveImpact(1));
            float deadline = Time.realtimeSinceStartup + 4;
            do
            {
                yield return null; if (runtime.Feedback.ActiveCount > 0) electricFrames++;
                Assert.Less(Time.realtimeSinceStartup, deadline);
            } while (_controls.HasScheduledBasic || runtime.PendingTitanicWaves > 0);
            Assert.AreEqual(1000, neighbor.health); Assert.AreEqual(0, electricFrames, "No cosmetic arc without authorized electric HP.");
            Assert.That(source.health, Is.EqualTo(mode == "miss" || mode == "immune" || mode == "full" ? 1000 : 988).Within(.001));
            Assert.AreEqual(roots, runtime.Budget.RootsCreated); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            Assert.LessOrEqual(root.ImpactAttempts, 32);
        }

        [UnityTest] public IEnumerator TitanicConductorOneEmissionAcrossMultipleVictimsFramesAndColliders()
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor); GrantTitanicProtocol(ProtocolKind.Detonation);
            var a = BasicVictim(Vector3.forward * 3.1f); a.gameObject.AddComponent<BoxCollider>();
            var b = BasicVictim(Vector3.forward * 5.8f); b.gameObject.AddComponent<BoxCollider>();
            var neighbor = BasicVictim(a.transform.position + Vector3.right * 2);
            int ah = 0, bh = 0, nh = 0; a.DamageReceived += (_, __) => ah++; b.DamageReceived += (_, __) => bh++; neighbor.DamageReceived += (_, __) => nh++;
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.AreEqual(1, ah); Assert.AreEqual(1, bh); Assert.AreEqual(1, nh);
            Assert.That(a.health, Is.EqualTo(988).Within(.001)); Assert.That(b.health, Is.EqualTo(988).Within(.001));
            Assert.That(neighbor.health, Is.EqualTo(994.6f).Within(.001));
        }

        [UnityTest] public IEnumerator TitanicConductorLethalWindSharesDetonationTargetDedupeWithoutResources()
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor); GrantTitanicProtocol(ProtocolKind.Detonation);
            var source = BasicVictim(Vector3.forward * 4.5f); source.SetMaxHealth(1); source.RestoreHealthToMax();
            var neighbor = BasicVictim(source.transform.position + Vector3.right * 2);
            SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f); _player.mana = 50;
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.IsTrue(source.IsDead); Assert.That(neighbor.health, Is.EqualTo(999.55f).Within(.001), "Conductor consumes the shared target before Detonation; no duplicate HP.");
            Assert.AreEqual(50, _player.mana); Assert.AreEqual(0, _player.GetComponent<BreakerGauntletCombat>().Energy);
        }

        [UnityTest] public IEnumerator TitanicConductorAcquisitionInReverseOrderPreservesInteraction()
        {
            yield return PrepareTitanic(false); GrantTitanicProtocol(ProtocolKind.Conductor);
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_titanRun, new object[] { new RunBoons.Offer(TitanicFistsDefinition.Id, "fixture", "Assisted reverse order"), false }));
            var source = BasicVictim(Vector3.forward * 4.5f); var neighbor = BasicVictim(source.transform.position + Vector3.right * 2);
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.That(neighbor.health, Is.EqualTo(994.6f).Within(.001));
        }

        [UnityTest] public IEnumerator TitanicConductorNaturalTicketsAcquireBothWithoutFixtureGrants() => TitanicNaturalTickets("conductor");
        [UnityTest, Category("GPR014e")] public IEnumerator CompositionNaturalTicketsAcquireAAndCWithoutFixtureGrants() => TitanicNaturalTickets("detonation");
        private IEnumerator TitanicNaturalTickets(string protocolId)
        {
            yield return PrepareTitanic(false);
            var source = BuildManualRewardRoute().Session.Route;
            var aPool = ManualRewardCatalog.Eligible(RunRewardKind.Mutation, _titanRun.WeaponModifiers, _titanRun.RunWeapon, _titanRun.Protocols);
            var bPool = ManualRewardCatalog.Eligible(RunRewardKind.Protocol, _titanRun.WeaponModifiers, _titanRun.RunWeapon, _titanRun.Protocols);
            ulong selectedSeed = 0; bool found = false;
            for (ulong seed = 0; seed < 128 && !found; seed++)
            {
                var probe = new ManualRoutePolicy(new ManualRouteSnapshot(source, source.Nodes, seed));
                bool compatible = true;
                foreach (int node in new[] { 1, 2, 4, 5, 6 })
                {
                    ReachManualReward(probe, node);
                    if (node == 5 || node == 6)
                    {
                        var ticket = new RunRewardPolicy(probe).Open(new RunRewardRequest(probe.Session.Id, node, 0), RewardReady, "probe", node == 5 ? aPool : bPool);
                        compatible &= ticket.Choices.Any(x => x.Id == (node == 5 ? TitanicFistsDefinition.Id : protocolId));
                    }
                    Assert.IsTrue(probe.TryCompleteReward(probe.Session.Id, node, 0, node == 5, RewardReady, node == 6));
                }
                if (compatible) { found = true; selectedSeed = seed; }
            }
            Assert.IsTrue(found, "No legal A+B seed; do not change reward policy.");
            var route = new ManualRoutePolicy(new ManualRouteSnapshot(source, source.Nodes, selectedSeed));
            Assert.IsTrue(_titanRun.BindManualRun(route));
            var rows = new System.Collections.Generic.List<string> { "Fixture route; real tickets/draw/Choose; no A/B grants. Seed=" + selectedSeed };
            foreach (int node in new[] { 1, 2, 4, 5, 6 })
            {
                ReachManualReward(route, node); Assert.IsTrue(_titanRun.OfferReward(new RunRewardRequest(route.Session.Id, node, 0)));
                yield return null; yield return null;
                int index = node < 5 ? 0 : _titanRun.Choices.ToList().FindIndex(x => x.Id == (node == 5 ? TitanicFistsDefinition.Id : protocolId));
                Assert.GreaterOrEqual(index, 0); rows.Add(node + ": " + string.Join(",", _titanRun.Choices.Select(x => x.Id)) + " => " + _titanRun.Choices[index].Id);
                Assert.IsTrue(_titanRun.Choose(index)); Assert.IsFalse(_titanRun.Choose(index));
            }
            Assert.AreEqual(TitanicFistsDefinition.Id, _titanRun.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic));
            Assert.IsTrue(ProtocolInventory.TryKind(protocolId, out var protocol));
            Assert.AreEqual(1, _titanRun.Protocols.Rank(protocol));
            Assert.AreEqual(1, route.Session.MutationAcquisitions); Assert.AreEqual(1, route.Session.ProtocolAcquisitions);
            string path = System.IO.Path.Combine(CompositionEvidenceDirectory, "natural-tickets-" + protocolId + ".txt");
            Assert.IsFalse(System.IO.File.Exists(path), "Use a fresh evidence directory for each run.");
            System.IO.File.WriteAllLines(path, rows);
        }

        [UnityTest] public IEnumerator TitanicConductorAcquiredAfterTerminalAcceptanceCannotRetroactivelyGrantBranch()
        {
            yield return PrepareTitanic(); var source = BasicVictim(Vector3.forward * 4.5f);
            var neighbor = BasicVictim(source.transform.position + Vector3.right * 2); yield return null;
            yield return TornadoCombo(2); Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
            GrantTitanicProtocol(ProtocolKind.Conductor); yield return WaitForBasicEnd();
            yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.That(source.health, Is.EqualTo(988).Within(.001)); Assert.AreEqual(1000, neighbor.health);
        }

        [UnityTest] public IEnumerator TitanicConductorRankThreeHasOneBoundedFanOutWithoutRecursion()
        {
            yield return PrepareTitanic(); for (int i = 0; i < 3; i++) GrantTitanicProtocol(ProtocolKind.Conductor);
            var source = BasicVictim(Vector3.forward * 4.5f);
            var victims = new[] { BasicVictim(source.transform.position + Vector3.right * 2),
                BasicVictim(source.transform.position + Vector3.left * 2), BasicVictim(source.transform.position + new Vector3(2, 0, 2)) };
            int hits = 0; foreach (var victim in victims) victim.DamageReceived += (_, __) => hits++;
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.AreEqual(3, hits); foreach (var victim in victims) Assert.That(victim.health, Is.EqualTo(994.6f).Within(.001));
        }

        private IEnumerator ComposedCapture(string name)
        {
            string path = System.IO.Path.Combine(CompositionEvidenceDirectory, name + ".png");
            Assert.IsFalse(System.IO.File.Exists(path), "Evidence names must be fresh; never overwrite a previous capture.");
            ScreenCapture.CaptureScreenshot(path); float deadline = Time.realtimeSinceStartup + 3;
            while (!System.IO.File.Exists(path)) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null;
        }

        [UnityTest] public IEnumerator TitanicConductorGameViewShowsMovingWindAndImpactBoundArc()
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor);
            var source = BasicVictim(Vector3.forward * 3.1f); var neighbor = BasicVictim(source.transform.position + Vector3.right * 2);
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            using (var cameras = new TitanicCaptureCameras())
            {
                var cameraObject = new GameObject("GPR01.3 evidence camera"); _spawned.Add(cameraObject);
                var view = cameraObject.AddComponent<Camera>(); view.depth = 100;
                view.transform.position = new Vector3(7, 6, -5); view.transform.LookAt(new Vector3(.6f, .8f, 3));
                view.orthographic = true; view.orthographicSize = 4.8f;
                view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.035f, .05f, .075f);
                foreach (var actor in new[] { source, neighbor })
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Capsule); _spawned.Add(marker);
                    UnityEngine.Object.Destroy(marker.GetComponent<Collider>()); marker.transform.position = actor.transform.position + Vector3.up;
                    marker.GetComponent<Renderer>().material.color = actor == source ? new Color(1,.3f,.2f) : new Color(.2f,.6f,1);
                }
                var lightObject = new GameObject("GPR01.3 evidence light"); _spawned.Add(lightObject);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2;
                light.transform.rotation = Quaternion.Euler(50,-30,0);
                var rows = new System.Collections.Generic.List<string> { "time,windZ,sourceHp,neighborHp,activeElectricVfx,liveRoots" };
                Action record = () => rows.Add(string.Join(",", new[] { Time.time.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                    (runtime.FirstTitanicPosition ?? runtime.LastTitanicEndPosition).z.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                    source.health.ToString(System.Globalization.CultureInfo.InvariantCulture), neighbor.health.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    runtime.Feedback.ActiveCount.ToString(), runtime.Budget.LiveRoots.ToString() }));
                yield return TornadoCombo(2); Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
                float deadline = Time.realtimeSinceStartup + 4;
                while (runtime.PendingTitanicWaves == 0) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                record(); yield return ComposedCapture("ab-emission");
                while (neighbor.health == 1000) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                Assert.Greater(runtime.Feedback.ActiveCount, 0); record();
                var accent = _player.GetComponentsInChildren<Transform>().Single(x => x.name == "Protocol accent" && x.gameObject.activeSelf);
                Assert.That(Vector3.Distance(accent.position, source.transform.position + Vector3.up * .22f), Is.LessThan(.001), "Electric origin belongs to impacted victim, not player.");
                yield return ComposedCapture("ab-impact-arc");
                yield return new WaitForSeconds(.12f); record(); yield return ComposedCapture("ab-travel-after-impact");
                yield return TornadoEnd(runtime); record(); yield return ComposedCapture("ab-cleanup");
                System.IO.File.WriteAllLines(System.IO.Path.Combine(CompositionEvidenceDirectory, "ab-travel.csv"), rows);
                Assert.AreEqual(0, runtime.Budget.LiveRoots); Assert.That(neighbor.health, Is.EqualTo(994.6f).Within(.001));
            }
        }
    }
}
