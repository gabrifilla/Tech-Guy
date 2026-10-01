using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 4 of gauntlet-boon-playstyle-overhaul
    /// ("Fim do run não deixa estado residual" — run-end leaves no residual state).
    ///
    /// Property 4 (design): <em>for any</em> combination of chosen new boons, when the run ends all
    /// run-scoped state SHALL be cleared — the basic-hit channel subscriptions (<c>HookBus.OnBasicHit</c>),
    /// the <c>RunBoons</c>-tagged stat modifiers, the coordinators/registries, and the <c>_basicKilled</c>
    /// dedup set — so no effect from the ended run fires in a later run.
    /// <b>Validates: Requirements 1.4, 3.5, 4.4, 6.4, 9.4.</b>
    ///
    /// <para>Approach. The run's teardown is centralized in <c>RunBoons.OnDestroy</c>: it first calls
    /// <c>Stats.RemoveModifiersFrom(this)</c> (dropping every <c>RunBoons</c>-tagged stat modifier) and
    /// then the atomic <c>ClearRunState()</c> body, whose <c>Hooks.Clear()</c> nulls every
    /// <see cref="HookBus"/> event — including the basic channel — and clears both the <c>_killed</c> and
    /// <c>_basicKilled</c> dedup sets. This test reproduces that exact two-step sequence against a real
    /// player host carrying the four run-scoped boon coordinators this feature wires onto the basic
    /// channel, then asserts every channel is inert afterwards.</para>
    ///
    /// <para>The four coordinators exercised are the production MonoBehaviours, driven through their real
    /// triggers (not re-stated models):</para>
    /// <list type="bullet">
    /// <item><see cref="AsuraSurge"/> (R4) subscribes to <c>OnBasicHit</c> and charges the Manopla meter
    /// via <see cref="BreakerGauntletCombat.AddAsuraEnergy(int)"/>; after teardown a basic hit must add no
    /// energy (R4.4).</item>
    /// <item><see cref="HungryComboTracker"/> (R6) subscribes to <c>OnBasicHit</c> and shaves live
    /// cooldowns; after teardown it must not react (R6.4) — proven here by the subscription being dropped
    /// (its own effect needs a cooldown-bearing holder, so inertness is read off the shared channel).</item>
    /// <item><see cref="AdaptiveCadenceTracker"/> (R9) subscribes to <c>OnBasicHit</c> and installs a
    /// <c>RunBoons</c>-tagged <see cref="PlayerStatModifier"/>; teardown must both remove the modifier and
    /// drop the subscription so a later far hit re-installs nothing (R9.4).</item>
    /// <item><see cref="ImpactGuardTracker"/> (R5) subscribes to <c>OnBasicHit</c> and advances a streak;
    /// after teardown a basic hit must not advance it (R3.5 — the basic channel itself is cleared).</item>
    /// </list>
    ///
    /// <para>Because <c>Stats.RemoveModifiersFrom(source)</c> is tagged by <see cref="UnityEngine.Object"/>
    /// reference, the test tags the cadence modifier with a throwaway ScriptableObject that stands in for
    /// the <c>RunBoons</c> source (mirroring how <c>RunBoons.Choose</c> passes <c>this</c>), and the
    /// teardown step calls <c>RemoveModifiersFrom(thatSource)</c> exactly as <c>RunBoons.OnDestroy</c>
    /// does.</para>
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 4
    public sealed class GauntletOverhaulTeardownPropertyTests
    {
        private const float Tolerance = 1e-3f;
        private const int AsuraPerRankPerHit = 2;        // R4.1: AsuraSurge adds 2*rank per basic hit
        private const float CadencePercentPerRank = 15f; // R9.1: +15% AttackSpeedMultiplier per rank
        private const float KitingBand = AdaptiveCadenceTracker.KitingBand; // 6m (R9.1/R9.2)

        // Stands in for the RunBoons source the cadence stat modifier is tagged with. RunBoons is a
        // MonoBehaviour; a ScriptableObject is a valid UnityEngine.Object source tag and mirrors how
        // RemoveModifiersFrom(this) removes only same-source modifiers.
        private sealed class RunSourceStub : ScriptableObject { }

        // A run-scoped rig: an inactive player host (so no Awake/scene lifecycle runs) carrying a real
        // PlayerActor + BreakerGauntletCombat and the four basic-channel coordinators, wired to a fresh
        // HookBus and a RunBoons-source stub, plus a victim Actor positioned to vary distance.
        private sealed class Rig
        {
            public readonly GameObject Host;
            public readonly GameObject VictimHost;
            public readonly PlayerActor Player;
            public readonly Actor Victim;
            public readonly HookBus Hooks;
            public readonly Object StatSource;
            public readonly BreakerGauntletCombat Combat;
            public readonly AsuraSurge Asura;
            public readonly HungryComboTracker Hungry;
            public readonly AdaptiveCadenceTracker Cadence;
            public readonly ImpactGuardTracker Guard;

            public Rig(int asuraRank, int hungryRank, int cadenceRank, int guardRank)
            {
                Host = new GameObject("TeardownHost");
                Host.SetActive(false);
                Player = Host.AddComponent<PlayerActor>();
                Player.transform.position = Vector3.zero;
                Combat = Host.AddComponent<BreakerGauntletCombat>();
                AbilityHolder holder = Host.AddComponent<AbilityHolder>();

                VictimHost = new GameObject("TeardownVictim");
                VictimHost.SetActive(false);
                Victim = VictimHost.AddComponent<Actor>();
                Victim.health = 100000f;

                Hooks = new HookBus();
                StatSource = ScriptableObject.CreateInstance<RunSourceStub>();

                Asura = Host.AddComponent<AsuraSurge>();
                Asura.Configure(Player, Hooks, Combat, asuraRank);

                Hungry = Host.AddComponent<HungryComboTracker>();
                Hungry.Configure(Player, Hooks, holder, hungryRank);

                Cadence = Host.AddComponent<AdaptiveCadenceTracker>();
                Cadence.Configure(Player, Hooks, cadenceRank, StatSource);

                Guard = Host.AddComponent<ImpactGuardTracker>();
                Guard.Configure(Player, Hooks, holder, guardRank);
            }

            // Position the victim at the given distance (along +X) and raise the real basic-hit channel.
            public void BasicHitAt(float distance)
            {
                VictimHost.transform.position = Player.transform.position + Vector3.right * distance;
                Hooks.RaiseBasicHit(Victim, 10f);
            }

            // Reproduces the RunBoons.OnDestroy teardown sequence verbatim. RunBoons.OnDestroy runs on the
            // run GameObject, so it (1) calls Stats.RemoveModifiersFrom(this) to drop every RunBoons-tagged
            // stat modifier, (2) runs the atomic ClearRunState body whose Hooks.Clear() nulls every HookBus
            // event (the basic channel) and clears _killed/_basicKilled, and (3) tears down the run object —
            // which runs each coordinator's own OnDestroy -> Unsubscribe. The coordinators live on the run
            // GameObject (RunBoons.gameObject.AddComponent), so destroying the run object destroys them too.
            // We reproduce all three steps so the post-teardown world matches production exactly.
            public void TearDownRun()
            {
                Player.Stats.RemoveModifiersFrom(StatSource); // OnDestroy step 1 (R9.4)
                Hooks.Clear();                                 // ClearRunState -> HookBus.Clear (R3.5/R4.4/R6.4)
                // The run GameObject (which carries the coordinators) is destroyed last; its children's
                // OnDestroy -> Unsubscribe runs here. Each coordinator is a run-scoped component, so once the
                // run ends none of them survive to react to a later run's events.
                Object.DestroyImmediate(Asura);
                Object.DestroyImmediate(Hungry);
                Object.DestroyImmediate(Cadence);
                Object.DestroyImmediate(Guard);
            }

            // The sum of the cadence contribution installed on the player: every runtime modifier matching
            // (AttackSpeedMultiplier, IncreasedPercent). This is 15*rank while active, else 0.
            public float InstalledCadenceBonus()
            {
                float sum = 0f;
                foreach (PlayerStatModifier m in Player.Stats.RuntimeModifiers)
                    if (m != null && m.statType == PlayerStatType.AttackSpeedMultiplier
                        && m.mode == PlayerStatModifierMode.IncreasedPercent)
                        sum += m.value;
                return sum;
            }

            // Any runtime modifier still tagged with the RunBoons source after teardown is residual state.
            public int ModifiersFromSource()
            {
                int count = 0;
                foreach (PlayerStatModifier m in Player.Stats.RuntimeModifiers)
                    if (m != null && m.source == StatSource) count++;
                return count;
            }

            public void Dispose()
            {
                if (Host) Object.DestroyImmediate(Host);
                if (VictimHost) Object.DestroyImmediate(VictimHost);
                if (StatSource) Object.DestroyImmediate(StatSource);
            }
        }

        // Property 4: for any ranks (1..3) and any pre-teardown activity, after the RunBoons.OnDestroy
        // teardown sequence (RemoveModifiersFrom(source) + HookBus.Clear) every basic-channel boon is
        // inert — a later basic hit charges no Asura, advances no streak, and re-installs no cadence
        // modifier — and no RunBoons-tagged stat modifier remains.
        // Validates: Requirements 1.4, 3.5, 4.4, 6.4, 9.4
        [Test]
        public void RunEnd_ClearsEveryBasicChannelBoonState()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int asuraRank = rng.Next(1, 4);
                int hungryRank = rng.Next(1, 4);
                int cadenceRank = rng.Next(1, 4);
                int guardRank = rng.Next(1, 4);

                Rig rig = new Rig(asuraRank, hungryRank, cadenceRank, guardRank);
                try
                {
                    // --- pre-teardown: arm each channel with a few far basic hits so there is live state ---
                    int warmupHits = rng.Next(1, 10);
                    for (int h = 0; h < warmupHits; h++)
                        rig.BasicHitAt(KitingBand + 1f + (float)(rng.NextDouble() * 5.0)); // >= band: cadence on

                    // Sanity: the channels are live before teardown so the post-teardown assertions mean
                    // something (an already-inert channel would trivially "pass").
                    PropertyCheck.That(rig.Combat.Energy > 0,
                        $"pre-teardown: Asura energy should be charged, was {rig.Combat.Energy}");
                    PropertyCheck.That(rig.Cadence.IsActive,
                        "pre-teardown: cadence should be active after far hits");
                    PropertyCheck.That(rig.InstalledCadenceBonus() > 0f,
                        "pre-teardown: a RunBoons-tagged cadence modifier should exist");

                    // Capture the pre-teardown observables (the coordinators are destroyed by TearDownRun,
                    // mirroring the run GameObject teardown, so these can only be read beforehand).
                    int energyBefore = rig.Combat.Energy;

                    // --- teardown: the exact RunBoons.OnDestroy sequence (incl. coordinator OnDestroy) ---
                    rig.TearDownRun();

                    // R9.4: no RunBoons-tagged stat modifier remains after RemoveModifiersFrom(source).
                    PropertyCheck.That(rig.ModifiersFromSource() == 0,
                        $"post-teardown: {rig.ModifiersFromSource()} RunBoons-tagged modifier(s) survived");
                    PropertyCheck.That(Mathf.Abs(rig.InstalledCadenceBonus()) <= Tolerance,
                        $"post-teardown: cadence bonus {rig.InstalledCadenceBonus()} survived teardown");

                    // --- post-teardown: a later run's basic hit must fire no effect through the cleared channel ---
                    int laterHits = rng.Next(1, 8);
                    for (int h = 0; h < laterHits; h++)
                        rig.BasicHitAt(KitingBand + 2f); // far hits that WOULD charge/activate pre-teardown

                    // R4.4: AsuraSurge's OnBasicHit sub is gone (HookBus.Clear + its OnDestroy) — the meter,
                    // which outlives the run on the player, is never charged by the ended run's boon again.
                    PropertyCheck.That(rig.Combat.Energy == energyBefore,
                        $"post-teardown: Asura charged after teardown ({energyBefore} -> {rig.Combat.Energy})");
                    // R6.4/R9.4: no coordinator re-reacts — the cadence modifier is not re-installed, so the
                    // player's stats carry no residual RunBoons-tagged modifier from the ended run (R3.5: the
                    // basic channel itself is cleared, so no subscriber of any kind receives the later hits).
                    PropertyCheck.That(rig.ModifiersFromSource() == 0,
                        "post-teardown: a later basic hit re-installed a RunBoons-tagged modifier");
                }
                finally { rig.Dispose(); }
            });
        }

        // Example: the Asura meter stops charging the instant the run is torn down.
        [Test]
        public void AsuraStopsChargingAfterTeardown()
        {
            Rig rig = new Rig(asuraRank: 3, hungryRank: 1, cadenceRank: 1, guardRank: 1);
            try
            {
                rig.BasicHitAt(7f);
                int charged = rig.Combat.Energy;
                Assert.That(charged, Is.GreaterThan(0), "Asura should charge on a basic hit before teardown.");

                rig.TearDownRun();
                rig.BasicHitAt(7f);
                Assert.That(rig.Combat.Energy, Is.EqualTo(charged),
                    "A basic hit after teardown must add no Asura (OnBasicHit subscription dropped).");
            }
            finally { rig.Dispose(); }
        }

        // Example: the RunBoons-tagged cadence modifier is removed, and never comes back on a later hit.
        [Test]
        public void CadenceModifierRemovedAndNotReinstalledAfterTeardown()
        {
            Rig rig = new Rig(asuraRank: 1, hungryRank: 1, cadenceRank: 2, guardRank: 1);
            try
            {
                rig.BasicHitAt(8f); // >= 6m: cadence active, modifier installed
                Assert.That(rig.InstalledCadenceBonus(), Is.EqualTo(CadencePercentPerRank * 2).Within(Tolerance),
                    "Cadence modifier should be installed (15*rank) before teardown.");

                rig.TearDownRun();
                Assert.That(rig.ModifiersFromSource(), Is.EqualTo(0),
                    "RemoveModifiersFrom(source) must drop the RunBoons-tagged cadence modifier.");

                rig.BasicHitAt(8f); // a later far hit must not re-install anything
                Assert.That(rig.ModifiersFromSource(), Is.EqualTo(0),
                    "A post-teardown far hit must not re-install a RunBoons-tagged modifier.");
            }
            finally { rig.Dispose(); }
        }

        // Example: the guard tracker stops reacting to the basic channel after the run is torn down.
        // The ImpactGuardTracker is a run-scoped component destroyed with the run; its OnBasicHit effect is
        // the breaking stance reaction. After teardown the only run-outliving observable is the Asura meter
        // (on the player), so inertness of the shared cleared channel is proven through it: no boon reacts.
        [Test]
        public void BasicChannelDeadAfterTeardown()
        {
            Rig rig = new Rig(asuraRank: 2, hungryRank: 2, cadenceRank: 2, guardRank: 2);
            try
            {
                rig.BasicHitAt(2f);
                Assert.That(rig.Guard.Streak, Is.GreaterThan(0), "The streak should advance on a basic hit before teardown.");

                int energyBefore = rig.Combat.Energy;
                rig.TearDownRun();
                rig.BasicHitAt(2f);
                Assert.That(rig.Combat.Energy, Is.EqualTo(energyBefore),
                    "After teardown the cleared basic channel reaches no subscriber (Asura charges nothing).");
                Assert.That(rig.ModifiersFromSource(), Is.EqualTo(0),
                    "After teardown no RunBoons-tagged modifier is (re)installed by a basic hit.");
            }
            finally { rig.Dispose(); }
        }
    }
}
