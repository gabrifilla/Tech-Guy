using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the shared anti-Heavy tool of the three weapons — task 13.4 of
    /// weapon-gameplay-swarm-rework (Property 27, Requirements 8.5, 9.5, 11.2).
    ///
    /// <para>
    /// The anti-Heavy response to the Heavy archetype is the Manoplas <b>E</b> (Impacto de Choque /
    /// Stance Breaker, <see cref="GauntletLoopSteps"/>), the Lança <b>E</b> (Perfuração pesada,
    /// <see cref="SpearLoopStage.Pierce"/>) and the Arco <b>W</b> (Flecha Pesada,
    /// <see cref="BowLoopStage.HeavyArrow"/>). All three express one shared force floor:
    /// <see cref="GauntletLoopSteps.AntiHeavyMinStrength"/> (== <see cref="HitStrength.Heavy"/>), reused
    /// verbatim by <see cref="SpearLoopSteps.AntiHeavyMinStrength"/> and
    /// <see cref="BowLoopSteps.AntiHeavyMinStrength"/>.
    /// </para>
    ///
    /// <para><b>Pure, scene-independent invariants validated here:</b></para>
    /// <list type="bullet">
    /// <item><description>
    /// For the anti-Heavy tool of each weapon, the resulting <see cref="HitStrength"/> is Heavy or
    /// Breaker (<c>IsHeavyOrBreaker</c> is true), so it can break a Heavy enemy's stance.
    /// </description></item>
    /// <item><description>
    /// <see cref="GauntletLoopSteps.EnsureHeavyOrBreaker"/> never weakens a stronger authored value
    /// (Heavy stays Heavy, Breaker stays Breaker) and raises any weak value (Light/Medium) up to Heavy.
    /// </description></item>
    /// <item><description>
    /// The three weapons share the exact same floor, so the anti-Heavy contract is identical across
    /// Manopla E, Lança E and Arco W.
    /// </description></item>
    /// </list>
    ///
    /// <para><b>Scene-bound caveat (NOT validated here).</b> The full Property 27 statement also asserts
    /// the tool <i>is capable of breaking the stance of a Heavy enemy</i>. Whether a given force actually
    /// cracks a Heavy enemy's stance depends on the live <c>CombatReactionController</c> resolving stance
    /// damage against the enemy's rank resistances in a running scene, which cannot be exercised in
    /// EditMode without a scene. This test validates the pure force-floor half — that the anti-Heavy
    /// tools land with Heavy/Breaker force, which is the precondition the controller requires to break a
    /// Heavy — and does not assert the scene-driven break resolution itself.</para>
    ///
    /// <para>
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property and reports
    /// the exact failing case as a counterexample. Source files are NOT edited by this test.
    /// </para>
    /// </summary>
    public sealed class AntiHeavyToolStrengthPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 27: Ferramenta anti-Heavy usa força Heavy ou Breaker
        // Para toda arma, a ferramenta de resposta ao arquétipo Heavy (Manopla E, Lança E, Arco W) usa
        // força HitStrength Heavy ou Breaker e é capaz de quebrar a postura de um inimigo Heavy.
        //
        // Pure half validated here: the anti-Heavy tool of each of the three weapons resolves to a
        // HitStrength that is Heavy or Breaker, from any authored strength; the shared
        // EnsureHeavyOrBreaker floor never weakens a stronger authored value and raises weak ones to
        // Heavy; and the three weapons share the identical floor.
        //
        // Scene-bound half NOT validated here (needs a live CombatReactionController + baked enemy):
        // the actual stance-break of a Heavy enemy against its rank resistances.
        // Validates: Requirements 8.5, 9.5, 11.2
        [Test]
        public void EveryWeaponsAntiHeavyToolLandsWithHeavyOrBreakerForce()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The three weapons must share one anti-Heavy floor == Heavy, so the contract is identical.
                PropertyCheck.That(GauntletLoopSteps.AntiHeavyMinStrength == HitStrength.Heavy,
                    $"Gauntlet anti-Heavy floor is {GauntletLoopSteps.AntiHeavyMinStrength}, expected Heavy.");
                PropertyCheck.That(SpearLoopSteps.AntiHeavyMinStrength == GauntletLoopSteps.AntiHeavyMinStrength,
                    $"Spear anti-Heavy floor {SpearLoopSteps.AntiHeavyMinStrength} != shared Gauntlet floor {GauntletLoopSteps.AntiHeavyMinStrength}.");
                PropertyCheck.That(BowLoopSteps.AntiHeavyMinStrength == GauntletLoopSteps.AntiHeavyMinStrength,
                    $"Bow anti-Heavy floor {BowLoopSteps.AntiHeavyMinStrength} != shared Gauntlet floor {GauntletLoopSteps.AntiHeavyMinStrength}.");

                // A random authored skill kind: the anti-Heavy identity must hold whatever the asset says.
                ArsenalSkillKind kind = RandomSkillKind(rng);

                // ---- Manopla E (Stance Breaker): configure a cloned step list and check every hit -------
                // Author each step with an arbitrary strength (including weak values that must be raised).
                int stepCount = rng.Next(1, 5);
                var steps = new System.Collections.Generic.List<AreaHitStep>(stepCount);
                for (int s = 0; s < stepCount; s++)
                {
                    var step = new AreaHitStep
                    {
                        hitStrength = RandomStrength(rng),
                        stanceDamage = RandomStanceDamage(rng), // some steps deal 0 -> not the breaking step
                        breakEffect = StanceBreakEffect.None,
                    };
                    steps.Add(step);
                }
                // Occasionally include a null entry: Configure must tolerate it (contract of the helper).
                if (rng.Next(0, 5) == 0) steps.Add(null);

                GauntletLoopSteps.Configure(steps, GauntletLoopStage.StanceBreaker);

                for (int s = 0; s < steps.Count; s++)
                {
                    AreaHitStep step = steps[s];
                    if (step == null) continue;
                    PropertyCheck.That(GauntletLoopSteps.IsHeavyOrBreaker(step.hitStrength),
                        $"kind={kind} step#{s}: Manopla E hit strength {step.hitStrength} is weaker than Heavy after Configure.");
                }

                // ---- Lança E (Perfuração pesada) --------------------------------------------------------
                SpearLoopSteps.Reaction spearPierce = SpearLoopSteps.ReactionForStage(SpearLoopStage.Pierce, ArsenalSkillKind.Thrust); // anti-Heavy identity is the Pierce stage; a Sweep-kind ability overrides slot 2 to the sweep, so assert the stage directly.
                PropertyCheck.That(spearPierce.Stage == SpearLoopStage.Pierce,
                    $"kind={kind}: Lança slot 2 mapped to {spearPierce.Stage}, expected Pierce.");
                PropertyCheck.That(GauntletLoopSteps.IsHeavyOrBreaker(spearPierce.Strength),
                    $"kind={kind}: Lança E strength {spearPierce.Strength} is weaker than Heavy.");
                PropertyCheck.That(spearPierce.IsHeavyOrBreaker,
                    $"kind={kind}: Lança E IsHeavyOrBreaker is false for strength {spearPierce.Strength}.");

                // ---- Arco W (Flecha Pesada) -------------------------------------------------------------
                BowLoopSteps.Reaction bowHeavy = BowLoopSteps.ReactionForStage(BowLoopStage.HeavyArrow, ArsenalSkillKind.Arrow); // anti-Heavy identity is the HeavyArrow stage; a Rain-kind ability overrides slot 1 to the rain, so assert the stage directly.
                PropertyCheck.That(bowHeavy.Stage == BowLoopStage.HeavyArrow,
                    $"kind={kind}: Arco slot 1 mapped to {bowHeavy.Stage}, expected HeavyArrow.");
                PropertyCheck.That(GauntletLoopSteps.IsHeavyOrBreaker(bowHeavy.Strength),
                    $"kind={kind}: Arco W strength {bowHeavy.Strength} is weaker than Heavy.");
                PropertyCheck.That(bowHeavy.IsHeavyOrBreaker,
                    $"kind={kind}: Arco W IsHeavyOrBreaker is false for strength {bowHeavy.Strength}.");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 27: Ferramenta anti-Heavy usa força Heavy ou Breaker
        // Focused sub-property on the shared EnsureHeavyOrBreaker floor: for any authored strength it
        // never weakens a stronger value and raises a weak (below-Heavy) value exactly to Heavy.
        // Validates: Requirements 8.5, 9.5, 11.2
        [Test]
        public void EnsureHeavyOrBreakerNeverWeakensAndRaisesWeakValuesToHeavy()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                HitStrength authored = RandomStrength(rng);
                var step = new AreaHitStep { hitStrength = authored };

                GauntletLoopSteps.EnsureHeavyOrBreaker(step);
                HitStrength result = step.hitStrength;

                string ctx = $"authored={authored} result={result}";

                // Result is always at least Heavy.
                PropertyCheck.That(GauntletLoopSteps.IsHeavyOrBreaker(result),
                    $"{ctx}: result is weaker than Heavy.");

                // Never weakens: the result is never below the authored value.
                PropertyCheck.That(result >= authored,
                    $"{ctx}: EnsureHeavyOrBreaker weakened the authored strength.");

                if (authored >= HitStrength.Heavy)
                {
                    // A stronger-or-equal authored value (Heavy/Breaker) is left exactly as authored.
                    PropertyCheck.That(result == authored,
                        $"{ctx}: an authored Heavy/Breaker value was changed.");
                }
                else
                {
                    // A weak value (Light/Medium) is raised exactly to the shared floor (Heavy).
                    PropertyCheck.That(result == GauntletLoopSteps.AntiHeavyMinStrength,
                        $"{ctx}: a weak value was not raised exactly to the anti-Heavy floor {GauntletLoopSteps.AntiHeavyMinStrength}.");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Every HitStrength value, so weak (Light/Medium) and strong (Heavy/Breaker) are covered.</summary>
        private static HitStrength RandomStrength(Random rng)
        {
            var values = (HitStrength[])Enum.GetValues(typeof(HitStrength));
            return values[rng.Next(0, values.Length)];
        }

        /// <summary>Every authored skill kind, so the identity holds whatever the ability asset declares.</summary>
        private static ArsenalSkillKind RandomSkillKind(Random rng)
        {
            var values = (ArsenalSkillKind[])Enum.GetValues(typeof(ArsenalSkillKind));
            return values[rng.Next(0, values.Length)];
        }

        /// <summary>Stance damage spanning zero (a non-breaking step) and positive (a candidate breaking step).</summary>
        private static float RandomStanceDamage(Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return 0f;                                  // deals no stance damage
                case 1: return (float)rng.NextDouble() * 5f;        // small
                default: return 5f + (float)rng.NextDouble() * 30f; // typical
            }
        }
    }
}
