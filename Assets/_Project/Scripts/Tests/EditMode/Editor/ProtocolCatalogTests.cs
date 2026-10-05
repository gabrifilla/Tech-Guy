using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class ProtocolCatalogTests
    {
        private ProtocolCatalogDefinition _catalog;
        [SetUp] public void SetUp() => _catalog = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_catalog);

        [Test] public void DefaultsMatchPhaseFiveAndSnapshotIsIsolated()
        {
            Assert.That(_catalog.TryCreateSnapshot(out var snapshot, out var error), Is.True, error);
            Assert.That(snapshot.Entries.Count, Is.EqualTo(7));
            Assert.That(snapshot.Get(ProtocolKind.Ignite).DamagePerRank, Is.EqualTo(6f));
            Assert.That(snapshot.Get(ProtocolKind.Reactor).RequiresIgnite, Is.True);
            Assert.That(snapshot.Get(ProtocolKind.Resonance).RequiresPropagation, Is.True);
            Assert.That(snapshot.Limits.ImpactsPerRoot, Is.EqualTo(32));
            Set(_catalog.Entries[0], "_damagePerRank", 17f);
            Set(_catalog.Limits, "_impactsPerRoot", 16);
            Assert.That(snapshot.Get(ProtocolKind.Ignite).DamagePerRank, Is.EqualTo(6f));
            Assert.That(snapshot.Limits.ImpactsPerRoot, Is.EqualTo(32));
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)] [TestCase(-1f)]
        public void InvalidRawDamageIsRejectedWithoutClamping(float value)
        {
            Set(_catalog.Entries[0], "_damagePerRank", value);
            Assert.That(_catalog.TryCreateSnapshot(out var snapshot, out var error), Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(error, Does.Contain("ignite").And.Contain("DamagePerRank"));
            Assert.That(_catalog.Entries[0].DamagePerRank, Is.EqualTo(value));
        }

        [Test] public void UnknownIdentityAndPrerequisiteAreRejected()
        {
            Set(_catalog.Entries[0], "_kind", (ProtocolKind)99);
            Assert.That(_catalog.TryCreateSnapshot(out _, out _), Is.False);
            Set(_catalog.Entries[0], "_kind", ProtocolKind.Ignite);
            Set(_catalog.Entries[4], "_requiresIgnite", false);
            Assert.That(_catalog.TryCreateSnapshot(out _, out var error), Is.False);
            Assert.That(error, Does.Contain("reactor"));
        }

        [TestCase(0)] [TestCase(4)]
        public void RankOutsideDeclaredContractIsRejected(int rank)
        {
            Set(_catalog.Entries[0], "_maxRank", rank);
            Assert.That(_catalog.TryCreateSnapshot(out _, out _), Is.False);
        }

        [Test] public void IncoherentBudgetAndChanceAreRejected()
        {
            Set(_catalog.Limits, "_queuePerRoot", 1);
            Assert.That(_catalog.TryCreateSnapshot(out _, out var error), Is.False);
            Assert.That(error, Does.Contain("caps"));
            Set(_catalog.Limits, "_queuePerRoot", 64);
            Set(_catalog.Entries[1], "_chancePerRank", 1.1f);
            Assert.That(_catalog.TryCreateSnapshot(out _, out error), Is.False);
            Assert.That(error, Does.Contain("frost"));
        }

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [Test] public void MissingAndDuplicateCatalogEntriesAreRejected()
        {
            Set(_catalog.Entries[1], "_kind", ProtocolKind.Ignite);
            Set(_catalog.Entries[1], "_id", "ignite");
            Assert.That(_catalog.TryCreateSnapshot(out _, out var error), Is.False);
            Assert.That(error, Does.Contain("duplicate"));
            Set(_catalog, "_entries", null);
            Assert.That(_catalog.TryCreateSnapshot(out _, out error), Is.False);
            Assert.That(error, Does.Contain("seven"));
        }

        [Test] public void UnsupportedClockAndZeroTickIntervalAreRejected()
        {
            Set(_catalog.Limits, "_clock", (ProtocolClock)99);
            Assert.That(_catalog.TryCreateSnapshot(out _, out var error), Is.False);
            Assert.That(error, Does.Contain("Clock"));
            Set(_catalog.Limits, "_clock", ProtocolClock.ScaledGameplay);
            Set(_catalog.Entries[0], "_tickInterval", 0f);
            Assert.That(_catalog.TryCreateSnapshot(out _, out error), Is.False);
            Assert.That(error, Does.Contain("interval"));
        }
        [TestCase("_collidersPerQuery", 129)] [TestCase("_applicationsPerType", int.MaxValue)]
        [TestCase("_activeVfx", 25)] [TestCase("_liveRoots", 65)] [TestCase("_impactsPerRoot", 33)]
        public void AuthoringCannotExceedPublishedSafetyCaps(string field, int value)
        {
            Set(_catalog.Limits, field, value);
            Assert.That(_catalog.TryCreateSnapshot(out _, out _), Is.False);
        }
    }
}
