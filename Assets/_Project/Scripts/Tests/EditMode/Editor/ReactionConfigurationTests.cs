using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;
using UnityEditor;

namespace TechGuy.Tests.EditMode
{
    public sealed class ReactionConfigurationTests
    {
        [Test]
        public void SerializedProfileSurvivesAssetSaveAndReimport()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/PostureReactionSerializationTest.asset");
            var profile = ScriptableObject.CreateInstance<EnemyProfile>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_reactionCategory\":2,\"_reaction\":{\"_overrideKnockdownResistance\":true,\"_knockdownResistance\":0}}", profile);
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var restored = AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
                Assert.AreEqual(ReactionCategory.MediumMob, restored.ResolveReaction(EnemyRank.Normal).Category);
                Assert.AreEqual(0f, restored.ResolveReaction(EnemyRank.Normal).KnockdownResistance);
                Assert.AreEqual(StanceBreakEffect.Knockdown, restored.ResolveReaction(EnemyRank.Normal).DefaultBreakEffect);
                Assert.IsEmpty(restored.ValidateReactionData());
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (profile) Object.DestroyImmediate(profile);
            }
        }

        // Feature: posture-enemy-interaction, Property 3: deterministic category and real rank identity.
        // Validates: Requirements 3.1, 3.7, 3.8, 3.9, 8.2
        [Test]
        public void CategoryFallbackAndBossProtectionAreDeterministic()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var rank = (EnemyRank)rng.Next(0, 4);
                var declared = (ReactionCategory)rng.Next(-2, 8);
                var a = new ReactionConfiguration(rank, declared);
                var b = new ReactionConfiguration(rank, declared);
                Assert.AreEqual(a.Category, b.Category);
                Assert.AreEqual(rank, a.Rank);
                Assert.AreEqual(rank == EnemyRank.Boss || a.Category == ReactionCategory.Boss, a.IsBoss);
                var window = new VulnerabilityWindow();
                window.SetBaseRank(rank); window.Open(0f, 1f);
                Assert.AreEqual(rank, a.Rank, "Window never changes physical identity.");
                if (declared < ReactionCategory.SmallMob || declared > ReactionCategory.Boss)
                    Assert.AreEqual(rank == EnemyRank.Normal ? ReactionCategory.SmallMob :
                        rank == EnemyRank.Boss ? ReactionCategory.Boss : ReactionCategory.Elite, a.Category);
            });
            Assert.AreEqual(0, (int)StanceBreakEffect.None);
            Assert.AreEqual(1, (int)StanceBreakEffect.Stun);
            Assert.AreEqual(2, (int)StanceBreakEffect.KnockUp);
            Assert.AreEqual(3, (int)StanceBreakEffect.Knockback);
            Assert.AreEqual(4, (int)StanceBreakEffect.Knockdown);
        }

        [Test]
        public void LegacyProfileAndExplicitZeroOverrideRoundTrip()
        {
            var profile = ScriptableObject.CreateInstance<EnemyProfile>();
            try
            {
                var before = JsonUtility.ToJson(profile);
                Assert.AreEqual(ReactionCategory.SmallMob, profile.ResolveReaction(EnemyRank.Normal).Category);
                Assert.AreEqual(ReactionCategory.Elite, profile.ResolveReaction(EnemyRank.Legendary).Category);
                JsonUtility.FromJsonOverwrite("{\"_reactionCategory\":2,\"_reaction\":{\"_overrideKnockdownResistance\":true,\"_knockdownResistance\":0}}", profile);
                var saved = JsonUtility.ToJson(profile);
                var clone = ScriptableObject.CreateInstance<EnemyProfile>();
                try
                {
                    JsonUtility.FromJsonOverwrite(saved, clone);
                    Assert.AreEqual(ReactionCategory.MediumMob, clone.ResolveReaction(EnemyRank.Normal).Category);
                    Assert.AreEqual(0f, clone.ResolveReaction(EnemyRank.Normal).KnockdownResistance);
                    Assert.AreEqual(StanceBreakEffect.Knockdown, clone.ResolveReaction(EnemyRank.Normal).DefaultBreakEffect);
                    Assert.AreNotEqual(before, saved);
                }
                finally { Object.DestroyImmediate(clone); }
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void InvalidRawDataIsReportedWithoutMutatingAuthoring()
        {
            var profile = ScriptableObject.CreateInstance<EnemyProfile>();
            try
            {
                var field = typeof(EnemyProfile).GetField("_stanceDamageMultiplier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                foreach (var value in new[] { float.NaN, float.PositiveInfinity, -1f, 6f })
                {
                    // Construct invalid raw input without invoking Unity's authoring callback.
                    field.SetValue(profile, value);
                    string raw = JsonUtility.ToJson(profile);
                    Assert.IsNotEmpty(profile.ValidateReactionData());
                    Assert.AreEqual(1f, profile.StanceDamageMultiplier);
                    Assert.AreEqual(raw, JsonUtility.ToJson(profile));
                }
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void ExtremeFiniteLaunchIsDiagnosedAndUsesBoundedFallback()
        {
            var data = new ReactionAuthoring();
            typeof(ReactionAuthoring).GetField("_defaultLaunchHeight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(data, float.MaxValue);
            var errors = new System.Collections.Generic.List<string>(); data.Validate(errors);
            Assert.That(errors, Has.Some.Contains("_defaultLaunchHeight"));
            var config = new ReactionConfiguration(EnemyRank.Normal, authoring:data);
            Assert.AreEqual(2f, config.LaunchHeight); Assert.LessOrEqual(config.LaunchHeight, DisplacementTier.KnockUpMax);
        }

        [Test]
        public void AuthoringRejectsInvalidEffectsAndExcessiveTotalDuration()
        {
            var authoring = new ReactionAuthoring();
            JsonUtility.FromJsonOverwrite("{\"_overrideDefaultBreakEffect\":true,\"_defaultBreakEffect\":3,\"_knockdownFallSeconds\":4,\"_knockdownGroundSeconds\":4,\"_knockdownRecoverSeconds\":4}", authoring);
            var errors = new System.Collections.Generic.List<string>();
            authoring.Validate(errors);
            Assert.That(errors, Has.Some.Contains("total duration"));
            Assert.That(errors, Has.Some.Contains("Knockback"));
            var resolved = new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob, authoring);
            Assert.AreEqual(StanceBreakEffect.Knockdown, resolved.DefaultBreakEffect);
            Assert.LessOrEqual(resolved.FallSeconds + resolved.GroundSeconds + resolved.RecoverSeconds, 10f);
        }
    }
}
