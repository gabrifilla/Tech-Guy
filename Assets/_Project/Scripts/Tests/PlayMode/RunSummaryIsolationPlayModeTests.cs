using NUnit.Framework;
using UnityEngine;
namespace TechGuy.Tests
{
    public sealed class RunSummaryIsolationPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        [TestCase(true)] [TestCase(false)]
        public void ScopeRestoresExactValueOrAbsence(bool existed)
        {
            if (existed) PlayerPrefs.SetString(RunSummaryStore.PreferenceKey, "original bytes: {not required to be valid}");
            else PlayerPrefs.DeleteKey(RunSummaryStore.PreferenceKey);
            using (new RunSummaryPreferenceScope()) PlayerPrefs.SetString(RunSummaryStore.PreferenceKey, "test replacement");
            Assert.That(PlayerPrefs.HasKey(RunSummaryStore.PreferenceKey), Is.EqualTo(existed));
            if (existed) Assert.That(PlayerPrefs.GetString(RunSummaryStore.PreferenceKey), Is.EqualTo("original bytes: {not required to be valid}"));
        }
    }
}
