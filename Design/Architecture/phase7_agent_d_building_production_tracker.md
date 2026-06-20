# Phase 7 Agent D Tracker - Building, Placement, Production, And Spawn

Purpose:
Retire building-domain non-UI `SystemBase` ownership by splitting broad building gameplay owners into focused ECS request, validation, placement, production, spawn, state, and result processors. This lane owns the paused five-SystemBase sub-track integration.

Branch:
`codex/phase7-agent-d-building-production`

Progress snapshot:

- Checklist progress: `0 / 61 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `61`.
- Current target: `D0 - building-domain inventory intake`.
- Direct conversions completed: `0`.
- Split ECS processors created: `0`.
- Retired/folded helpers: `0`.
- Managed presentation `SystemBase` exceptions created: `0`.
- Validation status: `not started`.

Ownership:

- Owns building placement, building runtime, production, spawn, combat, resource, selection-click, building UI query, and building composition systems assigned by Agent A.
- Owns `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md` as a sub-track only if explicitly resumed by the integration agent.
- Coordinates with Agent C for selection/building interaction contracts.
- Coordinates with Agent F for building visual, marker, destroyed visual, and foundation presentation.

Do not touch:

- Road build systems except road/building contract surfaces agreed with Agent E.
- Runtime-city systems except building spawn request/result contracts agreed with Agent E.
- UI Toolkit/Canvas implementation.
- Main Phase 7 tracker except through handoff reports.
- Any new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, or manager-style MonoBehaviour ticker. MonoBehaviours are view/reference holders only.

Candidate examples to verify, not pre-approved:

- `BuildingPlacement*`
- `BuildingRuntime*`
- `BuildingGameplay*`
- `BuildingProduction*`
- `BuildingSpawnCellSystem`
- `BuildingUiQuerySystem`
- `BuildingSelection*`
- `BuildingCombatSystem`
- `MapBuildingPlacementSpawnSystem`
- `BuildingDestroyedVisualSystem` only with Agent F.

## D0 - Building Inventory Intake

- [ ] Wait for Agent A inventory and guardrails.
- [ ] Pull all building-domain rows assigned to Agent D.
- [ ] Cross-reference `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`.
- [ ] Identify systems that are broad owners and must be split, not directly converted.
- [ ] Identify systems that only compose or expose helper APIs and can be retired/folded.
- [ ] Identify managed blockers: `GameObject`, prefab, `RuntimeBuildingEntity`, `Transform`, visual preview, scene references, public helper APIs.
- [ ] Write an initial Agent D handoff with target grouping and dependencies.

Acceptance:

- Every building target has an owner, disposition, and first safe slice.
- No broad building owner is planned as one large `ISystem`.

## D1 - Building Request/Result Contracts

- [ ] Inventory existing building placement, spawn, production, UI query, and transport request data.
- [ ] Replace public managed helper calls with ECS request/result components or buffers one call site at a time.
- [ ] Define clear ownership for placement validation result, placement preview result, spawn request, production queue mutation, produced-unit result, and building status read model.
- [ ] Preserve same-frame requirements for UI/build placement feedback.
- [ ] Add contract validation before implementation moves deeper.

Acceptance:

- Managed boundaries write requests or read results only.
- Gameplay decisions move toward ECS processors.

## D2 - Placement Split

- [ ] Split placement input/pointer/preview visual work from ECS placement validation.
- [ ] Convert pure placement grid/session/validation state to focused `ISystem` processors.
- [ ] Keep preview meshes, placement ghost visuals, and pointer/camera work in counted managed presentation/camera `SystemBase` exceptions when they must tick.
- [ ] Do not move placement preview ticking into MonoBehaviour `Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [ ] Replace managed placement helper state with components/buffers.
- [ ] Convert placement commit to `ISystem` only after spawn requests are ECS-owned.
- [ ] Run building placement command validation after each slice.

Acceptance:

- Placement validation and commit are ECS-owned.
- Visual preview does not own gameplay policy.

## D3 - Runtime Building State

- [ ] Inventory `RuntimeBuildingEntity` and managed runtime-building mirrors.
- [ ] Replace runtime-building transform/faction/resource lookup with ECS component data where practical.
- [ ] Convert ownership/resource/focus-position/runtime-boundary publication to `ISystem` after data is pure ECS.
- [ ] Fold context/composition systems that only forward data.
- [ ] Preserve public read APIs through explicit ECS read-models or plain helpers.
- [ ] Run building UI query and runtime boundary validation.

Acceptance:

- Recurring building state publication does not require managed `SystemBase`.

## D4 - Production And Spawn

- [ ] Inventory production queue, slot reservation, produced-unit tracking, transport bridge, and spawn systems.
- [ ] Confirm prefab and produced-unit references are ECS entity/source-key data before conversion.
- [ ] Convert production tick/request/queue processors to focused `ISystem` systems.
- [ ] Convert spawn request processors after `GameObject` prefab fallback is removed or split.
- [ ] Convert transport production bridge only after footprint/spawn decisions use entity/source-key data.
- [ ] Coordinate with the five-SystemBase tracker for `BuildingSpawnSystem`, `BuildingProductionTransportBridgeSystem`, `CitizenVisibleUnitSystem`, `MapVehiclePlacementSpawnSystem`, and `CustomGameStartupSystem`.
- [ ] Run production, build drawer, transport, and placement-to-production PlayMode validation.

Acceptance:

- Unit/building production still spawns correct units.
- No production target depends on managed prefab reverse lookup.

## D5 - Building Selection And Combat

- [ ] Coordinate with Agent C before changing building selection click/interaction systems.
- [ ] Convert building selection ECS state to `ISystem` where data-only.
- [ ] Convert building combat ECS damage/target state to `ISystem` where data-only.
- [ ] Keep hit visuals, markers, destroyed visuals, and presentation in Agent F or counted managed presentation `SystemBase` exceptions.
- [ ] Run building selection, combat/death, and marker validations.

Acceptance:

- Building selection and combat policy is ECS-owned.
- Presentation remains passive.

## D6 - Retire/Fold Composition Helpers

- [ ] Inventory `Building*CompositionSystem`, `Building*ContextSystem`, `Building*BindingSystem`, and `Building*AdapterSystem`.
- [ ] Fold helpers with no independent update lifetime into callers or plain value helpers.
- [ ] Replace broad adapter/facade shells with explicit request/result data.
- [ ] Delete empty shells after call sites are removed.
- [ ] Preserve `.meta` files.
- [ ] Run compile and building-domain validation.

Acceptance:

- No broad building facade remains as managed ECS debt.

## D7 - Agent D Completion

- [ ] Run `git diff --check`.
- [ ] Run building placement validation.
- [ ] Run production and build drawer validation.
- [ ] Run building UI query validation.
- [ ] Run building selection marker/faction visual validation with Agent F if touched.
- [ ] Run combat/death validation.
- [ ] Run placement-to-production PlayMode smoke.
- [ ] Run architecture guardrails.
- [ ] Write `Design/AgentReports/YYYY-MM-DD_phase7_agent_d_building_production_handoff.md`.

Handoff format:

- Checklist progress.
- Systems converted to `ISystem`.
- Systems split and new processors.
- Retired/folded helpers.
- Managed presentation `SystemBase` exceptions created.
- Five-SystemBase sub-track status.
- Validation commands and logs.
- Cross-agent blockers.
