using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode tests for the lobby <see cref="TrainingDummy"/>.
    ///
    /// Feature: nexus-lobby-menu-restructure, Property 5 (indestructible and rewardless) and
    /// Property 6 (passive). Validates: Requirements 4.3, 4.4, 4.5.
    /// </summary>
    public sealed class TrainingDummyPassiveTests
    {
        private GameObject _dummyObject;

        [TearDown]
        public void TearDown()
        {
            if (_dummyObject) Object.DestroyImmediate(_dummyObject);
        }

        /// <summary>Builds a live <see cref="TrainingDummy"/> with a collider and a known health pool.</summary>
        private TrainingDummy BuildDummy(float health = 200f)
        {
            _dummyObject = new GameObject("TrainingDummy");
            _dummyObject.SetActive(false); // configure serialized fields before Awake runs
            _dummyObject.transform.position = Vector3.zero;

            var collider = _dummyObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one;

            var dummy = _dummyObject.AddComponent<TrainingDummy>();
            dummy.health = health;

            _dummyObject.SetActive(true); // Awake -> maxHealth = health, strips CoinDrop
            return dummy;
        }

        // Feature: nexus-lobby-menu-restructure, Property 5
        // Damage down to zero restores the dummy's health, grants no coins, and never destroys it.
        [UnityTest]
        public IEnumerator DamageToZeroRestoresHealthAndNeverDestroysTheDummy()
        {
            TrainingDummy dummy = BuildDummy(200f);
            float maxHealth = dummy.maxHealth;
            Assert.That(maxHealth, Is.EqualTo(200f), "Dummy should adopt its authored health as maxHealth.");

            // A partial hit reduces health and produces combat feedback like any Actor (Requisito 4.2).
            dummy.TakeDamage(50f);
            Assert.That(dummy.health, Is.EqualTo(150f), "A non-lethal hit should reduce health normally.");
            Assert.That(dummy.IsDead, Is.False);

            // A lethal hit must not destroy the dummy; it restores to full and stays usable (Requisito 4.3).
            dummy.TakeDamage(1000f);

            // Give the engine a frame in case a Destroy had been scheduled.
            yield return null;

            Assert.That(dummy == null, Is.False, "The dummy must never be destroyed.");
            Assert.That(_dummyObject, Is.Not.Null, "The dummy GameObject must survive a lethal hit.");
            Assert.That(dummy.IsDead, Is.False, "After 'dying' the dummy must be alive again.");
            Assert.That(dummy.health, Is.EqualTo(maxHealth), "Health must be restored to max after reaching zero.");

            // It remains a valid target for the next ability (Requisito 4.3/4.6).
            dummy.TakeDamage(40f);
            Assert.That(dummy.health, Is.EqualTo(maxHealth - 40f), "The restored dummy must keep taking damage.");
        }

        // Feature: nexus-lobby-menu-restructure, Property 5
        // The dummy never grants coins: the CoinDrop that Actor.Awake attaches is removed.
        [UnityTest]
        public IEnumerator DummyGrantsNoCoins()
        {
            TrainingDummy dummy = BuildDummy(120f);

            // Let the deferred Destroy(coinDrop) from Awake resolve.
            yield return null;

            Assert.That(dummy.TryGetComponent<CoinDrop>(out _), Is.False,
                "TrainingDummy must not carry a CoinDrop (no rewards on hit or defeat).");

            int coinsBefore = CountCoinPickupsInScene();

            // "Defeat" the dummy repeatedly; a normal Actor would drop coins via CoinDrop on Died.
            for (int i = 0; i < 5; i++)
            {
                dummy.TakeDamage(10000f);
                yield return null;
            }

            int coinsAfter = CountCoinPickupsInScene();
            Assert.That(coinsAfter, Is.EqualTo(coinsBefore), "Defeating the dummy must not spawn any coins.");
        }

        // Feature: nexus-lobby-menu-restructure, Property 6
        // The dummy is passive: it has no EnemyAI, so it can neither chase nor attack the player.
        [UnityTest]
        public IEnumerator DummyIsPassiveWithoutEnemyAI()
        {
            TrainingDummy dummy = BuildDummy();

            yield return null;

            Assert.That(dummy.TryGetComponent<EnemyAI>(out _), Is.False,
                "A passive TrainingDummy must not have an EnemyAI (no chase/attack).");

            // Even after taking damage it must not gain any attack behavior.
            dummy.TakeDamage(dummy.maxHealth + 1f);
            yield return null;

            Assert.That(dummy.TryGetComponent<EnemyAI>(out _), Is.False,
                "The dummy must remain passive after being hit.");
        }

        private static int CountCoinPickupsInScene()
        {
            // Coins are the only side effect a CoinDrop would produce; count them directly rather
            // than relying on wallet state, since CoinDrop instantiates pickups on death.
            return Object.FindObjectsByType<CoinPickup>(FindObjectsSortMode.None).Length;
        }
    }
}
