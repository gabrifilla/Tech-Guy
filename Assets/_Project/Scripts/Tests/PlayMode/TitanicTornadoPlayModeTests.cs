using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private IEnumerator TornadoCombo(int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                float deadline = Time.realtimeSinceStartup + 3;
                while (!_controls.TryDirectionalBasicAttack(Vector3.forward))
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                yield return WaitForBasicEnd();
            }
        }

        [UnityTest] public IEnumerator TornadoTerminalWhiffEmitsExactlyOnceWithoutNativeHp()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo(2); Assert.AreEqual(0, runtime.TitanicWavesResolved);
            yield return TornadoCombo(1); Assert.AreEqual(1, runtime.TitanicWavesResolved);
            Assert.AreEqual(3, _attackCount); Assert.AreEqual(0, _player.GetComponent<BreakerGauntletCombat>().Energy);
        }

        [UnityTest] public IEnumerator TornadoRemainsInFlightAfterTerminalInsteadOfInstantOverlap()
        {
            yield return PrepareTitanic(); BasicVictim(Vector3.forward * 1.2f); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            Assert.AreEqual(1, runtime.TitanicWavesResolved); Assert.Greater(runtime.PendingTitanicWaves, 0);
        }

        [UnityTest] public IEnumerator TornadoWhiffLaterHitsDistantEnemyOnlyAfterTravel()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f); yield return null;
            yield return TornadoCombo(); Assert.AreEqual(1000, victim.health);
            yield return new WaitForSeconds(1.5f); Assert.Less(victim.health, 1000);
        }
        private IEnumerator TornadoEnd(ProtocolRuntime runtime)
        { float deadline = Time.realtimeSinceStartup + 4; while (runtime.PendingTitanicWaves > 0) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; } }

        [UnityTest] public IEnumerator TornadoNoBoonLeavesBaseUnchanged()
        { yield return PrepareTitanic(false); yield return TornadoCombo(6); Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().TitanicWavesResolved); Assert.AreEqual(6, _attackCount); }

        [UnityTest] public IEnumerator TornadoEachTerminalEmitsOneAndAllRootsEnd()
        { yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
            yield return TornadoCombo(6); Assert.AreEqual(2, runtime.TitanicWavesResolved); Assert.AreEqual(6, _attackCount);
            Assert.AreEqual(roots + 6, runtime.Budget.RootsCreated); yield return TornadoEnd(runtime); Assert.AreEqual(0, runtime.Budget.LiveRoots); }

        [UnityTest] public IEnumerator TornadoUncommittedRequestAndCanceledTerminalCannotEmit()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>();
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var fake = combat.BeginBasicExecutionWithFinisher(true);
            Assert.IsNull(runtime.ReserveConfirmedBasicChild(fake));
            Assert.IsFalse(runtime.QueueTitanicFists(fake, Vector3.zero, Vector3.forward, 2, Vector3.one, ~0, _player.CurrentWeapon));
            combat.EnergyPolicy.Close(fake);
            yield return TornadoCombo(2); Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward)); _controls.CancelCombo();
            yield return new WaitForSeconds(.35f); Assert.AreEqual(0, runtime.TitanicWavesResolved); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoCommittedNativeContactSurvivesRecoveryCancelOnce()
        {
            yield return PrepareTitanic(); var native = BasicVictim(Vector3.forward * 1.2f); yield return null;
            yield return TornadoCombo(2); native.DamageReceived += (_, __) => _controls.CancelCombo();
            // Busy/recovery can end while the preceding native hitstop/cadence gate is
            // still active. This test concerns cancellation AFTER committed contact,
            // so prepare a legitimately admissible terminal before asserting its result.
            float readyDeadline = Time.realtimeSinceStartup + 3;
            while (!_controls.CanStartDirectionalBasic)
            {
                Assert.Less(Time.realtimeSinceStartup, readyDeadline, "Native readiness gate must eventually reopen.");
                yield return null;
            }
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward)); yield return new WaitForSeconds(.25f);
            Assert.AreEqual(1, _player.GetComponent<ProtocolRuntime>().TitanicWavesResolved); Assert.AreEqual(3, _attackCount);
        }

        [UnityTest] public IEnumerator TornadoKeepsCommittedDirectionAndMovesProgressively()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            Vector3 start = runtime.FirstTitanicPosition.Value; _player.transform.rotation = Quaternion.Euler(0, 180, 0);
            yield return new WaitForSeconds(.2f); Vector3 end = runtime.FirstTitanicPosition.Value;
            Assert.That(end.z - start.z, Is.InRange(.5f, 1.0f)); Assert.That(end.x, Is.EqualTo(start.x).Within(.001f));
            Assert.Greater(end.z, start.z); Assert.Less(end.z, TitanicFistsDefinition.TravelDistance);
        }

        [UnityTest] public IEnumerator TornadoWhiffDamageUsesAcceptedNoncriticalPacketOnlyOnce()
        {
            yield return PrepareTitanic(); _player.Stats.criticalChance = 100;
            var victim = BasicVictim(Vector3.forward * 4.5f); yield return null;
            float reference = _player.Stats.CaptureAttackDamage(_player.CurrentWeapon.attackDamage, 1, 0).Amount;
            yield return TornadoCombo(); _player.Stats.baseDamage = 1000;
            yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.That(1000 - victim.health, Is.EqualTo(reference * .60f).Within(.001f));
        }

        [UnityTest] public IEnumerator TornadoMultipleTargetsHitOnceAndNativeVictimsStayExcluded()
        {
            yield return PrepareTitanic(); var native = BasicVictim(Vector3.forward * 1.2f);
            var a = BasicVictim(Vector3.forward * 3.5f); var b = BasicVictim(Vector3.forward * 5.3f); a.gameObject.AddComponent<BoxCollider>();
            int nativeHits = 0, ah = 0, bh = 0; native.DamageReceived += (_, __) => nativeHits++; a.DamageReceived += (_, __) => ah++; b.DamageReceived += (_, __) => bh++;
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.AreEqual(3, nativeHits); Assert.AreEqual(1, ah); Assert.AreEqual(1, bh); Assert.That(a.health, Is.EqualTo(988).Within(.001));
        }

        [UnityTest] public IEnumerator TornadoSweepsThinTargetsAtFourFpsAndEndsAtAuthoredDistance()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f); victim.GetComponent<BoxCollider>().size = new Vector3(.04f, 1, .04f);
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            float saved = Time.captureDeltaTime;
            try { Time.captureDeltaTime = .25f; yield return TornadoEnd(runtime); }
            finally { Time.captureDeltaTime = saved; }
            Assert.Less(victim.health, 1000); Assert.That(runtime.LastTitanicTravelled, Is.EqualTo(5.5f).Within(.001));
            Assert.That(runtime.LastTitanicEndPosition.z, Is.EqualTo(6.25f).Within(.001)); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoWallStopsTravelAndDamageIncludingSpawnOffsetOcclusion()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(wall); wall.transform.position = new Vector3(0, 1, 2.5f); wall.transform.localScale = new Vector3(3, 2, .1f);
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo(); yield return TornadoEnd(runtime);
            Assert.AreEqual(1000, victim.health); Assert.Less(runtime.LastTitanicEndPosition.z, 2.5f);
            wall.transform.position = new Vector3(0, 1, .05f); wall.transform.localScale = new Vector3(3, 2, .02f); Physics.SyncTransforms();
            yield return TornadoCombo(); yield return TornadoEnd(runtime); Assert.AreEqual(1000, victim.health); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoPauseAndHitstopFreezeTravelAndResumeSameReservation()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            var start = runtime.FirstTitanicPosition.Value; long roots = runtime.Budget.RootsCreated;
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.1f); Assert.That(Vector3.Distance(start, runtime.FirstTitanicPosition.Value), Is.LessThan(.001));
            Time.timeScale = 1; yield return null;
            using (var feedback = _player.BeginImpactFeedback(.3f)) feedback.Scope.ConfirmDamage(true);
            start = runtime.FirstTitanicPosition.Value; yield return new WaitForSecondsRealtime(.08f);
            Assert.IsTrue(_player.IsCombatHitStopped); Assert.That(Vector3.Distance(start, runtime.FirstTitanicPosition.Value), Is.LessThan(.001));
            yield return new WaitForSecondsRealtime(.3f); yield return TornadoEnd(runtime); Assert.AreEqual(roots, runtime.Budget.RootsCreated); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoPoolingDedupesEachTargetGeneration()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f); victim.gameObject.AddComponent<BoxCollider>();
            var counts = new Dictionary<long, int>(); int hits = 0;
            victim.DamageReceived += (_, __) => { long generation = victim.CombatGeneration; counts[generation] = counts.ContainsKey(generation) ? counts[generation] + 1 : 1;
                if (++hits == 1) { victim.gameObject.SetActive(false); victim.RestoreHealthToMax(); victim.gameObject.SetActive(true); } };
            yield return null; yield return TornadoCombo(); yield return TornadoEnd(_player.GetComponent<ProtocolRuntime>());
            Assert.AreEqual(2, counts.Count); Assert.IsTrue(counts.Values.All(x => x == 1));
        }

        [UnityTest] public IEnumerator TornadoBudgetExhaustionCannotDealDamageOrMintRoots()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo(2);
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
            var parent = (GauntletImpactContext)typeof(CharControlScript).GetField("_basicContext", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controls);
            var root = runtime.FindRoot(parent); while (root.ReserveImpact(1)) { }
            long roots = runtime.Budget.RootsCreated; yield return WaitForBasicEnd(); yield return TornadoEnd(runtime);
            Assert.AreEqual(1000, victim.health); Assert.AreEqual(roots, runtime.Budget.RootsCreated); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoSwapAndReturnRestoreOwnedCuffWithoutStaleProjectile()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1])); yield return null;
            Assert.AreEqual(0, runtime.PendingTitanicWaves); Assert.IsFalse(_player.GetComponent<TitanicFistsVisual>().WaveActive);
            _player.EquipWeapon(_titanRun.RunWeapon); yield return null; yield return null;
            Assert.IsTrue(_player.GetComponent<TitanicFistsVisual>().OverlayActive); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoDisableAndRunCleanupReleaseAllVfxAndRoots()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            var visual = _player.GetComponent<TitanicFistsVisual>(); _player.gameObject.SetActive(false); yield return null;
            Assert.AreEqual(0, runtime.PendingTitanicWaves); Assert.AreEqual(0, runtime.Budget.LiveRoots); Assert.IsFalse(visual.OverlayActive); Assert.IsFalse(visual.WaveActive);
        }

        [UnityTest] public IEnumerator TornadoEndRunAndNewRunDoNotRetainMutation()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo();
            typeof(RunBoons).GetMethod("ClearRunState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_titanRun, null);
            yield return null; Assert.AreEqual(0, runtime.PendingTitanicWaves); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            UnityEngine.Object.Destroy(_titanRun); yield return null;
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); _player.GetComponent<AbilityHolder>().RefreshLoadout();
            _titanRun = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            Assert.IsNull(_titanRun.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic)); Assert.IsFalse(_player.GetComponent<TitanicFistsVisual>().OverlayActive);
        }

        [UnityTest] public IEnumerator TornadoSceneTransitionDestroysWorldSpacePool()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo(); var budget = runtime.Budget;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("NexusBlockout"); yield return null;
            Assert.AreEqual(0, budget.LiveRoots);
            Assert.AreEqual(0, UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(x => x.name == "Swirling wind"));
        }

        [UnityTest] public IEnumerator TornadoDetonationOwnershipAuthorizesConfirmedKillWithoutResourcesAfterWhiff()
        {
            yield return PrepareTitanic(); var inventory = _titanRun.Protocols;
            Assert.IsTrue(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Detonation, inventory.Revision, _ => true));
            var victim = BasicVictim(Vector3.forward * 4.5f); victim.SetMaxHealth(1);
            var neighbor = BasicVictim(Vector3.forward * 4.5f + Vector3.right * 2);
            SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f); _player.mana = 50;
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo(); yield return TornadoEnd(runtime);
            Assert.IsTrue(victim.IsDead); Assert.That(neighbor.health, Is.EqualTo(999.25f).Within(.001)); Assert.AreEqual(50, _player.mana); Assert.AreEqual(0, _player.GetComponent<BreakerGauntletCombat>().Energy);
        }

        [UnityTest] public IEnumerator TornadoImpactAttributionCountsOnlyEffectiveHpWithOriginalBasicParent()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f); yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            { stats.Watch(victim); yield return TornadoCombo(); Assert.AreEqual(0, stats.Snapshot().ConfirmedHpTransactions);
                yield return TornadoEnd(runtime); var snapshot = stats.Snapshot(); Assert.AreEqual(1, snapshot.ConfirmedHpTransactions);
                Assert.That(snapshot.EffectiveDamage, Is.EqualTo(1000 - victim.health).Within(.001));
                var source = snapshot.RecentDamage.Single(); Assert.IsTrue(source.Secondary); Assert.AreEqual("Basic", source.Ability); Assert.AreEqual(TitanicFistsDefinition.Id, source.ProducerId); Assert.IsNotEmpty(source.Parents); }
        }

        [UnityTest] public IEnumerator TornadoGameViewRecordsTravelAndDelayedImpact()
        {
            yield return PrepareTitanic();
            var hand = _player.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
            if (hand && _player.HandTransform) _player.HandTransform.SetParent(hand, false);
            var victim = BasicVictim(Vector3.forward * 4.5f); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>();
            using (var cameras = new TitanicCaptureCameras())
            {
                var cameraObject = new GameObject("Tornado evidence camera"); _spawned.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>(); camera.depth = 100;
                camera.transform.position = new Vector3(6, 5, -5); camera.transform.LookAt(new Vector3(0, .9f, 2.7f));
                camera.orthographic = true; camera.orthographicSize = 4.8f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .05f, .075f);
                var target = GameObject.CreatePrimitive(PrimitiveType.Capsule); _spawned.Add(target);
                UnityEngine.Object.Destroy(target.GetComponent<Collider>()); target.transform.position = victim.transform.position + Vector3.up;
                target.GetComponent<Renderer>().material.color = Color.red;
                var lightObject = new GameObject("Tornado evidence light"); _spawned.Add(lightObject);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                var rows = new List<string> { "scaledTime,positionZ,hp,pending,liveRoots" };
                Action record = () => rows.Add(string.Join(",", new[] { Time.time.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                    (runtime.FirstTitanicPosition.HasValue ? runtime.FirstTitanicPosition.Value.z : runtime.LastTitanicEndPosition.z).ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                    victim.health.ToString(System.Globalization.CultureInfo.InvariantCulture), runtime.PendingTitanicWaves.ToString(), runtime.Budget.LiveRoots.ToString() }));
                yield return TitanicCapture("tornado-equipped"); yield return TornadoCombo(2);
                Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
                while (runtime.TitanicWavesResolved == 0) yield return null;
                record(); yield return TitanicCapture("tornado-emitted");
                yield return new WaitForSeconds(.25f); record(); Assert.AreEqual(1000, victim.health); yield return TitanicCapture("tornado-travel-a");
                yield return new WaitForSeconds(.3f); record(); yield return TitanicCapture("tornado-travel-b");
                while (victim.health == 1000 && runtime.PendingTitanicWaves > 0) { record(); yield return null; }
                record(); Assert.That(victim.health, Is.EqualTo(988).Within(.001)); yield return TitanicCapture("tornado-impact");
                yield return TornadoEnd(runtime); record(); yield return TitanicCapture("tornado-cleanup");
                System.IO.File.WriteAllLines(System.IO.Path.Combine(TitanicLegacyEvidenceDirectory, "travel.csv"), rows);
                Assert.AreEqual(0, runtime.Budget.LiveRoots);
            }
        }

        [UnityTest] public IEnumerator TornadoPlayerDeathReleasesInFlightWork()
        {
            yield return PrepareTitanic(); var victim = BasicVictim(Vector3.forward * 4.5f); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>(); var budget = runtime.Budget;
            var visual = _player.GetComponent<TitanicFistsVisual>(); yield return TornadoCombo();
            _player.TakeDamage(1000000); yield return null; yield return null;
            Assert.IsTrue(!_player || _player.IsDead); Assert.AreEqual(1000, victim.health);
            Assert.AreEqual(0, runtime.PendingTitanicWaves); Assert.AreEqual(0, budget.LiveRoots);
            Assert.IsTrue(!visual || !visual.WaveActive);
        }

        [UnityTest] public IEnumerator TornadoVfxPoolReusesOneInactiveInstanceWithoutColliders()
        {
            yield return PrepareTitanic(); var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo(); yield return TornadoEnd(runtime);
            var first = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(x => x.name == "Titanic wind tornado — pooled"); var id = first.GetEntityId();
            Assert.IsFalse(first.gameObject.activeSelf); Assert.AreEqual(0, first.GetComponentsInChildren<Collider>(true).Length);
            yield return TornadoCombo(); Assert.IsTrue(first.gameObject.activeSelf);
            Assert.AreEqual(id, first.GetEntityId()); yield return TornadoEnd(runtime);
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator TornadoReentrantWeaponSwapRevokesRemainingQueuedDamage()
        {
            yield return PrepareTitanic(); var a = BasicVictim(new Vector3(-.1f, 0, 4.5f)); var b = BasicVictim(new Vector3(.1f, 0, 4.5f));
            int hits = 0; Action<Actor, float> swap = (_, __) => { hits++; _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1])); };
            a.DamageReceived += swap; b.DamageReceived += swap; yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>(); yield return TornadoCombo(); yield return TornadoEnd(runtime);
            Assert.AreEqual(1, hits); Assert.That(a.health + b.health, Is.EqualTo(1988).Within(.001)); Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }
    }
}
