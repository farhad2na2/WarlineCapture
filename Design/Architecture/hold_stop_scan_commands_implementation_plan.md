# Hold, Stop, And Scan Commands Implementation Plan

## Status

Overall status: Complete

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
- Move/attack/scan world markers belong to `SelectionOrderMarkerPresentationSystemHelper`.

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
| 1. Capability and reason-code audit | Complete | Gameplay/UI | Read-model capability/reason fields and HUD command-control interactability are wired. Validated with `SelectionUiReadModelLookupTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-readmodel-focused.log` and `UIShellCurrentContentLoadTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-command-controls.log`. |
| 2. Stop command hardening | Complete | Gameplay | Added mixed vehicle/air Stop coverage. Validated with `RtsSelectionInputSystemTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-stop-hardening.log`; pass line: `[RtsSelectionInputSystemValidation] result=Passed tests=50`. |
| 3. Hold command hardening | Complete | Gameplay | Split Hold from Stop air cleanup and added hold leash/air loiter coverage. Validated with `RtsSelectionInputSystemTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-hold-command-input.log`, `FocusedUnitCommandSystemTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-hold-focused-command.log`, and `UnitMovementBlockerValidationTests.RunHoldCommandFocusedValidation` at `/private/tmp/warline-hold-stop-scan-hold-movement.log`. |
| 4. Scan command targeting and input | Complete | Gameplay | Scan mode now preserves camera pan, routes scan taps through runtime input without focus fallthrough, rejects invalid targets with typed feedback, and clears one-shot scan mode after accepted or rejected taps. Validated with `RtsSelectionInputSystemTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-scan-input.log`, `ScanIntelCommandSystemTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-scan-intel.log`, and `SelectionCommandRequestResultContractTests.RunBatchValidation` at `/private/tmp/warline-hold-stop-scan-scan-flush.log`. |
| 5. Scan execution, patrol, and intel feedback | Complete | Gameplay | Selected scanner source/order foundation, scan-duration reveal pulses, bounded scan-area engagement, air scanner return-home completion, building reveal coverage, HUD result routing, minimap scan-intel markers, ground scanner patrol waypoints, runway/airborne scanner recon behavior, and readable composite scan marker feedback are implemented and validated. Cooldown/charges/resource data is intentionally deferred until a mission/source config contract exists. Latest validation: `SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-phase5-scan-marker.log`; pass line: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=14`. |
| 6. ECS/job migration pass | Complete | Gameplay | Completed the safe ECS migration pass without broad rewrites. `UnitScanOrderExecutionSystem` uses cached component lookups and ECB-backed structural writes; redundant patrol dispatch checks were removed; reveal pulses use the stored command frame; `ScanIntelCommandSystem` reveal writes were reviewed and candidate helpers no longer take unused `EntityManager` parameters. Stop/Hold jobs and scan candidate job split are explicitly deferred until profiling or selected-count/entity-count risk justifies them. Latest validation: `SelectionCommandRequestResultContractTests.RunBatchValidation` at `/private/tmp/warline-hold-stop-scan-phase6-reveal-audit.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=46`. |
| 7. HUD and marker polish | Complete | Gameplay/UI | Hold, Stop, and Scan command-mode prompt copy is explicit, accepted/rejected one-shot Scan clears command-mode presentation, Hold/Stop prompt/result lifetimes are covered, scan world-marker readability/grounding/overlay behavior is validated, and minimap selected/revealed markers are covered. Latest validation: `MatchHudMinimapMarkerSystemTests.RunFocusedValidation` at `/private/tmp/warline-hold-stop-scan-phase7-minimap-markers.log`; pass line: `[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=3`. |
| 8. Tests and visual QA | Complete | Gameplay/QA | Automated focused validation passed for command contracts, input/camera behavior, selected unit command behavior, HUD feedback, scan markers/feed, minimap markers, and focused PlayMode Stop/Hold/Scan tests. Latest validation: Unity Test Runner PlayMode filter `HoldStopScanCommandPlayModeTests` at `/private/tmp/warline-hold-stop-scan-phase8-playmode-results.xml`; pass summary: `result="Passed" total="3" passed="3" failed="0"`. |

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

Status: Complete

Implementation checklist:

- [x] Confirm HUD buttons call only `ISelectionUiCommand` methods.
- [x] Confirm `RequestHoldPosition`, `RequestStop`, and `RequestScanCommandMode` enqueue ECS intent requests.
- [x] Confirm command bar and command wheel use the same capability model.
- [x] Add or update read-model fields for whether Hold, Stop, and Scan are enabled.
- [x] Ensure disabled reasons use typed `TacticalCommandReasonCode` values.
- [x] Resolve scan source priority: selected scanner unit first, then mission/faction global tactical scan if available.
- [x] Confirm scan can be enabled without selection only when mission rules allow global/faction tactical scan.
- [x] Disable or reject Scan for selected units that cannot scan when no global/faction tactical scan source is available.
- [x] Confirm Stop is enabled only when selected units have interruptible state or active orders, unless design chooses always-enabled Stop.
- [x] Confirm Hold is enabled for selected units that can accept hold-position behavior.

Implementation notes:

- `FocusedUnitUiReadModelComponent` publishes `CanHold`, `HoldDisabledReason`, `CanStop`, `StopDisabledReason`, `CanScan`, and `ScanDisabledReason`.
- `SelectionUiReadModelLookup` owns the first-pass capability rules. Hold and Stop require a living, player-owned, movable unit that is not a transport passenger. Stop is intentionally always enabled for those units to match current command execution semantics, even when it becomes a no-op.
- `ISelectionUiReadModel` is the UI-facing read-model contract. `UIShellContentView` passes it into `MatchOverlayCommandInputUiSystemHelper`, which updates Hold, Stop, and command-wheel Stop interactability from the same capability source. Scan stays pressable so unavailable selections can show typed rejection feedback instead of silently swallowing the click.
- Scan priority is selected attack-capable unit first. Ordinary combat units such as rifle squads can scan with a reduced area, while recon/air scan specialists keep the larger scan area. Non-combat units such as cargo trucks still reject Scan with typed feedback.
- Latest Scan HUD feedback validation completed with `MatchHudCommandFeedbackPanelTests.RunFocusedValidation` at `/private/tmp/warline-scan-button-feedback.log` and `UIShellCurrentContentLoadTests.RunFocusedValidation` at `/private/tmp/warline-scan-button-shell.log`.
- Temporary `[ScanCommandTrace]` instrumentation and command-strip pointer probes are disabled/removed after the Scan button hit target was fixed. The legacy `SupportCommand` tab remains routed as a Scan alias without noisy logs.
- The live `SCN08_MatchHudContent` prefab had a Scan button hit-target mismatch: `ScanCommand.Button.targetGraphic` pointed at the Build tab frame and the Scan root lacked the transparent root `Image` used by the working command buttons. The prefab now has its own transparent root raycast target, and `MatchOverlayCommandInputUiSystemHelper` repairs the same issue defensively at bind time for stale instances. Latest validation: `MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation` at `/private/tmp/warline-scan-command-controls.log`, `MatchHudCommandFeedbackPanelTests.RunFocusedValidation` at `/private/tmp/warline-scan-debug-feedback.log`, and `UIShellCurrentContentLoadTests.RunFocusedValidation` at `/private/tmp/warline-scan-debug-shell.log`.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionUiReadModelLookupTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-readmodel-focused.log`; pass line: `[SelectionUiReadModelLookupValidation] result=Passed tests=5`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-command-controls.log`; pass line: `[UIShellCurrentContentLoadValidation] result=Passed tests=8`.

Acceptance criteria:

- HUD never decides command behavior locally.
- Command buttons display consistent enabled/disabled state.
- Rejections are typed and routed through `SelectionHudFeedbackBoundary`.

Suggested tests:

- `NoSelection` for Hold and Stop.
- Scan availability with no selected unit.
- Scan unavailable/cooldown/resource failure, if those config fields exist.

## Phase 2 - Stop Command Hardening

Status: Complete

Implementation checklist:

- [x] Keep command request entry point in `SelectionUiCommandSystem.RequestStop`.
- [x] Keep command intent kind as `RtsSelectionCommandIntentKind.Stop`.
- [x] Process Stop in `RtsSelectionImmediateSelectedUnitCommandSystem` or a narrower `ISystem` owned by the immediate selected-unit command domain.
- [x] Clear active command mode through `RtsSelectionInputStateComponent`.
- [x] Clear queued move order and pending move command requests.
- [x] Clear movement/path components through existing movement-order cleanup helpers.
- [x] Clear or abort attack/engage components according to command policy.
- [x] Reset `UnitVehicleKinematics` for ground vehicles.
- [x] Reset safe, explicit `UnitAirComponent` transient states for aircraft without deleting valid home/runway state.
- [x] Keep `SelectedUnitTag` on units.
- [x] Publish a command result with accepted count.
- [x] Add diagnostics only where they help validate command flow; remove temporary logs before completion.

Implementation notes:

- No runtime command rewrite was needed in this phase. The existing architecture already routes Stop through `SelectionUiCommandSystem.RequestStop`, `RtsSelectionCommandIntentKind.Stop`, and the `RtsSelectionImmediateSelectedUnitCommandSystem` `ISystem`.
- `UnitMoveOrderSystem.ClearMovementOrderComponents` already owns the movement/order cleanup set used by Stop, including move targets, path requests, path follow/range/retry, long-distance move, manual group membership, auto-wander, hold, engage, transport boarding/disembark, resource haul, and base breach orders.
- Stop now has focused EditMode coverage for a selected ground vehicle and selected air unit in the same command. The test verifies selected tags remain, accepted count is correct, queued move state clears, active scan/targeting mode clears, drag state clears, vehicle kinematics stop, and aircraft transient attack/taxi/return flags clear while home/runway/airborne state remains intact.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-stop-hardening.log`; pass line: `[RtsSelectionInputSystemValidation] result=Passed tests=50`.

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

Status: Complete

Implementation checklist:

- [x] Keep command request entry point in `SelectionUiCommandSystem.RequestHoldPosition`.
- [x] Keep command intent kind as `RtsSelectionCommandIntentKind.HoldPosition`.
- [x] Process Hold in the immediate selected-unit command domain.
- [x] Decide whether `HoldPositionOrderTag` is sufficient or replace/extend with data component `HoldPositionOrder`.
- [x] Skip `HoldPositionOrder` data component for now because current tag-based hold plus attack-range acquisition and engaged movement clearing is sufficient for the requested local defensive hold semantics.
- [x] Remove active movement/path/group/auto-wander components.
- [x] Enable `UnitCombat.AutoEngage` for attack-capable units.
- [x] Update `UnitEngagementSystem` so hold units acquire targets only inside hold rules.
- [x] Update engaged movement behavior so hold units do not chase past their leash.
- [x] Confirm air-unit hold policy: aircraft should either loiter at current safe altitude/anchor or hold current ground/runway state. Do not force runway behavior unless explicitly ordered.
- [x] Publish command mode/result feedback.

Implementation notes:

- `SelectionUiCommandSystem.RequestHoldPosition` still only enqueues `RtsSelectionCommandIntentKind.HoldPosition`.
- `RtsSelectionImmediateSelectedUnitCommandSystem` remains the active ECS immediate selected-unit command owner for Hold. Hold now uses hold-specific runtime cleanup: it clears active orders and stops vehicle kinematics, but no longer clears `UnitAirComponent` runway/airborne transient state the way Stop does.
- `FocusedUnitCommandSystem` was updated with the same Hold-vs-Stop cleanup split so compatibility callers and focused tests do not diverge from the active command path.
- `UnitAirMovementSystem` now respects `HoldPositionOrderTag` when an airborne or idle grounded aircraft has no active target. Held airborne units no longer automatically enter the return-home path; existing takeoff, landing, or returning-home transients continue so aircraft are not stranded mid-runway.
- `HoldPositionOrderTag` remains sufficient for Phase 3. `UnitEngagementSystem` already limits held acquisition to attack range, and `UnitEngagedMovementSystem` already clears held targets outside effective attack range instead of chasing.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-hold-command-input.log`; pass line: `[RtsSelectionInputSystemValidation] result=Passed tests=51`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FocusedUnitCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-hold-focused-command.log`; pass line: `[FocusedUnitCommandFocusedValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMovementBlockerValidationTests.RunHoldCommandFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-hold-movement.log`; pass line: `[HoldCommandMovementValidation] result=Passed tests=2`.

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

Status: Complete

Implementation checklist:

- [x] Keep scan command button entry in `SelectionUiCommandSystem.RequestScanCommandMode`.
- [x] Keep scan target-mode entry in `RtsSelectionScanTargetModeCommandSystem`.
- [x] Ensure entering Scan clears conflicting Move/Attack/Board targeting modes.
- [x] Ensure entering Scan does not enter selection rectangle mode.
- [x] Ensure camera panning remains active while scan targeting is armed.
- [x] Confirm tap release after pressing the Scan button is suppressed so the button click does not also scan.
- [x] Route scan world tap through `RtsSelectionPointerTargetCommandSystem.TryIssueScanOrder`.
- [x] Queue scan with either pre-resolved cell/world data or screen position using existing command intent buffer.
- [x] Keep selected scanner/source execution data deferred to Phase 5. Phase 4 owns target-mode input and target payload routing only; selected-unit patrol source data belongs to scan execution.
- [x] Reject out-of-bounds target with `TargetOutOfBounds`.
- [x] Clear one-shot scan mode after successful scan or rejected world tap according to the spec.

Implementation notes:

- `RtsSelectionRuntimeInputSystem.AllowsCameraPanDuringCommandMode` now treats `TacticalCommandMode.Scan` the same as Move and Attack for camera drag, so scan targeting no longer traps the camera.
- Runtime scan tap handling routes through `TryRequestScanOrder` and does not fall through to unit focus/selection when scan mode consumes the tap.
- `RtsSelectionCommandResultFlushSystem.ProcessScanCommandRequests` now clears one-shot Scan mode after rejected screen/world taps as well as accepted taps, including HUD command-mode cleanup and camera-drag reset.
- `ScanIntelCommandSystem` already rejects unresolved scan target cells with `TargetOutOfBounds`; focused coverage confirms that behavior.
- Selected scanner source data, scan patrol orders, aircraft recon passes, reveal markers, and minimap/intel feedback remain Phase 5 scope.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-scan-input.log`; pass line: `[RtsSelectionInputSystemValidation] result=Passed tests=53`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ScanIntelCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-scan-intel.log`; pass line: `[ScanIntelCommandFocusedValidation] result=Passed tests=2`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-scan-flush.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=36`.

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

Status: Complete

Implementation checklist:

- [x] Keep scan execution in `ScanIntelCommandSystem` or narrower scan/intel systems.
- [x] Add selected-unit scan order data for scan-capable units, such as scan center, radius, duration, source entity, and engagement policy.
- [x] Add scanner source data to scan command request/result/feed paths.
- [x] Add a narrow `UnitScanOrderExecutionSystem` that resolves scanner-sourced reveals when the scanner reaches the scan area.
- [x] Route selected scan-capable units toward the scan center through existing movement order components.
- [x] For landed scan-capable aircraft/drones, validate and polish the existing runway/takeoff flow before the recon pass starts.
- [x] For airborne scan-capable aircraft/drones, validate and polish route-to-scan behavior without unnecessary landing first.
- [x] For scan-capable ground units, extend center movement into scan-area patrol instead of a single approach point.
- [x] During scan execution, reveal contacts inside scan radius when the scanner reaches the target area.
- [x] During scan patrol, allow the scanning unit to engage detected enemies according to combat rules and unit role.
- [x] Keep scan patrol engagement bounded to the scan area/order; do not convert it into unrestricted chase.
- [x] Return/land aircraft after scan duration or completion when configured to do so.
- [x] Validate grid exists before accepting scan.
- [x] Validate target cell bounds before spending resources/cooldown.
- [x] Use configured scan radius if a mission/faction/source config exists; otherwise keep documented default.
- [x] Reveal eligible enemy units inside radius.
- [x] Reveal eligible enemy buildings inside radius.
- [x] Add `ScanIntelRevealedTag` and update `ScanIntelLastSeen`.
- [x] Append `ScanIntelFeedEntry` for accepted scans.
- [x] Publish `RtsSelectionCommandResultElement` with source entity, revealed count, target cell/world, radius, and marker payload.
- [x] Show a scan marker through `SelectionOrderMarkerPresentationSystemHelper` for accepted scan commands.
- [x] Polish accepted scan markers through `SelectionOrderMarkerPresentationSystemHelper` so they are readable at gameplay camera distance and use a connected composite ring/bracket shape.
- [x] Route HUD result through `SelectionHudFeedbackBoundary`.
- [x] Update minimap enemy markers if minimap reads revealed intel instead of live visibility.
- [x] Defer cooldown/charges/resource data until the mission/source config contract is identified.

Implementation notes:

- `RtsSelectionCommandIntentRequestElement`, `RtsSelectionCommandResultElement`, `ScanIntelCommandRequestElement`, `ScanIntelCommandResultElement`, and `ScanIntelFeedEntry` now carry optional scanner source data.
- `UnitScanOrder` stores selected scanner scan center, world position, radius, duration, source entity, timing state, and conservative engagement/return policy flags.
- `ScanIntelCommandSystem` still owns reveal writes and feed entries. If a selected scan-capable unit is present, the initial scan command defers reveal and creates a `UnitScanOrder`; anonymous/global scan requests still reveal immediately.
- `UnitScanOrderExecutionSystem` is a narrow ECS execution system. It waits until the scanner is inside scan radius, starts the scan-duration window, queues scanner-sourced reveal pulses, and removes the scan order only after duration expires.
- On scan completion, `UnitScanOrderExecutionSystem` now returns air scanners through the existing `UnitAirComponent.ReturningHome` path, clears scan movement/order components, and drops scan engage targets.
- `UnitEngagementSystem` now treats active scan orders as a bounded acquisition context: scanners can acquire enemies inside the scan area even after the center move has started, but scan targets outside the area are ignored.
- `UnitEngagedMovementSystem` clears scanner engage targets that leave the scan area, preventing unrestricted chase from a scan order.
- Scanner-sourced scan command results now carry deferred-source state into `RtsSelectionCommandResultElement`, so HUD feedback through `SelectionHudFeedbackBoundary` says `SCAN ORDERED: SCANNER EN ROUTE` instead of incorrectly reporting `SCAN COMPLETE: 0 CONTACTS` before the scout reaches the area.
- `MatchHudMinimapMarkerSystem` now appends hostile `ScanIntelLastSeen` contacts that are not already represented by live unit markers, so scan-revealed buildings and other intel-only contacts can appear on the minimap without duplicating live scanned units.
- Ground scanner scan orders now rotate through cardinal patrol waypoints inside the scan radius once the scanner has reached the scan area. Patrol movement is issued through `UnitMoveOrderSystem.IssueImmediateMoveCommand`, keeping `UnitPathRequest` writes centralized in the existing move-order owner.
- Selected scanner movement routes to the scan center using existing `UnitMoveOrderSystem.IssueImmediateMoveCommand`. This uses existing ground path requests and existing air `UnitTarget`/runway behavior.
- `UnitAirMovementSystem` now respects active `UnitScanOrder` state: landed runway scanners keep the normal taxi/takeoff flow before recon, and airborne scanners with an active scan order do not start an unsolicited return/landing while the scan duration is still active. Scan completion remains owned by `UnitScanOrderExecutionSystem`, which returns aircraft home when configured.
- Accepted scan markers now use a composite runtime marker owned by `SelectionOrderMarkerPresentationSystemHelper`: 128-segment outer ring, inner ring, four bracket arcs, readable minimum radius, surface-aware vertical offset, overlay material, and a longer minimum visibility window. This replaces the previous tiny single-line scan ring.
- Cooldown/charges/resource data was not invented in this phase because the mission/source economy contract is not defined yet. Add it only after that contract exists.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-scan-contract.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=38`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-bounded-engagement.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=40`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-air-return-building-reveal.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=42`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-hud-feedback.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=43`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudMinimapMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-minimap-intel.log`; pass line: `[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=2`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-ground-patrol.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=44`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-air-recon.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=46`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase5-scan-marker.log`; pass line: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=14`.

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

Status: Complete

Implementation checklist:

- [x] Convert any new command execution code to `ISystem` by default.
- [x] Keep components and buffer elements data-only.
- [x] Remove avoidable per-order `EntityManager` component reads from `UnitScanOrderExecutionSystem`.
- [x] Remove redundant post-loop `EntityManager.Exists/HasComponent` checks from scan patrol dispatch.
- [x] Keep `UnitScanOrderExecutionSystem` structural cleanup behind `EntityCommandBuffer`.
- [x] Defer Stop/Hold `IJobEntity` conversion until selected-unit batch mutation becomes hot or large.
- [x] Defer Scan target candidate job split until scan radius/entity count makes main-thread scans costly.
- [x] Review `ScanIntelCommandSystem` reveal structural writes before any scan candidate job split.
- [x] Avoid managed allocations in the new per-frame scan-order execution path where practical.
- [x] Avoid `EntityManager.CreateEntityQuery` inside hot per-frame scan-order execution; prefer cached/looked-up component data in systems.
- [x] Complete Burst compatibility audit; do not add `[BurstCompile]` to systems that still cross managed `EntityManager` command/reveal boundaries.

Implementation notes:

- `UnitScanOrderExecutionSystem` is already an `ISystem` and remains the narrow owner for selected-unit scan patrol/reveal pulse execution.
- The scan-order execution loop now uses `ComponentLookup<Disabled>`, `ComponentLookup<UnitDeathAnimationComponent>`, and `ComponentLookup<UnitHealth>` for invalid/dead source checks instead of calling through `EntityManager` per order.
- Scan completion now removes `UnitTarget`, `UnitPathRequest`, `ManualMoveOrderTag`, and `EngageTarget` through lookup-gated `EntityCommandBuffer` writes.
- Scan patrol dispatch now trusts the pending patrol list generated from the same scan-order query pass instead of rechecking entity existence and `UnitScanOrder` after the loop.
- Scan reveal pulses now use `UnitScanOrder.StartedFrame`, keeping the system data-driven instead of reading `UnityEngine.Time.frameCount`.
- `ScanIntelCommandSystem` reveal application still performs structural writes (`ScanIntelRevealedTag`, `ScanIntelLastSeen`) and feed/result ordering on the main thread. Candidate collection is the job-safe portion; reveal application should move only after an ECB-backed design preserves deterministic result/feed behavior.
- `CollectRevealUnits` and `CollectRevealBuildings` no longer take unused `EntityManager` parameters, making the future job-split boundary clearer without changing reveal behavior.
- Stop/Hold job conversion is intentionally deferred until selected-unit batch size or profiling shows the immediate command path is hot.
- Scan candidate collection in `ScanIntelCommandSystem` is intentionally left main-thread for this step because reveal writes and feed/result ordering need a separate job-safe design pass.
- `[BurstCompile]` was not added to `UnitScanOrderExecutionSystem` because the system still intentionally crosses managed command boundaries: it calls existing move-order issuing and scan enqueue helpers through `EntityManager`. Adding Burst before those seams are split would be cosmetic and risk compile-time churn.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase6-ecs-pass.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=46`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase6-patrol-dispatch.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=46`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase6-reveal-audit.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=46`.

Acceptance criteria:

- No avoidable GC allocations in command execution hot paths.
- No broad managed command facade introduced.
- Existing request/result flow remains readable and testable.

Suggested tests:

- Run focused EditMode tests for command systems.
- Run profiler/GC validation if command execution is touched heavily.

## Phase 7 - HUD And Marker Polish

Status: Complete

Implementation checklist:

- [x] Confirm `TacticalCommandFeedbackText` has clear text for Hold, Stop, and Scan.
- [x] Confirm `BattleHudRuntimeFeedbackBoundary` visual state handles Hold/Stop/Scan.
- [x] Confirm command mode clears after one-shot Scan.
- [x] Confirm Hold/Stop feedback is transient or persistent according to HUD spec.
- [x] Confirm world markers for Scan are consistent with move/attack marker style.
- [x] Confirm no command marker is tiny, fragmented, or hidden under terrain.
- [x] Confirm minimap markers update for revealed enemies and selected units.

Implementation notes:

- `TacticalCommandFeedbackText.ToInstructionText` now includes `Hold` and `Stop`, so those modes no longer show only a command-mode title without an actionable feedback prompt.
- Hold prompt: `Hold position and return fire.`
- Stop prompt: `Stop selected units and clear orders.`
- Hold uses ready feedback severity; Stop uses warning feedback severity; Scan keeps ready feedback severity.
- Added focused command-contract coverage for accepted one-shot selected-scanner Scan. The test verifies the scan order is accepted, the scanner receives `UnitScanOrder`, active Scan mode clears, HUD command mode clears, and camera dragging is reset.
- Added focused HUD feedback lifetime coverage for Hold and Stop. Command-mode prompts remain persistent while active, clear when command mode clears, and accepted Hold/Stop command results use transient feedback that auto-hides.
- Added focused scan world-marker coverage. The test suite now verifies scan markers use the premium composite LineRenderer marker family, overlay/no-shadow/no-occlusion render settings, readable minimum radius, connected line segments, and positions above the resolved command-marker surface even when the clicked world point is below ground.
- Added focused minimap coverage for selected player units and scan-revealed hostile contacts in the same minimap marker buffer. Friendly scan intel contacts remain filtered out.
- Validation completed with:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandFeedbackPanelTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase7-feedback-text.log`; pass line: `[MatchHudCommandFeedbackValidation] result=Passed tests=11`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase7-scan-mode-clear.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=47`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandFeedbackPanelTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase7-hold-stop-feedback-lifetime.log`; pass line: `[MatchHudCommandFeedbackValidation] result=Passed tests=12`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase7-scan-marker-polish.log`; pass line: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudMinimapMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase7-minimap-markers.log`; pass line: `[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=3`.

Acceptance criteria:

- HUD feedback and marker feedback feel like one command family.
- No direct child UI path writes are added.
- Command modes do not leave stale highlighted buttons.

## Phase 8 - Tests And Visual QA

Status: Complete

Validation checklist:

- [x] Run focused EditMode tests for command contracts.
- [x] Run focused PlayMode test or manual validation for Stop.
- [x] Run focused PlayMode test or manual validation for Hold.
- [x] Run focused PlayMode test or manual validation for Scan.
- [x] Validate soldiers, vehicles, aircraft, and mixed selections.
- [x] Validate no-selection rejections.
- [x] Validate camera panning in Scan/Move/Attack modes.
- [x] Validate command result HUD feedback.
- [x] Validate scan/feed/minimap marker visibility.
- [x] Record exact validation command and log path below.

Validation log:

- Automated focused validation started from heartbeat `2026-06-16T16:13:22.717Z`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-command-contracts.log`; pass line: `[SelectionCommandRequestResultContractValidation] result=Passed tests=47`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-runtime-input.log`; pass line: `[RtsSelectionInputSystemValidation] result=Passed tests=53`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FocusedUnitCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-focused-unit-command.log`; pass line: `[FocusedUnitCommandFocusedValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMovementBlockerValidationTests.RunHoldCommandFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-hold-movement.log`; pass line: `[HoldCommandMovementValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ScanIntelCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-scan-intel.log`; pass line: `[ScanIntelCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandFeedbackPanelTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-hud-feedback.log`; pass line: `[MatchHudCommandFeedbackValidation] result=Passed tests=12`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-order-markers.log`; pass line: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudMinimapMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-hold-stop-scan-phase8-minimap-markers.log`; pass line: `[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-Clone -runTests -testPlatform PlayMode -testFilter HoldStopScanCommandPlayModeTests -testResults /private/tmp/warline-hold-stop-scan-phase8-playmode-results.xml -logFile /private/tmp/warline-hold-stop-scan-phase8-playmode.log`; pass summary in results XML: `result="Passed" total="3" passed="3" failed="0"`.
- General match runtime smoke was also attempted without `-quit` at `/private/tmp/warline-hold-stop-scan-phase8-match-runtime-smoke.log`. It reached `[MatchRuntimeShellSmokeValidation] result=Passed ...`, but the log also contains a Unity Entities Graphics shutdown `NullReferenceException` after the pass line, so it is recorded as supplemental only and was not used to close the command-specific PlayMode checklist.
- No separate human visual inspection pass was run in-editor. Automated marker/HUD/minimap tests cover geometry, grounding, feedback state, and marker publishing; human visual review is still recommended before release, but the implementation checklist is complete.

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
