using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example test for R5.4 of modifier-synergies-theme17: the Orbit combination hint.
    ///
    /// <see cref="RunModifierPresentation.Hint"/> for "weapon_Orbit" must return
    /// "COMBINAÇÃO ATIVA · luas avançam a cada pulso." when TripleMoon and Orbit are combined —
    /// either both already owned, or both offered together in the same selection — and fall back
    /// to the "COMBINE ..." prompt otherwise.
    ///
    /// Test seam: <see cref="RunBoons"/> is a MonoBehaviour whose acquired/offered sets are private
    /// readonly lists exposed only through the read-only <c>Acquired</c>/<c>Choices</c> getters that
    /// <c>Hint</c> reads. There is no public mutator, so the test reaches the backing lists by
    /// reflection and appends offers to them directly. The RunBoons is created on an INACTIVE
    /// GameObject so no MonoBehaviour lifecycle (Awake/Start) runs and no scene/player setup is
    /// required — we exercise only the pure presentation logic.
    /// </summary>
    public sealed class OrbitCombinationHintTests
    {
        private const string OrbitId = "weapon_Orbit";
        private const string TripleMoonId = "weapon_TripleMoon";
        private const string ActiveHint = "COMBINAÇÃO ATIVA · luas avançam a cada pulso.";
        private const string FallbackHint = "COMBINE · Órbita das luas acrescenta pulsos ao W.";

        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            _host = null;
        }

        // Builds a RunBoons with the given acquired ids and offered (choice) ids populated via the
        // private backing lists that Hint reads through Acquired/Choices.
        private RunBoons BuildRun(string[] acquiredIds, string[] offeredIds)
        {
            _host = new GameObject("RunBoonsHintHost");
            _host.SetActive(false); // suppress Awake/Start; we only need the object graph, not lifecycle
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

        // Feature: modifier-synergies-theme17, R5.4 example — active hint when TripleMoon is owned.
        // Validates: Requirement 5.4
        [Test]
        public void OrbitHint_IsActive_WhenTripleMoonOwned()
        {
            RunBoons run = BuildRun(acquiredIds: new[] { TripleMoonId }, offeredIds: new[] { OrbitId });
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, OrbitId));
        }

        // Feature: modifier-synergies-theme17, R5.4 example — active hint when both offered together.
        // Validates: Requirement 5.4
        [Test]
        public void OrbitHint_IsActive_WhenBothOfferedTogether()
        {
            RunBoons run = BuildRun(acquiredIds: new string[0], offeredIds: new[] { OrbitId, TripleMoonId });
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, OrbitId));
        }

        // Feature: modifier-synergies-theme17, R5.4 example — active hint when both already owned.
        // Validates: Requirement 5.4
        [Test]
        public void OrbitHint_IsActive_WhenBothOwned()
        {
            RunBoons run = BuildRun(acquiredIds: new[] { OrbitId, TripleMoonId }, offeredIds: new string[0]);
            Assert.AreEqual(ActiveHint, RunModifierPresentation.Hint(run, OrbitId));
        }

        // Feature: modifier-synergies-theme17, R5.4 example — fallback when Orbit is alone.
        // Offering Orbit without TripleMoon (and no TripleMoon owned) must NOT show the active hint.
        // Validates: Requirement 5.4
        [Test]
        public void OrbitHint_IsFallback_WhenOrbitAloneOffered()
        {
            RunBoons run = BuildRun(acquiredIds: new string[0], offeredIds: new[] { OrbitId });
            Assert.AreEqual(FallbackHint, RunModifierPresentation.Hint(run, OrbitId));
        }

        // Feature: modifier-synergies-theme17, R5.4 example — fallback when only Orbit is owned.
        // Validates: Requirement 5.4
        [Test]
        public void OrbitHint_IsFallback_WhenOnlyOrbitOwned()
        {
            RunBoons run = BuildRun(acquiredIds: new[] { OrbitId }, offeredIds: new string[0]);
            Assert.AreEqual(FallbackHint, RunModifierPresentation.Hint(run, OrbitId));
        }
    }
}
