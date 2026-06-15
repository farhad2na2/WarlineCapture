# Hold, Stop, And Scan Commands Implementation Plan

## Status

Overall status: Pending

This document tracks the implementation of Hold, Stop, and Scan commands for all controllable units. It is written as a progress tracker: each phase should be updated to `Pending`, `In Progress`, `Complete`, or `Blocked` as work lands.

## Sources Reviewed

- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/Match_Selection_Implementation_Spec.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- Current command code paths:
  - `Assets/Game/Scripts/Systems/SelectionUiCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/FocusedUnitCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionScanTargetModeCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/ScanIntelCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/SelectionHudFeedbackBoundary.cs`
  - `Assets/Game/Scripts/Systems/UnitEngagementSystem.cs`

Note: `Design/Match_Unit_Command_Behavior_Spec.md` was requested but is not present in this checkout. This plan uses the two available match specs and the architecture contract as the canonical sources.

## Goal

Implement reliable, architecture-aligned Hold, Stop, and Scan commands for soldiers, ground vehicles, air units, and future controllable unit categories.

The final behavior must:

- Route HUD and command-wheel clicks through `SelectionUiCommandSystem`.
- Store player command intent in ECS request buffers, not UI objects.
- Process command behavior in command-specific ECS systems.
- Publish HUD feedback through `SelectionHudFeedbackBoundary` and ECS feedback data.
- Keep camera panning, pointer suppression, targeting modes, and world clicks separated through `RtsSelectionRuntimeInputSystem`.
- Prefer `ISystem`, Burst, and jobs for hot behavior. Managed boundary classes are allowed only at UI/runtime seams or as temporary compatibility while converting existing code.

## Architecture Contract

### Non-Negotiable Rules

- Do not introduce a broad command manager, gameplay facade, or controller.
- Do not put gameplay state in HUD views, UI buttons, MonoBehaviours, or cached child UI paths.
- Do not call selection or command mutation directly from `MenuView`, `MainMenuPlayUI`, or match overlay child views.
- Do not reintroduce pointer-to-gameplay decisions into the runtime loop. Pointer orchestration belongs to `RtsSelectionRuntimeInputSystem`.
- Do not bypass the ECS feedback queue for HUD command mode/result feedback.
- Do not change pathfinding constants, traversal costs, search limits, or path request scheduling as part of this feature.

### Ownership

- UI command enqueueing belongs to `SelectionUiCommandSystem` and `ISelectionUiCommand`.
- Pointer press/release, camera drag, active targeting modes, and world click routing belong to `RtsSelectionRuntimeInputSystem`, backed by `RtsSelectionInputStateComponent` and command intent buffers.
- Immediate selected-unit command mutations for Hold and Stop belong to `FocusedUnitCommandSystem` and/or `RtsSelectionImmediateSelectedUnitCommandSystem`.
- Scan target-mode entry belongs to `RtsSelectionScanTargetModeCommandSystem`.
- Scan execution, reveal writes, and scan feed entries belong to `ScanIntelCommandSystem` or narrower scan/intel systems.
- Auto-engage behavior for hold-position units belongs to combat systems such as `UnitEngagementSystem`.
- HUD command mode, result, and marker visibility feedback belong to `SelectionHudFeedbackBoundary`, `SelectionHudFeedbackElement`, and runtime feedback views.
- Move/attack/scan world markers belong to `SelectionOrderMarkerSystem`.

## Command Semantics

### Stop

Stop is an immediate selected-unit command.

Expected behavior:

- Requires at least one selected controllable unit.
- Clears active world targeting modes and queued move order state.
- Clears movement/path/order state for all selected eligible units.
- Clears commanded attack/engage state when the current order is interruptible.
- Stops current vehicle speed and in-flight air attack/return/taxi transient state where applicable.
- Leaves the unit selected.
- Publishes `TacticalCommandMode.Stop` and a success result such as `Stopped selected units.`
- Rejects with `NoSelection` when no eligible selected units exist.

Eligibility:

- Soldiers: stop path following, current move requests, manual group movement, auto-wander, and active engage target if interruptible.
- Ground vehicles: same as soldiers plus vehicle kinematic speed/stall reset.
- Air units: stop/abort current non-terminal command state through explicit air-policy cleanup. Do not strand aircraft in impossible runway, attack-run, or return-home state.

### Hold

Hold is an immediate selected-unit command that anchors units at their current position and allows defensive fire.

Expected behavior:

- Requires at least one selected controllable unit.
- Clears active movement/path/order state.
- Adds or updates hold-position state.
- Keeps `UnitCombat.AutoEngage` enabled for units that can attack.
- Prevents units from chasing beyond their allowed hold leash.
- Allows defensive fire within attack range and configured local acquisition radius.
- Leaves the unit selected.
- Publishes `TacticalCommandMode.Hold` and a success result such as `Holding current position.`
- Rejects with `NoSelection` when no eligible selected units exist.

Current implementation note:

- `HoldPositionOrderTag` exists and `UnitEngagementSystem` already reduces acquisition to attack range for holding units. The implementation should validate whether a data-only hold anchor/leash component is needed beyond the current tag. If units still drift or chase too far, replace or extend the tag with a data component such as `HoldPositionOrder`.

Recommended data shape if needed:

```csharp
public struct HoldPositionOrder : IComponentData
{
    public int2 AnchorCell;
    public float3 AnchorWorld;
    public float LeashRadius;
}
```

### Scan

Scan is a targeted intel command. Per the HUD spec, it is selection-independent by default unless a mission/source rule explicitly requires a selected scanner.

Expected behavior:

- Command button enters `TacticalCommandMode.Scan`.
- Camera panning remains available while in scan targeting mode.
- Valid world tap executes scan at the tapped cell/world position.
- Invalid tap rejects with `TargetOutOfBounds` or `ScanUnavailable`.
- Valid scan reveals hidden/unknown enemy units, buildings, hazards, or mission-authored intel inside radius.
- Valid scan emits a visible scan marker and/or minimap/intel feedback.
- Scan execution spends charges/resources and starts cooldown only after target validation succeeds, if those systems are enabled.
- One-shot scan targeting clears after execution unless a future mission explicitly enables repeat scan.

Current implementation note:

- `EnterScanTargetMode`, `Scan`, `ScanIntelCommandRequestElement`, `ScanIntelCommandResultElement`, `ScanIntelFeedEntry`, and `ScanIntelCommandSystem` already exist.
- `ScanIntelCommandSystem` currently processes candidate collection on the main thread. The plan should first harden correctness, then convert candidate collection/reveal evaluation to jobs if scans can touch large entity counts.

## Progress Tracker

| Phase | Status | Owner | Validation / Notes |
| --- | --- | --- | --- |
| 0. Contract and code audit | Complete | Gameplay | Validated by reading specs and current systems while creating this document. |
| 1. Capability and reason-code audit | Pending | Gameplay | Confirm buttons expose correct enabled/disabled state and typed reasons. |
| 2. Stop command hardening | Pending | Gameplay | Requires unit tests/play validation for soldier, vehicle, and air units. |
| 3. Hold command hardening | Pending | Gameplay | Requires hold leash/auto-engage validation. |
| 4. Scan command targeting and input | Pending | Gameplay | Requires camera pan + scan tap validation. |
| 5. Scan execution and intel feedback | Pending | Gameplay | Requires reveal/feed/minimap/marker validation. |
| 6. ECS/job migration pass | Pending | Gameplay | Convert hot scan/hold/stop loops only where measurable. |
| 7. HUD and marker polish | Pending | Gameplay/UI | Ensure command mode/result/markers are clear and consistent. |
| 8. Tests and visual QA | Pending | Gameplay/QA | Record exact commands and log paths here when complete. |

## Phase 0 - Contract And Code Audit

Status: Complete

Checklist:

- [x] Review available HUD implementation spec.
- [x] Review available selection implementation spec.
- [x] Review gameplay ECS architecture contract.
- [x] Confirm requested `Design/Match_Unit_Command_Behavior_Spec.md` is not present.
- [x] Identify current command request enum entries for `HoldPosition`, `Stop`, `EnterScanTargetMode`, and `Scan`.
- [x] Identify existing HUD feedback and scan systems.

Notes:

- Existing command intent and feedback data are a good base.
- Hold/stop currently have an immediate selected-unit path that should be hardened and then migrated toward a narrower `ISystem`/job path if needed.
- Scan already has ECS request/result data and an `ISystem`, but reveal collection should be reviewed for job suitability.

## Phase 1 - Capability And Reason-Code Audit

Status: Pending

Implementation checklist:

- [ ] Confirm HUD buttons call only `ISelectionUiCommand` methods.
- [ ] Confirm `RequestHoldPosition`, `RequestStop`, and `RequestScanCommandMode` enqueue ECS intent requests.
- [ ] Confirm command bar and command wheel use the same capability model.
- [ ] Add or update read-model fields for whether Hold, Stop, and Scan are enabled.
- [ ] Ensure disabled reasons use typed `TacticalCommandReasonCode` values.
- [ ] Confirm scan can be enabled without selection when mission rules allow it.
- [ ] Confirm Stop is enabled only when selected units have interruptible state or active orders, unless design chooses always-enabled Stop.
- [ ] Confirm Hold is enabled for selected units that can accept hold-position behavior.

Acceptance criteria:

- HUD never decides command behavior locally.
- Command buttons display consistent enabled/disabled state.
- Rejections are typed and routed through `SelectionHudFeedbackBoundary`.

Suggested tests:

- `NoSelection` for Hold and Stop.
- Scan availability with no selected unit.
- Scan unavailable/cooldown/resource failure, if those config fields exist.

## Phase 2 - Stop Command Hardening

Status: Pending

Implementation checklist:

- [ ] Keep command request entry point in `SelectionUiCommandSystem.RequestStop`.
- [ ] Keep command intent kind as `RtsSelectionCommandIntentKind.Stop`.
- [ ] Process Stop in `RtsSelectionImmediateSelectedUnitCommandSystem` or a narrower `ISystem` owned by the immediate selected-unit command domain.
- [ ] Clear active command mode through `RtsSelectionInputStateComponent`.
- [ ] Clear queued move order and pending move command requests.
- [ ] Clear movement/path components through existing movement-order cleanup helpers.
- [ ] Clear or abort attack/engage components according to command policy.
- [ ] Reset `UnitVehicleKinematics` for ground vehicles.
- [ ] Reset safe, explicit `UnitAirComponent` transient states for aircraft without deleting valid home/runway state.
- [ ] Keep `SelectedUnitTag` on units.
- [ ] Publish a command result with accepted count.
- [ ] Add diagnostics only where they help validate command flow; remove temporary logs before completion.

Acceptance criteria:

- Selected soldiers stop moving immediately and do not continue queued group paths.
- Selected vehicles stop moving immediately and do not retain velocity.
- Selected air units abort the active order without entering invalid taxi/takeoff/landing state.
- Stop does not deselect units.
- Stop does not block camera panning on subsequent pointer input.

Suggested tests:

- EditMode test: Stop with no selection returns `NoSelection`.
- EditMode test: Stop removes movement/path/order components from selected entity.
- EditMode test: Stop clears air transient attack/runway state without destroying required air components.
- Play validation: select mixed squad, issue move, then Stop.

## Phase 3 - Hold Command Hardening

Status: Pending

Implementation checklist:

- [ ] Keep command request entry point in `SelectionUiCommandSystem.RequestHoldPosition`.
- [ ] Keep command intent kind as `RtsSelectionCommandIntentKind.HoldPosition`.
- [ ] Process Hold in the immediate selected-unit command domain.
- [ ] Decide whether `HoldPositionOrderTag` is sufficient or replace/extend with data component `HoldPositionOrder`.
- [ ] If using `HoldPositionOrder`, write current cell/world anchor and leash radius when Hold is issued.
- [ ] Remove active movement/path/group/auto-wander components.
- [ ] Enable `UnitCombat.AutoEngage` for attack-capable units.
- [ ] Update `UnitEngagementSystem` so hold units acquire targets only inside hold rules.
- [ ] Update engaged movement behavior so hold units do not chase past their leash.
- [ ] Confirm air-unit hold policy: aircraft should either loiter at current safe altitude/anchor or hold current ground/runway state. Do not force runway behavior unless explicitly ordered.
- [ ] Publish command mode/result feedback.

Acceptance criteria:

- Soldiers hold the current area and fire defensively without long chasing.
- Ground vehicles hold the current area and fire defensively without long chasing.
- Air units do not break runway/attack-run state when Hold is issued.
- Hold does not deselect units.
- Hold result is visible in HUD feedback.

Suggested tests:

- EditMode test: Hold adds hold component/tag and clears movement path.
- EditMode test: Hold keeps auto-engage enabled on attack-capable units.
- EditMode test: holding unit only acquires enemies inside allowed hold radius.
- Play validation: hold mixed selected group, spawn enemy inside/outside hold radius.

## Phase 4 - Scan Targeting And Input

Status: Pending

Implementation checklist:

- [ ] Keep scan command button entry in `SelectionUiCommandSystem.RequestScanCommandMode`.
- [ ] Keep scan target-mode entry in `RtsSelectionScanTargetModeCommandSystem`.
- [ ] Ensure entering Scan clears conflicting Move/Attack/Board targeting modes.
- [ ] Ensure entering Scan does not enter selection rectangle mode.
- [ ] Ensure camera panning remains active while scan targeting is armed.
- [ ] Confirm tap release after pressing the Scan button is suppressed so the button click does not also scan.
- [ ] Route scan world tap through `RtsSelectionPointerTargetCommandSystem.TryIssueScanOrder`.
- [ ] Queue scan with either pre-resolved cell/world data or screen position using existing command intent buffer.
- [ ] Reject out-of-bounds target with `TargetOutOfBounds`.
- [ ] Clear one-shot scan mode after successful scan or rejected world tap according to the spec.

Acceptance criteria:

- Player can pan the camera while Scan mode is active.
- First valid map tap after Scan executes scan once.
- UI tap on the Scan button never leaks into an immediate world scan.
- Invalid map tap shows typed feedback.

Suggested tests:

- Input state test: enter scan sets `ActiveCommandMode == Scan`, one-shot, requires world target.
- Input test: camera drag does not issue scan.
- Input test: valid tap queues `RtsSelectionCommandIntentKind.Scan`.

## Phase 5 - Scan Execution And Intel Feedback

Status: Pending

Implementation checklist:

- [ ] Keep scan execution in `ScanIntelCommandSystem` or narrower scan/intel systems.
- [ ] Validate grid exists before accepting scan.
- [ ] Validate target cell bounds before spending resources/cooldown.
- [ ] Use configured scan radius if a mission/faction/source config exists; otherwise keep documented default.
- [ ] Reveal eligible enemy units inside radius.
- [ ] Reveal eligible enemy buildings inside radius.
- [ ] Add `ScanIntelRevealedTag` and update `ScanIntelLastSeen`.
- [ ] Append `ScanIntelFeedEntry` for accepted scans.
- [ ] Publish `RtsSelectionCommandResultElement` with revealed count, target cell/world, radius, and marker payload.
- [ ] Show a clear scan marker through `SelectionOrderMarkerSystem`.
- [ ] Route HUD result through `SelectionHudFeedbackBoundary`.
- [ ] Update minimap enemy markers if minimap reads revealed intel instead of live visibility.
- [ ] Add cooldown/charges/resource data only after the mission/source config contract is identified.

Acceptance criteria:

- Accepted scan reveals all eligible contacts in radius.
- Scan feedback says how many contacts were revealed or gives a clear success message.
- Scan marker is visible at gameplay camera distance.
- Minimap reflects revealed contacts if minimap visibility depends on scan state.
- Rejected scan does not spend resources/cooldown/charges.

Suggested tests:

- EditMode test: scan out of bounds returns `TargetOutOfBounds`.
- EditMode test: scan reveals enemy unit/building in radius.
- EditMode test: scan ignores friendly or dead entities where appropriate.
- EditMode test: scan appends feed entry.

## Phase 6 - ECS And Job Migration Pass

Status: Pending

Implementation checklist:

- [ ] Convert any new command execution code to `ISystem` by default.
- [ ] Keep components and buffer elements data-only.
- [ ] For Stop/Hold, use `IJobEntity` if selected-unit batch mutation becomes hot or large.
- [ ] For Scan, split target candidate collection into jobs if scan radius/entity count makes main-thread scans costly.
- [ ] Use `EntityCommandBuffer` for structural changes.
- [ ] Avoid managed allocations in per-frame command paths.
- [ ] Avoid `EntityManager.CreateEntityQuery` inside hot per-frame static helpers; prefer cached queries in systems.
- [ ] Add Burst where data access is Burst-compatible.

Acceptance criteria:

- No avoidable GC allocations in command execution hot paths.
- No broad managed command facade introduced.
- Existing request/result flow remains readable and testable.

Suggested tests:

- Run focused EditMode tests for command systems.
- Run profiler/GC validation if command execution is touched heavily.

## Phase 7 - HUD And Marker Polish

Status: Pending

Implementation checklist:

- [ ] Confirm `TacticalCommandFeedbackText` has clear text for Hold, Stop, and Scan.
- [ ] Confirm `BattleHudRuntimeFeedbackBoundary` visual state handles Hold/Stop/Scan.
- [ ] Confirm command mode clears after one-shot Scan.
- [ ] Confirm Hold/Stop feedback is transient or persistent according to HUD spec.
- [ ] Confirm world markers for Scan are consistent with move/attack marker style.
- [ ] Confirm no command marker is tiny, fragmented, or hidden under terrain.
- [ ] Confirm minimap markers update for revealed enemies and selected units.

Acceptance criteria:

- HUD feedback and marker feedback feel like one command family.
- No direct child UI path writes are added.
- Command modes do not leave stale highlighted buttons.

## Phase 8 - Tests And Visual QA

Status: Pending

Validation checklist:

- [ ] Run focused EditMode tests for command contracts.
- [ ] Run focused PlayMode test or manual validation for Stop.
- [ ] Run focused PlayMode test or manual validation for Hold.
- [ ] Run focused PlayMode test or manual validation for Scan.
- [ ] Validate soldiers, vehicles, aircraft, and mixed selections.
- [ ] Validate no-selection rejections.
- [ ] Validate camera panning in Scan/Move/Attack modes.
- [ ] Validate command result HUD feedback.
- [ ] Validate scan/feed/minimap marker visibility.
- [ ] Record exact validation command and log path below.

Validation log:

- Pending.

## Implementation Order

1. Audit and fill capability/read-model gaps first.
2. Harden Stop because it is the simplest immediate command and validates cleanup safety.
3. Harden Hold using Stop cleanup plus hold-specific defensive behavior.
4. Harden Scan targeting/input next because it depends on command mode and camera input behavior.
5. Harden Scan execution/reveal/feed/marker behavior.
6. Run ECS/job migration only after behavior is correct.
7. Polish HUD/marker feedback.
8. Add tests and update this tracker as phases complete.

## Open Questions For Implementation

- Does the mission design want Scan to be globally available, faction-owned, building-owned, or unit-owned?
- Should Hold be a toggle that clears hold on second press, or always issue a fresh hold order?
- Should Stop clear `EngageTarget` for all units, or only commanded/interruptible engage targets?
- For air units, should Hold mean loiter in the air, hold on runway/ground, or cancel current order and return to base?
- What is the production scan radius/cooldown/charge economy?

Until those are answered, implement conservative defaults:

- Scan is selection-independent when mission allows it.
- Hold always issues/refreshes hold position, not toggle.
- Stop clears current active orders and interruptible engage state.
- Air-unit Stop/Hold must preserve valid aircraft identity/home/runway data and avoid invalid transient state.
