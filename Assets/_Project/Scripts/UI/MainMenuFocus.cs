/// <summary>Action the Home menu triggers when the focused option is activated.</summary>
public enum MainMenuAction
{
    /// <summary>Option 0: start the journey (tutorial or Nexus, depending on progress).</summary>
    StartJourney,

    /// <summary>Option 1: replay the prologue tutorial.</summary>
    ReplayPrologue,

    /// <summary>Option 2: open the Settings page.</summary>
    OpenSettings,

    /// <summary>Option 3: open the Controls page.</summary>
    OpenControls,

    /// <summary>Option 4: open the Quit page.</summary>
    Quit
}

/// <summary>
/// Pure, Unity-free focus/activation model for the Home page of <see cref="MainMenuUI"/>.
///
/// The menu keeps a single focused option index over a fixed list of options and moves that focus
/// with the down/up arrows (wrapping) or by pointing (mouse move over an option). Activating the
/// focused option (Enter or click) maps its index to a <see cref="MainMenuAction"/>. This class
/// concentrates exactly that decision logic so it can be exercised in EditMode without a scene or
/// IMGUI, while <see cref="MainMenuUI"/> stays a thin coordinator that only reads the focused index
/// and dispatches the resolved action.
///
/// Behaviour parity with the previous inline logic is preserved:
/// down = <c>(sel + 1) % Count</c>, up = <c>(sel + Count - 1) % Count</c>, pointing sets the index
/// when it is in range, and the option-to-action map is unchanged.
/// </summary>
public sealed class MainMenuFocus
{
    /// <summary>Number of Home options (Iniciar, Repetir prólogo, Configurações, Controles, Sair).</summary>
    public const int OptionCount = 5;

    private int _selected;

    /// <summary>Index of the currently focused option; always in <c>[0, OptionCount)</c>.</summary>
    public int Selected => _selected;

    /// <summary>Moves focus to the next option, wrapping past the last back to the first.</summary>
    public void MoveDown() => _selected = (_selected + 1) % OptionCount;

    /// <summary>Moves focus to the previous option, wrapping past the first back to the last.</summary>
    public void MoveUp() => _selected = (_selected + OptionCount - 1) % OptionCount;

    /// <summary>
    /// Points focus at <paramref name="index"/> (mouse move over an option). Out-of-range indices
    /// are ignored so a stray pointer position can never leave focus in an invalid state.
    /// </summary>
    public void PointTo(int index)
    {
        if (index >= 0 && index < OptionCount) _selected = index;
    }

    /// <summary>Maps an option index to the action it triggers when activated (Enter or click).</summary>
    public static MainMenuAction ActionFor(int index)
    {
        switch (index)
        {
            case 0: return MainMenuAction.StartJourney;
            case 1: return MainMenuAction.ReplayPrologue;
            case 2: return MainMenuAction.OpenSettings;
            case 3: return MainMenuAction.OpenControls;
            default: return MainMenuAction.Quit;
        }
    }

    /// <summary>The action the currently focused option triggers when activated.</summary>
    public MainMenuAction Activate() => ActionFor(_selected);
}
