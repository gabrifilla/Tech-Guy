using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TechGuy.Tests.GprMovement
{
    public sealed class GprMovementTests
    {
        private GprMovementArena _arena;
        private GprMovementMotor Motor => _arena.Motor;
        private static string Evidence => Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/c01-2a");
        private readonly List<string> _rows = new List<string>();
        private float _oldScale;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _oldScale = Time.timeScale;
            Time.timeScale = 1;
            Directory.CreateDirectory(Evidence);
            _arena = new GameObject("C01.2a TEST FIXTURE").AddComponent<GprMovementArena>();
            _arena.Build();
            _rows.Clear();
            _rows.Add("case,frame,dt,x,y,z,actualPlanarSpeed,agentVelocity,owner,cpuMicroseconds");
            yield return null;
            Assert.That(Motor.Agent.isOnNavMesh, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Time.timeScale = _oldScale;
            string name = TestContext.CurrentContext.Test.Name;
            File.WriteAllLines(Path.Combine(Evidence, name + ".csv"), _rows);
            UnityEngine.Object.Destroy(_arena.gameObject);
            yield return null;
        }

        private IEnumerator Walk(string label, Vector2 input, float dt, int frames, Vector3? aim = null, Quaternion? camera = null)
        {
            for (int i = 0; i < frames; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Motor.Step(input, _arena.Origin + (aim ?? new Vector3(0, 0, 10)), camera ?? Quaternion.identity, dt);
                watch.Stop();
                yield return null;
                Motor.SampleDisplacement();
                Vector3 p = Motor.transform.position - _arena.Origin;
                _rows.Add(string.Join(",", label, i, F(dt), F(p.x), F(p.y), F(p.z), F(Motor.ActualPlanarSpeed), F(Motor.Agent.velocity.magnitude), Motor.LastOwner, F(watch.ElapsedTicks * 1000000.0 / System.Diagnostics.Stopwatch.Frequency)));
            }
        }
        private static string F(double value) => value.ToString("F6", CultureInfo.InvariantCulture);

        [UnityTest]
        public IEnumerator VirtualKeyboardMouseUsesPreviewReader()
        {
            var previousKeyboard = Keyboard.current;
            var previousMouse = Mouse.current;
            var keyboard = InputSystem.AddDevice<Keyboard>("C01.2a virtual keyboard");
            var mouse = InputSystem.AddDevice<Mouse>("C01.2a virtual mouse");
            try
            {
                _arena.Place(new Vector3(0, 0, -8));
                Motor.CameraRelative = false;
                Motor.ReadPhysicalInput = true; // Same reader, virtual devices: never human acceptance.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .8f, Screen.height * .5f) });
                yield return null;
                Vector3 start = Motor.transform.position;
                float started = Time.time;
                yield return new WaitForSeconds(.3f);
                Vector3 delta = Motor.transform.position - start;
                float elapsed = Time.time - started;
                File.WriteAllText(Path.Combine(Evidence, "virtual-input-real-time.json"),
                    "{\"input\":\"virtual\",\"elapsedSeconds\":" + F(elapsed) + ",\"distance\":" + F(delta.magnitude) + ",\"speed\":" + F(delta.magnitude / elapsed) + "}");
                Assert.That(delta.x, Is.GreaterThan(.3));
                Assert.That(delta.z, Is.GreaterThan(.3));
                Assert.That(delta.x / delta.z, Is.EqualTo(1).Within(.05));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;
                start = Motor.transform.position;
                yield return new WaitForSeconds(.1f);
                Assert.That(Vector3.Distance(start, Motor.transform.position), Is.LessThan(.01));
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "arena-virtual-input-gameview.png"));
                yield return null;
            }
            finally
            {
                Motor.ReadPhysicalInput = false;
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                previousKeyboard?.MakeCurrent();
                previousMouse?.MakeCurrent();
            }
        }

        [UnityTest]
        public IEnumerator ZeroTimePauseAndInvalidTimeClearAnimationWithoutMoving()
        {
            yield return Walk("before-zero-dt", Vector2.down, .02f, 3);
            Assert.That(Motor.Presentation.GetFloat("ActualPlanarSpeed"), Is.GreaterThan(4.9));
            Vector3 start = Motor.transform.position;
            Time.timeScale = 0;
            Motor.Step(Vector2.up, start + Vector3.forward, Quaternion.identity, 0);
            Assert.That(Motor.AwaitRelease, Is.True);
            Assert.That(Motor.Presentation.GetFloat("ActualPlanarSpeed"), Is.EqualTo(0));
            Time.timeScale = 1;
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1 })
            {
                Motor.Step(Vector2.up, start + Vector3.forward, Quaternion.identity, invalid);
                yield return null;
                Assert.That(Motor.ActualPlanarSpeed, Is.EqualTo(0));
                Assert.That(Vector3.Distance(start, Motor.transform.position), Is.LessThan(.005));
            }
        }

        [UnityTest]
        public IEnumerator CardinalDiagonalStopAndAim()
        {
            Assert.That(_arena.Place(new Vector3(0, 0, -8)), Is.True);
            yield return null;
            Vector3 p = Motor.transform.position;
            yield return Walk("cardinal", Vector2.up, 1f / 60, 60, new Vector3(10, 0, -8));
            float cardinal = Vector3.ProjectOnPlane(Motor.transform.position - p, Vector3.up).magnitude;
            Assert.That(cardinal, Is.EqualTo(5).Within(.03));
            Assert.That(Motor.Presentation.GetFloat("ActualPlanarSpeed"), Is.EqualTo(Motor.ActualPlanarSpeed).Within(.001));
            Assert.That(Motor.ActualPlanarSpeed, Is.GreaterThan(4.9));
            Assert.That(Motor.transform.forward.x, Is.GreaterThan(.8), "Aim must remain independent of locomotion.");
            Assert.That(_arena.Place(new Vector3(-1, 0, -9)), Is.True);
            yield return null;
            p = Motor.transform.position;
            yield return Walk("diagonal", Vector2.one, 1f / 60, 60);
            float diagonal = Vector3.ProjectOnPlane(Motor.transform.position - p, Vector3.up).magnitude;
            Assert.That(diagonal / cardinal, Is.EqualTo(1).Within(.01));
            p = Motor.transform.position;
            yield return Walk("release", Vector2.zero, 1f / 60, 10);
            Assert.That(Vector3.Distance(p, Motor.transform.position), Is.LessThan(.005));
            Assert.That(Motor.ActualPlanarSpeed, Is.LessThan(.01));
            Assert.That(Motor.Agent.hasPath, Is.False);
        }

        [UnityTest]
        public IEnumerator CameraVersusWorldAndFrameSteps()
        {
            foreach (float dt in new[] { 1f / 30, 1f / 60, 1f / 144 })
            {
                Assert.That(_arena.Place(new Vector3(0, 0, -10)), Is.True);
                yield return null;
                Motor.CameraRelative = true;
                Vector3 p = Motor.transform.position;
                yield return Walk("camera-" + F(dt), Vector2.up, dt, Mathf.RoundToInt(1f / dt), camera: Quaternion.Euler(45, 90, 0));
                Assert.That(Motor.transform.position.x - p.x, Is.EqualTo(5).Within(.04));
                Assert.That(Motor.transform.position.z - p.z, Is.EqualTo(0).Within(.02));
            }
            Assert.That(_arena.Place(new Vector3(0, 0, -10)), Is.True);
            yield return null;
            Motor.CameraRelative = false;
            Vector3 start = Motor.transform.position;
            yield return Walk("world", Vector2.up, 1f / 60, 60, camera: Quaternion.Euler(45, 90, 0));
            Assert.That(Motor.transform.position.z - start.z, Is.EqualTo(5).Within(.04));
        }

        [UnityTest]
        public IEnumerator BakedWallCornerObstacleAndBoundary()
        {
            yield return Walk("baked-wall", Vector2.right, 1f / 30, 50);
            Assert.That(Motor.transform.position.x - _arena.Origin.x, Is.LessThan(3.45));
            float wallX = Motor.transform.position.x;
            yield return Walk("corner", Vector2.one, 1f / 30, 60);
            Assert.That(Motor.transform.position.x, Is.LessThanOrEqualTo(wallX + .03f));
            yield return Walk("wall-parallel-to-corner", Vector2.up, 1f / 30, 60);
            Assert.That(Motor.transform.position.z - _arena.Origin.z, Is.LessThan(4));
            Assert.That(Motor.transform.position.z - _arena.Origin.z, Is.GreaterThan(3), "Must reach the corner along the wall.");
            Vector3 corner = Motor.transform.position;
            yield return Walk("corner-escape", Vector2.down, 1f / 30, 12);
            Assert.That(corner.z - Motor.transform.position.z, Is.GreaterThan(1.5));
            Assert.That(_arena.Place(new Vector3(-3, 0, -8)), Is.True);
            yield return null;
            yield return Walk("obstacle", Vector2.up, 1f / 30, 60);
            Assert.That(Motor.transform.position.z - _arena.Origin.z, Is.LessThan(-5.45));
            Assert.That(_arena.Place(new Vector3(0, 0, -12)), Is.True);
            yield return null;
            yield return Walk("boundary", Vector2.down, .1f, 30);
            Assert.That(Motor.Agent.isOnNavMesh, Is.True);
            Assert.That(Motor.transform.position.z - _arena.Origin.z, Is.GreaterThan(-14.6));
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "arena-boundary-gameview.png"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RawMoveFailureAndGuardedThinWallDash()
        {
            Assert.That(_arena.Place(new Vector3(-11, 0, -7)), Is.True);
            yield return null;
            Motor.GuardCollisions = false;
            yield return Walk("raw-unbaked-wall", Vector2.right, .1f, 12);
            Assert.That(Motor.transform.position.x - _arena.Origin.x, Is.GreaterThan(-7), "Expected diagnostic: raw Agent.Move ignores unbaked collider.");
            Assert.That(_arena.Place(new Vector3(-11, 0, -7)), Is.True);
            yield return null;
            Motor.GuardCollisions = true;
            Assert.That(Motor.BeginDash(Vector3.right, Vector2.up, Quaternion.identity), Is.True);
            yield return Walk("guarded-dash-thin-wall", Vector2.up, .1f, 8);
            Assert.That(Motor.transform.position.x - _arena.Origin.x, Is.LessThan(-8.5));
            Assert.That(Motor.DashRemaining, Is.EqualTo(0));
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "arena-thin-wall-gameview.png"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterControllerAlternativeHasNoNavMeshBoundary()
        {
            Motor.enabled = false;
            Motor.Agent.enabled = false;
            var go = new GameObject("C01.2a CharacterController ALTERNATIVE ONLY");
            go.transform.SetParent(_arena.transform);
            go.transform.position = _arena.Origin + new Vector3(-11, .04f, -7);
            var controller = go.AddComponent<CharacterController>();
            controller.radius = .5f;
            controller.height = 2;
            controller.center = Vector3.up;
            controller.skinWidth = .02f;
            controller.minMoveDistance = 0;
            for (int i = 0; i < 15; i++)
            {
                controller.Move(new Vector3(20 * .02f, -.02f, 0));
                yield return null;
            }
            float wallX = go.transform.position.x - _arena.Origin.x;
            Assert.That(wallX, Is.LessThan(-8.45f));
            controller.enabled = false;
            go.transform.position = _arena.Origin + new Vector3(0, .04f, -13);
            controller.enabled = true;
            for (int i = 0; i < 60; i++)
            {
                controller.Move(new Vector3(0, -.05f, -5f / 60));
                yield return null;
            }
            float boundaryZ = go.transform.position.z - _arena.Origin.z;
            Assert.That(boundaryZ, Is.LessThan(-16), "Physical controller alone does not enforce NavMesh limits.");
            File.WriteAllText(Path.Combine(Evidence, "character-controller-assessment.json"),
                "{\"unbakedWallX\":" + F(wallX) + ",\"boundaryZ\":" + F(boundaryZ) + ",\"simultaneousAgent\":false}");
        }

        [UnityTest]
        public IEnumerator RootCastHitstopForcedPauseFocusAndDisable()
        {
            foreach (MovementGate gate in new[] { MovementGate.RootedAttack, MovementGate.RootedCast, MovementGate.Hitstop, MovementGate.Paused, MovementGate.FocusLost })
            {
                Motor.Gates = gate;
                Vector3 p = Motor.transform.position;
                yield return Walk(gate.ToString(), Vector2.up, .02f, 5);
                Assert.That(Vector3.Distance(p, Motor.transform.position), Is.LessThan(.005));
            }
            Motor.Gates = MovementGate.None;
            Vector3 resume = Motor.transform.position;
            yield return Walk("held-after-focus", Vector2.up, .02f, 5);
            Assert.That(Vector3.Distance(resume, Motor.transform.position), Is.LessThan(.005));
            yield return Walk("release-latch", Vector2.zero, .02f, 1);
            yield return Walk("fresh-press", Vector2.up, .02f, 5);
            Assert.That(Motor.transform.position.z - resume.z, Is.EqualTo(.5).Within(.02));
            Motor.Gates = MovementGate.RootedCast;
            Motor.ForcedVelocity = Vector3.left * 2;
            Vector3 forced = Motor.transform.position;
            yield return Walk("pull-over-root", Vector2.right, .02f, 10);
            Assert.That(Motor.transform.position.x - forced.x, Is.EqualTo(-.4).Within(.02));
            Motor.Gates = MovementGate.Paused;
            Time.timeScale = 0;
            Vector3 paused = Motor.transform.position;
            yield return Walk("paused-external", Vector2.right, .02f, 3);
            Assert.That(Vector3.Distance(paused, Motor.transform.position), Is.LessThan(.005));
            Time.timeScale = 1;
            Motor.enabled = false;
            Assert.That(Motor.ActualPlanarSpeed, Is.EqualTo(0));
            Assert.That(Motor.DashRemaining, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator CursorDashAlternativeAndFrozenClock()
        {
            Assert.That(_arena.Place(new Vector3(0, 0, -10)), Is.True);
            yield return null;
            Vector3 start = Motor.transform.position;
            Assert.That(Motor.BeginDash(Vector3.forward, Vector2.right, Quaternion.identity), Is.True);
            yield return Walk("cursor-dash-first", Vector2.left, .05f, 1);
            Motor.Gates = MovementGate.Hitstop;
            float remaining = Motor.DashRemaining;
            yield return Walk("dash-hitstop", Vector2.left, .05f, 4);
            Assert.That(Motor.DashRemaining, Is.EqualTo(remaining));
            Motor.Gates = MovementGate.None;
            yield return Walk("cursor-dash-end", Vector2.left, .05f, 4);
            Assert.That(Motor.transform.position.z - start.z, Is.EqualTo(5).Within(.04));
            Assert.That(Motor.transform.position.x - start.x, Is.EqualTo(0).Within(.02));
            Assert.That(_arena.Place(new Vector3(0, 0, -10)), Is.True);
            yield return null;
            start = Motor.transform.position;
            Assert.That(Motor.BeginDash(Vector3.forward, Vector2.right, Quaternion.identity, true), Is.True);
            yield return Walk("wasd-dash-alternative", Vector2.up, .05f, 5);
            Assert.That(Motor.transform.position.x - start.x, Is.EqualTo(5).Within(.04));
        }

        [UnityTest]
        public IEnumerator SlopeUpDownAndActualDisplacement()
        {
            Assert.That(_arena.Place(new Vector3(-7, 0, 4.7f)), Is.True);
            yield return null;
            Vector3 start = Motor.transform.position;
            yield return Walk("slope-up", Vector2.up, 1f / 60, 65);
            Assert.That(Motor.transform.position.y - start.y, Is.GreaterThan(1));
            Assert.That(Motor.transform.position.z - start.z, Is.GreaterThan(4));
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "arena-slope-gameview.png"));
            yield return null;
            yield return Walk("slope-down", Vector2.down, 1f / 60, 65);
            Assert.That(Mathf.Abs(Motor.transform.position.y - start.y), Is.LessThan(.2));
            Assert.That(Motor.ActualPlanarSpeed, Is.EqualTo(5).Within(.15));
        }
    }
}
