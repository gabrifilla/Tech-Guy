using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.PlayMode
{
    public sealed class DamageConfirmationPlayModeTests
    {
        private readonly List<GameObject> _victims = new List<GameObject>();
        private float _timeScale;

        [SetUp]
        public void SetUp() { _timeScale = Time.timeScale; Time.timeScale = 1f; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = _timeScale;
            foreach (var victim in _victims) if (victim) Object.Destroy(victim);
            _victims.Clear();
            foreach (var popup in Popups()) if (popup) Object.Destroy(popup.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualDamageMergesPerVictim_AndLethalConfirmationSurvivesDeath()
        {
            Actor a = Victim(20f), b = Victim(40f);
            a.TakeDamage(5f); a.TakeDamage(7f); b.TakeDamage(9f);
            var popups = Popups();
            Assert.AreEqual(2, popups.Length, "Rapid damage on two enemies requires two separate confirmations.");
            Assert.That(System.Array.Exists(popups, p => p.Amount == 12f && !p.IsLethal));
            Assert.That(System.Array.Exists(popups, p => p.Amount == 9f && !p.IsLethal));
            a.TakeDamage(100f);
            popups = Popups();
            Assert.AreEqual(2, popups.Length);
            Assert.That(System.Array.Exists(popups, p => p.Amount == 20f && p.IsLethal), "Overkill must show actual HP removed, not requested damage.");
            yield return null;
            Assert.AreEqual(2, Popups().Length, "Confirmation must remain detached after victim destruction.");
        }

        [UnityTest]
        public IEnumerator ImmunityAndZeroDamageDoNotProduceNumbers_AndPauseFreezesTheirLifetime()
        {
            Actor actor = Victim(40f);
            var immunity = new GameObject("Immunity source"); _victims.Add(immunity);
            actor.AddDamageImmunity(immunity);
            actor.TakeDamage(10f);
            actor.RemoveDamageImmunity(immunity);
            actor.TakeDamage(0f);
            Assert.AreEqual(0, Popups().Length);
            actor.TakeDamage(10f);
            Assert.AreEqual(1, Popups().Length);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreEqual(1, Popups().Length, "The existing scaled presentation clock must preserve popup lifetime during pause.");
            actor.TakeDamage(5f);
            Assert.AreEqual(1, Popups().Length);
            Assert.AreEqual(15f, Popups()[0].Amount);
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(0, Popups().Length);
        }

        [UnityTest]
        public IEnumerator LaterHitStartsANewNumberInsteadOfRenewingAnOldBurst()
        {
            Actor actor = Victim(50f);
            actor.TakeDamage(5f);
            yield return new WaitForSeconds(.15f);
            actor.TakeDamage(7f);
            var popups = Popups();
            Assert.AreEqual(2, popups.Length);
            Assert.That(System.Array.Exists(popups, p => p.Amount == 5f));
            Assert.That(System.Array.Exists(popups, p => p.Amount == 7f));
        }

        [UnityTest]
        public IEnumerator DisabledFeedbackDoesNotMergeDamageAcrossReactivation()
        {
            Actor actor = Victim(50f);
            actor.TakeDamage(5f);
            var feedback = actor.GetComponent<EnemyCombatFeedback>();
            feedback.enabled = false;
            actor.TakeDamage(7f);
            Assert.AreEqual(1, Popups().Length);
            Assert.AreEqual(5f, Popups()[0].Amount);
            feedback.enabled = true;
            actor.TakeDamage(3f);
            var popups = Popups();
            Assert.AreEqual(2, popups.Length, "A new appearance must have a new confirmation, even within 120 ms.");
            Assert.That(System.Array.Exists(popups, p => p.Amount == 3f));
            yield return null;
        }

        private Actor Victim(float health)
        {
            var go = new GameObject("Damage confirmation victim");
            go.SetActive(false); _victims.Add(go);
            var actor = go.AddComponent<Actor>(); actor.health = health;
            go.SetActive(true);
            // This fixture verifies damage/presentation only, with no random coin spawns on death.
            var coins = go.GetComponent<CoinDrop>(); if (coins) Object.DestroyImmediate(coins);
            return actor;
        }

        private static DamagePopup[] Popups() => Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None);
    }
}
