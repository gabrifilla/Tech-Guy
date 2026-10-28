using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit migration of the published sector, preserving the scene and asset GUIDs.</summary>
public static class NewRoomRunAuthoring
{
    [MenuItem("Tech Guy/Run Structure/Publish procedural new rooms")]
    public static void Publish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit Mode.");
        Scene scene = SceneManager.GetSceneByPath(FirstSectorBuilder.ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        var previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(FirstSectorBuilder.ScenePath, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("FirstSector has unsaved changes. Save the intended edits first.");
            var director = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ProgressionDirector>(true)).Single();
            Configure(scene, director);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("NEW_ROOM_RUN_PUBLISHED: old room geometry removed; seeded new rooms enabled for all three weapons.");
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    public static void Configure(Scene scene, ProgressionDirector director)
    {
        var definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(RunStructureAuthoring.DefinitionPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RunStructureAuthoring.PrefabPath);
        if (!definition || !prefab || !ManualRouteValidator.Validate(definition, out _, out string error))
            throw new InvalidOperationException("New room assets are unavailable or invalid.");
        var data = new SerializedObject(director);
        var environment = data.FindProperty("_manualEnvironment").objectReferenceValue as ManualStageEnvironment;
        if (!environment)
            environment = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<ManualStageEnvironment>();
        data.FindProperty("_manualDefinition").objectReferenceValue = definition;
        data.FindProperty("_manualEnvironment").objectReferenceValue = environment;
        data.FindProperty("_progressionMode").intValue = (int)RunProgressionMode.ManualSlice;
        data.FindProperty("_randomizeRooms").boolValue = true;
        data.ApplyModifiedPropertiesWithoutUndo();
        var oldEnvironment = data.FindProperty("_environment").objectReferenceValue as ProceduralStageEnvironment;
        if (oldEnvironment)
        {
            var oldData = new SerializedObject(oldEnvironment);
            var geometryProperty = oldData.FindProperty("_geometry");
            var geometry = geometryProperty.objectReferenceValue as Transform;
            if (geometry)
            {
                // Reject unknown scene references instead of leaving broken serialized links.
                foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
                {
                    if (!component || component is Transform || component == oldEnvironment || component.transform.IsChildOf(geometry)) continue;
                    var properties = new SerializedObject(component).GetIterator();
                    while (properties.Next(true))
                        if (properties.propertyType == SerializedPropertyType.ObjectReference && properties.objectReferenceValue)
                        {
                            var value = properties.objectReferenceValue;
                            var target = value is GameObject go ? go.transform : value is Component referenced ? referenced.transform : null;
                            if (target && (target == geometry || target.IsChildOf(geometry)))
                                throw new InvalidOperationException("Old rooms still referenced by " + component.name + ": " + properties.propertyPath);
                        }
                }
                geometryProperty.objectReferenceValue = null;
                oldData.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(geometry.gameObject);
            }
        }
        environment.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
