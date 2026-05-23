# WarlineCapture Handoff

Lane: Gameplay

Task: Step 2 - introduce ECS-facing building runtime read model/command boundary so AI/building integrations can move away from direct `BuildingPlacementSystem` calls.

Files changed:
- `Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs`
- `Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs.meta`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-building-ecs-boundary-step2.md`

Contracts touched:
- Added `BuildingRuntimeBoundaryTag` plus ECS buffer contracts for configured building/unit read models, faction runtime/resource summaries, owned-building summaries, unit production summaries, faction unit-production requests, and runtime building spawn requests.
- `GameBootstrap` now installs the building runtime boundary tag and buffers on the building runtime entity.
- `BuildingPlacementSystem` temporarily publishes read models and consumes pending ECS request buffers while the facade is being retired.
- Architecture contract now requires AI/building cross-domain integration to move through the ECS building runtime boundary.
- Architecture test coverage now locks the boundary component file, bootstrap buffer installation, and temporary facade publisher/consumer hooks.

User-visible behavior:
- No intended gameplay behavior change.
- This is an architecture boundary step. Existing UI/building behavior remains routed through the current facade while the ECS buffers are introduced.

Validation run:
- Unity 6000.4.0f1 batchmode EditMode test run in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Command: `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-ecs-boundary.log`

Validation result:
- Passed.
- Result file: `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151733738792550.xml`
- `GameplayArchitectureContractTests`: 86/86 passed.

Known gaps:
- AI systems still read `BuildingPlacementRuntimeComponent` and call `BuildingPlacementSystem` facade methods. The ECS boundary now exists, but the AI call sites have not been migrated yet.
- The boundary publisher currently lives in `BuildingPlacementSystem` as temporary facade debt. It should move to a narrower building boundary/projection system after AI migration proves the contract shape.

Cross-lane impacts:
- AI lane can now migrate build planning, production, economy, and faction-control reads/commands to ECS buffers without adding new singleton/static access.
- UI lane should avoid consuming this boundary directly until UI read models are intentionally moved off the facade.

Next recommended task:
- Migrate `AIFactionControlSystem` and `AIEconomySystem` read paths first to `BuildingRuntimeFactionSummary` / `BuildingRuntimeOwnedBuildingSummary`, because those are read-only and lower risk than command request migration.
