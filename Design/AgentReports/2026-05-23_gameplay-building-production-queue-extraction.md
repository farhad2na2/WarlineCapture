Lane
Gameplay

Task
Move TryQueuePlayerUnitFromBuilding into the existing BuildingProductionSystem ownership boundary.

Files changed
- Assets/Game/Scripts/Systems/BuildingProductionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so player unit production queue mutation is explicitly owned by BuildingProductionSystem.
- Updated GameplayArchitectureContractTests to require BuildingProductionSystem ownership of TryQueuePlayerUnitFromBuilding, pending production append, and production slot reservation orchestration, and to prevent the old private queue mutation method from returning to BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Player and faction unit production requests still enter through the same BuildingPlacementSystem public APIs.
- Pending production creation, slot reservation, transport settings, helipad exception, duration setup, and queue append now execute inside BuildingProductionSystem.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingProductionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-production-queue-tests.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 74/74.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151523969731760.xml

Known gaps
- BuildingPlacementSystem still has pending production processing orchestration and several building-domain facade calls; it is now 6148 lines.
- CountRuntimeProducedUnitsForFaction still calls BuildingProductionSystem.PruneProducedUnits from BuildingPlacementSystem. That is read/query cleanup, not queue mutation, but it can move in a later faction production/query slice.

Cross-lane impacts
- UI and AI/faction callers keep the same public BuildingPlacementSystem production APIs.
- No art, scene, or UI prefab files were intentionally modified by this slice.
- Existing unrelated dirty Armory/POP05/UI files were left untouched.

Next recommended task
Extract faction production lookup/count query behavior or pending-production processing orchestration out of BuildingPlacementSystem, depending on whether the next goal is shrinking faction production facade debt or runtime update-loop debt.
