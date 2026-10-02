using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration tests for the authored Manopla kit (task 15.4, R2.8, R3.6, R3.9). They
    /// load each shipped Breaker ability asset (Q/W/E/R), read its authored
    /// <see cref="CombatActionProfile"/>, and drive the production
    /// <see cref="ActionImpactScheduler"/> across a full normalized <c>0 → 1</c> progress sweep — the
    /// same single advance source the executor runs every Quadro_de_Simulacao — asserting that each
    /// ability emits exactly the number of impacts it authors (R2.8):
    /// <list type="bullet">
    /// <item>Q — Avanço Relâmpago: advance + 2 punches ⇒ 2 ImpactEvents.</item>
    /// <item>W — Punhos Relâmpago: a flurry sequence + its finisher ⇒ 7 ImpactEvents (last is the finisher).</item>
    /// <item>E — Impacto de Choque: 2 impacts.</item>
    /// <item>R — Rajada Asura: the burst + its circular finisher ⇒ 18 ImpactEvents (last is the finisher).</item>
    /// </list>
    ///
    /// <para>
    /// Driving the scheduler against the AUTHORED <see cref="ImpactEvent"/>s (rather than a live
    /// end-to-end <see cref="BreakerGauntletCombat"/> cast, which needs a weapon prefab, hitbox,
    /// Animator, mana, and a charged Asura meter and would make exact per-frame emission timing
    /// non-deterministic) is the robust path the task prefers: the scheduler is the SINGLE place that
    /// decides emission from the authored events, so the sweep count is exactly what a live cast would
    /// emit through the same scheduler, verified deterministically. The assets are loaded through the
    /// editor's asset database (these run under the editor's PlayMode), so the tests assert the real
    /// shipped data, not a hand-built copy.
    /// </para>
    ///
    /// <para>
    /// The R (Asura) Channel-preservation assertion (R3.6, R3.9) checks that the shipped Rajada Asura
    /// keeps its current control: it authors <see cref="CommitmentCategory.Channel"/> with
    /// <see cref="ChannelTerminationMode.Timed"/> (NOT Hold), so it is not hold-terminated early and
    /// its fixed-duration sequence plays out verbatim — exactly the "preserve the Asura's current
    /// control" decision recorded for task 15.3.
    /// </para>
    /// </summary>
    public sealed class BreakerKitEmissionPlayModeTests
    {
        private const string AssetDir = "Assets/_Project/ScriptableObjects/Abilities/Weapon/";

        // R2.8 (Q): Avanço Relâmpago authors the advance plus 2 punches as 2 discrete ImpactEvents;
        // a full sweep emits exactly 2 impacts, once each.
        [UnityTest]
        public IEnumerator Q_Advance_EmitsAuthoredTwoImpacts()
        {
            AssertSweepEmitsAuthoredCount("BreakerAdvance", expectedCount: 2);
            yield return null;
        }

        // R2.8 (W): Punhos Relâmpago authors a flurry sequence plus a finisher as 7 ImpactEvents; a
        // full sweep emits exactly 7, and the LAST emitted impact is the authored finisher.
        [UnityTest]
        public IEnumerator W_Flurry_EmitsSequencePlusFinisher()
        {
            CombatActionProfile profile = LoadProfile("BreakerFlurry");
            List<int> order = SweepEmit(profile);

            Assert.AreEqual(7, order.Count, "Flurry authors a 6-hit sequence + finisher = 7 impacts (R2.8)");
            AssertLastIsFinisher(profile, order);
            yield return null;
        }

        // R2.8 (E): Impacto de Choque authors 2 impacts; a full sweep emits exactly 2.
        [UnityTest]
        public IEnumerator E_Shock_EmitsAuthoredTwoImpacts()
        {
            AssertSweepEmitsAuthoredCount("BreakerShock", expectedCount: 2);
            yield return null;
        }

        // R2.8 (R): Rajada Asura authors the burst + its circular finisher as 18 ImpactEvents; a full
        // sweep emits exactly 18, and the LAST emitted impact is the circular finisher.
        [UnityTest]
        public IEnumerator R_Asura_EmitsBurstPlusCircularFinisher()
        {
            CombatActionProfile profile = LoadProfile("BreakerAsura");
            List<int> order = SweepEmit(profile);

            Assert.AreEqual(18, order.Count, "Asura authors a 17-hit burst + circular finisher = 18 impacts (R2.8)");
            AssertLastIsFinisher(profile, order);
            yield return null;
        }

        // R3.6 / R3.9: the shipped Rajada Asura preserves its current control. It authors a Channel
        // commitment (so it is a sustained action) but with Timed termination — NOT Hold — so task
        // 15.3's generic Channel-Hold early-termination path stays dormant for it and its fixed
        // sequence is not cut short by releasing the skill control. This pins the "preserve Asura
        // control" decision as authored data, independent of any live cast.
        [UnityTest]
        public IEnumerator R_Asura_IsChannelButTimed_NotHoldTerminated()
        {
            CombatActionProfile profile = LoadProfile("BreakerAsura");

            CommitmentCategory commitment = profile.ResolveCommitment(out _);
            Assert.AreEqual(CommitmentCategory.Channel, commitment,
                "Rajada Asura is a Channel action (R3.6)");
            Assert.AreEqual(ChannelTerminationMode.Timed, profile.ChannelTermination,
                "Rajada Asura preserves its control: Timed termination, never Hold-terminated early (R3.9)");
            yield return null;
        }

        // --- helpers ---------------------------------------------------------

        private void AssertSweepEmitsAuthoredCount(string assetName, int expectedCount)
        {
            CombatActionProfile profile = LoadProfile(assetName);
            Assert.AreEqual(expectedCount, profile.ImpactEvents.Count,
                $"{assetName} must author exactly {expectedCount} ImpactEvents");

            List<int> order = SweepEmit(profile);
            Assert.AreEqual(expectedCount, order.Count,
                $"{assetName} must emit exactly {expectedCount} impacts across a full sweep (R2.8)");

            // No index emits twice across the sweep (dedup through the ledger).
            CollectionAssert.AllItemsAreUnique(order, "no authored impact may emit more than once");
        }

        // Builds the scheduler from the authored profile and advances it in fine, monotonically
        // increasing ticks over [0, 1], returning the ordered list of emitted ImpactEvent indices.
        // 400 ticks (step 0.0025) resolves even the Asura's closely spaced instants (~0.045 apart)
        // and the two Asura impacts that share At == 0.733, so each authored event is crossed.
        private static List<int> SweepEmit(CombatActionProfile profile)
        {
            var emitted = new List<int>();
            var scheduler = new ActionImpactScheduler(
                ExecutionId.Next(),
                new ExecutionImpactLedger(),
                profile.BuildTimeline(),
                profile.ImpactEvents,
                impact => emitted.Add(impact.Index),
                impact => emitted.Add(impact.Index)); // windows (none authored in the kit) also count

            const int ticks = 400;
            for (int i = 0; i <= ticks; i++)
            {
                scheduler.Advance(i / (float)ticks);
            }

            return emitted;
        }

        // Asserts the LAST emitted impact corresponds to the authored finisher — the ImpactEvent whose
        // referenced HitStopProfile is classified Finisher. The kit authors exactly one Finisher-class
        // profile per skill, referenced by the final impact, so the last emission must be it.
        private static void AssertLastIsFinisher(CombatActionProfile profile, List<int> order)
        {
            Assert.Greater(order.Count, 0, "a sweep must emit at least one impact");
            int lastIndex = order[order.Count - 1];

            // Find the authored ImpactEvent with that Index and resolve its hit-stop class.
            ImpactEvent last = default;
            bool found = false;
            foreach (ImpactEvent e in profile.ImpactEvents)
            {
                if (e.Index == lastIndex) { last = e; found = true; break; }
            }

            Assert.IsTrue(found, "the last emitted index must map to an authored ImpactEvent");
            Assert.GreaterOrEqual(last.HitStopProfileIndex, 0,
                "the finisher impact references a hit-stop profile");
            Assert.Less(last.HitStopProfileIndex, profile.HitStopProfiles.Count,
                "the finisher's hit-stop profile index is in range");
            Assert.AreEqual(HitStopClass.Finisher,
                profile.HitStopProfiles[last.HitStopProfileIndex].HitStopClass,
                "the final emitted impact is the authored finisher (R2.8)");
        }

        private static CombatActionProfile LoadProfile(string assetName)
        {
#if UNITY_EDITOR
            var ability = UnityEditor.AssetDatabase.LoadAssetAtPath<BreakerGauntletAbility>(
                AssetDir + assetName + ".asset");
            Assert.IsNotNull(ability, $"the shipped {assetName} ability asset must load");
            CombatActionProfile profile = ability.CombatProfile;
            Assert.IsNotNull(profile, $"{assetName} must author a CombatProfile");
            Assert.IsNotNull(profile.ImpactEvents, $"{assetName} must author ImpactEvents");
            return profile;
#else
            Assert.Ignore("Kit-emission tests read authored assets through the editor asset database.");
            return null;
#endif
        }
    }
}
