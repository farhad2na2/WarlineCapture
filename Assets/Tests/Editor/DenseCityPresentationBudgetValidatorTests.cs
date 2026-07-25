using System;
using System.IO;
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
            suite.Budget_RejectsTransformBoundsMismatch,
            suite.Budget_RejectsIncompleteGeometryEvidence,
            suite.Budget_RejectsIncompleteBakedOwnershipEvidence,
            suite.DenseOwnership_AcceptsCompleteEntitySceneEvidence,
            suite.DenseOwnership_RejectsMissingGeneratedIdentity,
            suite.DenseOwnership_RejectsAuthoringSceneEntry,
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
        StringAssert.Contains("\"packedContentMetricsComplete\": 0", first);
        StringAssert.Contains("\"entitySceneBytes\": -1", first);
        StringAssert.Contains("\"productionCutover\": 0", first);
        StringAssert.Contains("\"instancedTriangleCount\": 2000", first);
        StringAssert.Contains("\"uniqueTextureAssetCount\": 12", first);
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
            entryCount = 1849,
            sharedDependencyCount = 1844,
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
            unresolvedGeneratedRendererEntityCount = 0,
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
