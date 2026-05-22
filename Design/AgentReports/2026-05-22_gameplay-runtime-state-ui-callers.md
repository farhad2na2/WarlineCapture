# WarlineCapture Handoff

## Lane

Gameplay

## Task

Migrate `MainMenuPlayUI` and `MenuView` to the `RuntimeGameplayStateSystem` boundary for the runtime flags already covered by ECS singleton components.

## Files changed

- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`
- `Assets/Game/Scripts/UI/MenuView.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-runtime-state-ui-callers.md`

## Contracts touched

- Extended architecture contract tests so `MainMenuPlayUI`, `MenuView`, and `RTSSelectionSystem` must use `RuntimeGameplayStateSystem` for migrated runtime flags.

## User-visible behavior

- Intended no behavior change.
- `MainMenuPlayUI` now initializes and triggers selection-hold runtime flags through `RuntimeGameplayStateSystem`.
- `MenuView` now reads/writes play state, selection mode, zoom-held state, and world-click suppression through `RuntimeGameplayStateSystem`.

## Validation run

- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RuntimeGameplayStateSystemTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `BattleHudGameplayBridgeConnectionTests`

## Validation result

- `RuntimeGameplayStateSystemTests`: passed, 4/4.
- `GameplayArchitectureContractTests`: passed, 38/38.
- `BattleHudGameplayBridgeConnectionTests`: passed, 6/6.

## Known gaps

- `PlayerAutoModeEnabled` still uses `InitialUnitsRuntimeState` because it has not been added to the ECS runtime state component set.
- `RoadBuildSystem`, `BuildingPlacementSystem`, `GameBootstrap`, AI systems, diagnostics, and other callers still use direct `InitialUnitsRuntimeState` access and should migrate in focused groups.

## Cross-lane impacts

- UI/HUD lane should see no behavior change; HUD bridge tests passed.
- Future UI work touching migrated runtime flags should use `RuntimeGameplayStateSystem`, not direct static state.

## Next recommended task

Migrate the build-mode caller group: `RoadBuildSystem`, `BuildingPlacementSystem`, and the bootstrap start/reset paths to `RuntimeGameplayStateSystem` for the same migrated flags.
