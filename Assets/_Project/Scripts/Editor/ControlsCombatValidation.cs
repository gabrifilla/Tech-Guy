using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class ControlsCombatValidation
{
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private const string Pending = "TechGuy.ControlsCombatValidation";
    static ControlsCombatValidation()
    {
        if (SessionState.GetBool(Pending, false)) EditorApplication.playModeStateChanged += OnPlayMode;
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.EnterPlaymode();
    }

    public static void RunBatch()
    {
        int code = 0;
        try { Check(); }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.delayCall += () =>
        {
            int code = 0;
            try { Check(); } catch (Exception error) { Debug.LogException(error); code = 1; }
            SessionState.SetBool(Pending, false);
            EditorApplication.Exit(code);
        };
    }
    private static void Check()
    {
        var original = GamePreferences.ReadBindings();
        var updateMode = InputSystem.settings.updateMode;
        Keyboard keyboard = null;
        Mouse mouse = null;
        try
        {
            var defaults = GamePreferences.DefaultBindings();
            Require(defaults.Distinct().Count() == defaults.Length, "Default controls must be unique");
            Require(defaults.All(GamePreferences.IsBindable), "Defaults must be bindable");
            Require(!GamePreferences.IsBindable(KeyCode.Escape), "Escape reserved for pause/cancel");
            var options = GamePreferences.Read();
            var copy = options.Copy();
            copy.Bindings[0] = KeyCode.F;
            Require(options.Bindings[0] != copy.Bindings[0], "Editing draft must not change saved bindings");
            defaults[2] = KeyCode.F;
            defaults[3] = KeyCode.Mouse3;
            GamePreferences.SaveBindings(defaults);
            Require(GamePreferences.ReadBindings().SequenceEqual(defaults), "Bindings survive preference reload");
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            InputSystem.EnableDevice(keyboard);
            InputSystem.Update();
            _ = keyboard.fKey.wasPressedThisFrame;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F));
            InputSystem.Update();
            Require(GamePreferences.IsHeld(GameControl.Dash), "Remapped dash reads the pressed keyboard binding");
            Require(!GamePreferences.IsHeld(GameControl.Skill1), "Old skill key does not activate remapped slot");
            mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            InputSystem.Update();
            _ = mouse.backButton.wasPressedThisFrame;
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Back));
            InputSystem.Update();
            Require(GamePreferences.IsHeld(GameControl.Skill1), "Mouse binding activates skill");

            Require(CharControlScript.ShouldTriggerDirectionalBasicAttack(true, true, false),
                "Left mouse plus left Shift requests a directional basic attack");
            Require(CharControlScript.ShouldTriggerDirectionalBasicAttack(true, false, true),
                "Right mouse plus right Shift requests a directional basic attack");
            Require(!CharControlScript.ShouldTriggerDirectionalBasicAttack(false, true, false),
                "A mouse click without Shift keeps its normal action");
            Require(!CharControlScript.ShouldTriggerDirectionalBasicAttack(true, false, false),
                "Shift without a mouse click does not attack");

            var build = typeof(AttackAreaSwoosh).GetMethod("BuildBoundary", BindingFlags.Static | BindingFlags.NonPublic);
            var box = (Mesh)build.Invoke(null, new object[] { new Vector3(3, 2, 8), AreaHitShape.Box, 0f });
            Require(Vector3.Distance(box.bounds.size, new Vector3(3, 0, 8)) < .001f, "Box footprint dimensions");
            Require(box.bounds.center == Vector3.zero, "Box footprint centered on physics query");
            UnityEngine.Object.DestroyImmediate(box);
            var circle = (Mesh)build.Invoke(null, new object[] { Vector3.one, AreaHitShape.Sphere, 4f });
            for (int i = 0; i < circle.vertexCount; i += 2)
                Require(Mathf.Abs(circle.vertices[i].magnitude - 4f) < .001f, "Circle outer edge matches radius");
            UnityEngine.Object.DestroyImmediate(circle);
            var stats = new PlayerArpgStats();
            stats.AddModifier(new PlayerStatModifier(PlayerStatType.MovementSpeedMultiplier, PlayerStatModifierMode.IncreasedPercent, 20));
            Require(Mathf.Abs(stats.MovementSpeedMultiplier - 1.2f) < .001f, "Movement boon gives twenty percent");
            Debug.Log("CONTROLS_COMBAT_VALIDATION_PASS: bindings, Shift directional attacks on both mouse buttons, isolated drafts, reserved keys, box/circle geometry and movement bonus");
        }
        finally
        {
            InputSystem.settings.updateMode = updateMode;
            GamePreferences.SaveBindings(original);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
        }
    }
}

