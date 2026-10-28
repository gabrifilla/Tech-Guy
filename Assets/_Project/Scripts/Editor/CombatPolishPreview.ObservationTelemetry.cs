using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Observation belongs to the existing Editor preview tool. It never raises combat events,
// advances combat clocks, edits profiles, or applies damage.
public static partial class CombatPolishPreview
{
    private static ObservationTelemetry _observationTelemetry;
    private static string _observationTelemetryLastFailure;
    private static string ObservationTelemetryFailure => _observationTelemetry?.Failure ?? _observationTelemetryLastFailure;

    private static void BeginObservationTelemetry(PlayerActor player, ProgressionDirector director, string outputDirectory, int startFrame)
    {
        EndObservationTelemetry();
        _observationTelemetryLastFailure = null;
        try
        {
            _observationTelemetry = new ObservationTelemetry(player, director, outputDirectory, startFrame);
            _observationTelemetry.Begin();
            EditorApplication.playModeStateChanged += ObservationTelemetryPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += EndObservationTelemetry;
            EditorApplication.quitting += EndObservationTelemetry;
        }
        catch
        {
            EndObservationTelemetry();
            throw;
        }
    }

    private static void SampleObservationTelemetry(int relativeFrame)
    {
        _observationTelemetry?.Sample(relativeFrame);
    }

    private static void RecordObservationEvent(string kind, Object source, string detail)
    {
        _observationTelemetry?.Write(kind, source, detail, "callback");
    }

    private static void EndObservationTelemetry()
    {
        EditorApplication.playModeStateChanged -= ObservationTelemetryPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= EndObservationTelemetry;
        EditorApplication.quitting -= EndObservationTelemetry;
        var telemetry = _observationTelemetry;
        _observationTelemetry = null;
        if (telemetry == null) return;
        telemetry.Dispose();
        _observationTelemetryLastFailure = telemetry.Failure;
    }

    private static void ObservationTelemetryPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            EndObservationTelemetry();
    }

    private sealed class ObservationTelemetry : IDisposable
    {
        private static readonly System.Reflection.FieldInfo EnemyRoutine = typeof(EnemyAI).GetField("_attackRoutine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.FieldInfo EnemyNextAttack = typeof(EnemyAI).GetField("nextAttackTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private const System.Reflection.BindingFlags PrivateInstance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        private static readonly System.Reflection.FieldInfo WarningExecutor = typeof(EnemyCombatActions).GetField("_warning", PrivateInstance);
        private static readonly System.Reflection.FieldInfo SniperExecution = typeof(EnemyCombatActions).GetField("_reactionExecution", PrivateInstance);
        private static readonly System.Reflection.FieldInfo WarningOutlines = typeof(EnemyAttackExecution).GetField("_outlines", PrivateInstance);
        private static readonly System.Reflection.FieldInfo ProjectileOwner = typeof(EnemyProjectile).GetField("_owner", PrivateInstance);
        private static readonly System.Reflection.FieldInfo ProjectileDirection = typeof(EnemyProjectile).GetField("_direction", PrivateInstance);
        private static readonly System.Reflection.FieldInfo ProjectileSpeed = typeof(EnemyProjectile).GetField("_speed", PrivateInstance);
        private static readonly System.Reflection.FieldInfo ProjectileRadius = typeof(EnemyProjectile).GetField("_radius", PrivateInstance);
        private readonly HashSet<string> _observedSniperFlights = new HashSet<string>();
        private readonly PlayerActor _owner;
        private readonly ProgressionDirector _director;
        private readonly StreamWriter _writer;
        private readonly int _startFrame;
        private readonly double _startTime, _startUnscaled, _startRealtime;
        private readonly Dictionary<ulong, ObservedActor> _actors = new Dictionary<ulong, ObservedActor>();
        private readonly Dictionary<HookBus, Action> _hookRemovals = new Dictionary<HookBus, Action>();
        private AbilityHolder _holder;
        private CharControlScript _controls;
        private GauntletEnergyPolicy _energy;
        private int _enemyOrdinal, _otherOrdinal, _lastSampleFrame = -1;
        private long _sequence;
        private bool _disposed;
        private string _lastSkillState, _lastRunState;
        private readonly Dictionary<GameControl, bool> _observedHeld = new Dictionary<GameControl, bool>();
        public string Failure { get; private set; }

        public ObservationTelemetry(PlayerActor player, ProgressionDirector director, string directory, int startFrame)
        {
            _owner = player;
            _director = director;
            _startFrame = startFrame;
            _startTime = Time.timeAsDouble;
            _startUnscaled = Time.unscaledTimeAsDouble;
            _startRealtime = Time.realtimeSinceStartupAsDouble;
            Directory.CreateDirectory(directory);
            // Each observation owns a fresh directory. Never silently replace baseline evidence.
            _writer = new StreamWriter(new FileStream(Path.Combine(directory, "events.jsonl"), FileMode.CreateNew,
                FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
        }

        public void Begin()
        {
            Write("ObservationCoverage", _owner,
                "ActorDamageReceived is the factual HP-loss channel; hook/basic/protocol notifications overlap and must not be added to it. " +
                "Callbacks are timestamped when published; BasicAttackPerformed reports accepted setup. Authored Gauntlet basic damage is scheduled later; legacy Bow/Spear emit during setup. " +
                "Snapshot/transition timing is bounded by sampling cadence; no inferred cancel reason, exact stagger duration, or unobserved phase is asserted. " +
                "ProtocolImpact excludes Environment and has no skill slot; EnergyCommitted only publishes committed gain/reserve parcels, not every blocked attempt, and is not a damage receipt. " +
                "Hook listeners are reattached at each sample after possible HookBus.Clear; events between Clear and the next sample may be missing. " +
                "New actors/components are discovered at sampling boundaries; earlier callbacks are not observable. " +
                "Aliases follow initial hierarchy order and include combat generation; instance IDs are local to this execution. " +
                "Enemy AI RNG is seeded independently from entity ID; route seed alone does not reproduce AI timing. " +
                "Animator normalized time describes the sampled layer-0 state, not an authored impact. " +
                "Sampling/PNG readback overhead can affect real-time hitstop and AI timing; no bitwise determinism is claimed.", "snapshot");
            Write("ObservationInputCoverage", _owner,
                "InputEdgeObserved passively samples rebindable gameplay getters at end of frame; it does not prove command acceptance, physical hardware, or an exact intra-frame order. " +
                "Correlate absolute_frame with AbilityUsed/AbilityRejected/BasicAttackPerformed and HP receipts. Held input at recording start may predate the recording. " +
                "NavigationSnapshot is actual agent state, not a movement-order acceptance or cancellation receipt. Missing rejection for a basic does not prove a lost command.", "snapshot");
            RefreshBindings();
            CheckFailure();
        }

        public void Sample(int relativeFrame)
        {
            if (_disposed) return;
            CheckFailure();
            if (relativeFrame < 0 || relativeFrame <= _lastSampleFrame)
                throw new ArgumentOutOfRangeException(nameof(relativeFrame), "Observation samples require increasing relative frames.");
            _lastSampleFrame = relativeFrame;
            RefreshBindings();
            SampleInputAndNavigation(relativeFrame);
            foreach (var binding in _actors.Values.ToArray())
            {
                if (!binding.Actor) continue;
                SampleActor(binding, relativeFrame);
            }
            SamplePlayer(relativeFrame);
            SampleSniperProjectiles(relativeFrame);
            SampleRun(relativeFrame);
            _writer.Flush();
            CheckFailure();
        }

        // Passive end-of-frame observation of the same rebindable getters used by gameplay.
        // This neither consumes input nor asserts that the command was accepted. Callbacks may
        // precede this sample within the frame; correlate absolute_frame, not JSONL line order.
        private void SampleInputAndNavigation(int frame)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            foreach (GameControl control in Enum.GetValues(typeof(GameControl)))
            {
                bool pressed = GamePreferences.WasPressed(control), held = GamePreferences.IsHeld(control);
                bool previous; _observedHeld.TryGetValue(control, out previous);
                if (pressed || held != previous)
                    Write("InputEdgeObserved", _owner, JsonUtility.ToJson(new InputDetail {
                        control = control.ToString(), binding = GamePreferences.Binding(control).ToString(),
                        pressed = pressed, held = held,
                        shift = GamePreferences.IsHeld(KeyCode.LeftShift) || GamePreferences.IsHeld(KeyCode.RightShift),
                        pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero,
                        keyboard_device = keyboard != null ? keyboard.name : "none",
                        mouse_device = mouse != null ? mouse.name : "none",
                        app_focused = Application.isFocused,
                        world_input_blocked = _holder && _holder.BlocksWorldInput
                    }), "sample_transition", frame: frame);
                _observedHeld[control] = held;
            }
            if (!_owner) return;
            var agent = _owner.GetComponent<NavMeshAgent>();
            bool onMesh = agent && agent.enabled && agent.isOnNavMesh;
            Write("NavigationSnapshot", _owner, JsonUtility.ToJson(new NavigationDetail {
                on_mesh = onMesh, has_path = onMesh && agent.hasPath,
                path_pending = onMesh && agent.pathPending,
                distance_known = onMesh && !float.IsInfinity(agent.remainingDistance) && !float.IsNaN(agent.remainingDistance),
                destination = onMesh ? agent.destination : _owner.transform.position,
                velocity = onMesh ? agent.velocity : Vector3.zero,
                remaining_distance = onMesh ? Finite(agent.remainingDistance) : 0
            }), "snapshot", frame: frame);
        }

        [Serializable] private sealed class InputDetail
        {
            public string control, binding, keyboard_device, mouse_device;
            public bool pressed, held, shift, app_focused, world_input_blocked;
            public Vector2 pointer;
        }
        [Serializable] private sealed class NavigationDetail
        {
            public bool on_mesh, has_path, path_pending, distance_known;
            public Vector3 destination, velocity;
            public float remaining_distance;
        }

        private void RefreshBindings()
        {
            var actors = Object.FindObjectsByType<Actor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderBy(actor => HierarchyKey(actor.transform), StringComparer.Ordinal);
            foreach (var actor in actors)
            {
                ulong id = EntityId.ToULong(actor.GetEntityId());
                if (!_actors.TryGetValue(id, out var binding) || !ReferenceEquals(binding.Actor, actor))
                {
                    binding?.Dispose();
                    string alias = actor == _owner ? "player" : actor.Affiliation == CombatAffiliation.Enemy
                        ? "enemy-" + (++_enemyOrdinal).ToString("D3") : "actor-" + (++_otherOrdinal).ToString("D3");
                    binding = new ObservedActor(this, actor, alias);
                    _actors[id] = binding;
                    Write("ActorObserved", actor, "name=" + actor.name + "; hierarchy=" + HierarchyKey(actor.transform), "snapshot");
                }
                binding.RefreshReaction();
                if (binding.Generation != actor.CombatGeneration)
                {
                    binding.Generation = actor.CombatGeneration;
                    binding.SniperAimOrdinal = 0; binding.SniperAimExecution = 0;
                    binding.LastReactionState = binding.LastEnemyState = null;
                    Write("ActorGenerationObserved", actor, "Pooling generation changed; identity is alias plus generation.", "sample_transition");
                }
            }
            if (!_owner) return;
            var holder = _owner.GetComponent<AbilityHolder>();
            if (holder != _holder)
            {
                if (_holder) { _holder.AbilityUsed -= AbilityUsed; _holder.AbilityRejected -= AbilityRejected; }
                _holder = holder;
                if (_holder) { _holder.AbilityUsed += AbilityUsed; _holder.AbilityRejected += AbilityRejected; }
            }
            var controls = _owner.GetComponent<CharControlScript>();
            if (controls != _controls)
            {
                if (_controls) _controls.BasicAttackPerformed -= BasicPerformed;
                _controls = controls;
                if (_controls) _controls.BasicAttackPerformed += BasicPerformed;
            }
            var combat = _owner.GetComponent<BreakerGauntletCombat>();
            var energy = combat ? combat.EnergyPolicy : null;
            if (!ReferenceEquals(energy, _energy))
            {
                if (_energy != null) _energy.Committed -= EnergyCommitted;
                _energy = energy;
                if (_energy != null) _energy.Committed += EnergyCommitted;
            }
            RefreshHooks(_owner.Hooks);
        }

        private void RefreshHooks(HookBus hooks)
        {
            foreach (var removal in _hookRemovals.Values) removal();
            _hookRemovals.Clear();
            if (hooks == null) return;
            // -=/+= also repairs our subscription if the SAME bus was cleared between samples.
            Action<Actor, float> hit = (target, amount) => Write("HookHit", target, "Overlaps ActorDamageReceived and possibly HookBasicHit.", "callback", amount);
            Action<Actor, float> basic = (target, amount) => Write("HookBasicHit", target, "Basic subtype of HookHit, not additional HP loss.", "callback", amount);
            Action<Actor, float> crit = (target, amount) => Write("HookCrit", target, "Critical subtype; do not add to damage totals.", "callback", amount);
            Action<Actor> kill = target => Write("HookKill", target, "Bus dedupes enemy kills per run.", "callback");
            Action<Actor> basicKill = target => Write("HookBasicKill", target, "Basic subtype of HookKill.", "callback");
            Action<Actor> freeze = target => Write("HookFreeze", target, "", "callback");
            Action<Actor> burn = target => Write("HookBurn", target, "Applied or refreshed; not tick damage.", "callback");
            Action<Actor> broken = target => Write("HookStanceBreak", target, "May overlap StanceBroken callback.", "callback");
            Action dash = () => Write("HookDash", _owner, "", "callback");
            Action<Vector3> explosion = point => Write("HookExplosion", _owner, JsonUtility.ToJson(new PointDetail { position = point }), "callback");
            Action<ProtocolCombatEvent> protocol = impact => Write("ProtocolImpact", FindActor(impact.Target), JsonUtility.ToJson(new ProtocolDetail
            {
                session_id = impact.SessionId.ToString(), root_id = impact.RootId, encounter_id = impact.EncounterId,
                target_id = impact.Target.TargetId.ToString(), generation = impact.Target.Generation,
                producer = impact.Producer.ToString(), depth = impact.Depth, position = impact.Position, is_death = impact.IsDeath
            }), "callback", impact.Damage);
            hooks.OnHit += hit; hooks.OnBasicHit += basic; hooks.OnCrit += crit;
            hooks.OnKill += kill; hooks.OnBasicKill += basicKill; hooks.OnFreeze += freeze;
            hooks.OnBurn += burn; hooks.OnStanceBreak += broken; hooks.OnDash += dash;
            hooks.OnExplosion += explosion; hooks.OnProtocolImpact += protocol;
            _hookRemovals.Add(hooks, () =>
            {
                hooks.OnHit -= hit; hooks.OnBasicHit -= basic; hooks.OnCrit -= crit;
                hooks.OnKill -= kill; hooks.OnBasicKill -= basicKill; hooks.OnFreeze -= freeze;
                hooks.OnBurn -= burn; hooks.OnStanceBreak -= broken; hooks.OnDash -= dash;
                hooks.OnExplosion -= explosion; hooks.OnProtocolImpact -= protocol;
            });
        }

        private Actor FindActor(ProtocolTargetHandle target)
        {
            foreach (var binding in _actors.Values)
                if (binding.Actor && EntityId.ToULong(binding.Actor.GetEntityId()) == target.TargetId &&
                    binding.Actor.CombatGeneration == target.Generation) return binding.Actor;
            return null; // The causal handle remains in detail even if a lethal target was already destroyed.
        }

        private void AbilityUsed(int slot) => Write("AbilityUsed", _owner, "slot=" + slot, "callback", slot);
        private void AbilityRejected(int slot, AbilityUseFailure failure) => Write("AbilityRejected", _owner,
            "slot=" + slot + "; reason=" + failure, "callback", slot);
        private void BasicPerformed() => Write("BasicAttackPerformed", _owner,
            "Accepted basic setup completed; authored Gauntlet contact is pending, not a damage receipt.", "callback");

        private void EnergyCommitted(GauntletEnergyGain gain)
        {
            var context = gain.Context;
            Write("EnergyCommitted", _owner, JsonUtility.ToJson(new EnergyDetail
            {
                before = gain.Before, after = gain.After, requested = gain.Requested, block = gain.Block.ToString(),
                basic = gain.Basic, skill = gain.Skill, alternation = gain.Alternation, stance_break = gain.StanceBreak,
                technique = gain.Technique, reserve = gain.Reserve,
                execution = context?.Execution ?? 0, root_execution = context?.RootExecution ?? 0,
                epoch = context?.Epoch ?? 0, slot = context?.Slot.ToString() ?? "unknown",
                origin = context?.Origin.ToString() ?? "unknown", producer = context?.ProducerId ?? "",
                impact_index = context?.ImpactIndex ?? -1, target_id = context?.TargetId.ToString() ?? "",
                generation = context?.TargetGeneration ?? -1, energy_eligible = context != null && context.EnergyEligible
            }), "callback", gain.Effective);
        }

        private void SampleActor(ObservedActor binding, int frame)
        {
            Actor actor = binding.Actor;
            var agent = actor.GetComponent<NavMeshAgent>();
            var animator = actor.GetComponentInChildren<Animator>();
            var snapshot = new ActorDetail
            {
                generation = actor.CombatGeneration, affiliation = actor.Affiliation.ToString(), alive = !actor.IsDead,
                enabled = actor.isActiveAndEnabled, position = actor.transform.position, rotation = actor.transform.rotation,
                health = Finite(actor.health), max_health = Finite(actor.maxHealth), has_nav = agent && agent.enabled && agent.isOnNavMesh,
                has_animator = animator && animator.isActiveAndEnabled && animator.runtimeAnimatorController && animator.layerCount > 0
            };
            if (snapshot.has_nav)
            {
                snapshot.velocity = agent.velocity; snapshot.nav_stopped = agent.isStopped; snapshot.nav_has_path = agent.hasPath;
                if (snapshot.nav_has_path) snapshot.nav_destination = agent.destination;
            }
            if (snapshot.has_animator)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                snapshot.animator_state = state.fullPathHash; snapshot.animator_time = Finite(state.normalizedTime);
                snapshot.animator_transition = animator.IsInTransition(0); snapshot.animator_speed = Finite(animator.speed);
            }
            Write("ActorSnapshot", actor, JsonUtility.ToJson(snapshot), "snapshot", frame: frame);
            if (binding.Reaction && actor.isActiveAndEnabled)
            {
                var reaction = binding.Reaction;
                var detail = new ReactionDetail
                {
                    rank = reaction.Rank.ToString(), category = reaction.Category.ToString(),
                    stance = Finite(reaction.CurrentStance), max_stance = Finite(reaction.MaxStance),
                    control_locked = reaction.IsControlLocked, stunned = reaction.IsStunned, airborne = reaction.IsAirborne,
                    vulnerable = reaction.IsVulnerable, resume_protected = reaction.IsResumeProtected,
                    launch_sequence = reaction.LaunchSequence, landed_sequence = reaction.LandedLaunchSequence,
                    break_sequence = reaction.ConfirmedBreakSequence, knockdown_phase = reaction.KnockdownPhase.ToString(),
                    gameplay_time = Finite(reaction.GameplayTime)
                };
                Write("ReactionSnapshot", actor, JsonUtility.ToJson(detail), "snapshot", frame: frame);
                string stateKey = detail.control_locked + "/" + detail.stunned + "/" + detail.airborne + "/" +
                    detail.vulnerable + "/" + detail.resume_protected + "/" + detail.launch_sequence + "/" +
                    detail.landed_sequence + "/" + detail.break_sequence + "/" + detail.knockdown_phase;
                if (binding.LastReactionState != stateKey)
                {
                    Write("ReactionStateChanged", actor, JsonUtility.ToJson(detail), "sample_transition", frame: frame);
                    binding.LastReactionState = stateKey;
                }
            }
            var ai = actor.GetComponent<EnemyAI>();
            var actions = actor.GetComponent<EnemyCombatActions>();
            if (ai || actions)
            {
                var detail = new EnemyDetail
                {
                    ai_enabled = ai && ai.isActiveAndEnabled, winding_up = ai && ai.IsWindingUp,
                    retreating = ai && ai.IsRetreating, retreat_phase = ai ? ai.RetreatPhase.ToString() : "unavailable",
                    actions_enabled = actions && actions.isActiveAndEnabled, has_actions = actions,
                    attack_kind = actions ? actions.CurrentKind.ToString() : "unavailable", completed = actions && actions.Completed,
                    resolved_impacts = actions ? actions.ResolvedImpacts : 0,
                    attack_center = actions ? actions.AttackCenter : Vector3.zero, attack_radius = actions ? Finite(actions.AttackRadius) : 0
                };
                if (ai && ai.player)
                {
                    Vector3 offset = ai.player.position - actor.transform.position;
                    detail.target_distance = offset.magnitude;
                    offset.y = 0;
                    detail.target_planar_distance = offset.magnitude;
                    detail.facing_dot = offset.sqrMagnitude > .001f ? Vector3.Dot(actor.transform.forward, offset.normalized) : 1;
                    detail.attack_eligible = ai.playerInAttackRange;
                    detail.attack_busy = EnemyRoutine != null && EnemyRoutine.GetValue(ai) != null;
                    detail.next_attack_time = EnemyNextAttack != null ? (float)EnemyNextAttack.GetValue(ai) : 0;
                    detail.melee_proximity = Mathf.Min(ai.attackRange, 1.45f * Mathf.Clamp(actor.transform.lossyScale.y, .5f, 2f) + .35f);
                    var positioning = actor.GetComponent<PreferredDistanceLayer>();
                    if (positioning) detail.has_preferred_target = positioning.TryGetPositioningTarget(out detail.preferred_target);
                    detail.root_motion = animator && animator.applyRootMotion;
                    if (snapshot.has_nav)
                    {
                        detail.nav_status = agent.pathStatus.ToString();
                        detail.nav_remaining = Finite(agent.remainingDistance);
                        detail.desired_velocity = agent.desiredVelocity;
                        detail.nav_next_position = agent.nextPosition;
                        detail.nav_radius = agent.radius;
                        detail.stopping_distance = agent.stoppingDistance;
                        detail.nav_update_rotation = agent.updateRotation;
                    }
                }
                Write("EnemySnapshot", actor, JsonUtility.ToJson(detail), "snapshot", frame: frame);
                if (actions && actions.CurrentKind == EnemyAttackKind.SniperShot && actions.IsWindingUp)
                    SampleSniperWarning(binding, actions, frame);
                string stateKey = detail.ai_enabled + "/" + detail.winding_up + "/" + detail.retreating + "/" +
                    detail.retreat_phase + "/" + detail.attack_kind + "/" + detail.completed + "/" + detail.resolved_impacts;
                if (binding.LastEnemyState != stateKey)
                {
                    Write("EnemyStateChanged", actor, JsonUtility.ToJson(detail), "sample_transition", frame: frame);
                    binding.LastEnemyState = stateKey;
                }
            }
        }

        // Read the rendered warning and configured flight; never issue combat callbacks or aim writes.
        private void SampleSniperWarning(ObservedActor binding, EnemyCombatActions actions, int frame)
        {
            var executor = (EnemyAttackExecution)WarningExecutor.GetValue(actions);
            var outlines = (List<CombatGroundRing>)WarningOutlines.GetValue(executor);
            if (outlines.Count == 0 || !outlines[0]) return;
            var line = outlines[0].GetComponent<LineRenderer>();
            if (!line) return;
            Vector3 direction = Vector3.zero; float longest = 0;
            for (int i = 1; i < line.positionCount; i++)
            {
                Vector3 segment = line.GetPosition(i) - line.GetPosition(i - 1);
                if (segment.sqrMagnitude > longest) { longest = segment.sqrMagnitude; direction = segment.normalized; }
            }
            if (Vector3.Dot(direction, actions.transform.forward) < 0) direction = -direction;
            int ordinal = actions.ResolvedImpacts + 1;
            long execution = (long)SniperExecution.GetValue(actions);
            var detail = new SniperAimDetail { execution_id = execution, emission_ordinal = ordinal, origin = actions.AttackCenter, direction = direction };
            if (binding.SniperAimExecution != execution)
            {
                binding.SniperAimExecution = execution;
                binding.SniperAimOrdinal = ordinal;
                binding.SniperWarningOrigin = detail.origin; binding.SniperWarningDirection = direction;
                Write("SniperWarningFirstObserved", binding.Actor, JsonUtility.ToJson(detail), "first_observed_frame", frame: frame);
            }
            Write("SniperWarningSnapshot", binding.Actor, JsonUtility.ToJson(detail), "rendered_geometry_snapshot", frame: frame);
        }

        private void SampleSniperProjectiles(int frame)
        {
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
            {
                var owner = (Actor)ProjectileOwner.GetValue(projectile);
                if (!owner || !_actors.TryGetValue(EntityId.ToULong(owner.GetEntityId()), out var binding)) continue;
                var actions = owner.GetComponent<EnemyCombatActions>();
                if (!actions || actions.CurrentKind != EnemyAttackKind.SniperShot) continue;
                Vector3 direction = (Vector3)ProjectileDirection.GetValue(projectile);
                var detail = new SniperFlightDetail {
                    projectile = EntityId.ToULong(projectile.GetEntityId()).ToString(), emission_ordinal = actions.ResolvedImpacts,
                    warning_observed = binding.SniperAimOrdinal == actions.ResolvedImpacts,
                    warning_origin = binding.SniperWarningOrigin, warning_direction = binding.SniperWarningDirection,
                    direction = direction, position = projectile.transform.position,
                    speed = (float)ProjectileSpeed.GetValue(projectile), radius = (float)ProjectileRadius.GetValue(projectile)
                };
                detail.angle = detail.warning_observed ? Vector3.Angle(detail.warning_direction, direction) : -1;
                string flight = EntityId.ToULong(owner.GetEntityId()) + "/" + owner.CombatGeneration + "/" + detail.projectile + "/" + detail.emission_ordinal;
                if (_observedSniperFlights.Add(flight))
                    Write("SniperProjectileFirstObserved", owner, JsonUtility.ToJson(detail), "first_observed_frame", frame: frame);
                Write("SniperProjectileSnapshot", owner, JsonUtility.ToJson(detail), "configured_flight_snapshot", frame: frame);
            }
        }

        [Serializable] private sealed class SniperAimDetail
        { public long execution_id; public int emission_ordinal; public Vector3 origin, direction; }
        [Serializable] private sealed class SniperFlightDetail
        {
            public string projectile; public int emission_ordinal; public bool warning_observed;
            public Vector3 warning_origin, warning_direction, direction, position; public float angle, speed, radius;
        }

        private void SamplePlayer(int frame)
        {
            if (!_owner) return;
            var combat = _owner.GetComponent<BreakerGauntletCombat>();
            var arsenal = _owner.GetComponent<ArsenalCombat>();
            var bow = _owner.GetComponent<BowBoonCombat>();
            var spear = _owner.GetComponent<SpearBoonCombat>();
            var hitstop = _owner.GetComponent<HitStopRunner>();
            var motion = _owner.GetComponent<SkillAnimationPlayer>();
            var detail = new PlayerDetail
            {
                basic_pending = _controls && _controls.HasScheduledBasic,
                basic_progress = _controls ? _controls.BasicImpactProgress : 0,
                basic_duration = _controls ? _controls.BasicImpactDuration : 0,
                basic_contact = _controls ? _controls.BasicImpactFraction : 0,
                motion_playing = motion && motion.IsPlaying, motion = motion ? motion.CurrentMotion.ToString() : "unavailable",
                motion_time = motion ? Finite(motion.NormalizedTime) : 0f,
                mana = Finite(_owner.mana), max_mana = Finite(_owner.maxMana),
                weapon = _owner.CurrentWeapon ? _owner.CurrentWeapon.name : "none", casting = _holder && _holder.IsCasting,
                movement_allowed_while_casting = _holder && _holder.MovementAllowedWhileCasting,
                world_input_blocked = _holder && _holder.BlocksWorldInput,
                asura_energy = combat ? combat.Energy : 0, asura_active = combat && combat.IsAsuraActive,
                cadenced_waiting = combat && combat.IsCadencedWaiting,
                hitstop_active = hitstop && hitstop.IsActive, hitstop_applied_count = hitstop ? hitstop.AppliedCount : 0,
                has_hitstop_runner = hitstop,
                cooldowns = _holder ? Enumerable.Range(0, _holder.ActiveAbilities.Count).Select(i => Finite(_holder.GetRemainingCooldown(i))).ToArray() : new float[0]
            };
            Write("PlayerSnapshot", _owner, JsonUtility.ToJson(detail), "snapshot", frame: frame);
            var skill = new SkillDetail { executor = "none", phase = "unavailable", ability = "", phase_observed = false };
            ActionPhase phase; float progress;
            if (combat && combat.IsExecuting)
            {
                skill.executor = nameof(BreakerGauntletCombat); skill.ability = combat.ActiveAbility ? combat.ActiveAbility.name : "";
                skill.elapsed = Finite(combat.Elapsed); skill.duration = Finite(combat.ExecutionDuration);
                skill.phase_observed = combat.TryGetActionProgress(out phase, out progress);
                if (skill.phase_observed) { skill.phase = phase.ToString(); skill.phase_progress = Finite(progress); }
            }
            else if (bow && bow.IsExecuting)
            {
                skill.executor = nameof(BowBoonCombat); skill.duration = Finite(bow.ExecutionDuration); skill.slot = bow.ActiveSlot;
                skill.phase_observed = bow.TryGetActionProgress(out phase, out progress);
                if (skill.phase_observed) { skill.phase = phase.ToString(); skill.phase_progress = Finite(progress); }
            }
            else if (spear && spear.IsExecuting)
            {
                skill.executor = nameof(SpearBoonCombat); skill.duration = Finite(spear.ExecutionDuration); skill.slot = spear.ActiveSlot;
                skill.phase_observed = spear.TryGetActionProgress(out phase, out progress);
                if (skill.phase_observed) { skill.phase = phase.ToString(); skill.phase_progress = Finite(progress); }
            }
            else if (arsenal && arsenal.IsExecuting)
            {
                skill.executor = nameof(ArsenalCombat); skill.duration = Finite(arsenal.ExecutionDuration);
                skill.phase_observed = arsenal.TryGetActionProgress(out phase, out progress);
                if (skill.phase_observed) { skill.phase = phase.ToString(); skill.phase_progress = Finite(progress); }
            }
            Write("SkillSnapshot", _owner, JsonUtility.ToJson(skill), "snapshot", frame: frame);
            string stateKey = skill.executor + "/" + skill.ability + "/" + skill.slot + "/" + skill.phase;
            if (_lastSkillState != stateKey)
            {
                Write("SkillStateChanged", _owner, JsonUtility.ToJson(skill), "sample_transition", frame: frame);
                _lastSkillState = stateKey;
            }
        }

        private void SampleRun(int frame)
        {
            if (!_director || _director.ManualSession == null) return;
            var session = _director.ManualSession;
            string state = "session=" + session.Id + "; node=" + session.CurrentNode + "; phase=" + session.Phase +
                "; reward_ordinal=" + session.RewardOrdinal + "; seed=" + _director.ActiveRunSeed;
            if (state != _lastRunState)
            {
                Write("RunStateObserved", _director, state, "sample_transition", frame: frame);
                _lastRunState = state;
            }
        }

        public void Write(string kind, Object source, string detail, string evidenceKind, float? value = null, int? frame = null)
        {
            if (_disposed || Failure != null) return;
            try
            {
                Actor actor = source as Actor;
                if (!actor && source is Component component) actor = component.GetComponentInParent<Actor>();
                string alias = "";
                if (actor && _actors.TryGetValue(EntityId.ToULong(actor.GetEntityId()), out var binding))
                    alias = binding.Alias + "@" + actor.CombatGeneration;
                var record = new TelemetryRecord
                {
                    seq = ++_sequence, frame = frame ?? Time.frameCount - _startFrame,
                    t = Time.timeAsDouble - _startTime, unscaled_t = Time.unscaledTimeAsDouble - _startUnscaled,
                    realtime_t = Time.realtimeSinceStartupAsDouble - _startRealtime, absolute_frame = Time.frameCount,
                    time_scale = Time.timeScale, kind = kind ?? "", actor = alias,
                    instance_id = source ? EntityId.ToULong(source.GetEntityId()) : 0, source = source ? source.GetType().Name : "none",
                    evidence_kind = evidenceKind, detail = detail ?? "", has_value = value.HasValue,
                    value = value.HasValue ? Finite(value.Value) : 0
                };
                _writer.WriteLine(JsonUtility.ToJson(record));
            }
            catch (Exception exception)
            {
                // A listener must never throw into damage/reaction execution. The next sample fails
                // the observation, so I/O failure cannot masquerade as a complete evidence run.
                Failure = exception.GetType().Name + ": " + exception.Message;
            }
        }

        private void CheckFailure()
        {
            if (Failure != null) throw new IOException("Gameplay observation telemetry failed: " + Failure);
        }

        public void Dispose()
        {
            if (_disposed) return;
            Write("ObservationTelemetryEnded", _owner, "Listeners are removed and the JSONL writer is closed.", "callback");
            _disposed = true;
            foreach (var binding in _actors.Values) binding.Dispose();
            _actors.Clear();
            if ((object)_holder != null) { _holder.AbilityUsed -= AbilityUsed; _holder.AbilityRejected -= AbilityRejected; }
            if ((object)_controls != null) _controls.BasicAttackPerformed -= BasicPerformed;
            if (_energy != null) _energy.Committed -= EnergyCommitted;
            foreach (var removal in _hookRemovals.Values) removal();
            _hookRemovals.Clear();
            try { _writer.Dispose(); }
            catch (Exception exception) { Failure = exception.GetType().Name + ": " + exception.Message; }
        }

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : value;
        private static string HierarchyKey(Transform transform)
        {
            string path = transform.name + "[" + transform.GetSiblingIndex() + "]";
            for (var parent = transform.parent; parent; parent = parent.parent)
                path = parent.name + "[" + parent.GetSiblingIndex() + "]/" + path;
            return transform.gameObject.scene.path + "/" + path;
        }

        private sealed class ObservedActor : IDisposable
        {
            private readonly ObservationTelemetry _observation;
            public readonly Actor Actor;
            public readonly string Alias;
            public int SniperAimOrdinal;
            public long SniperAimExecution;
            public Vector3 SniperWarningOrigin, SniperWarningDirection;
            public long Generation;
            public CombatReactionController Reaction;
            public string LastReactionState, LastEnemyState;

            public ObservedActor(ObservationTelemetry observation, Actor actor, string alias)
            {
                _observation = observation; Actor = actor; Alias = alias; Generation = actor.CombatGeneration;
                actor.DamageReceived += Damage; actor.Died += Death; actor.DamageAbsorbed += Absorbed;
            }
            public void RefreshReaction()
            {
                var next = Actor.GetComponent<CombatReactionController>();
                if (next == Reaction) return;
                if ((object)Reaction != null) { Reaction.StanceBroken -= Broken; Reaction.VulnerabilityChanged -= Vulnerability; }
                Reaction = next;
                if (Reaction) { Reaction.StanceBroken += Broken; Reaction.VulnerabilityChanged += Vulnerability; }
            }
            private void Damage(Actor actor, float amount)
            {
                _observation.Write("ActorDamageReceived", actor,
                    "Confirmed HP removed; factual total channel. Other hit/crit/basic/protocol callbacks overlap.", "callback", amount);
                var receipt = actor.PublishedDamageTransaction;
                var controls = _observation._controls;
                var motion = _observation._owner ? _observation._owner.GetComponent<SkillAnimationPlayer>() : null;
                _observation.Write("HpContactTiming",actor,JsonUtility.ToJson(new ContactDetail {
                    has_transaction=receipt.HasValue, basic_pending=controls&&controls.HasScheduledBasic,
                    basic_progress=controls?controls.BasicImpactProgress:0,
                    basic_duration=controls?controls.BasicImpactDuration:0,
                    basic_contact=controls?controls.BasicImpactFraction:0,
                    sequence=receipt.HasValue?receipt.Value.Sequence:0,
                    target=receipt.HasValue?receipt.Value.TargetId:"",generation=receipt.HasValue?receipt.Value.Generation:0,
                    before=receipt.HasValue?receipt.Value.Before:0,after=receipt.HasValue?receipt.Value.After:0,
                    ability=receipt.HasValue?receipt.Value.Source?.Ability:"",execution=receipt.HasValue?receipt.Value.Source?.Execution:"",
                    motion=motion?motion.CurrentMotion.ToString():"none",motion_time=motion?motion.NormalizedTime:0
                }),"callback"); // Metadata only: never another HP amount.
            }
            private void Death(Actor actor) => _observation.Write("ActorDied", actor, "", "callback");
            private void Absorbed(Actor actor) => _observation.Write("ActorDamageAbsorbed", actor, "No HP amount is available from this callback.", "callback");
            private void Broken(Vector3 point) => _observation.Write("StanceBroken", Actor,
                JsonUtility.ToJson(new PointDetail { position = point }), "callback");
            private void Vulnerability(bool active) => _observation.Write("VulnerabilityChanged", Actor,
                "active=" + active, "callback", active ? 1 : 0);
            public void Dispose()
            {
                // Managed event removal remains valid after Unity destroys the native object.
                if ((object)Actor != null) { Actor.DamageReceived -= Damage; Actor.Died -= Death; Actor.DamageAbsorbed -= Absorbed; }
                if ((object)Reaction != null) { Reaction.StanceBroken -= Broken; Reaction.VulnerabilityChanged -= Vulnerability; }
            }
        }

        [Serializable] private sealed class TelemetryRecord
        {
            public long seq; public int frame, absolute_frame; public double t, unscaled_t, realtime_t; public float time_scale;
            public string kind, actor, source, evidence_kind, detail; public ulong instance_id; public bool has_value; public float value;
        }
        [Serializable] private sealed class PointDetail { public Vector3 position; }
        [Serializable] private sealed class ProtocolDetail
        {
            public string session_id, target_id, producer; public long root_id, generation;
            public int encounter_id, depth; public Vector3 position; public bool is_death;
        }
        [Serializable] private sealed class EnergyDetail
        {
            public int before, after, requested, basic, skill, alternation, stance_break, technique, reserve, impact_index;
            public long execution, root_execution, epoch, generation;
            public string block, slot, origin, producer, target_id; public bool energy_eligible;
        }
        [Serializable] private sealed class ActorDetail
        {
            public long generation; public string affiliation; public bool alive, enabled;
            public Vector3 position, velocity, nav_destination; public Quaternion rotation;
            public float health, max_health, animator_time, animator_speed;
            public bool has_nav, nav_stopped, nav_has_path, has_animator, animator_transition; public int animator_state;
        }
        [Serializable] private sealed class ReactionDetail
        {
            public string rank, category, knockdown_phase; public float stance, max_stance, gameplay_time;
            public bool control_locked, stunned, airborne, vulnerable, resume_protected;
            public long launch_sequence, landed_sequence, break_sequence;
        }
        [Serializable] private sealed class EnemyDetail
        {
            public bool ai_enabled, winding_up, retreating, actions_enabled, has_actions, completed;
            public string retreat_phase, attack_kind; public int resolved_impacts; public Vector3 attack_center; public float attack_radius;
            public bool attack_eligible, has_preferred_target, root_motion, nav_update_rotation, attack_busy;
            public float next_attack_time;
            public float target_distance, target_planar_distance, melee_proximity, facing_dot, nav_remaining, nav_radius, stopping_distance;
            public Vector3 preferred_target, desired_velocity, nav_next_position;
            public string nav_status;
        }
        [Serializable] private sealed class PlayerDetail
        {
            public bool basic_pending; public float basic_progress,basic_duration,basic_contact;
            public float mana, max_mana, motion_time; public string weapon, motion;
            public bool motion_playing;
            public bool casting, movement_allowed_while_casting, world_input_blocked, asura_active, cadenced_waiting,
                hitstop_active, has_hitstop_runner;
            public int asura_energy, hitstop_applied_count; public float[] cooldowns;
        }
        [Serializable] private sealed class ContactDetail
        {
            public bool has_transaction,basic_pending; public float basic_progress,basic_duration,basic_contact,motion_time;
            public long sequence,generation; public float before,after; public string target,ability,execution,motion;
        }
        [Serializable] private sealed class SkillDetail
        {
            public string executor, phase, ability; public bool phase_observed;
            public float elapsed, duration, phase_progress; public int slot = -1;
        }
    }
}
