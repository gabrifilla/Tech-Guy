using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class ProtocolBudgetTests
    {
        private ProtocolInventory Fresh()
        {
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(source.TryCreateSnapshot(out var snapshot, out _), Is.True); UnityEngine.Object.DestroyImmediate(source);
            return new ProtocolInventory(Guid.NewGuid(), snapshot);
        }
        [Test] public void MultipleRootsShareFrameCapAndPauseDoesNotProduceDamage()
        {
            var inventory = Fresh(); var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            for (int i = 0; i < 8; i++)
            {
                var root = budget.BeginRoot(inventory.SessionId, 1, inventory);
                for (int hit = 0; hit < 32; hit++) Assert.That(root.ReserveImpact(1), Is.True);
                Assert.That(root.ReserveImpact(1), Is.False); root.Seal();
            }
            var next = budget.BeginRoot(inventory.SessionId, 1, inventory);
            Assert.That(next.ReserveImpact(1), Is.False);
            budget.AdvanceFrame(1, .1f); Assert.That(next.ReserveImpact(1), Is.False);
            budget.AdvanceFrame(2, 0f); Assert.That(next.ReserveImpact(1), Is.False);
            budget.AdvanceFrame(3, .1f); Assert.That(next.ReserveImpact(4), Is.True);
            Assert.That(next.ReserveImpact(5), Is.False);
        }
        [Test] public void ReservedChildKeepsRanksAndRootAliveAfterProducerIsSealed()
        {
            var inventory = Fresh(); inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, 0, _ => true);
            var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            var root = budget.BeginRoot(inventory.SessionId, 1, inventory);
            Assert.That(root.ReserveChild(2), Is.True); root.Seal();
            Assert.That(root.HasChild(2), Is.True);
            Assert.That(root.HasChild(3), Is.False, "Another child's lease must never authorize an unreserved producer.");
            inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, 1, _ => true);
            Assert.That(root.Rank(ProtocolKind.Ignite), Is.EqualTo(1));
            Assert.That(root.ReserveImpact(1), Is.True);
            Assert.That(root.ReserveChild(3), Is.False);
            root.ReleaseChild(2); Assert.That(root.IsClosed, Is.True); Assert.That(budget.LiveRoots, Is.Zero);
            Assert.That(root.ReserveImpact(1), Is.False);
        }
        [Test] public void EndedSessionCannotOpenRootAndBurnTickKeepsApplicationRanks()
        {
            var inventory = Fresh();
            inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, inventory.Revision, _ => true);
            var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            var applicationRoot = budget.BeginRoot(inventory.SessionId, 1, inventory);
            var captured = new ProtocolRanks(applicationRoot); applicationRoot.Seal();
            inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, inventory.Revision, _ => true);
            var tick = budget.BeginRoot(inventory.SessionId, 1, inventory, ProtocolProducer.BurnTick, 1, captured);
            Assert.That(tick.Rank(ProtocolKind.Ignite), Is.EqualTo(1));
            tick.Seal(); inventory.EndSession();
            Assert.That(budget.BeginRoot(inventory.SessionId, 1, inventory), Is.Null);
        }
        [Test] public void QueueBatchAndIdentityClaimsAreBoundedAndReleased()
        {
            var inventory = Fresh(); var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            var root = budget.BeginRoot(inventory.SessionId, 1, inventory);
            Assert.That(root.BeginBatch(), Is.True);
            for (int i = 0; i < 64; i++) Assert.That(root.ReserveQueueItem(), Is.True);
            Assert.That(root.ReserveQueueItem(), Is.False);
            Assert.That(root.ClaimPropagation(1, 1), Is.True); Assert.That(root.ClaimPropagation(1, 1), Is.False);
            Assert.That(root.ClaimPropagation(1, 2), Is.True);
            Assert.That(root.ClaimDeath(1, 1), Is.True); Assert.That(root.ClaimDeath(1, 1), Is.False);
            root.Seal(); for (int i = 0; i < 64; i++) root.ReleaseQueueItem();
            Assert.That(root.IsClosed, Is.False); root.EndBatch(); Assert.That(root.IsClosed, Is.True);
        }
        [Test] public void RootCapacityAndScaledExpiryCannotBeBypassed()
        {
            var inventory = Fresh(); var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            for (int i = 0; i < 64; i++) Assert.That(budget.BeginRoot(inventory.SessionId, 1, inventory), Is.Not.Null);
            Assert.That(budget.BeginRoot(inventory.SessionId, 1, inventory), Is.Null);
            budget.CloseAll(); Assert.That(budget.LiveRoots, Is.Zero);
            var root = budget.BeginRoot(inventory.SessionId, 1, inventory); budget.AdvanceFrame(2, 10f);
            Assert.That(root.IsExpired, Is.True); Assert.That(root.ReserveImpact(1), Is.False);
            Assert.That(budget.LiveRoots, Is.Zero, "TTL releases capacity without waiting for a producer callback.");
            root.CancelFuture(); Assert.That(root.IsClosed, Is.True);
        }
        [Test] public void ActiveChildCanReserveDescendantsButCannotRenewLifetimeReservationBudget()
        {
            var inventory = Fresh(); var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            var root = budget.BeginRoot(inventory.SessionId, 1, inventory);
            Assert.That(root.ReserveChild(1), Is.True); root.Seal();
            Assert.That(root.ReserveChild(2), Is.False);
            Assert.That(root.ReserveDescendant(99, 2), Is.False);
            for (long id = 2; id <= 32; id++)
            {
                Assert.That(root.ReserveDescendant(1, id), Is.True);
                root.ReleaseChild(id);
            }
            Assert.That(root.ReserveDescendant(1, 33), Is.False, "Releasing children cannot refill this execution's cap.");
            root.ReleaseChild(1); Assert.That(budget.LiveRoots, Is.Zero);
        }
        [Test] public void ProducerNamespacesAndReleasedReservationReplayRemainDistinct()
        {
            var inventory = Fresh(); var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            var root = budget.BeginRoot(inventory.SessionId, 1, inventory);
            Assert.That(root.ReserveChild(1), Is.True); Assert.That(root.ReserveChild(-1), Is.True);
            root.ReleaseChild(-1); Assert.That(root.ReserveChild(-1), Is.False);
            root.Seal(); Assert.That(root.HasChild(1), Is.True); root.ReleaseChild(1);
            Assert.That(budget.LiveRoots, Is.Zero);
        }
        [Test] public void QueryAndTickCapsCannotBeResetByReentryAndConfirmedBatchSurvivesCancellation()
        {
            var inventory = Fresh(); var budget = new ProtocolBudget(inventory.Catalog.Limits); budget.AdvanceFrame(1, .1f);
            for (int i = 0; i < 64; i++) { Assert.That(budget.ReserveQuery(), Is.True); Assert.That(budget.ReserveTick(), Is.True); }
            Assert.That(budget.ReserveQuery(), Is.False); Assert.That(budget.ReserveTick(), Is.False);
            budget.AdvanceFrame(1, 1); Assert.That(budget.ReserveQuery(), Is.False);
            var root = budget.BeginRoot(inventory.SessionId, 1, inventory); root.ReserveChild(1); root.BeginBatch();
            root.CancelFuture(); Assert.That(budget.HasConfirmedBatch, Is.True); Assert.That(root.HasChild(1), Is.False);
            root.EndBatch(); Assert.That(budget.LiveRoots, Is.Zero);
            budget.AdvanceFrame(2, .1f); Assert.That(budget.ReserveQuery(), Is.True); Assert.That(budget.ReserveTick(), Is.True);
        }
    }
}
