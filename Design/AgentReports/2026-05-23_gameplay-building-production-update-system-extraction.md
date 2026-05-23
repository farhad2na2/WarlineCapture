Lane
Gameplay

Task
Extract pending production runtime update orchestration from BuildingPlacementSystem into a narrow BuildingProductionUpdateSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionUpdateSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionUpdateSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs

Contracts touched
- GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedProductionSlice now requires pending production runtime updates to live in BuildingProductionUpdateSystem.
- BuildingPlacementSystem remains the compatibility facade for production update calls and context creation.

User-visible behavior
- No intended gameplay behavior change.
- Pending production ticking, transport launch timing, production delay, and produced-unit spawning should behave as before.

Validation run
- Copied the changed source/test files into /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity batchmode EditMode tests:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-production-update-architecture-rerun.log

Validation result
- Passed: 85/85.
- Result file: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151717873576520.xml

Known gaps
- BuildingPlacementSystem is smaller but still not a pure facade.
- Production transport callback wrappers still route through BuildingPlacementSystem for compatibility.

Cross-lane impacts
- UI lane should not depend on pending production update internals; the public BuildingPlacementSystem API remains stable.
- AI/economy callers should see no contract change.

Next recommended task
- Remove the remaining production transport wrapper delegates from BuildingPlacementSystem by letting BuildingProductionTransportSystem consume the production transport bridge/context directly, then re-run the facade-only review.
