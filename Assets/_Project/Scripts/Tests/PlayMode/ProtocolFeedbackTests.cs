using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ProtocolFeedbackTests
    {
        [Test] public void SharedMaterialKeepsSemanticTintAndDistinctShockCombustionGlyphs()
        {
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            source.TryCreateSnapshot(out var catalog, out _); Object.DestroyImmediate(source);
            var host = new GameObject("Protocol tint fixture"); var pool = new ProtocolFeedbackPool(host.transform, catalog.Limits);
            try
            {
                Assert.That(pool.Emit(ProtocolFeedbackKind.Conductor, Vector3.zero, Vector3.right, 1), Is.True);
                Assert.That(pool.Emit(ProtocolFeedbackKind.Combustion, Vector3.zero, Vector3.zero, 1), Is.True);
                Assert.That(pool.Emit(ProtocolFeedbackKind.ThermalShock, Vector3.zero, Vector3.zero, 1), Is.True);
                var block = new MaterialPropertyBlock();
                host.transform.GetChild(0).GetComponentInChildren<LineRenderer>().GetPropertyBlock(block);
                var chainColor = block.GetColor("_BaseColor"); Assert.That(chainColor.b, Is.GreaterThan(chainColor.r));
                host.transform.GetChild(1).GetComponentInChildren<LineRenderer>().GetPropertyBlock(block);
                var fireColor = block.GetColor("_BaseColor"); Assert.That(fireColor.r, Is.GreaterThan(fireColor.b));
                Assert.That(fireColor.a, Is.LessThan(1)); Assert.That(chainColor.a, Is.LessThan(1));
                int combustionLines = 0, shockLines = 0;
                foreach (var line in host.transform.GetChild(1).GetComponentsInChildren<LineRenderer>()) if (line.enabled) combustionLines++;
                foreach (var line in host.transform.GetChild(2).GetComponentsInChildren<LineRenderer>()) if (line.enabled) shockLines++;
                Assert.That(combustionLines, Is.EqualTo(2)); Assert.That(shockLines, Is.EqualTo(3));
            }
            finally { pool.Dispose(); Object.DestroyImmediate(host); }
        }
        [UnityTest] public IEnumerator PoolCapsPauseExpiryAndReuseDoNotReserveCombat()
        {
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            source.TryCreateSnapshot(out var catalog, out _); Object.DestroyImmediate(source);
            var host = new GameObject("ProtocolFeedbackFixture");
            var pool = new ProtocolFeedbackPool(host.transform, catalog.Limits);
            try
            {
                Assert.That(pool.Enabled, Is.True, "Published URP shader must exist.");
                for (int i = 0; i < 16; i++) Assert.That(pool.Emit((ProtocolFeedbackKind)(i % 6), Vector3.zero, Vector3.right, 2, 4), Is.True);
                Assert.That(pool.Emit(ProtocolFeedbackKind.Overflow, Vector3.zero, Vector3.zero, 1), Is.False);
                pool.Tick(0); Assert.That(pool.ActiveCount, Is.EqualTo(16));
                yield return null;
                for (int i = 0; i < 8; i++) Assert.That(pool.Emit(ProtocolFeedbackKind.Shatter, Vector3.zero, Vector3.zero, 2, 6), Is.True);
                Assert.That(pool.Emit(ProtocolFeedbackKind.Detonation, Vector3.zero, Vector3.zero, 2), Is.False);
                Assert.That(pool.ActiveCount, Is.EqualTo(24)); Assert.That(host.transform.childCount, Is.EqualTo(24));
                pool.Tick(.6f); Assert.That(pool.ActiveCount, Is.Zero);
                yield return null;
                Assert.That(pool.Emit(ProtocolFeedbackKind.Conductor, Vector3.zero, Vector3.right, 2), Is.True);
                Assert.That(host.transform.childCount, Is.EqualTo(24), "Reuse cannot grow the pool.");
                pool.Clear(); pool.Clear(); Assert.That(pool.ActiveCount, Is.Zero);
            }
            finally { pool.Dispose(); Object.DestroyImmediate(host); }
        }
    }
}
