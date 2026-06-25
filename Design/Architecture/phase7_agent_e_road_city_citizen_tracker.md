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

- Checklist progress: `87 / 109 complete (79.8%)`.
- In progress: `0`.
- Remaining open: `10`.
- Current target: `E7 helper fold complete for P7-0206 CitizenVisibleUnitSystem; Agent E open split candidates complete; review remaining counted managed exceptions`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Retired/folded helpers: `87`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `CitizenVisibleUnitSystem folded from a disabled SystemBase wrapper into a plain citizen visible-unit helper. Compile, inventory regeneration, citizen visible-unit focused validation, git diff --check, and Phase 7 architecture guard passed. Latest logs include /private/tmp/warline-phase7-agent-e-citizen-visible-unit-helper-fold-citizen-visible-unit.log and /private/tmp/warline-phase7-agent-a-architecture.log. Inventory now reports 58 production SystemBase/legacy declarations, 134 production ISystem declarations, and 69.8% production ISystem share.`

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
- [x] Fold `P7-0147 RuntimeCityBuildingSpawnContextCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city spawn context helper; context creation, fallback context creation, building spawn system package data, and runtime city composition ownership stayed unchanged.
- [x] Fold `P7-0157 RuntimeCityDiagnosticsSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city diagnostic helper; diagnostic logging behavior and runtime city composition ownership stayed unchanged.
- [x] Fold `P7-0170 RuntimeCityReadModelCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city read-model helper; read-model properties, `Publish`, and runtime grid/decorations consumers stayed unchanged.
- [x] Fold `P7-0171 RuntimeCityReadinessQueryCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city readiness query helper; grid, initial spawn, and base-exclusion query behavior stayed unchanged.
- [x] Fold `P7-0191 CitizenPopulationDiagnosticSystem` from a disabled `SystemBase` wrapper into a plain citizen diagnostics helper; frame timing APIs and citizen lifecycle callers stayed unchanged.
- [x] Fold `P7-0195 CitizenPopulationReadModelSystem` from a disabled `SystemBase` wrapper into a plain citizen read-model helper; totals state, refresh/reset APIs, and runtime/UI read callers stayed unchanged.
- [x] Fold `P7-0211 RoadBuildCompositionSourceSystem` from a disabled `SystemBase` wrapper into a plain road build composition source helper; child-system source fields, resolver state, and direct road composition ownership stayed unchanged.
- [x] Fold `P7-0214 RoadBuildContextSystem` from a disabled `SystemBase` wrapper into a plain road build context helper; ECS boundary context construction and road composition callers stayed unchanged.
- [x] Fold `P7-0220 RoadBuildInteractionContextSystem` from a disabled `SystemBase` wrapper into a plain road build interaction context helper; session/input/command/delete prompt context construction and road runtime action callers stayed unchanged.
- [x] Fold `P7-0224 RoadBuildReadModelSystem` from a disabled `SystemBase` wrapper into a plain road build read-model helper; public read properties, configure/clear API, and selection/camera consumers stayed unchanged.
- [x] Fold `P7-0234 RoadRuntimeGenerationContextSystem` from a disabled `SystemBase` wrapper into a plain road runtime generation context helper; deferred road ECS sync context construction and road runtime generation callers stayed unchanged.
- [x] Fold `P7-0181 RuntimeCityYardGateSystem` from a disabled `SystemBase` wrapper into a plain runtime-city yard-gate helper; gate-side/opening calculations, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0167 RuntimeCityLifecycleCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city lifecycle helper; generation routine ownership, spawned/generating flags, lifecycle context, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0172 RuntimeCityRoadBuildBridgeCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city road-build bridge helper; road runtime generation bridge calls, deferred ECS sync hooks, road-cell sizing, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0226 RoadBuildSessionSystem` from a disabled `SystemBase` wrapper into a plain road-build session helper; build mode/session state, delete prompt state, command callbacks, and road composition callers stayed unchanged.
- [x] Fold `P7-0231 RoadNetworkSystem` from a disabled `SystemBase` wrapper into a plain road network graph helper; stroke graph state, road tile data, snapshot/restore behavior, and road composition callers stayed unchanged.
- [x] Fold `P7-0235 RoadRuntimeGenerationSystem` from a disabled `SystemBase` wrapper into a plain runtime road generation helper; runtime road stroke creation, deferred ECS sync callbacks, special visual bridge calls, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0208 RoadBuildCommandSystem` from a disabled `SystemBase` wrapper into a plain road-build command helper; EntityManager command queue/buffer API, synchronous command processing, result writing, and road composition callers stayed unchanged.
- [x] Fold `P7-0164 RuntimeCityLandmarkOffsetSystem` from a disabled `SystemBase` wrapper into a plain runtime-city landmark-offset helper; landmark offset arrays, hall-distance filtering, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0163 RuntimeCityIngressSystem` from a disabled `SystemBase` wrapper into a plain runtime-city ingress helper; city layout creation, incoming-anchor stroke wiring, connection-cell math, ingress corridor pruning, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0168 RuntimeCityMinimapEventSystem` from a disabled `SystemBase` wrapper into a plain runtime-city minimap event helper; static minimap change publication, UI-facing flush, clear behavior, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0173 RuntimeCityRoadCommitCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city road commit helper; road network commit, source-exit/autobahn commit, standalone connector handoff, state access, and runtime-city composition/generation callers stayed unchanged.
- [x] Fold `P7-0174 RuntimeCityRoadLayoutUtilitySystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city road layout helper; town road stroke creation, straight/autobahn path planning, stroke append behavior, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0149 RuntimeCityBulkPlotPlanUtilitySystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city bulk plot plan helper; central/outer/entry plot plan creation, prefab-selection shuffling, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0150 RuntimeCityChainUtilitySystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city chain helper; next-city planning, exit/autobahn path validation, chain context, state access, and runtime-city composition/generation callers stayed unchanged.
- [x] Fold `P7-0154 RuntimeCityCorridorBuildingSpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city corridor building spawn helper; corridor roadside plot placement, shop/house spawn calls, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0158 RuntimeCityEntryBuildingSpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city entry building spawn helper; entry shop/house plot placement, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0175 RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city roadside building spawn helper; roadside plan creation, gas station/shop/house placement, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0179 RuntimeCitySurfaceIntegrationUtilitySystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city surface integration helper; building footprint surface checks, road path surface validation, primary surface sampling, and runtime-city visual callers stayed unchanged.
- [x] Fold `P7-0182 RuntimeCityYardWallPlanSystem` from a disabled `SystemBase` wrapper into a plain runtime-city yard-wall plan helper; house-plan shuffling, yard rectangle search, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0190 CitizenPopulationDebugSystem` from a disabled `SystemBase` wrapper into a plain citizen debug helper; debug snapshot/status/kill helpers, ECS projection reads, delegate type, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0193 CitizenPopulationEventSystem` from a disabled `SystemBase` wrapper into a plain citizen population event helper; home-building destroyed and visible-citizen destroyed event paths, refugee handoff delegates, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0194 CitizenPopulationLifecycleSystem` from a disabled `SystemBase` wrapper into a plain citizen population lifecycle helper; update interval state, path-job skip handling, totals refresh, and citizen composition/runtime update callers stayed unchanged.
- [x] Fold `P7-0196 CitizenPopulationRuntimeUpdateSystem` from a disabled `SystemBase` wrapper into a plain citizen population runtime update helper; runtime bind/reset, logical citizen update, visible sync, store/death callbacks, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0198 CitizenPopulationTotalsSystem` from a disabled `SystemBase` wrapper into a plain citizen population totals helper; totals calculation, citizen/household data checks, read-model refresh, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0202 CitizenResourceSystem` from a disabled `SystemBase` wrapper into a plain citizen resource helper; resource context delegates, configuration checks, dollar spend clamping, and citizen refugee callers stayed unchanged.
- [x] Fold `P7-0203 CitizenScheduleSystem` from a disabled `SystemBase` wrapper into a plain citizen schedule helper; schedule phase, target-building, weekday/weekend/refugee status policy, and citizen runtime callers stayed unchanged.
- [x] Fold `P7-0216 RoadBuildDependencySystem` from a disabled `SystemBase` wrapper into a plain road-build dependency helper; dependency state, building-interaction binding, command-mode calls, minimap configuration, and road composition callers stayed unchanged.
- [x] Fold `P7-0217 RoadBuildDisposalSystem` from a disabled `SystemBase` wrapper into a plain road-build disposal helper; disposal context, runtime root cleanup, visual/cache cleanup, ECS boundary cleanup, road tile clearing, and road composition lifecycle callers stayed unchanged.
- [x] Fold `P7-0222 RoadBuildMutationSystem` from a disabled `SystemBase` wrapper into a plain road-build mutation helper; stroke creation/deletion, session snapshot/restore, dirty-cell refresh, and road composition callers stayed unchanged.
- [x] Fold `P7-0228 RoadDeletePromptSystem` from a disabled `SystemBase` wrapper into a plain road-delete prompt helper; existing IMGUI prompt rendering, delete/cancel actions, session prompt state, and road-build runtime callers stayed unchanged.
- [x] Fold `P7-0230 RoadMinimapEventSystem` from a disabled `SystemBase` wrapper into a plain road-minimap event helper; direct source ownership, static minimap change notification, UI binding, clear/flush behavior, and road composition callers stayed unchanged.
- [x] Fold `P7-0232 RoadPathPlanningSystem` from a disabled `SystemBase` wrapper into a plain road-path planning helper; drag-axis resolution, path building, preview-plan dirty cells/edges, preview masks, and road-build input/preview callers stayed unchanged.
- [x] Fold `P7-0237 RoadSurfacePlacementSystem` from a disabled `SystemBase` wrapper into a plain road-surface placement helper; surface configuration, path validation, primary sample evaluation, road surface type resolution, and road-build/runtime-city callers stayed unchanged.
- [x] Fold `P7-0238 RuntimeGridBootstrapSystem` from a disabled `SystemBase` wrapper into a plain runtime-grid bootstrap helper; explicit EntityManager bootstrap, grid config projection, grid buffers, dynamic blocker/occupancy storage, path pool setup, and match-bootstrap caller behavior stayed unchanged.
- [x] Fold `P7-0144 RuntimeCityArchwaySpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city archway spawn helper; central archway placement, prefab list handling, plot spacing, reserved footprints, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0145 RuntimeCityBuildingPlacementPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city building placement helper; request/result contract, footprint lookup, spawn-and-reserve behavior, road overlap checks, reserved-footprint updates, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0148 RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city bulk building spawn routine helper; generation random state, spawn coroutine sequencing, entry/roadside/rural/decorative placement calls, yard-wall/decor callback delegates, and runtime-city generation callers stayed unchanged.
- [x] Fold `P7-0151 RuntimeCityClothCoverSpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city cloth-cover spawn helper; prefab shuffling, adjacent-origin search, spawn-and-reserve request behavior, reserved-footprint use, and decoration building callers stayed unchanged.
- [x] Fold `P7-0153 RuntimeCityConfigCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city config helper; snapshot defaults, config projection, prefab-list fallback behavior, current snapshot state, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0155 RuntimeCityDecorationBuildingSpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city decoration building spawn helper; decoration group selection, cloth-cover/archway/free-scatter sequencing, remaining-count calculation, and runtime-city generation delegate callers stayed unchanged.
- [x] Fold `P7-0156 RuntimeCityDecorationGroupPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city decoration prefab grouping helper; cloth-cover, archway, free-scatter grouping, null-prefab handling, group contract, and decoration building callers stayed unchanged.
- [x] Fold `P7-0159 RuntimeCityFreeScatterDecorationPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city free-scatter decoration helper; scatter plot sampling, road/spacing rejection, random prefab selection, spawn-and-reserve behavior, used-plot tracking, and decoration building callers stayed unchanged.
- [x] Fold `P7-0161 RuntimeCityHallSpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime-city hall spawn helper; civic-center placement, hall-prefab shuffling, landmark offset checks, spawn-and-reserve behavior, and generation callers stayed unchanged.
- [x] Fold `P7-0162 RuntimeCityHouseYardWallSystem` from a disabled `SystemBase` wrapper into a plain runtime-city house-yard-wall helper; yard wall plan selection, yard rectangle search, gate-side selection, wall prefab selection, boundary visual calls, footprint reservation, and generation callers stayed unchanged.
- [x] Fold `P7-0165 RuntimeCityLandmarkSpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime-city landmark spawn helper; clock tower, fountain, monument, and pillar selection, hall-distance rejection, spawn-and-reserve behavior, and generation callers stayed unchanged.
- [x] Fold `P7-0176 RuntimeCityRuralBuildingSpawnPrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city rural building spawn helper; rural plot sampling, distance and road rejection, prefab selection, spawn-and-reserve behavior, used-plot tracking, placement anchors, and generation callers stayed unchanged.
- [x] Fold `P7-0177 RuntimeCitySpawnBridgePrefabSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city spawn bridge helper; runtime building spawn-system binding, deferred side-effect calls, city building spawn/delete bridge behavior, and composition/generation callers stayed unchanged.
- [x] Fold `P7-0169 RuntimeCityPrefabSelectionSystem` from a disabled `SystemBase` wrapper into a plain runtime-city prefab selection helper; prefab membership checks, random selection, shuffling, footprint caching, and composition callers stayed unchanged.
- [x] Fold `P7-0178 RuntimeCityStartupSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city startup helper; startup readiness evaluation, manual generation evaluation, blocker descriptions, diagnostic wait logging, and composition callers stayed unchanged.
- [x] Fold `P7-0186 CitizenDangerSystem` from a disabled `SystemBase` wrapper into a plain citizen danger helper; danger-source registration, periodic position refresh, safe-building selection, flee-target selection, and citizen population callers stayed unchanged.
- [x] Fold `P7-0207 RoadBuildBuildingPlacementSystem` from a disabled `SystemBase` wrapper into a plain road build building placement helper; placement drag state, preview instance creation/cancellation, footprint positioning, validity checks, and placement visual callbacks stayed unchanged.
- [x] Fold `P7-0236 RoadRuntimeRootSystem` from a disabled `SystemBase` wrapper into a plain road runtime root helper; runtime road/building root creation and disposal, child-root naming, and road build composition callers stayed unchanged.
- [x] Fold `P7-0210 RoadBuildCompositionLifecycleSystem` from a disabled `SystemBase` wrapper into a plain road build lifecycle helper; initialization, dependency binding, disposal, exit-build-mode fallback, and road composition callers stayed unchanged.
- [x] Fold `P7-0209 RoadBuildCompositionContextSystem` from a disabled `SystemBase` wrapper into a plain road build context factory helper; road footprint, runtime generation, read-model, input, command, delete prompt, disposal, ECS, visual, mutation, and placement context creation stayed unchanged.
- [x] Fold `P7-0212 RoadBuildCompositionSystem` from a disabled `SystemBase` wrapper into a plain road build composition helper; initialization result, runtime update/GUI/dispose delegates, dependency binding, and managed startup callers stayed unchanged.
- [x] Fold `P7-0225 RoadBuildRuntimeActionSystem` from a disabled `SystemBase` wrapper into a plain road build runtime action helper; command processing, input update, GUI prompt routing, state creation, and road composition source callers stayed unchanged.
- [x] Fold `P7-0218 RoadBuildEcsBoundarySystem` from a disabled `SystemBase` wrapper into a plain road build ECS boundary helper; entity-manager resolution, blocker/combat entity creation, runtime link attachment, player unit spawn, and runtime building disposal stayed unchanged.
- [x] Fold `P7-0233 RoadPreviewSystem` from a disabled `SystemBase` wrapper into a plain road preview helper; preview object pooling, material alpha copies, path preview rebuild, clear/update/dispose API, and road composition callers stayed unchanged.
- [x] Fold `P7-0152 RuntimeCityCompositionSystem` from a disabled `SystemBase` wrapper into a plain runtime-city composition helper; startup configuration, lifecycle tick, read-model publication, manual generation entry point, child-boundary composition, and disposal callers stayed unchanged.
- [x] Fold `P7-0160 RuntimeCityGenerationSystem` from a disabled `SystemBase` wrapper into a plain runtime-city generation helper; generation state, city-generation coroutine orchestration, deferred road/building side-effect ordering, minimap event publication, and composition callers stayed unchanged.
- [x] Fold `P7-0183 RuntimeDecorationSpawnerPresentationSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime decoration spawner helper; config application, prefab selection, decoration placement, combine scheduling, update/dispose behavior, and gameplay startup callers stayed unchanged.
- [x] Fold `P7-0184 RuntimeGridBlockerPresentationSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime grid blocker helper; blocker config, prefab metadata, ECS dependency-state publication, blocker entity/object creation, removal, update/dispose behavior, and gameplay startup callers stayed unchanged.
- [x] Fold `P7-0189 CitizenPopulationCompositionSystem` from a disabled `SystemBase` wrapper into a plain citizen population composition helper; child helper creation, initialization, visible-citizen cleanup, read-model refresh, event binding, disposal, and building composition callers stayed unchanged.
- [x] Fold `P7-0206 CitizenVisibleUnitSystem` from a disabled `SystemBase` wrapper into a plain citizen visible-unit helper; sync, spawn, remove, clear, same-frame entity setup, movement-command enqueueing, and citizen population callers stayed unchanged.
- [x] Fold `P7-0219 RoadBuildInputSystem` from a disabled `SystemBase` wrapper into a plain road build input helper; pointer handling, build/delete gesture state, preview callbacks, building placement drag handling, and road composition callers stayed unchanged.
- [x] Fold `P7-0221 RoadBuildInteractionSystem` from a disabled `SystemBase` wrapper into a plain road build interaction helper; building placement commit, selection hit tests, building selection, deletion, ECS entity cleanup, and storage callbacks stayed unchanged.
- [x] Fold `P7-0223 RoadBuildPlacementStorageSystem` from a disabled `SystemBase` wrapper into a plain road build placement storage helper; runtime building collection state, active placement storage, building id allocation, selection state, and road composition callers stayed unchanged.
- [x] Fold `P7-0146 RuntimeCityBuildingPlotUtilitySystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-city plot algorithm helper; plot candidate types, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0166 RuntimeCityLayoutSystem` from a disabled `SystemBase` wrapper into a plain runtime-city layout algorithm helper; city layout types, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0180 RuntimeCityWalkabilitySystem` from a disabled `SystemBase` wrapper into a plain runtime-city walkability helper; reserved-footprint types, state access, and runtime-city composition callers stayed unchanged.
- [x] Fold `P7-0204 CitizenStatusTransitionSystem` from a disabled `SystemBase` wrapper into a plain citizen status transition helper; status policy methods, delegate type, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0197 CitizenPopulationStateSystem` from a disabled `SystemBase` wrapper into a plain citizen population state holder; dictionaries, scratch lists, id allocation, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0185 CitizenBuildingReadSystem` from a disabled `SystemBase` wrapper into a plain citizen building read helper; runtime building list refresh, lookup helpers, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0187 CitizenHouseholdRegistrationSystem` from a disabled `SystemBase` wrapper into a plain citizen household registration helper; household assignment, dwelling registration, delegate types, and citizen composition callers stayed unchanged.
- [x] Fold `P7-0192 CitizenPopulationEcsProjectionSystem` from a disabled `SystemBase` wrapper into a plain citizen ECS projection helper; entity-manager resolution, summary publication, entity projection, and visible-citizen callers stayed unchanged.
- [x] Fold `P7-0201 CitizenRefugeeSystem` from a disabled `SystemBase` wrapper into a plain citizen refugee helper; displacement, tent assignment, refugee upkeep, delegate types, and citizen composition callers stayed unchanged.

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
