using UnityEngine;
using UnityEngine.InputSystem;

public partial class CharControlScript
{
    [Header("Direct keyboard movement (GPR-C01)")]
    [SerializeField] private bool _directKeyboardMovement;
    private PlayerNavigationMotor _directMotor;
    private Vector3 _directPreviousPosition;
    private Vector3 _directVelocity;
    private Vector3 _directLocomotionVelocity;
    private bool _directMovementRelease;
    private bool _keyboardOwnsMovement;
    private bool _directOriginalRotation;
    private UnityEngine.AI.ObstacleAvoidanceType _directOriginalAvoidance;
    private bool _directHasFocus = true;
    private bool _gameplayReleaseRequired = true;
    private int _gameplayReleaseFrame = -1;

    internal bool GameplayInputReady
    {
        get
        {
            if (!_directKeyboardMovement) return true;
            if (!isActiveAndEnabled || !_directHasFocus || !playerActor || playerActor.IsDead ||
                Time.timeScale <= 0 || _abilityHolder && _abilityHolder.BlocksWorldInput)
            { InvalidateDirectGameplayInput(); return false; }
            if (_gameplayReleaseRequired)
            {
                bool held = GamePreferences.AnyGameplayHeld;
                if (held) return false;
                // The shared gate has observed release for every gameplay control.
                // Local gates must agree now; otherwise a fresh press on the following
                // frame would require a second release just because Update ran later.
                _waitForAttackRelease = false;
                _directMovementRelease = false;
                _gameplayReleaseRequired = false;
                _gameplayReleaseFrame = Time.frameCount;
            }
            return Time.frameCount > _gameplayReleaseFrame;
        }
    }

    internal void InvalidateDirectGameplayInput()
    {
        if (!_directKeyboardMovement) return;
        _gameplayReleaseRequired = true;
        _directMovementRelease = true;
        _directVelocity = _directLocomotionVelocity = Vector3.zero;
        _groundMoveBuffer.Discard();
        _waitForAttackRelease = true;
        _basicAttackDriver.DiscardPendingInput();
        _primaryHeldLast = false; _primaryHeldSeconds = 0;
        if (_playerHUD) _playerHUD.SetChargeIndicator(0);
        if (_abilityHolder) _abilityHolder.DiscardUncommittedInput();
    }

    public bool DirectMovementEnabled => _directKeyboardMovement;
    internal bool DirectInputHasFocus => _directHasFocus;
    public Vector3 ActualMovementVelocity => _directKeyboardMovement ? _directVelocity : agent ? agent.velocity : Vector3.zero;

    public void SetDirectMovementEnabled(bool enabled)
    {
        if (_directKeyboardMovement == enabled && _directMotor != null) return;
        _directKeyboardMovement = enabled;
        if (enabled) InitializeDirectMovement();
        else
        {
            GamePreferences.ReleaseDirectControls(this);
            if (agent) { agent.updateRotation = _directOriginalRotation; agent.obstacleAvoidanceType = _directOriginalAvoidance; }
            _directVelocity = Vector3.zero;
        }
    }

    private void InitializeDirectMovement()
    {
        if (!_directKeyboardMovement || !agent) return;
        if (_directMotor == null)
        {
            _directMotor = new PlayerNavigationMotor(agent);
            _directOriginalRotation = agent.updateRotation;
            _directOriginalAvoidance = agent.obstacleAvoidanceType;
        }
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        agent.updateRotation = false;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
        _directPreviousPosition = transform.position;
        _directVelocity = Vector3.zero;
        _directLocomotionVelocity = Vector3.zero;
        InvalidateDirectGameplayInput();
        GamePreferences.UseDirectControls(this);
    }

    internal void MoveThroughDirectMotor(Vector3 offset, bool ignoreActors, PlayerMovementSource source = PlayerMovementSource.Locomotion, bool synchronizePose = false)
    {
        if (_directMotor == null) InitializeDirectMovement();
        if (!playerActor || playerActor.IsDead || Time.timeScale <= 0 || playerActor.IsCombatHitStopped) return;
        if (_abilityHolder && _abilityHolder.BlocksWorldInput) return;
        if (playerActor.HasExternalPull && source != PlayerMovementSource.ExternalPull) return;
        if (dashScript && dashScript.IsDashing(gameObject) && source != PlayerMovementSource.Dash && source != PlayerMovementSource.ExternalPull) return;
        if (source == PlayerMovementSource.Locomotion && (playerBusy || _abilityHolder && _abilityHolder.IsCasting && !_abilityHolder.MovementAllowedWhileCasting)) return;
        _directMotor?.Move(offset, ignoreActors, synchronizePose, source == PlayerMovementSource.Locomotion);
    }

    internal static Vector3 ResolveDirectDirection(Vector2 input, Quaternion camera)
    {
        if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.x) || float.IsInfinity(input.y)) return Vector3.zero;
        Vector3 forward = Vector3.ProjectOnPlane(camera * Vector3.forward, Vector3.up);
        if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
        forward.Normalize();
        input = Vector2.ClampMagnitude(input, 1);
        return Vector3.Cross(Vector3.up, forward) * input.x + forward * input.y;
    }

    internal Vector3 ConfiguredMovementDirection
    {
        get { var view = mainCamera ? mainCamera : Camera.main; return ResolveDirectDirection(GamePreferences.MovementInput, view ? view.transform.rotation : Quaternion.identity); }
    }

    private void HandleDirectMovement()
    {
        _groundMoveBuffer.Discard();
        Vector2 input = GamePreferences.MovementInput;
        if (_directMovementRelease)
        {
            if (input.sqrMagnitude > .0001f) return;
            _directMovementRelease = false;
        }
        if (Time.timeScale <= 0 || playerActor.IsCombatHitStopped || !_directHasFocus)
        { _directMovementRelease = true; return; }
        Camera view = mainCamera ? mainCamera : Camera.main;
        if (!view) return;
        // No facing overwrite during a committed basic/cast or a body traversal.
        if (!playerBusy && !(_abilityHolder && _abilityHolder.IsCasting))
        {
            Ray ray = view.ScreenPointToRay(GetPointerPosition());
            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float distance)) FacePosition(ray.GetPoint(distance));
        }
        if (input.sqrMagnitude > .0001f)
        {
            if (playerBusy) return; // Rooted basic remains owned by the combat executor.
            if (agent.hasPath) agent.ResetPath();
            _keyboardOwnsMovement = true;
            MoveThroughDirectMotor(ResolveDirectDirection(input, view.transform.rotation) * agent.speed * Time.deltaTime, false);
        }
        if (target) TryAttackTarget(); // Preserve tap/hold authorization, without chasing a destination.
    }

    private void SampleDirectMovement()
    {
        if (!_directKeyboardMovement) return;
        Vector3 delta = transform.position - _directPreviousPosition;
        _directPreviousPosition = transform.position;
        delta.y = 0;
        _directVelocity = Time.deltaTime > 0 && playerActor && !playerActor.IsCombatHitStopped ? delta / Time.deltaTime : Vector3.zero;
        _directLocomotionVelocity = _keyboardOwnsMovement ? _directVelocity : Vector3.zero;
        if (isDashing || playerBusy || _abilityHolder && _abilityHolder.IsCasting) return;
        SetAnimations();
    }

    private void OnApplicationFocus(bool focused)
    {
        _directHasFocus = focused;
        if (_directKeyboardMovement && !focused) { RequireAttackRelease(); }
    }

    private void OnDestroy() => GamePreferences.ReleaseDirectControls(this);
}
