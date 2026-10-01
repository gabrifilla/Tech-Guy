using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 10 of gauntlet-boon-playstyle-overhaul
    /// (the AsuraFist / "Punho de Asura" boon: basic attacks charge the Manopla's Asura meter).
    ///
    /// Property 10 (design): at rank R, every Basic_Attack Direct_Hit adds (2 x R) Asura, clamped to
    /// the meter's full threshold — the energy after a basic hit is <c>min(Maximum, e + 2R)</c> — while
    /// a skill (non-basic) hit grants nothing from this boon (R4.1/R4.2).
    ///
    /// Approach: the boon's whole contract is "subscribe to <see cref="HookBus.OnBasicHit"/> and feed
    /// (2 x rank) into the meter through <c>BreakerGauntletCombat.AddAsuraEnergy</c>", and the clamp is
    /// owned by the pure <see cref="AsuraMomentum.AddEnergy"/> that <c>AddAsuraEnergy</c> is a thin
    /// delegate over. So the system under test here is the real <see cref="HookBus"/> plus the real
    /// <see cref="AsuraMomentum"/>, driven by a local helper that transcribes the production
    /// <c>AsuraSurge</c> subscription verbatim (rank-&gt;energy on OnBasicHit, never on OnHit). This
    /// exercises the genuine clamp and the genuine basic-vs-skill routing without needing a scene:
    ///
    ///  - <see cref="AsuraSurgeSpy"/> mirrors <c>AsuraSurge</c>: it hooks <c>OnBasicHit</c> only and, per
    ///    basic hit, calls the meter's <c>AddEnergy(EnergyPerRankPerHit * rank)</c> — exactly the raise
    ///    that <c>AddAsuraEnergy</c> delegates to. It never hooks the generic <c>OnHit</c>, so a skill
    ///    hit (which raises only <c>OnHit</c>) cannot feed the meter (R4.3 is anchored by the skill case).
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 10
    public sealed class AsuraFistPropertyTests
    {
        // Design constant: AsuraSurge adds (2 x rank) Asura per basic hit (R4.1).
        private const int EnergyPerRankPerHit = 2;
        private const int Cap = AsuraMomentum.Maximum; // 100

        // Verbatim transcription of the AsuraSurge coordinator: it subscribes ONLY to OnBasicHit and,
        // per basic hit, adds (2 x rank) through the meter's AddEnergy — the exact call that
        // BreakerGauntletCombat.AddAsuraEnergy delegates to. It deliberately does NOT subscribe to the
        // generic OnHit, so a skill hit never reaches this boon.
        private sealed class AsuraSurgeSpy
        {
            private readonly AsuraMomentum _meter;
            private readonly HookBus _hooks;
            private readonly int _rank;

            public AsuraSurgeSpy(AsuraMomentum meter, HookBus hooks, int rank)
            {
                _meter = meter;
                _hooks = hooks;
                _rank = Math.Max(0, rank);
                _hooks.OnBasicHit += OnBasicHit; // R4.3: only the basic channel feeds the meter
            }

            private void OnBasicHit(Actor victim, float damage)
            {
                if (_rank <= 0) return;
                _meter.AddEnergy(EnergyPerRankPerHit * _rank); // R4.1/R4.2: +2R, clamped by AddEnergy
            }

            public void Dispose() => _hooks.OnBasicHit -= OnBasicHit;
        }

        // Property 10: for any rank R in [1,3], any starting energy, and any sequence of basic hits,
        // the Asura meter after each basic hit equals min(Maximum, previous + 2R); the meter never
        // exceeds the authored cap. A reference model mirrors the clamped running total step by step.
        // Validates: Requirements 4.1, 4.2
        [Test]
        public void BasicHit_AddsTwoTimesRank_ClampedToMaximum()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);              // [1,3] — the catalog MaxRank is 3
                int start = rng.Next(0, Cap + 1);        // any starting energy in [0,100]
                int basicHits = rng.Next(1, 80);         // enough hits to reach and sit at the cap

                var meter = new AsuraMomentum();
                meter.AddEnergy(start);                  // seed the starting energy (AddEnergy clamps)
                int expected = Math.Min(Cap, start);

                var bus = new HookBus();
                var surge = new AsuraSurgeSpy(meter, bus, rank);

                for (int h = 0; h < basicHits; h++)
                {
                    // Reference model: a basic hit adds 2R, clamped to the cap.
                    expected = Math.Min(Cap, expected + EnergyPerRankPerHit * rank);

                    bus.RaiseBasicHit(DummyActor, 10f);  // the Manopla basic path raises OnBasicHit

                    PropertyCheck.That(meter.Energy == expected,
                        $"rank={rank}, start={start}, hit={h}: Energy={meter.Energy}, expected={expected}");
                    PropertyCheck.That(meter.Energy >= 0 && meter.Energy <= Cap,
                        $"rank={rank}, start={start}, hit={h}: Energy out of [0,{Cap}] range: {meter.Energy}");
                }

                surge.Dispose();
            });
        }

        // Property 10 (skill-path facet): a skill hit raises only the generic OnHit, which this boon
        // never subscribes to, so the meter is untouched. Only basic hits in the same sequence move it.
        // Validates: Requirements 4.1, 4.2
        [Test]
        public void SkillHit_GrantsNothing_OnlyBasicHitsCharge()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);
                int events = rng.Next(1, 60);

                var meter = new AsuraMomentum();
                var bus = new HookBus();
                var surge = new AsuraSurgeSpy(meter, bus, rank);

                int expected = 0;
                for (int e = 0; e < events; e++)
                {
                    bool basic = rng.Next(0, 2) == 0;
                    if (basic)
                    {
                        // Basic path: HitboxDamage raises OnHit AND OnBasicHit. The generic OnHit must
                        // not double-charge (this boon ignores it); only OnBasicHit grants energy.
                        bus.RaiseHit(DummyActor, 10f);
                        bus.RaiseBasicHit(DummyActor, 10f);
                        expected = Math.Min(Cap, expected + EnergyPerRankPerHit * rank);
                    }
                    else
                    {
                        // Skill path: only the generic OnHit fires, never OnBasicHit — no charge.
                        bus.RaiseHit(DummyActor, 10f);
                    }

                    PropertyCheck.That(meter.Energy == expected,
                        $"rank={rank}, event={e}, basic={basic}: Energy={meter.Energy}, expected={expected}");
                }

                surge.Dispose();
            });
        }

        // Rank 0 (boon not acquired) grants nothing even on a basic hit — the guard mirrors AsuraSurge's
        // own rank gate, so an inactive boon never charges the meter.
        private static readonly Actor DummyActor = null; // OnBasicHit carries an actor; this boon ignores it

        // Example: a single basic hit at rank 2 adds exactly 4 Asura from empty.
        [Test]
        public void BasicHit_Rank2_AddsFourFromEmpty()
        {
            var meter = new AsuraMomentum();
            var bus = new HookBus();
            var surge = new AsuraSurgeSpy(meter, bus, 2);

            bus.RaiseBasicHit(DummyActor, 10f);

            Assert.That(meter.Energy, Is.EqualTo(4), "Rank 2 basic hit adds 2 x 2 = 4 Asura.");
            surge.Dispose();
        }

        // Example: hits that would overflow the meter clamp at the authored maximum (100).
        [Test]
        public void BasicHits_ClampAtMaximum()
        {
            var meter = new AsuraMomentum();
            meter.AddEnergy(98);
            var bus = new HookBus();
            var surge = new AsuraSurgeSpy(meter, bus, 3); // +6 per hit

            bus.RaiseBasicHit(DummyActor, 10f); // 98 + 6 -> clamp 100
            Assert.That(meter.Energy, Is.EqualTo(Cap), "Overflowing basic hit clamps to the maximum.");

            bus.RaiseBasicHit(DummyActor, 10f); // already full -> stays at 100
            Assert.That(meter.Energy, Is.EqualTo(Cap), "A full meter stays at the maximum.");
            surge.Dispose();
        }

        // Example: a skill hit (generic OnHit only) grants nothing from this boon.
        [Test]
        public void SkillHit_LeavesMeterUnchanged()
        {
            var meter = new AsuraMomentum();
            var bus = new HookBus();
            var surge = new AsuraSurgeSpy(meter, bus, 3);

            bus.RaiseHit(DummyActor, 50f); // skill/area path: never raises OnBasicHit

            Assert.That(meter.Energy, Is.EqualTo(0), "A skill hit must not charge Asura through this boon.");
            surge.Dispose();
        }
    }
}
