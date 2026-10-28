using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Run-local Bow transformations. Holder owns activation, mana and cooldown exactly once.</summary>
[RequireComponent(typeof(PlayerActor)), DisallowMultipleComponent]
public sealed class BowBoonCombat : MonoBehaviour
{
    private PlayerActor _player;
    private AbilityHolder _holder;
    private ProtocolRuntime _protocols;
    private SkillAnimationPlayer _animation;
    private RunBoons _run;
    private NavMeshAgent _agent;
    private WeaponScript _weapon;
    private Coroutine _cast;
    private ProtocolExecution _execution;
    private bool _bond, _judgment, _garden, _seal, _return, _eclipse, _detonator;
    private bool _locked, _wasStopped, _wasRotating, _slowed;
    private float _agentBaseSpeed;
    private float _elapsed, _clock, _held, _recovery, _nextMineScan;
    private long _epoch;
    private int _slot;
    private readonly Dictionary<Actor, Mark> _seals = new Dictionary<Actor, Mark>();
    private readonly List<Actor> _expired = new List<Actor>(64);
    private Mark _priority;
    private readonly List<Mine> _mines = new List<Mine>(3);
    private Collider[] _overlap = new Collider[128];
    private readonly List<Actor> _targets = new List<Actor>(128);
    private readonly List<Vector3> _minePositions = new List<Vector3>(3);
    private BowBoonPresentation _presentation;
    private sealed class Mark { public Actor Target; public long Generation; public float Until; }
    private sealed class Mine : IDisposable
    {
        public Vector3 Position; public float At, Until, Damage; public ProtocolExecution Execution;
        public void Dispose() { Execution?.Dispose(); Execution = null; }
    }
    public bool IsExecuting { get; private set; }
    public int ActiveSlot => IsExecuting ? _slot : -1;
    public bool IsCharging { get; private set; }
    public bool AllowsMovementWhileFiring => IsExecuting && _slot != 3;
    public float ExecutionDuration { get; private set; }
    public float ChargeRatio => IsCharging ? Mathf.Clamp01(_held / BowBoonModel.FullChargeSeconds) : 0;
    public int ActiveMines => _mines.Count;
    public Actor PriorityTarget => Valid(_priority) ? _priority.Target : null;
    public int MinesDetonated { get; private set; }
    private float Delta => !_player || _player.IsCombatHitStopped || _holder && _holder.BlocksWorldInput ? 0 : _protocols ? _protocols.ScaledDelta : Time.deltaTime;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>(); _holder = GetComponent<AbilityHolder>();
        _agent = GetComponent<NavMeshAgent>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        _player.Died += OnDeath; _player.DamageReceived += OnDamage;
    }
    public void Configure(bool huntingBond, bool judgmentArrow, bool mineGarden, bool predatorSeal = false,
        bool cuttingReturn = false, bool eclipseArcher = false, bool huntingDetonator = false)
    {
        if (_weapon != _player.CurrentWeapon) StopEncounter();
        _weapon = _player.CurrentWeapon; _bond = huntingBond; _judgment = judgmentArrow; _garden = mineGarden;
        _seal = predatorSeal; _return = cuttingReturn; _eclipse = eclipseArcher; _detonator = huntingDetonator;
        _player.TryGetComponent(out _protocols); _player.TryGetComponent(out _run);
        int colliderLimit = _protocols ? _protocols.Inventory.Catalog.Limits.CollidersPerQuery : 128;
        if (_overlap.Length != colliderLimit) _overlap = new Collider[colliderLimit];
        if (!_bond) _priority = null;
        if (!_seal) _seals.Clear();
        if (!_garden) ClearMines();
    }
    public bool Handles(ArsenalAbility ability)
    {
        if (!ability || !_player || !_player.CurrentWeapon || !_player.CurrentWeapon.FiresArrows) return false;
        int slot = Array.IndexOf(_player.CurrentWeapon.abilities, ability);
        return slot == 0 && _bond || slot == 2 && _garden || slot == 3 && _judgment;
    }
    public bool CanUse(ArsenalAbility ability) => isActiveAndEnabled && Handles(ability) && !IsExecuting &&
        !_player.IsDead && (!_holder || !_holder.BlocksWorldInput) && !_player.IsCombatHitStopped &&
        ReactionNumeric.IsFinite(ability.Windup) && ability.Windup >= 0 && ReactionNumeric.IsFinite(ability.Interval) && ability.Interval > 0 &&
        ReactionNumeric.IsFinite(ability.Range) && ability.Range > 0 && ReactionNumeric.IsFinite(ability.Width) && ability.Width > 0 &&
        ReactionNumeric.IsFinite(ability.DamageMultiplier) && ability.DamageMultiplier >= 0 && ability.Hits > 0;
    public void Use(ArsenalAbility ability)
    {
        if (!CanUse(ability)) return;
        _weapon = _player.CurrentWeapon; _slot = Array.IndexOf(_weapon.abilities, ability);
        _cast = StartCoroutine(Execute(ability));
    }
    public bool TryGetCancelContext(out CombatActionProfile profile, out float progress)
    {
        profile = null; progress = 0;
        if (!IsExecuting) return false;
        profile = _slot == 3 ? CombatActionProfile.BowBoonChannel() : new CombatActionProfile();
        progress = _slot == 3 ? IsCharging ? Mathf.Min(.79f, .79f * _held / BowBoonModel.MaximumHoldSeconds) :
            .8f + .2f * Mathf.Clamp01(_recovery / .2f) : Mathf.Clamp01(_elapsed / Mathf.Max(.01f, ExecutionDuration));
        return true;
    }
    public bool TryGetCommitment(out CommitmentCategory commitment)
    { commitment = _slot == 3 ? CommitmentCategory.Channel : CommitmentCategory.Committed; return IsExecuting; }
    public bool TryGetActionProgress(out ActionPhase phase, out float progress)
    {
        phase = ActionPhase.Startup; progress = 0;
        if (!TryGetCancelContext(out var profile, out float global)) return false;
        var timeline = profile.BuildTimeline(); phase = timeline.PhaseOf(global); progress = timeline.LocalProgressOf(global); return true;
    }
    private IEnumerator Execute(ArsenalAbility ability)
    {
        var plan = _player.RunModifiers?.Plan(ability, _slot) ?? new ArsenalCastPlan(ability);
        IsExecuting = true; _elapsed = _held = _recovery = 0;
        _execution = _protocols ? _protocols.BeginExecution() : null;
        ExecutionDuration = _slot == 3 ? BowBoonModel.MaximumHoldSeconds + .2f : Mathf.Max(.2f, plan.Windup + plan.Hits * plan.Interval);
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Lock();
        try
        {
            if (_slot == 3)
            {
                IsCharging = true;
                do
                {
                    yield return null;
                    if (!Live()) yield break;
                    float delta = Delta;
                    if (delta <= 0) continue;
                    _held += delta; _elapsed += delta;
                    SequencedAreaAttackAbility.FaceMousePosition(transform);
                    _animation.Strike(SkillMotion.BowShot, _held, 0, BowBoonModel.FullChargeSeconds, BowBoonModel.MaximumHoldSeconds);
                } while (Delta <= 0 || _held < BowBoonModel.MaximumHoldSeconds && GamePreferences.IsHeld(_holder ? _holder.GetAbilityKey(3) : GamePreferences.Binding(GameControl.Skill4)));
                IsCharging = false;
                bool charged = BowBoonModel.IsCharged(_held);
                bool focused = charged && _eclipse && Valid(_priority) &&
                    BowBoonModel.WithinCone(transform.position + Vector3.up, transform.forward, _priority.Target.transform.position + Vector3.up, plan.Range) &&
                    HasSight(transform.position + Vector3.up, _priority.Target.transform.position + Vector3.up);
                var shot = new BowBoonShot(this, _epoch, charged: charged, cuttingReturn: charged && _return,
                    concentrated: focused, radius: charged && !focused ? BowBoonModel.ChargedRadius : BowBoonModel.ShortRadius);
                using (_protocols ? _protocols.OpenExecutionScope(_execution) : null)
                    ArsenalProjectile.Fire(_player, transform.position + Vector3.up, transform.forward, _weapon.attackDamage,
                        plan.Damage * plan.Hits * BowBoonModel.JudgmentScale(_held), plan.Range, charged, ability.AccentColor, boonShot: shot);
                _animation.Contact(SkillMotion.BowShot);
                while (_recovery < .2f) { yield return null; if (!Live()) yield break; _recovery += Delta; }
            }
            else if (_slot == 0)
            {
                for (int i = 0; i < plan.Hits; i++)
                {
                    float at = plan.Windup + i * plan.Interval;
                    while (_elapsed < at) { yield return null; if (!Live()) yield break; _elapsed += Delta; }
                    if (!Live()) yield break;
                    SequencedAreaAttackAbility.FaceMousePosition(transform);
                    using (_protocols ? _protocols.OpenExecutionScope(_execution) : null)
                        ArsenalProjectile.Fire(_player, transform.position + Vector3.up, transform.forward, _weapon.attackDamage,
                            plan.Damage, plan.Range, ability.Piercing, ability.AccentColor,
                            boonShot: new BowBoonShot(this, _epoch, bond: true));
                    _animation.Contact(SkillMotion.BowShot);
                }
                while (_elapsed < ExecutionDuration) { yield return null; if (!Live()) yield break; _elapsed += Delta; }
            }
            else
            {
                while (_elapsed < plan.Windup) { yield return null; if (!Live()) yield break; _elapsed += Delta; }
                if (!Live()) yield break;
                Plant(plan);
                while (_elapsed < ExecutionDuration) { yield return null; if (!Live()) yield break; _elapsed += Delta; }
            }
        }
        finally { Release(); }
    }
    private bool Live() => _player && !_player.IsDead && isActiveAndEnabled && (!_holder || _holder.isActiveAndEnabled) &&
        _weapon && _player.CurrentWeapon == _weapon && (!_protocols || _execution != null && _execution.IsLifecycleCurrent);
    private bool Valid(Mark mark) => mark != null && mark.Target && !mark.Target.IsDead && mark.Target.isActiveAndEnabled &&
        mark.Target.CombatGeneration == mark.Generation && mark.Until >= _clock && Eligible(mark.Target);
    private bool Eligible(Actor actor) => actor && actor.Affiliation == CombatAffiliation.Enemy &&
        (!_run || _run.ProtocolSession == null || actor.TryGetComponent(out ProtocolEncounterMembership member) &&
            member.Matches(_run.ProtocolSession.Id, _protocols.EncounterId));
    internal bool IsShotCurrent(long epoch) => this && epoch == _epoch && isActiveAndEnabled && _player && !_player.IsDead &&
        _weapon && _player.CurrentWeapon == _weapon && (!_holder || _holder.isActiveAndEnabled) && (!_protocols || _protocols.IsOpen);
    internal BowBoonShot NativeShot() => _seal && IsShotCurrent(_epoch) ? new BowBoonShot(this, _epoch) : null;
    internal bool HasConsumableMark(Actor target) => target &&
        (Valid(_priority) && _priority.Target == target || _seals.TryGetValue(target, out var seal) && Valid(seal));
    internal void ConfirmArrow(Actor target, long generation, bool bond, bool charged)
    {
        long epoch = _epoch;
        if (!IsShotCurrent(epoch) || !target || target.IsDead || !target.isActiveAndEnabled || target.CombatGeneration != generation || !Eligible(target)) return;
        bool hadSeal = _seals.TryGetValue(target, out var prior) && Valid(prior);
        if (charged)
        {
            if (Valid(_priority) && _priority.Target == target) _priority = null;
            _seals.Remove(target);
        }
        if (hadSeal && _detonator)
        {
            bool detonated = TryDetonateNearby(target.transform.position, 4f);
            // Mine damage may synchronously close the encounter, kill the owner, or pool the victim.
            if (!IsShotCurrent(epoch) || !target || target.IsDead || !target.isActiveAndEnabled || target.CombatGeneration != generation) return;
            if (detonated) { _seals.Remove(target); return; }
        }
        if (bond && _bond) _priority = new Mark { Target = target, Generation = generation, Until = _clock + BowBoonModel.BondSeconds };
        if (_seal && !charged && !hadSeal && _seals.Count < 64)
            _seals[target] = new Mark { Target = target, Generation = generation, Until = _clock + BowBoonModel.SealSeconds };
    }
    public static Vector3 ResolvePriority(PlayerActor owner, Vector3 origin, Vector3 forward, float range)
    {
        if (!owner || !owner.TryGetComponent(out BowBoonCombat combat) || !combat.IsShotCurrent(combat._epoch) || !combat.Valid(combat._priority)) return forward;
        Actor target = combat._priority.Target; Vector3 point = target.transform.position + Vector3.up;
        return BowBoonModel.WithinCone(origin, forward, point, range) && combat.HasSight(origin, point, target) ? (point - origin).normalized : forward;
    }
    private bool HasSight(Vector3 from, Vector3 to, Actor target = null)
    {
        Vector3 delta = to - from; if (delta.sqrMagnitude < .0001f) return true;
        // Ignore characters; a priority may cross another enemy, but never solid scenery.
        foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.collider.GetComponentInParent<Actor>()) return false;
        return true;
    }
    private Vector3 Placement(ArsenalCastPlan plan)
    {
        Vector3 origin = transform.position; Vector3 point = origin + transform.forward * Mathf.Min(5f, plan.Range);
        if (Camera.main && UnityEngine.InputSystem.Mouse.current != null)
        {
            var ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            if (new Plane(Vector3.up, origin).Raycast(ray, out float t)) point = origin + Vector3.ClampMagnitude(ray.GetPoint(t) - origin, plan.Range);
        }
        Vector3 delta = point - origin;
        foreach (var hit in Physics.RaycastAll(origin + Vector3.up * .5f, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.collider.GetComponentInParent<Actor>() && hit.distance < delta.magnitude)
            { delta = delta.normalized * Mathf.Max(0, hit.distance - .5f); }
        return origin + delta;
    }
    private void Plant(ArsenalCastPlan plan)
    {
        ClearMines(); Vector3 center = Placement(plan); Vector3 side = Vector3.Cross(Vector3.up, transform.forward);
        for (int i = 0; i < BowBoonModel.MaximumMines; i++)
        {
            Vector3 position = center + side * ((i - 1) * 1.6f);
            if (!HasSight(transform.position + Vector3.up * .5f, position + Vector3.up * .5f)) continue;
            ProtocolExecution child;
            using (_protocols ? _protocols.OpenExecutionScope(_execution) : null)
                child = _protocols ? _protocols.ReserveExecution(ProtocolProducer.Technique) : null;
            if (_protocols && (child == null || !child.IsValid)) { child?.Dispose(); continue; }
            _mines.Add(new Mine { Position = position, At = _clock + BowBoonModel.MineArmSeconds,
                Until = _clock + BowBoonModel.MineSeconds, Damage = _player.RollAttackDamage(_weapon.attackDamage,
                    plan.Damage * plan.Hits / BowBoonModel.MaximumMines).Amount, Execution = child });
        }
    }
    private void Update()
    {
        if (_weapon && (_player.CurrentWeapon != _weapon || _player.IsDead || _holder && !_holder.isActiveAndEnabled || _protocols && !_protocols.IsOpen))
        { StopEncounter(); _weapon = null; return; }
        _clock += Delta;
        if (!Valid(_priority)) _priority = null;
        _expired.Clear(); foreach (var pair in _seals) if (!Valid(pair.Value)) _expired.Add(pair.Key);
        foreach (var actor in _expired) _seals.Remove(actor);
        for (int i = _mines.Count - 1; i >= 0; i--)
            if (_mines[i].Until < _clock || _protocols && (_mines[i].Execution == null || !_mines[i].Execution.IsValid)) RemoveMine(i);
        if (Delta > 0 && _clock >= _nextMineScan)
        {
            _nextMineScan = _clock + .1f;
            for (int i = _mines.Count - 1; i >= 0; i--)
            {
                if (i >= _mines.Count) continue;
                var mine = _mines[i]; if (_clock < mine.At) continue;
                _targets.Clear();
                if (!TryReserveMineQuery()) break;
                int count = Physics.OverlapSphereNonAlloc(mine.Position, BowBoonModel.MineRadius, _overlap, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                for (int c = 0; c < count; c++)
                {
                    var collider = _overlap[c]; _overlap[c] = null;
                    var actor = collider ? collider.GetComponentInParent<Actor>() : null;
                    if (!actor || actor.IsDead || !actor.isActiveAndEnabled || !Eligible(actor) || _targets.Contains(actor)) continue;
                    _targets.Add(actor);
                }
                _targets.Sort((a,b) => EntityId.ToULong(a.GetEntityId()).CompareTo(EntityId.ToULong(b.GetEntityId())));
                int sightTests = 0;
                int sightLimit = _protocols ? _protocols.Inventory.Catalog.Limits.SightTestsPerQuery : _overlap.Length;
                foreach (var actor in _targets)
                {
                    bool marked = _seals.TryGetValue(actor, out var seal) && Valid(seal);
                    float radius = marked ? 2.5f : 1.25f;
                    if ((actor.transform.position - mine.Position).sqrMagnitude > radius * radius) continue;
                    if (sightTests++ >= sightLimit) break;
                    if (!HasSight(mine.Position + Vector3.up * .5f, actor.transform.position + Vector3.up * .5f)) continue;
                    if (marked) _seals.Remove(actor);
                    Detonate(i); break;
                }
            }
        }
        if ((_bond || _judgment || _garden) && _weapon)
        {
            _presentation ??= new BowBoonPresentation(transform);
            _minePositions.Clear(); foreach (var mine in _mines) _minePositions.Add(mine.Position);
            _presentation.Draw(IsCharging, ChargeRatio, transform.position, transform.forward, PriorityTarget, _minePositions);
        }
    }
    private bool TryReserveMineQuery() => Delta > 0 && (!_protocols || _protocols.IsOpen && _protocols.Budget.ReserveQuery());
    private bool TryDetonateNearby(Vector3 point, float range)
    {
        if (_mines.Count == 0 || !TryReserveMineQuery()) return false;
        int best = -1, sightTests = 0; float distance = range * range;
        int sightLimit = _protocols ? _protocols.Inventory.Catalog.Limits.SightTestsPerQuery : _overlap.Length;
        for (int i = 0; i < _mines.Count; i++)
        {
            var mine = _mines[i]; float sqr = (mine.Position - point).sqrMagnitude;
            if (_clock < mine.At || sqr > distance) continue;
            if (sightTests++ >= sightLimit) break;
            if (!HasSight(mine.Position + Vector3.up * .5f, point + Vector3.up * .5f)) continue;
            best = i; distance = sqr;
        }
        if (best < 0) return false;
        Detonate(best); return true;
    }
    private void Detonate(int index)
    {
        var mine = _mines[index]; _mines.RemoveAt(index); // consume before any damage callback
        using (mine)
        {
            if (_protocols && mine.Execution != null && mine.Execution.IsValid)
                _protocols.EmitBoonArea(mine.Execution, mine.Position, BowBoonModel.MineRadius, mine.Damage);
            MinesDetonated++;
        }
    }
    private void RemoveMine(int index) { var mine = _mines[index]; _mines.RemoveAt(index); mine.Dispose(); }
    private void ClearMines() { foreach (var mine in _mines) mine.Dispose(); _mines.Clear(); }
    private void Lock()
    {
        _locked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (!_locked) return;
        _wasStopped = _agent.isStopped; _wasRotating = _agent.updateRotation;
        if (_slot == 3)
        { _agent.isStopped = true; _agent.updateRotation = false; }
        else
        {
            _agentBaseSpeed = _agent.speed;
            var arsenal = GetComponent<ArsenalCombat>();
            _agent.speed = arsenal ? arsenal.BowFiringSpeed(_agentBaseSpeed) :
                new BowFireMovement(BowFireMovementConfig.Default).ReducedSpeed(_agentBaseSpeed);
            _slowed = true; _agent.isStopped = false;
        }
    }
    private void Release()
    {
        _execution?.Dispose(); _execution = null;
        if (_slowed && _agent) _agent.speed = _agentBaseSpeed;
        _slowed = false;
        if (_locked && _agent && _agent.enabled && _agent.isOnNavMesh)
        { _agent.isStopped = _wasStopped; _agent.updateRotation = _wasRotating; }
        _locked = false; IsExecuting = IsCharging = false; _animation?.Release(); _cast = null;
    }
    public void Cancel() { if (_cast != null) StopCoroutine(_cast); Release(); }
    public void StopEncounter() { Cancel(); _epoch++; _priority = null; _seals.Clear(); ClearMines(); _presentation?.Hide(); }
    private void OnDeath(Actor actor) => StopEncounter();
    private void OnDamage(Actor actor, float damage) { if (damage > 0 && IsCharging) Cancel(); }
    private void OnDisable() => StopEncounter();
    private void OnDestroy()
    { if (_player) { _player.Died -= OnDeath; _player.DamageReceived -= OnDamage; } _presentation?.Dispose(); }
}
