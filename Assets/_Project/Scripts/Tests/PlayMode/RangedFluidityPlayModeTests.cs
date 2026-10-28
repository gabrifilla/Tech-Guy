using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechGuy.Tests
{
    /// <summary>Exercises the same EnemyAI + PreferredDistanceLayer composition as the shipped Shooter.</summary>
    public sealed class RangedFluidityPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private NavMeshData _navigation;
        private NavMeshDataInstance _navigationInstance;
        private static readonly Vector3 Center = new Vector3(4000, 0, 4000);

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in _objects) if (obj) Object.DestroyImmediate(obj);
            _objects.Clear();
            if (_navigationInstance.valid) _navigationInstance.Remove();
            if (_navigation) Object.DestroyImmediate(_navigation);
        }

        private EnemyAI BuildShooter(float distance = 4f, bool disconnected = false, bool restrictedArea = false)
        {
#if UNITY_EDITOR
            var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(40, .2f, 40),
                transform = Matrix4x4.TRS(Center + Vector3.down * .1f, Quaternion.identity, Vector3.one), area = 0 };
            var sources = new List<NavMeshBuildSource> { source };
            if (disconnected)
            {
                sources[0] = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(10, .2f, 20), transform = source.transform, area = 0 };
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(10, .2f, 20),
                    transform = Matrix4x4.TRS(Center + new Vector3(11, -.1f, 0), Quaternion.identity, Vector3.one), area = 0 });
            }
            if (restrictedArea)
            {
                sources[0] = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(10, .2f, 20), transform = source.transform, area = 0 };
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(10, .2f, 20),
                    transform = Matrix4x4.TRS(Center + new Vector3(10, -.1f, 0), Quaternion.identity, Vector3.one), area = 3 });
            }
            _navigation = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                sources, new Bounds(Center, new Vector3(40, 5, 40)),
                Vector3.zero, Quaternion.identity);
            Assert.That(_navigation, Is.Not.Null, "The integration fixture needs a valid NavMesh.");
            _navigationInstance = NavMesh.AddNavMeshData(_navigation);

            var player = new GameObject("Ranged fluidity player");
            _objects.Add(player); player.transform.position = Center; player.layer = 8;
            player.AddComponent<CapsuleCollider>();
            var target = player.AddComponent<Actor>(); target.health = 10000; target.SetMaxHealth(10000);

            var parent = new GameObject("Ranged fluidity encounter");
            _objects.Add(parent); parent.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Shooter.prefab");
            Assert.That(prefab, Is.Not.Null, "The actual Shooter prefab must exist.");
            var shooter = Object.Instantiate(prefab, Center + Vector3.right * distance, Quaternion.LookRotation(Vector3.left), parent.transform);
            var ai = shooter.GetComponent<EnemyAI>();
            // The fixture lives outside the scene camera; animation checks must still sample the real clips.
            shooter.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.That(shooter.GetComponent<PreferredDistanceLayer>(), Is.Not.Null, "The fixture must include the real distance layer.");
            ai.player = player.transform; ai.whatIsPlayer = 1 << 8; ai.sightRange = 100; ai.walkPointRange = 0;
            Set(ai, "_natural", new EnemyAI.NaturalBehaviorConfig { AngularSpeed = 1440, ReactionDelay = 0,
                SightLossReset = 1, ApproachOffset = 0, ApproachRefresh = 1 });
            Set(ai, "_kiting", new EnemyAI.KitingConfig { RetreatSpeedMultiplier = .5f,
                StandoffFraction = .45f, StandoffMargin = 1.15f, RetreatWindow = 1, RetreatCooldown = 2 });
            if (restrictedArea) ai.agent.areaMask = 1;
            parent.SetActive(true);
            Assert.That(ai.agent.Warp(shooter.transform.position), Is.True);
            Physics.SyncTransforms();
            return ai;
#else
            Assert.Ignore("The shipped prefab composition is loaded through AssetDatabase in the Editor.");
            return null;
#endif
        }

        private static void Set(object owner, string name, object value) =>
            owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(owner, value);
        private static T Get<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(owner);

        [UnityTest]
        public IEnumerator RetreatRespectsAgentAreaMaskEvenWhenAnotherAreaIsSampleable()
        {
            var ai = BuildShooter(restrictedArea: true);
            ai.enabled = false;
            yield return null;
            var desired = Center + Vector3.right * 6.21f;
            Assert.That(NavMesh.SamplePosition(desired, out NavMeshHit hit, .5f, NavMesh.AllAreas), Is.True,
                "The excluded destination must exist on the NavMesh for this regression.");
            Assert.That(hit.mask & ai.agent.areaMask, Is.EqualTo(0));
            Set(ai, "nextAttackTime", Time.time + 1000);
            ai.enabled = true;
            yield return null;
            Assert.That(ai.IsRetreating, Is.False);
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(ai.agent.hasPath, Is.False, "The agent must not inherit AllAreas from an unrelated positioning query.");
        }

        [UnityTest]
        public IEnumerator RepeatedIdleFramesDoNotRestartTheExistingAnimation()
        {
            var ai = BuildShooter(7f);
            Set(ai, "nextAttackTime", Time.time + 1000);
            yield return new WaitForSeconds(.15f);
            var animator = ai.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, "The shipped Shooter must exercise its real controller.");
            float before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSeconds(.3f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(before + .03f),
                "Repeated locomotion updates must not reset an unchanged Idle clip to frame zero.");
        }

        [UnityTest]
        public IEnumerator BinaryLocomotionPreservesFlinchUntilTheReactionOwnerReleasesIt()
        {
            var ai = BuildShooter(7f);
            Set(ai, "nextAttackTime", Time.time + 1000);
            yield return null;
            ai.enabled = false; // Isolate the presentation call from targeting/kiting decisions.
            var r = ai.GetComponent<CombatReactionController>() ?? ai.gameObject.AddComponent<CombatReactionController>();
#if UNITY_EDITOR
            r.ConfigureProfile(AssetDatabase.LoadAssetAtPath<EnemyProfile>("Assets/_Project/ScriptableObjects/Enemies/Archetypes/Shooter.asset"));
#endif
            Set(ai, "_reaction", r);
            Set(ai, "_hasMovementBlendParameter", false); // Exercise the existing binary fallback branch.
            var animator = ai.GetComponent<Animator>();
            r.ApplyReaction(new HitReactionRequest(null, ai.transform.position, Vector3.forward,
                HitReactionType.Stagger, HitStrength.Light, 0f, StanceBreakEffect.None, 0f));
            animator.Update(0f); animator.Update(.1f);
            typeof(EnemyAI).GetMethod("SetMovementAnimation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ai, null);
            animator.Update(.1f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), Is.True,
                "The binary fallback must not truncate the owned clip before its end.");
            animator.Update(.8f);
            typeof(CombatReactionController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r, null);
            animator.Update(.15f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), Is.False);
        }

        [UnityTest]
        public IEnumerator RetreatKeepsFacingPlayerAcrossEntryThresholdAndRestoresOnArrival()
        {
            var ai = BuildShooter(5.2f);
            Set(ai, "nextAttackTime", Time.time + 1000);
            yield return null;
            Assert.That(ai.IsRetreating, Is.True);
            float chase = ai.GetComponent<EnemyVariant>().ChaseSpeed;
            Assert.That(ai.agent.speed, Is.EqualTo(chase * .5f).Within(.001f));
            Assert.That(ai.agent.updateRotation, Is.False, "The NavMesh must not turn a backpedaling Shooter away from its target.");
            // Let the live agent cross the entry boundary. Warp is an external navigation reset and
            // legitimately ends a held retreat, so it cannot model continuous threshold crossing.
            float crossingDeadline = Time.time + .75f;
            while (Vector3.Distance(ai.transform.position, ai.player.position) < 5.55f && Time.time < crossingDeadline)
            {
                Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Kiting));
                Assert.That(ai.agent.pathPending || ai.agent.hasPath, Is.True,
                    $"Threshold crossing requires the original live retreat route. distance={Vector3.Distance(ai.transform.position, ai.player.position):F4}, " +
                    $"destination={ai.agent.destination}, heldDestination={Get<Vector3>(ai, "_retreatDestination")}, " +
                    $"remaining={ai.agent.remainingDistance:F4}, stopped={ai.agent.isStopped}, stopDistance={ai.agent.stoppingDistance:F3}, " +
                    $"velocity={ai.agent.velocity}, status={ai.agent.pathStatus}, phase={ai.RetreatPhase}, " +
                    $"frame={Time.frameCount}, rootMotion={ai.GetComponent<Animator>().applyRootMotion}");
                yield return null;
            }
            Assert.That(Vector3.Distance(ai.transform.position, ai.player.position), Is.GreaterThan(5.4f),
                "The real navigation must cross the entry threshold before testing hysteresis.");
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Kiting),
                "Crossing the entry threshold must not end retreat before the hysteresis exit distance.");
            Vector3 direction = ai.player.position - ai.transform.position; direction.y = 0;
            Assert.That(Vector3.Angle(ai.transform.forward, direction), Is.LessThan(30));
            Assert.That(ai.GetComponent<Animator>().GetFloat("Speed"), Is.EqualTo(.5f).Within(.1f),
                "Half-speed backpedal must use the chase-speed gait reference and blend toward Walk, not Run.");
            Time.timeScale = 0;
            var pausedPosition = ai.transform.position;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Kiting));
            Assert.That(Vector3.Distance(pausedPosition, ai.transform.position), Is.LessThan(.03f));
            Time.timeScale = 1;
            float arrivalDeadline = Time.time + 1.25f;
            while (ai.IsRetreating && Time.time < arrivalDeadline) yield return null;
            Assert.That(Vector3.Distance(ai.transform.position, ai.player.position), Is.GreaterThan(6.01f),
                "The integration must recover the radial distance through navigation rather than teleporting to the exit.");
            Assert.That(ai.IsRetreating, Is.False);
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(ai.agent.speed, Is.EqualTo(chase).Within(.001f));
            Assert.That(ai.agent.updateRotation, Is.True);
            Assert.That(ai.agent.hasPath, Is.False);
        }

        [UnityTest]
        public IEnumerator DisconnectedRetreatDestinationClearsPreviousPathAndStartsCooldown()
        {
            var ai = BuildShooter(4f, disconnected: true);
            ai.enabled = false;
            yield return null;
            ai.agent.SetDestination(Center + Vector3.left * 2);
            yield return null;
            Assert.That(ai.agent.hasPath, Is.True, "The regression needs an existing valid path to clear.");
            ai.enabled = true;
            Set(ai, "nextAttackTime", Time.time + 1000);
            yield return null;
            Assert.That(ai.IsRetreating, Is.False, "Sampling a disconnected island does not make it reachable.");
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(ai.agent.hasPath, Is.False, "A failed retreat must cancel the earlier path.");
        }

        [UnityTest]
        public IEnumerator RetreatNeverLeavesItsConfiguredEncounter()
        {
            var ai = BuildShooter();
            ai.ConfigureEncounterBounds(Center, new Vector2(10, 20));
            Set(ai, "nextAttackTime", Time.time + 1000);
            yield return null;
            Assert.That(ai.IsRetreating, Is.False, "A reachable point outside the encounter must still be rejected.");
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(ai.agent.hasPath, Is.False);
            Assert.That(Vector3.Distance(ai.transform.position, Center + Vector3.right * 4), Is.LessThan(.1f));
        }

        [UnityTest]
        public IEnumerator CooldownAdvancesDuringAttackRecoveryAndDisableCleansRetreat()
        {
            var ai = BuildShooter();
            yield return null;
            ai.InterruptAttack();
            var kite = Get<KiteController>(ai, "_kite");
            kite.EndRetreat();
            Set(ai, "nextAttackTime", Time.time + 1000);
            var config = Get<EnemyAI.KitingConfig>(ai, "_kiting"); config.RetreatCooldown = .15f;
            Set(ai, "_kiting", config);
            ai.agent.Warp(Center + Vector3.right * 7);
            Set(ai, "nextAttackTime", 0f);
            yield return null;
            Assert.That(Get<Coroutine>(ai, "_attackRoutine"), Is.Not.Null, "The cooldown must overlap a real attack execution.");
            yield return new WaitForSeconds(.25f);
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Eligible),
                "Distance changes and recovery waiting must not freeze the gameplay cooldown clock.");
            ai.InterruptAttack();
            Set(ai, "nextAttackTime", Time.time + 1000);
            ai.agent.Warp(Center + Vector3.right * 4);
            yield return null;
            Assert.That(ai.IsRetreating, Is.True);
            ai.enabled = false;
            Assert.That(ai.IsRetreating, Is.False);
            Assert.That(ai.RetreatPhase, Is.EqualTo(KiteController.Phase.Eligible));
            Assert.That(ai.agent.updateRotation, Is.True);
            Assert.That(ai.agent.hasPath, Is.False);
        }

        [UnityTest]
        public IEnumerator ShooterDistanceLayerCannotRecreateRetreatDuringKiteCooldown()
        {
            var ai = BuildShooter();
            yield return null; // Variant.Start resolves the prefab profile and chase speed.
            ai.InterruptAttack();
            Set(ai, "nextAttackTime", Time.time + 1000);
            var kite = Get<KiteController>(ai, "_kite"); kite.Reset();
            Assert.That(kite.Tick(true, 0, 1, 2), Is.True);
            Assert.That(kite.Tick(true, 1, 1, 2), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            ai.agent.ResetPath();
            var initial = ai.transform.position;
            var deadline = Time.time + .35f;
            while (Time.time < deadline)
            {
                yield return null;
                Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
                if (ai.agent.hasPath)
                    Assert.That(Vector3.Distance(ai.agent.destination, ai.player.position),
                        Is.LessThanOrEqualTo(Vector3.Distance(initial, ai.player.position) + .1f),
                        "A second movement writer must not send the Shooter toward 8m during retreat cooldown.");
            }
            Assert.That(Vector3.Distance(ai.transform.position, initial), Is.LessThan(.1f),
                "Cooldown must give melee a real opening instead of another layer extending the escape.");
        }
    }
}
