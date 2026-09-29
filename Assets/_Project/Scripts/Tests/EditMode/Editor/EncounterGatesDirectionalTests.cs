// Feature: procedural-stage-room-generation
// Validates: Requirements 3.1, 3.4, 3.6
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Example-based EditMode tests for the directional door API added to <see cref="EncounterGates"/>.
    /// Exercises per-direction idempotency, seal/open independence, <c>SealAll</c>, lazy creation via
    /// <c>SealDoor</c>, and confirms the legacy 2-door API remains regression-free.
    /// </summary>
    public sealed class EncounterGatesDirectionalTests
    {
        private GameObject _host;
        private EncounterGates _gates;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("EncounterGatesTestHost");
            _gates = _host.AddComponent<EncounterGates>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_host)
                Object.DestroyImmediate(_host);
            _host = null;
            _gates = null;
        }

        [Test]
        public void AddDoorIsIdempotentPerDirection()
        {
            _gates.ConfigureDirectional(Vector3.zero, new Vector2(6, 8));

            _gates.AddDoor(Direction.North);
            int afterFirst = _host.transform.childCount;
            _gates.AddDoor(Direction.North);
            int afterSecond = _host.transform.childCount;

            Assert.That(afterFirst, Is.EqualTo(1), "First AddDoor should create exactly one door child.");
            Assert.That(afterSecond, Is.EqualTo(afterFirst), "Repeating AddDoor for the same direction must not add a second door.");
            Assert.That(_gates.IsDoorSealed(Direction.North), Is.True, "Door starts sealed and stays consistent across repeated AddDoor.");
        }

        [Test]
        public void SealAndOpenAreIndependentPerSide()
        {
            _gates.ConfigureDirectional(Vector3.zero, new Vector2(6, 8));

            _gates.AddDoor(Direction.East);
            Assert.That(_gates.IsDoorSealed(Direction.East), Is.True, "Newly added door starts sealed.");

            _gates.OpenDoor(Direction.East);
            Assert.That(_gates.IsDoorSealed(Direction.East), Is.False, "OpenDoor should unseal the East door.");

            _gates.SealDoor(Direction.East);
            Assert.That(_gates.IsDoorSealed(Direction.East), Is.True, "SealDoor should re-seal the East door.");

            // A second, untouched side must not be affected by East operations.
            _gates.AddDoor(Direction.West);
            _gates.OpenDoor(Direction.West);
            Assert.That(_gates.IsDoorSealed(Direction.West), Is.False, "West should be open after OpenDoor(West).");

            _gates.SealDoor(Direction.East);
            Assert.That(_gates.IsDoorSealed(Direction.West), Is.False, "Sealing East must not re-seal West.");
            Assert.That(_gates.IsDoorSealed(Direction.East), Is.True, "East remains sealed.");
        }

        [Test]
        public void SealAllSealsEveryAddedDirection()
        {
            _gates.ConfigureDirectional(Vector3.zero, new Vector2(6, 8));

            _gates.AddDoor(Direction.North);
            _gates.AddDoor(Direction.South);
            _gates.AddDoor(Direction.East);
            _gates.OpenDoor(Direction.North);
            _gates.OpenDoor(Direction.East);

            _gates.SealAll();

            Assert.That(_gates.IsDoorSealed(Direction.North), Is.True);
            Assert.That(_gates.IsDoorSealed(Direction.South), Is.True);
            Assert.That(_gates.IsDoorSealed(Direction.East), Is.True);
            // A direction that was never added must not be sealed (no door exists).
            Assert.That(_gates.IsDoorSealed(Direction.West), Is.False, "SealAll only affects doors that were added.");
        }

        [Test]
        public void SealDoorCreatesMissingDoor()
        {
            _gates.ConfigureDirectional(Vector3.zero, new Vector2(6, 8));

            Assert.That(_gates.IsDoorSealed(Direction.West), Is.False, "No West door exists before SealDoor.");

            _gates.SealDoor(Direction.West);

            Assert.That(_gates.IsDoorSealed(Direction.West), Is.True, "SealDoor should create the missing door and seal it.");
            Assert.That(_host.transform.childCount, Is.EqualTo(1), "SealDoor should materialize exactly one door.");
        }

        [Test]
        public void LegacyExitLockedSemanticsRemainRegressionFree()
        {
            _gates.Configure(Vector3.zero, new Vector2(6, 8));

            // After Configure, the exit door exists and is active (locked).
            Assert.That(_gates.ExitLocked, Is.True, "Configure creates a sealed exit -> ExitLocked true.");

            _gates.OpenExit();
            Assert.That(_gates.ExitLocked, Is.False, "OpenExit should unlock the exit.");

            _gates.Seal();
            Assert.That(_gates.ExitLocked, Is.True, "Seal should re-lock the exit.");
        }

        [Test]
        public void DirectionalAndLegacyApisDoNotInterfere()
        {
            _gates.Configure(Vector3.zero, new Vector2(6, 8));
            _gates.ConfigureDirectional(Vector3.zero, new Vector2(6, 8));

            _gates.AddDoor(Direction.North);
            _gates.SealAll();
            Assert.That(_gates.IsDoorSealed(Direction.North), Is.True);

            // Legacy exit control still behaves independently of directional doors.
            _gates.OpenExit();
            Assert.That(_gates.ExitLocked, Is.False, "Directional operations must not affect legacy exit state.");
            Assert.That(_gates.IsDoorSealed(Direction.North), Is.True, "Legacy OpenExit must not open directional doors.");
        }
    }
}
