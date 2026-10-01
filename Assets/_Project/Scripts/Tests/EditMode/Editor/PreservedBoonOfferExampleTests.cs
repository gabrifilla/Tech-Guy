using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for gauntlet-boon-playstyle-overhaul task 4.2: Preserved_Boons stay
    /// offerable after the retirement filter lands in <see cref="RunBoons.OfferReward"/>.
    ///
    /// Retirement (task 4) removes the inline Numeric_Boons (power/haste/recharge/crit/brutal/bulwark/
    /// swift) from the pool and skips the retired family boons in the family gate. These example tests
    /// confirm the complementary half of R2.4/R14.4: every Preserved_Boon CAN still appear in offers and
    /// is NOT filtered out by the retirement logic.
    ///
    /// Preserved_Boons checked here:
    ///   - Inline gameplay boons: conductor, detonation, reactor, resonance, overflow, vitality, focus,
    ///     ignite, frost, transform (R2.4).
    ///   - Preserved family boons per weapon (R2.4): the impactful-weapon-boons set plus every family
    ///     boon that was NOT retired, offered WHILE that family is equipped.
    ///
    /// Test seam: mirrors <see cref="RewriteOfferConstraintTests"/>. <see cref="RunBoons"/> is built on an
    /// INACTIVE GameObject so no MonoBehaviour lifecycle (Awake/Start) runs; the private fields that
    /// <c>OfferReward</c> reads (<c>_player</c>, <c>_weapon</c>, <c>WeaponModifiers</c>) are injected by
    /// reflection. This exercises the real <c>OfferReward</c> offer-construction path (not a
    /// re-implementation), so the assertions reflect the production retirement filter.
    ///
    /// Because <c>OfferReward</c> presents only three choices per set, "offerable" is asserted
    /// empirically: drive many reward sets on a fresh run each time (so single-use boons are never
    /// consumed) and require each Preserved_Boon id to surface at least once, while NO retired id ever
    /// surfaces.
    ///
    /// Validates: Requirements 2.4, 14.4
    /// </summary>
    public sealed class PreservedBoonOfferExampleTests
    {
        // Enough reward sets that every Preserved_Boon is overwhelmingly likely to surface at least once.
        // Each set draws 3 of a bounded pool; with a fixed RNG seed in OfferReward this is deterministic.
        private const int OfferSets = 600;

        // Inline gameplay Preserved_Boons kept in the pool after retirement (R2.4). "transform" is the
        // skill-rewrite id; it only appears when the Rewrite roll admits it, so the roll seam is forced on.
        private static readonly string[] InlineGameplayBoons =
        {
            "conductor", "detonation", "reactor", "resonance", "overflow",
            "vitality", "focus", "ignite", "frost", "transform"
        };

        // Inline Numeric_Boons that retirement removes entirely (R2.1); none may ever be offered.
        private static readonly string[] RetiredNumericBoons =
        {
            "power", "haste", "recharge", "crit", "brutal", "bulwark", "swift"
        };

        // Preserved (non-retired) family boons per family (R2.4) — must stay offerable while equipped.
        private static readonly Dictionary<RunWeaponFamily, WeaponBoon[]> PreservedFamilyBoons =
            new Dictionary<RunWeaponFamily, WeaponBoon[]>
            {
                {
                    RunWeaponFamily.Bow, new[]
                    {
                        WeaponBoon.TwinShot, WeaponBoon.Piercing, WeaponBoon.Ricochet, WeaponBoon.Homing,
                        WeaponBoon.WideVolley, WeaponBoon.GuidedRain, WeaponBoon.SplitArrow,
                        WeaponBoon.ChargedShot, WeaponBoon.RapidBurst,
                        // the new impactful substitutes are also offerable Bow boons
                        WeaponBoon.KitingStep, WeaponBoon.AdaptiveCadence, WeaponBoon.RainMark
                    }
                },
                {
                    RunWeaponFamily.Spear, new[]
                    {
                        WeaponBoon.Trident, WeaponBoon.PhantomSpear, WeaponBoon.MoonShard,
                        WeaponBoon.ReturnWave, WeaponBoon.ChainThrust, WeaponBoon.ImpalingLine,
                        WeaponBoon.Orbit, WeaponBoon.EchoThrust, WeaponBoon.DragonWave,
                        WeaponBoon.SpearTip, WeaponBoon.Execution, WeaponBoon.PerfectSpacing,
                        WeaponBoon.Siphon,
                        WeaponBoon.SpacingRecoil, WeaponBoon.PikeWall, WeaponBoon.EdgeStrike
                    }
                },
                {
                    RunWeaponFamily.Gauntlet, new[]
                    {
                        WeaponBoon.ComboNova, WeaponBoon.ShockRing, WeaponBoon.MomentumStrike,
                        WeaponBoon.Shockwave, WeaponBoon.Momentum, WeaponBoon.AsuraReserve,
                        WeaponBoon.RocketAdvance, WeaponBoon.FlurryEcho, WeaponBoon.AsuraEcho,
                        WeaponBoon.AsuraFist, WeaponBoon.GuardBreaker, WeaponBoon.HungryCombo,
                        WeaponBoon.SeismicFist
                    }
                }
            };

        // Retired family boons per family (R2.2) — must NEVER be offered, even though they stay catalogued.
        private static readonly WeaponBoon[] RetiredFamilyBoons =
        {
            WeaponBoon.LongFists, WeaponBoon.StanceCrusher, WeaponBoon.Berserker, // Gauntlet
            WeaponBoon.HeavyBolt, WeaponBoon.Sniper, WeaponBoon.LongRain,         // Bow
            WeaponBoon.LongReach, WeaponBoon.TripleMoon, WeaponBoon.Affliction    // Spear
        };

        private readonly List<WeaponScript> _createdWeapons = new List<WeaponScript>();
        private readonly List<Ability> _createdAbilities = new List<Ability>();
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            _host = null;
            foreach (WeaponScript weapon in _createdWeapons) if (weapon) UnityEngine.Object.DestroyImmediate(weapon);
            _createdWeapons.Clear();
            foreach (Ability ability in _createdAbilities) if (ability) UnityEngine.Object.DestroyImmediate(ability);
            _createdAbilities.Clear();
        }

        // Collects every offer id seen across many fresh reward sets for the given family. A new RunBoons
        // is built per set so single-use boons (transform/focus/recharge/vitality) are never consumed and
        // the full pool is always reachable. The Rewrite roll is forced to admit the transform so it is
        // reachable in the offer set (its <= 20% appearance gate would otherwise suppress most sets).
        private HashSet<string> CollectOfferedIds(RunWeaponFamily family)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < OfferSets; i++)
            {
                WeaponScript weapon = NewWeapon(family);
                RunBoons run = BuildRun(weapon);
                SetRewriteRoll(run, () => 0.0); // always admit the Rewrite (transform) slot
                run.OfferReward(0);
                foreach (RunBoons.Offer offer in run.Choices) seen.Add(offer.Id);
                UnityEngine.Object.DestroyImmediate(_host);
                _host = null;
            }
            return seen;
        }

        // R2.4/R14.4: every inline gameplay Preserved_Boon stays offerable after retirement.
        [Test]
        public void InlineGameplayBoons_StayOfferable_AfterRetirement()
        {
            // Inline gameplay boons are family-independent (except the transform title); any family works.
            HashSet<string> offered = CollectOfferedIds(RunWeaponFamily.Gauntlet);
            foreach (string id in InlineGameplayBoons)
                Assert.IsTrue(offered.Contains(id),
                    $"Preserved inline gameplay boon '{id}' must still be offerable after retirement.");
        }

        // R2.1: no retired inline Numeric_Boon is ever offered, for any family.
        [Test]
        public void RetiredNumericBoons_AreNeverOffered()
        {
            foreach (RunWeaponFamily family in (RunWeaponFamily[])Enum.GetValues(typeof(RunWeaponFamily)))
            {
                HashSet<string> offered = CollectOfferedIds(family);
                foreach (string id in RetiredNumericBoons)
                    Assert.IsFalse(offered.Contains(id),
                        $"Retired numeric boon '{id}' must never be offered ({family} run).");
            }
        }

        // R2.4: every preserved (non-retired) family boon stays offerable while its family is equipped.
        [Test]
        public void PreservedFamilyBoons_StayOfferable_WhileEquipped()
        {
            foreach (KeyValuePair<RunWeaponFamily, WeaponBoon[]> entry in PreservedFamilyBoons)
            {
                RunWeaponFamily family = entry.Key;
                HashSet<string> offered = CollectOfferedIds(family);
                foreach (WeaponBoon boon in entry.Value)
                {
                    string id = WeaponRunModifiers.CatalogId(boon);
                    Assert.IsTrue(offered.Contains(id),
                        $"Preserved family boon {boon} ('{id}') must stay offerable on a {family} run.");
                }
            }
        }

        // R2.2: a retired family boon is never offered, even while its own family is equipped (it stays
        // catalogued for save/run compat, but the family gate skips it).
        [Test]
        public void RetiredFamilyBoons_AreNeverOffered_EvenInOwnFamily()
        {
            foreach (RunWeaponFamily family in (RunWeaponFamily[])Enum.GetValues(typeof(RunWeaponFamily)))
            {
                HashSet<string> offered = CollectOfferedIds(family);
                foreach (WeaponBoon boon in RetiredFamilyBoons)
                {
                    string id = WeaponRunModifiers.CatalogId(boon);
                    Assert.IsFalse(offered.Contains(id),
                        $"Retired family boon {boon} ('{id}') must never be offered ({family} run).");
                }
            }
        }

        // Builds a weapon runtime copy whose WeaponRunModifiers.Identify resolves to the requested family:
        //   Bow      -> FiresArrows == true
        //   Gauntlet -> a BreakerGauntletAbility in slot 1
        //   Spear    -> a plain ArsenalAbility in slot 1 (not firing arrows, no gauntlet ability)
        // Slot 1 always holds a live ability so HasRewriteTarget() is true and the transform is reachable.
        private WeaponScript NewWeapon(RunWeaponFamily family)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "TestRunWeapon_" + family;

            Ability q;
            if (family == RunWeaponFamily.Gauntlet)
            {
                q = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            }
            else
            {
                q = ScriptableObject.CreateInstance<ArsenalAbility>();
                if (family == RunWeaponFamily.Bow) SetPrivate(weapon, "_firesArrows", true);
            }
            q.name = "TestQ_" + family;
            weapon.abilities = new Ability[] { q };

            // Guard against a mis-built fixture: the test only makes sense if the weapon identifies as
            // the family under test.
            Assert.AreEqual(family, WeaponRunModifiers.Identify(weapon),
                $"Test weapon fixture must identify as {family}.");

            _createdWeapons.Add(weapon);
            _createdAbilities.Add(q);
            return weapon;
        }

        // Builds a RunBoons ready for OfferReward without running Awake/Start: the fields OfferReward reads
        // are injected directly. The GameObject is inactive so no lifecycle runs.
        private RunBoons BuildRun(WeaponScript weapon)
        {
            _host = new GameObject("RunBoonsPreservedHost");
            _host.SetActive(false);
            PlayerActor player = _host.AddComponent<PlayerActor>(); // fresh -> IsDead == false
            RunBoons run = _host.AddComponent<RunBoons>();

            SetPrivate(run, "_player", player);
            SetPrivate(run, "_weapon", weapon);
            SetProperty(run, "WeaponModifiers", new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon)));
            return run;
        }

        private static void SetRewriteRoll(RunBoons run, Func<double> roll)
        {
            FieldInfo field = typeof(RunBoons).GetField("_rewriteRoll", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected private field _rewriteRoll on RunBoons (test seam).");
            field.SetValue(run, roll);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected private field " + fieldName + " on " + target.GetType().Name + " (test seam).");
            field.SetValue(target, value);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(property, "Expected property " + propertyName + " on " + target.GetType().Name + " (test seam).");
            property.SetValue(target, value);
        }
    }
}
