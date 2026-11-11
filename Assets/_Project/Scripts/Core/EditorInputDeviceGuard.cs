#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor-only input guards (10/11/2026). Both problems left the player unable to move or act after pressing Play while
/// the legacy click-to-move (old Input Manager) still worked:
/// 1. Aborted/failed test runs can leave virtual keyboards/mice registered, and the last one stays
///    <c>Keyboard.current</c>. On entering Play those leftovers are removed and the real keyboard and mouse become current.
///    Tests add their own virtual devices afterwards, inside the test, and those still own <c>.current</c>.
/// 2. With the "ResetAndDisableNonBackgroundDevices" background behaviour the Input System disables the keyboard and mouse
///    when the Editor loses focus and should re-enable them on focus; in this Editor setup that re-enable was missed, so the
///    devices stayed disabled for the rest of the session. While playing, a focused Editor re-enables them. While a test's
///    virtual keyboard/mouse exists, the real ones are switched off instead, so tests stay isolated from the desk.
/// Builds and the project's input settings are unchanged.
/// </summary>
internal static class EditorInputDeviceGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnEnterPlay()
    {
        // Virtual keyboards/mice that exist before the first scene loads are leftovers of earlier test runs (tests create
        // theirs inside the test, after Play has started).
        var leaked = new System.Collections.Generic.List<InputDevice>();
        foreach (var device in InputSystem.devices) if (!device.native && (device is Keyboard || device is Mouse)) leaked.Add(device);
        foreach (var device in leaked) InputSystem.RemoveDevice(device);
        foreach (var device in InputSystem.devices) if (device.native && (device is Keyboard || device is Mouse)) device.MakeCurrent();
        // Domain reload is disabled in this project: unsubscribe first so the handler is never added twice.
        InputSystem.onBeforeUpdate -= ReenableNativePointerAndKeyboard;
        InputSystem.onBeforeUpdate += ReenableNativePointerAndKeyboard;
    }

    private static void ReenableNativePointerAndKeyboard()
    {
        if (!Application.isPlaying) { InputSystem.onBeforeUpdate -= ReenableNativePointerAndKeyboard; return; }
        // A virtual keyboard/mouse means a test owns input right now: the real ones are switched off so a stray real
        // mouse move cannot steal .current from the test's devices; they come back when the test removes its devices.
        bool testOwnsInput = false;
        foreach (var device in InputSystem.devices) if (!device.native && (device is Keyboard || device is Mouse)) { testOwnsInput = true; break; }
        if (!testOwnsInput && !Application.isFocused) return;
        foreach (var device in InputSystem.devices)
        {
            if (!device.native || !(device is Keyboard || device is Mouse)) continue;
            if (testOwnsInput && device.enabled) InputSystem.DisableDevice(device);
            else if (!testOwnsInput && !device.enabled) InputSystem.EnableDevice(device);
        }
    }
}
#endif
