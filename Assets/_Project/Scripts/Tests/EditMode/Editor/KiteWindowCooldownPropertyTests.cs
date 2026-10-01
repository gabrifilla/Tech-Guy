using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure, scene-free <see cref="KiteController"/> — task 3.2 of
    /// ranged-kiting-and-attack-telegraph-overhaul (Property 7).
    ///
    /// The controller is the retreat window/cooldown state machine (design Diagram 2): from
    /// <see cref="KiteController.Phase.Eligible"/> a wants-to-kite frame begins
    /// <see cref="KiteController.Phase.Kiting"/>; once continuous kite time reaches the retreat
    /// window it enters <see cref="KiteController.Phase.Cooldown"/> and denies kiting for the
    /// cooldown duration before becoming eligible again; leaving the standoff band resets to
    /// eligible. Because the logic is a plain C# class driven by explicit
    /// (wantsToKite, dt, retreatWindow, retreatCooldown) inputs, this test drives the real
    /// <see cref="KiteController"/> directly — it does NOT touch a live Unity scene or <c>EnemyAI</c>.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per
    /// property (sampling windows, cooldowns, and frame deltas across and around each threshold)
    /// and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class KiteWindowCooldownPropertyTests
    {
        // Design Property 7 bounds: retreatWindow in [0.5, 30], retreatCooldown in [0.1, 30].
        private const float MinWindow = 0.5f;
        private const float MaxWindow = 30f;
        private const float MinCooldown = 0.1f;
        private const float MaxCooldown = 30f;

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 7
        // For any sequence of wants-to-kite frames with dt > 0, retreatWindow in [0.5,30], and
        // retreatCooldown in [0.1,30], KiteController.Tick permits kiting only while continuous kite
        // time < retreatWindow; upon reaching the window it enters Cooldown and denies kiting for
        // retreatCooldown, then becomes Eligible again; leaving the standoff band resets to Eligible.
        // Validates: Requirements 3.1, 3.2, 3.3, 3.4, 9.4
        [Test]
        public void RetreatWindowBoundsContinuousKitingAndCooldownBlocksThenReArms()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float retreatWindow = NextFloat(rng, MinWindow, MaxWindow);
                float retreatCooldown = NextFloat(rng, MinCooldown, MaxCooldown);
                // A positive, bounded frame delta so each threshold is approached in several steps.
                float dt = NextFloat(rng, 0.01f, 0.2f);

                string state = $"[retreatWindow={retreatWindow} retreatCooldown={retreatCooldown} dt={dt}]";

                var kite = new KiteController();

                // ---- Initial condition: Eligible, not yet kiting ----------------------------
                PropertyCheck.That(kite.Current == KiteController.Phase.Eligible,
                    $"controller must start Eligible for {state}");

                // ---- Window bounds continuous kiting (R3.1) ---------------------------------
                // Feed continuous wants-to-kite frames. The first frame from Eligible must permit
                // kiting; it must keep permitting while accrued kite time < window; once the accrued
                // time reaches the window the frame must deny and enter Cooldown exactly once.
                float accrued = 0f;
                bool deniedAtWindow = false;
                // Enough steps to comfortably exceed the max window (30s) at the min dt (0.01s).
                int kiteSteps = Mathf.CeilToInt(MaxWindow / 0.01f) + 2;
                for (int step = 0; step < kiteSteps && !deniedAtWindow; step++)
                {
                    bool permitted = kite.Tick(true, dt, retreatWindow, retreatCooldown);

                    if (step == 0)
                    {
                        // First kite frame from Eligible always permits and enters Kiting (R3.1).
                        PropertyCheck.That(permitted && kite.Current == KiteController.Phase.Kiting,
                            $"first wants-to-kite frame must permit kiting and enter Kiting for {state}");
                        continue;
                    }

                    // Mirror the controller's own accrual: time accrues on the frame before the check.
                    accrued += dt;

                    if (accrued < retreatWindow - 1e-4f)
                    {
                        // Still inside the window: must keep permitting and remain Kiting (R3.1).
                        PropertyCheck.That(permitted && kite.Current == KiteController.Phase.Kiting,
                            $"must keep kiting while accrued time ({accrued}) is below the window for {state}");
                    }
                    else if (accrued >= retreatWindow)
                    {
                        // Window reached: this frame must deny and enter Cooldown (R3.1/R3.2).
                        PropertyCheck.That(!permitted && kite.Current == KiteController.Phase.Cooldown,
                            $"must deny kiting and enter Cooldown once accrued time ({accrued}) reaches the window for {state}");
                        deniedAtWindow = true;
                    }
                }

                PropertyCheck.That(deniedAtWindow,
                    $"continuous kiting must be bounded and reach Cooldown for {state}");

                // ---- Cooldown blocks kiting then re-arms (R3.2/R3.3/R3.4) --------------------
                // While in Cooldown, continued wants-to-kite frames must be denied until the
                // cooldown elapses, at which point the controller becomes eligible again and
                // resumes kiting on that same frame.
                float cooldownAccrued = 0f;
                bool reArmed = false;
                int cooldownSteps = Mathf.CeilToInt(MaxCooldown / 0.01f) + 2;
                for (int step = 0; step < cooldownSteps && !reArmed; step++)
                {
                    cooldownAccrued += dt;
                    bool permitted = kite.Tick(true, dt, retreatWindow, retreatCooldown);

                    if (cooldownAccrued < retreatCooldown - 1e-4f)
                    {
                        // Still cooling down: kiting must stay blocked (R3.3).
                        PropertyCheck.That(!permitted && kite.Current == KiteController.Phase.Cooldown,
                            $"must block kiting while cooldown elapsed ({cooldownAccrued}) is below the cooldown for {state}");
                    }
                    else if (cooldownAccrued >= retreatCooldown)
                    {
                        // Cooldown elapsed: eligible again and resume kiting this frame (R3.4).
                        PropertyCheck.That(permitted && kite.Current == KiteController.Phase.Kiting,
                            $"must re-arm and resume kiting once cooldown ({cooldownAccrued}) elapses for {state}");
                        reArmed = true;
                    }
                }

                PropertyCheck.That(reArmed,
                    $"cooldown must elapse and re-arm kiting for {state}");

                // ---- Leaving the standoff band resets to Eligible (R2.4 support) ------------
                // A wants-to-kite == false frame must deny kiting and return to Eligible with a
                // zeroed kite timer, so the next detection begins a fresh window.
                bool permittedOnExit = kite.Tick(false, dt, retreatWindow, retreatCooldown);
                PropertyCheck.That(!permittedOnExit && kite.Current == KiteController.Phase.Eligible,
                    $"leaving the standoff band must deny kiting and reset to Eligible for {state}");

                // The next wants-to-kite frame must begin a fresh window (permit + Kiting).
                bool permittedAfterReset = kite.Tick(true, dt, retreatWindow, retreatCooldown);
                PropertyCheck.That(permittedAfterReset && kite.Current == KiteController.Phase.Kiting,
                    $"a fresh window must begin after leaving and re-entering the standoff band for {state}");
            });
        }

        /// <summary>
        /// Determinism: identical (wantsToKite, dt, retreatWindow, retreatCooldown) sequences
        /// produce an identical permit/phase trace across two independent controllers (R9.4).
        /// </summary>
        [Test]
        public void KiteControllerIsDeterministicForIdenticalInputSequences()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float retreatWindow = NextFloat(rng, MinWindow, MaxWindow);
                float retreatCooldown = NextFloat(rng, MinCooldown, MaxCooldown);

                var a = new KiteController();
                var b = new KiteController();

                int frames = 60 + rng.Next(0, 80);
                for (int f = 0; f < frames; f++)
                {
                    // Bias toward wanting to kite so windows/cooldowns are actually exercised.
                    bool wantsToKite = rng.Next(0, 4) != 0;
                    float dt = NextFloat(rng, 0.01f, 0.25f);

                    bool ra = a.Tick(wantsToKite, dt, retreatWindow, retreatCooldown);
                    bool rb = b.Tick(wantsToKite, dt, retreatWindow, retreatCooldown);

                    PropertyCheck.That(ra == rb && a.Current == b.Current,
                        $"identical input sequences must produce identical permit/phase traces at frame {f} " +
                        $"[retreatWindow={retreatWindow} retreatCooldown={retreatCooldown} wantsToKite={wantsToKite} dt={dt}]");
                }
            });
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
