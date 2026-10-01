using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TechGuy.EditorTools.Cleanup
{
    /// <summary>
    /// Spec project-cleanup-optimization, Task 11.3 — the Editor-side "identify" step of the
    /// cleanup gate (design §10). This tool ONLY produces a readable report; it removes NOTHING.
    /// The removal step (item-by-item, user-confirmed) is a separate task (13.1) per R7.4.
    ///
    /// What it does:
    ///   1. Enumerates every asset under <c>Assets/</c> via <see cref="AssetDatabase"/>.
    ///   2. Reads the project's reference files (<c>.unity</c>/<c>.prefab</c>/<c>.asset</c>/<c>.meta</c>)
    ///      from disk once, and feeds their raw text to the pure <see cref="AssetReferenceScanner"/>
    ///      to decide, per asset GUID, whether a tracked reference exists (R7.1/R7.5).
    ///   3. Classifies each unreferenced asset using the same guards the design assigns to
    ///      <c>OrphanClassifier</c> (task 11.2): assets under a <c>Resources</c> folder are never
    ///      called pure orphans (they may be <c>Resources.Load</c> targets by path, R7.2); whitelisted
    ///      packages proven in use (TextMesh Pro by the UI, QuickOutline by <c>OutlineScript</c>) are
    ///      never proposed for removal (R7.7).
    ///   4. For third-party packages it distinguishes "remoção total do pacote" from "trim parcial"
    ///      (R7.8), highlighting "STYLIZED MALE CHARACTER" (~565 MB) as the primary candidate.
    ///   5. Writes a versionable report artifact following the exact format in design §10.
    ///
    /// The guard/whitelist policy is kept inline here so the report tool is self-contained and runnable
    /// before the pure <c>OrphanClassifier</c> (11.2) is finalized; once that type lands it can own the
    /// policy and this tool can delegate to it without changing the report format.
    /// </summary>
    public static class ProjectCleanupReport
    {
        private const string MenuItemPath = "Tech Guy/Cleanup/Orphan Report (dry run, no changes)";
        private const string ReportPath = "Assets/_Project/project-cleanup-report.md";

        // The confirmed heaviest third-party package (design §10 highlight: ~565 MB).
        private const string HeaviestPackageName = "STYLIZED MALE CHARACTER";

        // Packages proven in use — never proposed for removal (R7.7).
        private static readonly (string Package, string UsedBy)[] WhitelistedPackages =
        {
            ("TextMesh Pro", "usado pela UI"),
            ("QuickOutline", "usado por OutlineScript"),
        };

        // Extensions whose text content Unity uses to store cross-asset GUID references.
        private static readonly string[] ReferenceExtensions = { ".unity", ".prefab", ".asset", ".meta" };

        /// <summary>How a candidate is categorized in the report.</summary>
        private enum Category
        {
            Orphan,              // no GUID reference found anywhere
            RequiresPathCheck,   // under Resources/ — possible Resources.Load target
        }

        private readonly struct Candidate
        {
            public readonly string Path;
            public readonly long SizeBytes;
            public readonly Category Category;

            public Candidate(string path, long sizeBytes, Category category)
            {
                Path = path;
                SizeBytes = sizeBytes;
                Category = category;
            }
        }

        private readonly struct ThirdPartyPackage
        {
            public readonly string Name;
            public readonly long SizeBytes;
            public readonly bool AnySubAssetReferenced;
            public readonly IReadOnlyList<string> ReferencedSubAssets;
            public readonly bool IsWhitelisted;
            public readonly string WhitelistReason;

            public ThirdPartyPackage(string name, long sizeBytes, bool anyReferenced,
                IReadOnlyList<string> referencedSubAssets, bool isWhitelisted, string whitelistReason)
            {
                Name = name;
                SizeBytes = sizeBytes;
                AnySubAssetReferenced = anyReferenced;
                ReferencedSubAssets = referencedSubAssets ?? Array.Empty<string>();
                IsWhitelisted = isWhitelisted;
                WhitelistReason = whitelistReason;
            }

            /// <summary>
            /// A package qualifies for "remoção total" only when NONE of its sub-assets are referenced;
            /// if any sub-asset is referenced it becomes a "trim parcial" candidate (R7.8).
            /// </summary>
            public string Classification => AnySubAssetReferenced ? "trim parcial" : "remoção total";
        }

        [MenuItem(MenuItemPath)]
        public static void GenerateReport()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Project Cleanup Report", "Reading reference files...", 0.1f);
                var referenceFiles = ReadReferenceFiles();

                EditorUtility.DisplayProgressBar("Project Cleanup Report", "Scanning asset references...", 0.5f);
                var report = BuildReport(referenceFiles);

                File.WriteAllText(ReportPath, report);
                AssetDatabase.ImportAsset(ReportPath);
                Debug.Log($"[ProjectCleanupReport] Report written to {ReportPath}. " +
                          "This tool only reports — no assets were modified or removed (R7.4).");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectCleanupReport] Failed to generate report: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Reads every <c>.unity</c>/<c>.prefab</c>/<c>.asset</c>/<c>.meta</c> file under <c>Assets/</c>
        /// into memory as <see cref="ReferenceFile"/> records the pure scanner can consume.
        /// </summary>
        private static List<ReferenceFile> ReadReferenceFiles()
        {
            var files = new List<ReferenceFile>();
            var assetsRoot = Application.dataPath; // absolute path to <project>/Assets

            foreach (var absolute in Directory.EnumerateFiles(assetsRoot, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(absolute);
                if (!ReferenceExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;

                string content;
                try
                {
                    content = File.ReadAllText(absolute);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ProjectCleanupReport] Could not read '{absolute}': {e.Message}");
                    continue;
                }

                files.Add(new ReferenceFile(ToProjectRelative(absolute), content));
            }

            return files;
        }

        private static string BuildReport(List<ReferenceFile> referenceFiles)
        {
            // Build a quick lookup of each file's own GUID so an asset's own .meta is excluded from its
            // own reference scan (otherwise every asset would look referenced by itself).
            var guidToOwnMeta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var candidates = new List<Candidate>();
            var allAssetGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });

            // Precompute, per asset, its GUID -> own .meta path so we can exclude it from the scan.
            foreach (var guid in allAssetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    guidToOwnMeta[guid] = path + ".meta";
                }
            }

            foreach (var guid in allAssetGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var normalized = assetPath.Replace('\\', '/');

                // Skip folders — only concrete assets are removal candidates.
                if (AssetDatabase.IsValidFolder(assetPath)) continue;

                // Whitelisted packages and third-party packages are reported separately below.
                if (IsThirdParty(normalized)) continue;

                // Scan every reference file except this asset's own .meta.
                var ownMeta = guidToOwnMeta.TryGetValue(guid, out var m) ? m : null;
                var scanInput = referenceFiles.Where(f =>
                    !string.Equals(f.Path, ownMeta, StringComparison.OrdinalIgnoreCase));

                var result = AssetReferenceScanner.Scan(guid, scanInput);
                if (!result.IsOrphanCandidate) continue; // referenced — not a candidate (R7.1)

                var category = IsUnderResources(normalized) ? Category.RequiresPathCheck : Category.Orphan;
                candidates.Add(new Candidate(normalized, GetFileSize(normalized), category));
            }

            var thirdParty = BuildThirdPartyPackages(referenceFiles, guidToOwnMeta);

            return Render(candidates, thirdParty);
        }

        /// <summary>
        /// Builds the third-party package rows. For each package it determines whether ANY of its
        /// sub-assets is referenced from outside the package (which turns "remoção total" into
        /// "trim parcial", R7.8) and marks whitelisted packages as kept (R7.7).
        /// </summary>
        private static List<ThirdPartyPackage> BuildThirdPartyPackages(
            List<ReferenceFile> referenceFiles, Dictionary<string, string> guidToOwnMeta)
        {
            var packages = new List<ThirdPartyPackage>();
            const string thirdPartyRoot = "Assets/_ThirdParty";

            if (!AssetDatabase.IsValidFolder(thirdPartyRoot))
            {
                return packages;
            }

            var packageFolders = AssetDatabase.GetSubFolders(thirdPartyRoot);
            foreach (var packageFolder in packageFolders)
            {
                var packageName = Path.GetFileName(packageFolder);
                var whitelist = WhitelistedPackages
                    .FirstOrDefault(w => string.Equals(w.Package, packageName, StringComparison.OrdinalIgnoreCase));
                var isWhitelisted = !string.IsNullOrEmpty(whitelist.Package);

                long packageSize = 0;
                var referencedSubAssets = new List<string>();

                // Reference files that belong to THIS package are intra-package; a package is only
                // "used" if something OUTSIDE it references one of its sub-assets.
                var packageGuids = AssetDatabase.FindAssets(string.Empty, new[] { packageFolder });
                foreach (var guid in packageGuids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath)) continue;

                    var normalized = assetPath.Replace('\\', '/');
                    packageSize += GetFileSize(normalized);

                    // External reference files = all files that are NOT inside this package folder,
                    // and never the asset's own .meta.
                    var ownMeta = guidToOwnMeta.TryGetValue(guid, out var m) ? m : null;
                    var externalRefs = referenceFiles.Where(f =>
                        !f.Path.Replace('\\', '/').StartsWith(packageFolder + "/", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(f.Path, ownMeta, StringComparison.OrdinalIgnoreCase));

                    var result = AssetReferenceScanner.Scan(guid, externalRefs);
                    if (result.IsReferenced)
                    {
                        referencedSubAssets.Add(normalized);
                    }
                }

                packages.Add(new ThirdPartyPackage(
                    packageName,
                    packageSize,
                    referencedSubAssets.Count > 0,
                    referencedSubAssets,
                    isWhitelisted,
                    isWhitelisted ? whitelist.UsedBy : null));
            }

            // Heaviest / highlighted package first, then by size.
            return packages
                .OrderByDescending(p => string.Equals(p.Name, HeaviestPackageName, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(p => p.SizeBytes)
                .ToList();
        }

        private static string Render(List<Candidate> candidates, List<ThirdPartyPackage> thirdParty)
        {
            var sb = new StringBuilder();
            var now = DateTime.Now.ToString("u", CultureInfo.InvariantCulture);

            sb.AppendLine($"# Relatório de candidatos a remoção — {now}");
            sb.AppendLine();
            sb.AppendLine("> Esta ferramenta **só reporta** — não remove nada (R7.4). A remoção é um passo");
            sb.AppendLine("> separado, item-a-item, com confirmação explícita do usuário (Task 13.1).");
            sb.AppendLine();

            // ── Órfãos ──────────────────────────────────────────────────────────────────────
            var orphans = candidates.Where(c => c.Category == Category.Orphan)
                .OrderByDescending(c => c.SizeBytes).ToList();
            sb.AppendLine("## Órfãos (sem referência de GUID encontrada)");
            if (orphans.Count == 0)
            {
                sb.AppendLine("- _Nenhum candidato órfão encontrado._");
            }
            else
            {
                foreach (var c in orphans)
                {
                    sb.AppendLine($"- {c.Path}  | {FormatSize(c.SizeBytes)} | motivo: nenhum GUID em .unity/.prefab/.asset/.meta");
                }
            }
            sb.AppendLine();

            // ── Requer verificação de path (sob Resources) ──────────────────────────────────
            var resources = candidates.Where(c => c.Category == Category.RequiresPathCheck)
                .OrderByDescending(c => c.SizeBytes).ToList();
            sb.AppendLine("## Requer verificação de path (sob Resources)");
            if (resources.Count == 0)
            {
                sb.AppendLine("- _Nenhum candidato sob Resources._");
            }
            else
            {
                foreach (var c in resources)
                {
                    sb.AppendLine($"- {c.Path}  | {FormatSize(c.SizeBytes)} | motivo: pode ser alvo de Resources.Load — confirmar antes");
                }
            }
            sb.AppendLine();

            // ── Pacotes third-party ─────────────────────────────────────────────────────────
            sb.AppendLine("## Pacotes third-party");
            var nonWhitelisted = thirdParty.Where(p => !p.IsWhitelisted).ToList();
            if (nonWhitelisted.Count == 0)
            {
                sb.AppendLine("- _Nenhum pacote third-party candidato._");
            }
            else
            {
                foreach (var p in nonWhitelisted)
                {
                    if (string.Equals(p.Name, "FastScriptReload", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"- {p.Name} | {FormatSize(p.SizeBytes)} | ver Requisito 8");
                        continue;
                    }

                    var subAssets = p.AnySubAssetReferenced
                        ? string.Join(", ", p.ReferencedSubAssets.Take(10)) +
                          (p.ReferencedSubAssets.Count > 10 ? $" (+{p.ReferencedSubAssets.Count - 10})" : string.Empty)
                        : "nenhum";
                    sb.AppendLine($"- {p.Name} | {FormatSize(p.SizeBytes)} | classificação: [{p.Classification}] | sub-assets usados: {subAssets}");
                }
            }
            sb.AppendLine();

            // ── Mantidos (whitelist / referenciados) ────────────────────────────────────────
            sb.AppendLine("## Mantidos (whitelist / referenciados)");
            foreach (var p in thirdParty.Where(p => p.IsWhitelisted))
            {
                sb.AppendLine($"- {p.Name} | {p.WhitelistReason}");
            }
            // Also list whitelist entries whose package folder may be absent, so the policy is explicit.
            foreach (var w in WhitelistedPackages)
            {
                if (!thirdParty.Any(p => p.IsWhitelisted &&
                        string.Equals(p.Name, w.Package, StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine($"- {w.Package} | {w.UsedBy}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine("_Gerado por `TechGuy.EditorTools.Cleanup.ProjectCleanupReport` " +
                          "(Tech Guy/Cleanup/Orphan Report). Nenhum asset foi alterado ou removido._");

            return sb.ToString();
        }

        // ── Helpers ────────────────────────────────────────────────────────────────────────

        // NOTE: targets .NET Framework 4.7.1 (C# 9) — string.Contains(string, StringComparison) is
        // not available there, so we use IndexOf(..., StringComparison) >= 0 for culture-safe matching.
        private static bool IsThirdParty(string normalizedPath) =>
            normalizedPath.IndexOf("/_ThirdParty/", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsUnderResources(string normalizedPath) =>
            normalizedPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalizedPath.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase);

        private static long GetFileSize(string projectRelativePath)
        {
            try
            {
                var absolute = ToAbsolute(projectRelativePath);
                var info = new FileInfo(absolute);
                return info.Exists ? info.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size.ToString(unit == 0 ? "0" : "0.0", CultureInfo.InvariantCulture)} {units[unit]}";
        }

        private static string ToProjectRelative(string absolutePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/') + "/";
            var normalized = absolutePath.Replace('\\', '/');
            return normalized.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(normalizedRoot.Length)
                : normalized;
        }

        private static string ToAbsolute(string projectRelativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, projectRelativePath);
        }
    }
}
