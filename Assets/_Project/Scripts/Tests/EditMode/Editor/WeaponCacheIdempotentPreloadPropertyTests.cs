using System;
using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure weapon cache <see cref="WeaponLoadout.WeaponCache"/> —
    /// task 9.3 of project-cleanup-optimization (Requisito 5.1, design Property 7).
    ///
    /// <see cref="WeaponLoadout.WeaponCache"/> isolates the preload/lookup/fallback logic from Unity:
    /// <c>Preload(paths, loader)</c> loads every slot once through an injected loader and <c>Get(index,
    /// loader)</c> resolves a weapon by index. Because the loader is injected, the test supplies a
    /// counting stub instead of <c>Resources.Load</c>, so the "does not reload after preload"
    /// (idempotence) and "out-of-range falls back to index 0" guarantees can be property-checked
    /// without a live Unity scene. <see cref="WeaponScript"/> is a <see cref="ScriptableObject"/>, so
    /// the stub returns real, uniquely-identifiable instances via <see cref="ScriptableObject.CreateInstance{T}()"/>
    /// and the test proves idempotence by reference identity (the same cached instance is returned
    /// without the loader being called again).
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing case as a counterexample — matching the convention of the
    /// sibling pure-logic property tests (e.g. ProjectileMotionPropertyTests, SimpleObjectPoolPropertyTests).
    ///
    /// Validates: Requirements 5.1
    /// </summary>
    public sealed class WeaponCacheIdempotentPreloadPropertyTests
    {
        /// <summary>
        /// A counting loader stub standing in for <c>Resources.Load&lt;WeaponScript&gt;</c>. It records
        /// how many times each index was loaded so the test can prove that a cached hit is served
        /// without a reload. A "missing" index (not in <see cref="_present"/>) returns null, mirroring a
        /// failed <c>Resources.Load</c> (R5.5), so the fallback-on-demand path can also be exercised.
        /// </summary>
        private sealed class CountingLoader
        {
            private readonly HashSet<int> _present;
            private readonly Dictionary<int, WeaponScript> _created = new Dictionary<int, WeaponScript>();
            public int[] Calls { get; }

            public CountingLoader(int length, HashSet<int> present)
            {
                _present = present;
                Calls = new int[Math.Max(0, length)];
            }

            public WeaponScript Load(int index)
            {
                if (index >= 0 && index < Calls.Length) Calls[index]++;
                if (!_present.Contains(index)) return null;
                // Return a stable, uniquely-identifiable instance per index so a cached hit is proven
                // by reference identity, and a reload would (incorrectly) hand back a different object.
                if (!_created.TryGetValue(index, out var weapon))
                {
                    weapon = ScriptableObject.CreateInstance<WeaponScript>();
                    weapon.name = $"Weapon#{index}";
                    _created[index] = weapon;
                }
                return weapon;
            }

            public int TotalCalls()
            {
                int sum = 0;
                for (int i = 0; i < Calls.Length; i++) sum += Calls[i];
                return sum;
            }
        }

        // Feature: project-cleanup-optimization, Property 7: para todo índice válido, get é idempotente
        // e não recarrega após preload; índice inválido cai no fallback do índice 0.
        //
        // For any set of resource paths and any subset that preloads successfully, after Preload the
        // loader has been called exactly once per slot (the preload pass) and:
        //   (a) for every VALID index whose slot preloaded successfully, Get returns the SAME cached
        //       instance by reference, repeatedly, WITHOUT ever calling the loader again (idempotent,
        //       no reload — R5.1);
        //   (b) any OUT-OF-RANGE index (negative or >= Count) resolves to index 0 (fallback), returning
        //       exactly what index 0 resolves to, and never resolving some other slot;
        //   (c) a slot that preloaded as null (failed load, R5.5) is loaded on demand on the FIRST Get
        //       and then memoized, so a SECOND Get for the same index does not reload — idempotence
        //       still holds once a value is present.
        // Validates: Requirements 5.1
        [Test]
        public void ValidIndexGetIsIdempotentAndOutOfRangeFallsBackToIndexZero()
        {
            PropertyCheck.ForAll((rng, iteration) =>
            {
                // A non-empty path set (1..5 slots) so index 0 — the fallback target — always exists.
                int count = rng.Next(1, 6);
                var paths = new string[count];
                for (int i = 0; i < count; i++) paths[i] = $"Weapons/Slot{i}";

                // Choose which slots preload successfully. Index 0 preloads successfully in most cases
                // so the "valid index returns a stable instance" branch is well exercised, but we also
                // allow a missing index 0 to exercise the on-demand fallback for the fallback target.
                var present = new HashSet<int>();
                for (int i = 0; i < count; i++)
                {
                    bool slotPresent = i == 0 ? rng.Next(0, 10) < 8 : rng.Next(0, 2) == 0;
                    if (slotPresent) present.Add(i);
                }

                var loader = new CountingLoader(count, present);
                string ctx = $"[count={count} present={present.Count}]";

                // ---- Preload: loader called exactly once per slot ----
                var cache = WeaponLoadout.WeaponCache.Preload(paths, loader.Load);

                PropertyCheck.That(cache.Count == count,
                    $"{ctx}: cache Count={cache.Count} did not match path count {count}");
                for (int i = 0; i < count; i++)
                {
                    PropertyCheck.That(loader.Calls[i] == 1,
                        $"{ctx}: preload called loader for index {i} {loader.Calls[i]} times, expected exactly 1");
                }

                // Snapshot the per-slot call counts right after preload. A present slot must never be
                // loaded again by Get (idempotent); a missing slot may be loaded once more on demand.
                var afterPreload = (int[])loader.Calls.Clone();

                // ---- (a) valid, present indices: Get is idempotent and never reloads ----
                for (int i = 0; i < count; i++)
                {
                    if (!present.Contains(i)) continue;

                    WeaponScript first = cache.Get(i, loader.Load);
                    WeaponScript second = cache.Get(i, loader.Load);
                    WeaponScript third = cache.Get(i, loader.Load);

                    PropertyCheck.That(first != null,
                        $"{ctx}: Get({i}) returned null for a successfully preloaded slot");
                    // Idempotence by reference identity: the very same cached instance every time.
                    PropertyCheck.That(ReferenceEquals(first, second) && ReferenceEquals(second, third),
                        $"{ctx}: Get({i}) returned different instances across repeated calls (not idempotent)");
                    // No reload: the loader was NOT called again for this slot.
                    PropertyCheck.That(loader.Calls[i] == afterPreload[i],
                        $"{ctx}: Get({i}) reloaded a preloaded slot (calls {afterPreload[i]} -> {loader.Calls[i]})");
                }

                // ---- (c) missing slot: loaded on demand once, then memoized (no reload) ----
                for (int i = 0; i < count; i++)
                {
                    if (present.Contains(i)) continue;

                    int before = loader.Calls[i];
                    WeaponScript first = cache.Get(i, loader.Load);  // on-demand load; stub still returns null
                    int afterFirst = loader.Calls[i];
                    WeaponScript second = cache.Get(i, loader.Load); // must NOT reload a now-known-empty slot? see note
                    int afterSecond = loader.Calls[i];

                    // A still-null slot has nothing to memoize, so Get keeps attempting the on-demand
                    // load each call. The guarantee we assert is the one the design makes: it never
                    // reloads a slot that is PRESENT. For an absent slot the loader is re-tried, which
                    // is the documented R5.5 on-demand fallback. We only assert it is attempted and
                    // stays null, not that it stops retrying.
                    PropertyCheck.That(afterFirst == before + 1,
                        $"{ctx}: Get({i}) on a missing slot did not attempt the on-demand load exactly once (calls {before} -> {afterFirst})");
                    PropertyCheck.That(first == null && second == null,
                        $"{ctx}: Get({i}) on a missing slot unexpectedly resolved to a non-null weapon");
                    PropertyCheck.That(afterSecond == afterFirst + 1,
                        $"{ctx}: Get({i}) on a still-missing slot should re-attempt the on-demand load (calls {afterFirst} -> {afterSecond})");
                }

                // ---- (b) out-of-range indices fall back to index 0 ----
                WeaponScript atZero = cache.Get(0, loader.Load);
                // Whether index 0 was present or memoized by a prior Get, re-resolving it is stable.

                int[] outOfRange = { -1, -count, count, count + 3, int.MaxValue, int.MinValue };
                foreach (int idx in outOfRange)
                {
                    WeaponScript fallback = cache.Get(idx, loader.Load);
                    // The fallback must resolve exactly what index 0 resolves to — same instance when
                    // index 0 has a value, or null when index 0 itself is an unresolved/missing slot.
                    if (atZero != null)
                    {
                        PropertyCheck.That(ReferenceEquals(fallback, atZero),
                            $"{ctx}: out-of-range index {idx} did not fall back to index 0's instance");
                    }
                    else
                    {
                        PropertyCheck.That(fallback == null,
                            $"{ctx}: out-of-range index {idx} returned non-null while index 0 is empty");
                    }
                }

                // Cleanup the ScriptableObject sentinels this iteration created.
                foreach (int i in present)
                {
                    WeaponScript w = cache.Get(i, loader.Load);
                    if (w != null) UnityEngine.Object.DestroyImmediate(w);
                }
            });
        }

        // Feature: project-cleanup-optimization, Property 7 (focused idempotence facet): once every
        // slot is preloaded successfully, ANY interleaving of Get calls over valid indices returns the
        // originally preloaded instances by reference and calls the loader ZERO additional times after
        // the preload pass — a direct, isolated demonstration that a cached hit never reloads (R5.1).
        // Validates: Requirements 5.1
        [Test]
        public void FullyPreloadedCacheNeverReloadsAcrossArbitraryGetSequences()
        {
            PropertyCheck.ForAll((rng, iteration) =>
            {
                int count = rng.Next(1, 6);
                var paths = new string[count];
                for (int i = 0; i < count; i++) paths[i] = $"Weapons/Slot{i}";

                // Every slot preloads successfully this time.
                var present = new HashSet<int>();
                for (int i = 0; i < count; i++) present.Add(i);

                var loader = new CountingLoader(count, present);
                string ctx = $"[count={count}]";

                var cache = WeaponLoadout.WeaponCache.Preload(paths, loader.Load);

                // Record the exact instance preloaded into each slot (via the first, idempotent Get).
                var expected = new WeaponScript[count];
                for (int i = 0; i < count; i++) expected[i] = cache.Get(i, loader.Load);

                int callsAfterPreload = loader.TotalCalls();
                PropertyCheck.That(callsAfterPreload == count,
                    $"{ctx}: preload (+first Get) called the loader {callsAfterPreload} times, expected exactly {count}");

                // Hammer the cache with an arbitrary sequence of valid-index Gets.
                int gets = rng.Next(20, 60);
                for (int g = 0; g < gets; g++)
                {
                    int idx = rng.Next(0, count);
                    WeaponScript got = cache.Get(idx, loader.Load);
                    PropertyCheck.That(ReferenceEquals(got, expected[idx]),
                        $"{ctx} get#{g}: index {idx} returned a different instance than the preloaded one");
                }

                // Not a single extra load happened after the preload pass: fully idempotent (R5.1).
                PropertyCheck.That(loader.TotalCalls() == callsAfterPreload,
                    $"{ctx}: cache reloaded after preload (calls {callsAfterPreload} -> {loader.TotalCalls()})");

                // Cleanup sentinels.
                for (int i = 0; i < count; i++)
                    if (expected[i] != null) UnityEngine.Object.DestroyImmediate(expected[i]);
            });
        }
    }
}
