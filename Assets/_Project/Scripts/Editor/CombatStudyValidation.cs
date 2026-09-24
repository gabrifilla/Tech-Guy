using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class CombatStudyValidation
{
    private const string Pending = "CombatStudy.Validation";
    private static int _phase, _errors;
    private static double _next;
    private static PlayerActor _player;
    private static EnemyAI _normal, _rare;
    private static EnemyVariant _variant;
    private static EnemyFrostAura _frost, _second;
    private static float _health, _speed, _baseline;

    static CombatStudyValidation() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    public static void Run()
    {
        EditorSceneManager.OpenScene(CombatStudyBuilder.ScenePath);
        SessionState.SetBool(Pending, true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= State;
        EditorApplication.playModeStateChanged += State;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
    }
    private static void Log(string text, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++;
    }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _next = EditorApplication.timeSinceStartup + 3; EditorApplication.update += Tick;
    }
    private static void Advance(float seconds) { _phase++; _next = EditorApplication.timeSinceStartup + seconds; }
    private static void Warp(Actor actor, Vector3 position)
    {
        var agent = actor.GetComponent<NavMeshAgent>(); agent.ResetPath();
        Require(agent.Warp(position), "Warp failed."); Physics.SyncTransforms();
    }
    private static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < 0.025f, message + $" Actual={actual}, expected={expected}");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        try
        {
            switch (_phase)
            {
                case 0:
                    _player = UnityEngine.Object.FindObjectsByType<PlayerActor>().Single();
                    _player.GetComponent<CharControlScript>().enabled = false;
                    var enemies = UnityEngine.Object.FindObjectsByType<EnemyAI>();
                    _normal = enemies.First(e => e.GetComponent<EnemyVariant>().Profile.Rarity == EnemyRarity.Normal);
                    _rare = enemies.First(e => e.GetComponent<EnemyVariant>().Profile.Rarity == EnemyRarity.Rare);
                    foreach (var enemy in enemies) enemy.enabled = false;
                    _variant = _rare.GetComponent<EnemyVariant>();
                    _frost = _rare.GetComponentInChildren<EnemyFrostAura>();
                    Require(_frost && _frost.GetComponentInChildren<CombatGroundRing>(), "Frost boundary missing.");
                    _player.Stats.AddModifier(new PlayerStatModifier(PlayerStatType.MovementSpeedMultiplier, PlayerStatModifierMode.MoreMultiplier, 1.1f), _player);
                    _baseline = _player.Stats.MovementSpeedMultiplier;
                    Warp(_player, _rare.transform.position + Vector3.back * 3);
                    Advance(0.4f); break;
                case 1:
                    Near(_player.Stats.MovementSpeedMultiplier, _baseline * 0.7f, "First frost slow");
                    Capture();
                    _second = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyFrostAura.prefab"), _rare.transform).GetComponent<EnemyFrostAura>();
                    Advance(0.4f); break;
                case 2:
                    Near(_player.Stats.MovementSpeedMultiplier, _baseline * 0.49f, "Overlapping sources");
                    _frost.enabled = false;
                    Near(_player.Stats.MovementSpeedMultiplier, _baseline * 0.7f, "Disable removes only its source");
                    Warp(_player, new Vector3(0, 0, -8));
                    Advance(0.4f); break;
                case 3:
                    Near(_player.Stats.MovementSpeedMultiplier, _baseline, "Leaving aura preserves passive");
                    _second.enabled = false;
                    _speed = _rare.agent.speed;
                    _variant.Configure(_variant.Profile);
                    Near(_rare.agent.speed, _speed, "Repeated profile accumulates haste");
                    _health = _rare.GetComponent<Actor>().health;
                    _rare.GetComponent<Actor>().TakeDamage(10);
                    Near(_rare.GetComponent<Actor>().health, _health - 7, "Guard reduction");
                    Warp(_player, _rare.transform.position + Vector3.back * 3);
                    Advance(0.4f); break;
                case 4:
                    Near(_player.Stats.MovementSpeedMultiplier, _baseline * 0.7f, "Reconfigured frost");
                    _rare.GetComponent<Actor>().TakeDamage(99999);
                    Near(_player.Stats.MovementSpeedMultiplier, _baseline, "Death must remove slow immediately");
                    Warp(_normal.GetComponent<Actor>(), Vector3.zero);
                    Warp(_player, Vector3.back);
                    _normal.timeBetweenAttacks = 2;
                    _normal.enabled = true;
                    _health = _player.health;
                    Advance(0.15f); break;
                case 5:
                    Require(_normal.IsWindingUp, "No windup");
                    Near(_player.health, _health, "Damage before telegraph ends");
                    Warp(_player, Vector3.back * 5);
                    Advance(0.9f); break;
                case 6:
                    Near(_player.health, _health, "Dodged attack still hit");
                    _normal.enabled = false;
                    Warp(_normal.GetComponent<Actor>(), Vector3.zero);
                    Warp(_player, Vector3.back);
                    _normal.enabled = true;
                    Advance(2.5f); break;
                case 7:
                    // One complete attack after cooldown; a second hit must not be caused by animation events.
                    Near(_player.health, _health - 12, "Impact damage must occur exactly once");
                    _normal.ActivateHitbox();
                    Advance(0.2f); break;
                case 8:
                    Near(_player.health, _health - 12, "Duplicate animation-event damage");
                    _normal.enabled = false;
                    Require(!_normal.IsWindingUp, "Disabled attack still winding up");
                    _player.Stats.RemoveModifiersFrom(_player);
                    Require(_errors == 0, "Runtime errors: " + _errors);
                    Debug.Log("COMBAT_VALIDATION_SUCCESS: frost enter/exit/overlap/disable/death, passive preservation, affix recomposition, resistance, windup, dodge and single impact.");
                    Finish(0); break;
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }
    private static void Capture()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main;
        var rt = new RenderTexture(1280, 800, 24);
        var image = new Texture2D(1280, 800, TextureFormat.RGB24, false);
        RenderTexture old = RenderTexture.active;
        camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        image.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); image.Apply();
        File.WriteAllBytes("Docs/CombatStudy.png", image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = old;
        UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }
    private static void Finish(int code)
    {
        SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log; EditorApplication.Exit(code);
    }
}
