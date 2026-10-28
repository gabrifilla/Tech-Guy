using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed class EnemyProjectileLifecyclePlayModeTests
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo PoolField = typeof(EnemyProjectile).GetField("_pool", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly List<Scene> _scenes = new List<Scene>();
        private Scene _original;

        [SetUp]
        public void SetUp()
        {
            _original = SceneManager.GetActiveScene();
            // Isolate the initial fixture only. Never reset the pool between the three lifetimes.
            PoolField.SetValue(null, null);
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (_original.IsValid() && _original.isLoaded) SceneManager.SetActiveScene(_original);
            foreach (var scene in _scenes)
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            _scenes.Clear();
        }

        private Scene NewScene()
        {
            var scene = SceneManager.CreateScene("Projectile lifecycle " + Guid.NewGuid());
            _scenes.Add(scene);
            Assert.IsTrue(SceneManager.SetActiveScene(scene));
            return scene;
        }

        private static Actor BuildActor(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.position = position;
            var actor = go.AddComponent<Actor>();
            actor.health = 100;
            go.SetActive(true);
            return actor;
        }

        private static ComponentPool<EnemyProjectile> Pool => (ComponentPool<EnemyProjectile>)PoolField.GetValue(null);
        private static Actor Owner(EnemyProjectile shot) => (Actor)typeof(EnemyProjectile).GetField("_owner", Private).GetValue(shot);
        private static Actor Target(EnemyProjectile shot) => (Actor)typeof(EnemyProjectile).GetField("_target", Private).GetValue(shot);
        private static int ProjectileListeners(Actor owner)
        {
            var handlers = (Delegate)typeof(Actor).GetField("Died", Private).GetValue(owner);
            return handlers?.GetInvocationList().Count(x => x.Target is EnemyProjectile) ?? 0;
        }

        private static IEnumerator Until(Func<bool> condition, string message, float seconds = 3f)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, end, message);
                yield return null;
            }
        }

        private static EnemyProjectile Spawn(Actor owner, Actor target, float range = 20f)
        {
            EnemyProjectile result = null;
            Assert.DoesNotThrow(() => result = EnemyProjectile.Spawn(owner.transform.position + Vector3.up * .55f,
                Vector3.forward, owner, target, 10, 8, range, .35f, EnemyProjectile.PathKind.Straight, Color.red),
                "A destroyed pooled component must never become the next run's projectile.");
            Assert.IsTrue(result, "Spawn must return a live Unity component.");
            return result;
        }

        [UnityTest]
        public IEnumerator StaticPoolEmitsAcrossThreeUnloadedScenesWithActiveAndParkedShots()
        {
            ComponentPool<EnemyProjectile> sharedPool = null;
            Actor previousOwner = null, previousTarget = null;
            EnemyProjectile previousShot = null;
            for (int run = 1; run <= 3; run++)
            {
                var scene = NewScene();
                var owner = BuildActor("Owner " + run, new Vector3(5000, 0, 5000));
                var target = BuildActor("Target " + run, owner.transform.position + Vector3.forward * 8);
                var shot = Spawn(owner, target, .5f);
                if (sharedPool == null) sharedPool = Pool;
                else Assert.AreSame(sharedPool, Pool, "The existing adapter must recover without a new manager or external reset.");
                Assert.AreEqual(scene, shot.gameObject.scene);
                Assert.AreSame(owner, Owner(shot)); Assert.AreSame(target, Target(shot));
                Assert.AreEqual(100, target.health, "No damage on the firing frame.");
                yield return null;
                Assert.Greater(shot.Motion.Travelled, 0, "A recovered projectile must actually travel.");
                yield return Until(() => !shot.gameObject.activeSelf, "Range expiration must park the projectile.");
                Assert.AreEqual(0, ProjectileListeners(owner));
                Assert.AreEqual(1, Pool.FreeCount);
                var flying = Spawn(owner, target);
                Assert.AreSame(shot, flying, "Reuse within a live scene must remain intact.");
                Assert.AreEqual(1, ProjectileListeners(owner));
                var parked = Spawn(owner, target, .5f);
                yield return Until(() => !parked.gameObject.activeSelf, "Second shot must park before unload.");
                Assert.IsTrue(flying.gameObject.activeSelf, "Unload must also exercise an in-flight projectile.");
                if (run > 1)
                {
                    Assert.IsFalse(previousOwner); Assert.IsFalse(previousTarget); Assert.IsFalse(previousShot);
                    Assert.AreEqual(2, Pool.CreatedCount, "Destroyed scene capacity must not inflate the new scene's live count.");
                    Assert.AreEqual(1, Pool.LiveCount); Assert.AreEqual(1, Pool.FreeCount);
                }
                previousOwner = owner; previousTarget = target; previousShot = flying;
                SceneManager.SetActiveScene(_original);
                yield return SceneManager.UnloadSceneAsync(scene);
                Assert.IsFalse(owner); Assert.IsFalse(target); Assert.IsFalse(flying); Assert.IsFalse(parked);
            }
        }

        [UnityTest]
        public IEnumerator AimedAndSniperShotsEmitAndTravelAfterTwoSceneUnloads()
        {
            for (int run = 1; run <= 3; run++)
            {
                var scene = NewScene();
                var owner = BuildActor("Ranged owner " + run, new Vector3(5100, 0, 5100));
                var target = BuildActor("Ranged target " + run, owner.transform.position + Vector3.forward * 10);
                var actions = owner.gameObject.AddComponent<EnemyCombatActions>();
                foreach (var kind in new[] { EnemyAttackKind.AimedShot, EnemyAttackKind.SniperShot })
                {
                    int before = actions.ResolvedImpacts;
                    owner.StartCoroutine(actions.Perform(kind, target, 10, () => true));
                    yield return Until(() => actions.ResolvedImpacts > before, "The real ranged windup must reach emission.");
                    var shot = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
                        .Single(x => x.isActiveAndEnabled && Owner(x) == owner);
                    Assert.AreSame(target, Target(shot)); Assert.AreEqual(scene, shot.gameObject.scene);
                    float travelled = shot.Motion.Travelled;
                    yield return null;
                    Assert.Greater(shot.Motion.Travelled, travelled);
                    yield return Until(() => !shot.gameObject.activeSelf, "Ranged projectile must resolve and return.");
                    Assert.AreEqual(0, ProjectileListeners(owner));
                }
                SceneManager.SetActiveScene(_original);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator OwnerDeathReturnsInFlightProjectileWithoutDamageAndUnsubscribes()
        {
            NewScene();
            var owner = BuildActor("Owner", new Vector3(5200, 0, 5200));
            var target = BuildActor("Target", owner.transform.position + Vector3.forward * 10);
            var shot = Spawn(owner, target);
            Assert.AreEqual(1, ProjectileListeners(owner));
            owner.TakeDamage(1000); // Fixture: exercise Actor.Died, not a natural encounter victory.
            Assert.IsFalse(shot.gameObject.activeSelf); Assert.IsTrue(shot.Motion.IsMarkedForDestruction);
            Assert.AreEqual(0, ProjectileListeners(owner)); Assert.AreEqual(1, Pool.FreeCount);
            yield return null;
            Assert.AreEqual(100, target.health);
        }

        [UnityTest]
        public IEnumerator ReuseDoesNotRetainPreviousOwnerListenerOrTarget()
        {
            NewScene();
            var oldOwner = BuildActor("Old owner", new Vector3(5300, 0, 5300));
            var oldTarget = BuildActor("Old target", oldOwner.transform.position + Vector3.forward * 10);
            var shot = Spawn(oldOwner, oldTarget, .5f);
            yield return Until(() => !shot.gameObject.activeSelf, "First owner must release its shot.");
            var owner = BuildActor("New owner", oldOwner.transform.position + Vector3.right * 3);
            var target = BuildActor("New target", owner.transform.position + Vector3.forward * 10);
            var reused = Spawn(owner, target);
            Assert.AreSame(shot, reused); Assert.AreSame(owner, Owner(reused)); Assert.AreSame(target, Target(reused));
            Assert.AreEqual(0, ProjectileListeners(oldOwner)); Assert.AreEqual(1, ProjectileListeners(owner));
            oldOwner.TakeDamage(1000);
            yield return null;
            Assert.IsTrue(reused.gameObject.activeSelf, "An old owner's death cannot release the new owner's shot.");
            owner.TakeDamage(1000);
            Assert.IsFalse(reused.gameObject.activeSelf); Assert.AreEqual(0, ProjectileListeners(owner));
            Assert.AreEqual(100, target.health); Assert.AreEqual(100, oldTarget.health);
        }

        [UnityTest]
        public IEnumerator CancelBeforeEmissionPreventsShotAndCancelAfterEmissionPreservesFlight()
        {
            NewScene();
            var owner = BuildActor("Cancel owner", new Vector3(5400, 0, 5400));
            var target = BuildActor("Cancel target", owner.transform.position + Vector3.forward * 10);
            var actions = owner.gameObject.AddComponent<EnemyCombatActions>();
            owner.StartCoroutine(actions.Perform(EnemyAttackKind.SniperShot, target, 10, () => true));
            yield return Until(() => actions.IsWindingUp, "Sniper must start its real warning.");
            actions.Cancel();
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(0, actions.ResolvedImpacts); Assert.IsFalse(actions.IsWindingUp);
            Assert.IsTrue(Pool == null || Pool.CreatedCount == 0, "Canceled windup must not acquire a projectile.");
            owner.StartCoroutine(actions.Perform(EnemyAttackKind.AimedShot, target, 10, () => true));
            yield return Until(() => actions.ResolvedImpacts == 1, "Aimed shot must emit before post-contact cancel.");
            var shot = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Single(x => Owner(x) == owner);
            float travelled = shot.Motion.Travelled;
            actions.Cancel(); yield return null;
            Assert.IsTrue(shot.gameObject.activeSelf); Assert.Greater(shot.Motion.Travelled, travelled);
            yield return Until(() => !shot.gameObject.activeSelf, "Emitted projectile must still resolve normally.");
            Assert.AreEqual(90, target.health); Assert.AreEqual(0, ProjectileListeners(owner));
        }
    }
}
