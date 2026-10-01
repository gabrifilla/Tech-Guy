using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Spec project-cleanup-optimization, Task 10.1 — texture import-settings pass.
///
/// This tool reduces reported Texture Memory (Requirement 6.1) by adjusting import settings
/// through the <see cref="TextureImporter"/> API — the safe, GUID-preserving way Unity itself
/// edits these. It NEVER recreates assets, NEVER rewrites GUID lines, and NEVER touches
/// third-party content/code — it only changes importer fields (R6.5/R6.6).
///
/// Policy (R6.1/R6.2/R6.4):
///   - 3D textures (Default/NormalMap): mipmaps stay ON (disabling them on 3D causes shimmer
///     = a visual regression). The heaviest character textures (STYLIZED MALE CHARACTER, 2048px)
///     are capped at a lower <c>maxTextureSize</c> and set to platform-appropriate compression.
///   - UI/Sprite textures: mipmaps are turned OFF when unnecessary (saves ~33% with no visible
///     loss at usage resolution).
///   - Compression uses <see cref="TextureImporterCompression"/> (not a hardcoded format) so Unity
///     picks the correct per-platform format (BC/DXT on desktop, ETC/ASTC on mobile) — R6.1.
///
/// Everything is reversible and must be re-imported + measured by the user in the Unity Editor
/// and Profiler (R10): this CLI/host environment cannot drive the Editor import pipeline.
/// </summary>
public static class TextureImportOptimizer
{
    private const string MenuRoot = "Tech Guy/Cleanup/Texture Import/";

    // The heaviest set confirmed during investigation: STYLIZED MALE CHARACTER (81 textures, ~547 MB).
    private const string HeaviestPackageMarker = "/STYLIZED MALE CHARACTER/";

    // Conservative cap for character textures. Characters are rarely shown large enough on screen
    // to need 2048; 1024 cuts texture memory ~4x. Adjust in the Editor and verify visually (R6.4).
    private const int CharacterMaxSize = 1024;

    /// <summary>A single planned change, used for both the dry-run report and the applied pass.</summary>
    private readonly struct Plan
    {
        public readonly string Path;
        public readonly string Reason;
        public readonly bool IsThirdParty;
        public readonly int OldMaxSize;
        public readonly int NewMaxSize;
        public readonly bool OldMipmaps;
        public readonly bool NewMipmaps;
        public readonly TextureImporterType Type;

        public Plan(string path, string reason, bool thirdParty, int oldMax, int newMax,
            bool oldMip, bool newMip, TextureImporterType type)
        {
            Path = path;
            Reason = reason;
            IsThirdParty = thirdParty;
            OldMaxSize = oldMax;
            NewMaxSize = newMax;
            OldMipmaps = oldMip;
            NewMipmaps = newMip;
            Type = type;
        }

        public bool ChangesAnything => OldMaxSize != NewMaxSize || OldMipmaps != NewMipmaps;
    }

    [MenuItem(MenuRoot + "Report (dry run, no changes)", priority = 0)]
    public static void Report()
    {
        var plans = BuildPlans();
        var report = Render(plans, applied: false);
        var outPath = "Assets/_Project/texture-import-report.md";
        File.WriteAllText(outPath, report);
        AssetDatabase.ImportAsset(outPath);
        Debug.Log($"[TextureImportOptimizer] Dry-run complete. {plans.Count(p => p.ChangesAnything)} texture(s) " +
                  $"would change. Report written to {outPath}. No assets were modified.");
    }

    [MenuItem(MenuRoot + "Apply import settings", priority = 1)]
    public static void Apply()
    {
        var plans = BuildPlans().Where(p => p.ChangesAnything).ToList();
        if (plans.Count == 0)
        {
            Debug.Log("[TextureImportOptimizer] Nothing to change — all textures already match the policy.");
            return;
        }

        var ok = EditorUtility.DisplayDialog(
            "Optimize texture import settings",
            $"Apply import-setting changes to {plans.Count} texture(s)?\n\n" +
            "This edits only importer fields (maxTextureSize / compression / mipmaps) through the " +
            "TextureImporter API. GUIDs and references are preserved; assets are NOT recreated.\n\n" +
            "Re-import will regenerate the compressed textures. Verify appearance in-scene and " +
            "Texture Memory in the Profiler afterwards.",
            "Apply", "Cancel");
        if (!ok) return;

        var applied = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var plan in plans)
            {
                if (ApplyOne(plan)) applied++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        var report = Render(BuildPlans(), applied: true);
        var outPath = "Assets/_Project/texture-import-report.md";
        File.WriteAllText(outPath, report);
        AssetDatabase.ImportAsset(outPath);
        Debug.Log($"[TextureImportOptimizer] Applied import settings to {applied}/{plans.Count} texture(s). " +
                  $"Report written to {outPath}. Confirm Texture Memory reduction in the Profiler (R10).");
    }

    private static bool ApplyOne(Plan plan)
    {
        var importer = AssetImporter.GetAtPath(plan.Path) as TextureImporter;
        if (importer == null) return false;

        var changed = false;

        if (plan.NewMaxSize != plan.OldMaxSize)
        {
            importer.maxTextureSize = plan.NewMaxSize;
            changed = true;
        }

        if (plan.NewMipmaps != plan.OldMipmaps)
        {
            importer.mipmapEnabled = plan.NewMipmaps;
            changed = true;
        }

        // Ensure compressed (not uncompressed) on the default platform so memory actually drops.
        // TextureImporterCompression lets Unity choose the correct per-platform format (R6.1).
        if (importer.textureCompression == TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Compressed;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        return changed;
    }

    private static List<Plan> BuildPlans()
    {
        var plans = new List<Plan>();
        var guids = AssetDatabase.FindAssets("t:Texture2D");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var normalized = path.Replace('\\', '/');
            var isThirdParty = normalized.Contains("/_ThirdParty/");
            var oldMax = importer.maxTextureSize;
            var oldMip = importer.mipmapEnabled;

            int newMax = oldMax;
            bool newMip = oldMip;
            string reason;

            switch (importer.textureType)
            {
                case TextureImporterType.Sprite:
                    // Strictly UI/sprite: mipmaps are unnecessary; sharpness preserved at usage res (R6.2).
                    newMip = false;
                    reason = "UI/Sprite — mipmaps off (R6.2)";
                    break;

                default:
                    // 3D (Default/NormalMap/etc.): keep mipmaps ON to avoid shimmer (R6.4).
                    newMip = oldMip;
                    if (normalized.Contains(HeaviestPackageMarker) && oldMax > CharacterMaxSize)
                    {
                        newMax = CharacterMaxSize;
                        reason = $"3D character (heaviest set) — cap {oldMax}->{CharacterMaxSize} (R6.1)";
                    }
                    else
                    {
                        reason = "3D — mipmaps kept on; no size change";
                    }
                    break;
            }

            plans.Add(new Plan(normalized, reason, isThirdParty, oldMax, newMax, oldMip, newMip,
                importer.textureType));
        }

        return plans
            .OrderByDescending(p => p.ChangesAnything)
            .ThenByDescending(p => p.OldMaxSize)
            .ThenBy(p => p.Path, StringComparer.Ordinal)
            .ToList();
    }

    private static string Render(List<Plan> plans, bool applied)
    {
        var sb = new StringBuilder();
        var changing = plans.Where(p => p.ChangesAnything).ToList();
        var verb = applied ? "Applied" : "Planned";

        sb.AppendLine("# Texture import-settings pass — Task 10.1");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now.ToString("u", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Mode: {(applied ? "APPLIED" : "DRY RUN (no assets modified)")}");
        sb.AppendLine();
        sb.AppendLine($"- Textures scanned: {plans.Count}");
        sb.AppendLine($"- Textures {verb.ToLowerInvariant()} to change: {changing.Count}");
        sb.AppendLine();
        sb.AppendLine("Policy: 3D textures keep mipmaps ON (no shimmer); the heaviest character set is " +
                      $"capped to {CharacterMaxSize}px; UI/Sprite textures drop mipmaps. Compression uses the " +
                      "per-platform Compressed format. GUIDs/.meta preserved; assets not recreated (R6.5/R6.6).");
        sb.AppendLine();
        sb.AppendLine("> Texture Memory reduction must be confirmed by the user in the Unity Editor + Profiler (R10).");
        sb.AppendLine();

        sb.AppendLine($"## {verb} changes");
        sb.AppendLine();
        if (changing.Count == 0)
        {
            sb.AppendLine("_No changes — everything already matches the policy._");
        }
        else
        {
            sb.AppendLine("| Texture | 3rd-party | type | maxSize | mipmaps | reason |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var p in changing)
            {
                var size = p.OldMaxSize == p.NewMaxSize ? p.OldMaxSize.ToString() : $"{p.OldMaxSize}->{p.NewMaxSize}";
                var mip = p.OldMipmaps == p.NewMipmaps ? (p.OldMipmaps ? "on" : "off")
                    : $"{(p.OldMipmaps ? "on" : "off")}->{(p.NewMipmaps ? "on" : "off")}";
                sb.AppendLine($"| {p.Path} | {(p.IsThirdParty ? "yes" : "no")} | {p.Type} | {size} | {mip} | {p.Reason} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Atlas (R6.3)");
        sb.AppendLine();
        sb.AppendLine("No atlas was applied. The heaviest cost is the 3D character texture set, where atlasing " +
                      "offers no draw-call win and risks edge bleeding. Sprite atlasing is only worth it where a " +
                      "demonstrable draw-call reduction exists with no visual loss — none was demonstrable here.");

        return sb.ToString();
    }
}
