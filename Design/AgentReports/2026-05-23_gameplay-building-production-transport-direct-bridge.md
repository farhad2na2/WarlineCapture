Lane
Gameplay

Task
Remove remaining production transport wrapper delegates from BuildingPlacementSystem by wiring BuildingProductionTransportSystem directly to BuildingProductionTransportBridgeSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionUpdateSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs

Contracts touched
- GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedProductionSlice now forbids production transport wrapper delegates and wrapper methods in BuildingPlacementSystem.
- BuildingProductionTransportSystem.Context now carries BuildingProductionTransportBridgeSystem and its context directly.

User-visible behavior
- No intended gameplay behavior change.
- Produced-unit transport spawn, ground goal resolution, movement order assignment, and rotation alignment should behave as before.

Validation run
- Copied the changed files into /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity batchmode EditMode tests:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-transport-direct-bridge-architecture-rerun.log

Validation result
- Passed: 85/85.
- Result file: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151723681445940.xml

Known gaps
- BuildingPlacementSystem is still not a pure facade; it is now 2931 lines.
- BuildingProductionTransportSystem still owns visual transport/drop orchestration and now directly calls the bridge for ECS-side spawn/move/rotation effects.

Cross-lane impacts
- UI callers remain on the existing BuildingPlacementSystem API.
- AI/economy production callers should see no contract change.

Next recommended task
- Run a facade-only review of BuildingPlacementSystem and identify the next non-facade responsibility to extract, likely selected-building UI query wrappers or remaining context-factory consolidation.
