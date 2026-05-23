Lane
Gameplay

Task
Extract BuildingDefinition and RuntimeBuildingData out of BuildingPlacementSystem so extracted building systems no longer depend on facade-nested data contracts.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingDefinition.cs
- Assets/Game/Scripts/Systems/BuildingDefinition.cs.meta
- Assets/Game/Scripts/Systems/RuntimeBuildingData.cs
- Assets/Game/Scripts/Systems/RuntimeBuildingData.cs.meta
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
- Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementLifecycleSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementPreviewSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementValidationSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionSlotSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionUpdateSystem.cs
- Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeVisualSystem.cs
- Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs
- Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs
- Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs

Contracts touched
- GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedDefinitionSlice now requires BuildingDefinition and RuntimeBuildingData to be standalone data contracts.
- The contract now blocks BuildingDefinition and RuntimeBuildingData from being re-nested inside BuildingPlacementSystem.

User-visible behavior
- No intended gameplay behavior change.
- This is a data ownership cleanup only.

Validation run
- Copied updated Systems scripts, BuildingPlacementSystem, and GameplayArchitectureContractTests into /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity batchmode EditMode tests:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-data-contract-extraction.log

Validation result
- Passed: 85/85.
- Result file: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151728458337590.xml

Known gaps
- ECS AI systems still call BuildingPlacementSystem through BuildingPlacementRuntimeComponent.
- BuildingPlacementSystem is still the compatibility facade for UI/runtime callers.

Cross-lane impacts
- UI API remains stable.
- Extracted building systems can now share domain data without referencing BuildingPlacementSystem nested types.

Next recommended task
- Step 2: introduce an ECS-facing building runtime read model/command boundary so AI systems can stop calling BuildingPlacementSystem directly.
