using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.Profiling;

/// <summary>Refined presentation only. Native AI/actions/reactions retain every gameplay decision.</summary>
public sealed class FirstSectorEnemyPose : MonoBehaviour
{
    [Serializable] public sealed class Leg
    {
        public Transform hip,upper,lower,foot;
        public Vector3 restFoot;
        [NonSerialized] public Vector3 planted,swingFrom;
        [NonSerialized] public float previousPhase;
        [NonSerialized] public float swingTime;
    }
    [SerializeField] private bool _ranged;
    [SerializeField] private bool _weightedLocomotion;
    [SerializeField] private bool _laminatedPresentation;
    [SerializeField] private Transform _body,_head,_core,_barrel;
    [SerializeField] private Transform[] _shells=Array.Empty<Transform>(),_jaws=Array.Empty<Transform>();
    [SerializeField] private Leg[] _legs=Array.Empty<Leg>();
    private Vector3[] _shellRest;
    private Vector3 _headRest,_coreRest,_barrelRest,_lastPosition;
    private Actor _actor;private NavMeshAgent _agent;private EnemyCombatActions _actions;private CombatReactionController _reaction;
    private float _clock,_stride,_flinch,_commit,_opening;private int _impacts;private bool _feetReady;
    private Vector3 _lastForward;private float _turnBank;
    private static readonly ProfilerMarker Marker=new ProfilerMarker("RUNEXP03.EnemyPose");
    public string Phase {get;private set;}="idle";
    public bool Ranged=>_ranged;
    public float MaximumPlantedFootDrift {get;private set;}
    private void Awake()
    {
        _actor=GetComponent<Actor>();_agent=GetComponent<NavMeshAgent>();_actions=GetComponent<EnemyCombatActions>();_reaction=GetComponent<CombatReactionController>();
        _shellRest=new Vector3[_shells.Length];for(int i=0;i<_shells.Length;i++)_shellRest[i]=_shells[i].localPosition;
        if(_head)_headRest=_head.localPosition;if(_core)_coreRest=_core.localScale;if(_barrel)_barrelRest=_barrel.localPosition;
    }
    private void OnEnable()
    {
        _actor=GetComponent<Actor>();_actor.DamageReceived+=OnHit;_actor.Died+=OnDied;
        _clock=_stride=_flinch=_commit=_opening=0;_impacts=0;_feetReady=false;MaximumPlantedFootDrift=0;Phase="idle";_lastPosition=transform.position;
        _lastForward=transform.forward;_turnBank=0;
    }
    private void OnDisable(){if(_actor){_actor.DamageReceived-=OnHit;_actor.Died-=OnDied;}}
    private void OnHit(Actor actor,float damage){_flinch=.16f;}
    private void OnDied(Actor actor)
    {
        Phase="despawn";
        // Pure visual child, owned by the same encounter. No Actor/collider/AI or persistent root.
        var ghost=Instantiate(_body.gameObject,_body.position,_body.rotation,transform.parent);
        ghost.name="Fragmento/Emissor · fragmentação visual";ghost.transform.localScale=_body.lossyScale;
        var despawn=ghost.AddComponent<FirstSectorEnemyDespawn>();
        if(_weightedLocomotion)despawn.EnableDisassembly();
    }
    private void LateUpdate()
    {
        if(Time.timeScale<=0||Time.deltaTime<=0||!_actor||_actor.IsDead)return;
        using(Marker.Auto())
        {
            float dt=Time.deltaTime;_clock+=dt;_flinch=Mathf.Max(0,_flinch-dt);_commit=Mathf.Max(0,_commit-dt);
            if(_actions && _actions.ResolvedImpacts!=_impacts){_impacts=_actions.ResolvedImpacts;_commit=_ranged?.45f:.3f;}
            bool windup=_actions&&_actions.IsWindingUp;float speed=_agent&&_agent.enabled&&_agent.isOnNavMesh?_agent.velocity.magnitude:0;
            float activeEnd=_ranged?.27f:.18f;
            Phase=_flinch>0?"hit":windup?"windup":_commit>activeEnd?"attack":_commit>0?"recovery":speed>.15f?"move":"idle";
            _opening=Mathf.MoveTowards(_opening,windup?1:0,dt*(_weightedLocomotion?3:7));
            if(_laminatedPresentation)
            {
                float turnRate=Vector3.SignedAngle(_lastForward,transform.forward,Vector3.up)/dt;
                _turnBank=Mathf.Lerp(_turnBank,Mathf.Clamp(-turnRate*.03f,-3.5f,3.5f),1-Mathf.Exp(-dt*10));
                _lastForward=transform.forward;
            }
            if(_ranged)RangedPose(speed);else MeleePose(speed,dt);
        }
    }
    private void RangedPose(float speed)
    {
        // Barrel remains at the native muzzle. Only post-emission recoil moves it briefly backward.
        _body.localPosition=Vector3.zero;_body.localRotation=Quaternion.identity;
        float recoil=_commit>.27f?Mathf.Sin(Mathf.InverseLerp(.45f,.27f,_commit)*Mathf.PI):0;
        if(_barrel)_barrel.localPosition=_barrelRest+Vector3.back*(recoil*.075f);
        for(int i=0;i<_shells.Length;i++)
        {
            float sign=i==0?-1:1;
            _shells[i].localPosition=_shellRest[i]+new Vector3(sign*_opening*.2f,Mathf.Sin(_clock*2.4f+i*.4f)*.035f,0);
            float flinch=_weightedLocomotion?-8*_flinch/.16f:0;
            _shells[i].localRotation=Quaternion.Euler(-Mathf.Min(speed,3)*2+flinch,sign*_opening*24,sign*Mathf.Sin(_clock*1.7f)*2+(_laminatedPresentation?_turnBank*.4f:0));
        }
        if(_core)_core.localScale=_coreRest*(1+_opening*.055f+Mathf.Sin(_clock*3)*.012f);
    }
    private void MeleePose(float speed,float dt)
    {
        float snap=_commit>.18f?Mathf.InverseLerp(.18f,.3f,_commit):0;
        _body.localPosition=new Vector3(0,Mathf.Sin(_clock*4)*.012f-_opening*.035f,_opening*-.06f+snap*.09f);
        if(_weightedLocomotion)_body.localPosition+=Vector3.down*(Mathf.Abs(Mathf.Sin(_stride*Mathf.PI*2))*.018f*Mathf.Clamp01(speed/3));
        _body.localRotation=Quaternion.Euler(_flinch>0?-11:_opening*11-snap*13,0,_laminatedPresentation?_turnBank:0);
        if(_head){_head.localPosition=_headRest;_head.localRotation=Quaternion.Euler(-_opening*8+snap*8,0,0);}
        for(int i=0;i<_jaws.Length;i++)_jaws[i].localRotation=Quaternion.Euler(0,(i==0?-1:1)*_opening*16,0);
        bool lockPose=_reaction&&(_reaction.IsControlLocked||_reaction.OwnsFlinchPresentation);
        if(Vector3.Distance(transform.position,_lastPosition)>1.5f)_feetReady=false;
        _lastPosition=transform.position;_stride+=speed*dt/.92f;
        for(int i=0;i<_legs.Length;i++)
        {
            var leg=_legs[i];float phase=Mathf.Repeat(_stride+(i==0||i==3?0:.5f),1);
            Vector3 rest=_body.TransformPoint(leg.restFoot),target;
            if(!lockPose)rest.y=transform.position.y-.045f;
            if(!_feetReady){leg.planted=rest;leg.swingFrom=rest;leg.previousPhase=phase;leg.swingTime=-1;}
            bool planted=false;
            if(lockPose){target=rest;leg.planted=rest;leg.swingTime=-1;}
            else if(speed<.12f)
            {target=Vector3.Lerp(leg.planted,rest,dt*8);leg.planted=target;}
            else
            {
                // A sharp turn may exhaust reach before the nominal gait phase. Lift that foot
                // instead of clamping a planted target and visibly dragging it across the floor.
                if(leg.swingTime<0 && (Vector3.Distance(leg.hip.position,leg.planted)>.8f || phase>=.65f && leg.previousPhase<.65f))
                {leg.swingFrom=leg.foot.position;leg.swingTime=0;}
                if(leg.swingTime>=0)
                {
                    leg.swingTime=Mathf.Min(1,leg.swingTime+dt*Mathf.Max(5,speed/.32f));
                    float t=leg.swingTime;
                    target=Vector3.Lerp(leg.swingFrom,rest+transform.forward*.18f,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.15f;
                    if(t>=1){leg.planted=target;leg.swingTime=-1;}
                }
                else{target=leg.planted;planted=true;}
            }
            float side=(i&1)==0?-1:1;Vector3 hip=leg.hip.position,delta=target-hip;
            const float upper=.44f,lower=.5f;float distance=Mathf.Clamp(delta.magnitude,.07f,upper+lower-.015f);Vector3 direction=delta.sqrMagnitude>.0001f?delta.normalized:Vector3.down;
            Vector3 foot=hip+direction*distance;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            Vector3 bend=Vector3.ProjectOnPlane(transform.right*side+Vector3.up*.5f,direction).normalized;
            Vector3 knee=hip+direction*along+bend*height;
            Segment(leg.upper,hip,knee);Segment(leg.lower,knee,foot);
            leg.foot.position=foot;leg.foot.rotation=Quaternion.LookRotation(transform.forward,Vector3.up);
            if(planted)MaximumPlantedFootDrift=Mathf.Max(MaximumPlantedFootDrift,Vector3.Distance(foot,leg.planted));
            leg.previousPhase=phase;
        }
        _feetReady=true;
    }
    private static void Segment(Transform part,Vector3 start,Vector3 end)
    {part.position=start;part.rotation=Quaternion.LookRotation(end-start,Vector3.up);part.localScale=new Vector3(1,1,Vector3.Distance(start,end));}
}
