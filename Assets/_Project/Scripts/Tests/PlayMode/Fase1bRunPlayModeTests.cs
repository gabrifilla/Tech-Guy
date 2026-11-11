using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// Fase 1b on the real scenes: Fragmentos banking, ally capsule placement and rescue, upgrades applied to a run,
    /// and the Nexus terminal. Each test owns a fresh temporary profile (never the real one, never Docs/).
    /// </summary>
    public sealed class Fase1bRunPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private string _profileDirectory;
        private int _weapon;

        [SetUp]
        public void FreshProfile()
        {
            _profileDirectory = Path.Combine(Path.GetTempPath(), "TechGuyTests", "Fase1bRun-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_profileDirectory);
            MetaProgressStore.DirectoryOverride = _profileDirectory;
            _weapon = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
        }

        [TearDown]
        public void RestoreProfile()
        {
            if (DialoguePanel.Current) DialoguePanel.Current.Close();
            Time.timeScale = 1f;
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _weapon);
            MetaProgressStore.DirectoryOverride = MetaProgressTestIsolation.Directory;
            try { Directory.Delete(_profileDirectory, true); } catch (Exception) { }
        }

        private static MetaProgressProfile Profile() => MetaProgressStore.CreateDefault().Load();

        private static IEnumerator LoadFirstSector(ulong seed)
        {
            SessionState.SetString(RunSeedSource.NextEditorSeedKey, seed.ToString());
            yield return SceneManager.LoadSceneAsync("FirstSector");
            for (int frame = 0; frame < 4; frame++) yield return null;
            var director = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
            Assert.IsNotNull(director?.ManualSession, "FirstSector did not start a run.");
        }

        private static Vector3 StandingPoint(ProgressionDirector director, int room)
        {
            var center = director.ManualSession.Route.Node(room).Center; var layout = director.ManualEnvironment.NodeLayout(room);
            if (layout == null || layout.Clear(Vector3.zero, 1.5f)) return center;
            for (int r = 1; r < 12; r++) for (int x = -r; x <= r; x++) for (int z = -r; z <= r; z++)
                if (layout.Clear(new Vector3(x, 0, z), 1.5f)) return center + new Vector3(x, 0, z);
            return center;
        }

        /// <summary>Clears one room through the real route policy, encounter frontier and reward tickets (first card).</summary>
        private static IEnumerator ClearRoom(ProgressionDirector director, PlayerActor player, int room, bool takeRewards = true,
            Action duringFight = null)
        {
            var session = director.ManualSession;
            Assert.IsTrue(director.TryCommitDestination(session.Id, room), "Commit " + room);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(StandingPoint(director, room)));
            Assert.IsTrue(director.TryEnterNode(session.Id, room), "Enter " + room);
            yield return null;
            duringFight?.Invoke();
            float end = Time.realtimeSinceStartup + 8f;
            while (session.Phase == RunPhase.Encounter || session.Phase == RunPhase.FinalResolution)
            {
                foreach (var actor in director.GetComponentsInChildren<Actor>()) if (actor && !actor.IsDead) actor.TakeDamage(1000000f);
                Assert.Less(Time.realtimeSinceStartup, end, "Room " + room + " did not close.");
                yield return null;
            }
            if (!takeRewards) yield break;
            var run = player.GetComponent<RunBoons>();
            end = Time.realtimeSinceStartup + 8f;
            while (session.Phase == RunPhase.RewardPending)
            {
                if (!run.IsChoosing) director.TryOpenManualReward(session.Id, room, session.RewardOrdinal);
                yield return null;
                if (run.IsChoosing && run.Choices.Count > 0) run.Choose(0);
                Assert.Less(Time.realtimeSinceStartup, end, "Rewards of room " + room + " did not resolve.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator FirstRunBanksFragmentsPerRoomRescuesInRoomTwoAndKeepsEverythingOnDeath()
        {
            yield return LoadFirstSector(0);
            var director = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            string session = director.ManualSession.Id.ToString();
            Assert.AreEqual(2, director.ManualAllyTargetRoom, "First run: the ally waits in the second combat room.");

            yield return ClearRoom(director, player, 1);
            var afterRoom1 = Profile();
            Assert.GreaterOrEqual(afterRoom1.fragments, FragmentEconomy.CombatRoomCleared, "Room bonus banked at the room clear.");
            Assert.AreEqual(afterRoom1.fragments, afterRoom1.FindRun(session).fragmentsEarned);
            Assert.AreEqual(director.RunFragments, afterRoom1.fragments, "HUD count equals what was banked.");
            Assert.IsNull(director.ManualRescueCapsule, "Not in the first room on the first run.");

            RescueCapsule capsule = null;
            yield return ClearRoom(director, player, 2, duringFight: () =>
            {
                capsule = director.ManualRescueCapsule;
                Assert.IsNotNull(capsule, "The ally is already in the second combat room when the fight starts.");
                Assert.IsFalse(capsule.Unlocked, "Locked while the room is fought.");
                Assert.IsNotNull(capsule.CurrentBark, "The ally calls out on arrival.");
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(capsule.transform.position + Vector3.right * 1.5f));
                Assert.IsFalse(capsule.Rescue(), "Cannot be freed mid-fight.");
                Assert.IsFalse(Profile().HasAlly(MetaAllies.Unknown));
            });
            Assert.AreSame(capsule, director.ManualRescueCapsule);
            Assert.IsTrue(capsule.Unlocked, "Winning the room drops the lock.");
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(capsule.transform.position + Vector3.right * 1.5f));
            Assert.IsTrue(capsule.PlayerInRange);
            player.health = player.maxHealth * .5f;
            Assert.IsTrue(capsule.Rescue());
            Assert.AreEqual(player.maxHealth * (.5f + MetaAllies.RescueHealFraction), player.health, 1f, "The ally heals on rescue.");
            Assert.IsTrue(Profile().HasAlly(MetaAllies.Unknown), "Rescue saved at once.");
            Assert.IsTrue(DialoguePanel.IsOpen); Assert.AreEqual(0f, Time.timeScale, "Dialogue owns the clock.");
            DialoguePanel.Current.Close();
            Assert.AreEqual(1f, Time.timeScale); yield return null;
            Assert.IsTrue(capsule == null, "The ally disappears after the dialogue.");

            int banked = Profile().fragments;
            player.TakeDamage(100000000f);
            float end = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "NexusBlockout") { Assert.Less(Time.realtimeSinceStartup, end); yield return null; }
            var final = Profile();
            Assert.AreEqual(1, final.deaths);
            Assert.GreaterOrEqual(final.fragments, banked, "Death keeps every banked Fragmento.");
            Assert.IsTrue(final.HasAlly(MetaAllies.Unknown));
            Assert.AreEqual(MetaAllies.Unknown, final.FindRun(session).allyRescued);
            Assert.AreEqual("Death", final.FindRun(session).outcome);
        }

        [UnityTest]
        public IEnumerator FromTheSecondRunTheAllyWaitsInTheFirstCombatRoom()
        {
            yield return LoadFirstSector(0);
            var first = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
            Assert.AreEqual(2, first.ManualAllyTargetRoom);
            Assert.IsTrue(first.TryAbortManualRun(first.ManualSession.Id));
            float end = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "NexusBlockout") { Assert.Less(Time.realtimeSinceStartup, end); yield return null; }
            Assert.AreEqual(1, Profile().abandons);

            yield return LoadFirstSector(1);
            var director = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            Assert.AreEqual(1, director.ManualAllyTargetRoom, "Not rescued yet: guaranteed in the first combat room.");
            bool seenDuringFight = false;
            yield return ClearRoom(director, player, 1, takeRewards: false,
                duringFight: () => seenDuringFight = director.ManualRescueCapsule && !director.ManualRescueCapsule.Unlocked);
            Assert.IsTrue(seenDuringFight, "Locked capsule present from the start of the first combat room.");
            Assert.IsTrue(director.ManualRescueCapsule.Unlocked);
        }

        [UnityTest]
        public IEnumerator OwnedUpgradesShapeTheNextRun()
        {
            var store = MetaProgressStore.CreateDefault();
            var seeded = store.Load();
            seeded.upgrades.AddRange(MetaUpgrades.All.Select(u => u.Id));
            seeded.rescuedAllies.Add(MetaAllies.Unknown);
            Assert.IsTrue(store.Save(seeded));

            yield return LoadFirstSector(0);
            var director = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var run = player.GetComponent<RunBoons>();
            Assert.AreEqual(0, director.ManualAllyTargetRoom, "Already rescued: no capsule.");
            Assert.AreEqual(player.maxHealth, player.health, "Vida reforçada starts full.");
            Assert.AreEqual(900f, player.maxHealth, 0.01f, "Vida reforçada: +200 over the 700 base (threat pass 11/11/2026).");
            Assert.AreEqual(2, run.ManualRerollsRemaining, "Reroll + Rolagem extra.");
            var repair = (RunRepairPolicy)typeof(ProgressionDirector).GetField("_manualRepair", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
            Assert.AreEqual(MetaUpgrades.RepairFractionBonus, repair.FractionBonus, 1e-5f);

            yield return ClearRoom(director, player, 1, takeRewards: false);
            var session = director.ManualSession;
            Assert.IsTrue(director.TryOpenManualReward(session.Id, 1, session.RewardOrdinal) || run.IsChoosing);
            yield return null;
            Assert.IsTrue(run.IsChoosing);
            Assert.AreEqual(4, run.Choices.Count, "Quarta opção: four cards.");
            // The first gauntlet Technique pool has exactly four entries: with four cards there is nothing to redraw,
            // so the reroll stays unspent and its button hidden.
            Assert.AreEqual(4, run.ManualTicket.Pool.Count);
            Assert.IsFalse(run.CanRerollManual); Assert.IsFalse(run.TryRerollManual()); Assert.AreEqual(2, run.ManualRerollsRemaining);
        }

        [UnityTest]
        public IEnumerator RerollRedrawsTheOpenOfferOncePerRun()
        {
            var store = MetaProgressStore.CreateDefault(); var seeded = store.Load();
            seeded.upgrades.Add(MetaUpgrades.Reroll); seeded.rescuedAllies.Add(MetaAllies.Unknown); Assert.IsTrue(store.Save(seeded));
            yield return LoadFirstSector(0);
            var director = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var run = player.GetComponent<RunBoons>();
            yield return ClearRoom(director, player, 1, takeRewards: false);
            var session = director.ManualSession;
            Assert.IsTrue(director.TryOpenManualReward(session.Id, 1, session.RewardOrdinal) || run.IsChoosing);
            yield return null;
            Assert.AreEqual(3, run.Choices.Count, "Without Quarta opção the offer keeps three cards.");
            Assert.IsTrue(run.CanRerollManual);
            int revision = run.ManualTicket.Revision;
            Assert.IsTrue(run.TryRerollManual());
            Assert.AreEqual(revision + 1, run.ManualTicket.Revision, "Old cards can no longer be confirmed.");
            Assert.AreEqual(3, run.Choices.Count); Assert.AreEqual(0, run.ManualRerollsRemaining);
            Assert.IsFalse(run.CanRerollManual); Assert.IsFalse(run.TryRerollManual());
            Assert.IsTrue(run.Choose(0), "A rerolled card is acquired normally.");
        }

        [UnityTest]
        public IEnumerator NexusTerminalIsDarkUntilTheRescueThenTalksOnceAndSells()
        {
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
            var dark = UnityEngine.Object.FindFirstObjectByType<NexusUpgradeTerminal>();
            Assert.IsNotNull(dark, "The terminal is part of the Nexus scene.");
            Assert.IsFalse(dark.IsActive, "Before the rescue the terminal is dark.");

            var store = MetaProgressStore.CreateDefault(); var profile = store.Load();
            profile.rescuedAllies.Add(MetaAllies.Unknown); profile.fragments = 20; Assert.IsTrue(store.Save(profile));
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
            var terminal = UnityEngine.Object.FindFirstObjectByType<NexusUpgradeTerminal>();
            Assert.IsTrue(terminal.IsActive);
            terminal.Talk();
            Assert.IsTrue(DialoguePanel.IsOpen); Assert.AreEqual(MetaAllies.UnknownDisplayName, DialoguePanel.Current.Speaker);
            DialoguePanel.Current.Close();
            Assert.IsTrue(terminal.IsOpen, "The dialogue hands over to the terminal."); Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(terminal.TryBuy(0));
            Assert.IsFalse(terminal.TryBuy(0), "Bought once.");
            Assert.IsFalse(terminal.TryBuy(1), "40 > 0 left.");
            terminal.CloseTerminal();
            Assert.AreEqual(1f, Time.timeScale, "Closing gives the clock back.");
            var saved = Profile();
            Assert.IsTrue(saved.HasUpgrade(MetaUpgrades.Vitality)); Assert.AreEqual(0, saved.fragments);
            Assert.IsTrue(saved.HasSeenDialogue(MetaAllies.UnknownNexusDialogueKey));

            // Nothing new to say: the next visit opens the terminal directly.
            yield return null;
            Assert.IsFalse(terminal.HasNewDialogue);
            terminal.Talk();
            Assert.IsFalse(DialoguePanel.IsOpen, "No repeated dialogue."); Assert.IsTrue(terminal.IsOpen);
            terminal.CloseTerminal();
        }
    }
}
