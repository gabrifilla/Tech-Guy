using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RunSynergyValidation
{
    private const string Pending = "TechGuy.RunSynergyValidation";
    private static readonly List<GameObject> Objects = new List<GameObject>();
    static RunSynergyValidation()
    {
        if (SessionState.GetBool(Pending, false)) EditorApplication.playModeStateChanged += Validate;
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.playModeStateChanged -= Validate;
        EditorApplication.playModeStateChanged += Validate;
        EditorApplication.EnterPlaymode();
    }
    private static Actor Enemy(Vector3 position, float health = 1000)
    {
        var go = new GameObject("Synergy target"); Objects.Add(go); go.SetActive(false);
        go.transform.position = position;
        var actor = go.AddComponent<Actor>(); actor.health = health;
        go.AddComponent<BoxCollider>(); go.AddComponent<SphereCollider>();
        go.SetActive(true); return actor;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Validate(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        int code = 0;
        try
        {
            var owner = new GameObject("Synergy owner"); Objects.Add(owner);
            var effects = owner.AddComponent<PlayerOnHitEffects>();
            effects.EnableChill(.9f, 2.5f, .35f);
            Require(Mathf.Approximately(effects.ChillChance, .35f), "First frost chance must be 35%");
            effects.EnableChill(.9f, 2.5f, .35f);
            Require(Mathf.Approximately(effects.ChillChance, .7f), "Repeated frost must stack");
            effects.Clear();
            effects.EnableBurn(6, 4); effects.EnableChill(.9f, 2.5f, 1);
            effects.Synergies.Add(RunSynergy.Conductor);
            Actor first = Enemy(Vector3.zero), second = Enemy(Vector3.right * 3), third = Enemy(Vector3.right * 6);
            Physics.SyncTransforms();
            first.TakeDamage(100); effects.ApplyTo(first, 100);
            Require(Mathf.Approximately(second.health, 955), "Chain deals 45%, once despite multiple colliders");
            Require(Mathf.Approximately(third.health, 979.75f), "Secondary hit continues chain with attenuation");
            Require(second.GetComponent<BurnStatus>() && third.GetComponent<ChillStatus>(), "Secondary hits inherit elements");
            Require(first.health == 900 && effects.Synergies.LastSecondaryHits == 2, "No ping-pong back to prior targets");
            effects.Synergies.Add(RunSynergy.Resonance);
            effects.ApplyTo(first, 100);
            Require(Mathf.Abs(second.health - 874) < .01f, "Fire + ice amplify next discharge to 81");
            effects.Clear();
            Require(!effects.HasAnyEffect && effects.Synergies.Rank(RunSynergy.Conductor) == 0, "Run reset removes all ranks/elements");
            effects.Synergies.Add(RunSynergy.Detonation);
            Actor victim = Enemy(Vector3.forward * 20, 100), neighbor = Enemy(Vector3.forward * 20 + Vector3.right * 2);
            Physics.SyncTransforms(); victim.TakeDamage(100); effects.ApplyTo(victim, 100);
            Require(Mathf.Approximately(neighbor.health, 925), "Lethal impact detonates after death");
            effects.Clear(); effects.Synergies.Add(RunSynergy.Conductor);
            effects.Synergies.Add(RunSynergy.Reactor);
            Require(Mathf.Approximately(effects.Synergies.BurnScaling, .12f), "Reactor scales burn from actual hit damage");
            effects.Synergies.Add(RunSynergy.Reactor);
            Require(Mathf.Approximately(effects.Synergies.BurnScaling, .24f), "Reactor stacks");
            for (int i = 0; i < 40; i++) effects.Synergies.Add(RunSynergy.Conductor);
            Actor crowdRoot = Enemy(Vector3.forward * 50);
            for (int i = 0; i < 45; i++) Enemy(Vector3.forward * 50 + Vector3.right * (1 + i * .1f));
            Physics.SyncTransforms(); effects.ApplyTo(crowdRoot, 100);
            Require(effects.Synergies.LastSecondaryHits == 32, "Dense cascades respect the 32-hit budget");
            Debug.Log("RUN_SYNERGY_VALIDATION_SUCCESS: stacking, reset, elemental inheritance, resonance, lethal explosion, collider deduplication, cascade budget.");
        }
        catch (Exception exception) { Debug.LogException(exception); code = 1; }
        finally
        {
            foreach (GameObject go in Objects) if (go) Object.Destroy(go);
            Objects.Clear(); SessionState.SetBool(Pending, false);
            EditorApplication.playModeStateChanged -= Validate;
            EditorApplication.Exit(code);
        }
    }
}
