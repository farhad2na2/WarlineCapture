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

- Checklist progress: `39 / 113 complete (34.5%)`.
- In progress: `0`.
- Remaining open: `74`.
- Phase progress: `4 / 13 phases complete; 1 in progress; 8 not started`.
- Authoritative non-ECS runtime `*System` inventory: `390` after excluding `93` Unity ECS systems, `1` MonoBehaviour system, and `8` editor-only systems.
- Generated inventory artifact: `Design/Architecture/non_ecs_to_ecs_system_inventory.md`.
- Proposed inventory dispositions: `6` ConvertToISystem, `289` ConvertToSystemBase, `73` FoldIntoOwner, `22` PassiveBoundary, and `0` ReviewRequired.
- Converted to `ISystem`: `6`.
- Converted to `SystemBase`: `0`.
- Folded into ECS owners/jobs: `5`.
- Kept as passive view/config/authoring/editor boundary: `0`.
- Remaining plain runtime gameplay `*System` classes: `390`.
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.
- Last implementation update: 2026-06-14 converted Select All, Select All Soldiers, and Select All Vehicles into explicit ECS command request processing while keeping camera selection projection in the managed boundary.

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
- 2026-06-14: Fixed `SelectedMoveOrderCommandSystem.ProcessCommandIntentRequests` command-buffer lifetime after structural move-order changes and added a focused regression test with two move requests in the same buffer.
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

Status: [~]

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
- [ ] Convert Deselect All into an ECS request processor.
- [ ] Convert Enter Selection Mode into an ECS request processor.
- [ ] Convert Exit Selection Mode into an ECS request processor.
- [ ] Convert Enter Move Target Mode into an ECS request processor.
- [ ] Convert Enter Attack Target Mode into an ECS request processor.
- [ ] Convert Enter Scan Target Mode into an ECS request processor.
- [ ] Convert Enter Board Target Mode into an ECS request processor.
- [ ] Convert Cancel Active Command Mode into an ECS request processor.
- [ ] Keep HUD tab state as presentation of ECS command-mode state.
- [ ] Add tests for selection command requests and command-mode transitions.

Acceptance checks:

- UI buttons only enqueue command intent.
- ECS owns command-mode state.
- HUD displays command-mode state but does not own gameplay state.

Progress notes:

- 2026-06-14: Added `RtsSelectionSelectAllCommandSystem` as an explicit `ISystem` request processor for `SelectAll`, `SelectAllSoldiers`, and `SelectAllVehicles` command-intent requests carrying a screen rectangle. `SelectionUiCommandSystem` now queues screen-rect command data for all three select-all commands instead of bare intents. The ECS processor consumes the requests, clears active command-mode state, maps each variant to the correct selection filter, and writes `SelectionRectCommitted` pointer requests; `SelectionGameplayStartupSystem` then performs managed HUD cleanup and drains the existing camera-aware rectangle selection boundary. Focused Unity validation passed with `[RtsSelectionInputSystemValidation] result=Passed tests=15`.

## Phase 5: Focused Unit Commands

Status: [ ]

Purpose:
Convert focused-unit commands into explicit ECS command request processors.

Recommended disposition:

- Split simple command actions into ECS request processors.
- Separate automatic missile/radar targeting from focused UI command handling.

Implementation steps:

- [ ] Convert Hold Position into an ECS request processor.
- [ ] Convert Stop into an ECS request processor.
- [ ] Convert Return To Base into an ECS request processor.
- [ ] Convert Destroy Focused Unit into an ECS request processor.
- [ ] Convert Toggle Attack Target Mode into ECS command-mode state mutation.
- [ ] Convert Cancel Attack Target Mode into ECS command-mode state mutation.
- [ ] Move missile/radar automatic target acquisition into a dedicated ECS automatic targeting system.
- [ ] Add result codes and feedback mapping for each command.
- [ ] Add tests for each focused command result.

Acceptance checks:

- Focused unit UI commands do not call public methods that mutate ECS directly.
- Missile/radar attack policy is not mixed with UI command-mode handling.
- Feedback reports requested, accepted, or failure reasons.

## Phase 6: Movement And Target Order Primitives

Status: [ ]

Purpose:
Remove shared direct-call mutation helpers as public non-ECS systems.

Recommended disposition:

- Convert command execution into ECS systems.
- Fold reusable math into private/static helpers or Burst jobs.
- Replace direct method calls with request components/buffers.

Implementation steps:

- [ ] Inventory all callers of `UnitMoveOrderSystem`.
- [ ] Replace direct move order calls with `UnitMoveOrderRequest` data.
- [ ] Convert order clearing into ECS requests or command-buffered ECS helpers.
- [ ] Replace direct path-request mutation with ECS order application.
- [ ] Inventory all callers of `UnitTargetOrderSystem`.
- [ ] Replace direct attack target calls with `UnitAttackOrderRequest` data.
- [ ] Replace radar target direct issuing with ECS automatic targeting requests.
- [ ] Batch structural changes through ECBs.
- [ ] Add tests for move order, attack order, clear order, and path request output.

Acceptance checks:

- Command systems do not call public `UnitMoveOrderSystem` or `UnitTargetOrderSystem` methods for mutation.
- Structural changes are ECB-backed unless same-frame behavior is documented.
- Existing Move, Attack, Board, Return, Hold, Stop, and debug-fire behavior still works.

## Phase 7: Building, Production, And Road Commands

Status: [ ]

Purpose:
Split command-shaped building and road code that currently mixes UI, GameObjects, placement sessions, resources, and camera focus.

Recommended disposition:

- ECS owns gameplay state, resource validation, production requests, placement validation, and command results.
- Managed boundaries own prefab/GameObject/session visuals and camera focus.

Implementation steps:

- [ ] Split `BuildingUiCommandSystem` into passive UI request creation plus ECS command processors.
- [ ] Split `BuildingProductionRequestSystem` into ECS production validation/request processing and managed prefab/config boundary.
- [ ] Split `BuildingPlacementCommandSystem` into ECS placement command state plus managed placement visual/session boundary.
- [ ] Split `RoadBuildCommandSystem` into ECS road-build command state plus managed road-build visual/session boundary.
- [ ] Split `BuildingRuntimeSpawnCommandSystem` into ECS spawn request data and managed prefab spawn boundary.
- [ ] Add stable result codes for not enough money, missing producer, invalid placement, blocked placement, unavailable prefab, and queue full.
- [ ] Add tests for production request results.
- [ ] Add tests for placement confirm/cancel/rotate results.
- [ ] Add tests for road build enter/confirm/cancel/exit results.

Acceptance checks:

- Build drawer buttons enqueue ECS requests.
- Production/placement failures are represented as ECS results.
- Camera focus and prefab instantiation remain managed boundaries.
- No building command code owns direct UI text or button state.

## Phase 8: HUD Feedback, Markers, And Presentation Boundaries

Status: [ ]

Purpose:
Make feedback and marker systems consume ECS read models/results instead of owning gameplay decisions.

Recommended disposition:

- Presentation systems that touch UI views or UnityEngine objects become `SystemBase` or remain passive view helpers.
- Gameplay state and command result logic remains ECS data.

Implementation steps:

- [ ] Convert command feedback queue consumption into a managed ECS presentation boundary.
- [ ] Ensure `BattleHudRuntimeFeedbackSystem` only maps/display results and lifetimes.
- [ ] Ensure `SelectionHudFeedbackSystem` does not execute gameplay commands.
- [ ] Ensure `SelectionOrderMarkerSystem` consumes marker result data instead of being called from command execution.
- [ ] Convert marker requests/results to ECS data where practical.
- [ ] Add tests for persistent command prompts and transient result feedback.
- [ ] Add tests for move/attack/scan/board marker output.

Acceptance checks:

- Feedback panel is driven by ECS command-mode/result data.
- Marker visuals are presentation of marker request data.
- UI does not need command-specific gameplay logic.

## Phase 9: Query, Rule, Cell, And Helper Cleanup

Status: [ ]

Purpose:
Remove misleading standalone non-ECS helper `*System` types.

Recommended disposition:

- Fold pure helpers into owning ECS systems/jobs.
- Keep only helpers that are clearly private/internal implementation details and do not own runtime state.
- Do not rename gameplay helpers to `Query`, `Rule`, `Cell`, `Resolver`, `Adapter`, `Composer`, or `Context`.

Implementation steps:

- [ ] Inventory pure helper `*System` types such as transport boarding rule/query/approach helpers.
- [ ] Fold transport boarding helper logic into the transport command ECS owner or private helper structs inside that owner.
- [ ] Fold map-surface command target logic into the pointer target boundary or map-surface ECS owner.
- [ ] Fold road footprint/grid helper logic into road/building ECS owners.
- [ ] Fold small initial spawn cell/helper systems into initial spawn ECS owners.
- [ ] Remove or rename files only when Unity `.meta` preservation is planned.
- [ ] Add tests around helper behavior before folding risky algorithms.

Acceptance checks:

- No public gameplay helper remains as a plain non-ECS `*System` without an explicit owner.
- Folded helpers do not create broad owners.
- Existing pathing, boarding, placement, and selection behavior is preserved.

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
- `BuildingProductionRequestSystem`: ECS production command processor plus managed prefab/config boundary.
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
