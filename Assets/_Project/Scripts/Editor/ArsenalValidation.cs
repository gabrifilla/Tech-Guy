using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Run in an isolated project with -executeMethod ArsenalValidation.Run (without -quit).</summary>
[InitializeOnLoad]
public static class ArsenalValidation
{
    private const string Pending = "TechGuy.ArsenalValidation";
    private static int _phase, _skill, _checks, _errors;
    private static double _next;
    private static PlayerActor _player;
    private static AbilityHolder _holder;
    private static Actor _target;
    private static float _mana;

    static ArsenalValidation()
    {
        if (SessionState.GetBool(Pending, false)) Subscribe();
    }

    public static void Run()
    {
        // Editor search indexing can throw during startup; begin gameplay checks after startup settles.
        _next = EditorApplication.timeSinceStartup + 20;
        EditorApplication.update += WaitForStartup;
    }

    private static void WaitForStartup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < _next) return;
        EditorApplication.update -= WaitForStartup;
        SessionState.SetBool(Pending + ".HadPreference", PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey));
        SessionState.SetInt(Pending + ".Preference", PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey));
        PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
        EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        SessionState.SetBool(Pending, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    public static void BuildAndRun()
    {
        ArsenalBuilder.Build();
        Run();
    }

    private static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= State;
        EditorApplication.playModeStateChanged += State;
    }

    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _phase = _skill = _checks = _errors = 0;
        _next = EditorApplication.timeSinceStartup + 3;
        Application.logMessageReceived += Log;
        EditorApplication.update += Tick;
    }

    private static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Assert || type == LogType.Error) _errors++;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + .15;
        try
        {
            switch (_phase)
            {
                case 0:
                    ResolvePlayer();
                    Require(_player.CurrentWeapon == Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), "Default gauntlet");
                    Require(_holder.ActiveAbilities.Count == 4, "Gauntlet skills retained");
                    var guide = Object.FindFirstObjectByType<LobbyInteraction>();
                    var guideData = new SerializedObject(guide);
                    var stations = guideData.FindProperty("_stations");
                    Require(Enumerable.Range(0, stations.arraySize).Any(i => stations.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("weaponSelection").boolValue), "Arsenal station serialized");
                    FieldInfo panel = typeof(LobbyInteraction).GetField("_showDetails", BindingFlags.Instance | BindingFlags.NonPublic);
                    panel.SetValue(guide, true);
                    Require(_holder.BlocksWorldInput && !_holder.TryUseAbility(0), "Open panel blocks attacks and skills");
                    panel.SetValue(guide, false);
                    Require(!WeaponLoadout.Select(_player, -1) && !WeaponLoadout.Select(_player, 3), "Invalid loadouts rejected");
                    Require(WeaponLoadout.Select(_player, 1), "Equip bow");
                    _phase++;
                    break;
                case 1:
                case 4:
                    Require(_holder.ActiveAbilities.Count == 4 && _holder.ActiveAbilities.All(a => a is ArsenalAbility), "Four configured skills");
                    _player.RestoreMana(_player.maxMana);
                    _mana = _player.mana;
                    Require(_holder.TryUseAbility(_skill), "Cast skill " + _skill);
                    Require(_player.mana < _mana, "Mana consumed");
                    Require(_holder.IsCasting, "Cast locks combat");
                    if (_phase == 4 && _skill == 0)
                    {
                        var dummy = new GameObject("Spear test target");
                        dummy.SetActive(false);
                        _target = dummy.AddComponent<Actor>();
                        _target.health = 1000;
                        dummy.AddComponent<BoxCollider>().size = Vector3.one;
                        dummy.transform.position = _player.transform.position + _player.transform.forward * 2 + Vector3.up;
                        dummy.SetActive(true);
                        Physics.SyncTransforms();
                    }
                    Require(!_holder.TryUseAbility((_skill + 1) % 4), "Overlapping cast rejected");
                    Require(!WeaponLoadout.Select(_player, 0), "Cannot swap during cast");
                    _phase++;
                    _next = EditorApplication.timeSinceStartup + 2.5;
                    break;
                case 2:
                case 5:
                    if (_phase == 5 && _skill == 0)
                    {
                        Require(_target && _target.health < 1000, "Spear thrust damages target");
                        Object.Destroy(_target.gameObject);
                    }
                    Require(!_holder.IsCasting, "Cast releases combat");
                    Require(_holder.GetRemainingCooldown(_skill) > 0 && !_holder.TryUseAbility(_skill), "Cooldown enforced");
                    _skill++;
                    if (_skill < 4) _phase--;
                    else { _skill = 0; _phase++; }
                    break;
                case 3:
                    Require(WeaponLoadout.Select(_player, 2), "Equip spear");
                    _phase++;
                    break;
                case 6:
                    Require(WeaponLoadout.LoadSelected() == _player.CurrentWeapon, "Selection persisted");
                    // Explicit arrow test on empty space, with an actual collider and multiple frames of travel.
                    var target = new GameObject("Arsenal test target");
                    target.SetActive(false);
                    _target = target.AddComponent<Actor>();
                    _target.health = 1000;
                    target.AddComponent<BoxCollider>().size = Vector3.one;
                    target.transform.position = _player.transform.position + Vector3.up * 20 + Vector3.forward * 3;
                    target.SetActive(true);
                    Physics.SyncTransforms();
                    ArsenalProjectile.Fire(_player, target.transform.position - Vector3.forward * 3, Vector3.forward,
                        20, 1, 6, false, Color.cyan);
                    _phase++;
                    _next = EditorApplication.timeSinceStartup + .5;
                    break;
                case 7:
                    Require(_target.health < 1000, "Arrow collision damages target");
                    Object.Destroy(_target.gameObject);
                    SceneManager.LoadScene("FirstSector");
                    _phase++;
                    _next = EditorApplication.timeSinceStartup + 3;
                    break;
                case 8:
                    ResolvePlayer();
                    Require(_player.CurrentWeapon.weaponName == Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2]).weaponName, "Spear persists into FirstSector");
                    Require(_holder.ActiveAbilities.Count == 4 && _holder.ActiveAbilities[0] is ArsenalAbility, "Incursion skill bar matches selection");
                    Require(WeaponLoadout.Select(_player, 0), "Return to gauntlet");
                    _phase++;
                    break;
                case 9:
                    Require(_holder.ActiveAbilities.All(a => a is BreakerGauntletAbility), "Breaker restored after swapping");
                    Require(_errors == 0, "No runtime errors: " + _errors);
                    Finish(true, "ARSENAL_VALIDATION_SUCCESS: " + _checks + " checks passed.");
                    break;
            }
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static void ResolvePlayer()
    {
        _player = Object.FindFirstObjectByType<PlayerActor>();
        Require(_player && _player.HandTransform, "Player equipped dependencies");
        _holder = _player.GetComponent<AbilityHolder>();
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        SessionState.SetBool(Pending, false);
        if (SessionState.GetBool(Pending + ".HadPreference", false))
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, SessionState.GetInt(Pending + ".Preference", 0));
        else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
        PlayerPrefs.Save();
        Debug.Log(message);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
