using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 1 of nexus-lobby-menu-restructure
    /// (resolução de ação do pedestal).
    ///
    /// Property 1 — <see cref="WeaponPedestalState.Resolve"/> retorna
    /// <see cref="PedestalAction.Equip"/> se-e-somente-se a arma está desbloqueada;
    /// <see cref="PedestalAction.Unlock"/> quando está bloqueada e o jogador pode pagar; e
    /// <see cref="PedestalAction.Denied"/> quando está bloqueada e não pode pagar. Em nenhum caso
    /// <see cref="PedestalAction.Unlock"/> é retornado sem moedas suficientes.
    ///
    /// Approach: <see cref="WeaponPedestalState"/> é uma classe pura (sem MonoBehaviour/cena),
    /// construída com valores injetados. O teste cobre os três índices de arma (0 Manopla,
    /// 1 Arco, 2 Lança) e todas as combinações de estado (desbloqueada × pode-pagar), forçando os
    /// quatro cantos determinísticos e sorteando o restante. O custo de desbloqueio é irrelevante
    /// para <see cref="WeaponPedestalState.Resolve"/> (a decisão vem do par bloqueio/pode-pagar),
    /// por isso é gerado aleatoriamente para garantir que a resolução independe dele.
    ///
    /// Validates: Requirements 3.1, 3.2, 3.3
    /// </summary>
    public sealed class PedestalActionResolutionPropertyTests
    {
        private const int WeaponCount = 3;

        // Feature: nexus-lobby-menu-restructure, Property 1: resolução de ação do pedestal
        // For every weapon index (0/1/2) and every (isUnlocked, canAfford) combination:
        //   - unlocked            -> Equip
        //   - locked   + canAfford -> Unlock
        //   - locked   + !canAfford -> Denied
        // and Resolve never returns Unlock without enough coins.
        // Validates: Requirements 3.1, 3.2, 3.3
        [Test]
        public void ResolveMapsLockAndAffordStateToTheExpectedAction()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Force the deterministic corners so every state combination is exercised, then
                // draw the rest uniformly. Corner layout (per weapon index):
                //   0 -> unlocked, canAfford      1 -> unlocked, !canAfford
                //   2 -> locked,   canAfford      3 -> locked,   !canAfford
                int weaponIndex;
                bool isUnlocked;
                bool canAfford;

                if (i < WeaponCount * 4)
                {
                    weaponIndex = i / 4;
                    int corner = i % 4;
                    isUnlocked = corner < 2;
                    canAfford = corner % 2 == 0;
                }
                else
                {
                    weaponIndex = rng.Next(0, WeaponCount);
                    isUnlocked = rng.Next(0, 2) == 0;
                    canAfford = rng.Next(0, 2) == 0;
                }

                // Unlock cost must not influence the resolved action; randomize it.
                int unlockCost = rng.Next(0, 100);
                var pedestal = new WeaponPedestalState(weaponIndex, isUnlocked, unlockCost);

                PedestalAction action = pedestal.Resolve(canAfford);

                string ctx =
                    $"index={weaponIndex}, isUnlocked={isUnlocked}, canAfford={canAfford}, cost={unlockCost}";

                // Equip iff unlocked.
                PropertyCheck.That((action == PedestalAction.Equip) == isUnlocked,
                    $"{ctx}: Equip must be returned iff the weapon is unlocked, got {action}");

                if (isUnlocked)
                {
                    PropertyCheck.That(action == PedestalAction.Equip,
                        $"{ctx}: unlocked pedestal must resolve to Equip, got {action}");
                }
                else if (canAfford)
                {
                    PropertyCheck.That(action == PedestalAction.Unlock,
                        $"{ctx}: locked + affordable must resolve to Unlock, got {action}");
                }
                else
                {
                    PropertyCheck.That(action == PedestalAction.Denied,
                        $"{ctx}: locked + not affordable must resolve to Denied, got {action}");
                }

                // Never unlock without enough coins.
                PropertyCheck.That(!(action == PedestalAction.Unlock && !canAfford),
                    $"{ctx}: Resolve must never return Unlock when the player cannot afford it");
            });
        }
    }
}
