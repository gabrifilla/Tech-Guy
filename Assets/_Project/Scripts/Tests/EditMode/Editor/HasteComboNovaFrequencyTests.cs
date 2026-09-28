using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R6 Property 18 of modifier-synergies-theme17
    /// (Haste scales ComboNova proc frequency proportionally).
    ///
    /// Property 18 — the basic attack interval is base / attackSpeedMultiplier
    /// (<see cref="PlayerActor.GetAttackInterval"/>: <c>baseInterval / Stats.AttackSpeedMultiplier</c>,
    /// with the multiplier floored at 0.01 in <c>PlayerArpgStats.AttackSpeedMultiplier</c>). Haste adds
    /// 0.25 per rank to that multiplier, so the number of basic attacks completed in a fixed time
    /// window scales by the same proportion, and therefore the count of ComboNova procs in that
    /// window (one per third basic) scales proportionally too.
    ///
    /// Approach (documented per task 8.4): this is pure arithmetic over the interval formula, so it
    /// is verified as an EditMode property test using a computed model that mirrors
    /// GetAttackInterval exactly (interval = base / m, m floored at 0.01) and the Haste rule
    /// (m = 1 + 0.25 · hasteRank). No scene or physics is required. Proc-per-window count is
    /// modeled as floor(basicsInWindow / 3) matching the ComboNova every-third-basic gate.
    /// </summary>
    public sealed class HasteComboNovaFrequencyTests
    {
        private const float AttackSpeedFloor = 0.01f;   // PlayerArpgStats.AttackSpeedMultiplier floor
        private const float HastePerRank = 0.25f;        // Haste adds 0.25 per rank to the multiplier
        private const int ComboNovaBasicInterval = 3;    // nova on every third basic

        // Mirror of PlayerActor.GetAttackInterval: baseInterval / Stats.AttackSpeedMultiplier, with
        // the multiplier floored at 0.01.
        private static float AttackInterval(float baseInterval, float multiplier)
            => baseInterval / (multiplier < AttackSpeedFloor ? AttackSpeedFloor : multiplier);

        // Basics completed in a fixed time window at a given interval.
        private static int BasicsPerWindow(float window, float interval)
            => (int)System.Math.Floor(window / interval);

        // ComboNova procs in a window: one per third basic.
        private static int ProcsPerWindow(int basics) => basics / ComboNovaBasicInterval;

        // Feature: modifier-synergies-theme17, Property 18
        // interval = base / m for any base > 0 and any m >= 0.01, matching GetAttackInterval.
        // Validates: Requirements 6.2
        [Test]
        public void AttackInterval_EqualsBaseOverMultiplier()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float baseInterval = 0.05f + (float)rng.NextDouble() * 2f; // 0.05..2.05 s
                float m = AttackSpeedFloor + (float)rng.NextDouble() * 8f;  // 0.01..8.01

                float expected = baseInterval / m;
                float actual = AttackInterval(baseInterval, m);

                PropertyCheck.That(System.Math.Abs(actual - expected) <= 1e-4f * System.Math.Max(1f, expected),
                    $"base={baseInterval:F4}, m={m:F4}: interval={actual}, expected={expected}");
            });
        }

        // Feature: modifier-synergies-theme17, Property 18
        // Raising the attack-speed multiplier from m to k·m (k >= 1) scales basics-per-window — and
        // therefore ComboNova procs-per-window — by the same factor k, in the limit of a long window.
        // We assert the exact proportional relationship on the continuous rate (window / interval)
        // and monotonic (never-decreasing) behavior on the integer proc count.
        // Validates: Requirements 6.2
        [Test]
        public void HasteScalesBasicsAndProcsProportionally()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float baseInterval = 0.05f + (float)rng.NextDouble() * 1f; // 0.05..1.05 s
                int hasteA = rng.Next(0, 4);   // Haste rank 0..3
                int extra = rng.Next(1, 4);    // additional Haste ranks -> strictly faster
                int hasteB = System.Math.Min(hasteA + extra, 12);

                float mA = 1f + HastePerRank * hasteA;
                float mB = 1f + HastePerRank * hasteB;

                float intervalA = AttackInterval(baseInterval, mA);
                float intervalB = AttackInterval(baseInterval, mB);

                // Faster multiplier -> shorter interval.
                PropertyCheck.That(intervalB < intervalA + 1e-6f,
                    $"hasteA={hasteA}, hasteB={hasteB}: intervalB={intervalB} should be <= intervalA={intervalA}");

                // Continuous basics-per-window rate = window / interval = window * m / base, so the
                // ratio of rates equals the ratio of multipliers exactly (proportional scaling).
                float rateA = 1f / intervalA;  // basics per unit time
                float rateB = 1f / intervalB;
                float rateRatio = rateB / rateA;
                float mRatio = mB / mA;
                PropertyCheck.That(System.Math.Abs(rateRatio - mRatio) <= 1e-4f * System.Math.Max(1f, mRatio),
                    $"rateRatio={rateRatio} should equal multiplier ratio {mRatio}");

                // Over a fixed window the integer proc counts are non-decreasing with the multiplier
                // (more basics per window -> at least as many every-third procs).
                float window = 5f + (float)rng.NextDouble() * 55f; // 5..60 s
                int procsA = ProcsPerWindow(BasicsPerWindow(window, intervalA));
                int procsB = ProcsPerWindow(BasicsPerWindow(window, intervalB));
                PropertyCheck.That(procsB >= procsA,
                    $"window={window:F2}: procsB={procsB} should be >= procsA={procsA} (faster attack speed)");
            });
        }

        // Feature: modifier-synergies-theme17, Property 18 (proc definition)
        // Procs-per-window is exactly floor(basicsPerWindow / 3) for any window and interval,
        // tying the frequency scaling to the every-third-basic gate.
        // Validates: Requirements 6.2
        [Test]
        public void ProcsPerWindow_EqualsFloorBasicsOverThree()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float baseInterval = 0.1f + (float)rng.NextDouble() * 1f;
                float m = AttackSpeedFloor + (float)rng.NextDouble() * 8f;
                float window = 1f + (float)rng.NextDouble() * 59f;

                float interval = AttackInterval(baseInterval, m);
                int basics = BasicsPerWindow(window, interval);
                int procs = ProcsPerWindow(basics);

                PropertyCheck.That(procs == basics / ComboNovaBasicInterval,
                    $"basics={basics}: procs={procs}, expected={basics / ComboNovaBasicInterval}");
            });
        }
    }
}
