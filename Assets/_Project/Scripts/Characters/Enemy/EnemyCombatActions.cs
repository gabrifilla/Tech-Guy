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
        EnemyAttackKind.AimedShot => "Tiro certeiro",
        EnemyAttackKind.SpreadShot => "Rajada em leque",
        EnemyAttackKind.SniperShot => "Tiro de precisão",
        EnemyAttackKind.LobShot => "Bomba em arco",
        EnemyAttackKind.HookLine => "Gancho",
        EnemyAttackKind.HazardPlace => "Zona de perigo",
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

    /// <summary>
    /// Executes a telegraphed attack of <paramref name="kind"/> against <paramref name="target"/>.
    /// </summary>
    /// <param name="meleeWindupOverride">
    /// Optional per-archetype telegraph duration (seconds) for the melee <see cref="EnemyAttackKind.Punch"/>/
    /// <see cref="EnemyAttackKind.DoublePunch"/> beat, letting the caller make the Punch windup
    /// archetype-scoped (Rush &lt; Grunt, R3.4/R4.3). A non-positive value (the default) keeps each
    /// attack's own authored windup, so existing callers behave identically. Heavy's slam/ground-pound
    /// and Charger's charge own their own telegraph timing and ignore this override.
    /// </param>
    public IEnumerator Perform(EnemyAttackKind kind, Actor target, float damage, Func<bool> canAttack, bool boss = false, float meleeWindupOverride = -1f)
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
            case EnemyAttackKind.AimedShot: yield return AimedShot(target,damage,valid); break;
            case EnemyAttackKind.SpreadShot: yield return SpreadShot(target,damage,valid); break;
            case EnemyAttackKind.SniperShot: yield return SniperShot(target,damage,valid); break;
            case EnemyAttackKind.LobShot: yield return LobShot(target,damage,valid); break;
            case EnemyAttackKind.HookLine: yield return HookLine(target,damage,valid); break;
            case EnemyAttackKind.HazardPlace: yield return HazardPlace(target,valid); break;
            default:
                int punches = kind == EnemyAttackKind.DoublePunch ? 2 : 1;
                for (int i=0; i<punches && valid(); i++)
                {
                    bool heavy = kind == EnemyAttackKind.HeavySlam;
                    // Punch/DoublePunch windup: use the archetype override when supplied (Rush < Grunt,
                    // R3.4/R4.3), otherwise the authored default (.7s lead jab, .5s follow-up). Heavy's
                    // slam keeps its own long windup and ignores the override. The EnemyAttackExecution
                    // 0.25s floor still applies, so a too-small override can never go below the global min.
                    float punchWindup = i==0 ? .7f : .5f;
                    if (!heavy && meleeWindupOverride > 0f)
                        punchWindup = i==1 ? meleeWindupOverride * (.5f/.7f) : meleeWindupOverride;
                    yield return Melee(target,damage*(punches==2 ? .65f : heavy ? 1.25f : 1f), valid,
                        heavy ? SkillMotion.Slam : i==1 ? SkillMotion.PunchLeft : SkillMotion.PunchRight,
                        heavy ? (boss ? 1.15f : .95f) : punchWindup, boss, heavy);
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
        // On a miss or a geometry-obstructed dash (both leave hit == false), the Charger enters a
        // longer recovery of at least 1.0s during which it starts no new attack (R6.5/R6.6). A dash
        // that connected keeps the shorter recovery so a landed charge is not over-punished.
        if(!valid()) yield break;
        _animation.Contact(SkillMotion.PunchRight);
        float recovery = hit ? (boss ? 1f : .8f) : Mathf.Max(1f, boss ? 1f : .8f);
        yield return Recover(SkillMotion.PunchRight,.5f,recovery,valid);
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

    // Straight-shot tuning shared by the ranged archetypes. Kept local so no per-shot asset is authored;
    // per-archetype range/damage/interval differences come through EnemyProfile and the Perform damage arg.
    private const float ShotColorR = 1f, ShotColorG = .35f, ShotColorB = .2f;
    private static readonly Color ShotColor = new Color(ShotColorR,ShotColorG,ShotColorB);
    private const float StraightShotSpeed = 16f;
    private const float StraightShotRadius = .35f;
    private const float ShooterShotRange = 18f;
    private const float SpreadShotRange = 14f;
    private const float SniperShotRange = 26f;
    private const int SpreadShotCount = 5;      // >=3 (R8.2)
    private const float SpreadConeDegrees = 40f; // fixed forward cone, locked at telegraph start (R8.3)

    // Bomber lob tuning (R10). The arced EnemyProjectile owns the ground telegraph + single land check;
    // these only shape the arc's reach and the impact area. Per-archetype damage comes through Perform.
    private const float LobShotRange = 20f;          // max horizontal reach of the lob
    private const float LobShotSpeed = 10f;          // horizontal travel speed feeding the arc duration
    private const float LobShotRadius = .35f;        // projectile body overlap radius (unused on land, kept for Spawn)
    private const float LobShotImpactRadius = 2f;    // ground-area impact radius (R10.2/R10.3)
    private const float LobShotWindup = .35f;        // brief aim before the projectile leaves the hand

    // Hooker line tuning (R12). This case owns the locked-direction line telegraph and the
    // connect/miss decision at fire; the actual displacement is the player-side pull entry point
    // (IExternalPullTarget.BeginExternalPull, implemented by PlayerActor in task 9.1).
    private const float HookLineWindup = .6f;        // >= Grunt melee windup (0.4s baseline, R4.3/R12.4)
    private const float HookLineRange = 12f;         // length of the locked hook line
    private const float HookLineTolerance = 1f;      // hook-line tolerance in [0.5,1.5]m (R12.5)
    private const float HookPullMaxDuration = 1.5f;  // bounded pull duration <=1.5s (R12.3/R12.6)
    private static readonly Color HookColor = new Color(.7f,.35f,1f);

    // Hazard_Caster placement tuning (R11). The concrete HazardZone.Activate is driven by
    // HazardCasterBehavior (task 8.3); this case only owns the >=0.25s placement telegraph (R11.3).
    private const float HazardPlaceWindup = .25f;    // >=0.25s placement telegraph (R11.3)
    private const float HazardPlaceRadius = 2.5f;    // telegraph footprint hint; the zone owns its real radius
    private static readonly Color HazardColor = new Color(1f,.5f,.15f);

    /// <summary>
    /// Shooter (R7): telegraph >=0.25s then fire a SINGLE straight <see cref="EnemyProjectile"/> toward
    /// the target. The shot cancels without firing if the enemy is control-locked (via <paramref name="valid"/>)
    /// or if line of sight to the target is broken during the windup — LoS is folded into the windup
    /// predicate per the design note ("add LoS to the predicate"). Requirements: 7.2, 7.4, 7.6.
    /// </summary>
    private IEnumerator AimedShot(Actor target,float damage,Func<bool> valid)
    {
        Vector3 forward=Face(target);
        Vector3 origin=MuzzleOrigin(forward);
        AttackCenter=origin; AttackRadius=0;
        // LoS-aware windup predicate: control lock is already inside valid(); add the sight check so a
        // broken line of sight cancels the shot mid-windup without firing (R7.6).
        Func<bool> canFire=()=>valid() && HasLineOfSight(target);
        yield return _warning.Execute(Array.Empty<EnemyAttackArea>(),target,.25f,0,ShotColor,canFire,
            animateWindup:t=>_animation.Strike(SkillMotion.PunchRight,t*.25f,0,.25f,.6f),
            showWarning:false,impactHold:0);
        if(!_warning.Completed || !canFire()) yield break;
        _animation.Contact(SkillMotion.PunchRight); ResolvedImpacts++;
        // Aim captured at windup end; single straight projectile travels over frames and hits at most once.
        Vector3 aim=Face(target);
        EnemyProjectile.Spawn(MuzzleOrigin(aim),aim,_owner,target,damage,StraightShotSpeed,
            ShooterShotRange,StraightShotRadius,EnemyProjectile.PathKind.Straight,ShotColor);
        yield return Recover(SkillMotion.PunchRight,.25f,.3f,valid);
    }

    /// <summary>
    /// Spread_Shooter (R8): telegraph >=0.25s, orient the cone center toward the target AT telegraph start
    /// and lock it, then fire <see cref="SpreadShotCount"/> (>=3) straight projectiles at EQUAL angular
    /// spacing across a fixed forward cone. The volley cancels on control lock during the windup, emitting
    /// nothing. Requirements: 8.2, 8.3, 8.4, 8.5.
    /// </summary>
    private IEnumerator SpreadShot(Actor target,float damage,Func<bool> valid)
    {
        // Cone center locked toward the target's position at telegraph start and held fixed (R8.3).
        Vector3 center=Face(target);
        Vector3 origin=MuzzleOrigin(center);
        AttackCenter=origin; AttackRadius=0;
        yield return _warning.Execute(Array.Empty<EnemyAttackArea>(),target,.25f,0,ShotColor,valid,
            animateWindup:t=>_animation.Strike(SkillMotion.PunchRight,t*.25f,0,.25f,.6f),
            showWarning:false,impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        _animation.Contact(SkillMotion.PunchRight); ResolvedImpacts++;
        // Equal angular spacing across the fixed cone: N shots span [-half, +half] in even steps.
        Vector3 muzzle=MuzzleOrigin(center);
        for(int i=0;i<SpreadShotCount;i++)
        {
            float fraction=SpreadShotCount>1 ? i/(float)(SpreadShotCount-1) : .5f;
            float angle=Mathf.Lerp(-SpreadConeDegrees*.5f,SpreadConeDegrees*.5f,fraction);
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*center;
            EnemyProjectile.Spawn(muzzle,direction,_owner,target,damage,StraightShotSpeed,
                SpreadShotRange,StraightShotRadius,EnemyProjectile.PathKind.Straight,ShotColor);
        }
        yield return Recover(SkillMotion.PunchRight,.25f,.3f,valid);
    }

    /// <summary>
    /// Sniper (R9): aim-line telegraph >=1.0s that INTENSIFIES toward impact (reusing the
    /// <see cref="EnemyAttackExecution"/> color-lerp-to-white pattern via the outline warning), then fires
    /// a single straight projectile along the aim line captured at windup END, applying damage at most once
    /// (the projectile hits once and is destroyed). Cancels on control lock or LoS break during the windup.
    /// Requirements: 9.3, 9.5, 9.6.
    /// </summary>
    private IEnumerator SniperShot(Actor target,float damage,Func<bool> valid)
    {
        Vector3 forward=Face(target);
        Vector3 origin=MuzzleOrigin(forward);
        AttackCenter=origin; AttackRadius=0;
        // Aim-line telegraph: a lane the length of the firing range so the intensifying line reads as the
        // shot's path. EnemyAttackExecution lerps its color toward white across the windup (R9.3 intensify).
        var line=new EnemyAttackArea(EnemyAttackShape.Lane,origin,forward,SniperShotRange,StraightShotRadius*2);
        Func<bool> canFire=()=>valid() && HasLineOfSight(target);
        // damage=0 here: the aim line is a warning only; the actual hit is resolved by the projectile so
        // damage is applied at most once (R9.5), never twice (once by the beat and once by the projectile).
        yield return _warning.Execute(new[]{line},target,1f,0,Frost,canFire,
            animateWindup:t=>_animation.Strike(SkillMotion.PunchRight,t*1f,0,1f,1.4f),
            showWarning:true,impactHold:0);
        if(!_warning.Completed || !canFire()) yield break;
        _animation.Contact(SkillMotion.PunchRight); ResolvedImpacts++;
        // Aim line captured at the moment the windup completes (R9.5).
        Vector3 aim=Face(target);
        EnemyProjectile.Spawn(MuzzleOrigin(aim),aim,_owner,target,damage,StraightShotSpeed*1.5f,
            SniperShotRange,StraightShotRadius,EnemyProjectile.PathKind.Straight,Frost);
        yield return Recover(SkillMotion.PunchRight,1f,.45f,valid);
    }

    /// <summary>
    /// Bomber (R10): a brief aim windup, then launch a single ARCED <see cref="EnemyProjectile"/> toward
    /// the target's ground position. The projectile itself owns the whole area contract: it draws the
    /// ground-area impact telegraph the moment it launches and keeps it visible for the FULL flight,
    /// intensifying it toward its impact look as it approaches (R10.2, R10.4); on landing it performs a
    /// SINGLE area check over the impact radius and applies damage at most once when the target is inside
    /// (R10.3), then removes both itself and the telegraph within the same frame (R10.5). This case
    /// therefore never resolves damage directly — passing <c>damage=0</c> to the beat guarantees the hit
    /// is applied at most once, by the projectile. Requirements: 10.2, 10.3, 10.4, 10.5.
    /// </summary>
    private IEnumerator LobShot(Actor target,float damage,Func<bool> valid)
    {
        Vector3 forward=Face(target);
        Vector3 origin=MuzzleOrigin(forward);
        // Report the intended landing footprint so observers (AI/tests) can read the telegraphed area.
        Vector3 groundPoint=target.transform.position; groundPoint.y=transform.position.y;
        AttackCenter=groundPoint; AttackRadius=LobShotImpactRadius;
        // Brief aim only. The area telegraph belongs to the projectile (full flight), so the windup shows
        // no separate warning ring — damage=0 keeps the beat harmless; the projectile is the sole hit.
        yield return _warning.Execute(Array.Empty<EnemyAttackArea>(),target,LobShotWindup,0,HazardColor,valid,
            animateWindup:t=>_animation.Strike(SkillMotion.Slam,t*LobShotWindup,0,LobShotWindup,LobShotWindup+.6f),
            showWarning:false,impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        _animation.Contact(SkillMotion.Slam); ResolvedImpacts++;
        // Arced projectile lands on the target's ground position captured at windup end. Its Arced path
        // draws + intensifies the ground-area telegraph for the full flight and resolves the single land
        // hit over LobShotImpactRadius, then tears down projectile + telegraph on land (R10.4/R10.5).
        Vector3 aim=Face(target);
        EnemyProjectile.Spawn(MuzzleOrigin(aim),aim,_owner,target,damage,LobShotSpeed,
            LobShotRange,LobShotRadius,EnemyProjectile.PathKind.Arced,HazardColor,LobShotImpactRadius);
        yield return Recover(SkillMotion.Slam,LobShotWindup,.5f,valid);
    }

    /// <summary>
    /// Hooker (R12): face the target and LOCK the hook's straight-line direction at telegraph start,
    /// showing a LINE telegraph (<see cref="EnemyAttackShape.Lane"/>) for a windup at least as long as
    /// the Grunt's melee windup (R12.4, floor 0.4s — <see cref="HookLineWindup"/> is 0.6s). The
    /// direction and origin captured here are held fixed for the whole windup and used unchanged at
    /// fire (R12.2). When the windup completes, the fire step measures the player's distance to the
    /// locked hook line: WITHIN <see cref="HookLineTolerance"/> (0.5–1.5m, R12.5) => connect, invoking
    /// the player-side bounded pull via <see cref="IExternalPullTarget.BeginExternalPull"/> (owned by
    /// PlayerActor, task 9.1); BEYOND tolerance => miss, resolving with no displacement (R12.5). The
    /// beat carries <c>damage=0</c>: the hook displaces, it does not deal telegraph damage. Cancels on
    /// control lock during the windup like every other beat.
    /// </summary>
    private IEnumerator HookLine(Actor target,float damage,Func<bool> valid)
    {
        // Lock the straight-line direction + origin at telegraph start; both stay fixed until fire (R12.2).
        Vector3 forward=Face(target);
        Vector3 origin=MuzzleOrigin(forward);
        Vector3 lineEnd=origin+forward*HookLineRange;
        // Line telegraph along the locked direction (a lane the length of the hook's reach), shown for
        // the full windup so the pull is always readable and avoidable before it fires.
        var line=new EnemyAttackArea(EnemyAttackShape.Lane,origin,forward,HookLineRange,HookLineTolerance*2);
        AttackCenter=origin; AttackRadius=HookLineRange;
        // damage=0: connect resolves as a pull, not telegraph damage. Windup >= Grunt melee windup (R12.4).
        yield return _warning.Execute(new[]{line},target,HookLineWindup,0,HookColor,valid,
            animateWindup:t=>_animation.Strike(SkillMotion.PunchRight,t*HookLineWindup,0,HookLineWindup,HookLineWindup+.5f),
            showWarning:true,impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        _animation.Contact(SkillMotion.PunchRight); ResolvedImpacts++;
        // Fire: connect vs miss is decided against the LOCKED hook line (origin->lineEnd), not a fresh
        // aim, so a player who left the telegraphed line during the windup is missed (R12.2/R12.5).
        bool connect=SegmentContains(target.transform.position,origin,lineEnd,HookLineTolerance);
        if(connect && target is IExternalPullTarget pullTarget)
        {
            // Within tolerance => pull the player toward this Hooker over a bounded <=1.5s (R12.3).
            // PlayerActor.BeginExternalPull (task 9.1) suspends CharControlScript steering, moves the
            // player agent, and always restores control (R12.6/R12.7). Until 9.1 lands, no type
            // implements IExternalPullTarget, so this branch is skipped and the hook resolves as a
            // harmless miss rather than breaking compilation.
            pullTarget.BeginExternalPull(transform,HookPullMaxDuration);
        }
        // Beyond tolerance (or no pull target) => miss: resolve with no displacement (R12.5). Nothing
        // else to do — the recovery below plays whether the hook connected or missed.
        yield return Recover(SkillMotion.PunchRight,HookLineWindup,.4f,valid);
    }

    /// <summary>
    /// Hazard_Caster (R11): show a placement telegraph at the zone position for a windup of at least
    /// 0.25s (R11.3), then complete. This case owns ONLY the pre-activation telegraph; it does not create
    /// or reference a <see cref="HazardZone"/> — <c>HazardCasterBehavior</c> (task 8.3) drives the actual
    /// zone lifecycle and calls <see cref="HazardZone.Activate"/> once this telegraph completes (R11.4),
    /// since the zone variant/prefab is configured on that behavior via serialized fields. Because the
    /// beat carries <c>damage=0</c> the placement itself never damages the player. Requirements: 11.3, 11.4.
    /// </summary>
    private IEnumerator HazardPlace(Actor target,Func<bool> valid)
    {
        Face(target);
        // Placement footprint at the target's ground position; the actual zone radius is owned by the
        // HazardZone the behavior activates. The telegraph is a warning ring shown for the full windup.
        Vector3 zonePoint=target.transform.position; zonePoint.y=transform.position.y;
        var area=new EnemyAttackArea(EnemyAttackShape.Circle,zonePoint,transform.forward,HazardPlaceRadius);
        AttackCenter=zonePoint; AttackRadius=HazardPlaceRadius;
        yield return _warning.Execute(new[]{area},target,HazardPlaceWindup,0,HazardColor,valid,
            animateWindup:t=>_animation.Strike(SkillMotion.Slam,t*HazardPlaceWindup,0,HazardPlaceWindup,HazardPlaceWindup+.4f),
            showWarning:true,impactHold:0);
        if(!_warning.Completed || !valid()) yield break;
        // Telegraph completed: mark the beat so Completed reports true and HazardCasterBehavior can
        // activate its configured HazardZone at zonePoint (R11.4). No damage is applied here.
        _animation.Contact(SkillMotion.Slam); ResolvedImpacts++;
        yield return Recover(SkillMotion.Slam,HazardPlaceWindup,.3f,valid);
    }

    /// <summary>Muzzle point in front of and slightly above the enemy, so shots clear its own body.</summary>
    private Vector3 MuzzleOrigin(Vector3 forward)
    {
        float size=Mathf.Clamp(transform.lossyScale.y,.5f,2f);
        return transform.position+forward*(.65f*size)+Vector3.up*(1.1f*size);
    }

    /// <summary>
    /// True when nothing static stands between the enemy's muzzle and the target. Actors (including the
    /// target and this enemy) never block sight — only static geometry does — matching the projectile's
    /// clip rule. Used to fold LoS into the Shooter/Sniper windup predicate (R7.6, R9.6).
    /// </summary>
    private bool HasLineOfSight(Actor target)
    {
        if(!target) return false;
        Vector3 forward=target.transform.position-transform.position; forward.y=0;
        forward = forward.sqrMagnitude>.001f ? forward.normalized : transform.forward;
        Vector3 start=MuzzleOrigin(forward);
        Vector3 eye=target.transform.position+Vector3.up*1f;
        Vector3 delta=eye-start;
        float distance=delta.magnitude;
        if(distance<.01f) return true;
        Vector3 direction=delta/distance;
        foreach(var hit in Physics.RaycastAll(start,direction,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.transform.IsChildOf(transform)) continue;
            if(hit.collider.GetComponentInParent<Actor>()) continue; // actors don't block sight
            return false; // static geometry between muzzle and target breaks LoS
        }
        return true;
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
