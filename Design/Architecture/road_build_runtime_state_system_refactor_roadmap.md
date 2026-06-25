# RoadBuildRuntimeStateSystem Refactor Roadmap

This document owns the follow-up road refactor after `RoadBuildSystem.cs` was deleted. The current file is a temporary runtime-state holder created during the RoadBuildSystem retirement, but it still concentrates too many responsibilities. This roadmap is the source of truth for retiring it without changing road gameplay behavior.

## Fixed Step Count

This roadmap has 34 steps. Do not append surprise steps after step 34. If new work is discovered, update the relevant existing step and keep the final validation gate as the last step.

## Target

Target file: `Assets/Game/Scripts/Systems/RoadBuildRuntimeStateSystem.cs`

Current size at roadmap creation: 1351 lines.

Step 3 composition-source transition size: 1358 lines. Child-system construction now lives in `RoadBuildCompositionSourceSystem`; the temporary holder may read through that source only while later steps move contexts and caller-facing surface out. The temporary static `SetBuildMode` bridge was removed in step 8.

Final target: delete `RoadBuildRuntimeStateSystem.cs` and `.meta`. `RoadBuildCompositionSystemHelper` may remain as a wiring-only composition boundary, but it must not own gameplay policy, scene queries, visual refresh algorithms, legacy building behavior, static commands, or runtime state mutation.

## Current Responsibility Inventory

- Child system ownership: constructs and stores runtime gameplay state, road config, roots, network, path planning, footprint query, grid projection, visual variant, chunk visual, preview, special visual, session, minimap, input, command, delete prompt, legacy building storage/ECS, and runtime generation systems.
- Serialized/config cache: stores `RoadBuildSystemConfig`, camera, road prefabs, grid origin, build plane, road grid size, chunk size, preview alpha, soldier-base prefab fields, placement outline dimensions, and placement colors.
- Runtime roots: stores runtime root and road root handles through `RoadRuntimeRootSystem.Roots`.
- Context factories: constructs contexts for footprint, grid projection, road prefabs, chunk visuals, preview, special visuals, session, input, command, delete prompt, legacy building ECS, and runtime generation.
- Public compatibility surface: exposes road runtime-generation commands, footprint queries, init/bind/dispose/update/gui, road commands, and building placement commands.
- Road mutation and refresh: creates/deletes strokes, refreshes dirty cells, resolves visual type, chooses prefab variants, syncs road cells to ECS, rebuilds chunks, and rebuilds special road visuals.
- Session rollback: captures/restores road network snapshots and rebuilds visuals/ECS from current tiles.
- Legacy building behavior: soldier-base definition setup, building bounds caching, placement outline creation/materials, placement preview update, placement validity checks, building commit, building selection hit tests, building delete, and fallback labels/status strings.
- ECS/global access: uses `World.DefaultGameObjectInjectionWorld`, `EntityManager`, `EntityQuery`, `DynamicBuffer<GridRoad>`, and `DynamicBlockerData` directly in the temporary state holder.
- Disposal: tears down roads, previews, chunks, special visuals, legacy building visuals/entities, minimap events, ECS road data, and storage dictionaries.

## Public/Internal Surface Inventory Freeze

New public/internal members must not be added to `RoadBuildRuntimeStateSystem`. Later steps may remove members from this list as callers migrate to the target owners.

- Runtime generation exposure:
  - `RoadRuntimeGenerationSystem`, `RoadRuntimeGenerationContext`
  - Target owner: `RoadBuildCompositionSourceSystem` plus `RoadRuntimeGenerationContextCompositionSystemHelper`.
- Road footprint exposure:
  - `RoadFootprintQuerySystem`, `RoadFootprintQueryContext`
  - Target owner: `RoadBuildCompositionSourceSystem` plus `RoadGridContextSystem`.
- Runtime update / GUI exposure:
  - `RoadBuildInputCompositionSystemHelper`, `RoadBuildInputContext`, `RoadBuildInputCamera`, `RoadDeletePromptUiSystemHelper`, `RoadDeletePromptContext`, `Update`, `OnGui`
  - Target owner: `RoadBuildInteractionContextSystem`, `RoadBuildCompositionSystemHelper`, `RoadBuildInputCompositionSystemHelper`, and `RoadDeletePromptUiSystemHelper`.
- Read-model exposure:
  - `HasPendingBuildingPlacement`, `CanConfirmBuildingPlacement`, `HasSelectedBuilding`, `IsRoadBuildModeActive`, `IsDraggingBuildInteraction`, `PlacementStatusText`, `SelectedBuildingLabel`, `ActiveModeStatusText`
  - Target owner: `RoadBuildReadModelCompositionSystemHelper` plus building interaction/read boundaries.
- Runtime-city road generation commands:
  - `BeginDeferredRoadEcsSync`, `EndDeferredRoadEcsSync`, `TryGetRoadCellSizeInGridCells`, `CreateRoadStrokeFromRoadCells`, `CreateAutobahnStrokeFromRoadCells`, `TryGetAutobahnConnectorRoadCell`, `TryLogRoadConnectMarkers`, `CreateStandaloneStraightRoadChainFromConnector`, `TryGetStandaloneStraightChainEndRoadCell`, `CreateStandaloneDebugCityRoadNetworkFromStraightChain`
  - Target owner: `RoadRuntimeGenerationSystem`, `RoadRuntimeGenerationContextCompositionSystemHelper`, and `RoadGridProjectionSystem`.
- Road footprint commands:
  - `HasRoadInFootprint`, `FillRoadFootprintMask`
  - Target owner: `RoadFootprintQuerySystem` plus `RoadGridContextSystem`.
- Startup/lifecycle:
  - `Init`, `BindDependencies`, `Dispose`
  - Target owner: `RoadBuildStartupSystem`, `RoadBuildDependencyCompositionSystemHelper`, `RoadBuildDisposalCompositionSystemHelper`, and `RoadBuildCompositionSystemHelper`.
- Road build commands:
  - `SetBuildMode`, `ActivateRoadBuildMode`, `ConfirmRoadBuildSession`, `CancelRoadBuildSession`, `ExitBuildMode`
  - Target owner: `RoadBuildCommandCompositionSystemHelper`; static `SetBuildMode` was deleted in step 8.
- Legacy building compatibility commands:
  - `BeginSoldierBasePlacement`, `ConfirmBuildingPlacement`, `CancelBuildingPlacement`, `CreateSoldierFromSelectedBuilding`, `DeleteSelectedBuilding`, `ClearSelectedBuilding`
  - Target owner: `BuildingPlacementInteractionBoundaryCompositionSystemHelper` and the temporary building-road legacy systems until compatibility is deleted.

## Architecture Rules

- Do not restore `RoadBuildSystem.cs`.
- Do not replace `RoadBuildRuntimeStateSystem` with `RoadBuildManager`, `RoadBuildFacade`, `RoadBuildController`, `RoadRuntimeStateSystem`, or another broad managed shell.
- Serialized names such as `RoadBuildSystemConfig` and `RoadBuildSystemSceneConfigAsset` remain allowed data compatibility debt until a separate asset migration plan exists.
- New runtime behavior must land in narrow `*System` boundaries.
- View classes remain only for serialized UI references. No gameplay logic belongs in views.
- Static road commands are forbidden except pure math/data helpers. Runtime build-mode changes must route through `RoadBuildCommandCompositionSystemHelper` or an ECS command/request path.
- Do not use reflection.

## Performance Rules

- Preserve road grid size, chunk size, road-cell projection behavior, runtime-city road generation behavior, and current road placement/session semantics.
- Do not add per-frame managed allocations to road input, preview, visual refresh, grid projection, or runtime-city road generation paths.
- Keep road mutation and visual refresh work proportional to dirty cells/chunks.
- Do not introduce LINQ, reflection, boxed delegates, or string formatting in hot road update paths.
- Reacquire ECS buffers safely after structural changes; do not keep invalid `DynamicBuffer` handles across entity mutations.

## Required Validation Gates

Every implementation step must run:

- `git diff --check` scoped to touched files.
- Focused road architecture validation once the new tests exist.

Every phase boundary must also run the existing road validation set when feasible:

- `GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation`.
- Runtime-city smoke, because runtime city consumes road generation.
- Building placement validation, because building placement consumes road footprint queries.
- Bootstrap/menu play-button smoke, because road commands and delete prompt are wired through startup.
- Runtime FPS probe when a step touches runtime update, visual refresh, grid projection, or runtime-city road generation.

## Phase 1: Baseline, Contract, And Surface Freeze

1. Complete: Add roadmap and baseline architecture guard
   - Add this document.
   - Add architecture contract wording that `RoadBuildRuntimeStateSystem.cs` is temporary and must be deleted.
   - Add focused architecture tests for baseline line count, roadmap tracking, forbidden broad replacement names, and no new static road commands.
   - Expected output: future changes cannot grow or normalize the temporary state holder.
   - Added `GameplayArchitectureContractTests.RunRoadBuildRuntimeStateArchitectureBatchValidation`.
   - Added guards for fixed 34-step roadmap tracking, 1351-line baseline, final deletion target, forbidden broad replacement shells, and no new public static runtime commands.
   - Updated `gameplay_solid_ecs_contract.md` with the RoadBuildRuntimeStateSystem deletion target and broad-shell replacement ban.

2. Complete: Freeze public/internal surface inventory
   - Inventory every public/internal member on `RoadBuildRuntimeStateSystem`.
   - Assign each member to a target owner listed in this roadmap.
   - Add a guard preventing new public/internal members from being added to the temporary holder.
   - Expected output: later steps retire named surface groups, not random line ranges.
   - Added the Public/Internal Surface Inventory Freeze section above.
   - Added `RoadBuildRuntimeStatePublicSurfaceMustOnlyShrink` to the focused architecture batch so the temporary holder can shrink but cannot grow new facade surface.

3. Complete: Create `RoadBuildCompositionSourceSystem`
   - Move child-system construction and persistent child-system fields out of `RoadBuildRuntimeStateSystem`.
   - Own the explicit graph of narrow road systems and their persistent state objects.
   - Do not add gameplay policy here; it is construction/wiring only.
   - Expected output: the temporary state holder no longer decides which road systems exist.
   - Added `RoadBuildCompositionSourceSystem` as the owner for road child-system construction and persistent state objects.
   - `RoadBuildCompositionSystemHelper` now creates the source and passes it into `RoadBuildRuntimeStateSystem`.
   - `RoadBuildRuntimeStateSystem` now reads child systems through the source and no longer constructs those child systems directly.
   - Added `RoadBuildCompositionSourceMustOwnChildSystemConstruction` to the focused architecture batch.

4. Complete: Migrate `RoadBuildCompositionSystemHelper.Result` off broad `RoadState`
   - Expose narrow systems, contexts, actions, and bind hooks directly from composition.
   - Keep the runtime-state bridge only inside composition while call sites migrate.
   - Expected output: no peer system needs a `RoadBuildRuntimeStateSystem` reference.
   - Removed `RoadBuildCompositionSystemHelper.Result.RoadState` from the returned result.
   - `RoadBuildCompositionSystemHelper` now keeps the temporary `RoadBuildRuntimeStateSystem` in private `_roadState` only for its own bind methods.
   - Added `RoadBuildCompositionResultMustNotExposeRoadState` to the focused architecture batch.

## Phase 2: Startup, Dependencies, And Read Model

5. Complete: Extract road startup/config application
   - Create or extend `RoadBuildStartupSystem`.
   - Move config snapshot application, camera/config validation, root creation, variant cache warmup, and startup sequencing out of `RoadBuildRuntimeStateSystem`.
   - Keep serialized config asset names unchanged.
   - Expected output: runtime state holder has no serialized config or copied config fields.
   - Added `RoadBuildStartupSystem` as the owner for road startup state, config snapshot application, runtime-root creation, and variant cache warmup.
   - `RoadBuildRuntimeStateSystem` no longer stores serialized config/camera/prefab/grid/placement-color fields or config projection methods; it reads startup data through `_startupState`.
   - Added `RoadBuildStartupConfigMustLiveInStartupSystem` to the focused architecture batch.

6. Complete: Extract road dependency binding
   - Create `RoadBuildDependencyCompositionSystemHelper`.
   - Own current building interaction dependency, building interaction context, main menu/minimap dependency, runtime grid blocker dependency, and dependency rebinding.
   - Expected output: `BindDependencies`, minimap configuration, and runtime blocker storage leave the temporary holder.
   - Added `RoadBuildDependencyCompositionSystemHelper` as the owner for road dependency state and dependency rebinding.
   - `RoadBuildRuntimeStateSystem` now delegates initial building-interaction binding and later menu/runtime blocker rebinding through the dependency boundary.
   - Added `RoadBuildDependenciesMustLiveInDependencySystem` to the focused architecture batch.

7. Complete: Move road read-model predicates to `RoadBuildReadModelCompositionSystemHelper`
   - Move active road mode, dragging interaction, pending building placement, selected building, confirm placement, placement status text, selected label, and active mode status text reads.
   - Use explicit dependency/read contexts instead of broad holder getters.
   - Expected output: UI/startup reads are fully supplied by the read model boundary.
   - `RoadBuildReadModelCompositionSystemHelper` now owns active road mode, dragging interaction, pending placement, selected building, confirm placement, placement status text, selected label, and active mode status text reads through an explicit context.
   - `RoadBuildCompositionSourceSystem` now owns the read-model instance; composition returns that instance instead of creating/configuring delegate wrappers around the temporary holder.
   - Removed the read predicate/text public surface from `RoadBuildRuntimeStateSystem`.
   - Added `RoadBuildReadModelPredicatesMustLiveInReadModelSystem` to the focused architecture batch.

8. Complete: Remove static `SetBuildMode` compatibility
   - Delete `RoadBuildRuntimeStateSystem.SetBuildMode`.
   - Route remaining build-mode command use through `RoadBuildCommandCompositionSystemHelper` or explicit command context.
   - Add a contract guard that static road commands cannot return.
   - Expected output: no static road runtime command surface remains.
   - Deleted `RoadBuildRuntimeStateSystem.SetBuildMode`.
   - Existing command use remains on `RoadBuildCommandCompositionSystemHelper.SetBuildMode` and explicit command contexts.
   - Updated architecture guards so `RoadBuildRuntimeStateSystem` must have zero public static runtime commands.

## Phase 3: Context Factories

9. Complete: Extract road visual context construction
   - Create `RoadBuildVisualContextSystem`.
   - Move prefab-set creation plus chunk visual, preview, and special visual context construction.
   - Expected output: visual systems receive explicit context from a visual context boundary, not from the temporary holder.

10. Complete: Extract road session/input/command/delete context construction
   - Create `RoadBuildInteractionContextSystem`.
   - Move session, input, command, and delete-prompt context construction.
   - Keep callbacks explicit and point them to narrow owner systems.
   - Expected output: road interaction systems can run without `RoadBuildRuntimeStateSystem`.

11. Complete: Extract road runtime-generation context construction
   - Create `RoadRuntimeGenerationContextCompositionSystemHelper`.
   - Move road-cell-size query binding, deferred road ECS sync callbacks, stroke creation callback, and special visual context handoff.
   - Expected output: runtime city uses `RoadRuntimeGenerationSystem` plus context without touching the temporary holder.

12. Complete: Extract footprint/grid-projection context construction
   - Create `RoadGridContextSystem`.
   - Move footprint query context and grid projection context construction.
   - Expected output: building placement and projection systems can consume context directly from composition.

13. Complete: Extract legacy building context construction
   - Move `BuildingRoadLegacyEcsSystem.Context` construction to a building-road legacy owner.
   - Keep entity manager/grid/footprint callbacks explicit.
   - Expected output: legacy building ECS behavior is no longer wired by road runtime state.

## Phase 4: Road Mutation, Refresh, And Rebuild

14. Complete: Extract road stroke mutation bridge
   - Create `RoadBuildMutationCompositionSystemHelper`.
   - Move `CreateStroke`, `DeleteStroke`, dirty-cell refresh triggering, and network snapshot restore handoff.
   - Use `RoadNetworkCompositionSystemHelper` for graph mutation and `RoadVisualRefreshSystem` for visual/ECS refresh.
   - Expected output: stroke mutation no longer lives beside startup and legacy building behavior.

15. Complete: Extract visual type resolution
   - Create `RoadVisualResolutionSystem`.
   - Move road visual type resolution, prefab lookup handoff, and variant lookup handoff.
   - Preserve current visual-type rules exactly.
   - Expected output: visual resolution is testable and not tied to runtime state.

16. Complete: Extract dirty-cell visual refresh
   - Create `RoadVisualRefreshSystem`.
   - Move `RefreshCells`, `RefreshCell`, chunk dirtying, road tile updates/removal, ECS sync requests, and special-road rebuild triggers.
   - Preserve dirty-cell/chunk behavior and avoid full rebuilds except rollback/full restore.
   - Expected output: road graph changes refresh visuals and ECS through a narrow visual refresh boundary.

17. Complete: Extract road state rebuild/rollback refresh
   - Move `RebuildRoadStateFromCurrentTiles`, special road metadata rebuild, chunk clear/re-add, ECS full sync, and special visual full rebuild into `RoadVisualRefreshSystem` or a narrow `RoadBuildRollbackSystem`.
   - Expected output: session restore no longer calls back into broad runtime state.

18. Complete: Move deferred road ECS sync wrappers out
   - Move `BeginDeferredRoadEcsSync`, `EndDeferredRoadEcsSync`, and internal sync callbacks into `RoadGridProjectionSystem` / `RoadRuntimeGenerationContextCompositionSystemHelper`.
   - Expected output: runtime city no longer reaches through temporary state for deferred sync.

## Phase 5: Legacy Building Compatibility Removal

19. Complete: Move soldier-base definition and bounds cache
   - Move `BuildDefinitions`, `CacheBuildingBounds`, and `TryGetLocalBounds` into `BuildingRoadLegacyDefinitionSystem`.
   - Keep this as compatibility only; do not add new building gameplay here.
   - Expected output: road runtime state no longer owns building definitions or prefab bounds.

20. Complete: Move placement outline visuals
   - Create `BuildingRoadLegacyPlacementVisualSystem`.
   - Move outline GameObject creation, material creation, outline positioning, color application, and hide/dispose behavior.
   - Expected output: visual-only legacy building placement code is isolated.
   - Added `BuildingRoadLegacyPlacementVisualSystem` as the owner for placement outline GameObject creation, material setup, positioning, color updates, hide, and dispose behavior.
   - `RoadBuildRuntimeStateSystem` now delegates legacy placement outline visuals through this boundary and no longer stores outline GameObject/render state or rendering material code.

21. Complete: Move legacy building placement lifecycle
   - Create `BuildingRoadLegacyPlacementSystem`.
   - Move begin/cancel placement, center-screen origin selection, pointer placement update, validity evaluation, and placement preview positioning.
   - Preserve fallback behavior only for compatibility when `BuildingPlacementInteractionBoundaryCompositionSystemHelper` is absent.
   - Expected output: road runtime state no longer owns building placement lifecycle.
   - Added `BuildingRoadLegacyPlacementSystem` as the owner for legacy placement drag state, begin/cancel placement, center-screen origin selection, pointer placement updates, validity checks, and preview positioning.
   - `RoadBuildRuntimeStateSystem` now delegates those lifecycle operations through an explicit context; building commit, selection, and delete remain for step 22.

22. Complete: Move legacy building commit and selection
   - Create `BuildingRoadLegacyInteractionSystem`.
   - Move commit placement, building selection hit-test, select, delete, selected label fallback, and selected-building state mutation.
   - Expected output: legacy building selection/commit/delete no longer lives in road runtime state.
   - Added `BuildingRoadLegacyInteractionSystem` as the owner for legacy placement commit, runtime building add/release, selection hit-tests, selection mutation, and delete behavior.
   - `RoadBuildRuntimeStateSystem` no longer carries the legacy `PlaceBuilding`, selection hit-test, select, or delete methods.

23. Complete: Move legacy building ECS/global access
   - Move direct `World.DefaultGameObjectInjectionWorld`, `EntityManager`, grid query, blocker data, combat/blocker entity cleanup, and grid-cell raycast helpers into legacy building/grid owner systems.
   - Expected output: `RoadBuildRuntimeStateSystem` has no direct `World`, `EntityManager`, `EntityQuery`, `DynamicBuffer<GridRoad>`, or `DynamicBlockerData` usage.
   - Added `BuildingRoadLegacyGridSystem` as the owner for legacy grid-data access, grid config lookup, footprint-center calculation, and grid-cell raycast helpers.
   - `BuildingRoadLegacyEcsSystem` now owns default-world entity-manager access and runtime building entity/visual disposal.
   - `RoadBuildRuntimeStateSystem` no longer directly references `World.DefaultGameObjectInjectionWorld`, `EntityManager`, `EntityQuery`, `DynamicBuffer<GridRoad>`, or `DynamicBlockerData`.

24. Complete: Delete legacy building command wrappers from road state
   - Remove `BeginSoldierBasePlacement`, `ConfirmBuildingPlacement`, `CancelBuildingPlacement`, `CreateSoldierFromSelectedBuilding`, `DeleteSelectedBuilding`, `ClearSelectedBuilding`, `CanConfirmBuildingPlacement`, and `HasSelectedBuilding` from the temporary road holder.
   - Route production callers through `BuildingPlacementInteractionBoundaryCompositionSystemHelper` or the legacy compatibility owner directly.
   - Expected output: road runtime state has no building gameplay command surface.
   - Removed the public legacy building command wrappers from `RoadBuildRuntimeStateSystem`.
   - Road interaction cancel/clear callbacks now call `BuildingPlacementInteractionBoundaryCompositionSystemHelper` through explicit callbacks instead of keeping road-owned public command surface.

## Phase 6: Runtime Actions And Disposal

25. Complete: Move runtime update action out
   - Have `RoadBuildCompositionSystemHelper` call `RoadBuildInputCompositionSystemHelper.Update` through `RoadBuildInteractionContextSystem` output, not through `RoadBuildRuntimeStateSystem.RoadBuildInputCompositionSystemHelper`.
   - Expected output: runtime update action does not touch the temporary holder.
   - Added `RoadBuildRuntimeActionCompositionSystemHelper` as the narrow runtime-update action owner.
   - `RoadBuildCompositionSystemHelper.Result.RuntimeUpdate` now invokes `RoadBuildRuntimeActionCompositionSystemHelper.Update` through composition source state instead of reading `RoadBuildInputCompositionSystemHelper`, `RoadBuildInputContext`, or camera through `RoadBuildRuntimeStateSystem`.

26. Complete: Move GUI action out
   - Have `RoadBuildCompositionSystemHelper` call `RoadDeletePromptUiSystemHelper.OnGui` through `RoadBuildInteractionContextSystem` output, not through `RoadBuildRuntimeStateSystem.RoadDeletePromptUiSystemHelper`.
   - Expected output: GUI action does not touch the temporary holder.
   - Extended `RoadBuildRuntimeActionCompositionSystemHelper` to own the delete-prompt GUI action.
   - `RoadBuildCompositionSystemHelper.Result.OnGui` now invokes `RoadBuildRuntimeActionCompositionSystemHelper.OnGui` through composition source state instead of reading `RoadDeletePromptUiSystemHelper` or context through `RoadBuildRuntimeStateSystem`.

27. Complete: Extract road disposal sequencing
   - Create `RoadBuildDisposalCompositionSystemHelper`.
   - Move root disposal, preview disposal, chunk disposal, special visual disposal, cached visual data disposal, minimap event clear, ECS road clear, legacy building cleanup, and storage clear.
   - Expected output: cleanup is explicit and can be invoked without broad runtime state.
   - Added `RoadBuildDisposalCompositionSystemHelper` as the owner for teardown sequencing across runtime roots, placement outline visuals, variant cache, preview, chunks, legacy building entities/visuals, special visuals, minimap events, ECS road data, road tiles, and legacy storage.
   - `RoadBuildRuntimeStateSystem.Dispose` now only exits build mode, resets skip-click session state, and delegates cleanup sequencing through `RoadBuildDisposalCompositionSystemHelper`.

28. Complete: Move command/public API consumers to narrow systems
   - Migrate any remaining production/test calls to `ActivateRoadBuildMode`, `ConfirmRoadBuildSession`, `CancelRoadBuildSession`, `ExitBuildMode`, road generation wrappers, footprint wrappers, update/gui, and dispose.
   - Expected output: `rg "RoadBuildRuntimeStateSystem" Assets/Game/Scripts -g '*.cs'` finds only the file being retired and temporary composition construction until deletion.
   - Removed the remaining public road-generation, footprint-query, runtime update, GUI, and road-command wrapper methods from `RoadBuildRuntimeStateSystem`.
   - Runtime generation and footprint consumers now receive `RoadRuntimeGenerationSystem`, `RoadRuntimeGenerationSystem.Context`, `RoadFootprintQuerySystem`, and `RoadFootprintQuerySystem.Context` from `RoadBuildCompositionSystemHelper.Result`.
   - Runtime update and GUI consumers now use `RoadBuildRuntimeActionCompositionSystemHelper` through composition, not `RoadBuildRuntimeStateSystem.Update` or `OnGui`.
   - The temporary holder keeps only startup/bind/disposal compatibility and internal context creation needed for steps 29-30.

29. Complete: Convert temporary holder to empty adapter or skip directly to deletion
   - If references remain after step 28, reduce `RoadBuildRuntimeStateSystem` to a no-behavior adapter for one step only.
   - If no references remain, skip the adapter and proceed to deletion.
   - Expected output: deletion blockers are mechanical and listed.
   - Added `RoadBuildCompositionContextCompositionSystemHelper` for remaining context construction and `RoadBuildCompositionLifecycleCompositionSystemHelper` for startup/bind/dispose sequencing.
   - Moved persistent startup/dependency/legacy placement state ownership into `RoadBuildCompositionSourceSystem`.
   - Reduced `RoadBuildRuntimeStateSystem` from 557 lines to a 58-line no-behavior adapter that only delegates to composition context/lifecycle boundaries.
   - Remaining deletion blockers are mechanical: `RoadBuildCompositionSystemHelper` still constructs the adapter, focused architecture tests still temporarily allow/expect the adapter, and documentation still records temporary holder debt until steps 30-31.

## Phase 7: Delete Temporary Holder And Remove Debt

30. Complete: Delete `RoadBuildRuntimeStateSystem`
   - Delete `Assets/Game/Scripts/Systems/RoadBuildRuntimeStateSystem.cs` and `.meta`.
   - Fix remaining compile references by routing to narrow systems.
   - Expected output: no source file named `RoadBuildRuntimeStateSystem`.
   - Deleted `Assets/Game/Scripts/Systems/RoadBuildRuntimeStateSystem.cs` and `.meta`.
   - `RoadBuildCompositionSystemHelper` now wires directly to `RoadBuildCompositionSourceSystem`, `RoadBuildCompositionContextCompositionSystemHelper`, and `RoadBuildCompositionLifecycleCompositionSystemHelper`.
   - Runtime generation, footprint query, runtime update, GUI, bind, and dispose paths no longer route through a temporary adapter.

31. Complete: Remove architecture debt allowances
   - Remove tests/contract wording that temporarily allow construction of `RoadBuildRuntimeStateSystem`.
   - Update `road_build_system_refactor_roadmap.md` to say the temporary holder is retired.
   - Add hard guard: `RoadBuildRuntimeStateSystem.cs` must not exist.
   - Expected output: architecture tests fail if the temporary holder returns.
   - Updated `gameplay_solid_ecs_contract.md` and `road_build_system_refactor_roadmap.md` so `RoadBuildRuntimeStateSystem.cs` is forbidden, not temporary debt.
   - Updated focused architecture tests to read the source/context/lifecycle road boundary and to assert the deleted holder file stays absent.

32. Complete: Remove composition `RoadState` exposure
   - Delete `RoadBuildCompositionSystemHelper.Result.RoadState`.
   - Ensure composition exposes only narrow systems, contexts, actions, bind hooks, and disposal action.
   - Expected output: no broad road state field remains in startup composition.
   - Completed early in step 4: `RoadBuildCompositionSystemHelper.Result` no longer exposes `RoadState`; step 30 removed the private temporary bridge as well.

33. Complete: Update documentation and handoff
   - Update `gameplay_solid_ecs_contract.md`, `road_build_system_refactor_roadmap.md`, and this roadmap with final ownership.
   - Write a WarlineCapture handoff report under `Design/AgentReports`.
   - Expected output: PM/user can see what moved, validation run, known gaps, and next recommended task.
   - Updated `gameplay_solid_ecs_contract.md`, `road_build_system_refactor_roadmap.md`, and this roadmap to record that the temporary holder is deleted.
   - Wrote `Design/AgentReports/2026-05-27_gameplay_road_runtime_state_step33_holder_deleted.md`.

34. Complete: Validation gate
   - Run focused road runtime-state architecture validation.
   - Run existing road-build architecture validation.
   - Run runtime-city smoke.
   - Run building placement validation.
   - Run bootstrap/menu play-button smoke.
   - Run runtime FPS probe if runtime update, visual refresh, or grid projection changed during the final batch.
   - Expected result: compile clean, `RoadBuildSystem.cs` and `RoadBuildRuntimeStateSystem.cs` deleted, no broad road shell remains, runtime city still creates roads, building placement still respects road footprints, road build mode still creates/deletes/rolls back roads, and no new road/runtime-city performance regression appears.
   - Focused road runtime-state architecture validation passed: `[RoadBuildRuntimeStateArchitectureValidation] result=Passed methods=29`.
   - Existing road-build architecture validation passed: `[RoadBuildArchitectureValidation] result=Passed methods=31`.
   - Runtime-city architecture validation passed: `[RuntimeCityArchitectureValidation] result=Passed methods=28`.
   - Runtime-city Game scene smoke passed: cityPrefabs=36, productionCityCount=1, validationCityCount=1, buildingSpawnables=32, blockerPrefabs=63.
   - Building placement validation passed 4/4.
   - Building runtime boundary validation passed 1/1.
   - Bootstrap/menu play-mode smoke passed 7/7.
   - Runtime FPS play-button probe completed; RoadBuild runtime cost stayed low after startup. The probe captured startup hitches in BuildingPlacement/RuntimeCity and one Unity QuickSearch indexing exception, which are not introduced road-shell ownership failures.
   - `git diff --check` passed for touched road scripts, tests, and docs.

## Progress Notes

- Step 1 complete: roadmap, contract wording, baseline line-count guard, broad replacement guard, and focused architecture batch are in place.
- Step 2 complete: public/internal temporary surface is inventoried, assigned to target owners, and guarded so it can only shrink.
- Step 3 complete: road child-system construction moved to `RoadBuildCompositionSourceSystem`; the temporary holder now consumes a source object instead of constructing child systems directly.
- Step 4 complete: `RoadBuildCompositionSystemHelper.Result` no longer exposes `RoadState`; only composition keeps a private temporary `_roadState` bridge for bind methods.
- Step 5 complete: road startup/config application moved to `RoadBuildStartupSystem`; the temporary holder no longer owns serialized config/cache fields, config projection, root creation, or variant-cache warmup.
- Step 6 complete: road dependency storage and rebinding moved to `RoadBuildDependencyCompositionSystemHelper`; the temporary holder no longer stores building interaction, menu, minimap, or runtime-grid blocker dependencies directly.
- Step 7 complete: road read predicates and labels moved to `RoadBuildReadModelCompositionSystemHelper`; composition now returns the source-owned read model instead of configuring facade getter delegates.
- Step 8 complete: deleted the temporary static `RoadBuildRuntimeStateSystem.SetBuildMode` bridge; runtime build-mode changes must stay on `RoadBuildCommandCompositionSystemHelper`/explicit command contexts.
- Step 9 complete: chunk, preview, and special-road visual context construction moved to `RoadBuildVisualContextSystem`; visual behavior remains in the existing visual systems.
- Step 10 complete: session, input, command, and delete-prompt context construction moved to `RoadBuildInteractionContextSystem`; callbacks remain explicit and narrow.
- Step 11 complete: runtime road-generation context construction moved to `RoadRuntimeGenerationContextCompositionSystemHelper`; road-cell-size, deferred sync, stroke creation, and special visual handoff remain explicit callbacks.
- Step 12 complete: footprint query and grid projection context construction moved to `RoadGridContextSystem`; projection behavior and road grid sizing are unchanged.
- Step 13 complete: legacy building ECS context construction moved to `BuildingRoadLegacyContextSystem`; entity manager, grid, footprint, interaction, and spawn-random callbacks remain explicit.
- Step 14 complete: road stroke creation/deletion plus session snapshot capture/restore mutation moved to `RoadBuildMutationCompositionSystemHelper`; runtime state only supplies refresh/rebuild callbacks pending steps 16-17.
- Step 15 complete: visual type resolution plus prefab/variant lookup handoff moved to `RoadVisualResolutionSystem`; visual-type rules are unchanged.
- Step 16 complete: dirty-cell road tile refresh, chunk dirtying, ECS sync request, and special-road dirty rebuild trigger moved to `RoadVisualRefreshSystem`.
- Step 17 complete: full road state rollback/rebuild refresh moved to `RoadVisualRefreshSystem`; snapshot restore now triggers the visual refresh boundary instead of broad runtime-state rebuild code.
- Step 18 complete: deferred road ECS sync begin/end callbacks moved to `RoadRuntimeGenerationContextCompositionSystemHelper`, which now calls `RoadGridProjectionSystem` directly.
- Step 19 complete: soldier-base definition construction and prefab local-bounds caching moved to `BuildingRoadLegacyDefinitionSystem`.
- Step 29 complete: remaining context construction moved to `RoadBuildCompositionContextCompositionSystemHelper`, startup/bind/dispose sequencing moved to `RoadBuildCompositionLifecycleCompositionSystemHelper`, persistent state moved into `RoadBuildCompositionSourceSystem`, and `RoadBuildRuntimeStateSystem` is now only a 58-line delegating adapter. Step 30 can delete the adapter file once composition and tests stop referencing it.
- Step 30 complete: `RoadBuildRuntimeStateSystem.cs` and `.meta` were deleted. `RoadBuildCompositionSystemHelper` now consumes source/context/lifecycle systems directly, so production code has no temporary road-runtime holder reference.
- Step 31 complete: architecture contract, road-build roadmap, and focused tests now treat `RoadBuildRuntimeStateSystem.cs` as forbidden deleted debt rather than temporary compatibility debt.
- Step 33 complete: documentation and WarlineCapture handoff report were updated for the deleted road runtime-state holder.
- Step numbers are fixed. If new work is discovered, update the relevant existing step instead of adding step 35+.
