using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TechGuy.EditorTools.Cleanup
{
    /// <summary>
    /// Spec project-cleanup-optimization, Task 13.1 — the Editor-side "remover" of the
    /// "identificar → confirmar → remover" gate (design §10). This is the step that is
    /// deliberately SEPARATE from the scan/report (<see cref="ProjectCleanupReport"/>, Task 11.3):
    /// the report only lists candidates; this window is the only place an asset can actually be
    /// deleted, and even here nothing is removed in bulk or silently.
    ///
    /// Safety contract (R7.4/R7.5/R7.6, R9.2/R9.3):
    /// <list type="number">
    ///   <item><description>
    ///     <b>Per-item confirmation (R7.4):</b> deletion is driven one asset at a time by an explicit
    ///     user gesture — an <see cref="EditorUtility.DisplayDialog"/> is raised for the single asset
    ///     before each delete. There is no "delete all" path; nothing is removed in lote or silently.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Re-verification immediately before delete (R7.5):</b> the moment before deleting, the
    ///     asset's GUID is scanned again through the pure <see cref="AssetReferenceScanner"/> against the
    ///     project's current reference files. If ANY reference is found (scene/prefab/SO or a Resources
    ///     path risk), the removal is REJECTED and the origin is reported — the stale report never wins
    ///     over a fresh scan.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Delete the asset together with its <c>.meta</c> (R7.6):</b> deletion always goes through
    ///     <see cref="AssetDatabase.DeleteAsset"/>, which removes the asset and its <c>.meta</c> as a
    ///     unit, leaving no orphan <c>.meta</c> nor dangling GUID. A raw <see cref="File"/> delete is
    ///     never used.
    ///   </description></item>
    /// </list>
    ///
    /// The classification/guard policy is reused verbatim from the identify step via
    /// <see cref="OrphanClassifier"/>, so a candidate shown here was already filtered by the Resources
    /// guard (R7.2) and the whitelist guard (R7.7). Resources-managed assets are listed only as
    /// "requer verificação de path" and are never one-click deletable from here.
    /// </summary>
    public sealed class ProjectCleanupRemover : EditorWindow
    {
        private const string MenuItemPath = "Tech Guy/Cleanup/Remover (item-a-item, confirmado)";

        // Extensions whose text content Unity uses to store cross-asset GUID references (R7.1/R7.5).
        private static readonly string[] ReferenceExtensions = { ".unity", ".prefab", ".asset", ".meta" };

        /// <summary>One row the user can act on. Immutable except for the fields the UI recomputes.</summary>
        private sealed class Row
        {
            public string AssetPath;
            public string Guid;
            public long SizeBytes;
            public OrphanCategory Category;
            public string Reason;
        }

        [SerializeField] private Vector2 _scroll;

        // Candidates found by the last scan. Empty until the user scans.
        private readonly List<Row> _rows = new List<Row>();
        private bool _hasScanned;
        private string _lastLog = string.Empty;

        [MenuItem(MenuItemPath)]
        public static void Open()
        {
            var window = GetWindow<ProjectCleanupRemover>(utility: false, title: "Cleanup Remover");
            window.minSize = new Vector2(680, 360);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Escanear candidatos (dry run)", GUILayout.Height(26)))
                {
                    ScanCandidates();
                }

                using (new EditorGUI.DisabledScope(!_hasScanned))
                {
                    if (GUILayout.Button("Limpar lista", GUILayout.Width(110), GUILayout.Height(26)))
                    {
                        _rows.Clear();
                        _hasScanned = false;
                        _lastLog = string.Empty;
                    }
                }
            }

            EditorGUILayout.Space();
            DrawRows();

            if (!string.IsNullOrEmpty(_lastLog))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_lastLog, MessageType.Info);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.HelpBox(
                "Passo de REMOÇÃO (separado da varredura). Nada é removido em lote ou silenciosamente.\n" +
                "• Cada remoção exige confirmação explícita item-a-item (R7.4).\n" +
                "• Imediatamente antes de deletar, o GUID é re-verificado; se QUALQUER referência existir " +
                "(cena/prefab/SO) ou o asset estiver sob Resources, a remoção é REJEITADA e a origem relatada (R7.5).\n" +
                "• A deleção remove o asset JUNTO do .meta via AssetDatabase.DeleteAsset (R7.6).",
                MessageType.Warning);
        }

        private void DrawRows()
        {
            if (!_hasScanned)
            {
                EditorGUILayout.LabelField("Clique em \"Escanear candidatos\" para listar os órfãos.");
                return;
            }

            if (_rows.Count == 0)
            {
                EditorGUILayout.LabelField("Nenhum candidato a remoção encontrado.");
                return;
            }

            EditorGUILayout.LabelField($"{_rows.Count} candidato(s). Remoção é item-a-item.",
                EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            // Iterate over a snapshot so removing a row inside the loop is safe.
            foreach (var row in _rows.ToList())
            {
                DrawRow(row);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Row row)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(row.AssetPath, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField(FormatSize(row.SizeBytes), GUILayout.Width(80));
                }

                EditorGUILayout.LabelField(row.Reason, EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Selecionar no Project", GUILayout.Width(160)))
                    {
                        PingAsset(row.AssetPath);
                    }

                    GUILayout.FlexibleSpace();

                    // Only a pure orphan is one-click removable. Resources/whitelisted/referenced rows
                    // are shown for transparency but never get a Remove button here (R7.2/R7.7).
                    using (new EditorGUI.DisabledScope(row.Category != OrphanCategory.PureOrphan))
                    {
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                        if (GUILayout.Button("Remover (confirmar)", GUILayout.Width(170)))
                        {
                            ConfirmAndRemove(row);
                        }
                        GUI.backgroundColor = prev;
                    }
                }
            }
        }

        // ── Scan ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the candidate list using the exact same pipeline as the identify step:
        /// read reference files once, then run <see cref="AssetReferenceScanner"/> +
        /// <see cref="OrphanClassifier"/> per asset. Only pure orphans and Resources-path-check rows
        /// are listed; referenced/whitelisted assets are filtered out (they are never candidates).
        /// </summary>
        private void ScanCandidates()
        {
            _rows.Clear();
            _hasScanned = true;
            _lastLog = string.Empty;

            try
            {
                EditorUtility.DisplayProgressBar("Cleanup Remover", "Lendo arquivos de referência...", 0.1f);
                var referenceFiles = ReadReferenceFiles();

                EditorUtility.DisplayProgressBar("Cleanup Remover", "Classificando assets...", 0.5f);

                var allGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });
                var guidToOwnMeta = BuildOwnMetaLookup(allGuids);

                foreach (var guid in allGuids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath)) continue;

                    var normalized = assetPath.Replace('\\', '/');

                    var classification = ClassifyAgainstProject(guid, normalized, referenceFiles, guidToOwnMeta);

                    // Only unreferenced-and-not-whitelisted assets reach the list; the two listed
                    // categories are PureOrphan (removable) and RequiresPathVerification (shown, not
                    // one-click removable).
                    if (classification.Category != OrphanCategory.PureOrphan &&
                        classification.Category != OrphanCategory.RequiresPathVerification)
                    {
                        continue;
                    }

                    _rows.Add(new Row
                    {
                        AssetPath = normalized,
                        Guid = guid,
                        SizeBytes = GetFileSize(normalized),
                        Category = classification.Category,
                        Reason = classification.Reason,
                    });
                }

                _rows.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
                _lastLog = $"Varredura concluída: {_rows.Count} candidato(s). " +
                           "A remoção continua sendo item-a-item e confirmada (R7.4).";
            }
            catch (Exception e)
            {
                _lastLog = $"Falha ao escanear: {e.Message}";
                Debug.LogError($"[ProjectCleanupRemover] Scan failed: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ── Confirm + remove ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drives the removal of a single asset: it RE-VERIFIES with a fresh scan right before deleting
        /// (R7.5), asks for explicit confirmation for this one asset (R7.4), and only then deletes via
        /// <see cref="AssetDatabase.DeleteAsset"/> so the <c>.meta</c> goes with it (R7.6).
        /// </summary>
        private void ConfirmAndRemove(Row row)
        {
            // Guard: this path is only for pure orphans. Resources rows must never be deleted from here.
            if (row.Category != OrphanCategory.PureOrphan)
            {
                RejectAndReport(row, "asset sob Resources — remoção bloqueada aqui; confirme ausência de uso por path primeiro (R7.2).");
                return;
            }

            // 1. Re-verify against the CURRENT project state, not the (possibly stale) scan result.
            var referenceFiles = ReadReferenceFiles();
            var ownMeta = row.AssetPath + ".meta";
            var scanInput = referenceFiles.Where(f =>
                !string.Equals(f.Path, ownMeta, StringComparison.OrdinalIgnoreCase));

            var freshResult = AssetReferenceScanner.Scan(row.Guid, scanInput);
            var freshClassification = OrphanClassifier.Classify(row.AssetPath, freshResult);

            // 2. If anything now references the asset (or it is Resources-managed), REJECT and report.
            if (freshClassification.Category != OrphanCategory.PureOrphan)
            {
                if (freshResult.IsReferenced)
                {
                    RejectAndReport(row,
                        "remoção REJEITADA — referência encontrada em: " +
                        string.Join(", ", freshResult.ReferencingFiles));
                }
                else
                {
                    RejectAndReport(row, "remoção REJEITADA — " + freshClassification.Reason);
                }
                // Refresh the row so the UI reflects the newly-found reference.
                row.Category = freshClassification.Category;
                row.Reason = freshClassification.Reason;
                return;
            }

            // 3. Explicit, per-item user confirmation (R7.4). No bulk delete exists.
            var confirmed = EditorUtility.DisplayDialog(
                "Confirmar remoção item-a-item",
                $"Remover este asset?\n\n{row.AssetPath}\n({FormatSize(row.SizeBytes)})\n\n" +
                "Re-verificado agora: nenhuma referência de GUID encontrada em .unity/.prefab/.asset/.meta.\n" +
                "O asset será removido JUNTO do seu .meta (AssetDatabase.DeleteAsset). " +
                "Esta ação não pode ser desfeita facilmente.",
                "Remover",
                "Cancelar");

            if (!confirmed)
            {
                _lastLog = $"Cancelado pelo usuário: {row.AssetPath}";
                return;
            }

            // 4. Delete via AssetDatabase so the .meta is removed together (R7.6) — never a raw file delete.
            var deleted = AssetDatabase.DeleteAsset(row.AssetPath);
            if (deleted)
            {
                _rows.Remove(row);
                AssetDatabase.Refresh();
                _lastLog = $"Removido (asset + .meta): {row.AssetPath}";
                Debug.Log($"[ProjectCleanupRemover] Removido via AssetDatabase.DeleteAsset (asset + .meta): {row.AssetPath}");
            }
            else
            {
                _lastLog = $"Falha ao remover: {row.AssetPath} (AssetDatabase.DeleteAsset retornou false).";
                Debug.LogWarning($"[ProjectCleanupRemover] AssetDatabase.DeleteAsset falhou para {row.AssetPath}.");
            }
        }

        private void RejectAndReport(Row row, string message)
        {
            _lastLog = $"{row.AssetPath}: {message}";
            Debug.LogWarning($"[ProjectCleanupRemover] {row.AssetPath}: {message}");
            EditorUtility.DisplayDialog("Remoção rejeitada", $"{row.AssetPath}\n\n{message}", "OK");
            PingAsset(row.AssetPath);
        }

        // ── Shared pipeline helpers (mirror ProjectCleanupReport so both steps agree) ───────────

        /// <summary>
        /// Classifies one asset against the whole project: scans every reference file except the asset's
        /// own <c>.meta</c>, then applies the <see cref="OrphanClassifier"/> guards (R7.2/R7.7).
        /// </summary>
        private static OrphanClassification ClassifyAgainstProject(
            string guid, string normalizedPath,
            List<ReferenceFile> referenceFiles, Dictionary<string, string> guidToOwnMeta)
        {
            var ownMeta = guidToOwnMeta.TryGetValue(guid, out var m) ? m : null;
            var scanInput = referenceFiles.Where(f =>
                !string.Equals(f.Path, ownMeta, StringComparison.OrdinalIgnoreCase));

            var result = AssetReferenceScanner.Scan(guid, scanInput);
            return OrphanClassifier.Classify(normalizedPath, result);
        }

        private static Dictionary<string, string> BuildOwnMetaLookup(IEnumerable<string> guids)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    map[guid] = path.Replace('\\', '/') + ".meta";
                }
            }
            return map;
        }

        /// <summary>
        /// Reads every <c>.unity</c>/<c>.prefab</c>/<c>.asset</c>/<c>.meta</c> file under <c>Assets/</c>
        /// into memory so the pure <see cref="AssetReferenceScanner"/> can be run against the current
        /// project state (the re-verification before delete reads these fresh, R7.5).
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
                    Debug.LogWarning($"[ProjectCleanupRemover] Não foi possível ler '{absolute}': {e.Message}");
                    continue;
                }

                files.Add(new ReferenceFile(ToProjectRelative(absolute), content));
            }

            return files;
        }

        private static void PingAsset(string assetPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }

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
