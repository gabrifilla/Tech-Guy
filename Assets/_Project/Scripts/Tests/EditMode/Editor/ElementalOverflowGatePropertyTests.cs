using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 12 of impactful-weapon-boons (Elemental Overflow,
    /// <see cref="PlayerOnHitEffects"/> -> <see cref="RunSynergyEffects.ReportImpact"/>).
    ///
    /// Property 12 (design): for any Direct_Hit of damage <c>d &gt; 0</c> and rank <c>r &gt;= 1</c>, an
    /// Overflow burst of magnitude <c>0.5·r·d</c> SHALL trigger if and only if the struck enemy carries
    /// an active <see cref="BurnStatus"/> or <see cref="ChillStatus"/>; the underlying status SHALL remain
    /// afterward; and the burst SHALL count against the cascade budget (a single bounded impact that never
    /// exceeds <see cref="RunSynergyEffects"/>'s per-frame <c>MaxSecondaryHits</c>).
    ///
    /// Approach: the burst trigger <c>TryElementalOverflow</c> is private and invoked from the public
    /// <see cref="PlayerOnHitEffects.ApplyTo(Actor, float)"/> direct-hit path, so the test drives the real
    /// component exactly as gameplay does — enable overflow to rank r (one <c>EnableElementalOverflow()</c>
    /// per pick), attach a real status via its <c>Apply</c> method, then call <c>ApplyTo(enemy, d)</c> and
    /// measure the enemy's health delta. No burn/chill is enabled on the player, so <c>ApplyElements</c> is a
    /// no-op and the measured delta is exactly the Overflow burst.
    ///
    /// Two routing paths, matching the design:
    /// <list type="bullet">
    /// <item>No Resonance -> the burst falls back to a plain <see cref="Actor.TakeDamage"/>, so the dealt
    /// amount equals <c>0.5·r·d</c> exactly (used to assert the burst magnitude and the gate).</item>
    /// <item>Resonance active -> the burst routes through <c>ReportImpact</c>, which emits exactly one
    /// bounded secondary hit (<c>LastSecondaryHits == 1</c>), proving it counts against — and cannot exceed —
    /// the cascade budget (R8.3).</item>
    /// </list>
    ///
    /// Rig: an inactive host GameObject (no <c>Awake</c>/scene lifecycle, same pattern as
    /// <see cref="MomentumStackAndDamagePropertyTests"/>). Enemies are built on their own inactive hosts so
    /// <see cref="Actor.Awake"/> does not run (it would add feedback/coin components) while the status
    /// component is still present for the <c>GetComponent</c> gate.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property and reports the
    /// exact failing case as a counterexample.
    /// </summary>
    public sealed class ElementalOverflowGatePropertyTests
    {
        private const float Tolerance = 1e-2f;

        private enum StatusKind { None, Burn, Chill }

        // Builds an inactive host carrying a PlayerOnHitEffects. Inactive so no Awake/scene lifecycle runs.
        // withResonance > 0 attaches a RunSynergyEffects with that many Resonance ranks so the burst routes
        // through ReportImpact instead of the plain-TakeDamage fallback.
        private static PlayerRig BuildPlayer(int overflowRank, int resonanceRanks)
        {
            var host = new GameObject("OverflowPlayerHost");
            host.SetActive(false);

            PlayerOnHitEffects effects = host.AddComponent<PlayerOnHitEffects>();
            for (int i = 0; i < overflowRank; i++) effects.EnableElementalOverflow();

            RunSynergyEffects synergies = null;
            if (resonanceRanks > 0)
            {
                synergies = effects.Synergies; // lazily adds the component to the same host
                for (int i = 0; i < resonanceRanks; i++) synergies.Add(RunSynergy.Resonance);
            }

            return new PlayerRig(host, effects, synergies);
        }

        private readonly struct PlayerRig
        {
            public readonly GameObject Host;
            public readonly PlayerOnHitEffects Effects;
            public readonly RunSynergyEffects Synergies;

            public PlayerRig(GameObject host, PlayerOnHitEffects effects, RunSynergyEffects synergies)
            {
                Host = host; Effects = effects; Synergies = synergies;
            }

            public void Dispose() { if (Host) Object.DestroyImmediate(Host); }
        }

        // Builds an enemy Actor on its own inactive host (so Actor.Awake never runs) with plenty of health,
        // then optionally attaches an active BurnStatus/ChillStatus via the production Apply methods.
        private static EnemyRig BuildEnemy(StatusKind status)
        {
            var host = new GameObject("OverflowEnemyHost");
            host.SetActive(false);

            Actor enemy = host.AddComponent<Actor>();
            enemy.health = 100000f; // large so the burst never kills it and the delta stays linear

            switch (status)
            {
                case StatusKind.Burn: BurnStatus.Apply(enemy, 5f, 5f); break;
                case StatusKind.Chill: ChillStatus.Apply(enemy, 0.5f, 5f); break;
            }

            return new EnemyRig(host, enemy);
        }

        private readonly struct EnemyRig
        {
            public readonly GameObject Host;
            public readonly Actor Enemy;

            public EnemyRig(GameObject host, Actor enemy) { Host = host; Enemy = enemy; }

            public bool HasBurn => Enemy && Enemy.GetComponent<BurnStatus>();
            public bool HasChill => Enemy && Enemy.GetComponent<ChillStatus>();

            public void Dispose() { if (Host) Object.DestroyImmediate(Host); }
        }

        // Feature: impactful-weapon-boons, Property 12: Elemental Overflow gate.
        // For any rank r in [1,3] and damage d > 0, ApplyTo(enemy, d) deals an additional 0.5*r*d burst iff
        // the enemy carries an active BurnStatus or ChillStatus (and nothing when it carries neither), and
        // the status component remains present afterward. Driven through the no-Resonance fallback so the
        // measured delta equals the raw burst exactly.
        // Validates: Requirements 8.1, 8.2, 8.4
        [Test]
        public void OverflowBurstTriggersIffStatusPresentAndPreservesStatus()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);                       // ElementalOverflow ranks 1..3
                float damage = 1f + (float)rng.NextDouble() * 49f; // d in [1, 50)
                StatusKind status = (StatusKind)rng.Next(0, 3);   // None, Burn, or Chill

                PlayerRig player = BuildPlayer(overflowRank: rank, resonanceRanks: 0);
                EnemyRig enemy = BuildEnemy(status);
                try
                {
                    bool hadStatus = enemy.HasBurn || enemy.HasChill;
                    float before = enemy.Enemy.health;
                    player.Effects.ApplyTo(enemy.Enemy, damage);
                    float dealt = before - enemy.Enemy.health;

                    if (hadStatus)
                    {
                        // R8.1/R8.2: burst of exactly 0.5*r*d (no burn/chill enabled on the player, so this
                        // delta is the Overflow burst alone, routed through the TakeDamage fallback).
                        float expected = 0.5f * rank * damage;
                        PropertyCheck.That(Mathf.Abs(dealt - expected) <= Tolerance * Mathf.Max(1f, expected),
                            $"status={status}, rank={rank}, d={damage}: burst {dealt} != 0.5*r*d={expected}");

                        // R8.2: the underlying status is instantaneous-damage only; it must remain present.
                        bool stillHasStatus = enemy.HasBurn || enemy.HasChill;
                        PropertyCheck.That(stillHasStatus,
                            $"status={status}, rank={rank}: the underlying status was removed by the burst");
                    }
                    else
                    {
                        // R8.4: no active status -> no burst at all.
                        PropertyCheck.That(Mathf.Abs(dealt) <= Tolerance,
                            $"no-status case dealt {dealt}, expected 0 (rank={rank}, d={damage})");
                    }
                }
                finally { player.Dispose(); enemy.Dispose(); }
            });
        }

        // Feature: impactful-weapon-boons, Property 12: the burst counts against the cascade budget.
        // With Resonance active the burst routes through ReportImpact, which emits exactly one bounded
        // secondary hit (LastSecondaryHits == 1), so it counts against — and can never exceed —
        // RunSynergyEffects' per-frame MaxSecondaryHits budget.
        // Validates: Requirements 8.3
        [Test]
        public void OverflowBurstRoutesThroughBoundedImpactWhenResonanceActive()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);                       // ElementalOverflow ranks 1..3
                int resonance = rng.Next(1, 4);                  // Resonance ranks 1..3 (active)
                float damage = 1f + (float)rng.NextDouble() * 49f; // d in [1, 50)
                // Always give an active status here so the gate opens and the burst is emitted.
                StatusKind status = rng.Next(0, 2) == 0 ? StatusKind.Burn : StatusKind.Chill;

                PlayerRig player = BuildPlayer(overflowRank: rank, resonanceRanks: resonance);
                EnemyRig enemy = BuildEnemy(status);
                try
                {
                    player.Effects.ApplyTo(enemy.Enemy, damage);

                    // R8.3: the Overflow burst is a single bounded impact under the cascade budget. ApplyTo
                    // does not start a Resolve cascade here (no cascade modifiers are configured), so the
                    // only reported impact is the Overflow burst itself.
                    int hits = player.Synergies.LastSecondaryHits;
                    PropertyCheck.That(hits == 1,
                        $"rank={rank}, resonance={resonance}: expected exactly one bounded impact from the " +
                        $"Overflow burst, saw {hits}");
                    PropertyCheck.That(hits <= 32,
                        $"rank={rank}, resonance={resonance}: bounded impacts {hits} exceeded MaxSecondaryHits (32)");

                    // R8.2: status still present after routing through ReportImpact.
                    PropertyCheck.That(enemy.HasBurn || enemy.HasChill,
                        $"rank={rank}, resonance={resonance}: status removed after the bounded burst");
                }
                finally { player.Dispose(); enemy.Dispose(); }
            });
        }

        // Example: with the boon disabled (rank 0), an active status produces no burst — guards against the
        // gate firing when Elemental Overflow was never picked (R8.1 negative).
        [Test]
        public void Overflow_RankZeroNeverBursts()
        {
            PlayerRig player = BuildPlayer(overflowRank: 0, resonanceRanks: 0);
            EnemyRig enemy = BuildEnemy(StatusKind.Burn);
            try
            {
                float before = enemy.Enemy.health;
                player.Effects.ApplyTo(enemy.Enemy, 20f);
                Assert.That(before - enemy.Enemy.health, Is.EqualTo(0f).Within(Tolerance),
                    "Rank 0 (boon not picked) must never trigger an Overflow burst.");
                Assert.That(enemy.HasBurn, Is.True, "The status must be untouched when no burst fires.");
            }
            finally { player.Dispose(); enemy.Dispose(); }
        }

        // Example: the burst scales linearly with rank at a fixed damage — anchors the 0.5*r*d magnitude the
        // property samples (R8.2). Rank 1/2/3 on the same hit deal 0.5d / 1.0d / 1.5d.
        [Test]
        public void Overflow_BurstScalesWithRank()
        {
            const float d = 40f;
            for (int rank = 1; rank <= 3; rank++)
            {
                PlayerRig player = BuildPlayer(overflowRank: rank, resonanceRanks: 0);
                EnemyRig enemy = BuildEnemy(StatusKind.Chill);
                try
                {
                    float before = enemy.Enemy.health;
                    player.Effects.ApplyTo(enemy.Enemy, d);
                    float dealt = before - enemy.Enemy.health;
                    Assert.That(dealt, Is.EqualTo(0.5f * rank * d).Within(Tolerance),
                        $"Rank {rank} burst must equal 0.5*r*d.");
                }
                finally { player.Dispose(); enemy.Dispose(); }
            }
        }
    }
}
