using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class PostureReactionPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private float _scale;
        [SetUp] public void SetUp() { _scale = Time.timeScale; Time.timeScale = 1f; }
        [TearDown] public void TearDown()
        {
            foreach (var obj in _objects) if (obj) Object.DestroyImmediate(obj);
            _objects.Clear(); Time.timeScale = _scale;
        }
        private CombatReactionController Build(EnemyRank rank = EnemyRank.Normal, ReactionCategory category = ReactionCategory.SmallMob)
        {
            var go = new GameObject("Posture test target"); go.SetActive(false); _objects.Add(go);
            var actor = go.AddComponent<Actor>(); actor.health = 100f;
            var visual = new GameObject("Visual"); visual.transform.SetParent(go.transform);
            var reaction = go.AddComponent<CombatReactionController>();
            typeof(CombatReactionController).GetField("visualRoot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(reaction, visual.transform);
            go.SetActive(true);
            reaction.ConfigureStance(rank, 10f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            reaction.ConfigureReaction(new ReactionConfiguration(rank, category));
            return reaction;
        }
        private static HitReactionRequest Hit(float damage, StanceBreakEffect effect = StanceBreakEffect.None, HitReactionType type = HitReactionType.None, float duration = 0.3f)
            => new HitReactionRequest(null, Vector3.up, Vector3.forward, type, HitStrength.Light, damage, effect, 5f, duration, 1f, 2f);

        [UnityTest]
        public IEnumerator RepeatedFlinchRestartsAtEachContactWithoutAddingControlOrDisplacement()
        {
#if UNITY_EDITOR
            // One real common-enemy model/controller, isolated from navigation and AI for this
            // presentation regression. The gameplay captures use the untouched production encounter.
            var parent = new GameObject("Repeated flinch fixture"); parent.SetActive(false); _objects.Add(parent);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab");
            Assert.IsNotNull(prefab);
            var enemy = Object.Instantiate(prefab, parent.transform);
            enemy.GetComponent<EnemyAI>().enabled = false;
            enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            var animator = enemy.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            parent.SetActive(true);
            var reaction = enemy.GetComponent<CombatReactionController>() ?? enemy.AddComponent<CombatReactionController>();
            reaction.ConfigureProfile(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyProfile>("Assets/_Project/ScriptableObjects/Enemies/Archetypes/Grunt.asset"));
            animator.Update(0f);
            var actor = enemy.GetComponent<Actor>();
            float health = actor.health, stance = reaction.CurrentStance;
            Vector3 position = enemy.transform.position;
            var contact = new HitReactionRequest(null, position, Vector3.forward,
                HitReactionType.Stagger, HitStrength.Light, 0f, StanceBreakEffect.None, 0f);
            reaction.ApplyReaction(contact); animator.Update(0f); animator.Update(.25f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"));
            Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, .2f);
            reaction.ApplyReaction(contact); animator.Update(0f);
            Assert.Less(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, .01f,
                "A new contact must restart GetHit even when GetHit is already the current state.");
            Assert.AreEqual(health, actor.health); Assert.AreEqual(stance, reaction.CurrentStance);
            Assert.IsFalse(reaction.IsControlLocked); Assert.IsFalse(reaction.IsAirborne);
            Assert.AreEqual(position, enemy.transform.position);
            animator.Update(.2f);
            float beforeNone = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            reaction.ApplyReaction(Hit(0f)); animator.Update(0f);
            Assert.AreEqual(beforeNone, animator.GetCurrentAnimatorStateInfo(0).normalizedTime, .001f,
                "A None reaction must not manufacture a new flinch.");
            yield return null;
#else
            Assert.Ignore("This regression uses the shipped Grunt prefab through AssetDatabase in the Editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        private CombatReactionController BuildFlinchFixture(out Animator animator)
        {
            var parent = new GameObject("Flinch recovery fixture"); parent.SetActive(false); _objects.Add(parent);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab");
            var enemy = Object.Instantiate(prefab, parent.transform);
            enemy.GetComponent<EnemyAI>().enabled = false;
            // The fixture applies its profile explicitly; do not let Start reconfigure ownership later.
            enemy.GetComponent<EnemyVariant>().enabled = false;
            enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            animator = enemy.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            parent.SetActive(true);
            var r = enemy.GetComponent<CombatReactionController>() ?? enemy.AddComponent<CombatReactionController>();
            r.ConfigureProfile(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyProfile>("Assets/_Project/ScriptableObjects/Enemies/Archetypes/Grunt.asset"));
            animator.Update(0f);
            return r;
        }
        private static void Flinch(CombatReactionController r, Animator animator)
        {
            r.ApplyReaction(new HitReactionRequest(null, r.transform.position, Vector3.forward,
                HitReactionType.Stagger, HitStrength.Light, 0f, StanceBreakEffect.None, 0f));
            animator.Update(0f);
        }
        private static void EvaluatePresentation(CombatReactionController r, Animator animator)
        {
            typeof(CombatReactionController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r, null);
            animator.Update(.15f); // Complete the existing locomotion blend if one was requested.
        }
        [UnityTest] public IEnumerator FinishedFlinchReturnsToLocomotionWithoutChangingMechanics()
        {
            var r = BuildFlinchFixture(out var animator);
            float health = r.GetComponent<Actor>().health, stance = r.CurrentStance;
            Flinch(r, animator); animator.Update(.8f);
            Assert.GreaterOrEqual(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
            EvaluatePresentation(r, animator);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "A completed owned GetHit must release to locomotion.");
            Assert.AreEqual(health, r.GetComponent<Actor>().health); Assert.AreEqual(stance, r.CurrentStance);
            Assert.IsFalse(r.IsControlLocked); Assert.IsFalse(r.IsAirborne);
            yield return null;
        }
        [UnityTest] public IEnumerator AnotherImpactRestartsFlinchAndDefersItsVisualRecovery()
        {
            var r = BuildFlinchFixture(out var animator);
            Flinch(r, animator); animator.Update(.5f); Flinch(r, animator);
            animator.Update(.2f); EvaluatePresentation(r, animator);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), "The new contact owns a fresh full clip.");
            animator.Update(.5f); EvaluatePresentation(r, animator);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            yield return null;
        }
        [UnityTest] public IEnumerator FlinchRecoveryDoesNotOverwriteExternalAttackOrSurviveDisable()
        {
            var r = BuildFlinchFixture(out var animator);
            Flinch(r, animator); animator.Play("Attack", 0, .1f); animator.Update(0f);
            EvaluatePresentation(r, animator);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
            r.enabled = false; r.enabled = true;
            animator.Play("GetHit", 0, 1.1f); animator.Update(0f); EvaluatePresentation(r, animator);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), "Disabled ownership must not leak into a later external state.");
            yield return null;
        }
        [UnityTest] public IEnumerator PauseAndHitstopDoNotReleaseACompletedFlinchEarly()
        {
            var r = BuildFlinchFixture(out var animator);
            Flinch(r, animator); animator.Update(.8f); Time.timeScale = 0f;
            EvaluatePresentation(r, animator); Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"));
            Time.timeScale = 1f;
            var source = new GameObject("Presentation hitstop clock"); _objects.Add(source);
            var runner = source.AddComponent<HitStopRunner>(); r.BindGameplayClock(runner); runner.Apply(.15f);
            EvaluatePresentation(r, animator); Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"));
            yield return new WaitForSecondsRealtime(.25f);
            EvaluatePresentation(r, animator); Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
        }
        [UnityTest] public IEnumerator ActiveWindupKeepsPriorityWhenTheHitCannotInterruptIt()
        {
            var r = BuildFlinchFixture(out var animator);
            var targetObject = new GameObject("Windup target"); _objects.Add(targetObject);
            var target = targetObject.AddComponent<Actor>(); target.health = 100f;
            target.transform.position = r.transform.position + Vector3.forward;
            var actions = r.GetComponent<EnemyCombatActions>() ?? r.gameObject.AddComponent<EnemyCombatActions>();
            r.StartCoroutine(actions.Perform(EnemyAttackKind.Punch, target, 5f, () => true));
            yield return null; Assert.IsTrue(actions.IsWindingUp);
            r.ApplyReaction(new HitReactionRequest(null, Vector3.zero, Vector3.forward,
                HitReactionType.Stagger, HitStrength.Light, 0f, StanceBreakEffect.None, 0f).WithInterruptContribution(0f));
            animator.Update(0f); EvaluatePresentation(r, animator);
            Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), "A non-interrupting flinch must not steal the attack presentation.");
            Assert.IsTrue(actions.IsWindingUp); actions.Cancel();
        }
        [UnityTest] public IEnumerator ControlAndRankFallbacksKeepPriorityOverFlinchRecovery()
        {
            foreach (var rank in new[] { EnemyRank.Normal, EnemyRank.Elite, EnemyRank.Boss })
            foreach (var effect in new[] { StanceBreakEffect.Stun, StanceBreakEffect.KnockUp, StanceBreakEffect.Knockdown })
            {
                var r = BuildFlinchFixture(out var animator);
                r.ConfigureStance(rank, 10f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
                r.ConfigureReaction(new ReactionConfiguration(rank, rank == EnemyRank.Boss ? ReactionCategory.Boss : ReactionCategory.SmallMob,
                    vulnerabilitySeconds:.1f, breakImmunitySeconds:.1f));
                Flinch(r, animator); r.ApplyReaction(Hit(10f, effect, duration:.2f));
                Assert.IsTrue(r.IsControlLocked, rank + "/" + effect);
                animator.Update(.8f); EvaluatePresentation(r, animator);
                Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Control owns presentation until release: " + rank + "/" + effect);
                yield return new WaitForSeconds(1.5f);
                EvaluatePresentation(r, animator);
                Assert.IsFalse(r.IsControlLocked); Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
                r.gameObject.SetActive(false);
            }
        }
        [UnityTest] public IEnumerator DeathDuringFlinchCannotRestoreLocomotion()
        {
            var r = BuildFlinchFixture(out var animator); Flinch(r, animator); animator.Update(.8f);
            r.GetComponent<Actor>().TakeDamage(9999f);
            animator.Play("Die", 0, 0f); animator.Update(0f); EvaluatePresentation(r, animator);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Die"));
            yield return null;
        }
#endif

        // Feature: posture-enemy-interaction, Property 5: callbacks observe committed state.
        // Validates: Requirements 4.1, 4.4, 4.5, 4.6, 7.2
        [UnityTest] public IEnumerator NoneBreakIsSameStepAndReentrantSafe()
        {
            var r = Build(); int count = 0;
            r.StanceBroken += point =>
            {
                count++; Assert.AreEqual(10f, r.CurrentStance); Assert.IsTrue(r.IsVulnerable);
                Assert.IsTrue(r.IsStunned); r.ApplyReaction(Hit(100f));
            };
            r.ApplyReaction(Hit(10f)); Assert.AreEqual(1, count);
            Assert.AreEqual(EnemyRank.Normal, r.Rank);
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(r.IsStunned);
        }

        [UnityTest] public IEnumerator ImmuneBossOpeningLocksWithoutPhysicalStunAndProtectsOneAction()
        {
            var r = Build(EnemyRank.Boss);
            r.ConfigureStance(EnemyRank.Boss, 10f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
            r.ConfigureReaction(new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob,
                vulnerabilitySeconds: 0.2f, breakImmunitySeconds: 0.1f));
            int count = 0; r.StanceBroken += _ => count++;
            r.ApplyReaction(Hit(10f, StanceBreakEffect.KnockUp));
            Assert.IsTrue(r.IsControlLocked); Assert.IsFalse(r.IsStunned);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(r.IsControlLocked); Assert.IsTrue(r.IsResumeProtected);
            r.BeginOffensiveAction(17);
            for (int i = 0; i < 12; i++) r.ApplyReaction(Hit(100f, type: HitReactionType.Stagger));
            Assert.AreEqual(1, count); Assert.AreEqual(10f, r.CurrentStance);
            r.EndOffensiveAction(16); Assert.IsTrue(r.IsResumeProtected);
            r.EndOffensiveAction(17); Assert.IsFalse(r.IsResumeProtected);
            r.ApplyReaction(Hit(10f)); Assert.AreEqual(2, count);
        }

        // Feature: posture-enemy-interaction, Property 2: actual Rigidbody push stays bounded.
        // Validates: Requirements 1.3, 1.7, 6.6, 6.7
        [UnityTest] public IEnumerator PushIsBoundedAndNoneZeroDoesNotMove()
        {
            var r = Build(); var body = r.gameObject.AddComponent<Rigidbody>(); body.useGravity = false;
            typeof(CombatReactionController).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(r, body);
            Vector3 start = r.transform.position;
            r.ApplyReaction(Hit(0f)); yield return null;
            Assert.AreEqual(start, r.transform.position); Assert.IsFalse(r.IsControlLocked);
            r.ApplyReaction(Hit(0f, type:HitReactionType.Push));
            yield return new WaitForSeconds(0.25f);
            Assert.That(Vector3.Distance(start, r.transform.position), Is.InRange(0.01f, 0.5001f));
            Assert.IsFalse(r.IsControlLocked);
        }

        [UnityTest] public IEnumerator KnockdownStaysGroundedAndDisableRestoresOnlyOwnedPose()
        {
            var r = Build(category: ReactionCategory.MediumMob); var visual = r.transform.GetChild(0);
            Vector3 position = visual.localPosition; Quaternion rotation = visual.localRotation; Vector3 scale = visual.localScale;
            r.ApplyReaction(Hit(10f)); Assert.AreEqual(KnockdownPhase.Falling, r.KnockdownPhase);
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(KnockdownPhase.Grounded, r.KnockdownPhase);
            // The reaction clock advances in Update; sample the owned pose after its LateUpdate.
            yield return null;
            Assert.AreEqual(position.y, visual.localPosition.y);
            Assert.Greater(Quaternion.Angle(rotation, visual.localRotation), 45f);
            r.gameObject.SetActive(false);
            Assert.AreEqual(position, visual.localPosition); Assert.Less(Quaternion.Angle(rotation, visual.localRotation), 0.01f);
            Assert.AreEqual(scale, visual.localScale);
            r.gameObject.SetActive(true); Assert.IsFalse(r.IsControlLocked);
            yield return new WaitForSeconds(0.2f); Assert.AreEqual(position, visual.localPosition);
        }

        [UnityTest] public IEnumerator PauseAndInjectedHitStopFreezeLocalGameplayClock()
        {
            var r = Build(); var source = new GameObject("Clock"); _objects.Add(source);
            var runner = source.AddComponent<HitStopRunner>(); r.BindGameplayClock(runner);
            r.ApplyReaction(Hit(10f, StanceBreakEffect.Stun));
            float now = r.GameplayTime; Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.1f); Assert.AreEqual(now, r.GameplayTime);
            Time.timeScale = 0.5f; runner.Apply(0.15f);
            yield return new WaitForSecondsRealtime(0.05f); Assert.IsTrue(runner.IsActive); Assert.AreEqual(now, r.GameplayTime);
            yield return new WaitForSecondsRealtime(0.2f); Assert.AreEqual(0.5f, Time.timeScale);
            Assert.Greater(r.GameplayTime, now);
        }

        [UnityTest] public IEnumerator ObstructedLaunchRechecksStunImmunityWithoutCreatingLock()
        {
            var r = Build(); r.ConfigureStance(EnemyRank.Normal, 10f, 1f, 0f, 0f, 0f, 1f, 0f, 0f);
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube); _objects.Add(ceiling);
            ceiling.transform.position = Vector3.up * 1.4f; ceiling.transform.localScale = new Vector3(5f, 0.2f, 5f);
            Physics.SyncTransforms();
            r.ApplyReaction(Hit(10f, StanceBreakEffect.KnockUp));
            Assert.IsTrue(r.IsVulnerable); Assert.IsFalse(r.IsControlLocked); Assert.IsFalse(r.IsStunned);
            yield return null; Assert.AreEqual(0f, r.transform.GetChild(0).localPosition.y);
        }

        [UnityTest] public IEnumerator MissingProfileWarnsOnceAtConfigurationAndNotOnHits()
        {
            var r = Build();
            LogAssert.Expect(LogType.Warning, "RankReactionDefaults.MissingProfile.Normal: using complete reaction defaults.");
            r.ConfigureProfile(null); r.ConfigureProfile(null);
            for (int i = 0; i < 10; i++) r.ApplyReaction(Hit(0f));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SpawnProfileIsAppliedBeforeFirstHitAndStartDoesNotResetIt()
        {
            var r = Build(); r.gameObject.SetActive(false);
            var targetGo = new GameObject("Early spawn target"); _objects.Add(targetGo);
            var target = targetGo.AddComponent<Actor>();
            var agent = r.gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.enabled = false;
            var ai = r.gameObject.AddComponent<EnemyAI>(); ai.player = target.transform; ai.enabled = false;
            var variant = r.gameObject.AddComponent<EnemyVariant>();
            // Heavy is outside Resources; obtain the authored asset through the Editor only in this test.
#if UNITY_EDITOR
            variant.Configure(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyProfile>("Assets/_Project/ScriptableObjects/Enemies/Archetypes/Heavy.asset"));
#endif
            r.gameObject.SetActive(true);
            Assert.AreEqual(ReactionCategory.MediumMob, r.Category); Assert.AreEqual(220f, r.MaxStance);
            r.ApplyReaction(Hit(12f)); float afterHit = r.CurrentStance;
            Assert.Less(afterHit, r.MaxStance);
            yield return null;
            Assert.AreEqual(afterHit, r.CurrentStance, "Start must preserve posture already applied on the spawn frame");
            Assert.AreEqual(EnemyRank.Normal, r.Rank);
        }

        [UnityTest] public IEnumerator PushAndBelowThresholdPreserveWindupButEligibleStaggerCancelsPendingBeat()
        {
            var r = Build(category: ReactionCategory.MediumMob);
            r.gameObject.SetActive(false);
            var targetGo = new GameObject("Attack target"); targetGo.SetActive(false); _objects.Add(targetGo);
            var target = targetGo.AddComponent<Actor>(); target.health = 100f; targetGo.SetActive(true);
            target.transform.position = Vector3.forward;
            var agent = r.gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.enabled = false;
            var ai = r.gameObject.AddComponent<EnemyAI>(); ai.player = target.transform; ai.enabled = false;
            r.gameObject.SetActive(true);
            typeof(CombatReactionController).GetField("_enemyAI", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(r, ai);
            var actions = r.GetComponent<EnemyCombatActions>();
            r.StartCoroutine(actions.Perform(EnemyAttackKind.Punch, target, 5f, () => !r.IsControlLocked));
            yield return null;
            Assert.IsTrue(ai.IsWindingUp);
            r.ApplyReaction(Hit(0f, type:HitReactionType.Push)); Assert.IsTrue(ai.IsWindingUp);
            r.ApplyReaction(Hit(0f, type:HitReactionType.Stagger).WithInterruptContribution(0f));
            Assert.IsTrue(ai.IsWindingUp, "zero contribution cannot cancel a windup");
            var decision = r.ApplyInterruptStagger(Hit(1f, type:HitReactionType.Stagger), 0.1f);
            Assert.IsFalse(decision.CancelsAttack); Assert.IsTrue(ai.IsWindingUp); Assert.AreEqual(9f, r.CurrentStance);
            decision = r.ApplyInterruptStagger(Hit(1f, type:HitReactionType.Stagger), 1f);
            Assert.IsTrue(decision.CancelsAttack); Assert.IsFalse(ai.IsWindingUp); Assert.AreEqual(8f, r.CurrentStance);
            yield return new WaitForSeconds(0.9f); Assert.AreEqual(0, actions.ResolvedImpacts);
            Assert.AreEqual(100f, target.health);
        }

        [UnityTest] public IEnumerator PresentationUsesRealTimeAndRestoresScaleWhilePaused()
        {
            var r = Build(EnemyRank.Elite); var visual = r.transform.GetChild(0);
            Vector3 scale = visual.localScale;
            var existing = new HashSet<StanceBreakPopup>(Object.FindObjectsByType<StanceBreakPopup>());
            r.ApplyReaction(Hit(10f));
            StanceBreakPopup popup = null;
            foreach (var candidate in Object.FindObjectsByType<StanceBreakPopup>())
                if (!existing.Contains(candidate)) popup = candidate;
            Assert.IsNotNull(popup); Assert.IsNotNull(r.GetComponent<StanceBreakPresentation>());
            Assert.AreNotEqual(scale, visual.localScale);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.31f);
            Assert.AreEqual(scale, visual.localScale); Assert.IsTrue(popup);
            yield return new WaitForSecondsRealtime(0.83f);
            Assert.IsFalse(popup, "popup expires after 1.1 real seconds even during pause");
            Assert.IsTrue(r.IsControlLocked, "mechanical time must remain frozen");
            Time.timeScale = 1f;
        }

        [UnityTest] public IEnumerator ProtectedEliteLegendaryAndBossCompleteAnActualAttackUnderPressure()
        {
            foreach (var rank in new[] { EnemyRank.Elite, EnemyRank.Legendary, EnemyRank.Boss })
            {
                var r = Build(rank, rank == EnemyRank.Boss ? ReactionCategory.Boss : ReactionCategory.Elite);
                r.ConfigureStance(rank, 10f, 1f, 0f, 0f, 0f, 1f, 1f, 1f);
                r.ConfigureReaction(new ReactionConfiguration(rank, vulnerabilitySeconds: 0.1f, breakImmunitySeconds: 0.1f));
                int breaks = 0; r.StanceBroken += _ => breaks++;
                r.ApplyReaction(Hit(10f, StanceBreakEffect.Stun));
                yield return new WaitForSeconds(0.15f);
                var targetGo = new GameObject("Resume target"); targetGo.SetActive(false); _objects.Add(targetGo);
                var target = targetGo.AddComponent<Actor>(); target.health = 100f;
                targetGo.transform.position = Vector3.forward; targetGo.SetActive(true);
                var actions = r.gameObject.AddComponent<EnemyCombatActions>();
                r.StartCoroutine(actions.Perform(EnemyAttackKind.Punch, target, 5f, () => !r.IsControlLocked));
                Assert.IsTrue(r.IsResumeProtected);
                for (int i = 0; i < 12; i++)
                {
                    r.ApplyReaction(Hit(100f, type: HitReactionType.Stagger));
                    r.GetComponent<Actor>().TakeDamage(0.5f);
                    yield return new WaitForSeconds(0.05f);
                    Assert.AreEqual(1, breaks);
                }
                yield return new WaitForSeconds(1f);
                Assert.AreEqual(1, actions.ResolvedImpacts); Assert.IsTrue(actions.Completed);
                Assert.IsFalse(r.IsResumeProtected); Assert.Less(r.GetComponent<Actor>().health, 100f);
                r.gameObject.SetActive(false); targetGo.SetActive(false);
            }
        }
    }
}
