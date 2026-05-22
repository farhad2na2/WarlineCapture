Lane
Gameplay

Task
Move `AIBuildPlannerSystem` diagnostics off the static `AILog` facade and onto an ECS diagnostic event buffer flushed by a shell-edge logging system.

Files changed
- `Assets/Game/Scripts/Components/AIDiagnosticLogComponents.cs`
- `Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs`
- `Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs`
- `Assets/Tests/Editor/AIBuildPlannerValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-build-diagnostic-events.md`

Contracts touched
- Added `AIDiagnosticLogQueueComponent` and `AIDiagnosticLogComponent` as the ECS diagnostic event contract for migrated AI logs.
- Added `AIDiagnosticLogFlushSystem` as the logging shell boundary that performs `Debug.Log`.
- `AIBuildPlannerSystem` now queues diagnostics through ECS data and no longer calls `AILog`.
- `GameplayArchitectureContractTests` now asserts that `AIBuildPlannerSystem` remains off `AILog` and uses ECS diagnostic events.
- `gameplay_solid_ecs_contract.md` now documents the ECS diagnostic event pattern for AI log migrations.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AIBuild]` diagnostics still appear in validation and batchmode when the build planner places a building.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Components/AIDiagnosticLogComponents.cs Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs Assets/Tests/Editor/AIBuildPlannerValidationTests.cs Assets/Tests/Editor/AIEndToEndValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AIBuildPlannerValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AIBuildPlannerSystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- `AIBuildPlannerValidationTests`: passed 1/1.
- `GameplayArchitectureContractTests`: passed 45/45.
- Broad Unity EditMode `AI`: passed 22/22.
- Initial validation exposed a test-boundary ordering issue in `AIEndToEndValidationTests`; the test now flushes the ECS diagnostic queue immediately after the build planner update before expecting later AI logs.

Known gaps
- Other AI systems still use the static `AILog` facade and should be migrated one domain slice at a time.
- The current `AIDiagnosticLogComponent` stores formatted messages. Future slices can introduce typed diagnostic components if we need more structured QA/performance reporting.

Cross-lane impacts
- QA keeps the existing `[AIBuild]` log surface for AI validation.
- Architecture now has a concrete ECS diagnostic event pattern for replacing static gameplay logging without moving `Debug.Log` calls into hot gameplay systems.

Next recommended task
Migrate `AIProductionSystem` diagnostics to the same ECS diagnostic event path, then remove it from the static `AILog` debt allowlist.
