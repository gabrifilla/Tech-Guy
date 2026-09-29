using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the additional-displacement suppression rule enforced by the pure,
    /// shared gate <see cref="ControlLockGate"/> — task 3.12 of weapon-gameplay-swarm-rework.
    ///
    /// <see cref="ControlLockGate"/> is a static, scene-free predicate, so the "displacement suppressed
    /// while control-locked" invariant (Requisito 4.5) can be property-checked without a live Unity
    /// scene. This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed
    /// seeded harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per
    /// property (generating every lock state × displacement tier combination) and reports the exact
    /// failing case as a counterexample.
    ///
    /// NOTE: This is intentionally a distinct file/class from the pre-existing
    /// <c>ControlLockGatePropertyTests</c> (which belongs to the enemy-swarm-core-archetypes spec and
    /// tests <see cref="ArchetypeActionGate"/>); the two do not overlap.
    /// </summary>
    public sealed class ControlLockGateDisplacementPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 14: Deslocamento suprimido durante travamento de controle
        // Para todo inimigo com controle travado (atordoado ou no ar), Micro_Displacement e Push
        // adicionais são suprimidos; quando o travamento termina, acertos subsequentes voltam a
        // permitir Micro_Displacement e Push. Launch (hard CC) não é uma reação imediata e nunca é
        // regido por este gate.
        // Validates: Requirements 4.5
        [Test]
        public void MicroAndPushSuppressedWhileLockedThenAllowedWhenLockEnds()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate the full space: control-lock state × displacement tier.
                bool controlLocked = rng.Next(0, 2) == 0;
                DisplacementTierKind tier = (DisplacementTierKind)rng.Next(0, 3); // Micro, Push, Launch

                bool allowed = ControlLockGate.AllowsDisplacement(controlLocked, tier);

                string state = $"[controlLocked={controlLocked} tier={tier}]";

                // R4.5 reference rule: while locked, only Launch (hard CC) may still displace; the
                // additional Micro/Push nudge is suppressed. While unlocked, every tier is allowed.
                bool expected = !controlLocked || tier == DisplacementTierKind.Launch;
                PropertyCheck.That(allowed == expected,
                    $"AllowsDisplacement mismatch for {state}: expected {expected}, got {allowed}");

                if (controlLocked)
                {
                    // Micro and Push additional displacement are suppressed while control-locked.
                    if (tier == DisplacementTierKind.Micro || tier == DisplacementTierKind.Push)
                    {
                        PropertyCheck.That(!allowed,
                            $"additional {tier} displacement must be suppressed while control-locked for {state}");
                    }

                    // Launch (hard CC) is a separate channel and is not governed by this gate.
                    if (tier == DisplacementTierKind.Launch)
                    {
                        PropertyCheck.That(allowed,
                            $"Launch (hard CC) must not be suppressed by the control-lock gate for {state}");
                    }
                }
                else
                {
                    // When the lock ends, subsequent hits again allow Micro/Push (and Launch).
                    PropertyCheck.That(allowed,
                        $"an unlocked enemy must allow displacement of every tier for {state}");
                }

                // The convenience overload models an immediate reaction, which only ever produces a
                // Micro or Push nudge (never a Launch): it must allow displacement exactly when NOT
                // control-locked, matching AllowsDisplacement for the Micro/Push tiers.
                bool immediateAllowed = ControlLockGate.AllowsImmediateDisplacement(controlLocked);
                PropertyCheck.That(immediateAllowed == !controlLocked,
                    $"AllowsImmediateDisplacement must be true iff not control-locked for {state}");
                PropertyCheck.That(
                    immediateAllowed == ControlLockGate.AllowsDisplacement(controlLocked, DisplacementTierKind.Micro),
                    $"the immediate-reaction overload must agree with the Micro tier for {state}");
                PropertyCheck.That(
                    immediateAllowed == ControlLockGate.AllowsDisplacement(controlLocked, DisplacementTierKind.Push),
                    $"the immediate-reaction overload must agree with the Push tier for {state}");

                // Lock lifecycle: the SAME tier that was suppressed under lock is allowed once the lock
                // ends (subsequent hits recover Micro_Displacement and Push).
                bool allowedWhenUnlocked = ControlLockGate.AllowsDisplacement(false, tier);
                PropertyCheck.That(allowedWhenUnlocked,
                    $"when the lock ends, tier {tier} must be allowed again for {state}");
            });
        }
    }
}
