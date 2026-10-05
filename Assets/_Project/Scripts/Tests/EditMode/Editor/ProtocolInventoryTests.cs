using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class ProtocolInventoryTests
    {
        private ProtocolInventory Fresh()
        {
            var catalog = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(catalog.TryCreateSnapshot(out var snapshot, out _), Is.True);
            UnityEngine.Object.DestroyImmediate(catalog);
            return new ProtocolInventory(Guid.NewGuid(), snapshot);
        }
        [Test] public void DependenciesCapsAndRevisionAreEnforced()
        {
            var inventory = Fresh();
            Assert.That(inventory.CanAcquire(ProtocolKind.Reactor), Is.False);
            Assert.That(inventory.CanAcquire(ProtocolKind.Resonance), Is.False);
            Assert.That(inventory.CanAcquire(ProtocolKind.Overflow), Is.False);
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, 0, _ => true), Is.True);
            Assert.That(inventory.CanAcquire(ProtocolKind.Reactor), Is.True);
            Assert.That(inventory.CanAcquire(ProtocolKind.Overflow), Is.True);
            Assert.That(inventory.CanAcquire(ProtocolKind.Resonance), Is.False);
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Conductor, 1, _ => true), Is.True);
            Assert.That(inventory.CanAcquire(ProtocolKind.Resonance), Is.True);
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, 0, _ => true), Is.False);
            for (int i = 0; i < 2; i++) Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, inventory.Revision, _ => true), Is.True);
            Assert.That(inventory.Rank(ProtocolKind.Ignite), Is.EqualTo(3));
            Assert.That(inventory.CanAcquire(ProtocolKind.Ignite), Is.False);
        }
        [Test] public void ReentryAndRejectedApplicationDoNotConsumeRank()
        {
            var inventory = Fresh(); int calls = 0;
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Frost, 0, rank =>
            {
                calls++;
                Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Frost, 0, _ => true), Is.False);
                return false;
            }), Is.False);
            Assert.That(calls, Is.EqualTo(1)); Assert.That(inventory.Revision, Is.Zero);
            Assert.That(inventory.Rank(ProtocolKind.Frost), Is.Zero);
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Frost, 0, _ => true), Is.True);
        }
        [Test] public void ExceptionFaultsInventoryAndSessionEndRejectsStaleClaims()
        {
            var inventory = Fresh();
            Assert.Throws<InvalidOperationException>(() => inventory.TryAcquire(inventory.SessionId, ProtocolKind.Frost, 0,
                _ => throw new InvalidOperationException("fixture")));
            Assert.That(inventory.IsFaulted, Is.True);
            Assert.That(inventory.Rank(ProtocolKind.Frost), Is.Zero);
            Assert.That(inventory.CanAcquire(ProtocolKind.Frost), Is.False);
            inventory = Fresh(); var oldId = inventory.SessionId;
            var newId = Guid.NewGuid(); Assert.That(inventory.TryBindFreshSession(newId), Is.True);
            Assert.That(inventory.TryAcquire(oldId, ProtocolKind.Ignite, 0, _ => true), Is.False);
            Assert.That(inventory.TryAcquire(newId, ProtocolKind.Ignite, 0, _ => true), Is.True);
            inventory.EndSession(); Assert.That(inventory.Rank(ProtocolKind.Ignite), Is.Zero);
            Assert.That(inventory.TryAcquire(newId, ProtocolKind.Ignite, inventory.Revision, _ => true), Is.False);
            Assert.That(Fresh().Rank(ProtocolKind.Ignite), Is.Zero);
        }
    }
}
