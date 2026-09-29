using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the missing-configuration fallback per rarity — task 4.7 of
    /// weapon-gameplay-swarm-rework (Requisito 3.7).
    ///
    /// R3.7: when an enemy has no <see cref="EnemyProfile"/> to source concrete stance/resistance
    /// numbers from, the reaction path must (a) apply the data-driven per-<see cref="EnemyRank"/>
    /// defaults and (b) log a stable missing-config indicator, all without interrupting the hit
    /// processing.
    ///
    /// The fallback is split into two pieces:
    ///  - the pure, scene-free <see cref="RankReactionDefaults"/> table (<c>For(rank)</c> returns the
    ///    expected per-rarity values, <c>MissingConfigId(rank)</c> is a stable/deterministic id), and
    ///  - <c>EnemyVariant.ApplyStanceProfile</c>, which — when no profile is assigned — logs a warning
    ///    carrying <see cref="RankReactionDefaults.MissingConfigId"/> and then calls
    ///    <c>CombatReactionController.ConfigureRank(rank)</c> (which reads from the same table),
    ///    returning normally so processing continues.
    ///
    /// <c>EnemyVariant</c> is a <see cref="MonoBehaviour"/> that needs a live scene (Actor + EnemyAI +
    /// NavMeshAgent + a child CombatReactionController) to exercise, so per the design these example
    /// tests target the pure table + the deterministic missing-config identifier that the fallback
    /// relies on. They pin, by explicit example, that every rank yields a fully-configured set of
    /// defaults inside its valid ranges (so applying them can never interrupt processing) and that the
    /// logged identifier is stable and rank-specific.
    /// </summary>
    public sealed class MissingProfileRankDefaultsFallbackExampleTests
    {
        // ---- R3.7: per-EnemyRank defaults are applied (the pure table the fallback reads from) ----

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — Normal defaults match the table.
        // Validates: Requirement 3.7
        [Test]
        public void For_Normal_AppliesExpectedPerRankDefaults()
        {
            RankReactionDefaults.Defaults d = RankReactionDefaults.For(EnemyRank.Normal);

            Assert.AreEqual(EnemyRank.Normal, d.Rank);
            Assert.AreEqual(100f, d.MaxStance, 1e-4f);
            Assert.AreEqual(1f, d.StanceDamageMultiplier, 1e-4f);
            Assert.AreEqual(25f, d.StanceRecoveryPerSecond, 1e-4f);
            Assert.AreEqual(2.5f, d.StanceRecoveryDelay, 1e-4f);
            Assert.AreEqual(0f, d.StaggerResistance, 1e-4f);
            Assert.AreEqual(0f, d.StunResistance, 1e-4f);
            Assert.AreEqual(0f, d.KnockUpResistance, 1e-4f);
            Assert.AreEqual(0f, d.KnockbackResistance, 1e-4f);
            Assert.AreEqual(4f, d.VulnerabilityWindowSeconds, 1e-4f);
            Assert.AreEqual(1.5f, d.BreakImmunitySeconds, 1e-4f);
        }

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — Elite defaults match the table.
        // Validates: Requirement 3.7
        [Test]
        public void For_Elite_AppliesExpectedPerRankDefaults()
        {
            RankReactionDefaults.Defaults d = RankReactionDefaults.For(EnemyRank.Elite);

            Assert.AreEqual(EnemyRank.Elite, d.Rank);
            Assert.AreEqual(220f, d.MaxStance, 1e-4f);
            Assert.AreEqual(0.8f, d.StanceDamageMultiplier, 1e-4f);
            Assert.AreEqual(25f, d.StanceRecoveryPerSecond, 1e-4f);
            Assert.AreEqual(2.5f, d.StanceRecoveryDelay, 1e-4f);
            Assert.AreEqual(0.3f, d.StaggerResistance, 1e-4f);
            Assert.AreEqual(0.25f, d.StunResistance, 1e-4f);
            Assert.AreEqual(0.4f, d.KnockUpResistance, 1e-4f);
            Assert.AreEqual(0.3f, d.KnockbackResistance, 1e-4f);
            Assert.AreEqual(4f, d.VulnerabilityWindowSeconds, 1e-4f);
            Assert.AreEqual(2f, d.BreakImmunitySeconds, 1e-4f);
        }

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — Legendary defaults match the table.
        // Validates: Requirement 3.7
        [Test]
        public void For_Legendary_AppliesExpectedPerRankDefaults()
        {
            RankReactionDefaults.Defaults d = RankReactionDefaults.For(EnemyRank.Legendary);

            Assert.AreEqual(EnemyRank.Legendary, d.Rank);
            Assert.AreEqual(400f, d.MaxStance, 1e-4f);
            Assert.AreEqual(0.6f, d.StanceDamageMultiplier, 1e-4f);
            Assert.AreEqual(25f, d.StanceRecoveryPerSecond, 1e-4f);
            Assert.AreEqual(2.5f, d.StanceRecoveryDelay, 1e-4f);
            Assert.AreEqual(0.6f, d.StaggerResistance, 1e-4f);
            Assert.AreEqual(0.5f, d.StunResistance, 1e-4f);
            Assert.AreEqual(0.75f, d.KnockUpResistance, 1e-4f);
            Assert.AreEqual(0.6f, d.KnockbackResistance, 1e-4f);
            Assert.AreEqual(3.5f, d.VulnerabilityWindowSeconds, 1e-4f);
            Assert.AreEqual(3f, d.BreakImmunitySeconds, 1e-4f);
        }

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — Boss defaults match the table.
        // Validates: Requirement 3.7
        [Test]
        public void For_Boss_AppliesExpectedPerRankDefaults()
        {
            RankReactionDefaults.Defaults d = RankReactionDefaults.For(EnemyRank.Boss);

            Assert.AreEqual(EnemyRank.Boss, d.Rank);
            Assert.AreEqual(900f, d.MaxStance, 1e-4f);
            Assert.AreEqual(0.5f, d.StanceDamageMultiplier, 1e-4f);
            Assert.AreEqual(25f, d.StanceRecoveryPerSecond, 1e-4f);
            Assert.AreEqual(2.5f, d.StanceRecoveryDelay, 1e-4f);
            Assert.AreEqual(0.85f, d.StaggerResistance, 1e-4f);
            Assert.AreEqual(0.7f, d.StunResistance, 1e-4f);
            Assert.AreEqual(1f, d.KnockUpResistance, 1e-4f);
            Assert.AreEqual(1f, d.KnockbackResistance, 1e-4f);
            Assert.AreEqual(3f, d.VulnerabilityWindowSeconds, 1e-4f);
            Assert.AreEqual(4f, d.BreakImmunitySeconds, 1e-4f);
        }

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — every rank yields in-range defaults.
        // The fallback applies these numbers directly without re-validating, so a fully-configured,
        // in-range default for every rank is exactly what lets the fallback "not interrupt the hit":
        // ConfigureRank always receives a valid stance pool (>= 1), a multiplier in [0, 5], recovery
        // delay in [0, 10] s, resistances in [0, 1], a strictly-positive vulnerability window (R3.3)
        // and a break-immunity window in [0.1, 10] s (R2.6).
        // Validates: Requirement 3.7
        [Test]
        public void For_EveryRank_ProducesFullyConfiguredInRangeDefaults()
        {
            foreach (EnemyRank rank in System.Enum.GetValues(typeof(EnemyRank)))
            {
                RankReactionDefaults.Defaults d = RankReactionDefaults.For(rank);

                Assert.AreEqual(rank, d.Rank, $"defaults for {rank} must be tagged with the requested rank");
                Assert.GreaterOrEqual(d.MaxStance, 1f, $"{rank}: stance pool must be at least 1");

                Assert.GreaterOrEqual(d.StanceDamageMultiplier, 0f, $"{rank}: stance-damage multiplier must be >= 0");
                Assert.LessOrEqual(d.StanceDamageMultiplier, 5f, $"{rank}: stance-damage multiplier must be <= 5");

                Assert.GreaterOrEqual(d.StanceRecoveryPerSecond, 0f, $"{rank}: recovery-per-second must be >= 0");

                Assert.GreaterOrEqual(d.StanceRecoveryDelay, 0f, $"{rank}: recovery delay must be >= 0");
                Assert.LessOrEqual(d.StanceRecoveryDelay, 10f, $"{rank}: recovery delay must be <= 10 s");

                AssertResistanceInRange(d.StaggerResistance, rank, nameof(d.StaggerResistance));
                AssertResistanceInRange(d.StunResistance, rank, nameof(d.StunResistance));
                AssertResistanceInRange(d.KnockUpResistance, rank, nameof(d.KnockUpResistance));
                AssertResistanceInRange(d.KnockbackResistance, rank, nameof(d.KnockbackResistance));

                Assert.Greater(d.VulnerabilityWindowSeconds, 0f, $"{rank}: vulnerability window must be > 0 (R3.3)");

                Assert.GreaterOrEqual(d.BreakImmunitySeconds, 0.1f, $"{rank}: break immunity must be >= 0.1 s (R2.6)");
                Assert.LessOrEqual(d.BreakImmunitySeconds, 10f, $"{rank}: break immunity must be <= 10 s (R2.6)");
            }
        }

        // ---- R3.7: the absence is logged with a stable identifier ----

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — missing-config id is rank-specific.
        // The fallback logs RankReactionDefaults.MissingConfigId(rank); this pins the exact stable
        // identifier per rank so the logged indication is traceable and does not depend on scene or
        // instance names.
        // Validates: Requirement 3.7
        [Test]
        public void MissingConfigId_IsStableAndRankSpecific()
        {
            Assert.AreEqual("RankReactionDefaults.MissingProfile.Normal", RankReactionDefaults.MissingConfigId(EnemyRank.Normal));
            Assert.AreEqual("RankReactionDefaults.MissingProfile.Elite", RankReactionDefaults.MissingConfigId(EnemyRank.Elite));
            Assert.AreEqual("RankReactionDefaults.MissingProfile.Legendary", RankReactionDefaults.MissingConfigId(EnemyRank.Legendary));
            Assert.AreEqual("RankReactionDefaults.MissingProfile.Boss", RankReactionDefaults.MissingConfigId(EnemyRank.Boss));
        }

        // Feature: weapon-gameplay-swarm-rework, R3.7 example — the id is deterministic across calls.
        // Same rank always produces the same identifier (no time/instance/scene dependency), so the
        // same missing configuration is logged identically across runs and distinct ranks never
        // collide.
        // Validates: Requirement 3.7
        [Test]
        public void MissingConfigId_IsDeterministicAndDistinctPerRank()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (EnemyRank rank in System.Enum.GetValues(typeof(EnemyRank)))
            {
                string first = RankReactionDefaults.MissingConfigId(rank);
                string second = RankReactionDefaults.MissingConfigId(rank);

                Assert.AreEqual(first, second, $"MissingConfigId({rank}) must be deterministic across calls");
                Assert.IsNotEmpty(first, $"MissingConfigId({rank}) must not be empty");
                Assert.IsTrue(seen.Add(first), $"MissingConfigId({rank}) must be unique across ranks (got duplicate '{first}')");
            }
        }

        private static void AssertResistanceInRange(float value, EnemyRank rank, string name)
        {
            Assert.GreaterOrEqual(value, 0f, $"{rank}: {name} must be >= 0");
            Assert.LessOrEqual(value, 1f, $"{rank}: {name} must be <= 1");
        }
    }
}
