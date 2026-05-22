# WarlineCapture Handoff

## Lane

Gameplay

## Task

Extract RTS pointer, drag-selection, suppression, and queued move-order input state from `RTSSelectionSystem`.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/RtsSelectionInputSystem.cs`
- `Assets/Game/Scripts/Systems/RtsSelectionInputSystem.cs.meta`
- `Assets/Tests/Editor/RtsSelectionInputSystemTests.cs`
- `Assets/Tests/Editor/RtsSelectionInputSystemTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay_rts-selection-input-state-extraction.md`

## Contracts touched

- Added architecture contract coverage requiring RTS input/session state to live in `RtsSelectionInputSystem`.
- Updated the RTS selection responsibility audit with the ninth extraction slice.

## User-visible behavior

- Intended no behavior change.
- `RTSSelectionSystem` still orchestrates input decisions and gameplay command dispatch.
- Mutable pointer drag state, UI/world click suppression state, selection-hold timing, live selection rectangle state, last-known pointer state, and deferred move-order queue state now live in `RtsSelectionInputSystem`.

## Validation run

- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RtsSelectionInputSystemTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `BattleHudGameplayBridgeConnectionTests`

## Validation result

- `RtsSelectionInputSystemTests`: passed, 5/5.
- `GameplayArchitectureContractTests`: passed, 37/37.
- `BattleHudGameplayBridgeConnectionTests`: passed, 6/6.

## Known gaps

- `RTSSelectionSystem` still contains the branch-heavy input orchestration and command side effects.
- This slice intentionally moved state ownership first; deeper extraction should wait until command side effects have narrower interfaces.

## Cross-lane impacts

- UI/HUD behavior should be unchanged; HUD bridge tests passed.
- Future RTS input state should be added to `RtsSelectionInputSystem`, not as mutable fields on `RTSSelectionSystem`.

## Next recommended task

Move the branch-heavy RTS input orchestration out of `RTSSelectionSystem` after command dispatch has narrower interfaces, or start the `InitialUnitsRuntimeState` static-runtime-state replacement with ECS singleton/request components.
