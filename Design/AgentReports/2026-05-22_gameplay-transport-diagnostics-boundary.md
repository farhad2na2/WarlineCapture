Lane
Gameplay

Task
Move transport boarding diagnostics off direct `InitialUnitsRuntimeState` reads and behind the runtime diagnostics ECS boundary.

Files changed
- `Assets/Game/Scripts/Components/RuntimeDiagnosticsStateComponent.cs`
- `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs`
- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs`
- `Assets/Tests/Editor/RuntimeDiagnosticsSystemTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-transport-diagnostics-boundary.md`

Contracts touched
- `RuntimeDiagnosticsStateComponent` now carries `TransportBoardingDiagnostics`.
- `RuntimeDiagnosticsSystem` is now the compatibility boundary for transport boarding diagnostics.
- `GameplayArchitectureContractTests` now blocks production scripts from directly reading `InitialUnitsRuntimeState.TransportBoardingDiagnostics`.

User-visible behavior
- No intended gameplay or UI behavior change.
- Existing transport boarding diagnostic logs still use the same `TransportBoard` and `Selection` prefixes when diagnostics are enabled.

Validation run
- `rg -n "InitialUnitsRuntimeState\\.TransportBoardingDiagnostics" Assets/Game/Scripts --glob '!**/Editor/**'`
- `git diff --check -- Assets/Game/Scripts/Components/RuntimeDiagnosticsStateComponent.cs Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs Assets/Game/Scripts/UI/RTSSelectionSystem.cs Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs Assets/Tests/Editor/RuntimeDiagnosticsSystemTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `RuntimeDiagnosticsSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `UnitTransport`

Validation result
- Direct production reads of `InitialUnitsRuntimeState.TransportBoardingDiagnostics` remain only inside `RuntimeDiagnosticsSystem`.
- Diff whitespace check passed.
- `RuntimeDiagnosticsSystemTests`: passed 4/4.
- `GameplayArchitectureContractTests`: passed 44/44.
- `UnitTransport`: passed 20/20.

Known gaps
- `InitialUnitsRuntimeState.TransportBoardingDiagnostics` remains as legacy compatibility storage inside `InitialUnitsRuntimeState` and the diagnostics boundary.
- `AILog` remains a static compatibility facade. Retiring it requires a separate domain-by-domain ECS log event migration.

Cross-lane impacts
- QA/debug workflows can continue enabling transport boarding diagnostics through the existing compatibility flag during migration.
- Gameplay code must use `RuntimeDiagnosticsSystem` or `RuntimeDiagnosticsStateComponent` for transport diagnostics state.

Next recommended task
Start retiring static `AILog` usage by one AI domain slice, beginning with `AIBuildPlannerSystem` or `AIProductionSystem`, and replace string-based static logging with ECS diagnostic events flushed by a shell logging boundary.
