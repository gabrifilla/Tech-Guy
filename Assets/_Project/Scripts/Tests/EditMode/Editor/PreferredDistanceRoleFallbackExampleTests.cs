using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the preferred-distance <see cref="CombatRole"/> fallback — task 7.3
    /// of weapon-gameplay-swarm-rework (Requisito 6.4).
    ///
    /// R6.4: when an enemy's archetype has no declared <c>Preferred_Distance</c> entry, the enemy must
    /// still tend to a sensible distance by falling back to its <see cref="CombatRole"/> default
    /// (always &gt; 0), and the absence must be logged so the missing configuration is traceable —
    /// without interrupting behavior.
    ///
    /// The fallback splits into two pieces:
    ///  - the pure, scene-free <see cref="PreferredDistanceResolver"/> that decides the value: with a
    ///    null/absent profile entry, <c>Resolve</c> returns a <c>CombatRole</c> default with
    ///    <see cref="PreferredDistanceResolver.Resolution.FromProfile"/> == false, and
    ///    <c>RoleDefaultDistance</c> is always &gt; 0 for every role/band; and
    ///  - <c>PreferredDistanceLayer</c> (a <see cref="UnityEngine.MonoBehaviour"/> over
    ///    <c>EnemyAI</c>/<c>NavMeshAgent</c>) which logs the missing configuration exactly once,
    ///    gated precisely on <c>!resolution.FromProfile</c>.
    ///
    /// The layer needs a live scene (agent + player + variant) to exercise, so per the design these
    /// example tests target the pure resolver where the R6.4 decision actually lives: they pin that a
    /// missing entry yields <c>FromProfile == false</c> (the exact flag that drives the log) with a
    /// <c>CombatRole</c> default &gt; 0 for every role — including when no profile asset exists at all
    /// and when a profile exists but has no entry for the queried archetype.
    /// </summary>
    public sealed class PreferredDistanceRoleFallbackExampleTests
    {
        private static readonly CombatRole[] Roles = (CombatRole[])System.Enum.GetValues(typeof(CombatRole));
        private static readonly ArchetypeId[] Archetypes = (ArchetypeId[])System.Enum.GetValues(typeof(ArchetypeId));

        // Feature: weapon-gameplay-swarm-rework, R6.4 example — with a null profile, every CombatRole
        // resolves to a fallback distance > 0 flagged FromProfile == false (the flag that drives the
        // "missing configuration" log in PreferredDistanceLayer), using the fixed per-role constants
        // (engagement band = 0).
        // Validates: Requirement 6.4
        [Test]
        public void NullProfile_ForEveryRole_FallsBackToPositiveDistanceNotFromProfile()
        {
            foreach (CombatRole role in Roles)
            {
                PreferredDistanceResolver.Resolution resolution =
                    PreferredDistanceResolver.Resolve(profile: null, archetype: null, role, engagementBand: 0f);

                Assert.IsFalse(
                    resolution.FromProfile,
                    $"{role}: a null profile must resolve via the CombatRole fallback (FromProfile == false), " +
                    "which is exactly the flag that makes PreferredDistanceLayer log the missing configuration (R6.4)");
                Assert.Greater(
                    resolution.PreferredDistance, 0f,
                    $"{role}: the CombatRole fallback distance must be > 0 (R6.4)");
                Assert.GreaterOrEqual(
                    resolution.PreferredDistance, PreferredDistanceResolver.MinPreferredDistance - 1e-6f,
                    $"{role}: the fallback distance must never fall below the floor " +
                    $"{PreferredDistanceResolver.MinPreferredDistance}");
            }
        }

        // Feature: weapon-gameplay-swarm-rework, R6.4 example — a profile that exists but declares no
        // entry for the queried archetype still falls back per CombatRole (FromProfile == false) with a
        // distance > 0. This is the "no Preferred_Distance declared for this archetype" case.
        // Validates: Requirement 6.4
        [Test]
        public void ProfileWithoutEntryForArchetype_ForEveryRole_FallsBackToPositiveDistanceNotFromProfile()
        {
            // An empty profile: it exists, but has no entry for any archetype, so every lookup misses.
            var profile = UnityEngine.ScriptableObject.CreateInstance<PreferredDistanceProfile>();
            try
            {
                var field = typeof(PreferredDistanceProfile).GetField(
                    "_entries",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field.SetValue(profile, new PreferredDistanceProfile.Entry[0]);

                foreach (CombatRole role in Roles)
                {
                    foreach (ArchetypeId archetype in Archetypes)
                    {
                        PreferredDistanceResolver.Resolution resolution =
                            PreferredDistanceResolver.Resolve(profile, archetype, role, engagementBand: 0f);

                        Assert.IsFalse(
                            resolution.FromProfile,
                            $"role={role} archetype={archetype}: a profile with no matching entry must fall back " +
                            "per CombatRole (FromProfile == false) so the absence is logged (R6.4)");
                        Assert.Greater(
                            resolution.PreferredDistance, 0f,
                            $"role={role} archetype={archetype}: the CombatRole fallback distance must be > 0 (R6.4)");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        // Feature: weapon-gameplay-swarm-rework, R6.4 example — RoleDefaultDistance is strictly > 0 for
        // every CombatRole, both with the fixed per-role constant (band = 0) and when anchored to a live
        // engagement band. This is the value the fallback hands the layer, so it must always be positive.
        // Validates: Requirement 6.4
        [Test]
        public void RoleDefaultDistance_ForEveryRole_IsStrictlyPositive()
        {
            float[] bands = { 0f, 0.01f, 1f, 6f, 30f };

            foreach (CombatRole role in Roles)
            {
                foreach (float band in bands)
                {
                    float def = PreferredDistanceResolver.RoleDefaultDistance(role, band);

                    Assert.Greater(
                        def, 0f,
                        $"RoleDefaultDistance({role}, band={band}) = {def} must be > 0 (R6.4/R6.1)");
                    Assert.GreaterOrEqual(
                        def, PreferredDistanceResolver.MinPreferredDistance - 1e-6f,
                        $"RoleDefaultDistance({role}, band={band}) = {def} must never fall below the floor " +
                        $"{PreferredDistanceResolver.MinPreferredDistance}");
                }
            }
        }
    }
}
