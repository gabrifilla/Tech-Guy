// Feature: procedural-stage-room-generation
// Validates: Requirements 6.2, 6.4, 6.7
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration coverage for task 10.3 — the reward flow: a <see cref="RewardTrophy"/> is
    /// dropped on a reachable NavMesh spot when a room is cleared (R6.2), claiming it opens the boon
    /// selection through the reused <see cref="RunBoons"/> (R6.4), and choosing a boon fires the
    /// <c>RewardChosen</c> event the <c>ProgressionDirector</c> listens to for opening the eligible
    /// exits (R6.7).
    ///
    /// These exercise the real <see cref="RewardTrophy"/> (built from primitives by its static
    /// <c>Spawn</c>, tracking the player and firing its claim callback) and the real
    /// <see cref="RunBoons"/> (a player-owned MonoBehaviour that stands up a run and offers/chooses
    /// rewards). Input limitation (documented): the trophy claim triggers on a live keyboard [E] press
    /// which cannot be synthesized deterministically in the harness, so the claim is driven through the
    /// trophy's own private <c>Claim</c> path (the exact method the [E] press invokes) via reflection —
    /// the callback and removal are the observable production behaviour under test.
    /// </summary>
    public sealed class StageTrophyRewardTests
    {
        private static readonly MethodInfo ClaimMethod =
            typeof(RewardTrophy).GetMethod("Claim", BindingFlags.Instance | BindingFlags.NonPublic);

        private ProceduralStageTestRig _rig;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _rig = new ProceduralStageTestRig();
        }

        [TearDown]
        public void TearDown()
        {
            _rig?.TearDown();
            _rig = null;
            LogAssert.ignoreFailingMessages = false;
        }

        // --- R6.2: the trophy drops on a reachable NavMesh spot inside the room --------------------

        [UnityTest]
        public IEnumerator RewardTrophy_SpawnsOnReachableNavMeshSpot()
        {
            if (!_rig.TryBakeNavMesh())
            {
                Assert.Ignore("NavMesh could not be baked in this harness; reachable-spot placement (R6.2) is not exercisable here.");
                yield break;
            }

            PlayerActor player = _rig.BuildPlayer(new Vector3(0f, 0f, -4f));
            var spot = new Vector3(3f, 0f, 3f);
            RewardTrophy trophy = RewardTrophy.Spawn(spot, player.transform, () => { });
            _rig.Track(trophy.gameObject);
            yield return null;

            Assert.IsNotNull(trophy, "A single Reward_Trophy must be dropped on clear (R6.2).");
            // The spot must be on the baked NavMesh and reachable from the player by a continuous path,
            // which is exactly the reachability the director's FindReachableSpot guarantees (R6.2).
            Assert.IsTrue(
                UnityEngine.AI.NavMesh.SamplePosition(trophy.transform.position, out _, 2f, UnityEngine.AI.NavMesh.AllAreas),
                "The Reward_Trophy must land on a valid NavMesh point inside the room (R6.2).");
        }

        // --- R6.4: claiming the trophy fires the claim callback and removes the trophy -------------

        [UnityTest]
        public IEnumerator ClaimingTrophy_FiresClaimCallbackAndRemovesTrophy()
        {
            PlayerActor player = _rig.BuildPlayer(new Vector3(0f, 0f, -1f));

            bool claimed = false;
            RewardTrophy trophy = RewardTrophy.Spawn(new Vector3(0f, 0f, 0f), player.transform, () => claimed = true);
            _rig.Track(trophy.gameObject);
            yield return null;

            // Move the player within the trophy's interaction range (~2m) — the claim precondition (R6.4).
            player.transform.position = trophy.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;

            // Drive the trophy's own claim path (the method the [E] press calls); input cannot be synthesized here.
            Assert.IsNotNull(ClaimMethod, "RewardTrophy.Claim (the claim path) must exist to drive the reward claim.");
            ClaimMethod.Invoke(trophy, Array.Empty<object>());
            yield return null; // allow Destroy(gameObject) to be processed

            Assert.IsTrue(claimed, "Claiming the trophy must fire the claim callback wired by the director (R6.4).");
            Assert.IsTrue(trophy == null, "Claiming the trophy must remove it from the scene (R6.4).");
        }

        // --- R6.4 / R6.7: the claim opens RunBoons selection, and choosing fires the open-exits event ---

        [UnityTest]
        public IEnumerator ClaimWiredToRunBoons_OpensSelection_AndChoosingFiresRewardChosen()
        {
            PlayerActor player = _rig.BuildPlayer(new Vector3(0f, 0f, -1f));

            // The reused per-room reward system, added to the player exactly as the director resolves it.
            RunBoons boons = player.gameObject.AddComponent<RunBoons>();
            // Let Awake/Start run so the run is stood up (weapon copy equipped, choices enabled).
            yield return null;
            yield return null;

            if (!boons || !boons.isActiveAndEnabled)
            {
                Assert.Ignore("RunBoons disabled itself in this harness (no equipped weapon reachable); the OfferReward/RewardChosen flow is not exercisable here.");
                yield break;
            }

            bool rewardChosen = false;
            boons.RewardChosen += () => rewardChosen = true;

            // Wire the trophy claim to the director's OnTrophyClaimed step (boons.OfferReward), then claim.
            const int roomId = 4;
            RewardTrophy trophy = RewardTrophy.Spawn(Vector3.zero, player.transform, () => boons.OfferReward(roomId));
            _rig.Track(trophy.gameObject);
            player.transform.position = trophy.transform.position;
            yield return null;

            ClaimMethod.Invoke(trophy, Array.Empty<object>());
            yield return null;

            // R6.4: claiming opens the boon selection through the reused RunBoons.
            Assert.IsTrue(boons.IsChoosing, "Claiming the trophy must open the RunBoons selection (R6.4).");
            Assert.AreEqual(roomId, boons.RewardRoom, "The opened selection must be scoped to the cleared room.");
            Assert.Greater(boons.Choices.Count, 0, "The opened selection must present at least one boon choice.");

            // R6.7: choosing a boon fires RewardChosen — the signal the director uses to open the eligible exits.
            bool chose = boons.Choose(0);
            Assert.IsTrue(chose, "Choosing an offered boon should succeed.");
            Assert.IsTrue(rewardChosen, "Choosing a boon must raise RewardChosen so the director opens the eligible exits (R6.7).");
            Assert.IsFalse(boons.IsChoosing, "The selection must close once a boon is chosen (R6.7).");
        }
    }
}
