# WarlineCapture Handoff

Lane: Gameplay

Task: Migrate `AIFactionControlSystem` and `AIEconomySystem` read paths to the new ECS building runtime boundary buffers.

Files changed:
- `Assets/Game/Scripts/Systems/AIFactionControlSystem.cs`
- `Assets/Game/Scripts/Systems/AIEconomySystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/AgentReports/2026-05-23_gameplay-ai-building-boundary-read-migration.md`

Contracts touched:
- `AIFactionControlSystem` now reads faction building counts from `BuildingRuntimeFactionSummary` on the `BuildingRuntimeBoundaryTag` entity.
- `AIEconomySystem` now reads stored oil/fuel and income rates from `BuildingRuntimeFactionSummary`.
- Added architecture guard coverage requiring these AI read paths to use the ECS boundary and preventing faction-control from returning to `BuildingPlacementSystem` building-count reads.

User-visible behavior:
- No intended gameplay behavior change.
- AI faction-control diagnostics should report the same controlled building counts once the boundary publisher has emitted its summary.
- AI economy still sells resources through the existing building facade; only the resource read snapshot moved this step.

Validation run:
- Unity 6000.4.0f1 batchmode EditMode test run in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Command: `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-ai-building-boundary-read-migration.log`

Validation result:
- Passed.
- Result file: `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151736435372210.xml`
- `GameplayArchitectureContractTests`: 87/87 passed.

Known gaps:
- `AIEconomySystem` still calls `BuildingPlacementSystem.SellFactionResources` for the mutation path because the ECS building runtime boundary does not yet define a sell-resource command/result buffer.
- `AIBuildPlannerSystem` and `AIProductionSystem` still use `BuildingPlacementRuntimeComponent`/`BuildingPlacementSystem` for build and production command paths.

Cross-lane impacts:
- AI read-only building/resource state now has a structured ECS buffer source.
- Future UI or AI work should not add new read calls to the building facade for faction building/resource summaries.

Next recommended task:
- Add a narrow ECS sell-resource request/result buffer, then migrate `AIEconomySystem` sell mutation away from `BuildingPlacementSystem`.
