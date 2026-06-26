# Phase 7 Agent C Tracker - Selection, Commands, Focus, And Player Intent

Purpose:
Convert non-UI selection, focus, command-intent, and command-result `SystemBase` systems to narrow ECS request/result `ISystem` processors. Agent C owns player-intent gameplay data, not UI Toolkit views, camera ownership, visual markers, or building/road domain execution.

Branch:
`codex/phase7-agent-c-selection-commands`

Execution order:

1. Wait for Agent A to publish the authoritative inventory and assign `AgentC` rows.
2. Read every assigned row and caller before implementation.
3. Split mixed systems into data processors plus explicit managed presentation/config/camera exceptions when needed.
4. Coordinate with Agent D for building commands, Agent E for road/city/citizen commands, and Agent F for selection/focus visuals.
5. Write handoffs under `Design/AgentReports/`; Agent A owns the main tracker and integration.

Progress snapshot:

- Checklist progress: `29 / 77 complete (37.7%)`.
- In progress: `0`.
- Remaining open: `48`.
- Current target: `Agent C naming-only Batch 210 MatchHudSquadTraySelectionUiSystemHelper complete; open SystemBase rows remain complete`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `18 retired/folded helpers`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `All open Agent C SystemBase rows have been folded out of ECS into plain helpers/facades. Naming-only Batch 222 renamed P7-0046 to SelectionGameplayStartupSystemHelper and regenerated the non-ECS helper inventory to denominator 7; focused selection/input validation and architecture validation passed in /private/tmp/warline-non-ecs-helper-naming-batch222-rts-selection-input.log and /private/tmp/warline-non-ecs-helper-naming-batch222-architecture.log. Naming-only Batch 221 renamed P7-0045 to SelectionBuildingInteractionCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 8; focused selection/input validation and architecture validation passed in /private/tmp/warline-non-ecs-helper-naming-batch221-rts-selection-input.log and /private/tmp/warline-non-ecs-helper-naming-batch221-architecture.log. Naming-only Batch 210 renamed the direct squad-tray HUD selection bridge to MatchHudSquadTraySelectionUiSystemHelper and regenerated the non-ECS helper inventory to denominator 19; focused squad-tray validation and architecture validation passed in /private/tmp/warline-non-ecs-helper-naming-batch210-match-hud-squad-tray.log and /private/tmp/warline-non-ecs-helper-naming-batch210-architecture.log. Naming-only Batch 205 renamed P7-0026 to FocusedUnitUiReadModelUiSystemHelper and regenerated the non-ECS helper inventory to denominator 24; unit transport validation and architecture validation passed in /private/tmp/warline-non-ecs-helper-naming-batch205-unit-transport.log and /private/tmp/warline-non-ecs-helper-naming-batch205-architecture.log. Naming-only Batch 204 renamed P7-0025 to FocusedUnitLifecycleCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 25; selection-state focused validation and architecture validation passed in /private/tmp/warline-non-ecs-helper-naming-batch204-selection-state.log and /private/tmp/warline-non-ecs-helper-naming-batch204-architecture.log. Naming-only Batch 203 renamed P7-0023 to FocusableUnitLookupCameraSystemHelper and regenerated the non-ECS helper inventory to denominator 26; focused focusable-unit lookup and architecture validations passed in /private/tmp/warline-non-ecs-helper-naming-batch203-focusable-unit-lookup.log and /private/tmp/warline-non-ecs-helper-naming-batch203-architecture.log. Naming-only Batch 183 renamed P7-0040 to RtsSelectionRuntimeInputCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 46; focused selection input validation and architecture passed in /private/tmp/warline-non-ecs-helper-naming-batch183-rts-selection-input.log and /private/tmp/warline-non-ecs-helper-naming-batch183-architecture.log. Naming-only Batch 182 renamed P7-0039 to RtsSelectionPointerTargetCommandCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 47; focused selection input validation and architecture passed in /private/tmp/warline-non-ecs-helper-naming-batch182-rts-selection-input.log and /private/tmp/warline-non-ecs-helper-naming-batch182-architecture.log. Naming-only Batch 181 renamed P7-0035 to RtsSelectionInputCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 48; focused selection input validation and architecture passed in /private/tmp/warline-non-ecs-helper-naming-batch181-rts-selection-input.log and /private/tmp/warline-non-ecs-helper-naming-batch181-architecture.log. Naming-only Batch 180 renamed P7-0034 to RtsSelectionInputStateCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 49; focused selection input validation and architecture passed in /private/tmp/warline-non-ecs-helper-naming-batch180-rts-selection-input.log and /private/tmp/warline-non-ecs-helper-naming-batch180-architecture.log. Naming-only Batch 179 renamed P7-0032 to RtsSelectionFocusCommandCompositionSystemHelper and regenerated the non-ECS helper inventory to denominator 50; focused selection input validation and architecture passed. Batch 178 renamed P7-0030 to RtsSelectionCommandResultFlushCompositionSystemHelper and passed selection input plus architecture, while command request/result validation reached the documented unrelated UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea blocker. Latest completed rows: P7-0045 SelectionBuildingInteractionCompositionSystemHelper and P7-0046 SelectionGameplayStartupSystemHelper. dotnet build passed; inventory regenerated; latest focused logs are /private/tmp/warline-phase7-agent-c-selection-startup-rts-selection-input.log, /private/tmp/warline-phase7-agent-c-selection-startup-request-result.log, /private/tmp/warline-phase7-agent-c-selection-startup-squad-tray.log, and /private/tmp/warline-phase7-agent-a-architecture.log; prior Agent C validation paths remain recorded in completed slices and handoff reports`.

Owned files:

- `Design/Architecture/phase7_agent_c_selection_commands_tracker.md`
- Agent C implementation rows assigned in `Design/Architecture/systembase_to_isystem_inventory.md`
- Focused tests for selection state, command requests, command result snapshots, and player intent
- Agent C handoff reports under `Design/AgentReports/`

Do not touch:

- UI Toolkit/Canvas views, UXML, USS, screen presenters, or menu code.
- Camera movement/zoom/pan ownership except to define a request/result boundary that an assigned managed camera system consumes.
- Building placement or production execution internals owned by Agent D.
- Road/city/citizen execution internals owned by Agent E.
- Rendering, marker meshes, line renderers, particle systems, or visual presenters owned by Agent F.
- Shared trackers except this file.

Shared rules:

- Do not introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers.
- MonoBehaviours are view/reference holders only. They may raise one-shot UI/input events into ECS requests but must not poll or orchestrate gameplay each frame.
- Managed input/camera/presentation ticking must remain an explicit counted managed `SystemBase` exception when it cannot be represented as pure ECS.
- Do not use `Object.Find*`, `GameObject.Find`, `Camera.main`, runtime hierarchy paths, service locators, mutable static gameplay registries, or broad controller/facade shells.
- Do not convert a system if its public API is still the contract used by UI/composition; first replace the API with ECS singleton/request/result data or keep a managed exception.
- Preserve Unity `.meta` files.

Reference documents:

- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`

Likely Agent C target families after Agent A review:

- RTS selection state and selected-unit query/update systems.
- Focused-unit and focusable-unit lookup systems.
- Player command request processors for move, stop, hold, scan, board, attack, and ability intent.
- Command result/snapshot systems that publish ECS-readable outcomes.
- Disabled or wrapper selection helpers that can be retired or folded.

Likely not Agent C:

- UI build menu, armory screen, HUD, or screen-binding systems.
- Camera transform systems that directly read or write `Camera`, `Transform`, or Cinemachine components.
- Selection marker rendering and visual indicator systems.
- Domain command execution for buildings, roads, citizens, or transport internals unless Agent A explicitly assigns a row.

## C0 - Intake And Call Graph

Goal:
Build a precise ownership map before touching selection or command behavior.

- [x] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [x] Filter rows assigned to `AgentC`.
- [x] Copy row ids, type names, paths, dispositions, blockers, and validation gates into this tracker or an Agent C intake report.
- [~] For each target, run `rg "<TypeName>" Assets/Game/Scripts Assets/Tests` (complete for `P7-0034`; remaining rows pending per-slice).
- [ ] For each public method/property, record all callers and decide whether the API becomes ECS data, a command request, a result snapshot, or a managed exception.
- [ ] Identify every singleton, buffer, and component that represents selected entities, focus state, command intent, or command outcome.
- [ ] Identify update group and ordering dependencies around simulation, command validation, building/road execution, transport, and visuals.
- [x] Identify any managed blockers: camera, UI, `GameObject`, `Transform`, renderer, material, input-action asset, list of GameObjects, or Unity events.
- [x] Identify rows that need Agent A reclassification before changes.
- [x] Mark current target and checklist denominator in the progress snapshot.

Agent C intake rows after the `2026-06-21T10:09:15Z` inventory regeneration:

| Id | Type | Base | Disposition | Status | Blockers | Slice note |
| --- | --- | --- | --- | --- | --- | --- |
| `P7-0023` | `FocusableUnitLookupCameraSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | Camera argument | Folded out of `SystemBase`; it is a manually constructed focusable-unit query/camera hit helper, not scheduled ECS. |
| `P7-0024` | `FocusedUnitCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only unless validation regresses. |
| `P7-0025` | `FocusedUnitLifecycleCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | None | Folded out of `SystemBase`; it is a manually constructed selected/focused lifecycle helper, not scheduled ECS. |
| `P7-0026` | `FocusedUnitUiReadModelUiSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | Managed scratch list | Folded out of `SystemBase`; it is a manually constructed focused-unit UI read-model helper, not scheduled ECS. |
| `P7-0027` | `RtsSelectionAttackTargetModeCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0028` | `RtsSelectionBoardTargetModeCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0029` | `RtsSelectionCancelActiveCommandModeSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0030` | `RtsSelectionCommandResultFlushCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | None | Folded out of `SystemBase`; it is a manually constructed command-result flush helper, not scheduled ECS. |
| `P7-0031` | `RtsSelectionDeselectAllCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0032` | `RtsSelectionFocusCommandCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | Camera/context argument | Folded out of `SystemBase`; it is a manually constructed focus command helper, not scheduled ECS. |
| `P7-0033` | `RtsSelectionImmediateSelectedUnitCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0034` | `RtsSelectionInputStateCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | None | Folded out of `SystemBase`; it is manually constructed as a request-buffer helper, not scheduled ECS. |
| `P7-0035` | `RtsSelectionInputCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | None | Folded out of `SystemBase`; it is a manually constructed input state and command-intent enqueue helper, not scheduled ECS. |
| `P7-0036` | `RtsSelectionMissileLauncherRadarAttackCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0037` | `RtsSelectionModeCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0038` | `RtsSelectionMoveTargetModeCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0039` | `RtsSelectionPointerTargetCommandCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | Camera/context argument | Folded out of `SystemBase`; it is a manually constructed pointer target helper, not scheduled ECS. |
| `P7-0040` | `RtsSelectionRuntimeInputCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | UnityEngine input/time | Folded out of `SystemBase`; it is a manually constructed pointer/input helper, not scheduled ECS. |
| `P7-0041` | `RtsSelectionScanTargetModeCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0042` | `RtsSelectionSelectAllCommandSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0043` | `SelectedUnitDebugFireSystem` | `ISystem` | `Converted` | Converted | None | Monitor only. |
| `P7-0044` | `SelectedUnitOrderSnapshotCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | None | Folded out of `SystemBase`; it is manually constructed as a selected-order snapshot helper, not scheduled ECS. |
| `P7-0045` | `SelectionBuildingInteractionCompositionSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | Camera argument | Folded out of `SystemBase`; it is a manually constructed building selection/move helper, not scheduled ECS. |
| `P7-0046` | `SelectionGameplayStartupSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | Transform, Camera arguments | Folded out of `SystemBase`; it is a manually constructed startup composition boundary, not scheduled ECS. |
| `P7-0047` | `SelectionRectangleRequestSystem` | `PlainClass` | `RetiredFolded` | Folded | Camera argument | Folded out of `SystemBase`; it is a manually constructed selection-rectangle request helper, not scheduled ECS. |
| `P7-0048` | `SelectionRuntimeConfigSystem` | `PlainClass` | `RetiredFolded` | Folded | GameObject, Camera state | Folded out of `SystemBase`; it is a startup config-state factory, not scheduled ECS. |
| `P7-0049` | `SelectionRuntimeDiagnosticsSystemHelper` | `PlainClass` | `RetiredFolded` | Folded | UnityEngine Debug/Application | Folded out of `SystemBase`; it is a manually constructed/static diagnostics helper, not scheduled ECS. |
| `P7-0050` | `SelectionStateSystem` | `PlainClass` | `RetiredFolded` | Folded | None | Folded out of `SystemBase`; it is a manually constructed selected/focused state helper, not scheduled ECS. |
| `P7-0051` | `SelectionUiCommandSystem` | `PlainClass` | `RetiredFolded` | Folded | UnityEngine Time/Screen | Folded out of `SystemBase`; it is a manually constructed UI command facade implementing `ISelectionUiCommand`, not scheduled ECS. |
| `P7-0052` | `SelectionUiReadModelSystem` | `PlainClass` | `RetiredFolded` | Folded | Camera argument, UI interface | Folded out of `SystemBase`; it is a manually constructed `ISelectionUiReadModel` adapter, not scheduled ECS. |

Acceptance:

- Agent C has a concrete row list and no ownership overlap with Agents D-F.
- Public API replacement plan exists before conversion starts.
- Managed camera/UI/visual blockers are explicitly split or returned.

## C1 - Request/Result Boundary Design

Goal:
Make player intent a clear ECS contract instead of a broad managed system surface.

- [ ] Inventory existing selection command request components and buffers.
- [ ] Inventory existing command result, error, feedback, and selected-order snapshot components.
- [ ] Define or reuse request entities for one-frame player intent.
- [ ] Define or reuse singleton state for persistent selection/focus state.
- [ ] Define or reuse result buffers for UI/visual systems to observe without calling gameplay systems directly.
- [ ] Ensure requests include stable source keys or entity references, not GameObject references.
- [ ] Ensure result data is blittable or contained in explicit managed presentation exceptions when strings/Unity objects are required.
- [ ] Keep validation policy separate from execution policy when the current system mixes both.
- [ ] Keep presentation feedback separate from gameplay acceptance/rejection.
- [ ] Coordinate result schema with Agent F when visuals need selection/command feedback.

Acceptance:

- The target system can be converted without public managed helper calls.
- UI and visuals can observe ECS result data without owning command execution.
- Command requests are one-shot and do not repeat unintentionally.

## C2 - Selection State Conversions

Goal:
Convert selection state processing while preserving player-visible behavior.

- [x] Start with the lowest-risk pure ECS selection row.
- [x] Inspect lifecycle, query shape, and ordering.
- [ ] Convert managed cached query/lookups to unmanaged `ISystem` fields only when refreshed safely.
- [ ] Replace `Entities.ForEach` with `SystemAPI.Query`, explicit query iteration, or `IJobEntity`.
- [ ] Preserve selection clear, add, remove, replace, and multi-select semantics.
- [ ] Preserve faction/team filters and dead/destroyed-unit cleanup.
- [ ] Preserve selected-unit order when the UI or command system depends on stable order.
- [ ] Keep UI selection screens out of scope.
- [ ] Add tests when stable ordering or cleanup behavior lacks coverage.
- [x] Run focused selection validation before moving to the next row.

Acceptance:

- Selection state remains correct after unit death, deselection, scene restart, and faction changes.
- Converted selection systems have no UI/camera/GameObject dependencies.

## C3 - Focus And Target Lookup Conversions

Goal:
Convert focused-unit and target lookup systems without moving camera or visual ownership into ECS.

- [ ] Identify focus source: selected entity, cursor hit request, command target, or UI selection.
- [ ] Identify focus outputs consumed by camera, UI, command validation, or visuals.
- [ ] Convert pure ECS focus resolution to `ISystem`.
- [ ] Keep camera transform movement in an explicit managed camera `SystemBase` exception if needed.
- [ ] Keep focus markers and visual highlights in Agent F owned systems.
- [ ] Preserve validity checks for destroyed entities, hidden entities, enemy/friendly filters, and selectable tags.
- [ ] Avoid direct `Camera`, `Physics.Raycast`, or `Transform` usage in converted systems.
- [ ] Add or update tests for focus cleanup and invalid target handling.

Acceptance:

- Focus state remains readable through ECS.
- Camera and visual consumers no longer need to call selection systems directly.

## C4 - Command Intent Processors

Goal:
Convert command-intent processors into narrow `ISystem` request handlers.

- [ ] Classify each target command as move, attack, hold, stop, scan, board, build, road, or domain-specific.
- [ ] Return building/road/citizen execution systems to Agent D/E when the target crosses domain ownership.
- [ ] Keep Agent C responsible for intent normalization, validation routing, and command-result publication.
- [ ] Preserve command priority and cancellation behavior.
- [ ] Preserve command batching for selected groups.
- [ ] Preserve input-mode and targeting-mode state without tying it to UI controls.
- [ ] Use ECB playback safely for request consumption or result creation.
- [ ] Ensure requests are consumed exactly once.
- [ ] Do not add mutable static caches for selected entities or command targets.
- [ ] Add tests for accepted, rejected, repeated, and stale requests.

Acceptance:

- Commands triggered through existing UI/input paths still produce the same gameplay requests.
- Converted processors are narrow and do not execute unrelated domain logic.

## C5 - Managed Boundary Exceptions

Goal:
Keep unavoidable managed input/camera/presentation code explicit and counted.

- [ ] Identify any target row that directly uses `Camera`, `Transform`, `InputAction`, Unity events, strings, or managed UI.
- [ ] Split pure data processing into `ISystem`.
- [ ] Keep managed polling/presentation in a small `SystemBase` exception only if it genuinely must tick.
- [ ] Rename or document the managed exception as a boundary, not a gameplay policy system.
- [ ] Ensure managed exceptions write requests/results and do not execute domain decisions.
- [ ] Do not introduce MonoBehaviour ticking as an alternative to `SystemBase`.
- [ ] Add the exception to the Agent C handoff and request Agent A to count it.

Acceptance:

- Mixed systems are split, not blindly converted.
- Managed exceptions are small, documented, and visible to the denominator.

## C6 - Cross-Agent Coordination

Goal:
Avoid conflicts with domain and visual lanes.

- [ ] For building commands, agree with Agent D on the request/result component names and ownership.
- [ ] For road/city commands, agree with Agent E on request/result component names and ownership.
- [ ] For selection/focus visuals, agree with Agent F on ECS result data that visuals consume.
- [ ] For shared tests, coordinate one owner for each test file to avoid merge conflicts.
- [ ] Do not edit another agent's implementation file unless Agent A reassigns the row.
- [ ] Record coordination notes in the handoff report.

Acceptance:

- Agent C changes can merge without rewriting Agent D-F work.
- Requests and results have one owner and one consumer contract.

Completed slices:

| Id | Type | Result | Validation |
| --- | --- | --- | --- |
| `P7-0023` | `FocusableUnitLookupCameraSystemHelper` | Retired/folded from `SystemBase` into a plain focusable-unit lookup helper; preserved EntityQuery cache setup, cell coverage lookup, screen-distance lookup, selection-hitbox screen bounds, transit-state filtering, and the Burst chunk collector. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focusable lookup validation passed in `/private/tmp/warline-phase7-agent-c-focusable-unit-lookup.log`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-focus-command-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-focus-command-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`; `git diff --check` passed. |
| `P7-0025` | `FocusedUnitLifecycleCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain selected/focused lifecycle helper; preserved selected-tag clearing, focused entity state mutation through `SelectionStateSystem`, clicked-unit focus, HUD selection callbacks, selected-entity collection, and lifecycle diagnostics. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; selection-state validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-selection-state.log`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-request-result.log`; squad-tray validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-squad-tray.log`; selection summary validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-lifecycle-summary.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0026` | `FocusedUnitUiReadModelUiSystemHelper` | Retired/folded from disabled `SystemBase` into a plain focused-unit UI read-model helper; preserved `Publish` and `TryRead` plus passenger-buffer publication for selection HUD and transport UI. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-ui-readmodel-selection.log`; transport validation passed in `/private/tmp/warline-phase7-agent-c-focused-unit-ui-readmodel-transport.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0030` | `RtsSelectionCommandResultFlushCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain command-result flush helper; preserved command-result flushing, HUD callbacks, command-mode transitions, order-marker updates, selected-building destroy fallback, and command-family request/result processors. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-command-result-flush-request-result.log`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-pointer-target-rts-selection-input.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`; `git diff --check` passed. |
| `P7-0032` | `RtsSelectionFocusCommandCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain focus command helper; preserved external focus/select-all/deselect/selection-mode request consumption, HUD command result/mode callbacks, focus validation, and input guard cleanup. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focusable lookup validation passed in `/private/tmp/warline-phase7-agent-c-focusable-unit-lookup.log`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-focus-command-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-focus-command-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`; `git diff --check` passed. |
| `P7-0034` | `RtsSelectionInputStateCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain request-buffer helper; preserved the public API used by input, UI boundary adapters, command-result flushing, and tests. Also refreshed two stale transport boarding source-contract assertions inside the affected selection input validation suite. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-rts-selection-input.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0035` | `RtsSelectionInputCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain input state and command-intent enqueue helper; preserved drag state, active command mode state, queued move order state, pointer request buffers, command intent request buffers, and transport/scan/selection rectangle enqueue helpers. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-rts-selection-input-fold.log`; HUD command controls validation passed in `/private/tmp/warline-phase7-agent-c-rts-selection-input-hud-controls.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-rts-selection-input-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0039` | `RtsSelectionPointerTargetCommandCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain pointer target helper; preserved pointer-to-unit, pointer-to-cell, move/attack/scan/board target request routing, map-surface target resolution, selected footprint target search, and click diagnostics. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-pointer-target-rts-selection-input.log`; map-surface focused validation passed in `/private/tmp/warline-phase7-agent-c-pointer-target-map-surface.log`; transport validation passed in `/private/tmp/warline-phase7-agent-c-pointer-target-transport.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-command-result-flush-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`; `git diff --check` passed. |
| `P7-0040` | `RtsSelectionRuntimeInputCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain pointer/input helper; preserved the public `Context`, queued move processing, normal pointer input, command-target, scan, board, selection-rect, and camera-pan behaviors. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-rts-selection-runtime-input.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0044` | `SelectedUnitOrderSnapshotCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain selected-order snapshot helper, then renamed to the approved composition-helper suffix; preserved the explicit preserve/restore API used by selection startup and focused tests. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; selected-order snapshot validation passed in `/private/tmp/warline-phase7-agent-c-selected-unit-order-snapshot.log` and `/private/tmp/warline-non-ecs-helper-naming-batch220-selected-order-snapshot.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log` and `/private/tmp/warline-non-ecs-helper-naming-batch220-architecture.log`. |
| `P7-0045` | `SelectionBuildingInteractionCompositionSystemHelper` | Retired/folded from disabled `SystemBase` into a plain building selection/move helper, then renamed to the approved composition-helper suffix; preserved match HUD selection panel binding, building selection HUD feedback, focused-unit clearing, boardable transport click tests, and move-order-to-building routing. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-startup-rts-selection-input.log` and `/private/tmp/warline-non-ecs-helper-naming-batch221-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-startup-request-result.log`; squad-tray validation passed in `/private/tmp/warline-phase7-agent-c-selection-startup-squad-tray.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log` and `/private/tmp/warline-non-ecs-helper-naming-batch221-architecture.log`; `git diff --check` passed. |
| `P7-0046` | `SelectionGameplayStartupSystemHelper` | Retired/folded from disabled `SystemBase` into a plain startup composition boundary, then renamed to the approved startup-helper suffix; preserved selection runtime update orchestration, context construction, UI binding callbacks, command result draining, pointer/input/camera wiring, HUD read-model refresh, and direct construction by `ManagedGameplayStartupSystem`. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-startup-rts-selection-input.log` and `/private/tmp/warline-non-ecs-helper-naming-batch222-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-startup-request-result.log`; squad-tray validation passed in `/private/tmp/warline-phase7-agent-c-selection-startup-squad-tray.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log` and `/private/tmp/warline-non-ecs-helper-naming-batch222-architecture.log`; `git diff --check` passed. |
| `P7-0047` | `SelectionRectangleRequestSystem` | Retired/folded from disabled `SystemBase` into a plain selection-rectangle request helper; preserved pending rectangle request extraction, visible-player selection collection, building fallback selection, selected-tag application, selected move cache updates, HUD callbacks, and focus assignment. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-rectangle-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-rectangle-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0048` | `SelectionRuntimeConfigSystem` | Retired/folded from disabled `SystemBase` into a plain startup config-state factory; removed the `DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged` dependency and preserved config normalization, camera fallback, marker prefab references, selection thresholds, and camera-mode settings. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-runtime-config-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-runtime-config-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0049` | `SelectionRuntimeDiagnosticsSystemHelper` | Retired/folded from disabled `SystemBase` into a plain diagnostics helper; preserved the static/instance diagnostics API used by selection startup, move/scan traces, pathfinding, command flushing, and UI boundary adapters. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-runtime-diagnostics.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0050` | `SelectionStateSystem` | Retired/folded from disabled `SystemBase` into a plain selected/focused state helper; preserved focused entity state, selected move cache helpers, cacheability filtering, and lifecycle debug recording. Also corrected selection-mode entry so `RuntimeGameplayStateComponent.SelectionModeActive` owns selection mode while active command state is cleared, matching the command request/result contract. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; MonoBehaviour loop baseline refreshed to `40` existing loop keys; selection-state validation passed in `/private/tmp/warline-phase7-agent-c-selection-state.log`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-state-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-state-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0051` | `SelectionUiCommandSystem` | Retired/folded from disabled `SystemBase` into a plain UI command facade; preserved `ISelectionUiCommand`, command-intent queuing, select-all screen rect requests, focused transport disembark request helpers, and intro-input lock behavior. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-command-selection.log`; HUD command controls validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-command-hud-controls.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-command-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0052` | `SelectionUiReadModelSystem` | Retired/folded from disabled `SystemBase` into a plain `ISelectionUiReadModel` adapter; preserved focused-unit status/health/capability reads, focused transport passenger reads, selected-unit presence, and visible player unit/soldier/vehicle screen queries. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; UI read-model lookup validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-lookup.log`; squad-tray validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-squad-tray.log`; focused selection/input validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-rts-selection-input.log`; command request/result validation passed in `/private/tmp/warline-phase7-agent-c-selection-ui-readmodel-request-result.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |

## C7 - Focused Validation Matrix

Goal:
Prove player intent still behaves correctly.

- [x] Always run `git diff --check -- <changed files>`.
- [x] Run selection architecture tests after each conversion.
- [x] Run focused selection state tests for select, deselect, multi-select, dead cleanup, and faction filtering.
- [ ] Run command-intent tests for move, stop, hold, scan, board, and attack if changed.
- [ ] Run focus tests for valid target, invalid target, destroyed entity, and no selection.
- [ ] Run a PlayMode smoke test that starts a match and issues at least one unit command when possible.
- [ ] Check Unity logs for hidden runtime errors after the smoke test.
- [ ] If Unity is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation when available.

Suggested commands:

```bash
git diff --check -- <changed files>
```

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -runTests -testPlatform EditMode \
  -logFile /private/tmp/warline-phase7-agent-c-editmode.log
```

Acceptance:

- Focused validation passes or the failure is fixed in the same slice.
- Any unrun validation is recorded with a concrete reason.

## C8 - Handoff To Agent A

Goal:
Make Agent C integration auditable.

- [ ] Create a dated handoff report under `Design/AgentReports/`.
- [ ] Include inventory row ids, systems changed, and final disposition.
- [ ] Include converted-to-`ISystem`, split managed-boundary, retired/folded, and managed-exception counts.
- [ ] Include command/request/result contracts added or changed.
- [ ] Include validation commands, outcomes, and log paths.
- [ ] Include coordination notes for Agents D-F.
- [ ] Include any rows returned to Agent A for reclassification.
- [ ] Confirm this tracker progress snapshot is current.

Handoff template:

```markdown
# Phase 7 Agent C Handoff - YYYY-MM-DD

Branch:
`codex/phase7-agent-c-selection-commands`

Rows completed:
- `P7-####` - `TypeName` - `Converted/Split/Retired`

Contracts changed:
- Request/result component:

Counts:
- Converted to ISystem:
- Split passive/managed boundaries:
- Managed SystemBase exceptions:
- Retired/folded:

Validation:
- `git diff --check`: passed/failed
- Unity validation: passed/failed/not run, log path

Risks:
- ...
```

Completion criteria:

- Every Agent C row has final status.
- No Agent C conversion owns UI, camera, GameObject, or visual rendering state inside `ISystem`.
- No MonoBehaviour ticking introduced.
- Player selection, focus, and command intent continue to work through ECS contracts.
