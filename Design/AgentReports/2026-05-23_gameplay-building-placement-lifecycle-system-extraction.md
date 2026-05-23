Lane
Gameplay

Task
Extract active placement lifecycle/state out of BuildingPlacementSystem into BuildingPlacementLifecycleSystem while keeping the existing public facade stable.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementLifecycleSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementLifecycleSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-placement-lifecycle-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so active placement session state, begin/cancel/confirm flow, active placement cost, active placement preview handoff, and active placement facade queries belong to BuildingPlacementLifecycleSystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate lifecycle methods/state and preventing _activePlacement, _activePlacementCost, local PlacementState construction, and preview cancellation ownership from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Building placement public API remains on BuildingPlacementSystem.
- Active placement begin/cancel/confirm/session state is now owned by BuildingPlacementLifecycleSystem.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementLifecycleSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementLifecycleSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-placement-lifecycle-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 79/79.
- Unity log included an initial licensing handshake warning, then resolved entitlement details and completed the test run with exit code 0.

Known gaps
- BuildingPlacementSystem is still not a pure facade. It remains about 4604 lines and still owns additional placement/grid math, runtime orchestration, hauler/path bridge behavior, and visual/grid helper seams.
- BeginPlacement remains as a private facade entry point for existing public methods, but it delegates lifecycle work to BuildingPlacementLifecycleSystem.

Cross-lane impacts
- No UI asset or scene changes.
- No ECS component contract changes.
- Architecture tests now enforce the lifecycle boundary for future Gameplay work.

Next recommended task
Extract BuildingPlacementGridSystem for remaining placement/grid math and focus/origin helper behavior, then reassess whether BuildingPlacementSystem can be renamed to a facade after grid and wall residuals are moved.
