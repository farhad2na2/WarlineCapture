Lane
Gameplay

Task
Move AI log enablement off direct `InitialUnitsRuntimeState` reads and behind a runtime diagnostics ECS boundary.

Files changed
- `Assets/Game/Scripts/Components/RuntimeDiagnosticsStateComponent.cs`
- `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs`
- `Assets/Game/Scripts/Systems/AILog.cs`
- `Assets/Tests/Editor/RuntimeDiagnosticsSystemTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-logging-diagnostics-boundary.md`

Contracts touched
- Added the diagnostics boundary rule to `Design/Architecture/gameplay_solid_ecs_contract.md`.
- Added contract coverage so production scripts cannot directly read `InitialUnitsRuntimeState.VerboseAILogs` or `InitialUnitsRuntimeState.ShouldLogAI` outside `RuntimeDiagnosticsSystem`.

User-visible behavior
- No intended gameplay or UI behavior change.
- AI logging still respects verbose AI logging and batchmode logging, but the policy now flows through `RuntimeDiagnosticsSystem` / `RuntimeDiagnosticsStateComponent`.

Validation run
- `rg -n "InitialUnitsRuntimeState\\.(VerboseAILogs|ShouldLogAI)" Assets/Game/Scripts --glob '!**/Editor/**'`
- `git diff --check -- Assets/Game/Scripts/Components/RuntimeDiagnosticsStateComponent.cs Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs Assets/Game/Scripts/Systems/AILog.cs Assets/Tests/Editor/RuntimeDiagnosticsSystemTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `RuntimeDiagnosticsSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- Direct legacy AI diagnostics state reads remain only in `RuntimeDiagnosticsSystem`.
- Diff whitespace check passed.
- `RuntimeDiagnosticsSystemTests`: passed 3/3.
- `GameplayArchitectureContractTests`: passed 42/42.
- `AI`: passed 20/20.

Known gaps
- `AILog` remains a static compatibility facade for existing AI log call sites. This slice moved the enablement state behind an ECS diagnostics boundary; retiring the static logging facade itself should be done by domain slice.
- Transport boarding diagnostics still have static runtime diagnostic debt and should be migrated separately.

Cross-lane impacts
- QA can continue using AI validation logs in batchmode.
- Future gameplay work must use `RuntimeDiagnosticsSystem` or ECS diagnostics data for AI log policy, not direct `InitialUnitsRuntimeState` access.

Next recommended task
Migrate transport boarding diagnostics static state behind an ECS diagnostics/request boundary and add contract coverage so the static diagnostic debt cannot spread.
