using System;
using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.EditorTools.Cleanup;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, Unity-free orphan detector
    /// <see cref="AssetReferenceScanner"/> — task 11.4 of project-cleanup-optimization
    /// (Requisitos 7.1, 7.5, 7.7, design Property 8).
    ///
    /// <see cref="AssetReferenceScanner.Scan(string, IEnumerable{ReferenceFile})"/> is the single
    /// trustworthy "identify" step of the cleanup gate: it takes an asset GUID plus the raw text of
    /// the project's reference files (<c>.unity</c>/<c>.prefab</c>/<c>.asset</c>/<c>.meta</c>) and
    /// returns a <see cref="ReferenceScanResult"/>. It performs no <c>AssetDatabase</c> or disk access,
    /// so the orphan-candidate decision can be property-checked without a live Unity scene — exactly
    /// the pure-logic floor the spec says is the only CLI-automatable target.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing case as a counterexample — matching the convention of the
    /// sibling pure-logic property tests (e.g. ProjectileMotionPropertyTests, SimpleObjectPoolPropertyTests,
    /// WeaponCacheIdempotentPreloadPropertyTests).
    ///
    /// Validates: Requirements 7.1, 7.5, 7.7
    /// </summary>
    public sealed class OrphanCandidateReferenceScanPropertyTests
    {
        // A GUID is a 32-char lowercase hex string in Unity; we generate values from this alphabet so
        // the generated "GUID" and the "noise" we embed are realistic and can genuinely collide/avoid.
        private const string HexDigits = "0123456789abcdef";

        /// <summary>Generates a random 32-char lowercase-hex GUID from the seeded generator.</summary>
        private static string RandomGuid(Random rng)
        {
            var chars = new char[32];
            for (int i = 0; i < chars.Length; i++) chars[i] = HexDigits[rng.Next(0, HexDigits.Length)];
            return new string(chars);
        }

        /// <summary>
        /// Generates a random 32-char hex GUID that is guaranteed NOT equal to <paramref name="forbidden"/>
        /// (case-insensitively), so a file built from it cannot accidentally reference the target asset.
        /// </summary>
        private static string RandomGuidOtherThan(Random rng, string forbidden)
        {
            for (int attempt = 0; attempt < 64; attempt++)
            {
                string candidate = RandomGuid(rng);
                if (!string.Equals(candidate, forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
            // Astronomically unlikely fallback: flip one digit so it differs from the target.
            char[] chars = forbidden.ToCharArray();
            chars[0] = chars[0] == '0' ? '1' : '0';
            return new string(chars);
        }

        /// <summary>
        /// Wraps a GUID in realistic YAML surroundings so the match must survive embedding, mimicking
        /// how Unity stores cross-asset references, e.g. <c>{fileID: 11400000, guid: ..., type: 2}</c>.
        /// </summary>
        private static string EmbedGuid(string guid, Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return $"  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n";
                case 1: return $"  - target: {{fileID: 400000, guid: {guid}, type: 2}}\n";
                default: return $"someField:\n  guid: {guid}\n";
            }
        }

        /// <summary>Builds a chunk of plausible YAML "noise" that references only unrelated GUIDs.</summary>
        private static string NoiseContent(Random rng, string excludedGuid)
        {
            int lines = rng.Next(0, 5);
            var sb = new System.Text.StringBuilder();
            sb.Append("%YAML 1.1\n--- !u!1 &").Append(rng.Next(1, 999999)).Append('\n');
            for (int i = 0; i < lines; i++)
            {
                sb.Append(EmbedGuid(RandomGuidOtherThan(rng, excludedGuid), rng));
            }
            return sb.ToString();
        }

        // Feature: project-cleanup-optimization, Property 8: um asset é candidato sse seu GUID não
        // aparece em nenhum arquivo de referência; se aparece em >=1, não é candidato (e a origem é
        // reportável).
        //
        // For any target GUID and any set of reference files, we partition the files into "referencing"
        // (we deliberately embed the target GUID somewhere in their content) and "non-referencing" (we
        // fill them only with unrelated GUID noise), then assert:
        //   (a) IsOrphanCandidate is TRUE if and only if NO file contains the GUID — i.e. it is true
        //       exactly when the referencing set is empty, and false the moment at least one file
        //       references the GUID (R7.1);
        //   (b) when referenced, ReferencingFiles names EXACTLY the files that contain the GUID — no
        //       more (no false positive from unrelated noise) and no less (every embedded file listed) —
        //       so the origin is reportable to the user before any removal (R7.5/R7.7);
        //   (c) IsReferenced is the logical complement of IsOrphanCandidate, and the convenience
        //       overload AssetReferenceScanner.IsOrphanCandidate agrees with the full Scan result.
        // Validates: Requirements 7.1, 7.5, 7.7
        [Test]
        public void OrphanCandidateIffGuidAbsentAndReferencingFilesAreReportable()
        {
            PropertyCheck.ForAll((rng, iteration) =>
            {
                string target = RandomGuid(rng);
                int fileCount = rng.Next(0, 8);

                // Build the files, tracking exactly which paths we embedded the target GUID into so we
                // can compare against the scanner's reported origins.
                var files = new List<ReferenceFile>();
                var expectedReferencing = new HashSet<string>();

                for (int f = 0; f < fileCount; f++)
                {
                    string path = $"Assets/Fake/File{f}_{iteration}.unity";
                    bool shouldReference = rng.Next(0, 2) == 0;

                    string content = NoiseContent(rng, target);
                    if (shouldReference)
                    {
                        // Splice the target GUID into otherwise-unrelated content.
                        content += EmbedGuid(target, rng);
                        content += NoiseContent(rng, target);
                        expectedReferencing.Add(path);
                    }

                    files.Add(new ReferenceFile(path, content));
                }

                string ctx = $"[iter={iteration} files={fileCount} refs={expectedReferencing.Count}]";

                ReferenceScanResult result = AssetReferenceScanner.Scan(target, files);

                // (a) orphan candidate IFF no file referenced the GUID.
                bool noneReference = expectedReferencing.Count == 0;
                PropertyCheck.That(result.IsOrphanCandidate == noneReference,
                    $"{ctx}: IsOrphanCandidate={result.IsOrphanCandidate} but expected {noneReference} (no file referenced the GUID = {noneReference})");

                // (c) IsReferenced is the exact complement, and the convenience overload agrees.
                PropertyCheck.That(result.IsReferenced == !result.IsOrphanCandidate,
                    $"{ctx}: IsReferenced={result.IsReferenced} is not the complement of IsOrphanCandidate={result.IsOrphanCandidate}");
                PropertyCheck.That(result.IsReferenced == !noneReference,
                    $"{ctx}: IsReferenced={result.IsReferenced} disagreed with model (expected {!noneReference})");

                bool convenience = AssetReferenceScanner.IsOrphanCandidate(target, files);
                PropertyCheck.That(convenience == result.IsOrphanCandidate,
                    $"{ctx}: IsOrphanCandidate convenience overload ({convenience}) disagreed with Scan ({result.IsOrphanCandidate})");

                // (b) ReferencingFiles names exactly the files we embedded the GUID into (origin reportable).
                PropertyCheck.That(result.ReferencingFiles.Count == expectedReferencing.Count,
                    $"{ctx}: reported {result.ReferencingFiles.Count} referencing files, expected {expectedReferencing.Count}");

                var reported = new HashSet<string>(result.ReferencingFiles);
                foreach (string path in result.ReferencingFiles)
                {
                    PropertyCheck.That(expectedReferencing.Contains(path),
                        $"{ctx}: scanner reported a false-positive referencing file '{path}' (unrelated noise matched)");
                }
                foreach (string path in expectedReferencing)
                {
                    PropertyCheck.That(reported.Contains(path),
                        $"{ctx}: scanner missed a file that embeds the GUID: '{path}'");
                }

                // The result always echoes the searched GUID, so the report can label the asset (R7.5).
                PropertyCheck.That(result.Guid == target,
                    $"{ctx}: result.Guid='{result.Guid}' did not echo the searched GUID '{target}'");
            });
        }

        // Feature: project-cleanup-optimization, Property 8 (focused "appears in >=1 => not candidate"
        // facet): if the target GUID is embedded in AT LEAST ONE file, the asset is never an orphan
        // candidate and the first such file is always reported, regardless of how many unrelated-GUID
        // noise files surround it — a direct, isolated demonstration that a single genuine reference
        // defeats candidacy and the origin is always surfaced (R7.1/R7.5).
        // Validates: Requirements 7.1, 7.5, 7.7
        [Test]
        public void AtLeastOneReferenceNeverOrphanAndOriginIsReported()
        {
            PropertyCheck.ForAll((rng, iteration) =>
            {
                string target = RandomGuid(rng);

                // A pile of files that reference ONLY unrelated GUIDs...
                int noiseCount = rng.Next(0, 6);
                var files = new List<ReferenceFile>();
                for (int f = 0; f < noiseCount; f++)
                {
                    files.Add(new ReferenceFile($"Assets/Noise/N{f}_{iteration}.prefab", NoiseContent(rng, target)));
                }

                // ...plus at least one file that genuinely references the target GUID, inserted at a
                // random position so order independence is exercised.
                string refPath = $"Assets/Real/Ref_{iteration}.asset";
                string refContent = NoiseContent(rng, target) + EmbedGuid(target, rng) + NoiseContent(rng, target);
                int insertAt = rng.Next(0, files.Count + 1);
                files.Insert(insertAt, new ReferenceFile(refPath, refContent));

                string ctx = $"[iter={iteration} noise={noiseCount} insertAt={insertAt}]";

                ReferenceScanResult result = AssetReferenceScanner.Scan(target, files);

                PropertyCheck.That(!result.IsOrphanCandidate,
                    $"{ctx}: a GUID present in a file was still marked orphan candidate");
                PropertyCheck.That(result.IsReferenced,
                    $"{ctx}: IsReferenced was false despite an embedded GUID");

                var reported = new HashSet<string>(result.ReferencingFiles);
                PropertyCheck.That(reported.Contains(refPath),
                    $"{ctx}: the file that embeds the GUID ('{refPath}') was not reported as origin");
                // None of the pure-noise files should ever be reported.
                foreach (var file in files)
                {
                    if (file.Path == refPath) continue;
                    PropertyCheck.That(!reported.Contains(file.Path),
                        $"{ctx}: unrelated noise file '{file.Path}' was falsely reported as a reference");
                }
            });
        }

        // Feature: project-cleanup-optimization, Property 8 (focused "appears in NONE => candidate"
        // facet): if NO file contains the target GUID — including the empty file set and files full of
        // unrelated-GUID noise — the asset is always an orphan candidate with an empty origin list, and
        // a null/blank GUID is treated as a (non-false-matching) orphan candidate rather than matching
        // incidental content (R7.1).
        // Validates: Requirements 7.1, 7.5, 7.7
        [Test]
        public void NoReferenceAlwaysOrphanCandidateWithEmptyOrigin()
        {
            PropertyCheck.ForAll((rng, iteration) =>
            {
                string target = RandomGuid(rng);

                int fileCount = rng.Next(0, 8);
                var files = new List<ReferenceFile>();
                for (int f = 0; f < fileCount; f++)
                {
                    files.Add(new ReferenceFile($"Assets/Only/Noise{f}_{iteration}.unity", NoiseContent(rng, target)));
                }

                string ctx = $"[iter={iteration} files={fileCount}]";

                ReferenceScanResult result = AssetReferenceScanner.Scan(target, files);

                PropertyCheck.That(result.IsOrphanCandidate,
                    $"{ctx}: GUID absent from all files but not marked orphan candidate");
                PropertyCheck.That(!result.IsReferenced,
                    $"{ctx}: IsReferenced was true despite the GUID being absent from every file");
                PropertyCheck.That(result.ReferencingFiles.Count == 0,
                    $"{ctx}: orphan candidate reported {result.ReferencingFiles.Count} origins, expected 0");

                // A null/blank GUID can never be matched meaningfully and must be an orphan candidate
                // (never false-matching), and null reference input is tolerated as "nothing to scan".
                string blank = rng.Next(0, 3) == 0 ? null : (rng.Next(0, 2) == 0 ? "" : "   ");
                ReferenceScanResult blankResult = AssetReferenceScanner.Scan(blank, files);
                PropertyCheck.That(blankResult.IsOrphanCandidate && blankResult.ReferencingFiles.Count == 0,
                    $"{ctx}: blank GUID was not treated as a no-origin orphan candidate");

                ReferenceScanResult nullFilesResult = AssetReferenceScanner.Scan(target, null);
                PropertyCheck.That(nullFilesResult.IsOrphanCandidate && nullFilesResult.ReferencingFiles.Count == 0,
                    $"{ctx}: null reference-file set was not treated as a no-origin orphan candidate");
            });
        }
    }
}
