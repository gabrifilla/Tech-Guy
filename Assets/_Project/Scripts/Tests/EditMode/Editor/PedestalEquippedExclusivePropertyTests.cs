using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 2 of nexus-lobby-menu-restructure
    /// (estado "equipada" exclusivo).
    ///
    /// Property 2 — dado um índice equipado, exatamente um pedestal reporta
    /// <see cref="WeaponPedestalState.IsEquipped"/> == true, e os demais false. Os pedestais cobrem
    /// os três índices de arma do <see cref="WeaponLoadout"/> (0 Manopla, 1 Arco, 2 Lança), então a
    /// seleção de qualquer índice válido deixa exatamente um pedestal marcado como equipado.
    ///
    /// Approach: <see cref="WeaponPedestalState"/> é uma classe pura (sem MonoBehaviour/cena),
    /// construída com valores injetados. Os valores de bloqueio/custo são irrelevantes para
    /// <see cref="WeaponPedestalState.IsEquipped"/>, portanto são gerados aleatoriamente para
    /// garantir que a exclusividade do estado "equipada" independe deles.
    ///
    /// Validates: Requirements 2.5, 3.4
    /// </summary>
    public sealed class PedestalEquippedExclusivePropertyTests
    {
        private const int WeaponCount = 3;

        // Feature: nexus-lobby-menu-restructure, Property 2: estado "equipada" exclusivo
        // For any equipped index over the three pedestals (0/1/2), exactly one pedestal reports
        // IsEquipped == true and the others false, regardless of lock/cost state.
        // Validates: Requirements 2.5, 3.4
        [Test]
        public void ExactlyOnePedestalReportsEquippedForTheEquippedIndex()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Build the three pedestals (indices 0/1/2). Lock/cost are randomized because they
                // must not affect IsEquipped exclusivity.
                var pedestals = new WeaponPedestalState[WeaponCount];
                for (int idx = 0; idx < WeaponCount; idx++)
                {
                    bool isUnlocked = idx == 0 || rng.Next(0, 2) == 0;
                    int unlockCost = rng.Next(0, 100);
                    pedestals[idx] = new WeaponPedestalState(idx, isUnlocked, unlockCost);
                }

                // Equip every valid index at least once; the boundaries 0/1/2 are forced and the
                // remaining cases are drawn uniformly.
                int equippedIndex = i < WeaponCount ? i : rng.Next(0, WeaponCount);

                int equippedCount = 0;
                for (int idx = 0; idx < WeaponCount; idx++)
                {
                    bool equipped = pedestals[idx].IsEquipped(equippedIndex);
                    if (idx == equippedIndex)
                    {
                        PropertyCheck.That(equipped,
                            $"equippedIndex={equippedIndex}: pedestal {idx} must report IsEquipped=true");
                    }
                    else
                    {
                        PropertyCheck.That(!equipped,
                            $"equippedIndex={equippedIndex}: pedestal {idx} must report IsEquipped=false");
                    }

                    if (equipped) equippedCount++;
                }

                PropertyCheck.That(equippedCount == 1,
                    $"equippedIndex={equippedIndex}: exactly one pedestal must be equipped, got {equippedCount}");
            });
        }

        // A no-pedestal-matches case: if the equipped index falls outside the three pedestals
        // (e.g. a stale/invalid selection), no pedestal reports IsEquipped, so the card never shows
        // a false "equipada" state on any pedestal.
        // Validates: Requirements 2.5, 3.4
        [Test]
        public void NoPedestalReportsEquippedForAnOutOfRangeIndex()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var pedestals = new WeaponPedestalState[WeaponCount];
                for (int idx = 0; idx < WeaponCount; idx++)
                {
                    pedestals[idx] = new WeaponPedestalState(idx, rng.Next(0, 2) == 0, rng.Next(0, 100));
                }

                // Generate an index outside [0; WeaponCount): negatives and values >= WeaponCount.
                int equippedIndex = rng.Next(0, 2) == 0
                    ? -1 - rng.Next(0, 5)
                    : WeaponCount + rng.Next(0, 5);

                int equippedCount = 0;
                for (int idx = 0; idx < WeaponCount; idx++)
                {
                    if (pedestals[idx].IsEquipped(equippedIndex)) equippedCount++;
                }

                PropertyCheck.That(equippedCount == 0,
                    $"equippedIndex={equippedIndex} (out of range): no pedestal must report equipped, got {equippedCount}");
            });
        }
    }
}
