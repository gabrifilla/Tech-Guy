using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 22 of weapon-gameplay-swarm-rework: the Bow never applies
    /// soft grouping.
    ///
    /// The pure calculation core <see cref="SoftGroupingCalculator"/> was extracted from the thin
    /// <c>SoftGroupingService</c> MonoBehaviour so the R7.5 rule can be property-checked without a live
    /// Unity scene. The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed
    /// seeded harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per
    /// property (spanning arbitrary enemy/group positions, time steps and valid configurations) and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SoftGroupingBowNoGroupingPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 22: Arco não aplica agrupamento
        // Para todo estado em que o Arco está equipado, o agrupamento suave produz deslocamento zero em
        // todos os inimigos: AppliesTo(Bow) é false e ComputeDisplacement devolve Vector3.zero para
        // qualquer posição de inimigo/grupo, passo de tempo e configuração. Para contrastar, uma família
        // que não é o Arco (Gauntlet/Spear) pode deslocar o mesmo inimigo com as mesmas entradas.
        // Validates: Requirements 7.5
        [Test]
        public void BowEquippedProducesZeroSoftGroupingDisplacement()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The Bow never opts into soft grouping (R7.5).
                PropertyCheck.That(!SoftGroupingCalculator.AppliesTo(RunWeaponFamily.Bow),
                    "AppliesTo(RunWeaponFamily.Bow) must be false: the Bow never applies soft grouping");

                // Generate an arbitrary enemy position and a group centre within (and sometimes beyond)
                // the grouping radius, so cases exercise enemies both inside and outside the radius.
                Vector3 enemyPosition = RandomPosition(rng, 12f);
                Vector3 offset = RandomPosition(rng, SoftGroupingConfig.RadiusCap * 1.5f);
                Vector3 groupPoint = enemyPosition + offset;

                // Arbitrary time step, including non-positive values to cover the guard.
                float deltaTime = (float)(rng.NextDouble() * 0.5 - 0.05); // ~[-0.05, 0.45]

                // A valid, non-trivial config pinned within the R7.1 caps so grouping would otherwise move
                // an in-radius enemy (makes the Bow no-op meaningful rather than trivially satisfied).
                var config = new SoftGroupingConfig(
                    SoftGroupingConfig.MaxSpeedCap,
                    SoftGroupingConfig.RadiusCap,
                    SoftGroupingConfig.MaxDisplacementPerApplicationCap);

                Vector3 bowDisplacement = SoftGroupingCalculator.ComputeDisplacement(
                    RunWeaponFamily.Bow, enemyPosition, groupPoint, deltaTime, config);

                // Core property (R7.5): with the Bow equipped the displacement is exactly zero, for every
                // generated state.
                PropertyCheck.That(bowDisplacement == Vector3.zero,
                    $"Bow must produce zero soft-grouping displacement, got {bowDisplacement} for " +
                    $"enemy={enemyPosition}, group={groupPoint}, dt={deltaTime}");

                // Contrast: for a non-Bow family the same inputs may move the enemy. When the enemy is
                // inside the radius with a positive time step, a non-Bow family must produce a strictly
                // non-zero displacement — proving the Bow zero is a real suppression, not a dead config.
                RunWeaponFamily nonBow = rng.Next(0, 2) == 0 ? RunWeaponFamily.Gauntlet : RunWeaponFamily.Spear;
                PropertyCheck.That(SoftGroupingCalculator.AppliesTo(nonBow),
                    $"non-Bow family {nonBow} must opt into soft grouping");

                Vector3 nonBowDisplacement = SoftGroupingCalculator.ComputeDisplacement(
                    nonBow, enemyPosition, groupPoint, deltaTime, config);

                float distance = (groupPoint - enemyPosition).magnitude;
                bool movable = deltaTime > 0f && distance > 0f && distance <= config.Radius;
                if (movable)
                {
                    PropertyCheck.That(nonBowDisplacement != Vector3.zero,
                        $"a non-Bow family ({nonBow}) must move an in-radius enemy with dt>0, but the " +
                        $"displacement was zero for enemy={enemyPosition}, group={groupPoint}, dt={deltaTime}");
                }
            });
        }

        private static Vector3 RandomPosition(System.Random rng, float extent)
        {
            return new Vector3(
                (float)(rng.NextDouble() * 2.0 - 1.0) * extent,
                (float)(rng.NextDouble() * 2.0 - 1.0) * extent,
                (float)(rng.NextDouble() * 2.0 - 1.0) * extent);
        }
    }
}
