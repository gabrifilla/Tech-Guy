using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the chase-approach destination logic in <c>EnemyAI.ChasePlayer</c> —
    /// task 8.4 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// Two edge cases from Requirement 13 are pinned here by example:
    ///  - R13.8: WHERE the Approach_Offset magnitude is configured to 0.0, the enemy chases directly
    ///    toward the player position (the existing beeline behavior).
    ///  - R13.6: IF applying the Approach_Offset would place the destination off the NavMesh, the chase
    ///    falls back to the player position for that refresh.
    ///
    /// <c>ChasePlayer</c> computes <c>desired = player.position + _approach.Tick(...)</c> and then issues
    /// <c>agent.SetDestination(hit.position)</c> only when both the offset is non-zero AND
    /// <see cref="UnityEngine.AI.NavMesh.SamplePosition"/> succeeds; otherwise it sets the destination to
    /// the raw <c>player.position</c>. A full exercise of <c>ChasePlayer</c> needs a live scene with a
    /// baked NavMesh and agent, which EditMode cannot set up without a baked surface. Per the design we
    /// therefore target where each R13.6/R13.8 decision actually lives:
    ///  - the pure <see cref="ApproachOffset"/> resolver, which returns exactly <see cref="Vector3.zero"/>
    ///    for a configured magnitude of 0 so <c>player.position + zero == player.position</c> (the
    ///    beeline, R13.8); and
    ///  - the destination-selection formula itself, reproduced here: a zero offset and the off-mesh
    ///    branch both resolve the chase destination to <c>player.position</c> (R13.6/R13.8). The
    ///    NavMesh-dependent <c>SamplePosition</c> success path is structurally the only other branch of
    ///    <c>ChasePlayer</c>'s <c>if/else</c> and is covered by live PlayMode validation in task 12.
    /// </summary>
    public sealed class ApproachOffsetChaseExampleTests
    {
        // Mirrors ChasePlayer's branch selection: SetDestination(hit.position) is issued only when the
        // offset is non-zero AND the sampled point is on the mesh; every other case (zero offset, or an
        // off-mesh sample) falls back to the raw player position. This pure helper lets the example
        // assert both R13.8 (zero offset) and R13.6 (off-mesh) without a baked NavMesh.
        private static Vector3 ResolveChaseDestination(Vector3 playerPosition, Vector3 offset, bool sampleOnMesh, Vector3 sampledPoint)
        {
            Vector3 desired = playerPosition + offset;
            if (offset != Vector3.zero && sampleOnMesh)
            {
                return sampledPoint; // hit.position
            }
            // Zero offset (beeline, R13.8) or an off-mesh sample (R13.6): chase the raw player position.
            return playerPosition;
        }

        // Validates: Requirements 13.6, 13.8
        // R13.8 (pure resolver): a configured Approach_Offset magnitude of 0 yields exactly Vector3.zero,
        // so the chase destination player.position + offset == player.position — the dead-straight beeline.
        [Test]
        public void MagnitudeZero_ProducesZeroOffset_SoChaseBeelinesToPlayer()
        {
            var approach = new ApproachOffset();
            var rng = new System.Random(12345);
            var player = new Vector3(7f, 0f, -3f);

            // Walk several ticks across refresh windows; magnitude 0 must stay a zero offset throughout.
            float[] dts = { 0.016f, 0.25f, 1.0f, 2.5f, 0.5f };
            foreach (float dt in dts)
            {
                Vector3 offset = approach.Tick(dt, 0f, 1.0f, rng);
                Assert.AreEqual(Vector3.zero, offset,
                    $"magnitude==0 must suppress the Approach_Offset to Vector3.zero (R13.8), got {offset}");

                Vector3 destination = ResolveChaseDestination(player, offset, sampleOnMesh: true, sampledPoint: new Vector3(99f, 0f, 99f));
                Assert.AreEqual(player, destination,
                    "with a zero offset the chase destination must equal the raw player position (beeline, R13.8)");
            }
        }

        // Validates: Requirement 13.8
        // A negative configured magnitude clamps to 0, so it behaves as the beeline too (defensive clamp).
        [Test]
        public void NegativeMagnitude_ClampsToZero_SoChaseBeelinesToPlayer()
        {
            var approach = new ApproachOffset();
            var rng = new System.Random(999);
            var player = new Vector3(-2f, 0f, 11f);

            Vector3 offset = approach.Tick(0.1f, -3f, 1.0f, rng);
            Assert.AreEqual(Vector3.zero, offset,
                "a magnitude below the floor must clamp to 0 and suppress the offset (R13.8)");

            Vector3 destination = ResolveChaseDestination(player, offset, sampleOnMesh: true, sampledPoint: Vector3.one * 50f);
            Assert.AreEqual(player, destination,
                "a clamped-to-zero magnitude must still beeline to the player (R13.8)");
        }

        // Validates: Requirement 13.6
        // When a non-zero Approach_Offset would place the destination off the NavMesh (SamplePosition
        // fails), ChasePlayer falls back to the raw player position for that refresh — never the off-mesh
        // desired point.
        [Test]
        public void NonZeroOffset_OffMesh_FallsBackToPlayerPosition()
        {
            var player = new Vector3(4f, 0f, 4f);
            var offset = new Vector3(1.5f, 0f, 0.5f); // a real bounded ground-plane offset

            Vector3 destination = ResolveChaseDestination(player, offset, sampleOnMesh: false, sampledPoint: Vector3.zero);

            Assert.AreEqual(player, destination,
                "an off-mesh Approach_Offset sample must fall back to the raw player position (R13.6)");
            Assert.AreNotEqual(player + offset, destination,
                "the chase must not steer toward the off-mesh desired point when SamplePosition fails (R13.6)");
        }

        // Validates: Requirement 13.6
        // Sanity contrast: when the offset is non-zero AND the sampled point is on the mesh, ChasePlayer
        // steers to the sampled (hit) position — not the raw player position. This fixes the branch the
        // off-mesh fallback is the complement of.
        [Test]
        public void NonZeroOffset_OnMesh_UsesSampledHitPosition()
        {
            var player = new Vector3(4f, 0f, 4f);
            var offset = new Vector3(1.5f, 0f, 0.5f);
            var hit = new Vector3(5.4f, 0f, 4.4f); // the on-mesh sampled point

            Vector3 destination = ResolveChaseDestination(player, offset, sampleOnMesh: true, sampledPoint: hit);

            Assert.AreEqual(hit, destination,
                "a non-zero offset with a valid on-mesh sample must steer to the sampled hit position (complement of R13.6)");
        }
    }
}
