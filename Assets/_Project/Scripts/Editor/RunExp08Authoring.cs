using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit authoring command, restricted to the isolated review scene and new art folder.</summary>
public static class RunExp08Authoring
{
    public const string ScenePath = "Assets/_Project/Scenes/Review/RunExp08PainterlyRoom.unity";
    private const string Art = "Assets/_Project/Art/RunExp08";

    [MenuItem("Tools/Tech Guy/RUN-EXP-08/Build isolated painterly room")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var active = SceneManager.GetActiveScene();
        if (active.isDirty) throw new InvalidOperationException("Current scene has unsaved changes.");
        var paint = MaterialAt("MineralPigment", "PigmentStrata.png", Color.white);
        var floor = MaterialAt("QuietSlate", "QuietSlate.png", Color.white);
        var edge = MaterialAt("ForwardIvory", null, new Color(.62f, .68f, .54f));
        var closed = MaterialAt("ClosedInk", "PigmentStrata.png", new Color(.34f, .38f, .48f));
        var top = MaterialAt("CoverTop", "QuietSlate.png", new Color(.69f, .62f, .79f));
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) && !AssetDatabase.CopyAsset("Assets/_Project/Scenes/FirstSector.unity", ScenePath))
            throw new InvalidOperationException("Cannot copy baseline scene.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var director = roots.SelectMany(r => r.GetComponentsInChildren<ProgressionDirector>(true)).Single();
            foreach (var source in roots.SelectMany(r => r.GetComponentsInChildren<RunSeedSource>(true)))
            {
                var seed = new SerializedObject(source);
                seed.FindProperty("_randomizeEachRun").boolValue = false;
                seed.FindProperty("_seed").stringValue = "63";
                seed.ApplyModifiedPropertiesWithoutUndo();
            }
            var prototype = roots.SelectMany(r => r.GetComponentsInChildren<PainterlyRoomPrototype>(true)).SingleOrDefault();
            if (!prototype)
            {
                var go = new GameObject("RUN-EXP-08 / one room presentation only");
                SceneManager.MoveGameObjectToScene(go, scene);
                prototype = go.AddComponent<PainterlyRoomPrototype>();
            }
            var data = new SerializedObject(prototype);
            data.FindProperty("_director").objectReferenceValue = director;
            data.FindProperty("_paint").objectReferenceValue = paint;
            data.FindProperty("_floor").objectReferenceValue = floor;
            data.FindProperty("_edge").objectReferenceValue = edge;
            data.FindProperty("_closed").objectReferenceValue = closed;
            data.FindProperty("_coverTop").objectReferenceValue = top;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(active); }
        AssetDatabase.SaveAssets();
    }

    private static Material MaterialAt(string name, string texture, Color color)
    {
        var path = Art + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find(name == "QuietSlate" ? "Universal Render Pipeline/Lit" : "Tech Guy/Illustrated Toon")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .08f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
        if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", 0);
        if (material.HasProperty("_OutlineWidth")) material.SetShaderPassEnabled("SRPDefaultUnlit", false);
        if (material.HasProperty("_PigmentStrength")) material.SetFloat("_PigmentStrength", .015f);
        if (texture != null)
        {
            var file = Art + "/" + texture;
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
            importer.mipmapEnabled = true; importer.isReadable = false; importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(file));
        }
        EditorUtility.SetDirty(material); return material;
    }

    [MenuItem("Tools/Tech Guy/RUN-EXP-08/Open isolated room")]
    public static void Open() => EditorSceneManager.OpenScene(ScenePath);

    [MenuItem("Tools/Tech Guy/RUN-EXP-08/Toggle baseline and painterly in Play")]
    public static void Toggle()
    {
        var prototype = UnityEngine.Object.FindFirstObjectByType<PainterlyRoomPrototype>();
        if (prototype && prototype.Ready) prototype.SetPainterlyVisible(!prototype.PainterlyVisible);
    }
}
