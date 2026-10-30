using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
private RunBoons _titanRun;
private static string TitanicLegacyEvidenceDirectory
        {
            get
            {
                string path = System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" +
                    UnityEditor.SessionState.GetString("TechGuy.Tests.GprPhase", "gpr-01-2b"));
                System.IO.Directory.CreateDirectory(path); return path;
            }
        }

private IEnumerator PrepareTitanic(bool acquire = true)
        {
            BuildAuthoredBasicPlayer(); _titanRun = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            if (acquire) Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_titanRun, new object[] { new RunBoons.Offer(TitanicFistsDefinition.Id, "fixture", "Assisted grant; not reward evidence"), false }));
            yield return null;
        }

[UnityTest] public IEnumerator TitanicCatalogTargetsBasicAndRejectsDuplicateOrOtherWeapons()
        {
            yield return PrepareTitanic(false);
            var pool = ManualRewardCatalog.Eligible(RunRewardKind.Mutation, _titanRun.WeaponModifiers, _titanRun.RunWeapon, _titanRun.Protocols);
            Assert.AreEqual(1, pool.Count(x => x.Id == TitanicFistsDefinition.Id));
            StringAssert.Contains("LMB", pool.Single(x => x.Id == TitanicFistsDefinition.Id).Description);
            Assert.IsTrue(BoonExpansionCatalog.TryAcquire(TitanicFistsDefinition.Id, _titanRun.WeaponModifiers, _titanRun.RunWeapon, _titanRun.Protocols, out var error), error);
            Assert.IsFalse(BoonExpansionCatalog.TryAcquire(TitanicFistsDefinition.Id, _titanRun.WeaponModifiers, _titanRun.RunWeapon, _titanRun.Protocols, out _));
            Assert.IsFalse(BoonExpansionCatalog.HasTarget(TitanicFistsDefinition.Create(), Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1])));
            Assert.IsFalse(BoonExpansionCatalog.HasTarget(TitanicFistsDefinition.Create(), Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2])));
            Assert.AreEqual(15, (int)BoonSlotMask.Any);
        }

[UnityTest] public IEnumerator TitanicAcquisitionKeepsCooldownsResourcesAndVisualCleansWithRun()
        {
            yield return PrepareTitanic(false); var holder = _player.GetComponent<AbilityHolder>();
            var timers = (float[])typeof(AbilityHolder).GetField("cooldownTimers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(holder);
            for (int i = 0; i < 4; i++) timers[i] = i + 3;
            float[] before = timers.ToArray(); float mana = _player.mana; int energy = _player.GetComponent<BreakerGauntletCombat>().Energy;
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_titanRun, new object[] { new RunBoons.Offer(TitanicFistsDefinition.Id, "fixture", "debug acquisition"), false }));
            CollectionAssert.AreEqual(before, timers); Assert.AreEqual(mana, _player.mana); Assert.AreEqual(energy, _player.GetComponent<BreakerGauntletCombat>().Energy);
            yield return null; var visual = _player.GetComponent<TitanicFistsVisual>(); Assert.IsTrue(visual.OverlayActive);
            typeof(RunBoons).GetMethod("ClearRunState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_titanRun, null);
            yield return null; Assert.IsFalse(visual.OverlayActive); Assert.IsFalse(visual.WaveActive);
            Assert.IsNull(_titanRun.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic));
        }

[UnityTest] public IEnumerator TitanicRealRewardTicketOffersAcquiresOnceAndCountsMutationWithoutGrant()
        {
            yield return PrepareTitanic(false);
            var baseRoute = BuildManualRewardRoute(); var source = baseRoute.Session.Route;
            var pool = ManualRewardCatalog.Eligible(RunRewardKind.Mutation, _titanRun.WeaponModifiers, _titanRun.RunWeapon, _titanRun.Protocols);
            ulong foundSeed = 0; bool found = false;
            for (ulong seed = 0; seed < 128 && !found; seed++)
            {
                var probe = new ManualRoutePolicy(new ManualRouteSnapshot(source, source.Nodes, seed));
                foreach (int node in new[] { 1, 2, 4, 5 })
                { ReachManualReward(probe, node); if (node != 5) Assert.IsTrue(probe.TryCompleteReward(probe.Session.Id, node, 0, false, RewardReady)); }
                var ticket = new RunRewardPolicy(probe).Open(new RunRewardRequest(probe.Session.Id, 5, 0), RewardReady, "seed probe", pool);
                if (ticket.Choices.Any(x => x.Id == TitanicFistsDefinition.Id)) { found = true; foundSeed = seed; }
            }
            Assert.IsTrue(found, "No eligible seed in the bounded probe; do not change reward policy to conceal this.");
            var route = new ManualRoutePolicy(new ManualRouteSnapshot(source, source.Nodes, foundSeed));
            Assert.IsTrue(_titanRun.BindManualRun(route));
            Assert.IsNull(_titanRun.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic));
            foreach (int node in new[] { 1, 2, 4, 5 })
            {
                ReachManualReward(route, node);
                var request = new RunRewardRequest(route.Session.Id, node, 0);
                Assert.IsTrue(_titanRun.OfferReward(request)); yield return null; yield return null;
                Assert.AreEqual(_titanRun.Choices.Count, _titanRun.Choices.Select(x => x.Id).Distinct().Count());
                int index = node == 5 ? _titanRun.Choices.ToList().FindIndex(x => x.Id == TitanicFistsDefinition.Id) : 0;
                Assert.GreaterOrEqual(index, 0); Assert.IsTrue(_titanRun.Choose(index));
                if (node == 5) Assert.IsFalse(_titanRun.Choose(index));
            }
            Assert.AreEqual(1, route.Session.MutationAcquisitions);
            Assert.AreEqual(RunRewardTicketState.Resolved, _titanRun.ManualTicket.State);
            Assert.AreEqual(TitanicFistsDefinition.Id, _titanRun.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic));
            yield return null; Assert.IsTrue(_player.GetComponent<TitanicFistsVisual>().OverlayActive);
            System.IO.File.WriteAllText(System.IO.Path.Combine(TitanicLegacyEvidenceDirectory, "reward-ticket.json"),
                JsonUtility.ToJson(new TitanicRewardEvidence { seed = foundSeed.ToString(), node = 5, mutationAcquisitions = route.Session.MutationAcquisitions,
                    id = _titanRun.ManualTicket.ResultId, cards = _titanRun.ManualTicket.Choices.Select(x => x.Id).ToArray(),
                    status = _titanRun.ManualTicket.State.ToString(), fixtureRoute = true, debugGrant = false }));
        }

[Serializable] private sealed class TitanicRewardEvidence
        { public string seed, id, status; public int node, mutationAcquisitions; public string[] cards; public bool fixtureRoute, debugGrant; }

private IEnumerator TitanicCapture(string name)
        {
            string path = System.IO.Path.Combine(TitanicLegacyEvidenceDirectory, name + ".png");
            var previousWrite = System.IO.File.Exists(path) ? System.IO.File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!System.IO.File.Exists(path) || System.IO.File.GetLastWriteTimeUtc(path) <= previousWrite)
            { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null;
        }

private sealed class TitanicCaptureCameras : IDisposable
        {
            private readonly Camera[] _previous = Camera.allCameras;
            internal TitanicCaptureCameras() { foreach (var camera in _previous) if (camera) camera.enabled = false; }
            public void Dispose() { foreach (var camera in _previous) if (camera) camera.enabled = true; }
        }
    }
}
