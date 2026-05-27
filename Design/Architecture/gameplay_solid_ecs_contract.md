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
Managed gameplay runtime update orchestration is owned by `GameplayRuntimeUpdateSystem`. `GameBootstrap` may call that system from Unity lifecycle methods and pass current composed references into it, but it must not own the long managed `Update`, `LateUpdate`, or `OnGUI` step sequence directly. Building runtime updates inside that loop must go through `BuildingRuntimeUpdateSystem`. `BuildingRuntimeUpdateSystem` ownership and context construction belong in managed composition, and it must invoke a narrow building runtime tick callback. `GameBootstrap` must not hold a public or private `BuildingPlacementSystem` facade; it may only store the narrow systems, contexts, and disposal callback produced by managed composition.
Managed building gameplay composition is owned by `BuildingGameplayCompositionSystem`. `BuildingGameplayCompositionSystem` constructs narrow building systems directly and must not construct `BuildingGameplaySystem`; the retired `BuildingPlacementSystem` facade must not exist. Building placement startup/config wiring must be routed directly from composition into `BuildingPlacementStartupSystem` and `BuildingGameplayDependencySystem`, not through `BuildingGameplaySystem.Init`, building disposal ownership must route through `BuildingGameplayDisposalSystem`, not through `BuildingGameplaySystem.Dispose`, building ECS query caching must live in `BuildingGameplayEcsQuerySystem`, not in `BuildingGameplaySystem`, building grid data access must route through `BuildingGameplayGridDataSystem`, not direct grid query/buffer reads in `BuildingGameplaySystem`, building placement invalid-cell cache ownership must live in `BuildingPlacementInvalidCellSystem`, not in `BuildingGameplaySystem`, building spawn random state must live in `BuildingSpawnSystem`, not in `BuildingGameplaySystem`, building build-button placement commands must live in `BuildingPlacementCommandSystem`, not in `BuildingGameplaySystem`, building placement confirm, cancel, exit, pointer-down, and active-placement cost commands must route through `BuildingPlacementCommandSystem`, not direct session calls in `BuildingGameplaySystem`, building placement focus, visual update, confirm validation, and placement object handoff must live in `BuildingPlacementVisualUpdateSystem`, not in `BuildingGameplaySystem`, building wall placement preview/commit scratch state, wall validation context construction, and placement rotate-vertical policy must live in placement preview/context/barrier systems, not in `BuildingGameplaySystem`, building production button commands must route through `BuildingUiCommandSystem` and `BuildingProductionRequestSystem`, not direct production-request command calls in `BuildingGameplaySystem`, building camp item request flow must route through `BuildingUiCommandSystem` and `BuildingProductionRequestSystem`, not shell camp callbacks in `BuildingGameplaySystem`, building UI read methods must route through `BuildingUiQuerySystem`, not direct placement query or production request reads in `BuildingGameplaySystem`, building menu startup binding must route through managed composition's narrow UI command/query/interaction systems and `BuildingGameplayDependencySystem`, not through `BuildingGameplaySystem.BindDependencies`, building runtime building read APIs must route through `BuildingRuntimeQuerySystem` and `BuildingRuntimeQuerySystem.Context`, including base-breach target read routing, and building runtime spawn commands must route through `BuildingRuntimeSpawnCommandSystem` and `BuildingRuntimeSpawnSystem`, and runtime-city building spawn must use the same spawn command boundary, and building faction production spawn point and available helipad spawn queries must live in `BuildingSpawnSystem`, not in `BuildingGameplaySystem`, and building configured unit prefab entity lookup, spawn prefab reverse lookup, and live-unit preview prefab resolution must live in `RuntimeUnitPrefabSystem`, not in `BuildingGameplaySystem`, building initial roster spawn must live in `BuildingRuntimeSpawnSystem` and `BuildingRuntimeSpawnCommandSystem`, and editor-only runtime test helpers must use narrow runtime tick callbacks or local fixtures, not `BuildingGameplaySystem`, building visual helper wrappers for instance creation, positioning, footprint centers, runtime visual initialization, marker refresh, and owner-faction visual tint must not live in `BuildingGameplaySystem`, building selection, visible-selectable checks, selected-building deletion, and camera-focus helpers must live in `BuildingSelectionSystem`, not in `BuildingGameplaySystem`, runtime building delete callbacks plus runtime entity destroyed callbacks must route through `BuildingRuntimeEntitySystem` / `BuildingCombatSystem`, not public shell methods on `BuildingGameplaySystem`, runtime building blocker creation, path-blocking policy, and combat entity creation must bind through `BuildingRuntimeContextSystem` to `BuildingRuntimeEntitySystem`, not private shell wrapper methods on `BuildingGameplaySystem`, runtime redirect callbacks, selected-hauler order assignment, and building approach checks must bind through `BuildingRuntimeContextSystem` to `BuildingPlacementRedirectSystem` / `BuildingResourceHaulerBridgeSystem`, not private shell wrapper methods on `BuildingGameplaySystem`, runtime tick/runtime city context composition must call `BuildingRuntimeContextSystem` directly for spawn command, runtime visual, combat, runtime query, and barrier contexts instead of shell context wrapper methods on `BuildingGameplaySystem`, and runtime tick composition must use direct child systems and must not use `BuildingGameplaySystem.RuntimeTickDomains`, `RuntimeInputDomains`, or shell runtime state getter delegates. `ManagedGameplayStartupSystem` may consume that composition result, but it must not hold or reach through `BuildingPlacementSystem` to retrieve child systems, contexts, runtime update delegates, interaction boundaries, citizen resource contexts, prefab contexts, or disposal. BuildingGameplaySystem refactor is tracked in `Design/Architecture/building_gameplay_system_refactor_roadmap.md`. The final target is deletion of `BuildingGameplaySystem.cs`. `BuildingGameplaySystem.cs` and `BuildingGameplayTestHarness.cs` must not exist. No broad shell replacement may be introduced under another name.

RuntimeCityBuildingSpawnSystem follow-up refactor is tracked in `Design/Architecture/runtime_city_building_spawn_system_refactor_roadmap.md`. `RuntimeCityBuildingSpawnSystem` may remain only as an algorithm-light city-building spawn coordinator while it is being decomposed. Landmark placement, roadside plot placement, rural scatter placement, yard-wall fit/visual algorithms, decoration prefab classification, decoration placement, spawn/reserve validation, mutable config/dependency state, and coroutine random-state plumbing must move to narrow `*System` boundaries. Shared runtime-city building footprint lookup, spawn/delete-on-failed-validation, reservation, road-overlap validation, required-touch validation, and placement anchor recording belong in `RuntimeCityBuildingPlacementSystem`. Roadside plot spacing, random prefab choice, centered-origin projection, spawn/reserve request construction, and used-plot recording belong in `RuntimeCityBuildingPlacementSystem.PlaceFromPlots`. Hall, clock-tower, fountain, monument, and pillar offset order plus landmark hall-distance filtering belong in `RuntimeCityLandmarkOffsetSystem`. City hall placement, hall candidate shuffle, centered-origin search, clearance reservation, and hall failure diagnostics belong in `RuntimeCityHallSpawnSystem`. Clock tower, fountain, monument, and pillar placement belong in `RuntimeCityLandmarkSpawnSystem`, including prefab selection, display labels/descriptions, offset iteration, hall-distance rejection, road/reserved validation, and clearance reservation through the shared placement boundary. RuntimeCityBuildingSpawnSystem.SpawnCityImportantBuildings may only sequence city hall placement before non-hall landmark placement by delegating to `RuntimeCityHallSpawnSystem` and `RuntimeCityLandmarkSpawnSystem`; it must not own private hall or landmark placement algorithms. Bulk runtime-city plot collection and shuffling belong in `RuntimeCityBulkPlotPlanSystem`, including central, outer, and entry plot collection ranges and the central-then-outer-then-entry shuffle order. Entry shop and entry house placement belong in `RuntimeCityEntryBuildingSpawnSystem`, including entry counts, labels, descriptions, spacing, random prefab choice through the shared placement boundary, and footprint anchor recording. Central and outer roadside commercial/residential placement belongs in `RuntimeCityRoadsideBuildingSpawnSystem`, including central shop target calculation, gas station spacing, rural house ratio split, labels, descriptions, and footprint anchor recording. Rural scatter placement belongs in `RuntimeCityRuralBuildingSpawnSystem`, including attempt limits, distance gates, road rejection, plot spacing, random prefab choice, spawn/reserve request construction, and anchor recording. Bulk runtime-city building coroutine sequencing and yield cadence belong in `RuntimeCityBulkBuildingSpawnRoutineSystem`, including entry, roadside, rural, yard-wall, other-building, and decoration sequencing; the temporary yard-wall and decoration callbacks must remain narrow until their dedicated extraction steps. Coroutine random-state handoff for runtime-city bulk building spawning belongs in `RuntimeCityBulkBuildingSpawnRoutineSystem.GenerationRandomState`; `RuntimeCityBuildingSpawnSystem` must not own nested random-state bridge types, and `RuntimeCityGenerationSystem` must hand the value in and out through the routine-system owner. Corridor entrance shop and house placement belongs in `RuntimeCityCorridorBuildingSpawnSystem`, including corridor plot construction, shuffle order, shop/house counts, labels, descriptions, zero spacing, and reserved-footprint placement calls. Runtime-city generation must call `RuntimeCityCorridorBuildingSpawnSystem` directly for corridor entrance placement through explicit spawn context and placement-system dependencies, not through `RuntimeCityBuildingSpawnSystem`. Yard-wall candidate house planning and yard-rect fit checks belong in `RuntimeCityYardWallPlanSystem`, including house shuffle, success target count, padding candidate creation/shuffle, min/max distance clamp, and `CanPlaceHouseYardRect` checks. Yard gate side policy and centered opening math belong in `RuntimeCityYardGateSystem`, including `YardSide`, city-center side selection, and opening start clamp behavior. Yard-wall boundary visual spawning belongs in `RuntimeCityYardWallVisualSystem`, including wall/gate segment splitting, horizontal/vertical side placement, gate rotation choices, pillar footprints, and visual-only prefab spawn calls. House yard-wall orchestration belongs in `RuntimeCityHouseYardWallSystem`, including wall prefab preconditions, house-plan iteration, successful-wall target counting, yard-rect request, gate-side request, wall prefab choice, visual spawn handoff, and reserved-footprint reservation. Decoration prefab classification belongs in `RuntimeCityDecorationPrefabGroupSystem`, including cloth-cover, archway, and free-scatter grouping with the existing ordinal-ignore-case name matching rules. Cloth-cover adjacent decoration placement belongs in `RuntimeCityClothCoverSpawnSystem`, including anchor shuffle, prefab cursor behavior, adjacent-origin candidates, required-touch validation, and reserved-footprint placement calls. Central archway decoration placement belongs in `RuntimeCityArchwaySpawnSystem`, including hall-distance limits, attempt budget, prefab cycling, plot spacing, labels, descriptions, and reservation. Free-scatter decoration placement belongs in `RuntimeCityFreeScatterDecorationSystem`, including distance checks, attempt budget, plot spacing, random prefab choice, labels, descriptions, and reservation. Decoration building sequencing belongs in `RuntimeCityDecorationBuildingSpawnSystem`, including decoration group creation, cloth-cover placement, archway placement, remaining-count calculation, free-scatter fallback prefabs, and free-scatter placement order. RuntimeCityBuildingSpawnSystem must not own private landmark, plot, rural, yard-wall, or decoration algorithm methods after step 30. RuntimeCityBuildingSpawnSystem must not cache mutable runtime-city config, dependency fields, or building-spawn context after step 31; runtime-city generation must pass `RuntimeCityBuildingSpawnContextSystem.Context` explicitly into building-spawn methods. RuntimeCityCompositionSystem must compose extracted runtime-city building-spawn child systems explicitly after step 32 and pass them through `RuntimeCityBuildingSpawnSystem.Systems`; child-system construction must not be hidden inside `RuntimeCityBuildingSpawnSystem`. RuntimeCityGenerationSystem must not depend on RuntimeCityBuildingSpawnSystem after step 33; generation may sequence city building work only by calling the composed child systems through `RuntimeCityBuildingSpawnSystem.Systems` while preserving road generation, city chaining, deferred road ECS sync, deferred building side effects, minimap publication, yield points, and random-state handoff. Shared placement paths must receive explicit `RuntimeCityBuildingSpawnContextSystem.Context` and `RuntimeCityConfigSystem.Snapshot` values, not read coordinator `_context` or config property wrappers. Do not replace RuntimeCityBuildingSpawnSystem with RuntimeCityBuildingManager, RuntimeCityBuildingController, RuntimeCityBuildingSpawnerFacade, or another broad shell.

Runtime-city building-spawn audit drift is blocked after step 34: extracted responsibilities must not return to `RuntimeCityBuildingSpawnSystem`, the deleted `RuntimeCitySpawnerSystem`, or a replacement broad shell, and the audit must name the child-system owners used by `RuntimeCityCompositionSystem` and `RuntimeCityGenerationSystem`.

RuntimeCityBuildingSpawnSystem was deleted in step 35 after it became only pass-through wrapper surface. Runtime-city building-spawn dependency bundling belongs in `RuntimeCityBuildingSpawnContextSystem.Systems`, composed by `RuntimeCityCompositionSystem` and consumed by `RuntimeCityGenerationSystem`; `Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs` must not be restored.

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

## Selection Domain Migration

The retired `RTSSelectionSystem` source/type must not be reintroduced. `SelectionRuntimeUpdateSystem.cs` and `SelectionRuntimeContextSystem.cs` must stay deleted; runtime selection phases must not return to a monolithic managed `Update()` shell or broad managed context shell. The target architecture is no managed selection orchestration shell: UI and shell code write ECS request buffers, ECS systems process gameplay decisions/mutations, and UI views read ECS read models/results.

Allowed direction:
- clicked-unit focus lookup, focusable-unit cache refresh, padded footprint candidate scoring, and focusable candidate policy belong in `FocusableUnitLookupSystem`.
- visible player-unit screen selection, select-all filtering, and selected-tag application belong in `VisibleUnitSelectionSystem`.
- focused-unit lifecycle, focused entity validity checks, selected tag/focus synchronization, clear-selection selected-tag mutation, direct focus assignment, and clicked focus command routing belong in `FocusedUnitLifecycleSystem`.
- focus/clear/select-all/select-filter compatibility commands, external selection command branching, focus command HUD forwarding, full-screen selection request routing, and select-runtime-entity compatibility belong in `RtsSelectionFocusCommandSystem`; focus command context construction belongs in `RtsSelectionFocusCommandContextSystem`; do not reintroduce this command branching or focus/select-all mutation logic into the runtime loop.
- pointer target command dispatch, clicked move/attack/transport/focus queueing, clicked-unit/cell resolution, boardable-transport click tests, and building-target move compatibility belong in `RtsSelectionPointerTargetCommandSystem` or narrower ECS command systems; pointer-target context construction belongs in `RtsSelectionPointerTargetCommandContextSystem`; do not reintroduce pointer-to-gameplay command decisions into the runtime loop.
- focused-unit UI read-model publication, focused labels/descriptions, health/capacity/status projection, focused transport passenger row projection, world-position projection, and portrait pose projection belong in `FocusedUnitUiReadModelSystem` and ECS read-model data such as `FocusedUnitUiReadModelComponent`; UI compatibility getters belong in `SelectionUiReadModelSystem` and must not return to a managed selection shell.
- attack-click target resolution, selected attacker query ownership, attack target validation dispatch, base-breach target resolution bridge, and attack issue result ownership belong in `AttackOrderCommandSystem`.
- move/attack order marker prefab instantiation, runtime marker GameObject ownership, marker material property block ownership, marker show/hide timers, marker grid-blocked validation, and marker world positioning belong in `SelectionOrderMarkerSystem`.
- HUD selection feedback, squad-selection labels, command mode feedback, command result feedback, world-marker visibility forwarding, the HUD feedback context contract, ECS feedback queue publication/consumption, and `BattleHudGameplayBridge` lookup/cache ownership belong in `SelectionHudFeedbackSystem` and ECS feedback data such as `SelectionHudFeedbackElement`; runtime code must not directly call the bridge or bypass the ECS feedback queue.
- camera drag state, smooth focus state, zoom transition state, camera mode math, camera ground projection, camera pan/zoom mutation, and camera mode interpolation belong in `RtsCameraSystem`; camera control requests must flow through ECS data such as `RtsCameraRequestElement`, `RtsCameraRequestQueueComponent`, and `RtsCameraStateComponent`, processed by `RtsCameraRequestSystem`; runtime camera tick orchestration, build/fullscreen pan handling, smooth focus ticking, initial camera focus consumption, zoom ticking, and camera request flushing belong in `RtsSelectionRuntimeCameraSystem`; runtime camera context construction belongs in `RtsSelectionRuntimeCameraContextSystem`; runtime code must not directly enqueue/flush camera requests, call camera mutation/control APIs, or reintroduce private camera runtime tick wrappers.
- selected move-order click rejection, selected move-query consumption, manual move goal assignment orchestration, group path-request staggering, selected move-order diagnostics, and move-order command results belong in `SelectedMoveOrderCommandSystem`.
- selection runtime cached query construction, selected-move query handles, selected-tag query handles, and grid-config query handles belong in `SelectionRuntimeQuerySystem`; managed selection startup must not own `CreateEntityQuery` calls or cached `EntityQuery` fields.
- selected move command request consumption, selected move command execution dispatch, and ECS command result publication belong in `SelectionMoveCommandRequestSystem`; runtime code must not directly execute selected move-order command logic.
- selected attack command request consumption, clicked attack dispatch, ECS attack command result publication, and attack marker result payloads belong in `SelectionAttackCommandRequestSystem`; runtime code must not directly execute clicked attack-order command logic.
- move/attack/transport command result draining, command-result HUD feedback forwarding, command-mode cleanup, command-result screen marker emission, command-result order marker projection, command-result world-marker visibility forwarding, and order marker visibility ticking belong in `RtsSelectionCommandResultFlushSystem`; command-result flush context construction belongs in `RtsSelectionCommandResultContextSystem`; runtime code must not directly drain command result buffers or flush command-result marker/HUD side effects.
- focused-unit command mutations, focused return-to-base lookup, radar target-mode policy, and immediate hold/stop command cleanup belong in `FocusedUnitCommandSystem`.
- selected-unit order snapshot and restore state belongs in `SelectedUnitOrderSnapshotSystem`.
- building-target move order approach search and selected-unit movement component writes belong in `BuildingTargetMoveOrderSystem`.
- selected boarding-source collection, clicked/nearby transport resolution, transport boarding order creation, pending boarding-count checks, and boarding command diagnostics coordination belong in `TransportBoardingCommandSystem`.
- transport boarding/disembark request consumption, boarding result marker payloads, focused transport disembark mutation, and transport command ECS result publication belong in `SelectionTransportCommandRequestSystem`; runtime code must not directly execute boarding or disembark command logic.
- pointer press/release, drag, click, camera drag, selection rectangle, and command intent requests must use ECS data-only request components/buffers such as `RtsSelectionInputRequestQueueComponent`, `RtsSelectionPointerRequestElement`, and `RtsSelectionCommandIntentRequestElement`.
- pointer/session state including drag origin/current positions, UI click suppression, pending release suppression, live selection rectangle, queued move-order click, and last known pointer position belongs in `RtsSelectionInputStateComponent`, accessed through `RtsSelectionInputStateSystem`; do not reintroduce those fields into runtime input wrappers.
- runtime pointer input orchestration belongs in `RtsSelectionRuntimeInputSystem`, backed by ECS input state and request buffers; runtime input context construction belongs in `RtsSelectionRuntimeInputContextSystem`; do not reintroduce queued move-order processing, normal pointer press/hold/release flow, selection-hold triggering, live rectangle diffing, or selection rectangle request queueing into the runtime loop.
- selection rectangle request consumption, visible unit collection for rectangle requests, selected-tag application, selected move cache update, selection focus handoff, and rectangle selection diagnostics belong in `SelectionRectangleRequestSystem`; runtime code must not directly run rectangle selection mutation.
- selection rectangle GUI rendering belongs in `SelectionRectangleView`, which reads `RtsSelectionInputStateComponent` through `RtsSelectionInputStateSystem` and must not own gameplay selection mutation.
- UI command buttons must enqueue ECS selection command intents through `SelectionUiCommandSystem`; `MainMenuPlayUI`, `MatchOverlayCommandControlsController`, and assistant runtime binding must not depend on `RTSSelectionSystem`.
- UI selection read models must flow through `SelectionUiReadModelSystem`; `MenuView` must not read focused-unit status, focused-unit labels, focused-unit health, focused transport passengers, selected unit lists, or visible player-unit queries from `RTSSelectionSystem`.
- UI camera commands and selection screen-marker events must flow through `SelectionUiCameraSystem` and `SelectionScreenMarkerSystem`; `MenuView` must not hold or call `RTSSelectionSystem`.
- Mission and building camera focus delegates must flow through `SelectionUiCameraSystem`; `MissionCameraSystem`, `MissionStartupSystem`, and `BuildingGameplaySystem` must not use `RTSSelectionSystem` for camera focus.
- Building-side selection clearing, transport boarding click tests, and building-target move-order compatibility must flow through `SelectionBuildingInteractionSystem`; `BuildingGameplaySystem` and `BuildingGameplayCompositionSystem` must not depend on `RTSSelectionSystem`.
- Bootstrap, menu startup, and runtime update must receive selection behavior through narrow delegates and ECS/UI selection boundaries; `GameBootstrap`, `MenuStartupSystem`, and `GameplayRuntimeUpdateSystem` must not depend on `RTSSelectionSystem`.
- Production startup must not construct, store, or call the retired `RTSSelectionSystem` or `SelectionRuntimeContextSystem` types. Selection startup may only expose narrow menu-bind/runtime-update/dispose delegates and concrete ECS/UI selection boundaries.
- M01 assistant/tutorial gameplay commands must use ECS request/result data such as `M01AssistantCommandRequestElement` and `M01AssistantCommandResultElement`, processed by `M01AssistantCommandRequestSystem`; `M01AssistantCommandRuntime`, `CommandIntentExecutor`, and `AssistantContextProvider` must not depend on `RTSSelectionSystem` or direct HUD bridge calls for assistant command execution.

## Refactor Direction

Use narrow migrations. Do not rewrite the entire project at once.

1. Introduce service interfaces and ECS-aligned startup systems at the shell edge.
2. Move bootstrap domain behavior into ECS startup systems, services, or configs.
3. Convert `static Instance` access and static runtime state into explicit injection or ECS singleton components.
4. Replace singleton fallback lookups with configured dependencies, ECS queries, or ECS request/response components.
5. Replace static logging with ECS log events plus a log flush service.
6. Convert mission-specific hardcoding into mission configs and systems.
7. Retire legacy class names only when touching that domain for real behavior work.

## Unit Pathfinding Migration

UnitPathfindingSystem refactor is tracked in `Design/Architecture/unit_pathfinding_system_refactor_roadmap.md`.

Pathfinding is a hot gameplay system. Refactoring must preserve current movement behavior and current performance characteristics before improving architecture. Do not change pathing constants, traversal costs, request budgets, search radii, search expansion limits, segment thresholds, scheduling semantics, allocator lifetimes, or hot-path data layout unless a separate approved gameplay/performance task asks for it.

Path request collection, live-unit snapshot ownership, native scratch workspace ownership, reserved-goal state, hierarchical waypoint planning, nearest-goal assignment, result application, retry/abandon policy, adaptive request budgeting, validation metrics, and pathfinding diagnostics must migrate into narrow `*System` boundaries. The remaining `UnitPathfindingSystem` may stay only as a narrow ECS schedule/apply coordinator.

`UnitPathfindingSystem.HasPendingPathJob` is temporary static runtime-state debt. Pending path job state must migrate to an ECS singleton/read-model boundary, and building production, citizen population, and selection/building click guards must read that boundary instead of the static property.

## Building Domain Migration

`BuildingPlacementSystem` must not exist. The retired facade is closed architecture debt; do not recreate it as a source file, wrapper, test harness, singleton, or compatibility type.

The completed deletion record is in `Design/Architecture/buildingplacement_retirement_audit.md`. Production code, editor tests, playmode tests, scenes, prefabs, and generated assets must not construct, serialize, type against, or reference the exact `BuildingPlacementSystem` facade. `BuildingGameplayTestHarness` must not exist; tests should use narrow systems or local fixtures. Building behavior must be implemented in the owning narrow `*System` boundary.

Allowed direction:
- active placement mutable state, active placement cost, and active placement preview handoff belong in `BuildingPlacementLifecycleSystem`; active placement begin/cancel/confirm/exit command flow and selection-preservation state belong in `BuildingPlacementSessionSystem`.
- placement grid/input/preview/commit context construction belongs in `BuildingPlacementContextSystem`; placement cancel/begin/confirm lifecycle context creation plus placement session/command context creation must live in `BuildingPlacementContextSystem`, not private shell wrapper methods on `BuildingGameplaySystem`.
- Footprint, road, blocker, wall-placement validity, wall run/origin validation, and wall overlap-cell checks belong in `BuildingPlacementValidationSystem`.
- Runtime building registry ownership, count/dictionary read access, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`; managed composition may receive the registry boundary, but `BuildingPlacementSystem` must not expose separate runtime building count or dictionary facade properties.
- Runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy belong in `BuildingRuntimeCreationSystem`.
- Runtime building blocker entity creation, runtime building combat entity creation, path-blocking policy for runtime buildings, and runtime building combat component setup belong in `BuildingRuntimeEntitySystem`.
- Runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing belong in `BuildingRuntimeQuerySystem`.
- Citizen population runtime must use explicit narrow citizen `*System` boundaries. `CitizenPopulationSystem.cs` must not exist; `GameBootstrap`, `ManagedGameplayStartupSystem`, UI, building gameplay, runtime city, road build, and selection code must not construct, store, type-reference, or call through `CitizenPopulationSystem`. Do not replace the retired shell with `CitizenPopulationManager`, `CitizenPopulationFacade`, `CitizenPopulationController`, or any other broad managed shell.
- Citizen population building read paths must use `BuildingRuntimeQuerySystem` for building lists, positions, destroyed state, refugee settings, and approach cells. The backing dollar pool belongs in `RuntimeResourceSystem`; citizen upkeep spending belongs in `CitizenResourceSystem`; unit prefab registry composition belongs in `RuntimeUnitPrefabSystem`; citizen configured prefab/entity resolution belongs in `CitizenPrefabSystem`. `BuildingPlacementSystem` must not own citizen resource or prefab context factories.
- Citizen records, household records, ECS projection, building role caches, schedule policy, refugee/displacement behavior, danger/fleeing behavior, visible civilian unit sync, movement command mutation, totals/read models, diagnostics, debug commands, external citizen events, and lifecycle update sequencing live in narrow citizen `*System` boundaries.
- Runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction belongs in `BuildingRuntimeResourcePrefabContextSystem`.
- Runtime/manual building spawn orchestration, initial test roster spawn requests, runtime wall-run/segment spawn orchestration, runtime placement footprint queries, runtime wall footprint queries, initial building origin search, and building-definition footprint cloning belong in `BuildingRuntimeSpawnSystem`; runtime/manual spawn command translation belongs in `BuildingRuntimeSpawnCommandSystem`.
- Runtime city generated building spawn/delete/deferred-side-effect bridging belongs in `BuildingRuntimeCitySpawnSystem`; runtime city code must not call the `BuildingPlacementSystem` facade directly.
- Runtime city generation lifecycle state, spawned/generating flags, generation routine ownership, generation frame counters, and generation yield cadence belong in `RuntimeCityLifecycleSystem`; `RuntimeCityCompositionSystem` must not own those fields directly.
- Runtime city startup gating, spawn-on-start readiness, play-request checks, mission exclusion policy, dependency availability checks, required prefab readiness, initial-unit readiness gating, and startup gate result shaping belong in `RuntimeCityStartupSystem`; `RuntimeCityCompositionSystem` must not own that policy directly.
- Runtime city ECS readiness query ownership, grid-data query caching, grid config lookup, initial-unit readiness checks, and initial base exclusion road-rect collection belong in `RuntimeCityReadinessQuerySystem`; `RuntimeCityCompositionSystem` must not own `World`, `EntityQuery`, `EntityManager`, `Allocator`, or direct ECS query setup for runtime city readiness.
- Runtime city generation begin flow, generation coroutine sequencing, deferred road sync begin/end ordering, deferred spawn side-effect begin/end ordering, city-list/RNG lifetime, bulk-building routine stepping, minimap notification handoff, and generation completion belong in `RuntimeCityGenerationSystem`; `RuntimeCityCompositionSystem` must not own `GenerateCityRoutine` or direct generation side-effect ordering.
- Runtime city-chain next-city planning, travel-direction selection, reverse-direction avoidance, target-center candidate policy, city spacing checks, autobahn length policy, source/target connection-cell resolution, city exit validation, and autobahn path validation belong in `RuntimeCityChainSystem`; `RuntimeCityCompositionSystem` must not own `TryPlanNextCity` or cardinal direction state.
- Runtime city road network commit, road-cell population, source-exit road commit, autobahn commit, standalone connector handoff, occupied-road-cell mutation, and road commit failure result shaping belong in `RuntimeCityRoadCommitSystem`; `RuntimeCityCompositionSystem` and `RuntimeCityGenerationSystem` must not own road commit loops or direct road-build commit calls.
- Runtime city layout creation, incoming-anchor stroke wiring, inner connection-cell math, city connection offset math, and ingress-corridor pruning belong in `RuntimeCityIngressSystem`; `RuntimeCityCompositionSystem` must not own `CreateCityLayout`, `GetCityInnerConnectionCell`, `GetCityConnectionOffset`, or `PruneIngressCorridorStrokes`.
- Runtime city state diagnostics, generation wait diagnostics, warning formatting, city-chain planning failure diagnostics, road commit failure diagnostics, and hall-placement failure diagnostics belong in `RuntimeCityDiagnosticSystem`; runtime city gameplay systems must not format or emit direct `Debug.Log*` diagnostics outside that boundary.
- Runtime city static minimap invalidation belongs in `RuntimeCityMinimapEventSystem`; `RuntimeCityGenerationSystem` must publish static-minimap-changed events and must not receive or invoke direct UI callbacks.
- Runtime city visual root ownership belongs in `RuntimeCityVisualSystem`; `RuntimeCityCompositionSystem` may pass the composed runtime city root into that boundary but must not store `_runtimeRoot` or create/parent runtime city visual roots directly.
- Runtime city child-system graph construction, bridge/visual/minimap configuration, context factories, update orchestration, and child-system disposal belong in `RuntimeCityCompositionSystem`; `RuntimeCitySpawnerSystem.cs` must not be restored as a public compatibility shell.
- Runtime city peer state reads belong in `RuntimeCityReadModelSystem`; peer systems such as grid blockers and decoration spawning must not store or call `RuntimeCitySpawnerSystem` when they only need `SpawnOnStartEnabled`, `HasSpawned`, or `IsGenerating`.
- Serialized runtime-city config names such as `RuntimeCitySpawnerSystemConfig`, `RuntimeCitySpawnerSystemSceneConfigAsset`, and `Game_RuntimeCitySpawner_Config.asset` are allowed compatibility debt until a separate asset migration plan exists; this exception applies only to serialized data/config naming, not runtime orchestration code.
- RoadBuildSystem refactor is tracked in `Design/Architecture/road_build_system_refactor_roadmap.md`; `RoadBuildSystem.cs` must not exist, and road runtime behavior must route through narrow road boundaries rather than a broad compatibility shell.
- RoadBuildRuntimeStateSystem follow-up refactor is tracked in `Design/Architecture/road_build_runtime_state_system_refactor_roadmap.md`. The final target is deletion of `RoadBuildRuntimeStateSystem.cs`; that target is complete, and `RoadBuildRuntimeStateSystem.cs` must not exist.
- Do not restore the deleted temporary holder or replace it with `RoadBuildManager`, `RoadBuildFacade`, `RoadBuildController`, `RoadRuntimeStateSystem`, or any other broad managed shell. New road behavior must land in the narrow road systems named by the roadmap.
- Road build read state such as active road mode, drag/build interaction state, pending placement state, and selected-road/delete-prompt state belongs in `RoadBuildReadModelSystem`; peer systems must not store a broad `RoadBuildSystem` reference just to read road interaction state.
- Road build config projection from `RoadBuildSystemConfig` belongs in `RoadBuildConfigSystem`; scene root creation for `RuntimeRoads`, `RuntimeAutobahns`, `RuntimeAutobahnConnectors`, and `RuntimeDebugStraightRoads` belongs in `RoadRuntimeRootSystem`.
- Road graph mutation belongs in `RoadNetworkSystem`; drag/path construction belongs in `RoadPathPlanningSystem`; road footprint queries for building/runtime placement belong in `RoadFootprintQuerySystem`.
- Road-to-ECS projection belongs in `RoadGridProjectionSystem`; it must own road/sidewalk/dirt buffer writes and must reacquire buffers safely after structural changes.
- Road prefab variant parsing belongs in `RoadVisualVariantSystem`; chunk rendering belongs in `RoadChunkVisualSystem`; preview object pooling belongs in `RoadPreviewSystem`; autobahn/special-road visual placement belongs in `RoadSpecialVisualSystem`.
- Road build session lifecycle belongs in `RoadBuildSessionSystem`; pointer input belongs in `RoadBuildInputSystem`; road build commands and the replacement for `RoadBuildSystem.SetBuildMode` belong in `RoadBuildCommandSystem`; delete-road modal state belongs in `RoadDeletePromptSystem`.
- Soldier-base placement, runtime building dictionaries, building blocker/combat entity creation, and building destruction callbacks must move out of road build and into building gameplay/interaction boundaries.
- Runtime-city road generation commands belong in `RoadRuntimeGenerationSystem`; `RuntimeCityRoadBuildBridgeSystem` should depend on that narrow boundary instead of the broad road shell.
- Serialized road config names such as `RoadBuildSystemConfig`, `RoadBuildSystemSceneConfigAsset`, and existing `RoadBuildSystem` config asset references are allowed compatibility debt until a separate asset migration plan exists; this exception applies only to serialized data/config naming, not runtime orchestration code.
- `GameplayFeatureStartupSystem` must receive `BuildingRuntimeCitySpawnSystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition; it must not receive or call `BuildingPlacementSystem`.
- Building runtime tick orchestration and per-phase timing belong in `BuildingPlacementRuntimeTickSystem`; placement pointer/click frame flow belongs in `BuildingPlacementInputRuntimeTickSystem`; building runtime tick diagnostics threshold, enablement, timing normalization, and log formatting belong in `BuildingPlacementRuntimeTickDiagnosticsSystem`; runtime tick context assembly belongs in `BuildingPlacementRuntimeTickContextSystem`, not `BuildingPlacementSystem`; production progress ticking, resource production ticking, resource hauler ticking, and recent spawn reservation cleanup belong in `BuildingProductionRuntimeTickSystem`; runtime boundary publish ticking belongs in `BuildingRuntimeBoundaryPublishSystem`; `BuildingRuntimeUpdateSystem` must not receive or invoke a `BuildingPlacementSystem.Update` delegate.
- Runtime building owner-faction assignment, combat `Faction` component projection, owner marker color projection, and gate friendly-pass blocker updates belong in `BuildingRuntimeOwnershipSystem`.
- Runtime spawn, runtime creation, runtime ownership, runtime city-spawn, building spawn, runtime entity, runtime visual, redirect, combat, runtime query, and barrier context construction belongs in `BuildingRuntimeContextSystem`.
- Placement redirect side-effect deferral, deferred redirect footprints, pending marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and redirect movement component mutation belong in `BuildingPlacementRedirectSystem`.
- Building definition/configured spawnable/unit lookup, configured spawnable/unit prefab list/read access, spawnable/unit prefab lookup aliases, runtime building prefab metadata cache, prefab bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, and runtime/configured building definition construction belong in `BuildingDefinitionSystem`.
- Building placement config application, runtime building root creation, configured definition startup selection, build plane/camera/preview config state, and placement preview initialization belong in `BuildingPlacementStartupSystem`; `BuildingPlacementSystem` must not own serialized config/cache fields or create the `RuntimeBuildings` root directly.
- Building selection screen-click guards, screen-to-grid click routing, and selection-click context construction belong in `BuildingSelectionClickSystem`; building selection clearing, select-and-focus behavior, selected-building focus position resolution, selection context construction, and runtime building cell hit-test/routing belong in `BuildingSelectionSystem`.
- Building visual helper behavior, animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`; runtime building visual initialization, runtime resource animation updates, and runtime marker visibility projection belong in `BuildingRuntimeVisualSystem`.
- Placement visual instance creation, placement visual positioning, prefab model bounds, and transformed bounds helpers belong in `BuildingPlacementVisualSystem`.
- Building deletion orchestration, destruction state, cleanup timing, blocker cleanup, combat-health destruction checks, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in `BuildingCombatSystem`; full building combat ownership should continue moving there by slice.
- Resource storage classification, capacity display math, resource totals, faction economy snapshot contracts, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`.
- Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`; resource-hauler update orchestration, selected-hauler assignment bridging, hauler move-order/path request bridging, building approach checks, and building approach-cell search belong in `BuildingResourceHaulerBridgeSystem`.
- Unit production queue item initialization, player unit production queue mutation, pending production timing/progress, readiness checks, produced-unit liveness pruning, pending queue removal, ready/soon transport-pending lookup, production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`; production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation belong in `BuildingProductionSlotSystem`; active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`; production transport ground-cell conversion, produced-unit movement orders, produced-unit rotation alignment, and transport-spawn bridging belong in `BuildingProductionTransportBridgeSystem`; produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization belong in `BuildingSpawnSystem`; spawn-cell perimeter search helpers belong in `BuildingSpawnCellSystem`; spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup belong in `BuildingSpawnPrefabSystem`.
- Production request, production queue, production update, production transport, production transport bridge, and resource hauler bridge context construction belongs in `BuildingProductionContextSystem`; production source construction must route through `BuildingProductionContextSystem.CreateSource`, not direct source construction in `BuildingGameplaySystem`.
- Menu/camp UI money reads, camp catalog reads, camp request validation/commands, selected-building modal confirm/destroy commands, and placement/session UI commands belong behind `BuildingUiCommandSystem`; configured spawnable/unit UI read models and camp request failure codes must be owned by that boundary, not nested inside `BuildingPlacementSystem`; UI command/query context construction belongs in `BuildingUiContextSystem`, not `BuildingPlacementSystem`; UI source construction must route through `BuildingUiContextSystem.CreateSource`, not direct source construction in `BuildingGameplaySystem`; `MenuView` must not hold a `BuildingPlacementSystem` facade instance or reference its nested UI/data contracts.
- Friendly pending-production UI read models, produced-unit UI read models, selected-building display/health/preview reads, building minimap flags, visible-building selection checks, live-unit preview prefab resolution, and UI progress shaping belong in `BuildingUiQuerySystem`; these read-model contracts must not be nested inside `BuildingPlacementSystem`, and `BuildingUiCommandSystem` must not own read-model query delegates, query context construction, or pending-production UI list retrieval.
- `MenuStartupSystem` must receive `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition; it must not receive or call `BuildingPlacementSystem`.
- `BuildingPlacementSystem` must not expose public building UI read/query or menu/camp command compatibility wrappers.
- RoadBuildSystem and selection gameplay startup/building peer interactions belong behind `BuildingPlacementInteractionSystem`; interaction context construction belongs in `BuildingPlacementInteractionContextSystem`, not `BuildingPlacementSystem`; interaction source construction must route through `BuildingPlacementInteractionContextSystem.CreateSource`, not direct source construction in `BuildingGameplaySystem`; these peer systems must not hold or call a `BuildingPlacementSystem` facade instance directly.
- Runtime building entity-link callbacks must route through `BuildingPlacementInteractionSystem` instead of storing a `BuildingPlacementSystem` facade owner; `MainMenuPlayUI` must not accept a `BuildingPlacementSystem` dependency when it does not use one.
- Selected-building unit production request routing, faction unit-production result contracts, faction unit-production request orchestration, camp item request failure policy, UI production arm consumption, friendly/faction producer lookup, production request focus, and last camp production focus memory belong in `BuildingProductionRequestSystem`.
- Runway prefab metadata discovery, runway footprint expansion for placement validity, and nearest airport runway lookup belong in `BuildingRunwaySystem`.
- Placement outline object lifetime, outline material/color updates, wall preview segment rebuilds, and preview segment validity tinting belong in `BuildingPlacementPreviewSystem`.
- Placement commit expansion, wall-run origin construction, wall segment footprint/rotation helpers, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy belong in `BuildingPlacementCommitSystem`.
- Active placement pointer event orchestration, drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing belong in `BuildingPlacementInputSystem`.
- Placement/grid math, footprint center projection, center-screen placement origin resolution, screen-to-grid raycasts, placement footprint rotation, and placement focus bounds belong in `BuildingPlacementGridSystem`.
- Placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, selected-building production prefab read models, and selected-building query context construction belong in `BuildingPlacementQuerySystem`.
- Road barrier gate classification, gate-to-nearby-wall alignment, base-breach memory, enemy wall/gate perimeter lookup, breach-target resolution, breach-building target selection, breach approach-cell search, barrier door proximity checks, and barrier door visual open-state updates belong in `BuildingBarrierSystem`.
- Produced-unit UI lists, pending-production UI entries, selected-building UI read models, minimap building read models, live-unit preview read models, UI progress shaping, and temporary building UI list read models belong in `BuildingUiQuerySystem` until UI uses ECS query/request components.
- AI/building cross-domain integration must move through `BuildingRuntimeBoundaryTag` ECS buffers. Configured building/unit read models, faction building/resource summaries, produced/queued unit summaries, faction unit-production requests, faction resource-sell requests, and runtime building spawn requests belong in `BuildingRuntimeEcsBoundaryComponents`; `GameBootstrap` only installs the boundary entity/buffers, and temporary boundary publish/consume orchestration belongs in `BuildingRuntimeBoundarySystem` until the facade is retired.
- `GameBootstrap` must not publish a managed `BuildingPlacementSystem` facade through ECS component objects; the runtime building ECS boundary is `BuildingRuntimeBoundaryTag` plus explicit buffers only.
- `BuildingPlacementSystem` must not expose faction production, faction resource economy/sell, or faction count compatibility wrappers; callers should use `BuildingProductionRequestSystem`, `FactionResourceSystem`, `BuildingRuntimeQuerySystem`, or the `BuildingRuntimeBoundaryTag` ECS buffers directly.

When touching building code, do not reintroduce `BuildingPlacementSystem`; extract or extend the matching `*System` slice.

## Decision Test

For every class, answer:

> What single reason should cause this class to change?

If the answer mentions more than one domain or layer, split the responsibility before adding more behavior.
