using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for gauntlet-boon-playstyle-overhaul task 19.2 — presentation of the ten new
    /// playstyle boons.
    ///
    /// Three concrete behaviours:
    ///   - <see cref="RunModifierPresentation.For"/> / <see cref="RunModifierPresentation.ScopeFor"/> return
    ///     the Portuguese family category, the playstyle scope, and the catalog description for each new boon
    ///     (R15.2/R15.4). The browsable "Nível X/Y" rank format is produced by
    ///     <see cref="RunBoons.OfferReward"/> when it composes the family offer, so this suite also asserts
    ///     the offer Description carries that prefix for a new boon (R15.2).
    ///   - <see cref="RunModifierPresentation.Hint"/> surfaces the expected combination hints for the new
    ///     boons when the partner is owned or co-offered, and falls back otherwise (R15.3).
    ///   - The per-set choice count stays unchanged at three while the new boons are catalogued and offerable
    ///     (R14.3).
    ///
    /// Test seams: <see cref="RunModifierPresentation.For"/>/<see cref="RunModifierPresentation.ScopeFor"/> are
    /// pure, driven directly off a constructed <see cref="RunBoons.Offer"/>. <see cref="RunModifierPresentation.Hint"/>
    /// reads <c>RunBoons.Acquired</c>/<c>Choices</c>, whose backing lists are reached by reflection on an
    /// INACTIVE <see cref="RunBoons"/> (no MonoBehaviour lifecycle), mirroring <see cref="OrbitCombinationHintTests"/>.
    /// The choice-count check drives the real <c>OfferReward</c> with injected private fields, mirroring
    /// <see cref="RetirementOfferPropertyTests"/>.
    ///
    /// Validates: Requirements 15.2, 15.3, 14.3
    /// </summary>
    public sealed class GauntletOverhaulPresentationExampleTests
    {
        // Expected scope string per new boon (RunModifierPresentation.ScopeFor), phrased by playstyle (R15.4).
        private static readonly Dictionary<WeaponBoon, (string scope, string category)> Expected =
            new Dictionary<WeaponBoon, (string, string)>
            {
                { WeaponBoon.AsuraFist,       ("ATAQUE BÁSICO",     "MANOPLAS") },
                { WeaponBoon.GuardBreaker,    ("ATAQUE BÁSICO",     "MANOPLAS") },
                { WeaponBoon.HungryCombo,     ("ATAQUE BÁSICO",     "MANOPLAS") },
                { WeaponBoon.SeismicFist,     ("BÁSICOS + SKILLS",  "MANOPLAS") },
                { WeaponBoon.KitingStep,      ("KITING",            "ARCO")     },
                { WeaponBoon.AdaptiveCadence, ("KITING",            "ARCO")     },
                { WeaponBoon.RainMark,        ("R",                 "ARCO")     },
                { WeaponBoon.SpacingRecoil,   ("ESTOCADAS",         "LANÇA")    },
                { WeaponBoon.PikeWall,        ("W",                 "LANÇA")    },
                { WeaponBoon.EdgeStrike,      ("ESTOCADAS",         "LANÇA")    },
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

        // R15.2/R15.4: ScopeFor returns the playstyle scope and For returns the family category + catalog
        // description for each new boon.
        [Test]
        public void ForAndScopeFor_ReturnPlaystyleScopeCategoryAndDescription_ForNewBoons()
        {
            foreach (KeyValuePair<WeaponBoon, (string scope, string category)> entry in Expected)
            {
                WeaponBoon kind = entry.Key;
                WeaponRunModifiers.Definition def = FindDefinition(kind);
                Assert.IsNotNull(def, $"{kind} must be catalogued.");

                string scope = ScopeFor(kind);
                Assert.AreEqual(entry.Value.scope, scope, $"{kind} scope should read as the playstyle it changes.");

                // For() derives category/accent/scope/description from the Catalog for a family boon offer.
                var offer = new RunBoons.Offer(def.Id, def.Title, def.Description);
                RunModifierPresentation view = RunModifierPresentation.For(offer);

                Assert.AreEqual(entry.Value.category, view.Category, $"{kind} category should be its family label.");
                Assert.AreEqual(entry.Value.scope, view.Scope, $"{kind} scope on the view should match ScopeFor.");
                Assert.AreEqual(def.Description, view.Description, $"{kind} description should be the PT catalog text.");
                Assert.AreEqual(3, view.MaxRank, $"{kind} MaxRank should surface as 3.");
            }
        }

        // R15.2: the browsable rank format "Nível X/Y\n<description>" is produced by OfferReward when it
        // composes a new family boon offer. Confirm on a Gauntlet run for AsuraFist at a fresh rank 1.
        [Test]
        public void OfferReward_FormatsNewBoonDescription_AsNivelXofY()
        {
            RunBoons run = BuildRun(RunWeaponFamily.Gauntlet, out _);
            SetRewriteRoll(run, () => 1.0);
            SetProperty(run, "IsChoosing", false);

            // Drive several fresh reward sets until AsuraFist surfaces (the family slot picks one weapon_ per
            // set), then assert the "Nível 1/3" prefix and the catalog description tail.
            WeaponRunModifiers.Definition def = FindDefinition(WeaponBoon.AsuraFist);
            string expectedPrefix = "Nível 1/" + def.MaxRank;

            bool seen = false;
            for (int i = 0; i < 200 && !seen; i++)
            {
                SetProperty(run, "IsChoosing", false);
                run.OfferReward(0);
                foreach (RunBoons.Offer offer in run.Choices)
                {
                    if (offer.Id != def.Id) continue;
                    seen = true;
                    StringAssert.StartsWith(expectedPrefix, offer.Description,
                        "A new family boon offer must present the 'Nível X/Y' rank format.");
                    StringAssert.Contains(def.Description, offer.Description,
                        "The offer description must carry the catalog description.");
                    break;
                }
            }

            Assert.IsTrue(seen, "AsuraFist should have surfaced in at least one Gauntlet reward set.");
        }

        // R15.3: AsuraFist hint goes active when an Asura finisher boon is owned, and falls back otherwise.
        [Test]
        public void Hint_AsuraFist_IsActive_WhenAsuraSkillOwned()
        {
            RunBoons active = BuildHintRun(acquiredIds: new[] { "weapon_AsuraEcho" }, offeredIds: new[] { "weapon_AsuraFist" });
            Assert.AreEqual("COMBINAÇÃO ATIVA · básicos enchem o Asura mais rápido.",
                RunModifierPresentation.Hint(active, "weapon_AsuraFist"));

            RunBoons fallback = BuildHintRun(acquiredIds: new string[0], offeredIds: new[] { "weapon_AsuraFist" });
            Assert.AreEqual("COMBINE · básicos carregam as skills de Asura.",
                RunModifierPresentation.Hint(fallback, "weapon_AsuraFist"));
        }

        // R15.3: HungryCombo + FlurryEcho, surfaced when both are offered together in the same set.
        [Test]
        public void Hint_HungryCombo_IsActive_WhenCoOfferedWithFlurryEcho()
        {
            RunBoons active = BuildHintRun(acquiredIds: new string[0],
                offeredIds: new[] { "weapon_HungryCombo", "weapon_FlurryEcho" });
            Assert.AreEqual("COMBINAÇÃO ATIVA · básicos trazem as skills de volta.",
                RunModifierPresentation.Hint(active, "weapon_HungryCombo"));

            RunBoons fallback = BuildHintRun(acquiredIds: new string[0], offeredIds: new[] { "weapon_HungryCombo" });
            Assert.AreEqual("COMBINE · básicos reduzem a recarga das skills.",
                RunModifierPresentation.Hint(fallback, "weapon_HungryCombo"));
        }

        // R15.3: RainMark + GuidedRain active when GuidedRain is owned.
        [Test]
        public void Hint_RainMark_IsActive_WhenGuidedRainOwned()
        {
            RunBoons active = BuildHintRun(acquiredIds: new[] { "weapon_GuidedRain" }, offeredIds: new[] { "weapon_RainMark" });
            Assert.AreEqual("COMBINAÇÃO ATIVA · a nuvem persegue e marca.",
                RunModifierPresentation.Hint(active, "weapon_RainMark"));

            RunBoons fallback = BuildHintRun(acquiredIds: new string[0], offeredIds: new[] { "weapon_RainMark" });
            Assert.AreEqual("COMBINE · guie a chuva para manter os alvos marcados.",
                RunModifierPresentation.Hint(fallback, "weapon_RainMark"));
        }

        // R15.3: PikeWall + Orbit active when both already owned.
        [Test]
        public void Hint_PikeWall_IsActive_WhenOrbitOwned()
        {
            RunBoons active = BuildHintRun(acquiredIds: new[] { "weapon_Orbit", "weapon_PikeWall" }, offeredIds: new string[0]);
            Assert.AreEqual("COMBINAÇÃO ATIVA · a zona segura o espaço à frente.",
                RunModifierPresentation.Hint(active, "weapon_PikeWall"));

            RunBoons fallback = BuildHintRun(acquiredIds: new string[0], offeredIds: new[] { "weapon_PikeWall" });
            Assert.AreEqual("COMBINE · Órbita das luas controla a zona de hastes.",
                RunModifierPresentation.Hint(fallback, "weapon_PikeWall"));
        }

        // R14.3: the per-set choice count stays three on each family with the new boons catalogued/offerable.
        [Test]
        public void OfferReward_KeepsThreeChoices_PerFamily()
        {
            foreach (RunWeaponFamily family in (RunWeaponFamily[])Enum.GetValues(typeof(RunWeaponFamily)))
            {
                RunBoons run = BuildRun(family, out _);
                SetRewriteRoll(run, () => 1.0);
                SetProperty(run, "IsChoosing", false);
                run.OfferReward(0);
                Assert.AreEqual(3, run.Choices.Count, $"{family} reward set must present exactly three choices.");
                DestroyHost();
            }
        }

        // ---- Helpers ----------------------------------------------------------------------------------

        private static WeaponRunModifiers.Definition FindDefinition(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition def in WeaponRunModifiers.Catalog)
                if (def.Kind == kind) return def;
            return null;
        }

        // Invokes the private RunModifierPresentation.ScopeFor(WeaponBoon) through reflection (it has no
        // public entry point, but For() routes through it; this gives a direct, focused assertion on scope).
        private static string ScopeFor(WeaponBoon kind)
        {
            MethodInfo method = typeof(RunModifierPresentation).GetMethod(
                "ScopeFor", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Expected private static ScopeFor(WeaponBoon) on RunModifierPresentation (test seam).");
            return (string)method.Invoke(null, new object[] { kind });
        }

        // Builds a RunBoons with acquired/offered ids populated via the private backing lists Hint reads
        // through Acquired/Choices (mirrors OrbitCombinationHintTests). Inactive -> no Awake/Start.
        private RunBoons BuildHintRun(string[] acquiredIds, string[] offeredIds)
        {
            _host = new GameObject("PresentationHintHost");
            _host.SetActive(false);
            RunBoons run = _host.AddComponent<RunBoons>();
            AppendOffers(run, "_acquired", acquiredIds);
            AppendOffers(run, "_choices", offeredIds);
            return run;
        }

        private static void AppendOffers(RunBoons run, string fieldName, string[] ids)
        {
            if (ids == null) return;
            FieldInfo field = typeof(RunBoons).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected private field " + fieldName + " on RunBoons (test seam).");
            var list = (List<RunBoons.Offer>)field.GetValue(run);
            Assert.IsNotNull(list, fieldName + " backing list should be initialized.");
            foreach (string id in ids) list.Add(new RunBoons.Offer(id, id, id));
        }

        // Builds a weapon runtime copy for the requested family and a RunBoons ready for OfferReward.
        private RunBoons BuildRun(RunWeaponFamily family, out WeaponRunModifiers modifiers)
        {
            _host = new GameObject("PresentationOfferHost");
            _host.SetActive(false);
            PlayerActor player = _host.AddComponent<PlayerActor>();
            RunBoons run = _host.AddComponent<RunBoons>();

            WeaponScript weapon = NewWeapon(family);
            modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            Assert.AreEqual(family, modifiers.Family, "Test weapon did not resolve to the requested family.");

            SetPrivate(run, "_player", player);
            SetPrivate(run, "_weapon", weapon);
            SetProperty(run, "WeaponModifiers", modifiers);
            return run;
        }

        private WeaponScript NewWeapon(RunWeaponFamily family)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "PresentationWeapon_" + family;
            SetPrivate(weapon, "_firesArrows", family == RunWeaponFamily.Bow);

            Ability slotQ;
            if (family == RunWeaponFamily.Gauntlet)
            {
                var gauntlet = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
                gauntlet.name = "TestGauntletQ";
                slotQ = gauntlet;
            }
            else
            {
                var arsenal = ScriptableObject.CreateInstance<ArsenalAbility>();
                arsenal.name = "TestQ";
                slotQ = arsenal;
            }
            _createdAbilities.Add(slotQ);
            weapon.abilities = new Ability[] { slotQ };
            _createdWeapons.Add(weapon);
            return weapon;
        }

        private void DestroyHost()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            _host = null;
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
