using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class CharControlScript
{
    private readonly List<RaycastResult> _basicUiHits = new List<RaycastResult>();
    private PointerEventData _basicPointerEvent;
    private EventSystem _basicEventSystem;

    private bool BasicPointerIsOverUi(Vector2 position)
    {
        if (_playerHUD && _playerHUD.BlocksPointer(position)) return true;
        var events = EventSystem.current;
        if (!events) return false;
        if (_basicEventSystem != events)
        { _basicEventSystem = events; _basicPointerEvent = new PointerEventData(events); }
        _basicPointerEvent.Reset(); _basicPointerEvent.position = position;
        _basicUiHits.Clear();
        events.RaycastAll(_basicPointerEvent, _basicUiHits);
        // Physics raycasters may report world geometry; only graphics own this UI gate.
        foreach (var hit in _basicUiHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }

    private bool TryReadDirectBasicDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        Vector2 pointer = GetPointerPosition();
        Camera view = mainCamera ? mainCamera : Camera.main;
        if (!view) return false;
        var plane = new Plane(Vector3.up, transform.position);
        Ray ray = view.ScreenPointToRay(pointer);
        if (!plane.Raycast(ray, out float distance) || distance < 0) return false;
        direction = ray.GetPoint(distance) - transform.position;
        direction.y = 0;
        if (!GroundMovementBuffer.IsFinite(direction) || direction.sqrMagnitude < .0001f) return false;
        direction.Normalize();
        return true;
    }

    internal bool DirectPointerIsOverUi => BasicPointerIsOverUi(GetPointerPosition());

    private void HandleDirectBasicInput()
    {
        if (UsesGauntletBasic && GamePreferences.WasWeaponSpecialPressed()) return; // Special wins a simultaneous press.
        bool primaryHeld = GamePreferences.IsHeld(GameControl.Primary);
        if (!primaryHeld && !GamePreferences.IsHeld(GameControl.Move)) _waitForAttackRelease = false;
        if (_waitForAttackRelease || !_abilityHolder) return;
        bool pressed = GamePreferences.WasPressed(GameControl.Primary);
        // Approved compatibility exception: ChargedShot accumulates between cadence-driven shots.
        // Releasing resets charge; it never manufactures another shot or another buffered press.
        bool chargedHold = weapon && weapon.FiresArrows && ChargedShotRank > 0 && primaryHeld && CanStartBasicAttack();
        if (!pressed && !chargedHold) return;
        if (BasicPointerIsOverUi(GetPointerPosition())) { RequireAttackRelease(); return; }
        if (!TryReadDirectBasicDirection(out Vector3 direction)) return;
        _abilityHolder.TryRequestDirectionalBasic(direction, pressed);
    }

    private void HandleLegacySecondaryPointerInput()
    {
        if (UsesGauntletBasic || _waitForAttackRelease || GamePreferences.WasPressed(GameControl.Primary)) return;
        // Bow/Spear retain their pre-existing secondary interaction/Shift gesture.
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        if (shift && GamePreferences.IsHeld(GameControl.Move)) TryDirectionalBasicAttackAtPointer();
        else if (GamePreferences.WasPressed(GameControl.Move)) RequestMove();
    }
}
