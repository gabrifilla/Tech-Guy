using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Creates native assets and updates only the existing lobby workbench.</summary>
public static class ArsenalBuilder
{
    private const string Root = "Assets/_Project/ScriptableObjects/Arsenal";
    private const string Prefabs = "Assets/_Project/Prefabs/Arsenal";

    [MenuItem("Tools/Tech Guy/Arsenal/Build Arsenal")]
    public static void Build()
    {
        Folder(Root);
        Folder(Prefabs);
        var blue = new Color(.2f, .8f, 1f);
        var red = new Color(1f, .4f, .3f);
        WeaponScript bow = Asset<WeaponScript>("Assets/_Project/Resources/Weapons/Ranged/Bow_arrow/Bow.asset");
        bow.weaponName = "Arco e flecha";
        bow.attackDamage = 13;
        bow.attackSpeed = .65f;
        bow.attackDistance = 12;
        bow.attackDelay = 0;
        bow.weaponPrefab = BuildModel(true, blue);
        var data = new SerializedObject(bow);
        data.FindProperty("_firesArrows").boolValue = true;
        data.ApplyModifiedPropertiesWithoutUndo();
        bow.abilities = new Ability[]
        {
            Skill("BowRapid", "Disparo duplo", "Duas flechas rápidas na direção do cursor.", ArsenalSkillKind.Arrow, 4, 80, .12f, 2, .15f, 14, 1, .85f, false, blue),
            Skill("BowCharged", "Tiro concentrado", "Prepara por 0,7s uma flecha que atravessa inimigos.", ArsenalSkillKind.Arrow, 8, 150, .7f, 1, .15f, 18, 1, 3.2f, true, blue),
            Skill("BowVolley", "Leque de flechas", "Cinco flechas em leque para atingir grupos.", ArsenalSkillKind.Volley, 10, 180, .25f, 1, .2f, 12, 1, 1.1f, false, blue),
            Skill("BowRain", "Chuva de flechas", "Quatro pulsos na área do cursor, até 12m.", ArsenalSkillKind.Rain, 14, 240, .4f, 4, .35f, 12, 2.6f, .8f, false, blue)
        };
        EditorUtility.SetDirty(bow);
        WeaponScript spear = Asset<WeaponScript>("Assets/_Project/Resources/Weapons/Melee/Spear/Spear.asset");
        spear.weaponName = "Lança";
        spear.attackDamage = 17;
        spear.attackSpeed = .6f;
        spear.attackDistance = 3.6f;
        spear.attackDelay = 0;
        spear.attackBoxSize = new Vector3(.9f, 2, 3.6f);
        spear.weaponPrefab = BuildModel(false, red);
        spear.abilities = new Ability[]
        {
            Skill("SpearThrust", "Estocada do dragão", "Golpe frontal estreito com alcance de 5m.", ArsenalSkillKind.Thrust, 4, 80, .15f, 1, .18f, 5, .9f, 1.6f, false, red),
            Skill("SpearSweep", "Lua crescente", "Varredura circular de 3m ao redor do personagem.", ArsenalSkillKind.Sweep, 7, 120, .25f, 1, .2f, 3, 3, 1.8f, false, red),
            Skill("SpearFlurry", "Rajada espiral", "Três estocadas consecutivas na mesma direção.", ArsenalSkillKind.Thrust, 10, 180, .18f, 3, .18f, 4.5f, 1.3f, .9f, false, red),
            Skill("SpearDragon", "Dragão vermelho", "Prepara por 0,65s uma estocada poderosa de 7m.", ArsenalSkillKind.Thrust, 14, 240, .65f, 1, .25f, 7, 1.4f, 4f, false, red)
        };
        EditorUtility.SetDirty(spear);
        AssetDatabase.SaveAssets();
        InstallLobby();
        Debug.Log("ARSENAL_BUILD_SUCCESS: three weapons, eight new skills, lobby selection installed.");
    }

    private static ArsenalAbility Skill(string id, string title, string description, ArsenalSkillKind kind,
        float cooldown, float mana, float windup, int hits, float interval, float range, float width,
        float damage, bool piercing, Color color)
    {
        var skill = Asset<ArsenalAbility>(Root + "/" + id + ".asset");
        skill.name = title;
        skill.cooldownTime = cooldown;
        skill.activeTime = windup + hits * interval;
        var data = new SerializedObject(skill);
        data.FindProperty("_displayName").stringValue = title;
        data.FindProperty("_description").stringValue = description;
        data.FindProperty("_manaCost").floatValue = mana;
        data.FindProperty("_glyph").enumValueIndex = (int)(id.StartsWith("Bow") ? SkillGlyphKind.Arrow : SkillGlyphKind.Spear);
        data.FindProperty("_accentColor").colorValue = color;
        data.FindProperty("_kind").enumValueIndex = (int)kind;
        data.FindProperty("_windup").floatValue = windup;
        data.FindProperty("_hits").intValue = hits;
        data.FindProperty("_interval").floatValue = interval;
        data.FindProperty("_range").floatValue = range;
        data.FindProperty("_width").floatValue = width;
        data.FindProperty("_damageMultiplier").floatValue = damage;
        data.FindProperty("_piercing").boolValue = piercing;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static GameObject BuildModel(bool bow, Color color)
    {
        string name = bow ? "NexusBow" : "NexusSpear";
        string path = Prefabs + "/" + name + ".prefab";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(Prefabs + "/" + name + ".mat");
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, Prefabs + "/" + name + ".mat");
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        var root = new GameObject(name);
        try
        {
            if (bow)
            {
                Vector3 previous = new Vector3(0, -.65f, 0);
                for (int i = 1; i <= 8; i++)
                {
                    float angle = i / 8f * Mathf.PI;
                    Vector3 next = new Vector3(0, -Mathf.Cos(angle) * .65f, Mathf.Sin(angle) * .3f);
                    Segment(root.transform, previous, next, .055f, material);
                    previous = next;
                }
                Segment(root.transform, new Vector3(0,-.65f,0), new Vector3(0,.65f,0), .012f, material);
            }
            else
            {
                Segment(root.transform, new Vector3(0,0,-1.1f), new Vector3(0,0,1.3f), .035f, material);
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(blade.GetComponent<Collider>());
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = new Vector3(0,0,1.35f);
                blade.transform.localScale = new Vector3(.18f,.035f,.45f);
                blade.transform.localRotation = Quaternion.Euler(0,0,45);
                blade.GetComponent<Renderer>().sharedMaterial = material;
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Segment(Transform parent, Vector3 from, Vector3 to, float radius, Material material)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(segment.GetComponent<Collider>());
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = (from + to) * .5f;
        segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        segment.transform.localScale = new Vector3(radius * 2, Vector3.Distance(from,to) * .5f, radius * 2);
        segment.GetComponent<Renderer>().sharedMaterial = material;
    }

    public static void InstallLobby()
    {
        Scene scene = SceneManager.GetSceneByPath(LobbySceneBuilder.ScenePath);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("Save the lobby scene before installing the arsenal.");
        if (opened) scene = EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath, OpenSceneMode.Additive);
        try
        {
            LobbyInteraction guide = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyInteraction>(true)).Single();
            var data = new SerializedObject(guide);
            SerializedProperty stations = data.FindProperty("_stations");
            bool found = false;
            for (int i = 0; i < stations.arraySize; i++)
            {
                var station = stations.GetArrayElementAtIndex(i);
                string title = station.FindPropertyRelative("title").stringValue;
                if (!title.Contains("MANOPLA") && !title.Contains("ARSENAL")) continue;
                found = true;
                station.FindPropertyRelative("weaponSelection").boolValue = true;
                station.FindPropertyRelative("title").stringValue = "ARSENAL / ARMAS";
                station.FindPropertyRelative("description").stringValue = "Escolha manopla, arco e flecha ou lança antes da incursão.";
                var anchor = (Transform)station.FindPropertyRelative("anchor").objectReferenceValue;
                Transform workshop = anchor.parent;
                foreach (TextMesh label in workshop.GetComponentsInChildren<TextMesh>())
                    if (label.text.Contains("NEURAL") || label.text.Contains("MANOPLA")) label.text = "ARSENAL / ARMAS";
                if (!workshop.Find("Arsenal displays"))
                {
                    var displays = new GameObject("Arsenal displays");
                    displays.transform.SetParent(workshop, false);
                    for (int j = 0; j < 2; j++)
                    {
                        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + (j == 0 ? "/NexusBow.prefab" : "/NexusSpear.prefab"));
                        var display = (GameObject)PrefabUtility.InstantiatePrefab(model, displays.transform);
                        display.transform.localPosition = new Vector3(j == 0 ? -1.7f : 1.7f, 2.5f, 0);
                        display.transform.localRotation = Quaternion.Euler(j == 0 ? 0 : -70, 0, 0);
                        display.AddComponent<LobbyCoreMotion>();
                    }
                }
            }
            if (!found) throw new InvalidOperationException("Lobby workbench station not found.");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static T Asset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        Folder(path.Substring(0,slash));
        AssetDatabase.CreateFolder(path.Substring(0,slash), path.Substring(slash+1));
    }
}
