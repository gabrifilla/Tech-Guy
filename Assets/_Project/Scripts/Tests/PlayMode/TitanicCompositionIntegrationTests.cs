using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Profiling;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest, Category("GPR014e")] public IEnumerator CompositionNativeCWithoutA() => CompositionNativeProtocols(false);
        [UnityTest, Category("GPR014e")] public IEnumerator CompositionNativeBCWithoutA() => CompositionNativeProtocols(true);
        private IEnumerator CompositionNativeProtocols(bool conductor)
        {
            yield return PrepareTitanic(false); GrantTitanicProtocol(ProtocolKind.Detonation);
            if (conductor) GrantTitanicProtocol(ProtocolKind.Conductor);
            var source = BasicVictim(Vector3.forward * 4.5f); source.SetMaxHealth(5); source.RestoreHealthToMax();
            var neighbor = BasicVictim(new Vector3(2, 0, 4.5f)); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>();
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                stats.Watch(source); stats.Watch(neighbor); _player.DealResolvedAttackDamage(source, 10);
                Assert.IsTrue(source.IsDead); Assert.AreEqual(0, runtime.TitanicWavesResolved);
                Assert.That(neighbor.health, Is.EqualTo(conductor ? 997.75f : 996.25f).Within(.001));
                Assert.IsFalse(stats.Snapshot().RecentDamage.Any(x => x.ProducerId == TitanicFistsDefinition.Id));
                CompositionRecord(conductor ? "native-BC" : "native-C", stats.Snapshot());
            }
        }

        [UnityTest, Category("GPR014e")]
        public IEnumerator CompositionBoonAcquiredDuringFlightAffectsOnlyNextTornado()
        {
            yield return PrepareTitanic(); var source = BasicVictim(Vector3.forward * 4.5f);
            var child = BasicVictim(new Vector3(2, 0, 4.5f)); child.SetMaxHealth(5); child.RestoreHealthToMax();
            var blast = BasicVictim(new Vector3(4, 0, 4.5f)); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo(); AssertElementalIdentity(ProtocolBasicEffects.None);
            GrantTitanicProtocol(ProtocolKind.Conductor); GrantTitanicProtocol(ProtocolKind.Detonation);
            AssertElementalIdentity(ProtocolBasicEffects.None);
            yield return TornadoEnd(runtime); Assert.AreEqual(5, child.health); Assert.AreEqual(1000, blast.health);
            yield return TornadoCombo(); AssertElementalIdentity(ProtocolBasicEffects.Both);
            yield return TornadoEnd(runtime); Assert.IsTrue(child.IsDead); Assert.That(blast.health, Is.EqualTo(996.25f).Within(.001));
        }

        [Serializable] private sealed class CompositionLoadEvidence
        {
            public int enemies, emitted, maxActiveTornadoes, maxFrameImpacts, maxQueue, impactedTargets, droppedImpacts, poolSize;
            public bool cpuRecorderValid, gcRecorderValid;
            public double cpuMedianMs, cpuP95Ms, frameMedianMs, frameP95Ms;
            public long gcMaxBytes;
            public string limitation = "Editor PlayMode fixture; aggregate Protocol marker and whole-frame GC include Editor/test overhead, not a standalone CPU/GPU benchmark.";
        }
        [UnityTest, Category("GPR014e")] public IEnumerator CompositionProfileCrowd8() => CompositionProfileCrowd(8);
        [UnityTest, Category("GPR014e")] public IEnumerator CompositionProfileCrowd32() => CompositionProfileCrowd(32);
        [UnityTest, Category("GPR014e")] public IEnumerator CompositionProfileCrowd64() => CompositionProfileCrowd(64);
        private IEnumerator CompositionProfileCrowd(int count)
        {
            yield return PrepareTitanic(); GrantTitanicProtocol(ProtocolKind.Conductor); GrantTitanicProtocol(ProtocolKind.Detonation);
            var enemies = new List<Actor>();
            for (int i = 0; i < count; i++) enemies.Add(BasicVictim(new Vector3((i % 8 - 3.5f) * .06f, 0, 4.2f + i / 8 * .06f)));
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            var evidence = new CompositionLoadEvidence { enemies = count };
            var cpuSamples = new List<double>(); var frames = new List<double>();
            using (var cpu = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "TechGuy.Protocol.CPU", 128))
            using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 128))
            {
                evidence.cpuRecorderValid = cpu.Valid; evidence.gcRecorderValid = gc.Valid;
                var sampler = _player.gameObject.AddComponent<TitanicCompositionFrameSampler>();
                sampler.Sample = () =>
                {
                    evidence.maxActiveTornadoes = Math.Max(evidence.maxActiveTornadoes, runtime.PendingTitanicWaves);
                    evidence.maxFrameImpacts = Math.Max(evidence.maxFrameImpacts, runtime.Budget.FrameImpacts);
                    if (cpu.Valid) cpuSamples.Add(cpu.LastValue / 1000000d);
                    if (gc.Valid) evidence.gcMaxBytes = Math.Max(evidence.gcMaxBytes, gc.LastValue);
                    frames.Add(Time.unscaledDeltaTime * 1000d);
                };
                yield return TornadoCombo(6); yield return TornadoEnd(runtime); sampler.Sample = null;
            }
            evidence.emitted = runtime.TitanicWavesResolved; evidence.maxQueue = runtime.Budget.MaximumQueue;
            evidence.droppedImpacts = runtime.Budget.DroppedImpacts; evidence.impactedTargets = enemies.Count(x => x.health < 1000);
            evidence.poolSize = ((System.Collections.ICollection)typeof(TitanicFistsVisual).GetField("_pool", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(_player.GetComponent<TitanicFistsVisual>())).Count;
            cpuSamples.Sort(); frames.Sort();
            if (cpuSamples.Count > 0) { evidence.cpuMedianMs = cpuSamples[cpuSamples.Count / 2]; evidence.cpuP95Ms = cpuSamples[Math.Min(cpuSamples.Count - 1, (int)(cpuSamples.Count * .95))]; }
            if (frames.Count > 0) { evidence.frameMedianMs = frames[frames.Count / 2]; evidence.frameP95Ms = frames[Math.Min(frames.Count - 1, (int)(frames.Count * .95))]; }
            Assert.AreEqual(2, evidence.emitted); Assert.GreaterOrEqual(evidence.maxActiveTornadoes, 2);
            Assert.AreEqual(0, runtime.Budget.LiveRoots); Assert.AreEqual(0, _player.GetComponent<TitanicFistsVisual>().ActiveTornadoVisuals);
            Assert.LessOrEqual(evidence.maxQueue, runtime.Inventory.Catalog.Limits.QueuePerRoot);
            CompositionRecord("crowd-" + count, evidence);
        }
    }
    public sealed class TitanicCompositionFrameSampler : MonoBehaviour
    {
        internal Action Sample;
        private void LateUpdate() => Sample?.Invoke();
    }
}
