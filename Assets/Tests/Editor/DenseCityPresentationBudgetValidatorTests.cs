using System;
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
            suite.Budget_SerializationIsDeterministicAndMarksPackedMetricsPending
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
        CreateEvidence(out var bake, out var art, out var parity, out var layout);
        parity.bakedRenderEntityCount--;

        Assert.That(
            DenseCityPresentationBudgetValidator.TryCreateReport(
                bake, art, parity, layout, out _, out string error),
            Is.False);
        Assert.That(error, Is.EqualTo("transform-bounds-parity-budget"));
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
    }

    private static bool TryCreateValidReport(
        out DenseCityPresentationBudgetValidator.PresentationBudgetReport report,
        out string error)
    {
        CreateEvidence(out var bake, out var art, out var parity, out var layout);
        return DenseCityPresentationBudgetValidator.TryCreateReport(
            bake, art, parity, layout, out report, out error);
    }

    private static void CreateEvidence(
        out DenseCityPresentationBudgetValidator.CandidateBakeEvidence bake,
        out DenseCityPresentationBudgetValidator.SharedArtEvidence art,
        out DenseCityPresentationBudgetValidator.TransformParityEvidence parity,
        out DenseCityPresentationBudgetValidator.CandidateLayoutEvidence layout)
    {
        bake = new DenseCityPresentationBudgetValidator.CandidateBakeEvidence
        {
            result = "CandidateBakeValidationPassed",
            gameplayBuildingCount = 432,
            gameplayVehicleCount = 22,
            presentationRootCount = 3,
            presentationIdentityCount = 9544,
            renderMeshEntityCount = 14249,
            buildingRenderChildCount = 698,
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
    }
}
