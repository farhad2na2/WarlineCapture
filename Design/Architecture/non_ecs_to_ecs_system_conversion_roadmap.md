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

- Checklist progress: `0 / TBD complete (0.0%)`.
- In progress: `0`.
- Remaining open: `TBD`.
- Phase progress: `0 / 12 phases complete; 0 in progress; 12 not started`.
- Authoritative non-ECS runtime `*System` inventory: `TBD`.
- Converted to `ISystem`: `0`.
- Converted to `SystemBase`: `0`.
- Folded into ECS owners/jobs: `0`.
- Kept as passive view/config/authoring/editor boundary: `0`.
- Remaining plain runtime gameplay `*System` classes: `TBD`.
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.

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

Status: [ ]

Purpose:
Create the exact list of non-ECS runtime `*System` types and classify each one before conversion starts.

Implementation steps:

- [ ] Build a script or focused architecture test that enumerates all runtime `*System` type declarations.
- [ ] Exclude actual Unity ECS systems: `ISystem` and `SystemBase`.
- [ ] Exclude `MonoBehaviour` views and shell components from the gameplay conversion denominator.
- [ ] Exclude editor-only systems from runtime gameplay conversion, but list them separately.
- [ ] Classify every remaining type into Disposition A, B, C, or D.
- [ ] Produce a generated inventory table with file path, type name, current base type, disposition, owner phase, and reason.
- [ ] Add a guardrail test that fails if a new plain runtime gameplay `*System` is added without classification.
- [ ] Update the progress snapshot with the authoritative denominator.

Acceptance checks:

- Every non-ECS runtime `*System` has one owner phase.
- No system is counted in two dispositions.
- The inventory distinguishes "convert to ECS" from "fold helper into ECS owner".

## Phase 1: Command Request/Result Foundation

Status: [ ]

Purpose:
Standardize command request and result flow before converting individual command systems.

Implementation steps:

- [ ] Review `RtsSelectionCommandIntentRequestElement` and `RtsSelectionCommandResultElement`.
- [ ] Decide whether current shared buffers are enough or whether specific request/result components are needed for Move, Attack, Scan, and Board.
- [ ] Add stable reason-code enums where current results rely on loose text.
- [ ] Ensure command requests can carry pre-resolved target data: entity, cell, world position, screen position, and target kind.
- [ ] Ensure command results can carry accepted/rejected state, reason code, command mode, target entity/cell/world position, marker intent, and feedback lifetime.
- [ ] Ensure command requests are consumed exactly once.
- [ ] Ensure command results are drained exactly once by presentation boundaries.
- [ ] Add EditMode tests for request consumption and result emission.

Acceptance checks:

- UI can enqueue a command without knowing whether it will succeed.
- Result feedback is the only source of accepted/rejected command feedback.
- No command processor needs direct UI references.

## Phase 2: Straightforward Selection Command Processors

Status: [ ]

Purpose:
Convert the command processors that already follow the request/result shape.

Implementation steps:

- [ ] Convert `SelectionMoveCommandRequestSystem` into an ECS system or fold it into the new move command ECS owner.
- [ ] Convert `SelectedMoveOrderCommandSystem` into the move command ECS owner.
- [ ] Convert `SelectionAttackCommandRequestSystem` into an ECS system or fold it into the new attack command ECS owner.
- [ ] Convert `AttackOrderCommandSystem` into the attack command ECS owner.
- [ ] Convert `SelectionScanCommandRequestSystem` into an ECS system or fold it into the new scan command ECS owner.
- [ ] Convert `ScanIntelCommandSystem` into the scan command ECS owner.
- [ ] Convert `SelectionTransportCommandRequestSystem` into an ECS system or fold it into the new transport command ECS owner.
- [ ] Convert `TransportBoardingCommandSystem` into the transport boarding command ECS owner.
- [ ] Fold `UnitTransportRopeDisembarkCommandSystem` into the transport command ECS owner or make it a narrow disembark request processor.
- [ ] Convert `BuildingTargetMoveOrderSystem` into a request/result ECS command processor.
- [ ] Convert `CitizenMovementCommandSystem` into a request/result ECS command processor.

Acceptance checks:

- Move command requests produce accepted/rejected move results.
- Attack command requests produce accepted/rejected attack results.
- Scan command requests produce accepted/rejected scan results.
- Board/disembark command requests produce accepted/rejected transport results.
- Current user-visible behavior is preserved.
- Command processors do not call UI, camera, or GameObject APIs.

## Phase 3: Pointer Target Boundary Split

Status: [ ]

Purpose:
Split `RtsSelectionPointerTargetCommandSystem` so it no longer executes gameplay commands.

Recommended disposition:

- Convert the pointer/camera part to a managed ECS boundary, likely `SystemBase`, or fold it into an existing managed input boundary if that is cleaner.
- It may read camera/pointer data and write ECS request data.
- It must not directly issue Move, Attack, Scan, Board, or BuildingTargetMove orders.

Implementation steps:

- [ ] Identify every direct command execution call currently routed through `RtsSelectionPointerTargetCommandSystem`.
- [ ] Move camera/screen-to-world resolution into a boundary-only target resolution pass.
- [ ] Write resolved Move target requests instead of calling move command execution.
- [ ] Write resolved Attack target requests instead of calling attack command execution.
- [ ] Write resolved Scan target requests instead of calling scan command execution.
- [ ] Write resolved Board target requests instead of calling transport command execution.
- [ ] Preserve command-mode UX: active command taps do not select hostile units/buildings.
- [ ] Preserve mobile pan behavior during long-range Attack and transport-first Board mode.
- [ ] Add tests for command-mode click priority over selection.

Acceptance checks:

- Pointer boundary has no gameplay order mutation.
- Gameplay command execution happens only in ECS command systems.
- Failed target taps stay in the intended command mode unless the mode is invalid.

## Phase 4: Focus Command Split

Status: [ ]

Purpose:
Split `RtsSelectionFocusCommandSystem` into ECS command-mode and selection request processors.

Recommended disposition:

- Selection/focus state mutation becomes ECS.
- UI/HUD command-mode display stays presentation.
- Camera/pointer focus lookup stays managed boundary where required.

Implementation steps:

- [ ] Convert Select All into an ECS request processor.
- [ ] Convert Select All Soldiers into an ECS request processor.
- [ ] Convert Select All Vehicles into an ECS request processor.
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
- `SelectionTransportCommandRequestSystem`
- `TransportBoardingCommandSystem`
- `UnitTransportRopeDisembarkCommandSystem`
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
