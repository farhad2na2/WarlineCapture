#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using UnityEngine;

public static class FieldFabricationCloseoutValidationRunner
{
    public static void RunFocusedValidation()
    {
        try
        {
            RunSuite(MaterialsScenarioRecoveryStartupSystemHelperTests.RunFocusedValidation);
            RunSuite(MaterialFabricationSystemTests.RunFocusedValidation);
            RunSuite(BuildingResourceProductionEcsSystemTests.RunFocusedValidation);
            RunSuite(BuildingMaterialsCostConfigProjectionTests.RunFocusedValidation);
            RunSuite(BuildingPlacementConstructionTransactionTests.RunFocusedValidation);
            RunSuite(UiBuildDrawerDualCostReadModelTests.RunFocusedValidation);
            RunSuite(BuildDrawerCatalogQueryUiSystemHelperTests.RunFocusedValidation);
            RunSuite(ResourceExchangeHeaderRoutingTests.RunFocusedValidation);
            RunSuite(ResourceExchangeRequestValidationSystemTests.RunFocusedValidation);
            RunSuite(ResourceExchangeQueueTickSystemTests.RunFocusedValidation);
            RunSuite(ResourceExchangeStartupProjectionSystemHelperTests.RunFocusedValidation);
            RunSuite(FactionEconomyStartupSystemValidationTests.RunFocusedValidation);
            RunSuite(CustomGameStartupSystemHelperTests.RunFocusedValidation);
            RunSuite(AIBuildPlannerValidationTests.RunFocusedValidation);
            RunSuite(AIEndToEndValidationTests.RunFocusedValidation);
            RunSuite(InitialUnitsSpawnFocusedTests.RunResourceBuildingSourceKeyBatchValidation);
            RunSuite(InitialUnitsSpawnFocusedTests.RunSpawnProgressCompletionBatchValidation);
            Debug.Log("[FieldFabricationCloseoutValidation] result=Passed suites=17");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[FieldFabricationCloseoutValidation] result=Failed");
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
