#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;

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
        internal const string DenseOwnershipReportPath =
            "Design/AgentReports/2026-07-25_dense_city_entity_scene_ownership.json";
        internal const string DenseCandidateBakeReportPath =
            "Design/AgentReports/2026-07-24_dense_city_generated_candidate_bake_validation.json";
        internal const string DenseTransformParityReportPath =
            OperationMapDenseCityGeneratedTransformParityValidator.DefaultReportPath;
        internal const string DenseCandidateLayoutReportPath =
            "Design/AgentReports/2026-07-24_dense_city_candidate_entityscene_addressables_layout.json";
        internal const string DenseRuntimeContentReportPath =
            "Design/AgentReports/2026-07-24_dense_city_candidate_runtime_content.json";
        internal const string DensePackedAssetSharingReportPath =
            "Design/AgentReports/2026-07-25_dense_city_packed_asset_sharing.json";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Candidate Presentation Budget")]
        public static void ValidateCurrentCandidate() => ValidateCurrentCandidateCore();

        public static void ValidateCurrentCandidateBatch() => ValidateCurrentCandidateCore();

        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Dense City EntityScene Ownership")]
        public static void ValidateDenseCityEntitySceneOwnership()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            DenseCandidateBakeEvidence bake =
                Read<DenseCandidateBakeEvidence>(projectRoot, DenseCandidateBakeReportPath);
            TransformParityEvidence existingParity =
                Read<TransformParityEvidence>(projectRoot, TransformParityReportPath);
            DenseTransformParityEvidence parity =
                Read<DenseTransformParityEvidence>(projectRoot, DenseTransformParityReportPath);
            DenseCandidateLayoutEvidence layout =
                Read<DenseCandidateLayoutEvidence>(projectRoot, DenseCandidateLayoutReportPath);
            string entitySceneGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);

            if (!TryCreateDenseOwnershipReport(
                    bake,
                    existingParity,
                    parity,
                    layout,
                    entitySceneGuid,
                    out DenseEntitySceneOwnershipReport report,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Dense-city EntityScene ownership rejected: {error}");
            }

            WriteDenseOwnershipReport(projectRoot, report);
            Debug.Log(
                $"[DenseCityEntitySceneOwnership] result={report.result} " +
                $"existingIdentities={report.existingVisualIdentityCount} " +
                $"generatedIdentities={report.generatedVisualIdentityCount} " +
                $"generatedRenderers={report.generatedBakedRenderEntityCount} " +
                $"authoringEntries={report.authoringSceneEntryCount} " +
                $"staticEntries={report.staticPresentationEntryCount} " +
                $"managedCompanions={report.managedMapVisualCompanionCount} productionCutover=0");
        }

        public static void ValidateDenseCityEntitySceneOwnershipBatch() =>
            ValidateDenseCityEntitySceneOwnership();

        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Dense City Packed Asset Sharing")]
        public static void ValidateDenseCityPackedAssetSharing()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            DenseCandidateBakeEvidence bake =
                Read<DenseCandidateBakeEvidence>(projectRoot, DenseCandidateBakeReportPath);
            DenseTransformParityEvidence parity =
                Read<DenseTransformParityEvidence>(projectRoot, DenseTransformParityReportPath);
            DenseRuntimeContentEvidence runtime =
                Read<DenseRuntimeContentEvidence>(projectRoot, DenseRuntimeContentReportPath);

            if (!TryCreateDensePackedAssetSharingReport(
                    bake,
                    parity,
                    runtime,
                    out DensePackedAssetSharingReport report,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Dense-city packed asset sharing rejected: {error}");
            }

            WriteJsonReport(projectRoot, DensePackedAssetSharingReportPath, report);
            Debug.Log(
                $"[DenseCityPackedAssetSharing] result={report.result} " +
                $"renderEntities={report.renderEntityCount} " +
                $"renderMeshArrays={report.sharedRenderMeshArrayIdentityCount} " +
                $"archiveBytes={report.entitySceneArchiveBytes} " +
                $"duplicatedGuids={report.duplicatedDependencyGuidCount} " +
                $"duplicatedBytes={report.duplicatedDependencyBytes} productionCutover=0");
        }

        public static void ValidateDenseCityPackedAssetSharingBatch() =>
            ValidateDenseCityPackedAssetSharing();

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
            CandidateGeometryEvidence geometry = CaptureCandidateGeometryEvidence();

            if (!TryCreateReport(
                    bake,
                    art,
                    parity,
                    layout,
                    geometry,
                    out PresentationBudgetReport report,
                    out string error))
                throw new InvalidOperationException($"Candidate presentation budget rejected: {error}");

            WriteReport(projectRoot, report);
            Debug.Log(
                $"[DenseCityPresentationBudget] result={report.result} " +
                $"identities={report.presentationIdentityCount} renderEntities={report.renderEntityCount} " +
                $"uniqueMeshes={report.uniqueMeshAssetCount} uniqueMaterials={report.uniqueMaterialAssetCount} " +
                $"triangles={report.instancedTriangleCount} shadows={report.shadowCasterCount} " +
                $"packedMetrics={report.packedContentMetricsComplete} productionCutover=0");
        }

        internal static bool TryCreateReport(
            CandidateBakeEvidence bake,
            SharedArtEvidence art,
            TransformParityEvidence parity,
            CandidateLayoutEvidence layout,
            CandidateGeometryEvidence geometry,
            out PresentationBudgetReport report,
            out string error)
        {
            report = null;
            if (bake == null || art == null || parity == null || layout == null || geometry == null)
            {
                error = "required-evidence-null";
                return false;
            }
            if (!string.Equals(bake.result, "CandidateBakeValidationPassed", StringComparison.Ordinal) ||
                bake.gameplayBuildingCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayBuildings ||
                bake.gameplayVehicleCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayVehicles ||
                bake.presentationRootCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedPresentationRoots ||
                bake.presentationIdentityCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedPresentationIdentities ||
                bake.gameplayBuildingIdentityCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayBuildings ||
                bake.gameplayVehicleIdentityCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayVehicles ||
                bake.renderOnlyIdentityCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedRenderOnlyOwners ||
                bake.unknownRoleIdentityCount != 0 || bake.totalEntityCount <= 0 ||
                bake.entityArchetypeCount <= 0 || bake.entityChunkCount <= 0 ||
                bake.renderMeshEntityCount <= 0 || bake.nonFiniteTransformCount != 0 ||
                bake.renderChildEntityCount <= 0 || bake.sharedRenderMeshArrayIdentityCount <= 0 ||
                bake.sharedMeshAssetIdentityCount <= 0 || bake.sharedMaterialAssetIdentityCount <= 0 ||
                bake.intactVisualRootCount != OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayBuildings ||
                bake.missingIntactVisualRootCount != 0 ||
                bake.sharedIntactDestroyedVisualRootCount != 0 ||
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
            if (!string.Equals(geometry.result, "CandidateGeometryEvidencePassed", StringComparison.Ordinal) ||
                geometry.acceptedSourceAuthoredRendererCount <= 0 ||
                geometry.authoredRendererCount <= 0 || geometry.activeRendererCount <= 0 ||
                geometry.uniqueMeshAssetCount <= 0 || geometry.uniqueMaterialAssetCount <= 0 ||
                geometry.uniqueTextureAssetCount <= 0 || geometry.uniqueTriangleCount <= 0 ||
                geometry.instancedTriangleCount < geometry.uniqueTriangleCount ||
                geometry.shadowCasterCount < 0 ||
                geometry.batchingEligibleRendererCount <= 0 ||
                geometry.batchingEligibleRendererCount > geometry.authoredRendererCount ||
                geometry.missingAssetReferenceCount != 0 || geometry.nonFiniteBoundsCount != 0 ||
                !IsFinitePositive(geometry.worldBoundsSize.x) ||
                !IsFinitePositive(geometry.worldBoundsSize.z) ||
                !IsFinitePositive(geometry.rendererDensityPerSquareKilometer))
            {
                error = "candidate-geometry-budget";
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
                gameplayBuildingIdentityCount = bake.gameplayBuildingIdentityCount,
                gameplayVehicleIdentityCount = bake.gameplayVehicleIdentityCount,
                renderOnlyIdentityCount = bake.renderOnlyIdentityCount,
                totalEntityCount = bake.totalEntityCount,
                entityArchetypeCount = bake.entityArchetypeCount,
                entityChunkCount = bake.entityChunkCount,
                renderEntityCount = bake.renderMeshEntityCount,
                renderChildEntityCount = bake.renderChildEntityCount,
                sharedRenderMeshArrayIdentityCount = bake.sharedRenderMeshArrayIdentityCount,
                sharedMeshAssetIdentityCount = bake.sharedMeshAssetIdentityCount,
                sharedMaterialAssetIdentityCount = bake.sharedMaterialAssetIdentityCount,
                buildingRenderChildCount = bake.buildingRenderChildCount,
                intactVisualRootCount = bake.intactVisualRootCount,
                destroyedVisualRootCount = bake.destroyedVisualRootCount,
                missingIntactVisualRootCount = bake.missingIntactVisualRootCount,
                missingDestroyedVisualRootCount = bake.missingDestroyedVisualRootCount,
                sharedIntactDestroyedVisualRootCount = bake.sharedIntactDestroyedVisualRootCount,
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
                acceptedSourceAuthoredRendererCount = geometry.acceptedSourceAuthoredRendererCount,
                candidateAuthoringRendererCount = geometry.authoredRendererCount,
                candidateActiveRendererCount = geometry.activeRendererCount,
                candidateUniqueMeshAssetCount = geometry.uniqueMeshAssetCount,
                candidateUniqueMaterialAssetCount = geometry.uniqueMaterialAssetCount,
                uniqueTextureAssetCount = geometry.uniqueTextureAssetCount,
                uniqueTriangleCount = geometry.uniqueTriangleCount,
                instancedTriangleCount = geometry.instancedTriangleCount,
                shadowCasterCount = geometry.shadowCasterCount,
                batchingEligibleRendererCount = geometry.batchingEligibleRendererCount,
                missingGeometryAssetReferenceCount = geometry.missingAssetReferenceCount,
                nonFiniteRendererBoundsCount = geometry.nonFiniteBoundsCount,
                worldBoundsCenter = geometry.worldBoundsCenter,
                worldBoundsSize = geometry.worldBoundsSize,
                rendererDensityPerSquareKilometer = geometry.rendererDensityPerSquareKilometer,
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

        internal static bool TryCreateDenseOwnershipReport(
            DenseCandidateBakeEvidence bake,
            TransformParityEvidence existingParity,
            DenseTransformParityEvidence parity,
            DenseCandidateLayoutEvidence layout,
            string expectedEntitySceneGuid,
            out DenseEntitySceneOwnershipReport report,
            out string error)
        {
            report = null;
            if (bake == null || existingParity == null || parity == null || layout == null)
            {
                error = "dense-ownership-evidence-null";
                return false;
            }

            if (!string.Equals(bake.result, "DenseCandidateBakeValidationPassed", StringComparison.Ordinal) ||
                bake.authoringDenseIdentityCount !=
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedIdentities ||
                bake.denseIdentityCount != bake.authoringDenseIdentityCount ||
                bake.authoringDenseGameplayBuildingIdentityCount !=
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedGameplayBuildings ||
                bake.denseGameplayBuildingIdentityCount !=
                bake.authoringDenseGameplayBuildingIdentityCount ||
                bake.authoringDenseRenderOnlyIdentityCount !=
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedRenderOnlyOwners ||
                bake.denseRenderOnlyIdentityCount != bake.authoringDenseRenderOnlyIdentityCount ||
                bake.legacyPresentationIdentityCount !=
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedPresentationIdentities ||
                bake.denseUnknownRoleIdentityCount != 0 ||
                bake.duplicateDenseIdentityCount != 0 ||
                bake.missingIntactVisualRootCount != 0 ||
                bake.sharedIntactDestroyedVisualRootCount != 0 ||
                bake.nonFiniteTransformCount != 0 ||
                bake.managedMapVisualCompanionCount != 0)
            {
                error = "dense-baked-visual-ownership";
                return false;
            }

            if (!string.Equals(existingParity.checkpoint, "ecs-bake", StringComparison.Ordinal) ||
                !string.Equals(
                    existingParity.result,
                    "SourceCandidateBakedParityPassed",
                    StringComparison.Ordinal) ||
                existingParity.candidateIdentityCount != bake.legacyPresentationIdentityCount ||
                existingParity.bakedIdentityCount != existingParity.candidateIdentityCount ||
                existingParity.bakedRenderEntityCount <= 0 ||
                existingParity.rejectedRowCount != 0)
            {
                error = "dense-existing-renderer-parity";
                return false;
            }

            if (!string.Equals(
                    parity.result,
                    "DenseCityGeneratedTransformParityPassed",
                    StringComparison.Ordinal) ||
                parity.candidateIdentityCount != bake.denseIdentityCount ||
                parity.uniqueCandidateIdentityCount != parity.candidateIdentityCount ||
                parity.bakedIdentityCount != parity.candidateIdentityCount ||
                parity.uniqueBakedIdentityCount != parity.bakedIdentityCount ||
                parity.generatedCandidateRendererEntityCount <= 0 ||
                parity.generatedBakedRenderEntityCount !=
                parity.generatedCandidateRendererEntityCount ||
                parity.persistentGeneratedSourceFailureCount != 0 ||
                parity.unresolvedGeneratedRendererEntityCount != 0 ||
                parity.unconsumedCandidateRendererEntityCount != 0 ||
                parity.rejectedRowCount != 0 ||
                parity.duplicateCandidateStableIdCount != 0 ||
                parity.duplicateBakedStableIdCount != 0 ||
                parity.missingBakedStableIdCount != 0 ||
                parity.unexpectedBakedStableIdCount != 0 ||
                string.IsNullOrWhiteSpace(parity.candidateIdentitySetSha256) ||
                !string.Equals(
                    parity.candidateIdentitySetSha256,
                    parity.bakedIdentitySetSha256,
                    StringComparison.Ordinal))
            {
                error = "dense-generated-renderer-parity";
                return false;
            }

            if (!TryValidateDenseOwnershipLayout(
                    layout,
                    expectedEntitySceneGuid,
                    out int authoringSceneEntryCount,
                    out int staticPresentationEntryCount,
                    out error))
            {
                return false;
            }

            report = new DenseEntitySceneOwnershipReport
            {
                schema = "warline.operation-map.dense-city-entity-scene-ownership",
                schemaVersion = 1,
                result = "DenseCityEntitySceneOwnershipPassed",
                operationMapId =
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                entitySceneGuid = layout.entitySceneGuid,
                existingVisualIdentityCount = bake.legacyPresentationIdentityCount,
                existingBakedRenderEntityCount = existingParity.bakedRenderEntityCount,
                generatedVisualIdentityCount = bake.denseIdentityCount,
                generatedCandidateRendererEntityCount =
                    parity.generatedCandidateRendererEntityCount,
                generatedBakedRenderEntityCount = parity.generatedBakedRenderEntityCount,
                missingGeneratedVisualIdentityCount = parity.missingBakedStableIdCount,
                unexpectedGeneratedVisualIdentityCount = parity.unexpectedBakedStableIdCount,
                managedMapVisualCompanionCount = bake.managedMapVisualCompanionCount,
                authoringSceneEntryCount = authoringSceneEntryCount,
                explicitSharedDependencyEntryCount = layout.sharedDependencyCount,
                staticPresentationEntryCount = staticPresentationEntryCount,
                legacyPlacementEntryCount = layout.legacyPlacementEntryCount,
                productionAddressablesMutated = layout.productionAddressablesMutated,
                productionCutover = 0
            };
            error = null;
            return true;
        }

        internal static bool TryCreateDensePackedAssetSharingReport(
            DenseCandidateBakeEvidence bake,
            DenseTransformParityEvidence parity,
            DenseRuntimeContentEvidence runtime,
            out DensePackedAssetSharingReport report,
            out string error)
        {
            report = null;
            if (bake == null || parity == null || runtime == null)
            {
                error = "packed-asset-sharing-evidence-null";
                return false;
            }

            if (!string.Equals(
                    bake.result,
                    "DenseCandidateBakeValidationPassed",
                    StringComparison.Ordinal) ||
                bake.renderMeshEntityCount <= 0 ||
                bake.sharedRenderMeshArrayIdentityCount != 1 ||
                bake.sharedMeshAssetIdentityCount <= 0 ||
                bake.sharedMaterialAssetIdentityCount <= 0 ||
                bake.managedMapVisualCompanionCount != 0)
            {
                error = "packed-asset-sharing-bake";
                return false;
            }

            if (!string.Equals(
                    parity.result,
                    "DenseCityGeneratedTransformParityPassed",
                    StringComparison.Ordinal) ||
                parity.generatedBakedRenderEntityCount <= 0 ||
                parity.persistentGeneratedSourceFailureCount != 0 ||
                parity.repeatedGeneratedPrefabSourceCount <= 0 ||
                parity.repeatedGeneratedPrefabPlacementCount <= 0 ||
                parity.repeatedGeneratedPresentationSignatureCount <= 0 ||
                parity.repeatedGeneratedPresentationEntryCount <= 0 ||
                parity.unresolvedGeneratedMeshCount != 0 ||
                parity.unresolvedGeneratedMaterialCount != 0 ||
                parity.generatedMeshMismatchCount != 0 ||
                parity.generatedMaterialMismatchCount != 0 ||
                parity.generatedManagedInstanceComponentCount != 0 ||
                parity.repeatedSignatureAssetPairMismatchCount != 0 ||
                parity.rejectedRowCount != 0)
            {
                error = "packed-asset-sharing-parity";
                return false;
            }

            if (!string.Equals(
                    runtime.result,
                    "DenseCityCandidateRuntimeContentBuilt",
                    StringComparison.Ordinal) ||
                runtime.schemaVersion != 9 ||
                !string.Equals(
                    runtime.operationMapId,
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    StringComparison.Ordinal) ||
                runtime.packedDependencyMetricsComplete != 1 ||
                runtime.sharedDependencyGuidCount != 0 ||
                runtime.sharedDependencyBytes != 0 ||
                runtime.duplicatedDependencyGuidCount != 0 ||
                runtime.duplicatedDependencyBytes != 0 ||
                runtime.entityContentArchiveCount != 1 ||
                runtime.entitySceneArchiveBytes <= 0 ||
                runtime.staticRuntimeEntryCount != 0 ||
                runtime.productionSettingsMutated != 0 ||
                runtime.productionCutover != 0)
            {
                error = "packed-asset-sharing-runtime";
                return false;
            }

            report = new DensePackedAssetSharingReport
            {
                schema = "warline.operation-map.dense-city-packed-asset-sharing",
                schemaVersion = 1,
                result = "DenseCityPackedAssetSharingPassed",
                operationMapId = runtime.operationMapId,
                entitySceneGuid = runtime.entitySceneGuid,
                renderEntityCount = bake.renderMeshEntityCount,
                sharedRenderMeshArrayIdentityCount =
                    bake.sharedRenderMeshArrayIdentityCount,
                sharedMeshAssetIdentityCount = bake.sharedMeshAssetIdentityCount,
                sharedMaterialAssetIdentityCount =
                    bake.sharedMaterialAssetIdentityCount,
                repeatedGeneratedPrefabSourceCount =
                    parity.repeatedGeneratedPrefabSourceCount,
                repeatedGeneratedPrefabPlacementCount =
                    parity.repeatedGeneratedPrefabPlacementCount,
                repeatedGeneratedPresentationSignatureCount =
                    parity.repeatedGeneratedPresentationSignatureCount,
                repeatedGeneratedPresentationEntryCount =
                    parity.repeatedGeneratedPresentationEntryCount,
                entityContentArchiveCount = runtime.entityContentArchiveCount,
                entitySceneArchiveBytes = runtime.entitySceneArchiveBytes,
                sharedDependencyGuidCount = runtime.sharedDependencyGuidCount,
                sharedDependencyBytes = runtime.sharedDependencyBytes,
                duplicatedDependencyGuidCount =
                    runtime.duplicatedDependencyGuidCount,
                duplicatedDependencyBytes = runtime.duplicatedDependencyBytes,
                staticRuntimeEntryCount = runtime.staticRuntimeEntryCount,
                managedMapVisualCompanionCount =
                    bake.managedMapVisualCompanionCount,
                productionSettingsMutated = runtime.productionSettingsMutated,
                productionCutover = 0
            };
            error = null;
            return true;
        }

        private static bool TryValidateDenseOwnershipLayout(
            DenseCandidateLayoutEvidence layout,
            string expectedEntitySceneGuid,
            out int authoringSceneEntryCount,
            out int staticPresentationEntryCount,
            out string error)
        {
            authoringSceneEntryCount = 0;
            staticPresentationEntryCount = 0;
            if (!string.Equals(
                    layout.result,
                    "CandidateEntitySceneAddressablesLayoutReady",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(expectedEntitySceneGuid) ||
                !string.Equals(layout.entitySceneGuid, expectedEntitySceneGuid, StringComparison.Ordinal) ||
                layout.entryCount != 5 ||
                layout.entries == null ||
                layout.entries.Count != layout.entryCount ||
                layout.sharedDependencyCount != 0 ||
                layout.staticManifestEntryCount != 0 ||
                layout.presentationChunkEntryCount != 0 ||
                layout.legacyPlacementEntryCount != 0 ||
                layout.productionAddressablesMutated != 0)
            {
                error = "dense-candidate-layout-ownership";
                return false;
            }

            var expectedEntries = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["definition"] =
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath,
                ["source-scene"] =
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath,
                ["entity-scene"] = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                ["map-surface"] = OperationMapAddressablesLayoutBuilder.MapSurfacePath,
                ["minimap-raster"] = OperationMapAddressablesLayoutBuilder.MinimapRasterPath
            };
            var observedRoles = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < layout.entries.Count; i++)
            {
                DenseCandidateLayoutEntryEvidence entry = layout.entries[i];
                if (entry == null ||
                    !expectedEntries.TryGetValue(entry.role ?? string.Empty, out string expectedPath) ||
                    !string.Equals(entry.assetPath, expectedPath, StringComparison.Ordinal) ||
                    !observedRoles.Add(entry.role))
                {
                    error = "dense-candidate-layout-entry";
                    return false;
                }

                if (string.Equals(
                        entry.assetPath,
                        DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        entry.assetPath,
                        OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        entry.assetPath,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        entry.assetPath,
                        OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                        StringComparison.Ordinal))
                {
                    authoringSceneEntryCount++;
                }
                if (string.Equals(
                        entry.assetPath,
                        StaticMapPresentationBaker.ManifestPath,
                        StringComparison.Ordinal) ||
                    entry.assetPath.StartsWith(
                        StaticMapPresentationBaker.SceneOutputFolder + "/",
                        StringComparison.Ordinal))
                {
                    staticPresentationEntryCount++;
                }
            }

            if (observedRoles.Count != expectedEntries.Count ||
                authoringSceneEntryCount != 0 ||
                staticPresentationEntryCount != 0)
            {
                error = "dense-authoring-or-static-entry-present";
                return false;
            }

            error = null;
            return true;
        }

        private static CandidateGeometryEvidence CaptureCandidateGeometryEvidence()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene acceptedOperationMap = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    OpenSceneMode.Additive);
                Scene acceptedSubScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                    OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                    OpenSceneMode.Additive);
                int acceptedSourceRendererCount =
                    CountRenderers(acceptedOperationMap) + CountRenderers(acceptedSubScene);
                var renderers = new List<Renderer>();
                GameObject[] roots = candidateScene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                    renderers.AddRange(roots[rootIndex].GetComponentsInChildren<Renderer>(true));
                CandidateGeometryEvidence evidence = BuildGeometryEvidence(renderers);
                evidence.acceptedSourceAuthoredRendererCount = acceptedSourceRendererCount;
                return evidence;
            }
            finally
            {
                if (OperationMapEntitySceneCandidateBakeAll.HasRestorableSceneSetup(previousSetup))
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static int CountRenderers(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                count += roots[rootIndex].GetComponentsInChildren<Renderer>(true).Length;
            return count;
        }

        internal static CandidateGeometryEvidence BuildGeometryEvidence(IReadOnlyList<Renderer> renderers)
        {
            var meshTriangles = new Dictionary<string, long>(StringComparer.Ordinal);
            var materialKeys = new HashSet<string>(StringComparer.Ordinal);
            var textureKeys = new HashSet<string>(StringComparer.Ordinal);
            var inspectedMaterials = new HashSet<string>(StringComparer.Ordinal);
            int active = 0;
            int shadows = 0;
            int batchingEligible = 0;
            int missing = 0;
            int nonFiniteBounds = 0;
            long instancedTriangles = 0;
            Bounds aggregateBounds = default;
            bool hasBounds = false;

            for (int index = 0; index < renderers.Count; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                {
                    missing++;
                    continue;
                }

                bool isActive = renderer.enabled && renderer.gameObject.activeInHierarchy;
                if (isActive)
                {
                    active++;
                    if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                        shadows++;
                    Bounds bounds = renderer.bounds;
                    if (!IsFinite(bounds.center) || !IsFinite(bounds.size))
                    {
                        nonFiniteBounds++;
                    }
                    else
                    {
                        if (!hasBounds)
                        {
                            aggregateBounds = bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            aggregateBounds.Encapsulate(bounds);
                        }
                    }
                }

                Mesh mesh = ResolveMesh(renderer);
                bool rendererBatchingEligible = TryGetPersistentAssetKey(mesh, out string meshKey);
                if (!rendererBatchingEligible)
                {
                    missing++;
                }
                else
                {
                    if (!meshTriangles.TryGetValue(meshKey, out long triangleCount))
                    {
                        triangleCount = GetTriangleCount(mesh);
                        meshTriangles.Add(meshKey, triangleCount);
                    }
                    instancedTriangles += triangleCount;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                    rendererBatchingEligible = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (!TryGetPersistentAssetKey(material, out string materialKey))
                    {
                        missing++;
                        rendererBatchingEligible = false;
                        continue;
                    }

                    materialKeys.Add(materialKey);
                    if (!inspectedMaterials.Add(materialKey))
                        continue;
                    string[] textureProperties = material.GetTexturePropertyNames();
                    for (int propertyIndex = 0; propertyIndex < textureProperties.Length; propertyIndex++)
                    {
                        Texture texture = material.GetTexture(textureProperties[propertyIndex]);
                        if (texture == null)
                            continue;
                        if (!TryGetPersistentAssetKey(texture, out string textureKey))
                        {
                            missing++;
                            rendererBatchingEligible = false;
                            continue;
                        }
                        textureKeys.Add(textureKey);
                    }
                }

                if (renderer.HasPropertyBlock())
                    rendererBatchingEligible = false;
                if (rendererBatchingEligible)
                    batchingEligible++;
            }

            long uniqueTriangles = 0;
            foreach (long triangleCount in meshTriangles.Values)
                uniqueTriangles += triangleCount;
            Vector3 boundsSize = hasBounds ? aggregateBounds.size : Vector3.zero;
            float squareKilometers = boundsSize.x * boundsSize.z / 1_000_000f;
            float density = squareKilometers > 0f ? active / squareKilometers : 0f;
            bool passed = renderers.Count > 0 && active > 0 && meshTriangles.Count > 0 &&
                          materialKeys.Count > 0 && textureKeys.Count > 0 &&
                          uniqueTriangles > 0 && instancedTriangles >= uniqueTriangles &&
                          batchingEligible > 0 && missing == 0 && nonFiniteBounds == 0 &&
                          hasBounds && IsFinitePositive(density);

            return new CandidateGeometryEvidence
            {
                result = passed ? "CandidateGeometryEvidencePassed" : "CandidateGeometryEvidenceRejected",
                authoredRendererCount = renderers.Count,
                activeRendererCount = active,
                uniqueMeshAssetCount = meshTriangles.Count,
                uniqueMaterialAssetCount = materialKeys.Count,
                uniqueTextureAssetCount = textureKeys.Count,
                uniqueTriangleCount = uniqueTriangles,
                instancedTriangleCount = instancedTriangles,
                shadowCasterCount = shadows,
                batchingEligibleRendererCount = batchingEligible,
                missingAssetReferenceCount = missing,
                nonFiniteBoundsCount = nonFiniteBounds,
                worldBoundsCenter = hasBounds ? aggregateBounds.center : Vector3.zero,
                worldBoundsSize = boundsSize,
                rendererDensityPerSquareKilometer = density
            };
        }

        private static Mesh ResolveMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static long GetTriangleCount(Mesh mesh)
        {
            long indices = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indices += (long)mesh.GetIndexCount(subMesh);
            return indices / 3L;
        }

        private static bool TryGetPersistentAssetKey(UnityEngine.Object asset, out string key)
        {
            key = string.Empty;
            if (asset == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId) ||
                string.IsNullOrWhiteSpace(guid))
                return false;
            key = guid + ":" + localId;
            return true;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static bool IsFinitePositive(float value) => float.IsFinite(value) && value > 0f;

        internal static string ToDeterministicJson(PresentationBudgetReport report) =>
            JsonUtility.ToJson(report, true) + "\n";

        private static void WriteReport(string projectRoot, PresentationBudgetReport report)
        {
            string physicalPath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath) ?? projectRoot);
            File.WriteAllText(physicalPath, ToDeterministicJson(report), Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WriteDenseOwnershipReport(
            string projectRoot,
            DenseEntitySceneOwnershipReport report)
        {
            string physicalPath = Path.Combine(projectRoot, DenseOwnershipReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath) ?? projectRoot);
            File.WriteAllText(
                physicalPath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);
            AssetDatabase.ImportAsset(
                DenseOwnershipReportPath,
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WriteJsonReport<T>(
            string projectRoot,
            string assetPath,
            T report)
        {
            string physicalPath = Path.Combine(projectRoot, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath) ?? projectRoot);
            File.WriteAllText(
                physicalPath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
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
            public int gameplayBuildingIdentityCount;
            public int gameplayVehicleIdentityCount;
            public int renderOnlyIdentityCount;
            public int unknownRoleIdentityCount;
            public int totalEntityCount;
            public int entityArchetypeCount;
            public int entityChunkCount;
            public int renderMeshEntityCount;
            public int renderChildEntityCount;
            public int sharedRenderMeshArrayIdentityCount;
            public int sharedMeshAssetIdentityCount;
            public int sharedMaterialAssetIdentityCount;
            public int buildingRenderChildCount;
            public int intactVisualRootCount;
            public int destroyedVisualRootCount;
            public int missingIntactVisualRootCount;
            public int missingDestroyedVisualRootCount;
            public int sharedIntactDestroyedVisualRootCount;
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
        internal sealed class CandidateGeometryEvidence
        {
            public string result;
            public int acceptedSourceAuthoredRendererCount;
            public int authoredRendererCount;
            public int activeRendererCount;
            public int uniqueMeshAssetCount;
            public int uniqueMaterialAssetCount;
            public int uniqueTextureAssetCount;
            public long uniqueTriangleCount;
            public long instancedTriangleCount;
            public int shadowCasterCount;
            public int batchingEligibleRendererCount;
            public int missingAssetReferenceCount;
            public int nonFiniteBoundsCount;
            public Vector3 worldBoundsCenter;
            public Vector3 worldBoundsSize;
            public float rendererDensityPerSquareKilometer;
        }

        [Serializable]
        internal sealed class DenseCandidateBakeEvidence
        {
            public string result;
            public int authoringDenseIdentityCount;
            public int authoringDenseGameplayBuildingIdentityCount;
            public int authoringDenseRenderOnlyIdentityCount;
            public int legacyPresentationIdentityCount;
            public int denseIdentityCount;
            public int denseGameplayBuildingIdentityCount;
            public int denseRenderOnlyIdentityCount;
            public int denseUnknownRoleIdentityCount;
            public int duplicateDenseIdentityCount;
            public int renderMeshEntityCount;
            public int sharedRenderMeshArrayIdentityCount;
            public int sharedMeshAssetIdentityCount;
            public int sharedMaterialAssetIdentityCount;
            public int missingIntactVisualRootCount;
            public int sharedIntactDestroyedVisualRootCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
        }

        [Serializable]
        internal sealed class DenseTransformParityEvidence
        {
            public string result;
            public int candidateIdentityCount;
            public int uniqueCandidateIdentityCount;
            public int bakedIdentityCount;
            public int uniqueBakedIdentityCount;
            public int generatedCandidateRendererEntityCount;
            public int generatedBakedRenderEntityCount;
            public int persistentGeneratedSourceFailureCount;
            public int repeatedGeneratedPrefabSourceCount;
            public int repeatedGeneratedPrefabPlacementCount;
            public int repeatedGeneratedPresentationSignatureCount;
            public int repeatedGeneratedPresentationEntryCount;
            public int unresolvedGeneratedRendererEntityCount;
            public int unresolvedGeneratedMeshCount;
            public int unresolvedGeneratedMaterialCount;
            public int generatedMeshMismatchCount;
            public int generatedMaterialMismatchCount;
            public int generatedManagedInstanceComponentCount;
            public int repeatedSignatureAssetPairMismatchCount;
            public int unconsumedCandidateRendererEntityCount;
            public int rejectedRowCount;
            public int duplicateCandidateStableIdCount;
            public int duplicateBakedStableIdCount;
            public int missingBakedStableIdCount;
            public int unexpectedBakedStableIdCount;
            public string candidateIdentitySetSha256;
            public string bakedIdentitySetSha256;
        }

        [Serializable]
        internal sealed class DenseRuntimeContentEvidence
        {
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string entitySceneGuid;
            public int staticRuntimeEntryCount;
            public int packedDependencyMetricsComplete;
            public int sharedDependencyGuidCount;
            public long sharedDependencyBytes;
            public int duplicatedDependencyGuidCount;
            public long duplicatedDependencyBytes;
            public int entityContentArchiveCount;
            public long entitySceneArchiveBytes;
            public int productionSettingsMutated;
            public int productionCutover;
        }

        [Serializable]
        internal sealed class DenseCandidateLayoutEvidence
        {
            public string result;
            public string entitySceneGuid;
            public int entryCount;
            public int sharedDependencyCount;
            public int staticManifestEntryCount;
            public int presentationChunkEntryCount;
            public int legacyPlacementEntryCount;
            public int productionAddressablesMutated;
            public List<DenseCandidateLayoutEntryEvidence> entries;
        }

        [Serializable]
        internal sealed class DenseCandidateLayoutEntryEvidence
        {
            public string role;
            public string assetPath;
        }

        [Serializable]
        internal sealed class DenseEntitySceneOwnershipReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string entitySceneGuid;
            public int existingVisualIdentityCount;
            public int existingBakedRenderEntityCount;
            public int generatedVisualIdentityCount;
            public int generatedCandidateRendererEntityCount;
            public int generatedBakedRenderEntityCount;
            public int missingGeneratedVisualIdentityCount;
            public int unexpectedGeneratedVisualIdentityCount;
            public int managedMapVisualCompanionCount;
            public int authoringSceneEntryCount;
            public int explicitSharedDependencyEntryCount;
            public int staticPresentationEntryCount;
            public int legacyPlacementEntryCount;
            public int productionAddressablesMutated;
            public int productionCutover;
        }

        [Serializable]
        internal sealed class DensePackedAssetSharingReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string entitySceneGuid;
            public int renderEntityCount;
            public int sharedRenderMeshArrayIdentityCount;
            public int sharedMeshAssetIdentityCount;
            public int sharedMaterialAssetIdentityCount;
            public int repeatedGeneratedPrefabSourceCount;
            public int repeatedGeneratedPrefabPlacementCount;
            public int repeatedGeneratedPresentationSignatureCount;
            public int repeatedGeneratedPresentationEntryCount;
            public int entityContentArchiveCount;
            public long entitySceneArchiveBytes;
            public int sharedDependencyGuidCount;
            public long sharedDependencyBytes;
            public int duplicatedDependencyGuidCount;
            public long duplicatedDependencyBytes;
            public int staticRuntimeEntryCount;
            public int managedMapVisualCompanionCount;
            public int productionSettingsMutated;
            public int productionCutover;
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
            public int gameplayBuildingIdentityCount;
            public int gameplayVehicleIdentityCount;
            public int renderOnlyIdentityCount;
            public int totalEntityCount;
            public int entityArchetypeCount;
            public int entityChunkCount;
            public int renderEntityCount;
            public int renderChildEntityCount;
            public int sharedRenderMeshArrayIdentityCount;
            public int sharedMeshAssetIdentityCount;
            public int sharedMaterialAssetIdentityCount;
            public int buildingRenderChildCount;
            public int intactVisualRootCount;
            public int destroyedVisualRootCount;
            public int missingIntactVisualRootCount;
            public int missingDestroyedVisualRootCount;
            public int sharedIntactDestroyedVisualRootCount;
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
            public int acceptedSourceAuthoredRendererCount;
            public int candidateAuthoringRendererCount;
            public int candidateActiveRendererCount;
            public int candidateUniqueMeshAssetCount;
            public int candidateUniqueMaterialAssetCount;
            public int uniqueTextureAssetCount;
            public long uniqueTriangleCount;
            public long instancedTriangleCount;
            public int shadowCasterCount;
            public int batchingEligibleRendererCount;
            public int missingGeometryAssetReferenceCount;
            public int nonFiniteRendererBoundsCount;
            public Vector3 worldBoundsCenter;
            public Vector3 worldBoundsSize;
            public float rendererDensityPerSquareKilometer;
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
