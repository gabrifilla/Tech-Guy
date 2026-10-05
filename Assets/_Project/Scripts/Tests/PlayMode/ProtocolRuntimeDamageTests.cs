using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class ProtocolRuntimeDamageTests
    {
        private GameObject _host;
        private GameObject _target;
        private GameObject _neighbor;
        private ProtocolInventory _inventory;
        private ProtocolRuntime _runtime;
        private Actor _actor;
        private readonly System.Collections.Generic.List<GameObject> _extraTargets = new System.Collections.Generic.List<GameObject>();
        [SetUp] public void SetUp()
        {
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(source.TryCreateSnapshot(out var catalog, out _), Is.True);
            UnityEngine.Object.DestroyImmediate(source);
            _inventory = new ProtocolInventory(Guid.NewGuid(), catalog);
            _host = new GameObject("ProtocolReceiptHost"); _host.SetActive(false);
            var player = _host.AddComponent<PlayerActor>();
            _runtime = _host.AddComponent<ProtocolRuntime>(); _runtime.Configure(player, _inventory);
            // No scene player lifecycle is needed: this fixture exercises the real target HP receipt.
            _runtime.Budget.AdvanceFrame(Time.frameCount + 1, .01f);
            _target = new GameObject("ProtocolReceiptTarget"); _target.SetActive(false);
            _actor = _target.AddComponent<Actor>(); _actor.health = 1000;
            _target.SetActive(true);
        }
        [TearDown] public void TearDown()
        {
            if (_host) UnityEngine.Object.DestroyImmediate(_host);
            if (_target) UnityEngine.Object.DestroyImmediate(_target);
            if (_neighbor) UnityEngine.Object.DestroyImmediate(_neighbor);
            foreach (var target in _extraTargets) if (target) UnityEngine.Object.DestroyImmediate(target);
            _extraTargets.Clear();
        }
        private void Acquire(ProtocolKind kind)
        { Assert.That(_inventory.TryAcquire(_inventory.SessionId, kind, _inventory.Revision, _ => true), Is.True); }
        private void Hit(ProtocolProducer producer)
        {
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory, producer);
            var before = new ProtocolHitSnapshot(_actor);
            float actual = 0;
            using (_actor.CaptureNextDamage(value => actual = value)) _actor.TakeDamage(10);
            _runtime.SubmitConfirmedImpact(root, _actor, before, actual, producer);
            root.Seal();
        }
        [Test] public void ElementCallbackRecyclingTargetCannotApplyFrostToItsNewGeneration()
        {
            _host.AddComponent<AbilityHolder>(); var boons = _host.AddComponent<RunBoons>();
            var hooks = new HookBus(); typeof(RunBoons).GetProperty("Hooks").SetValue(boons, hooks);
            _runtime.Configure(_host.GetComponent<PlayerActor>(), _inventory, boons);
            _runtime.Budget.AdvanceFrame(Time.frameCount + 1, .01f);
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost); _runtime.SetChillRollForTests(() => 0);
            _target.SetActive(true);
            hooks.OnBurn += target => { target.gameObject.SetActive(false); target.RestoreHealthToMax(); target.gameObject.SetActive(true); };
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.health, Is.EqualTo(1000));
            Assert.That(_runtime.BurnApplications, Is.Zero); Assert.That(_runtime.ChillApplications, Is.Zero);
            Assert.That(ProtocolElementQuery.HasChill(_actor), Is.False);
        }
        [Test] public void IgniteDoesNotTickImmediatelyAndOverflowUsesConfirmedDamageOnce()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Overflow);
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.health, Is.EqualTo(985).Within(.001));
            Assert.That(ProtocolElementQuery.HasBurn(_actor), Is.True);
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(1));
            Assert.That(_runtime.Budget.LiveRoots, Is.Zero);
        }
        [Test] public void TechniqueCannotOverflowAndEnvironmentCannotApplyElements()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Overflow);
            Hit(ProtocolProducer.Environment);
            Assert.That(ProtocolElementQuery.HasBurn(_actor), Is.False);
            Hit(ProtocolProducer.Technique);
            Assert.That(_actor.health, Is.EqualTo(980).Within(.001));
            Assert.That(ProtocolElementQuery.HasBurn(_actor), Is.True);
            Assert.That(_runtime.Budget.FrameImpacts, Is.Zero);
        }
        [Test] public void SecondaryAbsorbedDamageStillConsumesAttempt()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Overflow);
            var before = new ProtocolHitSnapshot(_actor);
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            _actor.TakeDamage(10); _actor.AddDamageImmunity(_host);
            _runtime.SubmitConfirmedImpact(root, _actor, before, 10, ProtocolProducer.Direct);
            root.Seal();
            Assert.That(_actor.health, Is.EqualTo(990).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(1));
        }
        [Test] public void TwoOppositeElementApplicationsProduceTwoBurstsWithoutDuplicateDamage()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost);
            _runtime.SetChillRollForTests(() => 0);
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.health, Is.EqualTo(990).Within(.001));
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.health, Is.EqualTo(950).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(2));
            Assert.That(ProtocolElementQuery.HasBurn(_actor), Is.True);
            Assert.That(ProtocolElementQuery.HasChill(_actor), Is.True);
        }
        [Test] public void DetonationUsesPreDeathStatusAndActualHpInsteadOfOverkill()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Detonation);
            Hit(ProtocolProducer.Direct);
            _actor.health = 6;
            _neighbor = new GameObject("ProtocolDetonationNeighbor"); _neighbor.SetActive(false);
            _neighbor.transform.position = new Vector3(2, 0, 0);
            _neighbor.AddComponent<BoxCollider>();
            var neighbor = _neighbor.AddComponent<Actor>(); neighbor.health = 100;
            _neighbor.SetActive(true); Physics.SyncTransforms();
            var before = new ProtocolHitSnapshot(_actor);
            Assert.That(before.Burn, Is.True);
            float actual = 0;
            using (_actor.CaptureNextDamage(amount => actual = amount)) _actor.TakeDamage(100);
            Assert.That(actual, Is.EqualTo(6));
            Assert.That(ProtocolElementQuery.HasBurn(_actor), Is.False, "Death cleanup already removed owned Burn.");
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            _runtime.SubmitConfirmedImpact(root, _actor, before, actual, ProtocolProducer.Direct);
            root.Seal();
            Assert.That(neighbor.health, Is.EqualTo(95.5f).Within(.001));
            Assert.That(_runtime.Budget.FrameQueries, Is.EqualTo(1));
        }
        [Test] public void LethalReceiptKeepsOriginalPositionWhenDeathCallbackMovesSource()
        {
            Acquire(ProtocolKind.Detonation);
            _actor.health = 6;
            _neighbor = new GameObject("ProtocolMovedDeathNeighbor"); _neighbor.SetActive(false);
            _neighbor.transform.position = new Vector3(2, 0, 0); _neighbor.AddComponent<BoxCollider>();
            var neighbor = _neighbor.AddComponent<Actor>(); neighbor.health = 100; _neighbor.SetActive(true);
            Physics.SyncTransforms();
            _actor.Died += actor => actor.transform.position = Vector3.right * 100;
            var before = new ProtocolHitSnapshot(_actor);
            float actual = 0;
            using (_actor.CaptureNextDamage(amount => actual = amount)) _actor.TakeDamage(100);
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            _runtime.SubmitConfirmedImpact(root, _actor, before, actual, ProtocolProducer.Direct);
            _runtime.SubmitConfirmedImpact(root, _actor, before, actual, ProtocolProducer.Direct);
            root.Seal();
            Assert.That(neighbor.health, Is.EqualTo(95.5f).Within(.001));
            Assert.That(_runtime.Budget.FrameQueries, Is.EqualTo(1), "The same death receipt cannot emit twice.");
        }
        [Test] public void BurnApplicationCapRejectsOverflowAndReleasesCapacityOnDeath()
        {
            Acquire(ProtocolKind.Ignite);
            Actor overflowTarget = null;
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            for (int i = 0; i <= 256; i++)
            {
                var target = new GameObject("ProtocolApplicationCapTarget"); target.SetActive(false); _extraTargets.Add(target);
                var actor = target.AddComponent<Actor>(); actor.health = 1000; target.SetActive(true);
                var before = new ProtocolHitSnapshot(actor); actor.TakeDamage(10);
                _runtime.SubmitConfirmedImpact(root, actor, before, 10, ProtocolProducer.Direct);
                if (i == 256) overflowTarget = actor;
            }
            Assert.That(_runtime.BurnApplications, Is.EqualTo(256));
            Assert.That(ProtocolElementQuery.HasBurn(overflowTarget), Is.False);
            _extraTargets[0].GetComponent<Actor>().TakeDamage(10000);
            Assert.That(_runtime.BurnApplications, Is.EqualTo(255));
            var next = new ProtocolHitSnapshot(overflowTarget); overflowTarget.TakeDamage(10);
            _runtime.SubmitConfirmedImpact(root, overflowTarget, next, 10, ProtocolProducer.Direct);
            root.Seal();
            Assert.That(_runtime.BurnApplications, Is.EqualTo(256));
            Assert.That(ProtocolElementQuery.HasBurn(overflowTarget), Is.True);
            _runtime.CloseEncounter(); Assert.That(_runtime.BurnApplications, Is.Zero);
        }
        [Test] public void NonlethalReceiptFromPreviousTargetGenerationCannotApplyElements()
        {
            Acquire(ProtocolKind.Ignite);
            var before = new ProtocolHitSnapshot(_actor);
            _actor.TakeDamage(10);
            _target.SetActive(false); _actor.RestoreHealthToMax(); _target.SetActive(true);
            Assert.That(_actor.CombatGeneration, Is.GreaterThan(before.Handle.Generation));
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            _runtime.SubmitConfirmedImpact(root, _actor, before, 10, ProtocolProducer.Direct); root.Seal();
            Assert.That(ProtocolElementQuery.HasBurn(_actor), Is.False);
            Assert.That(_runtime.BurnApplications, Is.Zero);
        }
        [Test] public void FrostChanceScalesPerRankAndRankThreeIncludesRandomUpperEndpoint()
        {
            Acquire(ProtocolKind.Frost); _runtime.SetChillRollForTests(() => .4f);
            Hit(ProtocolProducer.Direct); Assert.That(ProtocolElementQuery.HasChill(_actor), Is.False);
            Acquire(ProtocolKind.Frost); Hit(ProtocolProducer.Direct);
            Assert.That(ProtocolElementQuery.HasChill(_actor), Is.True);
            _runtime.CloseEncounter();
            Acquire(ProtocolKind.Frost); _runtime.SetChillRollForTests(() => 1f);
            Hit(ProtocolProducer.Direct); Assert.That(ProtocolElementQuery.HasChill(_actor), Is.True);
            _runtime.CloseEncounter(); _runtime.SetChillRollForTests(() => float.NaN);
            Hit(ProtocolProducer.Direct); Assert.That(ProtocolElementQuery.HasChill(_actor), Is.False);
        }
        [Test] public void ReplayedReceiptCannotRefreshElementsOrRepeatOverflow()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Overflow);
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            var before = new ProtocolHitSnapshot(_actor); _actor.TakeDamage(10);
            _runtime.SubmitConfirmedImpact(root, _actor, before, 10, ProtocolProducer.Direct);
            float hp = _actor.health;
            _runtime.SubmitConfirmedImpact(root, _actor, before, 10, ProtocolProducer.Direct);
            root.Seal();
            Assert.That(_actor.health, Is.EqualTo(hp));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(1));
        }
        [TestCase(1, 7.2f)] [TestCase(2, 14.4f)] [TestCase(3, 21.6f)]
        public void ReactorUsesCapturedRanksAndConfirmedDamage(int rank, float expectedDps)
        {
            for (int i = 0; i < rank; i++) Acquire(ProtocolKind.Ignite);
            for (int i = 0; i < rank; i++) Acquire(ProtocolKind.Reactor);
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.GetComponent<ProtocolElementStatus>().BurnDps, Is.EqualTo(expectedDps).Within(.001));
            Assert.That(_actor.health, Is.EqualTo(990).Within(.001));
        }
        [TestCase(1, 981f)] [TestCase(2, 977f)] [TestCase(3, 973f)]
        public void ResonanceAmplifiesOverflowOnceFromPostDirectElementsWithoutAmplifyingDirect(int rank, float expectedHp)
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost); Acquire(ProtocolKind.Conductor);
            for (int i = 0; i < rank; i++) Acquire(ProtocolKind.Resonance);
            Acquire(ProtocolKind.Overflow); _runtime.SetChillRollForTests(() => 0);
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.health, Is.EqualTo(expectedHp).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(1));
        }
        [Test] public void OverflowDeathDetonatesFromItsOwnLethalHpReceipt()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Overflow); Acquire(ProtocolKind.Detonation);
            _actor.health = 12;
            _neighbor = new GameObject("ProtocolOverflowDeathNeighbor"); _neighbor.SetActive(false);
            _neighbor.transform.position = Vector3.right * 2; _neighbor.AddComponent<BoxCollider>();
            var neighbor = _neighbor.AddComponent<Actor>(); neighbor.health = 100; _neighbor.SetActive(true); Physics.SyncTransforms();
            Hit(ProtocolProducer.Direct);
            Assert.That(_actor.IsDead, Is.True);
            Assert.That(neighbor.health, Is.EqualTo(98.5f).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(2));
        }
        private Actor Neighbor(Vector3 position, CombatAffiliation affiliation = CombatAffiliation.Enemy)
        {
            var obj = new GameObject("ProtocolFilteredNeighbor"); obj.SetActive(false); _extraTargets.Add(obj);
            obj.transform.position = position; obj.AddComponent<BoxCollider>();
            var actor = obj.AddComponent<Actor>(); actor.health = 100;
            typeof(Actor).GetField("_combatAffiliation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(actor, affiliation);
            obj.SetActive(true); return actor;
        }
        [Test] public void ConductorSkipsAlliesNeutralWallsAndDuplicateCollidersAndStillConductsLethalHit()
        {
            Acquire(ProtocolKind.Conductor);
            var ally = Neighbor(Vector3.forward, CombatAffiliation.Ally);
            var neutral = Neighbor(Vector3.back, CombatAffiliation.Neutral);
            var blocked = Neighbor(Vector3.right * 2);
            var clear = Neighbor(Vector3.left * 2); clear.gameObject.AddComponent<SphereCollider>();
            var wall = new GameObject("ProtocolLineOfSightWall"); _extraTargets.Add(wall);
            wall.transform.position = Vector3.right; wall.AddComponent<BoxCollider>().size = new Vector3(.3f, 3, 1);
            Physics.SyncTransforms(); _actor.health = 6;
            Hit(ProtocolProducer.Direct);
            Assert.That(ally.health, Is.EqualTo(100)); Assert.That(neutral.health, Is.EqualTo(100));
            Assert.That(blocked.health, Is.EqualTo(100)); Assert.That(clear.health, Is.EqualTo(97.3f).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(1), "Two colliders cannot duplicate the propagated receipt.");
        }
        [Test] public void DepthFourDealsItsConfirmedDamageWithoutCreatingElementsOrChildren()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Conductor); Acquire(ProtocolKind.Detonation); Acquire(ProtocolKind.Overflow);
            var neighbor = Neighbor(Vector3.right * 2); Physics.SyncTransforms();
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory);
            Assert.That(root.ReserveImpact(4), Is.True);
            var before = new ProtocolHitSnapshot(_actor); _actor.TakeDamage(10);
            _runtime.SubmitConfirmedImpact(root, _actor, before, 10, ProtocolProducer.Conductor, 4); root.Seal();
            Assert.That(_actor.health, Is.EqualTo(990)); Assert.That(neighbor.health, Is.EqualTo(100));
            Assert.That(_runtime.BurnApplications, Is.Zero); Assert.That(_runtime.Budget.FrameQueries, Is.Zero);
        }
        [Test] public void ShatterPrecedesCombustionAndLimitsExplosionToSixDistinctVictims()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost); Acquire(ProtocolKind.Detonation);
            _runtime.SetChillRollForTests(() => 0); Hit(ProtocolProducer.Direct);
            var victims = new System.Collections.Generic.List<Actor>();
            for (int i = 0; i < 7; i++)
            { float angle = i * Mathf.PI * 2 / 7; victims.Add(Neighbor(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2)); }
            var outside = Neighbor(Vector3.forward * 4);
            _actor.health = 6; Physics.SyncTransforms(); Hit(ProtocolProducer.Direct);
            Assert.That(victims.FindAll(actor => actor.health < 100).Count, Is.EqualTo(6));
            Assert.That(outside.health, Is.EqualTo(100), "Burn must not enlarge a Chill death's radius.");
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(6));
        }
        [Test] public void CleanupPreservesLegacyStatusAndRebindInvalidatesExistingRoots()
        {
            Acquire(ProtocolKind.Ignite); Hit(ProtocolProducer.Direct);
            var legacy = _target.AddComponent<BurnStatus>();
            var root = _runtime.Budget.BeginRoot(_inventory.SessionId, 0, _inventory); root.ReserveChild(1);
            _runtime.Configure(_host.GetComponent<PlayerActor>(), _inventory);
            Assert.That(root.IsClosed, Is.True); Assert.That(_runtime.BurnApplications, Is.Zero);
            Assert.That(legacy, Is.Not.Null); Assert.That(_target.GetComponent<BurnStatus>(), Is.SameAs(legacy));
        }
        [TestCase(1, 926f)] [TestCase(2, 902f)] [TestCase(3, 878f)]
        public void ResonanceAmplifiesEachThermalShockExactlyOnce(int rank, float expectedHp)
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost); Acquire(ProtocolKind.Conductor);
            for (int i = 0; i < rank; i++) Acquire(ProtocolKind.Resonance);
            _runtime.SetChillRollForTests(() => 0); Hit(ProtocolProducer.Direct); Hit(ProtocolProducer.Direct);
            Assert.That(_actor.health, Is.EqualTo(expectedHp).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(2));
        }
        [Test] public void ThermalShockAndOverflowPreserveExistingTimersAndDoNotPropagateElements()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost); Acquire(ProtocolKind.Conductor); Acquire(ProtocolKind.Overflow);
            _runtime.SetChillRollForTests(() => 0); Hit(ProtocolProducer.Direct);
            var view = _actor.GetComponent<ProtocolElementStatus>(); float burn = view.BurnRemaining, chill = view.ChillRemaining;
            _runtime.SetChillRollForTests(() => throw new System.InvalidOperationException("Bursts must not roll Frost."));
            Hit(ProtocolProducer.ThermalShock); Hit(ProtocolProducer.Overflow);
            Assert.That(view.BurnRemaining, Is.EqualTo(burn)); Assert.That(view.ChillRemaining, Is.EqualTo(chill));
            Assert.That(_actor.health, Is.EqualTo(965).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(1), "Only the original direct hit's Overflow may recurse.");
        }
        [Test] public void SaturatedExplosionBufferCannotExceedRootDamageBudget()
        {
            Acquire(ProtocolKind.Detonation); var victims = new System.Collections.Generic.List<Actor>();
            for (int i = 0; i < 129; i++)
            {
                float angle = i * Mathf.PI * 2 / 129;
                var actor = Neighbor(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2);
                actor.GetComponent<BoxCollider>().size = new Vector3(.001f, 1, .001f); victims.Add(actor);
            }
            _actor.health = 10; Physics.SyncTransforms(); Hit(ProtocolProducer.Direct);
            Assert.That(victims.FindAll(actor => actor.health < 100).Count, Is.EqualTo(32));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(32));
            Assert.That(_runtime.Budget.MaximumQueue, Is.LessThanOrEqualTo(64));
            Assert.That(_runtime.Budget.LiveRoots, Is.Zero);
        }
        [TestCase(1, 92.5f)] [TestCase(2, 90f)] [TestCase(3, 87.5f)]
        public void DetonationDamageScalesFromConfirmedLethalReceiptAtEveryRank(int rank, float hp)
        {
            for (int i = 0; i < rank; i++) Acquire(ProtocolKind.Detonation);
            var neighbor = Neighbor(Vector3.right * 2); _actor.health = 10; Physics.SyncTransforms(); Hit(ProtocolProducer.Direct);
            Assert.That(neighbor.health, Is.EqualTo(hp).Within(.001));
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ConductorRankControlsVictimsPerEmissionWithoutChangingItsDamageFactor(int rank)
        {
            for (int i = 0; i < rank; i++) Acquire(ProtocolKind.Conductor);
            var victims = new System.Collections.Generic.List<Actor>();
            for (int i = 0; i < 3; i++)
            { float angle = i * Mathf.PI * 2 / 3; victims.Add(Neighbor(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 3)); }
            Physics.SyncTransforms(); Hit(ProtocolProducer.Direct);
            Assert.That(victims.FindAll(actor => actor.health < 100).Count, Is.EqualTo(rank));
            foreach (var victim in victims) if (victim.health < 100) Assert.That(victim.health, Is.EqualTo(95.5f).Within(.001));
        }
        [Test] public void FirstThermalBurstDeathCancelsRemainingBurstsAndDetonatesItsOwnDamage()
        {
            Acquire(ProtocolKind.Ignite); Acquire(ProtocolKind.Frost); Acquire(ProtocolKind.Detonation); Acquire(ProtocolKind.Overflow);
            _runtime.SetChillRollForTests(() => 0); Hit(ProtocolProducer.Direct);
            var neighbor = Neighbor(Vector3.right * 2); _actor.health = 11; Physics.SyncTransforms(); Hit(ProtocolProducer.Direct);
            Assert.That(_actor.IsDead, Is.True); Assert.That(neighbor.health, Is.EqualTo(99.25f).Within(.001));
            Assert.That(_runtime.Budget.FrameImpacts, Is.EqualTo(3), "Original Overflow + lethal shock + its Detonation; remaining local bursts were canceled.");
        }
    }
}
