using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free <see cref="CancelResolver"/> of
    /// combat-foundation-rework (task 4.4), covering the single shared cancel authorization and its
    /// safe 4-step ordering (R4.2, R4.3, R4.4).
    ///
    /// <para>
    /// Each property is checked against the real production <see cref="CancelResolver.Resolve"/>
    /// (not a re-stated model) through the project's seeded <see cref="PropertyCheck"/> harness over
    /// <see cref="PropertyCheck.DefaultCases"/> (&gt;= 100) deterministic generated cases, since no
    /// FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>P2 — a cancel is <see cref="CancelDecision.Authorized"/> if and only if a rule exists
    /// for the destination and is open at <c>progress</c>, the command is still valid, the
    /// destination is available, and the transition is feasible (R4.3).</item>
    /// <item>P3 — a destination with no rule in the set is never authorized for any <c>progress</c>;
    /// the decision is always <see cref="CancelDecision.DeniedWindowClosed"/> (R4.2).</item>
    /// <item>P4 — when the rule is open and the command is valid but the destination is unavailable,
    /// the decision is <see cref="CancelDecision.DeniedUnavailable"/> regardless of transition
    /// feasibility; the current action is preserved (R4.4).</item>
    /// </list>
    /// </summary>
    // Feature: combat-foundation-rework, task 4.4. Requirements: R4.2, R4.3, R4.4.
    public sealed class CancelResolverPropertyTests
    {
        // Draws any of the three cancel destinations so the properties are exercised across targets.
        private static CancelTarget GenTarget(Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return CancelTarget.Dash;
                case 1: return CancelTarget.Basic;
                default: return CancelTarget.Skill;
            }
        }

        // Returns the destination other than the given one, used by P3 to seed a set that holds a
        // rule for a DIFFERENT destination than the one being resolved (so the queried destination
        // genuinely has no rule).
        private static CancelTarget OtherTarget(Random rng, CancelTarget target)
        {
            switch (target)
            {
                case CancelTarget.Dash: return rng.Next(0, 2) == 0 ? CancelTarget.Basic : CancelTarget.Skill;
                case CancelTarget.Basic: return rng.Next(0, 2) == 0 ? CancelTarget.Dash : CancelTarget.Skill;
                default: return rng.Next(0, 2) == 0 ? CancelTarget.Dash : CancelTarget.Basic;
            }
        }

        // Draws a normalized position in [0, 1], biased to include the exact window edges so the
        // inclusive IsOpenAt boundary is exercised.
        private static float GenProgress(Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return 1f;
                default: return (float)rng.NextDouble();
            }
        }

        // Feature: combat-foundation-rework, Property 2: Resolve authorizes a cancel if and only if
        // a rule exists for the destination and is open at progress, the command is still valid, the
        // destination is available, and the transition is feasible.
        // Validates: Requirements 4.3
        [Test]
        public void AuthorizesIffRuleOpenCommandValidAvailableAndFeasible()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                CancelTarget target = GenTarget(rng);

                // A single rule for the target with randomized bounds, so sometimes the window is
                // open at progress and sometimes it is not.
                float start = (float)rng.NextDouble();
                float end = (float)rng.NextDouble();
                var rule = new CancelRule(target, start, end);
                var rules = new CancelRuleSet();
                rules.Add(rule);

                float progress = GenProgress(rng);
                bool commandStillValid = rng.Next(0, 2) == 0;
                bool available = rng.Next(0, 2) == 0;
                bool transitionFeasible = rng.Next(0, 2) == 0;

                DestinationAvailability availability = t => available;

                CancelDecision decision = CancelResolver.Resolve(
                    rules, target, progress, commandStillValid, availability, transitionFeasible);

                // The reference biconditional: every one of the four checks must pass.
                bool windowOpen = rule.IsOpenAt(progress);
                bool expectedAuthorized = windowOpen && commandStillValid && available && transitionFeasible;

                bool actualAuthorized = decision == CancelDecision.Authorized;

                PropertyCheck.That(actualAuthorized == expectedAuthorized,
                    $"target={target}, start={start}, end={end} (rule=[{rule.Start},{rule.End}]), "
                    + $"progress={progress}, windowOpen={windowOpen}, commandStillValid={commandStillValid}, "
                    + $"available={available}, transitionFeasible={transitionFeasible}: "
                    + $"Resolve returned {decision} (authorized={actualAuthorized}) but expected authorized={expectedAuthorized}.");
            });
        }

        // Feature: combat-foundation-rework, Property 3: a destination with no rule in the set is
        // never authorized for any progress; the decision is always DeniedWindowClosed.
        // Validates: Requirements 4.2
        [Test]
        public void DestinationWithoutRuleIsNeverAuthorizedForAnyProgress()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                CancelTarget target = GenTarget(rng);

                // Build a set that has NO rule for the queried target. Half the time it is empty,
                // half the time it holds a rule for a different destination only.
                var rules = new CancelRuleSet();
                if (rng.Next(0, 2) == 0)
                {
                    CancelTarget other = OtherTarget(rng, target);
                    rules.Add(new CancelRule(other, (float)rng.NextDouble(), (float)rng.NextDouble()));
                }

                float progress = GenProgress(rng);
                // The other inputs are irrelevant to the outcome, but randomize them to prove the
                // window-closed decision does not depend on them.
                bool commandStillValid = rng.Next(0, 2) == 0;
                bool available = rng.Next(0, 2) == 0;
                bool transitionFeasible = rng.Next(0, 2) == 0;
                DestinationAvailability availability = t => available;

                CancelDecision decision = CancelResolver.Resolve(
                    rules, target, progress, commandStillValid, availability, transitionFeasible);

                PropertyCheck.That(decision != CancelDecision.Authorized,
                    $"target={target} (no rule), progress={progress}: Resolve returned {decision}; "
                    + "a destination with no rule must never be authorized.");
                PropertyCheck.That(decision == CancelDecision.DeniedWindowClosed,
                    $"target={target} (no rule), progress={progress}: Resolve returned {decision} "
                    + "but a missing rule must yield DeniedWindowClosed.");
            });
        }

        // Feature: combat-foundation-rework, Property 4: when the rule is open and the command is
        // valid but the destination is unavailable, Resolve returns DeniedUnavailable regardless of
        // transition feasibility, preserving the current action.
        // Validates: Requirements 4.4
        [Test]
        public void RuleOpenButUnavailableYieldsDeniedUnavailable()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                CancelTarget target = GenTarget(rng);

                // Construct a progress that is guaranteed inside the window: pick the bounds first,
                // then draw progress within [Start, End] so IsOpenAt(progress) is always true.
                float a = (float)rng.NextDouble();
                float b = (float)rng.NextDouble();
                float lo = Math.Min(a, b);
                float hi = Math.Max(a, b);
                var rule = new CancelRule(target, lo, hi);
                float progress = rule.Start + (float)rng.NextDouble() * (rule.End - rule.Start);

                var rules = new CancelRuleSet();
                rules.Add(rule);

                // Command valid and window open, but the destination is explicitly unavailable.
                const bool commandStillValid = true;
                DestinationAvailability availability = t => false;
                // Transition feasibility is irrelevant because availability fails first.
                bool transitionFeasible = rng.Next(0, 2) == 0;

                CancelDecision decision = CancelResolver.Resolve(
                    rules, target, progress, commandStillValid, availability, transitionFeasible);

                PropertyCheck.That(rule.IsOpenAt(progress),
                    $"target={target}, rule=[{rule.Start},{rule.End}], progress={progress}: "
                    + "generator bug — progress should be inside the open window.");
                PropertyCheck.That(decision == CancelDecision.DeniedUnavailable,
                    $"target={target}, rule=[{rule.Start},{rule.End}], progress={progress}, "
                    + $"transitionFeasible={transitionFeasible}: Resolve returned {decision} but an open "
                    + "rule with an unavailable destination must yield DeniedUnavailable (current action preserved).");
            });
        }
    }
}
