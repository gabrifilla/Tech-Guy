using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared owner for gameplay-blocking panels (dialogue, Nexus terminal). While any is open the game clock is frozen
/// (Time.timeScale = 0, which AbilityHolder/CharControlScript already treat as "world input blocked", like pause),
/// the uGUI selection is cleared so Enter cannot submit a button behind the panel, and the pause menu stays closed
/// (PauseMenuUI checks <see cref="IsOpen"/>). Closing restores the clock and requires the attack input to be released.
/// </summary>
public static class GameplayModal
{
    private static int _open;
    private static float _restoreScale = 1f;
    private static GameObject _restoreSelection;

    public static bool IsOpen => _open > 0;
    /// <summary>Frame the outermost panel opened: the key that opened it must not also act on it.</summary>
    public static int OpenedFrame { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _open = 0; _restoreScale = 1f; _restoreSelection = null; OpenedFrame = -1; }

    public static void Enter()
    {
        if (_open++ > 0) return;
        _restoreScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        OpenedFrame = Time.frameCount;
        var events = EventSystem.current;
        _restoreSelection = events ? events.currentSelectedGameObject : null;
        if (events) events.SetSelectedGameObject(null);
    }

    public static void Exit(CharControlScript inputOwner)
    {
        if (_open == 0) return;
        if (--_open == 0)
        {
            Time.timeScale = _restoreScale;
            var events = EventSystem.current;
            if (events && _restoreSelection && _restoreSelection.activeInHierarchy) events.SetSelectedGameObject(_restoreSelection);
            _restoreSelection = null;
        }
        if (inputOwner) inputOwner.RequireAttackRelease();
    }
}
