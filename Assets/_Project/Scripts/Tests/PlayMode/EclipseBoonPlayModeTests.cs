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
        private AbilityHolder _eclipseHolder;
        private RunBoons _eclipseRun;
        private BreakerGauntletCombat _eclipseCombat;

        private IEnumerator EclipseRig(bool fall = true)
        {
            if (!_navMesh) Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.gameObject.AddComponent<AudioListener>();
            _eclipseHolder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); _eclipseHolder.RefreshLoadout();
            _eclipseRun = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            _eclipseCombat = _player.GetComponent<BreakerGauntletCombat>();
            Assert.IsTrue(_eclipseRun.WeaponModifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut));
            if (fall) Assert.IsTrue(_eclipseRun.WeaponModifiers.TryAcquireViolentFall());
            SetPrivate(_player, "baseMaxMana", 1000f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(true); _player.transform.rotation = Quaternion.identity;
        }
        private Actor EclipseTarget(Vector3 position, bool airborne = false, EnemyRank rank = EnemyRank.Normal)
        {
            var actor = BuildEnemy(position, 10000f).actor;
            var reaction = actor.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(rank, airborne ? 1 : 100000, 1, 0, 10, 0, 0, airborne ? 0 : 1, 1);
            Physics.SyncTransforms(); return actor;
        }
        private IEnumerator EclipseCastQ()
        {
            Assert.IsTrue(_eclipseHolder.TryUseAbility(0));
            float end = Time.realtimeSinceStartup + 4;
            while (_eclipseCombat.IsExecuting && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsFalse(_eclipseCombat.IsExecuting);
        }
        private void EclipseBasic(bool finisher = true, bool hpExpected = true)
        {
            int hits = _player.TryApplyGauntletBasicDamage(_player.transform.position, Vector3.forward, 3,
                new Vector3(4, 4, 3), ~0, 10,
                new HitReactionRequest(_player, Vector3.forward, Vector3.forward, HitReactionType.None,
                    HitStrength.Light, 12, StanceBreakEffect.None, 0, 0, 0, 0), finisher);
            if (hpExpected) Assert.Greater(hits, 0, "Native basic must confirm HP before testing its wave.");
            else Assert.AreEqual(0, hits, "HP immunity must remain independent of posture confirmation.");
        }
        private IEnumerator EclipseWaitForFalls(int count)
        {
            float end = Time.realtimeSinceStartup + 4;
            while (_eclipseCombat.EclipseFallsEmitted < count && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(count, _eclipseCombat.EclipseFallsEmitted);
        }

        [UnityTest]
        public IEnumerator EclipseUppercutAndFallAreAcquiredThroughRealManualTickets()
        {
            yield return EclipseRig(false);
            // Start this acceptance case with an empty inventory; no direct grant substitutes the ticket.
            _eclipseRun.WeaponModifiers.GauntletMutations.Clear();
            var sourceRoute = BuildManualRewardRoute().Session.Route;
            // This scenario verifies ticket acquisition, not drop rates. Seed the known Q/Fall
            // sequence independently of the fresh session GUID; keep the production draw intact.
            var route = new ManualRoutePolicy(new ManualRouteSnapshot(sourceRoute, sourceRoute.Nodes, rewardSeed: 1UL));
            Assert.IsTrue(_eclipseRun.BindManualRun(route));
            foreach (int node in new[] { 1, 2, 4 })
            { ReachManualReward(route, node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id, node, 0, false, RewardReady)); }
            ReachManualReward(route, 5);
            Assert.IsTrue(_eclipseRun.OfferReward(new RunRewardRequest(route.Session.Id, 5, 0)));
            int q = _eclipseRun.Choices.ToList().FindIndex(o => o.Id == BoonReworkCatalog.UppercutId); Assert.GreaterOrEqual(q, 0);
            Assert.IsTrue(_eclipseRun.Choose(q)); Assert.AreEqual(1, route.Session.MutationAcquisitions);
            ReachManualReward(route, 7);
            Assert.IsTrue(_eclipseRun.OfferReward(new RunRewardRequest(route.Session.Id, 7, 0)));
            int fall = _eclipseRun.Choices.ToList().FindIndex(o => o.Id == BoonReworkCatalog.ViolentFallId); Assert.GreaterOrEqual(fall, 0);
            var ids = _eclipseRun.Choices.Select(o => o.Id).ToArray(); _eclipseRun.CloseManualReward();
            Assert.IsTrue(_eclipseRun.OfferReward(new RunRewardRequest(route.Session.Id, 7, 0)));
            Assert.That(_eclipseRun.Choices.Select(o => o.Id), Is.EqualTo(ids));
            yield return null; Assert.IsTrue(_eclipseRun.Choose(fall)); Assert.IsTrue(_eclipseRun.WeaponModifiers.HasViolentFall);
            Assert.IsFalse(_eclipseRun.Choose(fall));
            Assert.IsTrue(_eclipseRun.OfferReward(new RunRewardRequest(route.Session.Id, 7, 1)));
            Assert.IsFalse(_eclipseRun.Choices.Any(o => o.Id == BoonReworkCatalog.ViolentFallId));
            yield return null; Assert.IsTrue(_eclipseRun.Choose(0));
            Assert.IsTrue(route.TryCommitDestination(route.Session.Id, 8, RewardReady));
            Assert.IsTrue(route.TryEnterNode(route.Session.Id, 8, RewardReady));
            var enemy = EclipseTarget(Vector3.forward * 1.2f);
            enemy.gameObject.AddComponent<ProtocolEncounterMembership>().Configure(route.Session.Id, 8);
            yield return EclipseCastQ(); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
            EclipseBasic(); yield return EclipseWaitForFalls(1);
            Assert.That(enemy.health, Is.LessThan(10000));
        }

        [UnityTest]
        public IEnumerator EclipseNativeQIsStationaryAndEmptyCastDoesNotGenerateEnergy()
        {
            yield return EclipseRig();
            Vector3 start = _player.transform.position; float mana = _player.mana;
            float cost = _eclipseRun.RunWeapon.abilities[0].ManaCost;
            yield return EclipseCastQ();
            Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.03f));
            Assert.AreEqual(mana - cost, _player.mana, .001f); Assert.AreEqual(0, _eclipseCombat.Energy);
            Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
        }

        [UnityTest]
        public IEnumerator EclipseTwoCommonsLaunchOnlyOnBreakAndProduceOneWavePerMarkedTarget()
        {
            yield return EclipseRig();
            var left = EclipseTarget(new Vector3(-.45f, 0, 1.3f), true);
            var right = EclipseTarget(new Vector3(.45f, 0, 1.3f), true);
            left.gameObject.AddComponent<BoxCollider>(); right.gameObject.AddComponent<BoxCollider>();
            yield return EclipseCastQ();
            Assert.IsTrue(left.GetComponent<CombatReactionController>().IsAirborne);
            Assert.IsTrue(right.GetComponent<CombatReactionController>().IsAirborne);
            Assert.AreEqual(2, _eclipseCombat.EclipseMarkCount); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
            EclipseBasic(false); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
            EclipseBasic(); yield return EclipseWaitForFalls(2);
            Assert.IsFalse(left.GetComponent<CombatReactionController>().IsAirborne);
            Assert.IsFalse(right.GetComponent<CombatReactionController>().IsAirborne);
            Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
            EclipseBasic(); yield return null; yield return null; Assert.AreEqual(2, _eclipseCombat.EclipseFallsEmitted);
        }

        [UnityTest]
        public IEnumerator EclipseHeavyEliteAndBossUseMarksAndPostureWithoutVirtualLaunch()
        {
            yield return EclipseRig();
            var heavy = EclipseTarget(new Vector3(-.5f, 0, 1.3f));
            heavy.GetComponent<CombatReactionController>().ConfigureReaction(new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob));
            var elite = EclipseTarget(new Vector3(0, 0, 1.3f), false, EnemyRank.Elite);
            var boss = EclipseTarget(new Vector3(.5f, 0, 1.3f), false, EnemyRank.Boss);
            yield return EclipseCastQ(); Assert.AreEqual(3, _eclipseCombat.EclipseMarkCount, "Q hp=" + heavy.health + "," + elite.health + "," + boss.health + " player=" + _player.transform.position + " forward=" + _player.transform.forward);
            foreach (var target in new[] { heavy, elite, boss }) Assert.IsFalse(target.GetComponent<CombatReactionController>().IsAirborne);
            float stance = boss.GetComponent<CombatReactionController>().CurrentStance;
            EclipseBasic(); float after = boss.health;
            yield return EclipseWaitForFalls(3);
            Assert.Less(boss.health, after); Assert.Less(boss.GetComponent<CombatReactionController>().CurrentStance, stance - 12);
            Assert.AreEqual(0f, boss.transform.position.y, .001f);
        }

        [UnityTest]
        public IEnumerator EclipseCadencedThirdStrikeConsumesMarkAndEchoCannotRearmIt()
        {
            float captureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            try
            {
                yield return EclipseRig();
                var target = EclipseTarget(Vector3.forward * 1.2f, true);
                _eclipseRun.WeaponModifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura);
                WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho), out var echo);
                _eclipseRun.WeaponModifiers.Add(echo);
                yield return EclipseCastQ(); _eclipseCombat.AddAsuraEnergy(100);
                var observations = new System.Collections.Generic.List<string>();
                _eclipseRun.Hooks.OnProtocolImpact += e => observations.Add(e.Producer + "@" + e.Depth + " y=" + target.transform.position.y + " hp=" + target.health + " marks=" + _eclipseCombat.EclipseMarkCount + " t=" + target.GetComponent<CombatReactionController>().GameplayTime);
                float mana = _player.mana; float cost = _eclipseRun.RunWeapon.abilities[3].ManaCost;
                Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
                for (int strike = 0; strike < 2; strike++)
                {
                    float end = Time.realtimeSinceStartup + 3;
                    while (!_eclipseCombat.IsCadencedWaiting && Time.realtimeSinceStartup < end) yield return null;
                    Assert.IsTrue(_eclipseCombat.IsCadencedWaiting); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
                    Assert.IsTrue(_eclipseHolder.TryUseAbility(3)); Assert.AreEqual(mana - cost, _player.mana, .001f);
                }
                float landingDeadline = Time.realtimeSinceStartup + 4;
                while (_eclipseCombat.EclipseFallsEmitted < 1 && Time.realtimeSinceStartup < landingDeadline) yield return null;
                Assert.AreEqual(1, _eclipseCombat.EclipseFallsEmitted, string.Join("; ", observations));
                yield return new WaitForSeconds(.4f);
                Assert.AreEqual(1, _eclipseCombat.EclipseFallsEmitted); Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
                Assert.AreEqual(0, _eclipseCombat.Energy); Assert.AreEqual(mana - cost, _player.mana, .001f);
            }
            finally { Time.captureDeltaTime = captureDelta; }
        }

        [UnityTest]
        public IEnumerator EclipseMarksAndArmedLandingsPauseWithGameplayAndPlayerHitstop()
        {
            yield return EclipseRig();
            var target = EclipseTarget(Vector3.forward * 1.2f, true);
            yield return EclipseCastQ(); EclipseBasic();
            var reaction = target.GetComponent<CombatReactionController>();
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(reaction.IsAirborne); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
            Time.timeScale = 1; _player.GetComponent<HitStopRunner>().Apply(.3f);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsTrue(_player.IsCombatHitStopped); Assert.IsTrue(reaction.IsAirborne);
            Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
            yield return EclipseWaitForFalls(1);
        }

        [UnityTest]
        public IEnumerator EclipseUnconfirmedLandingAndTargetRecycleCannotProduceDamage()
        {
            yield return EclipseRig();
            var target = EclipseTarget(Vector3.forward * 1.2f, true);
            yield return EclipseCastQ();
            float landingDeadline = Time.realtimeSinceStartup + 4;
            while (target.GetComponent<CombatReactionController>().IsAirborne && Time.realtimeSinceStartup < landingDeadline) yield return null;
            yield return null;
            Assert.IsFalse(target.GetComponent<CombatReactionController>().IsAirborne);
            Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted); Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
            // Capture another real mark, then change the target generation before the armed wave.
            _eclipseHolder.ReduceCooldowns(100); yield return null; target.GetComponent<CombatReactionController>().ConfigureStance(EnemyRank.Normal, 1, 1, 0, 10, 0, 0, 0, 1);
            yield return EclipseCastQ(); EclipseBasic();
            long generation = target.CombatGeneration; target.gameObject.SetActive(false); target.gameObject.SetActive(true);
            Assert.AreNotEqual(generation, target.CombatGeneration);
            float health = target.health; yield return new WaitForSeconds(1.8f);
            Assert.AreEqual(health, target.health); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
        }

        [UnityTest]
        public IEnumerator EclipseEndEncounterAndWeaponSwapReleaseEveryArmedReservation()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f, true);
            yield return EclipseCastQ(); EclipseBasic();
            var runtime = _player.GetComponent<ProtocolRuntime>();
            Assert.Greater(runtime.Budget.LiveRoots, 0);
            Assert.IsTrue(_eclipseCombat.TryStopEncounterOffense()); _eclipseRun.CloseProtocolEncounter();
            Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            float health = target.health;
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1]));
            yield return new WaitForSeconds(1.8f); Assert.AreEqual(health, target.health);
            Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
        }

        [UnityTest]
        public IEnumerator EclipseDerivedWavesRespectTheSharedRootAtEightThirtyTwoAndSixtyFourEnemies()
        {
            foreach (int density in new[] { 8, 32, 64 })
            {
                yield return EclipseRig();
                var targets = Enumerable.Range(0, density).Select(i => EclipseTarget(new Vector3((i % 8 - 3.5f) * .18f, 0, 1.1f + (i / 8) * .05f))).ToArray();
                yield return EclipseCastQ(); Assert.AreEqual(density, _eclipseCombat.EclipseMarkCount);
                var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
                int derived = 0; _eclipseRun.Hooks.OnProtocolImpact += e => { if (e.Producer == ProtocolProducer.Technique) derived++; };
                EclipseBasic(); var afterNative = targets.Select(t => t.health).ToArray();
                var postureAfterNative = targets.Select(t => t.GetComponent<CombatReactionController>().CurrentStance).ToArray();
                float deadline = Time.realtimeSinceStartup + 4;
                while (_eclipseCombat.EclipseMarkCount > 0 && Time.realtimeSinceStartup < deadline) yield return null;
                int harmed = targets.Where((t,i) => t.health < afterNative[i]).Count();
                Assert.That(harmed, Is.InRange(1, Math.Min(density, 32)));
                Assert.That(derived, Is.InRange(1, 32), "All waves share one derived impact cap.");
                float derivedPosture = targets.Select((t,i) => postureAfterNative[i] - t.GetComponent<CombatReactionController>().CurrentStance).Sum();
                Assert.AreEqual(derived * 12f * .35f, derivedPosture, .5f, "Dropped HP attempts cannot bypass the same cap through posture.");
                Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated, "The wave cannot open a new root per target or emission.");
                Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
                Assert.LessOrEqual(runtime.Feedback.ActiveCount, runtime.Inventory.Catalog.Limits.ActiveVfx);
                UnityEngine.Object.DestroyImmediate(_player.gameObject);
                foreach (var actor in targets) if (actor) UnityEngine.Object.DestroyImmediate(actor.gameObject);
                UnityEngine.Object.DestroyImmediate(_weapon); yield return null;
            }
        }
        [UnityTest]
        public IEnumerator EclipseNativeWAndEFinalizersConsumeMarks()
        {
            yield return EclipseRig();
            foreach (int slot in new[] { 1, 2 })
            {
                if (slot == 2)
                {
                    while (_eclipseHolder.IsAbilityActive(0)) yield return null;
                    _eclipseHolder.ReduceCooldowns(100); yield return null;
                }
                var target = EclipseTarget(Vector3.forward * 1.2f);
                yield return EclipseCastQ(); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
                Assert.IsTrue(_eclipseHolder.TryUseAbility(slot));
                yield return EclipseWaitForFalls(slot);
                UnityEngine.Object.DestroyImmediate(target.gameObject);
                while (_eclipseCombat.IsExecuting) yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EclipseCanceledUppercutBeforeImpactCannotMarkOrDealDamage()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f);
            float mana = _player.mana, cost = _eclipseRun.RunWeapon.abilities[0].ManaCost;
            Assert.IsTrue(_eclipseHolder.TryUseAbility(0));
            Assert.IsTrue(_eclipseCombat.TryStopEncounterOffense());
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(10000, target.health); Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
            Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted); Assert.AreEqual(mana - cost, _player.mana, .001f);
        }

        [UnityTest]
        public IEnumerator EclipseWeaponSwapAndDisableIndependentlyDiscardArmedWaves()
        {
            foreach (bool swap in new[] { true, false })
            {
                yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f, true);
                yield return EclipseCastQ(); EclipseBasic();
                var runtime = _player.GetComponent<ProtocolRuntime>(); Assert.Greater(runtime.Budget.LiveRoots, 0);
                float health = target.health;
                if (swap) _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1]));
                else _eclipseCombat.enabled = false;
                Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount); Assert.AreEqual(0, runtime.Budget.LiveRoots);
                yield return new WaitForSeconds(1.8f);
                Assert.AreEqual(health, target.health); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
                UnityEngine.Object.DestroyImmediate(_player.gameObject); UnityEngine.Object.DestroyImmediate(target.gameObject);
                UnityEngine.Object.DestroyImmediate(_weapon); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EclipseHpImmunityStillAllowsConfirmedPostureBreakMarkAndNativeEnergy()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f, true);
            target.AddDamageImmunity(_player);
            yield return EclipseCastQ();
            Assert.AreEqual(10000, target.health); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
            Assert.Greater(_eclipseCombat.Energy, 0); Assert.IsTrue(target.GetComponent<CombatReactionController>().IsAirborne);
            target.RemoveDamageImmunity(_player); EclipseBasic(); float health = target.health; int energy = _eclipseCombat.Energy;
            target.AddDamageImmunity(_player);
            yield return EclipseWaitForFalls(1);
            Assert.AreEqual(health, target.health); Assert.AreEqual(energy, _eclipseCombat.Energy);
            Assert.IsFalse(target.GetComponent<CombatReactionController>().IsAirborne, "Derived posture must never inherit a new Launch.");
        }
        [UnityTest]
        public IEnumerator EclipseUppercutCannotSkipContactAcrossACoarseSimulationStep()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f);
            float before = Time.fixedDeltaTime;
            try
            {
                Time.fixedDeltaTime = .4f;
                yield return EclipseCastQ();
                Assert.Less(target.health, 10000); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
            }
            finally { Time.fixedDeltaTime = before; }
        }

        [UnityTest]
        public IEnumerator EclipsePostureOnlyContactsCanMarkAndFinishWithoutGeneratingEnergy()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f, false, EnemyRank.Boss);
            target.AddDamageImmunity(_player); float before = target.GetComponent<CombatReactionController>().CurrentStance;
            yield return EclipseCastQ();
            Assert.AreEqual(10000, target.health); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
            Assert.AreEqual(0, _eclipseCombat.Energy); EclipseBasic(hpExpected: false);
            yield return EclipseWaitForFalls(1);
            Assert.AreEqual(10000, target.health); Assert.AreEqual(0, _eclipseCombat.Energy);
            Assert.Less(target.GetComponent<CombatReactionController>().CurrentStance, before - 12);
        }
        [UnityTest]
        public IEnumerator EclipseFallComposesWithFrostConductorAndDetonationOnTheFinisherRoot()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f);
            yield return EclipseCastQ();
            foreach (var kind in new[] { ProtocolKind.Frost, ProtocolKind.Conductor, ProtocolKind.Detonation })
                Assert.IsTrue(_eclipseRun.Protocols.TryAcquire(_eclipseRun.Protocols.SessionId, kind, _eclipseRun.Protocols.Revision, _ => true));
            var neighbor = BuildEnemy(new Vector3(1.2f, 0, -.8f), 1).actor;
            // Leave distinct victims for the explosion after Conductor claims its nearest victim.
            BuildEnemy(new Vector3(1.2f, 0, -2f), 10000); BuildEnemy(new Vector3(2.4f, 0, -1f), 10000);
            Physics.SyncTransforms(); var runtime = _player.GetComponent<ProtocolRuntime>(); runtime.SetChillRollForTests(() => 0);
            var events = new System.Collections.Generic.List<ProtocolCombatEvent>();
            _eclipseRun.Hooks.OnProtocolImpact += events.Add;
            long before = runtime.Budget.RootsCreated; EclipseBasic(); int energy = _eclipseCombat.Energy; float mana = _player.mana;
            yield return EclipseWaitForFalls(1);
            long rootId = events.First(e => e.Producer == ProtocolProducer.Direct).RootId;
            Assert.IsTrue(events.Any(e => e.Producer == ProtocolProducer.Technique && e.Depth == 1 && e.RootId == rootId));
            Assert.IsTrue(events.Any(e => e.Producer == ProtocolProducer.Conductor && e.RootId == rootId));
            Assert.IsTrue(neighbor.IsDead); Assert.IsTrue(ProtocolElementQuery.HasChill(target));
            Assert.IsTrue(events.Any(e => e.Producer == ProtocolProducer.Detonation && e.RootId == rootId));
            Assert.That(events.Where(e => e.Producer != ProtocolProducer.Direct).Select(e => e.RootId), Is.All.EqualTo(rootId));
            Assert.LessOrEqual(events.Count(e => e.Producer != ProtocolProducer.Direct), 32);
            Assert.AreEqual(before + 1, runtime.Budget.RootsCreated); Assert.AreEqual(energy, _eclipseCombat.Energy);
            Assert.AreEqual(mana, _player.mana, .001f);
        }

        [UnityTest]
        public IEnumerator EclipseFallCannotDamageAcrossWallsOrHitAllies()
        {
            yield return EclipseRig(); var target = EclipseTarget(Vector3.forward * 1.2f);
            yield return EclipseCastQ();
            var blocked = EclipseTarget(new Vector3(1.7f, 0, 1.2f));
            var ally = EclipseTarget(new Vector3(-1.7f, 0, 1.2f));
            SetPrivate(ally, "_combatAffiliation", CombatAffiliation.Ally);
            var wall = new GameObject("EclipseWall"); _spawned.Add(wall);
            wall.transform.position = new Vector3(.85f, 1, 1.2f); wall.AddComponent<BoxCollider>().size = new Vector3(.1f, 3, 1);
            Physics.SyncTransforms(); EclipseBasic(); float blockedHp = blocked.health, allyHp = ally.health, sourceHp = target.health;
            yield return EclipseWaitForFalls(1);
            Assert.AreEqual(blockedHp, blocked.health); Assert.AreEqual(allyHp, ally.health); Assert.Less(target.health, sourceHp);
        }

        [UnityTest]
        public IEnumerator EclipseTargetDeathAndCanceledLaunchReleaseArmedWaves()
        {
            yield return EclipseRig();
            foreach (bool killed in new[] { false, true })
            {
                if (killed)
                {
                    while (_eclipseHolder.IsAbilityActive(0)) yield return null;
                    _eclipseHolder.ReduceCooldowns(100); yield return null;
                }
                var target = EclipseTarget(Vector3.forward * 1.2f, true);
                yield return EclipseCastQ(); EclipseBasic();
                if (killed) { target.TakeDamage(1000000); Assert.IsTrue(target.IsDead); }
                else target.GetComponent<CombatReactionController>().ConfigureRank(EnemyRank.Normal);
                float health = target.health; yield return new WaitForSeconds(1.8f);
                Assert.AreEqual(health, target.health); Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted);
                Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount); Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().Budget.LiveRoots);
                if (target) UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }
    }
}
