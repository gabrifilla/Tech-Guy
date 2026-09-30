using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 10 of impactful-weapon-boons (Momentum Strike,
    /// <see cref="MomentumStacks"/>).
    ///
    /// Property 10 (design): for any MomentumStrike rank <c>r</c>, each Direct_Hit adds one stack up to
    /// <see cref="MomentumStacks.MaxStacks"/> (10), the outgoing-damage increase equals
    /// <c>(2 · r · stacks)%</c>, and taking any damage resets the stacks (and the bonus) to zero.
    ///
    /// Approach: <see cref="MomentumStacks"/> is a thin MonoBehaviour whose logic is trivial event-driven
    /// arithmetic — it holds run-scoped references, tracks the stack count, and keeps a single
    /// <see cref="PlayerStatModifier"/> in sync. So the test drives the real component directly rather
    /// than a re-stated model: it builds an inactive host GameObject (so no <c>Awake</c>/scene lifecycle
    /// runs), attaches a <see cref="PlayerActor"/> and the component, wires a real <see cref="HookBus"/>,
    /// and exercises the two production triggers exactly as gameplay does — <c>HookBus.RaiseHit</c> for a
    /// Direct_Hit (R6.1) and <c>PlayerActor.TakeDamage</c> for the damage-reset path (R6.3, which fires
    /// <c>Actor.DamageReceived</c> only when health is actually lost).
    ///
    /// Reading the bonus (R6.2): the component installs a <c>PlayerStatModifier(IncreasedDamagePercent,
    /// IncreasedPercent, 2*rank*stacks)</c>. The primary assertion sums the matching runtime modifiers —
    /// the exact contribution the component installed — and a corroborating assertion confirms it flows
    /// through the real <see cref="PlayerArpgStats.GetStat"/> pipeline: with the player's base
    /// <c>increasedDamagePercent</c> seeded to a known value B, <c>GetStat</c> returns
    /// <c>B * (1 + bonus/100)</c>, so the recovered bonus <c>(GetStat/B - 1)*100</c> equals <c>2·r·stacks</c>.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and reports
    /// the exact failing case as a counterexample.
    /// </summary>
    public sealed class MomentumStackAndDamagePropertyTests
    {
        private const float Tolerance = 1e-3f;
        private const float PercentPerRankPerStack = 2f;

        // A known non-zero base for IncreasedDamagePercent so GetStat surfaces the IncreasedPercent-mode
        // momentum modifier: GetStat(IncreasedDamagePercent) == base * (1 + bonus/100). (With a zero base
        // the IncreasedPercent term multiplies zero, so the base must be non-zero to recover the bonus.)
        private const float BaseIncreasedPercent = 100f;

        // Builds an inactive host carrying a PlayerActor + a configured MomentumStacks, wired to a fresh
        // HookBus. Inactive so no Awake/scene lifecycle runs (same rig pattern as RewriteOfferConstraintTests).
        // The player starts with plenty of health so a reset hit via TakeDamage loses health (firing
        // DamageReceived) without killing it. statSource stands in for the RunBoons source tag.
        private static Rig BuildRig(int rank)
        {
            var host = new GameObject("MomentumStacksHost");
            host.SetActive(false);

            PlayerActor player = host.AddComponent<PlayerActor>();
            player.health = 1000f;
            player.Stats.increasedDamagePercent = BaseIncreasedPercent;

            var hooks = new HookBus();
            var statSource = ScriptableObject.CreateInstance<MomentumStatSourceStub>();

            MomentumStacks momentum = host.AddComponent<MomentumStacks>();
            momentum.Configure(player, hooks, rank, statSource);

            return new Rig(host, player, hooks, momentum, statSource);
        }

        private readonly struct Rig
        {
            public readonly GameObject Host;
            public readonly PlayerActor Player;
            public readonly HookBus Hooks;
            public readonly MomentumStacks Momentum;
            public readonly Object StatSource;

            public Rig(GameObject host, PlayerActor player, HookBus hooks, MomentumStacks momentum, Object statSource)
            {
                Host = host; Player = player; Hooks = hooks; Momentum = momentum; StatSource = statSource;
            }

            public void Dispose()
            {
                if (Host) Object.DestroyImmediate(Host);
                if (StatSource) Object.DestroyImmediate(StatSource);
            }
        }

        // A throwaway ScriptableObject used only as the modifier's source tag, mirroring how RunBoons tags
        // the momentum modifier so run-end RemoveModifiersFrom(source) would clear it.
        private sealed class MomentumStatSourceStub : ScriptableObject { }

        // The exact momentum contribution the component installed: the sum of every runtime modifier that
        // matches (IncreasedDamagePercent, IncreasedPercent). This is the raw (2*r*stacks) value.
        private static float InstalledBonus(PlayerActor player)
        {
            float sum = 0f;
            foreach (PlayerStatModifier m in player.Stats.RuntimeModifiers)
                if (m != null && m.statType == PlayerStatType.IncreasedDamagePercent
                    && m.mode == PlayerStatModifierMode.IncreasedPercent)
                    sum += m.value;
            return sum;
        }

        // The same bonus recovered through the real GetStat pipeline, given a known non-zero base:
        // GetStat == base * (1 + bonus/100)  =>  bonus == (GetStat/base - 1) * 100.
        private static float PipelineBonus(PlayerActor player)
        {
            float stat = player.Stats.GetStat(PlayerStatType.IncreasedDamagePercent);
            return (stat / BaseIncreasedPercent - 1f) * 100f;
        }

        // Feature: impactful-weapon-boons, Property 10: Momentum stack and damage.
        // For any rank r and any number of hits k, after k Direct_Hits Stacks == min(k, 10) and the
        // outgoing-damage increase == (2 * r * min(k, 10))%; a subsequent damage-taken event resets both
        // Stacks and the bonus to zero.
        // Validates: Requirements 6.1, 6.2, 6.3
        [Test]
        public void MomentumStacksClampAndBonusScaleAndResetOnDamage()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);                 // MomentumStrike ranks 1..3
                int hits = rng.Next(0, 16);                // spans below, at, and above the 10-stack clamp
                int expectedStacks = Mathf.Min(hits, MomentumStacks.MaxStacks);
                float expectedBonus = PercentPerRankPerStack * rank * expectedStacks;

                Rig rig = BuildRig(rank);
                try
                {
                    // R6.1: each Direct_Hit adds one stack, clamped at MaxStacks. The handler ignores the
                    // victim/damage payload, so a null victim exercises the same add-a-stack path a real hit does.
                    for (int h = 0; h < hits; h++) rig.Hooks.RaiseHit(null, 1f);

                    PropertyCheck.That(rig.Momentum.Stacks == expectedStacks,
                        $"rank={rank}, hits={hits}: Stacks {rig.Momentum.Stacks} != min(hits,10)={expectedStacks}");

                    // R6.2: bonus == 2 * rank * stacks, both as the installed modifier value and through GetStat.
                    float installed = InstalledBonus(rig.Player);
                    PropertyCheck.That(Mathf.Abs(installed - expectedBonus) <= Tolerance,
                        $"rank={rank}, hits={hits}: installed bonus {installed} != 2*r*stacks={expectedBonus}");
                    float pipeline = PipelineBonus(rig.Player);
                    PropertyCheck.That(Mathf.Abs(pipeline - expectedBonus) <= Tolerance,
                        $"rank={rank}, hits={hits}: GetStat bonus {pipeline} != 2*r*stacks={expectedBonus}");

                    // R6.3: taking damage resets stacks (and the bonus) to zero. TakeDamage fires
                    // Actor.DamageReceived because health is actually lost (1000 -> 990).
                    rig.Player.TakeDamage(10f);

                    PropertyCheck.That(rig.Momentum.Stacks == 0,
                        $"rank={rank}, hits={hits}: Stacks {rig.Momentum.Stacks} not reset to 0 after damage");
                    PropertyCheck.That(Mathf.Abs(InstalledBonus(rig.Player)) <= Tolerance,
                        $"rank={rank}, hits={hits}: installed bonus {InstalledBonus(rig.Player)} not 0 after damage");
                    PropertyCheck.That(Mathf.Abs(PipelineBonus(rig.Player)) <= Tolerance,
                        $"rank={rank}, hits={hits}: GetStat bonus {PipelineBonus(rig.Player)} not 0 after damage");
                }
                finally { rig.Dispose(); }
            });
        }

        // Example: stacks clamp exactly at MaxStacks — the 11th hit does not raise the count past 10, and
        // the bonus at the clamp equals 2*r*10. Anchors the boundary the property samples around (R6.1/6.2).
        [Test]
        public void MomentumStacks_ClampAtTenAcrossRanks()
        {
            for (int rank = 1; rank <= 3; rank++)
            {
                Rig rig = BuildRig(rank);
                try
                {
                    for (int h = 0; h < MomentumStacks.MaxStacks + 5; h++) rig.Hooks.RaiseHit(null, 1f);

                    Assert.That(rig.Momentum.Stacks, Is.EqualTo(MomentumStacks.MaxStacks),
                        "Stacks must clamp at MaxStacks even after extra hits.");
                    Assert.That(InstalledBonus(rig.Player),
                        Is.EqualTo(PercentPerRankPerStack * rank * MomentumStacks.MaxStacks).Within(Tolerance),
                        "Bonus at the clamp must equal 2*r*MaxStacks.");
                }
                finally { rig.Dispose(); }
            }
        }

        // Example: the reset is retroactive mid-sequence — hits build stacks, a damage event zeroes them,
        // and hits after the reset build again from zero (R6.1 + R6.3 interleaved).
        [Test]
        public void MomentumStacks_RebuildAfterDamageReset()
        {
            Rig rig = BuildRig(rank: 2);
            try
            {
                rig.Hooks.RaiseHit(null, 1f);
                rig.Hooks.RaiseHit(null, 1f);
                rig.Hooks.RaiseHit(null, 1f);
                Assert.That(rig.Momentum.Stacks, Is.EqualTo(3), "Three hits should build three stacks.");

                rig.Player.TakeDamage(10f);
                Assert.That(rig.Momentum.Stacks, Is.EqualTo(0), "Damage should reset stacks to zero.");
                Assert.That(InstalledBonus(rig.Player), Is.EqualTo(0f).Within(Tolerance),
                    "The bonus should be zero after a reset.");

                rig.Hooks.RaiseHit(null, 1f);
                rig.Hooks.RaiseHit(null, 1f);
                Assert.That(rig.Momentum.Stacks, Is.EqualTo(2), "Hits after a reset should build from zero.");
                Assert.That(InstalledBonus(rig.Player),
                    Is.EqualTo(PercentPerRankPerStack * 2 * 2).Within(Tolerance),
                    "Rebuilt bonus must equal 2*r*stacks for the new stack count.");
            }
            finally { rig.Dispose(); }
        }

        // Example: at rank 0 the boon is inert — the component adds no stacks and installs no bonus even as
        // hits arrive, guarding against a mis-configured/absent rank driving the stat pipeline.
        [Test]
        public void MomentumStacks_RankZeroIsInert()
        {
            Rig rig = BuildRig(rank: 0);
            try
            {
                for (int h = 0; h < 5; h++) rig.Hooks.RaiseHit(null, 1f);

                Assert.That(rig.Momentum.Stacks, Is.EqualTo(0), "Rank 0 must never accumulate stacks.");
                Assert.That(InstalledBonus(rig.Player), Is.EqualTo(0f).Within(Tolerance),
                    "Rank 0 must install no damage bonus.");
            }
            finally { rig.Dispose(); }
        }
    }
}
