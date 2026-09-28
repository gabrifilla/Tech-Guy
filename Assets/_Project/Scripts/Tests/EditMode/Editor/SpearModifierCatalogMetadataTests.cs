using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for R7 of modifier-synergies-theme17: catalog membership and metadata
    /// for the four new transformative Spear modifiers (PhantomSpear, MoonShard, ReturnWave,
    /// ChainThrust).
    ///
    /// Verifies each new modifier is registered in <see cref="WeaponRunModifiers.Catalog"/> with a
    /// title, description, family = Spear, a positive max rank, and the expected presentation scope
    /// (via <c>RunModifierPresentation.For</c>, which delegates to the private <c>ScopeFor</c>), and
    /// that offer eligibility is gated to an equipped Spear weapon through the existing
    /// <c>definition.Family == WeaponModifiers.Family</c> gate — exercised here through
    /// <see cref="WeaponRunModifiers.Add"/>, which enforces that same family gate.
    ///
    /// Pure metadata/logic: no scene, no physics.
    /// Validates: Requirements 7.1, 7.2, 7.4, 7.7
    /// </summary>
    public sealed class SpearModifierCatalogMetadataTests
    {
        private static readonly WeaponBoon[] NewSpearBoons =
        {
            WeaponBoon.PhantomSpear, WeaponBoon.MoonShard, WeaponBoon.ReturnWave, WeaponBoon.ChainThrust
        };

        // Expected presentation scope per new modifier (see RunModifierPresentation.ScopeFor):
        //   PhantomSpear/ChainThrust -> thrust family "ESTOCADAS"; MoonShard -> "W"; ReturnWave -> "R".
        private static readonly Dictionary<WeaponBoon, string> ExpectedScope = new Dictionary<WeaponBoon, string>
        {
            { WeaponBoon.PhantomSpear, "ESTOCADAS" },
            { WeaponBoon.ChainThrust,  "ESTOCADAS" },
            { WeaponBoon.MoonShard,    "W" },
            { WeaponBoon.ReturnWave,   "R" },
        };

        private static WeaponRunModifiers.Definition Find(WeaponBoon kind)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            return null;
        }

        // R7.1/R7.7: each new modifier is registered with the required metadata.
        [Test]
        public void NewSpearModifiers_HaveTitleDescriptionSpearFamilyAndMaxRank()
        {
            foreach (WeaponBoon kind in NewSpearBoons)
            {
                WeaponRunModifiers.Definition def = Find(kind);
                Assert.IsNotNull(def, $"{kind} must be registered in the weapon catalog.");
                Assert.AreEqual(RunWeaponFamily.Spear, def.Family, $"{kind} must be Spear family.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Title), $"{kind} must have a non-empty title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Description), $"{kind} must have a non-empty description.");
                Assert.Greater(def.MaxRank, 0, $"{kind} must have a positive max rank.");
                Assert.AreEqual("weapon_" + kind, def.Id, $"{kind} id should follow the weapon_ convention.");
            }
        }

        // R7.1/R7.7: each new modifier resolves a presentation scope through RunModifierPresentation.
        [Test]
        public void NewSpearModifiers_ResolveExpectedPresentationScope()
        {
            foreach (WeaponBoon kind in NewSpearBoons)
            {
                WeaponRunModifiers.Definition def = Find(kind);
                Assert.IsNotNull(def, $"{kind} must be registered in the weapon catalog.");

                var offer = new RunBoons.Offer(def.Id, def.Title, def.Description);
                RunModifierPresentation view = RunModifierPresentation.For(offer);

                Assert.AreEqual("LANÇA", view.Category, $"{kind} should present under the Spear category.");
                Assert.AreEqual(ExpectedScope[kind], view.Scope, $"{kind} presentation scope mismatch.");
                Assert.AreEqual(def.MaxRank, view.MaxRank, $"{kind} presentation max rank should mirror the catalog.");
                Assert.IsFalse(view.IsRewrite, $"{kind} is a Spear behavior, not a Rewrite.");
            }
        }

        // R7.7: offer eligibility is gated to an equipped Spear weapon (Family gate). A Spear run
        // accepts each new modifier; Bow and Gauntlet runs reject them via the same family gate that
        // RunBoons.OfferReward uses to build the offer pool.
        [Test]
        public void NewSpearModifiers_AreOfferedOnlyForSpearFamily()
        {
            foreach (WeaponBoon kind in NewSpearBoons)
            {
                WeaponRunModifiers.Definition def = Find(kind);
                Assert.IsNotNull(def, $"{kind} must be registered in the weapon catalog.");

                var spear = new WeaponRunModifiers(RunWeaponFamily.Spear);
                Assert.IsTrue(spear.Add(def), $"{kind} must be acquirable on a Spear run.");
                Assert.AreEqual(1, spear.Rank(kind), $"{kind} rank should increment on a Spear run.");

                var bow = new WeaponRunModifiers(RunWeaponFamily.Bow);
                Assert.IsFalse(bow.Add(def), $"{kind} must NOT be acquirable on a Bow run (family gate).");
                Assert.AreEqual(0, bow.Rank(kind), $"{kind} must stay at rank 0 on a Bow run.");

                var gauntlet = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                Assert.IsFalse(gauntlet.Add(def), $"{kind} must NOT be acquirable on a Gauntlet run (family gate).");
                Assert.AreEqual(0, gauntlet.Rank(kind), $"{kind} must stay at rank 0 on a Gauntlet run.");
            }
        }

        // R7.7: the family gate can be climbed up to the declared max rank and no further, matching
        // the per-offer eligibility check (Rank < MaxRank) in RunBoons.OfferReward.
        [Test]
        public void NewSpearModifiers_StackToMaxRankThenStop()
        {
            foreach (WeaponBoon kind in NewSpearBoons)
            {
                WeaponRunModifiers.Definition def = Find(kind);
                var spear = new WeaponRunModifiers(RunWeaponFamily.Spear);

                for (int r = 0; r < def.MaxRank; r++)
                    Assert.IsTrue(spear.Add(def), $"{kind} rank {r + 1} should be addable up to MaxRank {def.MaxRank}.");

                Assert.AreEqual(def.MaxRank, spear.Rank(kind), $"{kind} should reach its max rank.");
                Assert.IsFalse(spear.Add(def), $"{kind} must not exceed MaxRank {def.MaxRank}.");
            }
        }
    }
}
