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

If a bootstrap change adds domain behavior, move it into an ECS startup system, service, or config.

The current bootstrap migration map is `Design/Architecture/gamebootstrap_responsibility_audit.md`. `GameBootstrap` is legacy composition debt and should shrink by the audited slices; do not add new AI, mission, camera, spawning, routing, asset-resolution, or diagnostics policy to it.
AI startup config projection is owned by `AIStartupSystem`; `GameBootstrap` may pass serialized `AIControllerConfig` references into that system, but it must not create or mutate `FactionEconomy`, `FactionControlEntry`, `AIBuildPlan`, `AIProductionPlan`, `AISquadPlan`, `AITargetPrioritySetting`, or AI diagnostic events directly, and it must not own mission-specific fixed tactical policy.
Faction economy startup projection is owned by `FactionEconomyStartupSystem`; `AIStartupSystem` may request economy startup projection from that system, but it must not create or mutate `FactionEconomy` or `FactionEconomyPolicy` directly.
AI faction-control startup projection is owned by `AIFactionControlStartupSystem`; `AIStartupSystem` may request faction-control startup projection from that system and convert its result to `AIStartupSystem.Result`, but it must not create or mutate `FactionControlConfigTag` or `FactionControlEntry` directly.
AI default build and production fallback ids are owned by authored `AIPlanEntryStartupConfig` assets; `AIStartupSystem` may pass that config to `AIPlanEntryStartupSystem`, and `AIPlanEntryStartupSystem` may write ECS buffers from preferred ids plus config fallbacks, but neither system may hardcode fallback building or unit ids directly.
Mission startup is owned by `MissionStartupSystem`; M01 camera/framing policy is owned by `MissionCameraSystem`. `GameBootstrap` may pass serialized mission binders, cameras, and legacy visual roots into mission startup, but it must not calculate mission camera framing, hide mission-specific visual roots, run fixed tactical mission guardrails directly, or disable generic AI plans for a fixed tactical mission.
Configured faction spawn-cell resolution is owned by `InitialFactionSpawnCellSystem`; `GameBootstrap` may configure that system with the ECS world and authored fallback initial-unit config, but it must not query `InitialUnitsSpawnConfig` buffers or fallback faction lists directly.
Broad scene lookup and UI runtime binding are owned by `GameplaySceneBindingSystem`; `GameBootstrap` may call that boundary during startup, but it must not call `Resources.FindObjectsOfTypeAll`, discover loaded scene UI collaborators directly, or own loaded-scene filtering helpers.
Performance diagnostics are owned by `PerformanceDiagnosticsSystem`; `GameBootstrap` may bracket lifecycle calls through that system, but it must not format or emit `FreezeDetect`, `FrameRateDiag`, or `PerfDiag` diagnostics directly and must not own profiler recorder state.
Managed gameplay runtime update orchestration is owned by `GameplayRuntimeUpdateSystem`. `GameBootstrap` may call that system from Unity lifecycle methods and pass current composed references into it, but it must not own the long managed `Update`, `LateUpdate`, or `OnGUI` step sequence directly.

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
Managed callers use `RuntimeGameplayStateSystem`; ECS `ISystem` callers should read the runtime ECS singleton components directly so hot gameplay loops do not allocate managed bridge objects or perform compatibility sync work per frame.

Unity object references must not be added to unmanaged gameplay state components. Runtime camera references must flow through `RuntimeCameraReferenceComponent` and `RuntimeCameraReferenceSystem`, with gameplay systems reading the managed ECS reference instead of `InitialUnitsRuntimeState.WorldCamera`.

## Logging

Gameplay code must not add new calls to static logging facades or direct `Debug.Log*`. New gameplay logging should use one of:
- ECS log event buffer processed by a shell logging system.
- An injected `ILogService` at the shell/service edge.
- A test-local logger implementation.

The retired `AILog` facade must not be reintroduced.
AI log enablement and transport boarding diagnostics must flow through `RuntimeDiagnosticsStateComponent` and `RuntimeDiagnosticsSystem`. `InitialUnitsRuntimeState.VerboseAILogs`, `InitialUnitsRuntimeState.ShouldLogAI`, and `InitialUnitsRuntimeState.TransportBoardingDiagnostics` are legacy compatibility state and must not be read directly by production systems outside the diagnostics boundary.
AI domain logs must use ECS diagnostic event buffers such as `AIDiagnosticLogComponent`, flushed by a shell-edge logging system such as `AIDiagnosticLogFlushSystem`. Hot systems must gate diagnostic message construction before formatting strings.
Transport boarding diagnostics must use ECS diagnostic event buffers such as `TransportBoardingDiagnosticLogComponent`, flushed by `TransportBoardingDiagnosticLogFlushSystem`. Boarding command and boarding execution call sites must gate diagnostic message construction before formatting entity descriptions or pathing details.

## Refactor Direction

Use narrow migrations. Do not rewrite the entire project at once.

1. Introduce service interfaces and ECS-aligned startup systems at the shell edge.
2. Move bootstrap domain behavior into ECS startup systems, services, or configs.
3. Convert `static Instance` access and static runtime state into explicit injection or ECS singleton components.
4. Replace singleton fallback lookups with configured dependencies, ECS queries, or ECS request/response components.
5. Replace static logging with ECS log events plus a log flush service.
6. Convert mission-specific hardcoding into mission configs and systems.
7. Retire legacy class names only when touching that domain for real behavior work.

## Building Domain Migration

`BuildingPlacementSystem` is legacy facade debt. It must shrink by domain slice instead of gaining new behavior.

Allowed direction:
- `BuildingPlacementSystem` keeps only temporary facade methods during migration; active placement session state, begin/cancel/confirm flow, active placement cost, active placement preview handoff, and active placement facade queries belong in `BuildingPlacementLifecycleSystem`.
- Footprint, road, blocker, wall-placement validity, wall run/origin validation, and wall overlap-cell checks belong in `BuildingPlacementValidationSystem`.
- Runtime building registry ownership, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`.
- Runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy belong in `BuildingRuntimeCreationSystem`.
- Runtime building blocker entity creation, runtime building combat entity creation, path-blocking policy for runtime buildings, and runtime building combat component setup belong in `BuildingRuntimeEntitySystem`.
- Runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing belong in `BuildingRuntimeQuerySystem`.
- Runtime/manual building spawn orchestration, initial test roster spawn requests, runtime wall-run/segment spawn orchestration, runtime placement footprint queries, runtime wall footprint queries, initial building origin search, and building-definition footprint cloning belong in `BuildingRuntimeSpawnSystem`.
- Runtime building owner-faction assignment, combat `Faction` component projection, owner marker color projection, and gate friendly-pass blocker updates belong in `BuildingRuntimeOwnershipSystem`.
- Placement redirect side-effect deferral, deferred redirect footprints, pending marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and redirect movement component mutation belong in `BuildingPlacementRedirectSystem`.
- Building definition/configured spawnable lookup, spawnable/unit prefab lookup aliases, runtime building prefab metadata cache, prefab bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, and runtime/configured building definition construction belong in `BuildingDefinitionSystem`.
- Building selection clearing, select-and-focus behavior, selected-building focus position resolution, and runtime building click hit-test/routing belong in `BuildingSelectionSystem`.
- Building visual helper behavior, animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`; runtime building visual initialization, runtime resource animation updates, and runtime marker visibility projection belong in `BuildingRuntimeVisualSystem`.
- Placement visual instance creation, placement visual positioning, prefab model bounds, and transformed bounds helpers belong in `BuildingPlacementVisualSystem`.
- Building deletion orchestration, destruction state, cleanup timing, blocker cleanup, combat-health destruction checks, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in `BuildingCombatSystem`; full building combat ownership should continue moving there by slice.
- Resource storage classification, capacity display math, resource totals, faction economy snapshots, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`.
- Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`; resource-hauler update orchestration, selected-hauler assignment bridging, hauler move-order/path request bridging, building approach checks, and building approach-cell search belong in `BuildingResourceHaulerBridgeSystem`.
- Unit production queue item initialization, player unit production queue mutation, pending production timing/progress, readiness checks, produced-unit liveness pruning, pending queue removal, ready/soon transport-pending lookup, production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`; production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation belong in `BuildingProductionSlotSystem`; active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`; production transport ground-cell conversion, produced-unit movement orders, produced-unit rotation alignment, and transport-spawn bridging belong in `BuildingProductionTransportBridgeSystem`; produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization belong in `BuildingSpawnSystem`; spawn-cell perimeter search helpers belong in `BuildingSpawnCellSystem`; spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup belong in `BuildingSpawnPrefabSystem`.
- Selected-building unit production request routing, camp item request failure policy, UI production arm consumption, friendly producer lookup, production request focus, and last camp production focus memory belong in `BuildingProductionRequestSystem`.
- Runway prefab metadata discovery, runway footprint expansion for placement validity, and nearest airport runway lookup belong in `BuildingRunwaySystem`.
- Placement outline object lifetime, outline material/color updates, wall preview segment rebuilds, and preview segment validity tinting belong in `BuildingPlacementPreviewSystem`.
- Placement commit expansion, wall-run origin construction, wall segment footprint/rotation helpers, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy belong in `BuildingPlacementCommitSystem`.
- Active placement pointer event orchestration, drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing belong in `BuildingPlacementInputSystem`.
- Placement/grid math, footprint center projection, center-screen placement origin resolution, screen-to-grid raycasts, placement footprint rotation, and placement focus bounds belong in `BuildingPlacementGridSystem`.
- Placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, and selected-building production prefab read models belong in `BuildingPlacementQuerySystem`.
- Road barrier gate classification, gate-to-nearby-wall alignment, base-breach memory, enemy wall/gate perimeter lookup, breach-target resolution, breach-building target selection, breach approach-cell search, barrier door proximity checks, and barrier door visual open-state updates belong in `BuildingBarrierSystem`.
- Produced-unit UI lists, pending-production UI entries, UI progress shaping, and temporary building UI list read models belong in `BuildingUiQuerySystem` until UI uses ECS query/request components.

When touching building code, do not add a new responsibility to `BuildingPlacementSystem`; extract or extend the matching `*System` slice.

## Decision Test

For every class, answer:

> What single reason should cause this class to change?

If the answer mentions more than one domain or layer, split the responsibility before adding more behavior.
