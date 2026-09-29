using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 40 of weapon-gameplay-swarm-rework (task 15.7):
    /// an invalid Run_Modifier is ignored without side effect (Requisito 12.8).
    ///
    /// <see cref="WeaponRunModifiers.Add"/> is the single gate that records a run rank, so it is
    /// also the single point that must <b>refuse</b> a modifier that is not catalogued for the run's
    /// family, belongs to a foreign weapon family, or whose resulting rank would fall outside
    /// [1; MaxRank]. Per Requisito 12.8 a refused Add must:
    ///   * return <c>false</c> and leave the boon's <see cref="WeaponRunModifiers.Rank"/> unchanged
    ///     (it never appears in the ranks if it was not there before), and
    ///   * log a warning that identifies the refused modifier + rank, and
    ///   * have no observable effect on the assembled Cast_Plan / GauntletSteps snapshot nor on the
    ///     source assets.
    ///
    /// The Cast_Plan/asset half is proven by the "no side effect" property below: two independent
    /// <see cref="WeaponRunModifiers"/> are grown to the SAME valid rank multiset; one of them is then
    /// bombarded with a randomized sequence of refused Adds (over-cap, foreign-family, uncatalogued,
    /// null). The <see cref="WeaponRunModifiers.Plan"/> snapshot for every slot (and, for the Gauntlet
    /// family, the <see cref="WeaponRunModifiers.GauntletSteps"/> snapshot) must be byte-for-byte
    /// identical whether or not the refused Adds were attempted.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases (min 128)
    /// and reports the exact failing case as a counterexample. The refusals emit
    /// <c>Debug.LogWarning</c>; <see cref="LogAssert.ignoreFailingMessages"/> is enabled for the run so
    /// the expected warnings do not fail the test (the warning content itself is asserted separately
    /// in <see cref="RefusedAdd_LogsWarningIdentifyingModifierAndRank"/> via <c>LogAssert.Expect</c>).
    /// Pure plan generation on ScriptableObject instances, so it runs in EditMode (no scene, no physics).
    /// </summary>
    public sealed class InvalidModifierIgnoredNoSideEffectPropertyTests
    {
        private static readonly RunWeaponFamily[] Families =
        {
            RunWeaponFamily.Bow, RunWeaponFamily.Spear, RunWeaponFamily.Gauntlet,
        };

        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        [SetUp]
        public void EnableExpectedWarnings()
        {
            // Every refused Add logs a warning by contract (Requisito 12.8). Across >=128 cases that is
            // a large, expected volume of warnings; ignore them here so the property under test (no side
            // effect) is not masked by the expected logging. The warning content is asserted separately.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void ResetLogAssert()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        private static WeaponRunModifiers.Definition Find(WeaponBoon kind)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        // ---- serialized ArsenalAbility with a chosen Kind (see AssetIsolationTests) --------------
        [Serializable]
        private struct ArsenalKindOverlay { public int _kind; }

        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind, int index)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "SourceArsenal_" + kind + "_" + index;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static BreakerGauntletAbility NewGauntletAbility(int index)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "SourceGauntlet_" + index;
            return ability;
        }

        // A stable, deterministic snapshot of an ArsenalCastPlan. ArsenalCastPlan is a plain class (not
        // [Serializable]); comparing its public fields directly is the observable Cast_Plan for a cast.
        private static string SnapshotPlan(ArsenalCastPlan p)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "W={0:R};I={1:R};Rg={2:R};Wd={3:R};Dm={4:R};Wv={5:R};H={6};Ar={7};Dir={8};Trk={9};Trv={10};Ph={11:R};Sh={12};Rw={13};Ch={14}",
                p.Windup, p.Interval, p.Range, p.Width, p.Damage, p.WaveMultiplier,
                p.Hits, p.Arrows, p.Directions, p.TrackCursor, p.Travel,
                p.PhantomDelay, p.ShardCount, p.ReturnWave, p.ChainThrust);
        }

        // A stable snapshot of a GauntletSteps result. AreaHitStep IS serializable, so the JSON of each
        // cloned step is the observable Cast_Plan for a gauntlet cast.
        private static string SnapshotSteps(List<AreaHitStep> steps)
        {
            var sb = new StringBuilder();
            sb.Append(steps.Count).Append('|');
            foreach (AreaHitStep step in steps) sb.Append(JsonUtility.ToJson(step)).Append('|');
            return sb.ToString();
        }

        // Grow a WeaponRunModifiers to a randomized-but-recorded valid rank multiset for its family.
        // Returns the wanted rank per boon so a sibling instance can be grown identically.
        private static Dictionary<WeaponBoon, int> GrowValidRanks(WeaponRunModifiers mods, RunWeaponFamily family, System.Random rng)
        {
            var wantedByBoon = new Dictionary<WeaponBoon, int>();
            foreach (var definition in WeaponRunModifiers.Catalog)
            {
                if (definition.Family != family) continue;
                int wanted = rng.Next(0, definition.MaxRank + 1);
                wantedByBoon[definition.Kind] = wanted;
                for (int r = 0; r < wanted; r++)
                    PropertyCheck.That(mods.Add(definition), "in-family valid rank should be addable");
            }
            return wantedByBoon;
        }

        private static void GrowValidRanksLike(WeaponRunModifiers mods, Dictionary<WeaponBoon, int> wantedByBoon)
        {
            foreach (KeyValuePair<WeaponBoon, int> entry in wantedByBoon)
            {
                WeaponRunModifiers.Definition def = Find(entry.Key);
                for (int r = 0; r < entry.Value; r++)
                    PropertyCheck.That(mods.Add(def), "sibling in-family valid rank should be addable");
            }
        }

        // Feature: weapon-gameplay-swarm-rework, Property 40: Modificador inválido é ignorado sem efeito colateral
        // Rank fora de [1; máximo], não catalogado, de outra família, ou nulo é recusado por Add
        // (retorna false, ranks inalterados) e não altera o Cast_Plan (Plan/GauntletSteps) montado a
        // partir dos ranks válidos: dois WeaponRunModifiers crescidos ao mesmo multiconjunto de ranks
        // válidos produzem snapshots idênticos, um deles tendo sido bombardeado com Adds recusados.
        // Validates: Requirements 12.8
        [Test]
        public void InvalidModifierIsRefusedAndLeavesCastPlanUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = Families[rng.Next(Families.Length)];

                // ---- two independent runs grown to the SAME valid rank multiset ------------------
                var clean = new WeaponRunModifiers(family);
                var polluted = new WeaponRunModifiers(family);
                Dictionary<WeaponBoon, int> wanted = GrowValidRanks(clean, family, rng);
                GrowValidRanksLike(polluted, wanted);

                // sanity: both runs agree on every in-family rank before any refused Add.
                foreach (KeyValuePair<WeaponBoon, int> entry in wanted)
                    PropertyCheck.That(clean.Rank(entry.Key) == polluted.Rank(entry.Key),
                        $"[case #{i}] setup mismatch for {entry.Key}: {clean.Rank(entry.Key)} vs {polluted.Rank(entry.Key)}");

                // ---- bombard `polluted` with a randomized sequence of REFUSED Adds ---------------
                int attempts = rng.Next(1, 24);
                for (int a = 0; a < attempts; a++)
                {
                    int choice = rng.Next(0, 4);
                    if (choice == 0)
                    {
                        // Over-cap: on a throwaway run, fill an in-family boon to MaxRank so the next Add
                        // is the over-cap case, and assert that refusal returns false + leaves the rank at
                        // the cap. This is exercised on a scratch run (not `polluted`) precisely because a
                        // valid fill to MaxRank WOULD change the multiset; the over-cap Add — the actual
                        // refused Add — must be the no-op. `polluted`'s multiset is left untouched.
                        WeaponBoon overKind = PickInFamily(family, rng);
                        WeaponRunModifiers.Definition def = Find(overKind);
                        int pollutedBefore = polluted.Rank(overKind);

                        var scratch = new WeaponRunModifiers(family);
                        for (int r = 0; r < def.MaxRank; r++) PropertyCheck.That(scratch.Add(def), "scratch max fill");

                        bool refused = scratch.Add(def);
                        PropertyCheck.That(!refused, $"[case #{i}] over-cap Add of {overKind} should return false");
                        PropertyCheck.That(scratch.Rank(overKind) == def.MaxRank,
                            $"[case #{i}] over-cap Add changed rank of {overKind}: expected {def.MaxRank}, got {scratch.Rank(overKind)}");
                        PropertyCheck.That(polluted.Rank(overKind) == pollutedBefore,
                            $"[case #{i}] over-cap scratch leaked into polluted for {overKind}");
                    }
                    else if (choice == 1)
                    {
                        // Foreign-family: an in-catalog boon that belongs to a different family.
                        RunWeaponFamily foreign = OtherFamily(family, rng);
                        WeaponBoon foreignKind = PickInFamily(foreign, rng);
                        WeaponRunModifiers.Definition def = Find(foreignKind);
                        int before = polluted.Rank(foreignKind);
                        bool refused = polluted.Add(def);
                        PropertyCheck.That(!refused, $"[case #{i}] foreign-family Add of {foreignKind} ({foreign}) should return false");
                        PropertyCheck.That(polluted.Rank(foreignKind) == before,
                            $"[case #{i}] foreign-family Add changed rank of {foreignKind}: {before} -> {polluted.Rank(foreignKind)}");
                        PropertyCheck.That(polluted.Rank(foreignKind) == 0,
                            $"[case #{i}] foreign-family boon {foreignKind} must never appear in ranks");
                    }
                    else if (choice == 2)
                    {
                        // Uncatalogued: a Definition whose (Kind, Family, MaxRank) is absent from Catalog.
                        // Use a real in-family Kind but a MaxRank that does not match its catalog entry.
                        WeaponBoon kind = PickInFamily(family, rng);
                        WeaponRunModifiers.Definition catalogDef = Find(kind);
                        int bogusMax = catalogDef.MaxRank + 1 + rng.Next(0, 5); // guaranteed to differ
                        var uncatalogued = new WeaponRunModifiers.Definition(kind, family, "bogus", "bogus", bogusMax);
                        int before = polluted.Rank(kind);
                        bool refused = polluted.Add(uncatalogued);
                        PropertyCheck.That(!refused, $"[case #{i}] uncatalogued Add of {kind} (maxRank {bogusMax}) should return false");
                        PropertyCheck.That(polluted.Rank(kind) == before,
                            $"[case #{i}] uncatalogued Add changed rank of {kind}: {before} -> {polluted.Rank(kind)}");
                    }
                    else
                    {
                        // Null definition is refused with no effect.
                        bool refused = polluted.Add(null);
                        PropertyCheck.That(!refused, $"[case #{i}] Add(null) should return false");
                    }
                }

                // ---- the refused Adds left every in-family rank identical to the clean run --------
                foreach (var definition in WeaponRunModifiers.Catalog)
                    PropertyCheck.That(clean.Rank(definition.Kind) == polluted.Rank(definition.Kind),
                        $"[case #{i}] rank drift for {definition.Kind}: clean={clean.Rank(definition.Kind)} polluted={polluted.Rank(definition.Kind)}");

                // ---- and the assembled Cast_Plan / GauntletSteps snapshots are identical ----------
                if (family == RunWeaponFamily.Gauntlet)
                {
                    for (int slot = 0; slot < 4; slot++)
                    {
                        BreakerGauntletAbility a = NewGauntletAbility(slot);
                        try
                        {
                            string cleanSnap = SnapshotSteps(clean.GauntletSteps(a, slot));
                            string pollutedSnap = SnapshotSteps(polluted.GauntletSteps(a, slot));
                            PropertyCheck.That(cleanSnap == pollutedSnap,
                                $"[case #{i}] GauntletSteps snapshot differs at slot {slot} after refused Adds:\n clean={cleanSnap}\n poll ={pollutedSnap}");
                        }
                        finally { if (a) UnityEngine.Object.DestroyImmediate(a); }
                    }
                }
                else
                {
                    for (int slot = 0; slot < 4; slot++)
                    {
                        ArsenalSkillKind kind = ArsenalKinds[rng.Next(ArsenalKinds.Length)];
                        ArsenalAbility a = NewArsenalAbility(kind, slot);
                        try
                        {
                            string cleanSnap = SnapshotPlan(clean.Plan(a, slot));
                            string pollutedSnap = SnapshotPlan(polluted.Plan(a, slot));
                            PropertyCheck.That(cleanSnap == pollutedSnap,
                                $"[case #{i}] Plan snapshot differs at slot {slot} ({kind}) after refused Adds:\n clean={cleanSnap}\n poll ={pollutedSnap}");
                        }
                        finally { if (a) UnityEngine.Object.DestroyImmediate(a); }
                    }
                }
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 40: Modificador inválido é ignorado sem efeito colateral
        // Cada Add recusado registra um aviso identificando o modificador recusado + o rank. Verifica os
        // três motivos de recusa via LogAssert.Expect com o identificador estável do modificador.
        // Validates: Requirements 12.8
        [Test]
        public void RefusedAdd_LogsWarningIdentifyingModifierAndRank()
        {
            LogAssert.ignoreFailingMessages = false; // this test asserts the exact warnings.

            // Over-cap on a Bow run: fill Piercing (MaxRank 1) then one more Add refuses at rank 2.
            var bow = new WeaponRunModifiers(RunWeaponFamily.Bow);
            WeaponRunModifiers.Definition piercing = Find(WeaponBoon.Piercing);
            Assert.IsTrue(bow.Add(piercing), "Piercing rank 1 should be addable.");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex($"{piercing.Id}.*rank 2"));
            Assert.IsFalse(bow.Add(piercing), "Piercing over MaxRank should be refused.");
            Assert.AreEqual(1, bow.Rank(WeaponBoon.Piercing), "Piercing rank must stay at its cap.");

            // Foreign-family: a Spear boon offered to a Bow run is refused and logged.
            WeaponRunModifiers.Definition longReach = Find(WeaponBoon.LongReach);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex($"{longReach.Id}"));
            Assert.IsFalse(bow.Add(longReach), "Spear boon on a Bow run should be refused.");
            Assert.AreEqual(0, bow.Rank(WeaponBoon.LongReach), "Foreign boon must never appear in ranks.");

            // Uncatalogued: same Kind/Family but a MaxRank absent from the catalog.
            var uncatalogued = new WeaponRunModifiers.Definition(
                WeaponBoon.RapidBurst, RunWeaponFamily.Bow, "bogus", "bogus", Find(WeaponBoon.RapidBurst).MaxRank + 7);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(uncatalogued.Id));
            Assert.IsFalse(bow.Add(uncatalogued), "Uncatalogued definition should be refused.");
            Assert.AreEqual(0, bow.Rank(WeaponBoon.RapidBurst), "Uncatalogued Add must not record a rank.");

            // Null: refused with a warning, no crash.
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("null Run_Modifier"));
            Assert.IsFalse(bow.Add(null), "Null definition should be refused.");
        }

        // ---- generators ----------------------------------------------------------------------
        private static WeaponBoon PickInFamily(RunWeaponFamily family, System.Random rng)
        {
            var pool = new List<WeaponBoon>();
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Family == family) pool.Add(definition.Kind);
            return pool[rng.Next(pool.Count)];
        }

        private static RunWeaponFamily OtherFamily(RunWeaponFamily family, System.Random rng)
        {
            RunWeaponFamily other;
            do { other = Families[rng.Next(Families.Length)]; } while (other == family);
            return other;
        }
    }
}
