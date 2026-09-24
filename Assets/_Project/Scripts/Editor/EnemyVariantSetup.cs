using UnityEditor;
using UnityEngine;

public static class EnemyVariantSetup
{
    private const string Folder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string AuraPath = "Assets/_Project/Prefabs/EnemyFrostAura.prefab";

    [MenuItem("Tools/Tech Guy/Enemies/Create Example Profiles")]
    public static void CreateExamples()
    {
        EnsureFolder("Assets/_Project/ScriptableObjects");
        EnsureFolder(Folder);
        EnsureFolder("Assets/_Project/Prefabs");
        GameObject aura = AssetDatabase.LoadAssetAtPath<GameObject>(AuraPath);
        if (!aura)
        {
            var root = new GameObject("EnemyFrostAura");
            try
            {
                root.AddComponent<EnemyFrostAura>();
                aura = PrefabUtility.SaveAsPrefabAsset(root, AuraPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
        CreateProfile("Normal", EnemyRarity.Normal, 1f, 1f, 1f, 1f, null);
        CreateProfile("Magic", EnemyRarity.Magic, 2f, 1.3f, 1.1f, 1.1f, null);
        MigrateRareProfile(aura);
        CreateProfile("Rare", EnemyRarity.Rare, 4f, 1.6f, 1.15f, 1.2f, null);
        AssetDatabase.SaveAssets();
        Debug.Log("Enemy profiles ready in " + Folder);
    }

    [MenuItem("Tools/Tech Guy/Enemies/Add Variant To Selected Enemy")]
    private static void AddToSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (!selected || !selected.GetComponent<EnemyAI>() || !selected.GetComponent<Actor>())
        {
            Debug.LogWarning("Select the enemy object containing Actor and EnemyAI.");
            return;
        }
        CreateExamples();
        EnemyVariant variant = selected.GetComponent<EnemyVariant>();
        if (!variant) variant = Undo.AddComponent<EnemyVariant>(selected);
        var serialized = new SerializedObject(variant);
        if (serialized.FindProperty("_profile").objectReferenceValue == null)
        {
            serialized.FindProperty("_profile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<EnemyProfile>(Folder + "/Normal.asset");
            serialized.ApplyModifiedProperties();
        }
        Selection.activeObject = selected;
    }

    private static void MigrateRareProfile(GameObject aura)
    {
        string oldPath = Folder + "/RareFrost.asset";
        EnemyProfile oldProfile = AssetDatabase.LoadAssetAtPath<EnemyProfile>(oldPath);
        if (!oldProfile || AssetDatabase.LoadAssetAtPath<EnemyProfile>(Folder + "/Rare.asset")) return;
        string error = AssetDatabase.RenameAsset(oldPath, "Rare");
        if (!string.IsNullOrEmpty(error)) throw new System.InvalidOperationException(error);
        var serialized = new SerializedObject(oldProfile);
        SerializedProperty affixes = serialized.FindProperty("_affixPrefabs");
        for (int i = affixes.arraySize - 1; i >= 0; i--)
        {
            if (affixes.GetArrayElementAtIndex(i).objectReferenceValue != aura) continue;
            affixes.GetArrayElementAtIndex(i).objectReferenceValue = null;
            affixes.DeleteArrayElementAtIndex(i);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateProfile(string name, EnemyRarity rarity, float health, float damage,
        float movement, float attackSpeed, GameObject aura)
    {
        string path = Folder + "/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyProfile>(path)) return;
        EnemyProfile profile = ScriptableObject.CreateInstance<EnemyProfile>();
        var serialized = new SerializedObject(profile);
        serialized.FindProperty("_rarity").enumValueIndex = (int)rarity;
        serialized.FindProperty("_healthMultiplier").floatValue = health;
        serialized.FindProperty("_damageMultiplier").floatValue = damage;
        serialized.FindProperty("_movementMultiplier").floatValue = movement;
        serialized.FindProperty("_attackSpeedMultiplier").floatValue = attackSpeed;
        if (aura)
        {
            SerializedProperty affixes = serialized.FindProperty("_affixPrefabs");
            affixes.arraySize = 1;
            affixes.GetArrayElementAtIndex(0).objectReferenceValue = aura;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(profile, path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
    }
}
