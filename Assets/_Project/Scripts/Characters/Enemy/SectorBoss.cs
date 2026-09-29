using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>A heavy brawler: closes distance, commits to physical strikes and releases core shockwaves.</summary>
[RequireComponent(typeof(Actor), typeof(EnemyAI), typeof(CombatReactionController))]
[DisallowMultipleComponent]
public sealed class SectorBoss : MonoBehaviour
{
    [SerializeField] private PlayerActor _player;
    [SerializeField, Min(1)] private float _maximumHealth = 1800;
    [SerializeField, Min(1)] private float _slamDamage = 30;
    [SerializeField, Min(1)] private float _bombDamage = 22;
    private Actor _actor;
    private NavMeshAgent _agent;
    private Animator _animator;
    private CombatReactionController _reaction;
    private EnemyCombatActions _actions;
    private Coroutine _routine;
    private bool _interrupted;
    public string DisplayName => "GUARDIÃO DO NÚCLEO";
    public float MaximumHealth => _maximumHealth;
    public bool IsEnraged => _actor && _actor.health <= _actor.maxHealth*.5f;
    public bool IsTelegraphing => _actions && _actions.IsWindingUp;
    public string CurrentAttackName => _actions ? _actions.AttackName : "";
    public Vector3 AttackCenter => _actions ? _actions.AttackCenter : transform.position;
    public float AttackRadius => _actions ? _actions.AttackRadius : 0;
    public EnemyAttackKind CurrentAttack => _actions ? _actions.CurrentKind : EnemyAttackKind.Punch;
    public int ResolvedAttacks => _actions ? _actions.ResolvedImpacts : 0;
    public int SecondPhaseFollowups { get; private set; }
    public void Configure(PlayerActor player) => _player=player;

    private void Awake()
    {
        _actor=GetComponent<Actor>(); _agent=GetComponent<NavMeshAgent>(); _animator=GetComponent<Animator>();
        GetComponent<EnemyAI>().enabled=false;
        _actions=GetComponent<EnemyCombatActions>() ?? gameObject.AddComponent<EnemyCombatActions>();
        _reaction=GetComponent<CombatReactionController>();
        _reaction.ConfigureRank(EnemyRank.Boss);
        _actor.SetMaxHealth(_maximumHealth); _actor.Died+=OnDeath;
    }
    private void Start()
    {
        if(!_player) { Debug.LogError("SectorBoss requires its encounter player.",this); enabled=false; return; }
        if(_agent && _agent.enabled && _agent.isOnNavMesh) { _agent.ResetPath(); _agent.isStopped=false; }
        _routine=StartCoroutine(Fight());
    }
    private bool Alive => _player && !_player.IsDead && _actor && !_actor.IsDead;
    private bool CanAttack() => isActiveAndEnabled && Alive && !_interrupted &&
        (!_reaction || !_reaction.IsControlLocked) &&
        (!_agent || !_agent.enabled || !_agent.isOnNavMesh || !_agent.isStopped);

    private IEnumerator Fight()
    {
        yield return new WaitForSeconds(1);
        int sequence=0;
        while(Alive)
        {
            while(Alive && _reaction.IsControlLocked) yield return null;
            if(!Alive) yield break;
            _interrupted=false;
            bool enraged=IsEnraged;
            // Walk into threat range before punching; never punch the air from the far end of the room.
            yield return Approach(8f);
            if(!CanAttack()) { yield return new WaitForSeconds(.5f); continue; }
            float distance=Vector3.Distance(transform.position,_player.transform.position);
            EnemyAttackKind move;
            if(distance>4.5f && _agent && _agent.enabled && _agent.isOnNavMesh) move=EnemyAttackKind.Charge;
            else if(sequence%3==2) move=EnemyAttackKind.Shockwave;
            else
            {
                float meleeRange=1.45f*Mathf.Clamp(transform.lossyScale.y,.5f,2f)+.35f;
                yield return Approach(meleeRange);
                if(!CanAttack()) continue;
                move=Vector3.Distance(transform.position,_player.transform.position)>meleeRange+.2f
                    ? EnemyAttackKind.Shockwave
                    : sequence%3==0 ? EnemyAttackKind.HeavySlam : EnemyAttackKind.DoublePunch;
            }
            if(!CanAttack()) continue;
            sequence++;
            yield return _actions.Perform(move,_player,move==EnemyAttackKind.DoublePunch ? _bombDamage : _slamDamage,CanAttack,true);
            if(enraged && CanAttack() && _actions.Completed)
            {
                yield return new WaitForSeconds(.45f);
                if(CanAttack())
                {
                    if(move==EnemyAttackKind.Charge || move==EnemyAttackKind.DoublePunch)
                    {
                        if(Vector3.Distance(transform.position,_player.transform.position)<=1.6f*Mathf.Clamp(transform.lossyScale.y,.5f,2f)+.4f)
                        {
                            SecondPhaseFollowups++;
                            yield return _actions.Perform(EnemyAttackKind.HeavySlam,_player,_slamDamage,CanAttack,true);
                        }
                    }
                    else if(move==EnemyAttackKind.Shockwave)
                    {
                        SecondPhaseFollowups++;
                        yield return _actions.Perform(EnemyAttackKind.Shockwave,_player,_bombDamage,CanAttack,true);
                    }
                }
            }
            yield return new WaitForSeconds(enraged ? .9f : 1.4f);
        }
    }
    private IEnumerator Approach(float range)
    {
        if(!_agent || !_agent.enabled || !_agent.isOnNavMesh) yield break;
        float deadline=Time.time+3f;
        while(CanAttack() && Vector3.Distance(transform.position,_player.transform.position)>range && Time.time<deadline)
        {
            _agent.SetDestination(_player.transform.position);
            if(_animator && _animator.HasState(0,Animator.StringToHash("Walk"))) _animator.Play("Walk");
            yield return new WaitForSeconds(.15f);
        }
        if(_agent && _agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
    }
    public void InterruptAttack() { _interrupted=true; if(_actions) _actions.Cancel(); }
    public static bool Contains(Vector3 point,Vector3 center,float radius) =>
        new EnemyAttackArea(EnemyAttackShape.Circle,center,Vector3.forward,radius).Contains(point);
    private void OnDeath(Actor actor) => StopFight();
    private void OnDisable() => StopFight();
    private void StopFight()
    {
        if(_routine!=null) StopCoroutine(_routine);
        _routine=null; InterruptAttack();
        if(_agent && _agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
    }
    private void OnDestroy() { if(_actor) _actor.Died-=OnDeath; }
}
