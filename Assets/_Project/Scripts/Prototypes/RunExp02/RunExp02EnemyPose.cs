using UnityEngine;
using UnityEngine.AI;
using Unity.Profiling;

/// <summary>Presentation only. Production actions, reactions and navigation own all gameplay.</summary>
public sealed class RunExp02EnemyPose : MonoBehaviour
{
    [SerializeField] private bool _ranged;
    [SerializeField] private Transform _body;
    [SerializeField] private Transform[] _limbs;
    private Vector3[] _rest;
    private Actor _actor;
    private EnemyCombatActions _actions;
    private NavMeshAgent _agent;
    private float _clock, _flinch, _commit;
    private int _impacts;
    private static readonly ProfilerMarker PoseMarker = new ProfilerMarker("RUNEXP02.CustomPose");
    public string Phase { get; private set; } = "idle";
    public bool Ranged => _ranged;
    private void Awake()
    {
        _actor = GetComponent<Actor>(); _agent = GetComponent<NavMeshAgent>();
        _actions = GetComponent<EnemyCombatActions>();
        _rest = new Vector3[_limbs.Length];
        for (int i = 0; i < _limbs.Length; i++) _rest[i] = _limbs[i].localPosition;
    }
    private void OnEnable()
    {
        _actor = GetComponent<Actor>(); _actor.DamageReceived += Flinch;
        _clock=_flinch=_commit=0;_impacts=0;Phase="idle";
    }
    private void OnDisable() { if (_actor) _actor.DamageReceived -= Flinch; }
    private void Flinch(Actor actor, float amount) { _flinch = .16f; }
    private void LateUpdate()
    {
        if (Time.deltaTime <= 0 || !_actor || _actor.IsDead || !_body) return;
        using (PoseMarker.Auto())
        {
            float dt = Time.deltaTime;
            _clock += dt; _flinch = Mathf.Max(0, _flinch - dt); _commit = Mathf.Max(0, _commit - dt);
            if (_actions && _actions.ResolvedImpacts != _impacts)
            { _impacts = _actions.ResolvedImpacts; _commit = .45f; }
            bool windup = _actions && _actions.IsWindingUp;
            float speed = _agent && _agent.enabled && _agent.isOnNavMesh ? _agent.velocity.magnitude : 0;
            Phase = _flinch > 0 ? "hit" : windup ? "windup" : _commit > .27f ? "attack" : _commit > 0 ? "recovery" : speed > .15f ? "move" : "idle";
            float anticipation = windup ? 1 : 0;
            float strike = _commit > .27f ? (_commit - .27f) / .18f : 0;
            _body.localPosition = new Vector3(0, _ranged ? Mathf.Sin(_clock * 2.1f) * .075f : Mathf.Sin(_clock * 15) * Mathf.Min(speed,.8f) * .025f,
                windup ? -.15f : strike * .24f);
            _body.localRotation = Quaternion.Euler(_flinch > 0 ? -14 : anticipation * (_ranged ? -9 : 16) - strike * 18, 0, 0);
            for (int i = 0; i < _limbs.Length; i++)
            {
                float sign = (i & 1) == 0 ? -1 : 1;
                if (_ranged)
                {
                    _limbs[i].localPosition = _rest[i] + new Vector3(sign * anticipation * .22f, 0, strike * -.18f);
                    _limbs[i].localRotation = Quaternion.Euler(0, sign * anticipation * 18, sign * Mathf.Sin(_clock * 2) * 3);
                }
                else
                {
                    // Diagonal pairs alternate; the root remains exclusively NavMesh/reaction-owned.
                    float gait = Mathf.Sin(_clock * (8 + speed * 2) + (i == 0 || i == 3 ? 0 : Mathf.PI));
                    _limbs[i].localPosition = _rest[i] + new Vector3(0, Mathf.Max(0,gait) * Mathf.Min(speed,1) * .12f, gait * Mathf.Min(speed,1) * .16f);
                    _limbs[i].localRotation = Quaternion.Euler(gait * Mathf.Min(speed,1) * 19, 0, sign * anticipation * 10);
                }
            }
        }
    }
}
