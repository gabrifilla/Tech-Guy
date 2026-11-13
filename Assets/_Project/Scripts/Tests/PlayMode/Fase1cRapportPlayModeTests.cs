using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// Fase 1c on the real scenes: the serene fishing room, gifts, rapport-locked boons, the Segundo Round artifact
    /// and the bond panel at the weapon pedestal. Each test owns a fresh temporary profile (never the real one).
    /// </summary>
    public sealed class Fase1cRapportPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private string _profileDirectory;
        private int _weapon;
        private bool _forceSerene;

        [SetUp]
        public void FreshProfile()
        {
            _profileDirectory = Path.Combine(Path.GetTempPath(), "TechGuyTests", "Fase1cRun-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_profileDirectory);
            MetaProgressStore.DirectoryOverride = _profileDirectory;
            _weapon = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            _forceSerene = EditorPrefs.GetBool(ProgressionDirector.ForceSereneRoomPrefKey, false);
        }

        [TearDown]
        public void RestoreProfile()
        {
            if (DialoguePanel.Current) DialoguePanel.Current.Close();
            Time.timeScale = 1f;
            EditorPrefs.SetBool(ProgressionDirector.ForceSereneRoomPrefKey, _forceSerene);
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

        /// <summary>Goes through one room by the real policy: fights and takes the first card, or skips a plain repair.</summary>
        private static IEnumerator PassRoom(ProgressionDirector director, PlayerActor player, int room, Action<RunBoons> onOffer = null)
        {
            var session = director.ManualSession;
            Assert.IsTrue(director.TryCommitDestination(session.Id, room), "Commit " + room);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(StandingPoint(director, room)));
            Assert.IsTrue(director.TryEnterNode(session.Id, room), "Enter " + room);
            yield return null;
            if (session.Phase == RunPhase.RepairPending) { Assert.IsTrue(director.TryResolveRepair(session.Id, room, false)); yield break; }
            float end = Time.realtimeSinceStartup + 8f;
            while (session.Phase == RunPhase.Encounter)
            {
                foreach (var actor in director.GetComponentsInChildren<Actor>()) if (actor && !actor.IsDead) actor.TakeDamage(1000000f);
                Assert.Less(Time.realtimeSinceStartup, end, "Room " + room + " did not close.");
                yield return null;
            }
            var run = player.GetComponent<RunBoons>();
            end = Time.realtimeSinceStartup + 8f;
            while (session.Phase == RunPhase.RewardPending)
            {
                if (!run.IsChoosing) director.TryOpenManualReward(session.Id, room, session.RewardOrdinal);
                yield return null;
                if (run.IsChoosing && run.Choices.Count > 0) { onOffer?.Invoke(run); run.Choose(0); }
                Assert.Less(Time.realtimeSinceStartup, end, "Rewards of room " + room + " did not resolve.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SereneRoomIsAChoiceAtAForkRestsFishesAndTheFirstCatchIsAGift()
        {
            EditorPrefs.SetBool(ProgressionDirector.ForceSereneRoomPrefKey, true);
            ProgressionDirector director = null;
            for (ulong seed = 11; seed < 16; seed++)
            {
                yield return LoadFirstSector(seed);
                director = UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();
                if (director.ManualSereneNode >= 0) break;
            }
            Assert.GreaterOrEqual(director.ManualSereneNode, 0, "With the test switch on, a run with a valid fork has the serene room.");
            var session = director.ManualSession; var route = session.Route;
            var serene = route.Node(director.ManualSereneNode);
            Assert.IsTrue(serene.IsSerene); Assert.AreEqual(ManualNodeKind.Repair, serene.Kind); Assert.AreEqual(0, serene.Spawns.Count);
            Assert.IsTrue(route.Groups.Any(g => g.FirstBranch.Contains(serene.Id) || g.SecondBranch.Contains(serene.Id)), "Always one side of a fork.");
            Assert.AreEqual("PESCA", ManualRouteMapPresentation.RewardAbbreviation(serene), "Doors and map name the room.");

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var path = route.Paths.First(p => p.Contains(serene.Id));
            // Level-1 rapport: the four rapport boons never show up on the way.
            Action<RunBoons> noLockedBoons = run =>
            {
                foreach (var offer in run.Choices) Assert.IsTrue(Rapport.IsBoonUnlocked(offer.Id, 1), offer.Id + " is locked at rapport level 1.");
            };
            foreach (int room in path.Skip(1).TakeWhile(id => id != serene.Id)) yield return PassRoom(director, player, room, noLockedBoons);

            player.TakeDamage(player.maxHealth * .5f);
            float hurt = player.health;
            Assert.IsTrue(director.TryCommitDestination(session.Id, serene.Id));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(StandingPoint(director, serene.Id)));
            Assert.IsTrue(director.TryEnterNode(session.Id, serene.Id));
            Assert.AreEqual(RunPhase.RouteChoice, session.Phase, "No menu and no fight: the room resolves on entry.");
            Assert.AreEqual(hurt + player.maxHealth * ProgressionDirector.SereneRestFraction, player.health, 1f, "A small rest heal.");
            var pond = director.ManualFishingSpot;
            Assert.IsNotNull(pond); Assert.AreEqual(GiftEconomy.CastsPerVisit, pond.CastsLeft);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(pond.transform.position + Vector3.right * 2f)); yield return null;

            // Cast 1: pull on the bite — the first gift ever is guaranteed and banked at once.
            pond.Press(); Assert.IsTrue(pond.IsWaitingForBite);
            pond.SkipToBite(); Assert.IsTrue(pond.HasBite);
            pond.Press();
            Assert.AreEqual(1, director.RunGifts); Assert.AreEqual(1, Profile().gifts);
            Assert.AreEqual(session.Id.ToString(), Profile().runHistory.Last().sessionId); Assert.AreEqual(1, Profile().runHistory.Last().giftsFound);
            Assert.AreEqual(GiftEconomy.CastsPerVisit - 1, pond.CastsLeft);
            StringAssert.Contains(Rapport.GiftName, pond.LastResult);

            // Cast 2: pulling before the bite loses the fish and the cast.
            yield return new WaitForSeconds(FishingRules.ResultSeconds + .1f);
            int fragments = director.RunFragments;
            pond.Press(); pond.Press();
            Assert.AreEqual(GiftEconomy.CastsPerVisit - 2, pond.CastsLeft); Assert.AreEqual(fragments, director.RunFragments);
            Assert.AreEqual(1, Profile().gifts, "No second gift from a lost fish.");
        }

        [UnityTest]
        public IEnumerator SegundoRoundRefusesOneKnockoutPerRun()
        {
            var store = MetaProgressStore.CreateDefault();
            for (int i = 0; i < 6; i++) { store.AddGift(); Assert.IsTrue(store.TryGiveGift(Rapport.Ico, out _)); }
            Assert.IsTrue(store.TryEquipArtifact(Artifacts.SecondRound.Id, out _));
            yield return LoadFirstSector(21);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            player.TakeDamage(1000000f);
            Assert.IsFalse(player.IsDead, "The artifact refuses the first knockout.");
            Assert.AreEqual(player.maxHealth * Artifacts.SecondRoundHealFraction, player.health, 1f);
            player.TakeDamage(1000000f);
            Assert.IsFalse(player.IsDead, "A short immunity follows the revive.");
            yield return new WaitForSeconds(Artifacts.SecondRoundImmunitySeconds + .3f);
            player.TakeDamage(1000000f);
            Assert.IsTrue(player.IsDead, "Only once per run.");
        }

        [UnityTest]
        public IEnumerator IcoAtTheGauntletPedestalTalksAndTakesAGiftWithoutAFullScreenPanel()
        {
            var store = MetaProgressStore.CreateDefault(); store.AddGift();
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
            var arsenal = UnityEngine.Object.FindFirstObjectByType<LobbyArsenal>();
            Assert.IsNotNull(arsenal, "The Nexus has the weapon pedestals.");
            arsenal.TalkToIco();
            Assert.IsTrue(DialoguePanel.IsOpen, "The first talk with Ico is free and plays once.");
            Assert.AreEqual(Rapport.IcoDisplayName, DialoguePanel.Current.Speaker);
            DialoguePanel.Current.Close();
            Assert.AreEqual(1f, Time.timeScale, "Nothing stays open after the conversation.");
            Assert.IsTrue(Profile().HasSeenDialogue(NexusDialogues.IcoKey(0)));

            yield return null;
            Assert.IsTrue(arsenal.TryGiveGiftToIco());
            Assert.IsTrue(DialoguePanel.IsOpen, "Each gift plays a new conversation.");
            Assert.AreEqual(NexusDialogues.Ico(1)[0], DialoguePanel.Current.CurrentLine);
            DialoguePanel.Current.Close();
            var saved = Profile();
            Assert.AreEqual(2, Rapport.IcoLevel(saved)); Assert.AreEqual(0, saved.gifts);
            Assert.IsTrue(saved.HasArtifact(Artifacts.SparringWrap.Id)); Assert.IsTrue(saved.HasSeenDialogue(NexusDialogues.IcoKey(1)));
            yield return null;
            Assert.IsFalse(arsenal.TryGiveGiftToIco(), "No gift left.");

            // Later talks are a single chat line, never the intro again.
            arsenal.TalkToIco();
            Assert.IsTrue(DialoguePanel.IsOpen);
            Assert.AreNotEqual(NexusDialogues.Ico(0)[0], DialoguePanel.Current.CurrentLine);
            DialoguePanel.Current.Close();
        }

        [UnityTest]
        public IEnumerator DesconhecidoStartsIncompleteAndEachGiftRebuildsPartOfHim()
        {
            var store = MetaProgressStore.CreateDefault(); var profile = store.Load();
            profile.rescuedAllies.Add(MetaAllies.Unknown); profile.seenDialogues.Add(MetaAllies.UnknownNexusDialogueKey); profile.gifts = 3; profile.giftsFoundTotal = 3;
            Assert.IsTrue(store.Save(profile));
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
            var terminal = UnityEngine.Object.FindFirstObjectByType<NexusUpgradeTerminal>();
            Assert.IsNotNull(terminal.Figure, "He stands by the terminal.");
            Assert.AreEqual(0, terminal.Figure.Layers);
            int parts = terminal.Figure.SolidParts;
            for (int layer = 1; layer <= Rapport.UnknownMaxLayers; layer++)
            {
                Assert.IsTrue(terminal.TryGiveGift(), "Gift " + layer);
                Assert.IsFalse(terminal.IsOpen, "No full-screen terminal for a gift.");
                Assert.IsTrue(DialoguePanel.IsOpen); Assert.AreEqual(MetaAllies.UnknownDisplayName, DialoguePanel.Current.Speaker);
                DialoguePanel.Current.Close();
                Assert.AreEqual(layer, terminal.Figure.Layers);
                Assert.GreaterOrEqual(terminal.Figure.SolidParts, parts, "He never loses pieces.");
                if (layer < 3) Assert.Greater(terminal.Figure.SolidParts, parts, "Layer " + layer + " adds body pieces.");
                parts = terminal.Figure.SolidParts;
                yield return null;
            }
            Assert.IsFalse(terminal.TryGiveGift(), "Complete.");
            var saved = Profile();
            Assert.AreEqual(3, saved.GiftsGivenTo(MetaAllies.Unknown)); Assert.AreEqual(0, saved.gifts);
            Assert.IsTrue(saved.HasArtifact(Artifacts.LuckyHook.Id)); Assert.IsTrue(saved.HasSeenDialogue(NexusDialogues.UnknownKey(3)));

            terminal.Chat();
            Assert.IsTrue(DialoguePanel.IsOpen, "Conversar always has a line."); DialoguePanel.Current.Close();
            yield return null;
            terminal.Talk();
            Assert.IsTrue(terminal.IsOpen, "The interaction key still opens the upgrade terminal."); terminal.CloseTerminal();
        }

        [UnityTest]
        public IEnumerator ArtifactCabinetInTheNexusKeepsAndEquipsTheArtifacts()
        {
            var store = MetaProgressStore.CreateDefault();
            store.AddGift(); store.AddGift();
            Assert.IsTrue(store.TryGiveGift(Rapport.Ico, out _)); Assert.IsTrue(store.TryGiveGift(MetaAllies.Unknown, out _));
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
            var cabinet = UnityEngine.Object.FindFirstObjectByType<ArtifactCabinet>();
            Assert.IsNotNull(cabinet, "The Nexus scene has the artifact cabinet.");
            int sparring = Artifacts.All.ToList().IndexOf(Artifacts.SparringWrap), hook = Artifacts.All.ToList().IndexOf(Artifacts.LuckyHook),
                second = Artifacts.All.ToList().IndexOf(Artifacts.SecondRound);
            Assert.IsFalse(cabinet.TryStore(), "Nothing equipped yet.");
            Assert.IsFalse(cabinet.TryToggle(second), "An empty niche cannot be equipped.");
            Assert.IsTrue(cabinet.TryToggle(sparring)); Assert.IsTrue(Profile().HasEquipped(Artifacts.SparringWrap.Id));
            Assert.IsTrue(cabinet.TryToggle(hook)); Assert.IsTrue(Profile().HasEquipped(Artifacts.LuckyHook.Id), "One slot: the new one replaces the old.");
            Assert.IsTrue(cabinet.TryToggle(hook)); Assert.AreEqual("", Profile().equippedArtifact, "Choosing the equipped one puts it back.");
            Assert.IsTrue(cabinet.TryToggle(sparring)); Assert.IsTrue(cabinet.TryStore()); Assert.AreEqual("", Profile().equippedArtifact);
            Assert.AreEqual(1f, Time.timeScale, "The cabinet never freezes the game.");

            // Playtest 12/11/2026: the first position was over the void between the Nexus platforms. The spot in
            // front of the cabinet must be walkable floor connected to where the player arrives.
            Vector3 front = cabinet.transform.position - cabinet.transform.forward * 1.1f;
            Assert.IsTrue(NavMesh.SamplePosition(front, out var standing, .3f, NavMesh.AllAreas), "Floor in front of the cabinet.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var route = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(player.transform.position, standing.position, NavMesh.AllAreas, route) &&
                route.status == NavMeshPathStatus.PathComplete, "The player can walk from the arrival point to the cabinet.");
            Assert.LessOrEqual(Vector3.Distance(standing.position, cabinet.transform.position), ArtifactCabinet.InteractionRadius, "Standing there opens the menu.");

            // It sits apart from the pedestals and the terminal, so prompts never overlap.
            var terminal = UnityEngine.Object.FindFirstObjectByType<NexusUpgradeTerminal>();
            Assert.Greater(Vector3.Distance(cabinet.transform.position, terminal.transform.position),
                ArtifactCabinet.InteractionRadius + NexusUpgradeTerminal.InteractionRadius);
        }
    }
}
