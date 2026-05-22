# WarlineCapture Handoff

## Lane

Gameplay

## Task

RTS selection refactor step 11: extract camera drag and smooth-focus state out of `RTSSelectionSystem`.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/RtsCameraSystem.cs`
- `Assets/Game/Scripts/Systems/RtsCameraSystem.cs.meta`
- `Assets/Tests/Editor/RtsCameraSystemTests.cs`
- `Assets/Tests/Editor/RtsCameraSystemTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay_rts-selection-refactor-step-11.md`

## Contracts touched

- Added architecture contract coverage requiring RTS camera drag and smooth-focus state to live in `RtsCameraSystem`.
- Updated the RTS selection responsibility audit to mark the camera state extraction complete and identify camera transform/mode behavior as the next camera slice.

## User-visible behavior

- Intended no behavior change.
- Camera drag state and smooth camera focus state now live in `RtsCameraSystem`; `RTSSelectionSystem` still applies camera transforms and mode transitions.

## Validation run

- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RtsCameraSystemTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `BattleHudGameplayBridgeConnectionTests`

## Validation result

- `RtsCameraSystemTests`: passed, 4/4.
- `GameplayArchitectureContractTests`: passed, 36/36.
- `BattleHudGameplayBridgeConnectionTests`: passed, 6/6.

## Known gaps

- Camera transform writes, fullscreen iso transition state, build-mode camera transition state, and normal iso transition state still live in `RTSSelectionSystem`.
- The `_cameraDragging` property remains in `RTSSelectionSystem` as a compatibility facade over `RtsCameraSystem` while existing input code is still in the facade.

## Cross-lane impacts

- UI/HUD lane should see no behavior change; existing bridge tests passed.
- Future UI or gameplay camera changes should use `RtsCameraSystem` for drag and smooth-focus state instead of adding new mutable camera fields to `RTSSelectionSystem`.

## Next recommended task

Move camera transform/mode transition behavior into `RtsCameraSystem` behind focused tests for perspective, fullscreen iso, build-mode, and normal iso transitions.
