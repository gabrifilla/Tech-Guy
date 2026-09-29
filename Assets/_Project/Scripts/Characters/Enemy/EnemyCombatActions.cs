using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Body-driven attacks. Animation contact, travel and damage share the same clock.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Actor))]
public sealed class EnemyCombatActions : MonoBehaviour
{
    [SerializeField] private SkillAnimationLibrary _motions;
    private readonly EnemyAttackExecution _warning = new EnemyAttackExecution();
    private Actor _owner;
    private NavMeshAgent _agent;
    private SkillAnimationPlayer _animation;
    private Animator _animator;
    private CombatGroundRing _wave;
    private GameObject _bolt;
    private Material _boltMaterial;
    private int _version;
    private bool _busy, _agentRotation;
    private static readonly Color Warm = new Color(1f,.55f,.16f);
    private static readonly Color Frost = new Color(.25f,.85f,1f);
    public bool IsWindingUp => _warning.IsWindingUp;
    public bool Completed { get; private set; }
    public EnemyAttackKind CurrentKind { get; private set; }
    public int ResolvedImpacts { get; private set; }
    public Vector3 AttackCenter { get; private set; }
    public float AttackRadius { get; private set; }
    public string AttackName => CurrentKind switch
    {
        EnemyAttackKind.Charge => "Investida",
        EnemyAttackKind.DoublePunch => "Um-dois",
        EnemyAttackKind.HeavySlam => "Pancada pesada",
        EnemyAttackKind.FrostBolt => "Estilhaço de gelo",
        EnemyAttackKind.Shockwave => "Onda do núcleo",
        _ => "Soco"
    };

    private void Awake()
    {
        _owner = GetComponent<Actor>(); _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        if (_motions) _animation.Configure(_motions);
    }
    public void ConfigureAnimations(SkillAnimationLibrary library)
    { _motions=library; if(_animation) _animation.Configure(library); }

    public IEnumerator Perform(EnemyAttackKind kind, Actor target, float damage, Func<bool> canAttack, bool boss = false)
    {
        Cancel();
        int version = _version;
        CurrentKind = kind; _busy = true; Completed = false;
        int impactsBefore=ResolvedImpacts;
        if (_agent) { _agentRotation = _agent.updateRotation; _agent.updateRotation = false; }
        Func<bool> valid = () => version == _version && isActiveAndEnabled && _owner && !_owner.IsDead &&
            target && !target.IsDead && target.isActiveAndEnabled && canAttack();
        if (!valid()) { Cancel(); yield break; }
        switch (kind)
        {
            case EnemyAttackKind.Charge: yield return Charge(target,damage,valid,boss); break;
            case EnemyAttackKind.FrostBolt: yield return Bolt(target,damage,valid); break;
            case EnemyAttackKind.Shockwave: yield return Shockwave(target,damage,valid); break;
            default:
                int punches = kind == EnemyAttackKind.DoublePunch ? 2 : 1;
                for (int i=0; i<punches && valid(); i++)
                {
                    bool heavy = kind == EnemyAttackKind.HeavySlam;
                    yield return Melee(target,damage*(punches==2 ? .65f : heavy ? 1.25f : 1f), valid,
                        heavy ? SkillMotion.Slam : i==1 ? SkillMotion.PunchLeft : SkillMotion.PunchRight,
                        heavy ? (boss ? 1.15f : .95f) : i==0 ? .7f : .5f, boss, heavy);
                }
                break;
        }
        if (version != _version) yield break;
        Completed = valid() && ResolvedImpacts>impactsBefore;
        _animation.Release();
        if (_agent) _agent.updateRotation = _agentRotation;
        _busy = false;
        ClearEffects();
    }

    private Vector3 Face(Actor target)
    {
        Vector3 forward = target.transform.position-transform.position; forward.y=0;
        forward = forward.sqrMagnitude>.001f ? forward.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(forward);
        return forward;
    }

    private IEnumerator Melee(Actor target,float damage,Func<bool> valid,SkillMotion motion,float windup,bool boss,bool heavy)
    {
        if (!valid()) yield break;
        Vector3 forward = Face(target), origin = transform.position;
        float size = Mathf.Clamp(transform.lossyScale.y,.5f,2f);
        float reach = 1.6f*size+.4f;
        var area = heavy
            ? new EnemyAttackArea(EnemyAttackShape.Circle,origin+forward*reach*.65f,forward,.95f*size+.3f)
            : new EnemyAttackArea(EnemyAttackShape.Cone,origin,forward,reach,angle:75);
        AttackCenter=area.Center; AttackRadius=area.Reach;
        yield return _warning.Execute(new[]{area},target,windup,damage,Warm,valid,
            () =>
            {
                _animation.Contact(motion); ResolvedImpacts++;
                if (heavy)
                {
                    _wave=CombatGroundRing.Create(null,"Ground impact",Warm);
                    _wave.Draw(area.Center,area.Reach,.18f);
                }
                else GauntletImpactVfx.Spawn(origin,forward,reach,1,Warm,false,false,motion==SkillMotion.PunchLeft?-1:1);
            }, t=>_animation.Strike(motion,t*windup,0,windup,windup+.5f), showWarning:heavy, impactHold:0);
        if (!_warning.Completed || !valid()) yield break;
        yield return Recover(motion,windup,heavy ? .85f : .3f,valid);
        if (_wave) { Destroy(_wave.gameObject); _wave=null; }
    }

    private IEnumerator Recover(SkillMotion motion,float contact,float duration,Func<bool> valid)
    {
        for(float elapsed=0;elapsed<duration && valid();elapsed+=Time.deltaTime)
        {
            _animation.Strike(motion,contact+elapsed,0,contact,contact+duration);
            yield return null;
        }
    }

    private IEnumerator Charge(Actor target,float damage,Func<bool> valid,bool boss)
    {
        if (!_agent || !_agent.enabled || !_agent.isOnNavMesh) yield break;
        Vector3 origin=transform.position, forward=Face(target);
        float distance=Mathf.Clamp(Vector3.Distance(origin,target.transform.position)+1f,2f,boss?10f:6f);
        Vector3 end=origin+forward*distance;
        if (_agent.Raycast(end,out NavMeshHit edge)) end=edge.position;
        distance=Vector3.Distance(origin,end);
        if(distance<.5f) yield break;
        float radius=boss ? 1.15f : .65f;
        var lane=new EnemyAttackArea(EnemyAttackShape.Lane,origin-forward*radius,forward,distance+radius*2,radius*2);
        AttackCenter=origin; AttackRadius=distance;
        float windup=boss ? 1.15f : .85f;
        yield return _warning.Execute(new[]{lane},target,windup,0,Warm,valid,
            animateWindup:t=>_animation.Strike(SkillMotion.PunchRight,t*.25f,0,1,1.5f),impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        bool hit=false;
        float travelled=0, speed=boss ? 9f : 10f;
        _animation.Release();
        if(_animator && _animator.HasState(0,Animator.StringToHash("Walk"))) _animator.Play("Walk");
        ResolvedImpacts++;
        while(travelled<distance && valid())
        {
            if(Time.deltaTime<=0) { yield return null; continue; }
            float step=Mathf.Min(speed*Time.deltaTime,distance-travelled);
            Vector3 start=_agent.nextPosition;
            float allowed=ClipTravel(start+Vector3.up*.8f,forward,step,.35f);
            _agent.Move(forward*allowed);
            Vector3 finish=_agent.nextPosition;
            if(!hit && SegmentContains(target.transform.position,start,finish,radius))
            { target.TakeDamage(damage); hit=true; }
            travelled+=Vector3.Distance(start,finish);
            if(allowed+.001f<step || (step>.001f && Vector3.Distance(start,finish)<.001f)) break;
            yield return null;
        }
        // The attacker visibly commits to the endpoint, leaving a punish window after a miss.
        if(!valid()) yield break;
        _animation.Contact(SkillMotion.PunchRight);
        yield return Recover(SkillMotion.PunchRight,.5f,boss?1f:.8f,valid);
    }

    private IEnumerator Bolt(Actor target,float damage,Func<bool> valid)
    {
        Vector3 forward=Face(target);
        _bolt=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _bolt.name="Enemy frost projectile";
        var collider=_bolt.GetComponent<Collider>(); collider.enabled=false; Destroy(collider);
        _boltMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _boltMaterial.SetColor("_BaseColor",Frost);
        _bolt.GetComponent<Renderer>().sharedMaterial=_boltMaterial;
        var trail=_bolt.AddComponent<TrailRenderer>(); trail.sharedMaterial=_boltMaterial;
        trail.time=.12f; trail.startWidth=.2f; trail.endWidth=0;
        trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        AttackCenter=transform.position; AttackRadius=0;
        yield return _warning.Execute(Array.Empty<EnemyAttackArea>(),target,.95f,0,Frost,valid,
            animateWindup:t=>
            {
                _animation.Strike(SkillMotion.PunchRight,t*.95f,0,.95f,1.3f);
                Transform hand=_animator && _animator.isHuman ? _animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
                _bolt.transform.position=hand ? hand.position :
                    transform.position+forward*.65f+Vector3.up*(1.1f*Mathf.Clamp(transform.lossyScale.y,.5f,2f));
                _bolt.transform.localScale=Vector3.one*Mathf.Lerp(.08f,.4f,t);
            },showWarning:false,impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        _animation.Contact(SkillMotion.PunchRight); ResolvedImpacts++;
        for(float travelled=0;travelled<12 && valid();)
        {
            if(Time.deltaTime<=0) { yield return null; continue; }
            float step=Mathf.Min(8f*Time.deltaTime,12-travelled);
            Vector3 start=_bolt.transform.position;
            float allowed=ClipTravel(start,forward,step,.2f);
            Vector3 finish=start+forward*allowed;
            _bolt.transform.position=finish;
            if(SegmentContains(target.transform.position,start,finish,.5f))
            { target.TakeDamage(damage); break; }
            travelled+=allowed;
            if(allowed+.001f<step) break;
            yield return null;
        }
        if(_bolt && valid())
        {
            _wave=CombatGroundRing.Create(null,"Frost shatter",Frost);
            Vector3 point=_bolt.transform.position; point.y=transform.position.y;
            _wave.Draw(point,.45f,.12f);
            _bolt.SetActive(false);
            yield return Recover(SkillMotion.PunchRight,.95f,.45f,valid);
        }
    }

    private IEnumerator Shockwave(Actor target,float damage,Func<bool> valid)
    {
        Vector3 forward=Face(target), center=transform.position+forward*1.8f;
        AttackCenter=center; AttackRadius=2;
        var area=new EnemyAttackArea(EnemyAttackShape.Circle,center,forward,2);
        yield return _warning.Execute(new[]{area},target,1.3f,0,Warm,valid,
            ()=>_animation.Contact(SkillMotion.Slam),
            t=>_animation.Strike(SkillMotion.Slam,t*1.3f,0,1.3f,2),impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        _wave=CombatGroundRing.Create(null,"Expanding core shockwave",Warm);
        ResolvedImpacts++;
        bool hit=false;
        float radius=0;
        while(radius<10 && valid())
        {
            if(Time.deltaTime<=0) { yield return null; continue; }
            float next=Mathf.Min(10,radius+6f*Time.deltaTime);
            _wave.Draw(center,next,.3f);
            if(!hit && WaveContains(target.transform.position,center,radius,next,.3f))
            { target.TakeDamage(damage); hit=true; }
            radius=next;
            yield return null;
        }
        yield return Recover(SkillMotion.Slam,1.3f,.65f,valid);
    }

    private float ClipTravel(Vector3 start,Vector3 direction,float distance,float radius)
    {
        float allowed=distance;
        foreach(var hit in Physics.SphereCastAll(start,radius,direction,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<Actor>()) continue;
            allowed=Mathf.Min(allowed,Mathf.Max(0,hit.distance-.02f));
        }
        return allowed;
    }

    public static bool SegmentContains(Vector3 point,Vector3 start,Vector3 end,float radius)
    {
        if(Mathf.Abs(point.y-start.y)>2.5f) return false;
        point.y=start.y=end.y=0;
        Vector3 delta=end-start;
        float t=delta.sqrMagnitude>.00001f ? Mathf.Clamp01(Vector3.Dot(point-start,delta)/delta.sqrMagnitude) : 0;
        return (point-(start+delta*t)).sqrMagnitude<=radius*radius;
    }

    public static bool WaveContains(Vector3 point,Vector3 center,float previous,float current,float width)
    {
        Vector3 delta=point-center;
        if(Mathf.Abs(delta.y)>2.5f) return false;
        delta.y=0;
        return delta.magnitude>=Mathf.Max(0,previous-width) && delta.magnitude<=current+width;
    }

    public void Cancel()
    {
        _version++; Completed=false; _warning.Cancel();
        if(_animation) _animation.Release();
        if(_busy && _agent) _agent.updateRotation=_agentRotation;
        _busy=false; ClearEffects();
    }
    private void ClearEffects()
    {
        if(_wave) { _wave.gameObject.SetActive(false); Destroy(_wave.gameObject); _wave=null; }
        if(_bolt) { _bolt.SetActive(false); Destroy(_bolt); _bolt=null; }
        if(_boltMaterial) { Destroy(_boltMaterial); _boltMaterial=null; }
    }
    private void OnDisable() => Cancel();
}
