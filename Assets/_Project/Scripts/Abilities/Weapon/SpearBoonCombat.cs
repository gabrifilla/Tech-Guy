using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Run-owned spear geometry. Persistent projections never own player input, mana, or cooldown.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(PlayerActor))]
public sealed class SpearBoonCombat : MonoBehaviour
{
    private sealed class Snapshot
    {
        public WeaponScript Weapon;
        public ArsenalAbility Ability;
        public int Slot, Echoes;
        public long LifecycleRevision;
        public bool HasConfirmation;
        public Actor FirstConfirmed;
        public Vector3 FirstPoint;
        public float Windup, Recovery = .15f, Range, Width, Posture, Phantom;
        public bool Chain, Mark, Moon, ReturnLine;
        public Vector3 From, To, Direction;
        public AttackDamageBatch Damage;
        public ProtocolExecution Root;
        public readonly SpearBoonReceipts Receipts = new SpearBoonReceipts();
    }
    private sealed class Horizon { public Snapshot Cast; public ProtocolExecution Child; public float Remaining = 5; }
    private sealed class Stake { public Snapshot Cast; public Vector3 Point; public ProtocolExecution Child; public float Remaining = 6; public bool Spent; }
    private sealed class Orbit
    {
        public Snapshot Cast;
        public Vector3 Center, Point, Home;
        public float Elapsed, ReturnElapsed;
        public bool Returning;
        public ProtocolExecution MarkChild, EchoChild;
        public bool DuoConsumed;
    }
    private sealed class Delayed { public Snapshot Cast; public ProtocolExecution Child; public float Remaining, Fraction; public Vector3 From, To; }
    private PlayerActor _player;
    private AbilityHolder _holder;
    private ProtocolRuntime _protocols;
    private NavMeshAgent _agent;
    private SkillAnimationPlayer _animation;
    private bool _horizonEnabled, _siegeEnabled, _orbitEnabled, _returnMark, _moonEcho, _returnLine;
    private Snapshot _cast;
    private float _castElapsed;
    private bool _castContact, _locked, _wasStopped, _wasRotating;
    private Horizon _horizon;
    private readonly List<Stake> _stakes = new List<Stake>(6);
    private Orbit _orbit;
    private readonly List<Delayed> _delayed = new List<Delayed>(8);
    private readonly Collider[] _overlap = new Collider[256];
    private Collider[] _derivedOverlap;
    private readonly List<LineRenderer> _cues = new List<LineRenderer>(8);
    private Material _material;
    private WeaponScript _configuredWeapon;
    private int _revision;
    public bool IsExecuting => _cast != null;
    public int ActiveSlot => _cast?.Slot ?? -1;
    public float ExecutionDuration { get; private set; }
    public bool HasHorizon => _horizon != null;
    public int ActiveStakes => _stakes.Count;
    public bool HasOrbit => _orbit != null;
    public bool CanReturnOrbit => isActiveAndEnabled && _orbit != null && !_orbit.Returning && CanAdvance && !_player.HasOpenImpactFeedback;
    public int NativeContacts { get; private set; }
    public int StakePulses { get; private set; }
    public int HorizonDetonations { get; private set; }
    public float GameplayTime { get; private set; }
    private bool CanAdvance => _player && !_player.IsDead && !_player.IsCombatHitStopped &&
        (!_holder || _holder.isActiveAndEnabled && !_holder.BlocksWorldInput) && Time.timeScale > 0f;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>(); _holder = GetComponent<AbilityHolder>();
        _agent = GetComponent<NavMeshAgent>(); _protocols = GetComponent<ProtocolRuntime>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        _player.Died += OnDeath;
    }
    public void Configure(bool horizon, bool siege, bool orbit, bool returnMark = false, bool moonEcho = false, bool returnLine = false)
    {
        if ((_horizonEnabled && !horizon) || (_siegeEnabled && !siege) || (_orbitEnabled && !orbit)) StopEncounter();
        _horizonEnabled = horizon; _siegeEnabled = siege; _orbitEnabled = orbit;
        _returnMark = returnMark; _moonEcho = moonEcho; _returnLine = returnLine;
        _configuredWeapon = _player ? _player.CurrentWeapon : null;
        if (!_protocols) _protocols = GetComponent<ProtocolRuntime>();
    }
    public bool Handles(ArsenalAbility ability)
    {
        if (!_player || !_player.CurrentWeapon || WeaponRunModifiers.Identify(_player.CurrentWeapon) != RunWeaponFamily.Spear || !ability) return false;
        int slot = Array.IndexOf(_player.CurrentWeapon.abilities, ability);
        return slot == 0 && _horizonEnabled || slot == 2 && _siegeEnabled || slot == 3 && _orbitEnabled;
    }
    public bool CanUse(ArsenalAbility ability)
    {
        if (!isActiveAndEnabled || !CanAdvance || IsExecuting || !Handles(ability) || !ValidAbility(ability) || _player.HasOpenImpactFeedback ||
            !_protocols || !_protocols.IsOpen) return false;
        int slot = Array.IndexOf(_player.CurrentWeapon.abilities, ability);
        return (slot != 0 || _horizon == null) && (slot != 2 || _stakes.Count == 0) && (slot != 3 || _orbit == null);
    }
    private static bool ValidAbility(ArsenalAbility ability) => ability && ability.Hits > 0 && ability.Hits <= 32 &&
        ReactionNumeric.IsFinite(ability.Windup) && ability.Windup >= 0 &&
        ReactionNumeric.IsFinite(ability.Interval) && ability.Interval > 0 &&
        ReactionNumeric.IsFinite(ability.Range) && ability.Range > 0 &&
        ReactionNumeric.IsFinite(ability.Width) && ability.Width > 0 &&
        ReactionNumeric.IsFinite(ability.DamageMultiplier) && ability.DamageMultiplier >= 0;

    public void Use(ArsenalAbility ability)
    {
        if (!CanUse(ability)) return;
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 target = transform.position + transform.forward * (Array.IndexOf(_player.CurrentWeapon.abilities, ability) == 2 ? 7f : 8f);
#if ENABLE_INPUT_SYSTEM
        if (Camera.main && UnityEngine.InputSystem.Mouse.current != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            if (new Plane(Vector3.up, transform.position).Raycast(ray, out float distance)) target = ray.GetPoint(distance);
        }
#endif
        Use(ability, target);
    }
    /// <summary>Explicit ground targeting also supports injected aim sources without mouse or scene searches.</summary>
    public void Use(ArsenalAbility ability, Vector3 groundTarget)
    {
        if (!CanUse(ability) || !ReactionNumeric.IsFinite(groundTarget)) return;
        WeaponScript weapon = _player.CurrentWeapon;
        int slot = Array.IndexOf(weapon.abilities, ability);
        ArsenalCastPlan plan = _player.RunModifiers?.Plan(ability, slot) ?? new ArsenalCastPlan(ability);
        Vector3 from = transform.position, flat = groundTarget - from; flat.y = 0f;
        Vector3 direction = flat.sqrMagnitude > .0001f ? flat.normalized : transform.forward;
        float range = slot == 2 ? 7f : 8f;
        Vector3 to = ClampSolid(from, from + Vector3.ClampMagnitude(flat.sqrMagnitude > .0001f ? flat : direction * range, range), out _);
        int echoes = slot == 0 ? _player.RunModifiers?.Rank(WeaponBoon.EchoThrust) ?? 0 : 0;
        float total = plan.Damage * Mathf.Max(1, ability.Hits) * Mathf.Max(1, slot == 2 ? plan.Directions : 1);
        var identity = SpearLoopSteps.ReactionForSlot(slot, ArsenalSkillKind.Thrust);
        _cast = new Snapshot { Weapon = weapon, Ability = ability, Slot = slot, From = from, To = to, Direction = direction,
            Windup = slot == 3 ? .4f : slot == 2 ? .35f : Mathf.Max(.2f, plan.Windup), Range = range,
            Width = slot == 0 ? Mathf.Clamp(plan.Width, .45f, 1f) : 1f,
            Damage = _player.Stats.CaptureAttackDamage(weapon.attackDamage, total, 0f),
            Posture = 40f * identity.StanceScale * Mathf.Max(1, ability.Hits), Echoes = echoes,
            Phantom = slot == 0 ? plan.PhantomDelay : 0f, Chain = slot == 0 && plan.ChainThrust,
            Mark = _returnMark, Moon = _moonEcho, ReturnLine = _returnLine,
            Root = _protocols.BeginExecution(), LifecycleRevision = _protocols.LifecycleRevision };
        _castElapsed = 0; _castContact = false; ExecutionDuration = _cast.Windup + _cast.Recovery;
        _configuredWeapon = weapon;
        _locked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (_locked) { _wasStopped = _agent.isStopped; _wasRotating = _agent.updateRotation; _agent.ResetPath(); _agent.isStopped = true; _agent.updateRotation = false; }
    }
    public bool TryReturnOrbit()
    {
        if (!CanReturnOrbit) return false;
        BeginReturn(_orbit); return true;
    }
    public void NotifyNativePath(int slot, Vector3 from, Vector3 to)
    {
        if (!CanAdvance || !ReactionNumeric.IsFinite(from) || !ReactionNumeric.IsFinite(to)) return;
        int revision = _revision;
        to = ClampSolid(from, to, out _);
        if (slot == 2 && _horizon != null && SpearBoonGeometry.Crosses(_horizon.Cast.From, _horizon.Cast.To, from, to))
        {
            Horizon line = _horizon; _horizon = null; HorizonDetonations++;
            try { EmitLine(line.Cast, line.Child, line.Cast.From, line.Cast.To, .4f); }
            finally { line.Child?.Dispose(); }
            if (revision != _revision) return;
        }
        if (slot == 0 || slot == 3)
            for (int i = _stakes.Count - 1; i >= 0; i--)
            {
                Stake stake = _stakes[i];
                if (!stake.Spent && SpearBoonGeometry.DistanceToSegment(stake.Point, from, to) <= .8f) PulseStake(stake);
                // Damage callbacks can end the encounter and clear every projection synchronously.
                if (revision != _revision) return;
            }
    }
    private void Update()
    {
        if (!_player || _player.IsDead || _configuredWeapon && _player.CurrentWeapon != _configuredWeapon ||
            _protocols && !_protocols.IsOpen) { StopEncounter(); return; }
        if (!CanAdvance) return;
        float delta = _protocols ? _protocols.ScaledDelta : Time.deltaTime;
        if (!ReactionNumeric.IsFinite(delta) || delta <= 0) return;
        GameplayTime += delta;
        int revision = _revision;
        var casting = _cast;
        if (casting != null)
        {
            if (casting.Root != null && !casting.Root.IsLifecycleCurrent) { StopEncounter(); return; }
            _castElapsed += delta;
            _animation.Strike(casting.Slot == 0 ? SkillMotion.SpearThrust : SkillMotion.SpearSweep,
                _castElapsed, 0, casting.Windup, ExecutionDuration);
            if (!_castContact && _castElapsed >= casting.Windup)
            {
                _castContact = true;
                _animation.Contact(casting.Slot == 0 ? SkillMotion.SpearThrust : SkillMotion.SpearSweep);
                Contact(casting);
                if (revision != _revision) return;
            }
            if (_cast == casting && _castElapsed >= ExecutionDuration) ReleaseCast(false);
        }
        if (_horizon != null)
        {
            _horizon.Remaining -= delta;
            if (_horizon.Remaining <= 0 || !Live(_horizon.Cast)) { _horizon.Child?.Dispose(); _horizon = null; }
        }
        for (int i = _stakes.Count - 1; i >= 0; i--)
        {
            Stake stake = _stakes[i]; stake.Remaining -= delta;
            if (!stake.Spent && stake.Remaining > 0 && Live(stake.Cast) && HasEnemy(stake.Point, .7f)) PulseStake(stake);
            if (revision != _revision) return;
            if (stake.Spent || stake.Remaining <= 0 || !Live(stake.Cast)) { stake.Child?.Dispose(); _stakes.RemoveAt(i); }
        }
        for (int i = _delayed.Count - 1; i >= 0; i--)
        {
            Delayed delayed = _delayed[i]; delayed.Remaining -= delta;
            if (delayed.Remaining > 0 && Live(delayed.Cast)) continue;
            _delayed.RemoveAt(i);
            try { if (Live(delayed.Cast)) EmitLine(delayed.Cast, delayed.Child, delayed.From, delayed.To, delayed.Fraction); }
            finally { delayed.Child?.Dispose(); }
            if (revision != _revision) return;
        }
        if (_orbit != null) TickOrbit(_orbit, delta);
        DrawCues();
    }
    private bool Live(Snapshot cast) => isActiveAndEnabled && _player && !_player.IsDead && _player.CurrentWeapon == cast.Weapon &&
        _protocols && _protocols.IsOpen && cast.LifecycleRevision == _protocols.LifecycleRevision;
    private ProtocolExecution Child(Snapshot cast, ProtocolProducer producer = ProtocolProducer.Technique)
    {
        using (_protocols.OpenExecutionScope(cast.Root)) return _protocols.ReserveExecution(producer);
    }
    private void Contact(Snapshot cast)
    {
        if (!Live(cast)) return;
        int revision = _revision;
        if (cast.Slot == 0)
        {
            _horizon = new Horizon { Cast = cast, Child = Child(cast) };
            NativeLine(cast, cast.From, cast.To, .6f, 0);
            if (revision != _revision || !Live(cast)) return;
            NotifyNativePath(0, cast.From, cast.To);
            if (revision != _revision || !Live(cast)) return;
            if (cast.Phantom > 0) Schedule(cast, cast.From, cast.To, .6f, cast.Phantom);
            for (int i = 0; i < cast.Echoes; i++) Schedule(cast, cast.From, cast.To, .6f, .18f * (i + 1));
            if (cast.Chain) Chain(cast);
        }
        else if (cast.Slot == 2)
        {
            NotifyNativePath(2, cast.From, cast.To);
            if (revision != _revision || !Live(cast)) return;
            NativeLine(cast, cast.To - Vector3.right * .01f, cast.To + Vector3.right * .01f, .4f, 0, 3f);
            if (revision != _revision || !Live(cast)) return;
            for (int i = 0; i < 6; i++)
            {
                Vector3 point = SpearBoonGeometry.OrbitPoint(cast.To, cast.Direction, i / 6f, 3f);
                point = ClampSolid(cast.To, point, out _);
                _stakes.Add(new Stake { Cast = cast, Point = point, Child = Child(cast) });
            }
        }
        else
        {
            _orbit = new Orbit { Cast = cast, Center = cast.To,
                Point = ClampSolid(cast.To, SpearBoonGeometry.OrbitPoint(cast.To, cast.Direction, 0), out _),
                MarkChild = cast.Mark ? Child(cast) : null, EchoChild = cast.Moon ? Child(cast, ProtocolProducer.Echo) : null };
        }
    }
    private void Schedule(Snapshot cast, Vector3 from, Vector3 to, float fraction, float delay)
    {
        if (_delayed.Count >= 8) return;
        _delayed.Add(new Delayed { Cast = cast, From = from, To = to, Fraction = fraction,
            Remaining = delay, Child = Child(cast, ProtocolProducer.Echo) });
    }
    private void Chain(Snapshot cast)
    {
        if (!cast.HasConfirmation || !Live(cast) || !TryReserveDerivedQuery(out var limits)) return;
        Actor primary = cast.FirstConfirmed; Vector3 point = cast.FirstPoint;
        Actor next = null; float best = 36f; int sightTests = 0;
        foreach (Actor target in Query(point, point, 6f, _derivedOverlap))
        {
            if (!ValidTarget(target, cast) || target == primary) continue;
            if (sightTests++ >= limits.SightTestsPerQuery) break;
            if (!Visible(point, target.transform.position)) continue;
            float distance = (target.transform.position - point).sqrMagnitude;
            if (distance < best) { best = distance; next = target; }
        }
        if (!next) return;
        using (var child = Child(cast))
        {
            var roll = _player.Stats.RollAttackDamage(cast.Damage);
            _protocols.EmitBoonTarget(child, next, next.CombatGeneration, roll.Amount * .6f, cast.Posture * .6f);
        }
    }
    private void PulseStake(Stake stake)
    {
        if (stake.Spent) return;
        stake.Spent = true; StakePulses++;
        try
        {
            var roll = _player.Stats.RollAttackDamage(stake.Cast.Damage);
            _protocols.EmitBoonArea(stake.Child, stake.Point, 1.5f, roll.Amount * .1f, stake.Cast.Posture * .1f);
        }
        finally { stake.Child?.Dispose(); stake.Child = null; }
    }
    private void BeginReturn(Orbit orbit)
    {
        orbit.Returning = true; orbit.ReturnElapsed = 0;
        orbit.Home = _player.transform.position;
    }
    private void TickOrbit(Orbit orbit, float delta)
    {
        if (!Live(orbit.Cast)) { ReleaseOrbit(); return; }
        if (!orbit.Returning)
        {
            float target = Mathf.Min(3f, orbit.Elapsed + delta);
            int steps = Mathf.Clamp(Mathf.CeilToInt((target - orbit.Elapsed) * 64f), 1, 192);
            float fromTime = orbit.Elapsed;
            for (int i = 1; i <= steps && _orbit == orbit && !orbit.Returning; i++)
            {
                float time = Mathf.Lerp(fromTime, target, i / (float)steps);
                Vector3 next = SpearBoonGeometry.OrbitPoint(orbit.Center, orbit.Cast.Direction, time);
                Vector3 end = ClampSolid(orbit.Point, next, out bool blocked);
                NativeLine(orbit.Cast, orbit.Point, end, .25f, Mathf.Min(2, Mathf.FloorToInt((time - .00001f))));
                if (_orbit != orbit) return;
                NotifyNativePath(3, orbit.Point, end);
                if (_orbit != orbit) return;
                orbit.Point = end; orbit.Elapsed = time;
                if (blocked) BeginReturn(orbit);
            }
            if (_orbit == orbit && !orbit.Returning && orbit.Elapsed >= 3f) BeginReturn(orbit);
            return;
        }
        Vector3 old = orbit.Point;
        orbit.ReturnElapsed += delta;
        float speed = Mathf.Max(8f, Vector3.Distance(orbit.Point, orbit.Home) / Mathf.Max(.01f, 1f - orbit.ReturnElapsed + delta));
        Vector3 desired = Vector3.MoveTowards(old, orbit.Home, speed * delta);
        Vector3 to = ClampSolid(old, desired, out bool obstructed);
        NativeLine(orbit.Cast, old, to, .25f, 3);
        if (_orbit != orbit) return;
        NotifyNativePath(3, old, to);
        if (_orbit != orbit) return;
        orbit.Point = to;
        if (!obstructed && orbit.ReturnElapsed < 1f && (to - orbit.Home).sqrMagnitude > .01f) return;
        // A blocked return resolves at its last reachable point. Same receipt as the return path, never through the wall.
        NativeLine(orbit.Cast, to - Vector3.right * .01f, to + Vector3.right * .01f, .25f, 3, 1.25f);
        if (_orbit != orbit) return;
        if (orbit.Cast.Moon && orbit.EchoChild != null)
        {
            var roll = _player.Stats.RollAttackDamage(orbit.Cast.Damage);
            _protocols.EmitBoonArea(orbit.EchoChild, to, 1.5f, roll.Amount * .125f, orbit.Cast.Posture * .125f);
        }
        ReleaseOrbit();
    }
    private IEnumerable<Actor> Query(Vector3 from, Vector3 to, float radius, Collider[] colliders = null)
    {
        colliders ??= _overlap;
        int count = Physics.OverlapCapsuleNonAlloc(from + Vector3.up * .65f, to + Vector3.up * .65f, radius,
            colliders, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        var found = new List<Actor>(Mathf.Min(count, 128));
        for (int i = 0; i < count; i++)
        {
            Collider hit = colliders[i]; colliders[i] = null;
            Actor actor = hit ? hit.GetComponentInParent<Actor>() : null;
            if (actor && !found.Contains(actor)) found.Add(actor);
        }
        found.Sort((a, b) => EntityId.ToULong(a.GetEntityId()).CompareTo(EntityId.ToULong(b.GetEntityId())));
        return found;
    }
    private bool ValidTarget(Actor target, Snapshot cast) => target && target != _player && !target.IsDead &&
        target.isActiveAndEnabled && target.Affiliation == CombatAffiliation.Enemy &&
        _protocols && _protocols.IsBoonEnemy(target);
    private void NativeLine(Snapshot cast, Vector3 from, Vector3 to, float fraction, int passage, float radius = -1f)
    {
        if (!Live(cast)) return;
        int revision = _revision;
        float width = radius > 0 ? radius : cast.Width;
        var damaged = new List<Actor>();
        using (_player.BeginImpactFeedback())
        using (_protocols.OpenExecutionScope(cast.Root))
        {
            foreach (Actor target in Query(from, to, width))
            {
                if (revision != _revision || !Live(cast)) break;
                if (!ValidTarget(target, cast) || !Visible(from, target.transform.position)) continue;
                ulong id = EntityId.ToULong(target.GetEntityId()); long generation = target.CombatGeneration;
                if (!cast.Receipts.TryClaim(id, generation, passage)) continue;
                var roll = _player.Stats.RollAttackDamage(cast.Damage);
                float before = target.health;
                _player.DealResolvedAttackDamage(target, roll.Amount * fraction, roll.IsCritical);
                if (target && target.CombatGeneration == generation && target.health < before)
                {
                    NativeContacts++; damaged.Add(target);
                    if (!cast.HasConfirmation) { cast.HasConfirmation = true; cast.FirstConfirmed = target; cast.FirstPoint = target.transform.position; }
                    if (cast.Slot == 3 && passage < 3) cast.Receipts.ConfirmOutgoing(id, generation);
                    if (cast.Slot == 3 && passage == 3 && cast.Mark && cast.Receipts.TryConfirmReturn(id, generation) && _orbit != null)
                    {
                        float extra = cast.ReturnLine && !_orbit.DuoConsumed ? .5f : .2f;
                        if (cast.ReturnLine) _orbit.DuoConsumed = true;
                        _protocols.EmitBoonTarget(_orbit.MarkChild, target, generation, roll.Amount * extra, cast.Posture * extra);
                    }
                }
                // Posture is independent from HP immunity; the shared enemy controller still owns every break/resistance.
                if (revision == _revision && Live(cast) && target && !target.IsDead && target.CombatGeneration == generation)
                    _player.ApplyHitReactionTo(target, new HitReactionRequest(_player, target.transform.position,
                        (to - from).sqrMagnitude > .0001f ? (to - from).normalized : cast.Direction,
                        HitReactionType.None, cast.Slot == 2 ? HitStrength.Heavy : HitStrength.Medium,
                        cast.Posture * fraction, StanceBreakEffect.Stun, 0, .12f, 0, 0));
            }
            if (damaged.Count > 0 && _holder) _holder.NotifyAttackHits(_player, damaged);
        }
    }
    private void EmitLine(Snapshot cast, ProtocolExecution child, Vector3 from, Vector3 to, float fraction)
    {
        if (!Live(cast) || child == null || !child.IsValid || !TryReserveDerivedQuery(out var limits)) return;
        int revision = _revision, sightTests = 0;
        foreach (Actor target in Query(from, to, cast.Width, _derivedOverlap))
        {
            if (revision != _revision || !Live(cast)) break;
            if (!ValidTarget(target, cast)) continue;
            if (sightTests++ >= limits.SightTestsPerQuery) break;
            if (!Visible(from, target.transform.position)) continue;
            var roll = _player.Stats.RollAttackDamage(cast.Damage);
            _protocols.EmitBoonTarget(child, target, target.CombatGeneration, roll.Amount * fraction, cast.Posture * fraction);
        }
    }
    private bool TryReserveDerivedQuery(out ProtocolLimits limits)
    {
        limits = null;
        if (!_protocols || !_protocols.IsOpen || _protocols.ScaledDelta <= 0f || !_protocols.Budget.ReserveQuery()) return false;
        limits = _protocols.Inventory.Catalog.Limits;
        if (_derivedOverlap == null || _derivedOverlap.Length != limits.CollidersPerQuery)
            _derivedOverlap = new Collider[limits.CollidersPerQuery];
        return true;
    }
    private bool HasEnemy(Vector3 point, float radius)
    {
        if (!TryReserveDerivedQuery(out var limits)) return false;
        int sightTests = 0;
        foreach (Actor target in Query(point, point, radius, _derivedOverlap))
        {
            if (!_protocols.IsBoonEnemy(target)) continue;
            if (sightTests++ >= limits.SightTestsPerQuery) break;
            if (Visible(point, target.transform.position)) return true;
        }
        return false;
    }
    private static bool Visible(Vector3 from, Vector3 to)
    {
        ClampSolid(from, to, out bool blocked); return !blocked;
    }
    private static Vector3 ClampSolid(Vector3 from, Vector3 to, out bool blocked)
    {
        Vector3 delta = to - from; delta.y = 0;
        float nearest = delta.magnitude; blocked = false;
        if (nearest <= .0001f) return from;
        foreach (RaycastHit hit in Physics.RaycastAll(from + Vector3.up * .65f, delta.normalized, nearest,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<Actor>()) continue;
            if (hit.distance < nearest) { nearest = Mathf.Max(0, hit.distance - .04f); blocked = true; }
        }
        return from + delta.normalized * nearest;
    }
    public bool TryGetActionProgress(out ActionPhase phase, out float localProgress)
    {
        phase = _cast != null && _castElapsed >= _cast.Windup ? ActionPhase.Recovery : ActionPhase.Startup;
        localProgress = _cast == null ? 0 : phase == ActionPhase.Startup ? Mathf.Clamp01(_castElapsed / _cast.Windup) :
            Mathf.Clamp01((_castElapsed - _cast.Windup) / _cast.Recovery);
        return _cast != null;
    }
    public bool TryGetCancelContext(out CombatActionProfile profile, out float globalProgress)
    {
        profile = _cast?.Ability.CombatProfile; globalProgress = _cast == null ? 0 : Mathf.Clamp01(_castElapsed / ExecutionDuration);
        return _cast != null;
    }
    public bool TryGetCommitment(out CommitmentCategory commitment)
    {
        commitment = CommitmentCategory.Committed; return _cast != null;
    }
    /// <summary>Authorized action cancellation leaves already-launched independent projections alive.</summary>
    public void Cancel() => ReleaseCast(true);
    private void ReleaseCast(bool discardUnlaunched)
    {
        Snapshot cast = _cast; _cast = null;
        if (_locked && _agent && _agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = _wasStopped; _agent.updateRotation = _wasRotating; }
        _locked = false;
        if (_animation && cast != null) _animation.Release();
        if (cast != null && (discardUnlaunched && !_castContact || cast.Slot != 3)) cast.Root?.Dispose();
    }
    public void StopEncounter()
    {
        _revision++;
        ReleaseCast(true);
        _horizon?.Child?.Dispose(); _horizon = null;
        foreach (Stake stake in _stakes) stake.Child?.Dispose(); _stakes.Clear();
        foreach (Delayed delayed in _delayed) delayed.Child?.Dispose(); _delayed.Clear();
        ReleaseOrbit();
        foreach (LineRenderer cue in _cues) if (cue) cue.enabled = false;
    }
    private void ReleaseOrbit()
    {
        Orbit orbit = _orbit; _orbit = null;
        if (orbit == null) return;
        orbit.MarkChild?.Dispose(); orbit.EchoChild?.Dispose(); orbit.Cast.Root?.Dispose();
    }
    private void DrawCues()
    {
        int used = 0;
        if (_cast != null && !_castContact)
            DrawLine(ref used, _cast.From + Vector3.up * .08f, _cast.To + Vector3.up * .08f, new Color(.8f, .7f, .35f));
        if (_horizon != null) DrawLine(ref used, _horizon.Cast.From + Vector3.up * .08f, _horizon.Cast.To + Vector3.up * .08f, new Color(.3f, .8f, 1f));
        foreach (Stake stake in _stakes)
            if (!stake.Spent) DrawLine(ref used, stake.Point + Vector3.up * .1f, stake.Point + Vector3.up * 1.3f, new Color(.6f, .95f, 1f));
        if (_orbit != null) DrawLine(ref used, _orbit.Point + Vector3.up * .7f - _orbit.Cast.Direction * .45f,
            _orbit.Point + Vector3.up * .7f + _orbit.Cast.Direction * .45f, new Color(1f, .35f, .2f));
        for (int i = used; i < _cues.Count; i++) _cues[i].enabled = false;
    }
    private void DrawLine(ref int index, Vector3 from, Vector3 to, Color color)
    {
        if (index >= _cues.Count)
        {
            var child = new GameObject("Spear projection cue"); child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>(); line.positionCount = 2; line.useWorldSpace = true;
            line.startWidth = line.endWidth = .065f;
            if (!_material)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader) _material = new Material(shader);
            }
            line.sharedMaterial = _material; _cues.Add(line);
        }
        var cue = _cues[index++]; cue.enabled = true; cue.startColor = cue.endColor = color;
        cue.SetPosition(0, from); cue.SetPosition(1, to);
    }
    private void OnDeath(Actor actor) => StopEncounter();
    private void OnDisable() => StopEncounter();
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; if (_material) Destroy(_material); }
}
