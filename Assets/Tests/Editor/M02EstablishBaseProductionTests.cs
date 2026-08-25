#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Authoring;
using Game.Composition;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseProductionTests
{
    private const string Marker =
        "[M02EstablishBaseProductionValidation] result=Passed tests=8";

    [MenuItem("Game/Validation/Run M02 Establish Base Production Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseProductionTests tests = new();
            BuildingProductionQueueCompositionSystemHelperTests shared = new();
            tests.CanonicalRifleResolvesExactResourceCostsAndTimer();
            shared.OperationMapCampProductionBridge_AtomicConstructionResourcesSpendOnceAndQueueCanonicalRequest();
            shared.OperationMapProducerQueueConsumer_CompletesStrictFifoOnce();
            shared.OperationMapProducerSpawnTransaction_InstantiatesConfiguredUnitAndCompletesRequest();
            shared.ResolveProducedUnitFaction_DefaultsNeutralOrUnownedProductionToPlayer();
            shared.FocusNewestPlayerProducedUnit_UsesProducedUnitReadModel();
            shared.CountRuntimeProducedUnitsForFaction_UsesProducedUnitReadModel();
            shared.BuildingUiProductionCommandRequest_QueuesSelectedBuildingUnitAndWritesResult();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseProductionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Production Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(BuildingProductionQueueCompositionSystemHelperTests.RunEditorFirstProductionFunctionalBatchValidation);
            RunValidation(M02EstablishBaseObjectiveTests.RunFocusedValidation);
            RunValidation(M01FirstContactContractValidation.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseProductionRegressionValidation] result=Passed suites=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseProductionRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CanonicalRifleResolvesExactResourceCostsAndTimer()
    {
        GameObject rifle = AssetDatabase.LoadAssetAtPath<GameObject>(
            M02EstablishBaseConfigBuilder.RequiredRiflePrefabPath);
        Assert.That(rifle, Is.Not.Null);
        UnitGridAuthoring authoring = rifle.GetComponent<UnitGridAuthoring>();
        Assert.That(authoring, Is.Not.Null);
        BuildingDefinitionPrefabSystemHelper definitions = new();
        definitions.ConfigureAuthoringMetadataResolvers(
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        Assert.That(definitions.TryResolveConfiguredUnitResourceCosts(
            rifle,
            fallbackMaterialsCost: 0,
            out int creditsCost,
            out int materialsCost), Is.True);
        Assert.That(creditsCost, Is.EqualTo(10000));
        Assert.That(materialsCost, Is.EqualTo(20));
        Assert.That(authoring.ProductionDurationSeconds, Is.EqualTo(5f).Within(0.0001f));
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }
}
#endif
