using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    /// <summary>Etapa 3 (11/11/2026): Volátil bomb, Guarda front and the Grunt/Rush lunge on real prefabs.</summary>
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private static GameObject SpawnArchetype(string name, Vector3 position, Transform player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/" + name + ".prefab");
            Assert.IsNotNull(prefab, name);
            var instance = Object.Instantiate(prefab, position, Quaternion.identity);
            foreach (var coin in instance.GetComponents<CoinDrop>()) Object.DestroyImmediate(coin);
            var ai = instance.GetComponent<EnemyAI>();
            ai.player = player;
            ai.enabled = false; // the test drives the enemy; no AI turning/attacks
            return instance;
        }

        [UnityTest, Category("ETAPA3")]
        public IEnumerator ETAPA3VolatileLeavesABlinkingBombThatHurtsOnlyANearbyPlayerAfterTheFuse()
        {
            BuildDirectRig(); yield return null;
            Vector3 origin = _player.transform.position;
            var near = SpawnArchetype("Fragile", origin + Vector3.forward * 1.5f, _player.transform);
            GameObject far = null;
            try
            {
                yield return null; yield return null; // EnemyVariant.Start applies the archetype role
                Assert.IsNotNull(near.GetComponent<VolatileBehavior>(), "Fragile gets the Volátil behavior.");
                float before = _player.health;
                var actor = near.GetComponent<Actor>();
                actor.TakeDamage(actor.maxHealth * 10f);
                yield return null;
                Assert.IsTrue(actor == null || actor.IsDead);
                yield return new WaitForSeconds(VolatileBlastModel.Fuse * .5f);
                Assert.AreEqual(before, _player.health, .01f, "Nothing before the fuse ends.");
                yield return new WaitForSeconds(VolatileBlastModel.Fuse * .5f + .2f);
                Assert.AreEqual(before - VolatileBlastModel.Damage, _player.health, .5f, "The bomb hurts the player inside the radius.");

                far = SpawnArchetype("Fragile", origin + Vector3.forward * 5f, _player.transform);
                yield return null; yield return null;
                before = _player.health;
                var farActor = far.GetComponent<Actor>();
                farActor.TakeDamage(farActor.maxHealth * 10f);
                yield return new WaitForSeconds(VolatileBlastModel.Fuse + .3f);
                Assert.AreEqual(before, _player.health, .01f, "Outside the 2.5 m radius: no damage.");
            }
            finally { if (near) Object.Destroy(near); if (far) Object.Destroy(far); }
        }

        /// <summary>Playtest 12/11/2026: in real combat the bomb never appeared (the handler was gone by the time Died was
        /// published). The bomb must not depend on the component still being enabled when the death is announced.</summary>
        [UnityTest, Category("ETAPA3")]
        public IEnumerator ETAPA3VolatileStillLeavesItsBombWhenTheDeathIsAnnouncedLate()
        {
            BuildDirectRig(); yield return null;
            var volatileEnemy = SpawnArchetype("Fragile", _player.transform.position + Vector3.forward * 1.5f, _player.transform);
            try
            {
                yield return null; yield return null;
                var behavior = volatileEnemy.GetComponent<VolatileBehavior>();
                behavior.enabled = false; // what the destroy pass does before a deferred Died arrives
                float before = _player.health;
                var actor = volatileEnemy.GetComponent<Actor>();
                actor.TakeDamage(actor.maxHealth * 10f);
                yield return new WaitForSeconds(VolatileBlastModel.Fuse + .3f);
                Assert.AreEqual(before - VolatileBlastModel.Damage, _player.health, .5f, "The bomb still goes off.");
            }
            finally { if (volatileEnemy) Object.Destroy(volatileEnemy); }
        }

        /// <summary>Playtest 12/11/2026: a dash (or a hook pull) can end inside an enemy's body; walking then
        /// froze in every direction. Walking out of an overlap must always work.</summary>
        [UnityTest, Category("ETAPA3")]
        public IEnumerator ETAPA3PlayerOverlappingAnEnemyCanStillWalkAway()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                var enemy = BasicVictim(Vector3.forward * 4f);
                yield return null;
                var agent = _player.GetComponent<NavMeshAgent>();
                // Land right on the enemy, slightly off-centre, as a dash through it does.
                Vector3 inside = enemy.transform.position + Vector3.left * .15f;
                if (agent && agent.enabled && agent.isOnNavMesh) agent.Warp(inside); else _player.transform.position = inside;
                yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                Vector3 start = _player.transform.position;
                input.Keys(UnityEngine.InputSystem.Key.A);
                yield return new WaitForSeconds(.4f);
                input.Keys();
                Assert.Greater(Vector3.Distance(start, _player.transform.position), .5f, "Overlapping an enemy must not freeze walking.");
            }
        }

        [UnityTest, Category("ETAPA3")]
        public IEnumerator ETAPA3GuardaBlocksFromTheFrontAndTakesDamageFromBehind()
        {
            BuildDirectRig(); yield return null;
            Vector3 origin = _player.transform.position;
            var guard = SpawnArchetype("Mirror", origin + Vector3.forward * 3f, _player.transform);
            try
            {
                yield return null; yield return null;
                var reflector = guard.GetComponent<FrontalReflector>();
                Assert.IsNotNull(reflector);
                var actor = guard.GetComponent<Actor>();
                guard.transform.rotation = Quaternion.LookRotation(Vector3.back); // facing the player
                float hp = actor.health;
                actor.TakeDamage(20f);
                Assert.AreEqual(hp, actor.health, .01f, "Front blocks HP damage.");
                Assert.AreEqual(1, reflector.BlockedHits);
                guard.transform.rotation = Quaternion.LookRotation(Vector3.forward); // back to the player
                actor.TakeDamage(20f);
                Assert.Less(actor.health, hp, "From behind the Guarda takes damage.");
            }
            finally { if (guard) Object.Destroy(guard); }
        }
    }
}
