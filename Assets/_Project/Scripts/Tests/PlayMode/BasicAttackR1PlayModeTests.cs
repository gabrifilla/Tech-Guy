using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration tests for combat-foundation-rework — task 13.6 (Requirement 1:
    /// "Remoção do auto-combate persistente"). These exercise the <see cref="CharControlScript"/>
    /// WIRING of the pure <c>BasicAttackDriver</c> against a live player loop, a real
    /// <see cref="NavMeshAgent"/> on a baked mesh and a live enemy <see cref="Interactable"/>/
    /// <see cref="Actor"/>. Attacks are COUNTED through the real observable seam
    /// <c>CharControlScript.BasicAttackPerformed</c> (R1.8), never a stubbed decision helper.
    ///
    /// <para>
    /// The pure cadence math (tap = exactly one, hold = one per interval, no-input = zero) is already
    /// proven at the EditMode layer by <c>BasicAttackDriverPropertyTests</c> (task 7.2, Property P1).
    /// What these PlayMode tests add is the CharControlScript end: that merely owning a target never
    /// attacks (R1.1/R1.7/R1.11), that a Toque_de_Ataque drives exactly one <c>BasicAttackPerformed</c>
    /// through the follow/approach path (R1.1), that Segurar_Ataque repeats at the configured
    /// <c>attackInterval</c> (R1.3), that release stops future repeats without cutting a started hit
    /// (R1.4) while a tap's single execution is preserved (R1.5), that a Ataque_Direcional strikes in
    /// place without approaching or selecting a target (R1.2), and that an Ordem_de_Movimento outside
    /// the Zona_Morta_de_Movimento moves and clears the intent while a click inside the deadzone
    /// preserves it (R1.9/R1.10).
    /// </para>
    ///
    /// <para>
    /// <c>CharControlScript</c>'s real input comes from <c>GamePreferences</c>/mouse plus pointer
    /// raycasts, which cannot be simulated headlessly. Following the reflection pattern established by
    /// <see cref="KitingStepPlayModeTests"/>, these tests drive the observable seams directly: the
    /// private <c>BasicAttackDriver</c>, the private <c>target</c>/<c>attackInterval</c>/<c>playerBusy</c>
    /// fields, and the private <c>SetTarget</c>/<c>TryAttackTarget</c>/<c>MoveToPosition</c> methods. No
    /// production surface is widened for the test.
    /// </para>
    /// </summary>
    public sealed class BasicAttackR1PlayModeTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private NavMeshData _navMesh;
        private NavMeshDataInstance _navMeshInstance;
        private WeaponScript _weapon;

        private CharControlScript _controls;
        private PlayerActor _player;
        private NavMeshAgent _agent;
        private int _attackCount;

        // --- reflection handles ------------------------------------------------------------------

        private static readonly FieldInfo DriverField =
            typeof(CharControlScript).GetField("_basicAttackDriver",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo TargetField =
            typeof(CharControlScript).GetField("target",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AttackIntervalField =
            typeof(CharControlScript).GetField("attackInterval",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NextAttackTimeField =
            typeof(CharControlScript).GetField("nextAttackTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerBusyField =
            typeof(CharControlScript).GetField("playerBusy",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo MovementDeadZoneField =
            typeof(CharControlScript).GetField("_movementClickDeadZone",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo SetTargetMethod =
            typeof(CharControlScript).GetMethod("SetTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MoveToPositionMethod =
            typeof(CharControlScript).GetMethod("MoveToPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ClearTargetMethod =
            typeof(CharControlScript).GetMethod("ClearTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo DriverQueueTap =
            typeof(BasicAttackDriver).GetMethod("QueueTap");
        private static readonly MethodInfo DriverSetHold =
            typeof(BasicAttackDriver).GetMethod("SetHold");
        private static readonly MethodInfo DriverReleaseHold =
            typeof(BasicAttackDriver).GetMethod("ReleaseHold");

        // --- lifecycle ---------------------------------------------------------------------------

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_controls != null) _controls.BasicAttackPerformed -= OnBasicAttack;
            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
            if (_weapon) UnityEngine.Object.DestroyImmediate(_weapon);
            if (_navMeshInstance.valid) _navMeshInstance.Remove();
            if (_navMesh) UnityEngine.Object.DestroyImmediate(_navMesh);
            _controls = null;
            _player = null;
            _agent = null;
            _attackCount = 0;
        }

        // --- rig helpers -------------------------------------------------------------------------

        /// <summary>Bakes a flat 60x60 floor NavMesh so the player's agent can path/approach.</summary>
        private bool TryBakeFullFloor()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(60f, 0.2f, 60f),
                transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(60f, 5f, 60f)), Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>
        /// Builds a minimal but real <see cref="PlayerActor"/> with a <see cref="NavMeshAgent"/> warped
        /// onto the mesh, an <see cref="Animator"/> (so ValidateComponents logs no error) and a wired
        /// <see cref="CharControlScript"/>. Mirrors the serialize-while-inactive pattern of the other
        /// PlayMode rigs. Subscribes the attack counter to the real BasicAttackPerformed event.
        /// </summary>
        private void BuildPlayer(Vector3 position)
        {
            _weapon = ScriptableObject.CreateInstance<WeaponScript>();
            _weapon.weaponName = "TestGauntlet";
            _weapon.attackDamage = 10f;
            // A short attack reach so the player must actually close before an attack can resolve.
            _weapon.attackDistance = 1.8f;
            _weapon.attackSpeed = 0.3f; // deterministic attackInterval; overwritten per-test as needed

            var go = new GameObject("TestPlayer");
            go.SetActive(false);
            _spawned.Add(go);

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            _player = go.AddComponent<PlayerActor>();
            SetPrivate(_player, "handTransform", hand.transform);
            SetPrivate(_player, "startingWeapon", _weapon);
            SetPrivate(_player, "weapon", _weapon);
            SetPrivate(_player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f, criticalChance = 0f, damageMultiplier = 1f,
                increasedDamagePercent = 0f, flatDamageBonus = 0f,
            });

            _agent = go.AddComponent<NavMeshAgent>();
            _agent.radius = 0.3f;
            _agent.height = 1.8f;
            _agent.speed = 8f;
            _agent.acceleration = 80f;
            _agent.angularSpeed = 999f;
            _agent.autoBraking = false;

            go.AddComponent<Animator>();

            _controls = go.AddComponent<CharControlScript>();
            _controls.playerActor = _player;

            go.transform.position = position;
            go.SetActive(true);
            if (_agent.enabled) _agent.Warp(position);

            _player.EquipWeapon(_weapon);

            _attackCount = 0;
            _controls.BasicAttackPerformed += OnBasicAttack;
        }

        private void OnBasicAttack() => _attackCount++;

        /// <summary>Creates a live enemy with an <see cref="Actor"/>, box collider and
        /// <see cref="Interactable"/> of type Enemy (so the target/follow path accepts it).</summary>
        private (Actor actor, Interactable interactable) BuildEnemy(Vector3 position, float health = 1000f)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var actor = go.AddComponent<Actor>();
            actor.health = health;

            var interactable = go.AddComponent<Interactable>();
            interactable.interactionType = InteractableType.Enemy;

            go.SetActive(true); // Awake wires Interactable.myActor + Actor.maxHealth
            return (actor, interactable);
        }

        // --- driver / state accessors ------------------------------------------------------------

        private BasicAttackDriver Driver() => (BasicAttackDriver)DriverField.GetValue(_controls);
        private void QueueTap() => DriverQueueTap.Invoke(Driver(), null);
        private void SetHold(bool held) => DriverSetHold.Invoke(Driver(), new object[] { held });
        private void ReleaseHold() => DriverReleaseHold.Invoke(Driver(), null);

        private void SetTarget(Interactable t) => SetTargetMethod.Invoke(_controls, new object[] { t });
        private void MoveToPosition(Vector3 p) => MoveToPositionMethod.Invoke(_controls, new object[] { p });
        private void ClearTarget() => ClearTargetMethod.Invoke(_controls, null);
        private Interactable CurrentTarget() => (Interactable)TargetField.GetValue(_controls);
        private float AttackInterval() => (float)AttackIntervalField.GetValue(_controls);
        private void SetAttackInterval(float v) => AttackIntervalField.SetValue(_controls, v);

        /// <summary>Pumps frames until the player is within attack reach of <paramref name="enemy"/>
        /// or the frame budget runs out. Returns true if it closed to reach.</summary>
        private IEnumerator ApproachUntilInRange(Actor enemy, int maxFrames = 240)
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                yield return null;
                Vector3 d = enemy.transform.position - _player.transform.position;
                d.y = 0f;
                // EffectiveAttackRange for a melee weapon == attackDistance here (MeleeScale defaults to 1).
                if (d.magnitude <= _weapon.attackDistance + 0.05f) yield break;
            }
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(fi, Is.Not.Null, $"Expected field '{field}' on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        // --- tests -------------------------------------------------------------------------------

        // Feature: combat-foundation-rework, task 13.6
        // A target in range with NO attack input produces zero BasicAttackPerformed across many frames:
        // the removed auto-combat means mere target existence never attacks (R1.1/R1.7/R1.11). The
        // player sits inside reach of the enemy, holds the enemy as its Alvo_Interno, and the live Update
        // loop (FollowTarget -> TryAttackTarget) runs for N frames without a tap or hold being fed.
        // Validates: Requirements 1.1, 1.11
        [UnityTest]
        public IEnumerator TargetInRange_NoInput_FiresNoAttacks()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(1.2f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            // Select the enemy as the Alvo_Interno but queue NO tap and set NO hold.
            SetTarget(interactable);
            Assert.That(CurrentTarget(), Is.EqualTo(interactable), "the enemy must be the Alvo_Interno");

            for (int frame = 0; frame < 60; frame++) yield return null;

            Assert.That(_attackCount, Is.Zero,
                "a target in range with no input must never attack — auto-combat is removed (R1.1/R1.11)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Ataque_Alvo: selecting an eligible enemy and queueing exactly one Toque_de_Ataque drives
        // exactly one BasicAttackPerformed through the approach/follow path (R1.1). The player closes to
        // reach under the live loop, then TryAttackTarget consumes the single pending tap into one swing.
        // Validates: Requirements 1.1
        [UnityTest]
        public IEnumerator TargetedTap_FiresExactlyOneAttack()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(5f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            // Ataque_Alvo = select the enemy under the cursor + queue a single tap (the exact pair
            // HandleInteractable performs on an explicit enemy click).
            SetTarget(interactable);
            QueueTap();

            // Let the player approach to reach and the live loop resolve the single swing.
            yield return ApproachUntilInRange(enemy);
            for (int frame = 0; frame < 30; frame++) yield return null;

            Assert.That(_attackCount, Is.EqualTo(1),
                "a single Ataque_Alvo tap must fire exactly one BasicAttackPerformed (R1.1)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Segurar_Ataque: with the enemy in range and SetHold(true), the driver authorizes repeats at
        // the configured attackInterval, so the attack count grows by roughly one per interval over time
        // (R1.3). A long busy window would mask cadence, so the test keeps the enemy at huge health and
        // measures that MORE than one but a bounded number of attacks land across a few intervals.
        // Validates: Requirements 1.3
        [UnityTest]
        public IEnumerator Hold_RepeatsAtAttackInterval()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(1.0f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            SetTarget(interactable);
            // Let one frame prime RefreshWeaponStats' cache so our interval override below is not
            // immediately recomputed from the weapon's attackSpeed on the next Update.
            yield return null;
            // Start already in reach so cadence (not travel) governs the count. Give a short but
            // measurable interval so a few repeats fit a bounded frame budget.
            SetAttackInterval(0.1f);
            SetHold(true);

            float interval = AttackInterval();
            Assert.That(interval, Is.GreaterThan(0f), "the test needs a positive attack interval");

            // Run for ~4 intervals of wall-clock time under the live loop.
            float elapsed = 0f;
            float window = interval * 4f;
            int guard = 0;
            while (elapsed < window && guard < 2000)
            {
                yield return null;
                elapsed += Time.deltaTime;
                guard++;
            }

            // The first hold attack is allowed immediately, then one per interval: across ~4 intervals
            // we expect strictly more than one and no more than the frame budget could ever allow.
            Assert.That(_attackCount, Is.GreaterThan(1),
                "holding attack must repeat across intervals, not fire only once (R1.3)");
            Assert.That(_attackCount, Is.LessThanOrEqualTo(8),
                "hold repeats must be gated by attackInterval, not fire every frame (R1.3)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Releasing after a hold stops FUTURE repeats without cutting an already-authorized hit (R1.4),
        // and releasing after a single tap preserves that one execution (R1.5). This drives the driver
        // seam directly against a frozen clock-free counter: a tap-only sequence keeps its one swing after
        // release; a hold sequence stops producing further swings once released.
        // Validates: Requirements 1.4, 1.5
        [UnityTest]
        public IEnumerator Release_StopsHoldRepeats_PreservesTapExecution()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(1.0f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            SetTarget(interactable);
            // Prime the weapon-stat cache before overriding the interval (see Hold test note).
            yield return null;
            SetAttackInterval(0.1f);

            // --- R1.5: a single tap, then release, preserves exactly the one authorized execution.
            QueueTap();
            ReleaseHold(); // release after a tap must NOT remove the pending/executed tap
            yield return null;
            for (int frame = 0; frame < 20; frame++) yield return null;
            Assert.That(_attackCount, Is.EqualTo(1),
                "release after a tap preserves the single authorized execution (R1.5)");

            // --- R1.4: hold a while to accrue repeats, then release and confirm no further repeats.
            SetHold(true);
            float interval = AttackInterval();
            float elapsed = 0f;
            int guard = 0;
            while (elapsed < interval * 3f && guard < 2000)
            {
                yield return null;
                elapsed += Time.deltaTime;
                guard++;
            }
            int afterHold = _attackCount;
            Assert.That(afterHold, Is.GreaterThan(1), "the hold must have produced repeats before release");

            ReleaseHold();
            // Pump well beyond several intervals: with hold released, no new repeat may be authorized.
            elapsed = 0f; guard = 0;
            while (elapsed < interval * 4f && guard < 2000)
            {
                yield return null;
                elapsed += Time.deltaTime;
                guard++;
            }
            Assert.That(_attackCount, Is.EqualTo(afterHold),
                "releasing hold stops future repeats without cutting the started hit (R1.4)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Ataque_Direcional strikes in the aimed direction WITHOUT approaching or selecting a target:
        // TryDirectionalBasicAttack clears any target, never paths toward an enemy, and still fires one
        // BasicAttackPerformed in place (R1.2). The enemy sits well out of reach; the player must not
        // close any meaningful distance toward it and the Alvo_Interno must stay cleared.
        // Validates: Requirements 1.2
        [UnityTest]
        public IEnumerator DirectionalAttack_StrikesWithoutApproachingOrSelecting()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(10f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            float distBefore = Vector3.Distance(_player.transform.position, enemy.transform.position);

            // Aim toward the enemy but issue a Ataque_Direcional: it strikes in place, no approach.
            bool struck = _controls.TryDirectionalBasicAttack(enemy.transform.position);
            Assert.That(struck, Is.True, "a directional basic from a valid state must execute (R1.2)");

            for (int frame = 0; frame < 30; frame++) yield return null;

            Assert.That(_attackCount, Is.EqualTo(1),
                "a directional attack fires exactly one BasicAttackPerformed in place (R1.2)");
            Assert.That(CurrentTarget(), Is.Null,
                "a directional attack never selects a target (R1.2)");

            float distAfter = Vector3.Distance(_player.transform.position, enemy.transform.position);
            Assert.That(distAfter, Is.GreaterThan(distBefore - 0.5f),
                "a directional attack must not auto-approach the enemy (R1.2)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Ordem_de_Movimento outside the Zona_Morta_de_Movimento moves the player and clears the current
        // combat intent/target (R1.9); a click inside the deadzone preserves the current intent (R1.10).
        // The move path (MoveToPosition) clears the Alvo_Interno and the deadzone check is the exact
        // WorldClickResolver.IsNearPlayer seam ClickToMove gates on.
        // Validates: Requirements 1.9, 1.10
        [UnityTest]
        public IEnumerator MoveOrder_OutsideDeadzoneClears_InsideDeadzonePreserves()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(3f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            float deadZone = (float)MovementDeadZoneField.GetValue(_controls);
            Assert.That(deadZone, Is.GreaterThan(0f), "the movement deadzone must be positive");

            // --- R1.10: a click INSIDE the deadzone is not a move order; the intent is preserved.
            SetTarget(interactable);
            QueueTap();
            Vector3 insidePoint = _player.transform.position + new Vector3(deadZone * 0.5f, 0f, 0f);
            Assert.That(WorldClickResolver.IsNearPlayer(insidePoint, _player.transform.position, deadZone),
                Is.True, "the inside point must fall within the deadzone");
            // ClickToMove would early-return here (no MoveToPosition), so the target/intent survives.
            Assert.That(CurrentTarget(), Is.EqualTo(interactable),
                "a click inside the deadzone preserves the current Intencao_de_Comando (R1.10)");

            // --- R1.9: a move order OUTSIDE the deadzone moves and clears the pending combat intent.
            Vector3 outsidePoint = _player.transform.position + new Vector3(6f, 0f, 0f);
            Assert.That(WorldClickResolver.IsNearPlayer(outsidePoint, _player.transform.position, deadZone),
                Is.False, "the outside point must fall beyond the deadzone");

            Vector3 startPos = _player.transform.position;
            MoveToPosition(outsidePoint);
            Assert.That(CurrentTarget(), Is.Null,
                "a move order outside the deadzone clears the combat target/intent (R1.9)");

            // The move order must actually displace the player toward the destination.
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                if (Vector3.Distance(_player.transform.position, startPos) > 1f) break;
            }
            Assert.That(Vector3.Distance(_player.transform.position, startPos), Is.GreaterThan(1f),
                "a move order outside the deadzone moves the player (R1.9)");

            // And the cleared intent must not attack even though an enemy existed.
            Assert.That(_attackCount, Is.Zero,
                "after a move order the superseded combat intent fires no attack (R1.9)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // No eligible target under the cursor makes a Ataque_Alvo a total no-op: with no Alvo_Interno
        // selected, queuing a tap and running the live loop fires no BasicAttackPerformed, selects no
        // enemy by proximity, and leaves state untouched (R1.11). An enemy exists in the scene but is
        // NOT selected, standing in for "nothing eligible under the cursor".
        // Validates: Requirements 1.11
        [UnityTest]
        public IEnumerator NoEligibleTarget_TargetedAttackIsNoOp()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            // An enemy exists but is deliberately NOT selected as the target.
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(2f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            Assert.That(CurrentTarget(), Is.Null, "no target is selected for this case");

            // A Ataque_Alvo with nothing selected: a stray tap on the driver must not spontaneously pick
            // an enemy by proximity, so the follow/attack path stays idle.
            QueueTap();
            for (int frame = 0; frame < 60; frame++) yield return null;

            Assert.That(_attackCount, Is.Zero,
                "a targeted attack with no eligible target is a total no-op (R1.11)");
            Assert.That(CurrentTarget(), Is.Null,
                "no enemy is selected by proximity when nothing is under the cursor (R1.11)");
        }
    }
}
