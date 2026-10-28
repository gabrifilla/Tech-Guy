using System;
using UnityEngine;
using UnityEngine.AI;

namespace TechGuy.Tests.GprMovement
{
    [Flags]
    public enum MovementGate { None = 0, RootedAttack = 1, RootedCast = 2, Hitstop = 4, Paused = 8, FocusLost = 16 }

    // C01.2a EXPERIMENT ONLY. Never attach to a production player.
    // No combat executor, binding, save, invulnerability or cooldown integration.
    public sealed class GprMovementMotor : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }
        public bool GuardCollisions = true;
        public bool CameraRelative = true;
        public bool ReadPhysicalInput;
        public string InputLabel = "virtual/API de teste";
        public Animator Presentation;
        public Camera View;
        public MovementGate Gates;
        public Vector3 ForcedVelocity;
        public float Speed = 5f; // Observed legacy agent; not new balance.
        public float DashSpeed = 20f;
        public float DashDuration = .25f;
        public float ActualPlanarSpeed { get; private set; }
        public Vector3 ActualDirection { get; private set; }
        public float DashRemaining { get; private set; }
        public int MoveCalls { get; private set; }
        public bool AwaitRelease { get; private set; }
        public string LastOwner { get; private set; }
        private Vector3 _dashDirection;
        private Vector3 _before;
        private float _stepTime;
        private bool _samplePending;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private const float Skin = .02f;

        public void Initialize(NavMeshAgent agent)
        {
            if (GetComponent<CharControlScript>() || GetComponent<CharacterController>() || GetComponent<Rigidbody>())
                throw new InvalidOperationException("Prototype requires exclusive movement ownership.");
            Agent = agent;
            Agent.ResetPath();
            Agent.updatePosition = true;
            Agent.updateRotation = false;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            Agent.isStopped = true; // Move is explicit; no autonomous path writer.
            _before = transform.position;
        }

        public static Vector3 Direction(Vector2 input, Quaternion camera, bool relative)
        {
            if (float.IsNaN(input.x) || float.IsInfinity(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.y)) return Vector3.zero;
            input = Vector2.ClampMagnitude(input, 1f);
            if (!relative) return new Vector3(input.x, 0, input.y);
            Vector3 forward = Vector3.ProjectOnPlane(camera * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return right * input.x + forward * input.y;
        }

        public bool BeginDash(Vector3 cursorDirection, Vector2 movement, Quaternion camera, bool compareMovementDirection = false)
        {
            if (Gates != MovementGate.None || AwaitRelease || DashRemaining > 0) return false;
            Vector3 direction = compareMovementDirection ? Direction(movement, camera, CameraRelative) : Vector3.ProjectOnPlane(cursorDirection, Vector3.up);
            if (direction.sqrMagnitude < .0001f) return false;
            _dashDirection = direction.normalized; // Fixed at commit, like legacy cursor dash.
            DashRemaining = DashDuration;
            return true;
        }

        public void Step(Vector2 input, Vector3 aimPoint, Quaternion camera, float dt)
        {
            if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh) { LastOwner = "off-navmesh"; return; }
            if (_samplePending) SampleDisplacement();
            bool suspended = Time.timeScale <= 0 || (Gates & (MovementGate.Paused | MovementGate.FocusLost)) != 0;
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0)
            {
                if (suspended) AwaitRelease = true;
                if ((Gates & MovementGate.FocusLost) != 0) DashRemaining = 0;
                _samplePending = false;
                ResetPresentation();
                LastOwner = suspended ? "suspended" : "invalid-step";
                return;
            }
            _before = transform.position;
            _stepTime = dt;
            _samplePending = true;
            LastOwner = "idle";
            if (suspended)
            {
                AwaitRelease = true;
                if ((Gates & MovementGate.FocusLost) != 0) DashRemaining = 0;
                LastOwner = "suspended";
                return;
            }
            if (AwaitRelease && input.sqrMagnitude < .0001f) AwaitRelease = false;
            if ((Gates & MovementGate.Hitstop) != 0) { LastOwner = "hitstop"; return; }
            Vector3 velocity;
            float activeTime = dt;
            if (ForcedVelocity.sqrMagnitude > .0001f) { velocity = ForcedVelocity; LastOwner = "forced"; }
            else if (DashRemaining > 0)
            {
                velocity = _dashDirection * DashSpeed;
                activeTime = Mathf.Min(dt, DashRemaining);
                DashRemaining = Mathf.Max(0, DashRemaining - activeTime);
                if (DashRemaining < .00001f) DashRemaining = 0; // No floating residue may block the next dash.
                LastOwner = "dash";
            }
            else if ((Gates & (MovementGate.RootedAttack | MovementGate.RootedCast)) != 0)
            { LastOwner = "rooted"; return; }
            else { velocity = AwaitRelease ? Vector3.zero : Direction(input, camera, CameraRelative) * Speed; LastOwner = "keyboard"; }

            if (LastOwner == "keyboard")
            {
                Vector3 facing = Vector3.ProjectOnPlane(aimPoint - transform.position, Vector3.up);
                if (facing.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(facing);
            }
            Vector3 delta = velocity * activeTime;
            if (GuardCollisions) delta = Constrain(delta);
            // Only position write while moving. No Warp, transform position or SetDestination.
            if (delta.sqrMagnitude > .00000001f) { Agent.Move(delta); MoveCalls++; }
        }

        private Vector3 Constrain(Vector3 delta)
        {
            float length = delta.magnitude;
            if (length < .00001f) return Vector3.zero;
            Vector3 feet = Agent.nextPosition;
            // Physical sweep catches solid geometry not represented in the baked NavMesh.
            int count = Physics.CapsuleCastNonAlloc(feet + Vector3.up * (Agent.radius + Skin),
                feet + Vector3.up * (Agent.height - Agent.radius), Agent.radius, delta / length,
                _hits, length + Skin, ~0, QueryTriggerInteraction.Ignore);
            if (count == _hits.Length) return Vector3.zero; // Saturation must fail closed.
            float allowed = length;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Dot(_hits[i].normal, Vector3.up) >= .7f) continue; // Walkable slope; NavMesh owns height.
                allowed = Mathf.Min(allowed, Mathf.Max(0, _hits[i].distance - Skin));
            }
            delta *= allowed / length;
            if (Agent.Raycast(feet + delta, out NavMeshHit edge))
            {
                float planar = Vector3.ProjectOnPlane(edge.position - feet, Vector3.up).magnitude;
                float requested = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude;
                if (requested > 0) delta *= Mathf.Clamp01((planar - Skin) / requested);
            }
            return delta;
        }

        public void SampleDisplacement()
        {
            if (!_samplePending || _stepTime <= 0) return;
            Vector3 actual = Vector3.ProjectOnPlane(transform.position - _before, Vector3.up);
            ActualPlanarSpeed = actual.magnitude / _stepTime;
            ActualDirection = actual.sqrMagnitude > .000001f ? actual.normalized : Vector3.zero;
            if (Presentation != null) Presentation.SetFloat("ActualPlanarSpeed", ActualPlanarSpeed);
            _samplePending = false;
        }

        private void LateUpdate() => SampleDisplacement();
        private void OnApplicationFocus(bool focused)
        {
            if (!ReadPhysicalInput) return;
            if (focused) Gates &= ~MovementGate.FocusLost;
            else Gates |= MovementGate.FocusLost;
        }
        private void ResetPresentation()
        {
            ActualPlanarSpeed = 0;
            ActualDirection = Vector3.zero;
            if (Presentation != null) Presentation.SetFloat("ActualPlanarSpeed", 0);
        }
        private void OnDisable() { DashRemaining = 0; ResetPresentation(); _samplePending = false; AwaitRelease = true; }

        private void Update()
        {
            if (!ReadPhysicalInput || View == null) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (keyboard == null || mouse == null) return;
            Vector2 raw = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            Ray ray = View.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.up, transform.position);
            Vector3 aim = plane.Raycast(ray, out float d) ? ray.GetPoint(d) : transform.position + transform.forward;
            if (keyboard.spaceKey.wasPressedThisFrame) BeginDash(aim - transform.position, raw, View.transform.rotation);
            if (keyboard.tabKey.wasPressedThisFrame) CameraRelative = !CameraRelative;
            Step(raw, aim, View.transform.rotation, Time.deltaTime);
        }
    }
}
