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
