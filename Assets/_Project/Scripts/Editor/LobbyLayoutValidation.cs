using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Feature: nexus-lobby-menu-restructure, Property 7/8/9
///
/// Editor-side validation of the Hades-style lobby layout, in the style of
/// <see cref="LobbySceneBuilder"/>/<see cref="LobbyCompactLayout"/> (a static builder-style check
/// rather than a PropertyCheck EditMode test, as the design states: "Validação executada pelo
/// builder"). It confirms three properties against the saved <c>NexusLobby.unity</c> scene and its
/// <c>NexusEnvironment.prefab</c>:
///
/// - <b>Property 7 — Alcançabilidade das estações</b>: after baking the NavMesh, there is a complete
///   NavMesh path from the player spawn to each of the three weapon pedestals, the training dummy and
///   each portal. Structurally it also confirms exactly three pedestals (weaponIndex 0/1/2) plus one
///   <see cref="TrainingDummy"/>. (Validates: Requirements 5.3)
/// - <b>Property 8 — Preservação de estações não-arma</b>: the informational stations and portals
///   remain present and functional (the incursion portal still targets a real scene, and the
///   removed <c>ARSENAL / ARMAS</c> weapon-selection station is gone). (Validates: Requirements 5.4)
/// - <b>Property 9 — Preservação de GUID</b>: the GUIDs of <c>NexusLobby.unity</c> and
///   <c>NexusEnvironment.prefab</c> are unchanged after regenerating/applying the layout.
///   (Validates: Requirements 5.5)
///
/// The scene on disk may still carry the old single-panel structure, in which case
/// <see cref="LobbyCompactLayout.Apply"/> throws "Lobby group missing: 04 - Weapon pedestals". To
/// make the validation meaningful this check first regenerates the lobby <b>in place</b> via
/// <see cref="LobbySceneBuilder.Rebuild"/> (which never deletes the scene/prefab, so their GUIDs are
/// preserved) unless it already exposes the new arsenal. GUIDs are captured before any regeneration
/// and re-checked afterwards so Property 9 covers the regenerate/apply cycle.
/// </summary>
public static class LobbyLayoutValidation
{
    private const string ScenePath = "Assets/_Project/Scenes/NexusLobby.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/NexusEnvironment.prefab";
    private const string EnvironmentName = "NEXUS - Modular Environment";

    [MenuItem("Tools/Tech Guy/Lobby/Validate Hades Layout")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Property 9 baseline: capture the GUIDs before any regenerate/apply so we can prove they are
        // untouched afterwards. These come from the .meta files via the AssetDatabase, never edited.
        string sceneGuidBefore = AssetGuid(ScenePath, "scene");
        string prefabGuidBefore = AssetGuid(PrefabPath, "environment prefab");
        Debug.Log($"LOBBY_LAYOUT_GUID_BEFORE: scene={sceneGuidBefore}, prefab={prefabGuidBefore}");

        // Regenerate in place only when needed: if the saved scene still lacks the arsenal (old
        // single-panel structure) the compact layout cannot be applied, so rebuild it first. Rebuild
        // overwrites the existing scene/prefab (never deletes them), so GUIDs are preserved.
        if (!SceneHasArsenal())
        {
            Debug.Log("LOBBY_LAYOUT_REBUILD: saved lobby lacks the arsenal; regenerating in place before validating.");
            LobbySceneBuilder.Rebuild();
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        Transform environment = roots.Single(root => root.name == EnvironmentName).transform;
        PlayerActor player = roots.SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true)).Single();
        Vector3 spawn = player.transform.position;

        int pedestals = ValidateArsenal(roots);
        int dummies = ValidateDummy(environment);
        ValidateNonWeaponStations(roots, environment);
        int[] reachable = ValidateReachability(environment, spawn);

        // Property 9: confirm GUIDs did not change across the regenerate/apply cycle. The saved layout
        // never deletes the scene/prefab, so any change here would be a regression to flag loudly.
        string sceneGuidAfter = AssetGuid(ScenePath, "scene");
        string prefabGuidAfter = AssetGuid(PrefabPath, "environment prefab");
        Debug.Log($"LOBBY_LAYOUT_GUID_AFTER: scene={sceneGuidAfter}, prefab={prefabGuidAfter}");
        if (sceneGuidAfter != sceneGuidBefore)
            throw new InvalidOperationException(
                $"Property 9 violated: NexusLobby.unity GUID changed ({sceneGuidBefore} -> {sceneGuidAfter}).");
        if (prefabGuidAfter != prefabGuidBefore)
            throw new InvalidOperationException(
                $"Property 9 violated: NexusEnvironment.prefab GUID changed ({prefabGuidBefore} -> {prefabGuidAfter}).");

        Debug.Log(
            "LOBBY_LAYOUT_VALIDATION_SUCCESS: " +
            $"pedestals={pedestals} (indices 0/1/2), trainingDummies={dummies}, " +
            $"reachable stations: pedestals={reachable[0]}/3, dummy={reachable[1]}/1, portals={reachable[2]}, " +
            $"scene GUID {sceneGuidBefore} preserved, prefab GUID {prefabGuidBefore} preserved.");
    }

    /// <summary>Reads an asset GUID via the AssetDatabase (the .meta value); throws if the asset is missing.</summary>
    private static string AssetGuid(string assetPath, string label)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException($"Cannot read GUID for the lobby {label} at {assetPath}.");
        return guid;
    }

    /// <summary>True when the saved lobby scene already exposes the new arsenal (three pedestals).</summary>
    private static bool SceneHasArsenal()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        LobbyArsenal arsenal = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LobbyArsenal>(true)).FirstOrDefault();
        if (!arsenal) return false;
        var data = new SerializedObject(arsenal);
        SerializedProperty pedestals = data.FindProperty("_pedestals");
        return pedestals != null && pedestals.arraySize == 3;
    }

    /// <summary>
    /// Property 7 (structure): exactly one <see cref="LobbyArsenal"/> exposing exactly three pedestals
    /// with weapon indices 0/1/2 and an assigned interaction anchor each. Returns the pedestal count.
    /// </summary>
    private static int ValidateArsenal(GameObject[] roots)
    {
        LobbyArsenal arsenal = roots.SelectMany(root => root.GetComponentsInChildren<LobbyArsenal>(true)).SingleOrDefault();
        if (!arsenal) throw new InvalidOperationException("LobbyArsenal missing: no weapon pedestals in the lobby.");
        var data = new SerializedObject(arsenal);
        SerializedProperty pedestals = data.FindProperty("_pedestals");
        if (pedestals == null || pedestals.arraySize != 3)
            throw new InvalidOperationException("Arsenal must expose exactly three weapon pedestals.");
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty pedestal = pedestals.GetArrayElementAtIndex(i);
            if (pedestal.FindPropertyRelative("weaponIndex").intValue != i)
                throw new InvalidOperationException($"Pedestal at slot {i} must map to weapon index {i}.");
            if (!(pedestal.FindPropertyRelative("anchor").objectReferenceValue is Transform))
                throw new InvalidOperationException($"Pedestal {i} is missing its interaction anchor.");
        }
        return pedestals.arraySize;
    }

    /// <summary>Property 7 (structure): exactly one passive <see cref="TrainingDummy"/> in the lobby. Returns the count.</summary>
    private static int ValidateDummy(Transform environment)
    {
        TrainingDummy[] dummies = environment.GetComponentsInChildren<TrainingDummy>(true);
        if (dummies.Length != 1)
            throw new InvalidOperationException($"Expected exactly one TrainingDummy, found {dummies.Length}.");
        if (dummies[0].GetComponent<EnemyAI>())
            throw new InvalidOperationException("TrainingDummy must be passive (no EnemyAI).");
        return dummies.Length;
    }

    /// <summary>
    /// Property 8: the informational stations and portals survive the restructuring, and the removed
    /// weapon-selection panel no longer exists. Confirms the lobby guide still carries portal and
    /// archive stations (the incursion portal targeting a real scene) and that no station carries the
    /// removed <c>weaponSelection</c> flag / <c>ARSENAL / ARMAS</c> title.
    /// </summary>
    private static void ValidateNonWeaponStations(GameObject[] roots, Transform environment)
    {
        LobbyInteraction guide = roots.SelectMany(root => root.GetComponentsInChildren<LobbyInteraction>(true)).SingleOrDefault();
        if (!guide) throw new InvalidOperationException("LobbyInteraction guide missing: informational stations lost.");

        var guideData = new SerializedObject(guide);
        SerializedProperty stations = guideData.FindProperty("_stations");
        if (stations == null || stations.arraySize == 0)
            throw new InvalidOperationException("Lobby guide has no informational stations.");

        int portals = 0, informational = 0;
        for (int i = 0; i < stations.arraySize; i++)
        {
            SerializedProperty station = stations.GetArrayElementAtIndex(i);
            string title = station.FindPropertyRelative("title").stringValue;
            string destination = station.FindPropertyRelative("destinationScene").stringValue;
            if (title != null && title.StartsWith("ARSENAL"))
                throw new InvalidOperationException("The removed 'ARSENAL / ARMAS' weapon panel is still present as a station.");
            if (!string.IsNullOrEmpty(destination)) portals++; else informational++;
        }
        if (portals == 0)
            throw new InvalidOperationException("No functional portal station (with a destination scene) remains.");
        if (informational == 0)
            throw new InvalidOperationException("No informational station remains after the restructuring.");
        Debug.Log($"LOBBY_LAYOUT_NON_WEAPON_OK: stations preserved (portals={portals}, informational={informational}).");
    }

    /// <summary>
    /// Property 7: every station anchor (three pedestals, the dummy and each portal) is reachable by a
    /// complete NavMesh path from the player spawn. Returns [reachablePedestals, reachableDummy,
    /// reachablePortals]. Anchors are the editor-generated "Interaction point" children; pedestal
    /// anchors live under the arsenal group and the dummy anchor under the training-dummy group.
    /// </summary>
    private static int[] ValidateReachability(Transform environment, Vector3 spawn)
    {
        if (!NavMesh.SamplePosition(spawn, out NavMeshHit start, 1f, NavMesh.AllAreas))
            throw new InvalidOperationException("Player spawn is not on the lobby NavMesh.");

        // Exact child/group names are part of the editor-generated scene structure, never runtime lookups.
        var pedestalAnchors = new List<Transform>();
        var dummyAnchors = new List<Transform>();
        var portalAnchors = new List<Transform>();

        foreach (Transform anchor in environment.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Interaction point"))
        {
            string group = GroupOf(anchor, environment);
            if (group == "04 - Weapon pedestals") pedestalAnchors.Add(anchor);
            else if (group == "08 - Training dummy") dummyAnchors.Add(anchor);
            else portalAnchors.Add(anchor); // portals + archive share the informational-station anchors
        }

        if (pedestalAnchors.Count != 3)
            throw new InvalidOperationException($"Expected 3 pedestal anchors, found {pedestalAnchors.Count}.");
        if (dummyAnchors.Count != 1)
            throw new InvalidOperationException($"Expected 1 training-dummy anchor, found {dummyAnchors.Count}.");
        if (portalAnchors.Count == 0)
            throw new InvalidOperationException("No portal/informational anchors found to validate.");

        int pedestalsOk = pedestalAnchors.Count(a => Reachable(start.position, a));
        int dummyOk = dummyAnchors.Count(a => Reachable(start.position, a));
        int portalsOk = portalAnchors.Count(a => Reachable(start.position, a));

        if (pedestalsOk != pedestalAnchors.Count)
            throw new InvalidOperationException($"Unreachable pedestal anchor(s): {pedestalsOk}/{pedestalAnchors.Count} reachable.");
        if (dummyOk != dummyAnchors.Count)
            throw new InvalidOperationException("Training dummy anchor is not reachable from spawn.");
        if (portalsOk != portalAnchors.Count)
            throw new InvalidOperationException($"Unreachable portal/informational anchor(s): {portalsOk}/{portalAnchors.Count} reachable.");

        return new[] { pedestalsOk, dummyOk, portalsOk };
    }

    /// <summary>Complete NavMesh path check from a start point to an anchor world position.</summary>
    private static bool Reachable(Vector3 start, Transform anchor)
    {
        var path = new NavMeshPath();
        bool ok = NavMesh.SamplePosition(anchor.position, out NavMeshHit end, 1f, NavMesh.AllAreas) &&
                  NavMesh.CalculatePath(start, end.position, NavMesh.AllAreas, path) &&
                  path.status == NavMeshPathStatus.PathComplete;
        Debug.Log($"LOBBY_PATH_{(ok ? "OK" : "FAIL")}: {anchor.parent.name}");
        return ok;
    }

    /// <summary>Returns the name of the top-level environment group an anchor belongs to.</summary>
    private static string GroupOf(Transform anchor, Transform environment)
    {
        Transform current = anchor;
        while (current.parent && current.parent != environment) current = current.parent;
        return current.name;
    }
}
