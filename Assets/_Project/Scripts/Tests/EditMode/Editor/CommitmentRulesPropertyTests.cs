using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure <see cref="CommitmentRules"/> helper of
    /// combat-foundation-rework (task 3.2), covering the resolution/warning guarantee of R3.10.
    ///
    /// <para>
    /// The properties are checked against the real production <see cref="CommitmentRules"/> type
    /// (not a re-stated model), driven by the project's seeded <see cref="PropertyCheck"/> harness
    /// over <see cref="PropertyCheck.DefaultCases"/> (>= 100) deterministic generated cases, since no
    /// FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>For a randomly chosen <see cref="CommitmentCategory"/>? (sometimes null, sometimes each
    /// of the three concrete values), <see cref="CommitmentRules.Resolve"/> returns the declared
    /// value when present and sets <c>emitWarning=false</c>; returns <see cref="CommitmentRules.Default"/>
    /// (<see cref="CommitmentCategory.Committed"/>) when absent and sets <c>emitWarning=true</c> — so
    /// the warning is emitted <em>if and only if</em> the category was absent (R3.10).</item>
    /// <item><see cref="CommitmentRules.ClampMovementFraction"/> stays within the closed interval
    /// <c>[0, 1]</c> for arbitrary inputs, including out-of-range, NaN and infinite values (R3.2).</item>
    /// </list>
    /// </summary>
    // Feature: combat-foundation-rework, task 3.2. Requirements: R3.10, R3.2.
    public sealed class CommitmentRulesPropertyTests
    {
        private static readonly CommitmentCategory[] AllCategories =
        {
            CommitmentCategory.Fluid,
            CommitmentCategory.Committed,
            CommitmentCategory.Channel
        };

        // Draws a nullable category that samples the whole input space of Resolve: roughly a quarter
        // of the time null (no category authored), otherwise each of the three concrete values.
        private static CommitmentCategory? GenDeclared(Random rng)
        {
            int pick = rng.Next(0, 4);
            if (pick == 3)
            {
                return null;
            }

            return AllCategories[pick];
        }

        // Draws a float spanning the interesting input space for the clamp: mostly values in and
        // just outside [0, 1], plus the special floats (NaN, +/-Infinity) that must not break the
        // [0, 1] invariant.
        private static float GenBoundary(Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return float.NaN;
                case 1: return float.PositiveInfinity;
                case 2: return float.NegativeInfinity;
                case 3: return (float)(rng.NextDouble() * 4.0 - 2.0); // [-2, 2): straddles the range
                case 4: return (float)(-rng.NextDouble() * 100.0);    // clearly below 0
                case 5: return (float)(rng.NextDouble() * 100.0 + 1.0); // clearly above 1
                default: return (float)rng.NextDouble();              // in-range [0, 1)
            }
        }

        // Feature: combat-foundation-rework, Property 16: Resolve returns the declared category when
        // present (emitWarning=false), returns Committed when absent (emitWarning=true); the warning
        // is emitted if and only if the category was absent.
        // Validates: Requirements 3.10
        [Test]
        public void ResolveReturnsDeclaredElseCommittedAndWarnsIffAbsent()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                CommitmentCategory? declared = GenDeclared(rng);

                CommitmentCategory resolved = CommitmentRules.Resolve(declared, out bool emitWarning);

                if (declared.HasValue)
                {
                    PropertyCheck.That(resolved == declared.Value,
                        $"declared={declared.Value}: Resolve returned {resolved}, expected the declared category.");
                    PropertyCheck.That(!emitWarning,
                        $"declared={declared.Value}: emitWarning was true but no category was absent.");
                }
                else
                {
                    PropertyCheck.That(resolved == CommitmentRules.Default,
                        $"declared=null: Resolve returned {resolved}, expected Default ({CommitmentRules.Default}).");
                    PropertyCheck.That(resolved == CommitmentCategory.Committed,
                        $"declared=null: Resolve returned {resolved}, expected Committed.");
                    PropertyCheck.That(emitWarning,
                        "declared=null: emitWarning was false but the category was absent.");
                }

                // The iff relation stated as a single biconditional: warning <=> absent.
                PropertyCheck.That(emitWarning == !declared.HasValue,
                    $"declared={(declared.HasValue ? declared.Value.ToString() : "null")}: "
                    + $"emitWarning={emitWarning} must equal 'category absent'={!declared.HasValue}.");
            });
        }

        // Feature: combat-foundation-rework, Property 16 (companion): ClampMovementFraction maps any
        // input — including out-of-range, NaN and infinite values — into the closed interval [0, 1].
        // Validates: Requirements 3.2
        [Test]
        public void ClampMovementFractionAlwaysStaysWithinUnitInterval()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = GenBoundary(rng);

                float clamped = CommitmentRules.ClampMovementFraction(input);

                PropertyCheck.That(clamped >= 0f && clamped <= 1f,
                    $"input={input}: ClampMovementFraction returned {clamped}, which is outside [0, 1].");
                PropertyCheck.That(!float.IsNaN(clamped),
                    $"input={input}: ClampMovementFraction returned NaN, which is outside [0, 1].");
            });
        }
    }
}
