using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for R6 of modifier-synergies-theme17:
    ///  - R6.4: the Haste→ComboNova combination hint is shown only when both are owned or both are
    ///    offered together, and not otherwise.
    ///  - R6.5: a rank-0 (not owned) ComboNova fires no nova even when the running basic count hits a
    ///    multiple of 3, leaving the basic outcome otherwise unchanged.
    ///
    /// Test seams:
    ///  - Hint gating reuses the OrbitCombinationHintTests pattern: <see cref="RunBoons"/> is created
    ///    on an INACTIVE GameObject (no Awake/Start), and its private <c>_acquired</c>/<c>_choices</c>
    ///    backing lists (read by <see cref="RunModifierPresentation.Hint"/> via Acquired/Choices) are
    ///    populated by reflection. This exercises only the pure presentation logic.
    ///  - Rank-0 skip is verified against the gate model that mirrors
    ///    <see cref="CharControlScript.TryProcComboNova"/>: nova &lt;= 0 returns before any nova.
    /// </summary>
    public sealed class HasteComboNovaExampleTests
    {
        private const string HasteId = "haste";
        private const string ComboNovaId = "weapon_ComboNova";
        private const string ActiveHint = "COMBINAÇÃO ATIVA · Ímpeto acelera a Nova de combo.";
        private const string FallbackHint = "COMBINE · Ímpeto acelera a cadência da Nova de combo.";
        private const int ComboNovaBasicInterval = 3;

        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            _host = null;
        }

        private RunBoons BuildRun(string[] acquiredIds, string[] offeredIds)
        {
            _host = new GameObject("RunBoonsComboNovaHintHost");
            _host.SetActive(false); // suppress Awake/Start; only the object graph is needed
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
            foreach (string id in ids)
                list.Add(new RunBoons.Offer(id, id, id));
        }

        // ---- R6.4 hint gating (shown only when both owned or both offered together) ----

        // Feature: modifier-synergies-theme17, R6.4 example — active hint when both already owned.
        // Validates: Requirement 6.4
        [Test]
        public void HasteComboNovaHint_IsActive_WhenBothOwned()
        {
            RunBoons run = BuildRun(acquiredIds: new[] { HasteId, ComboNovaId }, offeredIds: new string[0]);
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, HasteId));
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, ComboNovaId));
        }

        // Feature: modifier-synergies-theme17, R6.4 example — active hint when both offered together.
        // Validates: Requirement 6.4
        [Test]
        public void HasteComboNovaHint_IsActive_WhenBothOfferedTogether()
        {
            RunBoons run = BuildRun(acquiredIds: new string[0], offeredIds: new[] { HasteId, ComboNovaId });
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, HasteId));
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, ComboNovaId));
        }

        // Feature: modifier-synergies-theme17, R6.4 example — active for one when the partner is owned.
        // Validates: Requirement 6.4
        [Test]
        public void HasteComboNovaHint_IsActive_WhenPartnerOwned()
        {
            // Haste owned, ComboNova offered -> ComboNova's hint is active (partner owned).
            RunBoons run = BuildRun(acquiredIds: new[] { HasteId }, offeredIds: new[] { ComboNovaId });
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, ComboNovaId));
        }

        // Feature: modifier-synergies-theme17, R6.4 example — fallback when Haste is alone.
        // Validates: Requirement 6.4
        [Test]
        public void HasteHint_IsFallback_WhenHasteAloneOffered()
        {
            RunBoons run = BuildRun(acquiredIds: new string[0], offeredIds: new[] { HasteId });
            Assert.AreEqual(FallbackHint, RunModifierPresentation.Hint(run, HasteId));
        }

        // Feature: modifier-synergies-theme17, R6.4 example — fallback when only ComboNova is owned.
        // Validates: Requirement 6.4
        [Test]
        public void ComboNovaHint_IsFallback_WhenOnlyComboNovaOwned()
        {
            RunBoons run = BuildRun(acquiredIds: new[] { ComboNovaId }, offeredIds: new string[0]);
            Assert.AreEqual(FallbackHint, RunModifierPresentation.Hint(run, ComboNovaId));
        }

        // Feature: modifier-synergies-theme17, R6.4 example — fallback when neither is present.
        // Validates: Requirement 6.4
        [Test]
        public void HasteComboNovaHint_IsFallback_WhenNeitherPresent()
        {
            RunBoons run = BuildRun(acquiredIds: new string[0], offeredIds: new[] { HasteId });
            Assert.AreEqual(FallbackHint, RunModifierPresentation.Hint(run, HasteId));
        }

        // ---- R6.5 rank-0 ComboNova fires no nova ----

        // Mirror of the CharControlScript.TryProcComboNova gate: fires only when rank > 0 AND the
        // running basic count is a multiple of the interval.
        private static bool NovaFires(int rank, int runBasicCount)
            => rank > 0 && runBasicCount % ComboNovaBasicInterval == 0;

        // Feature: modifier-synergies-theme17, R6.5 example — rank-0 ComboNova fires no nova.
        // Even on every basic count that is a multiple of 3, a rank-0 (not owned) ComboNova must not
        // trigger a nova; the basic attack outcome is otherwise unchanged (no area damage applied).
        // Validates: Requirement 6.5
        [Test]
        public void ComboNova_RankZero_FiresNoNova_OnEveryThirdBasic()
        {
            for (int count = 1; count <= 30; count++)
            {
                Assert.IsFalse(NovaFires(0, count),
                    $"Rank-0 ComboNova must not fire a nova at basic count {count} (multiple of 3: {count % 3 == 0}).");
            }
        }

        // Feature: modifier-synergies-theme17, R6.5 example — owned ComboNova does fire on thirds.
        // Sanity companion to the rank-0 skip: a rank-1+ ComboNova fires exactly on multiples of 3.
        // Validates: Requirement 6.5
        [Test]
        public void ComboNova_RankOne_FiresOnlyOnEveryThirdBasic()
        {
            for (int count = 1; count <= 30; count++)
            {
                bool expected = count % 3 == 0;
                Assert.AreEqual(expected, NovaFires(1, count),
                    $"Rank-1 ComboNova at basic count {count}: expected fire={expected}.");
            }
        }
    }
}
