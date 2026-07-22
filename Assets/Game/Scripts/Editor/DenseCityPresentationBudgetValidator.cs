#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Aggregates deterministic candidate ECS presentation evidence before publication. Packed
    /// content byte and duplication metrics remain explicit pending fields until candidate bundles
    /// can be built for the Editor-compatible target.
    /// </summary>
    public static class DenseCityPresentationBudgetValidator
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-22_dense_city_presentation_budget.json";
        internal const string CandidateBakeReportPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_validation.json";
        internal const string SharedArtReportPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_shared_art_ownership.json";
        internal const string TransformParityReportPath =
            OperationMapEntityPresentationTransformParityValidator.ReportPath;
        internal const string CandidateLayoutReportPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.json";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Candidate Presentation Budget")]
        public static void ValidateCurrentCandidate() => ValidateCurrentCandidateCore();

        public static void ValidateCurrentCandidateBatch() => ValidateCurrentCandidateCore();

        internal static void InvalidateEvidence(string projectRoot, string reason)
        {
            var report = new PresentationBudgetReport
            {
                schema = "warline.operation-map.dense-city-presentation-budget",
                schemaVersion = 1,
                result = "PresentationBudgetEvidenceInvalidated",
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                invalidationReason = reason ?? string.Empty,
                packedContentMetricsComplete = 0,
                entitySceneBytes = -1,
                sharedDependencyBytes = -1,
                duplicatedDependencyBytes = -1,
                productionCutover = 0
            };
            WriteReport(projectRoot, report);
        }

        private static void ValidateCurrentCandidateCore()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            CandidateBakeEvidence bake = Read<CandidateBakeEvidence>(projectRoot, CandidateBakeReportPath);
            SharedArtEvidence art = Read<SharedArtEvidence>(projectRoot, SharedArtReportPath);
            TransformParityEvidence parity = Read<TransformParityEvidence>(projectRoot, TransformParityReportPath);
            CandidateLayoutEvidence layout = Read<CandidateLayoutEvidence>(projectRoot, CandidateLayoutReportPath);

            if (!TryCreateReport(bake, art, parity, layout, out PresentationBudgetReport report, out string error))
                throw new InvalidOperationException($"Candidate presentation budget rejected: {error}");

            WriteReport(projectRoot, report);
            Debug.Log(
                $"[DenseCityPresentationBudget] result={report.result} " +
                $"identities={report.presentationIdentityCount} renderEntities={report.renderEntityCount} " +
                $"uniqueMeshes={report.uniqueMeshAssetCount} uniqueMaterials={report.uniqueMaterialAssetCount} " +
                $"packedMetrics={report.packedContentMetricsComplete} productionCutover=0");
        }

        internal static bool TryCreateReport(
            CandidateBakeEvidence bake,
            SharedArtEvidence art,
            TransformParityEvidence parity,
            CandidateLayoutEvidence layout,
            out PresentationBudgetReport report,
            out string error)
        {
            report = null;
            if (bake == null || art == null || parity == null || layout == null)
            {
                error = "required-evidence-null";
                return false;
            }
            if (!string.Equals(bake.result, "CandidateBakeValidationPassed", StringComparison.Ordinal) ||
                bake.gameplayBuildingCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayBuildings ||
                bake.gameplayVehicleCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayVehicles ||
                bake.presentationRootCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedPresentationRoots ||
                bake.presentationIdentityCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedPresentationIdentities ||
                bake.renderMeshEntityCount <= 0 || bake.nonFiniteTransformCount != 0 ||
                bake.managedMapVisualCompanionCount != 0)
            {
                error = "candidate-bake-budget";
                return false;
            }
            if (!string.Equals(art.result, "SharedArtOwnershipProven", StringComparison.Ordinal) ||
                art.sourceCount <= 0 || art.uniqueMeshAssetCount <= 0 ||
                art.uniqueMaterialAssetCount <= 0 || art.meshPlacementReferenceCount < art.uniqueMeshAssetCount ||
                art.materialReferenceCount < art.uniqueMaterialAssetCount || art.missingAssetCount != 0 ||
                !art.compactInstanceDataProven)
            {
                error = "shared-art-budget";
                return false;
            }
            if (!string.Equals(parity.checkpoint, "ecs-bake", StringComparison.Ordinal) ||
                !string.Equals(parity.result, "SourceCandidateBakedParityPassed", StringComparison.Ordinal) ||
                parity.candidateIdentityCount != bake.presentationIdentityCount ||
                parity.bakedIdentityCount != bake.presentationIdentityCount ||
                parity.rejectedRowCount != 0 || parity.bakedRenderEntityCount != bake.renderMeshEntityCount)
            {
                error = "transform-bounds-parity-budget";
                return false;
            }
            if (!string.Equals(
                    layout.result,
                    "CandidateEntitySceneAddressablesLayoutReady",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(layout.entitySceneGuid) || layout.entryCount <= 0 ||
                layout.sharedDependencyCount <= 0 || layout.staticManifestEntryCount != 0 ||
                layout.presentationChunkEntryCount != 0 || layout.legacyPlacementEntryCount != 0 ||
                layout.productionAddressablesMutated != 0)
            {
                error = "candidate-layout-budget";
                return false;
            }

            report = new PresentationBudgetReport
            {
                schema = "warline.operation-map.dense-city-presentation-budget",
                schemaVersion = 1,
                result = "PresentationBudgetCorePassedPendingPackedContentMetrics",
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                gameplayBuildingCount = bake.gameplayBuildingCount,
                gameplayVehicleCount = bake.gameplayVehicleCount,
                presentationRootCount = bake.presentationRootCount,
                presentationIdentityCount = bake.presentationIdentityCount,
                renderEntityCount = bake.renderMeshEntityCount,
                buildingRenderChildCount = bake.buildingRenderChildCount,
                nonFiniteTransformCount = bake.nonFiniteTransformCount,
                managedMapVisualCompanionCount = bake.managedMapVisualCompanionCount,
                sourceRendererCount = art.sourceCount,
                uniqueMeshAssetCount = art.uniqueMeshAssetCount,
                uniqueMaterialAssetCount = art.uniqueMaterialAssetCount,
                uniquePrefabAssetCount = art.uniquePrefabAssetCount,
                meshPlacementReferenceCount = art.meshPlacementReferenceCount,
                materialReferenceCount = art.materialReferenceCount,
                repeatedMeshAssetCount = art.repeatedMeshAssetCount,
                repeatedMaterialAssetCount = art.repeatedMaterialAssetCount,
                compactInstanceDataProven = art.compactInstanceDataProven ? 1 : 0,
                transformParityIdentityCount = parity.bakedIdentityCount,
                transformedBoundsEntityCount = parity.bakedRenderEntityCount,
                transformParityRejectedRowCount = parity.rejectedRowCount,
                entitySceneGuid = layout.entitySceneGuid,
                candidateEntryCount = layout.entryCount,
                candidateSharedDependencyCount = layout.sharedDependencyCount,
                candidateStaticManifestEntryCount = layout.staticManifestEntryCount,
                candidatePresentationChunkEntryCount = layout.presentationChunkEntryCount,
                candidateLegacyPlacementEntryCount = layout.legacyPlacementEntryCount,
                packedContentMetricsComplete = 0,
                entitySceneBytes = -1,
                sharedDependencyBytes = -1,
                duplicatedDependencyBytes = -1,
                productionCutover = 0
            };
            error = null;
            return true;
        }

        internal static string ToDeterministicJson(PresentationBudgetReport report) =>
            JsonUtility.ToJson(report, true) + "\n";

        private static void WriteReport(string projectRoot, PresentationBudgetReport report)
        {
            string physicalPath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath) ?? projectRoot);
            File.WriteAllText(physicalPath, ToDeterministicJson(report), Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static T Read<T>(string projectRoot, string path) where T : class
        {
            string physicalPath = Path.Combine(projectRoot, path);
            if (!File.Exists(physicalPath))
                throw new FileNotFoundException($"Presentation-budget evidence is missing: {path}", physicalPath);
            T value = JsonUtility.FromJson<T>(File.ReadAllText(physicalPath, Utf8WithoutBom));
            return value ?? throw new InvalidOperationException($"Presentation-budget evidence is invalid: {path}");
        }

        [Serializable]
        internal sealed class CandidateBakeEvidence
        {
            public string result;
            public int gameplayBuildingCount;
            public int gameplayVehicleCount;
            public int presentationRootCount;
            public int presentationIdentityCount;
            public int renderMeshEntityCount;
            public int buildingRenderChildCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
        }

        [Serializable]
        internal sealed class SharedArtEvidence
        {
            public string result;
            public int sourceCount;
            public int uniqueMeshAssetCount;
            public int uniqueMaterialAssetCount;
            public int uniquePrefabAssetCount;
            public int meshPlacementReferenceCount;
            public int materialReferenceCount;
            public int repeatedMeshAssetCount;
            public int repeatedMaterialAssetCount;
            public int missingAssetCount;
            public bool compactInstanceDataProven;
        }

        [Serializable]
        internal sealed class TransformParityEvidence
        {
            public string checkpoint;
            public string result;
            public int candidateIdentityCount;
            public int bakedIdentityCount;
            public int rejectedRowCount;
            public int bakedRenderEntityCount;
        }

        [Serializable]
        internal sealed class CandidateLayoutEvidence
        {
            public string result;
            public string entitySceneGuid;
            public int entryCount;
            public int sharedDependencyCount;
            public int staticManifestEntryCount;
            public int presentationChunkEntryCount;
            public int legacyPlacementEntryCount;
            public int productionAddressablesMutated;
        }

        [Serializable]
        internal sealed class PresentationBudgetReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string invalidationReason;
            public int gameplayBuildingCount;
            public int gameplayVehicleCount;
            public int presentationRootCount;
            public int presentationIdentityCount;
            public int renderEntityCount;
            public int buildingRenderChildCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
            public int sourceRendererCount;
            public int uniqueMeshAssetCount;
            public int uniqueMaterialAssetCount;
            public int uniquePrefabAssetCount;
            public int meshPlacementReferenceCount;
            public int materialReferenceCount;
            public int repeatedMeshAssetCount;
            public int repeatedMaterialAssetCount;
            public int compactInstanceDataProven;
            public int transformParityIdentityCount;
            public int transformedBoundsEntityCount;
            public int transformParityRejectedRowCount;
            public string entitySceneGuid;
            public int candidateEntryCount;
            public int candidateSharedDependencyCount;
            public int candidateStaticManifestEntryCount;
            public int candidatePresentationChunkEntryCount;
            public int candidateLegacyPlacementEntryCount;
            public int packedContentMetricsComplete;
            public long entitySceneBytes;
            public long sharedDependencyBytes;
            public long duplicatedDependencyBytes;
            public int productionCutover;
        }
    }
}

#endif
