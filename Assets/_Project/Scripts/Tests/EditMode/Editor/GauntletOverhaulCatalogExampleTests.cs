using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for gauntlet-boon-playstyle-overhaul task 3.1: catalog structure for the
    /// ten new playstyle boons.
    ///
    /// Verifies, as concrete examples:
    ///   - Each new <see cref="WeaponBoon"/> appears EXACTLY ONCE in <see cref="WeaponRunModifiers.Catalog"/>,
    ///     in its expected <see cref="RunWeaponFamily"/>, with <c>MaxRank == 3</c> and non-empty PT title/description.
    ///   - The ten new identifiers are inédito — they do not collide with any pre-existing enum value.
    ///   - Preserved boons (the impactful-weapon-boons set and the retired-but-kept family boons) remain
    ///     intact in the catalog with their original family.
    ///
    /// Pure metadata/logic: no scene, no physics.
    /// Validates: Requirements 1.5, 1.8, 4.5, 5.6, 6.5, 7.5, 8.5, 9.5, 10.5, 11.6, 12.5, 13.5
    /// </summary>
    public sealed class GauntletOverhaulCatalogExampleTests
    {
        // The ten new boons and the family each must be catalogued under (design "Where each boon plugs in").
        private static readonly Dictionary<WeaponBoon, RunWeaponFamily> NewBoons = new Dictionary<WeaponBoon, RunWeaponFamily>
        {
            { WeaponBoon.AsuraFist,       RunWeaponFamily.Gauntlet }, // R4
            { WeaponBoon.GuardBreaker,    RunWeaponFamily.Gauntlet }, // R5
            { WeaponBoon.HungryCombo,     RunWeaponFamily.Gauntlet }, // R6
            { WeaponBoon.SeismicFist,     RunWeaponFamily.Gauntlet }, // R7
            { WeaponBoon.KitingStep,      RunWeaponFamily.Bow      }, // R8
            { WeaponBoon.AdaptiveCadence, RunWeaponFamily.Bow      }, // R9
            { WeaponBoon.RainMark,        RunWeaponFamily.Bow      }, // R10
            { WeaponBoon.SpacingRecoil,   RunWeaponFamily.Spear    }, // R11
            { WeaponBoon.PikeWall,        RunWeaponFamily.Spear    }, // R12
            { WeaponBoon.EdgeStrike,      RunWeaponFamily.Spear    }, // R13
        };

        // Preserved boons that must stay intact after the overhaul. The impactful-weapon-boons set plus the
        // retired-but-kept family boons (retire != delete: they remain in the Catalog for run/save compat, R2.5/R2.6).
        private static readonly Dictionary<WeaponBoon, RunWeaponFamily> PreservedBoons = new Dictionary<WeaponBoon, RunWeaponFamily>
        {
            // impactful-weapon-boons (R1.8 "do not redefine/duplicate preserved boons").
            { WeaponBoon.MomentumStrike, RunWeaponFamily.Gauntlet },
            { WeaponBoon.Shockwave,      RunWeaponFamily.Gauntlet },
            { WeaponBoon.SplitArrow,     RunWeaponFamily.Bow      },
            { WeaponBoon.ChargedShot,    RunWeaponFamily.Bow      },
            { WeaponBoon.PerfectSpacing, RunWeaponFamily.Spear    },
            { WeaponBoon.ImpalingLine,   RunWeaponFamily.Spear    },
            // Retired-but-kept family boons (removed only from OfferReward, still catalogued).
            { WeaponBoon.LongFists,      RunWeaponFamily.Gauntlet },
            { WeaponBoon.StanceCrusher,  RunWeaponFamily.Gauntlet },
            { WeaponBoon.Berserker,      RunWeaponFamily.Gauntlet },
            { WeaponBoon.HeavyBolt,      RunWeaponFamily.Bow      },
            { WeaponBoon.Sniper,         RunWeaponFamily.Bow      },
            { WeaponBoon.LongRain,       RunWeaponFamily.Bow      },
            { WeaponBoon.LongReach,      RunWeaponFamily.Spear    },
            { WeaponBoon.TripleMoon,     RunWeaponFamily.Spear    },
            { WeaponBoon.Affliction,     RunWeaponFamily.Spear    },
        };

        private static int CountDefinitions(WeaponBoon kind)
        {
            int count = 0;
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) count++;
            return count;
        }

        private static WeaponRunModifiers.Definition Find(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            return null;
        }

        // R1.5/R4.5/R5.6/R6.5/R7.5/R8.5/R9.5/R10.5/R11.6/R12.5/R13.5: each new boon is catalogued exactly
        // once, in the expected family, with MaxRank 3 and non-empty PT metadata.
        [Test]
        public void EachNewBoon_AppearsExactlyOnce_WithExpectedFamilyAndMaxRank()
        {
            foreach (KeyValuePair<WeaponBoon, RunWeaponFamily> entry in NewBoons)
            {
                WeaponBoon kind = entry.Key;
                Assert.AreEqual(1, CountDefinitions(kind),
                    $"{kind} must appear exactly once in the Catalog.");

                WeaponRunModifiers.Definition def = Find(kind);
                Assert.IsNotNull(def, $"{kind} must be registered in the Catalog.");
                Assert.AreEqual(entry.Value, def.Family, $"{kind} must be in the {entry.Value} family.");
                Assert.AreEqual(3, def.MaxRank, $"{kind} must have MaxRank == 3.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Title), $"{kind} must have a non-empty title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Description), $"{kind} must have a non-empty description.");
                Assert.AreEqual("weapon_" + kind, def.Id, $"{kind} id should follow the weapon_ convention.");
            }
        }

        // R1.8: the ten new identifiers are inédito — the names are distinct from one another and from the
        // pre-existing boons. Enum values are unique by definition, so this guards against a copy-paste
        // slip where a "new" row reuses an existing enum member.
        [Test]
        public void NewBoonNames_DoNotCollideWithExistingEnumValues()
        {
            var newNames = new HashSet<string>();
            foreach (WeaponBoon kind in NewBoons.Keys)
                Assert.IsTrue(newNames.Add(kind.ToString()), $"{kind} is listed twice among the new boons.");

            // No new boon shares an identifier with any preserved boon tracked by this suite.
            foreach (WeaponBoon preserved in PreservedBoons.Keys)
                Assert.IsFalse(newNames.Contains(preserved.ToString()),
                    $"New boon name collides with preserved boon {preserved}.");

            // The ten new identifiers match the design's inédito list exactly (guards against renames).
            var expectedNames = new HashSet<string>
            {
                "AsuraFist", "GuardBreaker", "HungryCombo", "SeismicFist",
                "KitingStep", "AdaptiveCadence", "RainMark",
                "SpacingRecoil", "PikeWall", "EdgeStrike"
            };
            Assert.IsTrue(expectedNames.SetEquals(newNames),
                "The set of new boon names must match the design's inédito identifiers exactly.");
        }

        // Every WeaponBoon enum value is catalogued at most once (no duplicate Definition rows for any kind).
        [Test]
        public void Catalog_HasNoDuplicateDefinitions()
        {
            var seen = new HashSet<WeaponBoon>();
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                Assert.IsTrue(seen.Add(definition.Kind),
                    $"{definition.Kind} appears more than once in the Catalog.");
        }

        // R1.8/R2.5/R2.6: preserved boons remain intact in the Catalog with their original family after the
        // overhaul (the impactful-weapon-boons set and the retired-but-kept family boons).
        [Test]
        public void PreservedBoons_RemainIntact_InExpectedFamily()
        {
            foreach (KeyValuePair<WeaponBoon, RunWeaponFamily> entry in PreservedBoons)
            {
                WeaponBoon kind = entry.Key;
                Assert.AreEqual(1, CountDefinitions(kind),
                    $"Preserved boon {kind} must still appear exactly once in the Catalog.");

                WeaponRunModifiers.Definition def = Find(kind);
                Assert.IsNotNull(def, $"Preserved boon {kind} must still be in the Catalog.");
                Assert.AreEqual(entry.Value, def.Family, $"Preserved boon {kind} must stay in the {entry.Value} family.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Title), $"Preserved boon {kind} must keep a non-empty title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Description), $"Preserved boon {kind} must keep a non-empty description.");
            }
        }

        // R1.5: each new boon is acquirable only through its own family's gate (WeaponRunModifiers.Add),
        // and stacks exactly up to MaxRank == 3 and no further — the same gate RunBoons.OfferReward uses.
        [Test]
        public void EachNewBoon_StacksToMaxRankThree_OnlyInItsFamily()
        {
            foreach (KeyValuePair<WeaponBoon, RunWeaponFamily> entry in NewBoons)
            {
                WeaponBoon kind = entry.Key;
                RunWeaponFamily family = entry.Value;
                WeaponRunModifiers.Definition def = Find(kind);
                Assert.IsNotNull(def, $"{kind} must be registered in the Catalog.");

                var owning = new WeaponRunModifiers(family);
                for (int r = 1; r <= 3; r++)
                {
                    Assert.IsTrue(owning.Add(def), $"{kind} rank {r} should be addable in its own family.");
                    Assert.AreEqual(r, owning.Rank(kind), $"{kind} rank should be {r} after {r} adds.");
                }
                Assert.IsFalse(owning.Add(def), $"{kind} must not exceed MaxRank 3.");
                Assert.AreEqual(3, owning.Rank(kind), $"{kind} should cap at rank 3.");

                foreach (RunWeaponFamily other in (RunWeaponFamily[])Enum.GetValues(typeof(RunWeaponFamily)))
                {
                    if (other == family) continue;
                    var foreign = new WeaponRunModifiers(other);
                    Assert.IsFalse(foreign.Add(def), $"{kind} must NOT be acquirable on a {other} run (family gate).");
                    Assert.AreEqual(0, foreign.Rank(kind), $"{kind} must stay at rank 0 on a {other} run.");
                }
            }
        }
    }
}
