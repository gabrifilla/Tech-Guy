using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private static void CompositionRecord(string label, object receipt)
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" +
                UnityEditor.SessionState.GetString("TechGuy.Tests.GprPhase", "gpr-01-4") + "/receipts");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, label + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".json");
            Assert.IsFalse(System.IO.File.Exists(path)); System.IO.File.WriteAllText(path, JsonUtility.ToJson(receipt, true));
        }

        [UnityTest, Category("GPR014c")] public IEnumerator CompositionTripleConductorKillDetonatesWithoutRecursion() => CompositionTriple(0, true);
        [UnityTest, Category("GPR014c")] public IEnumerator CompositionTripleLastTwoSlotsStopAfterConductorKill() => CompositionTriple(30, false);
        [UnityTest, Category("GPR014c")] public IEnumerator CompositionTripleLastThreeSlotsPermitExactExplosion() => CompositionTriple(29, false);

        private IEnumerator CompositionTriple(int occupied, bool lethalExplosion)
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor); GrantTitanicProtocol(ProtocolKind.Detonation);
            var wind = BasicVictim(Vector3.forward * 4.5f);
            var electric = BasicVictim(new Vector3(2, 0, 4.5f)); electric.SetMaxHealth(5); electric.RestoreHealthToMax();
            var blast = BasicVictim(new Vector3(4, 0, 4.5f));
            if (lethalExplosion) { blast.SetMaxHealth(1); blast.RestoreHealthToMax(); }
            var beyond = BasicVictim(new Vector3(6.5f, 0, 4.5f));
            wind.gameObject.AddComponent<BoxCollider>(); electric.gameObject.AddComponent<BoxCollider>();
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                foreach (var actor in new[] { wind, electric, blast, beyond }) stats.Watch(actor);
                yield return TornadoCombo(2);
                float deadline = Time.realtimeSinceStartup + 3;
                while (!_controls.TryDirectionalBasicAttack(Vector3.forward)) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                var parent = (GauntletImpactContext)typeof(CharControlScript).GetField("_basicContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_controls);
                var root = runtime.FindRoot(parent); long roots = runtime.Budget.RootsCreated;
                for (int i = 0; i < occupied; i++) Assert.IsTrue(root.ReserveImpact(1));
                yield return WaitForBasicEnd(); yield return TornadoEnd(runtime);
                Assert.That(wind.health, Is.EqualTo(988).Within(.001)); Assert.IsTrue(electric.IsDead);
                Assert.That(blast.health, Is.EqualTo(occupied == 30 ? 1000 : lethalExplosion ? 0 : 996.25f).Within(.001));
                Assert.AreEqual(1000, beyond.health, "Detonation must not recurse or produce Conductor in the derived branch.");
                var receipt = stats.Snapshot();
                Assert.AreEqual(occupied == 30 ? 2 : 3, receipt.ConfirmedHpTransactions);
                Assert.AreEqual(1, receipt.RecentDamage.Count(x => x.Producer == "Conductor"));
                Assert.AreEqual(occupied == 30 ? 0 : 1, receipt.RecentDamage.Count(x => x.Producer == "Detonation"));
                Assert.That(receipt.EffectiveDamage, Is.EqualTo(occupied == 30 ? 17 : lethalExplosion ? 18 : 20.75f).Within(.001));
                Assert.AreEqual(lethalExplosion ? 2 : 1, receipt.PlayerCreditedKills);
                Assert.IsTrue(receipt.RecentDamage.All(x => x.Secondary && x.Ability == "Basic" && x.ProducerId == TitanicFistsDefinition.Id));
                Assert.AreEqual(1, receipt.RecentDamage.Select(x => string.Join(",", x.Parents)).Distinct().Count());
                Assert.AreEqual(roots, runtime.Budget.RootsCreated); Assert.LessOrEqual(root.ImpactAttempts, 32);
                CompositionRecord("triple-" + occupied, receipt);
            }
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest, Category("GPR014c")] public IEnumerator CompositionAcquireABC() => CompositionOrder("ABC");
        [UnityTest, Category("GPR014c")] public IEnumerator CompositionAcquireACB() => CompositionOrder("ACB");
        [UnityTest, Category("GPR014c")] public IEnumerator CompositionAcquireBAC() => CompositionOrder("BAC");
        [UnityTest, Category("GPR014c")] public IEnumerator CompositionAcquireCAB() => CompositionOrder("CAB");
        private IEnumerator CompositionOrder(string order)
        {
            yield return PrepareTitanic(false);
            foreach (char boon in order)
            {
                if (boon == 'A') Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(_titanRun, new object[] { new RunBoons.Offer(TitanicFistsDefinition.Id, "fixture", "Assisted order " + order), false }));
                else GrantTitanicProtocol(boon == 'B' ? ProtocolKind.Conductor : ProtocolKind.Detonation);
            }
            var wind = BasicVictim(Vector3.forward * 4.5f);
            var child = BasicVictim(new Vector3(2, 0, 4.5f)); child.SetMaxHealth(5); child.RestoreHealthToMax();
            var blast = BasicVictim(new Vector3(4, 0, 4.5f));
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.That(wind.health, Is.EqualTo(988).Within(.001)); Assert.IsTrue(child.IsDead);
            Assert.That(blast.health, Is.EqualTo(996.25f).Within(.001));
        }

        [UnityTest, Category("GPR014b")] public IEnumerator CompositionWindKillAuthorizesDetonation() => CompositionWindDetonation("kill");
        [UnityTest, Category("GPR014b")] public IEnumerator CompositionNonlethalWindNeverDetonates() => CompositionWindDetonation("nonlethal");
        [UnityTest, Category("GPR014b")] public IEnumerator CompositionImmuneWindNeverDetonates() => CompositionWindDetonation("immune");
        [UnityTest, Category("GPR014b")] public IEnumerator CompositionWindKillLastSlotCannotShowOrDamageExplosion() => CompositionWindDetonation("last");
        [UnityTest, Category("GPR014b")] public IEnumerator CompositionWindKillCancellationRevokesDetonation() => CompositionWindDetonation("cancel");
        [UnityTest, Category("GPR014b")] public IEnumerator CompositionDetonationOneOpportunityAcrossTwoWindDeaths() => CompositionWindDetonation("two");

        private IEnumerator CompositionWindDetonation(string mode)
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Detonation);
            var wind = BasicVictim(Vector3.forward * 4.5f);
            if (mode != "nonlethal") { wind.SetMaxHealth(1); wind.RestoreHealthToMax(); }
            if (mode == "immune") wind.AddDamageImmunity(_titanRun);
            var neighbor = BasicVictim(new Vector3(2, 0, 4.5f));
            Actor second = null;
            if (mode == "two")
            {
                second = BasicVictim(Vector3.forward * 5.8f); second.SetMaxHealth(1); second.RestoreHealthToMax();
                second.gameObject.SetActive(false);
                neighbor.DamageReceived += (_, __) => second.gameObject.SetActive(true);
            }
            SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f); _player.mana = 50;
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            if (mode == "cancel") wind.DamageReceived += (_, __) => runtime.CloseEncounter();
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                stats.Watch(wind); stats.Watch(neighbor); if (second) stats.Watch(second);
                yield return TornadoCombo(2);
                float deadline = Time.realtimeSinceStartup + 3;
                while (!_controls.TryDirectionalBasicAttack(Vector3.forward)) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                var parent = (GauntletImpactContext)typeof(CharControlScript).GetField("_basicContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_controls);
                var root = runtime.FindRoot(parent); long roots = runtime.Budget.RootsCreated;
                if (mode == "last") for (int i = 0; i < 31; i++) Assert.IsTrue(root.ReserveImpact(1));
                yield return WaitForBasicEnd(); yield return TornadoEnd(runtime);
                bool exploded = mode == "kill" || mode == "two";
                Assert.That(neighbor.health, Is.EqualTo(exploded ? 999.25f : 1000).Within(.001));
                Assert.AreEqual(mode != "nonlethal" && mode != "immune", wind.IsDead);
                if (second) Assert.IsTrue(second.IsDead);
                var receipt = stats.Snapshot();
                Assert.AreEqual(exploded ? 1 : 0, receipt.RecentDamage.Count(x => x.Producer == "Detonation" && x.ProducerId == TitanicFistsDefinition.Id));
                Assert.IsTrue(receipt.RecentDamage.All(x => x.Secondary && x.Ability == "Basic" && x.Parents.Length > 0));
                Assert.AreEqual(roots, runtime.Budget.RootsCreated); Assert.LessOrEqual(root.ImpactAttempts, 32);
                if (mode == "last" || mode == "cancel" || mode == "immune" || mode == "nonlethal") Assert.AreEqual(0, runtime.Feedback.ActiveCount);
            }
            Assert.AreEqual(50, _player.mana); Assert.AreEqual(0, _player.GetComponent<BreakerGauntletCombat>().Energy);
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest, Category("GPR014a")]
        public IEnumerator CompositionNativePunchAndWindEachConductOnSameTerminal()
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor);
            yield return TornadoCombo(2);
            var native = BasicVictim(Vector3.forward * 1.2f);
            var nativeChild = BasicVictim(new Vector3(-2, 0, 1.2f));
            // Keep future wind targets beyond the native chain's 3.5 m radius.
            // Otherwise native B legitimately claims them before the tornado arrives.
            var wind = BasicVictim(Vector3.forward * 5.8f);
            var windChild = BasicVictim(new Vector3(2, 0, 5.8f));
            native.gameObject.AddComponent<BoxCollider>(); wind.gameObject.AddComponent<BoxCollider>();
            yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                foreach (var actor in new[] { native, nativeChild, wind, windChild }) stats.Watch(actor);
                yield return TornadoCombo(1); yield return TornadoEnd(runtime);
                // Legacy B conducts from its own child back to the native source once.
                // Preserve that authored composition; only the Titanic branch is nonrecursive.
                Assert.That(native.health, Is.EqualTo(975.95f).Within(.001));
                Assert.That(nativeChild.health, Is.EqualTo(991).Within(.001));
                Assert.That(wind.health, Is.EqualTo(988).Within(.001));
                Assert.That(windChild.health, Is.EqualTo(994.6f).Within(.001));
                var receipt = stats.Snapshot();
                Assert.AreEqual(5, receipt.ConfirmedHpTransactions);
                Assert.That(receipt.EffectiveDamage, Is.EqualTo(50.45f).Within(.001));
                Assert.AreEqual(2, receipt.RecentDamage.Count(x => x.Producer == "Conductor" && x.ProducerId != TitanicFistsDefinition.Id));
                Assert.AreEqual(1, receipt.RecentDamage.Count(x => x.Producer == "Conductor" && x.ProducerId == TitanicFistsDefinition.Id));
                Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated);
            }
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest, Category("GPR014a")]
        public IEnumerator CompositionWindWithoutSecondaryTargetCannotInventChain()
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor);
            var wind = BasicVictim(Vector3.forward * 4.5f); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo();
            while (runtime.PendingTitanicWaves > 0)
            { Assert.AreEqual(0, runtime.Feedback.ActiveCount); yield return null; }
            Assert.That(wind.health, Is.EqualTo(988).Within(.001));
        }
    }
}
