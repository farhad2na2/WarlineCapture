# BuildingGameplayCompositionSystem Refactor Roadmap

This roadmap is the current architecture plan for shrinking `BuildingGameplayCompositionSystem` after `BuildingGameplaySystem` was retired. It is intentionally fixed-scope so the refactor does not drift into an endless sequence of new steps.

This roadmap has 36 steps.

Target file: `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`

Current size at roadmap creation: 1273 lines.

Final target: `BuildingGameplayCompositionSystem` remains only as the top-level managed building gameplay composition entrypoint. It may create narrow `*System` collaborators and return the composed result, but it must not own gameplay policy, mutable runtime state, context-factory algorithms, UI command logic, selection logic, production logic, runtime building query logic, visual helper logic, or disposal/resource prefab internals.

Do not replace `BuildingGameplayCompositionSystem` with `BuildingGameplayCompositionManager`, `BuildingGameplayCompositionController`, `BuildingGameplayCompositionFacade`, `BuildingGameplayInstaller`, or another broad managed shell. New building gameplay domain types must keep ECS-aligned `Entity`, `Component`, or `System` naming. No reflection, singleton fallback lookup, static service locator, or hidden global mutable gameplay state may be introduced while extracting this class.

## Behavior And Performance Constraints

- Preserve current building startup, initial dollars, menu binding, building placement, production, runtime city spawn, runtime building query, selected-building UI, visual marker, citizen resource binding, and disposal behavior.
- Do not change building costs, production queue behavior, spawn placement behavior, runtime city spawn semantics, building selection behavior, placement validity rules, grid cell projection, resource barrel ratios, marker colors, or runtime tick order unless a step explicitly calls for a validation-backed behavior change.
- Keep hot-path runtime update allocation-neutral. Do not allocate new delegates, closures, collections, or material property blocks per frame.
- Do not add reflection to discover child systems or context members.
- Do not move UI gameplay policy into `View` classes. Views remain serialized-reference binders only.

## Baseline Responsibility Inventory

At roadmap creation, `BuildingGameplayCompositionSystem` owns all of these responsibilities:

- constructing the building child-system graph;
- resolving initial dollars and startup config;
- creating the large nested `Result` composition carrier;
- owning the marker `MaterialPropertyBlock`;
- creating runtime resource prefab context sources;
- creating disposal sources;
- creating runtime tick source/context factories;
- creating production runtime tick and production command contexts;
- creating placement input, interaction, UI, command, visual, and context sources;
- creating selection click and selection runtime contexts;
- creating runtime boundary publish, runtime entity, runtime context, and runtime source contexts;
- exposing helper wrappers for focus, placement validation, placement visual update, placement confirm, deferred side effects, gate alignment, runtime building lookup, placement rect, and entity-manager access;
- forwarding citizen population initialization and binding.

The refactor removes those responsibilities from the composition entrypoint by moving them to narrow `*System` boundaries.

## Step 2 Surface Inventory

Step 2 completed by freezing the current public/internal and nested result surface. Future extraction steps should move callers away from this surface or keep it stable until deletion. New public/internal methods, nested result behavior, or broad delegate fields are drift unless a later roadmap step explicitly says why they are temporary.

Top-level type:

- `internal sealed class BuildingGameplayCompositionSystem`

Top-level public/internal methods:

- `public Result Initialize(BuildingPlacementSystemConfig buildingPlacementConfig, Camera worldCamera, Transform runtimeUiRoot, RoadFootprintQuerySystem roadFootprintQuerySystem, RoadFootprintQuerySystem.Context roadFootprintQueryContext, FactionVisualSettings factionVisuals, DayNightSystem dayNight)`
- `internal static int ResolveInitialDollars(BuildingPlacementSystemConfig buildingPlacementConfig)`
- `internal static BuildingGameplayCompositionSourceSystem CreateChildSystems()`
- `internal static BuildingPlacementRuntimeTickContextCompositionSystemHelper.Source CreateRuntimeTickSource(BuildingGameplayCompositionSourceSystem source, BuildingPlacementInteractionSystem.Context interactionContext, MaterialPropertyBlock markerPropertyBlock)`
- `public void BindSelection(Result building, DayNightSystem dayNight, SelectionUiCameraSystem selectionUiCameraSystem, SelectionBuildingInteractionSystem selectionBuildingInteractionSystem)`
- `public void InitializeCitizenPopulation(Result building, DayNightSystem dayNight, Camera worldCamera)`
- `public void BindCitizenPopulation(Result building, DayNightSystem dayNight, SelectionUiCameraSystem selectionUiCameraSystem, SelectionBuildingInteractionSystem selectionBuildingInteractionSystem, CitizenPopulationEventSystem citizenPopulationEventSystem)`

Nested types:

- `public readonly struct Result`
- No custom nested delegate types exist at step 2. Current delegate surface is exposed through `Func<>` and `Action<>` result fields.

`Result` public fields:

- `BuildingSelectionClickSystem SelectionClick`
- `BuildingSelectionClickSystem.Context SelectionClickContext`
- `BuildingRuntimeUpdateSystem RuntimeUpdate`
- `BuildingRuntimeUpdateSystem.Context RuntimeUpdateContext`
- `BuildingRuntimeCitySpawnSystem RuntimeCitySpawn`
- `BuildingRuntimeCitySpawnSystem.Context RuntimeCitySpawnContext`
- `BuildingRuntimeQuerySystem RuntimeQuery`
- `BuildingRuntimeQuerySystem.Context RuntimeQueryContext`
- `BuildingRuntimeSpawnCommandSystem RuntimeSpawnCommand`
- `BuildingRuntimeSpawnCommandSystem.Context RuntimeSpawnCommandContext`
- `BuildingSpawnSystem Spawn`
- `BuildingSpawnSystem.Context SpawnContext`
- `Func<BuildingSpawnSystem.Context> CreateSpawnContext`
- `BuildingBarrierSystem Barrier`
- `Func<BuildingBarrierSystem.Context> CreateBarrierContext`
- `BuildingCombatSystem Combat`
- `Func<BuildingCombatSystem.Context<RuntimeBuildingData>> CreateCombatContext`
- `BuildingUiCommandSystem UiCommand`
- `BuildingUiCommandSystem.Context UiCommandContext`
- `BuildingUiQuerySystem UiQuery`
- `BuildingUiQuerySystem.Context UiQueryContext`
- `BuildingPlacementInteractionSystem Interaction`
- `BuildingPlacementInteractionSystem.Context InteractionContext`
- `CitizenPopulationCompositionSystem.Result CitizenPopulationComposition`
- `System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings`
- `Action<MainMenuPlayUI> BindMainMenu`
- `Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> BindGameplayFeatures`
- `Action Dispose`

`Result` private dependency fields that support public result behavior:

- `BuildingGameplayDependencySystem DependencySystem`
- `BuildingRuntimeResourcePrefabContextSystem RuntimeResourcePrefabContextSystem`
- `BuildingRuntimeResourcePrefabContextSystem.Source RuntimeResourcePrefabSource`
- `CitizenPopulationCompositionSystem CitizenPopulationCompositionBoundary`

`Result` public methods:

- `public Result(...)`
- `public void BindSelection(DayNightSystem dayNight, SelectionUiCameraSystem selectionUiCameraSystem, SelectionBuildingInteractionSystem selectionBuildingInteractionSystem)`
- `public void InitializeCitizenPopulation(DayNightSystem dayNight, Camera worldCamera)`
- `public void DisposeCitizenPopulation()`
- `public void BindCitizenPopulation(DayNightSystem dayNight, SelectionUiCameraSystem selectionUiCameraSystem, SelectionBuildingInteractionSystem selectionBuildingInteractionSystem, CitizenPopulationEventSystem citizenPopulationEventSystem)`

Top-level private state, included for extraction tracking:

- `private const float DestroyedBuildingLifetimeSeconds`
- `private const float OilBarrelsPerFuelBarrel`
- `private readonly BuildingPlacementRuntimeTickContextCompositionSystemHelper _runtimeTickContextCompositionHelper`
- `private MaterialPropertyBlock _markerPropertyBlock`

## Step Plan

1. Complete: Add roadmap and baseline architecture guard
   - Add this roadmap, contract wording, and a deterministic architecture validation entry point.
   - Expected output: future steps have a fixed target and cannot reframe this as a broad shell rewrite.

2. Complete: Freeze public and nested result surface inventory
   - Record every public/internal method, nested type, nested delegate, and result field currently exposed by `BuildingGameplayCompositionSystem`.
   - Expected output: migration can prove callers move to narrow boundaries instead of silently adding new broad API.

3. Complete: Add composition result ownership guard
   - Create or prepare `BuildingGameplayCompositionResultSystem` as the owner of the result carrier and result bind/dispose helpers.
   - Result: `BuildingGameplayCompositionResultSystem.Result` now owns the result carrier; `BuildingGameplayCompositionSystem` no longer declares the large nested `Result` type.

4. Complete: Move result bind methods
   - Move `BindSelection`, `InitializeCitizenPopulation`, `DisposeCitizenPopulation`, and `BindCitizenPopulation` result behaviors to the result owner.
   - Result: result bind/dispose behavior now lives in `BuildingGameplayCompositionResultSystem.Result`; `BuildingGameplayCompositionSystem` keeps only temporary wrapper methods for existing managed startup callers.

5. Complete: Extract child-system construction
   - Move `CreateChildSystems` and child graph creation into `BuildingGameplayChildSystem`.
   - Result: `BuildingGameplayChildSystem.Create()` now owns child graph construction; `BuildingGameplayCompositionSystem` no longer exposes `CreateChildSystems()`.

6. Complete: Extract startup/config composition
   - Move initial dollars, dependency startup, placement startup, and startup-config sequencing into `BuildingGameplayStartupCompositionSystemHelper`.
   - Result: `BuildingGameplayStartupCompositionSystemHelper.Initialize()` owns initial dollars, dependency startup, road-footprint query configuration, and placement startup wiring.

7. Complete: Extract main menu and gameplay feature binding
   - Move main-menu callback binding, gameplay feature binding, and menu-facing composition glue into `BuildingGameplayBindingCompositionSystemHelper`.
   - Result: `BuildingGameplayBindingCompositionSystemHelper` now creates the main-menu and gameplay-feature binding delegates used by the composition result.

8. Complete: Extract citizen population bridge
   - Move citizen resource context creation, citizen population initialization, binding, and disposal callbacks into `BuildingCitizenPopulationCompositionSystem`.
   - Result: `BuildingCitizenPopulationCompositionSystem` now owns citizen boundary creation, citizen resource/prefab context creation, init, bind, and disposal delegation.

9. Complete: Extract disposal source composition
   - Move `CreateDisposalSource` and disposal callback bundling into `BuildingGameplayDisposalCompositionSystemHelper`.
   - Result: `BuildingGameplayDisposalCompositionSystemHelper` now owns the disposal source and dispose action assembly used by the composition result.

10. Complete: Extract marker property block ownership
   - Move marker `MaterialPropertyBlock` lazy creation and reuse to `BuildingMarkerVisualPresentationSystemHelper`.
   - Result: `BuildingMarkerVisualPresentationSystemHelper` now owns lazy marker `MaterialPropertyBlock` creation and reuse.

11. Complete: Extract runtime resource prefab source
   - Move `CreateRuntimeResourcePrefabSource` into `BuildingRuntimeResourcePrefabCompositionSystemHelper`.
   - Result: `BuildingRuntimeResourcePrefabCompositionSystemHelper` now owns runtime resource/unit/building spawn prefab source wiring.

12. Complete: Extract runtime tick source assembly
   - Move `CreateRuntimeTickSource` into `BuildingRuntimeTickCompositionSystemHelper`.
   - Result: `BuildingRuntimeTickCompositionSystemHelper` now owns runtime tick source delegate assembly while later steps extract its sub-context creators.

13. Complete: Extract input runtime tick context
   - Move `CreateInputRuntimeTickContext` into `BuildingPlacementInputTickCompositionSystemHelper`.
   - Result: `BuildingPlacementInputTickCompositionSystemHelper` now owns input runtime tick context wiring while pointer/selection context helpers remain explicit delegate seams for later steps.

14. Complete: Extract runtime boundary publish context
   - Move `CreateRuntimeBoundaryPublishContext` into `BuildingRuntimeBoundaryCompositionSystemHelper`.
   - Result: `BuildingRuntimeBoundaryCompositionSystemHelper` now owns ECS runtime boundary publish context wiring.

15. Complete: Extract production runtime tick context
   - Move `CreateProductionRuntimeTickContext` into `BuildingProductionTickCompositionSystemHelper`.
   - Result: `BuildingProductionTickCompositionSystemHelper` now owns production runtime tick context wiring while production context source extraction remains step 16.

16. Complete: Extract production context source and queue delegates
   - Move `CreateProductionRuntimeContextSource` and `TryQueuePlayerUnitProduction` into `BuildingProductionCompositionSystem`.
   - Result: `BuildingProductionCompositionSystem` now owns production context source construction, player-unit queue delegation, and production focus fallback resolution.

17. Complete: Extract placement interaction context
   - Move `CreateActivePlacementPointerContext` and `CreateBuildingPlacementInteractionContext` into `BuildingPlacementInteractionCompositionSystem`.
   - Result: `BuildingPlacementInteractionCompositionSystem` now owns active placement pointer and placement interaction context wiring.

18. Complete: Extract building UI context source
   - Move `CreateBuildingUiContextSource`, UI command context creation, and UI query context creation into `BuildingUiCompositionSystem`.
   - Result: `BuildingUiCompositionSystem` now owns building UI source, command context, and query context wiring.

19. Complete: Extract placement query context
   - Move `CreateBuildingPlacementQueryContext` into `BuildingPlacementQueryCompositionSystem`.
   - Result: `BuildingPlacementQueryCompositionSystem` now owns placement read/query context construction.

20. Complete: Extract placement command context source
   - Move `CreatePlacementCommandContext`, `CreatePlacementContextSource`, and command handoff glue into `BuildingPlacementCommandCompositionSystemHelper`.
   - Result: `BuildingPlacementCommandCompositionSystemHelper` now owns placement command context/source construction and build-mode command handoffs.

21. Complete: Extract placement visual update composition
   - Move `UpdatePlacement`, `CreatePlacementVisualUpdateContext`, `FocusActivePlacement`, `ValidateActivePlacementForConfirm`, `UpdatePlacementVisual`, and `PlaceBuilding` wrappers into `BuildingPlacementVisualCompositionSystem`.
   - Result: `BuildingPlacementVisualCompositionSystem` now owns placement visual update context and callback wrappers.

22. Complete: Extract initial placement origin and validation adapters
   - Move `TryResolveInitialPlacementOrigin`, `GetCenterScreenPlacementOrigin`, `IsActivePlacementValid`, `IsPlacementValid`, and gate alignment adapter glue into `BuildingPlacementAdapterSystem`.
   - Result: `BuildingPlacementAdapterSystem` now owns initial placement origin, screen-origin, active-placement validity, placement validity, and gate-alignment adapters.

23. Complete: Extract selection click context
   - Move `CreateBuildingSelectionClickContext` into `BuildingSelectionClickCompositionSystemHelper`.
   - Result: `BuildingSelectionClickCompositionSystemHelper` now owns building-click selection context wiring.

24. Complete: Extract building selection runtime context
   - Move `CreateBuildingSelectionContext` and selected-building focus/clear glue into `BuildingSelectionCompositionSystemHelper`.
   - Result: `BuildingSelectionCompositionSystemHelper` now owns building selection runtime context construction and selected-building focus/order glue.

25. Complete: Extract building runtime context source
   - Move `CreateBuildingRuntimeContextSource`, `CreateBuildingRuntimeEntityContext`, and `CreateRuntimeContextSource` into `BuildingRuntimeCompositionSystem`.
   - Result: `BuildingRuntimeCompositionSystem` now owns building runtime context source, runtime entity context, and runtime source construction.

26. Complete: Extract runtime query helper policies
   - Move `IsHouseBuilding`, `TryResolveBuildingFocusWorldPosition`, `TryGetRuntimeBuilding`, `GetEffectivePlacementRect`, and `OverlapsAnyRuntimeBuilding` into `BuildingRuntimeCompositionQuerySystem`.
   - Result: `BuildingRuntimeCompositionQuerySystem` now owns runtime building lookup, focus world-position resolution, house classification, effective placement rects, and runtime-overlap checks.

27. Complete: Extract grid access adapters
   - Move `TryGetGridData`, `TryGetGridForSelection`, `TryGetGridForPlacementInput`, and `TryGetGridCell` into `BuildingGridCompositionSystem`.
   - Result: `BuildingGridCompositionSystem` now owns grid data, selection-grid, placement-input-grid, and screen-to-cell adapter calls.

28. Complete: Extract deferred runtime side-effect bridge
   - Move `BeginDeferredRuntimeBuildingSideEffects` and `EndDeferredRuntimeBuildingSideEffects` composition glue into `BuildingRuntimeSideEffectCompositionSystemHelper`.
   - Result: `BuildingRuntimeSideEffectCompositionSystemHelper` now owns deferred runtime-building side-effect begin/end sequencing and marker refresh handoff.

29. Complete: Extract entity-manager access edge
   - Replace `TryGetEntityManager` helper ownership with `BuildingEntityManagerAccessSystem` or direct explicit context injection.
   - Result: `BuildingEntityManagerAccessSystem` now owns the `World.DefaultGameObjectInjectionWorld` EntityManager access edge and composition receives it through explicit local adapters.

30. Complete: Collapse `Initialize` to high-level composition calls
   - Rewrite `Initialize` so it sequences named composition systems instead of building all contexts inline.
   - Result: `Initialize` now delegates result carrier construction to `BuildingGameplayCompositionResultSystem.Create`; remaining inline delegate bundles are tracked by the follow-up private-method and wrapper cleanup steps.

31. Complete: Remove remaining private context-factory methods from composition
   - Delete all extracted private helper methods from `BuildingGameplayCompositionSystem`.
   - Result: runtime tick diagnostics context creation now lives in `BuildingRuntimeTickCompositionSystemHelper`; `BuildingGameplayCompositionSystem` no longer declares private context-factory methods.

32. Complete: Remove remaining wrapper/delegate pass-throughs
   - Move any leftover wrapper delegate bodies out of `BuildingGameplayCompositionSystem` or delete them if callers use narrow systems directly.
   - Result: managed startup now calls `BuildingGameplayCompositionResultSystem.Result` bind/init behavior directly; `BuildingGameplayCompositionSystem` no longer exposes pass-through bind/citizen wrapper methods.

33. Complete: Tighten contract debt allowances
   - Update architecture tests and contract text so extracted responsibilities cannot return to `BuildingGameplayCompositionSystem` or a broad replacement shell.
   - Result: drift guards name final owners and reject broad replacement files; contract and architecture guards now name extracted final owners, require `BuildingGameplayCompositionSourceSystem` to keep the explicit owner graph, and reject broad replacement files including manager/controller/facade/installer/service/bootstrap/orchestrator shells.

34. Complete: Performance and allocation audit
   - Verify the refactor did not add per-frame allocations, extra material property blocks, repeated context construction in hot update paths, or reflection.
   - Result: runtime tick behavior stays allocation-neutral after the first ECS-ready tick; runtime tick source/context assembly is lazy-cached after EntityManager query initialization, later frame callbacks invoke the cached context, marker `MaterialPropertyBlock` creation remains lazy and reused, and the extracted composition systems add no reflection-based discovery.

35. Complete: Focused runtime smoke validation
   - Validate `Game` startup, initial dollars, building request button flow, placement preview, confirm/cancel, unit production, runtime city spawn handoff, selected-building UI read model, and citizen population resource binding.
   - Result: focused Unity batch smoke passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `BuildingGameplayCompositionRuntimeSmokeValidation` covered initial dollars and camp building request flow, `BuildingRuntimeBoundaryValidation` covered runtime spawn-request completion across structural changes, and `InitialFactionBaseBuildingGameplaySmokeValidation` covered configured base prefabs, runtime building spawn/boundary publication, helipad spawn points, and transport helipad resolution. The broader `InitialFactionBaseValidationTests.RunBatchValidation` remains blocked by an existing faction-1 initial-unit variety config assertion unrelated to this composition refactor, so step 35 used the narrower building gameplay smoke entry point.

36. Complete: Final architecture validation gate
   - Run architecture tests, building gameplay composition batch validation, focused building runtime tests, and Unity smoke validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
   - Result: final gate passed with `git diff --check`, `GameplayArchitectureContractTests.RunBuildingGameplayCompositionArchitectureBatchValidation`, `BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation`, `BuildingRuntimeBoundaryValidationTests.RunBatchValidation`, and `InitialFactionBaseValidationTests.RunBuildingGameplayRuntimeSmokeValidation` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`. Output is compile-clean for the focused gate, broad shell replacements are still rejected, context-factory drift remains guarded, and all 36 roadmap steps are complete.

## Heartbeat Instruction

Continue with the next incomplete step from `Design/Architecture/building_gameplay_composition_system_refactor_roadmap.md`. Treat that roadmap as the current Gameplay refactor priority. Do not wait for the user to type "next step" or "continue". Work step by step until all 36 steps are complete. Preserve current building gameplay behavior and performance: do not change building costs, initial dollars, production queue behavior, spawn placement behavior, runtime city spawn semantics, selected-building UI behavior, placement validity rules, grid projection, marker colors, runtime tick order, or allocation-sensitive context lifetimes unless the roadmap explicitly requires it. After each step, update the roadmap progress, run focused validation when feasible, and stop only for a real blocker, sandbox permission requirement, compile/runtime failure that needs user input, or completion. Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for Unity validation. Do not start unrelated work and do not modify unrelated in-flight files.
