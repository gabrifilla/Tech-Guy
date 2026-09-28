using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WeaponModifierValidation
{
    private const string Pending = "TechGuy.WeaponModifierValidation";
    private static int _family, _skill, _phase, _checks, _errors;
    private static double _next, _deadline;
    private static PlayerActor _player;
    private static AbilityHolder _holder;
    private static RunBoons _run;
    private static readonly List<Actor> Targets = new List<Actor>();
    private static float _health;
    private static readonly Dictionary<WeaponScript, string> Originals = new Dictionary<WeaponScript, string>();
    static WeaponModifierValidation() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    public static void Run()
    {
        _next = EditorApplication.timeSinceStartup + 20;
        EditorApplication.update += WaitForStartup;
    }
    private static void WaitForStartup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < _next) return;
        EditorApplication.update -= WaitForStartup;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= State;
        EditorApplication.playModeStateChanged += State;
    }
    private static void Require(bool value, string message)
    { _checks++; if (!value) throw new Exception(message); }
    private static void Log(string text, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++; }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _family = _skill = _phase = _checks = _errors = 0;
        Application.logMessageReceived += Log;
        try { CheckRules(); }
        catch (Exception e) { Debug.LogException(e); Finish(1); return; }
        _next = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }
    private static void CheckRules()
    {
        Require(WeaponRunModifiers.Catalog.Count == 30, "Exactly 30 unique modifiers");
        Require(WeaponRunModifiers.Catalog.Select(d => d.Id).Distinct().Count() == 30, "Unique reward ids");
        foreach (RunWeaponFamily family in Enum.GetValues(typeof(RunWeaponFamily)))
        {
            Require(WeaponRunModifiers.Catalog.Count(d => d.Family == family) == 10, "Ten modifiers per weapon");
            var fresh = new WeaponRunModifiers(family);
            foreach (var definition in WeaponRunModifiers.Catalog)
            {
                if (definition.Family != family) { Require(!fresh.Add(definition), "Reject foreign weapon boon"); continue; }
                for (int i = 0; i < definition.MaxRank; i++) Require(fresh.Add(definition), "Rank accepted");
                Require(!fresh.Add(definition), "Rank cap enforced");
            }
            Require(new WeaponRunModifiers(family).MeleeScale == 1, "No ranks leak into another run");
        }
        WeaponScript bow = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1]);
        var mods = new WeaponRunModifiers(RunWeaponFamily.Bow);
        foreach (var definition in WeaponRunModifiers.Catalog.Where(d => d.Family == RunWeaponFamily.Bow)) mods.Add(definition);
        ArsenalCastPlan rain = mods.Plan((ArsenalAbility)bow.abilities[3], 3);
        Require(rain.Hits == 7 && rain.TrackCursor && rain.Width > 2.6f, "Rain duration, radius and cursor coexist");
        Require(mods.Plan((ArsenalAbility)bow.abilities[2], 2).Arrows == 9, "Volley adds four arrows");
        Require(mods.DirectDamageMultiplier(12, 1, 1, 0) > mods.DirectDamageMultiplier(1, 1, 1, 0), "Sniper rewards distance");
        WeaponScript spear = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2]);
        var spearMods = new WeaponRunModifiers(RunWeaponFamily.Spear);
        foreach (var definition in WeaponRunModifiers.Catalog.Where(d => d.Family == RunWeaponFamily.Spear)) spearMods.Add(definition);
        var flurry = spearMods.Plan((ArsenalAbility)spear.abilities[2], 2);
        Require(flurry.Hits == 4 && flurry.Directions == 3, "Echo and trident compose");
        Require(spearMods.Plan((ArsenalAbility)spear.abilities[1], 1).Travel, "Moon travels");
        Require(spearMods.DirectDamageMultiplier(4, .2f, 1, 2) > 3, "Tip, execution and elements multiply");
        WeaponScript gauntlet = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
        var fists = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
        foreach (var definition in WeaponRunModifiers.Catalog.Where(d => d.Family == RunWeaponFamily.Gauntlet)) fists.Add(definition);
        var original = (BreakerGauntletAbility)gauntlet.abilities[1];
        string saved = JsonUtility.ToJson(original);
        Require(fists.GauntletSteps(original, 1).Count == original.HitSteps.Count + 2, "Flurry echoes append actual steps");
        Require(fists.GauntletSteps((BreakerGauntletAbility)gauntlet.abilities[2], 2).All(s => s.hitShape == AreaHitShape.Sphere), "Shock becomes radial");
        Require(JsonUtility.ToJson(original) == saved, "Shared hit steps unchanged");
        var energy = new AsuraMomentum(); energy.AddEnergy(1000); Require(energy.IsReady && energy.Energy == 100, "Energy remains bounded");
        energy.TryConsume(); energy.AddEnergy(60); Require(energy.Energy == 60 && !energy.IsReady, "Asura reserve cannot self-loop");
        AsuraMomentumValidation.Run();
        CheckAssetIsolation();
        foreach (string path in WeaponLoadout.ResourcePaths)
        {
            WeaponScript weapon = Resources.Load<WeaponScript>(path);
            Originals[weapon] = JsonUtility.ToJson(weapon) + string.Join("", weapon.abilities.Select(JsonUtility.ToJson));
        }
    }

    // R11.2/R11.6: assert no reward/modifier code path assigns into a source weapon/ability/status
    // ScriptableObject. Statuses (BurnStatus/ChillStatus) are runtime MonoBehaviour components, not
    // assets, so only the source WeaponScript + Ability assets loaded from Resources can be mutated.
    // Snapshot every source asset (JSON), drive the reward-owned modifier plan path at maximum ranks
    // across every arsenal slot for every family, then re-assert the sources are byte-for-byte
    // unchanged. Uses the same JsonUtility snapshot mechanism as the end-of-run Originals check, but
    // runs synchronously and isolates the WeaponRunModifiers.Plan reward path specifically.
    private static void CheckAssetIsolation()
    {
        var before = new Dictionary<Object, string>();
        foreach (string path in WeaponLoadout.ResourcePaths)
        {
            WeaponScript weapon = Resources.Load<WeaponScript>(path);
            Require(weapon, "Source weapon asset loads: " + path);
            before[weapon] = JsonUtility.ToJson(weapon);
            foreach (Ability ability in weapon.abilities)
                if (ability) before[ability] = JsonUtility.ToJson(ability);
        }
        for (int family = 0; family < WeaponLoadout.ResourcePaths.Length; family++)
        {
            WeaponScript weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[family]);
            var mods = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            foreach (var definition in WeaponRunModifiers.Catalog.Where(d => d.Family == mods.Family))
                while (mods.Add(definition)) { }
            for (int slot = 0; slot < weapon.abilities.Length; slot++)
            {
                // Build a per-cast plan (arsenal) or resolved hit steps (gauntlet) from the source
                // asset at maximum ranks; the reward path must never write back into the shared asset.
                if (weapon.abilities[slot] is ArsenalAbility arsenal) mods.Plan(arsenal, slot);
                else if (weapon.abilities[slot] is BreakerGauntletAbility gauntlet) mods.GauntletSteps(gauntlet, slot);
            }
        }
        foreach (var entry in before)
            Require(JsonUtility.ToJson(entry.Key) == entry.Value,
                "Source asset unchanged after reward/modifier application: " + entry.Key.name);
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        try
        {
            if (_phase == 0)
            {
                var go = new GameObject("Modifier validation player"); go.SetActive(false);
                _holder = go.AddComponent<AbilityHolder>();
                _holder.keys = new[] { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
                _player = go.AddComponent<PlayerActor>(); _player.handTransform = go.transform;
                _player.health = 1000; _player.mana = 100000; _player.Stats.criticalChance = 0;
                go.SetActive(true);
                _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[_family]));
                _run = go.AddComponent<RunBoons>();
                foreach (float distance in new[] { 2f, 5f, 8f, 14f })
                {
                    var target = new GameObject("Modifier target"); target.SetActive(false);
                    target.transform.position = Vector3.forward * distance + Vector3.up;
                    Actor actor = target.AddComponent<Actor>(); actor.health = 100000;
                    target.AddComponent<BoxCollider>(); target.SetActive(true); Targets.Add(actor);
                }
                Physics.SyncTransforms(); _phase = 1; _next = EditorApplication.timeSinceStartup + .5;
            }
            else if (_phase == 1)
            {
                Require(_run.WeaponModifiers != null && _player.RunModifiers == _run.WeaponModifiers, "Live run binds modifiers");
                _run.OfferReward(1);
                Require(_run.Choices.Any(o => o.Id.StartsWith("weapon_")), "Every reward includes a weapon option");
                int choice = _run.Choices.ToList().FindIndex(o => o.Id.StartsWith("weapon_"));
                Require(_run.Choose(choice), "Weapon reward can be acquired");
                foreach (var definition in WeaponRunModifiers.Catalog.Where(d => d.Family == _run.WeaponModifiers.Family))
                    while (_run.WeaponModifiers.Add(definition)) { }
                _skill = 0; _phase = 2;
            }
            else if (_phase == 2)
            {
                _health = Targets.Sum(t => t.health);
                Require(_holder.TryUseAbility(_skill), "Cast accepted: weapon " + _family + " slot " + _skill);
                _deadline = EditorApplication.timeSinceStartup + 15;
                _next = EditorApplication.timeSinceStartup + 2; _phase = 3;
            }
            else
            {
                if (_holder.IsCasting)
                {
                    Require(EditorApplication.timeSinceStartup < _deadline, "Cast must terminate");
                    _next = EditorApplication.timeSinceStartup + .2; return;
                }
                Require(Targets.Sum(t => t.health) < _health, "Modified skill deals actual damage: weapon " + _family + " slot " + _skill);
                _skill++;
                if (_skill < 4) { _phase = 2; return; }
                Object.Destroy(_player.gameObject);
                foreach (Actor target in Targets) Object.Destroy(target.gameObject);
                Targets.Clear(); _family++;
                if (_family == 3)
                {
                    foreach (var entry in Originals)
                        Require(entry.Value == JsonUtility.ToJson(entry.Key) + string.Join("", entry.Key.abilities.Select(JsonUtility.ToJson)), "Assets unchanged after all casts");
                    Require(_errors == 0, "No runtime errors");
                    Debug.Log("WEAPON_MODIFIER_VALIDATION_SUCCESS: " + _checks + " checks; all 12 skills at maximum modifier ranks; source weapon/ability assets unchanged (R11.2)."); Finish(0);
                }
                else { _phase = 0; _next = EditorApplication.timeSinceStartup + .5; }
            }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Finish(int code)
    {
        SessionState.SetBool(Pending, false); EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= State; Application.logMessageReceived -= Log;
        EditorApplication.Exit(code);
    }
}
