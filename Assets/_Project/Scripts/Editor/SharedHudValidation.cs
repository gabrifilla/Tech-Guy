using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SharedHudValidation
{
    private const string Pending = "TechGuy.SharedHudValidation";
    private static readonly string[] Scenes = { "Playground", "NexusLobby", "CombatStudy" };
    private static int _sceneIndex, _phase, _errors, _knownShaderErrors;
    private static double _next;
    private static PlayerActor _player;
    private static PlayerHUD _hud;
    private static AbilityHolder _holder;
    private static WeaponScript _originalWeapon, _emptyWeapon;
    private static float _manaBeforeRegeneration;

    static SharedHudValidation()
    {
        if (SessionState.GetBool(Pending,false)) Subscribe();
    }

    public static void Run()
    {
        // Let Editor startup/indexing finish before measuring errors from gameplay.
        _next = EditorApplication.timeSinceStartup + 20;
        EditorApplication.update += WaitForStartup;
    }

    private static void WaitForStartup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < _next) return;
        EditorApplication.update -= WaitForStartup;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Playground.unity");
        SessionState.SetBool(Pending,true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    private static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnState;
        EditorApplication.playModeStateChanged += OnState;
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _sceneIndex = _phase = _errors = _knownShaderErrors = 0;
        _next = EditorApplication.timeSinceStartup + 3;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
    }

    private static void OnLog(string message,string stack,LogType type)
    {
        // Existing third-party include-path failure is reported separately, never silently ignored.
        if (message.StartsWith("Shader error in 'Universal Render Pipeline/RealToon/Version 5/Default/Default'", StringComparison.Ordinal) &&
            message.Contains("Couldn't open include file 'Assets/RealToon/RealToon Shaders/RealToon Core/URP/RT_URP_Core.hlsl'"))
        {
            _knownShaderErrors++;
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++;
    }
    private static void Advance(int phase,double delay) { _phase = phase; _next = EditorApplication.timeSinceStartup + delay; }
    private static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(float actual,float expected,string message) => Require(Mathf.Abs(actual-expected) < .02f,message+$" ({actual} vs {expected})");

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        try
        {
            if (_phase == 0)
            {
                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                _player = roots.SelectMany(root => root.GetComponentsInChildren<PlayerActor>()).Single();
                _hud = roots.SelectMany(root => root.GetComponentsInChildren<PlayerHUD>()).Single();
                _holder = _player.GetComponent<AbilityHolder>();
                foreach (EnemyAI enemy in roots.SelectMany(root => root.GetComponentsInChildren<EnemyAI>())) enemy.enabled = false;
                Require(_hud.Player == _player,"HUD bound to the wrong player");
                Require(_player.healthBar == _hud.HealthFill && _player.manaBar == _hud.ManaFill,"Resource fills not bound");
                Require(!_player.GetComponent<BreakerGauntletHUD>(),"Legacy Asura HUD duplicated");
                Require(_hud.GetComponentsInChildren<SkillGlyphGraphic>().All(glyph => glyph.GetComponent<CanvasRenderer>()),"Skill glyph renderer missing");
                SetRegeneration(0);
                _player.TrySpendMana(_player.mana);
                int energy = _player.GetComponent<BreakerGauntletCombat>().Energy;
                Require(!_holder.TryUseAbility(0),"Empty mana allowed a skill");
                Near(_holder.GetRemainingCooldown(0),0,"Denied skill started cooldown");
                Near(_player.mana,0,"Denied skill changed mana");
                Require(_player.GetComponent<BreakerGauntletCombat>().Energy == energy,"Denied skill generated energy");
                Require(_hud.FeedbackText == "MANA INSUFICIENTE","Mana rejection feedback missing");
                Near(_hud.ManaFill.fillAmount,0,"Mana orb not updated");
                Advance(1,.15);
            }
            else if (_phase == 1)
            {
                if (_sceneIndex == 1) Capture("HUD-no-mana");
                _player.RestoreMana(_player.maxMana*2);
                Near(_player.mana,_player.maxMana,"Mana overflow");
                float before = _player.mana;
                Require(!_holder.TryUseAbility(3),"Asura without energy started");
                Near(_player.mana,before,"Unavailable Asura consumed mana");
                Require(_holder.TryUseAbility(1),"Funded skill failed");
                Near(_player.mana,before-_holder.ActiveAbilities[1].ManaCost,"Skill did not charge exactly once");
                Require(!_holder.TryUseAbility(1),"Repeated skill cast twice");
                Near(_player.mana,before-_holder.ActiveAbilities[1].ManaCost,"Repeated cast consumed mana");
                Near(_hud.ManaFill.fillAmount,_player.mana/_player.maxMana,"Mana orb stale after cast");
                Advance(2,.15);
            }
            else if (_phase == 2)
            {
                Require(_hud.Slots[1].Status == "EM USO","Active skill indication missing");
                if (_sceneIndex == 1) Capture("HUD-casting");
                Advance(3,1.25);
            }
            else if (_phase == 3)
            {
                Require(_holder.GetRemainingCooldown(1) > 0,"Cooldown not started after cast");
                Require(!string.IsNullOrEmpty(_hud.Slots[1].Status),"Cooldown number missing");
                _player.TakeDamage(10);
                Near(_hud.HealthFill.fillAmount,_player.health/_player.maxHealth,"Health orb stale after damage");
                Capture("HUD-"+Scenes[_sceneIndex]);
                SetRegeneration(4);
                _player.TrySpendMana(10);
                _manaBeforeRegeneration = _player.mana;
                Advance(4,1.8);
            }
            else if (_phase == 4)
            {
                Require(_player.mana > _manaBeforeRegeneration,"Mana did not regenerate after delay");
                _player.RestoreMana(_player.maxMana*2);
                Near(_player.mana,_player.maxMana,"Regeneration/restoration exceeded maximum");
                SetRegeneration(0);
                DashScript dash = _player.GetComponent<CharControlScript>().dashScript;
                float before = _player.mana;
                Require(_holder.TryUseDash(dash),"Funded dash failed");
                Near(_player.mana,before-dash.ManaCost,"Dash mana cost wrong");
                Require(!_holder.TryUseDash(dash),"Dash restarted while active");
                Near(_player.mana,before-dash.ManaCost,"Repeated dash charged twice");
                Advance(5,.6);
            }
            else if (_phase == 5)
            {
                if (Scenes[_sceneIndex] == "NexusLobby")
                {
                    LobbyInteraction guide = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<LobbyInteraction>()).Single();
                    var data = new SerializedObject(guide);
                    Transform anchor = (Transform)data.FindProperty("_stations").GetArrayElementAtIndex(0).FindPropertyRelative("anchor").objectReferenceValue;
                    Require(_player.GetComponent<NavMeshAgent>().Warp(anchor.position),"Could not reach lobby terminal");
                    float before = _player.mana;
                    Require(!_holder.TryUseAbility(2),"Interaction key also activated a skill");
                    Near(_player.mana,before,"Terminal interaction consumed mana");
                }
                _originalWeapon = _player.CurrentWeapon;
                _emptyWeapon = ScriptableObject.CreateInstance<WeaponScript>();
                _emptyWeapon.abilities = Array.Empty<Ability>();
                _player.EquipWeapon(_emptyWeapon);
                Advance(6,.2);
            }
            else if (_phase == 6)
            {
                Require(_hud.Slots[0].Status == "Vazio","HUD did not follow weapon change");
                _player.EquipWeapon(_originalWeapon);
                Object.Destroy(_emptyWeapon);
                Require(!_player.TrySpendMana(_player.maxMana*2),"Overspend allowed");
                Require(!_player.TrySpendMana(float.NaN),"NaN cost allowed");
                Debug.Log("HUD_PLAYMODE_SCENE_SUCCESS: "+Scenes[_sceneIndex]);
                _sceneIndex++;
                if (_sceneIndex == Scenes.Length)
                {
                    Require(_errors == 0,"Runtime errors: "+_errors);
                    if (_knownShaderErrors > 0) Debug.LogWarning("SHARED_HUD_EXTERNAL_SHADER_ERRORS: " + _knownShaderErrors + " known RealToon include errors; Playground rendering still requires its existing shader repair.");
                    Debug.Log("SHARED_HUD_PLAYMODE_SUCCESS: three scenes, mana, regeneration, cooldowns, feedback, dash, weapon swaps and lobby input validated.");
                    Finish(0);
                }
                else
                {
                    SceneManager.LoadScene(Scenes[_sceneIndex]);
                    Advance(0,3);
                }
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }

    private static void SetRegeneration(float percent)
    {
        var data = new SerializedObject(_player);
        data.FindProperty("_manaRegenerationPercentPerSecond").floatValue = percent;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Capture(string label)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Camera camera = Camera.main;
        Canvas canvas = _hud.GetComponent<Canvas>();
        var mode = canvas.renderMode;
        Camera previousCamera = canvas.worldCamera;
        RenderTexture target = camera.targetTexture, previous = RenderTexture.active;
        var texture = new RenderTexture(1600,900,24);
        var image = new Texture2D(1600,900,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture = texture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply();
            Directory.CreateDirectory("Docs");
            File.WriteAllBytes("Docs/"+label+".png",image.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = mode; canvas.worldCamera = previousCamera;
            camera.targetTexture = target; RenderTexture.active = previous;
            Object.DestroyImmediate(image); texture.Release(); Object.DestroyImmediate(texture);
        }
    }

    private static void Finish(int code)
    {
        SessionState.SetBool(Pending,false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(code);
    }
}
