using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration tests for the hit-feedback foundation of combat-foundation-rework
    /// (task 16.3, R7). They cover the two halves of R7 the earlier tasks delivered:
    ///
    /// <list type="bullet">
    ///   <item><b>Hit-stop</b> (R7.1, R7.2, R7.3, R7.10): the thin <see cref="HitStopRunner"/>
    ///   MonoBehaviour driven on a real test GameObject against real-time, plus the pure
    ///   <see cref="HitStop"/>/<see cref="HitStopGrouping"/> decisions it consumes. A hit that damages
    ///   &gt;= 1 enemy applies a near-zero time scale for the duration and then restores the <em>captured
    ///   baseline</em> (not an unconditional 1); a zero/clamped-zero duration never touches
    ///   <c>Time.timeScale</c>; a pause set mid-hit-stop is preserved; grouping follows the clamped
    ///   maximum, never the sum, and two overlapping Apply calls in one window collapse to the max.</item>
    ///   <item><b>Cosmetic basic reaction</b> (R7.6, R7.9, R7.11): the basic reaction is a
    ///   <see cref="HitReactionType.Push"/> + <see cref="StanceBreakEffect.None"/> request (the shape
    ///   <c>CharControlScript.BuildBasicAttackReaction</c> builds, task 16.2). On a live
    ///   <see cref="CombatReactionController"/> it never control-locks and raises no
    ///   <c>StanceBroken</c> (no knockdown/launch/stun) on any rank, and — via a live boss telegraph —
    ///   it does not interrupt the AI, while a <see cref="HitReactionType.Stagger"/> request (the shared
    ///   mechanical path still used by skills) does interrupt. A zero-damage attack produces no feedback.
    ///   The authored posture reaction of the E (BreakerShock) is left intact.</item>
    /// </list>
    ///
    /// <para>
    /// Every object is created and torn down explicitly (no <c>FindObjectOfType</c>/<c>GameObject.Find</c>/
    /// magic strings beyond the one authored asset path the posture test loads). <c>Time.timeScale</c>
    /// is forced back to 1 in <see cref="TearDown"/> as a safety net so a flaky hit-stop coroutine can
    /// never leave the shared engine clock frozen for the rest of the run.
    /// </para>
    /// </summary>
    public sealed class HitFeedbackR7PlayModeTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            // Safety net: never leave the shared engine clock frozen for later tests, whatever happened
            // inside a hit-stop coroutine (R7.1/R7.10 tests drive Time.timeScale directly).
            Time.timeScale = 1f;
            foreach (GameObject go in _spawned)
                if (go) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private HitStopRunner NewRunner()
        {
            var go = new GameObject("HitStopRunnerHost");
            _spawned.Add(go);
            return go.AddComponent<HitStopRunner>();
        }

        // =====================================================================================
        // HIT-STOP (HitStopRunner MonoBehaviour + pure HitStop / HitStopGrouping)
        // =====================================================================================

        // R7.1 / R7.8: a damaging hit (duration > 0, enemiesDamaged >= 1) is authorized by the pure
        // decision, and the runner then freezes Time.timeScale to the near-zero hit-stop scale while
        // active and restores it after ~duration real seconds.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirements 7.1, 7.8, 7.10
        [UnityTest]
        public IEnumerator DamagingHit_AppliesHitStop_ThenRestores()
        {
            const float duration = 0.1f;
            Assert.IsTrue(HitStop.ShouldApply(duration, 1),
                "a damaging hit (>=1 enemy, duration>0) must authorize a hit-stop (R7.1/R7.8)");

            HitStopRunner runner = NewRunner();
            Assert.IsFalse(runner.IsActive, "runner starts idle");

            runner.Apply(duration);

            Assert.IsTrue(runner.IsActive, "hit-stop must be active immediately after Apply (R7.1)");
            Assert.Less(Time.timeScale, 1f,
                "the active hit-stop must drop Time.timeScale to the near-zero frozen scale (R7.1)");

            // Wait past the real-time window (plus a margin) so the coroutine's WaitForSecondsRealtime fires.
            yield return new WaitForSecondsRealtime(duration + 0.1f);

            Assert.IsFalse(runner.IsActive, "hit-stop must end after its real-time duration (R7.1)");
            Assert.AreEqual(1f, Time.timeScale, 1e-4f,
                "Time.timeScale must restore to the captured baseline (1 here) when the hit-stop ends (R7.10)");
        }

        // R7.2: a zero duration (and any value that clamps to zero: negative, NaN) is a no-op that does
        // NOT touch Time.timeScale and leaves the runner idle. The pure decision agrees: ShouldApply is
        // false for a zero-duration profile (e.g. the Asura burst pulses) regardless of enemies damaged.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirement 7.2
        [UnityTest]
        public IEnumerator ZeroOrClampedZeroDuration_NeverTouchesTimeScale()
        {
            // Zero-duration profile never pauses, no matter how many enemies it damaged.
            Assert.IsFalse(HitStop.ShouldApply(0f, 1), "duration 0 must not authorize a hit-stop (R7.2)");
            Assert.IsFalse(HitStop.ShouldApply(0f, 50), "duration 0 stays false for any enemy count (R7.2)");

            HitStopRunner runner = NewRunner();
            Time.timeScale = 1f;

            runner.Apply(0f);
            Assert.IsFalse(runner.IsActive, "a zero-duration Apply must not activate a hit-stop (R7.2)");
            Assert.AreEqual(1f, Time.timeScale, 0f, "a zero-duration Apply must not touch Time.timeScale (R7.2)");

            // Clamped-to-zero inputs (negative and NaN) are the same no-op.
            runner.Apply(-0.5f);
            Assert.IsFalse(runner.IsActive, "a negative duration clamps to 0 and must not activate (R7.2)");
            Assert.AreEqual(1f, Time.timeScale, 0f, "a negative duration must not touch Time.timeScale (R7.2)");

            runner.Apply(float.NaN);
            Assert.IsFalse(runner.IsActive, "a NaN duration clamps to 0 and must not activate (R7.2)");
            Assert.AreEqual(1f, Time.timeScale, 0f, "a NaN duration must not touch Time.timeScale (R7.2)");

            yield return null;
        }

        // R7.10 (baseline): a non-1 time scale set BEFORE the hit-stop (e.g. 0.5 slow-mo) is the value
        // restored when the hit-stop ends — NOT an unconditional 1.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirement 7.10
        [UnityTest]
        public IEnumerator Restore_ReinstatesNonOneBaseline_NotOne()
        {
            const float duration = 0.1f;
            const float baseline = 0.5f; // active slow-mo in effect when the hit lands

            HitStopRunner runner = NewRunner();
            Time.timeScale = baseline;

            runner.Apply(duration);
            Assert.IsTrue(runner.IsActive, "hit-stop active");
            Assert.Less(Time.timeScale, baseline,
                "the frozen scale while active is below the baseline slow-mo (R7.1)");

            yield return new WaitForSecondsRealtime(duration + 0.1f);

            Assert.IsFalse(runner.IsActive, "hit-stop ended");
            Assert.AreEqual(baseline, Time.timeScale, 1e-4f,
                "the captured 0.5 baseline must be restored, not an unconditional 1 (R7.10)");
        }

        // R7.10 (pause yields): if Time.timeScale is set to 0 (a menu pause) DURING the hit-stop, the
        // runner must NOT overwrite that pause with the captured baseline when its window ends — it
        // yields to the external value.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirement 7.10
        [UnityTest]
        public IEnumerator Restore_YieldsToExternalPauseSetMidHitStop()
        {
            const float duration = 0.15f;

            HitStopRunner runner = NewRunner();
            Time.timeScale = 1f;

            runner.Apply(duration);
            Assert.IsTrue(runner.IsActive, "hit-stop active");

            // External pause happens mid-hit-stop (menu opened). The runner must leave this alone.
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(duration + 0.1f);

            Assert.IsFalse(runner.IsActive, "hit-stop ended");
            Assert.AreEqual(0f, Time.timeScale, 0f,
                "an external pause set mid-hit-stop must survive the window ending (R7.10)");

            // Clean up the pause we injected so the restore safety-net in TearDown is not the only guard.
            Time.timeScale = 1f;
        }

        // R7.3 (grouping decision): one ImpactEvent hitting N enemies produces ONE grouped duration,
        // the clamped maximum of the per-impact durations, strictly less than their sum when two or
        // more are positive. The feedback is one Apply per event, never per enemy.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirement 7.3
        [Test]
        public void Grouping_FollowsClampedMaximum_NotSum()
        {
            var durations = new List<float> { 0.04f, 0.12f, 0.08f };
            float combined = HitStopGrouping.Combine(durations);

            Assert.AreEqual(0.12f, combined, 1e-6f, "grouped duration is the clamped maximum (R7.3)");

            float sum = 0f;
            foreach (float d in durations) sum += HitStop.ClampDuration(d);
            Assert.Less(combined, sum,
                "with >=2 positive durations the grouped maximum is strictly less than the sum (R7.3)");

            // Clamping is applied before the max: an out-of-range value cannot inflate the group.
            Assert.AreEqual(HitStop.MaxDuration, HitStopGrouping.Combine(new List<float> { 5f, 0.02f }), 1e-6f,
                "each duration is clamped to [0,1] before the maximum (R7.3)");
            Assert.AreEqual(0f, HitStopGrouping.Combine(null), 0f, "no impacts => no pause (R7.3)");
            Assert.AreEqual(0f, HitStopGrouping.Combine(new List<float>()), 0f, "empty group => no pause (R7.3)");
        }

        // R7.3 (grouping runtime): two overlapping Apply calls in the same hit-stop window collapse to
        // the LARGER duration (never the sum). A second Apply while active extends to the max and never
        // re-captures the baseline, so the pre-hit-stop scale is preserved.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirements 7.3, 7.10
        [UnityTest]
        public IEnumerator Overlap_CollapsesToMaxDuration_NotSum()
        {
            const float shortDur = 0.08f;
            const float longDur = 0.25f;
            const float baseline = 0.5f;

            HitStopRunner runner = NewRunner();
            Time.timeScale = baseline;

            // First impact of the group, then a second simultaneous impact extends the window to the max.
            runner.Apply(shortDur);
            runner.Apply(longDur);
            Assert.IsTrue(runner.IsActive, "overlapping Applies keep a single active hit-stop (R7.3)");

            // Past the SHORT duration but before the LONG one: must still be frozen (extended to max,
            // not ended at the short value, and definitely not summed into something longer than max).
            yield return new WaitForSecondsRealtime(shortDur + 0.04f);
            Assert.IsTrue(runner.IsActive,
                "the window extends to the max of the two durations, not the first/short one (R7.3)");

            // Past the LONG duration: the single grouped window ends and restores the captured baseline.
            yield return new WaitForSecondsRealtime(longDur + 0.1f);
            Assert.IsFalse(runner.IsActive, "the grouped window ends once the max duration elapses (R7.3)");
            Assert.AreEqual(baseline, Time.timeScale, 1e-4f,
                "overlap must not re-capture the baseline; the pre-hit-stop 0.5 is restored (R7.10)");
        }

        // =====================================================================================
        // COSMETIC BASIC REACTION (R7.6 / R7.9 / R7.11)
        // =====================================================================================

        /// <summary>
        /// Stands up a live <see cref="CombatReactionController"/> on a bare GameObject configured to the
        /// given rank (no NavMeshAgent/EnemyAI attack running), so the pure reaction channels can be
        /// exercised deterministically. Returns the controller and records every StanceBroken raise.
        /// </summary>
        private CombatReactionController NewReactionController(EnemyRank rank, List<Vector3> breaks)
        {
            var go = new GameObject($"Enemy_{rank}");
            go.SetActive(false);
            _spawned.Add(go);
            var controller = go.AddComponent<CombatReactionController>();
            go.SetActive(true); // Awake wires internal state
            controller.ConfigureRank(rank);
            if (breaks != null) controller.StanceBroken += p => breaks.Add(p);
            return controller;
        }

        private static HitReactionRequest BasicPushReaction()
        {
            // The exact shape CharControlScript.BuildBasicAttackReaction produces (task 16.2): a cosmetic
            // Push with StanceBreakEffect.None and a small stance chip. No hard CC is ever requested.
            return new HitReactionRequest(
                attacker: null,
                hitPoint: Vector3.zero,
                hitDirection: Vector3.forward,
                reactionType: HitReactionType.Push,
                strength: HitStrength.Light,
                stanceDamage: 12f,
                breakEffect: StanceBreakEffect.None,
                pushDistance: 0.35f);
        }

        // R7.9 / R7.11: the cosmetic basic reaction (Push + None) applies no hard CC on ANY rank — it
        // never control-locks and never raises a StanceBroken (no knockdown/launch/stun), because a
        // single basic stance chip does not deplete the stance pool. Covers Normal, Elite and Boss to
        // prove elite/boss enemies are not mechanically affected by a basic hit.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirements 7.9, 7.11
        [UnityTest]
        public IEnumerator BasicPush_AppliesNoHardCc_OnAnyRank()
        {
            foreach (EnemyRank rank in new[] { EnemyRank.Normal, EnemyRank.Elite, EnemyRank.Boss })
            {
                var breaks = new List<Vector3>();
                CombatReactionController controller = NewReactionController(rank, breaks);

                controller.ApplyReaction(BasicPushReaction());

                Assert.IsFalse(controller.IsControlLocked,
                    $"a basic Push must not control-lock a {rank} enemy (no stun/launch) (R7.9/R7.11)");
                Assert.AreEqual(0, breaks.Count,
                    $"a basic Push must not break the stance of a {rank} enemy (no knockdown/launch) (R7.9)");
                Assert.Greater(controller.CurrentStance, 0f,
                    $"a single basic chip must not deplete a {rank} enemy's stance pool (R7.9)");
            }

            yield return null;
        }

        // R7.8: a basic that resolves no damage produces no feedback — the pure decision denies the
        // hit-stop (ShouldApply false for 0 enemies), and the reaction is only ever applied inside the
        // damaged branch, so a controller that is never asked keeps full stance and no lock.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirement 7.8
        [UnityTest]
        public IEnumerator NoDamage_ProducesNoFeedback()
        {
            Assert.IsFalse(HitStop.ShouldApply(0.1f, 0),
                "a hit that damaged no enemy must not authorize a hit-stop (R7.8)");

            var breaks = new List<Vector3>();
            CombatReactionController controller = NewReactionController(EnemyRank.Normal, breaks);
            float fullStance = controller.CurrentStance;

            // The damaged branch never runs for a zero-damage attack, so ApplyReaction is never called:
            // the controller stays untouched. We assert the "never applied" state directly.
            HitStopRunner runner = NewRunner();
            if (HitStop.ShouldApply(0.1f, 0)) runner.Apply(0.1f); // guarded exactly as the executor guards it

            Assert.IsFalse(runner.IsActive, "no hit-stop for a zero-damage attack (R7.8)");
            Assert.AreEqual(1f, Time.timeScale, 0f, "Time.timeScale untouched for a zero-damage attack (R7.8)");
            Assert.IsFalse(controller.IsControlLocked, "no reaction applied for a zero-damage attack (R7.8)");
            Assert.AreEqual(fullStance, controller.CurrentStance, 1e-4f,
                "stance untouched for a zero-damage attack (R7.8)");
            Assert.AreEqual(0, breaks.Count, "no stance break for a zero-damage attack (R7.8)");

            yield return null;
        }

        // R7.6 / R7.7: the basic cosmetic reaction (Push) does NOT interrupt the enemy AI, while a
        // Stagger reaction (the shared mechanical path still used by skills) DOES — proving the basic is
        // distinct from mechanical stagger. Driven against a live boss telegraph (IsTelegraphing is the
        // interrupt observable), the proven pattern from EnemyVarietyIntegrationTests.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirements 7.6, 7.7, 7.11
        [UnityTest]
        public IEnumerator BasicPush_DoesNotInterruptAi_WhileStaggerDoes()
        {
            // --- 1) Basic Push does not interrupt a telegraphing boss (any rank; boss is the hardest case).
            SectorBoss boss = BuildTelegraphingBoss(out PlayerActor player1);
            float deadline = Time.time + 3f;
            while (!boss.IsTelegraphing && Time.time < deadline) yield return null;
            Assert.IsTrue(boss.IsTelegraphing, "precondition: the boss is winding up an attack");

            boss.GetComponent<CombatReactionController>().ApplyReaction(BasicPushReaction());

            Assert.IsTrue(boss.IsTelegraphing,
                "a cosmetic basic Push must NOT interrupt the boss's telegraph (R7.6/R7.7/R7.11)");

            // --- 2) A Stagger reaction (shared mechanical path) DOES interrupt the same kind of telegraph.
            SectorBoss boss2 = BuildTelegraphingBoss(out PlayerActor player2);
            deadline = Time.time + 3f;
            while (!boss2.IsTelegraphing && Time.time < deadline) yield return null;
            Assert.IsTrue(boss2.IsTelegraphing, "precondition: the second boss is winding up an attack");

            boss2.GetComponent<CombatReactionController>().ApplyReaction(new HitReactionRequest(
                attacker: player2, hitPoint: boss2.transform.position, hitDirection: Vector3.forward,
                reactionType: HitReactionType.Stagger, strength: HitStrength.Light,
                stanceDamage: 0f, breakEffect: StanceBreakEffect.None, pushDistance: 0f));

            Assert.IsFalse(boss2.IsTelegraphing,
                "a mechanical Stagger reaction must interrupt the telegraph (distinct from basic Push) (R7.6)");
        }

        /// <summary>
        /// Builds a live boss (its own NavMesh-free physical telegraph pipeline, mirroring
        /// <c>EnemyVarietyIntegrationTests.BossStanceBreakCancelsPendingImpact</c>) that winds up an
        /// attack we can interrupt. The boss auto-configures its own <see cref="CombatReactionController"/>
        /// to Boss rank in Awake.
        /// </summary>
        private SectorBoss BuildTelegraphingBoss(out PlayerActor player)
        {
            player = BuildBarePlayer();

            var go = new GameObject("TestBoss");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = Vector3.forward; // close to the player so it commits to a strike
            go.AddComponent<BoxCollider>();
            var actor = go.AddComponent<Actor>();
            actor.health = 1800f;
            go.SetActive(true);

            var boss = go.AddComponent<SectorBoss>();
            boss.Configure(player);
            return boss;
        }

        private PlayerActor BuildBarePlayer()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.weaponName = "FeedbackTestWeapon";
            weapon.attackDamage = 1f;

            var go = new GameObject("TestPlayer");
            go.SetActive(false);
            _spawned.Add(go);
            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            var player = go.AddComponent<PlayerActor>();
            player.health = 1000f;
            SetPrivate(player, "handTransform", hand.transform);
            SetPrivate(player, "startingWeapon", weapon);
            SetPrivate(player, "weapon", weapon);
            SetPrivate(player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f, criticalChance = 0f, damageMultiplier = 1f,
                increasedDamagePercent = 0f, flatDamageBonus = 0f,
            });
            go.transform.position = Vector3.zero;
            go.SetActive(true);
            player.SetMaxHealth(1000f);
            player.RestoreHealthToMax();
            return player;
        }

        // =====================================================================================
        // POSTURE PRESERVED (R7.9) — authored E (BreakerShock) reaction data unchanged
        // =====================================================================================

#if UNITY_EDITOR
        // R7.9: task 16.2 must NOT have widened or removed the mechanical posture reaction authored on
        // the E (BreakerShock). Its two AreaHitSteps still request their mechanical reaction + stun:
        // step 0 is a Push with a short 0.12 s stun seed, step 1 is a Stagger with a 0.65 s stun. Both
        // keep breakEffect None (the stun seeds the control-lock duration via the stance path), proving
        // the basic cosmetic reaction change left the authored skill reaction intact.
        // Feature: combat-foundation-rework, task 16.3.
        // Validates: Requirement 7.9
        [Test]
        public void BreakerShock_PostureReaction_IsUnchanged()
        {
            var ability = UnityEditor.AssetDatabase.LoadAssetAtPath<BreakerGauntletAbility>(
                "Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerShock.asset");
            Assert.IsNotNull(ability, "BreakerShock.asset must load");

            IReadOnlyList<AreaHitStep> steps = ability.HitSteps;
            Assert.IsNotNull(steps, "BreakerShock must author hit steps");
            Assert.AreEqual(2, steps.Count, "BreakerShock still has its two authored impacts (E, R7.9)");

            // Step 0 — the opening tap: a Push with a short stun seed and no hard-CC break effect.
            Assert.AreEqual(HitReactionType.Push, steps[0].reactionType,
                "E step 0 keeps its authored Push reaction (R7.9)");
            Assert.AreEqual(0.12f, steps[0].stunDuration, 1e-4f,
                "E step 0 keeps its authored 0.12 s stun seed (R7.9)");
            Assert.AreEqual(StanceBreakEffect.None, steps[0].breakEffect,
                "E step 0 keeps its authored break effect (R7.9)");

            // Step 1 — the heavy follow-up: a mechanical Stagger with the longer 0.65 s stun seed.
            Assert.AreEqual(HitReactionType.Stagger, steps[1].reactionType,
                "E step 1 keeps its authored mechanical Stagger reaction (R7.9)");
            Assert.AreEqual(0.65f, steps[1].stunDuration, 1e-4f,
                "E step 1 keeps its authored 0.65 s stun seed (R7.9)");
            Assert.AreEqual(StanceBreakEffect.None, steps[1].breakEffect,
                "E step 1 keeps its authored break effect (R7.9)");
        }
#endif

        private static void SetPrivate(object target, string field, object value)
        {
            System.Reflection.FieldInfo fi = target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }
    }
}
