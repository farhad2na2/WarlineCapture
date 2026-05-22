# WarlineCapture Handoff

## Lane

Gameplay

## Task

Start the `InitialUnitsRuntimeState` migration by adding an ECS-backed runtime gameplay state boundary and routing `RTSSelectionSystem` through it.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs`
- `Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs.meta`
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs.meta`
- `Assets/Tests/Editor/RuntimeGameplayStateSystemTests.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateSystemTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-runtime-state-boundary-step-1.md`

## Contracts touched

- Added architecture contract language naming `InitialUnitsRuntimeState` as legacy compatibility debt and `RuntimeGameplayStateSystem` as the migration boundary.
- Added contract coverage requiring `RTSSelectionSystem` to use `RuntimeGameplayStateSystem` for migrated runtime flags.

## User-visible behavior

- Intended no behavior change.
- `RTSSelectionSystem` now reads/writes play/build/map, selection mode, suppress-world-click, zoom-held, and initial camera-focus request flags through `RuntimeGameplayStateSystem`.
- `RuntimeGameplayStateSystem` mirrors those flags into ECS singleton components while preserving the legacy static bridge for unmigrated callers.

## Validation run

- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RuntimeGameplayStateSystemTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RtsSelectionInputSystemTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `BattleHudGameplayBridgeConnectionTests`

## Validation result

- `RuntimeGameplayStateSystemTests`: passed, 4/4.
- `GameplayArchitectureContractTests`: passed, 38/38.
- `RtsSelectionInputSystemTests`: passed, 5/5.
- `BattleHudGameplayBridgeConnectionTests`: passed, 6/6.

## Known gaps

- `InitialUnitsRuntimeState` still exists and remains the compatibility bridge for many unmigrated callers.
- `RuntimeGameplayStateSystem` currently mirrors legacy static state into ECS on reads so old direct writers continue to work during the migration.
- Other systems/UI files still directly read/write `InitialUnitsRuntimeState`; those should migrate by focused slices.

## Cross-lane impacts

- UI/HUD behavior should be unchanged; HUD bridge tests passed.
- Future code touching the migrated RTS runtime flags should use `RuntimeGameplayStateSystem`, not direct `InitialUnitsRuntimeState` access.

## Next recommended task

Migrate the next caller group, likely `MainMenuPlayUI` and `MenuView` zoom/selection/suppress-click writes, to `RuntimeGameplayStateSystem` while preserving the static compatibility bridge.
