using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private AbilityHolder _spearHolder;
        private RunBoons _spearRun;
        private SpearBoonCombat _spearCombat;
        private IEnumerator SpearBoonRig(bool horizon = true, bool siege = true, bool orbit = true, bool supports = false, bool preexistingExecutor = false)
        {
            if (!_navMesh) Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.gameObject.AddComponent<AudioListener>();
            _spearHolder = _player.gameObject.AddComponent<AbilityHolder>();
            _spearHolder.keys = new[] { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Spear/Spear")); _spearHolder.RefreshLoadout();
            // This rig omits pointer presentation and dash; consume only their known setup warnings.
            LogAssert.Expect(LogType.Warning, "Efeito de clique não configurado!");
            LogAssert.Expect(LogType.Warning, "DashScript nao configurado!");
            if (!Camera.main) LogAssert.Expect(LogType.Warning, "Main camera nao configurada!");
            var authoredExecutor = preexistingExecutor ? _player.gameObject.AddComponent<SpearBoonCombat>() : null;
            _spearRun = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var ids = new List<string>();
            if (horizon) ids.Add(SpearBoonCatalog.HorizonId);
            if (siege) ids.Add(SpearBoonCatalog.SiegeId);
            if (orbit) ids.Add(SpearBoonCatalog.OrbitId);
            if (supports) ids.AddRange(new[] { SpearBoonCatalog.ReturnMarkId, SpearBoonCatalog.MoonEchoId, SpearBoonCatalog.ReturnLineId });
            foreach (string id in ids)
                Assert.IsTrue(BoonExpansionCatalog.TryAcquire(id, _spearRun.WeaponModifiers, _spearRun.RunWeapon, _spearRun.Protocols, out var error), id + ": " + error);
            _spearHolder.RefreshLoadout(); _spearCombat = _player.GetComponent<SpearBoonCombat>();
            Assert.IsNotNull(_spearCombat, "The equipped spear must configure its real expansion executor.");
            if (preexistingExecutor)
            {
                Assert.AreSame(authoredExecutor, _spearCombat);
                Assert.AreSame(authoredExecutor, typeof(AbilityHolder).GetField("_spearBoonCombat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_spearHolder));
                Assert.AreEqual(1, _player.GetComponents<SpearBoonCombat>().Length);
            }
            _spearRun.RunWeapon.attackDamage = 10f; _player.Stats.criticalChance = 0f;
            SetPrivate(_player, "baseMaxMana", 1000f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(true); _player.transform.rotation = Quaternion.identity;
        }
        private IEnumerator SpearBoonWait(Func<bool> complete, float seconds = 6f)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!complete() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(complete(), "Spear scenario exceeded its real-time guard.");
        }
        private ArsenalAbility SpearBoonSkill(int slot) => (ArsenalAbility)_spearRun.RunWeapon.abilities[slot];
        private float SpearBoonNominal(int slot)
        {
            var skill = SpearBoonSkill(slot); var plan = _spearRun.WeaponModifiers.Plan(skill, slot);
            return _player.Stats.RollAttackDamage(10, plan.Damage * skill.Hits * (slot == 2 ? plan.Directions : 1)).Amount;
        }

        [UnityTest]
        public IEnumerator SpearBoonReusesAPreauthoredExecutorWhenTheRunBindsItsFirstMutation()
        {
            yield return SpearBoonRig(true, false, false, preexistingExecutor: true);
            float mana = _player.mana;
            Assert.IsTrue(_spearHolder.TryUseAbility(0));
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.IsTrue(_spearCombat.HasHorizon);
            Assert.AreEqual(mana - SpearBoonSkill(0).ManaCost, _player.mana, .01f);
        }

        [UnityTest]
        public IEnumerator SpearBoonHorizonUsesOneCostAndCrossingConsumesItsOriginalRootOnce()
        {
            yield return SpearBoonRig(true, false, false);
            Actor victim = BuildEnemy(Vector3.forward * 3, 10000).actor; victim.gameObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms(); float nominal = SpearBoonNominal(0), mana = _player.mana;
            var events = new List<ProtocolCombatEvent>(); _spearRun.Hooks.OnProtocolImpact += events.Add;
            int uses = 0; _spearHolder.AbilityUsed += _ => uses++;
            Assert.IsTrue(_spearHolder.TryUseAbility(0));
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.IsTrue(_spearCombat.HasHorizon); Assert.AreEqual(10000 - nominal * .6f, victim.health, .1f);
            Assert.AreEqual(mana - SpearBoonSkill(0).ManaCost, _player.mana, .01f); Assert.AreEqual(1, uses);
            long root = events.First(e => e.Producer == ProtocolProducer.Direct).RootId;
            _spearCombat.NotifyNativePath(2, new Vector3(-2, 0, 3), new Vector3(2, 0, 3));
            Assert.IsFalse(_spearCombat.HasHorizon); Assert.AreEqual(1, _spearCombat.HorizonDetonations);
            Assert.AreEqual(10000 - nominal, victim.health, .1f);
            _spearCombat.NotifyNativePath(2, new Vector3(-2, 0, 3), new Vector3(2, 0, 3));
            Assert.AreEqual(10000 - nominal, victim.health, .1f);
            Assert.AreEqual(1, events.Count(e => e.Producer == ProtocolProducer.Technique));
            Assert.That(events.Select(e => e.RootId), Is.All.EqualTo(root)); Assert.AreEqual(1, uses);
        }
        [UnityTest]
        public IEnumerator SpearBoonDerivedHorizonDropsWhenTheFrameQueryBudgetIsExhausted()
        {
            yield return SpearBoonRig(true, false, false);
            Actor victim = BuildEnemy(Vector3.forward * 3, 10000).actor;
            Physics.SyncTransforms();
            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting && !_player.IsCombatHitStopped);
            Assert.IsTrue(_spearCombat.HasHorizon);
            var runtime = _player.GetComponent<ProtocolRuntime>();
            Assert.Greater(runtime.ScaledDelta, 0);
            int queries = runtime.Inventory.Catalog.Limits.QueriesPerFrame;
            for (int i = runtime.Budget.FrameQueries; i < queries; i++) Assert.IsTrue(runtime.Budget.ReserveQuery());
            int dropped = runtime.Budget.DroppedQueries;
            float health = victim.health;

            _spearCombat.NotifyNativePath(2, new Vector3(-2, 0, 3), new Vector3(2, 0, 3));
            Assert.AreEqual(health, victim.health, .01f, "A consumed line cannot query or apply derived damage beyond the frame budget.");
            Assert.AreEqual(queries, runtime.Budget.FrameQueries);
            Assert.AreEqual(dropped + 1, runtime.Budget.DroppedQueries);
            Assert.IsFalse(_spearCombat.HasHorizon);
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }
        [UnityTest]
        public IEnumerator SpearBoonDerivedHorizonHonorsTheConfiguredSightTestLimit()
        {
            yield return SpearBoonRig(true, false, false);
            var victims = new List<Actor>();
            for (int i = 0; i < 4; i++) victims.Add(BuildEnemy(Vector3.forward * (2 + i), 10000).actor);
            Physics.SyncTransforms();
            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting && !_player.IsCombatHitStopped);
            var runtime = _player.GetComponent<ProtocolRuntime>();
            SetPrivate(runtime.Inventory.Catalog.Limits, "_sightTestsPerQuery", 2);
            float[] health = victims.Select(v => v.health).ToArray();
            Assert.Greater(runtime.ScaledDelta, 0);
            int queries = runtime.Budget.FrameQueries;

            _spearCombat.NotifyNativePath(2, new Vector3(-2, 0, 3), new Vector3(2, 0, 3));
            Assert.AreEqual(2, victims.Where((v, i) => v.health < health[i]).Count(), "The descendant query must stop testing targets at the owned catalog limit.");
            Assert.AreEqual(queries + 1, runtime.Budget.FrameQueries);
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }
        [UnityTest]
        public IEnumerator SpearBoonChainCannotSearchPastTheSharedFrameQueryBudget()
        {
            yield return SpearBoonRig(true, false, false);
            Assert.IsTrue(_spearRun.WeaponModifiers.Add(WeaponRunModifiers.Catalog.First(d => d.Kind == WeaponBoon.ChainThrust)));
            Actor primary = BuildEnemy(Vector3.forward * 3, 10000).actor;
            Actor next = BuildEnemy(new Vector3(2, 0, 3), 10000).actor;
            var runtime = _player.GetComponent<ProtocolRuntime>();
            int confirmations = 0, dropped = runtime.Budget.DroppedQueries;
            primary.DamageReceived += (_, __) =>
            {
                confirmations++;
                Assert.Greater(runtime.ScaledDelta, 0);
                int limit = runtime.Inventory.Catalog.Limits.QueriesPerFrame;
                for (int i = runtime.Budget.FrameQueries; i < limit; i++) Assert.IsTrue(runtime.Budget.ReserveQuery());
            };
            Physics.SyncTransforms();

            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting && confirmations > 0);
            Assert.AreEqual(1, confirmations);
            Assert.AreEqual(10000, next.health, .01f, "A retained chain must drop its secondary search when the native hit used the shared query allowance.");
            Assert.AreEqual(dropped + 1, runtime.Budget.DroppedQueries);
            _spearCombat.StopEncounter();
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }
        [UnityTest]
        public IEnumerator SpearBoonStakeProximityDefersUntilTheSharedFrameQueryBudgetRecovers()
        {
            yield return SpearBoonRig(false, true, false);
            _spearCombat.Use(SpearBoonSkill(2), Vector3.forward * 4);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.AreEqual(6, _spearCombat.ActiveStakes);
            Actor victim = BuildEnemy(Vector3.forward * 7, 10000).actor;
            Physics.SyncTransforms();
            var runtime = _player.GetComponent<ProtocolRuntime>();
            Assert.Greater(runtime.ScaledDelta, 0);
            int limit = runtime.Inventory.Catalog.Limits.QueriesPerFrame;
            for (int i = runtime.Budget.FrameQueries; i < limit; i++) Assert.IsTrue(runtime.Budget.ReserveQuery());
            int dropped = runtime.Budget.DroppedQueries;
            var tick = typeof(SpearBoonCombat).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.DoesNotThrow(() => tick.Invoke(_spearCombat, null));
            Assert.AreEqual(10000, victim.health, .01f);
            Assert.AreEqual(0, _spearCombat.StakePulses);
            Assert.AreEqual(6, _spearCombat.ActiveStakes);
            Assert.AreEqual(dropped + 6, runtime.Budget.DroppedQueries);
            yield return SpearBoonWait(() => _spearCombat.StakePulses > 0);
            Assert.Less(victim.health, 10000, "An unspent stake may retry on a later gameplay frame with query capacity.");
            _spearCombat.StopEncounter();
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }
        [UnityTest]
        public IEnumerator SpearBoonHorizonRejectsParallelEAndExpiresWithoutAutoDetonation()
        {
            yield return SpearBoonRig(true, false, false);
            Actor victim = BuildEnemy(Vector3.forward * 3, 10000).actor; Physics.SyncTransforms();
            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            float health = victim.health;
            _spearCombat.NotifyNativePath(2, Vector3.right, Vector3.right + Vector3.forward * 6);
            Assert.IsTrue(_spearCombat.HasHorizon); Assert.AreEqual(health, victim.health);
            yield return SpearBoonWait(() => !_spearCombat.HasHorizon, 8);
            Assert.AreEqual(health, victim.health); Assert.AreEqual(0, _spearCombat.HorizonDetonations);
            Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().Budget.LiveRoots);
        }
        [UnityTest]
        public IEnumerator SpearBoonSiegeHasNoCollidersAndEachStakePulsesAtMostOnce()
        {
            yield return SpearBoonRig(false, true, false);
            Actor victim = BuildEnemy(Vector3.forward * 7, 10000).actor; Physics.SyncTransforms();
            _spearCombat.Use(SpearBoonSkill(2), Vector3.forward * 4);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.AreEqual(1, _spearCombat.StakePulses); Assert.AreEqual(5, _spearCombat.ActiveStakes);
            Assert.AreEqual(0, _spearCombat.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>().Length);
            foreach (var cue in _spearCombat.GetComponentsInChildren<LineRenderer>())
                if (cue.name == "Spear projection cue") Assert.IsNull(cue.GetComponent<Collider>(), "Projection cues cannot pretend to imprison enemies.");
            float health = victim.health;
            _spearCombat.NotifyNativePath(0, Vector3.zero, Vector3.forward * 8);
            int pulses = _spearCombat.StakePulses;
            _spearCombat.NotifyNativePath(0, Vector3.zero, Vector3.forward * 8);
            Assert.AreEqual(pulses, _spearCombat.StakePulses);
            Assert.GreaterOrEqual(pulses, 1); Assert.LessOrEqual(pulses, 6);
            Assert.AreEqual(health, victim.health, .01f, "The already consumed northern stake cannot pulse again.");
        }
        [UnityTest]
        public IEnumerator SpearBoonContactStakeDamageCanEndEncounterInsideItsCallback()
        {
            yield return SpearBoonRig(false, true, false);
            _spearCombat.Use(SpearBoonSkill(2), Vector3.forward * 4);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.AreEqual(6, _spearCombat.ActiveStakes);
            Actor victim = BuildEnemy(Vector3.forward * 7, 10000).actor;
            int callbacks = 0;
            victim.DamageReceived += (_, __) => { callbacks++; _spearCombat.StopEncounter(); };
            Physics.SyncTransforms();

            yield return SpearBoonWait(() => callbacks > 0);
            float health = victim.health;
            Assert.AreEqual(1, callbacks);
            Assert.AreEqual(1, _spearCombat.StakePulses);
            Assert.AreEqual(0, _spearCombat.ActiveStakes);
            Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().Budget.LiveRoots);
            yield return null;
            Assert.AreEqual(health, victim.health, .01f, "Teardown inside a contact pulse must prevent later stake damage.");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator SpearBoonPathStakeDamageCanDisableTheExecutorInsideItsCallback()
        {
            yield return SpearBoonRig(false, true, false);
            _spearCombat.Use(SpearBoonSkill(2), Vector3.forward * 4);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.AreEqual(6, _spearCombat.ActiveStakes);
            // The full path crosses the south stake first; its first damaging pulse must cancel the rest.
            Actor victim = BuildEnemy(Vector3.forward, 10000).actor;
            int callbacks = 0;
            victim.DamageReceived += (_, __) => { callbacks++; _spearCombat.enabled = false; };
            Physics.SyncTransforms();

            Assert.DoesNotThrow(() => _spearCombat.NotifyNativePath(0, Vector3.zero, Vector3.forward * 8));
            Assert.AreEqual(1, callbacks);
            Assert.AreEqual(1, _spearCombat.StakePulses);
            Assert.AreEqual(0, _spearCombat.ActiveStakes);
            Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().Budget.LiveRoots);
            float health = victim.health;
            yield return null;
            Assert.AreEqual(health, victim.health, .01f);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator SpearBoonOrbitHitsOncePerTurnAndReturnWhileReleasingPlayerActions()
        {
            yield return SpearBoonRig(false, false, true);
            Actor victim = BuildEnemy(Vector3.forward * 8.5f, 10000).actor; victim.gameObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms(); float nominal = SpearBoonNominal(3);
            int confirmations = 0; victim.DamageReceived += (_, __) => confirmations++;
            _spearCombat.Use(SpearBoonSkill(3), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            Assert.IsTrue(_spearCombat.HasOrbit); Assert.IsFalse(_spearHolder.IsCasting); Assert.IsFalse(_agent.isStopped);
            yield return SpearBoonWait(() => !_spearCombat.HasOrbit, 7);
            Assert.AreEqual(4, confirmations); Assert.AreEqual(10000 - nominal, victim.health, .15f);
            Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().Budget.LiveRoots);
        }
        [UnityTest]
        public IEnumerator SpearBoonEarlyReturnLosesSkippedPassagesAndNeverPaysAgain()
        {
            yield return SpearBoonRig(false, false, true);
            float mana = _player.mana; int uses = 0; _spearHolder.AbilityUsed += _ => uses++;
            Assert.IsTrue(_spearHolder.TryUseAbility(3));
            yield return SpearBoonWait(() => _spearCombat.HasOrbit);
            float paid = _player.mana;
            Assert.IsTrue(_spearCombat.TryReturnOrbit()); Assert.IsFalse(_spearCombat.TryReturnOrbit());
            yield return SpearBoonWait(() => !_spearCombat.HasOrbit, 3);
            Assert.AreEqual(mana - SpearBoonSkill(3).ManaCost, paid, .01f);
            Assert.AreEqual(paid, _player.mana, .01f); Assert.AreEqual(1, uses);
        }
        [UnityTest]
        public IEnumerator SpearBoonReturnMarkDuoAndMoonEchoStayOnOneRootWithoutNewOrbit()
        {
            yield return SpearBoonRig(false, false, true, true);
            Actor victim = BuildEnemy(Vector3.forward * 8.5f, 10000).actor;
            Actor echoVictim = BuildEnemy(Vector3.forward * .3f, 10000).actor; Physics.SyncTransforms();
            float nominal = SpearBoonNominal(3);
            var events = new List<ProtocolCombatEvent>(); _spearRun.Hooks.OnProtocolImpact += events.Add;
            var runtime = _player.GetComponent<ProtocolRuntime>(); long created = runtime.Budget.RootsCreated;
            _spearCombat.Use(SpearBoonSkill(3), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.HasOrbit && !_spearCombat.IsExecuting, 8);
            Assert.AreEqual(5, events.Count(e => e.Producer == ProtocolProducer.Direct));
            Assert.AreEqual(1, events.Count(e => e.Producer == ProtocolProducer.Technique));
            Assert.AreEqual(1, events.Count(e => e.Producer == ProtocolProducer.Echo));
            Assert.That(events.Select(e => e.RootId).Distinct().Count(), Is.EqualTo(1));
            Assert.AreEqual(created + 1, runtime.Budget.RootsCreated); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            // The return mark occurs at the far point. The reduced echo cuts the actual final point at the player.
            Assert.AreEqual(10000 - nominal * 1.5f, victim.health, .2f);
            Assert.AreEqual(10000 - nominal * .375f, echoVictim.health, .2f);
        }
        [UnityTest]
        public IEnumerator SpearBoonGameplayClocksAndExtraInputFreezeDuringPauseMenuAndHitstop()
        {
            yield return SpearBoonRig();
            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            float clock = _spearCombat.GameplayTime;
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(clock, _spearCombat.GameplayTime); Assert.IsTrue(_spearCombat.HasHorizon);
            Time.timeScale = 1; _spearHolder.SetManualInitializationBlocked(true);
            yield return new WaitForSecondsRealtime(.15f); Assert.AreEqual(clock, _spearCombat.GameplayTime);
            Assert.IsFalse(_spearCombat.CanUse(SpearBoonSkill(3)));
            _spearHolder.SetManualInitializationBlocked(false);
            _player.GetComponent<HitStopRunner>().Apply(.3f);
            yield return new WaitForSecondsRealtime(.1f); Assert.AreEqual(clock, _spearCombat.GameplayTime);
            yield return SpearBoonWait(() => _spearCombat.GameplayTime > clock);
        }
        [UnityTest]
        public IEnumerator SpearBoonWallsBlockHorizonAndOrbitWithoutTeleportingThePlayer()
        {
            yield return SpearBoonRig();
            var wall = new GameObject("Spear test wall"); _spawned.Add(wall);
            wall.transform.position = new Vector3(0, 1, 2); wall.AddComponent<BoxCollider>().size = new Vector3(8, 3, .2f);
            Actor victim = BuildEnemy(Vector3.forward * 4, 10000).actor; Physics.SyncTransforms();
            Vector3 start = _player.transform.position;
            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting); Assert.AreEqual(10000, victim.health);
            _spearCombat.Use(SpearBoonSkill(3), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.HasOrbit && !_spearCombat.IsExecuting);
            Assert.AreEqual(10000, victim.health); Assert.Less(Vector3.Distance(start, _player.transform.position), .03f);
        }
        [UnityTest]
        public IEnumerator SpearBoonStopEncounterAndWeaponSwapReleaseEveryReservation()
        {
            yield return SpearBoonRig();
            _spearCombat.Use(SpearBoonSkill(0), Vector3.forward * 6);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            _spearCombat.Use(SpearBoonSkill(2), Vector3.forward * 4);
            yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
            var runtime = _player.GetComponent<ProtocolRuntime>(); Assert.Greater(runtime.Budget.LiveRoots, 0);
            _spearCombat.StopEncounter();
            Assert.AreEqual(0, runtime.Budget.LiveRoots); Assert.IsFalse(_spearCombat.HasHorizon); Assert.AreEqual(0, _spearCombat.ActiveStakes);
            _spearCombat.Use(SpearBoonSkill(3), Vector3.forward * 6);
            yield return SpearBoonWait(() => _spearCombat.HasOrbit);
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow")); yield return null;
            Assert.IsFalse(_spearCombat.HasOrbit); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            Assert.IsFalse(_agent.isStopped);
        }
        [UnityTest]
        public IEnumerator SpearBoonDerivedStakePulsesRespectThirtyTwoAcrossDenseTargets()
        {
            foreach (int density in new[] { 8, 32, 64 })
            {
                yield return SpearBoonRig(false, true, false);
                for (int i = 0; i < density; i++) BuildEnemy(new Vector3((i % 8 - 3.5f) * .025f, 0, 7 + (i / 8) * .02f), 10000);
                Physics.SyncTransforms(); int direct = 0, derived = 0;
                var roots = new HashSet<long>();
                _spearRun.Hooks.OnProtocolImpact += e => { roots.Add(e.RootId); if (e.Producer == ProtocolProducer.Direct) direct++; else derived++; };
                _spearCombat.Use(SpearBoonSkill(2), Vector3.forward * 4);
                yield return SpearBoonWait(() => !_spearCombat.IsExecuting);
                Assert.AreEqual(density, direct); Assert.AreEqual(Math.Min(density, 32), derived);
                Assert.AreEqual(1, roots.Count); _spearCombat.StopEncounter();
                Assert.AreEqual(0, _player.GetComponent<ProtocolRuntime>().Budget.LiveRoots);
                TearDown(); yield return null;
            }
        }
    }
}
