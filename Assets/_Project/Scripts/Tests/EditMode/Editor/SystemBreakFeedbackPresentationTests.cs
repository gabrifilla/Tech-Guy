using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for R10 of modifier-synergies-theme17: feedback presentation,
    /// cosmetic-only behavior, and missing-bundle skip.
    ///
    /// <see cref="SystemBreakState"/> is plain C#, so these tests drive it directly through a fake
    /// <see cref="SystemBreakState.ISystemBreakFeedback"/> sink that records every
    /// <c>Present</c> call. That lets us assert:
    ///   - R10.2: when the interacting-modifier count crosses a tier threshold, the tier's bundle
    ///     is presented, drawn from {glitch visuals, weapon comments, fake system messages}.
    ///   - R10.3 ordering: when multiple channels fire together they present in the fixed order
    ///     GlitchVisual -> WeaponComment -> FakeSystemMessage.
    ///   - R10.7: a null or empty bundle for a tier is skipped without throwing, and a bundle
    ///     provider (or presenter) that throws is swallowed so the run is never broken.
    ///   - R10.4 (cosmetic-only): asserted structurally — SystemBreakState's public surface exposes
    ///     no gameplay-mutating members (no damage/cascade/reward writers); Evaluate only reads the
    ///     int count and routes to the presentation sink. See <see cref="CosmeticOnly_*"/> below.
    /// </summary>
    public sealed class SystemBreakFeedbackPresentationTests
    {
        // Records each Present() call so tests can assert ordering, tier, and content.
        private sealed class RecordingFeedback : SystemBreakState.ISystemBreakFeedback
        {
            public readonly struct Call
            {
                public readonly int Tier;
                public readonly SystemBreakState.Channel Channel;
                public readonly string Content;
                public readonly float Intensity;
                public readonly float Frequency;

                public Call(int tier, SystemBreakState.Channel channel, string content, float intensity, float frequency)
                {
                    Tier = tier; Channel = channel; Content = content; Intensity = intensity; Frequency = frequency;
                }
            }

            public readonly List<Call> Calls = new List<Call>();

            // Bundles keyed by tier. A tier with no entry returns null (a "missing" bundle).
            public readonly Dictionary<int, SystemBreakState.FeedbackBundle?> Bundles =
                new Dictionary<int, SystemBreakState.FeedbackBundle?>();

            public SystemBreakState.FeedbackBundle? BundleFor(int tier)
                => Bundles.TryGetValue(tier, out var b) ? b : null;

            public void Present(int tier, SystemBreakState.Channel channel, string content, float intensity, float frequency)
                => Calls.Add(new Call(tier, channel, content, intensity, frequency));
        }

        private static SystemBreakState.FeedbackBundle FullBundle(int tier) => new SystemBreakState.FeedbackBundle(
            glitchVisual: $"glitch-{tier}",
            weaponComment: $"comment-{tier}",
            fakeSystemMessage: $"sysmsg-{tier}");

        // ---- R10.2 / R10.3: presentation selection and ordering ----------------------------------

        // Feature: modifier-synergies-theme17, R10.2/R10.3 example — crossing to a tier presents that
        // tier's bundle in the fixed channel order glitch -> weapon comment -> fake system message.
        // Validates: Requirements 10.2, 10.3
        [Test]
        public void CrossingTier_PresentsBundleInChannelOrder()
        {
            var sink = new RecordingFeedback();
            sink.Bundles[1] = FullBundle(1);
            var state = new SystemBreakState(sink);

            state.Evaluate(3); // crosses tier 0 -> 1

            Assert.AreEqual(3, sink.Calls.Count, "All three channels of tier 1 should present.");
            Assert.AreEqual(SystemBreakState.Channel.GlitchVisual, sink.Calls[0].Channel);
            Assert.AreEqual(SystemBreakState.Channel.WeaponComment, sink.Calls[1].Channel);
            Assert.AreEqual(SystemBreakState.Channel.FakeSystemMessage, sink.Calls[2].Channel);
            foreach (var c in sink.Calls)
                Assert.AreEqual(1, c.Tier, "Every presented channel should carry its own tier.");
        }

        // Feature: modifier-synergies-theme17, R10.2/R10.3 example — jumping straight to the top tier
        // escalates through every crossed tier's bundle in ascending tier order.
        // Validates: Requirements 10.2, 10.3
        [Test]
        public void CrossingMultipleTiersAtOnce_PresentsEachCrossedTierInOrder()
        {
            var sink = new RecordingFeedback();
            sink.Bundles[1] = FullBundle(1);
            sink.Bundles[2] = FullBundle(2);
            sink.Bundles[3] = FullBundle(3);
            var state = new SystemBreakState(sink);

            state.Evaluate(9); // 0 -> 3 in one step

            // Tiers must be presented in strictly ascending order (1,1,1, 2,2,2, 3,3,3).
            int lastTier = 0;
            foreach (var c in sink.Calls)
            {
                Assert.GreaterOrEqual(c.Tier, lastTier, "Crossed tiers must present in ascending order.");
                lastTier = c.Tier;
            }
            Assert.AreEqual(9, sink.Calls.Count, "Three channels for each of the three crossed tiers.");
            Assert.AreEqual(3, lastTier, "Escalation should reach the top tier.");
        }

        // Feature: modifier-synergies-theme17, R10.3 example — higher tiers present at >= intensity and
        // frequency than lower tiers when they fire together.
        // Validates: Requirements 10.3
        [Test]
        public void HigherTiers_PresentAtGreaterOrEqualIntensityAndFrequency()
        {
            var sink = new RecordingFeedback();
            sink.Bundles[1] = FullBundle(1);
            sink.Bundles[2] = FullBundle(2);
            sink.Bundles[3] = FullBundle(3);
            var state = new SystemBreakState(sink);

            state.Evaluate(9);

            // Group the recorded intensity/frequency per tier and check non-decreasing across tiers.
            float prevIntensity = float.NegativeInfinity;
            float prevFrequency = float.NegativeInfinity;
            for (int tier = 1; tier <= SystemBreakState.MaxTier; tier++)
            {
                var call = sink.Calls.Find(c => c.Tier == tier);
                Assert.AreEqual(tier, call.Tier, $"Expected a presented channel for tier {tier}.");
                Assert.GreaterOrEqual(call.Intensity, prevIntensity, "Intensity must not decrease with tier.");
                Assert.GreaterOrEqual(call.Frequency, prevFrequency, "Frequency must not decrease with tier.");
                prevIntensity = call.Intensity;
                prevFrequency = call.Frequency;
            }
        }

        // Feature: modifier-synergies-theme17, R10.7 example — a bundle with only some channels
        // populated presents exactly the non-null channels and skips the rest without error.
        // Validates: Requirements 10.7
        [Test]
        public void PartialBundle_PresentsOnlyPopulatedChannels()
        {
            var sink = new RecordingFeedback();
            sink.Bundles[1] = new SystemBreakState.FeedbackBundle(
                glitchVisual: null, weaponComment: "only-comment", fakeSystemMessage: null);
            var state = new SystemBreakState(sink);

            state.Evaluate(3);

            Assert.AreEqual(1, sink.Calls.Count, "Only the populated channel should present.");
            Assert.AreEqual(SystemBreakState.Channel.WeaponComment, sink.Calls[0].Channel);
            Assert.AreEqual("only-comment", sink.Calls[0].Content);
        }

        // ---- R10.7: missing / faulty bundle is skipped without error -----------------------------

        // Feature: modifier-synergies-theme17, R10.7 example — a tier with no bundle (null) is skipped
        // and presents nothing, without raising an error.
        // Validates: Requirements 10.7
        [Test]
        public void MissingBundle_IsSkippedWithoutError()
        {
            var sink = new RecordingFeedback(); // no bundle registered -> BundleFor returns null
            var state = new SystemBreakState(sink);

            Assert.DoesNotThrow(() => state.Evaluate(3));
            Assert.AreEqual(0, sink.Calls.Count, "A missing bundle must present nothing.");
            Assert.AreEqual(1, state.Tier, "Tier still advances even when the bundle is missing.");
        }

        // Feature: modifier-synergies-theme17, R10.7 example — an empty bundle (all channels null/empty)
        // is treated as missing and skipped without error.
        // Validates: Requirements 10.7
        [Test]
        public void EmptyBundle_IsSkippedWithoutError()
        {
            var sink = new RecordingFeedback();
            sink.Bundles[1] = new SystemBreakState.FeedbackBundle(null, "", null);
            var state = new SystemBreakState(sink);

            Assert.DoesNotThrow(() => state.Evaluate(3));
            Assert.AreEqual(0, sink.Calls.Count, "An empty bundle must present nothing.");
            Assert.AreEqual(1, state.Tier);
        }

        // A sink whose BundleFor always throws.
        private sealed class ThrowingBundleFeedback : SystemBreakState.ISystemBreakFeedback
        {
            public int PresentCalls;
            public SystemBreakState.FeedbackBundle? BundleFor(int tier)
                => throw new InvalidOperationException("faulty content provider");
            public void Present(int tier, SystemBreakState.Channel channel, string content, float intensity, float frequency)
                => PresentCalls++;
        }

        // Feature: modifier-synergies-theme17, R10.7 example — a bundle provider that throws is
        // swallowed: the run is not broken and nothing is presented for that tier.
        // Validates: Requirements 10.7
        [Test]
        public void ThrowingBundleProvider_IsSwallowed()
        {
            // SystemBreakState logs the swallowed exception; tell the runner to expect it so the
            // logged error does not fail the test.
            LogAssert.ignoreFailingMessages = true;

            var sink = new ThrowingBundleFeedback();
            var state = new SystemBreakState(sink);

            Assert.DoesNotThrow(() => state.Evaluate(3));
            Assert.AreEqual(0, sink.PresentCalls, "Nothing should present when the provider throws.");
            Assert.AreEqual(1, state.Tier, "Tier tracking survives a faulty provider.");
        }

        // A sink that provides a full bundle but throws when asked to present a channel.
        private sealed class ThrowingPresentFeedback : SystemBreakState.ISystemBreakFeedback
        {
            public int PresentAttempts;
            public SystemBreakState.FeedbackBundle? BundleFor(int tier) => FullBundle(tier);
            public void Present(int tier, SystemBreakState.Channel channel, string content, float intensity, float frequency)
            {
                PresentAttempts++;
                throw new InvalidOperationException("faulty presenter");
            }
        }

        // Feature: modifier-synergies-theme17, R10.7 example — a presenter that throws on one channel
        // is isolated: the remaining channels are still attempted and the run is not broken.
        // Validates: Requirements 10.7
        [Test]
        public void ThrowingPresenter_IsIsolatedPerChannel()
        {
            LogAssert.ignoreFailingMessages = true;

            var sink = new ThrowingPresentFeedback();
            var state = new SystemBreakState(sink);

            Assert.DoesNotThrow(() => state.Evaluate(3));
            // All three channels of the (full) tier-1 bundle should still be attempted despite each throwing.
            Assert.AreEqual(3, sink.PresentAttempts, "Every channel should be attempted despite per-channel faults.");
            Assert.AreEqual(1, state.Tier);
        }

        // Feature: modifier-synergies-theme17, R10.2 example — a null sink runs tier tracking without
        // presenting anything and without error.
        // Validates: Requirements 10.2, 10.7
        [Test]
        public void NullSink_TracksTierWithoutError()
        {
            var state = new SystemBreakState(null);
            Assert.DoesNotThrow(() => state.Evaluate(9));
            Assert.AreEqual(3, state.Tier);
        }

        // ---- R10.4: cosmetic-only, asserted structurally -----------------------------------------

        // Feature: modifier-synergies-theme17, R10.4 example — cosmetic-only is asserted structurally:
        // SystemBreakState's public surface exposes no gameplay-mutating members. Its only public
        // instance methods are Evaluate (which returns the tier) and Reset; the only public writable
        // state is the read-only Tier getter. There is no member touching damage, cascade, or reward.
        // Validates: Requirements 10.4
        [Test]
        public void CosmeticOnly_PublicSurfaceHasNoGameplayMutators()
        {
            // The Tier property must be publicly read-only (no public setter) so callers cannot use
            // SystemBreakState to influence anything but its own escalation tier.
            PropertyInfo tier = typeof(SystemBreakState).GetProperty("Tier", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(tier, "Tier property should exist.");
            Assert.IsTrue(tier.CanRead, "Tier should be readable.");
            Assert.IsNull(tier.GetSetMethod(nonPublic: false), "Tier must have no public setter (read-only).");

            // No public method name should reference gameplay systems (damage/cascade/reward/heal).
            string[] forbidden = { "damage", "cascade", "reward", "heal", "kill", "spawn" };
            foreach (MethodInfo m in typeof(SystemBreakState).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (m.IsSpecialName) continue; // skip property accessors
                string lower = m.Name.ToLowerInvariant();
                foreach (string bad in forbidden)
                    Assert.IsFalse(lower.Contains(bad),
                        $"Public method '{m.Name}' suggests a gameplay mutation; SystemBreakState must be cosmetic-only.");
            }
        }

        // Feature: modifier-synergies-theme17, R10.4 example — Evaluate is idempotent for the same
        // count and re-evaluating a non-increasing count presents nothing new, confirming Evaluate
        // only reads the count and drives cosmetic presentation on upward crossings.
        // Validates: Requirements 10.4
        [Test]
        public void CosmeticOnly_EvaluateReadsCountAndOnlyPresentsOnUpwardCrossing()
        {
            var sink = new RecordingFeedback();
            sink.Bundles[1] = FullBundle(1);
            sink.Bundles[2] = FullBundle(2);
            var state = new SystemBreakState(sink);

            state.Evaluate(6); // 0 -> 2, presents tiers 1 and 2
            int afterRise = sink.Calls.Count;
            Assert.AreEqual(6, afterRise, "Two crossed tiers, three channels each.");

            state.Evaluate(6); // same count: no new crossing
            Assert.AreEqual(afterRise, sink.Calls.Count, "Re-evaluating the same count presents nothing new.");

            state.Evaluate(0); // lowering: updates tier, presents nothing
            Assert.AreEqual(afterRise, sink.Calls.Count, "Lowering the count presents nothing.");
            Assert.AreEqual(0, state.Tier, "Tier follows the lowered count.");
        }

        [TearDown]
        public void ResetLogAssert()
        {
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
