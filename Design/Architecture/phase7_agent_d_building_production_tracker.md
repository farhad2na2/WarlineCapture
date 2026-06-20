# Phase 7 Agent D Tracker - Building Placement, Spawn, Production, And Base Systems

Purpose:
Convert non-UI building placement, building spawn, building state, and production `SystemBase` systems into small `ISystem` processors with clear responsibilities. Agent D must continue the SOLID direction from the earlier five-SystemBase work: split broad building systems into request validation, data mutation, spawn execution, production progression, and result publication rather than creating a single large `ISystem`.

Branch:
`codex/phase7-agent-d-building-production`

Execution order:

1. Wait for Agent A to publish authoritative inventory rows and guardrails.
2. Cross-check Agent A rows against `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`.
3. Convert only rows assigned to `AgentD`.
4. Split broad systems before converting when a target mixes UI/composition API, prefab/GameObject ownership, placement policy, production policy, visual feedback, and ECS spawning.
5. Write handoffs under `Design/AgentReports/`; Agent A owns shared tracker updates and integration.

Progress snapshot:

- Checklist progress: `0 / 83 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `83`.
- Current target: `D0 - wait for Agent A inventory assignment`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Retired/folded helpers: `0`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `not started`.

Owned files:

- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- Agent D rows assigned by `Design/Architecture/systembase_to_isystem_inventory.md`
- Building placement/production focused tests when needed
- Agent D handoff reports under `Design/AgentReports/`

Do not touch:

- UI Toolkit build menu, HUD, armory, or screen binding code.
- Road/city/citizen systems owned by Agent E.
- Selection command intent owned by Agent C, except for agreed request/result contracts.
- Rendering, building faction visuals, construction previews, marker meshes, particle effects, or material updates owned by Agent F unless Agent A assigns the row.
- Scenes, prefabs, material assets, or ScriptableObject config assets unless a row explicitly requires a passive data-projection boundary.
- Shared trackers except this file.

Shared rules:

- Do not introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers.
- MonoBehaviours are view/reference holders only. They must not own building placement or production loops.
- Managed prefab/config/presentation ticking belongs in a counted managed `SystemBase` exception when unavoidable.
- Converted `ISystem` code must not use `GameObject`, `Transform`, `UnityEngine.Object`, `Object.Instantiate`, `Object.Destroy`, `Camera.main`, runtime hierarchy lookup, service locators, or mutable static gameplay state.
- Do not move prefab GameObject selection into `ISystem`; use baked Entity prefab references and stable source keys.
- Do not replace broad `BuildingSpawnSystem`-style behavior with one broad `ISystem`; split by responsibility.
- Preserve Unity `.meta` files.

Reference documents:

- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`

Likely Agent D target families after Agent A review:

- Building placement request validation and placement result systems.
- Building spawn processors that instantiate ECS prefab entities.
- Building production progression, queue, transport handoff, and produced-unit request systems.
- Building grid/footprint/runtime state systems.
- Building startup/config projection systems when they are data-only or can be split.
- Earlier five-SystemBase tracker targets that remain open and are assigned to Agent D.

Likely not Agent D:

- UI build menu population and click handling.
- Building visual markers, faction color, construction mesh/material, smoke, fire, or explosion effects.
- Unit selection command intent, unless Agent C has already converted the request boundary and Agent A assigns execution to Agent D.
- Road placement/build execution owned by Agent E.

## D0 - Intake And Existing Tracker Reconciliation

Goal:
Start from the authoritative Phase 7 row list and reconcile with earlier building conversion plans.

- [ ] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [ ] Filter rows assigned to `AgentD`.
- [ ] Read `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`.
- [ ] Map each Agent D row to any existing five-SystemBase tracker item.
- [ ] Identify building systems already converted before Phase 7 and mark them as baseline, not new Phase 7 progress.
- [ ] For each target, run `rg "<TypeName>" Assets/Game/Scripts Assets/Tests`.
- [ ] Record public methods/properties and callers, especially `MatchBootstrapSystem`, composition systems, UI query systems, and tests.
- [ ] Record any prefab GameObject, ScriptableObject, Unity object, or managed collection blockers.
- [ ] Record update group/order attributes and dependencies with selection, transport, production, and visuals.
- [ ] Identify rows that need Agent A reclassification before code changes.
- [ ] Update the progress snapshot denominator and current target.

Acceptance:

- Agent D has a row list with no overlap with Agent C/E/F.
- Earlier five-SystemBase work is not duplicated.
- Mixed systems are tagged for split before conversion.

## D1 - Building Responsibility Split

Goal:
Prevent broad building systems from becoming broad `ISystem` structs.

- [ ] For each target, classify responsibilities as request intake, validation, grid/footprint calculation, data mutation, ECS prefab spawn, production progression, result publication, visual feedback, config projection, or managed presentation.
- [ ] Create or reuse separate request components for placement, spawn, production, and cancellation.
- [ ] Create or reuse result components/buffers for UI/visual feedback.
- [ ] Keep pure placement validation separate from entity instantiation.
- [ ] Keep production ticking separate from produced-unit spawn requests.
- [ ] Keep visuals and preview meshes out of gameplay processors.
- [ ] Keep serialized config and prefab GameObject ownership out of converted `ISystem` code.
- [ ] Prefer small files only when ownership is clear; avoid a new generic building manager/facade.
- [ ] Document the split in the handoff before marking the row complete.

Acceptance:

- Each converted `ISystem` has one primary reason to change.
- Building gameplay policy does not live in managed presentation/config exceptions.
- Visual and UI feedback consume ECS results instead of driving gameplay.

## D2 - Placement Request And Grid Systems

Goal:
Convert building placement data processing while preserving placement behavior.

- [ ] Identify existing placement request entities, selected building type data, footprint data, and grid blocker data.
- [ ] Ensure placement validation reads ECS data or baked config entities, not GameObject prefabs.
- [ ] Convert grid/footprint calculation systems to `ISystem` when all fields are unmanaged.
- [ ] Preserve rotated footprint behavior.
- [ ] Preserve blocked, occupied, out-of-bounds, faction, resource, and terrain constraints.
- [ ] Preserve cancellation and preview/result states.
- [ ] Leave preview rendering to Agent F or a managed presentation exception.
- [ ] Add or update tests for valid placement, blocked placement, rotation, and cancellation.

Acceptance:

- Placement results match previous behavior.
- Converted systems do not depend on Unity objects or UI state.

## D3 - Building Spawn Processors

Goal:
Spawn building ECS prefab entities through data, not GameObject prefab lookup.

- [ ] Identify existing prefab Entity references and source-key data.
- [ ] Confirm bakers provide Entity prefab refs for every buildable building type.
- [ ] Replace any remaining runtime GameObject prefab lookup with ECS prefab entity/source-key lookup.
- [ ] Keep GameObject/prefab preview APIs outside the `ISystem`.
- [ ] Convert ECS prefab instantiation processors to `ISystem` only after production/config inputs are pure ECS.
- [ ] Use ECB safely for instantiate, component set, buffer append, and request consumption.
- [ ] Preserve initial building components, faction assignment, health, production queue, footprint occupancy, and runtime tags.
- [ ] Publish spawn result data for UI/visual systems.
- [ ] Validate that the build menu still lists buildings and unit production remains populated.

Acceptance:

- Buildings spawn through Entity prefab refs.
- No runtime reverse lookup from Entity to GameObject prefab is required.
- Spawn requests are consumed once.

## D4 - Production And Transport Handoff

Goal:
Convert building production systems into narrow ECS processors.

- [ ] Inventory production queue data, produced-unit prefab references, rally point data, resource costs, and transport handoff data.
- [ ] Split production progress ticking from produced-unit spawn execution if currently mixed.
- [ ] Split production validation from transport boarding/assignment if currently mixed.
- [ ] Coordinate transport boarding request/result ownership with the medium-term tracker and Agent C/E if needed.
- [ ] Use ECS prefab entity/source-key data for produced units.
- [ ] Preserve queue order, progress timing, cancellation/refund, capacity, and faction ownership.
- [ ] Preserve rally/spawn point decisions.
- [ ] Publish result data for UI and visuals instead of direct public helper calls.
- [ ] Add tests or smoke validation for producing at least one unit from a building.

Acceptance:

- Unit production still produces the correct unit entities.
- Production state survives selection changes and does not require UI to tick.
- Transport handoff remains deterministic.

## D5 - Config Projection And Startup

Goal:
Keep serialized building config managed while converting gameplay data processors.

- [ ] Identify ScriptableObject, prefab, or serialized MonoBehaviour inputs for building config.
- [ ] Confirm whether config is already baked into ECS authoring data.
- [ ] If config must be projected at runtime, keep the projection in a counted managed config `SystemBase` exception.
- [ ] Ensure projected data uses stable ids/source keys and Entity prefab refs.
- [ ] Ensure projection runs once per match/world and is restart-safe.
- [ ] Do not put ScriptableObject or `UnityEngine.Object` fields inside `ISystem`.
- [ ] Add validation for missing config, missing prefab entity, and duplicate source key cases when practical.

Acceptance:

- Gameplay processors read ECS config data.
- Managed config boundary is explicit, small, and counted.

## D6 - Visual And Selection Coordination

Goal:
Avoid accidental ownership conflicts with Agent C and Agent F.

- [ ] Coordinate building selection command requests with Agent C.
- [ ] Coordinate building placement/building result visual data with Agent F.
- [ ] Keep building health/combat gameplay in ECS data processors.
- [ ] Keep material, renderer, mesh, particle, and preview updates in Agent F managed presentation systems.
- [ ] Do not add direct calls from building gameplay systems to visual systems.
- [ ] Publish ECS result/state data that visuals can observe.
- [ ] Record any shared contract changes in the handoff.

Acceptance:

- Building gameplay can run without direct UI/visual calls.
- Visual systems remain consumers, not gameplay owners.

## D7 - Retire Or Fold Building Helpers

Goal:
Remove wrappers that no longer need to be systems.

- [ ] Identify disabled, unused, or wrapper building `SystemBase` types.
- [ ] Search for serialized, reflection, and code references before removal.
- [ ] Fold pure helper logic into static domain methods or the nearest narrow `ISystem` only when ownership is obvious.
- [ ] Do not delete a referenced scene/prefab script without a serialized-reference migration approved by Agent A.
- [ ] Update tests that referenced retired helpers.
- [ ] Record retired/folded count in the progress snapshot.

Acceptance:

- No missing-script references introduced.
- Removed helpers had either no behavior or a clear replacement.

## D8 - Focused Validation Matrix

Goal:
Prove building placement and production still work.

- [ ] Always run `git diff --check -- <changed files>`.
- [ ] Run architecture tests for no forbidden blockers in converted `ISystem` files.
- [ ] Run placement validation tests for valid/invalid placement, rotation, blocked cells, and cancellation.
- [ ] Run building spawn validation for initial components, faction, health, and footprint occupancy.
- [ ] Run production validation for queue, progress, produced unit entity, cancellation, and rally/spawn point.
- [ ] Run a smoke test that opens build gameplay and confirms buildable items are populated.
- [ ] Run a smoke test that starts a match, places a building, and produces a unit when practical.
- [ ] Check Unity logs for missing prefab entity, empty build menu, and runtime exceptions.
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
  -logFile /private/tmp/warline-phase7-agent-d-editmode.log
```

Acceptance:

- Build menu and production lists are not empty.
- Building placement and production behavior match baseline.
- Any failed validation is fixed or handed off as a blocker with log paths.

## D9 - Handoff To Agent A

Goal:
Make Agent D integration safe.

- [ ] Create a dated handoff report under `Design/AgentReports/`.
- [ ] Include inventory row ids, files changed, and final disposition.
- [ ] Include split map: old system responsibilities to new systems/boundaries.
- [ ] Include converted-to-`ISystem`, split passive/managed-boundary, retired/folded, and managed-exception counts.
- [ ] Include validation commands and outcomes.
- [ ] Include any Agent C/E/F coordination notes.
- [ ] Include any rows returned to Agent A for reclassification.
- [ ] Confirm this tracker progress snapshot is current.

Handoff template:

```markdown
# Phase 7 Agent D Handoff - YYYY-MM-DD

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-####` - `TypeName` - `Converted/Split/Retired`

Responsibility split:
- Old:
- New:

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

- Every Agent D row has final status.
- No building gameplay `ISystem` owns UI, GameObject prefab lookup, camera, or visual presentation.
- Broad systems are split by responsibility before completion.
- No MonoBehaviour ticking introduced.
