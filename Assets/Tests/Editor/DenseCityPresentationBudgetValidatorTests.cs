using System;
using System.IO;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityPresentationBudgetValidatorTests
{
    public static void RunFocusedValidation()
    {
        var suite = new DenseCityPresentationBudgetValidatorTests();
        Action[] tests =
        {
            suite.Budget_AcceptsCompleteCoreEvidence,
            suite.Budget_RejectsLegacyExplicitSharedLayout,
            suite.Budget_RejectsTransformBoundsMismatch,
            suite.Budget_RejectsIncompleteGeometryEvidence,
            suite.Budget_RejectsIncompleteMobileMaterialLightingEvidence,
            suite.Budget_RejectsIncompleteBakedOwnershipEvidence,
            suite.DenseOwnership_AcceptsCompleteEntitySceneEvidence,
            suite.DenseOwnership_RejectsMissingGeneratedIdentity,
            suite.DenseOwnership_RejectsAuthoringSceneEntry,
            suite.PackedAssetSharing_AcceptsCompleteEvidence,
            suite.PackedAssetSharing_RejectsDuplicatedDependency,
            suite.PackedAssetSharing_RejectsMultipleEntityArchives,
            suite.PackedAssetSharing_RejectsInvalidVirtualizationDatabase,
            suite.PackedAssetSharing_CurrentEvidenceWritesAcceptedReport,
            suite.CategorySharing_AcceptsEveryRequiredSemanticFamily,
            suite.CategorySharing_RejectsMissingRequiredFamily,
            suite.CategorySharing_RejectsBakedAssetPairDrift,
            suite.MeshCombinationPolicy_AcceptsBoundedBuildingMeshesOnly,
            suite.MeshCombinationPolicy_RejectsInfrastructureCombinationWithoutEvidence,
            suite.MeshCombinationPolicy_RejectsWholeCityMesh,
            suite.BudgetFailure_RestoresAcceptedCandidateOutput,
            suite.Budget_SerializationIsDeterministicAndMarksPackedMetricsPending,
            suite.CandidateBakeAll_OrdersBudgetAfterBakeAndBeforePostflight,
            suite.CurrentMapBaker_BakesStaticPresentationOnlyForStaticDefinitions
        };
        for (int i = 0; i < tests.Length; i++)
            tests[i]();
        Debug.Log($"[DenseCityPresentationBudgetValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void Budget_AcceptsCompleteCoreEvidence()
    {
        Assert.That(TryCreateValidReport(out _, out string error), Is.True, error);
    }

    [Test]
    public void Budget_RejectsLegacyExplicitSharedLayout()
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
        layout.sharedDependencyCount = 1;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateReport(
                bake, art, parity, layout, geometry, out _, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("candidate-layout-budget"));
    }

    [Test]
    public void Budget_RejectsTransformBoundsMismatch()
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
        parity.bakedRenderEntityCount--;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateReport(
                bake, art, parity, layout, geometry, out _, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("transform-bounds-parity-budget"));
    }

    [Test]
    public void Budget_RejectsIncompleteGeometryEvidence()
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
        geometry.uniqueTextureAssetCount = 0;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateReport(
                bake, art, parity, layout, geometry, out _, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("candidate-geometry-budget"));
    }

    [Test]
    public void Budget_RejectsIncompleteMobileMaterialLightingEvidence()
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
        geometry.mobileMaterialLightingEvidenceComplete = 0;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateReport(
                bake, art, parity, layout, geometry, out _, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("candidate-geometry-budget"));
    }

    [Test]
    public void Budget_RejectsIncompleteBakedOwnershipEvidence()
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
        bake.missingIntactVisualRootCount = 1;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateReport(
                bake, art, parity, layout, geometry, out _, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("candidate-bake-budget"));
    }

    [Test]
    public void DenseOwnership_AcceptsCompleteEntitySceneEvidence()
    {
        CreateDenseOwnershipEvidence(
            out var bake,
            out var existingParity,
            out var parity,
            out var layout);

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDenseOwnershipReport(
                bake,
                existingParity,
                parity,
                layout,
                layout.entitySceneGuid,
                out var report,
                out string error),
            Is.True,
            error);
        Assert.That(report.existingVisualIdentityCount, Is.EqualTo(9544));
        Assert.That(report.generatedVisualIdentityCount, Is.EqualTo(36946));
        Assert.That(report.authoringSceneEntryCount, Is.Zero);
        Assert.That(report.staticPresentationEntryCount, Is.Zero);
    }

    [Test]
    public void DenseOwnership_RejectsMissingGeneratedIdentity()
    {
        CreateDenseOwnershipEvidence(
            out var bake,
            out var existingParity,
            out var parity,
            out var layout);
        parity.missingBakedStableIdCount = 1;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDenseOwnershipReport(
                bake,
                existingParity,
                parity,
                layout,
                layout.entitySceneGuid,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("dense-generated-renderer-parity"));
    }

    [Test]
    public void DenseOwnership_RejectsAuthoringSceneEntry()
    {
        CreateDenseOwnershipEvidence(
            out var bake,
            out var existingParity,
            out var parity,
            out var layout);
        layout.entries[1].assetPath =
            DenseCityCandidateAuthoringTransaction.CandidateMapScenePath;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDenseOwnershipReport(
                bake,
                existingParity,
                parity,
                layout,
                layout.entitySceneGuid,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("dense-candidate-layout-entry"));
    }

    [Test]
    public void PackedAssetSharing_AcceptsCompleteEvidence()
    {
        CreateDensePackedAssetSharingEvidence(
            out var bake,
            out var parity,
            out var runtime,
            out var virtualization);

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDensePackedAssetSharingReport(
                bake,
                parity,
                runtime,
                virtualization,
                out var report,
                out string error),
            Is.True,
            error);
        Assert.That(report.sharedRenderMeshArrayIdentityCount, Is.EqualTo(1));
        Assert.That(report.entityContentArchiveCount, Is.EqualTo(1));
        Assert.That(report.duplicatedDependencyBytes, Is.Zero);
        Assert.That(report.renderVirtualizationMetricsComplete, Is.Zero);
        Assert.That(report.renderVirtualizationSlotCapacity, Is.EqualTo(704));
        Assert.That(report.packedSourceRowsRemoved, Is.EqualTo(-1));
    }

    [Test]
    public void PackedAssetSharing_RejectsDuplicatedDependency()
    {
        CreateDensePackedAssetSharingEvidence(
            out var bake,
            out var parity,
            out var runtime,
            out var virtualization);
        runtime.duplicatedDependencyGuidCount = 1;
        runtime.duplicatedDependencyBytes = 4096;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDensePackedAssetSharingReport(
                bake,
                parity,
                runtime,
                virtualization,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("packed-asset-sharing-runtime"));
    }

    [Test]
    public void PackedAssetSharing_RejectsMultipleEntityArchives()
    {
        CreateDensePackedAssetSharingEvidence(
            out var bake,
            out var parity,
            out var runtime,
            out var virtualization);
        runtime.entityContentArchiveCount = 2;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDensePackedAssetSharingReport(
                bake,
                parity,
                runtime,
                virtualization,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("packed-asset-sharing-runtime"));
    }

    [Test]
    public void PackedAssetSharing_RejectsInvalidVirtualizationDatabase()
    {
        CreateDensePackedAssetSharingEvidence(
            out var bake,
            out var parity,
            out var runtime,
            out var virtualization);
        virtualization.sourceRowsRemoved = 1;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateDensePackedAssetSharingReport(
                bake,
                parity,
                runtime,
                virtualization,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("packed-asset-sharing-virtualization-database"));
    }

    [Test]
    public void PackedAssetSharing_CurrentEvidenceWritesAcceptedReport()
    {
        DenseCityPresentationBudgetValidator.ValidateDenseCityPackedAssetSharing();

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string reportPath = Path.Combine(
            projectRoot,
            DenseCityPresentationBudgetValidator.DensePackedAssetSharingReportPath);
        Assert.That(File.Exists(reportPath), Is.True);
        var report = JsonUtility.FromJson<
            DenseCityPresentationBudgetValidator.DensePackedAssetSharingReport>(
            File.ReadAllText(reportPath));
        Assert.That(report, Is.Not.Null);
        Assert.That(report.result, Is.EqualTo("DenseCityPackedAssetSharingPassed"));
        Assert.That(report.renderEntityCount, Is.EqualTo(82797));
        Assert.That(report.entityContentArchiveCount, Is.EqualTo(1));
        Assert.That(report.duplicatedDependencyGuidCount, Is.Zero);
        Assert.That(report.duplicatedDependencyBytes, Is.Zero);
        Assert.That(report.schemaVersion, Is.EqualTo(2));
        Assert.That(report.renderVirtualizationMetricsComplete, Is.Zero);
        Assert.That(report.packedVirtualizedSourceRowCount, Is.EqualTo(-1));
    }

    [Test]
    public void CategorySharing_AcceptsEveryRequiredSemanticFamily()
    {
        var reports = CreateRequiredCategorySharingReports();

        Assert.That(
            OperationMapDenseCityGeneratedTransformParityValidator
                .TryValidateRequiredCategorySharing(reports, out string error),
            Is.True,
            error);
    }

    [Test]
    public void CategorySharing_RejectsMissingRequiredFamily()
    {
        var reports = CreateRequiredCategorySharingReports();
        reports.RemoveAt(reports.Count - 1);

        Assert.That(
            OperationMapDenseCityGeneratedTransformParityValidator
                .TryValidateRequiredCategorySharing(reports, out string error),
            Is.False);
        StringAssert.StartsWith("category-sharing-incomplete:", error);
    }

    [Test]
    public void CategorySharing_RejectsBakedAssetPairDrift()
    {
        var reports = CreateRequiredCategorySharingReports();
        reports[1].repeatedAssetPairMismatchCount = 1;

        Assert.That(
            OperationMapDenseCityGeneratedTransformParityValidator
                .TryValidateRequiredCategorySharing(reports, out string error),
            Is.False);
        Assert.That(
            error,
            Is.EqualTo(
                $"category-sharing-incomplete:" +
                $"{(byte)DenseCityPresentationSemanticCategory.Infrastructure}"));
    }

    [Test]
    public void MeshCombinationPolicy_AcceptsBoundedBuildingMeshesOnly()
    {
        var report = CreateMeshCombinationPolicyReport();

        Assert.That(
            OperationMapDenseCityGeneratedTransformParityValidator
                .TryValidateMeshCombinationPolicy(report, out string error),
            Is.True,
            error);
    }

    [Test]
    public void MeshCombinationPolicy_RejectsInfrastructureCombinationWithoutEvidence()
    {
        var report = CreateMeshCombinationPolicyReport();
        report.infrastructureMeshBackedRendererEntryCount = 1;
        report.unauthorizedMeshBackedRendererEntryCount = 1;

        Assert.That(
            OperationMapDenseCityGeneratedTransformParityValidator
                .TryValidateMeshCombinationPolicy(report, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("mesh-combination-policy-rejected"));
    }

    [Test]
    public void MeshCombinationPolicy_RejectsWholeCityMesh()
    {
        var report = CreateMeshCombinationPolicyReport();
        report.maximumMeshBackedSpanRatioX =
            OperationMapDenseCityGeneratedTransformParityValidator.WholeCityMeshSpanRatio;
        report.maximumMeshBackedSpanRatioZ =
            OperationMapDenseCityGeneratedTransformParityValidator.WholeCityMeshSpanRatio;
        report.wholeCityMeshBackedRendererEntryCount = 1;

        Assert.That(
            OperationMapDenseCityGeneratedTransformParityValidator
                .TryValidateMeshCombinationPolicy(report, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("mesh-combination-policy-rejected"));
    }

    [Test]
    public void BudgetFailure_RestoresAcceptedCandidateOutput()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);
        const string outputDirectory =
            "Temp/DenseCityPresentationBudgetValidatorTests/BudgetRollback";
        const string acceptedRelative = outputDirectory + "/accepted-output.bin";
        const string partialRelative = outputDirectory + "/partial-output.bin";
        string acceptedPath = Path.Combine(projectRoot, acceptedRelative);
        string partialPath = Path.Combine(projectRoot, partialRelative);
        Directory.CreateDirectory(Path.GetDirectoryName(acceptedPath));
        byte[] acceptedBytes = { 1, 4, 9, 16 };
        File.WriteAllBytes(acceptedPath, acceptedBytes);

        try
        {
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction transaction =
                OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                    projectRoot,
                    new[] { acceptedRelative, partialRelative });
            File.WriteAllBytes(acceptedPath, new byte[] { 2, 3, 5, 7 });
            File.WriteAllBytes(partialPath, new byte[] { 11, 13 });
            CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
            geometry.uniqueTextureAssetCount = 0;

            Assert.That(
                DenseCityPresentationBudgetValidator.TryCreateReport(
                    bake, art, parity, layout, geometry, out _, out string error),
                Is.False);
            Assert.That(error, Is.EqualTo("candidate-geometry-budget"));

            transaction.Rollback();

            Assert.That(File.ReadAllBytes(acceptedPath), Is.EqualTo(acceptedBytes));
            Assert.That(File.Exists(partialPath), Is.False);
        }
        finally
        {
            string physicalDirectory = Path.Combine(projectRoot, outputDirectory);
            if (Directory.Exists(physicalDirectory))
                Directory.Delete(physicalDirectory, true);
        }
    }

    [Test]
    public void Budget_SerializationIsDeterministicAndMarksPackedMetricsPending()
    {
        Assert.That(TryCreateValidReport(out var report, out string error), Is.True, error);
        string first = DenseCityPresentationBudgetValidator.ToDeterministicJson(report);
        string second = DenseCityPresentationBudgetValidator.ToDeterministicJson(report);

        Assert.That(second, Is.EqualTo(first));
        StringAssert.Contains("\"schemaVersion\": 2", first);
        StringAssert.Contains("\"packedContentMetricsComplete\": 0", first);
        StringAssert.Contains("\"entitySceneBytes\": -1", first);
        StringAssert.Contains("\"productionCutover\": 0", first);
        StringAssert.Contains("\"instancedTriangleCount\": 2000", first);
        StringAssert.Contains("\"uniqueTextureAssetCount\": 12", first);
        StringAssert.Contains("\"transparentMaterialCount\": 2", first);
        StringAssert.Contains("\"alphaClippedMaterialCount\": 3", first);
        StringAssert.Contains("\"smallDetailMaximumExtentMeters\": 1.0", first);
        StringAssert.Contains("\"mobileMaterialLightingEvidenceComplete\": 1", first);
    }

    [Test]
    public void CandidateBakeAll_OrdersBudgetAfterBakeAndBeforePostflight()
    {
        const string path =
            "Assets/Game/Scripts/Editor/OperationMapEntitySceneCandidateBakeAll.cs";
        string source = System.IO.File.ReadAllText(path);
        int invalidation = source.IndexOf(
            "DenseCityPresentationBudgetValidator.InvalidateEvidence",
            StringComparison.Ordinal);
        int entityBake = source.IndexOf("\"candidate-entity-bake\"", StringComparison.Ordinal);
        int budget = source.IndexOf("\"presentation-budget\"", StringComparison.Ordinal);
        int postflight = source.IndexOf("\"postflight-isolation\"", StringComparison.Ordinal);

        Assert.That(invalidation, Is.GreaterThanOrEqualTo(0));
        Assert.That(entityBake, Is.GreaterThan(invalidation));
        Assert.That(budget, Is.GreaterThan(entityBake));
        Assert.That(postflight, Is.GreaterThan(budget));
        Assert.That(
            Count(source, "DenseCityPresentationBudgetValidator.InvalidateEvidence"),
            Is.EqualTo(2));
    }

    [Test]
    public void CurrentMapBaker_BakesStaticPresentationOnlyForStaticDefinitions()
    {
        Assert.That(
            OperationMapCurrentMapBaker.ShouldBakeStaticPresentation(
                Game.Configs.OperationMapPresentationKind.StaticSceneChunks),
            Is.True);
        Assert.That(
            OperationMapCurrentMapBaker.ShouldBakeStaticPresentation(
                Game.Configs.OperationMapPresentationKind.EntityScene),
            Is.False);
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapCurrentMapBaker.ShouldBakeStaticPresentation(
                (Game.Configs.OperationMapPresentationKind)byte.MaxValue));
    }

    private static bool TryCreateValidReport(
        out DenseCityPresentationBudgetValidator.PresentationBudgetReport report,
        out string error)
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout, out var geometry);
        return DenseCityPresentationBudgetValidator.TryCreateReport(
            bake, art, parity, layout, geometry, out report, out error);
    }

    private static void CreateEvidence(
        out DenseCityPresentationBudgetValidator.CandidateBakeEvidence bake,
        out DenseCityPresentationBudgetValidator.SharedArtEvidence art,
        out DenseCityPresentationBudgetValidator.TransformParityEvidence parity,
        out DenseCityPresentationBudgetValidator.CandidateLayoutEvidence layout,
        out DenseCityPresentationBudgetValidator.CandidateGeometryEvidence geometry)
    {
        bake = new DenseCityPresentationBudgetValidator.CandidateBakeEvidence
        {
            result = "CandidateBakeValidationPassed",
            gameplayBuildingCount = 432,
            gameplayVehicleCount = 22,
            presentationRootCount = 3,
            presentationIdentityCount = 9544,
            gameplayBuildingIdentityCount = 432,
            gameplayVehicleIdentityCount = 22,
            renderOnlyIdentityCount = 9090,
            unknownRoleIdentityCount = 0,
            totalEntityCount = 16000,
            entityArchetypeCount = 80,
            entityChunkCount = 300,
            renderMeshEntityCount = 14249,
            renderChildEntityCount = 10000,
            sharedRenderMeshArrayIdentityCount = 600,
            sharedMeshAssetIdentityCount = 700,
            sharedMaterialAssetIdentityCount = 40,
            buildingRenderChildCount = 698,
            intactVisualRootCount = 432,
            destroyedVisualRootCount = 266,
            missingIntactVisualRootCount = 0,
            missingDestroyedVisualRootCount = 0,
            sharedIntactDestroyedVisualRootCount = 0,
            nonFiniteTransformCount = 0,
            managedMapVisualCompanionCount = 0
        };
        art = new DenseCityPresentationBudgetValidator.SharedArtEvidence
        {
            result = "SharedArtOwnershipProven",
            sourceCount = 11892,
            uniqueMeshAssetCount = 670,
            uniqueMaterialAssetCount = 39,
            uniquePrefabAssetCount = 671,
            meshPlacementReferenceCount = 11892,
            materialReferenceCount = 11989,
            repeatedMeshAssetCount = 621,
            repeatedMaterialAssetCount = 32,
            missingAssetCount = 0,
            compactInstanceDataProven = true
        };
        parity = new DenseCityPresentationBudgetValidator.TransformParityEvidence
        {
            checkpoint = "ecs-bake",
            result = "SourceCandidateBakedParityPassed",
            candidateIdentityCount = 9544,
            bakedIdentityCount = 9544,
            rejectedRowCount = 0,
            bakedRenderEntityCount = 14249
        };
        layout = new DenseCityPresentationBudgetValidator.CandidateLayoutEvidence
        {
            result = "CandidateEntitySceneAddressablesLayoutReady",
            entitySceneGuid = "0f9ecd54a7f0f467fa35556af7d28f1d",
            entryCount = 5,
            sharedDependencyCount = 0,
            staticManifestEntryCount = 0,
            presentationChunkEntryCount = 0,
            legacyPlacementEntryCount = 0,
            productionAddressablesMutated = 0
        };
        geometry = new DenseCityPresentationBudgetValidator.CandidateGeometryEvidence
        {
            result = "CandidateGeometryEvidencePassed",
            acceptedSourceAuthoredRendererCount = 90,
            authoredRendererCount = 100,
            activeRendererCount = 90,
            uniqueMeshAssetCount = 10,
            uniqueMaterialAssetCount = 8,
            uniqueTextureAssetCount = 12,
            uniqueTriangleCount = 500,
            instancedTriangleCount = 2000,
            shadowCasterCount = 40,
            shadowReceiverCount = 35,
            transparentMaterialCount = 2,
            alphaClippedMaterialCount = 3,
            smallDetailRendererCount = 12,
            lightmappedRendererCount = 20,
            lightProbeRendererCount = 70,
            mobileMaterialLightingEvidenceComplete = 1,
            batchingEligibleRendererCount = 95,
            missingAssetReferenceCount = 0,
            nonFiniteBoundsCount = 0,
            worldBoundsCenter = new Vector3(10f, 5f, 20f),
            worldBoundsSize = new Vector3(1000f, 100f, 800f),
            rendererDensityPerSquareKilometer = 112.5f
        };
    }

    private static void CreateDenseOwnershipEvidence(
        out DenseCityPresentationBudgetValidator.DenseCandidateBakeEvidence bake,
        out DenseCityPresentationBudgetValidator.TransformParityEvidence existingParity,
        out DenseCityPresentationBudgetValidator.DenseTransformParityEvidence parity,
        out DenseCityPresentationBudgetValidator.DenseCandidateLayoutEvidence layout)
    {
        bake = new DenseCityPresentationBudgetValidator.DenseCandidateBakeEvidence
        {
            result = "DenseCandidateBakeValidationPassed",
            authoringDenseIdentityCount = 36946,
            authoringDenseGameplayBuildingIdentityCount = 4971,
            authoringDenseRenderOnlyIdentityCount = 31975,
            legacyPresentationIdentityCount = 9544,
            denseIdentityCount = 36946,
            denseGameplayBuildingIdentityCount = 4971,
            denseRenderOnlyIdentityCount = 31975,
            denseUnknownRoleIdentityCount = 0,
            duplicateDenseIdentityCount = 0,
            renderMeshEntityCount = 82797,
            sharedRenderMeshArrayIdentityCount = 1,
            sharedMeshAssetIdentityCount = 1769,
            sharedMaterialAssetIdentityCount = 80,
            missingIntactVisualRootCount = 0,
            sharedIntactDestroyedVisualRootCount = 0,
            nonFiniteTransformCount = 0,
            managedMapVisualCompanionCount = 0
        };
        existingParity = new DenseCityPresentationBudgetValidator.TransformParityEvidence
        {
            checkpoint = "ecs-bake",
            result = "SourceCandidateBakedParityPassed",
            candidateIdentityCount = 9544,
            bakedIdentityCount = 9544,
            rejectedRowCount = 0,
            bakedRenderEntityCount = 14249
        };
        parity = new DenseCityPresentationBudgetValidator.DenseTransformParityEvidence
        {
            result = "DenseCityGeneratedTransformParityPassed",
            candidateIdentityCount = 36946,
            uniqueCandidateIdentityCount = 36946,
            bakedIdentityCount = 36946,
            uniqueBakedIdentityCount = 36946,
            generatedCandidateRendererEntityCount = 68735,
            generatedBakedRenderEntityCount = 68735,
            persistentGeneratedSourceFailureCount = 0,
            repeatedGeneratedPrefabSourceCount = 57,
            repeatedGeneratedPrefabPlacementCount = 31973,
            repeatedGeneratedPresentationSignatureCount = 377,
            repeatedGeneratedPresentationEntryCount = 68722,
            unresolvedGeneratedRendererEntityCount = 0,
            unresolvedGeneratedMeshCount = 0,
            unresolvedGeneratedMaterialCount = 0,
            generatedMeshMismatchCount = 0,
            generatedMaterialMismatchCount = 0,
            generatedManagedInstanceComponentCount = 0,
            repeatedSignatureAssetPairMismatchCount = 0,
            unconsumedCandidateRendererEntityCount = 0,
            rejectedRowCount = 0,
            duplicateCandidateStableIdCount = 0,
            duplicateBakedStableIdCount = 0,
            missingBakedStableIdCount = 0,
            unexpectedBakedStableIdCount = 0,
            candidateIdentitySetSha256 = new string('a', 64),
            bakedIdentitySetSha256 = new string('a', 64)
        };
        layout = new DenseCityPresentationBudgetValidator.DenseCandidateLayoutEvidence
        {
            result = "CandidateEntitySceneAddressablesLayoutReady",
            entitySceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0",
            entryCount = 5,
            sharedDependencyCount = 0,
            staticManifestEntryCount = 0,
            presentationChunkEntryCount = 0,
            legacyPlacementEntryCount = 0,
            productionAddressablesMutated = 0,
            entries = new System.Collections.Generic.List<
                DenseCityPresentationBudgetValidator.DenseCandidateLayoutEntryEvidence>
            {
                new()
                {
                    role = "definition",
                    assetPath = OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateDefinitionPath
                },
                new()
                {
                    role = "source-scene",
                    assetPath = OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateRuntimeBindingPath
                },
                new()
                {
                    role = "entity-scene",
                    assetPath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath
                },
                new()
                {
                    role = "map-surface",
                    assetPath = OperationMapAddressablesLayoutBuilder.MapSurfacePath
                },
                new()
                {
                    role = "minimap-raster",
                    assetPath = OperationMapAddressablesLayoutBuilder.MinimapRasterPath
                }
            }
        };
    }

    private static void CreateDensePackedAssetSharingEvidence(
        out DenseCityPresentationBudgetValidator.DenseCandidateBakeEvidence bake,
        out DenseCityPresentationBudgetValidator.DenseTransformParityEvidence parity,
        out DenseCityPresentationBudgetValidator.DenseRuntimeContentEvidence runtime,
        out DenseCityPresentationBudgetValidator.DenseRenderVirtualizationDatabaseEvidence virtualization)
    {
        CreateDenseOwnershipEvidence(
            out bake,
            out _,
            out parity,
            out _);
        runtime = new DenseCityPresentationBudgetValidator.DenseRuntimeContentEvidence
        {
            schemaVersion = 9,
            result = "DenseCityCandidateRuntimeContentBuilt",
            operationMapId = "opmap.skirmish.desert_base_01",
            entitySceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0",
            staticRuntimeEntryCount = 0,
            packedDependencyMetricsComplete = 1,
            sharedDependencyGuidCount = 0,
            sharedDependencyBytes = 0,
            duplicatedDependencyGuidCount = 0,
            duplicatedDependencyBytes = 0,
            entityContentArchiveCount = 1,
            entitySceneArchiveBytes = 137862756,
            productionSettingsMutated = 0,
            productionCutover = 0
        };
        virtualization = new DenseCityPresentationBudgetValidator.DenseRenderVirtualizationDatabaseEvidence
        {
            schemaVersion = 1,
            result = "Passed",
            operationMapId = "opmap.skirmish.desert_base_01",
            contentHash = new string('a', 64),
            prototypeCount = 22,
            partCount = 26,
            placementCount = 9721,
            cellCount = 1635,
            policyBucketCount = 2,
            totalPoolSlotCapacity = 704,
            sourceRenderRowCount = 82797,
            eligibleSourceRowCount = 11299,
            logicalRenderRowCount = 11299,
            residentSourceRowCount = 71498,
            sourceRowsRemoved = 0,
            logicalParityResult = "Passed",
            isolationResult = "Passed"
        };
    }

    private static System.Collections.Generic.List<
        OperationMapDenseCityGeneratedTransformParityValidator
            .DenseCityGeneratedCategorySharingReport>
        CreateRequiredCategorySharingReports()
    {
        return new()
        {
            CreateCategorySharingReport(
                DenseCityPresentationSemanticCategory.GameplayBuildingIntact,
                "building"),
            CreateCategorySharingReport(
                DenseCityPresentationSemanticCategory.Infrastructure,
                "road-module,bridge-module,infrastructure"),
            CreateCategorySharingReport(
                DenseCityPresentationSemanticCategory.Vegetation,
                "tree,vegetation"),
            CreateCategorySharingReport(
                DenseCityPresentationSemanticCategory.Prop,
                "prop")
        };
    }

    private static OperationMapDenseCityGeneratedTransformParityValidator
        .DenseCityGeneratedCategorySharingReport CreateCategorySharingReport(
            DenseCityPresentationSemanticCategory category,
            string coveredFamilies)
    {
        return new OperationMapDenseCityGeneratedTransformParityValidator
            .DenseCityGeneratedCategorySharingReport
        {
            category = category.ToString(),
            categoryValue = (byte)category,
            coveredFamilies = coveredFamilies,
            rendererEntryCount = 20,
            presentationSignatureCount = 4,
            repeatedPresentationSignatureCount = 3,
            repeatedPresentationEntryCount = 19,
            repeatedAssetPairMismatchCount = 0,
            prefabBackedRendererEntryCount =
                category == DenseCityPresentationSemanticCategory.GameplayBuildingIntact ? 0 : 20,
            meshBackedRendererEntryCount =
                category == DenseCityPresentationSemanticCategory.GameplayBuildingIntact ? 20 : 0
        };
    }

    private static OperationMapDenseCityGeneratedTransformParityValidator
        .DenseCityGeneratedMeshCombinationPolicyReport CreateMeshCombinationPolicyReport()
    {
        return new OperationMapDenseCityGeneratedTransformParityValidator
            .DenseCityGeneratedMeshCombinationPolicyReport
        {
            wholeCitySpanRatio =
                OperationMapDenseCityGeneratedTransformParityValidator.WholeCityMeshSpanRatio,
            aggregateSpanX = 5000f,
            aggregateSpanZ = 4000f,
            maximumMeshBackedSpanRatioX = 0.05f,
            maximumMeshBackedSpanRatioZ = 0.04f,
            meshBackedRendererEntryCount = 20,
            authorizedBuildingMeshBackedRendererEntryCount = 20,
            infrastructureMeshBackedRendererEntryCount = 0,
            unauthorizedMeshBackedRendererEntryCount = 0,
            wholeCityMeshBackedRendererEntryCount = 0,
            continuousTerrainRoadCombinationAuthorized = 0
        };
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
