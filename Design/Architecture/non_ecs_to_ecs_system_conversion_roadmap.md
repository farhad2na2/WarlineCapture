# Non-ECS System To ECS System Conversion Roadmap

## Goal

Eliminate plain runtime gameplay `*System` classes that behave like gameplay systems but are not Unity ECS systems.

The target is not to force every C# helper into an `ISystem`. The target is:

- Gameplay command execution, selection policy, order mutation, simulation, and read/write ECS behavior become real ECS systems.
- Managed Unity boundaries that must touch UI, camera, GameObject, prefab, scene, or serialized references become explicit managed ECS boundaries, usually `SystemBase`, or passive `*View`/authoring/config code.
- Tiny pure helper `*System` types are folded into their owning ECS system or job, so they do not remain misleading standalone non-ECS systems.
- UI remains responsive by writing request data and reading result data. UI does not directly mutate gameplay.

This roadmap follows:

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`

## Baseline Snapshot

Audit seed date: 2026-06-14.

Quick static scan:

- Runtime `*System` declarations under `Assets/Game/Scripts`: `496`.
- Plain non-Unity `*System` declarations by simple exclusion of `ISystem`, `SystemBase`, and `MonoBehaviour`: `409`.

The first implementation phase must replace this quick scan with an authoritative inventory because a line-based scan can overcount nested helpers, static helper classes, generated/test-only code, or types whose base type appears on another line.

## Progress Snapshot

Always update this section when implementation begins or a phase completes.

- Checklist progress: `84 / 113 complete (74.3%)`.
- In progress: `1`.
- Remaining open: `28`.
- Phase progress: `9 / 13 phases complete; 1 in progress; 3 not started`.
- Authoritative non-ECS runtime `*System` inventory: `385` after excluding `105` Unity ECS systems, `1` MonoBehaviour system, and `8` editor-only systems.
- Generated inventory artifact: `Design/Architecture/non_ecs_to_ecs_system_inventory.md`.
- Proposed inventory dispositions: `6` ConvertToISystem, `285` ConvertToSystemBase, `73` FoldIntoOwner, `21` PassiveBoundary, and `0` ReviewRequired.
- Converted to `ISystem`: `17`.
- Converted to `SystemBase`: `0`.
- Folded into ECS owners/jobs: `5`.
- Kept as passive view/config/authoring/editor boundary: `5`.
- Remaining plain runtime gameplay `*System` classes: `385`.
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.
- Last implementation update: 2026-06-14 moved shared transport boarding clearance/grounding constants into neutral transport boarding data while keeping the remaining transport helper fold in progress.

## Architecture Rules

- No `Object.Find*`, `GameObject.Find`, `Camera.main`, hierarchy string lookup, static mutable registries, service locators, broad manager/controller/facade shells, or ungated hot-path logs.
- UI and shell code may create requests and display results, but must not execute gameplay policy or mutate gameplay ECS state directly.
- `Camera`, `GameObject`, `Transform`, `UnityEngine.Object`, prefabs, serialized scene references, and UI views stay in managed boundaries.
- Pure recurring gameplay data work should prefer `ISystem`, Burst-compatible jobs, cached type handles/lookups, and ECB-backed structural changes.
- Managed ECS boundaries may use `SystemBase` only when managed Unity references are truly required.
- Do not rename a non-ECS class to `Service`, `Query`, `Rule`, `Cell`, `Resolver`, `Adapter`, `Composer`, or `Context` as an escape hatch for gameplay behavior.
- Do not create a replacement broad shell. Convert, split, or fold the existing responsibility into the narrow owner.
- Preserve Unity `.meta` files.
- Work in small behavior-preserving slices. Each slice should compile and pass focused validation before the next slice begins.

## Classification Model

Every current non-Unity `*System` type must end in exactly one disposition.

### Disposition A: Convert To `ISystem`

Use this for pure ECS gameplay behavior:

- consumes request components/buffers,
- validates ECS state,
- mutates ECS components/buffers,
- emits result components/buffers,
- runs simulation or recurring data transforms.

Examples:

- command request processors,
- movement/order application,
- combat/target command execution,
- selection state mutations,
- transport boarding command execution.

### Disposition B: Convert To `SystemBase`

Use this only for ECS boundaries that must touch managed Unity references:

- camera raycast boundary,
- UI result presentation boundary,
- prefab/GameObject visual projection boundary,
- scene reference bridge.

The `SystemBase` must remain a boundary. Gameplay policy should still live in ECS data and ECS command systems.

### Disposition C: Fold Into Owning ECS System Or Job

Use this for narrow pure helper systems that do not need independent update lifetime:

- small validation helpers,
- candidate scoring helpers,
- approach-cell math,
- footprint helpers,
- read-only helper algorithms.

The result should be private/static helper code inside the owning ECS system/job, or a small value type that is not pretending to be a standalone runtime system.

### Disposition D: Keep Passive Boundary Or Editor Tooling

Use this only when the type is not runtime gameplay:

- UI `*View`,
- ScriptableObject `*Config`,
- `*Authoring` / `*Baker`,
- editor-only validation/build tooling,
- test fixtures.

If the type currently ends in `System` but is actually passive/editor/tooling, the implementation phase should decide whether to rename it, move it to an editor boundary, or leave it documented as grandfathered debt if renaming would be unsafe.

## Recommended Defaults For Former "Needs Input" Systems

These are the agreed defaults for planning:

- Pointer/camera resolution remains managed and writes resolved ECS command data.
- Active command mode taps belong to the active command first; they should not select an enemy or building.
- ECS command processors own accepted/rejected result codes.
- HUD feedback maps result codes to user-facing text and displays them.
- Failed Attack, Board, and Scan target taps stay in the active mode unless the mode itself is invalid.
- Successful Move, Attack, Scan, Board All, and Disembark commands clear the command mode.
- Passenger-to-transport or transport-to-passenger Board mode remains active while waiting for the counterpart target, then clears after the actual order succeeds.
- Immediate UI feedback may show "requested"; result feedback must say accepted or explain failure.

## Phase 0: Authoritative Inventory

Status: [x]

Purpose:
Create the exact list of non-ECS runtime `*System` types and classify each one before conversion starts.

Implementation steps:

- [x] Build a script or focused architecture test that enumerates all runtime `*System` type declarations.
- [x] Exclude actual Unity ECS systems: `ISystem` and `SystemBase`.
- [x] Exclude `MonoBehaviour` views and shell components from the gameplay conversion denominator.
- [x] Exclude editor-only systems from runtime gameplay conversion, but list them separately.
- [x] Classify every remaining type into Disposition A, B, C, or D.
- [x] Produce a generated inventory table with file path, type name, current base type, disposition, owner phase, and reason.
- [x] Add a guardrail test that fails if a new plain runtime gameplay `*System` is added without classification.
- [x] Update the progress snapshot with the authoritative denominator.

Acceptance checks:

- Every non-ECS runtime `*System` has one owner phase.
- No system is counted in two dispositions.
- The inventory distinguishes "convert to ECS" from "fold helper into ECS owner".

Progress notes:

- 2026-06-14: Added `Assets/Tests/Editor/NonEcsSystemConversionArchitectureTests.cs` with `RuntimeSystemInventoryCanBeEnumerated` and `RunFocusedValidation`. The test enumerates runtime `*System` declarations under `Assets/Game/Scripts`, excludes Unity ECS systems, excludes `MonoBehaviour` systems from the conversion denominator, lists editor-only systems separately, and logs the first-wave command conversion candidates. Focused Unity validation reported `totalSystemDeclarations=496`, `unityEcs=86`, `monoBehaviour=1`, `editorOnly=8`, and `runtimeNonEcsDenominator=401`.
- 2026-06-14: Added `Tools/Architecture/generate_non_ecs_system_inventory.py` and generated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`. The generated table includes file path, type name, current base type, proposed disposition, owner phase, and reason for all `401` runtime non-ECS `*System` declarations.
- 2026-06-14: Extended `NonEcsSystemConversionArchitectureTests` with `GeneratedInventoryContainsEveryRuntimeNonEcsSystem`, which compares the live runtime non-ECS denominator against the generated inventory file and fails on missing or stale rows. Focused Unity validation passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Completed Phase 0 classification. Current proposed split is `15` ConvertToISystem, `291` ConvertToSystemBase, `73` FoldIntoOwner, `22` PassiveBoundary, and `0` ReviewRequired.

## Phase 1: Command Request/Result Foundation

Status: [x]

Purpose:
Standardize command request and result flow before converting individual command systems.

Implementation steps:

- [x] Review `RtsSelectionCommandIntentRequestElement` and `RtsSelectionCommandResultElement`.
- [x] Decide whether current shared buffers are enough or whether specific request/result components are needed for Move, Attack, Scan, and Board.
- [x] Add stable reason-code enums where current results rely on loose text.
- [x] Ensure command requests can carry pre-resolved target data: entity, cell, world position, screen position, and target kind.
- [x] Ensure command results can carry accepted/rejected state, reason code, command mode, target entity/cell/world position, marker intent, and feedback lifetime.
- [x] Ensure command requests are consumed exactly once.
- [x] Ensure command results are drained exactly once by presentation boundaries.
- [x] Add EditMode tests for request consumption and result emission.

Acceptance checks:

- UI can enqueue a command without knowing whether it will succeed.
- Result feedback is the only source of accepted/rejected command feedback.
- No command processor needs direct UI references.

Progress notes:

- 2026-06-14: Reviewed the existing shared command intent/result buffers and kept the shared-buffer model for Move, Attack, Scan, Board, Disembark, and selection-mode commands. Added `RtsSelectionCommandTargetKind`, `RtsSelectionCommandFeedbackLifetime`, request `WorldPosition`/`TargetKind` fields, result `TargetKind`/`CommandMode`/feedback lifetime fields, and transport-specific `TacticalCommandReasonCode` values. Added `Assets/Tests/Editor/SelectionCommandRequestResultContractTests.cs` to guard pre-resolved target data, marker/result metadata, feedback lifetime data, and transport failure reason text. Focused EditMode validation for `SelectionCommandRequestResultContractTests` exited successfully after sandboxed Unity hit the known UPM IPC restriction and the same command was rerun outside the sandbox.
- 2026-06-14: Extended `SelectionCommandRequestResultContractTests` with first-wave request consumption/result emission coverage for Move, Attack, Scan, and Transport rejection paths, plus a Scan presentation-boundary drain test through `RtsSelectionCommandResultFlushSystem`. Focused EditMode validation passed for `SelectionCommandRequestResultContractTests`.

## Phase 2: Straightforward Selection Command Processors

Status: [x]

Purpose:
Convert the command processors that already follow the request/result shape.

Implementation steps:

- [x] Convert `SelectionMoveCommandRequestSystem` into an ECS system or fold it into the new move command ECS owner.
- [x] Convert `SelectedMoveOrderCommandSystem` into the move command ECS owner.
- [x] Convert `SelectionAttackCommandRequestSystem` into an ECS system or fold it into the new attack command ECS owner.
- [x] Convert `AttackOrderCommandSystem` into the attack command ECS owner.
- [x] Convert `SelectionScanCommandRequestSystem` into an ECS system or fold it into the new scan command ECS owner.
- [x] Convert `ScanIntelCommandSystem` into the scan command ECS owner.
- [x] Convert `SelectionTransportCommandRequestSystem` into an ECS system or fold it into the new transport command ECS owner.
- [x] Convert `TransportBoardingCommandSystem` into the transport boarding command ECS owner.
- [x] Fold `UnitTransportRopeDisembarkCommandSystem` into the transport command ECS owner or make it a narrow disembark request processor.
- [x] Convert `BuildingTargetMoveOrderSystem` into a request/result ECS command processor.
- [x] Convert `CitizenMovementCommandSystem` into a request/result ECS command processor.

Acceptance checks:

- Move command requests produce accepted/rejected move results.
- Attack command requests produce accepted/rejected attack results.
- Scan command requests produce accepted/rejected scan results.
- Board/disembark command requests produce accepted/rejected transport results.
- Current user-visible behavior is preserved.
- Command processors do not call UI, camera, or GameObject APIs.

Progress notes:

- 2026-06-14: Prepared first-wave command request processors for ECS result presentation by populating `CommandMode`, `TargetKind`, and `FeedbackLifetime` on Move, Attack, Scan, Board, and Disembark `RtsSelectionCommandResultElement` outputs. Added focused assertions in `SelectionCommandRequestResultContractTests`. This is metadata-only prep; the processors are not yet converted to `ISystem`, so Phase 2 conversion checklist items remain open.
- 2026-06-14: Removed managed pending-request list fields from `SelectionMoveCommandRequestSystem`, `SelectionAttackCommandRequestSystem`, `SelectionScanCommandRequestSystem`, and `SelectionTransportCommandRequestSystem`. These processors now consume matching command-buffer entries in place and emit the same result-buffer data, which reduces direct-call wrapper state before moving execution into ECS owners. Transport passenger/disembark working buffers remain for a later ECS data split. No Phase 2 conversion checklist item is complete yet.
- 2026-06-14: Converted `CitizenMovementCommandSystem` from a plain helper class into an `ISystem` that consumes `CitizenMoveCommandRequestElement` and emits `CitizenMoveCommandResultElement`. Managed visible-citizen code now queues requests only; `CitizenPopulationRuntimeUpdateSystem` flushes the queue after visible-citizen sync to preserve same-frame movement setup during the transition. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `400`, Unity ECS systems excluded is now `87`, converted-to-`ISystem` count is now `1`.
- 2026-06-14: Converted `BuildingTargetMoveOrderSystem` from a plain helper class into an `ISystem` that consumes `BuildingTargetMoveOrderRequestElement` and emits `BuildingTargetMoveOrderResultElement`. Existing managed selection/building boundaries now pass building target cells into the ECS request/result path and synchronously flush during the transition to preserve immediate selection clearing and marker behavior. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `399`, Unity ECS systems excluded is now `88`, converted-to-`ISystem` count is now `2`.
- 2026-06-14: Converted `ScanIntelCommandSystem` from a plain helper class into an `ISystem` that consumes `ScanIntelCommandRequestElement` and emits `ScanIntelCommandResultElement`. The selection scan boundary still resolves screen taps through the existing managed resolver, then enqueues and synchronously flushes the scan request during the transition. The scan owner collects all unit/building reveal candidates before applying reveal components, avoiding type-handle invalidation from mid-scan structural changes. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `398`, Unity ECS systems excluded is now `89`, converted-to-`ISystem` count is now `3`.
- 2026-06-14: Folded `SelectionScanCommandRequestSystem` into `ScanIntelCommandSystem`. The scan ECS owner now drains scan command-intent requests, maps scan results into `RtsSelectionCommandResultElement`, and keeps the managed screen-tap resolver as a transition boundary method. Removed the obsolete wrapper script and meta. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `397`, first-wave ConvertToISystem candidates are now `13`, and folded count is now `1`.
- 2026-06-14: Folded `SelectionMoveCommandRequestSystem` into `SelectedMoveOrderCommandSystem`. The move command owner now drains move command-intent requests, maps move results into `RtsSelectionCommandResultElement`, and keeps the managed pointer/cell resolvers as transition boundary delegates. Removed the obsolete wrapper script and meta. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `396`, first-wave ConvertToISystem candidates are now `12`, and folded count is now `2`.
- 2026-06-14: Folded `SelectionAttackCommandRequestSystem` into `AttackOrderCommandSystem`. The attack command owner now drains attack command-intent requests, maps attack results into `RtsSelectionCommandResultElement`, and keeps clicked-target/source collection and base-breach resolution as existing transition delegates. Removed the obsolete wrapper script and meta. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `395`, first-wave ConvertToISystem candidates are now `11`, and folded count is now `3`.
- 2026-06-14: Folded `SelectionTransportCommandRequestSystem` into `TransportBoardingCommandSystem`. The transport command owner now drains board/disembark command-intent requests, maps transport results into `RtsSelectionCommandResultElement`, owns disembark helper state, and refreshes command buffers after structural disembark/boarding changes. Removed the obsolete wrapper script and meta. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `394`, first-wave ConvertToISystem candidates are now `10`, and folded count is now `4`.
- 2026-06-14: Converted `SelectedMoveOrderCommandSystem` into an `ISystem` move command owner. It now owns an ECS `OnUpdate` path for pre-resolved Move command requests carrying target cell/world data, while the existing managed transition method continues to resolve screen clicks and leaves screen-only requests untouched. Removed managed selection scratch state from the owner by collecting selected units into caller-owned `NativeList<Entity>` storage. Also fixed `BuildingTargetMoveOrderSystem` request/result buffer lifetime so structural move-order changes do not invalidate result writes during focused move validation. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `393`, Unity ECS excluded count is now `90`, and converted-to-`ISystem` count is now `4`.
- 2026-06-14: Converted `AttackOrderCommandSystem` into an `ISystem` attack command owner. It now owns an ECS `OnUpdate` path for pre-resolved Attack command requests carrying a target entity, while the existing managed transition method continues to resolve screen clicks, focused attack sources, and base-breach targets for current UI behavior. Removed managed instance state from the owner by using cached ECS queries/type handles for the ECS path and caller-owned scratch storage for the managed transition path. Also refreshed command buffers after attack-order structural changes so result emission does not leave invalidated request buffers in the transition loop. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `392`, Unity ECS excluded count is now `91`, and converted-to-`ISystem` count is now `5`.
- 2026-06-14: Fixed `SelectedMoveOrderCommandSystem.ProcessCommandIntentRequests` command-buffer lifetime after structural move-order changes by collecting/removing matching Move requests before issuing move orders, then reacquiring the result buffer for each emitted result. Added focused regression coverage for the attached `BufferTypeHandle<RtsSelectionCommandIntentRequestElement>` invalidation path; focused Unity validation passed with `[SelectionCommandRequestResultContractValidation] result=Passed tests=12`.
- 2026-06-14: Folded `UnitTransportRopeDisembarkCommandSystem` into `TransportBoardingCommandSystem`. The transport command owner now owns helicopter rope-disembark detection and request creation directly, while the removed helper's behavior is preserved through the existing disembark command paths. Removed the obsolete script and meta, trimmed unused context plumbing, and regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `391`, first-wave ConvertToISystem candidates are now `7`, and folded count is now `5`.
- 2026-06-14: Started the `TransportBoardingCommandSystem` ECS-owner conversion by removing disembark-path instance scratch fields and making the disembark command helpers static with explicit grid-query input. This keeps board-click behavior managed for now, but removes another managed-state blocker before adding an ECS-owned resolved-disembark request pass.
- 2026-06-14: Converted `TransportBoardingCommandSystem` into an `ISystem`. Its ECS `OnUpdate` now consumes pre-resolved `BoardSelectedTransportPassenger`, `DisembarkTransport`, and `DisembarkTransportPassenger` command-intent requests and emits transport command results; screen-click board target resolution remains in the managed transition boundary. Removed remaining managed instance scratch fields from the owner and regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `390`, Unity ECS excluded count is now `92`, and converted-to-`ISystem` count is now `6`.

## Phase 3: Pointer Target Boundary Split

Status: [x]

Purpose:
Split `RtsSelectionPointerTargetCommandSystem` so it no longer executes gameplay commands.

Recommended disposition:

- Convert the pointer/camera part to a managed ECS boundary, likely `SystemBase`, or fold it into an existing managed input boundary if that is cleaner.
- It may read camera/pointer data and write ECS request data.
- It must not directly issue Move, Attack, Scan, Board, or BuildingTargetMove orders.

Implementation steps:

- [x] Identify every direct command execution call currently routed through `RtsSelectionPointerTargetCommandSystem`.
- [x] Move camera/screen-to-world resolution into a boundary-only target resolution pass.
- [x] Write resolved Move target requests instead of calling move command execution.
- [x] Write resolved Attack target requests instead of calling attack command execution.
- [x] Write resolved Scan target requests instead of calling scan command execution.
- [x] Write resolved Board target requests instead of calling transport command execution.
- [x] Preserve command-mode UX: active command taps do not select hostile units/buildings.
- [x] Preserve mobile pan behavior during long-range Attack and transport-first Board mode.
- [x] Add tests for command-mode click priority over selection.

Acceptance checks:

- Pointer boundary has no gameplay order mutation.
- Gameplay command execution happens only in ECS command systems.
- Failed target taps stay in the intended command mode unless the mode is invalid.

Progress notes:

- 2026-06-14: Audited direct command execution routes in `RtsSelectionPointerTargetCommandSystem`. The routes to split are: `IssueMoveOrder` queues Move and immediately invokes `ProcessMoveCommandRequests`; `TryIssueAttackOrderToClickedUnit` queues Attack and immediately invokes `ProcessAttackCommandRequests`; `TryIssueScanOrder` queues Scan and immediately invokes `ProcessScanCommandRequests`; `TryIssueBoardTransportOrderToClickedUnit` queues Board and immediately invokes `ProcessTransportCommandRequests`; `TryIssueMoveOrderToBuilding` directly calls `BuildingTargetMoveOrderSystem.TryIssueMoveOrderToBuilding`, clears selection/focus, and emits the screen marker. Adjacent selection mutation remains in `TryFocusUnit`, which calls `FocusedUnitLifecycleSystem.TryFocusUnit`, clears pending move requests, and updates pointer/camera state; this belongs to the Phase 4 focus split rather than the command execution split.
- 2026-06-14: `RtsSelectionPointerTargetCommandSystem.IssueMoveOrder` now resolves non-unit Move targets at the managed pointer boundary and writes command-intent requests carrying `TargetCell` and `WorldPosition`. Resolved Move requests are left for `SelectedMoveOrderCommandSystem.OnUpdate`; only unresolved/clicked-unit fallback requests still enter the managed transition drain. `SelectionGameplayStartupSystem` drains pending Move results during the runtime tick so HUD feedback remains responsive. Also fixed the attached runtime `ObjectDisposedException` by making `RtsSelectionCommandResultFlushSystem` reacquire command buffers after `EnsureEntityQueries` or other structural setup changes, with regression coverage in `SelectionCommandRequestResultContractTests`.
- 2026-06-14: Checked the next Phase 3 splits. Attack cannot be routed through the current pre-resolved ECS path blindly because the managed transition path can resolve base-breach orders through `BuildingPlacementInteractionSystem`, while `AttackOrderCommandSystem.OnUpdate` currently issues direct target orders only. Scan also needs a command-intent bridge first: `ScanIntelCommandSystem.OnUpdate` owns the scan queue, but RTS command-intent result mapping still happens synchronously in `ProcessCommandIntentRequests`.
- 2026-06-14: `RtsSelectionPointerTargetCommandSystem.TryIssueScanOrder` now resolves valid clicked cells at the pointer boundary and writes Scan command-intent requests carrying `TargetCell` and `WorldPosition`. `ScanIntelCommandSystem.OnUpdate` now consumes those pre-resolved RTS command-intent Scan requests directly, applies scan reveal/feed work, and emits `RtsSelectionCommandResultElement` output for the managed HUD/marker boundary to drain. Screen-only Scan requests remain as fallback for unresolved clicks. Added pending Scan result draining in `SelectionGameplayStartupSystem`, input coverage for resolved Scan request data, and command contract coverage for ECS Scan OnUpdate consumption.
- 2026-06-14: `RtsSelectionPointerTargetCommandSystem.TryIssueBoardTransportOrderToClickedUnit` now resolves boardable transport targets at the pointer boundary and writes `BoardTransport` command-intent requests carrying the transport entity. `TransportBoardingCommandSystem.OnUpdate` consumes those resolved transport requests directly, shares the existing passenger-first boarding mutation through a target-entity overload, and emits transport command results for the HUD/marker boundary to drain. Screen-only Board requests remain as fallback for unresolved clicks. Added pending transport result detection, input coverage for resolved Board request data, and transport validation coverage for ECS Board OnUpdate consumption.
- 2026-06-14: `RtsSelectionPointerTargetCommandSystem.TryIssueAttackOrderToClickedUnit` now resolves direct non-building attackable unit/entity targets at the pointer boundary and writes Attack command-intent requests carrying `TargetEntity`. `AttackOrderCommandSystem.OnUpdate` owns those resolved requests and emits Attack command results for the HUD/marker boundary to drain. Runtime-building/base-breach candidates and unresolved taps intentionally remain on the existing screen-resolved managed fallback path until breach-target data is represented explicitly in ECS request data.
- 2026-06-14: Added an explicit `PointerTargetBoundaryPass` inside `RtsSelectionPointerTargetCommandSystem`. Move, Attack, Scan, Board, and Focus command paths now ask that boundary pass for clicked cells/entities instead of directly calling camera/grid target lookup helpers. Public target lookup methods remain as transition wrappers for startup delegates. Added focused source validation so resolved command target paths continue to use the boundary pass.
- 2026-06-14: Made active world-target command releases return immediately after the active command path cleans release-scoped pointer state, so Attack/Move/Scan/Board clicks cannot fall through to normal focus selection. Building selection was already gated by `ShouldBlockBuildingSelectionClick` while command mode is active. Added `RuntimeInput_ActiveWorldCommandClickDoesNotFallThroughToFocusSelection`; focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=10`.
- 2026-06-14: Added behavioral coverage for command-mode pan. Attack target mode now has focused validation proving held pointer movement still pans the camera while targeting, and transport-first Board mode has focused validation proving non-passenger presses pan while passenger presses arm the passenger drag rectangle instead. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=12`.

## Phase 4: Focus Command Split

Status: [x]

Purpose:
Split `RtsSelectionFocusCommandSystem` into ECS command-mode and selection request processors.

Recommended disposition:

- Selection/focus state mutation becomes ECS.
- UI/HUD command-mode display stays presentation.
- Camera/pointer focus lookup stays managed boundary where required.

Implementation steps:

- [x] Convert Select All into an ECS request processor.
- [x] Convert Select All Soldiers into an ECS request processor.
- [x] Convert Select All Vehicles into an ECS request processor.
- [x] Convert Deselect All into an ECS request processor.
- [x] Convert Enter Selection Mode into an ECS request processor.
- [x] Convert Exit Selection Mode into an ECS request processor.
- [x] Convert Enter Move Target Mode into an ECS request processor.
- [x] Convert Enter Attack Target Mode into an ECS request processor.
- [x] Convert Enter Scan Target Mode into an ECS request processor.
- [x] Convert Enter Board Target Mode into an ECS request processor.
- [x] Convert Cancel Active Command Mode into an ECS request processor.
- [x] Keep HUD tab state as presentation of ECS command-mode state.
- [x] Add tests for selection command requests and command-mode transitions.

Acceptance checks:

- UI buttons only enqueue command intent.
- ECS owns command-mode state.
- HUD displays command-mode state but does not own gameplay state.

Progress notes:

- 2026-06-14: Added `RtsSelectionSelectAllCommandSystem` as an explicit `ISystem` request processor for `SelectAll`, `SelectAllSoldiers`, and `SelectAllVehicles` command-intent requests carrying a screen rectangle. `SelectionUiCommandSystem` now queues screen-rect command data for all three select-all commands instead of bare intents. The ECS processor consumes the requests, clears active command-mode state, maps each variant to the correct selection filter, and writes `SelectionRectCommitted` pointer requests; `SelectionGameplayStartupSystem` then performs managed HUD cleanup and drains the existing camera-aware rectangle selection boundary. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=15`.
- 2026-06-14: Added `RtsSelectionDeselectAllCommandSystem` as an explicit `ISystem` request processor for `DeselectAll` command-intent requests. The ECS processor consumes the request, clears active command-mode state, removes `SelectedUnitTag` from selected units, and leaves managed cache/HUD/focus cleanup in `SelectionGameplayStartupSystem` as the transition boundary. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `493`, Unity ECS exclusions are now `94`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=16` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Added `RtsSelectionModeCommandSystem` as an explicit `ISystem` request processor for `EnterSelectionMode` and `ExitSelectionMode` command-intent requests. The ECS processor consumes selection-mode requests, clears active command-mode state, resets selection drag/release suppression state, sets `RuntimeGameplayStateComponent.SelectionModeActive` and `SuppressNextWorldClick`, and clears pending Move requests only on enter to preserve legacy external-command behavior. `SelectionGameplayStartupSystem` keeps building-selection cleanup, HUD command-mode feedback, diagnostics, world-marker visibility, and camera-drag state in the managed boundary. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `494`, Unity ECS exclusions are now `95`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=18` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Fixed the move-command request flush path that could pass an invalidated `DynamicBuffer<RtsSelectionCommandIntentRequestElement>` into `SelectedMoveOrderCommandSystem` after intervening structural changes. `RtsSelectionCommandResultFlushSystem` now performs query setup before command-buffer acquisition and calls a new command-entity overload on `SelectedMoveOrderCommandSystem`, which reacquires fresh request/result buffers internally. Added a regression test for caller-side structural invalidation. Focused Unity validation passed with `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`.
- 2026-06-14: Added `RtsSelectionMoveTargetModeCommandSystem` as an explicit `ISystem` request processor for `EnterMoveTargetMode` command-intent requests. The ECS processor consumes Enter Move Target Mode requests, clears stale pending Move requests and queued move orders, validates selected movable player units from ECS data, arms `TacticalCommandMode.Move` as a one-shot world-target command on success, and reports `NoSelection` for the managed HUD boundary on rejection. `SelectionGameplayStartupSystem` keeps selected-building cleanup, HUD feedback, camera-drag state, and diagnostics in the managed boundary, and `RtsSelectionFocusCommandSystem` no longer owns the Enter Move Target Mode branch. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `495`, Unity ECS exclusions are now `96`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=20`.
- 2026-06-14: Added `RtsSelectionAttackTargetModeCommandSystem` as an explicit `ISystem` request processor for `EnterAttackTargetMode` command-intent requests. The ECS processor consumes Enter Attack Target Mode requests, clears stale pending Move requests and queued move orders, classifies selected ECS entities as normal attack-capable, air-defense-only, or not attack-capable, arms `TacticalCommandMode.Attack` as a one-shot world-target command on success, reports air-defense auto-engage as a managed HUD success message, and reports `NoSelection` or `TargetNotAttackable` on rejection. `SelectionGameplayStartupSystem` keeps explicit attack target presentation state, selected-building cleanup, HUD feedback, world-marker visibility, camera-drag state, and diagnostics in the managed boundary, and `RtsSelectionFocusCommandSystem` no longer owns the Enter Attack Target Mode branch. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `496`, Unity ECS exclusions are now `97`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=24`.
- 2026-06-14: Added `RtsSelectionScanTargetModeCommandSystem` as an explicit `ISystem` request processor for `EnterScanTargetMode` command-intent requests. The ECS processor consumes Enter Scan Target Mode requests, clears stale pending Move requests and queued move orders, resets selection drag/release suppression state, and arms `TacticalCommandMode.Scan` as a one-shot world-target command. `SelectionGameplayStartupSystem` keeps build-mode exit/cancel, selected-building cleanup, explicit attack state, HUD feedback, world-marker visibility, camera-drag state, and diagnostics in the managed boundary, and `RtsSelectionFocusCommandSystem` no longer owns the Enter Scan Target Mode branch. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `497`, Unity ECS exclusions are now `98`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=25`.
- 2026-06-14: Added `RtsSelectionBoardTargetModeCommandSystem` as an explicit `ISystem` request processor for `EnterBoardTargetMode` command-intent requests. The ECS processor consumes Enter Board Target Mode requests, clears stale pending Move requests and queued move orders, toggles off active Board mode, resolves selected board sources from ECS data with transport-first priority, arms `TacticalCommandMode.Board` with `TransportToPassenger` or `PassengerToTransport`, and reports `NoSelection` or `CommandUnavailable` for the managed HUD boundary on rejection. The transport source check is read-only and does not add capacity/passenger components while deciding command mode, avoiding structural-change invalidation during selection command processing. `SelectionGameplayStartupSystem` keeps selected-building cleanup, explicit attack state, HUD board-mode presentation, world-marker visibility, camera-drag state, and diagnostics in the managed boundary, and `RtsSelectionFocusCommandSystem` no longer owns the Enter Board Target Mode branch. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `498`, Unity ECS exclusions are now `99`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=29` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Added `RtsSelectionCancelActiveCommandModeSystem` as an explicit `ISystem` request processor for `CancelActiveCommandMode` command-intent requests. The ECS processor consumes cancel requests, clears active command-mode state including Board direction/transport data, exits selection mode, and suppresses the next world click without creating a persistent cancel feedback message. `SelectionGameplayStartupSystem` keeps explicit attack presentation state, camera-drag state, world-marker visibility, and HUD command-mode clearing in the managed boundary, and `RtsSelectionFocusCommandSystem` no longer owns the Cancel Active Command Mode branch. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: total runtime `*System` declarations are now `499`, Unity ECS exclusions are now `100`, and the runtime non-ECS denominator remains `390`. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=30` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Removed direct command-tab visual ownership from `MatchOverlayCommandInputSystem`. Command button clicks now enqueue ECS command-intent requests only; Select enter/exit is derived from the current HUD feedback state, and tab selection/clearing is driven by `BattleHudRuntimeFeedbackSystem` command-mode feedback. Updated HUD feedback tests so Select clicks record the ECS request while stale Board actions are cleared only when command-mode feedback is applied. Added a selection input guard that fails if command input starts constructing `MatchOverlayCommandTabVisualSystem`, toggling/selecting tabs, or directly applying HUD command modes again. Focused Unity validation passed with `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, `[RtsSelectionInputSystemValidation] result=Passed tests=31`, and `[MatchHudCommandFeedbackValidation] result=Passed tests=10`.
- 2026-06-14: Completed Phase 4 test coverage with `SelectionCommandRequests_ProcessUiRequestsThroughCommandModeTransitions`. The test starts from `SelectionUiCommandSystem` UI request methods, verifies the queued ECS command-intent kind, then runs the owning ECS processors for Enter Selection, Exit Selection, Enter Move Target, Enter Scan Target, and Cancel Active Command Mode. It asserts each request is consumed once and that runtime command-mode state transitions through selection active, selection inactive, Move target mode, Scan target mode, and finally no active command mode. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=32`.
- 2026-06-14: Started Phase 5 by converting Hold Position and Stop into `RtsSelectionImmediateSelectedUnitCommandSystem`. The UI still queues `RtsSelectionCommandIntentKind.HoldPosition` or `Stop`, but those requests are now consumed by an ECS processor before the legacy focus-command fallback. The processor clears command mode, removes stale Move requests on accepted immediate commands, applies the same selected-unit order cleanup as the former direct helper, adds or removes `HoldPositionOrderTag`, updates auto-engage for attack-capable units, and leaves HUD/build/camera feedback in the managed shell. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=35`; architecture validation passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Extended `RtsSelectionImmediateSelectedUnitCommandSystem` to consume `ReturnToBase` command-intent requests. The managed shell still owns UI feedback, world-marker visibility, and focused selection state, while the ECS processor clears command mode, suppresses the next world click, chooses the focused player unit when available, falls back to selected player units, resolves the matching `RespawnFactionSpawnPoint`, and applies the same immediate move component mutations as the former direct helper. Removed the old `ReturnFocusedSelectionToBase` callback path from `RtsSelectionFocusCommandSystem` and `SelectionGameplayStartupSystem`. Also fixed the move-command request path by copying pending move requests into an independent temp array before issuing orders that perform structural component changes, preventing stale `DynamicBuffer<RtsSelectionCommandIntentRequestElement>`/`BufferTypeHandle` access after structural changes. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=38`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Extended `RtsSelectionImmediateSelectedUnitCommandSystem` to consume `DestroyFocusedUnit` command-intent requests. Unit destruction now runs through ECS request processing: it destroys the focused player unit when valid, rejects focused enemy units without falling through to selected-unit fallback, and otherwise destroys selected player units. The managed shell still owns HUD/focused-selection cleanup and the selected-building deletion fallback because that path is a managed building placement boundary. Removed the old direct destroy callback branch from `RtsSelectionFocusCommandSystem`, removed the startup-shell `DestroyFocusedUnit`/selected-unit mutation helpers, and removed the obsolete public `FocusedUnitCommandSystem.DestroyFocusedUnit` direct mutation API. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=41`; architecture validation passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Extended `RtsSelectionAttackTargetModeCommandSystem` to consume `ToggleAttackTargetMode` command-intent requests for the non-radar fallback path. The managed startup boundary still tries the focused missile-launcher radar shortcut first, then lets the ECS processor validate focused/selected attack-capable units, arm Attack target mode, suppress the next world click, and preserve queued Move request state to match the former toggle fallback behavior. Removed Toggle from `RtsSelectionFocusCommandSystem` and from the external focus-command pending classifier. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=43`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Extended `RtsSelectionCancelActiveCommandModeSystem` to consume `CancelAttackTargetMode` as an ECS command-mode cancellation request. The processor now reports the processed cancel intent kind, clears active command-mode ECS state, exits selection mode, and suppresses the next world click. `SelectionGameplayStartupSystem` keeps explicit attack presentation, HUD command-mode clearing, world-marker visibility, and camera-drag cleanup in the managed boundary. Removed the old `CancelExplicitAttackTargetMode` callback from `RtsSelectionFocusCommandSystem` and its context factory. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=44`; architecture validation passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Added `RtsSelectionMissileLauncherRadarAttackCommandSystem` as a dedicated ECS command processor for the successful focused missile-launcher radar shortcut. The system consumes `ToggleAttackTargetMode` only after it validates a player-controlled missile launcher, resolves a radar target, and issues the direct commanded attack. `SelectionGameplayStartupSystem` now keeps only the managed presentation follow-up: attack marker, focus/selection presentation, explicit attack mode flag, HUD feedback, world-marker visibility, and camera dragging. Removed the old radar mutation API from `FocusedUnitCommandSystem`. Updated radar runtime tests to queue the Toggle request and use production `PlayerFactionId`/`EnemyFactionId`. Focused Unity validation passed with `[MissileLauncherRadarAttackRuntimeValidation] result=Passed tests=4`, `[RtsSelectionInputSystemValidation] result=Passed tests=44`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Completed Phase 5 result presentation mapping. `SelectionGameplayStartupSystem` now formats Hold, Stop, Return To Base, Destroy Focused Unit, and rejection feedback from the ECS command result tuple through `BuildImmediateSelectedUnitCommandResult`, with `TryGetImmediateSelectedUnitCommandMode` as the single mode mapping for immediate focused commands. Added direct EditMode coverage for accepted and rejected focused command result mapping. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=45`; architecture validation passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.

## Phase 5: Focused Unit Commands

Status: [x]

Purpose:
Convert focused-unit commands into explicit ECS command request processors.

Recommended disposition:

- Split simple command actions into ECS request processors.
- Separate automatic missile/radar targeting from focused UI command handling.

Implementation steps:

- [x] Convert Hold Position into an ECS request processor.
- [x] Convert Stop into an ECS request processor.
- [x] Convert Return To Base into an ECS request processor.
- [x] Convert Destroy Focused Unit into an ECS request processor.
- [x] Convert Toggle Attack Target Mode into ECS command-mode state mutation.
- [x] Convert Cancel Attack Target Mode into ECS command-mode state mutation.
- [x] Move missile/radar automatic target acquisition into a dedicated ECS automatic targeting system.
- [x] Add result codes and feedback mapping for each command.
- [x] Add tests for each focused command result.

Acceptance checks:

- Focused unit UI commands do not call public methods that mutate ECS directly.
- Missile/radar attack policy is not mixed with UI command-mode handling.
- Feedback reports requested, accepted, or failure reasons.

## Phase 6: Movement And Target Order Primitives

Status: [x]

Purpose:
Remove shared direct-call mutation helpers as public non-ECS systems.

Recommended disposition:

- Convert command execution into ECS systems.
- Fold reusable math into private/static helpers or Burst jobs.
- Replace direct method calls with request components/buffers.

Implementation steps:

- [x] Inventory all callers of `UnitMoveOrderSystem`.
- [x] Replace direct move order calls with `UnitMoveOrderRequest` data.
- [x] Convert order clearing into ECS requests or command-buffered ECS helpers.
- [x] Replace direct path-request mutation with ECS order application.
- [x] Inventory all callers of `UnitTargetOrderSystem`.
- [x] Replace direct attack target calls with `UnitAttackOrderRequest` data.
- [x] Replace radar target direct issuing with ECS automatic targeting requests.
- [x] Batch structural changes through ECBs.
- [x] Add tests for move order, attack order, clear order, and path request output.

Acceptance checks:

- Command systems do not call public `UnitMoveOrderSystem` or `UnitTargetOrderSystem` methods for mutation.
- Structural changes are ECB-backed unless same-frame behavior is documented.
- Existing Move, Attack, Board, Return, Hold, Stop, and debug-fire behavior still works.

Progress notes:

- 2026-06-14: Inventoried runtime `UnitMoveOrderSystem` callers. Direct runtime mutation paths are:
  - `SelectedMoveOrderCommandSystem`: uses `FindManualMoveGoal` and `IssueGroupedManualMoveOrder` for resolved Move command requests.
  - `TransportBoardingCommandSystem`: uses clear/immediate move helpers and ECB-backed clear helper for passenger boarding, disembark, and transport boarding cleanup.
  - `UnitTransportAirPickupSystem`: clears movement state and issues target-only pickup movement for air transports.
  - `SelectionGameplayStartupSystem`: still uses clear/immediate move helpers in the managed Board All transport presentation boundary after planning orders.
  - `FocusedUnitCommandSystem`: still contains public move-order mutation helpers, but current runtime startup no longer calls those helpers; remaining references are tests and the startup construction site. Treat this as a fold/remove cleanup after the equivalent ECS processors and tests own the behavior.
  - `RtsSelectionCommandResultContextSystem` and `RtsSelectionCommandResultFlushSystem`: pass the shared dependency into move/transport processing and should shrink as those processors become ECS request systems.
  Test-only and performance-validation references are not runtime conversion blockers.
- 2026-06-14: Added `UnitMoveOrderRequestSystem` plus `UnitMoveOrderRequestElement`/`UnitMoveOrderResultElement` request data. `SelectedMoveOrderCommandSystem` now enqueues grouped manual move requests and synchronously flushes the ECS request processor to preserve same-frame command result, marker, and diagnostics behavior. Board All, transport boarding, air-pickup, and the legacy focused-unit helper now route immediate/target-only move issuing through the same request processor. Direct public `UnitMoveOrderSystem.Issue*MoveCommand` runtime calls are now isolated inside `UnitMoveOrderRequestSystem`; direct order-clearing calls remain open for the next Phase 6 checklist item.
- 2026-06-14: Extended `UnitMoveOrderRequestSystem` with `ClearMovement` requests and an ECB-backed clear helper. Standalone clear callers in Board All, transport boarding, rope disembark setup, and air-pickup now enqueue and synchronously flush clear requests. Existing disembark loops that already batch `Disabled`, passenger state, grid, and transform mutations now use `UnitMoveOrderRequestSystem.ClearMovementOrderComponents` with their local ECB. Runtime direct calls to public `UnitMoveOrderSystem.ClearMovementOrderComponents` are now isolated inside `UnitMoveOrderRequestSystem`; test-only direct coverage remains for the legacy helper until it is folded. Focused Unity validation passed with `[UnitMoveOrderFocusedValidation] result=Passed tests=12`, `[RtsSelectionInputSystemValidation] result=Passed tests=45`, `[UnitTransportValidation] result=Passed tests=19`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=2`.
- 2026-06-14: Continued direct path-request mutation replacement by routing `RtsSelectionImmediateSelectedUnitCommandSystem` Return To Base movement through `UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder` and folding Hold/Stop movement clearing through `UnitMoveOrderRequestSystem.ClearMovementOrderComponents`. The local duplicate immediate-move writer and its `UnitPathRequest` add/set/remove branches were removed from the selection immediate command processor. `CitizenMovementCommandSystem` and `BuildingTargetMoveOrderSystem` now also delegate immediate movement to `UnitMoveOrderRequestSystem`; citizen request buffers are copied before structural changes and result buffers are reacquired after movement application to avoid invalidated `DynamicBuffer` safety handles. Broader direct `UnitPathRequest` writers remain in other gameplay owners and need classification before this checklist item is complete. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=45`, `[UnitMoveOrderFocusedValidation] result=Passed tests=12`, `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`, and `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- 2026-06-14: Added `UnitMoveOrderRequestKind.TargetPathOnly` for systems that must write only `UnitTarget` plus `UnitPathRequest` without converting the operation into a generic manual move order. `BuildingProductionTransportBridgeSystem`, `BuildingResourceHaulerBridgeSystem`, and `BuildingPlacementRedirectSystem` now use that centralized ECS move-order request path for target/path writes while preserving their existing tag and cleanup behavior. `UnitTargetOrderSystem` base-breach attack movement now uses `UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder` before adding `BaseBreachOrder`. Remaining direct writers are now central move-order application (`UnitMoveOrderSystem`, `UnitMoveOrderRequestSystem`) plus pathing/AI/internal recovery owners (`UnitManualMoveRetrySystem`, `UnitGridMovementSystem`, `UnitIdleWanderSystem`, `BaseBreachOrderSystem`, `AICombatOrderSystem`, `UnitAttackSystem`, `EngageTargetValidateSystem`) that need explicit classification or later target-order conversion. Focused Unity validation passed with `[UnitMoveOrderFocusedValidation] result=Passed tests=13` and `[UnitTargetOrderFocusedValidation] result=Passed tests=6`.
- 2026-06-14: Completed the command/boundary direct path-request replacement by adding `NonEcsSystemConversionArchitectureTests.DirectUnitPathRequestWritesStayInApprovedOrderOwners`. New direct `UnitPathRequest` creation is now guarded to stay inside centralized move-order application, pathing, AI, or internal recovery owners; command/UI/boundary code must use `UnitMoveOrderRequestSystem`.
- 2026-06-14: Inventoried runtime `UnitTargetOrderSystem` callers. Direct mutation paths are:
  - `AttackOrderCommandSystem`: calls `IssueAttackTarget` for pre-resolved attack requests and fallback selected-source attack queries; this is the primary candidate for `UnitAttackOrderRequest` data.
  - `RtsSelectionMissileLauncherRadarAttackCommandSystem`: calls `TryFindRadarTargetForMissileLauncher` and `IssueDirectAttackTarget` for radar-driven missile attack; split into automatic targeting/result requests instead of keeping direct mutation.
  - `FocusedUnitCommandSystem`: calls `ClearCommandedAttackOrderComponents` when enabling focused-unit auto attack; convert to an attack-clear request or fold into the future target-order ECS owner.
  - `FocusedUnitLifecycleSystem`: calls `ClearAccidentalAirSelectionMove` and `IsBuildingEntity`; classify as lifecycle/read-policy helper use and fold narrow logic into ECS owners when attack requests are converted.
  - `SelectionGameplayStartupSystem`: constructs/passes `UnitTargetOrderSystem` and uses `ValidateAttackSource` for debug-fire startup flow; replace with ECS validation/result data when debug-fire attack issuing moves.
  - `RtsSelectionPointerTargetCommandSystem`, `RtsSelectionFocusCommandSystem`, `RtsSelectionCommandResultFlushSystem`, and their context builders pass `UnitTargetOrderSystem` through command contexts; these should shrink once attack requests/results own target mutation.
  Test-only and performance-validation references are not runtime conversion blockers.
- 2026-06-14: Started replacing direct attack target mutation with ECS request data. Added `UnitAttackOrderRequestSystem` plus `UnitAttackOrderRequestElement`/`UnitAttackOrderResultElement`; `AttackOrderCommandSystem` now creates the attack request queue in `OnCreate`, keeps its entity type handle cached/updated, and routes pre-resolved Attack command-intent requests through the request/result processor before mapping the result back to `RtsSelectionCommandResultElement`. The managed click-resolution fallback and radar/focused-unit target-order paths remain direct callers for follow-up slices, so this checklist item stays in progress. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=7` and `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`.
- 2026-06-14: Routed `AttackOrderCommandSystem.IssueAttackTarget` through `UnitAttackOrderRequestSystem` for the pure selected-source fallback path when no managed base-breach resolver or custom source collector is required. Managed click fallback with base-breach resolution, radar missile launcher direct issuing, focused-unit attack clearing, and lifecycle/read-policy helper calls remain as the open direct `UnitTargetOrderSystem` follow-up paths. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=7` and `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`.
- 2026-06-14: Completed radar direct issuing replacement by adding `UnitAttackOrderRequestKind.RadarAttackTarget`. `RtsSelectionMissileLauncherRadarAttackCommandSystem` now removes the pending attack-toggle request and writes a radar attack request; `UnitAttackOrderRequestSystem` owns radar target acquisition through the existing target-order policy helper and applies the direct commanded attack mutation. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=8`, `[MissileLauncherRadarAttackRuntimeValidation] result=Passed tests=4`, and `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`.
- 2026-06-14: Added `UnitAttackOrderRequestKind.ClearCommandedAttackOrder` and routed `FocusedUnitCommandSystem.EnableFocusedUnitAutoAttack` through `UnitAttackOrderRequestSystem` instead of directly calling `UnitTargetOrderSystem.ClearCommandedAttackOrderComponents`. Remaining open direct target-order follow-ups are the managed click/base-breach attack fallback, lifecycle read-policy helpers, and debug-fire validation flow. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=9`, `[FocusedUnitCommandFocusedValidation] result=Passed tests=3`, and `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`.
- 2026-06-14: Added `UnitAttackOrderRequestKind.ClearAccidentalAirSelectionMove` and routed `FocusedUnitLifecycleSystem` air-unit focus cleanup through `UnitAttackOrderRequestSystem` instead of directly calling `UnitTargetOrderSystem.ClearAccidentalAirSelectionMove`. Folded the focused-unit building classification check into `FocusedUnitLifecycleSystem`, removed the stale target-order dependency from `RtsSelectionFocusCommandSystem`, and removed the unused target-order parameter from `FocusedUnitCommandSystem.EnableFocusedUnitAutoAttack`. Remaining open direct target-order follow-ups are the managed click/base-breach attack fallback, pointer attack-target validation, command-result flush context, and debug-fire validation flow. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=10`, `[SelectionStateFocusedValidation] result=Passed tests=7`, `[FocusedUnitCommandFocusedValidation] result=Passed tests=3`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Folded read-only pointer attack-target eligibility into `RtsSelectionPointerTargetCommandSystem` and removed `UnitTargetOrderSystem` from the pointer-target context. This keeps the managed camera/pointer boundary responsible only for resolved target request data and removes another target-order pass-through from selection input. Remaining open direct target-order follow-ups are the managed click/base-breach attack fallback, command-result flush context, and debug-fire validation flow. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=45`.
- 2026-06-14: Removed `UnitTargetOrderSystem` from `RtsSelectionCommandResultFlushSystem` and `RtsSelectionCommandResultContextSystem`; attack command flushing now calls `AttackOrderCommandSystem` without a target-order dependency. Folded debug-fire attack-source validation into `SelectionGameplayStartupSystem` so startup no longer constructs or calls `UnitTargetOrderSystem`. Remaining open runtime target-order calls are isolated inside `AttackOrderCommandSystem` for the managed click/base-breach/custom-source transition path and inside `UnitAttackOrderRequestSystem` as the current ECS request owner. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=10`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, `[GroundMissileAttackFocusedValidation] result=Passed tests=5`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Completed direct attack target request conversion by adding per-source `UnitAttackOrderRequestKind.SourceAttackTarget` and `SourceBaseBreachAttackTarget` requests. `AttackOrderCommandSystem` now enqueues selected-source, custom-source, and pre-resolved base-breach attack requests, then aggregates request results for HUD feedback and marker data; `UnitTargetOrderSystem` target mutation is isolated inside `UnitAttackOrderRequestSystem`, the current ECS request owner. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=10`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, `[GroundMissileAttackFocusedValidation] result=Passed tests=5`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Batched remaining `UnitTargetOrderSystem` structural writes through local `EntityCommandBuffer` playback. Attack issue, direct attack issue, commanded attack clear, and accidental air-selection move cleanup now queue component add/set/remove operations through ECBs while preserving the same-frame base-breach clear -> move-order -> breach-order sequence. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=10`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=13`, `[GroundMissileAttackFocusedValidation] result=Passed tests=5`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Completed Phase 6 focused output coverage by adding source/base-breach attack request coverage and an ECB clear/re-add regression for replacing an existing `BaseBreachOrder`. Existing move-order focused coverage already asserts grouped move, target/path-only, clear, selected move, and building-target output. Focused Unity validation passed with `[UnitTargetOrderFocusedValidation] result=Passed tests=12`, `[UnitMoveOrderFocusedValidation] result=Passed tests=13`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.

## Phase 7: Building, Production, And Road Commands

Status: [x]

Purpose:
Split command-shaped building and road code that currently mixes UI, GameObjects, placement sessions, resources, and camera focus.

Recommended disposition:

- ECS owns gameplay state, resource validation, production requests, placement validation, and command results.
- Managed boundaries own prefab/GameObject/session visuals and camera focus.

Implementation steps:

- [x] Split `BuildingUiCommandSystem` into passive UI request creation plus ECS command processors.
- [x] Split `BuildingProductionRequestBoundary` into ECS production validation/request processing and managed prefab/config boundary.
- [x] Split `BuildingPlacementCommandSystem` into ECS placement command state plus managed placement visual/session boundary.
- [x] Split `RoadBuildCommandSystem` into ECS road-build command state plus managed road-build visual/session boundary.
- [x] Split `BuildingRuntimeSpawnCommandSystem` into ECS spawn request data and managed prefab spawn boundary.
- [x] Add stable result codes for not enough money, missing producer, invalid placement, blocked placement, unavailable prefab, and queue full.
- [x] Add tests for production request results.
- [x] Add tests for placement confirm/cancel/rotate results.
- [x] Add tests for road build enter/confirm/cancel/exit results.

Acceptance checks:

- Build drawer buttons enqueue ECS requests.
- Production/placement failures are represented as ECS results.
- Camera focus and prefab instantiation remain managed boundaries.
- No building command code owns direct UI text or button state.

Progress notes:

- 2026-06-14: Started the `BuildingUiCommandSystem` split by adding ECS `BuildingUiProductionCommand*` request/result buffers for selected-building, explicit-building, and cancel-production commands. `BuildingUiContextSystem` now routes building UI production actions and production cancel actions through those buffers and immediately flushes them through the managed production boundary to preserve same-frame UI responsiveness and the existing production-arm guard. `BuildingProductionRequestBoundary` now records accepted/rejected result codes for queued, cancelled, missing active building, missing pending production, missing producer, missing unit config, not armed, queue rejected, and cancel rejected. Focused Unity validation passed with `[BuildingProductionRequestValidation] result=Passed tests=3`, `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Extended the Phase 7 UI command split to placement confirm, cancel, rotate, and exit actions. Added ECS `BuildingUiPlacementCommand*` queue/request/result buffers and `BuildingPlacementCommandSystem` enqueue/process helpers that copy pending requests before invoking the managed placement session boundary, preserving same-frame UI responsiveness while keeping GameObject/session work out of unmanaged ECS/Burst. `BuildingUiCompositionSystem` and `BuildingPlacementInteractionCompositionSystem` now route placement UI actions through the request buffer when an ECS world is available, with no-world fallback for editor/teardown paths. Focused Unity validation passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=3`, `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Extended the Phase 7 UI command split to build-drawer camp item actions. Added ECS `BuildingUiCampItemCommand*` queue/request/result buffers keyed by normalized item id plus price/focus flags, and routed `BuildingUiContextSystem.TryRequestCampItem` through `BuildingProductionRequestBoundary.EnqueueAndProcessCampItemRequest` when an ECS world is available. The processor resolves the item id through the existing managed building/unit config boundary, emits stable result codes for placement started, production queued, not enough money, missing producer, and invalid selection, and still returns the existing `CampRequestFailure` enum to keep presenter behavior unchanged. Added focused production-result coverage for building placement and unit production camp item requests. Focused Unity validation passed with `[BuildingProductionRequestValidation] result=Passed tests=5`, `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Extended the Phase 7 UI command split to selected-building delete and clear actions. Added ECS `BuildingUiSelectionCommand*` queue/request/result buffers and `BuildingSelectionSystem` enqueue/process helpers that copy pending requests before invoking the managed runtime-building selection/delete boundary, preserving same-frame UI feedback while keeping runtime object mutation outside unmanaged ECS. `BuildingUiCompositionSystem` and `BuildingPlacementInteractionCompositionSystem` now route delete/clear UI actions through the request buffer when an ECS world is available, with no-world fallback for editor/teardown paths. Focused Unity validation passed with `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`, `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Completed the Phase 7 placement result test checklist item by expanding `BuildingPlacementCommandRequestValidation` from 3 to 5 focused tests. New coverage asserts accepted confirm requests emit completed ECS results and commit the active placement, while accepted rotate requests emit completed ECS results and toggle the active placement rotation state. Focused Unity validation passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=5`.
- 2026-06-14: Started the `RoadBuildCommandSystem` split by adding ECS `RoadBuildCommand*` queue/request/result buffers for enter road-build mode, confirm road-build session, cancel road-build session, and exit build mode. The processor copies pending requests before invoking the managed road-build session boundary, writes stable completed/missing/rejected result codes, and keeps visual/session callbacks managed. Added focused coverage for accepted enter/confirm/cancel/exit command results and session side effects. Focused Unity validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=4`.
- 2026-06-14: Wired queued `RoadBuildCommandSystem` requests into `RoadBuildRuntimeActionSystem.Update` through the existing road-build ECS boundary entity-manager delegate. The runtime action now processes queued road-build requests once per tick before managed input handling, while visual/session effects remain in the managed road-build command context. Road-build disposal now routes exit through the ECS request/result buffer when an entity manager exists and keeps the direct exit call only as a no-world teardown fallback. Added focused coverage proving an enqueued enter-road-build request is processed through the runtime update path and that the enqueue/process exit helper writes an accepted result. Focused Unity validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=6`.
- 2026-06-14: Extended placement confirmation results with stable ECS failure codes for missing active placement, blocked placement, invalid placement, and not enough money. `BuildingPlacementLifecycleSystem` now exposes a reason-preserving confirm overload while the existing bool API remains intact, and `BuildingPlacementCommandSystem` maps those reasons into `BuildingUiPlacementCommandResultElement` values. Focused Unity validation passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=9`.
- 2026-06-14: Completed the Phase 7 stable result-code checklist item by adding production result codes for unavailable prefab and queue-full style rejection. `BuildingProductionRequestBoundary` now reports unavailable prefab when a resolved producer has no production prefab and queue full when an otherwise valid production request cannot be queued. Focused Unity validation passed with `[BuildingProductionRequestValidation] result=Passed tests=7`.
- 2026-06-14: Advanced the `BuildingProductionRequestBoundary` split by adding non-creating ECS queue drain helpers and wiring `BuildingRuntimeBoundarySystem` to process queued `BuildingUiProductionCommand*` and `BuildingUiCampItemCommand*` requests during the runtime boundary tick with the current frame count. This keeps prefab/config/session work in the managed boundary but makes queued production ECS requests tick-owned instead of only same-frame helper-owned. Focused Unity validation passed with `[BuildingProductionRequestValidation] result=Passed tests=9`.
- 2026-06-14: Advanced the `BuildingProductionRequestBoundary` split by moving the selected-building production arm frame into `BuildingUiProductionCommandRequestElement.FrameCount`. The processor now rejects stale production requests by comparing request frame data with the processing frame instead of reading mutable `_armedProductionFrame` state from the plain C# boundary object, and the direct public unit-production wrapper methods were removed. Added stale-frame regression coverage. Focused Unity validation passed with `[BuildingProductionRequestValidation] result=Passed tests=10` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Advanced the `BuildingProductionRequestBoundary` split by removing the unused camp-production focus cache and `FocusLastCampProductionRequest` replay method. Camp requests that ask to focus the producer still invoke the managed focus boundary immediately on accepted production; non-focus requests no longer leave stale `RuntimeBuildingEntity`/prefab references in the boundary object. Search validation found no remaining runtime or test callers for the removed method.
- 2026-06-14: Completed the production request split by renaming `BuildingProductionRequestSystem` to `BuildingProductionRequestBoundary` and moving the source `.meta` with the file to preserve Unity GUIDs. The renamed type no longer appears in the runtime non-ECS `*System` inventory; queued production and camp item requests are still drained by the runtime boundary tick, while prefab/config/camera focus work remains a managed boundary. Focused Unity validation passed with `[BuildingProductionRequestValidation] result=Passed tests=10`, `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Advanced the `BuildingPlacementCommandSystem` split by adding a non-creating ECS placement queue drain helper and wiring `BuildingPlacementInputRuntimeTickSystem` to process queued placement confirm/cancel/rotate/exit requests before camera-dependent pointer work. This keeps placement visual/session work in the managed boundary while giving queued placement requests a runtime tick owner. Focused Unity validation passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=10`.
- 2026-06-14: Advanced the `BuildingPlacementCommandSystem` split by removing direct confirm/rotate/cancel/exit command helpers from the command processor. UI and placement interaction composition now keep no-`EntityManager` fallbacks as explicit managed boundary calls, while the command system public surface for those actions is enqueue/process/result based. Focused Unity validation passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=10` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Completed the `BuildingPlacementCommandSystem` split by adding ECS begin-configured-placement request data keyed by normalized spawnable id and routing Soldier Base/build-drawer placement starts through the same enqueue/process/result path when an `EntityManager` exists. No-`EntityManager` placement-start fallbacks remain isolated in managed composition boundaries, and direct begin/confirm/rotate/cancel/exit command helpers have been removed from the command processor. Focused Unity validation passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=11` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Started the `BuildingRuntimeSpawnCommandSystem` split by adding command-level enqueue/result helpers over existing ECS `BuildingRuntimeSpawnRequest` data on the runtime boundary entity. The managed prefab/GameObject spawn remains inside `BuildingRuntimeBoundarySystem`, and focused validation now proves command-enqueued spawn requests complete through that boundary. Focused Unity validation passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=2`.
- 2026-06-14: Extended the `BuildingRuntimeSpawnCommandSystem` ECS request API to wall-run and wall-segment request kinds. `BuildingRuntimeBoundarySystem` now records wall-segment actual origin/footprint metadata from the managed runtime spawn boundary while prefab/GameObject work remains outside unmanaged ECS, and focused coverage now validates building, wall-run, and wall-segment command-enqueued requests. Focused Unity validation passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=4` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Completed the `RoadBuildCommandSystem` split by removing unused public direct command helpers from the command system. External road-build command execution now uses enqueue/process/result APIs, runtime ticks drain queued ECS road-build commands, and the remaining no-`EntityManager` disposal path is isolated in `RoadBuildCompositionLifecycleSystem` as managed teardown. Focused Unity validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=6` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Advanced the `BuildingRuntimeSpawnCommandSystem` split by routing runtime city building spawns through `BuildingRuntimeSpawnRequest` when the prefab has configured spawn data and an ECS boundary is available. The request path drains only runtime spawn requests through the managed `BuildingRuntimeBoundarySystem`, preserves unowned city buildings with explicit owner-presence request data, and falls back to the prior direct managed spawn only when the ECS request path is unavailable. Focused Unity validation passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=5`.
- 2026-06-14: Advanced the `BuildingRuntimeSpawnCommandSystem` split by adding explicit owner-presence data to the runtime spawn command enqueue API. Existing owned building spawn requests keep `HasOwnerFaction=1` by default, while runtime city spawns now enqueue `HasOwnerFaction=0` directly instead of mutating the boundary request buffer after enqueue. This keeps the managed prefab spawn boundary unchanged and makes the ECS request payload authoritative for owner intent.
- 2026-06-14: Advanced the `BuildingRuntimeSpawnCommandSystem` split by removing unused direct `SpawnInitialTestRoster` and `TrySpawnInitialBuilding` wrapper methods. The spawn-command context also no longer carries Soldier Base, Soldier Tent, or Factory definitions solely for those removed wrappers; live initial placement origin resolution and runtime spawn request paths are unchanged. Focused Unity validation passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=5` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Advanced the `BuildingRuntimeSpawnCommandSystem` split by moving initial placement origin resolution out of the spawn command owner. `BuildingPlacementAdapterSystem` now creates the narrow runtime spawn context and calls `BuildingRuntimeSpawnSystem.TryResolveInitialPlacementOrigin` directly for the read-only placement query, leaving `BuildingRuntimeSpawnCommandSystem` focused on ECS spawn request enqueue/result APIs plus still-open managed spawn wrappers.
- 2026-06-14: Advanced the `BuildingUiCommandSystem` and `BuildingProductionRequestBoundary` split by removing the placement-interaction selected-building unit production action's hop through `BuildingUiCommandSystem`. `BuildingPlacementInteractionCompositionSystem` now builds the existing managed production request context and calls `BuildingProductionRequestBoundary.EnqueueAndProcessCreateUnitFromSelectedBuilding` directly when an `EntityManager` is available, preserving the same-frame request/result flow and the existing production-arm frame guard.
- 2026-06-14: Advanced the `BuildingUiCommandSystem` split by removing the now-unused direct unit-production delegates and wrapper methods from `BuildingUiCommandSystem.Context`. Unit production from UI and placement interaction now routes through `BuildingProductionRequestBoundary` request/result helpers, while the remaining UI command contract surface is limited to camp requests, production cancellation, placement confirmation/cancel/rotate, and passive UI state.
- 2026-06-14: Advanced the `BuildingUiCommandSystem` split by removing stale delete/clear/exit/focus/arm delegates and wrapper methods from the UI command context. Delete and clear selection remain routed through `BuildingSelectionSystem` request/result owners from placement interaction, placement confirm/cancel/rotate stay on `BuildingPlacementCommandSystem` request/result paths, and production focus/arm state stays inside `BuildingProductionRequestBoundary` instead of being exposed as UI command methods. `BuildingUiCompositionSystem` also dropped the now-dead runtime-entity dependency that only existed for the removed delete delegate. Focused Unity validation passed with `[BuildDrawerCatalogQueryValidation] result=Passed tests=21` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Completed the `BuildingUiCommandSystem` split by renaming the remaining pass-through UI command shell to `BuildingUiCommandBoundary`. The type no longer appears in the runtime non-ECS `*System` inventory; it only exposes the managed UI boundary contract over ECS request/result owners for camp items, production cancellation, placement confirmation/cancel/rotate, and passive UI state. Regenerated `Design/Architecture/non_ecs_to_ecs_system_inventory.md`: runtime non-ECS denominator is now `389`, ConvertToSystemBase candidates are now `288`, and passive boundary completions are now `1`.
- 2026-06-14: Synced the completed runtime spawn-command split with the current code. `BuildingRuntimeSpawnCommandSystem` is no longer present in runtime sources or the generated non-ECS inventory; `BuildingRuntimeSpawnCommandBoundary` owns enqueue/result helpers over ECS `BuildingRuntimeSpawnRequest` data, and `BuildingRuntimeBoundarySystem` remains the managed prefab/GameObject spawn boundary. Current focused validation for the boundary remains `[BuildingRuntimeBoundaryValidation] result=Passed tests=5`.

## Phase 8: HUD Feedback, Markers, And Presentation Boundaries

Status: [x]

Purpose:
Make feedback and marker systems consume ECS read models/results instead of owning gameplay decisions.

Recommended disposition:

- Presentation systems that touch UI views or UnityEngine objects become `SystemBase` or remain passive view helpers.
- Gameplay state and command result logic remains ECS data.

Implementation steps:

- [x] Convert command feedback queue consumption into a managed ECS presentation boundary.
- [x] Ensure `BattleHudRuntimeFeedbackSystem` only maps/display results and lifetimes.
- [x] Ensure `SelectionHudFeedbackSystem` does not execute gameplay commands.
- [x] Ensure `SelectionOrderMarkerSystem` consumes marker result data instead of being called from command execution.
- [x] Convert marker requests/results to ECS data where practical.
- [x] Add tests for persistent command prompts and transient result feedback.
- [x] Add tests for move/attack/scan/board marker output.

Acceptance checks:

- Feedback panel is driven by ECS command-mode/result data.
- Marker visuals are presentation of marker request data.
- UI does not need command-specific gameplay logic.

Progress notes:

- 2026-06-14: Synced Phase 8 with the current HUD feedback boundary state. `SelectionHudFeedbackBoundary` owns `SelectionHudFeedbackQueueComponent`/`SelectionHudFeedbackElement` queue creation, drain, and mapping to the battle HUD view; `BattleHudRuntimeFeedbackBoundary` maps command modes, command results, sticky modes, actions, and result lifetimes without gameplay mutation. The retired `SelectionHudFeedbackSystem` and `BattleHudRuntimeFeedbackSystem` sources are no longer present.
- 2026-06-14: Confirmed persistent command prompts and transient result feedback are covered by `MatchHudCommandFeedbackPanelTests` through command-mode prompt lifetime, success auto-hide, rejected auto-hide, board error prompt restoration, and board success prompt clearing cases.
- 2026-06-14: Confirmed marker presentation now consumes accepted `RtsSelectionCommandResultElement` data through `SelectionOrderMarkerSystem.TryShowCommandResultMarker`. `RtsSelectionCommandResultFlushSystem` owns the managed marker presentation boundary for Move, Attack, Scan, and Board results, and `SelectedMoveOrderCommandSystem` no longer carries an order-marker dependency. The practical ECS marker data source remains the command-result buffer because it already carries accepted state, target entity/cell/world position, faction, radius, and marker flags. Focused Unity validation passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=13`, `[SelectionCommandRequestResultContractValidation] result=Passed tests=15`, `[UnitMoveOrderFocusedValidation] result=Passed tests=13`, `[RtsSelectionInputSystemValidation] result=Passed tests=46`, and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.

## Phase 9: Query, Rule, Cell, And Helper Cleanup

Status: [~]

Purpose:
Remove misleading standalone non-ECS helper `*System` types.

Recommended disposition:

- Fold pure helpers into owning ECS systems/jobs.
- Keep only helpers that are clearly private/internal implementation details and do not own runtime state.
- Do not rename gameplay helpers to `Query`, `Rule`, `Cell`, `Resolver`, `Adapter`, `Composer`, or `Context`.

Implementation steps:

- [x] Inventory pure helper `*System` types such as transport boarding rule/query/approach helpers.
- [~] Fold transport boarding helper logic into the transport command ECS owner or private helper structs inside that owner.
- [ ] Fold map-surface command target logic into the pointer target boundary or map-surface ECS owner.
- [ ] Fold road footprint/grid helper logic into road/building ECS owners.
- [ ] Fold small initial spawn cell/helper systems into initial spawn ECS owners.
- [ ] Remove or rename files only when Unity `.meta` preservation is planned.
- [ ] Add tests around helper behavior before folding risky algorithms.

Acceptance checks:

- No public gameplay helper remains as a plain non-ECS `*System` without an explicit owner.
- Folded helpers do not create broad owners.
- Existing pathing, boarding, placement, and selection behavior is preserved.

Progress notes:

- 2026-06-14: Started Phase 9 helper cleanup with the transport boarding helper cluster. `UnitTransportBoardingQuerySystem`, `UnitTransportBoardingRuleSystem`, and `UnitTransportApproachCellSystem` are pure/helper-style `*System` types that should be folded into `TransportBoardingCommandSystem`, `UnitTransportBoardingSystem`, or private helper structs as their owning call sites are narrowed. First slice moved the shared boarding reach-state payload out of `UnitTransportBoardingRuleSystem` and into neutral ECS transport boarding data as `TransportBoardingReachState`; `UnitTransportBoardingSystem` and `UnitTransportBoardingDiagnosticSystem` now share that payload without depending on a nested type on the standalone rule helper. Command-side rule-helper calls remain open for the next transport fold slice. Focused Unity validation passed with `[UnitTransportValidation] result=Passed tests=19` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.
- 2026-06-14: Continued the transport helper fold by moving shared boarding clearance constants and air-grounding tolerance from `UnitTransportBoardingRuleSystem` into neutral `TransportBoardingData`. `UnitTransportBoardingSystem`, `UnitTransportAirPickupSystem`, and `UnitTransportApproachCellSystem` now read those values from transport boarding data instead of the standalone rule helper; the rule helper keeps compatibility aliases until command-side calls are folded. Focused Unity validation passed with `[UnitTransportValidation] result=Passed tests=19` and `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=3`.

## Phase 10: Bootstrap, Composition, And Runtime Lifecycle

Status: [ ]

Purpose:
Reduce broad composition/startup systems without turning them into gameplay shells.

Recommended disposition:

- Entity initialization and gameplay startup projection move into ECS initialization systems.
- Serialized scene wiring stays in thin composition roots.
- Long runtime update sequences move into explicit ECS or narrow boundary systems.

Implementation steps:

- [ ] Audit `SelectionGameplayStartupSystem` responsibilities after command conversion.
- [ ] Move remaining selection gameplay policy into ECS systems.
- [ ] Leave only binding/composition responsibilities at the managed scene edge.
- [ ] Audit `MatchBootstrapSystem`, `MenuBootstrapSystem`, `GameplayFeatureStartupSystem`, and `ManagedGameplayStartupSystem`.
- [ ] Move authored data projection into ECS initialization systems where safe.
- [ ] Keep scene references serialized/injected, never found by runtime hierarchy lookup.
- [ ] Add contract tests preventing broad shells from growing new gameplay policy.

Acceptance checks:

- Bootstrap/composition code does not own mission, combat, selection, production, or AI policy.
- Runtime gameplay policy lives in ECS systems.
- Scene/UI wiring remains explicit and serialized.

## Phase 11: Visual, Camera, Prefab, And Environment Boundaries

Status: [ ]

Purpose:
Classify and split non-ECS systems that must interact with UnityEngine managed objects.

Recommended disposition:

- ECS data planning can use `ISystem`.
- Managed visual/prefab/camera application uses `SystemBase` or passive views.
- Environment generation should be split into data plan plus managed spawn boundary.

Implementation steps:

- [ ] Inventory camera systems and convert only request/data parts to ECS.
- [ ] Keep camera application in managed boundary code.
- [ ] Inventory building/unit visual systems and split ECS read-model updates from GameObject visual application.
- [ ] Inventory runtime city/environment generation systems.
- [ ] Split runtime city planning/data from prefab spawning and coroutine/yield boundaries.
- [ ] Keep editor-only tooling out of runtime conversion.
- [ ] Add visual/prefab boundary tests where serialized references are required.

Acceptance checks:

- No `ISystem` touches managed Unity objects.
- Visual systems do not own gameplay policy.
- Environment generation has a clear ECS data/planning side and managed spawn side.

## Phase 12: Final Guardrails And Cleanup

Status: [ ]

Purpose:
Prevent the project from drifting back to plain non-ECS gameplay systems.

Implementation steps:

- [ ] Add or update architecture tests so new runtime gameplay `*System` classes must inherit `ISystem` or `SystemBase`, or be explicitly classified.
- [ ] Add guardrails for direct UI-to-gameplay mutation.
- [ ] Add guardrails for direct command execution from pointer/UI boundaries.
- [ ] Add guardrails for public non-ECS command helper methods that mutate ECS state.
- [ ] Add guardrails for new `Service`, `Query`, `Rule`, `Cell`, `Resolver`, `Adapter`, `Composer`, or `Context` gameplay naming escapes.
- [ ] Remove obsolete direct-call context types after their owning commands are converted.
- [ ] Update this roadmap with final converted/folded/remaining counts.
- [ ] Run full focused architecture validation.
- [ ] Run focused EditMode tests for selection, command feedback, movement, attack, scan, board, production, placement, and road build.

Acceptance checks:

- Remaining plain `*System` runtime gameplay count is `0`, or every exception is documented as passive/editor/tooling debt with a removal owner.
- UI command paths use ECS requests/results.
- Gameplay mutation is owned by ECS systems.
- All validation commands pass.

## First Implementation Order

Recommended start order once implementation is explicitly approved:

1. Phase 0 inventory and guardrail classification.
2. Phase 1 command request/result foundation.
3. Phase 2 transport boarding conversion, because it is the agreed example.
4. Phase 2 move/attack/scan conversions.
5. Phase 3 pointer-target boundary split.
6. Phase 4 and Phase 5 selection/focused command split.
7. Phase 6 order primitive conversion.
8. Phase 7 building/road/production conversion.
9. Phase 8 presentation cleanup.
10. Phase 9 helper folding.
11. Phase 10 and Phase 11 lifecycle/visual/environment cleanup.
12. Phase 12 final guardrails.

This order keeps behavior stable by converting command execution before removing the managed bridge code that currently calls it.

## Initial Candidate Lists

### Straightforward First-Wave Candidates

- `SelectionMoveCommandRequestSystem`
- `SelectedMoveOrderCommandSystem`
- `SelectionAttackCommandRequestSystem`
- `AttackOrderCommandSystem`
- `SelectionScanCommandRequestSystem`
- `ScanIntelCommandSystem`
- `BuildingTargetMoveOrderSystem`
- `CitizenMovementCommandSystem`

### Split-With-Recommended-Default Candidates

- `RtsSelectionPointerTargetCommandSystem`: managed target resolution boundary plus ECS command request creation.
- `RtsSelectionFocusCommandSystem`: ECS selection/command-mode request processors plus managed UI/pointer boundary.
- `FocusedUnitCommandSystem`: simple focused commands to ECS; missile/radar automatic targeting to a dedicated ECS targeting system.
- `UnitMoveOrderSystem`: ECS order-apply systems plus folded movement helper math.
- `UnitTargetOrderSystem`: ECS target-order systems plus folded target validation/scoring helpers.
- `BuildingUiCommandSystem`: passive UI request creation plus ECS building command processors.
- `BuildingProductionRequestBoundary`: ECS production command processor plus managed prefab/config boundary.
- `BuildingPlacementCommandSystem`: ECS placement command state plus managed placement visual/session boundary.
- `RoadBuildCommandSystem`: ECS road-build command state plus managed road visual/session boundary.
- `BuildingRuntimeSpawnCommandSystem`: ECS spawn request data plus managed prefab spawn boundary.
- `RtsSelectionCommandResultFlushSystem`: ECS result drain plus managed HUD/marker presentation boundary.
- `SelectionHudFeedbackSystem`: presentation boundary only.
- `SelectionOrderMarkerSystem`: marker presentation boundary fed by ECS marker data.

### Fold Or Re-own Candidates

- Transport boarding helper systems such as boarding rule/query/approach helper types.
- Map-surface command target helper types.
- Road footprint/grid helper types.
- Initial spawn cell/helper types.
- Small pure value/math systems with no update lifetime.

### Managed/Passive Boundary Candidates

- UI screen command/visual/feedback helpers.
- Camera request/application helpers.
- Runtime visual/prefab projection helpers.
- Runtime city/environment prefab spawn boundaries.
- Bootstrap/composition systems after gameplay policy has been extracted.
- Editor-only validation/build tooling.

## Validation Plan

Run validation after each implementation slice:

- `git diff --check`
- focused Unity compile/EditMode validation for the touched domain
- architecture guardrail tests for system naming, UI boundary, and ECS ownership
- command-specific tests for request/result behavior

Preferred focused test groups by phase:

- Selection command tests for Phases 1-5.
- Movement/path/order tests for Phase 6.
- Building production/placement/road tests for Phase 7.
- HUD feedback and marker tests for Phase 8.
- Architecture guardrail tests for Phases 0, 9, 10, 11, and 12.

## Open Risks

- Some current systems combine same-frame UI responsiveness with direct gameplay execution. Conversion must preserve responsiveness by showing "requested" feedback immediately and accepted/rejected feedback from ECS results.
- Some command flows currently rely on same-frame processing. Where that matters, use an explicit same-frame ECS drain boundary before removing direct calls.
- Some helpers may be used by tests directly. Folding should add tests around behavior first, then update tests to validate through the owning ECS system.
- Building and runtime-city code has prefab/GameObject dependencies. Those must be split, not forced into unmanaged `ISystem` code.
- Renaming or moving Unity scripts must preserve `.meta` files and serialized references.
