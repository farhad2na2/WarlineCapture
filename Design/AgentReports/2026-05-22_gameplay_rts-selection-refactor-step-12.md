# WarlineCapture Handoff

## Lane

Gameplay

## Task

RTS selection refactor step 12: move camera transform and mode-transition behavior out of `RTSSelectionSystem`.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/RtsCameraSystem.cs`
- `Assets/Game/Scripts/Systems/RtsCameraSystem.cs.meta`
- `Assets/Tests/Editor/RtsCameraSystemTests.cs`
- `Assets/Tests/Editor/RtsCameraSystemTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay_rts-selection-refactor-step-12.md`

## Contracts touched

- Extended the architecture contract so `RTSSelectionSystem` must delegate camera mode state, transition numeric state, transform writes, camera mode writes, and ground-plane ray queries to `RtsCameraSystem`.
- Updated the RTS selection responsibility audit to mark the camera transform/mode extraction complete.

## User-visible behavior

- Intended no behavior change.
- Camera pan, perspective zoom, fullscreen iso zoom, camera mode transitions, ground-center movement, and camera ground-span fitting now live in `RtsCameraSystem`.
- `RTSSelectionSystem` still decides when camera actions happen from input/UI/runtime state.

## Validation run

- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RtsCameraSystemTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `BattleHudGameplayBridgeConnectionTests`

## Validation result

- `RtsCameraSystemTests`: passed, 9/9.
- `GameplayArchitectureContractTests`: passed, 36/36.
- `BattleHudGameplayBridgeConnectionTests`: passed, 6/6.

## Known gaps

- `RTSSelectionSystem` still owns pointer/input orchestration, drag-selection rectangle lifetime, click suppression, and runtime state decisions.
- Static `InitialUnitsRuntimeState` reads still drive camera requests; this remains architecture debt for a later ECS singleton/request-component slice.

## Cross-lane impacts

- UI/HUD lane should see no behavior change; bridge tests passed.
- Future camera tuning should be made in `RtsCameraSystem` rather than adding new camera transform or transition state to `RTSSelectionSystem`.

## Next recommended task

Move RTS input state and drag-selection rectangle lifetime into a dedicated selection-input system or ECS request components.
