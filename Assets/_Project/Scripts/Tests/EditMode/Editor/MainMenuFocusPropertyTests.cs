using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 10 of nexus-lobby-menu-restructure — consistent menu focus.
    ///
    /// Property 10 (design): under any keyboard/mouse navigation of the Home menu, exactly one option
    /// is focused, and activating it triggers that option's action (parity with the previous inline
    /// behavior). <b>Validates: Requirements 6.3, 6.5.</b>
    ///
    /// The focus/activation logic was extracted from the <see cref="MainMenuUI"/> MonoBehaviour into the
    /// pure <see cref="MainMenuFocus"/> class (no Unity types, no IMGUI), so this exercises it directly
    /// without a scene: a random sequence of navigation steps (down/up arrows and mouse "point at" moves,
    /// including out-of-range pointer indices) is applied, then the focused index and the action it
    /// activates are checked. A mirrored reference model reproduces the original inline math
    /// (down = (sel+1)%5, up = (sel+4)%5, point = clamp-by-ignore, and the fixed index->action map) to
    /// prove parity.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the seeded
    /// <see cref="PropertyCheck"/> harness drives &gt;= 100 deterministic cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    public sealed class MainMenuFocusPropertyTests
    {
        private const int OptionCount = MainMenuFocus.OptionCount; // 5

        // The original inline option -> action map from MainMenuUI.Activate, used as the parity oracle.
        private static readonly MainMenuAction[] ExpectedActions =
        {
            MainMenuAction.StartJourney,   // 0
            MainMenuAction.ReplayPrologue, // 1
            MainMenuAction.OpenSettings,   // 2
            MainMenuAction.OpenControls,   // 3
            MainMenuAction.Quit,           // 4
        };

        // Feature: nexus-lobby-menu-restructure, Property 10: foco do menu consistente.
        // Under any sequence of keyboard (down/up) and mouse (point) navigation, exactly one option is
        // focused (index in [0, OptionCount)) and activating it triggers that option's action, matching a
        // reference model that reproduces the original inline focus math.
        // Validates: Requirements 6.3, 6.5
        [Test]
        public void FocusIsAlwaysSingleAndInRange_AndActivationMatchesFocusedOption()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var focus = new MainMenuFocus();

                // Reference model mirroring the previous inline behavior in MainMenuUI.
                int reference = 0;

                // Initial state: focus starts on option 0, exactly one option focused.
                PropertyCheck.That(focus.Selected == reference,
                    $"[case #{i}] initial focus must be {reference}, got {focus.Selected}");
                AssertExactlyOneFocused(focus, i, "initial");

                int steps = rng.Next(1, 40);
                for (int s = 0; s < steps; s++)
                {
                    // 0 = down arrow, 1 = up arrow, 2 = point (mouse move over an option index).
                    int op = rng.Next(0, 3);
                    if (op == 0)
                    {
                        focus.MoveDown();
                        reference = (reference + 1) % OptionCount;
                    }
                    else if (op == 1)
                    {
                        focus.MoveUp();
                        reference = (reference + OptionCount - 1) % OptionCount;
                    }
                    else
                    {
                        // Include out-of-range pointer indices (negative and >= OptionCount): the original
                        // DrawHome only wrote _selected = i for i in [0, OptionCount), so out-of-range
                        // pointing must leave focus unchanged.
                        int pointed = rng.Next(-3, OptionCount + 3);
                        focus.PointTo(pointed);
                        if (pointed >= 0 && pointed < OptionCount) reference = pointed;
                    }

                    // Exactly one option is focused and the index stays in range at every step.
                    PropertyCheck.That(focus.Selected == reference,
                        $"[case #{i}] step {s} op {op}: focus {focus.Selected} != reference {reference}");
                    AssertExactlyOneFocused(focus, i, $"step {s}");

                    // Activating the focused option triggers exactly that option's action (parity).
                    MainMenuAction activated = focus.Activate();
                    PropertyCheck.That(activated == ExpectedActions[focus.Selected],
                        $"[case #{i}] step {s}: Activate() returned {activated} for focus {focus.Selected}, " +
                        $"expected {ExpectedActions[focus.Selected]}");
                    PropertyCheck.That(activated == MainMenuFocus.ActionFor(focus.Selected),
                        $"[case #{i}] step {s}: Activate() must equal ActionFor(Selected)");
                }
            });
        }

        // Feature: nexus-lobby-menu-restructure, Property 10 (activation map is a total bijection over options).
        // Every option index maps to a distinct action, and the focused option's activation always equals
        // the map for that index, so the focused option is the one whose action fires.
        // Validates: Requirements 6.3, 6.5
        [Test]
        public void ActionForCoversEveryOptionExactlyOnce()
        {
            var seen = new HashSet<MainMenuAction>();
            for (int index = 0; index < OptionCount; index++)
            {
                MainMenuAction action = MainMenuFocus.ActionFor(index);
                Assert.AreEqual(ExpectedActions[index], action,
                    $"option {index} must map to {ExpectedActions[index]}");
                Assert.IsTrue(seen.Add(action), $"action {action} mapped by more than one option");
            }

            Assert.AreEqual(OptionCount, seen.Count,
                "every option must map to a distinct action (one focused option -> one action)");
        }

        // Feature: nexus-lobby-menu-restructure, Property 10 (arrow wrap-around parity).
        // Walking down OptionCount times returns to the start, as does walking up OptionCount times, and
        // down-then-up (and up-then-down) is a no-op — matching the original modular navigation.
        // Validates: Requirements 6.3
        [Test]
        public void ArrowNavigationWrapsAndIsReversible()
        {
            var focus = new MainMenuFocus();
            for (int start = 0; start < OptionCount; start++)
            {
                // Move focus to 'start' via pointing, then verify wrap and reversibility from there.
                focus.PointTo(start);
                Assert.AreEqual(start, focus.Selected);

                for (int k = 0; k < OptionCount; k++) focus.MoveDown();
                Assert.AreEqual(start, focus.Selected, "down x OptionCount must return to start");

                for (int k = 0; k < OptionCount; k++) focus.MoveUp();
                Assert.AreEqual(start, focus.Selected, "up x OptionCount must return to start");

                focus.MoveDown();
                focus.MoveUp();
                Assert.AreEqual(start, focus.Selected, "down then up must be a no-op");

                focus.MoveUp();
                focus.MoveDown();
                Assert.AreEqual(start, focus.Selected, "up then down must be a no-op");
            }
        }

        // Exactly one option is focused: the focused index is in range and matches Selected, and every
        // other index is not the focused one. This models the "highlighted" flag in Button(rect, text,
        // _focus.Selected == i): precisely one i satisfies it.
        private static void AssertExactlyOneFocused(MainMenuFocus focus, int caseIndex, string where)
        {
            int sel = focus.Selected;
            PropertyCheck.That(sel >= 0 && sel < OptionCount,
                $"[case #{caseIndex}] {where}: focus {sel} out of range [0,{OptionCount})");

            int focusedCount = 0;
            for (int i = 0; i < OptionCount; i++) if (sel == i) focusedCount++;
            PropertyCheck.That(focusedCount == 1,
                $"[case #{caseIndex}] {where}: exactly one option must be focused, got {focusedCount}");
        }
    }
}
