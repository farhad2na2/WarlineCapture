# Phase 7 Agent E Tracker - Road, City, Grid, Citizen, And Environment Gameplay

Purpose:
Convert non-UI road building, city/runtime environment, grid blocker, and citizen gameplay `SystemBase` systems into narrow `ISystem` processors. Agent E owns simulation/data work for roads, city state, runtime blockers, and citizens. It does not own UI, rendering, camera, prefab GameObject presentation, or visual effects.

Branch:
`codex/phase7-agent-e-road-city-citizen`

Execution order:

1. Wait for Agent A to publish authoritative inventory rows and guardrails.
2. Pull only rows assigned to `AgentE`.
3. Classify each row as direct convert, split-then-convert, retire/fold, or managed exception.
4. Convert in small behavior-preserving slices, starting with pure ECS data systems and ending with split mixed systems.
5. Write handoffs under `Design/AgentReports/`; Agent A owns main tracker integration.

Progress snapshot:

- Checklist progress: `12 / 91 complete (13.2%)`.
- In progress: `0`.
- Remaining open: `79`.
- Current target: `E7 helper fold complete for P7-0181 RuntimeCityYardGateSystem; continue with the next low-risk Agent E row`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Retired/folded helpers: `12`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `RuntimeCityYardGateSystem folded from disabled SystemBase wrapper into a plain runtime-city yard-gate helper. Compile, inventory regeneration, runtime-city focused validation, and Phase 7 architecture guard passed. Latest logs include /private/tmp/warline-phase7-agent-e-runtime-city-yard-gate-helper-fold-city.log and /private/tmp/warline-phase7-agent-a-architecture.log. Inventory now reports 133 production SystemBase/legacy declarations, 133 production ISystem declarations, and 50.0% production ISystem share.`

Owned files:

- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- Agent E rows assigned by `Design/Architecture/systembase_to_isystem_inventory.md`
- Road/city/citizen focused tests or validation runners when needed
- Agent E handoff reports under `Design/AgentReports/`

Do not touch:

- UI Toolkit/Canvas systems, build menu views, HUD, or citizen UI views.
- Building placement/production systems owned by Agent D.
- Selection command-intent systems owned by Agent C, except agreed request/result contracts.
- Visual rendering, markers, materials, particles, line renderers, lights, or mesh presentation owned by Agent F.
- Scenes, prefabs, terrain/visual assets, or ScriptableObject config unless an assigned row requires a passive ECS data projection.
- Shared trackers except this file.

Shared rules:

- Do not introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers.
- MonoBehaviours are view/reference holders only and must not run road/city/citizen gameplay loops.
- Managed Unity-object ticking belongs in counted managed `SystemBase` exceptions when unavoidable.
- Converted `ISystem` files must not use `GameObject`, `Transform`, `UnityEngine.Object`, `Object.Instantiate`, `Object.Destroy`, `Camera.main`, hierarchy lookup, service locators, or mutable static gameplay state.
- Do not replace a broad road/city/citizen `SystemBase` with one broad `ISystem`.
- Preserve Unity `.meta` files.

Reference documents:

- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`

Likely Agent E target families after Agent A review:

- Road placement/build request processors and road network state updates.
- Runtime grid blocker and obstacle data systems.
- Runtime city composition data processors that can be separated from visuals.
- Citizen population, citizen visible-unit, citizen movement, and citizen simulation systems.
- Environment/runtime state processors without Unity object presentation ownership.

Likely not Agent E:

- Road/build UI, menu interactions, and UI Toolkit screens.
- Road/city/citizen visual meshes, materials, particles, or presentation effects.
- Building placement/production execution.
- Camera and map-surface visual presentation.

## E0 - Intake And Ownership Map

Goal:
Create a precise Agent E worklist from Agent A's inventory.

- [ ] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [ ] Filter rows assigned to `AgentE`.
- [ ] Copy row ids, type names, paths, dispositions, blockers, and validation gates into this tracker or an Agent E intake report.
- [ ] For each target, run `rg "<TypeName>" Assets/Game/Scripts Assets/Tests`.
- [ ] Record public methods/properties and every caller.
- [ ] Record update group/order attributes and dependencies with building, selection, transport, runtime grid, and visual systems.
- [ ] Record managed blockers: GameObject, Transform, Renderer, Material, ParticleSystem, camera, ScriptableObject, prefab, mesh, or managed collections.
- [ ] Identify whether each target owns simulation policy, request processing, result publication, config projection, or presentation.
- [ ] Return rows to Agent A when ownership overlaps with Agent C/D/F.
- [ ] Update the progress snapshot denominator and current target.

Acceptance:

- Agent E has a concrete row list and no ownership overlap.
- Mixed simulation/presentation systems are marked for split.
- No runtime code changed before intake is complete.

## E1 - Road Request And Result Contracts

Goal:
Separate road player intent, validation, mutation, and presentation.

- [ ] Inventory existing road build request components, road preview data, road network data, and road placement results.
- [ ] Coordinate request-intent ownership with Agent C if player command input is involved.
- [ ] Keep Agent E responsible for road validation and road data mutation.
- [ ] Define or reuse ECS result data for UI and visuals to observe.
- [ ] Ensure request data uses ECS entity/source-key/grid coordinates, not GameObject references.
- [ ] Preserve road placement constraints, adjacency, blocked cells, faction/resource constraints, and terrain/grid rules.
- [ ] Ensure one-shot requests are consumed exactly once.
- [ ] Keep road preview rendering and mesh/material changes in Agent F or managed presentation exceptions.

Acceptance:

- Road processing can run from ECS requests without UI or visual calls.
- Road results are visible to UI/visual consumers through ECS data.

## E2 - Road Systems Conversion

Goal:
Convert road data systems to `ISystem` in narrow slices.

- [ ] Start with the lowest-risk pure ECS road row.
- [ ] Inspect lifecycle, query shape, and ordering.
- [ ] Replace `SystemBase` lifecycle methods with `ISystem` lifecycle methods when no managed fields remain.
- [ ] Replace `Entities.ForEach` with `SystemAPI.Query`, explicit query iteration, or `IJobEntity`.
- [ ] Use refreshed component/buffer lookups for road network reads and writes.
- [ ] Preserve road creation, deletion, connection, path validity, and blocker interactions.
- [ ] Preserve determinism for road segment ids/source keys if existing code relies on stable order.
- [ ] Keep mesh/material/visual updates outside the converted `ISystem`.
- [ ] Add tests for road add, blocked road, adjacent connection, and cleanup when missing.

Acceptance:

- Road behavior matches baseline.
- Converted systems have no GameObject, Transform, renderer, or material dependencies.

## E3 - Runtime Grid And Blocker Systems

Goal:
Convert grid/blocker state updates without visual or scene-object coupling.

- [ ] Inventory grid blocker components, runtime blocker requests, occupancy data, and consumers.
- [ ] Identify blockers sourced from buildings, roads, vehicles, citizens, terrain, or city composition.
- [ ] Convert pure blocker mutation and cleanup systems to `ISystem`.
- [ ] Preserve add/remove behavior during spawn, destroy, road/build placement, and match restart.
- [ ] Preserve blocker priority and overlap rules.
- [ ] Avoid direct scene-object or transform reads in converted systems.
- [ ] If scene object data must be sampled, keep that sampling in a managed config/presentation boundary and project ECS data.
- [ ] Validate with placement and movement smoke paths.

Acceptance:

- Occupancy/blocker data stays correct after spawn, destroy, placement, and restart.
- Converted blocker systems are data-only.

## E4 - Runtime City Composition Data

Goal:
Split runtime city generation/composition into ECS data processors and managed presentation boundaries.

- [ ] Inventory city composition systems, city spawn requests, map cells, generated entities, and visual presentation calls.
- [ ] Identify whether city data is baked, generated at runtime, or projected from serialized config.
- [ ] Convert pure data generation/update systems to `ISystem`.
- [ ] Keep mesh, renderer, material, prefab GameObject, and decoration presentation in Agent F or managed exceptions.
- [ ] Ensure generated city data uses stable source keys and deterministic ordering.
- [ ] Preserve restart/shutdown cleanup and avoid stale entity references.
- [ ] Validate city composition in a fresh match and after returning to menu/restarting.

Acceptance:

- Runtime city data is ECS-readable and deterministic.
- Visual presentation stays outside gameplay processors.

## E5 - Citizen Simulation And Visibility

Goal:
Convert citizen gameplay systems without changing visual presentation.

- [ ] Inventory citizen population data, visible-unit data, spawn requests, despawn requests, and movement/combat interactions.
- [ ] Identify GameObject/prefab dependencies and confirm whether Entity prefab refs already exist.
- [ ] Convert pure population, selection, visibility, and request systems to `ISystem`.
- [ ] Keep citizen visual spawning/presentation in Agent F or managed presentation exceptions when Unity objects are required.
- [ ] Preserve citizen counts, spawn timing, team/faction filters, death cleanup, and visibility behavior.
- [ ] Preserve any citizen-to-unit interaction that affects combat or movement.
- [ ] Add tests for population update, visible unit lookup, spawn request, death cleanup, and restart if missing.

Acceptance:

- Citizen gameplay data remains correct and runtime visual behavior is not degraded.
- Converted systems do not own GameObject prefab APIs.

## E6 - Config And Serialized Boundary Handling

Goal:
Avoid pushing serialized Unity object/config ownership into unmanaged systems.

- [ ] Identify serialized config assets used by road, city, grid, or citizen systems.
- [ ] Confirm whether data is already baked into ECS.
- [ ] For runtime projection, keep a small counted managed config `SystemBase` exception.
- [ ] Project only blittable ECS data, Entity prefab refs, and stable source keys.
- [ ] Ensure projection is one-shot or explicitly gated.
- [ ] Validate missing config and missing prefab entity behavior where practical.

Acceptance:

- Converted systems consume ECS config data.
- Managed config exceptions are explicit and counted.

## E7 - Retire Or Fold Helpers

Goal:
Remove dead wrappers instead of converting them.

- [ ] Identify disabled, unused, or wrapper road/city/citizen `SystemBase` types.
- [ ] Search all code and serialized references before retiring.
- [ ] Fold pure helper logic into static domain methods or nearest narrow `ISystem` only when ownership is obvious.
- [ ] Do not delete referenced scene/prefab scripts without Agent A-approved serialized-reference migration.
- [ ] Update tests that referenced retired helpers.
- [ ] Record retired/folded count in the progress snapshot.
- [x] Fold `P7-0147 RuntimeCityBuildingSpawnContextSystem` from a disabled `SystemBase` wrapper into a plain runtime-city spawn context helper; context creation, fallback context creation, building spawn system package data, and runtime city composition ownership stayed unchanged.
- [x] Fold `P7-0157 RuntimeCityDiagnosticSystem` from a disabled `SystemBase` wrapper into a plain runtime-city diagnostic helper; diagnostic logging behavior and runtime city composition ownership stayed unchanged.
- [x] Fold `P7-0170 RuntimeCityReadModelSystem` from a disabled `SystemBase` wrapper into a plain runtime-city read-model helper; read-model properties, `Publish`, and runtime grid/decorations consumers stayed unchanged.
- [x] Fold `P7-0171 RuntimeCityReadinessQuerySystem` from a disabled `SystemBase` wrapper into a plain runtime-city readiness query helper; grid, initial spawn, and base-exclusion query behavior stayed unchanged.
- [x] Fold `P7-0191 CitizenPopulationDiagnosticSystem` from a disabled `SystemBase` wrapper into a plain citizen diagnostics helper; frame timing APIs and citizen lifecycle callers stayed unchanged.
- [x] Fold `P7-0195 CitizenPopulationReadModelSystem` from a disabled `SystemBase` wrapper into a plain citizen read-model helper; totals state, refresh/reset APIs, and runtime/UI read callers stayed unchanged.
- [x] Fold `P7-0211 RoadBuildCompositionSourceSystem` from a disabled `SystemBase` wrapper into a plain road build composition source helper; child-system source fields, resolver state, and direct road composition ownership stayed unchanged.
- [x] Fold `P7-0214 RoadBuildContextSystem` from a disabled `SystemBase` wrapper into a plain road build context helper; ECS boundary context construction and road composition callers stayed unchanged.
- [x] Fold `P7-0220 RoadBuildInteractionContextSystem` from a disabled `SystemBase` wrapper into a plain road build interaction context helper; session/input/command/delete prompt context construction and road runtime action callers stayed unchanged.
- [x] Fold `P7-0224 RoadBuildReadModelSystem` from a disabled `SystemBase` wrapper into a plain road build read-model helper; public read properties, configure/clear API, and selection/camera consumers stayed unchanged.
- [x] Fold `P7-0234 RoadRuntimeGenerationContextSystem` from a disabled `SystemBase` wrapper into a plain road runtime generation context helper; deferred road ECS sync context construction and road runtime generation callers stayed unchanged.
- [x] Fold `P7-0181 RuntimeCityYardGateSystem` from a disabled `SystemBase` wrapper into a plain runtime-city yard-gate helper; gate-side/opening calculations, state access, and runtime-city composition callers stayed unchanged.

Acceptance:

- No missing-script references introduced.
- Retired logic is either unused or has a clear replacement.

## E8 - Focused Validation Matrix

Goal:
Prove road, city, grid, and citizen behavior still works.

- [ ] Always run `git diff --check -- <changed files>`.
- [ ] Run architecture tests for no forbidden blockers in converted `ISystem` files.
- [ ] Run road validation for build, blocked placement, adjacency, cleanup, and restart.
- [ ] Run runtime grid blocker validation for add/remove/overlap/restart.
- [ ] Run city composition validation for generation, cleanup, and deterministic data.
- [ ] Run citizen validation for population update, visible unit spawn, death cleanup, and restart.
- [ ] Run a PlayMode smoke test that starts a match and exercises at least one road/build/citizen path when practical.
- [ ] Check Unity logs for missing prefab entity, stale entity, destroyed system state, or empty runtime data.
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
  -logFile /private/tmp/warline-phase7-agent-e-editmode.log
```

Acceptance:

- Focused validation passes or failures are fixed in-slice.
- Any unrun validation is recorded with a concrete reason.

## E9 - Handoff To Agent A

Goal:
Make Agent E integration clear.

- [ ] Create a dated handoff report under `Design/AgentReports/`.
- [ ] Include inventory row ids, files changed, and final disposition.
- [ ] Include split map for mixed systems.
- [ ] Include converted-to-`ISystem`, split passive/managed-boundary, retired/folded, and managed-exception counts.
- [ ] Include road/city/citizen request/result contracts added or changed.
- [ ] Include validation commands, outcomes, and log paths.
- [ ] Include coordination notes for Agents C/D/F.
- [ ] Include any rows returned to Agent A for reclassification.
- [ ] Confirm this tracker progress snapshot is current.

Handoff template:

```markdown
# Phase 7 Agent E Handoff - YYYY-MM-DD

Branch:
`codex/phase7-agent-e-road-city-citizen`

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

- Every Agent E row has final status.
- No road/city/citizen gameplay `ISystem` owns UI, GameObject prefab lookup, camera, or rendering presentation.
- Mixed simulation/presentation systems are split.
- No MonoBehaviour ticking introduced.
