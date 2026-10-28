using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class AsuraFinishControlTests
    {
        private static readonly KeyCode[] LegacyDefaults =
        {
            KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Space,
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R
        };

        [Test]
        public void FinishSequenceAppendsItsIdentityWithoutReorderingExistingControls()
        {
            var legacy = new[]
            {
                GameControl.Primary, GameControl.Move, GameControl.Dash,
                GameControl.Skill1, GameControl.Skill2, GameControl.Skill3, GameControl.Skill4
            };
            for (int i = 0; i < legacy.Length; i++)
                Assert.That((int)legacy[i], Is.EqualTo(i), legacy[i].ToString());
            Assert.That((int)GameControl.FinishSequence, Is.EqualTo(7));
        }

        [Test]
        public void DefaultsPreserveAllSevenOriginalControlsAndAddAnIndependentFinishKey()
        {
            var bindings = GamePreferences.DefaultBindings();
            Assert.That(bindings.Length, Is.EqualTo(8));
            Assert.That(bindings.Take(7), Is.EqualTo(LegacyDefaults));
            Assert.That(bindings[7], Is.EqualTo(KeyCode.F));
            Assert.That(bindings.All(GamePreferences.IsBindable), Is.True);
            Assert.That(bindings.Distinct().Count(), Is.EqualTo(bindings.Length));
            bindings[0] = KeyCode.Z;
            Assert.That(GamePreferences.DefaultBindings().Take(7), Is.EqualTo(LegacyDefaults),
                "Editing a defaults draft must not change another draft or a later reset.");
        }

        [TestCase(KeyCode.Z)]
        [TestCase(KeyCode.Mouse3)]
        [TestCase(KeyCode.K)]
        public void AFreeBindablePreferenceWinsBeforeFallbackCandidates(KeyCode preferred)
        {
            Assert.That(GamePreferences.ResolveFinishSequenceBinding(preferred, LegacyDefaults),
                Is.EqualTo(preferred));
        }

        [Test]
        public void AnExistingFBindingIsPreservedAndTheNewControlChoosesG()
        {
            var existing = (KeyCode[])LegacyDefaults.Clone();
            existing[2] = KeyCode.F;
            var before = (KeyCode[])existing.Clone();
            IReadOnlyList<KeyCode> readOnly = Array.AsReadOnly(existing);
            var resolved = GamePreferences.ResolveFinishSequenceBinding(KeyCode.F, readOnly);
            Assert.That(resolved, Is.EqualTo(KeyCode.G));
            Assert.That(existing, Is.EqualTo(before), "The resolver must not remap an existing action.");
            Assert.That(existing.Contains(resolved), Is.False);
        }

        [Test]
        public void AnOccupiedPreferredKeyFallsBackWithoutChangingTheOriginalOwner()
        {
            var existing = (KeyCode[])LegacyDefaults.Clone();
            existing[0] = KeyCode.K;
            var before = (KeyCode[])existing.Clone();
            Assert.That(GamePreferences.ResolveFinishSequenceBinding(KeyCode.K, existing),
                Is.EqualTo(KeyCode.F));
            Assert.That(existing, Is.EqualTo(before));
        }

        [TestCase(KeyCode.None)]
        [TestCase(KeyCode.Escape)]
        [TestCase((KeyCode)int.MaxValue)]
        [TestCase((KeyCode)(-1))]
        public void InvalidPreferencesChooseTheFirstFreeBindableFallback(KeyCode invalid)
        {
            Assert.That(GamePreferences.IsBindable(invalid), Is.False);
            var existing = (KeyCode[])LegacyDefaults.Clone();
            existing[0] = KeyCode.F;
            Assert.That(GamePreferences.ResolveFinishSequenceBinding(invalid, existing),
                Is.EqualTo(KeyCode.G));
        }

        [Test]
        public void FallbackOrderAlwaysLeavesOneUniqueChoiceForSevenExistingControls()
        {
            var candidates = new[]
            {
                KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.T,
                KeyCode.Y, KeyCode.U, KeyCode.J, KeyCode.K
            };
            for (int occupied = 0; occupied < candidates.Length; occupied++)
            {
                var existing = new KeyCode[7];
                for (int i = 0; i < existing.Length; i++)
                    existing[i] = i < occupied ? candidates[i] : LegacyDefaults[i];
                var before = (KeyCode[])existing.Clone();
                var resolved = GamePreferences.ResolveFinishSequenceBinding(KeyCode.None, existing);
                Assert.That(resolved, Is.EqualTo(candidates[occupied]),
                    "Incorrect fallback after " + occupied + " occupied candidates.");
                Assert.That(GamePreferences.IsBindable(resolved), Is.True);
                Assert.That(existing.Contains(resolved), Is.False);
                Assert.That(existing, Is.EqualTo(before));
            }
        }

        [Test]
        public void ResolutionIsDeterministicAndDoesNotDependOnExistingControlOrder()
        {
            var existing = new[]
            {
                KeyCode.T, KeyCode.F, KeyCode.H, KeyCode.G,
                KeyCode.Q, KeyCode.Mouse0, KeyCode.Space
            };
            var before = (KeyCode[])existing.Clone();
            var reverse = existing.Reverse().ToArray();
            for (int i = 0; i < 16; i++)
            {
                Assert.That(GamePreferences.ResolveFinishSequenceBinding(KeyCode.F, existing),
                    Is.EqualTo(KeyCode.Y));
                Assert.That(GamePreferences.ResolveFinishSequenceBinding(KeyCode.F, reverse),
                    Is.EqualTo(KeyCode.Y));
            }
            Assert.That(existing, Is.EqualTo(before));
            Assert.That(reverse, Is.EqualTo(before.Reverse()));
        }

        [Test]
        public void AnArtificiallyExhaustedFallbackSetReturnsNoneInsteadOfADuplicate()
        {
            var occupied = new[]
            {
                KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.T,
                KeyCode.Y, KeyCode.U, KeyCode.J, KeyCode.K
            };
            var before = (KeyCode[])occupied.Clone();
            Assert.That(GamePreferences.ResolveFinishSequenceBinding(KeyCode.None, occupied),
                Is.EqualTo(KeyCode.None));
            Assert.That(GamePreferences.ResolveFinishSequenceBinding(KeyCode.F, occupied),
                Is.EqualTo(KeyCode.None));
            Assert.That(occupied, Is.EqualTo(before));
        }

        [Test]
        public void AFreePreferenceStillWorksWhenAllFallbackCandidatesAreOccupied()
        {
            var occupied = new[]
            {
                KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.T,
                KeyCode.Y, KeyCode.U, KeyCode.J, KeyCode.K
            };
            Assert.That(GamePreferences.ResolveFinishSequenceBinding(KeyCode.Mouse4, occupied),
                Is.EqualTo(KeyCode.Mouse4));
        }
    }
}
