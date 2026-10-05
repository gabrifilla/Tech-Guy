using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class GauntletEnergyPolicyTests
    {
        private GameObject _object;
        private AsuraMomentum _meter;
        private GauntletContextRegistry _contexts;
        private GauntletEnergyPolicy _policy;
        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("GauntletPolicyTest");
            _contexts = new GauntletContextRegistry(_object.AddComponent<Actor>());
            _meter = new AsuraMomentum();
            _policy = new GauntletEnergyPolicy(_meter, _contexts, new GauntletEnergyAuthoring());
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_object); }
        private GauntletImpactContext Impact(GauntletActionSlot slot, bool shock = false, int target = 1,
            int momentum = 0, int fist = 0) => _contexts.Begin(slot, shock, momentum, fist).ForImpact(0, target, 0);

        [Test]
        public void GuardChildAndBasicCommitBeforeAnyObserverAndCloseWithoutReplay()
        {
            var root = _contexts.Begin(GauntletActionSlot.Basic, asuraFistRank: 3);
            var direct = root.ForImpact(0, 1, 0);
            var guard = _contexts.BeginChild(root, GauntletImpactOrigin.Technique,
                WeaponRunModifiers.CatalogId(WeaponBoon.GuardBreaker)).ForImpact(0, 1, 0);
            int observed = 0;
            _policy.Committed += gain => { Assert.AreEqual(29, _policy.Energy); observed++; };
            using (var transaction = new GauntletImpactTransaction(_policy, direct, null))
            {
                transaction.ConfirmHp(10f);
                transaction.Notify(() => { Assert.AreEqual(29, _policy.Energy); observed++; });
                using (var child = new GauntletImpactTransaction(_policy, guard, null, transaction))
                {
                    child.ConfirmBreak(1);
                    child.Notify(() => { Assert.AreEqual(29, _policy.Energy); observed++; });
                }
                Assert.AreEqual(0, _policy.Energy); Assert.AreEqual(0, observed);
            }
            Assert.AreEqual(4, observed);
            Assert.AreEqual(GauntletGainBlock.StaleContext, _policy.ConfirmImpact(guard, 0f, 1).Block);
            _policy.Close(root); Assert.AreEqual(0, _policy.RetainedRootCount);
        }

        // Feature: gauntlet-2-rework, Properties 1/3/5. Validates: Requirements 2.2, 2.3, 2.6.
        [Test]
        public void ConfirmedSkillsAlternateButEmptyRejectedAndBasicDoNotChangeCategory()
        {
            Assert.AreEqual(10, _policy.ConfirmImpact(Impact(GauntletActionSlot.Q), 1).Requested);
            Assert.AreEqual(10, _policy.ConfirmImpact(Impact(GauntletActionSlot.W), 1).Requested);
            Assert.AreEqual(0, _policy.ConfirmImpact(Impact(GauntletActionSlot.E, true), 0).Requested);
            Assert.AreEqual(3, _policy.ConfirmImpact(Impact(GauntletActionSlot.Basic), 1).Requested);
            var shock = _policy.ConfirmImpact(Impact(GauntletActionSlot.E, true), 0, 1);
            Assert.AreEqual(10, shock.Skill); Assert.AreEqual(15, shock.Alternation); Assert.AreEqual(20, shock.StanceBreak);
            Assert.AreEqual(68, _meter.Energy);
            var cancelled = Impact(GauntletActionSlot.Q);
            _policy.Close(cancelled);
            Assert.AreEqual(GauntletGainBlock.StaleContext, _policy.ConfirmImpact(cancelled, 1).Block);
            Assert.AreEqual(68, _meter.Energy);
        }

        [Test]
        public void MultiTargetBasicPaysNativeAndTechniqueOnlyOnceAndCancelDoesNotRefund()
        {
            var root = _contexts.Begin(GauntletActionSlot.Basic, asuraFistRank: 3);
            Assert.AreEqual(9, _policy.ConfirmImpact(root.ForImpact(0, 1, 0), 9f).Requested);
            Assert.AreEqual(0, _policy.ConfirmImpact(root.ForImpact(0, 2, 0), 9f).Requested);
            Assert.AreEqual(0, _policy.ConfirmImpact(root.ForImpact(1, 1, 0), 9f).Requested);
            _policy.Close(root);
            Assert.AreEqual(9, _meter.Energy);
            Assert.AreEqual(0, _policy.RetainedRootCount);
            Assert.AreEqual(0, _policy.ConfirmImpact(root.ForImpact(0, 1, 0), 9f).Requested);
        }

        [Test]
        public void DistinctBreaksRespectRootLimitAndDoNotDependOnHpOrPhysicalReaction()
        {
            var root = _contexts.Begin(GauntletActionSlot.E, true);
            var first = root.ForImpact(0, 1, 0);
            Assert.AreEqual(30, _policy.ConfirmImpact(first, 0, 1).Requested);
            Assert.AreEqual(0, _policy.ConfirmImpact(first, 0, 1).Requested);
            Assert.AreEqual(20, _policy.ConfirmImpact(root.ForImpact(0, 2, 0), 0, 1).Requested);
            Assert.AreEqual(0, _policy.ConfirmImpact(root.ForImpact(0, 3, 0), 0, 1).Requested);
            Assert.AreEqual(50, _meter.Energy);
            var basic = _contexts.Begin(GauntletActionSlot.Basic);
            var guard = _contexts.BeginChild(basic, GauntletImpactOrigin.Technique, WeaponRunModifiers.CatalogId(WeaponBoon.GuardBreaker));
            var gain = _policy.ConfirmImpact(guard.ForImpact(0, 4, 0), 0, 2);
            Assert.AreEqual(20, gain.StanceBreak); Assert.AreEqual(0, gain.Basic);
        }

        // Feature: gauntlet-2-rework, Properties 2/7. Validates: Requirements 2.5, 2.8, 3.2.
        [Test]
        public void AllParcelsCommitBeforeReentrantObserverStartsR()
        {
            _policy.AddExternal(60);
            var root = _contexts.Begin(GauntletActionSlot.E, true, momentumRank: 1).ForImpact(0, 1, 0);
            var r = _contexts.Begin(GauntletActionSlot.R);
            int callbacks = 0;
            _policy.Committed += gain =>
            {
                callbacks++;
                Assert.AreEqual(100, _meter.Energy);
                Assert.AreEqual(40, gain.Requested);
                Assert.AreEqual(0, _policy.ConfirmImpact(root, 1, 1).Requested);
                Assert.IsTrue(_policy.TryStartAsura(r, out _));
            };
            var result = _policy.ConfirmImpact(root, 1, 1);
            Assert.AreEqual(100, result.After); Assert.AreEqual(40, result.Effective);
            Assert.AreEqual(1, callbacks); Assert.AreEqual(0, _meter.Energy); Assert.IsTrue(_policy.IsAsuraActive);
        }

        [Test]
        public void RConsumesOnceReserveIsSeparateAndBlockedReceiptsNeverReplay()
        {
            var r = _contexts.Begin(GauntletActionSlot.R, reserveRank: 3);
            Assert.IsFalse(_policy.TryStartAsura(r, out _)); Assert.AreEqual(0, _meter.Energy);
            _policy.AddExternal(100);
            Assert.IsTrue(_policy.TryStartAsura(r, out var reserve)); Assert.AreEqual(60, reserve.Reserve);
            Assert.IsFalse(_policy.TryStartAsura(r, out _));
            var previous = Impact(GauntletActionSlot.Q);
            Assert.AreEqual(GauntletGainBlock.AsuraActive, _policy.ConfirmImpact(previous, 1, 1).Block);
            Assert.IsFalse(_policy.AddExternal(100));
            _policy.EndAsura(r);
            Assert.AreEqual(0, _policy.ConfirmImpact(previous, 1, 1).Requested);
            Assert.AreEqual(60, _meter.Energy);
            _policy.AddExternal(100);
            Assert.IsFalse(_policy.TryStartAsura(r, out _)); Assert.AreEqual(100, _meter.Energy);
            Assert.AreEqual(GauntletGainBlock.IneligibleOrigin, _policy.ConfirmImpact(r.ForImpact(0, 1, 0), 1, 1).Block);
        }

        [Test]
        public void DescendantsAndOtherFamiliesCannotGenerateEnergy()
        {
            var direct = _contexts.Begin(GauntletActionSlot.W);
            foreach (var origin in new[] { GauntletImpactOrigin.Echo, GauntletImpactOrigin.Persistent, GauntletImpactOrigin.External })
            {
                var child = _contexts.BeginChild(direct, origin, "producer").ForImpact(0, 1, 0);
                Assert.AreEqual(0, _policy.ConfirmImpact(child, 1, 1).Requested);
            }
            var bow = _contexts.Begin(GauntletActionSlot.Q, family: RunWeaponFamily.Bow).ForImpact(0, 1, 0);
            Assert.AreEqual(0, _policy.ConfirmImpact(bow, 1, 1).Requested);
            Assert.AreEqual(0, _meter.Energy);
        }

        [Test]
        public void InvalidInputsAndMismatchedReceiptsDoNotAlterEnergy()
        {
            var first = Impact(GauntletActionSlot.Q);
            foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
                Assert.AreEqual(GauntletGainBlock.InvalidReceipt, _policy.ConfirmImpact(first, invalid).Block);
            Assert.IsFalse(_policy.AddExternal(-1));
            var other = first.ForImpact(0, 2, 0);
            var result = _policy.Confirm(new ConfirmedHpReceipt(first, 1), new ConfirmedBreakReceipt(other, 1));
            Assert.AreEqual(GauntletGainBlock.InvalidReceipt, result.Block); Assert.AreEqual(0, _meter.Energy);
        }

        // Feature: gauntlet-2-rework, Properties 6/12. Validates: Requirements 2.7, 7.7.
        [Test]
        public void GeneratedGainsSaturateWithoutOverflowAndLifecycleReleasesState()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                _policy.Reset();
                _policy.AddExternal(rng.Next(0, 101));
                _policy.AddExternal(int.MaxValue);
                Assert.AreEqual(100, _meter.Energy);
                var hit = Impact(GauntletActionSlot.Basic, fist: 3);
                Assert.AreEqual(0, _policy.ConfirmImpact(hit, 1).Effective);
                _meter.TryConsume();
                Assert.AreEqual(0, _policy.ConfirmImpact(hit, 1).Requested);
                _policy.Close(hit);
                Assert.AreEqual(0, _policy.RetainedRootCount);
                _policy.Reset();
                Assert.AreEqual(0, _contexts.Count); Assert.AreEqual(0, _meter.Energy);
                Assert.AreEqual(GauntletGainBlock.StaleContext, _policy.ConfirmImpact(hit, 1).Block);
            });
        }
    }
}
