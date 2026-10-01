using System;
using System.Reflection;
using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 12 of gauntlet-boon-playstyle-overhaul
    /// (Combo faminto reduz só recargas ativas, clampado).
    ///
    /// Property 12 (design): a call to <see cref="AbilityHolder.ReduceCooldowns(float)"/> with a
    /// positive <c>seconds</c> SHALL lower only the slots currently in the <c>Cooldown</c> state by
    /// exactly that amount, clamped so no live timer ever drops below zero, while leaving every
    /// <c>Ready</c>/<c>Active</c> slot's timer untouched and never reading or writing the source
    /// ability asset's authored <see cref="Ability.cooldownTime"/> (Requirements 6.1, 6.3).
    ///
    /// Approach: <c>ReduceCooldowns</c> acts on the live, run-scoped <c>cooldownTimers</c>/<c>states</c>
    /// arrays that <c>AbilityHolder</c> rebuilds from the equipped weapon. Those arrays are private and
    /// the state enum is a private nested type, so — mirroring the inactive-host rigs in
    /// <see cref="ElementalOverflowGatePropertyTests"/> and the reflection seams used across the EditMode
    /// suite — the test builds the holder on an inactive GameObject (no <c>Awake</c>/scene lifecycle) and
    /// seeds the three parallel arrays by reflection before exercising the real method. The nested
    /// <c>AbilityState</c> enum (Ready=0, Active=1, Cooldown=2) is resolved by name so the test maps
    /// states without hard-coding the private type.
    ///
    /// Each generated case builds a random mix of Ready/Active/Cooldown slots with random timers,
    /// records the authored <c>cooldownTime</c> of each source asset, calls the method, and asserts:
    ///   • every Cooldown slot's live timer == max(0, before - seconds);
    ///   • every Ready/Active slot's live timer is byte-identical to before;
    ///   • no live timer is negative;
    ///   • every source asset's <c>cooldownTime</c> is unchanged.
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    public sealed class ReduceCooldownsPropertyTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

        // The private nested AbilityHolder.AbilityState enum (Ready=0, Active=1, Cooldown=2), resolved by
        // name so the test does not hard-code the inaccessible type.
        private static readonly Type StateType =
            typeof(AbilityHolder).GetNestedType("AbilityState", BindingFlags.NonPublic);

        private static object State(string name) => Enum.Parse(StateType, name);

        private static FieldInfo Field(string name)
        {
            FieldInfo field = typeof(AbilityHolder).GetField(name, Instance);
            Assert.IsNotNull(field, $"Expected private field '{name}' on AbilityHolder (test seam).");
            return field;
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 12: Combo faminto reduz só recargas ativas, clampado.
        // For any positive reduction, ReduceCooldowns lowers only Cooldown slots by that many seconds
        // (clamped at zero), leaves Ready/Active slots and the authored source asset untouched.
        // Validates: Requirements 6.1, 6.3
        [Test]
        public void ReduceCooldownsLowersOnlyActiveCooldownsClampedAndLeavesTheRestUntouched()
        {
            Assert.IsNotNull(StateType, "Could not resolve the private AbilityHolder.AbilityState enum.");

            PropertyCheck.ForAll((rng, i) =>
            {
                int slots = rng.Next(1, 7);                         // 1..6 ability slots
                float seconds = 0.01f + (float)rng.NextDouble() * 5f; // strictly positive reduction

                var abilities = new Ability[slots];
                var cooldownTimers = new float[slots];
                var activeTimers = new float[slots];
                var states = Array.CreateInstance(StateType, slots);

                var authoredCooldown = new float[slots];
                var expectedTimer = new float[slots];
                var wasCooldown = new bool[slots];

                for (int s = 0; s < slots; s++)
                {
                    Ability ability = ScriptableObject.CreateInstance<Ability>();
                    ability.name = "ReduceCooldownsSource_" + i + "_" + s;
                    // Authored cooldown the method must never read or write.
                    ability.cooldownTime = 1f + (float)rng.NextDouble() * 20f;
                    authoredCooldown[s] = ability.cooldownTime;
                    abilities[s] = ability;

                    float timer = (float)rng.NextDouble() * 10f;     // live remaining in [0, 10)
                    cooldownTimers[s] = timer;
                    activeTimers[s] = (float)rng.NextDouble() * 3f;

                    // Spread states across Ready(0)/Active(1)/Cooldown(2); force each boundary at least
                    // once in the first cases so the mix always exercises all three.
                    int pick = i switch
                    {
                        0 => 2, // all Cooldown
                        1 => 0, // all Ready
                        2 => 1, // all Active
                        _ => rng.Next(0, 3)
                    };
                    states.SetValue(State(pick switch { 0 => "Ready", 1 => "Active", _ => "Cooldown" }), s);

                    bool cooldown = pick == 2;
                    wasCooldown[s] = cooldown;
                    expectedTimer[s] = cooldown ? Mathf.Max(0f, timer - seconds) : timer;
                }

                var host = new GameObject("ReduceCooldownsHost");
                host.SetActive(false); // suppress Awake/scene lifecycle; only the field graph is needed
                try
                {
                    AbilityHolder holder = host.AddComponent<AbilityHolder>();
                    Field("activeAbilities").SetValue(holder, abilities);
                    Field("cooldownTimers").SetValue(holder, cooldownTimers);
                    Field("activeTimers").SetValue(holder, activeTimers);
                    Field("states").SetValue(holder, states);

                    holder.ReduceCooldowns(seconds);

                    var after = (float[])Field("cooldownTimers").GetValue(holder);
                    for (int s = 0; s < slots; s++)
                    {
                        PropertyCheck.That(after[s] >= 0f,
                            $"slot {s}: live cooldown {after[s]} must never be negative (seconds={seconds})");
                        PropertyCheck.That(after[s] == expectedTimer[s],
                            wasCooldown[s]
                                ? $"slot {s} (Cooldown): expected max(0, before-seconds)={expectedTimer[s]}, got {after[s]} (seconds={seconds})"
                                : $"slot {s} (Ready/Active): timer must be untouched ({expectedTimer[s]}), got {after[s]}");
                        PropertyCheck.That(abilities[s].cooldownTime == authoredCooldown[s],
                            $"slot {s}: source asset cooldownTime changed from {authoredCooldown[s]} to {abilities[s].cooldownTime}");
                    }
                }
                finally
                {
                    for (int s = 0; s < slots; s++)
                        if (abilities[s]) UnityEngine.Object.DestroyImmediate(abilities[s]);
                    if (host) UnityEngine.Object.DestroyImmediate(host);
                }
            });
        }
    }
}
