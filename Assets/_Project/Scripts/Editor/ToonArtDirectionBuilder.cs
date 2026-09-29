using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Applies the illustrated look using project-owned copies, preserving source assets and scene layout.</summary>
public static class ToonArtDirectionBuilder
{
    public const string ShaderName = "Tech Guy/Illustrated Toon";
    public const string ArtFolder = "Assets/_Project/Art/Toon";
    private const string MaterialsFolder = ArtFolder + "/Materials";
    private const string VolumeName = "Illustrated art direction";

    [MenuItem("Tools/Tech Guy/Art/Apply Illustrated Look")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before applying art direction.");
        for (int i=0;i<SceneManager.sceneCount;i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scene edits before applying art direction.");
        Shader shader = Shader.Find(ShaderName);
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Illustrated Toon shader must compile first.");
        Directory.CreateDirectory(MaterialsFolder);
        AssetDatabase.Refresh();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        int rendererCount = 0;
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs", "Assets/_Project/Resources" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = ApplyRenderers(root, shader);
                    if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root,path);
                    rendererCount += changed;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            VolumeProfile profile = CreateProfile();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots) rendererCount += ApplyRenderers(root,shader);
                bool hasWorld = roots.Any(r => r.GetComponentsInChildren<Renderer>(true).Any(v => v is MeshRenderer || v is SkinnedMeshRenderer));
                if (hasWorld) ApplyLighting(scene,profile);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ILLUSTRATED_LOOK_APPLIED: " + rendererCount + " renderers; source materials and geometry preserved.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static int ApplyRenderers(GameObject root, Shader shader)
    {
        int count = 0;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i=0; i<materials.Length; i++)
            {
                var replacement = ConvertMaterial(materials[i], shader, renderer is SkinnedMeshRenderer);
                if (replacement == materials[i]) continue;
                materials[i] = replacement; changed = true;
            }
            if (!changed) continue;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            count++;
        }
        return count;
    }

    private static Material ConvertMaterial(Material source, Shader shader, bool character)
    {
        if (!source || !source.shader) return source;
        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (sourcePath.StartsWith(ArtFolder+"/",StringComparison.Ordinal)) return source;
        string originalShader = source.shader.name;
        bool unlit = originalShader == "Universal Render Pipeline/Unlit";
        if (!unlit && originalShader != "Universal Render Pipeline/Lit" && originalShader != "Standard") return source;
        if (source.renderQueue >= 3000 || (source.HasProperty("_Surface") && source.GetFloat("_Surface") > .5f) ||
            (source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > .5f)) return source;
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long localId)) return source;
        string safeName = string.Concat(source.name.Select(c => char.IsLetterOrDigit(c) || c==' ' || c=='_' ? c : '_'));
        string path = MaterialsFolder+"/"+safeName+"_"+guid.Substring(0,8)+"_"+localId+(character?"_Actor":"")+".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        var result = new Material(unlit ? source.shader : shader) { name = source.name+" Illustrated", enableInstancing=true };
        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
        result.SetColor("_BaseColor",Palette(source.name,color));
        string textureProperty = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        if (source.HasProperty(textureProperty))
        {
            result.SetTexture("_BaseMap",source.GetTexture(textureProperty));
            result.SetTextureScale("_BaseMap",source.GetTextureScale(textureProperty));
            result.SetTextureOffset("_BaseMap",source.GetTextureOffset(textureProperty));
        }
        if (!unlit)
        {
            result.SetColor("_ShadowColor", new Color(.3f,.41f,.46f));
            result.SetColor("_HighlightColor",new Color(1.05f,1f,.87f));
            result.SetFloat("_OutlineWidth",character ? .022f : .014f);
            result.SetFloat("_PigmentStrength",character ? .025f : .10f);
            if (source.IsKeywordEnabled("_EMISSION") && source.HasProperty("_EmissionColor"))
            {
                result.SetColor("_EmissionColor",source.GetColor("_EmissionColor"));
                result.SetTexture("_EmissionMap",source.GetTexture("_EmissionMap"));
            }
        }
        AssetDatabase.CreateAsset(result,path);
        return result;
    }

    private static Color Palette(string name, Color fallback)
    {
        switch (name)
        {
            case "Midnight Deck": case "Basalt alloy": case "Deck": return new Color(.24f,.32f,.29f);
            case "Floor panels": case "Blue Steel": return new Color(.38f,.44f,.36f);
            case "Obsidian Alloy": case "Graphite": case "Structure": return new Color(.095f,.17f,.18f);
            case "Steel": case "Brushed steel": case "Trim": return new Color(.58f,.57f,.39f);
            case "Signal Cyan": case "Guide cyan": case "Signal": return new Color(.17f,.73f,.60f);
            case "Gauntlet Amber": case "Extraction gold": case "Amber ceramic": return new Color(1f,.64f,.16f);
            case "Glitch Magenta": case "Corrupted signal": return new Color(.8f,.18f,.29f);
            case "Distant Data": return new Color(.045f,.085f,.1f);
            default: return fallback;
        }
    }

    private static VolumeProfile CreateProfile()
    {
        string path = ArtFolder+"/IllustratedAtmosphere.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile) return profile;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile,path);
        var colors = profile.Add<ColorAdjustments>(true);
        colors.contrast.Override(14); colors.saturation.Override(8); colors.postExposure.Override(.05f);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(.18f); bloom.threshold.Override(1.15f); bloom.scatter.Override(.45f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(.22f); vignette.smoothness.Override(.45f);
        vignette.color.Override(new Color(.02f,.055f,.055f));
        foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ApplyLighting(Scene scene, VolumeProfile profile)
    {
        var roots = scene.GetRootGameObjects();
        var lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Where(l => l.type==LightType.Directional).ToArray();
        for (int i=0;i<lights.Length;i++)
        {
            lights[i].color = i==0 ? new Color(1f,.96f,.86f) : new Color(.32f,.65f,.64f);
            lights[i].intensity = i==0 ? 1.15f : .35f;
            lights[i].shadows = i==0 ? LightShadows.Hard : LightShadows.None;
            if (i==0) lights[i].transform.rotation = Quaternion.Euler(50,-35,0);
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.30f,.38f,.39f);
        RenderSettings.fogColor = new Color(.025f,.065f,.075f);
        var volume = roots.SelectMany(r => r.GetComponentsInChildren<Volume>(true)).FirstOrDefault(v => v.name==VolumeName);
        if (!volume) volume = new GameObject(VolumeName).AddComponent<Volume>();
        volume.isGlobal=true; volume.priority=20; volume.sharedProfile=profile;
        foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
        {
            camera.backgroundColor = new Color(.025f,.065f,.075f);
            var cameraData=camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing=true;
            cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality=AntialiasingQuality.High;
        }
    }
}
