using System;
using System.Text;

namespace TechGuy.Tests
{
    /// <summary>
    /// Minimal seeded property-check harness for the modifier-synergies-theme17 feature.
    ///
    /// Context: this project has no Assembly Definition around its gameplay code (everything lives
    /// in the predefined <c>Assembly-CSharp</c>), and no .NET property-based-testing package
    /// (FsCheck / CsCheck) could be added without a live Unity package resolve on this machine.
    /// This helper is the agreed fallback: it drives at least <see cref="DefaultCases"/> generated
    /// cases from a fixed seed so every run is deterministic and reproducible, and reports the
    /// exact failing case (like a PBT counterexample) when a property does not hold.
    ///
    /// It deliberately does NOT reference NUnit so it can live in <c>Assembly-CSharp</c> and be
    /// shared by both the EditMode (Assembly-CSharp-Editor) and PlayMode test assemblies. A failing
    /// property throws <see cref="PropertyViolationException"/>, which the NUnit runner surfaces as
    /// a failed test.
    /// </summary>
    public static class PropertyCheck
    {
        /// <summary>Default number of generated cases per property (spec requires >= 100).</summary>
        public const int DefaultCases = 128;

        /// <summary>Fixed base seed so property runs are reproducible.</summary>
        public const int DefaultSeed = 0x7EC17;

        /// <summary>
        /// Runs <paramref name="cases"/> deterministic iterations. For each iteration a fresh
        /// <see cref="Random"/> seeded from the base seed + iteration index is passed to
        /// <paramref name="body"/>. The body should generate its input from that Random and assert
        /// the property, throwing on violation. The failing iteration index and its seed are
        /// attached to the reported counterexample.
        /// </summary>
        public static void ForAll(Action<Random, int> body, int cases = DefaultCases, int seed = DefaultSeed)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            if (cases < 100) throw new ArgumentException("Property tests must run at least 100 generated cases.", nameof(cases));

            for (int i = 0; i < cases; i++)
            {
                int caseSeed = unchecked(seed * 31 + i);
                var rng = new Random(caseSeed);
                try
                {
                    body(rng, i);
                }
                catch (PropertyViolationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new PropertyViolationException(BuildFailure(i, caseSeed, ex.Message), ex);
                }
            }
        }

        /// <summary>Assertion used inside a property body; throws a counterexample-style failure.</summary>
        public static void That(bool condition, string message)
        {
            if (!condition) throw new PropertyViolationException(message);
        }

        private static string BuildFailure(int index, int caseSeed, string message)
        {
            var sb = new StringBuilder();
            sb.Append("Property falsified on generated case #").Append(index)
              .Append(" (caseSeed=").Append(caseSeed).Append("): ").Append(message);
            return sb.ToString();
        }
    }

    /// <summary>Raised when a property does not hold for a generated case (a counterexample).</summary>
    public sealed class PropertyViolationException : Exception
    {
        public PropertyViolationException(string message) : base(message) { }
        public PropertyViolationException(string message, Exception inner) : base(message, inner) { }
    }
}
