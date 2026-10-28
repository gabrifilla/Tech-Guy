using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    // Exercises observable ownership (writer/subscriber lifetime), not combat balance or game feel.
    public sealed class ObservationTelemetryTests
    {
        private string _directory;
        private GameObject _target;
        private static readonly Type Preview = typeof(CombatPolishPreview);

        [SetUp]
        public void Setup()
        {
            Call("EndObservationTelemetry");
            _directory = Path.Combine(Path.GetTempPath(), "TechGuy-ObservationTelemetry-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _target = new GameObject("Observation telemetry lifetime target");
            var actor = _target.AddComponent<Actor>();
            actor.SetMaxHealth(100);
            actor.RestoreHealthToMax();
        }

        [TearDown]
        public void Teardown()
        {
            Call("EndObservationTelemetry");
            if (_target) UnityEngine.Object.DestroyImmediate(_target);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void EndIsIdempotentAndRemovesDamageListenerWithoutKeepingTheWriterOpen()
        {
            Begin(_directory);
            _target.GetComponent<Actor>().TakeDamage(7);
            Call("EndObservationTelemetry");
            Call("EndObservationTelemetry");
            string path = Path.Combine(_directory, "events.jsonl");
            string before = File.ReadAllText(path);
            var events = before.Split('\n').Where(line => line.Contains("\"kind\":\"ActorDamageReceived\"")).ToArray();
            Assert.AreEqual(1, events.Length);
            var receipt = JsonUtility.FromJson<Receipt>(events[0]);
            Assert.IsTrue(receipt.has_value);
            Assert.AreEqual(7f, receipt.value);
            Assert.AreEqual(0, DamageSubscriberCount(_target.GetComponent<Actor>()), "End must detach the collector, not merely ignore future callbacks.");
            _target.GetComponent<Actor>().TakeDamage(9);
            Assert.AreEqual(before, File.ReadAllText(path));
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }

        [Test]
        public void BeginningAnotherObservationReleasesPreviousWriterAndDoesNotDuplicateDamageCallbacks()
        {
            Begin(_directory);
            string second = Path.Combine(_directory, "second");
            Begin(second);
            Assert.AreEqual(1, DamageSubscriberCount(_target.GetComponent<Actor>()));
            _target.GetComponent<Actor>().TakeDamage(3);
            Call("EndObservationTelemetry");
            Assert.IsFalse(File.ReadAllText(Path.Combine(_directory, "events.jsonl")).Contains("\"kind\":\"ActorDamageReceived\""));
            Assert.AreEqual(1, File.ReadLines(Path.Combine(second, "events.jsonl")).Count(line => line.Contains("\"kind\":\"ActorDamageReceived\"")));
        }

        [Test]
        public void ExistingEvidenceIsNeverTruncatedAndFailedBeginLeavesNoListeners()
        {
            string path = Path.Combine(_directory, "events.jsonl");
            File.WriteAllText(path, "baseline sentinel");
            var failure = Assert.Throws<TargetInvocationException>(() => Begin(_directory));
            Assert.IsInstanceOf<IOException>(failure.InnerException);
            Assert.AreEqual("baseline sentinel", File.ReadAllText(path));
            Assert.AreEqual(0, DamageSubscriberCount(_target.GetComponent<Actor>()));
            Call("EndObservationTelemetry");
        }

        [Test]
        public void ExitingPlayModeClosesObservationAndDetachesManagedCallbacks()
        {
            Begin(_directory);
            Call("ObservationTelemetryPlayModeChanged", PlayModeStateChange.ExitingPlayMode);
            Assert.AreEqual(0, DamageSubscriberCount(_target.GetComponent<Actor>()));
            using (new FileStream(Path.Combine(_directory, "events.jsonl"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            Call("EndObservationTelemetry");
        }

        [TestCase("{\"input_schedule\":[{\"frame\":0}]}")]
        [TestCase("{\"input_schedule\":[{\"frame\":30,\"action\":\"change_hp\"}]}")]
        [TestCase("{\"scenario_id\":\"GAUNTLET_SWARM\",\"mode\":\"seeded_combat\"}")]
        [TestCase("{\"mode\":\"current\",\"input_schedule\":[{\"frame\":30}]}")]
        [TestCase("{\"duration_frames\":999999}")]
        [TestCase("{\"mode\":\"current\",\"observe_seconds\":-1}")]
        [TestCase("{\"mode\":\"current\",\"observe_seconds\":31}")]
        [TestCase("{\"mode\":\"seeded_combat\",\"observe_seconds\":10}")]
        public void UnsafeOrUnsupportedRequestsAreRejectedWithoutStartingObservation(string json)
        {
            string before = CombatPolishPreview.ObservationStatus();
            Assert.IsFalse(CombatPolishPreview.TryValidateObservationRequest(json, out string error));
            Assert.IsNotEmpty(error);
            Assert.AreEqual(before, CombatPolishPreview.ObservationStatus());
        }

        [Test]
        public void RealScenarioAndPassiveCurrentRequestValidateWithoutChangingEditor()
        {
            Assert.IsTrue(CombatPolishPreview.TryValidateObservationRequest("{}", out string error), error);
            Assert.IsTrue(CombatPolishPreview.TryValidateObservationRequest("{\"scenario_id\":\"BOON_CHOICE\",\"mode\":\"current\",\"input_schedule\":[]}", out error), error);
            Assert.IsTrue(CombatPolishPreview.TryValidateObservationRequest("{\"mode\":\"current\",\"input_schedule\":[],\"observe_seconds\":30}", out error), error);
        }

        private void Begin(string directory) => Call("BeginObservationTelemetry", null, null, directory, Time.frameCount);
        private static object Call(string name, params object[] arguments) =>
            Preview.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        private static int DamageSubscriberCount(Actor actor) =>
            (typeof(Actor).GetField("DamageReceived", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actor) as Delegate)?.GetInvocationList().Length ?? 0;
        [Serializable] private sealed class Receipt { public bool has_value; public float value; }
    }
}
