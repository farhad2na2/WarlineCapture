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

- Checklist progress: `0 / 77 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `77`.
- Current target: `C0 - wait for Agent A inventory assignment`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `not started`.

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

- [ ] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [ ] Filter rows assigned to `AgentC`.
- [ ] Copy row ids, type names, paths, dispositions, blockers, and validation gates into this tracker or an Agent C intake report.
- [ ] For each target, run `rg "<TypeName>" Assets/Game/Scripts Assets/Tests`.
- [ ] For each public method/property, record all callers and decide whether the API becomes ECS data, a command request, a result snapshot, or a managed exception.
- [ ] Identify every singleton, buffer, and component that represents selected entities, focus state, command intent, or command outcome.
- [ ] Identify update group and ordering dependencies around simulation, command validation, building/road execution, transport, and visuals.
- [ ] Identify any managed blockers: camera, UI, `GameObject`, `Transform`, renderer, material, input-action asset, list of GameObjects, or Unity events.
- [ ] Identify rows that need Agent A reclassification before changes.
- [ ] Mark current target and checklist denominator in the progress snapshot.

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

- [ ] Start with the lowest-risk pure ECS selection row.
- [ ] Inspect lifecycle, query shape, and ordering.
- [ ] Convert managed cached query/lookups to unmanaged `ISystem` fields only when refreshed safely.
- [ ] Replace `Entities.ForEach` with `SystemAPI.Query`, explicit query iteration, or `IJobEntity`.
- [ ] Preserve selection clear, add, remove, replace, and multi-select semantics.
- [ ] Preserve faction/team filters and dead/destroyed-unit cleanup.
- [ ] Preserve selected-unit order when the UI or command system depends on stable order.
- [ ] Keep UI selection screens out of scope.
- [ ] Add tests when stable ordering or cleanup behavior lacks coverage.
- [ ] Run focused selection validation before moving to the next row.

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

## C7 - Focused Validation Matrix

Goal:
Prove player intent still behaves correctly.

- [ ] Always run `git diff --check -- <changed files>`.
- [ ] Run selection architecture tests after each conversion.
- [ ] Run focused selection state tests for select, deselect, multi-select, dead cleanup, and faction filtering.
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
