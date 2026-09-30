using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for Property 9 of impactful-weapon-boons (R5 — Impaling Line:
    /// pierce and pull).
    ///
    /// Feature: impactful-weapon-boons, Property 9
    /// Property 9: for any ImpalingLine rank r &gt;= 1, a thrust SHALL damage every enemy along its
    /// line (not just the first), and each connected enemy WITH locomotion SHALL be displaced toward
    /// the player by at most 0.75 * r metres, never through solid scenery.
    /// Validates: Requirements 5.1, 5.2, 5.3
    ///
    /// These properties are physics- and locomotion-dependent, so they run in PlayMode:
    /// <list type="bullet">
    /// <item><b>Pierce (R5.1)</b> is driven through the real
    /// <see cref="PlayerActor.TryApplyAreaDamage"/> box query the thrust uses. With the box length at
    /// the full <c>plan.Range</c> (the Impaling Line branch in <c>ArsenalCombat.Execute</c>), a line of
    /// N enemies is ALL damaged, not just the first — the box overlaps every collider on the line.</item>
    /// <item><b>Pull (R5.2/R5.3)</b> is driven through the real private
    /// <c>ArsenalCombat.ApplyImpalePull</c> — the exact production method that searches enemies along
    /// the thrust line and glides each connected enemy with a <see cref="SoftGroupingService"/> toward
    /// the player via <c>SoftGroupingService.ApplyExternalDisplacement</c> (a speed-capped,
    /// budget-bounded coroutine). An enemy WITHOUT a <see cref="SoftGroupingService"/> is never
    /// touched (no hard impulse, no teleport through scenery, R5.3), and each pulled enemy's total
    /// displacement stays within 0.75 * rank metres (R5.2).</item>
    /// </list>
    ///
    /// Reflection is used to invoke the private <c>ApplyImpalePull</c> and inject the private
    /// <c>_player</c> field, following the same seam-driving approach as
    /// <see cref="ChainThrustConditionalTests"/> and <see cref="ArsenalProjectileTestRig"/> — the spec
    /// forbids widening gameplay APIs purely for tests.
    /// </summary>
    public sealed class ImpalingLinePierceAndPullTests
    {
        private const float PullPerRank = 0.75f;    // R5.2: plan.ImpalePull = 0.75 * rank
        private const float Tolerance = 0.02f;      // per-frame overshoot slack on the glide budget

        private static readonly MethodInfo ApplyImpalePullMethod = typeof(ArsenalCombat).GetMethod(
            "ApplyImpalePull", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo PlayerField = typeof(ArsenalCombat).GetField(
            "_player", BindingFlags.Instance | BindingFlags.NonPublic);

        private ArsenalProjectileTestRig _rig;
        private ArsenalCombat _combat;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(ApplyImpalePullMethod, "Expected private ArsenalCombat.ApplyImpalePull (test seam).");
            Assert.IsNotNull(PlayerField, "Expected private ArsenalCombat._player (test seam).");
            _rig = new ArsenalProjectileTestRig();
        }

        [TearDown]
        public void TearDown()
        {
            if (_combat) Object.DestroyImmediate(_combat);
            _combat = null;
            _rig?.TearDown();
        }

        // Builds an ArsenalCastPlan snapshot for an Impaling Line thrust at the given rank, driven
        // entirely off value-type plan fields (R5.4 — the source ability asset is never mutated).
        private static ArsenalCastPlan BuildImpalePlan(float range, float width, int rank)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            var plan = new ArsenalCastPlan(ability)
            {
                Range = range,
                Width = width,
                ImpaleLine = rank > 0,          // R5.1
                ImpalePull = PullPerRank * rank // R5.2 (0.75 m per rank)
            };
            Object.DestroyImmediate(ability); // plan copied by value; the asset is no longer needed
            return plan;
        }

        // Adds a SoftGroupingService "locomotion" to an enemy. With no NavMeshAgent / CharacterController
        // present, ApplyExternalDisplacement falls back to translating the transform directly, so the
        // glide is fully deterministic in the test scene while still exercising the real locomotion
        // channel the pull routes through (R5.3).
        private static void AddLocomotion(Actor enemy) => enemy.gameObject.AddComponent<SoftGroupingService>();

        // Feature: impactful-weapon-boons, Property 9
        // Pierce (R5.1): a thrust with Impaling Line damages EVERY enemy along the line, not just the
        // first. Driven through the real PlayerActor.TryApplyAreaDamage box query the thrust uses.
        // Validates: Requirements 5.1
        [UnityTest]
        public IEnumerator Property9_ThrustDamagesEveryEnemyAlongTheLine()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 51);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                PlayerActor owner = _rig.BuildOwner();
                owner.transform.position = Vector3.zero;

                int rank = 1 + rng.Next(0, 3);              // ranks 1..3
                int lineCount = 2 + rng.Next(0, 4);         // 2..5 enemies in the line
                float range = 8f + (float)rng.NextDouble() * 4f;   // 8..12 m thrust reach
                float width = 1f + (float)rng.NextDouble();        // 1..2 m thrust width
                float spacing = range / (lineCount + 1);           // evenly spread inside the reach

                var direction = Vector3.forward;
                var line = new List<Actor>();
                for (int i = 0; i < lineCount; i++)
                {
                    // Enemies sit on the thrust heading, spread from just past the player to near the tip.
                    float along = spacing * (i + 1);
                    line.Add(_rig.BuildEnemy(direction * along, health: 100000f));
                }

                ArsenalCastPlan plan = BuildImpalePlan(range, width, rank);

                // Drive the exact box the Impaling Line thrust uses: full plan.Range length so the whole
                // line is covered (showEffect=false keeps the query scene-free). Every overlapped enemy
                // is damaged, proving the thrust pierces rather than stopping at the first (R5.1).
                int damaged = owner.TryApplyAreaDamage(
                    owner.transform.position, direction, plan.Range,
                    new Vector3(plan.Width, 2f, plan.Range), AreaHitShape.Box, plan.Width,
                    Physics.DefaultRaycastLayers, _rig.Weapon.attackDamage, plan.Damage, 0f, null, false, Color.white);

                Assert.AreEqual(lineCount, damaged,
                    $"case {c}: rank {rank}, expected the thrust to damage all {lineCount} enemies on the line.");
                foreach (Actor e in line)
                    Assert.IsTrue(_rig.DamageEvents.Contains(e),
                        $"case {c}: an enemy on the line was not damaged (thrust must pierce, not stop at the first).");

                yield return null;
            }
        }

        // Feature: impactful-weapon-boons, Property 9
        // Pull (R5.2): each connected enemy WITH locomotion is pulled toward the player by at most
        // 0.75 * rank metres, and generally toward the player. Driven through the real
        // ArsenalCombat.ApplyImpalePull + GlideImpaledEnemy coroutines.
        // Validates: Requirements 5.2
        [UnityTest]
        public IEnumerator Property9_ConnectedEnemiesArePulledTowardPlayerWithinRankBudget()
        {
            const int cases = 60; // each case spends multiple frames letting the glide coroutines run

            for (int c = 0; c < cases; c++)
            {
                var rng = new System.Random(PropertyCheck.DefaultSeed + 52 + c);

                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                PlayerActor owner = _rig.BuildOwner();
                owner.transform.position = Vector3.zero;
                _combat = owner.gameObject.AddComponent<ArsenalCombat>();
                PlayerField.SetValue(_combat, owner);

                int rank = 1 + rng.Next(0, 3);              // ranks 1..3
                float pull = PullPerRank * rank;
                int lineCount = 2 + rng.Next(0, 3);         // 2..4 enemies in the line
                float range = 9f + (float)rng.NextDouble() * 3f;   // 9..12 m
                float width = 1.5f;
                float spacing = range / (lineCount + 2);

                var direction = Vector3.forward;
                var line = new List<Actor>();
                var startPositions = new List<Vector3>();
                for (int i = 0; i < lineCount; i++)
                {
                    // Place each enemy well beyond the pull budget so the toward-player heading is stable
                    // and the enemy never reaches the player (which would legitimately cap the pull short).
                    float along = 2f + pull + spacing * i;
                    Actor enemy = _rig.BuildEnemy(direction * along, health: 100000f);
                    AddLocomotion(enemy);
                    line.Add(enemy);
                    startPositions.Add(enemy.transform.position);
                }

                ArsenalCastPlan plan = BuildImpalePlan(range, width, rank);

                // Invoke the real production pull. center = player position, direction = thrust heading.
                ApplyImpalePullMethod.Invoke(_combat, new object[] { owner.transform.position, direction, plan });

                // Let the glide coroutines run to completion. The budget is 0.75*rank m at <=1.5 m/s,
                // so a few hundred frames is generous; break out once every enemy has stopped moving.
                Vector3[] previous = new Vector3[lineCount];
                for (int frame = 0; frame < 600; frame++)
                {
                    for (int i = 0; i < lineCount; i++) previous[i] = line[i].transform.position;
                    yield return null;

                    bool anyMoving = false;
                    for (int i = 0; i < lineCount; i++)
                        if ((line[i].transform.position - previous[i]).sqrMagnitude > 1e-8f) { anyMoving = true; break; }
                    if (!anyMoving && frame > 2) break;
                }

                for (int i = 0; i < lineCount; i++)
                {
                    Vector3 moved = line[i].transform.position - startPositions[i];
                    moved.y = 0f;

                    // R5.2: total displacement never exceeds the 0.75*rank budget (small per-frame slack).
                    Assert.LessOrEqual(moved.magnitude, pull + Tolerance,
                        $"case {c}: rank {rank}, enemy {i} pulled {moved.magnitude:F3} m, exceeds budget {pull:F3} m.");

                    // The enemy actually moved (a connected enemy with locomotion is pulled).
                    Assert.Greater(moved.magnitude, 1e-3f,
                        $"case {c}: rank {rank}, enemy {i} with locomotion was not pulled at all.");

                    // Displacement is toward the player: the enemy is closer to the player than it started.
                    float startDist = new Vector2(startPositions[i].x, startPositions[i].z).magnitude;
                    Vector3 endPos = line[i].transform.position;
                    float endDist = new Vector2(endPos.x, endPos.z).magnitude;
                    Assert.Less(endDist, startDist + Tolerance,
                        $"case {c}: rank {rank}, enemy {i} was not pulled toward the player " +
                        $"(start {startDist:F3} m -> end {endDist:F3} m).");
                }
            }
        }

        // Feature: impactful-weapon-boons, Property 9
        // No teleport / locomotion-only (R5.3): an enemy WITHOUT a SoftGroupingService (no locomotion)
        // is never displaced by the pull — no hard impulse, nothing relocated past scenery. The pull
        // only ever moves enemies through their own locomotion channel.
        // Validates: Requirements 5.3
        [UnityTest]
        public IEnumerator Property9_EnemyWithoutLocomotionIsNeverMoved()
        {
            const int cases = 60;

            for (int c = 0; c < cases; c++)
            {
                var rng = new System.Random(PropertyCheck.DefaultSeed + 53 + c);

                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                PlayerActor owner = _rig.BuildOwner();
                owner.transform.position = Vector3.zero;
                _combat = owner.gameObject.AddComponent<ArsenalCombat>();
                PlayerField.SetValue(_combat, owner);

                int rank = 1 + rng.Next(0, 3);
                float range = 9f + (float)rng.NextDouble() * 3f;
                float width = 1.5f;

                var direction = Vector3.forward;

                // One enemy WITH locomotion (a control that should be pulled) and one WITHOUT (must not move).
                Actor withLoco = _rig.BuildEnemy(direction * 3f, health: 100000f);
                AddLocomotion(withLoco);
                Vector3 withLocoStart = withLoco.transform.position;

                Actor noLoco = _rig.BuildEnemy(direction * 5f, health: 100000f);
                Vector3 noLocoStart = noLoco.transform.position;

                ArsenalCastPlan plan = BuildImpalePlan(range, width, rank);
                ApplyImpalePullMethod.Invoke(_combat, new object[] { owner.transform.position, direction, plan });

                for (int frame = 0; frame < 300; frame++)
                    yield return null;

                // R5.3: the enemy without locomotion is exactly where it started — never teleported.
                Vector3 noLocoMoved = noLoco.transform.position - noLocoStart;
                Assert.AreEqual(0f, noLocoMoved.magnitude, 1e-4f,
                    $"case {c}: an enemy without locomotion must not be displaced by the pull (no teleport).");

                // Sanity: the enemy that HAS locomotion was pulled, so the pull did run this case.
                Vector3 withLocoMoved = withLoco.transform.position - withLocoStart;
                Assert.Greater(withLocoMoved.magnitude, 1e-3f,
                    $"case {c}: the control enemy with locomotion should have been pulled.");
            }
        }
    }
}
