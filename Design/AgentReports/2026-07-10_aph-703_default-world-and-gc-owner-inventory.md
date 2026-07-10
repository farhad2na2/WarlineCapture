# APH-703 Default World and APH-007 GC Owner Inventory

Date: 2026-07-10

Mode: analysis only; source and tracked evidence review; no Unity access

Workspace baseline: `9280ead856fd0bf117fdb3601cc2216c3a35e0f4` plus pre-existing worktree changes

Tracker: `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`

Primary evidence: `Design/AgentReports/2026-07-09_aph-007_match-gc-steady-state_raw-metadata-final_failed.md`

## Result

- The first-party runtime source contains **91** textual `World.DefaultGameObjectInjectionWorld` call sites in **44** files.
- Call-site classification: **37 composition edge**, **24 presentation edge**, **21 authoring/debug**, and **9 hidden service-locator debt**. None of the direct world call sites is classified as unrelated allocation debt.
- APH-007's 15 ranked allocation rows collapse to **13 distinct first-party owner methods**. They account for **204,480 of 269,482 bytes (75.9%)** and 3,676 of 4,826 samples in the 300-frame capture.
- All 13 measured owners are classified as **unrelated allocation debt** at the causal level. Their source methods do not directly call `World.DefaultGameObjectInjectionWorld`; the stacks instead identify scene-root array creation, recurring helper construction, transient query creation, managed diagnostic string formatting, and one UI presentation attribution that still needs reconciliation.
- There is therefore **no direct source-call-site overlap** between the 91 default-world expressions and the 13 APH-007 owner methods. APH-710 must handle world injection and allocation removal as separate workstreams.
- The APH-213 recapture corroborates persistence: the same 13 methods remain the top 15 rows while total player-relevant GC improves to `234,324 / 1,024` bytes. This report retains APH-007 bytes as the required Phase 7 baseline.

## Scope and Rules

The source inventory searched `Assets/Game/Scripts/**/*.cs` for both qualified and unqualified forms of `World.DefaultGameObjectInjectionWorld`. It excludes `Editor`, tests, packages, plugins, and third-party source. Runtime-compiled authoring gizmos and Scenario Lab code remain in scope so they can be explicitly classified as authoring/debug rather than silently omitted.

Classification precedence is behavioral, not name-based:

| Code | Classification | Rule |
| --- | --- | --- |
| `CE` | Composition edge | Scene/application startup, teardown, domain wiring, or an intentionally explicit ECS access boundary resolves the world. |
| `AD` | Authoring/debug | Gizmos, scenario proof playback, diagnostics, or diagnostic-only state reads. |
| `PE` | Presentation edge | UI, audio, camera, renderer, visual instance, or GameObject-to-ECS projection owns the lookup. |
| `HSL` | Hidden service-locator debt | A reusable or recurring gameplay/query helper resolves global world state instead of receiving `World`, `EntityManager`, an entity, or a cached query. This takes precedence over a `*Composition*` filename. |
| `UAD` | Unrelated allocation debt | APH-007 measured allocation has a cause independent of default-world resolution. |

`CE` and `PE` mean the ownership is boundary-aligned; they do not claim that the code is allocation-free or permanently exempt from explicit dependency injection.

## Default World Call-Site Inventory

Line numbers are from the current worktree at the baseline above. Every textual occurrence is represented; rows with multiple line numbers have one classification for each listed occurrence.

| Source call site(s) | Member or purpose | Class | Disposition |
| --- | --- | --- | --- |
| `Audio/Runtime/AudioPlaybackPresentationRuntimeView.cs:33` | Per-frame accepted-audio drain | `PE` | Managed audio playback is an ECS-to-GameObject presentation boundary. Prefer a configured world if multi-world support becomes required. |
| `Authorings/GridAuthoring.cs:270,300,322,346,386` | Runtime road, sidewalk, blocker, footprint, and path gizmo reads | `AD` | All five calls support authoring/debug drawing and should stay outside gameplay ownership. |
| `Composition/GameplayFeatureStartupCompositionSystemHelper.cs:90,103` | Resolve blocker and decoration presentation helpers during feature startup | `CE` | One-time domain construction and dependency wiring. |
| `Composition/MatchBootstrapCompositionSystemHelper.cs:186,700,707,708,709,713,722,740,765,804,991,1034` | Match `Awake`, incremental startup projection, spawn-cell callback, camera binding, and visual-quality initialization | `CE` | Match scene composition root. Resolve once per startup phase and pass dependencies inward. |
| `Composition/MatchBuildingRuntimeBootstrapStartupSystemHelper.cs:11` | Ensure the building runtime boundary entity | `CE` | Startup-owned ECS boundary creation. |
| `Composition/MatchIntroEcsStateQuery.cs:36` | `TryReadState` resolves the world on recurring input/camera reads | `HSL` | The query object is injected, but its world is not. Construct it with a world/entity manager or a composition-owned cached query. |
| `Composition/MatchStartSceneSystemHelper.cs:154` | Unit-prefab registry readiness gate | `CE` | Match load/start lifecycle boundary. |
| `Composition/MenuBootstrapCompositionSystemHelper.cs:187,792` | Startup settings application and shared world/entity-manager access for Menu update/reset | `CE` | Menu scene composition root, including its recurring lifecycle update. Cache the world per lifecycle rather than expanding access into children. |
| `Composition/UiRuntimeAdapters.cs:383` | Minimap/map-surface data-source read | `PE` | UI read-model adapter at the presentation boundary. |
| `Composition/UiRuntimeAdapters.cs:767` | `MatchLaunchCommand` queues Match load/start | `CE` | UI command terminates at the application scene-composition edge. |
| `Environment/RuntimeCityReadinessQueryCompositionSystemHelper.cs:122` | `TryGetLiveEntityManager` used by grid and startup-readiness queries | `HSL` | A recurring query helper owns an implicit global dependency. Pass the manager/query set from runtime-city composition. |
| `Environment/RuntimeCityRoadBuildBridgeCompositionSystemHelper.cs:179` | Grid-cell-size fallback creates a default-world query | `HSL` | The bridge already has a configured road context; grid config should be part of that context rather than a global fallback. |
| `Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs:229` | Read grid/roads/blockers before decoration projection | `PE` | Managed decoration spawning is presentation work. Its transient query is separate potential allocation debt. |
| `Environment/RuntimeGridBlockerPresentationSystemHelper.cs:888` | Entity-manager fallback for blocker visual lifecycle | `PE` | Managed blocker instance projection and teardown boundary. |
| `Rendering/UnitAttackTracePresentationSystemHelper.cs:157` | Cache attack-trace query for drawing | `PE` | Explicit rendering boundary. |
| `Rendering/UnitImpostorPresentationSystemHelper.cs:128` | Per-`LateUpdate` impostor query access | `PE` | Explicit rendering boundary. |
| `RuntimeState/RuntimeBuildingEntityLink.cs:51,110` | Sync and initial transform-offset reads for a linked GameObject | `PE` | GameObject-to-entity presentation link. Configure an entity manager if this link must support non-default worlds. |
| `ScenarioLab/BattleScenarioLabVisualPlayback.cs:105,147,191,289,331,373,415,457,499,541,584,637` | Scenario cleanup and live-ECS proof coroutine polling | `AD` | Scenario Lab proof/debug playback, not production gameplay ownership. |
| `Systems/AIStartupSystem.cs:534` | Resolve live entity manager for authored AI startup projection | `CE` | Startup config projection invoked by Match composition. |
| `Systems/BuildingCitizenPopulationCompositionSystemHelper.cs:20` | Create population boundary when a world exists | `CE` | Building/population domain construction. |
| `Systems/BuildingEntityManagerAccessSystem.cs:21` | Explicit `TryGetEntityManager` access boundary | `CE` | This type is deliberately the building composition access edge; do not let its responsibilities expand. |
| `Systems/BuildingGameplayDisposalExecutionCompositionSystemHelper.cs:77` | Entity destruction during building-domain teardown | `CE` | Composition-owned disposal edge. |
| `Systems/BuildingGameplaySourceCompositionSystemHelper.cs:107,115` | Resolve building visual/faction managed systems in source construction | `CE` | Domain source factory and managed-system wiring. |
| `Systems/BuildingProductionTransportPresentationSystemHelper.cs:413,419` | Configure newest runway unit during transport visual completion | `PE` | Presentation owner, although the repeated property access should become a context dependency. |
| `Systems/CitizenPopulationCompositionSystemHelper.cs:216` | Resolve `CitizenTravelSystem` during population composition | `CE` | Domain managed-system wiring. |
| `Systems/CitizenPopulationEcsProjectionCompositionSystemHelper.cs:20` | Resolve and cache population projection queries | `CE` | ECS projection is initialized and cached by population composition. |
| `Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:365` | Per-frame Match-ready initial-spawn gate | `HSL` | Runtime update already has composed state; pass/cache the entity manager and readiness queries instead of resolving the global world. |
| `Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:438` | Initial-spawn counts for periodic loading-gate diagnostics | `AD` | Diagnostic-only read. Reuse the readiness query set if retained. |
| `Systems/ManagedGameplayStartupSystemHelper.cs:345` | Resolve `DayNightSystem` during managed gameplay startup | `CE` | Startup dependency wiring. |
| `Systems/PerformanceDiagnosticsSystemHelper.cs:694` | Runtime visual-count diagnostics | `AD` | Performance diagnostics and their temporary queries are not gameplay ownership. |
| `Systems/RoadBuildCompositionSourceCompositionSystemHelper.cs:76,89,97,110,123,131` | Resolve road projection, variant, placement, resolution, chunk, and special-visual systems | `CE` | Road domain source factory and managed-system wiring. |
| `Systems/RoadBuildEcsCompositionSystemHelper.cs:62` | Explicit entity-manager access for road-build composition | `CE` | Road composition boundary. |
| `Systems/RtsSelectionInputStateCompositionSystemHelper.cs:95` | `TryResolve` for every input-state request | `HSL` | Command-state storage is a recurring runtime service. Inject/cache its world, entity manager, and state entity. |
| `Systems/RuntimeDiagnosticsSystem.cs:674` | Resolve diagnostics state entity | `AD` | Diagnostic state bridge. |
| `Systems/RuntimeGameplayStateSystem.cs:300` | Static `TryGetStateEntity` used by runtime state setters | `HSL` | The state service hides a global world and static entity cache. Bind it to an explicit world/state entity at composition. |
| `Systems/SelectionBuildingInteractionCompositionSystemHelper.cs:179` | Pointer/building interaction entity-manager fallback | `HSL` | Recurring gameplay interaction helper; entity manager belongs in its existing context. |
| `Systems/SelectionGameplayStartupSystemHelper.cs:1326` | Local `TryGetDefaultEntityManager` used across recurring selection phases | `HSL` | Startup creates the closures but runtime phases repeatedly resolve the global world. Capture an explicit world/manager/query context. |
| `Systems/SelectionGameplayStartupSystemHelper.cs:1510,1518` | Resolve camera request/application managed systems | `CE` | Selection startup dependency wiring. |
| `Systems/SelectionRuntimeDiagnosticsSystemHelper.cs:229` | Enqueue selection diagnostic messages | `AD` | Diagnostic transport only. |
| `Systems/SelectionUiCameraSystemHelper.cs:330,338,347` | Resolve camera systems and UI camera entity manager | `PE` | UI/camera application boundary. Query creation inside camera checks remains separate allocation debt. |
| `Systems/SelectionUiCommandUiSystemHelper.cs:212,258` | Queue tactical-follow requests and read focused selection for UI commands | `PE` | UI command boundary. |
| `Systems/SelectionUiReadModelUiSystemHelper.cs:238` | Resolve selection read-model entity manager | `PE` | UI read-model boundary. |
| `Systems/UnitPathfindingPendingStateStore.cs:108` | Lazily create a pending-path query | `HSL` | Gameplay state store has no explicit world dependency. Construct it with a world/entity manager and dispose on that lifecycle. |
| `UI/Shell/Ecs/AssistantSettingsPersistenceSystem.cs:47` | Settings event projection into ECS | `PE` | Static UI-settings-to-ECS presentation bridge. |
| `UI/Shell/Ecs/UiAudioEventBridgeSystem.cs:63` | UI audio event enqueue into ECS | `PE` | Static UI-event-to-ECS presentation bridge. |
| `UI/Shell/Ecs/UiAudioSettingsProjectionSystem.cs:82` | Audio settings event projection into ECS | `PE` | Static UI-settings-to-ECS presentation bridge. |
| `UI/Shell/Ecs/UiShellEcsGateway.cs:402,709,747,1714,2269` | Selection, command, passenger, minimap, and shell-boundary reads | `PE` | Central UI/ECS gateway. APH-708 should split/cache by route, read-model, action, and settings adapters without moving lookups into views. |

## Hidden Service-Locator Remediation Set

The nine `HSL` sites are the direct APH-710 dependency-injection set:

1. Bind `MatchIntroEcsStateQuery`, `RuntimeCityReadinessQueryCompositionSystemHelper`, and `UnitPathfindingPendingStateStore` to an explicit world/query lifecycle at construction.
2. Add `EntityManager` or cached query/entity fields to existing contexts for runtime-city road fallback, gameplay readiness, selection building interaction, selection runtime phases, and selection input state.
3. Bind `RuntimeGameplayStateSystem` to the composition-created state entity and remove its static default-world lookup/cache.

Do not replace these calls with another static gateway. The target state is explicit world ownership, stable query/entity caching, and lifecycle-bound disposal.

## APH-007 Recurring Allocation Owners

The table groups APH-007 rows 12/13 and 14/15 because each pair is the same first-party owner with separate `String.Format` and `String.Concat` raw stacks. `Evidence` is `bytes / samples / distinct frames`.

| First-party owner | APH-007 evidence | Class | Source-supported cause or next proof | Proposed tracker owner |
| --- | ---: | --- | --- | --- |
| `Composition/MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView` | `38,272 / 299 / 299` | `UAD` | Raw stack starts at `Scene.GetRootGameObjects`; current source creates a root array on every Menu update. Cache the scene view or use a capacity-stable non-allocating root list. | `APH-710` |
| `Systems/SelectionStateCompositionSystemHelper..ctor` | `23,920 / 598 / 299` | `UAD` | Two list fields allocate for each newly constructed helper. | `APH-707` |
| `Systems/TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests` | `16,744 / 299 / 299` | `UAD` | Current source constructs `SelectionStateCompositionSystemHelper` every update; the method-level row is consistent with the helper object allocation in addition to its list allocations. Cache/reuse the dependency. | `APH-707` |
| `UI/Shell/UIShellEcsPresentationSystem.Update` | `14,352 / 299 / 299` | `UAD` | Raw ownership is 48 bytes per frame, but the APH-007 in-method allocation probe reports zero. Reconcile invocation/profiler attribution with a focused capture before changing behavior. | `APH-708` |
| `Systems/RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes an entity query on every call. Cache the query or queue entity for the world lifecycle. | `APH-710` |
| `Systems/BuildingProductionRequestSystemHelper.TryGetUiCampItemCommandEntity` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes an entity query on every call. Cache query/entity state. | `APH-710` |
| `Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes an entity query every audio update. Cache the query or pass the runtime state entity/read model. | `APH-710` |
| `Systems/BuildingProductionRequestSystemHelper.TryGetUiProductionCommandEntity` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes an entity query on every call. Cache query/entity state. | `APH-710` |
| `Systems/BuildingPlacementCommandRequestCompositionSystemHelper.TryGetUiPlacementCommandEntity` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes an entity query on every call. Cache query/entity state. | `APH-710` |
| `Systems/RtsSelectionRuntimeCameraSystemHelper.HasValidTacticalFollowPose` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes a tactical-pose query every camera update. Put both camera queries in the existing context/world cache. | `APH-709` |
| `Systems/RtsSelectionRuntimeCameraSystemHelper.IsTacticalFollowPanLocked` | `11,960 / 299 / 299` | `UAD` | Source creates and disposes a tactical-mode query every camera update. Put both camera queries in the existing context/world cache. | `APH-709` |
| `Systems/UnitPathfindingScheduler.Schedule` | `15,504 / 48 / 8` | `UAD` | Raw stacks are `String.Format` and `String.Concat`; current source formats detailed path diagnostics. Gate formatting before interpolation or move to a non-allocating diagnostic path. | `APH-710` |
| `Systems/UnitPathResultApply.Apply` | `11,968 / 40 / 8` | `UAD` | Raw stacks are `String.Format` and `String.Concat`; current source formats per-result path diagnostics. Gate formatting before interpolation or move to a non-allocating diagnostic path. | `APH-710` |

The table covers the tracker's required scene-root, helper-construction, transport, UI shell, selection, command-query, audio, road, and pathfinding owner groups. APH-711 must not pass until every row is removed from the steady-state capture or explicitly reassigned by the coordinator.

## Priority and Acceptance

1. Remove the seven 299-frame transient query owners together: road, three building command queues, audio simulation state, and two selection-camera states. The common acceptance proof is cached world-bound query/entity state plus zero owner bytes in a fresh raw-metadata capture.
2. Fold the transport helper/object/list allocations into APH-707 and the selection camera query pair into APH-709. Keep the UI-shell attribution investigation with APH-708.
3. Remove per-frame scene-root enumeration and gate pathfinding diagnostic string construction under APH-710.
4. Replace the nine `HSL` sites with explicit dependencies, then run source inventory again. The expected direct-world target is not necessarily zero: composition, presentation, and authoring/debug edges may remain, but no reusable gameplay/query helper should resolve the global world.
5. Re-run the unchanged 180-warmup/300-measured-frame raw-metadata lane for APH-711. Source inspection alone cannot close any measured allocation row.

## Validation Limits

- No Unity editor, player, profiler, MCP, compile, or test execution was used or available for this analysis-only task.
- Counts were cross-checked with `rg --count-matches`: 91 occurrences and 44 files in first-party runtime source.
- Allocation causes marked above are either explicit raw-stack facts or current-source inferences. The UI-shell row is intentionally left unresolved where the raw profiler and in-method probe disagree.
