# Hold, Stop, And Scan Commands Implementation Plan

## Status

Overall status: Pending

This document tracks the implementation of Hold, Stop, and Scan commands for all controllable units. It is written as a progress tracker: each phase should be updated to `Pending`, `In Progress`, `Complete`, or `Blocked` as work lands.

## Sources Reviewed

- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/Match_Selection_Implementation_Spec.md`
- `Design/Match_Unit_Command_Behavior_Spec.md`
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

Note: `Design/Match_Unit_Command_Behavior_Spec.md` was missing when this tracker was first created. It has now been added as the detailed child behavior spec for Hold, Stop, and Scan.

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

Hold is an immediate selected-unit command that anchors units at their current position and allows defensive fire. It is not a passive "do nothing" state: if an enemy attacks or enters the unit's defensive range, the holding unit should fire back without chasing far from its anchor.

Expected behavior:

- Requires at least one selected controllable unit.
- Clears active movement/path/order state.
- Adds or updates hold-position state.
- Keeps `UnitCombat.AutoEngage` enabled for units that can attack.
- Prevents units from chasing beyond their allowed hold leash.
- Allows defensive fire within attack range and configured local acquisition radius.
- Allows retaliation when attacked, but clamps pursuit to the hold leash.
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

Scan is a targeted recon command. It has two valid sources:

- If a selected unit has recon/scan capability, Scan is a unit order: the unit patrols or performs a recon pass over the selected area, reveals contacts, and engages enemies it finds according to its combat rules.
- If no selected scanner is available and the mission allows tactical scan, Scan can fall back to a global/faction intel ability.

For aircraft and drones, selected-unit Scan should feel different from Hold: Hold guards a local anchor; Scan actively moves through the target area to search and engage.

Expected behavior:

- Command button enters `TacticalCommandMode.Scan`.
- Camera panning remains available while in scan targeting mode.
- Valid world tap issues scan at the tapped cell/world position.
- Invalid tap rejects with `TargetOutOfBounds` or `ScanUnavailable`.
- If the selected unit is a scan-capable aircraft/drone and is landed, it takes off using the runway flow, flies to the scan area, patrols or performs a recon pass, reveals contacts, engages enemies it detects when allowed, then returns/lands or continues scan patrol according to its configured scan order duration.
- If the selected unit is already airborne, it flies or loiters toward the scan area and starts scan patrol without landing first.
- If the selected unit is a scan-capable ground unit, it moves/patrols inside the scan area and reveals contacts while obeying movement/pathing rules.
- If using global/faction tactical scan, the scan resolves at the target location without moving a selected unit.
- Valid scan reveals hidden/unknown enemy units, buildings, hazards, or mission-authored intel inside radius or along the patrol/recon path.
- Valid scan emits a visible scan marker and/or minimap/intel feedback.
- Scan execution spends charges/resources and starts cooldown only after target validation succeeds, if those systems are enabled.
- One-shot scan targeting clears after the scan order is accepted unless a future mission explicitly enables repeat scan targeting.

Current implementation note:

- `EnterScanTargetMode`, `Scan`, `ScanIntelCommandRequestElement`, `ScanIntelCommandResultElement`, `ScanIntelFeedEntry`, and `ScanIntelCommandSystem` already exist.
- The current scan path is closest to a global tactical reveal. Selected-unit scan patrol needs a separate command execution layer that issues movement/air orders and uses scan/intel reveal as the patrol effect.
- `ScanIntelCommandSystem` currently processes candidate collection on the main thread. The plan should first harden correctness, then convert candidate collection/reveal evaluation to jobs if scans can touch large entity counts.

## Progress Tracker

| Phase | Status | Owner | Validation / Notes |
| --- | --- | --- | --- |
| 0. Contract and code audit | Complete | Gameplay | Validated by reading specs and current systems while creating this document. |
| 1. Capability and reason-code audit | Pending | Gameplay | Confirm buttons expose correct enabled/disabled state and typed reasons. |
| 2. Stop command hardening | Pending | Gameplay | Requires unit tests/play validation for soldier, vehicle, and air units. |
| 3. Hold command hardening | Pending | Gameplay | Requires hold leash/auto-engage validation. |
| 4. Scan command targeting and input | Pending | Gameplay | Requires camera pan + scan tap validation. |
| 5. Scan execution, patrol, and intel feedback | Pending | Gameplay | Requires selected scanner patrol plus reveal/feed/minimap/marker validation. |
| 6. ECS/job migration pass | Pending | Gameplay | Convert hot scan/hold/stop loops only where measurable. |
| 7. HUD and marker polish | Pending | Gameplay/UI | Ensure command mode/result/markers are clear and consistent. |
| 8. Tests and visual QA | Pending | Gameplay/QA | Record exact commands and log paths here when complete. |

## Phase 0 - Contract And Code Audit

Status: Complete

Checklist:

- [x] Review available HUD implementation spec.
- [x] Review available selection implementation spec.
- [x] Review gameplay ECS architecture contract.
- [x] Confirm requested `Design/Match_Unit_Command_Behavior_Spec.md` was missing at initial audit time.
- [x] Add `Design/Match_Unit_Command_Behavior_Spec.md` as the detailed child behavior design document.
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
- [ ] Resolve scan source priority: selected scanner unit first, then mission/faction global tactical scan if available.
- [ ] Confirm scan can be enabled without selection only when mission rules allow global/faction tactical scan.
- [ ] Disable or reject Scan for selected units that cannot scan when no global/faction tactical scan source is available.
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
- [ ] Include scan source data in the request/result path: selected scanner entity, global/faction scan source, or mission scan source.
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

## Phase 5 - Scan Execution, Patrol, And Intel Feedback

Status: Pending

Implementation checklist:

- [ ] Keep scan execution in `ScanIntelCommandSystem` or narrower scan/intel systems.
- [ ] Add selected-unit scan order data for scan-capable units, such as scan center, radius, duration, source entity, and engagement policy.
- [ ] For landed scan-capable aircraft/drones, route through the existing runway/takeoff flow before the recon pass starts.
- [ ] For airborne scan-capable aircraft/drones, route to the scan area and start recon patrol/pass without landing first.
- [ ] For scan-capable ground units, move/patrol inside the target scan area using existing movement/pathing command systems.
- [ ] During scan patrol, reveal contacts inside scan radius or along the recon path.
- [ ] During scan patrol, allow the scanning unit to engage detected enemies according to combat rules and unit role.
- [ ] Keep scan patrol engagement bounded to the scan area/order; do not convert it into unrestricted chase.
- [ ] Return/land aircraft after scan duration or completion when configured to do so.
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

- Accepted global/faction scan reveals all eligible contacts in radius.
- Accepted selected-unit scan issues a patrol/recon order to the selected scanner.
- Landed aircraft take off before scan patrol and return/land after completion if configured.
- Airborne aircraft fly to the scan area and patrol/recon without unnecessary landing.
- Scan patrol units engage detected enemies only within scan order policy.
- Scan feedback says how many contacts were revealed or gives a clear success message.
- Scan marker is visible at gameplay camera distance.
- Minimap reflects revealed contacts if minimap visibility depends on scan state.
- Rejected scan does not spend resources/cooldown/charges.

Suggested tests:

- EditMode test: scan out of bounds returns `TargetOutOfBounds`.
- EditMode test: scan reveals enemy unit/building in radius.
- EditMode test: scan ignores friendly or dead entities where appropriate.
- EditMode test: scan appends feed entry.
- EditMode test: selected scan-capable aircraft queues scan patrol order instead of global reveal only.
- Play validation: landed jet scans area by taking off, reconning, engaging detected enemies if present, then returning/landing.

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
5. Harden Scan execution/patrol/reveal/feed/marker behavior.
6. Run ECS/job migration only after behavior is correct.
7. Polish HUD/marker feedback.
8. Add tests and update this tracker as phases complete.

## Open Questions For Implementation

- Does the mission design want Scan to be globally available, faction-owned, building-owned, or unit-owned?
- Should Hold be a toggle that clears hold on second press, or always issue a fresh hold order?
- Should Stop clear `EngageTarget` for all units, or only commanded/interruptible engage targets?
- For air units, should Hold mean loiter in the air, hold on runway/ground, or cancel current order and return to base?
- Which units have scan capability: jets, drones, scouts, radar vehicles, buildings, or all aircraft?
- For selected-unit Scan, should the unit return home after a fixed duration, after all detected contacts are gone, or when the player cancels/stops?
- Should scan-patrol engagement include only self-defense and detected enemies in the scan area, or also pursue targets that flee the scan area?
- What is the production scan radius/cooldown/charge economy?

Until those are answered, implement conservative defaults:

- Scan source priority is selected scan-capable unit first, then global/faction tactical scan only when mission rules allow it.
- Selected scan-capable aircraft/drone Scan means active recon patrol/pass over the chosen area, with bounded engagement of detected enemies.
- Units without scan capability do not pretend to scan unless a global/faction tactical scan source is available.
- Hold always issues/refreshes hold position, not toggle.
- Hold means guard current anchor and fire back/defend inside the hold leash.
- Stop clears current active orders and interruptible engage state.
- Air-unit Stop/Hold must preserve valid aircraft identity/home/runway data and avoid invalid transient state.
