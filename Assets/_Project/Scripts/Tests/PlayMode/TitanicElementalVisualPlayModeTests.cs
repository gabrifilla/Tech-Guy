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
        private string _elementalEvidenceDirectory;
        private string ElementalEvidenceDirectory => _elementalEvidenceDirectory ?? (_elementalEvidenceDirectory =
            System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" +
                UnityEditor.SessionState.GetString("TechGuy.Tests.GprPhase", "gpr-01-4") + "/gameview/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")));

        private IEnumerator ElementalCapture(string name)
        {
            System.IO.Directory.CreateDirectory(ElementalEvidenceDirectory);
            string path = System.IO.Path.Combine(ElementalEvidenceDirectory, name + ".png");
            Assert.IsFalse(System.IO.File.Exists(path)); ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!System.IO.File.Exists(path)) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null;
        }
        private static void AssertElementalIdentity(ProtocolBasicEffects effects)
        {
            var lines = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
            Assert.AreEqual(4, lines.Count(x => x.name == "Swirling wind" && x.enabled));
            Assert.AreEqual((effects & ProtocolBasicEffects.Detonation) != 0 ? 3 : 0, lines.Count(x => x.name == "Titanic fire identity" && x.enabled));
            Assert.AreEqual((effects & ProtocolBasicEffects.Conductor) != 0 ? 2 : 0, lines.Count(x => x.name == "Titanic storm identity" && x.enabled));
            foreach (var line in lines.Where(x => x.enabled && (x.name == "Titanic fire identity" || x.name == "Titanic storm identity")))
            {
                var tint = new MaterialPropertyBlock(); line.GetPropertyBlock(tint); var color = tint.GetColor("_BaseColor");
                if (line.name == "Titanic fire identity") { Assert.Greater(color.r, .9f); Assert.Less(color.g, .6f); }
                else { Assert.Greater(color.b, .9f); Assert.Less(color.r, .3f); }
            }
        }

        [UnityTest, Category("GPR014d")] public IEnumerator CompositionGameViewA() => ElementalGameView(ProtocolBasicEffects.None);
        [UnityTest, Category("GPR014d")] public IEnumerator CompositionGameViewAB() => ElementalGameView(ProtocolBasicEffects.Conductor);
        [UnityTest, Category("GPR014d")] public IEnumerator CompositionGameViewAC() => ElementalGameView(ProtocolBasicEffects.Detonation);
        [UnityTest, Category("GPR014d")] public IEnumerator CompositionGameViewABC() => ElementalGameView(ProtocolBasicEffects.Both);

        private IEnumerator ElementalGameView(ProtocolBasicEffects effects)
        {
            yield return PrepareTitanic();
            if ((effects & ProtocolBasicEffects.Conductor) != 0) GrantTitanicProtocol(ProtocolKind.Conductor);
            if ((effects & ProtocolBasicEffects.Detonation) != 0) GrantTitanicProtocol(ProtocolKind.Detonation);
            var source = BasicVictim(Vector3.forward * 4.5f);
            var neighbor = BasicVictim(new Vector3(2, 0, 4.5f));
            var blast = BasicVictim(new Vector3(4, 0, 4.5f));
            if (effects == ProtocolBasicEffects.Detonation) { source.SetMaxHealth(1); source.RestoreHealthToMax(); }
            if (effects == ProtocolBasicEffects.Both) { neighbor.SetMaxHealth(5); neighbor.RestoreHealthToMax(); }
            yield return null; var runtime = _player.GetComponent<ProtocolRuntime>();
            using (var cameras = new TitanicCaptureCameras())
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                foreach (var actor in new[] { source, neighbor, blast }) stats.Watch(actor);
                var cameraObject = new GameObject("GPR01.4 gameplay-scale evidence camera"); _spawned.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>(); camera.depth = 100;
                camera.transform.position = new Vector3(8, 8, -6); camera.transform.LookAt(new Vector3(1, .5f, 3.2f));
                camera.orthographic = true; camera.orthographicSize = 5.5f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .05f, .075f);
                foreach (var actor in new[] { source, neighbor, blast })
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Capsule); _spawned.Add(marker);
                    UnityEngine.Object.Destroy(marker.GetComponent<Collider>()); marker.transform.position = actor.transform.position + Vector3.up;
                    marker.GetComponent<Renderer>().material.color = actor == source ? new Color(1, .3f, .2f) : new Color(.2f, .6f, 1);
                }
                var lightObject = new GameObject("GPR01.4 evidence light"); _spawned.Add(lightObject);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                yield return TornadoCombo(); AssertElementalIdentity(effects);
                string label = effects == ProtocolBasicEffects.None ? "A" : effects == ProtocolBasicEffects.Conductor ? "AB" : effects == ProtocolBasicEffects.Detonation ? "AC" : "ABC";
                Assert.AreEqual(0, runtime.Feedback.ActiveCount, "Persistent identity must not create damage arcs/explosions at spawn.");
                yield return ElementalCapture(label + "-travel-1");
                yield return new WaitForSeconds(.15f); AssertElementalIdentity(effects);
                yield return ElementalCapture(label + "-travel-2");
                float deadline = Time.realtimeSinceStartup + 3;
                while (effects == ProtocolBasicEffects.Detonation ? !source.IsDead : source.health == 1000)
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                if (effects != ProtocolBasicEffects.None) Assert.Greater(runtime.Feedback.ActiveCount, 0);
                if (effects == ProtocolBasicEffects.Both) { Assert.IsTrue(neighbor.IsDead); Assert.That(blast.health, Is.EqualTo(996.25f).Within(.001)); }
                if (effects == ProtocolBasicEffects.Conductor) Assert.That(neighbor.health, Is.EqualTo(994.6f).Within(.001));
                if (effects == ProtocolBasicEffects.Detonation) Assert.That(neighbor.health, Is.EqualTo(999.25f).Within(.001));
                yield return ElementalCapture(label + "-confirmed-event");
                yield return new WaitForSeconds(.1f); yield return ElementalCapture(label + "-after-event");
                yield return TornadoEnd(runtime); Assert.IsFalse(_player.GetComponent<TitanicFistsVisual>().WaveActive);
                yield return ElementalCapture(label + "-cleanup");
                if (effects == ProtocolBasicEffects.Both)
                {
                    var visual = _player.GetComponent<TitanicFistsVisual>();
                    var reused = visual.AcquireTornado(1);
                    visual.UpdateTornado(reused, reused.Generation, Vector3.forward * 3, Vector3.forward, .3f);
                    AssertElementalIdentity(ProtocolBasicEffects.None); yield return ElementalCapture("ABC-reused-as-pure-wind");
                    visual.ReleaseTornado(reused, reused.Generation);
                }
                CompositionRecord(label + "-gameview", stats.Snapshot());
                Assert.AreEqual(0, runtime.Budget.LiveRoots);
            }
        }

        [UnityTest, Category("GPR014d")]
        public IEnumerator CompositionAcceptanceSnapshotKeepsMechanicsAndIdentityBeforeAndAfterAcquisition()
        {
            yield return PrepareTitanic();
            var source = BasicVictim(Vector3.forward * 4.5f);
            var neighbor = BasicVictim(new Vector3(2, 0, 4.5f)); neighbor.SetMaxHealth(5); neighbor.RestoreHealthToMax();
            var blast = BasicVictim(new Vector3(4, 0, 4.5f)); yield return null;
            var runtime = _player.GetComponent<ProtocolRuntime>();
            yield return TornadoCombo(2); Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
            GrantTitanicProtocol(ProtocolKind.Conductor); GrantTitanicProtocol(ProtocolKind.Detonation);
            yield return WaitForBasicEnd(); AssertElementalIdentity(ProtocolBasicEffects.None);
            yield return TornadoEnd(runtime); Assert.AreEqual(5, neighbor.health); Assert.AreEqual(1000, blast.health);
            yield return TornadoCombo(); AssertElementalIdentity(ProtocolBasicEffects.Both);
            yield return TornadoEnd(runtime); Assert.IsTrue(neighbor.IsDead); Assert.That(blast.health, Is.EqualTo(996.25f).Within(.001));
        }

        [UnityTest, Category("GPR014d")]
        public IEnumerator CompositionPoolReusesWithoutStaleFireElectricityOrOldLeaseCleanup()
        {
            yield return PrepareTitanic(); var visual = _player.GetComponent<TitanicFistsVisual>();
            var first = visual.AcquireTornado(1, ProtocolBasicEffects.Both); long generation = first.Generation;
            visual.UpdateTornado(first, generation, Vector3.forward * 3, Vector3.forward, .2f);
            AssertElementalIdentity(ProtocolBasicEffects.Both); yield return ElementalCapture("reuse-before");
            visual.ReleaseTornado(first, generation);
            var next = visual.AcquireTornado(1); Assert.AreSame(first, next);
            visual.UpdateTornado(next, next.Generation, Vector3.forward * 3, Vector3.forward, .2f);
            visual.ReleaseTornado(first, generation); AssertElementalIdentity(ProtocolBasicEffects.None);
            Assert.IsTrue(visual.WaveActive); yield return ElementalCapture("reuse-after-pure");
            visual.Clear(); Assert.IsFalse(visual.WaveActive);
        }
    }
}
