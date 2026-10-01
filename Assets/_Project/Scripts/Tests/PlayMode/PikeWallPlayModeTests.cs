using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode test for Property 18 of gauntlet-boon-playstyle-overhaul (locomotion half) — the
    /// Spear's "Muralha de hastes" (<c>PikeWall</c>, R12) control zone.
    ///
    /// Feature: gauntlet-boon-playstyle-overhaul, Property 18
    /// Property 18 (locomotion/scenery half): for any PikeWall rank R &gt;= 1, each enemy caught by the
    /// W sweep's control zone and carrying a <see cref="SoftGroupingService"/> SHALL be pushed OUTWARD
    /// from the zone centre through that locomotion channel (never a hard impulse / teleport), and the
    /// push SHALL never move an enemy through solid scenery.
    /// Validates: Requirements 12.3
    ///
    /// These are locomotion- and physics-dependent, so they run in PlayMode and drive the exact
    /// production method <c>ArsenalCombat.ApplyZonePush</c> (which searches enemies in the zone and
    /// glides each with a <see cref="SoftGroupingService"/> OUTWARD from the centre via
    /// <c>SoftGroupingService.ApplyExternalDisplacement</c>). The private method and the private
    /// <c>_player</c> field are reached by reflection, following the same seam-driving approach as
    /// <see cref="ImpalingLinePierceAndPullTests"/> — the spec forbids widening gameplay APIs for tests.
    /// </summary>
    public sealed class PikeWallPlayModeTests
    {
        private const float PushPerRank = 0.3f;                 // R12.2: plan.ZonePush = 0.3 * rank
        private const float MaxTotal = SpearSweepDisplacement.MaxTotalDisplacement; // 0.75 m baseline budget
        private const float Tolerance = 0.05f;                  // per-frame glide slack

        private static readonly MethodInfo ApplyZonePushMethod = typeof(ArsenalCombat).GetMethod(
            "ApplyZonePush", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo PlayerField = typeof(ArsenalCombat).GetField(
            "_player", BindingFlags.Instance | BindingFlags.NonPublic);

        private ArsenalProjectileTestRig _rig;
        private ArsenalCombat _combat;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(ApplyZonePushMethod, "Expected private ArsenalCombat.ApplyZonePush (test seam).");
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

        // Builds an ArsenalCastPlan snapshot for a PikeWall control-zone sweep at the given rank, driven
        // entirely off value-type plan fields (R12.4 — the source ability asset is never mutated).
        private static ArsenalCastPlan BuildZonePlan(float width, int rank)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            var plan = new ArsenalCastPlan(ability)
            {
                Width = width,
                ControlZone = rank > 0,          // R12.1
                ZonePush = PushPerRank * rank    // R12.2 (0.3 m per rank)
            };
            Object.DestroyImmediate(ability); // plan copied by value; the asset is no longer needed
            return plan;
        }

        // Adds a SoftGroupingService "locomotion" to an enemy. With no NavMeshAgent / CharacterController
        // present it translates the transform directly, so the glide is deterministic in the test scene
        // while still exercising the real locomotion channel the push routes through (R12.3).
        private static void AddLocomotion(Actor enemy) => enemy.gameObject.AddComponent<SoftGroupingService>();

        // Adds a CharacterController-backed SoftGroupingService so ApplyExternalDisplacement moves the
        // enemy through CharacterController.Move, which collides against solid scenery (R12.3 — the push
        // can never carry an enemy through a wall).
        private static CharacterController AddCollidingLocomotion(Actor enemy, float radius = 0.5f, float height = 2f)
        {
            var cc = enemy.gameObject.AddComponent<CharacterController>();
            cc.radius = radius;
            cc.height = height;
            cc.center = new Vector3(0f, height * 0.5f, 0f);
            enemy.gameObject.AddComponent<SoftGroupingService>(); // auto-resolves the CharacterController in Awake
            return cc;
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 18
        // Caught enemies WITH locomotion are pushed OUTWARD from the zone centre, by at most the
        // rank-scaled budget 0.75 * (1 + 0.3R) metres, through SoftGroupingService — never inward.
        // Validates: Requirements 12.3
        [UnityTest]
        public IEnumerator Property18_CaughtEnemiesArePushedOutwardWithinRankBudget()
        {
            const int cases = 40; // each case spends multiple frames letting the glide coroutines run

            for (int c = 0; c < cases; c++)
            {
                var rng = new System.Random(PropertyCheck.DefaultSeed + 71 + c);

                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                PlayerActor owner = _rig.BuildOwner();
                owner.transform.position = Vector3.zero;
                _combat = owner.gameObject.AddComponent<ArsenalCombat>();
                PlayerField.SetValue(_combat, owner);

                int rank = 1 + rng.Next(0, 3);                 // ranks 1..3
                float budget = MaxTotal * (1f + PushPerRank * rank);
                float width = 3f + (float)rng.NextDouble() * 2f; // 3..5 m zone radius
                int count = 2 + rng.Next(0, 3);                // 2..4 enemies in the zone

                Vector3 center = Vector3.zero;
                var enemies = new List<Actor>();
                var startPositions = new List<Vector3>();
                for (int i = 0; i < count; i++)
                {
                    // Spread enemies around the centre, well inside the zone radius and clear of each
                    // other so each has a stable outward heading.
                    float angle = i / (float)count * Mathf.PI * 2f;
                    float dist = 0.6f + (float)rng.NextDouble() * (width - 1f);
                    Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
                    Actor enemy = _rig.BuildEnemy(pos, health: 100000f);
                    AddLocomotion(enemy);
                    enemies.Add(enemy);
                    startPositions.Add(enemy.transform.position);
                }

                ArsenalCastPlan plan = BuildZonePlan(width, rank);

                // Invoke the real production push: centre = zone centre, plan carries ControlZone/ZonePush.
                ApplyZonePushMethod.Invoke(_combat, new object[] { center, plan });

                // Let the glide coroutines run to completion (budget at a capped speed over a few frames).
                Vector3[] previous = new Vector3[count];
                for (int frame = 0; frame < 400; frame++)
                {
                    for (int i = 0; i < count; i++) previous[i] = enemies[i].transform.position;
                    yield return null;

                    bool anyMoving = false;
                    for (int i = 0; i < count; i++)
                        if ((enemies[i].transform.position - previous[i]).sqrMagnitude > 1e-8f) { anyMoving = true; break; }
                    if (!anyMoving && frame > 2) break;
                }

                for (int i = 0; i < count; i++)
                {
                    Vector3 start = startPositions[i];
                    Vector3 end = enemies[i].transform.position;
                    Vector3 moved = end - start; moved.y = 0f;

                    // The enemy actually moved (a caught enemy with locomotion is pushed).
                    Assert.Greater(moved.magnitude, 1e-3f,
                        $"case {c}: rank {rank}, enemy {i} with locomotion was not pushed at all.");

                    // Within the rank-scaled budget (small per-frame slack).
                    Assert.LessOrEqual(moved.magnitude, budget + Tolerance,
                        $"case {c}: rank {rank}, enemy {i} pushed {moved.magnitude:F3} m, exceeds budget {budget:F3} m.");

                    // Outward: farther from the centre than it started.
                    float startDist = new Vector2(start.x, start.z).magnitude;
                    float endDist = new Vector2(end.x, end.z).magnitude;
                    Assert.Greater(endDist, startDist - Tolerance,
                        $"case {c}: rank {rank}, enemy {i} was pushed inward (start {startDist:F3} m -> end {endDist:F3} m).");

                    // The displacement vector points outward (positive projection on the start heading).
                    Vector3 outward = new Vector3(start.x - center.x, 0f, start.z - center.z);
                    if (outward.sqrMagnitude > 1e-6f)
                        Assert.Greater(Vector3.Dot(moved, outward.normalized), -Tolerance,
                            $"case {c}: rank {rank}, enemy {i} push did not point outward.");
                }
            }
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 18
        // No teleport / locomotion-only (R12.3): an enemy WITHOUT a SoftGroupingService is never
        // displaced by the zone push — no hard impulse, nothing relocated.
        // Validates: Requirements 12.3
        [UnityTest]
        public IEnumerator Property18_EnemyWithoutLocomotionIsNeverPushed()
        {
            const int cases = 40;

            for (int c = 0; c < cases; c++)
            {
                var rng = new System.Random(PropertyCheck.DefaultSeed + 72 + c);

                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                PlayerActor owner = _rig.BuildOwner();
                owner.transform.position = Vector3.zero;
                _combat = owner.gameObject.AddComponent<ArsenalCombat>();
                PlayerField.SetValue(_combat, owner);

                int rank = 1 + rng.Next(0, 3);
                float width = 4f;

                // One enemy WITH locomotion (a control that should be pushed) and one WITHOUT (must not move).
                Actor withLoco = _rig.BuildEnemy(new Vector3(1.5f, 0f, 0f), health: 100000f);
                AddLocomotion(withLoco);
                Vector3 withLocoStart = withLoco.transform.position;

                Actor noLoco = _rig.BuildEnemy(new Vector3(0f, 0f, 1.5f), health: 100000f);
                Vector3 noLocoStart = noLoco.transform.position;

                ArsenalCastPlan plan = BuildZonePlan(width, rank);
                ApplyZonePushMethod.Invoke(_combat, new object[] { Vector3.zero, plan });

                for (int frame = 0; frame < 200; frame++)
                    yield return null;

                // R12.3: the enemy without locomotion is exactly where it started — never teleported.
                Vector3 noLocoMoved = noLoco.transform.position - noLocoStart;
                Assert.AreEqual(0f, noLocoMoved.magnitude, 1e-4f,
                    $"case {c}: an enemy without locomotion must not be displaced by the zone (no teleport).");

                // Sanity: the enemy that HAS locomotion was pushed, so the zone did run this case.
                Vector3 withLocoMoved = withLoco.transform.position - withLocoStart;
                Assert.Greater(withLocoMoved.magnitude, 1e-3f,
                    $"case {c}: the control enemy with locomotion should have been pushed.");
            }
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 18
        // Never through solid scenery (R12.3): when the outward push would drive a CharacterController-
        // backed enemy into a wall, CharacterController.Move collides and the enemy stops at the wall
        // instead of passing through it.
        // Validates: Requirements 12.3
        [UnityTest]
        public IEnumerator Property18_PushNeverMovesEnemyThroughSolidScenery()
        {
            // A caught enemy sits between the centre and a solid wall placed just outside it, so the
            // outward push heads straight into the wall. A CharacterController routes the glide, so the
            // enemy must stop at the wall face — never tunnel to the far side.
            PlayerActor owner = _rig.BuildOwner();
            owner.transform.position = Vector3.zero;
            _combat = owner.gameObject.AddComponent<ArsenalCombat>();
            PlayerField.SetValue(_combat, owner);

            const int rank = 3; // strongest push, so the budget would clearly cross a nearby wall
            const float width = 4f;

            Vector3 center = Vector3.zero;
            Vector3 enemyStart = new Vector3(1.5f, 0f, 0f);
            Actor enemy = _rig.BuildEnemy(enemyStart, health: 100000f);
            CharacterController cc = AddCollidingLocomotion(enemy);

            // Wall face sits a little beyond the enemy's capsule edge, inside the push budget's reach,
            // so an un-colliding push would cross it. The enemy must be stopped by the wall instead.
            float wallFaceX = enemyStart.x + cc.radius + 0.15f;
            float wallThickness = 2f;
            float wallCenterX = wallFaceX + wallThickness * 0.5f;
            _rig.BuildWall(new Vector3(wallCenterX, 1f, 0f), new Vector3(wallThickness, 4f, 10f));

            ArsenalCastPlan plan = BuildZonePlan(width, rank);
            ApplyZonePushMethod.Invoke(_combat, new object[] { center, plan });

            for (int frame = 0; frame < 400; frame++)
                yield return null;

            // The enemy must stay on the near side of the wall face (capsule edge never crosses it).
            float enemyEdgeX = enemy.transform.position.x + cc.radius;
            Assert.LessOrEqual(enemyEdgeX, wallFaceX + Tolerance,
                $"the outward push drove the enemy through solid scenery " +
                $"(edge {enemyEdgeX:F3} past wall face {wallFaceX:F3}).");
        }
    }
}
