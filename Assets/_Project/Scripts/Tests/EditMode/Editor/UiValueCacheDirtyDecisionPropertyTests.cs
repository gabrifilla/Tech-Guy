using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free UI dirty-tracker
    /// <see cref="UiValueCache{T}"/> — task 7.3 of project-cleanup-optimization
    /// (Requisitos 2.1, 2.3, 2.4).
    ///
    /// <see cref="UiValueCache{T}"/> is 100% framework-agnostic (no <see cref="UnityEngine.MonoBehaviour"/>,
    /// no <c>TMP_Text</c>, no scene), so its "only (re)emit when the source value changed" contract can
    /// be property-checked without a live Unity scene — exactly the pure logic floor the spec says is
    /// the only CLI-automatable target. It is the reusable core behind the per-frame allocation
    /// elimination in <c>PlayerHUD</c> (R2.1), <c>RunRewardUI</c> (R2.3) and <c>CombatReadabilityUI</c>
    /// (R2.4): the owner feeds the current source value every frame via <see cref="UiValueCache{T}.HasChanged"/>,
    /// and only a <c>true</c> result (a genuine change) should trigger a string rebuild / TMP reassign.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing operation sequence as a counterexample — matching the
    /// convention of the sibling pure-logic property tests (e.g. SimpleObjectPoolPropertyTests,
    /// ProjectileMotionPropertyTests, CombatBalanceBoundsPropertyTests).
    ///
    /// Validates: Requirements 2.1, 2.3, 2.4
    /// </summary>
    public sealed class UiValueCacheDirtyDecisionPropertyTests
    {
        // Feature: project-cleanup-optimization, Property 3: o cache só (re)emite texto quando o valor
        // de origem difere do anterior.
        //
        // For any sequence of fed source values (ints — the health/mana/asura/slot-index counters the
        // HUD renders), we drive a UiValueCache<int> and a plain reference model and assert, after every
        // single HasChanged call, that:
        //   (a) HasChanged returns true EXACTLY when the fed value differs from the last ACCEPTED value
        //       (or when nothing has been accepted yet) — i.e. the cache re-emits iff the source changed
        //       (R2.1/R2.3/R2.4). An unchanged value returns false (no rebuild, no allocation);
        //   (b) a true result records the value (Current == value, HasValue == true) so an immediately
        //       repeated feed of the same value returns false — the (re)emit is one-shot per change;
        //   (c) a false result leaves the cache untouched (Current and HasValue unchanged);
        //   (d) the very first feed always returns true (first paint), regardless of value.
        // Validates: Requirements 2.1, 2.3, 2.4
        [Test]
        public void CacheReEmitsIffSourceValueChanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var cache = new UiValueCache<int>();

                // Reference model: whether a value has been accepted, and what it was.
                bool modelHasValue = false;
                int modelLast = 0;

                // A cold cache has accepted nothing yet.
                PropertyCheck.That(!cache.HasValue,
                    $"[case {i}]: fresh cache reported HasValue=true before any feed");

                int feeds = rng.Next(20, 60);
                for (int f = 0; f < feeds; f++)
                {
                    int value = RandomValue(rng);

                    bool hadValueBefore = cache.HasValue;
                    int currentBefore = cache.Current;

                    // Expected decision: change iff nothing accepted yet, or value differs from last.
                    bool expectedChange = !modelHasValue || modelLast != value;

                    bool reported = cache.HasChanged(value);

                    // (a) The cache re-emits iff the source value changed.
                    PropertyCheck.That(reported == expectedChange,
                        $"[case {i}] feed#{f} value={value}: HasChanged returned {reported} but expected {expectedChange} " +
                        $"(modelHasValue={modelHasValue}, modelLast={modelLast})");

                    // (d) The very first feed is always a change (first paint).
                    if (!hadValueBefore)
                    {
                        PropertyCheck.That(reported,
                            $"[case {i}] feed#{f} value={value}: first-ever feed did not report a change");
                    }

                    if (reported)
                    {
                        // (b) A change records the value and marks the cache as populated.
                        PropertyCheck.That(cache.HasValue,
                            $"[case {i}] feed#{f} value={value}: change did not set HasValue");
                        PropertyCheck.That(cache.Current == value,
                            $"[case {i}] feed#{f} value={value}: change recorded Current={cache.Current} instead of {value}");

                        modelHasValue = true;
                        modelLast = value;
                    }
                    else
                    {
                        // (c) A no-change leaves the cache completely untouched.
                        PropertyCheck.That(cache.HasValue == hadValueBefore,
                            $"[case {i}] feed#{f} value={value}: no-change altered HasValue {hadValueBefore} -> {cache.HasValue}");
                        PropertyCheck.That(cache.Current == currentBefore,
                            $"[case {i}] feed#{f} value={value}: no-change altered Current {currentBefore} -> {cache.Current}");
                    }

                    // Cache's recorded value must always mirror the model after the call.
                    PropertyCheck.That(cache.HasValue == modelHasValue,
                        $"[case {i}] feed#{f} value={value}: HasValue={cache.HasValue} disagreed with model {modelHasValue}");
                    PropertyCheck.That(cache.Current == modelLast,
                        $"[case {i}] feed#{f} value={value}: Current={cache.Current} disagreed with model {modelLast}");

                    // Idempotence of a change: feeding the SAME value again must NOT re-emit.
                    bool repeat = cache.HasChanged(value);
                    PropertyCheck.That(!repeat,
                        $"[case {i}] feed#{f} value={value}: feeding the same value twice re-emitted (expected a one-shot change)");
                }
            });
        }

        // Feature: project-cleanup-optimization, Property 3 (string + comparer facet): the cache guards
        // the already-built UI STRING too (HUD tooltip / feedback text), and only re-emits when that
        // string differs from the last one accepted — using the supplied/default equality comparer. We
        // also prove Invalidate() forces the next feed (even of the identical value) to re-emit, which
        // is how a (re)bound view repaints from scratch without changing the "only on change" rule.
        // Validates: Requirements 2.1, 2.3, 2.4
        [Test]
        public void StringCacheReEmitsOnlyOnChangeAndInvalidateForcesRepaint()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var cache = new UiValueCache<string>();

                bool modelHasValue = false;
                string modelLast = null;
                var comparer = EqualityComparer<string>.Default;

                int feeds = rng.Next(20, 50);
                for (int f = 0; f < feeds; f++)
                {
                    string value = RandomString(rng);

                    bool expectedChange = !modelHasValue || !comparer.Equals(modelLast, value);
                    bool reported = cache.HasChanged(value);

                    PropertyCheck.That(reported == expectedChange,
                        $"[case {i}] feed#{f} value=\"{Describe(value)}\": HasChanged returned {reported} but expected {expectedChange} " +
                        $"(modelHasValue={modelHasValue}, modelLast=\"{Describe(modelLast)}\")");

                    if (reported)
                    {
                        PropertyCheck.That(comparer.Equals(cache.Current, value),
                            $"[case {i}] feed#{f}: change recorded Current=\"{Describe(cache.Current)}\" instead of \"{Describe(value)}\"");
                        modelHasValue = true;
                        modelLast = value;
                    }

                    // Occasionally invalidate: the next feed of the SAME value must re-emit (R2.x repaint
                    // on rebind) without otherwise breaking the only-on-change contract.
                    if (rng.Next(0, 6) == 0)
                    {
                        cache.Invalidate();
                        PropertyCheck.That(!cache.HasValue,
                            $"[case {i}] feed#{f}: Invalidate did not clear HasValue");

                        bool afterInvalidate = cache.HasChanged(value);
                        PropertyCheck.That(afterInvalidate,
                            $"[case {i}] feed#{f}: feed right after Invalidate did not re-emit the identical value");

                        modelHasValue = true;
                        modelLast = value;
                    }
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Counter-like values biased to a small range so repeats (unchanged frames — the no-rebuild
        /// case) are frequent, while still spanning negatives/zero/large and occasional distinct values.
        /// </summary>
        private static int RandomValue(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0;
                case 1: return rng.Next(0, 4);            // tight range -> many repeats
                case 2: return rng.Next(-5, 6);
                case 3: return rng.Next(-1000, 1001);
                default: return rng.Next(int.MinValue, int.MaxValue);
            }
        }

        /// <summary>
        /// UI-string-like values biased to a tiny pool (so unchanged frames are frequent) and including
        /// null and empty, which the default comparer treats as distinct from a non-null string.
        /// </summary>
        private static string RandomString(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return null;
                case 1: return string.Empty;
                case 2: return "100 / 100";
                case 3: return "50 / 100";
                case 4: return "0 / 100";
                default: return rng.Next(0, 20).ToString();
            }
        }

        private static string Describe(string s) => s ?? "<null>";
    }
}
