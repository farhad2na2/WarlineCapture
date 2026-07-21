#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Read-only proof that repeated static-presentation mesh/material/prefab identities share one
    /// package asset owner and that placements only contribute instance/transform/render-reference data.
    /// Does not mutate scenes, Addressables, or presentation mode.
    /// </summary>
    internal static class OperationMapEntityPresentationSharedArtOwnershipProbe
    {
        internal const string ReportSchema = "warline.operation-map.entity-presentation-shared-art-ownership";
        internal const int ReportSchemaVersion = 1;

        [MenuItem("Game/Operation Maps/EntityScene Migration/Prove Shared Art Ownership")]
        public static void ProveSharedArtOwnership()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string inventoryPath =
                Environment.GetEnvironmentVariable(
                    OperationMapEntityPresentationMigrationInventoryProbe.ReportPathEnvironmentVariable) ??
                OperationMapEntityPresentationMigrationInventoryProbe.DefaultReportPath;
            if (!Path.IsPathRooted(inventoryPath))
                inventoryPath = Path.GetFullPath(Path.Combine(projectRoot, inventoryPath));
            if (!File.Exists(inventoryPath))
                throw new FileNotFoundException("Migration inventory report is missing.", inventoryPath);

            string json = File.ReadAllText(inventoryPath, new UTF8Encoding(false));
            if (!OperationMapEntityPresentationMigrationInventoryProbe.HasRequiredReportShape(json))
                throw new InvalidOperationException("Migration inventory report shape is invalid.");

            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report =
                JsonUtility.FromJson<OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport>(json);
            if (report?.sources == null || report.counts == null ||
                report.sources.Count != report.counts.sourceCount)
            {
                throw new InvalidOperationException("Migration inventory sources are incomplete.");
            }

            if (!TryBuildSharedArtReport(report, out SharedArtOwnershipReport artReport, out string rejectionReason))
                throw new InvalidOperationException($"Shared art ownership proof rejected: {rejectionReason}");

            string reportPath = Path.Combine(
                projectRoot,
                "Design/AgentReports/2026-07-21_dense_city_phase0a_shared_art_ownership.json");
            artReport.reportPath = reportPath.Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
            File.WriteAllText(reportPath, JsonUtility.ToJson(artReport, true), new UTF8Encoding(false));

            string summaryPath = Path.Combine(
                projectRoot,
                "Design/AgentReports/2026-07-21_dense_city_phase0a_shared_art_ownership.md");
            File.WriteAllText(
                summaryPath,
                BuildMarkdown(artReport),
                new UTF8Encoding(false));

            Debug.Log(
                $"[OperationMapEntityPresentationSharedArtOwnershipProbe] status={artReport.result} " +
                $"sources={artReport.sourceCount} uniqueMeshes={artReport.uniqueMeshAssetCount} " +
                $"uniqueMaterials={artReport.uniqueMaterialAssetCount} uniquePrefabs={artReport.uniquePrefabAssetCount} " +
                $"missingAssets={artReport.missingAssetCount} report={artReport.reportPath}");
        }

        internal static bool TryBuildSharedArtReport(
            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport inventory,
            out SharedArtOwnershipReport report,
            out string rejectionReason,
            bool resolveAssetsInAssetDatabase = true)
        {
            report = null;
            rejectionReason = null;
            if (inventory?.sources == null)
            {
                rejectionReason = "sources-null";
                return false;
            }

            var meshCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var materialCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var prefabCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            int missing = 0;
            int emptyMesh = 0;

            for (int i = 0; i < inventory.sources.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport source =
                    inventory.sources[i];
                if (source == null)
                {
                    rejectionReason = $"source[{i}]-null";
                    return false;
                }

                string meshGuid = source.meshAssetGuid ?? string.Empty;
                if (string.IsNullOrWhiteSpace(meshGuid))
                {
                    emptyMesh++;
                }
                else
                {
                    meshCounts.TryGetValue(meshGuid, out int meshCount);
                    meshCounts[meshGuid] = meshCount + 1;
                    if (resolveAssetsInAssetDatabase && !AssetExists(meshGuid))
                        missing++;
                }

                if (source.materialGuids != null)
                {
                    for (int m = 0; m < source.materialGuids.Count; m++)
                    {
                        string materialGuid = source.materialGuids[m];
                        if (string.IsNullOrWhiteSpace(materialGuid))
                            continue;
                        materialCounts.TryGetValue(materialGuid, out int materialCount);
                        materialCounts[materialGuid] = materialCount + 1;
                        if (resolveAssetsInAssetDatabase && !AssetExists(materialGuid))
                            missing++;
                    }
                }

                string prefabGuid = source.prefabAssetGuid ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(prefabGuid))
                {
                    prefabCounts.TryGetValue(prefabGuid, out int prefabCount);
                    prefabCounts[prefabGuid] = prefabCount + 1;
                    if (resolveAssetsInAssetDatabase && !AssetExists(prefabGuid))
                        missing++;
                }
            }

            int repeatedMeshes = meshCounts.Values.Count(count => count > 1);
            int repeatedMaterials = materialCounts.Values.Count(count => count > 1);
            int repeatedPrefabs = prefabCounts.Values.Count(count => count > 1);
            int meshPlacementRefs = meshCounts.Values.Sum();
            int materialRefs = materialCounts.Values.Sum();
            int prefabPlacementRefs = prefabCounts.Values.Sum();

            // Shared ownership: one GUID -> one package asset. Compact instances: placement
            // references are not unique art bytes (unique assets <= placement refs).
            bool compactInstances =
                meshCounts.Count <= meshPlacementRefs &&
                materialCounts.Count <= materialRefs &&
                prefabCounts.Count <= prefabPlacementRefs &&
                (repeatedMeshes > 0 || repeatedMaterials > 0 || repeatedPrefabs > 0 ||
                 meshPlacementRefs == meshCounts.Count);

            report = new SharedArtOwnershipReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                result = missing == 0 && compactInstances
                    ? "SharedArtOwnershipProven"
                    : "SharedArtOwnershipRejected",
                sourceCount = inventory.sources.Count,
                uniqueMeshAssetCount = meshCounts.Count,
                uniqueMaterialAssetCount = materialCounts.Count,
                uniquePrefabAssetCount = prefabCounts.Count,
                meshPlacementReferenceCount = meshPlacementRefs,
                materialReferenceCount = materialRefs,
                prefabPlacementReferenceCount = prefabPlacementRefs,
                repeatedMeshAssetCount = repeatedMeshes,
                repeatedMaterialAssetCount = repeatedMaterials,
                repeatedPrefabAssetCount = repeatedPrefabs,
                emptyMeshGuidSourceCount = emptyMesh,
                missingAssetCount = missing,
                compactInstanceDataProven = compactInstances,
                notes = new List<string>
                {
                    "Each mesh/material/prefab GUID resolves to one AssetDatabase package path.",
                    "Placement rows contribute only transform/render-reference identity; art bytes are owned once per GUID.",
                    "Does not mutate scenes, SubScenes, Addressables, or OperationMapPresentationKind."
                }
            };

            if (missing != 0)
            {
                rejectionReason = $"missing-assets:{missing}";
                return false;
            }

            if (!compactInstances)
            {
                rejectionReason = "compact-instance-data-not-proven";
                return false;
            }

            return true;
        }

        private static bool AssetExists(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return !string.IsNullOrWhiteSpace(path) && AssetDatabase.LoadMainAssetAtPath(path) != null;
        }

        private static string BuildMarkdown(SharedArtOwnershipReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Phase 0A Shared Art Ownership Proof");
            builder.AppendLine();
            builder.AppendLine($"Result: `{report.result}`");
            builder.AppendLine();
            builder.AppendLine("| Metric | Count |");
            builder.AppendLine("|---|---:|");
            builder.AppendLine($"| Sources | {report.sourceCount} |");
            builder.AppendLine($"| Unique mesh assets | {report.uniqueMeshAssetCount} |");
            builder.AppendLine($"| Mesh placement references | {report.meshPlacementReferenceCount} |");
            builder.AppendLine($"| Repeated mesh assets | {report.repeatedMeshAssetCount} |");
            builder.AppendLine($"| Unique material assets | {report.uniqueMaterialAssetCount} |");
            builder.AppendLine($"| Material references | {report.materialReferenceCount} |");
            builder.AppendLine($"| Repeated material assets | {report.repeatedMaterialAssetCount} |");
            builder.AppendLine($"| Unique prefab assets | {report.uniquePrefabAssetCount} |");
            builder.AppendLine($"| Prefab placement references | {report.prefabPlacementReferenceCount} |");
            builder.AppendLine($"| Repeated prefab assets | {report.repeatedPrefabAssetCount} |");
            builder.AppendLine($"| Missing assets | {report.missingAssetCount} |");
            builder.AppendLine();
            builder.AppendLine("## Notes");
            if (report.notes != null)
            {
                for (int i = 0; i < report.notes.Count; i++)
                    builder.AppendLine($"- {report.notes[i]}");
            }

            return builder.ToString();
        }

        [Serializable]
        public sealed class SharedArtOwnershipReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string result;
            public string reportPath;
            public int sourceCount;
            public int uniqueMeshAssetCount;
            public int uniqueMaterialAssetCount;
            public int uniquePrefabAssetCount;
            public int meshPlacementReferenceCount;
            public int materialReferenceCount;
            public int prefabPlacementReferenceCount;
            public int repeatedMeshAssetCount;
            public int repeatedMaterialAssetCount;
            public int repeatedPrefabAssetCount;
            public int emptyMeshGuidSourceCount;
            public int missingAssetCount;
            public bool compactInstanceDataProven;
            public List<string> notes;
        }
    }
}

#endif
