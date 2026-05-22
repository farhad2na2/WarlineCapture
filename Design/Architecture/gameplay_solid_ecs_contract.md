# Gameplay SOLID/ECS Architecture Contract

This contract defines the intended architecture for WarlineCapture gameplay code. It is written as a drift guardrail: existing debt may be grandfathered temporarily, but new gameplay work must move toward this shape instead of expanding mixed-responsibility code.

## Core Rule

Gameplay runtime is ECS data plus ECS systems. Unity object code exists only at the edges: authoring, baking, UI views, bootstrap composition, config assets, and editor tooling.

Runtime gameplay code must not introduce singleton access patterns. `static Instance`, global registries, and static service locators are migration debt unless the type is a pure, stateless data/math helper.

Domain gameplay runtime types must be named for ECS, not application-layer patterns. New domain gameplay types should end in `Entity`, `Component`, or `System`. Canvas/reference UI types may end in `View`. ScriptableObject data may end in `Config`. Unity conversion-edge types may end in `Authoring` or `Baker`.

## Responsibilities

### Bootstrap

Bootstrap composes the application.

Allowed:
- Read serialized scene and config references.
- Register services.
- Install feature modules.
- Create or connect the ECS world.
- Start the application lifecycle.

Not allowed:
- Mission-specific behavior.
- Unit spawning policy.
- AI policy.
- Combat policy.
- Camera/framing policy.
- UI route rules.
- Asset-resolution policy.
- Static gameplay logging.

If a bootstrap change adds domain behavior, move it into a feature installer, ECS system, service, or config.

### ECS Components

Components hold data only. They should not own behavior beyond trivial value construction.

Expected names:
- `*Component` for `IComponentData`.
- `*Component` for `IBufferElementData`.
- `*Component` for zero-size marker/tag data.

Avoid:
- `*Element`.
- `*State`.
- `*Data` for gameplay runtime components.

### ECS Systems

Systems own gameplay behavior. Systems should depend on ECS data and should not reach into UI views, scene objects, `AssetDatabase`, or static service facades.

Expected names:
- `*System`.

### Authoring And Baking

Authoring MonoBehaviours and Bakers exist only to convert Unity-authored references/config into ECS data.

Expected names:
- `*Authoring`.
- `*Baker`.

### UI

UI MonoBehaviours named `*View` are serialized-reference binders only. They connect Canvas objects, child widgets, visual references, and serialized fields to code. They may expose simple getters/setters for visual state and wire UnityEvents to ECS request components, but they must not own gameplay policy, UI flow policy, validation, resource rules, production rules, selection rules, mission rules, AI rules, or state transitions.

Expected names:
- `*View` for UI reference holders and widgets.

Avoid:
- `*Presenter`.
- `*Controller`.
- `*Manager`.
- `*Port`.
- `*Adapter` for gameplay-facing UI code.

Existing bridge/controller names are legacy debt. Do not expand them when touching related behavior.

### Config

ScriptableObjects describe data. They do not execute gameplay behavior.

Expected names:
- `*Config`.
- `*ConfigAsset` is accepted for existing scene/prefab config assets.

### Services

Services bridge external concerns such as logging, persistence, asset lookup, telemetry, and platform APIs. They are shell-edge code, not gameplay domain code. Gameplay systems should prefer ECS event/data streams.

Expected names:
- `I*Service` for abstractions.
- `*Service` for implementations.

Static service facades are legacy debt unless they are pure constants/math.

Services must not own gameplay policy such as building placement, unit production, AI, combat, mission flow, or resource simulation. If behavior is gameplay, make it ECS data plus a `*System`.

### Static State And Singletons

Static runtime state is not an acceptable gameplay dependency boundary. New gameplay code must not add:
- `static Instance` properties or fields.
- Singleton fallback lookups such as `SomeSystem.Instance`.
- Static service locators or `ResolveDependency<T>()` helpers.
- Static mutable gameplay state shared across systems.

Allowed static code is limited to pure, stateless operations:
- Math helpers.
- Deterministic value conversion.
- Constant lookup tables that do not own runtime state.
- Test-local helpers.

If a class needs runtime collaborators, pass them through bootstrap composition or ECS data/events. If a class needs shared gameplay state, represent it as ECS singleton components, normal components, or buffers owned by systems.

`InitialUnitsRuntimeState` is legacy compatibility debt. New or touched gameplay code must not add direct reads/writes to its mutable flags. During migration, use `RuntimeGameplayStateSystem` as the compatibility boundary and mirror gameplay state through:
- `RuntimeGameplayStateComponent`
- `RuntimeCameraInputComponent`
- `RuntimeCameraFocusRequestComponent`

Once a flag is migrated behind `RuntimeGameplayStateSystem`, callers in that slice should use the boundary instead of touching `InitialUnitsRuntimeState` directly.

## Logging

Gameplay code must not add new calls to static logging facades such as `AILog.*` or direct `Debug.Log*`. New gameplay logging should use one of:
- ECS log event buffer processed by a shell logging system.
- An injected `ILogService` at the shell/service edge.
- A test-local logger implementation.

Existing `AILog` usage is grandfathered as migration debt and should be retired by domain slice.

## Refactor Direction

Use narrow migrations. Do not rewrite the entire project at once.

1. Introduce service interfaces and feature installers at the shell edge.
2. Move bootstrap domain behavior into feature installers and ECS startup systems.
3. Convert `static Instance` access and static runtime state into explicit injection or ECS singleton components.
4. Replace singleton fallback lookups with configured dependencies, ECS queries, or ECS request/response components.
5. Replace static logging with ECS log events plus a log flush service.
6. Convert mission-specific hardcoding into mission configs and systems.
7. Retire legacy class names only when touching that domain for real behavior work.

## Building Domain Migration

`BuildingPlacementSystem` is legacy facade debt. It must shrink by domain slice instead of gaining new behavior.

Allowed direction:
- `BuildingPlacementSystem` keeps active placement lifecycle and temporary facade methods during migration.
- Footprint, road, blocker, and wall-placement validity belong in `BuildingPlacementValidationSystem`.
- Runtime building registry ownership, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`.
- Building visual helper behavior, animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`; full building visual ownership should continue moving there by slice.
- Building destruction state, cleanup timing, blocker cleanup, and combat-health destruction checks belong in `BuildingCombatSystem`; full building combat ownership should continue moving there by slice.
- Resource storage classification, capacity display math, resource totals, faction economy snapshots, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`.
- Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`; movement/path request bridging may remain in `BuildingPlacementSystem` until the hauler loop is fully ECS-owned.
- Unit production queue item initialization, pending production timing/progress, readiness checks, produced-unit liveness pruning, production slot reservation, pending queue removal, ready/soon transport-pending lookup, and transport launch delay math belong in `BuildingProductionSystem`; spawn placement, transport visuals, and ECS unit instantiation may remain in `BuildingPlacementSystem` until those slices are extracted.
- Produced-unit UI lists, pending-production UI entries, UI progress shaping, and temporary building UI read models belong in `BuildingUiQuerySystem` until UI uses ECS query/request components.

When touching building code, do not add a new responsibility to `BuildingPlacementSystem`; extract or extend the matching `*System` slice.

## Decision Test

For every class, answer:

> What single reason should cause this class to change?

If the answer mentions more than one domain or layer, split the responsibility before adding more behavior.
