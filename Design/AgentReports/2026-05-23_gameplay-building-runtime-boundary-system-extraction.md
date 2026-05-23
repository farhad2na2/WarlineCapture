# WarlineCapture Handoff

Lane: Gameplay

Task: Extract building runtime ECS boundary publish/consume orchestration out of `BuildingPlacementSystem` into `BuildingRuntimeBoundarySystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs.meta`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-building-runtime-boundary-system-extraction.md`

Contracts touched:
- `BuildingRuntimeBoundarySystem` now owns temporary ECS boundary request consumption and read-model publishing.
- `BuildingPlacementSystem.Update()` now delegates boundary work through `UpdateBuildingRuntimeBoundary()`.
- Architecture contract now names `BuildingRuntimeBoundarySystem` as the temporary boundary publish/consume owner.
- Architecture guard now prevents configured/read-model boundary publishing from returning to `BuildingPlacementSystem`.

User-visible behavior:
- No intended gameplay behavior change.
- AI/building ECS boundary buffers are still published and consumed as before.

Validation run:
- Unity 6000.4.0f1 batchmode EditMode test run in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Command: `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-runtime-boundary-system-rerun.log`

Validation result:
- Passed.
- Result file: `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151743316045960.xml`
- `GameplayArchitectureContractTests`: 87/87 passed.

Known gaps:
- `BuildingRuntimeBoundarySystem` still depends on `BuildingPlacementSystem` as a temporary facade dependency while remaining AI command paths are migrated.
- `BuildingPlacementSystem` still owns the `BuildingRuntimeBoundaryTag` query because it already owns the managed Unity update bridge; this can move later when the boundary has no facade dependency.

Cross-lane impacts:
- AI lane can continue migrating command paths to ECS buffers without adding new code to `BuildingPlacementSystem`.
- Architecture lane now has a concrete guard against boundary code drifting back into the facade.

Next recommended task:
- Add a sell-resource ECS request/result buffer and migrate `AIEconomySystem` sell mutation away from `BuildingPlacementSystem`.
