#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using UnityEngine;

public static class ArchitectureHardeningCloseoutValidationRunner
{
    public static void RunFocusedValidation()
    {
        try
        {
            RunSuite(FieldFabricationCloseoutValidationRunner.RunFocusedValidation);
            RunSuite(MatchGameplayStartupCompositionSystemHelperTests.RunFocusedValidation);
            RunSuite(RuntimeFactionResourceSystemHelperTests.RunFocusedValidation);
            RunSuite(BuildingConstructionResourceTransactionSystemHelperTests.RunFocusedValidation);
            RunSuite(BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation);
            RunSuite(BuildingPlacementCommitCompositionSystemHelperTests.RunFocusedValidation);
            RunSuite(BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation);
            RunSuite(ResourceHaulerUtilitySystemHelperTests.RunFocusedValidation);
            RunSuite(ResourceExchangeRushSystemTests.RunFocusedValidation);
            RunSuite(UiResourceExchangeReadModelSystemTests.RunFocusedValidation);
            RunSuite(ResourceExchangeArchitectureGuardrailTests.RunFocusedValidation);
            RunSuite(ResourceExchangePopupPrefabTests.RunFocusedValidation);
            RunSuite(FirstLaunchGate89Validation.RunFocusedValidation);
            RunSuite(ContentResidencyInventoryGeneratorTests.RunFocusedValidation);
            RunSuite(MobileVisualQualityCaptureMatrixTests.RunFocusedValidation);
            RunSuite(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            RunSuite(NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation);
            RunSuite(EcsBurstSelectionCommandValidationRunner.RunFocusedValidation);
            RunSuite(NonEcsSystemConversionArchitectureTests.RunFocusedValidation);
            RunSuite(ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation);
            RunSuite(ScriptArchitectureAlignmentContractTests.RunBroadShellValidation);
            RunSuite(ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation);
            RunSuite(ScriptArchitectureAlignmentContractTests.RunRuntimeCompositionHelperLedgerValidation);
            Debug.Log("[ArchitectureHardeningCloseoutValidation] result=Passed suites=23");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[ArchitectureHardeningCloseoutValidation] result=Failed");
            Debug.LogException(exception);
            ValidationExit.Exit(1);
        }
    }

    private static void RunSuite(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
        {
            validation();
        }

        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
        {
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
        }
    }
}
#endif
