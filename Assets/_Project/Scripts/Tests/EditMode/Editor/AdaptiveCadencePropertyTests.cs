using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 15 of gauntlet-boon-playstyle-overhaul (the
    /// AdaptiveCadence / "Cadência adaptativa" boon, <see cref="AdaptiveCadenceTracker"/>).
    ///
    /// Property 15 (design): the cadence modifier is active <b>iff</b> the most recent basic hit landed
    /// at or beyond <see cref="AdaptiveCadenceTracker.KitingBand"/> (6m); a basic hit landed closer than
    /// the band removes it. While active, the installed modifier is a single
    /// <c>(AttackSpeedMultiplier, IncreasedPercent, 15 × rank)</c> on the player's stats (R9.1/R9.2).
    ///
    /// Approach: <see cref="AdaptiveCadenceTracker"/> is a thin MonoBehaviour whose logic is a trivial
    /// distance test driving a single run-scoped <see cref="PlayerStatModifier"/>. So the test drives the
    /// real component directly rather than a re-stated model (same rig pattern as
    /// <c>MomentumStackAndDamagePropertyTests</c>): it builds an inactive host (so no <c>Awake</c>/scene
    /// lifecycle runs), attaches a <see cref="PlayerActor"/> and the component, wires a real
    /// <see cref="HookBus"/>, and raises the genuine production trigger — <c>HookBus.RaiseBasicHit</c> —
    /// with a victim positioned at a generated distance from the player. The band decision therefore runs
    /// through the component's own <c>Vector3.Distance</c> test exactly as gameplay does.
    ///
    /// Reading the bonus (R9.1): the component installs a <c>PlayerStatModifier(AttackSpeedMultiplier,
    /// IncreasedPercent, 15*rank)</c>. The primary assertion sums the matching runtime modifiers — the
    /// exact contribution the component installed — and a corroborating assertion confirms it flows
    /// through the real <see cref="PlayerArpgStats.GetStat"/> pipeline: the base <c>attackSpeedMultiplier</c>
    /// is 1, so <c>GetStat</c> returns <c>1 * (1 + 15*rank/100)</c> while active and <c>1</c> otherwise.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 15
    public sealed class AdaptiveCadencePropertyTests
    {
        private const float Tolerance = 1e-3f;
        private const float PercentPerRank = 15f;              // R9.1: +15% AttackSpeedMultiplier per rank
        private const float Band = AdaptiveCadenceTracker.KitingBand; // 6m (R9.1/R9.2)

        // A throwaway ScriptableObject used only as the modifier's source tag, mirroring how RunBoons tags
        // the cadence modifier (passing `this`) so run-end RemoveModifiersFrom(source) would clear it.
        private sealed class CadenceStatSourceStub : ScriptableObject { }

        // Builds an inactive host carrying a PlayerActor + a configured AdaptiveCadenceTracker, wired to a
        // fresh HookBus, plus a separate victim Actor whose world position the test moves to vary distance.
        // Inactive hosts so no Awake/scene lifecycle runs. statSource stands in for the RunBoons source tag.
        private static Rig BuildRig(int rank)
        {
            var host = new GameObject("AdaptiveCadenceHost");
            host.SetActive(false);
            PlayerActor player = host.AddComponent<PlayerActor>();
            player.transform.position = Vector3.zero;

            var victimHost = new GameObject("AdaptiveCadenceVictim");
            victimHost.SetActive(false);
            Actor victim = victimHost.AddComponent<Actor>();

            var hooks = new HookBus();
            var statSource = ScriptableObject.CreateInstance<CadenceStatSourceStub>();

            AdaptiveCadenceTracker tracker = host.AddComponent<AdaptiveCadenceTracker>();
            tracker.Configure(player, hooks, rank, statSource);

            return new Rig(host, victimHost, player, victim, hooks, tracker, statSource);
        }

        private readonly struct Rig
        {
            public readonly GameObject Host;
            public readonly GameObject VictimHost;
            public readonly PlayerActor Player;
            public readonly Actor Victim;
            public readonly HookBus Hooks;
            public readonly AdaptiveCadenceTracker Tracker;
            public readonly Object StatSource;

            public Rig(GameObject host, GameObject victimHost, PlayerActor player, Actor victim,
                HookBus hooks, AdaptiveCadenceTracker tracker, Object statSource)
            {
                Host = host; VictimHost = victimHost; Player = player; Victim = victim;
                Hooks = hooks; Tracker = tracker; StatSource = statSource;
            }

            // Place the victim at the given distance from the player (along +X) and raise the basic-hit channel.
            public void BasicHitAt(float distance)
            {
                VictimHost.transform.position = Player.transform.position + Vector3.right * distance;
                Hooks.RaiseBasicHit(Victim, 10f);
            }

            public void Dispose()
            {
                if (Host) Object.DestroyImmediate(Host);
                if (VictimHost) Object.DestroyImmediate(VictimHost);
                if (StatSource) Object.DestroyImmediate(StatSource);
            }
        }

        // The exact cadence contribution the component installed: the sum of every runtime modifier that
        // matches (AttackSpeedMultiplier, IncreasedPercent). This is the raw (15*rank) value, or 0 if inactive.
        private static float InstalledBonus(PlayerActor player)
        {
            float sum = 0f;
            foreach (PlayerStatModifier m in player.Stats.RuntimeModifiers)
                if (m != null && m.statType == PlayerStatType.AttackSpeedMultiplier
                    && m.mode == PlayerStatModifierMode.IncreasedPercent)
                    sum += m.value;
            return sum;
        }

        // The same bonus recovered through the real GetStat pipeline. The base attackSpeedMultiplier is 1,
        // so GetStat == 1 * (1 + bonus/100)  =>  bonus == (GetStat - 1) * 100.
        private static float PipelineBonus(PlayerActor player)
        {
            float stat = player.Stats.GetStat(PlayerStatType.AttackSpeedMultiplier);
            return (stat - 1f) * 100f;
        }

        // Property 15: for any rank r in [1,3] and any sequence of basic hits at generated distances, the
        // cadence modifier is active iff the last hit landed at >= 6m. When active the installed bonus
        // (and the GetStat pipeline bonus) equals 15*r; when inactive both are 0.
        // Validates: Requirements 9.1, 9.2
        [Test]
        public void CadenceActiveIffLastBasicHitAtOrBeyondBand()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);              // AdaptiveCadence ranks 1..3
                int hits = rng.Next(1, 24);             // several band crossings in one sequence
                float expectedActiveBonus = PercentPerRank * rank;

                Rig rig = BuildRig(rank);
                try
                {
                    bool expectedActive = false;
                    for (int h = 0; h < hits; h++)
                    {
                        // Generated distances span clearly below, right around, and clearly above the band.
                        // Keep away from the exact 6.0 boundary (sampled explicitly by the example tests) so
                        // float distance never straddles it; the >= test itself is exercised by those examples.
                        float distance;
                        int bucket = rng.Next(0, 2);
                        if (bucket == 0) distance = (float)(rng.NextDouble() * (Band - 0.5)); // [0, 5.5): below
                        else distance = Band + 0.5f + (float)(rng.NextDouble() * 8.0);         // [6.5, 14.5): at/above

                        expectedActive = distance >= Band;
                        rig.BasicHitAt(distance);

                        PropertyCheck.That(rig.Tracker.IsActive == expectedActive,
                            $"rank={rank}, hit={h}, dist={distance:F3}: IsActive={rig.Tracker.IsActive}, expected={expectedActive}");

                        float expectedBonus = expectedActive ? expectedActiveBonus : 0f;
                        float installed = InstalledBonus(rig.Player);
                        PropertyCheck.That(Mathf.Abs(installed - expectedBonus) <= Tolerance,
                            $"rank={rank}, hit={h}, dist={distance:F3}: installed bonus {installed} != {expectedBonus}");
                        float pipeline = PipelineBonus(rig.Player);
                        PropertyCheck.That(Mathf.Abs(pipeline - expectedBonus) <= Tolerance,
                            $"rank={rank}, hit={h}, dist={distance:F3}: GetStat bonus {pipeline} != {expectedBonus}");
                    }
                }
                finally { rig.Dispose(); }
            });
        }

        // Example: a far basic hit (>= 6m) activates the cadence; a subsequent near hit (< 6m) removes it.
        // Anchors the band toggle both directions across ranks (R9.1 on, R9.2 off).
        [Test]
        public void FarHitActivates_NearHitRemoves_AcrossRanks()
        {
            for (int rank = 1; rank <= 3; rank++)
            {
                Rig rig = BuildRig(rank);
                try
                {
                    rig.BasicHitAt(8f); // >= 6m: activates
                    Assert.That(rig.Tracker.IsActive, Is.True, "A basic hit at 8m must activate the cadence.");
                    Assert.That(InstalledBonus(rig.Player), Is.EqualTo(PercentPerRank * rank).Within(Tolerance),
                        "Active bonus must equal 15*rank.");

                    rig.BasicHitAt(3f); // < 6m: removes
                    Assert.That(rig.Tracker.IsActive, Is.False, "A basic hit at 3m must remove the cadence.");
                    Assert.That(InstalledBonus(rig.Player), Is.EqualTo(0f).Within(Tolerance),
                        "The bonus must be zero after a near hit.");
                }
                finally { rig.Dispose(); }
            }
        }

        // Example: exactly 6m is in band (>= boundary), so a hit at the band edge activates the cadence.
        [Test]
        public void HitExactlyAtBand_Activates()
        {
            Rig rig = BuildRig(rank: 2);
            try
            {
                rig.BasicHitAt(Band); // exactly 6m: >= band, active
                Assert.That(rig.Tracker.IsActive, Is.True, "A hit at exactly 6m is in band and activates the cadence.");
                Assert.That(InstalledBonus(rig.Player), Is.EqualTo(PercentPerRank * 2).Within(Tolerance),
                    "Active bonus at the band edge must equal 15*rank.");
            }
            finally { rig.Dispose(); }
        }

        // Example: staying in band across repeated far hits keeps a single modifier (no stacking), and the
        // toggle only re-adds when it had been removed.
        [Test]
        public void RepeatedFarHits_KeepSingleModifier_NoStacking()
        {
            Rig rig = BuildRig(rank: 3);
            try
            {
                rig.BasicHitAt(9f);
                rig.BasicHitAt(10f);
                rig.BasicHitAt(7f);

                Assert.That(rig.Tracker.IsActive, Is.True, "Consecutive far hits keep the cadence active.");
                Assert.That(InstalledBonus(rig.Player), Is.EqualTo(PercentPerRank * 3).Within(Tolerance),
                    "The cadence must install a single modifier (15*rank), never stack per hit.");
            }
            finally { rig.Dispose(); }
        }

        // Example: at rank 0 the boon is inert — no modifier is installed even on a far hit.
        [Test]
        public void RankZeroIsInert()
        {
            Rig rig = BuildRig(rank: 0);
            try
            {
                rig.BasicHitAt(12f);

                Assert.That(rig.Tracker.IsActive, Is.False, "Rank 0 must never activate the cadence.");
                Assert.That(InstalledBonus(rig.Player), Is.EqualTo(0f).Within(Tolerance),
                    "Rank 0 must install no cadence bonus.");
            }
            finally { rig.Dispose(); }
        }
    }
}
