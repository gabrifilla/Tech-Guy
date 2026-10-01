using System;
using System.Collections.Generic;

namespace TechGuy.EditorTools.Cleanup
{
    /// <summary>
    /// How a single asset was classified by <see cref="OrphanClassifier"/>, in increasing order of
    /// "safe to propose for removal". Only <see cref="PureOrphan"/> may ever be proposed for removal;
    /// the other three are guards that keep an asset off the removal list (R7.2/R7.5/R7.7).
    /// </summary>
    public enum OrphanCategory
    {
        /// <summary>
        /// The asset's GUID was found in at least one reference file, so it is in use and must never
        /// be proposed for removal (R7.5).
        /// </summary>
        Referenced,

        /// <summary>
        /// The asset belongs to a whitelisted package proven to be in use (TextMesh Pro, QuickOutline).
        /// It is never proposed for removal even if its GUID happens to look unreferenced (R7.7).
        /// </summary>
        Whitelisted,

        /// <summary>
        /// The asset lives under a <c>Resources</c> folder, so it may be loaded by path via
        /// <c>Resources.Load</c> and has no statically trackable GUID reference. It is never a pure
        /// orphan without first confirming the absence of path-based use — reported as
        /// "requer verificação de path" (R7.2).
        /// </summary>
        RequiresPathVerification,

        /// <summary>
        /// No GUID reference was found, the asset is not under <c>Resources</c>, and it is not
        /// whitelisted. This is the only category that may be proposed for removal — still gated behind
        /// explicit, item-by-item user confirmation elsewhere (R7.1).
        /// </summary>
        PureOrphan
    }

    /// <summary>
    /// Result of classifying one asset: its category plus a human-readable reason suitable for the
    /// cleanup report.
    /// </summary>
    public readonly struct OrphanClassification
    {
        /// <summary>The asset path that was classified (used for reporting).</summary>
        public readonly string AssetPath;

        /// <summary>The category the asset was placed in.</summary>
        public readonly OrphanCategory Category;

        /// <summary>A short, report-ready explanation of why this category was chosen.</summary>
        public readonly string Reason;

        public OrphanClassification(string assetPath, OrphanCategory category, string reason)
        {
            AssetPath = assetPath;
            Category = category;
            Reason = reason;
        }

        /// <summary>
        /// <c>true</c> only for <see cref="OrphanCategory.PureOrphan"/> — the single category that may be
        /// put forward for (confirmed) removal.
        /// </summary>
        public bool IsRemovalCandidate => Category == OrphanCategory.PureOrphan;
    }

    /// <summary>
    /// Pure, Unity-free second half of the safe-cleanup gate (design §10, Properties 8/9).
    ///
    /// <see cref="AssetReferenceScanner"/> answers only "is this GUID referenced?". The classifier wraps
    /// that answer with the two safety guards the design requires before an asset is ever called an
    /// orphan:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///     <b>Resources guard (R7.2):</b> an asset under any <c>Resources</c> folder can be loaded by
    ///     path (<c>Resources.Load</c>), which leaves no GUID trace. Such an asset is never a pure
    ///     orphan — it is marked <see cref="OrphanCategory.RequiresPathVerification"/> so a human can
    ///     confirm the absence of path-based use first.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     <b>Whitelist guard (R7.7):</b> packages proven to be in use (TextMesh Pro by the UI,
    ///     QuickOutline by <c>OutlineScript</c>) are never proposed for removal, regardless of the scan
    ///     result — they are marked <see cref="OrphanCategory.Whitelisted"/>.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// The guard order matters: a referenced asset is reported as referenced first (so its origin is
    /// shown); otherwise the whitelist and Resources guards are applied before an unreferenced asset is
    /// ever allowed to become a <see cref="OrphanCategory.PureOrphan"/>. Both guards are pure string
    /// predicates (<see cref="IsUnderResources"/>, <see cref="IsWhitelisted"/>), so the whole decision
    /// is testable in EditMode without touching the AssetDatabase or disk.
    /// </summary>
    public static class OrphanClassifier
    {
        /// <summary>
        /// Path segments that identify a package which is proven to be in use and therefore whitelisted
        /// against removal (R7.7). Matched case-insensitively as a path segment so, e.g.,
        /// <c>Assets/TextMesh Pro/...</c> and <c>Assets/Plugins/QuickOutline/...</c> are both covered,
        /// while an unrelated path that merely contains the text elsewhere is not falsely matched.
        /// </summary>
        private static readonly string[] WhitelistedPackageSegments =
        {
            "TextMesh Pro",
            "TextMeshPro",
            "TMPro",
            "QuickOutline",
        };

        /// <summary>
        /// Classifies <paramref name="assetPath"/> using its <paramref name="scanResult"/> and the two
        /// safety guards. The returned classification carries a report-ready reason.
        /// </summary>
        /// <param name="assetPath">Project-relative path of the asset being classified.</param>
        /// <param name="scanResult">The GUID scan result produced by <see cref="AssetReferenceScanner"/>.</param>
        public static OrphanClassification Classify(string assetPath, ReferenceScanResult scanResult)
        {
            // 1. Referenced wins outright: the GUID was found, so the asset is in use and its origin is
            //    reportable (R7.5). This takes priority so a used asset is never second-guessed by a
            //    guard.
            if (scanResult.IsReferenced)
            {
                return new OrphanClassification(
                    assetPath,
                    OrphanCategory.Referenced,
                    BuildReferencedReason(scanResult.ReferencingFiles));
            }

            // 2. Whitelist guard: a proven-used package is never proposed for removal even when its GUID
            //    does not appear in the scanned files (R7.7).
            if (IsWhitelisted(assetPath))
            {
                return new OrphanClassification(
                    assetPath,
                    OrphanCategory.Whitelisted,
                    "pacote na whitelist (comprovadamente usado) — nunca proposto para remoção");
            }

            // 3. Resources guard: an asset under Resources may be loaded by path, so it is never a pure
            //    orphan without confirming the absence of path-based use (R7.2).
            if (IsUnderResources(assetPath))
            {
                return new OrphanClassification(
                    assetPath,
                    OrphanCategory.RequiresPathVerification,
                    "requer verificação de path — pode ser alvo de Resources.Load");
            }

            // 4. No reference, not whitelisted, not under Resources → the only removable category (R7.1).
            return new OrphanClassification(
                assetPath,
                OrphanCategory.PureOrphan,
                "nenhum GUID encontrado em .unity/.prefab/.asset/.meta");
        }

        /// <summary>
        /// Convenience overload that runs the scan and the classification in one step from the raw GUID
        /// and reference files.
        /// </summary>
        public static OrphanClassification Classify(
            string assetPath,
            string guid,
            IEnumerable<ReferenceFile> referenceFiles)
        {
            return Classify(assetPath, AssetReferenceScanner.Scan(guid, referenceFiles));
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="assetPath"/> contains a <c>Resources</c> folder as a
        /// path segment (e.g. <c>Assets/_Project/Resources/Items/CoinPickup.prefab</c>). The check is
        /// pure: it normalizes both <c>/</c> and <c>\</c> separators and matches <c>Resources</c> as a
        /// whole segment, so a file literally named <c>Resources.cs</c> or a folder like
        /// <c>MyResources</c> is not falsely treated as Resources-managed.
        /// </summary>
        public static bool IsUnderResources(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            foreach (string segment in SplitSegments(assetPath))
            {
                if (string.Equals(segment, "Resources", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="assetPath"/> belongs to a whitelisted package
        /// (TextMesh Pro / QuickOutline), matched as a case-insensitive whole path segment.
        /// </summary>
        public static bool IsWhitelisted(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            foreach (string segment in SplitSegments(assetPath))
            {
                foreach (string whitelisted in WhitelistedPackageSegments)
                {
                    if (string.Equals(segment, whitelisted, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Splits a path into its segments on both <c>/</c> and <c>\</c>, dropping empty entries so a
        /// trailing/leading separator does not produce blank segments.
        /// </summary>
        private static IEnumerable<string> SplitSegments(string path)
        {
            return path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Builds the report reason for a referenced asset, naming where the GUID was found so the
        /// origin is visible (R7.5).
        /// </summary>
        private static string BuildReferencedReason(IReadOnlyList<string> referencingFiles)
        {
            if (referencingFiles == null || referencingFiles.Count == 0)
            {
                // Defensive: IsReferenced implies Count > 0, but keep a sane message if that invariant
                // is ever violated by a hand-built result.
                return "referenciado";
            }

            return "referenciado em: " + string.Join(", ", referencingFiles);
        }
    }
}
