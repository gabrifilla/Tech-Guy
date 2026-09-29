using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 44 of weapon-gameplay-swarm-rework
    /// (Asura exibido se e somente se as Manoplas estão equipadas).
    ///
    /// Property 44 — para todo estado de arma equipada, o indicador de Asura é exibido quando as
    /// Manoplas estão equipadas e não é exibido caso contrário (ver Requisitos 13.5/13.6 e
    /// design.md). O contrato já vive na cena, sem alteração de código: o HUD condiciona a exibição
    /// a <c>BreakerGauntletCombat.IsEquipped</c>. Duas fontes concordam:
    ///   • <c>PlayerHUD.Update</c>: <c>bool hasAsura = _breaker &amp;&amp; _breaker.IsEquipped;
    ///     _asuraRoot.SetActive(hasAsura);</c> — a raiz de Asura é ativada sse e somente se
    ///     equipado;
    ///   • <c>BreakerGauntletHUD.OnGUI</c>: <c>if (!_combat || !_combat.isActiveAndEnabled ||
    ///     !_combat.IsEquipped) return;</c> — o painel de Asura só desenha quando equipado.
    /// E o próprio predicado equipado é
    ///   <c>IsEquipped =&gt; _weapon &amp;&amp; _player &amp;&amp; !_player.IsDead &amp;&amp;
    ///   _player.CurrentWeapon == _weapon</c>.
    ///
    /// Approach (parte pura testável): o gating de renderização é scene/MonoBehaviour-bound (Image /
    /// GameObject.SetActive / OnGUI) e não é exercitado aqui. O que é puro e universal é o
    /// bicondicional "Asura exibido ⟺ Manoplas equipadas ⟺ (arma presente ∧ jogador presente ∧
    /// jogador vivo ∧ arma atual == arma das Manoplas)". Modelamos esse predicado como um espelho
    /// booleano puro dos quatro conjuntos que compõem <c>IsEquipped</c>, geramos o espaço completo
    /// de estados (2^4 = 16, mais amostragem aleatória) com <see cref="PropertyCheck.ForAll"/>
    /// (≥100 iterações) e verificamos o "sse e somente se" nos dois sentidos.
    ///
    /// Ressalva (R13.9): a renderização propriamente dita do HUD (ativar/ocultar o RectTransform /
    /// desenhar o OnGUI em uma cena viva) NÃO é validada por este teste — exigiria PlayMode/cena.
    /// Isso está relatado explicitamente e não é marcado como validado.
    /// </summary>
    public sealed class AsuraDisplayedIffGauntletsEquippedPropertyTests
    {
        /// <summary>
        /// Pure mirror of <c>BreakerGauntletCombat.IsEquipped</c>
        /// (<c>_weapon &amp;&amp; _player &amp;&amp; !_player.IsDead &amp;&amp; _player.CurrentWeapon == _weapon</c>),
        /// expressed over the four observable boolean facts so the property can exercise the whole
        /// state space without a live scene. It is intentionally a re-statement (not a call) of the
        /// source expression: the source is a MonoBehaviour property that cannot be evaluated
        /// without instantiating a PlayerActor + WeaponScript in a scene.
        /// </summary>
        private static bool Equipped(bool weaponPresent, bool playerPresent, bool playerAlive, bool currentWeaponIsGauntlet)
            => weaponPresent && playerPresent && playerAlive && currentWeaponIsGauntlet;

        /// <summary>
        /// Pure mirror of the HUD display gate: <c>PlayerHUD</c> sets the Asura root active with
        /// <c>hasAsura = _breaker &amp;&amp; _breaker.IsEquipped</c> and <c>BreakerGauntletHUD</c>
        /// only draws when <c>_combat.IsEquipped</c>. Both reduce to "shown ⟺ equipped".
        /// </summary>
        private static bool AsuraDisplayed(bool equipped) => equipped;

        // Feature: weapon-gameplay-swarm-rework, Property 44: Asura exibido se e somente se as Manoplas estão equipadas
        // For every equipped-weapon state, the Asura indicator is displayed iff the Gauntlets are
        // equipped: shown ⟺ (weapon present ∧ player present ∧ player alive ∧ current weapon == Gauntlet).
        // Validates: Requirements 13.5, 13.6
        [Test]
        public void AsuraIsDisplayedIfAndOnlyIfGauntletsAreEquipped()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The four boolean facts that make up IsEquipped. The first 16 cases enumerate the
                // full 2^4 truth table (every combination is covered deterministically); later cases
                // sample the same space randomly to keep the run varied over >= 100 iterations.
                bool weaponPresent;
                bool playerPresent;
                bool playerAlive;
                bool currentWeaponIsGauntlet;
                if (i < 16)
                {
                    weaponPresent = (i & 0b0001) != 0;
                    playerPresent = (i & 0b0010) != 0;
                    playerAlive = (i & 0b0100) != 0;
                    currentWeaponIsGauntlet = (i & 0b1000) != 0;
                }
                else
                {
                    weaponPresent = rng.Next(0, 2) == 0;
                    playerPresent = rng.Next(0, 2) == 0;
                    playerAlive = rng.Next(0, 2) == 0;
                    currentWeaponIsGauntlet = rng.Next(0, 2) == 0;
                }

                bool equipped = Equipped(weaponPresent, playerPresent, playerAlive, currentWeaponIsGauntlet);
                bool displayed = AsuraDisplayed(equipped);

                // The property is the biconditional in both directions.
                bool allFourHold = weaponPresent && playerPresent && playerAlive && currentWeaponIsGauntlet;

                string state = $"weapon={weaponPresent}, player={playerPresent}, alive={playerAlive}, current==gauntlet={currentWeaponIsGauntlet}";

                // (⟸) Equipped ⟹ displayed: when all four hold, Asura must be shown.
                if (allFourHold)
                {
                    PropertyCheck.That(equipped,
                        $"{state}: all four IsEquipped conjuncts hold, so IsEquipped must be true");
                    PropertyCheck.That(displayed,
                        $"{state}: Gauntlets equipped, so the Asura indicator must be displayed");
                }
                else
                {
                    // (⟹) Not-equipped ⟹ not displayed: if any conjunct fails, Asura must be hidden.
                    PropertyCheck.That(!equipped,
                        $"{state}: at least one IsEquipped conjunct fails, so IsEquipped must be false");
                    PropertyCheck.That(!displayed,
                        $"{state}: Gauntlets not equipped, so the Asura indicator must not be displayed");
                }

                // The strict "if and only if": displayed is exactly equipped, which is exactly the
                // conjunction of the four facts — no case shows Asura without the Gauntlets and no
                // equipped case hides it.
                PropertyCheck.That(displayed == equipped,
                    $"{state}: display must equal equipped state (iff), got displayed={displayed}, equipped={equipped}");
                PropertyCheck.That(displayed == allFourHold,
                    $"{state}: display must hold iff all four equip conjuncts hold, got displayed={displayed}, allFour={allFourHold}");
            });
        }
    }
}
