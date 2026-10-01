using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.EditorTools.Cleanup;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the <c>Resources</c> safety guard of the pure
    /// <see cref="OrphanClassifier"/> — task 11.5 of project-cleanup-optimization (Requisito 7.2).
    ///
    /// <see cref="OrphanClassifier"/> is 100% framework-agnostic (no <see cref="UnityEngine.MonoBehaviour"/>,
    /// no scene, no <c>AssetDatabase</c>): it decides a category from a path string plus a
    /// <see cref="ReferenceScanResult"/>, so its guards can be property-checked without a live Unity
    /// scene — exactly the pure-logic floor the spec says is the only CLI-automatable target.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing input as a counterexample — matching the convention of the
    /// sibling pure-logic property tests (e.g. SimpleObjectPoolPropertyTests, ProjectileMotionPropertyTests).
    ///
    /// Validates: Requirements 7.2
    /// </summary>
    public sealed class ResourcesGuardNeverPureOrphanPropertyTests
    {
        // Feature: project-cleanup-optimization, Property 9: para qualquer asset sob `**/Resources/**`,
        // o classificador nunca o marca como órfão puro (marca "requer verificação de path").
        //
        // For ANY asset path that contains a `Resources` folder segment (at ANY depth) and ANY scan
        // result, OrphanClassifier.Classify never returns OrphanCategory.PureOrphan (R7.2). More
        // precisely:
        //   - If the GUID was referenced, the asset is Referenced (and the Resources guard is moot).
        //   - If the GUID was NOT referenced, the asset must be either Whitelisted (if it also lives
        //     under a whitelisted package) or RequiresPathVerification — because an unreferenced asset
        //     under Resources may still be loaded by path via Resources.Load, so it is never a pure
        //     orphan without first confirming the absence of path-based use.
        // In every case the category is NOT PureOrphan.
        // Validates: Requirements 7.2
        [Test]
        public void AssetUnderResourcesIsNeverClassifiedAsPureOrphan()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                string path = BuildResourcesPath(rng);
                ReferenceScanResult scan = BuildScanResult(rng);

                // The path really does contain a `Resources` segment (sanity on the generator so a
                // false pass can never come from a non-Resources path slipping through).
                PropertyCheck.That(OrphanClassifier.IsUnderResources(path),
                    $"generator produced a non-Resources path: '{path}'");

                OrphanClassification result = OrphanClassifier.Classify(path, scan);

                // Core guarantee (R7.2): an asset under Resources is NEVER a pure orphan ...
                PropertyCheck.That(result.Category != OrphanCategory.PureOrphan,
                    $"Resources asset '{path}' (referenced={scan.IsReferenced}) was classified as PureOrphan");
                // ... and therefore is never a removal candidate.
                PropertyCheck.That(!result.IsRemovalCandidate,
                    $"Resources asset '{path}' was marked as a removal candidate");

                if (scan.IsReferenced)
                {
                    // A found GUID is reported as Referenced regardless of any guard.
                    PropertyCheck.That(result.Category == OrphanCategory.Referenced,
                        $"referenced Resources asset '{path}' was classified as {result.Category} instead of Referenced");
                }
                else if (OrphanClassifier.IsWhitelisted(path))
                {
                    // Whitelist guard runs before the Resources guard, so a Resources asset that also
                    // sits under a whitelisted package is reported as Whitelisted (still not an orphan).
                    PropertyCheck.That(result.Category == OrphanCategory.Whitelisted,
                        $"unreferenced whitelisted+Resources asset '{path}' was classified as {result.Category} instead of Whitelisted");
                }
                else
                {
                    // The headline Resources guard: unreferenced, not whitelisted, under Resources ->
                    // RequiresPathVerification, with the "requer verificação de path" reason (R7.2).
                    PropertyCheck.That(result.Category == OrphanCategory.RequiresPathVerification,
                        $"unreferenced Resources asset '{path}' was classified as {result.Category} instead of RequiresPathVerification");
                    PropertyCheck.That(result.Reason != null && result.Reason.Contains("requer verificação de path"),
                        $"RequiresPathVerification reason for '{path}' did not mention path verification: '{result.Reason}'");
                }
            });
        }

        // Feature: project-cleanup-optimization, Property 9 (negative/contrast facet): the guard is
        // scoped to `Resources` segments only — an unreferenced, non-whitelisted asset NOT under any
        // Resources folder IS allowed to become a PureOrphan. This proves the previous property is a
        // real guard (it changes the outcome) rather than a classifier that never emits PureOrphan at
        // all. The `Resources` match is a whole-segment, case-insensitive check, so lookalikes such as
        // `MyResources` or a file literally named `Resources.cs` must NOT trigger the guard.
        // Validates: Requirements 7.2
        [Test]
        public void UnreferencedNonResourcesAssetCanBePureOrphan()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                string path = BuildNonResourcesPath(rng);
                // Unreferenced scan: no file contained the GUID (the only orphan-candidate situation).
                var scan = new ReferenceScanResult("deadbeefdeadbeefdeadbeefdeadbeef", new string[0]);

                PropertyCheck.That(!OrphanClassifier.IsUnderResources(path),
                    $"generator produced a Resources path for the negative case: '{path}'");
                PropertyCheck.That(!OrphanClassifier.IsWhitelisted(path),
                    $"generator produced a whitelisted path for the negative case: '{path}'");

                OrphanClassification result = OrphanClassifier.Classify(path, scan);

                PropertyCheck.That(result.Category == OrphanCategory.PureOrphan,
                    $"unreferenced non-Resources asset '{path}' was classified as {result.Category} instead of PureOrphan");
            });
        }

        /// <summary>
        /// Builds a project-relative asset path that is guaranteed to contain a <c>Resources</c> folder
        /// segment at a varying depth, with randomized separators (<c>/</c> vs <c>\</c>) and casing on the
        /// <c>Resources</c> segment (the guard is case-insensitive). Occasionally the path is also placed
        /// under a whitelisted package so the whitelist-wins branch is exercised too.
        /// </summary>
        private static string BuildResourcesPath(System.Random rng)
        {
            var segments = new List<string> { "Assets" };

            // Optional whitelisted package segment so the whitelist+Resources interaction is covered.
            if (rng.Next(0, 100) < 20)
            {
                segments.Add(PickWhitelisted(rng));
            }

            // Some leading non-Resources folders to vary the depth of the Resources segment.
            int leading = rng.Next(0, 4);
            for (int k = 0; k < leading; k++)
            {
                segments.Add(RandomFolder(rng));
            }

            // The Resources segment itself, with randomized casing (whole-segment, case-insensitive match).
            segments.Add(RandomCase(rng, "Resources"));

            // Some trailing folders under Resources.
            int trailing = rng.Next(0, 4);
            for (int k = 0; k < trailing; k++)
            {
                segments.Add(RandomFolder(rng));
            }

            // A file name (deliberately not literally "Resources" so the file-vs-folder distinction is clean).
            segments.Add(RandomFile(rng));

            return Join(rng, segments);
        }

        /// <summary>
        /// Builds a project-relative asset path that contains NO <c>Resources</c> folder segment and no
        /// whitelisted package segment, deliberately including lookalikes (e.g. <c>MyResources</c>, a file
        /// named <c>Resources.cs</c>) so the whole-segment semantics of the guard are exercised.
        /// </summary>
        private static string BuildNonResourcesPath(System.Random rng)
        {
            var segments = new List<string> { "Assets", "_Project" };

            int depth = rng.Next(1, 5);
            for (int k = 0; k < depth; k++)
            {
                // Lookalike folders that must NOT trip the whole-segment Resources match.
                int kind = rng.Next(0, 5);
                switch (kind)
                {
                    case 0: segments.Add("MyResources"); break;
                    case 1: segments.Add("ResourcesBackup"); break;
                    case 2: segments.Add("Resourcesx"); break;
                    case 3: segments.Add("xResources"); break;
                    default: segments.Add(RandomFolder(rng)); break;
                }
            }

            // A file name that may itself contain the word "Resources" (e.g. Resources.cs) — still not a
            // folder segment, so still not Resources-managed.
            segments.Add(rng.Next(0, 2) == 0 ? "Resources.cs" : RandomFile(rng));

            return Join(rng, segments);
        }

        private static string PickWhitelisted(System.Random rng)
        {
            string[] packages = { "TextMesh Pro", "TextMeshPro", "TMPro", "QuickOutline" };
            return packages[rng.Next(0, packages.Length)];
        }

        private static string RandomFolder(System.Random rng)
        {
            string[] folders =
            {
                "_Project", "Prefabs", "Items", "Weapons", "Ranged", "Effects", "Art", "Settings",
                "ScriptableObjects", "Characters", "Abilities", "UI", "Core", "Nested", "Deep", "Sub",
            };
            return folders[rng.Next(0, folders.Length)];
        }

        private static string RandomFile(System.Random rng)
        {
            string[] names = { "CoinPickup", "Bow", "Spear", "Gauntlet", "Hit", "Icon", "Clip", "Mat", "Data" };
            string[] exts = { ".prefab", ".asset", ".mat", ".png", ".anim", ".controller" };
            return names[rng.Next(0, names.Length)] + exts[rng.Next(0, exts.Length)];
        }

        /// <summary>Randomizes the casing of each character so the case-insensitive match is exercised.</summary>
        private static string RandomCase(System.Random rng, string value)
        {
            var chars = value.ToCharArray();
            for (int c = 0; c < chars.Length; c++)
            {
                chars[c] = rng.Next(0, 2) == 0 ? char.ToLowerInvariant(chars[c]) : char.ToUpperInvariant(chars[c]);
            }
            return new string(chars);
        }

        /// <summary>Joins segments with a randomly chosen separator (<c>/</c> or <c>\</c>), since the guard
        /// normalizes both.</summary>
        private static string Join(System.Random rng, List<string> segments)
        {
            char sep = rng.Next(0, 2) == 0 ? '/' : '\\';
            return string.Join(sep.ToString(), segments);
        }

        /// <summary>
        /// Builds a scan result that is referenced about half the time (one or more referencing files) and
        /// an orphan candidate otherwise (no referencing files), so both the Referenced and
        /// guard-applied branches are generated.
        /// </summary>
        private static ReferenceScanResult BuildScanResult(System.Random rng)
        {
            string guid = "deadbeefdeadbeefdeadbeefdeadbeef";
            if (rng.Next(0, 2) == 0)
            {
                int count = rng.Next(1, 4);
                var files = new List<string>();
                for (int k = 0; k < count; k++)
                {
                    files.Add($"Assets/Scene{k}.unity");
                }
                return new ReferenceScanResult(guid, files);
            }

            return new ReferenceScanResult(guid, new string[0]);
        }
    }
}
