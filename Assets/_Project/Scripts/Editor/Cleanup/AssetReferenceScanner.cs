using System;
using System.Collections.Generic;

namespace TechGuy.EditorTools.Cleanup
{
    /// <summary>
    /// A single reference file (its path plus full text content) to scan for a GUID.
    /// Pure data — no Unity, no disk access. The caller (an Editor tool) is responsible
    /// for reading <c>.unity</c>/<c>.prefab</c>/<c>.asset</c>/<c>.meta</c> files from disk
    /// and feeding their contents here.
    /// </summary>
    public readonly struct ReferenceFile
    {
        /// <summary>Project-relative (or absolute) path of the file, used only for reporting.</summary>
        public readonly string Path;

        /// <summary>Full text content of the file.</summary>
        public readonly string Content;

        public ReferenceFile(string path, string content)
        {
            Path = path;
            Content = content;
        }
    }

    /// <summary>
    /// Result of scanning one asset's GUID against a set of reference files.
    /// An asset is an <see cref="IsOrphanCandidate"/> only when the GUID was found in
    /// none of the scanned files. When it is referenced, <see cref="ReferencingFiles"/>
    /// lists exactly which files contain the GUID so the origin is reportable (R7.5).
    /// </summary>
    public readonly struct ReferenceScanResult
    {
        /// <summary>The GUID that was searched for.</summary>
        public readonly string Guid;

        /// <summary>
        /// Paths of the files whose content contained the GUID. Empty when the GUID was
        /// not found in any scanned file.
        /// </summary>
        public readonly IReadOnlyList<string> ReferencingFiles;

        public ReferenceScanResult(string guid, IReadOnlyList<string> referencingFiles)
        {
            Guid = guid;
            ReferencingFiles = referencingFiles ?? Array.Empty<string>();
        }

        /// <summary><c>true</c> when at least one scanned file referenced the GUID.</summary>
        public bool IsReferenced => ReferencingFiles.Count > 0;

        /// <summary>
        /// <c>true</c> when the GUID appeared in none of the scanned files — the only
        /// situation in which the asset may be treated as an orphan candidate (R7.1).
        /// </summary>
        public bool IsOrphanCandidate => ReferencingFiles.Count == 0;
    }

    /// <summary>
    /// Pure, Unity-free core of the safe-cleanup guarantee (design §10, Property 8).
    ///
    /// Given an asset's GUID and the raw text content of the project's reference files
    /// (<c>.unity</c>, <c>.prefab</c>, <c>.asset</c>, <c>.meta</c>), it decides whether the
    /// GUID is referenced. Unity stores cross-asset references by GUID inside these text
    /// (YAML) files, so a GUID that appears in none of them has no tracked reference — the
    /// asset is an orphan candidate. If the GUID appears in one or more files, the asset is
    /// referenced and the scanner reports which files contain it so the origin can be shown
    /// to the user before any removal is proposed (R7.1/R7.5).
    ///
    /// This class performs no <c>AssetDatabase</c> or disk access: it takes a GUID plus
    /// pre-read file contents as input, which keeps it fully testable in EditMode and makes
    /// it the single trustworthy point of the "identify" step of the cleanup gate.
    /// </summary>
    public static class AssetReferenceScanner
    {
        /// <summary>
        /// Scans <paramref name="referenceFiles"/> for <paramref name="guid"/> and returns a
        /// result that is reportable: it names every file that contained the GUID, and is an
        /// orphan candidate only when no file contained it.
        /// </summary>
        /// <param name="guid">The asset GUID to search for (as it appears in <c>.meta</c>).</param>
        /// <param name="referenceFiles">
        /// The reference files (path + content) to scan. The asset's own <c>.meta</c> file
        /// defines the GUID and should not be passed in as a "reference", otherwise the asset
        /// would always look referenced; callers scan the files that may reference the asset.
        /// </param>
        /// <returns>A <see cref="ReferenceScanResult"/> describing where (if anywhere) the GUID was found.</returns>
        public static ReferenceScanResult Scan(string guid, IEnumerable<ReferenceFile> referenceFiles)
        {
            // A null/blank GUID can never be matched meaningfully; treat as "not referenced"
            // (orphan candidate) rather than false-matching against incidental content.
            if (string.IsNullOrWhiteSpace(guid) || referenceFiles == null)
            {
                return new ReferenceScanResult(guid, Array.Empty<string>());
            }

            var referencingFiles = new List<string>();

            foreach (ReferenceFile file in referenceFiles)
            {
                if (ContainsGuid(file.Content, guid))
                {
                    referencingFiles.Add(file.Path);
                }
            }

            return new ReferenceScanResult(guid, referencingFiles);
        }

        /// <summary>
        /// Convenience wrapper that answers only the orphan-candidate question for a single
        /// asset, without the per-file origin breakdown.
        /// </summary>
        public static bool IsOrphanCandidate(string guid, IEnumerable<ReferenceFile> referenceFiles)
        {
            return Scan(guid, referenceFiles).IsOrphanCandidate;
        }

        /// <summary>
        /// Returns whether <paramref name="content"/> contains <paramref name="guid"/>.
        /// Uses an ordinal (byte-wise) substring match: Unity GUIDs are lowercase hex and are
        /// embedded verbatim inside the YAML (e.g. <c>guid: 1e0e6cc43e3c10e4abb30587aa6fa4df</c>),
        /// so a case-insensitive, culture-invariant containment check is both correct and cheap.
        /// </summary>
        private static bool ContainsGuid(string content, string guid)
        {
            if (string.IsNullOrEmpty(content)) return false;
            return content.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
