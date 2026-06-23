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

- Checklist progress: `94 / 120 complete (78.3%)`.
- In progress: `0`.
- Remaining open: `28`.
- Current target: `D7 Agent D inventory rows complete through P7-0132 BuildingSelectionSystem; hand off to Agent E road/city/citizen lane next`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Retired/folded helpers: `81`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `Eighty-one Agent D disabled building wrappers have been folded into plain helpers through P7-0132 BuildingSelectionSystem. Compile, git diff check, map-surface, placement command, building composition smoke, placement runtime tick, move-order, barrier, combat, target-order, runtime boundary, production request, production camera focus, production metadata, runtime building selection, building UI query, and Phase 7 architecture validations passed. Latest logs include /private/tmp/warline-phase7-agent-d-building-selection-helper-fold-runtime-building.log, /private/tmp/warline-phase7-agent-d-building-selection-helper-fold-smoke.log, and /private/tmp/warline-phase7-agent-a-architecture.log. Inventory now reports 145 production SystemBase/legacy declarations, 133 production ISystem declarations, and 47.8% production ISystem share.`

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

- [x] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [x] Filter rows assigned to `AgentD`.
- [x] Read `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`.
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

- [x] Identify disabled, unused, or wrapper building `SystemBase` types. Completed for `P7-0070`, `P7-0137`, `P7-0061`, `P7-0069`, `P7-0068`, `P7-0067`, `P7-0125`, `P7-0091`, `P7-0102`, `P7-0107`, `P7-0120`, `P7-0122`, `P7-0128`, `P7-0121`, `P7-0112`, `P7-0116`, `P7-0118`, `P7-0126`, `P7-0092`, `P7-0064`, `P7-0081`, `P7-0082`, `P7-0108`, `P7-0073`, `P7-0074`, `P7-0133`, `P7-0129`, `P7-0085`, `P7-0096`, `P7-0094`, `P7-0099`, `P7-0100`, `P7-0105`, `P7-0093`, `P7-0117`, `P7-0097`, `P7-0098`, `P7-0054`, `P7-0055`, `P7-0053`, `P7-0131`, `P7-0063`, `P7-0130`, `P7-0140`, `P7-0139`, `P7-0057`, `P7-0071`, `P7-0083`, `P7-0065`, `P7-0077`, `P7-0078`, `P7-0080`, `P7-0079`, `P7-0111`, `P7-0119`, `P7-0113`, `P7-0114`, `P7-0110`, `P7-0115`, `P7-0106`, `P7-0090`, `P7-0056`, and `P7-0135`.
- [x] Search for serialized, reflection, and code references before removal. Completed for `P7-0070`, `P7-0137`, `P7-0061`, `P7-0069`, `P7-0068`, `P7-0067`, `P7-0125`, `P7-0091`, `P7-0102`, `P7-0107`, `P7-0120`, `P7-0122`, `P7-0128`, `P7-0121`, `P7-0112`, `P7-0116`, `P7-0118`, `P7-0126`, `P7-0092`, `P7-0064`, `P7-0081`, `P7-0082`, `P7-0108`, `P7-0073`, `P7-0074`, `P7-0133`, `P7-0129`, `P7-0085`, `P7-0096`, `P7-0094`, `P7-0099`, `P7-0100`, `P7-0105`, `P7-0093`, `P7-0117`, `P7-0097`, `P7-0098`, `P7-0054`, `P7-0055`, `P7-0053`, `P7-0131`, `P7-0063`, `P7-0130`, `P7-0140`, `P7-0139`, `P7-0057`, `P7-0071`, `P7-0083`, `P7-0065`, `P7-0077`, `P7-0078`, `P7-0080`, `P7-0079`, `P7-0111`, `P7-0119`, `P7-0113`, `P7-0114`, `P7-0110`, `P7-0115`, `P7-0106`, `P7-0090`, `P7-0056`, and `P7-0135`.
- [x] Fold pure helper logic into static domain methods or the nearest narrow `ISystem` only when ownership is obvious. Completed by folding sixty-three wrappers into plain direct-owned helpers.
- [x] Fold `P7-0102 BuildingProductionTickCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain production tick context helper; behavior remained in existing `BuildingProductionRuntimeTickSystem.Context` wiring.
- [x] Fold `P7-0107 BuildingRuntimeBoundaryCompositionSystemHelper` from a disabled `SystemBase` wrapper into a plain runtime-boundary publish context helper; behavior remained in existing `BuildingRuntimeBoundaryPublishSystem.Context` wiring.
- [x] Fold `P7-0120 BuildingRuntimeResourcePrefabCompositionSystem` from a disabled `SystemBase` wrapper into a plain resource-prefab context helper; source ownership now uses direct construction instead of `World.GetOrCreateSystemManaged`.
- [x] Fold `P7-0122 BuildingRuntimeSideEffectCompositionSystem` from a disabled `SystemBase` wrapper into a plain deferred runtime side-effect helper; behavior remained in existing placement redirect and invalid-cell side-effect calls.
- [x] Fold `P7-0128 BuildingSelectionClickCompositionSystem` from a disabled `SystemBase` wrapper into a plain selection-click context helper; behavior remained in existing `BuildingSelectionClickSystem.Context` wiring.
- [x] Fold `P7-0121 BuildingRuntimeResourcePrefabContextSystem` from a disabled `SystemBase` wrapper into a plain runtime resource/prefab context helper; source ownership now uses direct construction instead of `World.GetOrCreateSystemManaged`.
- [x] Fold `P7-0112 BuildingRuntimeCompositionSystem` from a disabled `SystemBase` wrapper into a plain runtime composition context helper; behavior remained in existing runtime source/entity/context wiring.
- [x] Fold `P7-0116 BuildingRuntimeFocusPositionSystem` from a disabled `SystemBase` wrapper into a static runtime focus-position helper; avoided creating an empty replacement `ISystem` shell for static-only behavior.
- [x] Fold `P7-0118 BuildingRuntimeOwnershipSystem` from a disabled `SystemBase` wrapper into a plain runtime ownership helper; behavior remained in existing faction component, wall gate pass, and faction visual update paths.
- [x] Fold `P7-0126 BuildingRuntimeUpdateSystem` from a disabled `SystemBase` wrapper into a public plain runtime update dispatcher; startup and simulation callbacks stayed unchanged.
- [x] Fold `P7-0092 BuildingPlacementRuntimeTickDiagnosticsSystem` from a disabled `SystemBase` wrapper into a plain diagnostics helper; timing context, cooldown state, and gated log formatting stayed unchanged.
- [x] Fold `P7-0064 BuildingGameplayCompositionSourceSystem` from a disabled `SystemBase` wrapper into a plain explicit child-system graph owner; direct construction and default-world visual boundary lookups stayed unchanged.
- [x] Fold `P7-0081 BuildingPlacementInputTickCompositionSystem` from a disabled `SystemBase` wrapper into a plain placement input tick context composer; command flush, entity-manager access, and selection-click context wiring stayed unchanged.
- [x] Fold `P7-0082 BuildingPlacementInteractionCompositionSystem` from a disabled `SystemBase` wrapper into a plain placement interaction context composer; UI query, placement command, production request, selection, and breach-target wiring stayed unchanged.
- [x] Fold `P7-0108 BuildingRuntimeBoundaryPublishSystem` from a disabled `SystemBase` wrapper into a plain runtime boundary publish helper; entity query acquisition, boundary update dispatch, and frame/time arguments stayed unchanged.
- [x] Fold `P7-0073 BuildingPlacementAdapterSystem` from a disabled `SystemBase` wrapper into a plain placement adapter helper; grid, placement validity, wall gate alignment, and initial placement origin adapter behavior stayed unchanged.
- [x] Fold `P7-0074 BuildingPlacementCommandCompositionSystem` from a disabled `SystemBase` wrapper into a plain placement command-context helper; command context construction, placement visual delegate wiring, build purchase callbacks, minimap refresh, and selection clearing behavior stayed unchanged.
- [x] Fold `P7-0133 BuildingSpawnCellSystem` from a disabled `SystemBase` wrapper into a plain spawn-cell helper; perimeter candidate filtering, reservation, fallback spawn-cell search, and NativeArray/NativeBitArray behavior stayed unchanged.
- [x] Fold `P7-0129 BuildingSelectionClickSystem` from a disabled `SystemBase` wrapper into a plain selection-click helper; pending path-job gating, grid lookup, screen-to-cell lookup, and cell-selection delegate behavior stayed unchanged.
- [x] Fold `P7-0085 BuildingPlacementInvalidCellSystem` from a disabled `SystemBase` wrapper into a plain placement invalid-cell cache helper; cached prefix rebuild/clear, road-footprint mask use, placement validity, runtime blocker checks, and overlap validation behavior stayed unchanged.
- [x] Fold `P7-0096 BuildingPlacementValidationSystem` from a disabled `SystemBase` wrapper into a plain placement validation helper; footprint, invalid-prefix, wall-run, wall-conflict, runtime blocker, road, and runtime-building overlap validation behavior stayed unchanged.
- [x] Fold `P7-0094 BuildingPlacementSessionSystem` from a disabled `SystemBase` wrapper into a plain placement session helper; placement begin/confirm/rotate/cancel/exit, build-mode state, selection preservation, minimap notification, preview hide, and command-mode clearing behavior stayed unchanged.
- [x] Fold `P7-0099 BuildingProductionRuntimeTickSystem` from a disabled `SystemBase` wrapper into a plain production runtime tick helper; pending production, active transport, resource production, resource hauler, random-state, metrics, and spawn-reservation cleanup behavior stayed unchanged.
- [x] Fold `P7-0100 BuildingProductionSlotSystem` from a disabled `SystemBase` wrapper into a plain production slot helper; production slot reservation, occupied-slot cleanup, pending reservation checks, spawn local-position lookup, and `EntityManager` health/existence checks stayed unchanged.
- [x] Fold `P7-0105 BuildingProductionUpdateSystem` from a disabled `SystemBase` wrapper into a plain production update helper; pending-production iteration, transport launch checks, transport ticking, random-state mutation, spawn completion, and production timeline rebuild behavior stayed unchanged.
- [x] Fold `P7-0093 BuildingPlacementRuntimeTickSystem` from a disabled `SystemBase` wrapper into a plain placement runtime tick helper; startup tick ordering, simulation tick ordering, per-slice profiler markers, cadence throttles, diagnostics timing, and input tick dispatch stayed unchanged.
- [x] Fold `P7-0117 BuildingRuntimeObjectPresentationSystemHelper` from a disabled `SystemBase` wrapper into a plain Unity object lifecycle helper; play-mode `Destroy`, edit-mode `DestroyImmediate`, null guard, and all existing direct destruction delegate call sites stayed unchanged.
- [x] Fold `P7-0097 BuildingProductionCompositionSystem` from a disabled `SystemBase` wrapper into a plain production context composition helper; runtime source creation, production source wiring, placement fallback, resource delegates, production queue callbacks, hauler query delegates, and transport drop visual delegate wiring stayed unchanged.
- [x] Fold `P7-0098 BuildingProductionContextSystem` from a disabled `SystemBase` wrapper into a plain production context factory; production request/update/queue/transport/transport-bridge/resource-hauler context creation and player unit production queue handoff stayed unchanged.
- [x] Fold `P7-0054 BuildingProductionUnitMetadataSystem` from a disabled `SystemBase` wrapper into a static production metadata helper; unit production metadata extraction and transport drop visual preparation stayed unchanged.
- [x] Fold `P7-0055 BuildingSpawnPrefabLookupKeySystem` from a disabled `SystemBase` wrapper into a static spawn-prefab lookup helper; configured display-name key resolution, null handling, and prefab-name fallback stayed unchanged.
- [x] Fold `P7-0053 BuildingDefinitionAuthoringMetadataSystem` from a disabled `SystemBase` wrapper into a static authoring metadata helper; building/unit metadata extraction, authoring config application, and production prefab array behavior stayed unchanged.
- [x] Fold `P7-0131 BuildingSelectionPortraitSystem` from a disabled `SystemBase` wrapper into a static selection portrait helper; runtime building null handling, definition prefab lookup, and instance fallback stayed unchanged.
- [x] Fold `P7-0063 BuildingGameplayCompositionResultSystem` from a disabled `SystemBase` wrapper into a plain building gameplay composition result helper; result packaging, selection binding, citizen population initialization, binding, and disposal stayed unchanged.
- [x] Fold `P7-0130 BuildingSelectionCompositionSystem` from a disabled `SystemBase` wrapper into a plain building selection composition helper; selection context construction, marker refresh callback wiring, HUD selection binding, hauler-order handoff, and move-order callback wiring stayed unchanged.
- [x] Fold `P7-0140 BuildingUiContextSystem` from a disabled `SystemBase` wrapper into a plain building UI context helper; source packaging, command context creation, query context creation, production request callbacks, and entity-manager fallback behavior stayed unchanged.
- [x] Fold `P7-0139 BuildingUiCompositionSystem` from a disabled `SystemBase` wrapper into a plain building UI composition helper; source, command, query context creation, live-unit preview prefab lookup, and placement command fallback behavior stayed unchanged.
- [x] Fold `P7-0057 BuildingCitizenPopulationCompositionSystem` from a disabled `SystemBase` wrapper into a plain building citizen-population composition helper; citizen population boundary resolution, resource/prefab context creation, initialization, disposal, and dependency binding stayed unchanged.
- [x] Fold `P7-0071 BuildingGameplayStartupCompositionSystem` from a disabled `SystemBase` wrapper into a plain building startup composition helper; initial resource projection, startup dependency binding, road-footprint state configuration, and placement startup initialization stayed unchanged.
- [x] Fold `P7-0083 BuildingPlacementInteractionContextSystem` from a disabled `SystemBase` wrapper into a plain placement interaction context helper; source delegate packaging and `BuildingPlacementInteractionSystem.Context` creation stayed unchanged.
- [x] Fold `P7-0065 BuildingGameplayCompositionSystem` from a disabled `SystemBase` wrapper into a plain top-level building gameplay composition helper; `Initialize(...)` orchestration, direct child-system construction, context wiring, and composed result creation stayed unchanged.
- [x] Fold `P7-0077 BuildingPlacementContextSystem` from a disabled `SystemBase` wrapper into a plain placement context factory helper; placement command/session/commit/validation context creation and reusable wall-run scratch behavior stayed unchanged.
- [x] Fold `P7-0078 BuildingPlacementGridSystem` from a disabled `SystemBase` wrapper into a plain placement grid helper; footprint center math, screen-to-grid camera projection, rotated footprint resolution, and wall-run focus positioning stayed unchanged.
- [x] Fold `P7-0080 BuildingPlacementInputSystem` from a disabled `SystemBase` wrapper into a plain placement input helper; pointer drag state, wall-run scratch lists, hover updates, and wall-run commit behavior stayed unchanged.
- [x] Fold `P7-0079 BuildingPlacementInputRuntimeTickSystem` from a disabled `SystemBase` wrapper into a plain placement input runtime tick helper; queued placement command flushing, pointer gating, building selection release handling, and input timing results stayed unchanged.
- [x] Fold `P7-0111 BuildingRuntimeCompositionQuerySystem` from a disabled `SystemBase` wrapper into a plain runtime composition query helper; house detection, runtime building lookup, effective placement rects, overlap checks, and focus-position resolution stayed unchanged.
- [x] Fold `P7-0119 BuildingRuntimeQuerySystem` from a disabled `SystemBase` wrapper into a plain runtime building query helper; faction counts, produced-unit counts, role/id lists, combat info, destroyed/owner/refugee state reads, approach-cell queries, and base-breach target routing stayed unchanged.
- [x] Fold `P7-0113 BuildingRuntimeContextSystem` from a disabled `SystemBase` wrapper into a plain runtime context factory helper; runtime spawn, creation, ownership, city-spawn, entity, visual, selection-marker, redirect, combat, query, barrier, and resource-hauler context construction stayed unchanged.
- [x] Fold `P7-0114 BuildingRuntimeCreationSystem` from a disabled `SystemBase` wrapper into a plain runtime creation helper; runtime building registration, blocker/combat entity creation delegates, redirect callbacks, visual initialization callbacks, and placement side-effect hooks stayed unchanged.
- [x] Fold `P7-0110 BuildingRuntimeCitySpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime city spawn bridge helper; city building spawn request routing, fallback runtime spawn, delete callback, and deferred side-effect callbacks stayed unchanged.
- [x] Fold `P7-0115 BuildingRuntimeEntitySystem` from a disabled `SystemBase` wrapper into a plain runtime entity helper; blocker entity creation, combat entity creation, runtime building delete/destroy callbacks, and pathing-blocker policy stayed unchanged.
- [x] Fold `P7-0106 BuildingResourceHaulerBridgeSystem` from a disabled `SystemBase` wrapper into a plain resource hauler bridge helper; hauler query collection, selected-hauler assignment, runtime building approach-cell lookup, and haul phase updates stayed unchanged.
- [x] Fold `P7-0090 BuildingPlacementRedirectSystem` from a disabled `SystemBase` wrapper into a plain placement redirect helper; deferred side-effect depth, placed-footprint redirect queues, pending marker refresh, overlap/perimeter redirect goal search, and move-order request writes stayed unchanged.
- [x] Fold `P7-0056 BuildingBarrierSystem` from a disabled `SystemBase` wrapper into a plain barrier/gate helper; base-breach memory, wall-perimeter lookup, breach target resolution, road barrier door updates, gate alignment, and expanded selection checks stayed unchanged.
- [x] Fold `P7-0058 BuildingCombatSystem` from a disabled `SystemBase` wrapper into a plain building combat helper; destroyed-building state, cleanup-id collection, runtime combat-state resolution, blocker destruction, runtime building entity destruction sync, destroyed visual handoff, and marker/minimap callbacks stayed unchanged.
- [x] Fold `P7-0059 BuildingDefinitionSystem` from a disabled `SystemBase` wrapper into a plain building definition helper; configured spawnable lookup, configured unit lookup, definition metadata resolution, production source-key resolution, local-bounds caching, combined visual template cleanup, and runway metadata extraction stayed unchanged.
- [x] Fold `P7-0101 BuildingProductionSystem` from a disabled `SystemBase` wrapper into a plain building production helper; pending-production pooling, production queueing, transport setting resolution, source-key matching, production progress calculation, produced-unit pruning, and transport launch timing stayed unchanged.
- [x] Fold `P7-0103 BuildingProductionTransportBridgeSystem` from a disabled `SystemBase` wrapper into a plain production transport bridge helper; ground goal-cell resolution, faction helipad spawn resolution, produced-unit movement, produced-unit rotation alignment, spawn-near-building routing, newest produced-unit lookup, and camera focus callback policy stayed unchanged.
- [x] Fold `P7-0104 BuildingProductionTransportSystem` from a disabled `SystemBase` wrapper into a plain production transport visual helper; transport pooling, runtime root parenting, runway landing/taxi path setup, helicopter/self-arriving air delivery phases, blade rotation, door animation, produced-unit drop routing, and pool release stayed unchanged.
- [x] Fold `P7-0132 BuildingSelectionSystem` from a disabled `SystemBase` wrapper into a plain building selection helper; UI selection command queueing, clear/delete request processing, camera focus resolution, visible-building checks, screen-rect selection, grid-cell selection, visual screen-rect fallback selection, hauler-order handoff, marker refresh, focused-unit clearing, and HUD selection callbacks stayed unchanged.
- [x] Fold `P7-0135 BuildingSpawnSystem` from a disabled `SystemBase` wrapper into a plain spawn helper; produced-unit prefab resolution, placement selection, recent-spawn reservation, occupancy reservation, faction assignment, boundary read-model publication, and helipad spawn fallback stayed unchanged.
- [x] Fold `P7-0109 BuildingRuntimeBoundarySystem` from a disabled `SystemBase` wrapper into a plain runtime boundary helper; resource sell requests, UI production requests, production request draining, runtime spawn request processing, read-model publication, production summaries, resource summaries, surface overlay publishing, and configured read-model publication stayed unchanged.
- [x] Fold `P7-0142 MapBuildingPlacementSpawnSystem` from a disabled `SystemBase` wrapper into a plain map placement spawn helper; authored map placement queueing, hidden-authoring completion, runtime spawn routing, owner faction assignment, and authored visual wrapping stayed unchanged.
- [x] Fold `P7-0141 BuildingUiQuerySystem` from a disabled `SystemBase` wrapper into a plain building UI query helper; selected-building labels, descriptions, health, preview prefab lookup, produced-unit lists, pending-production UI entries, owner-faction filtering, and visible-selectable checks stayed unchanged.
- [x] Fold `P7-0123 BuildingRuntimeSpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime spawn helper; initial roster spawn, runtime building spawn, wall-run spawn, wall-segment spawn, footprint resolution, placement validation, visual instantiation callbacks, registration callbacks, and owner-faction assignment stayed unchanged.
- [x] Fold `P7-0089 BuildingPlacementQuerySystem` from a disabled `SystemBase` wrapper into a plain placement query helper; placement status text, placement duration, selected-building label/display/description, health lookup, preview prefab lookup, and production prefab list queries stayed unchanged.
- [x] Fold `P7-0066 BuildingGameplayDependencySystem` from a disabled `SystemBase` wrapper into a plain dependency helper; startup/runtime dependency binding, build command mode routing, runtime blocker/city/citizen callbacks, selection camera and HUD callbacks, minimap notification, and faction/day-night references stayed unchanged.
- [x] Fold `P7-0075 BuildingPlacementCommandSystem` from a disabled `SystemBase` wrapper into a plain placement command helper; ECS request/result queue helpers, begin/confirm/rotate/cancel/exit command processing, Soldier Base placement start, placement UI pointer notification, and active placement cost routing stayed unchanged.
- [x] Fold `P7-0076 BuildingPlacementCommitSystem` from a disabled `SystemBase` wrapper into a plain placement commit helper; wall-run expansion, wall segment footprint/rotation helpers, commit request/context data, visual creation/position/register delegates, preview consumption, and post-placement auto-select policy stayed unchanged.
- [x] Fold `P7-0084 BuildingPlacementInteractionSystem` from a disabled `SystemBase` wrapper into a plain placement interaction helper; selected-building state queries, placement confirm/cancel/exit routing, Soldier Base placement start, runtime entity destroyed handling, base-breach target resolution, and selected-building label/status helpers stayed unchanged.
- [x] Fold `P7-0086 BuildingPlacementLifecycleSystem` from a disabled `SystemBase` wrapper into a plain placement lifecycle helper; active placement state, begin/cancel/confirm/rotate flows, cost spending, preview ownership release, UI pointer notification, and placement failure reasons stayed unchanged.
- [x] Fold `P7-0087 BuildingPlacementPreviewSystem` from a disabled `SystemBase` wrapper into a plain placement preview helper; preview outline creation, material tinting, wall preview rebuilding, segment validity tinting, disposal, and runtime object destruction policy stayed unchanged.
- [x] Fold `P7-0095 BuildingPlacementStartupSystem` from a disabled `SystemBase` wrapper into a plain placement startup helper; config application, road-footprint state configuration, footprint-mask fill, preview/lifecycle initialization, and disposal stayed unchanged.
- [x] Do not delete a referenced scene/prefab script without a serialized-reference migration approved by Agent A. No script assets were deleted.
- [ ] Update tests that referenced retired helpers.
- [x] Record retired/folded count in the progress snapshot.

Acceptance:

- No missing-script references introduced.
- Removed helpers had either no behavior or a clear replacement.

## D8 - Focused Validation Matrix

Goal:
Prove building placement and production still work.

- [x] Always run `git diff --check -- <changed files>`.
- [x] Run architecture tests for no forbidden blockers in converted `ISystem` files.
- [x] Run placement validation tests for valid/invalid placement, rotation, blocked cells, and cancellation.
- [ ] Run building spawn validation for initial components, faction, health, and footprint occupancy.
- [ ] Run production validation for queue, progress, produced unit entity, cancellation, and rally/spawn point.
- [x] Run a smoke test that opens build gameplay and confirms buildable items are populated. Building gameplay composition smoke passed for this helper-fold slice.
- [ ] Run a smoke test that starts a match, places a building, and produces a unit when practical.
- [x] Check Unity logs for missing prefab entity, empty build menu, and runtime exceptions.
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

- [x] Create a dated handoff report under `Design/AgentReports/`.
- [x] Include inventory row ids, files changed, and final disposition.
- [x] Include split map: old system responsibilities to new systems/boundaries.
- [x] Include converted-to-`ISystem`, split passive/managed-boundary, retired/folded, and managed-exception counts.
- [x] Include validation commands and outcomes.
- [x] Include any Agent C/E/F coordination notes.
- [x] Include any rows returned to Agent A for reclassification.
- [x] Confirm this tracker progress snapshot is current.

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
