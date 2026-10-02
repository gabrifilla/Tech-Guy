using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the R4/R6 dash-cancel integration tests of combat-foundation-rework
    /// (task 13.7). It stands up a minimal but real player — a <see cref="PlayerActor"/> with an
    /// <see cref="AbilityHolder"/>, a hand transform, deterministic stats, and a scene
    /// <see cref="Camera"/> tagged MainCamera (the dash reads the cursor through <c>Camera.main</c>)
    /// — and drives the production <see cref="AbilityHolder.TryUseDash"/> path end to end.
    ///
    /// <para>
    /// The player is built WITHOUT a <see cref="UnityEngine.AI.NavMeshAgent"/> so
    /// <see cref="DashScript.Dash"/> takes its agent-free fallback (<c>transform.position +=</c>),
    /// keeping the dash deterministic and NavMesh-independent in a bare test scene.
    /// </para>
    ///
    /// <para>
    /// A live <see cref="HookBus"/> is attached through the same RunBoons-ownership path production
    /// resolves (<see cref="AbilityHolder.BindRun"/> → <c>_runBoons.Hooks</c>), with the private-setter
    /// <see cref="RunBoons.Hooks"/> assigned by reflection — mirroring <see cref="DetonationTestRig"/>
    /// — so <c>OnDash</c> is observable exactly as the run's boons observe it.
    /// </para>
    ///
    /// Every object is created and held explicitly (no <c>FindObjectOfType</c>/<c>GameObject.Find</c>/
    /// magic strings) and torn down deterministically. Reflection is used only to reach the private
    /// serialized/runtime fields the tests must control, matching the reflection style of the other
    /// PlayMode rigs.
    /// </summary>
    public sealed class DashCancelResolverTestRig
    {
        private const BindingFlags Instance =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly PropertyInfo HooksProperty =
            typeof(RunBoons).GetProperty("Hooks", BindingFlags.Instance | BindingFlags.Public);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public PlayerActor Player { get; private set; }
        public AbilityHolder Holder { get; private set; }
        public HookBus Hooks { get; private set; }

        /// <summary>Captured AbilityUsed(index) invocations, in order (dash = 4).</summary>
        public List<int> AbilityUsedLog { get; } = new List<int>();

        /// <summary>Captured AbilityRejected(index, reason) invocations, in order.</summary>
        public List<(int index, AbilityUseFailure reason)> AbilityRejectedLog { get; } =
            new List<(int, AbilityUseFailure)>();

        /// <summary>Count of OnDash hook raises observed through the bound RunBoons bus.</summary>
        public int OnDashCount { get; private set; }

        /// <summary>Ordered trace of events ("used:4" / "dash") used to assert AbilityUsed precedes OnDash.</summary>
        public List<string> OrderTrace { get; } = new List<string>();

        /// <summary>
        /// Builds the player at the origin. Serialized fields are assigned while the GameObject is
        /// inactive so the equip pipeline in <see cref="PlayerActor.Awake"/> has what it needs, then
        /// the object is activated so AbilityHolder/PlayerActor run their real lifecycle. A camera is
        /// created and tagged MainCamera so <c>Camera.main</c> (read by the dash) resolves.
        /// </summary>
        public void Build(float mana = 1000f)
        {
            EnsureCamera();

            var go = new GameObject("DashTestPlayer");
            go.SetActive(false);
            _spawned.Add(go);

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.weaponName = "DashTestWeapon";
            weapon.attackDamage = 1f;

            Player = go.AddComponent<PlayerActor>();
            Player.health = 1000f;
            Player.mana = mana;
            SetPrivate(Player, "handTransform", hand.transform);
            SetPrivate(Player, "startingWeapon", weapon);
            SetPrivate(Player, "weapon", weapon);
            SetPrivate(Player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f, criticalChance = 0f, damageMultiplier = 1f,
                increasedDamagePercent = 0f, flatDamageBonus = 0f,
            });

            Holder = go.AddComponent<AbilityHolder>();

            go.SetActive(true); // Awake/Start: equip weapon, wire AbilityHolder

            Holder.AbilityUsed += OnAbilityUsed;
            Holder.AbilityRejected += OnAbilityRejected;
        }

        /// <summary>
        /// Attaches a live <see cref="HookBus"/> and binds it to the holder exactly as production does.
        /// We bypass <see cref="RunBoons"/>'s heavy Awake/Start (which equips a run weapon and adds UI)
        /// by binding a RunBoons instance whose <see cref="RunBoons.Hooks"/> we set directly via
        /// reflection, then calling <see cref="AbilityHolder.BindRun"/>. The RunBoons component lives on
        /// a separate, inactive host so none of its lifecycle runs.
        /// </summary>
        public HookBus AttachHookBus()
        {
            if (HooksProperty == null) throw new InvalidOperationException("RunBoons.Hooks not found");

            // RunBoons [RequireComponent]s PlayerActor/AbilityHolder; put it on an inactive host so no
            // lifecycle (Awake equips a weapon, adds RunRewardUI) runs. We only need it as the Hooks owner.
            var runHost = new GameObject("DashTestRunHost");
            runHost.SetActive(false);
            _spawned.Add(runHost);
            runHost.AddComponent<PlayerActor>();
            runHost.AddComponent<AbilityHolder>();
            RunBoons run = runHost.AddComponent<RunBoons>();

            Hooks = new HookBus();
            HooksProperty.SetValue(run, Hooks);
            Hooks.OnDash += OnDashRaised;

            Holder.BindRun(run);
            return Hooks;
        }

        /// <summary>
        /// Creates a dedicated <see cref="DashScript"/> instance (never the shipped asset) with the
        /// given i-frame window, displacement time, mana cost, and cooldown, so a test can tune the
        /// Janela_de_i-frames independently of the Duracao_de_Deslocamento (R6.7). All fields are set
        /// through reflection where they are private/serialized.
        /// </summary>
        public DashScript CreateDash(float iframeWindow = 0.2f, float dashTime = 0.2f,
            float dashVelocity = 8f, float manaCost = 0f, float cooldownTime = 0f)
        {
            var dash = ScriptableObject.CreateInstance<DashScript>();
            dash.name = "DashTestDash";
            dash.dashTime = dashTime;
            dash.dashVelocity = dashVelocity;
            dash.dashAnimation = string.Empty; // no Animator; keep the coroutine from calling Play
            SetPrivate(dash, "_iframeWindow", iframeWindow);
            SetPrivate(dash, "_manaCost", manaCost);
            dash.cooldownTime = cooldownTime;
            return dash;
        }

        /// <summary>
        /// Injects a real <see cref="BreakerGauntletCombat"/> into the holder's private
        /// <c>_breakerCombat</c> field and drives it into an "executing" state with the supplied active
        /// ability, elapsed time, and total duration — all via reflection on the real component's
        /// private backing fields. This exercises the production
        /// <see cref="AbilityHolder"/>.<c>ResolveCancel</c>/<see cref="CancelResolver"/> path (which
        /// reads exactly <c>IsExecuting</c>/<c>ActiveAbility</c>/<c>Elapsed</c>/<c>ExecutionDuration</c>)
        /// without standing up the full cast coroutine, keeping <see cref="AbilityHolder.IsCasting"/>
        /// true for the duration of the test.
        /// </summary>
        public BreakerGauntletCombat InjectCasting(BreakerGauntletAbility active, float elapsed, float duration)
        {
            BreakerGauntletCombat combat = Player.gameObject.GetComponent<BreakerGauntletCombat>();
            if (!combat) combat = Player.gameObject.AddComponent<BreakerGauntletCombat>();

            SetBackingProperty(combat, nameof(BreakerGauntletCombat.IsExecuting), true);
            SetBackingProperty(combat, nameof(BreakerGauntletCombat.ExecutionDuration), duration);
            SetPrivate(combat, "_activeAbility", active);
            SetPrivate(combat, "_elapsed", elapsed);

            SetPrivate(Holder, "_breakerCombat", combat);
            return combat;
        }

        /// <summary>Updates the injected cast progress (seconds elapsed) so a buffered dash can see its window open.</summary>
        public void SetCastElapsed(BreakerGauntletCombat combat, float elapsed) =>
            SetPrivate(combat, "_elapsed", elapsed);

        /// <summary>Clears the injected executing state so IsCasting returns false (action ended).</summary>
        public void EndCast(BreakerGauntletCombat combat)
        {
            SetBackingProperty(combat, nameof(BreakerGauntletCombat.IsExecuting), false);
            SetPrivate(combat, "_activeAbility", null);
        }

        /// <summary>
        /// Builds a <see cref="BreakerGauntletAbility"/> with a <see cref="CombatActionProfile"/> whose
        /// single Dash <see cref="CancelRule"/> opens over <paramref name="dashStart"/>..
        /// <paramref name="dashEnd"/> (normalized). When <paramref name="dashStart"/> &gt;
        /// <paramref name="dashEnd"/> the ability has NO dash rule, so dash-cancel is always forbidden.
        /// </summary>
        public BreakerGauntletAbility CreateCastingAbility(float dashStart, float dashEnd, bool asuraBurst = false)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "DashTestCastAbility";
            SetPrivate(ability, "_asuraBurst", asuraBurst);

            var profile = new CombatActionProfile();
            if (dashStart <= dashEnd)
            {
                // CancelRuleData is a struct; box it ONCE so the reflected field writes land on the same
                // instance, then unbox into the array (mutating a fresh box per call would lose the writes).
                object boxed = new CancelRuleData();
                SetPrivateBoxed(boxed, "target", CancelTarget.Dash);
                SetPrivateBoxed(boxed, "start", dashStart);
                SetPrivateBoxed(boxed, "end", dashEnd);
                SetPrivate(profile, "cancelRules", new[] { (CancelRuleData)boxed });
            }
            else
            {
                SetPrivate(profile, "cancelRules", Array.Empty<CancelRuleData>());
            }

            SetPrivate(ability, "_combatProfile", profile);
            return ability;
        }

        /// <summary>Forces a remaining cooldown on the dash for this player via DashScript's private readyTimes map.</summary>
        public void ForceDashCooldown(DashScript dash, float remainingSeconds)
        {
            var map = (Dictionary<GameObject, float>)GetPrivate(dash, "nextReadyTimes");
            map[Player.gameObject] = Time.time + remainingSeconds;
        }

        public float GetDashRemainingCooldown(DashScript dash) => dash.GetRemainingCooldown(Player.gameObject);

        /// <summary>
        /// Interrupts any dash coroutine running on the holder by calling
        /// <see cref="MonoBehaviour.StopAllCoroutines"/>, which runs the coroutine's <c>finally</c> block
        /// (the dash's single immunity-cleanup exit path) — the deterministic way to exercise a cancelled
        /// dash while keeping the live actor so its residual immunity state can be asserted (R6.8).
        /// </summary>
        public void InterruptDash() => Holder.StopAllCoroutines();

        /// <summary>
        /// Number of GameObjects the given <see cref="DashScript"/> currently considers "dashing"
        /// (its private <c>activeDashOwners</c> set). Lets a test assert the dash is fully torn down —
        /// no owner left tracked — after an interruption, which the public <c>IsDashing</c> cannot show
        /// once the owner reference is destroyed. The dash's single cleanup exit path removes the owner
        /// here, so an empty set means no residual dash state (and thus no residual immunity) (R6.8).
        /// </summary>
        public int ActiveDashOwnerCount(DashScript dash)
        {
            var set = (System.Collections.Generic.HashSet<GameObject>)GetPrivate(dash, "activeDashOwners");
            // Prune destroyed-but-not-yet-removed references so a destroyed owner never counts as live.
            set.RemoveWhere(go => !go);
            return set.Count;
        }

        /// <summary>Destroys the live player GameObject, the deterministic death/room-change interruption.</summary>
        public void DestroyPlayer() => UnityEngine.Object.DestroyImmediate(Player.gameObject);

        private void OnAbilityUsed(int index)
        {
            AbilityUsedLog.Add(index);
            if (index == 4) OrderTrace.Add("used:4");
        }

        private void OnAbilityRejected(int index, AbilityUseFailure reason) =>
            AbilityRejectedLog.Add((index, reason));

        private void OnDashRaised()
        {
            OnDashCount++;
            OrderTrace.Add("dash");
        }

        private void EnsureCamera()
        {
            if (Camera.main) return;
            var camGo = new GameObject("DashTestCamera");
            _spawned.Add(camGo);
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(0f, 10f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // look straight down at the ground plane
            cam.orthographic = true;
        }

        public void TearDown()
        {
            if (Holder)
            {
                Holder.AbilityUsed -= OnAbilityUsed;
                Holder.AbilityRejected -= OnAbilityRejected;
            }
            if (Hooks != null) Hooks.OnDash -= OnDashRaised;

            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        // --- reflection helpers -------------------------------------------------

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = GetField(target.GetType(), field);
            if (fi == null) throw new InvalidOperationException($"Field '{field}' not found on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        // Writes to an already-boxed value type (struct) so successive writes accumulate on the same box.
        private static void SetPrivateBoxed(object boxed, string field, object value)
        {
            FieldInfo fi = GetField(boxed.GetType(), field);
            if (fi == null) throw new InvalidOperationException($"Field '{field}' not found on {boxed.GetType().Name}");
            fi.SetValue(boxed, value);
        }

        private static object GetPrivate(object target, string field)
        {
            FieldInfo fi = GetField(target.GetType(), field);
            if (fi == null) throw new InvalidOperationException($"Field '{field}' not found on {target.GetType().Name}");
            return fi.GetValue(target);
        }

        // Sets an auto-property with a private setter by writing its compiler-generated backing field.
        private static void SetBackingProperty(object target, string property, object value)
        {
            SetPrivate(target, $"<{property}>k__BackingField", value);
        }

        private static FieldInfo GetField(Type type, string field)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo fi = t.GetField(field, Instance);
                if (fi != null) return fi;
            }
            return null;
        }
    }
}
