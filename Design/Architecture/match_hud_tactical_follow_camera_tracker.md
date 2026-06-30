# Match HUD Tactical Follow Camera Tracker

Purpose:
Add a Match HUD `CameraButton` mode that smoothly switches the existing RTS camera into a third-person tactical follow view for the current selected unit, selected group, or selected building. While that mode is active, launcher-fired missiles from the followed selection temporarily take camera focus until impact/explosion, then the camera returns to the followed selection. The implementation must use existing ECS gameplay state and camera/request boundaries; it must not add a parallel camera/gameplay simulator.

Last updated:
2026-06-30

## Progress Snapshot

- Checklist progress: `81 / 103 complete (78.6%)`.
- In progress: `0`.
- Remaining open: `22`.
- Current target: `Phase 8 live CameraButton hover/press proof, then Phase 9 PlayMode validation`.
- Camera button status: `CameraButton is serialized on MatchHudSelectionPanelView, included in hit-test suppression, bound through SelectionGameplayStartupSystemHelper, and queues TacticalFollowCameraRequestElement.ToggleFollowMode through ISelectionUiCommand.`
- Follow mode status: `TacticalFollowCameraModeSystemHelper consumes ToggleFollowMode/Exit requests in the existing selection runtime tick, resolves focused/single-unit/group/building targets into TacticalFollowCameraTargetComponent, computes clamped TacticalFollowCameraPoseComponent, toggles TacticalFollowCameraModeComponent, stores restore pose when the world camera is available, publishes restore/default TacticalFollowCameraPoseComponent on exit, and publishes TacticalFollowCameraUiReadModelComponent.`
- Missile temporary follow status: `temporary follow now scans production GroundMissileProjectileComponent and AirMissileProjectileComponent entities, adopts the first missile whose Source belongs to the followed unit/group, ignores unrelated missiles, keeps the active missile instead of jittering to later missiles, frames missiles with forward look-ahead, holds ground/air impact or projectile-despawn views briefly before returning to the base target, finishes an already-adopted missile safely when the base target is lost, and has focused validation with missiles created by production ground and air launcher systems. Visual PlayMode proof/captures are still open under Phase 9.`
- Input lock status: `PanInputLocked is set/cleared in ECS mode data; RtsSelectionRuntimeCameraSystemHelper now blocks pan request creation, refuses drag start, and clears an already-active drag while tactical follow is locked. Mouse-wheel zoom remains allowed for V1 because the user asked to block pan, not zoom. Focused validation proves command clicks can still issue while tactical-follow pan lock refuses camera dragging.`
- UI visual state status: `prefab CameraButton has SpriteSwap transparent-normal/highlight/pressed/selected/disabled states, remains the actual clickable root with no hidden child hotspot button, applies selected state from TacticalFollowCameraUiReadModelComponent, applies enabled/disabled state from TacticalFollowCameraUiReadModelComponent, restores transparent normal state when follow mode exits, and shows one-shot HUD feedback for invalid click, enter, exit, and target-lost cases. Live hover/press visual proof is still open.`
- Validation status: `git diff --check passed; TacticalFollowCameraComponentTests.RunFocusedValidation passed with [TacticalFollowCameraComponentValidation] result=Passed tests=3; RtsSelectionInputSystemTests.RunFocusedValidation passed with [RtsSelectionInputSystemValidation] result=Passed tests=58; MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation passed with [MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=9; TacticalFollowCameraModeCommandSystemHelperTests.RunFocusedValidation passed with [TacticalFollowCameraModeCommandValidation] result=Passed tests=22; RtsCameraSystemTests.RunFocusedValidation passed with [RtsCameraFocusedValidation] result=Passed tests=14. Latest logs: /private/tmp/warline-tactical-follow-feedback-validation.log, /private/tmp/warline-tactical-follow-camera-button-visual-validation-2.log, /private/tmp/warline-tactical-follow-click-safety-validation-2.log, /private/tmp/warline-tactical-follow-production-launcher-validation.log. Non-blocking UnityConnect/package-test asmdef shutdown noise remains.`
- Still wrong / next iteration: `Follow mode now resolves unit/group/building target data, queues clamped tactical follow poses through the existing RTS camera request path, restores the saved pre-follow camera pose on exit, blocks pan/drag while active without blocking command clicks, has a documented PlayMode smoothness proof path, adopts only followed-source missiles as temporary camera targets, frames missile follow with forward look-ahead, returns to the base target after ground/air impact or projectile despawn, exits safely if the base target is lost during missile follow, validates CameraButton sprite-state wiring and read-model enabled/selected application against the SCN08 prefab, publishes one-shot feedback for no-selection/enter/exit/target-lost, and is validated against missiles fired by production ground/air launcher systems. Actual PlayMode proof execution/captures remain open under Phase 9. Mixed selection, destroyed target, passenger/onboard target-loss handling, and live CameraButton hover/press visual proof remain open.`
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.

## User-Facing Behavior Contract

### Enter Follow Mode

1. Player selects one unit, multiple units, or a building.
2. `CameraButton` is enabled when there is a valid player-commandable selected target.
3. Player clicks `CameraButton`.
4. The existing world camera smoothly transitions to a third-person tactical follow pose:
   - one mobile unit: behind and above the unit, looking toward the unit's forward/movement direction;
   - multiple selected units: behind and above the selection centroid, framed to keep the group visible;
   - selected building: angled view around the building bounds center.
5. `CameraButton` stays visually selected while follow mode is active.
6. Camera panning/dragging/edge-pan/keyboard-pan is blocked while follow mode is active.

### Exit Follow Mode

1. Player clicks `CameraButton` again.
2. Follow mode exits.
3. The camera smoothly returns to the previous/default RTS camera pose or a stable top-down RTS pose centered near the followed target if the previous pose is no longer valid.
4. `CameraButton` returns to normal visual state.
5. Normal camera pan input is restored.

### Missile Temporary Follow

1. Follow mode is active on one or more selected launchers.
2. A followed selected ground missile launcher or air missile launcher fires using the existing production launcher systems.
3. The camera temporarily follows that missile entity/visual:
   - follow only missiles whose `Source` is part of the followed base target selection;
   - do not steal the camera for unrelated missiles elsewhere in the match;
   - use a trailing, readable camera offset with look-ahead;
   - do not roll the camera aggressively.
4. On missile impact/explosion/despawn, the camera holds long enough for the explosion to read, then returns to the selected unit/group/building base target.
5. If several missiles launch close together, V1 follows the first eligible active missile and ignores/queues others until the current missile resolves. Avoid camera jitter between salvo projectiles.

### Target Loss

- If the followed unit/building is destroyed, despawned, or no longer selected:
  - if another selected valid target remains, resolve a new base target smoothly;
  - otherwise exit follow mode and restore normal RTS camera input.
- If a temporary missile target is destroyed without an impact request, treat it as resolved and return to the base target.
- If the selected target becomes hidden/passenger/onboard and has no meaningful world pose, exit follow mode unless a visible transport/building target can be resolved through production state.

## Architecture Contract

- Follow `Design/Architecture/gameplay_solid_ecs_contract.md`.
- Do not add UI Toolkit.
- Do not add MonoBehaviour gameplay `Update()` loops.
- Do not create a second gameplay camera system or a parallel cinematic simulation.
- Do not mutate unit, building, launcher, missile, selection, or damage gameplay from UI code.
- Do not use `GameObject.Find`, hierarchy-string lookup, `Camera.main`, singleton service locators, or static mutable gameplay state.
- Use existing ECS source data where practical:
  - selection tags/read-models for the base target;
  - `GroundMissileProjectileComponent.Source`;
  - `AirMissileProjectileComponent.Source`;
  - `GroundMissileImpactRequestComponent`;
  - `AirMissileImpactRequestComponent`;
  - `LocalTransform`/bounds/presentation data for target position;
  - `RuntimeCameraReferenceComponent` and existing camera reference plumbing for the actual world camera reference.
- Keep UI `*View` classes as serialized-reference binders and visual-state applicators only.
- Camera mode decisions, target resolution, pan lock, and temporary missile target ownership must be ECS data/system owned.
- The managed camera edge may update the Unity `Camera` transform, but the gameplay-facing mode state must remain ECS-readable.
- New plain managed helpers, if required at the camera edge, must use an approved reason suffix such as `CameraSystemHelper` or `UiSystemHelper`; bare `*System` remains reserved for Unity ECS systems.

## Existing Relevant Owners

| Area | Existing owner | Planned use |
| --- | --- | --- |
| Camera request queue | `RtsCameraRequestSystem` and `RtsCameraRequestElement` | Extend or reuse request flow for smooth target/pose updates and pan suppression. |
| Camera transform edge | `RtsCameraSystem` | Existing managed camera transform owner; do not bypass with another camera controller. |
| Runtime camera reference | `RuntimeCameraReferenceComponent` / `RuntimeCameraReferenceSystem` | Resolve the active world camera through existing ECS-managed reference path. |
| Selected-panel view | `MatchHudSelectionPanelView` | Add serialized `CameraButton` reference and visual application only. |
| Selected-panel command contract | `ISelectionUiCommand` / `SelectionUiCommandUiSystemHelper` | Add request method that enqueues ECS intent/request; UI must not toggle mode locally. |
| Selection input state | `RtsSelectionInputStateComponent` | Read/coordinate command mode and click suppression where needed. |
| Ground missile projectile | `GroundMissileProjectileComponent` | Use `Source` to decide whether projectile belongs to followed launcher. |
| Air missile projectile | `AirMissileProjectileComponent` | Use `Source` to decide whether projectile belongs to followed launcher. |
| Missile impact requests | `GroundMissileImpactRequestComponent`, `AirMissileImpactRequestComponent` | Resolve temporary missile follow and hold explosion beat before returning. |
| Camera pan input | `RtsSelectionRuntimeCameraSystemHelper`, `RtsSelectionRuntimeInputCompositionSystemHelper` | Main pan requests funnel through `PanCamera`, `QueuePan`, drag state, build-mode pan, fullscreen-iso pan, and selection-runtime drag fallback. |
| Focused unit read model | `FocusedUnitUiReadModelComponent` | Already exposes focused entity, player ownership, world position, portrait world position, and portrait forward for single-target follow. |
| Selected unit group | `SelectedUnitTag` queries with `LocalTransform` | Existing selection systems and tests use this tag as the source for multi-unit command selection. |
| Selected building edge | `BuildingPlacementInteractionBoundaryCompositionSystemHelper` / `RuntimeBuildingSystem` | Existing selected-building state is still managed-boundary debt; follow mode must use the existing boundary without moving building policy into UI. |
| Focused tests | `RtsSelectionInputSystemTests`, `MatchHudCommandControlsCurrentPrefabTests`, `RuntimeCameraReferenceSystemTests`, new focused camera-follow tests | Reuse these patterns for command request, prefab button wiring, runtime camera reference, and ECS mode state validation. |

## Proposed ECS Data Model

Exact field names can be adjusted during implementation, but the ownership shape should stay stable.

### `TacticalFollowCameraModeComponent`

Singleton-style component on the existing camera/request entity.

- `Enabled`
- `BaseTargetKind`
- `BaseTargetEntity`
- `HasBaseTarget`
- `TemporaryTargetKind`
- `TemporaryTargetEntity`
- `HasTemporaryTarget`
- `ModeEnteredFrame`
- `TemporaryTargetStartedTime`
- `ReturnHoldUntilTime`
- `PanInputLocked`
- `RestorePoseValid`
- previous camera pose fields needed to return smoothly

### `TacticalFollowCameraRequestElement`

Request buffer consumed by a camera-mode command system.

- `ToggleFollowMode`
- `ExitFollowMode`
- `SetBaseTarget`
- `RefreshBaseTarget`
- `SetTemporaryMissileTarget`
- `ClearTemporaryTarget`
- `RestoreDefaultCamera`

### `TacticalFollowCameraTargetComponent`

Optional computed target data for the current frame or singleton camera target.

- target center
- look-at point
- forward hint
- bounds radius
- desired camera distance
- desired camera height
- target validity

### `TacticalFollowCameraPoseComponent`

Computed desired camera pose before the managed camera edge applies it.

- desired position
- desired rotation/look-at
- field of view or orthographic mode target
- position damping time
- rotation damping time
- maximum transition speed
- pose source: base target, temporary missile, restore/default

### `TacticalFollowCameraUiReadModelComponent`

Read model for Canvas state.

- `Visible`
- `Enabled`
- `Selected`
- optional tooltip/feedback reason code

## System Ownership Plan

| System/helper | Type | Responsibility |
| --- | --- | --- |
| `TacticalFollowCameraCommandSystem` | ECS `ISystem` preferred | Consume UI toggle/exit requests, validate selection, set/clear mode components. |
| `TacticalFollowCameraTargetResolveSystem` | ECS `ISystem` preferred | Resolve single unit, group centroid, or building bounds from ECS selection/read-model data. |
| `TacticalFollowCameraMissileTargetSystem` | ECS `ISystem` preferred | Detect eligible missiles fired by followed selected launchers and publish temporary target state. |
| `TacticalFollowCameraPoseSystem` | ECS `ISystem` preferred where possible | Convert base/temporary target data into desired camera pose data. |
| `TacticalFollowCameraInputLockSystem` | ECS `ISystem` or narrow camera helper | Blocks/ignores pan requests while follow mode is active. |
| `TacticalFollowCameraUiReadModelSystem` | ECS/UI read-model system | Publishes CameraButton enabled/selected state for the Canvas view. |
| `TacticalFollowCameraCameraSystemHelper` | Managed camera edge only if required | Applies smoothed pose to the Unity `Camera` through existing camera reference/request plumbing. No gameplay policy. |
| `MatchHudSelectionPanelView` changes | UI View | Serialized button reference, bind/unbind event, apply visual selected/enabled state. No mode policy. |

## Implementation Phases

## Phase 0: Tracker, Audit, And Contract Lock

Purpose:
Lock the design before code so the implementation does not drift into parallel gameplay or UI-local state.

- [x] Create this tracker.
- [x] Audit existing RTS camera request path: `RtsCameraRequestSystem` and `RtsCameraSystem` already own camera requests, pan, smooth focus, and transform updates.
- [x] Audit existing selected-panel command pattern: `MatchHudSelectionPanelView`, `ISelectionUiCommand`, and `SelectionUiCommandUiSystemHelper` already show the Board button pattern.
- [x] Audit missile source data: `GroundMissileProjectileComponent.Source` and `AirMissileProjectileComponent.Source` can identify launcher ownership for temporary missile follow.
- [x] Inventory the current camera input loop and exact pan/drag/edge-pan request sources to block them without blocking unrelated UI clicks.
- [x] Inventory selected unit, selected group, and selected building read-model/component sources for reliable base target resolution.
- [x] Inspect the user-added `CameraButton` serialized reference state in the Canvas prefab/scene.
- [ ] Finalize the exact ECS component/buffer field names before code.
- [x] Identify focused tests and existing test assembly boundaries for camera-mode validation.
- [x] Update this tracker if discovery shows a better existing owner than the proposed names above.

## Phase 1: ECS Contract And Data Model

Purpose:
Add data-only ECS contracts for follow mode without touching camera motion yet.

- [x] Add or extend camera-mode request components/buffers with a `ToggleFollowCamera` request.
- [x] Add `TacticalFollowCameraModeComponent` or equivalent data-only mode component.
- [x] Add a computed target component or singleton data structure for base/temporary target state.
- [x] Add a computed pose component or request payload for desired camera pose.
- [x] Add a UI read-model component for `CameraButton` enabled/selected state.
- [x] Keep runtime component names ending in `Component` per `gameplay_solid_ecs_contract.md`.
- [x] Avoid adding Unity object references to unmanaged gameplay components.
- [x] Place code under existing appropriate asmdef boundaries; do not fall back to broad default assembly behavior.
- [x] Add component-level tests for default values and request-buffer creation if the repo has a matching pattern.
- [x] Run compile/focused validation after this phase.

## Phase 2: CameraButton View And Command Wiring

Purpose:
Bind the user-added Canvas button into the existing command boundary while keeping view responsibilities narrow.

- [x] Add a serialized `Button cameraAction` field to `MatchHudSelectionPanelView`.
- [x] Add bind/unbind logic for the button using the existing Board button pattern.
- [x] Add `ContainsScreenPoint` coverage for the CameraButton so world clicks do not leak through.
- [x] Add `BindActions` or a narrow binding method extension for camera follow requests without breaking existing callers.
- [x] Add `SetCameraActionSelected(bool selected)` and enabled-state application mirroring the Board selected visual approach.
- [x] Preserve default, hover/highlight, pressed, disabled, and selected sprites/colors on the Button/SpriteState.
- [x] Add `RequestToggleTacticalFollowCameraMode()` or equivalent to `ISelectionUiCommand`.
- [x] Implement the UI command helper method by capturing the UI click sequence and enqueueing an ECS request/intent.
- [x] Validate that clicking the button does not also select/move/attack in the world on release.

## Phase 3: Follow Mode Toggle Command

Purpose:
Turn button clicks into authoritative ECS mode state.

- [x] Add a camera follow command intent/request kind.
- [x] On toggle request with no valid selection, reject and publish feedback such as `Select a unit or building to follow.`
- [x] On toggle request while inactive and selection is valid, enter follow mode.
- [x] On toggle request while active, exit follow mode.
- [x] Store the current/default camera pose for smooth restore.
- [x] Set `PanInputLocked` when mode enters.
- [x] Clear `PanInputLocked` when mode exits.
- [x] Publish UI selected state from ECS read-model data, not local UI booleans.
- [x] Add tests for enter, reject, exit, and selected-read-model state.

## Phase 4: Base Target Resolution

Purpose:
Resolve the target the camera follows before adding missile handoff.

- [x] Single selected mobile unit resolves to that entity's world pose and forward/movement direction.
- [x] Multiple selected units resolve to group centroid, bounds radius, and dominant forward/movement hint.
- [x] Selected building resolves to building bounds center and a stable angled follow pose.
- [ ] Mixed unit/building selection chooses a deterministic primary target or group-center rule and documents it here.
- [ ] Base target refreshes when selection changes while follow mode is active.
- [ ] If the current base target is destroyed/despawned, choose another valid selected target or exit mode.
- [ ] If the base target becomes hidden/onboard/passenger, choose an explicit fallback or exit mode.
- [ ] Avoid per-frame managed allocations when collecting selected targets.
- [ ] Add focused tests for unit, group, building, mixed, destroyed, and no-selection cases.

## Phase 5: Smooth Camera Pose And Restore

Purpose:
Make the existing world camera move smoothly into/out of follow mode.

- [x] Define V1 camera geometry constants in config or a narrow data owner, not magic values scattered through code.
- [x] Unit follow pose: behind/above target, look at target center plus readable vertical offset.
- [x] Group follow pose: distance scales with selection radius so all selected units stay visible.
- [x] Building follow pose: stable angled pose around bounds, not too close to roof/collider geometry.
- [x] Smooth transition into follow mode with position and rotation damping.
- [x] Keep camera horizon readable; avoid roll.
- [x] Clamp camera distance/height to prevent clipping through terrain, buildings, or unit meshes.
- [x] Restore default camera smoothly on exit.
- [x] Keep camera motion implementation at the camera edge; target/pose decisions stay ECS-owned.
- [x] Add PlayMode/manual validation path for visual smoothness and no sudden jumps.

## Manual PlayMode Smoothness Proof Path

Use this path when Phase 9 reaches manual validation. It is a proof procedure, not a parallel gameplay path.

1. Open the production match scene or the current isolated validation scene that uses the production RTS camera, selection, and Canvas Match HUD.
2. Enter Play Mode with the Canvas HUD active and UI Toolkit disabled/absent.
3. Select one mobile unit, click `CameraButton`, and verify the camera eases into a readable behind/above view with no instant snap, roll, ground clipping, or mesh clipping.
4. While follow mode is active, try drag pan, keyboard pan, and edge pan; the camera must not pan, and the selected `CameraButton` state must stay visible.
5. Click `CameraButton` again and verify the camera smoothly returns to the saved RTS pose, then pan input works again.
6. Repeat the enter/exit check with a multi-unit selection; the group must stay framed and the camera must keep a readable horizon.
7. Repeat the enter/exit check with a selected building; the pose must stay outside the building bounds and preserve enough height/distance to avoid roof/collider clipping.
8. Capture one still or short video for each target kind and record the artifact path in Phase 9 before marking the manual validation gates complete.

## Phase 6: Missile Temporary Follow

Purpose:
Follow fired missiles only when they belong to the selected/followed source.

- [x] Detect new `GroundMissileProjectileComponent` entities whose `Source` is in the followed base target set.
- [x] Detect new `AirMissileProjectileComponent` entities whose `Source` is in the followed base target set.
- [x] Ignore missiles from unrelated launchers.
- [x] When no temporary target is active, adopt the first eligible missile as the temporary target.
- [x] While a temporary target is active, do not jitter to later missiles unless a deliberate queue policy is added.
- [x] Missile follow pose trails the projectile with look-ahead and readable explosion framing.
- [x] On `GroundMissileImpactRequestComponent` or projectile despawn, hold explosion view briefly, then return to base target.
- [x] On `AirMissileImpactRequestComponent` or projectile despawn, hold explosion view briefly, then return to base target.
- [x] If base target is lost during missile follow, finish missile follow then restore/exit safely.
- [x] Add scenario/manual validation with ground missile launcher and air missile launcher firing while follow mode is active.

## Phase 7: Pan Lock And Input Safety

Purpose:
Prevent RTS pan while follow mode is active without breaking UI or command input.

- [x] Locate every path that queues camera pan/drag/edge-pan/keyboard-pan requests.
- [x] Gate pan request creation or processing with the ECS follow-mode `PanInputLocked` state.
- [x] Allow CameraButton click to exit follow mode while pan is locked.
- [x] Decide whether mouse wheel zoom remains allowed; document the decision before implementation.
- [x] Ensure selection/command clicks still work if the design allows changing selection while follow mode is active.
- [x] Clear dragging state on follow-mode entry to avoid stuck drag velocity.
- [x] Add tests/manual checks that pan input is blocked only during follow mode and restored afterward.

## Phase 8: UI Visual State And Feedback Polish

Purpose:
Make the CameraButton feel like the approved command buttons.

- [x] Use the same Target Lock Canvas command-button sprite family as the other Match HUD command buttons.
- [x] Assign normal sprite.
- [x] Assign highlighted/hover sprite.
- [x] Assign pressed/impact sprite or transition.
- [x] Assign selected/current sprite.
- [x] Assign disabled sprite/color state.
- [x] Verify selected state persists while follow mode is active.
- [ ] Verify hover and press states still work when inactive.
- [x] Verify disabled state when there is no selected followable target.
- [x] Add short feedback for invalid click, enter, exit, and target-lost cases.
- [x] Ensure the button remains the actual clickable root; do not add hidden child hotspot buttons.
- [ ] Save focused visual proof if the button prefab needs sprite/PPU/9-slice tuning.

## Phase 9: Validation And Regression Gates

Purpose:
Prove behavior before handing over.

- [x] Run `git diff --check`.
- [x] Run focused compile/EditMode validation for changed systems.
- [x] Add/extend tests for ECS request enqueue and mode state transitions.
- [x] Add/extend tests for UI read-model selected/enabled state.
- [ ] Add/extend tests for base target resolution.
- [ ] Add/extend tests for temporary missile follow source filtering.
- [ ] Add/extend tests for return-after-impact behavior.
- [ ] Validate in Play Mode that clicking CameraButton enters and exits smoothly.
- [ ] Validate pan/drag/edge-pan/keyboard pan are blocked while active.
- [ ] Validate a ground missile launcher shot temporarily follows the missile then returns.
- [ ] Validate an air missile launcher shot temporarily follows the missile then returns.
- [ ] Record proof artifacts/log paths in this tracker.

## Phase 10: Rollout And Cleanup

Purpose:
Finish the feature without leaving partial states.

- [ ] Update this tracker progress snapshot after every implementation slice.
- [ ] Remove or document any temporary diagnostics.
- [ ] Confirm no UI Toolkit references were added.
- [ ] Confirm no MonoBehaviour gameplay `Update()` loops were added.
- [ ] Mark feature complete only after UI, input lock, missile follow, and return behavior are validated.

## Validation Commands

Use exact commands after implementation depending on the touched files and available Unity state.

- `git diff --check`
- Focused EditMode tests for camera/follow command systems once test names exist.
- Shadow-project validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` when the main project is locked or the main scene should not be disturbed.
- Main-project Play Mode/manual validation when camera behavior must be verified in the actual match scene.

## Design Decisions To Revisit During Implementation

- Whether zoom remains available while follow mode is active. Initial recommendation: block pan only, allow conservative wheel zoom if it does not break framing.
- Whether selection changes while follow mode is active should retarget immediately or require exit/re-enter. Initial recommendation: retarget smoothly to the new valid selection.
- Whether missile salvo behavior should ignore extra missiles or queue one follow target. Initial recommendation: follow the first eligible missile and ignore additional missiles until return to avoid jitter.
- Whether building follow should use current camera heading or authored building forward. Initial recommendation: use authored/world forward if reliable, otherwise preserve current camera heading.
- Whether the restore target should be the exact previous camera pose or an RTS top-down pose centered on the followed target. Initial recommendation: exact previous pose if still reasonable, centered RTS fallback otherwise.
