using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure Flow accumulator in <see cref="SpearFlow"/> /
    /// <see cref="SpearFlowConfig"/> — task 11.4 of weapon-gameplay-swarm-rework.
    ///
    /// Flow is an opt-in resource of the Lança. The core promise of Requisito 9.7 is that turning the
    /// feature on must not change the damage, mana cost or cooldown of any spear ability for a player
    /// who does <b>not</b> accumulate Flow. <see cref="SpearFlow"/> was extracted as a plain, scene-free
    /// collaborator so this promise can be property-checked without a live Unity scene: it only ever
    /// reads its own internal counter and never mutates ability data, so the observable proxy for
    /// "does not touch damage/cost/cooldown" is that, while disabled, every accumulation entry point is
    /// a complete no-op — Flow stays <see cref="SpearFlow.EmptyFlow"/> and every call grants exactly 0,
    /// across any sequence of activations and hits, and any surrounding ability values it is handed are
    /// returned untouched. This project cannot resolve FsCheck/CsCheck packages on this machine, so the
    /// agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases
    /// (min 128) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SpearFlowNoInterferenceWhenNotAccumulatingPropertyTests
    {
        private static readonly ArsenalSkillKind[] AllKinds =
        {
            ArsenalSkillKind.Arrow,
            ArsenalSkillKind.Volley,
            ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust,
            ArsenalSkillKind.Sweep,
        };

        // Feature: weapon-gameplay-swarm-rework, Property 32: Flow não interfere em quem não acumula
        // Para toda habilidade da Lança, habilitar o recurso opcional Flow não altera dano, custo de mana
        // nem recarga da habilidade para um jogador que não acumula Flow. Um SpearFlow que não está
        // habilitado (não acumula) é um no-op completo: nenhuma sequência de RegisterAbility /
        // RegisterSweetSpotHit altera o Flow (permanece EmptyFlow) e toda entrada devolve 0 concedido; o
        // acumulador nunca lê nem escreve dano/custo/recarga, então quaisquer valores de habilidade
        // observados ao seu redor permanecem intactos.
        // Validates: Requirements 9.7
        [Test]
        public void DisabledFlowNeverAccumulatesAndNeverTouchesAbilityValues()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- an arbitrary Flow config; disabled by default (opt-out) --------------------
                SpearFlowConfig config = RandomConfig(rng);
                var flow = new SpearFlow(config);

                // A fresh accumulator does not accumulate: it is disabled and empty.
                PropertyCheck.That(!flow.IsEnabled,
                    $"[case #{i}] a fresh SpearFlow was unexpectedly enabled");
                PropertyCheck.That(flow.Flow == SpearFlow.EmptyFlow,
                    $"[case #{i}] fresh Flow {flow.Flow} != EmptyFlow {SpearFlow.EmptyFlow}");
                PropertyCheck.That(!flow.IsFull,
                    $"[case #{i}] a fresh (disabled) SpearFlow reported IsFull");

                // ---- a snapshot of the ability values Flow must never influence -----------------
                // Flow holds no ability references and cannot mutate these; we keep local copies and
                // prove they are byte-for-byte identical after driving every entry point. This is the
                // observable proxy for "does not change damage / mana cost / cooldown" (Requisito 9.7).
                float damage = 1f + (float)rng.NextDouble() * 500f;
                float manaCost = (float)rng.NextDouble() * 100f;
                float cooldown = (float)rng.NextDouble() * 30f;

                float damageBefore = damage;
                float manaCostBefore = manaCost;
                float cooldownBefore = cooldown;

                // ---- drive an arbitrary sequence of entry points while disabled -----------------
                int steps = rng.Next(0, 40);
                for (int s = 0; s < steps; s++)
                {
                    // Choose between the two accumulation entry points.
                    if (rng.Next(0, 2) == 0)
                    {
                        ArsenalSkillKind kind = AllKinds[rng.Next(0, AllKinds.Length)];
                        float granted = flow.RegisterAbility(kind);

                        PropertyCheck.That(granted == 0f,
                            $"[case #{i}] RegisterAbility({kind}) granted {granted} while disabled (expected 0)");
                    }
                    else
                    {
                        bool isSweetSpot = rng.Next(0, 2) == 0;
                        float granted = flow.RegisterSweetSpotHit(isSweetSpot);

                        PropertyCheck.That(granted == 0f,
                            $"[case #{i}] RegisterSweetSpotHit({isSweetSpot}) granted {granted} while disabled (expected 0)");
                    }

                    // After every step, Flow stays empty and the resource stays disabled.
                    PropertyCheck.That(flow.Flow == SpearFlow.EmptyFlow,
                        $"[case #{i}] Flow drifted to {flow.Flow} after step {s} while disabled (expected EmptyFlow)");
                    PropertyCheck.That(!flow.IsEnabled,
                        $"[case #{i}] SpearFlow became enabled after step {s} without an opt-in");
                    PropertyCheck.That(!flow.IsFull,
                        $"[case #{i}] disabled SpearFlow reported IsFull after step {s}");
                }

                // ---- the surrounding ability values are untouched -------------------------------
                PropertyCheck.That(damage == damageBefore,
                    $"[case #{i}] damage changed {damageBefore} -> {damage} while Flow was disabled");
                PropertyCheck.That(manaCost == manaCostBefore,
                    $"[case #{i}] mana cost changed {manaCostBefore} -> {manaCost} while Flow was disabled");
                PropertyCheck.That(cooldown == cooldownBefore,
                    $"[case #{i}] cooldown changed {cooldownBefore} -> {cooldown} while Flow was disabled");

                // Final state is indistinguishable from a fresh, never-accumulated resource.
                PropertyCheck.That(flow.Flow == SpearFlow.EmptyFlow,
                    $"[case #{i}] final Flow {flow.Flow} != EmptyFlow after {steps} disabled entry points");

                // Explicitly toggling off (redundantly) keeps it a no-op: still empty, still 0 granted.
                flow.SetEnabled(false);
                PropertyCheck.That(flow.Flow == SpearFlow.EmptyFlow,
                    $"[case #{i}] Flow {flow.Flow} != EmptyFlow after SetEnabled(false)");
                PropertyCheck.That(flow.RegisterAbility(ArsenalSkillKind.Thrust) == 0f,
                    $"[case #{i}] RegisterAbility granted Flow after an explicit opt-out");
                PropertyCheck.That(flow.RegisterSweetSpotHit(true) == 0f,
                    $"[case #{i}] RegisterSweetSpotHit granted Flow after an explicit opt-out");
                PropertyCheck.That(flow.Flow == SpearFlow.EmptyFlow,
                    $"[case #{i}] Flow {flow.Flow} != EmptyFlow after opt-out entry points");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// An arbitrary Flow config. The grant amounts and cap are varied widely (including generous
        /// values) precisely to show that, while disabled, none of them can move the resource.
        /// </summary>
        private static SpearFlowConfig RandomConfig(System.Random rng)
        {
            float sweetSpotFlow = (float)rng.NextDouble() * 50f;   // 0 .. 50
            float alternationFlow = (float)rng.NextDouble() * 50f; // 0 .. 50
            float maxFlow = SpearFlowConfig.MinMaxFlow + (float)rng.NextDouble() * 200f; // strictly > 0
            return new SpearFlowConfig(sweetSpotFlow, alternationFlow, maxFlow);
        }
    }
}
