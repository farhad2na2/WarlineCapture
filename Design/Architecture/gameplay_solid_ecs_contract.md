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
Managed gameplay runtime update orchestration is owned by `GameplayRuntimeUpdateSystem`. `GameBootstrap` may call that system from Unity lifecycle methods and pass current composed references into it, but it must not own the long managed `Update`, `LateUpdate`, or `OnGUI` step sequence directly. Building runtime updates inside that loop must go through `BuildingRuntimeUpdateSystem`, not a direct `BuildingPlacementSystem` facade parameter. `BuildingRuntimeUpdateSystem` ownership and context construction belong in managed composition, not inside `BuildingPlacementSystem`, and it must invoke a narrow building runtime tick callback rather than `BuildingPlacementSystem.Update`. `GameBootstrap` must not hold a public or private `BuildingPlacementSystem` facade; it may only store the narrow systems, contexts, and disposal callback produced by managed composition.
Managed building gameplay composition is owned by `BuildingGameplayCompositionSystem`. Temporary `BuildingPlacementSystem` facade ownership is isolated inside `BuildingGameplayCompositionSystem` until the remaining facade update body is split. `ManagedGameplayStartupSystem` may consume that composition result, but it must not hold or reach through `BuildingPlacementSystem` to retrieve child systems, contexts, runtime update delegates, interaction boundaries, citizen resource contexts, prefab contexts, or disposal.

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

The deletion plan and current facade surface inventory are frozen in `Design/Architecture/buildingplacement_retirement_audit.md`. Until the facade is deleted, no new production code may construct or reference `BuildingPlacementSystem` outside the temporary managed composition debt file named in that audit. The facade file line count and public/internal member declaration count must only decrease; editor test construction is allowed only for the existing legacy validation harnesses while those tests migrate to narrower systems.

Allowed direction:
- `BuildingPlacementSystem` keeps only temporary facade methods during migration; active placement mutable state, active placement cost, and active placement preview handoff belong in `BuildingPlacementLifecycleSystem`; active placement begin/cancel/confirm/exit command flow and selection-preservation state belong in `BuildingPlacementSessionSystem`.
- Placement grid/input/preview/commit context construction belongs in `BuildingPlacementContextSystem`; `BuildingPlacementSystem` may temporarily expose compatibility methods but must not construct placement lifecycle, input, validation, or commit context structs directly.
- Footprint, road, blocker, wall-placement validity, wall run/origin validation, and wall overlap-cell checks belong in `BuildingPlacementValidationSystem`.
- Runtime building registry ownership, count/dictionary read access, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`; managed composition may receive the registry boundary, but `BuildingPlacementSystem` must not expose separate runtime building count or dictionary facade properties.
- Runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy belong in `BuildingRuntimeCreationSystem`.
- Runtime building blocker entity creation, runtime building combat entity creation, path-blocking policy for runtime buildings, and runtime building combat component setup belong in `BuildingRuntimeEntitySystem`.
- Runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing belong in `BuildingRuntimeQuerySystem`.
- Citizen population building read paths must use `BuildingRuntimeQuerySystem` for building lists, positions, destroyed state, refugee settings, and approach cells. The backing dollar pool belongs in `RuntimeResourceSystem`; Citizen upkeep spending belongs in `CitizenResourceSystem`; unit prefab registry composition belongs in `RuntimeUnitPrefabSystem`; citizen configured prefab/entity resolution belongs in `CitizenPrefabSystem`. `CitizenPopulationSystem` must receive these narrow systems/contexts directly from managed composition and must not accept `BuildingPlacementSystem`. `BuildingPlacementSystem` must not own citizen resource or prefab context factories.
- Runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction belongs in `BuildingRuntimeResourcePrefabContextSystem`; `BuildingPlacementSystem` may expose only a temporary source bundle while the facade is retired and must not construct those context structs directly.
- Runtime/manual building spawn orchestration, initial test roster spawn requests, runtime wall-run/segment spawn orchestration, runtime placement footprint queries, runtime wall footprint queries, initial building origin search, and building-definition footprint cloning belong in `BuildingRuntimeSpawnSystem`; runtime/manual spawn command translation belongs in `BuildingRuntimeSpawnCommandSystem`.
- Runtime city generated building spawn/delete/deferred-side-effect bridging belongs in `BuildingRuntimeCitySpawnSystem`; `RuntimeCitySpawnerSystem` must not call the `BuildingPlacementSystem` facade directly.
- `GameplayFeatureStartupSystem` must receive `BuildingRuntimeCitySpawnSystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition; it must not receive or call `BuildingPlacementSystem`.
- Building runtime tick orchestration and per-phase timing belong in `BuildingPlacementRuntimeTickSystem`; placement pointer/click frame flow belongs in `BuildingPlacementInputRuntimeTickSystem`; building runtime tick diagnostics threshold, enablement, timing normalization, and log formatting belong in `BuildingPlacementRuntimeTickDiagnosticsSystem`; runtime tick context assembly belongs in `BuildingPlacementRuntimeTickContextSystem`, not `BuildingPlacementSystem`; production progress ticking, resource production ticking, resource hauler ticking, and recent spawn reservation cleanup belong in `BuildingProductionRuntimeTickSystem`; runtime boundary publish ticking belongs in `BuildingRuntimeBoundaryPublishSystem`; `BuildingRuntimeUpdateSystem` must not receive or invoke a `BuildingPlacementSystem.Update` delegate.
- Runtime building owner-faction assignment, combat `Faction` component projection, owner marker color projection, and gate friendly-pass blocker updates belong in `BuildingRuntimeOwnershipSystem`.
- Runtime spawn, runtime creation, runtime ownership, and runtime city-spawn context construction belongs in `BuildingRuntimeContextSystem`; `BuildingPlacementSystem` may expose only a temporary source bundle while the facade is retired and must not construct those context structs directly.
- Placement redirect side-effect deferral, deferred redirect footprints, pending marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and redirect movement component mutation belong in `BuildingPlacementRedirectSystem`.
- Building definition/configured spawnable/unit lookup, configured spawnable/unit prefab list/read access, spawnable/unit prefab lookup aliases, runtime building prefab metadata cache, prefab bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, and runtime/configured building definition construction belong in `BuildingDefinitionSystem`.
- Building selection screen-click guards and screen-to-grid click routing belong in `BuildingSelectionClickSystem`; building selection clearing, select-and-focus behavior, selected-building focus position resolution, and runtime building cell hit-test/routing belong in `BuildingSelectionSystem`.
- Building visual helper behavior, animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`; runtime building visual initialization, runtime resource animation updates, and runtime marker visibility projection belong in `BuildingRuntimeVisualSystem`.
- Placement visual instance creation, placement visual positioning, prefab model bounds, and transformed bounds helpers belong in `BuildingPlacementVisualSystem`.
- Building deletion orchestration, destruction state, cleanup timing, blocker cleanup, combat-health destruction checks, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in `BuildingCombatSystem`; full building combat ownership should continue moving there by slice.
- Resource storage classification, capacity display math, resource totals, faction economy snapshot contracts, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`.
- Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`; resource-hauler update orchestration, selected-hauler assignment bridging, hauler move-order/path request bridging, building approach checks, and building approach-cell search belong in `BuildingResourceHaulerBridgeSystem`.
- Unit production queue item initialization, player unit production queue mutation, pending production timing/progress, readiness checks, produced-unit liveness pruning, pending queue removal, ready/soon transport-pending lookup, production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`; production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation belong in `BuildingProductionSlotSystem`; active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`; production transport ground-cell conversion, produced-unit movement orders, produced-unit rotation alignment, and transport-spawn bridging belong in `BuildingProductionTransportBridgeSystem`; produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization belong in `BuildingSpawnSystem`; spawn-cell perimeter search helpers belong in `BuildingSpawnCellSystem`; spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup belong in `BuildingSpawnPrefabSystem`.
- Production request, production queue, production update, production transport, production transport bridge, and resource hauler bridge context construction belongs in `BuildingProductionContextSystem`; `BuildingPlacementSystem` may expose only a temporary production source bundle while the facade is retired and must not construct those context structs directly.
- Menu/camp UI money reads, camp catalog reads, camp request validation/commands, selected-building modal confirm/destroy commands, and placement/session UI commands belong behind `BuildingUiCommandSystem`; configured spawnable/unit UI read models and camp request failure codes must be owned by that boundary, not nested inside `BuildingPlacementSystem`; UI command/query context construction belongs in `BuildingUiContextSystem`, not `BuildingPlacementSystem`; `MenuView` must not hold a `BuildingPlacementSystem` facade instance or reference its nested UI/data contracts.
- Friendly pending-production UI read models, produced-unit UI read models, selected-building display/health/preview reads, building minimap flags, visible-building selection checks, live-unit preview prefab resolution, and UI progress shaping belong in `BuildingUiQuerySystem`; these read-model contracts must not be nested inside `BuildingPlacementSystem`, and `BuildingUiCommandSystem` must not own read-model query delegates, query context construction, or pending-production UI list retrieval.
- `MenuStartupSystem` must receive `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition; it must not receive or call `BuildingPlacementSystem`.
- `BuildingPlacementSystem` must not expose public building UI read/query or menu/camp command compatibility wrappers once `MenuView` is bound to `BuildingUiQuerySystem` and `BuildingUiCommandSystem`; keep only live external composition hooks until those callers migrate.
- RoadBuildSystem and RTSSelectionSystem building-placement peer interactions belong behind `BuildingPlacementInteractionSystem`; these peer systems must not hold or call a `BuildingPlacementSystem` facade instance directly.
- Runtime building entity-link callbacks must route through `BuildingPlacementInteractionSystem` instead of storing a `BuildingPlacementSystem` facade owner; `MainMenuPlayUI` must not accept a `BuildingPlacementSystem` dependency when it does not use one.
- Selected-building unit production request routing, faction unit-production result contracts, faction unit-production request orchestration, camp item request failure policy, UI production arm consumption, friendly/faction producer lookup, production request focus, and last camp production focus memory belong in `BuildingProductionRequestSystem`.
- Runway prefab metadata discovery, runway footprint expansion for placement validity, and nearest airport runway lookup belong in `BuildingRunwaySystem`.
- Placement outline object lifetime, outline material/color updates, wall preview segment rebuilds, and preview segment validity tinting belong in `BuildingPlacementPreviewSystem`.
- Placement commit expansion, wall-run origin construction, wall segment footprint/rotation helpers, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy belong in `BuildingPlacementCommitSystem`.
- Active placement pointer event orchestration, drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing belong in `BuildingPlacementInputSystem`.
- Placement/grid math, footprint center projection, center-screen placement origin resolution, screen-to-grid raycasts, placement footprint rotation, and placement focus bounds belong in `BuildingPlacementGridSystem`.
- Placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, and selected-building production prefab read models belong in `BuildingPlacementQuerySystem`.
- Road barrier gate classification, gate-to-nearby-wall alignment, base-breach memory, enemy wall/gate perimeter lookup, breach-target resolution, breach-building target selection, breach approach-cell search, barrier door proximity checks, and barrier door visual open-state updates belong in `BuildingBarrierSystem`.
- Produced-unit UI lists, pending-production UI entries, selected-building UI read models, minimap building read models, live-unit preview read models, UI progress shaping, and temporary building UI list read models belong in `BuildingUiQuerySystem` until UI uses ECS query/request components.
- AI/building cross-domain integration must move through `BuildingRuntimeBoundaryTag` ECS buffers. Configured building/unit read models, faction building/resource summaries, produced/queued unit summaries, faction unit-production requests, faction resource-sell requests, and runtime building spawn requests belong in `BuildingRuntimeEcsBoundaryComponents`; `GameBootstrap` only installs the boundary entity/buffers, and temporary boundary publish/consume orchestration belongs in `BuildingRuntimeBoundarySystem` until the facade is retired.
- `GameBootstrap` must not publish a managed `BuildingPlacementSystem` facade through ECS component objects; the runtime building ECS boundary is `BuildingRuntimeBoundaryTag` plus explicit buffers only.
- `BuildingPlacementSystem` must not expose faction production, faction resource economy/sell, or faction count compatibility wrappers; callers should use `BuildingProductionRequestSystem`, `FactionResourceSystem`, `BuildingRuntimeQuerySystem`, or the `BuildingRuntimeBoundaryTag` ECS buffers directly.

When touching building code, do not add a new responsibility to `BuildingPlacementSystem`; extract or extend the matching `*System` slice.

## Decision Test

For every class, answer:

> What single reason should cause this class to change?

If the answer mentions more than one domain or layer, split the responsibility before adding more behavior.
