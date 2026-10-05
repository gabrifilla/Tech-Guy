using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit Phase 2 authoring migration. Never runs on import or in OnValidate.</summary>
public static class PostureReactionMigration
{
    public static string Preview()
    {
        var lines = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponScript", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(path);
            lines.Add(path + ": " + (IsGauntlet(path) ? weapon.OverridesBasicReaction ? "already migrated" : "basic Stagger, contribution 0.35/0.45/0.6" : "preserve weapon"));
        }
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyProfile", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
            lines.Add(path + ": category=" + profile.ReactionCategory + (AuthoredCategory(path) == ReactionCategory.Unspecified ? "; preserve fallback/custom" : "; candidate=" + AuthoredCategory(path)));
        }
        // Inventory every project-owned scene/prefab. No scene opening/saving, no instance override removal.
        foreach (string kind in new[] { "t:Scene", "t:Prefab" })
            foreach (string guid in AssetDatabase.FindAssets(kind, new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string text = File.ReadAllText(path);
                if (!text.Contains("defaultAttackReaction:") && !text.Contains("reactionType:")) continue;
                lines.Add(path + ": preserve serialized reactions and combo overrides; weapon snapshot supplies default only");
                foreach (string line in text.Split('\n'))
                    if (line.Contains("defaultAttackReaction:") || line.Contains("comboReactions:") || line.Contains("reactionType:")) lines.Add("  " + line.Trim());
            }
        return string.Join("\n", lines);
    }

    public static int Apply()
    {
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponScript", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsGauntlet(path) && MigrateWeapon(AssetDatabase.LoadAssetAtPath<WeaponScript>(path))) changed++;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyProfile", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var category = AuthoredCategory(path);
            if (category != ReactionCategory.Unspecified && MigrateProfile(AssetDatabase.LoadAssetAtPath<EnemyProfile>(path), category)) changed++;
        }
        AssetDatabase.SaveAssets();
        return changed;
    }
    private static bool IsGauntlet(string path) => path == "Assets/_Project/Resources/Weapons/Melee/Gauntlet/Gauntlet.asset";
    private static ReactionCategory AuthoredCategory(string path)
    {
        if (path == "Assets/_Project/ScriptableObjects/Enemies/Archetypes/Heavy.asset") return ReactionCategory.MediumMob;
        if (path == "Assets/_Project/ScriptableObjects/Enemies/Archetypes/Grunt.asset" ||
            path == "Assets/_Project/ScriptableObjects/Enemies/Archetypes/Fragile.asset") return ReactionCategory.SmallMob;
        return ReactionCategory.Unspecified;
    }
    public static bool MigrateWeapon(WeaponScript weapon)
    {
        var so = new SerializedObject(weapon);
        if (so.FindProperty("_basicReactionVersion").intValue >= 1) return false;
        so.FindProperty("_basicReactionVersion").intValue = 1;
        so.FindProperty("_overrideBasicReaction").boolValue = true;
        so.FindProperty("_basicReaction").enumValueIndex = (int)HitReactionType.Stagger;
        so.FindProperty("_basicInterruptContribution").floatValue = 0.35f;
        var stages = so.FindProperty("_basicStageContributions"); stages.arraySize = 3;
        for (int i = 0; i < 3; i++) stages.GetArrayElementAtIndex(i).floatValue = new[] { 0.35f, 0.45f, 0.6f }[i];
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(weapon); return true;
    }
    public static bool MigrateProfile(EnemyProfile profile, ReactionCategory category)
    {
        var so = new SerializedObject(profile);
        var data = so.FindProperty("_reaction");
        if (data.FindPropertyRelative("_version").intValue >= 1) return false;
        // Preserve a preexisting explicit category rather than assuming all legacy data is absent.
        var declared = so.FindProperty("_reactionCategory");
        if (declared.intValue == 0) declared.intValue = (int)category;
        data.FindPropertyRelative("_version").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(profile); return true;
    }
    [MenuItem("Tools/Tech Guy/Posture Phase 2/Preview migration")]
    private static void PreviewMenu() => Debug.Log(Preview());
    [MenuItem("Tools/Tech Guy/Posture Phase 2/Apply migration")]
    private static void ApplyMenu() => Debug.Log("Posture Phase 2 migrated assets: " + Apply());
}
