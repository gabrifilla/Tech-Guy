using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 11 of gauntlet-boon-playstyle-overhaul — the Gauntlet's
    /// "Guarda partida" (<c>GuardBreaker</c>, R5) streak, exercised at the <c>ImpactGuardTracker</c>
    /// level (the tracker wiring + the stance-break reaction it requests), one layer above the pure
    /// <see cref="ConsecutiveHitCounter"/> covered by <c>ConsecutiveHitCounterPropertyTests</c>.
    ///
    /// Property 11 (design, tracker facet): the tracker counts <b>only</b> basic hits delivered on
    /// the <see cref="HookBus.OnBasicHit"/> channel (R5.3) and resets the streak whenever a skill is
    /// used on the <c>AbilityHolder.AbilityUsed</c> channel (R5.3); every third consecutive basic hit
    /// breaks the guard (R5.1) and, on that breaking hit, requests a <c>Stagger</c>/<c>Heavy</c>
    /// reaction whose stance damage is scaled by <c>1 + 0.5R</c> with the deliberate
    /// <see cref="StanceBreakEffect.Knockback"/> break effect (R5.1/R5.2).
    ///
    /// Approach (mirrors <c>BasicHitChannelPropertyTests</c>): the real decision core lives in the
    /// per-run <see cref="HookBus"/> + the pure <see cref="ConsecutiveHitCounter"/> + the stance
    /// multiplier formula, so the test drives the <b>real</b> <see cref="HookBus"/> and a
    /// test-local tracker (<see cref="TrackerSpy"/>) that transcribes <c>ImpactGuardTracker</c>'s
    /// documented wiring verbatim: subscribe <c>OnBasicHit</c> to count, treat an <c>AbilityUsed</c>
    /// notification as a reset, and on the breaking hit record the exact reaction arguments it would
    /// hand to <c>PlayerActor.ApplyHitReactionTo</c> (the "reaction spy"). This keeps the test free of
    /// a live <c>CombatReactionController</c>/scene while asserting the full observable contract.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the seeded
    /// <see cref="PropertyCheck"/> harness drives &gt;= 100 deterministic cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 11
    public sealed class GuardBreakerPropertyTests
    {
        private const int HitsToBreak = 3;        // R5.1: every third consecutive basic hit breaks.
        private const float BaseStanceOnThird = 24f; // design starting point for the breaking hit's stance.
        private const float Tolerance = 1e-4f;

        /// <summary>One captured stance-break reaction the tracker requested on a breaking basic hit.</summary>
        private readonly struct ReactionCapture
        {
            public readonly HitReactionType ReactionType;
            public readonly HitStrength Strength;
            public readonly float StanceDamage;
            public readonly StanceBreakEffect BreakEffect;

            public ReactionCapture(HitReactionType reactionType, HitStrength strength,
                float stanceDamage, StanceBreakEffect breakEffect)
            {
                ReactionType = reactionType;
                Strength = strength;
                StanceDamage = stanceDamage;
                BreakEffect = breakEffect;
            }
        }

        /// <summary>
        /// Verbatim transcription of <c>ImpactGuardTracker</c>'s documented wiring (design R5):
        /// drives a real <see cref="ConsecutiveHitCounter"/> from <see cref="HookBus.OnBasicHit"/>
        /// (count) and resets it on an <c>AbilityUsed</c> notification (the <c>AbilityHolder.AbilityUsed</c>
        /// channel). On the breaking (third) hit it records the exact reaction it would request via
        /// <c>PlayerActor.ApplyHitReactionTo(victim, Stagger, Heavy, 24 * (1 + 0.5R), Knockback, 0f)</c>.
        /// The counter and bus are real; only the <c>PlayerActor</c>/scene reaction sink is replaced by
        /// the capture list (the "reaction spy").
        /// </summary>
        private sealed class TrackerSpy
        {
            private readonly ConsecutiveHitCounter _counter = new ConsecutiveHitCounter(HitsToBreak);
            private readonly int _rank;
            public readonly List<ReactionCapture> Reactions = new List<ReactionCapture>();
            public readonly List<int> StreakHistory = new List<int>();

            public TrackerSpy(HookBus hooks, int rank)
            {
                _rank = rank;
                // R5.3: only the basic channel increments the streak.
                hooks.OnBasicHit += OnBasicHit;
            }

            public int Streak => _counter.Count;

            // R5.1/R5.2: on the third basic hit, request stance = 24 * (1 + 0.5R) with Knockback.
            private void OnBasicHit(Actor victim, float damage)
            {
                if (_rank <= 0) return;
                bool breaks = _counter.RegisterBasicHit();
                StreakHistory.Add(_counter.Count);
                if (!breaks) return;
                float stance = BaseStanceOnThird * ConsecutiveHitCounter.StanceMultiplier(_rank);
                Reactions.Add(new ReactionCapture(
                    HitReactionType.Stagger, HitStrength.Heavy, stance, StanceBreakEffect.Knockback));
            }

            // R5.3: a skill use (AbilityHolder.AbilityUsed) resets the running streak.
            public void OnAbilityUsed(int slot)
            {
                _counter.Reset();
                StreakHistory.Add(_counter.Count);
            }
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 11: Guarda partida ao terceiro básico
        // Over an uninterrupted run of basic hits on OnBasicHit, the tracker breaks the guard on every
        // third hit (R5.1): a break occurs iff (hit % 3 == 0), the number of breaks equals floor(hits/3),
        // and each break requests a Stagger/Heavy/Knockback reaction whose stance = 24 * (1 + 0.5R).
        // Validates: Requirements 5.1, 5.2
        [Test]
        public void EveryThirdBasicHitBreaksGuard_WithScaledStanceAndKnockback()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);          // 1..3 (rank 0 handled separately below)
                int hits = rng.Next(0, 25);         // 0..24 consecutive basic hits
                float expectedStance = BaseStanceOnThird * (1f + 0.5f * rank);

                var bus = new HookBus();
                var tracker = new TrackerSpy(bus, rank);

                int expectedBreaks = 0;
                for (int hit = 1; hit <= hits; hit++)
                {
                    int reactionsBefore = tracker.Reactions.Count;
                    bus.RaiseBasicHit(NullVictim, 10f);
                    bool broke = tracker.Reactions.Count > reactionsBefore;
                    bool shouldBreak = (hit % HitsToBreak) == 0;

                    PropertyCheck.That(broke == shouldBreak,
                        $"[case #{i}] rank={rank} hit#{hit}: broke={broke}, expected {shouldBreak}");

                    if (shouldBreak) expectedBreaks++;
                }

                // The number of breaks equals floor(hits / 3).
                PropertyCheck.That(tracker.Reactions.Count == expectedBreaks,
                    $"[case #{i}] rank={rank} hits={hits}: {tracker.Reactions.Count} breaks, expected {expectedBreaks}");

                // Every requested reaction carries the deliberate Knockback break and scaled stance (R5.1/R5.2).
                foreach (ReactionCapture reaction in tracker.Reactions)
                {
                    PropertyCheck.That(reaction.ReactionType == HitReactionType.Stagger,
                        $"[case #{i}] rank={rank}: reaction type {reaction.ReactionType} != Stagger");
                    PropertyCheck.That(reaction.Strength == HitStrength.Heavy,
                        $"[case #{i}] rank={rank}: reaction strength {reaction.Strength} != Heavy");
                    PropertyCheck.That(reaction.BreakEffect == StanceBreakEffect.Knockback,
                        $"[case #{i}] rank={rank}: break effect {reaction.BreakEffect} != Knockback (R5.2)");
                    PropertyCheck.That(Math.Abs(reaction.StanceDamage - expectedStance) <= Tolerance,
                        $"[case #{i}] rank={rank}: stance {reaction.StanceDamage} != expected {expectedStance} (1 + 0.5R)");
                }
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 11: Guarda partida ao terceiro básico
        // Any interruption resets the streak (R5.3): a skill use (AbilityHolder.AbilityUsed) mid-run
        // clears the running count, so a streak that was one short of breaking never breaks across the
        // interruption, and the next three basic hits are required to break again.
        // Validates: Requirements 5.3
        [Test]
        public void SkillUseResetsStreak_SoNearCompleteRunNeverBreaksAcrossInterruption()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);

                var bus = new HookBus();
                var tracker = new TrackerSpy(bus, rank);

                // Build a partial streak of 1 or 2 hits (never enough to break on its own).
                int partial = rng.Next(1, HitsToBreak); // 1..2
                for (int hit = 1; hit <= partial; hit++) bus.RaiseBasicHit(NullVictim, 10f);

                PropertyCheck.That(tracker.Reactions.Count == 0,
                    $"[case #{i}] rank={rank}: a {partial}-hit partial run broke early");
                PropertyCheck.That(tracker.Streak == partial,
                    $"[case #{i}] rank={rank}: streak {tracker.Streak} != partial {partial} before interruption");

                // Interruption: a skill was used (R5.3) — the streak resets to 0.
                tracker.OnAbilityUsed(rng.Next(0, 5));
                PropertyCheck.That(tracker.Streak == 0,
                    $"[case #{i}] rank={rank}: skill use did not reset the streak (streak={tracker.Streak})");

                // After the reset, the remaining hits that would have completed the PRE-reset run must
                // NOT break — a fresh run of three is required.
                for (int extra = 1; extra <= HitsToBreak - partial; extra++)
                {
                    bus.RaiseBasicHit(NullVictim, 10f);
                    PropertyCheck.That(tracker.Reactions.Count == 0,
                        $"[case #{i}] rank={rank}: hit {extra} after reset broke early (pre-reset streak leaked)");
                }

                // Completing a full fresh run of three breaks exactly once.
                int needed = HitsToBreak - (HitsToBreak - partial); // = partial more hits to reach 3 post-reset
                for (int extra = 0; extra < needed; extra++) bus.RaiseBasicHit(NullVictim, 10f);
                PropertyCheck.That(tracker.Reactions.Count == 1,
                    $"[case #{i}] rank={rank}: a fresh run of {HitsToBreak} basic hits broke {tracker.Reactions.Count} times, expected 1");
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 11: Guarda partida ao terceiro básico
        // Rank 0 (boon not acquired) never breaks the guard regardless of how many basic hits land —
        // the tracker only reacts when it has a positive rank, so the OnBasicHit channel is inert.
        // Validates: Requirements 5.1
        [Test]
        public void RankZeroNeverBreaks()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int hits = rng.Next(3, 30);
                var bus = new HookBus();
                var tracker = new TrackerSpy(bus, 0);

                for (int hit = 1; hit <= hits; hit++) bus.RaiseBasicHit(NullVictim, 10f);

                PropertyCheck.That(tracker.Reactions.Count == 0,
                    $"[case #{i}] hits={hits}: rank 0 broke the guard {tracker.Reactions.Count} times, expected 0");
                PropertyCheck.That(tracker.Streak == 0,
                    $"[case #{i}] hits={hits}: rank 0 should never advance the streak (streak={tracker.Streak})");
            });
        }

        // Example: the canonical third-hit break for each rank requests the Knockback reaction with the
        // documented stance = 24 * (1 + 0.5R), anchoring the property's scaling clause with concrete values.
        [Test]
        public void ThirdHit_RequestsKnockbackWithRankScaledStance()
        {
            foreach (var (rank, expected) in new[] { (1, 36f), (2, 48f), (3, 60f) })
            {
                var bus = new HookBus();
                var tracker = new TrackerSpy(bus, rank);

                bus.RaiseBasicHit(NullVictim, 10f);
                bus.RaiseBasicHit(NullVictim, 10f);
                Assert.That(tracker.Reactions, Is.Empty, $"rank {rank}: broke before the third hit.");

                bus.RaiseBasicHit(NullVictim, 10f);
                Assert.That(tracker.Reactions, Has.Count.EqualTo(1), $"rank {rank}: third hit must break once.");

                ReactionCapture reaction = tracker.Reactions[0];
                Assert.That(reaction.ReactionType, Is.EqualTo(HitReactionType.Stagger), $"rank {rank}: reaction type.");
                Assert.That(reaction.Strength, Is.EqualTo(HitStrength.Heavy), $"rank {rank}: reaction strength.");
                Assert.That(reaction.BreakEffect, Is.EqualTo(StanceBreakEffect.Knockback), $"rank {rank}: break effect.");
                Assert.That(reaction.StanceDamage, Is.EqualTo(expected).Within(Tolerance),
                    $"rank {rank}: stance should be 24 * (1 + 0.5*{rank}) = {expected}.");
            }
        }

        // The tracker's reaction spy records arguments only; it never dereferences the victim, so a null
        // victim is a valid stand-in for "some enemy was hit" on the OnBasicHit channel without a scene.
        private static Actor NullVictim => null;
    }
}
