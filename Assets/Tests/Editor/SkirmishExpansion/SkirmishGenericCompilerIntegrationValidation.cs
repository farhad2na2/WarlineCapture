using System;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishGenericCompilerIntegrationValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                SkirmishCameraFramingMeasurementTests.RunFocusedValidation();
                SkirmishMissionViewProfileTests.RunFocusedValidation();
                SkirmishLegacyLayoutBaselineRegressionTests.RunFocusedValidation();
                SkirmishMeasuredLayoutBinderTests.RunFocusedValidation();
                SkirmishObjectiveAreaConfigTests.RunFocusedValidation();
                SkirmishConfigOnlyBatchBuilderTests.RunFocusedValidation();
                SkirmishGenericGroundRosterTests.RunFocusedValidation();
                // Combined suite already invokes ExpandedDefinitionTests exactly
                // once, and publishes that suite's independent pass marker.
                SkirmishCombinedArmsTests.RunFocusedValidation();
                SkirmishExpandedCheckpointTests.RunFocusedValidation();
                SkirmishNativeCheckpointCodecTests.RunFocusedValidation();
                SkirmishEnemyPolicyV1Tests.RunFocusedValidation();
                SkirmishExpandedVisualTests.RunFocusedValidation();
                var buildings = new SkirmishStartingBuildingsTests();
                buildings.PhysicalSupplyGrantIsAtomicAndDoesNotRefillOnRepeatedStartup();
                buildings.OilGrantStartsFabricationAndTheHaulChainBeforeRefineryOverflow();
                buildings.StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce();
                Debug.Log("[SkirmishStartingBuildingsRegression] result=Passed tests=3");
                global::OperationMapCurrentCompatibilityDefinitionTests.RunFocusedValidation();
                // Existing static wrappers call ValidationExit.Exit: invoke exact
                // NUnit methods directly so no suite can exit before later checks.
                var grid = new global::OperationMapGridStartupBindingTests();
                grid.ActiveMetadata_MatchingCompatibilityGridPublishesAuthoredBlockedCells();
                grid.ActiveMetadata_GridOrBlockedCountMismatchFailsClosed();
                grid.ActiveMetadata_ZeroBlockedCellsDoesNotRequireCompatibilityAsset();
                grid.ActiveMetadata_MissingRequiredCapabilityFailsClosed();
                grid.NoActiveMap_UsesCompatibilityGrid();
                Debug.Log("[OperationMapGridStartupBindingFocusedValidation] result=Passed tests=5");
                var ownership = new global::OperationMapEntitySceneAddressablesOwnershipTests();
                ownership.Planner_RequiresDistinctCandidateEntitySceneAndCoreRoles();
                ownership.Planner_DoesNotPromoteTransitiveDependenciesToExplicitOwnership();
                ownership.CandidateDefinition_ReferencesOnlyEntitySceneRuntimeAssets();
                ownership.ProductionDefinition_UsesAcceptedEntitySceneWhileCandidatePathIsSeparate();
                ownership.DenseCityPlanner_RequiresDistinctPathsAndExactlyFiveCoreRoles();
                ownership.DenseCityPlanner_IsGuidIsolatedAndContainsNoProductionReferences();
                ownership.DenseCityCandidateDefinition_ReferencesOnlyDenseEntitySceneRuntimeAssets();
                Debug.Log("[OperationMapEntitySceneAddressablesOwnershipValidation] result=Passed tests=7");
                SkirmishProductionContinuationValidation.RunFocusedValidation();
                Game.Editor.SkirmishMissionViewProfileAuthoring.CreateS005EngineeringCandidate();
                Game.Editor.SkirmishProductionNativeIdentityCensus.ExportS005RegularStandard155926Fa();
                Debug.Log("[SkirmishGenericCompilerIntegrationValidation] result=Passed legacyConfigurations=12 binder=8 areas=15 builder=4 campaignCurrent=4 campaignGrid=5 campaignOwnership=7 starting=3 nativeMissionCertified=0");
            }
            catch (Exception error)
            {
                // Includes missing-asset Assume/Inconclusive exceptions: those must
                // never silently count as successful source-ownership regression.
                Debug.LogError("[SkirmishGenericCompilerIntegrationValidation] result=Failed\n" + error);
                throw;
            }
        }
    }
}
