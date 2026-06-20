# Phase 7 Agent E Tracker - Road Build, Runtime City, Environment, And Citizen Systems

Purpose:
Convert road, runtime-city, environment, grid-blocker, and citizen simulation/read-model systems to focused `ISystem` processors while moving visual, prefab, coroutine, and GameObject ownership to counted managed presentation `SystemBase` exceptions or ECS entity-prefab pipelines. Phase 7 must not introduce updating MonoBehaviour bridges.

Branch:
`codex/phase7-agent-e-road-city-citizen`

Progress snapshot:

- Checklist progress: `0 / 66 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `66`.
- Current target: `E0 - road/city/citizen inventory intake`.
- Direct conversions completed: `0`.
- Split processors created: `0`.
- Managed presentation `SystemBase` exceptions created: `0`.
- Retired/folded helpers: `0`.
- Validation status: `not started`.

Ownership:

- Owns road build and road runtime systems assigned by Agent A.
- Owns runtime-city and environment systems assigned by Agent A.
- Owns citizen population/schedule/danger/resource/read-model systems assigned by Agent A.
- Coordinates with Agent D for road/building and runtime-city building spawn contracts.
- Coordinates with Agent F for road visuals, city visuals, decoration visuals, and citizen visible presentation.

Do not touch:

- Building production/placement internals except agreed contracts.
- Selection command systems.
- Rendering/VFX systems except agreed passive visual request contracts.
- UI Toolkit/Canvas implementation.
- Main Phase 7 tracker except through handoff reports.
- Any new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, or manager-style MonoBehaviour ticker. MonoBehaviours are view/reference holders only.

Candidate examples to verify, not pre-approved:

- `RoadBuild*`
- `RoadNetworkSystem`
- `RoadGridProjectionSystem`
- `RoadRuntime*`
- `RuntimeCity*`
- `RuntimeGridBlockerSystem`
- `RuntimeDecorationSpawnerSystem`
- `DayNightSystem`
- `Citizen*`

## E0 - Inventory Intake

- [ ] Wait for Agent A inventory and guardrails.
- [ ] Pull Agent E rows and inspect source files and call sites.
- [ ] Split rows into road, runtime-city/environment, and citizen batches.
- [ ] Identify managed blockers: preview visuals, chunk visuals, GameObject decoration spawn, coroutine/yield, scene roots, prefab lists, native container disposal, public helper APIs.
- [ ] Identify pure data algorithms that can convert directly.
- [ ] Write an initial Agent E handoff with target groups and cross-agent dependencies.

Acceptance:

- Every Agent E target has a disposition and first safe slice.
- No visual or prefab owner is planned as unmanaged `ISystem`.

## E1 - Road Request/Result Boundary

- [ ] Inventory road input, road build session, placement storage, road network, grid projection, and read-model data.
- [ ] Split pointer/camera/input capture from road ECS command requests.
- [ ] Define or reuse road command request and result components/buffers.
- [ ] Preserve nearest-road, blocker, movement, and minimap contracts.
- [ ] Add road contract validation.

Acceptance:

- Managed road input writes requests only.
- ECS road processors own validation and mutation.

## E2 - Road Direct Conversions

- [ ] Convert road command validation processors to `ISystem` after request data is ECS-owned.
- [ ] Convert road placement storage/grid projection/network updates to `ISystem`.
- [ ] Convert road read-model and minimap event publication to `ISystem` where data-only.
- [ ] Fold road composition/context helpers with no independent runtime responsibility.
- [ ] Keep road preview, chunk visual, and special visual work passive or Agent F-owned.
- [ ] Run road-build command, nearest-road, movement blocker, and minimap validations.

Acceptance:

- Road data state is ECS-owned.
- Visual road presentation is passive.

## E3 - Runtime City Data Split

- [ ] Inventory runtime-city config, lifecycle, layout, plot, road layout, ingress, walkability, spawn bridge, visual, decoration, readiness, and minimap systems.
- [ ] Separate config projection from deterministic generation algorithms.
- [ ] Convert layout, plot, road layout, ingress, walkability, minimap event, read-model, and readiness query systems to `ISystem` when data-only.
- [ ] Use explicit components/buffers for generation phase state instead of managed flags.
- [ ] Preserve generation order with update group/order attributes.
- [ ] Run runtime-city generation focused validation after each batch.

Acceptance:

- Deterministic city generation remains behavior-equivalent.
- Generation state does not require managed `SystemBase`.

## E4 - Runtime City Spawn And Visual Boundaries

- [ ] Identify city building/decoration spawn systems that still own GameObject or prefab selection.
- [ ] Convert spawn decisions to ECS requests only after prefab selection is entity/source-key data.
- [ ] Move GameObject visual spawn behavior to counted managed presentation `SystemBase` exceptions or Agent F-owned entity-prefab visual pipelines.
- [ ] Remove coroutine/yield gameplay ownership from runtime-city systems without replacing it with MonoBehaviour coroutine loops.
- [ ] Replace coroutine/yield behavior with explicit ECS phase components where practical.
- [ ] Coordinate building spawn request contracts with Agent D.
- [ ] Run city spawn, decoration, and match runtime smoke validation.

Acceptance:

- Runtime-city algorithms do not instantiate GameObjects directly.
- Visual spawn ownership is not recurring gameplay `SystemBase` and is not an updating MonoBehaviour.

## E5 - Runtime Grid Blocker And Environment

- [ ] Inspect `RuntimeGridBlockerSystem`, `RuntimeDecorationSpawnerSystem`, and `DayNightSystem`.
- [ ] Make native container ownership and disposal explicit before converting any blocker data processor.
- [ ] Convert blocker data updates to `ISystem` only after managed object references are removed or split.
- [ ] Move decoration spawning to entity-prefab requests or counted managed presentation `SystemBase` exceptions.
- [ ] Keep day/night light/material work in counted managed presentation `SystemBase` exceptions unless represented as ECS data and Agent F agrees.
- [ ] Run blocker, movement, runtime-city, and visual smoke validations.

Acceptance:

- Grid blocker ECS data updates are deterministic and disposal-safe.
- Environment presentation does not own gameplay policy.

## E6 - Citizen Simulation

- [ ] Inventory citizen population composition, lifecycle, state, schedule, danger, resource, refugee, building-read, household registration, diagnostics, debug, read-model, and visible-unit systems.
- [ ] Convert citizen totals/state/schedule/danger/resource/refugee/read-model systems to `ISystem` where data-only.
- [ ] Replace managed dictionaries/lists with ECS buffers/components or passive presentation state.
- [ ] Split visible-citizen spawn/lifetime into request, instantiate, movement-state, and lifetime processors.
- [ ] Coordinate citizen visible presentation with Agent F if visuals are touched.
- [ ] Run citizen population, visible unit, movement, and combat/death validations.

Acceptance:

- Citizen simulation policy is ECS-owned.
- Citizen visible presentation does not block data conversion.

## E7 - Agent E Completion

- [ ] Run `git diff --check`.
- [ ] Run road-build validation.
- [ ] Run runtime-city generation validation.
- [ ] Run runtime-grid blocker and movement validation.
- [ ] Run citizen population and visible-unit validation.
- [ ] Run match runtime smoke if city or citizen spawning changed.
- [ ] Run architecture guardrails.
- [ ] Write `Design/AgentReports/YYYY-MM-DD_phase7_agent_e_road_city_citizen_handoff.md`.

Handoff format:

- Checklist progress.
- Road systems converted/split/retired.
- Runtime-city systems converted/split/retired.
- Citizen systems converted/split/retired.
- Managed presentation `SystemBase` exceptions created.
- Cross-agent contracts changed.
- Validation commands and logs.
- Remaining blockers.
