# Match HUD Attack Command Mode Plan

## Summary

Attack should follow the same explicit command model as Move:

- Select a player unit or group.
- Click `Attack`.
- HUD enters Attack targeting mode.
- Feedback tells the player `TAP TARGET`.
- Valid hostile targets are highlighted with red target rings.
- Civilian, neutral, friendly, missing, or otherwise invalid targets are not attackable.
- Tapping a valid hostile target issues the attack order.
- The selected unit or group stays selected.
- Attack targeting mode exits after a valid order.

This must remain ECS-aligned: UI emits intent only, command systems validate and mutate ECS orders, and HUD systems only present command state/results.

## Current State

- Attack orders already flow through `RtsSelectionCommandIntentKind.Attack`, `SelectionAttackCommandRequestSystem`, `AttackOrderCommandSystem`, `UnitTargetOrderSystem`, and `RtsSelectionCommandResultFlushCompositionSystemHelper`.
- The legacy focused attack path uses `ToggleAttackTargetMode` and a separate `explicitAttackTargetModeActive` flag.
- Match HUD already has a serialized `attackButton` reference on `MatchOverlayCommandControlsView`.
- Runtime click handling still has an implicit selected-unit attack fallback when no command mode is armed.
- Accepted attack results currently clear selected units, which conflicts with the new design.
- The HUD static marker preview is intentionally disabled for live gameplay; target rings need to come from runtime world markers.

## Target Architecture

### UI Flow

- `MatchOverlayCommandInputUiSystemHelper` binds `AttackButton`.
- Attack button calls a new explicit request such as `SelectionUiCommandSystem.RequestAttackCommandMode()`.
- The UI request captures/suppresses the button click release so it cannot also become a world click.
- Button highlight is driven by command-mode feedback, not by direct UI mutation.

### Command State

- Add `EnterAttackTargetMode` as a distinct command intent.
- `RtsSelectionFocusCommandSystem` consumes the intent.
- It validates that at least one selected player unit can attack.
- On valid selection, it arms `TacticalCommandMode.Attack` as a one-shot world-target command and shows HUD feedback.
- On no selection or non-attacking selection, it rejects with centralized feedback and does not arm.

### Pointer Flow

- Normal map clicks without an active command mode should not issue attack orders.
- When `TacticalCommandMode.Attack` is armed, a world click queues an attack target request.
- Invalid clicks publish rejection feedback and keep Attack mode active so the player can choose a valid target.
- Valid target clicks issue the order, clear Attack mode, and keep the selected units selected.

### Target Rings

- While Attack mode is active, runtime world markers highlight valid hostile targets only.
- Friendly, civilian, neutral, dead, or invalid entities are skipped.
- Rings are hidden immediately when Attack mode exits or is cancelled.
- Reuse the existing attack marker prefab path when possible; do not re-enable the static HUD marker preview layer.

## Implementation Steps

1. [x] Create this plan document.
2. [x] Add explicit Attack command-mode intent and UI request.
3. [x] Bind Match HUD Attack button to explicit Attack mode.
4. [x] Process `EnterAttackTargetMode` in the focus command boundary.
5. [x] Gate attack issuing behind active Attack mode and remove the implicit attack fallback.
6. [x] Update attack result flushing so accepted attacks keep selection and clear only Attack mode.
7. [x] Keep Attack mode active on invalid target feedback.
8. [x] Add runtime red target preview rings for valid hostile targets.
9. [x] Update feedback text to `TAP TARGET`.
10. [x] Add focused EditMode tests for request wiring and runtime pointer gating.
11. [ ] Run targeted validation and inspect Unity compile status.

## Test Plan

- Attack button enqueues `EnterAttackTargetMode` and suppresses the UI release.
- Attack button on no selection publishes `NoSelection` feedback and does not arm mode.
- Attack button with selected non-attacking unit publishes `TargetNotAttackable`.
- Attack button with selected attacking unit highlights Attack and shows `TAP TARGET`.
- Map click without Attack mode does not enqueue attack.
- Attack mode + invalid target rejects, keeps mode armed, and leaves selection intact.
- Attack mode + valid hostile target issues attack, exits mode, and leaves selection intact.
- Runtime target rings appear only for valid hostile targets while Attack mode is active.

## Progress

- [x] Plan documented.
- [x] Implementation started.
- [x] Explicit Attack intent and HUD button binding.
- [x] Attack mode arming through shared command state.
- [x] Runtime pointer gating and accepted-result selection preservation.
- [x] Valid hostile target preview rings.
- [x] Focused tests added.
- [x] `git diff --check`.
- [ ] Unity EditMode validation complete.

## Validation Notes

- `git diff --check` passes.
- Unity batchmode EditMode run for the two focused tests was blocked because the project is already open in another Unity instance.
- `dotnet build Assembly-CSharp.csproj` is not a reliable substitute for this Unity project: it fails in Unity package/generated-reference setup before compiling the changed game assembly.
