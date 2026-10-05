using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>Phase 3 deliberately replaces the old opt-in conversion with bounded amplification only.</summary>
    public sealed class SeismicFistPropertyTests
    {
        [Serializable] private struct Overlay { public AreaHitStep[] _hitSteps; }
        [Test]
        public void SeismicAmplifiesOnlyAuthorizedKnockbackPreservingLaunchRAndSource()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var source = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
                try
                {
                    var effect = (StanceBreakEffect)rng.Next(0, 4);
                    float distance = 2.01f + (float)rng.NextDouble() * 5.99f;
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Overlay { _hitSteps = new[] {
                        new AreaHitStep { breakEffect = effect, knockbackDistance = distance,
                            knockUpHeight = 2f, pushDistance = .35f, stanceDamage = 12f } } }), source);
                    string original = JsonUtility.ToJson(source);
                    for (int slot = 0; slot <= 3; slot++) for (int rank = 0; rank <= 3; rank++)
                    {
                        var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                        foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.SeismicFist)
                            for (int r = 0; r < rank; r++) mods.Add(def);
                        var step = mods.GauntletSteps(source, slot)[0];
                        float expected = slot != 3 && effect == StanceBreakEffect.Knockback
                            ? Mathf.Min(8f, distance * (1f + .3f * rank)) : distance;
                        PropertyCheck.That(step.breakEffect == effect, $"case {i}, slot {slot}, rank {rank}: effect changed");
                        PropertyCheck.That(Mathf.Abs(expected - step.knockbackDistance) < .0001f,
                            $"case {i}, slot {slot}, rank {rank}: distance {step.knockbackDistance}, expected {expected}");
                        PropertyCheck.That(step.knockUpHeight == 2f && step.pushDistance == .35f && step.stanceDamage == 12f,
                            $"case {i}, slot {slot}, rank {rank}: unrelated reaction fields changed");
                        PropertyCheck.That(JsonUtility.ToJson(source) == original, $"case {i}: source mutated");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(source); }
            });
        }
    }
}
