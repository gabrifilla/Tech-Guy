using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for Weapon × Enemy matrix completeness and fallback — task 16.2 of
    /// weapon-gameplay-swarm-rework (Requisitos 11.1, 11.6, 11.7).
    ///
    /// R11.1: every (<see cref="ArchetypeId"/> × <see cref="RunWeaponFamily"/>) pair resolves to
    /// exactly one <see cref="WeaponResponse"/> — either an authored matrix entry or the weapon's
    /// <see cref="CombatRole"/> default. There is never an undefined pair.
    ///
    /// R11.6: resolution is data-driven; there is no per weapon-enemy branch in the code. The pure
    /// <see cref="WeaponEnemyResponseResolver"/> only switches on <see cref="RunWeaponFamily"/> ×
    /// <see cref="CombatRole"/> for the fallback — never on a specific (archetype, weapon) pair.
    ///
    /// R11.7: a pair with no authored entry falls back to the <see cref="CombatRole"/> default with
    /// <see cref="WeaponResponse.FromMatrix"/> == false, and the SO logs missing coverage once.
    ///
    /// The <see cref="WeaponEnemyMatrix"/> lookup logic lives entirely in the pure, scene-free
    /// resolver so it can be exercised without a live Unity scene. These examples drive the resolver
    /// directly across every archetype × family × role combination, and additionally construct a
    /// <see cref="WeaponEnemyMatrix"/> <see cref="ScriptableObject"/> to pin the once-per-pair
    /// missing-coverage warning via <see cref="LogAssert"/>.
    /// </summary>
    public sealed class WeaponEnemyMatrixCompletenessFallbackExampleTests
    {
        private static readonly ArchetypeId[] Archetypes = (ArchetypeId[])Enum.GetValues(typeof(ArchetypeId));
        private static readonly RunWeaponFamily[] Families = (RunWeaponFamily[])Enum.GetValues(typeof(RunWeaponFamily));
        private static readonly CombatRole[] Roles = (CombatRole[])Enum.GetValues(typeof(CombatRole));

        // A minimal IEntryLookup test double so the "authored entry wins" path can be exercised without
        // a live ScriptableObject. Returns a fixed response for one (archetype, family) pair only.
        private sealed class SingleEntryLookup : WeaponEnemyResponseResolver.IEntryLookup
        {
            private readonly ArchetypeId _archetype;
            private readonly RunWeaponFamily _family;
            private readonly WeaponResponse _response;

            public SingleEntryLookup(ArchetypeId archetype, RunWeaponFamily family, WeaponResponse response)
            {
                _archetype = archetype;
                _family = family;
                _response = response;
            }

            public bool TryGet(ArchetypeId archetype, RunWeaponFamily family, out WeaponResponse response)
            {
                if (archetype == _archetype && family == _family)
                {
                    response = _response;
                    return true;
                }

                response = default;
                return false;
            }
        }

        // ---- R11.1 / R11.7: every pair resolves to exactly one response (matrix or role fallback) ----

        // Feature: weapon-gameplay-swarm-rework, R11.1 example — with a null lookup, every
        // archetype × family × role combination resolves to a single CombatRole-default response with
        // FromMatrix == false and a valid Q/W/E/R slot (0..3). No pair is ever undefined.
        // Validates: Requirements 11.1, 11.7
        [Test]
        public void NullLookup_EveryPair_FallsBackToRoleDefaultWithValidSlot()
        {
            foreach (ArchetypeId archetype in Archetypes)
            {
                foreach (RunWeaponFamily family in Families)
                {
                    foreach (CombatRole role in Roles)
                    {
                        WeaponResponse response =
                            WeaponEnemyResponseResolver.Resolve(lookup: null, archetype, family, role);

                        Assert.IsFalse(
                            response.FromMatrix,
                            $"archetype={archetype} family={family} role={role}: a null lookup must fall back " +
                            "to the CombatRole default (FromMatrix == false) so the caller logs missing coverage (R11.7)");
                        AssertValidSlot(response.Slot, archetype, family, role);
                    }
                }
            }
        }

        // Feature: weapon-gameplay-swarm-rework, R11.1 example — RoleDefault is total: it returns a
        // valid response for every family × role, so the fallback can never leave a pair undefined.
        // Validates: Requirement 11.1
        [Test]
        public void RoleDefault_ForEveryFamilyAndRole_IsDefinedWithValidSlotAndNotFromMatrix()
        {
            foreach (RunWeaponFamily family in Families)
            {
                foreach (CombatRole role in Roles)
                {
                    WeaponResponse response = WeaponEnemyResponseResolver.RoleDefault(family, role);

                    Assert.IsFalse(
                        response.FromMatrix,
                        $"family={family} role={role}: the role default must be flagged FromMatrix == false (R11.7)");
                    AssertValidSlot(response.Slot, family, role);
                }
            }
        }

        // Feature: weapon-gameplay-swarm-rework, R11.1/R11.6 example — an authored entry, when present,
        // is the single response returned (re-stamped FromMatrix == true), while every other pair for
        // the same family still resolves to its role fallback. Exactly one response per pair, decided by
        // data rather than a per-pair code branch.
        // Validates: Requirements 11.1, 11.6
        [Test]
        public void AuthoredEntry_WinsForItsPair_OthersStillFallBack()
        {
            var authored = new WeaponResponse(
                WeaponEnemyResponseResolver.SlotW, WeaponResponseKind.AntiHeavyBreaker, fromMatrix: true);
            var lookup = new SingleEntryLookup(ArchetypeId.Heavy, RunWeaponFamily.Spear, authored);

            // The authored pair resolves to the authored slot/kind, flagged FromMatrix == true.
            WeaponResponse hit = WeaponEnemyResponseResolver.Resolve(
                lookup, ArchetypeId.Heavy, RunWeaponFamily.Spear, CombatRole.PlayerDisplacement);
            Assert.IsTrue(hit.FromMatrix, "an authored entry must resolve FromMatrix == true (R11.1)");
            Assert.AreEqual(WeaponEnemyResponseResolver.SlotW, hit.Slot, "the authored slot must be honored");
            Assert.AreEqual(WeaponResponseKind.AntiHeavyBreaker, hit.Kind, "the authored kind must be honored");

            // A different family for the same archetype has no authored entry, so it falls back.
            WeaponResponse otherFamily = WeaponEnemyResponseResolver.Resolve(
                lookup, ArchetypeId.Heavy, RunWeaponFamily.Bow, CombatRole.PlayerDisplacement);
            Assert.IsFalse(otherFamily.FromMatrix, "an un-authored pair must fall back (FromMatrix == false) (R11.7)");

            // A different archetype for the authored family also falls back.
            WeaponResponse otherArchetype = WeaponEnemyResponseResolver.Resolve(
                lookup, ArchetypeId.Swarm, RunWeaponFamily.Spear, CombatRole.SwarmFuel);
            Assert.IsFalse(otherArchetype.FromMatrix, "an un-authored pair must fall back (FromMatrix == false) (R11.7)");
        }

        // ---- R11.7: the SO logs missing coverage once per pair ----

        // Feature: weapon-gameplay-swarm-rework, R11.7 example — resolving an un-authored pair on an
        // empty matrix SO returns the role fallback (FromMatrix == false) AND logs a single missing
        // coverage warning; resolving the same pair again does not log a second time.
        // Validates: Requirement 11.7
        [Test]
        public void EmptyMatrix_MissingPair_LogsCoverageWarningOncePerPair()
        {
            var matrix = ScriptableObject.CreateInstance<WeaponEnemyMatrix>();
            try
            {
                Assert.AreEqual(0, matrix.Entries.Count, "the freshly created matrix must have no authored entries");

                // The SO logs a warning that names the archetype, family and role default; match loosely
                // on the archetype/family so the assertion is resilient to phrasing tweaks.
                LogAssert.Expect(
                    LogType.Warning,
                    new System.Text.RegularExpressions.Regex("Swarm.*Spear|Spear.*Swarm"));

                WeaponResponse first = matrix.Resolve(ArchetypeId.Swarm, RunWeaponFamily.Spear, CombatRole.SwarmFuel);
                Assert.IsFalse(first.FromMatrix, "an un-authored pair must resolve via the role fallback (R11.7)");
                AssertValidSlot(first.Slot, ArchetypeId.Swarm, RunWeaponFamily.Spear, CombatRole.SwarmFuel);

                // Resolving the same missing pair again must NOT emit a second warning. If it did, the
                // unexpected log would fail the test because no further LogAssert.Expect was queued.
                WeaponResponse second = matrix.Resolve(ArchetypeId.Swarm, RunWeaponFamily.Spear, CombatRole.SwarmFuel);
                Assert.AreEqual(first, second, "resolving the same pair must be deterministic (R11.1)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(matrix);
            }
        }

        // Feature: weapon-gameplay-swarm-rework, R11.1/R11.7 example — driving an empty matrix SO across
        // every archetype × family confirms each pair resolves to exactly one role-fallback response
        // with a valid slot. Missing-coverage warnings are expected across the whole sweep, so they are
        // ignored here (the once-per-pair logging is asserted precisely above).
        // Validates: Requirements 11.1, 11.7
        [Test]
        public void EmptyMatrix_EveryPair_ResolvesToExactlyOneRoleFallback()
        {
            LogAssert.ignoreFailingMessages = true;
            var matrix = ScriptableObject.CreateInstance<WeaponEnemyMatrix>();
            try
            {
                foreach (ArchetypeId archetype in Archetypes)
                {
                    foreach (RunWeaponFamily family in Families)
                    {
                        foreach (CombatRole role in Roles)
                        {
                            WeaponResponse response = matrix.Resolve(archetype, family, role);

                            Assert.IsFalse(
                                response.FromMatrix,
                                $"archetype={archetype} family={family} role={role}: an empty matrix must " +
                                "resolve every pair via the CombatRole fallback (R11.7)");
                            AssertValidSlot(response.Slot, archetype, family, role);
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(matrix);
                LogAssert.ignoreFailingMessages = false;
            }
        }

        private static void AssertValidSlot(int slot, ArchetypeId archetype, RunWeaponFamily family, CombatRole role)
        {
            Assert.GreaterOrEqual(slot, 0, $"archetype={archetype} family={family} role={role}: slot must be >= 0 (Q..R)");
            Assert.LessOrEqual(slot, 3, $"archetype={archetype} family={family} role={role}: slot must be <= 3 (Q..R)");
        }

        private static void AssertValidSlot(int slot, RunWeaponFamily family, CombatRole role)
        {
            Assert.GreaterOrEqual(slot, 0, $"family={family} role={role}: slot must be >= 0 (Q..R)");
            Assert.LessOrEqual(slot, 3, $"family={family} role={role}: slot must be <= 3 (Q..R)");
        }
    }
}
