# Lane
Gameplay

# Task
Migrate `PlayerAutoModeEnabled` from direct `InitialUnitsRuntimeState` access into the runtime gameplay state boundary.

# Files changed
- `Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs`
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/UI/MenuView.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateSystemTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`

# Contracts touched
- `RuntimeGameplayStateComponent` now includes `PlayerAutoModeEnabled`.
- `RuntimeGameplayStateSystem` now mirrors `PlayerAutoModeEnabled` between legacy compatibility state and ECS runtime state.
- Architecture contract tests now reject production direct access to `InitialUnitsRuntimeState.PlayerAutoModeEnabled`.

# User-visible behavior
No intended behavior change. The player auto/manual toggle and AI settings still drive the same faction control behavior, but state now flows through the runtime gameplay state boundary instead of direct static access.

# Validation run
- `git diff --check`
- Unity EditMode `RuntimeGameplayStateSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AIControlModeValidationTests`
- Unity EditMode `BattleHudGameplayBridgeConnectionTests`

# Validation result
- `git diff --check`: passed.
- `RuntimeGameplayStateSystemTests`: passed 6/6.
- `GameplayArchitectureContractTests`: passed 40/40.
- `AIControlModeValidationTests`: passed 1/1.
- `BattleHudGameplayBridgeConnectionTests`: passed 6/6.

# Known gaps
- `InitialUnitsRuntimeState.PlayerAutoModeEnabled` remains as legacy compatibility storage behind `RuntimeGameplayStateSystem`.
- `WorldCamera`, AI log verbosity/static logging, and transport boarding diagnostics are still legacy static runtime state.

# Cross-lane impacts
- UI should verify the Auto/Manual button still toggles and labels correctly in the game HUD.
- QA should verify player auto mode still updates faction control entries and plan enablement in live gameplay.

# Next recommended task
Migrate `WorldCamera` behind a dedicated camera-reference boundary. Do not put the camera object reference into normal unmanaged ECS `IComponentData`; keep it in a managed bridge/component or shell-owned service boundary.
