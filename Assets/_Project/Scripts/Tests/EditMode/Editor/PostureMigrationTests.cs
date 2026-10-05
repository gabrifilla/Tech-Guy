using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace TechGuy.Tests.EditMode
{
    public sealed class PostureMigrationTests
    {
        [Test] public void MigrationIsExplicitIdempotentAndPreservesCustomCategoryAndStats()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            var profile = ScriptableObject.CreateInstance<EnemyProfile>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_maxStance\":220,\"_healthMultiplier\":2.2,\"_movementMultiplier\":0.6,\"_reactionCategory\":3}", profile);
                Assert.IsTrue(PostureReactionMigration.MigrateWeapon(weapon));
                Assert.IsTrue(weapon.OverridesBasicReaction); Assert.AreEqual(HitReactionType.Stagger, weapon.BasicReaction);
                Assert.AreEqual(0.35f, weapon.BasicInterruptContribution(0)); Assert.AreEqual(0.6f, weapon.BasicInterruptContribution(2));
                string authored = JsonUtility.ToJson(weapon);
                Assert.IsFalse(PostureReactionMigration.MigrateWeapon(weapon)); Assert.AreEqual(authored, JsonUtility.ToJson(weapon));
                Assert.IsTrue(PostureReactionMigration.MigrateProfile(profile, ReactionCategory.MediumMob));
                Assert.AreEqual(ReactionCategory.Elite, profile.ReactionCategory);
                Assert.AreEqual(220f, profile.MaxStance); Assert.AreEqual(2.2f, profile.HealthMultiplier); Assert.AreEqual(0.6f, profile.MovementMultiplier);
                Assert.IsFalse(PostureReactionMigration.MigrateProfile(profile, ReactionCategory.MediumMob));
            }
            finally { Object.DestroyImmediate(weapon); Object.DestroyImmediate(profile); }
        }
    }
}
