// Feature: combat-foundation-rework
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// Smoke / example EditMode tests for task 12.4 of combat-foundation-rework. They validate the
    /// authored Manopla profiles (task 12.3) end-to-end through the public <c>CombatActionProfile</c>
    /// surface, plus the two cross-cutting defaults the task calls out: the "absent category =>
    /// Committed + warning" path (R3.10) and the Buffer_de_Input default/disabled behavior (R5.1).
    ///
    /// <para>
    /// The authored-profile checks load the real assets via <see cref="AssetDatabase"/> (this is an
    /// Editor-only test assembly, so <c>AssetDatabase</c> is always available), reading each
    /// profile through <c>WeaponScript.BasicProfile</c> / <c>Ability.CombatProfile</c>. This
    /// deliberately validates the authored data from task 12.3 rather than re-constructing profiles
    /// in memory. The remaining concerns (default commitment, input buffer) use plain in-memory
    /// construction.
    /// </para>
    ///
    /// <para>
    /// These are ordinary NUnit <c>[Test]</c> methods (one concern each), not 100-case property
    /// tests — task 12.4 is an explicit smoke/example gate. No GUIDs or <c>.meta</c> files are
    /// touched; the test only reads assets.
    /// </para>
    /// </summary>
    /// <remarks>Feature: combat-foundation-rework, task 12.4. Requirements: R2.8, R3.10, R5.1.</remarks>
    public sealed class CombatActionProfileSmokeTests
    {
        private const string GauntletWeaponPath =
            "Assets/_Project/Resources/Weapons/Melee/Gauntlet/Gauntlet.asset";

        private const string AdvancePath =
            "Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerAdvance.asset";
        private const string FlurryPath =
            "Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerFlurry.asset";
        private const string ShockPath =
            "Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerShock.asset";
        private const string AsuraPath =
            "Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerAsura.asset";
        private const string DashPath =
            "Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset";

        private static CombatActionProfile LoadWeaponBasicProfile(string assetPath)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(assetPath);
            Assert.That(weapon, Is.Not.Null, $"Expected to load WeaponScript at '{assetPath}'.");
            return weapon.BasicProfile;
        }

        private static CombatActionProfile LoadAbilityProfile(string assetPath)
        {
            var ability = AssetDatabase.LoadAssetAtPath<Ability>(assetPath);
            Assert.That(ability, Is.Not.Null, $"Expected to load Ability at '{assetPath}'.");
            return ability.CombatProfile;
        }

        /// <summary>
        /// Every authored Manopla profile is wired (non-null), and its <see cref="ActionTimeline"/>
        /// satisfies the phase invariant <c>0 &lt;= StartupEnd &lt;= ActiveEnd &lt;= 1</c> (R2.8).
        /// </summary>
        [Test]
        public void AuthoredManoplaProfiles_BuildValidTimelines()
        {
            var profiles = new (string Name, CombatActionProfile Profile)[]
            {
                ("Gauntlet basic", LoadWeaponBasicProfile(GauntletWeaponPath)),
                ("BreakerAdvance (Q)", LoadAbilityProfile(AdvancePath)),
                ("BreakerFlurry (W)", LoadAbilityProfile(FlurryPath)),
                ("BreakerShock (E)", LoadAbilityProfile(ShockPath)),
                ("BreakerAsura (R)", LoadAbilityProfile(AsuraPath)),
                ("Dash", LoadAbilityProfile(DashPath)),
            };

            foreach ((string name, CombatActionProfile profile) in profiles)
            {
                Assert.That(profile, Is.Not.Null, $"{name}: authored CombatActionProfile is missing.");

                ActionTimeline timeline = profile.BuildTimeline();
                Assert.That(timeline.StartupEnd, Is.InRange(0f, 1f),
                    $"{name}: StartupEnd out of [0, 1].");
                Assert.That(timeline.ActiveEnd, Is.InRange(0f, 1f),
                    $"{name}: ActiveEnd out of [0, 1].");
                Assert.That(timeline.StartupEnd, Is.LessThanOrEqualTo(timeline.ActiveEnd),
                    $"{name}: StartupEnd must be <= ActiveEnd.");
            }
        }

        /// <summary>
        /// Every authored cancel rule on every Manopla profile passes <see cref="CancelRule.Validate"/>
        /// (i.e. the authored data is self-consistent, not just clamped at runtime) (R2.8, R4.8).
        /// </summary>
        [Test]
        public void AuthoredManoplaProfiles_AllCancelRulesValidate()
        {
            var profiles = new (string Name, CombatActionProfile Profile)[]
            {
                ("Gauntlet basic", LoadWeaponBasicProfile(GauntletWeaponPath)),
                ("BreakerAdvance (Q)", LoadAbilityProfile(AdvancePath)),
                ("BreakerFlurry (W)", LoadAbilityProfile(FlurryPath)),
                ("BreakerShock (E)", LoadAbilityProfile(ShockPath)),
                ("BreakerAsura (R)", LoadAbilityProfile(AsuraPath)),
                ("Dash", LoadAbilityProfile(DashPath)),
            };

            foreach ((string name, CombatActionProfile profile) in profiles)
            {
                Assert.That(profile, Is.Not.Null, $"{name}: authored CombatActionProfile is missing.");

                IReadOnlyList<CancelRuleData> rules = profile.CancelRules;
                for (int i = 0; i < rules.Count; i++)
                {
                    CancelRuleData rule = rules[i];
                    bool valid = CancelRule.Validate(rule.Start, rule.End, out string error);
                    Assert.That(valid, Is.True,
                        $"{name}: cancelRules[{i}] (target {rule.Target}) invalid: {error}");
                }

                // BuildCancelRuleSet must not throw and must round-trip the authored rules.
                Assert.That(profile.BuildCancelRuleSet(), Is.Not.Null,
                    $"{name}: BuildCancelRuleSet returned null.");
            }
        }

        /// <summary>
        /// The actions that are authored to be dash-cancelable (basic, Q, E, R) expose a Dash
        /// <see cref="CancelRule"/> in their <see cref="CancelRuleSet"/> (R2.8, R4.1, R4.2).
        /// </summary>
        [Test]
        public void DashCancelableManoplaProfiles_HaveDashRule()
        {
            var dashCancelable = new (string Name, CombatActionProfile Profile)[]
            {
                ("Gauntlet basic", LoadWeaponBasicProfile(GauntletWeaponPath)),
                ("BreakerAdvance (Q)", LoadAbilityProfile(AdvancePath)),
                ("BreakerShock (E)", LoadAbilityProfile(ShockPath)),
                ("BreakerAsura (R)", LoadAbilityProfile(AsuraPath)),
            };

            foreach ((string name, CombatActionProfile profile) in dashCancelable)
            {
                CancelRuleSet set = profile.BuildCancelRuleSet();
                Assert.That(set.HasRule(CancelTarget.Dash), Is.True,
                    $"{name}: expected a Dash CancelRule but none was authored.");
            }
        }

        /// <summary>
        /// Asura (R) is a Channel action whose Dash CancelRule must cover the Active phase, so the
        /// Rajada Asura remains dash-cancelable throughout its sustained Active window (R3.8/R3.9).
        /// Verified by sampling a mid-Active progress and asserting the Dash rule is open there.
        /// </summary>
        [Test]
        public void AsuraProfile_DashRuleCoversActivePhase()
        {
            CombatActionProfile asura = LoadAbilityProfile(AsuraPath);
            Assert.That(asura, Is.Not.Null, "Asura: authored CombatActionProfile is missing.");

            ActionTimeline timeline = asura.BuildTimeline();
            CancelRuleSet set = asura.BuildCancelRuleSet();

            Assert.That(set.TryGet(CancelTarget.Dash, out CancelRule dash), Is.True,
                "Asura: expected a Dash CancelRule.");

            // A representative mid-Active progress: halfway between StartupEnd and ActiveEnd.
            float midActive = (timeline.StartupEnd + timeline.ActiveEnd) * 0.5f;
            Assert.That(timeline.PhaseOf(midActive), Is.EqualTo(ActionPhase.Active),
                "Asura: sampled progress is expected to fall in the Active phase.");
            Assert.That(dash.IsOpenAt(midActive), Is.True,
                "Asura: Dash CancelRule must be open across the Active phase.");
        }

        /// <summary>
        /// R3.10 absent-category path: when no category is declared,
        /// <see cref="CommitmentRules.Resolve"/> returns <see cref="CommitmentCategory.Committed"/>
        /// and emits a warning. The <see cref="CombatActionProfile"/> field itself always serializes
        /// to a concrete value (default <see cref="CommitmentCategory.Committed"/>), so a fresh
        /// profile resolves to Committed without a warning — the real "absent" path is the nullable
        /// overload exercised here.
        /// </summary>
        [Test]
        public void CommitmentResolution_AbsentCategoryDefaultsToCommittedWithWarning()
        {
            // Field-level default on a freshly constructed (non-authored) profile.
            var fresh = new CombatActionProfile();
            CommitmentCategory resolvedField = fresh.ResolveCommitment(out bool fieldWarn);
            Assert.That(resolvedField, Is.EqualTo(CommitmentCategory.Committed),
                "A new CombatActionProfile should default to Committed.");
            Assert.That(fieldWarn, Is.False,
                "A concrete serialized default is present, so no absent-category warning is expected.");

            // The genuine absent-category path (R3.10): null declared => Committed + warning.
            CommitmentCategory resolvedAbsent = CommitmentRules.Resolve(null, out bool absentWarn);
            Assert.That(resolvedAbsent, Is.EqualTo(CommitmentCategory.Committed),
                "An absent category must resolve to Committed (R3.10).");
            Assert.That(absentWarn, Is.True,
                "An absent category must emit a warning (R3.10).");
        }

        /// <summary>
        /// R5.1: a default <see cref="InputBuffer"/> uses a 120 ms buffer window.
        /// </summary>
        [Test]
        public void InputBuffer_DefaultBufferDurationIs120Milliseconds()
        {
            var buffer = new InputBuffer();
            Assert.That(buffer.BufferDuration, Is.EqualTo(0.120f).Within(1e-6f),
                "Default BufferDuration should be 0.120 s (R5.1).");
        }

        /// <summary>
        /// R5.1: a <see cref="InputBuffer"/> built with a 0 window is disabled — a stored intent is
        /// never retained and can never be consumed.
        /// </summary>
        [Test]
        public void InputBuffer_ZeroDurationDisablesBuffering()
        {
            var buffer = new InputBuffer(0f);
            Assert.That(buffer.BufferDuration, Is.EqualTo(0f),
                "Explicit 0 BufferDuration should disable the buffer (R5.1).");

            var intent = new CommandIntent(CommandKind.Dash, null, Vector3.forward, 0f, 1L);
            buffer.Store(intent);

            Assert.That(buffer.HasPending, Is.False,
                "A disabled buffer must not retain a stored intent (R5.1).");
            Assert.That(buffer.TryConsume(0f, true, out _), Is.False,
                "A disabled buffer must never fire a buffered intent (R5.1).");
        }
    }
}
