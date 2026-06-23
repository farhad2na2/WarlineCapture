# Gameplay SOLID/ECS Architecture Contract

This contract defines the intended architecture for WarlineCapture gameplay code. It is written as a drift guardrail: existing debt may be grandfathered temporarily, but new gameplay work must move toward this shape instead of expanding mixed-responsibility code.

## Core Rule

Gameplay runtime is ECS data plus ECS systems. Unity object code exists only at the edges: authoring, baking, UI views, bootstrap composition, config assets, and editor tooling.

Runtime gameplay code must not introduce singleton access patterns. `static Instance`, global registries, and static service locators are migration debt unless the type is a pure, stateless data/math helper.

Domain gameplay runtime types must be named for ECS, not application-layer patterns. New domain gameplay types should end in `Entity`, `Component`, or `System`. Canvas/reference UI types may end in `View`. ScriptableObject data may end in `Config`. Unity conversion-edge types may end in `Authoring` or `Baker`.

Bare `*System` names are reserved for Unity ECS systems (`ISystem`, `SystemBase`, or legacy ECS system bases). Plain C# runtime helpers that are not scheduled by ECS must not keep bare `*System` names. When a plain helper remains managed or non-ECS by design, rename it with an approved reason suffix that explains why it is outside ECS: `UiSystemHelper`, `CameraSystemHelper`, `PrefabSystemHelper`, `VfxSystemHelper`, `SceneSystemHelper`, `StartupSystemHelper`, `DiagnosticsSystemHelper`, `PresentationSystemHelper`, `CompositionSystemHelper`, or `UtilitySystemHelper`. The detailed migration tracker is `Design/Architecture/non_ecs_system_helper_naming_refactor_tracker.md`.

Source asset filenames must not start with the project/product name. Use feature or domain prefixes such as `UI`, `Gameplay`, `Unit`, `Building`, `Vehicle`, `Config`, `Save`, `Audio`, or `Brand`, and preserve Unity `.meta` files during renames. The full naming rule is tracked in `Design/Architecture/file_naming_architecture_contract.md`.

Faction identity is centralized in `FactionIdentity`: faction `0` is neutral/non-commandable, faction `1` is the player, and faction `2+` is hostile/AI by default. Gameplay code must not hard-code `Faction.Id == 0` as player control or `Faction.Id != 0` as enemy control; it must use `FactionIdentity` helpers so neutral authored map buildings and units remain non-commandable.

## Assembly, Allocation, And Burst Hot-Path Contract

The assembly split, GC allocation cleanup, and ECS Burst hot-path roadmap are part of the architecture contract, not optional optimization passes. New work must preserve these boundaries so the project does not drift back into one large default assembly, managed per-frame allocation, or non-Burst hot simulation loops.

### Assembly Boundaries

Runtime code must be compiled through explicit bounded assemblies, not the default `Assembly-CSharp` fallback. When adding runtime source under `Assets/Game/Scripts`, place it under an existing appropriate `.asmdef` boundary or add a focused assembly definition for the new bounded area.

Assembly dependencies must stay directional:
- Core runtime/data assemblies may depend on components, configs, contracts, and Unity ECS/runtime packages.
- Runtime gameplay must not depend on concrete UI runtime, rendering implementation, authoring, editor, or test assemblies.
- UI, rendering, authoring, editor, and tests may depend on the runtime/contracts they consume, but runtime must not reach back into those edges.
- Editor-only code must remain in editor assemblies or `Editor` folders.

Do not solve dependency pressure by creating a broad catch-all assembly, adding circular `.asmdef` references, or hardcoding default assembly names. Assembly changes must preserve `.meta` files and pass `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`.

### GC Allocation Rules

Match runtime hot paths target `0 B/frame` managed allocation after warmup. Any recurring residual must be profiler-backed, justified, and documented under `Design/AgentReports`; the accepted residual ceiling is under `1 KB/frame` unless the user explicitly approves a temporary exception.

Allocation fixes must be evidence-first. Before changing code for GC cleanup, capture `GC.Alloc` call stacks for the scenario being optimized, lock the exact edit list from those call stacks, then fix one confirmed site/file at a time. Do not rewrite unrelated systems while chasing allocations.

Frequent `Update`, `LateUpdate`, ECS `OnUpdate`, rendering, minimap, combat, selection, diagnostics, and UI-shell presentation paths must not create recurring managed allocations through:
- new managed arrays/lists/dictionaries in the frame loop,
- LINQ or closure/delegate capture,
- boxing from interface enumeration or managed ECS access,
- string interpolation, `ToString`, or logging before diagnostics gates,
- per-frame `ToEntityArray` / `ToComponentDataArray` snapshots,
- allocating Unity APIs when a non-alloc path is practical.

Use cached queries, reusable managed buffers, persistent or temp-job native containers with clear ownership, non-alloc Unity APIs, command buffers for structural changes, and diagnostics gates before string formatting. The active detailed plan is `Design/GC_Allocation_Elimination_Plan.md`.

### Burst And Job Rules

Pure, frequent gameplay simulation/data transforms must be evaluated for Burst and jobs when touched. If compatible, use `[BurstCompile]` with `ISystem`, `IJobEntity`, `IJobChunk`, or another Burst-compatible job path. If a touched hot path cannot be Burst-compatible, document the exact managed boundary reason in the relevant roadmap/report.

Do not force Burst into managed edge code. UI views, GameObject/prefab presentation, config asset loading, bootstrap composition, editor tooling, and diagnostics flushing may remain managed, but they must stay outside pure simulation hot loops.

Every runtime system with `OnUpdate` and no `[BurstCompile]` must be classified in `EcsBurstHotPathArchitectureTests` as either an intentional managed boundary or tracked hot-path debt. A managed-boundary classification is not permission to own gameplay policy: presentation/prefab bridges may display or mirror ECS data, diagnostics flush systems may format already-gated events, and startup/bootstrap systems may project authored data, but recurring simulation decisions still belong in ECS data/jobs. Tracked hot-path debt must point to the roadmap phase that will split data-only work from managed edges before the debt ceiling can be considered healthy.

Hot ECS work should prefer chunk/job iteration over main-thread entity/component snapshot copies. Avoid `ToEntityArray` and `ToComponentDataArray` in frequent paths unless the call is measured, justified, and guarded by the active roadmap. Structural changes should be batched through entity command buffers unless same-frame playback is required and documented. Do not introduce unnecessary sync points or dependency completions in order to simplify code.

The active roadmap is `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md`. New or changed hot-path work must keep `EcsBurstHotPathArchitectureTests.RunFocusedValidation` from regressing, and roadmap ratchets must move toward fewer non-Burst hot systems and fewer hot snapshot-copy calls.

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

`GameBootstrap` is retired and must not be restored. Persistent app/menu lifetime belongs to `MenuBootstrapSystem` plus `MenuBootstrapView`; match-scene lifetime belongs to `MatchBootstrapSystem` plus `MatchSceneView`. The views are serialized-reference binders only. The systems may compose existing startup/update systems, but they must not absorb domain gameplay policy.
AI startup config projection is owned by `AIStartupSystem`; `MatchBootstrapSystem` may pass serialized `AIControllerConfig` references into that system, but it must not create or mutate `FactionEconomy`, `FactionControlEntry`, `AIBuildPlan`, `AIProductionPlan`, `AISquadPlan`, `AITargetPrioritySetting`, or AI diagnostic events directly, and it must not own mission-specific fixed tactical policy.
Faction economy startup projection is owned by `FactionEconomyStartupSystem`; `AIStartupSystem` may request economy startup projection from that system, but it must not create or mutate `FactionEconomy` or `FactionEconomyPolicy` directly.
AI faction-control startup projection is owned by `AIFactionControlStartupSystem`; `AIStartupSystem` may request faction-control startup projection from that system and convert its result to `AIStartupSystem.Result`, but it must not create or mutate `FactionControlConfigTag` or `FactionControlEntry` directly.
AI default build and production fallback ids are owned by authored `AIPlanEntryStartupConfig` assets; `AIStartupSystem` may pass that config to `AIPlanEntryStartupSystem`, and `AIPlanEntryStartupSystem` may write ECS buffers from preferred ids plus config fallbacks, but neither system may hardcode fallback building or unit ids directly.
Mission startup is owned by `MissionStartupSystem`; M01 camera/framing policy is owned by `MissionCameraSystem`. `MatchBootstrapSystem` may pass serialized mission binders, cameras, and legacy visual roots into mission startup, but it must not calculate mission camera framing, hide mission-specific visual roots, run fixed tactical mission guardrails directly, or disable generic AI plans for a fixed tactical mission.
Configured faction spawn-cell resolution is owned by `InitialFactionSpawnCellSystem`; `MatchBootstrapSystem` may configure that system with the ECS world and authored fallback initial-unit config, but it must not query `InitialUnitsSpawnConfig` buffers or fallback faction lists directly.
Broad scene lookup and UI runtime binding are owned by `GameplaySceneBindingSystem`; `MatchBootstrapSystem` may call that boundary during startup, but it must not call `Resources.FindObjectsOfTypeAll`, `Object.FindObjectsByType`, `FindObjectOfType`, `FindAnyObjectByType`, `FindObjectsSortMode`, discover loaded scene UI collaborators directly, or own loaded-scene filtering helpers. New runtime references must be passed through serialized view fields, ECS managed reference components such as `MatchSceneReferenceComponent`, or injected boundary systems.
Performance diagnostics are owned by `PerformanceDiagnosticsSystem`; `MenuBootstrapSystem` owns persistent diagnostics setup, and `MatchBootstrapSystem` may resolve and bracket lifecycle calls through that boundary, but neither bootstrap system may format or emit `FreezeDetect`, `FrameRateDiag`, or `PerfDiag` diagnostics directly or own profiler recorder state.
Managed gameplay runtime update orchestration is owned by `GameplayRuntimeUpdateSystem`. `MatchBootstrapSystem` may call that system from Unity lifecycle methods and pass current composed references into it, but it must not own the long managed `Update`, `LateUpdate`, or `OnGUI` step sequence directly. Building runtime updates inside that loop must go through `BuildingRuntimeUpdateSystem`. `BuildingRuntimeUpdateSystem` ownership and context construction belong in managed composition, and it must invoke a narrow building runtime tick callback. `MatchBootstrapSystem` must not hold a public or private `BuildingPlacementSystem` facade; it may only store the narrow systems, contexts, and disposal callback produced by managed composition.

BuildingGameplayCompositionSystemHelper follow-up refactor is tracked in `Design/Architecture/building_gameplay_composition_system_refactor_roadmap.md`. `BuildingGameplayCompositionSystemHelper` may remain as the top-level managed building gameplay composition entrypoint, but it must shrink to sequencing named composition systems and returning the composed result. It must not own gameplay policy, mutable runtime state, context-factory algorithms, UI command logic, selection logic, production logic, runtime building query logic, visual helper logic, disposal internals, resource prefab internals, or entity-manager access helpers. Extracted responsibilities must not return to `BuildingGameplayCompositionSystemHelper`, and `BuildingGameplaySourceCompositionSystemHelper` must keep the explicit final owner system graph instead of hiding it behind discovery, reflection, a service locator, or a broad replacement shell. Composition result behavior belongs in `BuildingGameplayResultCompositionSystemHelper`; child-system graph creation belongs in `BuildingGameplayChildSystem`; startup/config composition belongs in `BuildingGameplayStartupCompositionSystemHelper`; menu/gameplay feature binding belongs in `BuildingGameplayBindingCompositionSystemHelper`; citizen population binding belongs in `BuildingCitizenPopulationCompositionSystemHelper`; disposal source ownership belongs in `BuildingGameplayDisposalCompositionSystemHelper`; marker property block reuse belongs in `BuildingMarkerVisualPresentationSystemHelper`; runtime resource prefab source wiring belongs in `BuildingRuntimeResourcePrefabCompositionSystemHelper`; runtime tick source assembly belongs in `BuildingRuntimeTickCompositionSystemHelper`; runtime input tick context wiring belongs in `BuildingPlacementInputTickCompositionSystemHelper`; runtime boundary publish wiring belongs in `BuildingRuntimeBoundaryCompositionSystemHelper`; production context wiring belongs in `BuildingProductionCompositionSystemHelper` and `BuildingProductionTickCompositionSystemHelper`; placement interaction, command, query, visual, and adapter wiring belong in the corresponding `BuildingPlacement*CompositionSystem` or `BuildingPlacementAdapterCompositionSystemHelper`; building selection wiring belongs in `BuildingSelection*CompositionSystem`; building selection marker visual ownership belongs in `BuildingSelectionMarkerSystem`; building owner-faction visual projection belongs in `BuildingFactionVisualSystem`; runtime context and query helper wiring belongs in `BuildingRuntimeContextCompositionSystemHelper`, `BuildingRuntimeQueryCompositionSystemHelper`, and `BuildingRuntimeBoundaryCompositionSystemHelper`; grid adapters belong in `BuildingGridCompositionSystem`; deferred runtime side-effect lifetime belongs in `BuildingRuntimeSideEffectCompositionSystemHelper`; and ECS access edge wiring must use explicit context injection or `BuildingEntityManagerAccessSystem`. Do not replace this owner with a `Manager`, `Controller`, `Facade`, `Installer`, `Service`, `Bootstrap`, `Orchestrator`, service locator, singleton, reflection-based auto-wiring layer, or another broad shell.
Managed building gameplay composition is owned by `BuildingGameplayCompositionSystemHelper`. `BuildingGameplayCompositionSystemHelper` constructs narrow building systems directly and must not construct `BuildingGameplaySystem`; the retired `BuildingPlacementSystem` facade must not exist. Building placement startup/config wiring must be routed directly from composition into `BuildingPlacementStartupSystemHelper` and `BuildingGameplayDependencyCompositionSystemHelper`, not through `BuildingGameplaySystem.Init`, building disposal ownership must route through `BuildingGameplayDisposalExecutionCompositionSystemHelper`, not through `BuildingGameplaySystem.Dispose`, building ECS query caching must live in `BuildingGameplayEcsQueryCompositionSystemHelper`, not in `BuildingGameplaySystem`, building grid data access must route through `BuildingGameplayGridDataCompositionSystemHelper`, not direct grid query/buffer reads in `BuildingGameplaySystem`, building placement invalid-cell cache ownership must live in `BuildingPlacementInvalidCellSystem`, not in `BuildingGameplaySystem`, building spawn random state must live in `BuildingSpawnSystem`, not in `BuildingGameplaySystem`, building build-button placement commands must live in `BuildingPlacementCommandRequestCompositionSystemHelper`, not in `BuildingGameplaySystem`, building placement confirm, cancel, exit, pointer-down, and active-placement cost commands must route through `BuildingPlacementCommandRequestCompositionSystemHelper`, not direct session calls in `BuildingGameplaySystem`, building placement focus, visual update, confirm validation, and placement object handoff must live in `BuildingPlacementVisualUpdateCompositionSystemHelper`, not in `BuildingGameplaySystem`, building wall placement preview/commit scratch state, wall validation context construction, and placement rotate-vertical policy must live in placement preview/context/barrier systems, not in `BuildingGameplaySystem`, building production button commands must route through `BuildingUiCommandSystem` and `BuildingProductionRequestBoundary`, not direct production-request command calls in `BuildingGameplaySystem`, building camp item request flow must route through `BuildingUiCommandSystem` and `BuildingProductionRequestBoundary`, not shell camp callbacks in `BuildingGameplaySystem`, building UI read methods must route through `BuildingUiQuerySystem`, not direct placement query or production request reads in `BuildingGameplaySystem`, building menu startup binding must route through managed composition's narrow UI command/query/interaction systems and `BuildingGameplayDependencyCompositionSystemHelper`, not through `BuildingGameplaySystem.BindDependencies`, building runtime building read APIs must route through `BuildingRuntimeQuerySystem` and `BuildingRuntimeQuerySystem.Context`, including base-breach target read routing, and building runtime spawn commands must route through `BuildingRuntimeSpawnCommandSystem` and `BuildingRuntimeSpawnSystem`, and runtime-city building spawn must use the same spawn command boundary, and building faction production spawn point and available helipad spawn queries must live in `BuildingSpawnSystem`, not in `BuildingGameplaySystem`, and building configured unit prefab entity lookup, spawn prefab reverse lookup, and live-unit preview prefab resolution must live in `RuntimeUnitPrefabSystem`, not in `BuildingGameplaySystem`, building initial roster spawn must live in `BuildingRuntimeSpawnSystem` and `BuildingRuntimeSpawnCommandSystem`, and editor-only runtime test helpers must use narrow runtime tick callbacks or local fixtures, not `BuildingGameplaySystem`, building visual helper wrappers for instance creation, positioning, footprint centers, runtime visual initialization, marker refresh, and owner-faction visual tint must not live in `BuildingGameplaySystem`, building selection, visible-selectable checks, selected-building deletion, and camera-focus helpers must live in `BuildingSelectionSystem`, not in `BuildingGameplaySystem`, runtime building delete callbacks plus runtime entity destroyed callbacks must route through `BuildingRuntimeEntitySystem` / `BuildingCombatSystem`, not public shell methods on `BuildingGameplaySystem`, runtime building blocker creation, path-blocking policy, and combat entity creation must bind through `BuildingRuntimeContextSystem` to `BuildingRuntimeEntitySystem`, not private shell wrapper methods on `BuildingGameplaySystem`, runtime redirect callbacks, selected-hauler order assignment, and building approach checks must bind through `BuildingRuntimeContextSystem` to `BuildingPlacementRedirectCompositionSystemHelper` / `BuildingResourceHaulerBridgeSystem`, not private shell wrapper methods on `BuildingGameplaySystem`, runtime tick/runtime city context composition must call `BuildingRuntimeContextSystem` directly for spawn command, runtime visual, combat, runtime query, and barrier contexts instead of shell context wrapper methods on `BuildingGameplaySystem`, and runtime tick composition must use direct child systems and must not use `BuildingGameplaySystem.RuntimeTickDomains`, `RuntimeInputDomains`, or shell runtime state getter delegates. `ManagedGameplayStartupSystem` may consume that composition result, but it must not hold or reach through `BuildingPlacementSystem` to retrieve child systems, contexts, runtime update delegates, interaction boundaries, citizen resource contexts, prefab contexts, or disposal. BuildingGameplaySystem refactor is tracked in `Design/Architecture/building_gameplay_system_refactor_roadmap.md`. The final target is deletion of `BuildingGameplaySystem.cs`. `BuildingGameplaySystem.cs` and `BuildingGameplayTestHarness.cs` must not exist. No broad shell replacement may be introduced under another name.

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

Only Unity ECS systems may use the bare `*System` suffix. Plain C# helpers with no ECS lifecycle must use the approved `*SystemHelper` reason suffixes from the core naming rule above until they are converted to `ISystem`, folded into an ECS owner, or deleted.

### Authoring And Baking

Authoring MonoBehaviours and Bakers exist only to convert Unity-authored references/config into ECS data.

Expected names:
- `*Authoring`.
- `*Baker`.

### UI

UI MonoBehaviours named `*View` are serialized-reference binders only. They connect Canvas objects, child widgets, visual references, and serialized fields to code. They may expose simple getters/setters for visual state and wire UnityEvents to ECS request components, but they must not own gameplay policy, UI flow policy, validation, resource rules, production rules, selection rules, mission rules, AI rules, or state transitions.

UI runtime code must not discover child controls by hierarchy strings such as `transform.Find("Frame/Title")`, deep name searches, or route/path literals. When a screen needs a card, row, tab, inspection panel, or repeated item, create a narrow `*View` component for that UI element and assign its `Button`, `Image`, `TMP_Text`, `GameObject`, and child view references through serialized fields on the prefab. Editor-only migration utilities and tests may inspect prefab paths, but shipped/runtime UI code must use explicit references. Existing hierarchy lookup code is legacy debt; do not copy it into new UI work, and remove it when touching that feature.
Clickable UI elements must put the `Button` on the view/root element that is conceptually clicked. Do not add hidden child hotspot buttons, null-sprite hit targets, or transparent proxy buttons to make a parent clickable; resize the actual button root/graphic and wire it through explicit serialized references.
Shell content prefabs are installed as separate region instances. A view in `MiddleContent`, `LeftContent`, `RightContent`, `HeaderContent`, or `FooterContent` must not serialize direct references to sibling region objects because those references point at the prefab source, not the live shell instance. Cross-region coordination must be wired by shell systems through root `*View` components after the live sections are instantiated.

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
Render-budget diagnostics must use ECS diagnostic event buffers such as `UnitRenderBudgetDiagnosticLogComponent`, flushed by `UnitRenderBudgetDiagnosticLogFlushSystem`. Render-budget diagnostic call sites must gate message construction before formatting LOD state, mismatch samples, or freeze timing details.

## Selection Domain Migration

The retired `RTSSelectionSystem` source/type must not be reintroduced. `SelectionRuntimeUpdateSystem.cs` and `SelectionRuntimeContextSystem.cs` must stay deleted; runtime selection phases must not return to a monolithic managed `Update()` shell or broad managed context shell. The target architecture is no managed selection orchestration shell: UI and shell code write ECS request buffers, ECS systems process gameplay decisions/mutations, and UI views read ECS read models/results.

Allowed direction:
- clicked-unit focus lookup, focusable-unit cache refresh, padded footprint candidate scoring, and focusable candidate policy belong in `FocusableUnitLookupSystem`.
- visible player-unit screen selection, select-all filtering, and selected-tag application belong in `VisibleUnitSelectionSystem`.
- focused-unit lifecycle, focused entity validity checks, selected tag/focus synchronization, clear-selection selected-tag mutation, direct focus assignment, and clicked focus command routing belong in `FocusedUnitLifecycleSystem`.
- focus/clear/select-all/select-filter compatibility commands, external selection command branching, focus command HUD forwarding, full-screen selection request routing, and select-runtime-entity compatibility belong in `RtsSelectionFocusCommandSystem`; focus command context construction is startup-boundary wiring in `SelectionGameplayStartupSystem`; do not reintroduce this command branching or focus/select-all mutation logic into the runtime loop.
- pointer target command dispatch, clicked move/attack/transport/focus queueing, clicked-unit/cell resolution, boardable-transport click tests, and building-target move compatibility belong in `RtsSelectionPointerTargetCommandSystem` or narrower ECS command systems; pointer-target context construction is startup-boundary wiring in `SelectionGameplayStartupSystem`; do not reintroduce pointer-to-gameplay command decisions into the runtime loop.
- focused-unit UI read-model publication, focused labels/descriptions, health/capacity/status projection, focused transport passenger row projection, world-position projection, and portrait pose projection belong in `FocusedUnitUiReadModelSystem` and ECS read-model data such as `FocusedUnitUiReadModelComponent`; UI compatibility getters belong in `SelectionUiReadModelSystem` and must not return to a managed selection shell.
- attack-click target resolution, selected attacker query ownership, attack target validation dispatch, base-breach target resolution bridge, and attack issue result ownership belong in `AttackOrderCommandSystem`.
- move/attack order marker prefab instantiation, runtime marker GameObject ownership, marker material property block ownership, marker show/hide timers, marker grid-blocked validation, and marker world positioning belong in `SelectionOrderMarkerSystem`.
- HUD selection feedback, squad-selection labels, command mode feedback, command result feedback, world-marker visibility forwarding, the HUD feedback context contract, ECS feedback queue publication/consumption, and `BattleHudRuntimeFeedbackView` lookup/cache ownership belong in `SelectionHudFeedbackSystem`, `BattleHudRuntimeFeedbackSystem`, and ECS feedback data such as `SelectionHudFeedbackElement`; runtime code must not bypass the ECS feedback queue.
- camera drag state, smooth focus state, zoom transition state, camera mode math, camera ground projection, camera pan/zoom mutation, and camera mode interpolation belong in `RtsCameraSystem`; camera control requests must flow through ECS data such as `RtsCameraRequestElement`, `RtsCameraRequestQueueComponent`, and `RtsCameraStateComponent`, processed by `RtsCameraRequestSystem`; runtime camera tick orchestration, build/fullscreen pan handling, smooth focus ticking, initial camera focus consumption, zoom ticking, and camera request flushing belong in `RtsSelectionRuntimeCameraSystem`; runtime camera context construction is a startup-boundary wiring responsibility in `SelectionGameplayStartupSystem`; runtime code must not directly enqueue/flush camera requests, call camera mutation/control APIs, or reintroduce private camera runtime tick wrappers.
- selected move-order click rejection, selected move-query consumption, manual move goal assignment orchestration, group path-request staggering, selected move-order diagnostics, and move-order command results belong in `SelectedMoveOrderCommandSystem`.
- selection runtime cached query construction, selected-move query handles, selected-tag query handles, and grid-config query handles are startup-boundary wiring in `SelectionGameplayStartupSystem`; do not reintroduce a standalone query wrapper system or move query creation into per-frame runtime selection branches.
- selected move command request consumption, selected move command execution dispatch, and ECS command result publication belong in `SelectionMoveCommandRequestSystem`; runtime code must not directly execute selected move-order command logic.
- selected attack command request consumption, clicked attack dispatch, ECS attack command result publication, and attack marker result payloads belong in `SelectionAttackCommandRequestSystem`; runtime code must not directly execute clicked attack-order command logic.
- move/attack/transport command result draining, command-result HUD feedback forwarding, command-mode cleanup, command-result screen marker emission, command-result order marker projection, command-result world-marker visibility forwarding, and order marker visibility ticking belong in `RtsSelectionCommandResultFlushSystem`; command-result flush context construction is startup-boundary wiring in `SelectionGameplayStartupSystem`; runtime code must not directly drain command result buffers or flush command-result marker/HUD side effects.
- focused-unit command mutations, focused return-to-base lookup, radar target-mode policy, and immediate hold/stop command cleanup belong in `FocusedUnitCommandSystem`.
- selected-unit order snapshot and restore state belongs in `SelectedUnitOrderSnapshotSystem`.
- building-target move order approach search and selected-unit movement component writes belong in `BuildingTargetMoveOrderSystem`.
- selected boarding-source collection, clicked/nearby transport resolution, transport boarding order creation, pending boarding-count checks, and boarding command diagnostics coordination belong in `TransportBoardingCommandSystem`.
- transport boarding/disembark request consumption, boarding result marker payloads, focused transport disembark mutation, and transport command ECS result publication belong in `SelectionTransportCommandRequestSystem`; runtime code must not directly execute boarding or disembark command logic.
- pointer press/release, drag, click, camera drag, selection rectangle, and command intent requests must use ECS data-only request components/buffers such as `RtsSelectionInputRequestQueueComponent`, `RtsSelectionPointerRequestElement`, and `RtsSelectionCommandIntentRequestElement`.
- pointer/session state including drag origin/current positions, UI click suppression, pending release suppression, live selection rectangle, queued move-order click, and last known pointer position belongs in `RtsSelectionInputStateComponent`, accessed through `RtsSelectionInputStateSystem`; do not reintroduce those fields into runtime input wrappers.
- runtime pointer input orchestration belongs in `RtsSelectionRuntimeInputSystem`, backed by ECS input state and request buffers; runtime input context construction is a startup-boundary wiring responsibility in `SelectionGameplayStartupSystem`; do not reintroduce queued move-order processing, normal pointer press/hold/release flow, selection-hold triggering, live rectangle diffing, or selection rectangle request queueing into the runtime loop.
- selection rectangle request consumption, visible unit collection for rectangle requests, selected-tag application, selected move cache update, selection focus handoff, and rectangle selection diagnostics belong in `SelectionRectangleRequestSystem`; runtime code must not directly run rectangle selection mutation.
- selection rectangle GUI rendering belongs in `SelectionRectangleView`, which reads `RtsSelectionInputStateComponent` through `RtsSelectionInputStateSystem` and must not own gameplay selection mutation.
- UI command buttons must enqueue ECS selection command intents through `SelectionUiCommandSystem`; `MainMenuPlayUI`, `MatchOverlayCommandControlsController`, and assistant runtime binding must not depend on `RTSSelectionSystem`.
- UI selection read models must flow through `SelectionUiReadModelSystem`; `MenuView` must not read focused-unit status, focused-unit labels, focused-unit health, focused transport passengers, selected unit lists, or visible player-unit queries from `RTSSelectionSystem`.
- UI camera commands and selection screen-marker events must flow through `SelectionUiCameraSystem` and `SelectionScreenMarkerUiSystemHelper`; `MenuView` must not hold or call `RTSSelectionSystem`.
- Mission and building camera focus delegates must flow through `SelectionUiCameraSystem`; `MissionCameraSystem`, `MissionStartupSystem`, and `BuildingGameplaySystem` must not use `RTSSelectionSystem` for camera focus.
- Building-side selection clearing, transport boarding click tests, and building-target move-order compatibility must flow through `SelectionBuildingInteractionSystem`; `BuildingGameplaySystem` and `BuildingGameplayCompositionSystemHelper` must not depend on `RTSSelectionSystem`.
- Bootstrap, menu startup, and runtime update must receive selection behavior through narrow delegates and ECS/UI selection boundaries; `MatchBootstrapSystem`, `MenuStartupSystem`, and `GameplayRuntimeUpdateSystem` must not depend on `RTSSelectionSystem`.
- Production startup must not construct, store, or call the retired `RTSSelectionSystem` or `SelectionRuntimeContextSystem` types. Selection startup may only expose narrow menu-bind/runtime-update/dispose delegates and concrete ECS/UI selection boundaries.
- M01 assistant/tutorial gameplay commands must use ECS request/result data such as `M01AssistantCommandRequestElement` and `M01AssistantCommandResultElement`, processed by `M01AssistantCommandRequestSystem`; `M01AssistantCommandRuntime`, `CommandIntentExecutor`, and `AssistantContextProvider` must not depend on `RTSSelectionSystem` or direct HUD view calls for assistant command execution.

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

## Map Surface, Slopes, Roads, And Bridges

Layered map-surface implementation is tracked in `Design/Architecture/map_surface_layered_grid_implementation_roadmap.md`.

Gameplay must not assume all units, buildings, roads, and command targets live at world `y = 0`. Terrain height, slope, roads, bridge decks, highways under bridges, ramps, and other walkable layers must flow through ECS-owned map-surface data.

The target boundary is a precomputed, layered surface grid:
- `MapSurfaceComponent` owns the baked surface reference and grid metadata.
- `MapSurfaceSampler` owns allocation-free height, normal, slope, surface-type, and layer sampling contexts.
- `MapSurfaceConnectionSearch` owns explicit connectivity between terrain, roads, bridge decks, lower highways, ramps, and authored transitions.
- `UnitSurfaceTrackingSystem`, `UnitGroundingSystem`, `VehicleSlopeAlignmentSystem`, `BuildingSurfacePlacementSystem`, and `PathfindingSurfaceCostSystem` consume the surface data through narrow contexts.

Runtime gameplay must not add per-frame physics raycasts, collider-dependent grounding, broad scene-object lookup, singleton surface registries, or `*Manager` / `*Controller` surface owners for normal unit movement, building placement, pathfinding, or bridge traversal. Static map-surface helpers are allowed only when they are pure stateless data/math operations.

Bridge and overpass cells must support multiple walkable surfaces at the same `x/z`. Units path and ground against their current surface/layer, not against a single cell height. Layer changes are valid only through authored connection edges such as ramps and bridge approaches; units must not jump between bridge decks and roads/highways underneath by height proximity.

`UnitPathfindingSystem.HasPendingPathJob` is temporary static runtime-state debt. Pending path job state must migrate to an ECS singleton/read-model boundary, and building production, citizen population, and selection/building click guards must read that boundary instead of the static property.

## Unit Render Budget Migration

UnitRenderBudgetSystem refactor is tracked in `Design/Architecture/unit_render_budget_system_refactor_roadmap.md`.

`UnitRenderBudgetSystem` is hot gameplay/rendering code. Refactoring must preserve current unit visibility, LOD, impostor/detail handoff, render-safety patching, diagnostics, and frame performance before improving architecture. Do not change LOD budget caps, update cadence, camera motion thresholds, visible-character detail policy, enemy impostor thresholds, visual transition stability, render bounds patching, culling tags, `EntityCommandBuffer` playback order, allocator lifetimes, or query membership unless a separate approved gameplay/performance task asks for it.

Query creation, runtime schedule/stability state, camera motion policy, unit snapshot projection, distance/viewport projection, budget-band planning, character/vehicle classification, visible-character policy, LOD readiness recursion, renderability predicates, visual-state transitions, render-safety patching, visibility-change collection, far-impostor tags, structural visibility apply, and render-budget diagnostics must migrate into narrow `*System` boundaries. The remaining `UnitRenderBudgetSystem` may stay only as the ECS render-budget update tick that sequences those systems. It must not expose public/static helper API after migration, except ECS lifecycle methods if the tick remains.

Do not replace `UnitRenderBudgetSystem` with `UnitRenderBudgetManager`, `UnitRenderBudgetController`, `UnitRenderBudgetFacade`, `UnitRenderBudgetOrchestrator`, or another broad shell.

## Initial Units Spawn Migration

InitialUnitsSpawnSystem refactor is tracked in `Design/Architecture/initial_units_spawn_system_refactor_roadmap.md`.

`InitialUnitsSpawnSystem` is startup-critical ECS gameplay code. Refactoring must preserve current initial unit, blocker, resource, faction-base, configured-building, Custom Game source-key, M01 compact-runtime, loading-gate, and respawn queue behavior before improving architecture. Do not change initial spawn batch size, blocker batch size, diagnostic cadence, fail-open wait frames, random-state order, building request order, air-platform slot policy, footprint reservation semantics, component reset list, or `EntityCommandBuffer` playback timing unless a separate approved gameplay/performance task asks for it.

Query ownership, play/startup gating, progress initialization, faction spawn snapshots, respawn queue projection, initial resource projection, building runtime boundary reads, spawnable lookup, faction-base request planning, configured building request creation, building completion processing, completion gating, grid reservation, unit spawn-cell search, air-platform spawn policy, M01 compact roster policy, Custom Game source-key handling, unit spawn application/reset, blocker spawning, structural playback, and initial-spawn diagnostics must migrate into narrow `*System` boundaries. The remaining `InitialUnitsSpawnSystem` may stay only as the ECS initial-spawn tick that sequences those systems. It must not expose public/static helper API after migration, except ECS lifecycle methods if the tick remains.

Do not replace `InitialUnitsSpawnSystem` with `InitialUnitsSpawnManager`, `InitialUnitsSpawnController`, `InitialUnitsSpawnFacade`, `InitialUnitsSpawnOrchestrator`, or another broad shell. Initial building/base spawning must keep using ECS building runtime boundary buffers and must not reintroduce `BuildingPlacementSystem`, `BuildingPlacementRuntimeComponent`, or managed placement facades.

## Building Domain Migration

`BuildingPlacementSystem` must not exist. The retired facade is closed architecture debt; do not recreate it as a source file, wrapper, test harness, singleton, or compatibility type.

The completed deletion record is in `Design/Architecture/buildingplacement_retirement_audit.md`. Production code, editor tests, playmode tests, scenes, prefabs, and generated assets must not construct, serialize, type against, or reference the exact `BuildingPlacementSystem` facade. `BuildingGameplayTestHarness` must not exist; tests should use narrow systems or local fixtures. Building behavior must be implemented in the owning narrow `*System` boundary.

Allowed direction:
- active placement mutable state, active placement cost, and active placement preview handoff belong in `BuildingPlacementLifecycleCompositionSystemHelper`; active placement begin/cancel/confirm/exit command flow and selection-preservation state belong in `BuildingPlacementSessionCompositionSystemHelper`.
- placement grid/input/preview/commit context construction belongs in `BuildingPlacementContextCompositionSystemHelper`; placement cancel/begin/confirm lifecycle context creation plus placement session/command context creation must live in `BuildingPlacementContextCompositionSystemHelper`, not private shell wrapper methods on `BuildingGameplaySystem`.
- Footprint, road, blocker, wall-placement validity, wall run/origin validation, and wall overlap-cell checks belong in `BuildingPlacementValidationSystem`.
- Runtime building registry ownership, count/dictionary read access, id allocation, and active/selected building ids belong in `RuntimeBuildingSystem`; managed composition may receive the registry boundary, but `BuildingPlacementSystem` must not expose separate runtime building count or dictionary facade properties.
- Runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy belong in `BuildingRuntimeCreationSystem`.
- Runtime building blocker entity creation, runtime building combat entity creation, path-blocking policy for runtime buildings, and runtime building combat component setup belong in `BuildingRuntimeEntitySystem`.
- Runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing belong in `BuildingRuntimeQuerySystem`.
- Citizen population runtime must use explicit narrow citizen `*System` boundaries. `CitizenPopulationSystem.cs` must not exist; `MatchBootstrapSystem`, `ManagedGameplayStartupSystem`, UI, building gameplay, runtime city, road build, and selection code must not construct, store, type-reference, or call through `CitizenPopulationSystem`. Do not replace the retired shell with `CitizenPopulationManager`, `CitizenPopulationFacade`, `CitizenPopulationController`, or any other broad managed shell.
- Citizen population building read paths must use `BuildingRuntimeQuerySystem` for building lists, positions, destroyed state, refugee settings, and approach cells. The backing dollar pool belongs in `RuntimeResourceSystem`; citizen upkeep spending belongs in `CitizenResourceSystem`; unit prefab registry composition belongs in `RuntimeUnitPrefabSystem`; citizen configured prefab/entity resolution belongs in `CitizenPrefabSystem`. `BuildingPlacementSystem` must not own citizen resource or prefab context factories.
- Citizen records, household records, ECS projection, building role caches, schedule policy, refugee/displacement behavior, danger/fleeing behavior, visible civilian unit sync, movement command mutation, totals/read models, diagnostics, debug commands, external citizen events, and lifecycle update sequencing live in narrow citizen `*System` boundaries.
- Runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction belongs in `BuildingRuntimeResourcePrefabContextCompositionSystemHelper`.
- Runtime/manual building spawn orchestration, initial test roster spawn requests, runtime wall-run/segment spawn orchestration, runtime placement footprint queries, runtime wall footprint queries, initial building origin search, and building-definition footprint cloning belong in `BuildingRuntimeSpawnSystem`; runtime/manual spawn command translation belongs in `BuildingRuntimeSpawnCommandSystem`.
- Runtime city generated building spawn/delete/deferred-side-effect bridging belongs in `BuildingRuntimeCitySpawnSystem`; runtime city code must not call the `BuildingPlacementSystem` facade directly.
- Runtime city generation lifecycle state, spawned/generating flags, generation routine ownership, generation frame counters, and generation yield cadence belong in `RuntimeCityLifecycleSystem`; `RuntimeCityCompositionSystem` must not own those fields directly.
- Runtime city startup gating, spawn-on-start readiness, play-request checks, mission exclusion policy, dependency availability checks, required prefab readiness, initial-unit readiness gating, and startup gate result shaping belong in `RuntimeCityStartupSystem`; `RuntimeCityCompositionSystem` must not own that policy directly.
- Runtime city ECS readiness query ownership, grid-data query caching, grid config lookup, initial-unit readiness checks, and initial base exclusion road-rect collection belong in `RuntimeCityReadinessQuerySystem`; `RuntimeCityCompositionSystem` must not own `World`, `EntityQuery`, `EntityManager`, `Allocator`, or direct ECS query setup for runtime city readiness.
- Runtime city generation begin flow, generation coroutine sequencing, deferred road sync begin/end ordering, deferred spawn side-effect begin/end ordering, city-list/RNG lifetime, bulk-building routine stepping, minimap notification handoff, and generation completion belong in `RuntimeCityGenerationSystem`; `RuntimeCityCompositionSystem` must not own `GenerateCityRoutine` or direct generation side-effect ordering.
- Runtime city-chain next-city planning, travel-direction selection, reverse-direction avoidance, target-center candidate policy, city spacing checks, autobahn length policy, source/target connection-cell resolution, city exit validation, and autobahn path validation belong in `RuntimeCityChainSystem`; `RuntimeCityCompositionSystem` must not own `TryPlanNextCity` or cardinal direction state.
- Runtime city road network commit, road-cell population, source-exit road commit, autobahn commit, standalone connector handoff, occupied-road-cell mutation, and road commit failure result shaping belong in `RuntimeCityRoadCommitSystem`; `RuntimeCityCompositionSystem` and `RuntimeCityGenerationSystem` must not own road commit loops or direct road-build commit calls.
- Runtime city layout creation, incoming-anchor stroke wiring, inner connection-cell math, city connection offset math, and ingress-corridor pruning belong in `RuntimeCityIngressSystem`; `RuntimeCityCompositionSystem` must not own `CreateCityLayout`, `GetCityInnerConnectionCell`, `GetCityConnectionOffset`, or `PruneIngressCorridorStrokes`.
- Runtime city state diagnostics, generation wait diagnostics, warning formatting, city-chain planning failure diagnostics, road commit failure diagnostics, and hall-placement failure diagnostics belong in `RuntimeCityDiagnosticsSystemHelper`; runtime city gameplay systems must not format or emit direct `Debug.Log*` diagnostics outside that boundary.
- Runtime city static minimap invalidation belongs in `RuntimeCityMinimapEventSystem`; `RuntimeCityGenerationSystem` must publish static-minimap-changed events and must not receive or invoke direct UI callbacks.
- Runtime city visual root ownership belongs in `RuntimeCityVisualSystem`; `RuntimeCityCompositionSystem` may pass the composed runtime city root into that boundary but must not store `_runtimeRoot` or create/parent runtime city visual roots directly.
- Runtime city child-system graph construction, bridge/visual/minimap configuration, context factories, update orchestration, and child-system disposal belong in `RuntimeCityCompositionSystem`; `RuntimeCitySpawnerSystem.cs` must not be restored as a public compatibility shell.
- Runtime city peer state reads belong in `RuntimeCityReadModelCompositionSystemHelper`; peer systems such as grid blockers and decoration spawning must not store or call `RuntimeCitySpawnerSystem` when they only need `SpawnOnStartEnabled`, `HasSpawned`, or `IsGenerating`.
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
- `GameplayFeatureStartupCompositionSystemHelper` must receive `BuildingRuntimeCitySpawnSystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition; it must not receive or call `BuildingPlacementSystem`.
- Building runtime tick orchestration and per-phase timing belong in `BuildingPlacementRuntimeTickSystem`; placement pointer/click frame flow belongs in `BuildingPlacementInputRuntimeTickSystem`; building runtime tick diagnostics threshold, enablement, timing normalization, and log formatting belong in `BuildingPlacementRuntimeTickDiagnosticsSystemHelper`; runtime tick context assembly belongs in `BuildingPlacementRuntimeTickContextCompositionSystemHelper`, not `BuildingPlacementSystem`; production progress ticking, resource production ticking, resource hauler ticking, and recent spawn reservation cleanup belong in `BuildingProductionRuntimeTickSystem`; runtime boundary publish ticking belongs in `BuildingRuntimeBoundaryPublishCompositionSystemHelper`; `BuildingRuntimeUpdateSystem` must not receive or invoke a `BuildingPlacementSystem.Update` delegate.
- Runtime building owner-faction assignment, combat `Faction` component projection, owner marker color projection, and gate friendly-pass blocker updates belong in `BuildingRuntimeOwnershipSystem`.
- Runtime spawn, runtime creation, runtime ownership, runtime city-spawn, building spawn, runtime entity, runtime visual, redirect, combat, runtime query, and barrier context construction belongs in `BuildingRuntimeContextSystem`.
- Placement redirect side-effect deferral, deferred redirect footprints, pending marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and redirect movement component mutation belong in `BuildingPlacementRedirectCompositionSystemHelper`.
- Building definition/configured spawnable/unit lookup, configured spawnable/unit prefab list/read access, spawnable/unit prefab lookup aliases, runtime building prefab metadata cache, prefab bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, and runtime/configured building definition construction belong in `BuildingDefinitionSystem`.
- Building placement config application, runtime building root creation, configured definition startup selection, build plane/camera/preview config state, and placement preview initialization belong in `BuildingPlacementStartupSystemHelper`; `BuildingPlacementSystem` must not own serialized config/cache fields or create the `RuntimeBuildings` root directly.
- Building selection screen-click guards, screen-to-grid click routing, and selection-click context construction belong in `BuildingSelectionClickSystem`; building selection clearing, select-and-focus behavior, selected-building focus position resolution, selection context construction, and runtime building cell hit-test/routing belong in `BuildingSelectionSystem`.
- Building visual helper behavior, animated-part discovery, and animated-part updates belong in `BuildingVisualSystem`; runtime building visual initialization and runtime resource animation updates belong in `BuildingRuntimeVisualSystem`; building selection marker projection belongs in `BuildingSelectionMarkerSystem`; building owner-faction visual projection belongs in `BuildingFactionVisualSystem`; destroyed building visual projection belongs in `BuildingDestroyedVisualPresentationSystemHelper`. Building prefabs under `Assets/Game/Prefabs/Buildings` must not contain `SelectionMarker`, `FactionMarker`, or `Destroyed` children; unit selection marker and unit destroyed visual authoring remain allowed for character prefabs.
- Unit visual adjunct projection must share unit-level ECS boundaries for vehicles and characters: shared marker/health prefab references belong on explicit unit startup ECS boundaries (`UnitSharedVisualPrefabReferences` and `InitialUnitsSpawnConfig`), multi-selection marker projection belongs in `UnitSelectionMarkerSystem`, damage health bar projection belongs in `UnitRuntimeHealthBarSystem`, owner-faction renderer tint target projection belongs in `UnitFactionTintTargetBackfillSystem`, and owner-faction coloring belongs in `FactionVisualSystem`. `UnitGridAuthoring` must not mutate nested model renderer entities during baking. Do not add duplicate vehicle-only or character-only marker/health-bar systems for unit visuals. Vehicle destroyed/wreck visual projection remains vehicle-specific in `VehicleDestroyedVisualSystem`. Vehicle prefabs under `Assets/Game/Prefabs/Vehicles` must not contain or inherit `SelectionMarker`, `FactionMarker`, `HealthBar`, or `Destroyed` children. Character prefabs under `Assets/Game/Prefabs/Characters` must not contain or inherit `SelectionMarker`, `FactionMarker`, or `HealthBar` children. Character destroyed/death behavior is not part of the character visual-adornment pass unless a separate destroyed-visual audit adds it explicitly.
- Authored `Match` scene map-building model conversion belongs in `MapBuildingPlacementSpawnSystem` using `MapBuildingPlacementConfig` baked by editor tooling. Runtime conversion must use explicit `MatchSceneView` references and existing `BuildingRuntimeSpawnSystem` registration; it must clone the authored map visual object and hide the authored source hierarchy after conversion, not instantiate the gameplay prefab `Model` child. Map-authored runtime visuals must preserve authored transform and authored materials while still registering normal building gameplay data. They must not be retargeted by runtime foundation projection or faction material tint. Runtime conversion must not use broad scene lookup, generic build-request relocation, duplicate building gameplay paths, or new manager/controller/facade shells. Faction ownership for authored map buildings is baked from explicit `Faction1` and `Faction2` authoring volumes.
- Placement visual instance creation, placement visual positioning, prefab model bounds, and transformed bounds helpers belong in `BuildingPlacementVisualPresentationSystemHelper`.
- Building deletion orchestration, destruction state, cleanup timing, blocker cleanup, combat-health destruction checks, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in `BuildingCombatSystem`; full building combat ownership should continue moving there by slice.
- Resource storage classification, capacity display math, resource totals, faction economy snapshot contracts, sell/drain behavior, and resource production ticks belong in `FactionResourceSystem`.
- Hauler source/destination classification, order construction, phase/timer state mutation, cargo capacity checks, and load/unload resource transfer mutation belong in `ResourceHaulerSystem`; resource-hauler update orchestration, selected-hauler assignment bridging, hauler move-order/path request bridging, building approach checks, and building approach-cell search belong in `BuildingResourceHaulerBridgeSystem`.
- Unit production queue item initialization, player unit production queue mutation, pending production timing/progress, readiness checks, produced-unit liveness pruning, pending queue removal, ready/soon transport-pending lookup, production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`; production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation belong in `BuildingProductionSlotSystem`; active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`; production transport ground-cell conversion, produced-unit movement orders, produced-unit rotation alignment, and transport-spawn bridging belong in `BuildingProductionTransportBridgeSystem`; produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization belong in `BuildingSpawnSystem`; spawn-cell perimeter search helpers belong in `BuildingSpawnCellSystem`; spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup belong in `BuildingSpawnPrefabSystem`.
- Production request, production queue, production update, production transport, production transport bridge, and resource hauler bridge context construction belongs in `BuildingProductionContextCompositionSystemHelper`; production source construction must route through `BuildingProductionContextCompositionSystemHelper.CreateSource`, not direct source construction in `BuildingGameplaySystem`.
- Menu/camp UI money reads, camp catalog reads, camp request validation/commands, selected-building modal confirm/destroy commands, and placement/session UI commands belong behind `BuildingUiCommandSystem`; configured spawnable/unit UI read models and camp request failure codes must be owned by that boundary, not nested inside `BuildingPlacementSystem`; UI command/query context construction belongs in `BuildingUiContextSystem`, not `BuildingPlacementSystem`; UI source construction must route through `BuildingUiContextSystem.CreateSource`, not direct source construction in `BuildingGameplaySystem`; `MenuView` must not hold a `BuildingPlacementSystem` facade instance or reference its nested UI/data contracts.
- Friendly pending-production UI read models, produced-unit UI read models, selected-building display/health/preview reads, building minimap flags, visible-building selection checks, live-unit preview prefab resolution, and UI progress shaping belong in `BuildingUiQuerySystem`; these read-model contracts must not be nested inside `BuildingPlacementSystem`, and `BuildingUiCommandSystem` must not own read-model query delegates, query context construction, or pending-production UI list retrieval.
- `MenuStartupSystem` must receive `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, `BuildingPlacementInteractionSystem`, and their contexts from managed composition; it must not receive or call `BuildingPlacementSystem`.
- `BuildingPlacementSystem` must not expose public building UI read/query or menu/camp command compatibility wrappers.
- RoadBuildSystem and selection gameplay startup/building peer interactions belong behind `BuildingPlacementInteractionSystem`; interaction context construction belongs in `BuildingPlacementInteractionContextCompositionSystemHelper`, not `BuildingPlacementSystem`; interaction source construction must route through `BuildingPlacementInteractionContextCompositionSystemHelper.CreateSource`, not direct source construction in `BuildingGameplaySystem`; these peer systems must not hold or call a `BuildingPlacementSystem` facade instance directly.
- Runtime building entity-link callbacks must route through `BuildingPlacementInteractionSystem` instead of storing a `BuildingPlacementSystem` facade owner; `MainMenuPlayUI` must not accept a `BuildingPlacementSystem` dependency when it does not use one.
- Selected-building unit production request routing, faction unit-production result contracts, faction unit-production request orchestration, camp item request failure policy, UI production arm consumption, friendly/faction producer lookup, production request focus, and last camp production focus memory belong in `BuildingProductionRequestBoundary`.
- Runway prefab metadata discovery, runway footprint expansion for placement validity, and nearest airport runway lookup belong in `BuildingRunwaySystem`.
- Placement outline object lifetime, outline material/color updates, wall preview segment rebuilds, and preview segment validity tinting belong in `BuildingPlacementPreviewPresentationSystemHelper`.
- Placement commit expansion, wall-run origin construction, wall segment footprint/rotation helpers, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy belong in `BuildingPlacementCommitSystem`.
- Active placement pointer event orchestration, drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing belong in `BuildingPlacementInputSystem`.
- Placement/grid math, footprint center projection, center-screen placement origin resolution, screen-to-grid raycasts, placement footprint rotation, and placement focus bounds belong in `BuildingPlacementGridSystem`.
- Placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, selected-building production prefab read models, and selected-building query context construction belong in `BuildingPlacementQueryUiSystemHelper`.
- Road barrier gate classification, gate-to-nearby-wall alignment, base-breach memory, enemy wall/gate perimeter lookup, breach-target resolution, breach-building target selection, breach approach-cell search, barrier door proximity checks, and barrier door visual open-state updates belong in `BuildingBarrierSystem`.
- Produced-unit UI lists, pending-production UI entries, selected-building UI read models, minimap building read models, live-unit preview read models, UI progress shaping, and temporary building UI list read models belong in `BuildingUiQuerySystem` until UI uses ECS query/request components.
- AI/building cross-domain integration must move through `BuildingRuntimeBoundaryTag` ECS buffers. Configured building/unit read models, faction building/resource summaries, produced/queued unit summaries, faction unit-production requests, faction resource-sell requests, and runtime building spawn requests belong in `BuildingRuntimeEcsBoundaryComponents`; `MatchBootstrapSystem` only installs the boundary entity/buffers, and temporary boundary publish/consume orchestration belongs in `BuildingRuntimeBoundarySystem` until the facade is retired.
- `MatchBootstrapSystem` must not publish a managed `BuildingPlacementSystem` facade through ECS component objects; the runtime building ECS boundary is `BuildingRuntimeBoundaryTag` plus explicit buffers only.
- `BuildingPlacementSystem` must not expose faction production, faction resource economy/sell, or faction count compatibility wrappers; callers should use `BuildingProductionRequestBoundary`, `FactionResourceSystem`, `BuildingRuntimeQuerySystem`, or the `BuildingRuntimeBoundaryTag` ECS buffers directly.

When touching building code, do not reintroduce `BuildingPlacementSystem`; extract or extend the matching `*System` slice.

## Unit Transport Boarding Migration

UnitTransportBoardingSystem refactor is tracked in `Design/Architecture/unit_transport_boarding_system_refactor_roadmap.md`.

Transport boarding is gameplay ECS logic, not UI or bootstrap logic. `UnitTransportBoardingSystem` may remain only as the ECS boarding-completion tick that consumes `UnitTransportBoardingTarget` and mutates boarded passenger state through narrow owners. It must expose only the ECS lifecycle methods required by `ISystem`, with no public/internal helper API. Capacity metadata, boardable/candidate read queries, landed/reached rules, approach-cell search, air pickup commands, rope disembark command setup, and transport boarding diagnostics must live in explicit narrow `*System` boundaries.

Do not replace `UnitTransportBoardingSystem` with `UnitTransportBoardingManager`, `UnitTransportBoardingController`, `TransportBoardingFacade`, or another broad shell. Selection, command-result flush, transport command, and startup composition code must receive only the narrow transport systems they need, not a bundled boarding helper surface.

Transport plane boarding and airdrop is tracked in `Design/Architecture/transport_plane_airdrop_boarding_implementation_plan.md`. That feature is the migration reference for replacing legacy managed prefab/VFX bridge patterns with pure ECS visual spawning: source prefabs are baked into entity-prefab references, runtime request state is data-only, unmanaged `ISystem` owners spawn/drop/animate/cleanup through ECS components and `EntityCommandBuffer`, and no runtime `SystemBase`, `MonoBehaviour`, manager, controller, facade, GameObject instantiation, or managed VFX bridge may own the behavior.

## Decision Test

For every class, answer:

> What single reason should cause this class to change?

If the answer mentions more than one domain or layer, split the responsibility before adding more behavior.
