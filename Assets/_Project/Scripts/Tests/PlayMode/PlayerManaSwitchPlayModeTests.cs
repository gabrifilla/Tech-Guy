using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>Game state since 11/11/2026: player mana switched off (free skills, no mana orb) and dash cooldown 2 s.</summary>
    public sealed class PlayerManaSwitchPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        [SetUp] public void UseGameDefault() => PlayerActor.ManaEnabledOverride = null;
        [TearDown] public void BackToTestDefault() => PlayerActor.ManaEnabledOverride = true; // see ManaSystemTestDefaults

        [Test]
        public void ShippedConfigSwitchesPlayerManaOffAndDashCoolsDownTwoSeconds()
        {
            Assert.IsNotNull(CombatBalance.Current);
            Assert.IsFalse(CombatBalance.Current.PlayerManaEnabled, "Decision 11/11/2026: mana off for the player.");
            Assert.IsFalse(PlayerActor.ManaEnabled);
            var dash = AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset");
            Assert.AreEqual(2f, dash.cooldownTime, .001f);
        }

        [UnityTest]
        public IEnumerator EmptyManaStillCastsAndTheHudHasNoManaOrb()
        {
            SessionState.SetString(RunSeedSource.NextEditorSeedKey, "3");
            yield return SceneManager.LoadSceneAsync("FirstSector");
            for (int frame = 0; frame < 4; frame++) yield return null;
            var player = Object.FindAnyObjectByType<PlayerActor>();
            Assert.IsNotNull(player);
            player.mana = 0f;
            Assert.IsTrue(player.HasMana(50f), "No mana gate while switched off.");
            Assert.IsTrue(player.TrySpendMana(50f));
            Assert.AreEqual(0f, player.mana, "Nothing is deducted.");

            var hud = Object.FindAnyObjectByType<PlayerHUD>();
            Assert.IsNotNull(hud);
            Assert.IsFalse(hud.ManaFill.transform.parent.gameObject.activeInHierarchy, "Mana orb hidden.");
            var caption = hud.ManaFill.transform.parent.parent.Find("Mana Caption");
            if (caption) Assert.IsFalse(caption.gameObject.activeSelf, "Mana caption hidden.");
        }
    }
}
