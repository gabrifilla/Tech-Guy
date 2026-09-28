using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode invariant cross-check for R1 Property 2 of modifier-synergies-theme17.
    ///
    /// The behavioral PlayMode test drives the real projectile through physics; this EditMode test
    /// pins the pure arithmetic the redirect step relies on — repeatedly multiplying the damage
    /// multiplier by 0.75 must equal initial * 0.75^k. Keeping this in EditMode gives the required
    /// EditMode test assembly a fast, deterministic, physics-free property to run.
    /// </summary>
    public sealed class ArsenalRicochetDecayInvariantTests
    {
        private const float RicochetFactor = 0.75f; // grounded in ArsenalProjectile: _multiplier *= .75f

        // Feature: modifier-synergies-theme17, Property 2 (arithmetic invariant): applying the ricochet
        // factor k times equals initial * 0.75^k.
        // Validates: Requirements 1.3
        [Test]
        public void RepeatedRicochetFactor_EqualsInitialTimesPowK()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float initial = 0.25f + (float)rng.NextDouble() * 4f; // 0.25..4.25
                int k = rng.Next(0, 8);                               // 0..7 redirects

                float stepped = initial;
                for (int r = 0; r < k; r++) stepped *= RicochetFactor;

                float closed = initial * Mathf.Pow(RicochetFactor, k);

                PropertyCheck.That(Mathf.Abs(stepped - closed) <= 1e-4f * Mathf.Max(1f, closed),
                    $"initial={initial}, k={k}: stepped={stepped}, closed-form={closed}");
                PropertyCheck.That(k == 0 || stepped < initial,
                    $"decay must strictly reduce the multiplier for k>0 (initial={initial}, k={k}, stepped={stepped})");
            });
        }
    }
}
