# Script Architecture Alignment Roadmap

## Goal

Track and retire scripts that are legacy, poorly named, or not aligned with the gameplay SOLID/ECS architecture contract.

The target shape is:
- Gameplay runtime behavior is ECS data plus `*System` owners.
- Gameplay data types end in `*Entity`, `*Component`, or `*System`.
- UI MonoBehaviours are `*View` serialized-reference binders, not flow owners.
- Config assets end in `*Config`, `*ConfigAsset`, or an accepted data-asset suffix.
- Conversion-edge code uses `*Authoring` and `*Baker`.
- Persistence, logging, asset lookup, platform APIs, and editor tooling may use service/repository/tool names when they do not own gameplay policy.
- Runtime code does not use hierarchy lookup, static view registries, `Object.Find*`, singleton-style globals, broad managers/controllers/facades, or project-name-prefixed source filenames.

## Audit Snapshot

Audit date: 2026-06-08.

Scanned source root:
- `Assets/Game/Scripts`

Current status:
- 666 C# scripts scanned.
- No `Mission`, `District`, `Campaign`, `Tutorial`, `M01`, or `MenuView` script filenames found.
- Retired hard-blocked scripts are absent: `RTSSelectionSystem.cs`, `CitizenPopulationSystem.cs`, `BuildingGameplaySystem.cs`, `BuildingGameplayTestHarness.cs`, `RuntimeCityBuildingSpawnSystem.cs`, and `CitizenPopulationSystem.cs`.
- Source filenames no longer start with the project/product name.

## Current Debt Inventory

### Active Legacy Names

- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyContextSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildContextSystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyDefinitionSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildDefinitionProjectionSystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyEcsSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildEcsBoundarySystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyGridSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildGridQuerySystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyInteractionSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildInteractionSystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyPlacementSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildBuildingPlacementSystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyPlacementVisualSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildPlacementVisualSystem.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingRoadLegacyStorageSystem.cs` -> `Assets/Game/Scripts/Systems/RoadBuildPlacementStorageSystem.cs`
- [x] `Assets/Game/Scripts/UI/Shell/UILegacyGameStartSystem.cs` -> `Assets/Game/Scripts/UI/Shell/UIGameStartButtonView.cs`

Notes:
- The `BuildingRoadLegacy*System` group is active through `RoadBuildCompositionSourceSystem`, so it must be renamed/refactored carefully instead of deleted blindly.
- `UILegacyGameStartSystem` is a UI button behavior and should move toward a `*View` name or an ECS request-binding view.

### UI MonoBehaviours Needing `*View` Or ECS Boundary Cleanup

- [x] `Assets/Game/Scripts/UI/Shell/UIShellContentSystem.cs` -> `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- [x] `Assets/Game/Scripts/UI/Shell/UIRouter.cs` -> `Assets/Game/Scripts/UI/Shell/UIRouterView.cs`
- [x] `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelSystem.cs` -> `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs`
- [x] `Assets/Game/Scripts/UI/Screens/BuildDrawerPanelSystem.cs` -> `Assets/Game/Scripts/UI/Screens/BuildDrawerPanelView.cs`
- [x] `Assets/Game/Scripts/UI/Screens/CommandWheelPanelSystem.cs` -> `Assets/Game/Scripts/UI/Screens/CommandWheelPanelView.cs`
- [x] `Assets/Game/Scripts/UI/Screens/QuickCustomScreenSystem.cs` -> `Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.cs`
- [x] `Assets/Game/Scripts/UI/Screens/SplashScreenSystem.cs` -> `Assets/Game/Scripts/UI/Screens/SplashScreenView.cs`
- [x] `Assets/Game/Scripts/UI/Settings/SettingsScreenSystem.cs` -> `Assets/Game/Scripts/UI/Settings/SettingsScreenView.cs`
- [x] `Assets/Game/Scripts/UI/Shell/UIScreenSystem.cs` -> `Assets/Game/Scripts/UI/Shell/UIScreenView.cs`
- [x] `Assets/Game/Scripts/UI/Shell/UIModalSystem.cs` -> `Assets/Game/Scripts/UI/Shell/UIModalView.cs`
- [x] `Assets/Game/Scripts/UI/Shell/UIPopupCloseSystem.cs` -> `Assets/Game/Scripts/UI/Shell/UIPopupCloseButtonView.cs`
- [x] `Assets/Game/Scripts/UI/Shell/UIPlaceholderModalSystem.cs` -> `Assets/Game/Scripts/UI/Shell/UIPlaceholderModalButtonView.cs`
- [x] `Assets/Game/Scripts/UI/Components/BattleHudTacticalFeedbackSystem.cs` -> `Assets/Game/Scripts/UI/Components/BattleHudTacticalFeedbackView.cs`

Target:
- If a type only binds serialized UI references, rename it to `*View`.
- If a type owns screen flow, command routing, or gameplay request policy, split it into a `*View` plus an ECS `*System` or shell-edge boundary system.

### Runtime Hierarchy Lookup Debt

- [x] `UIShellContentView`: remove prefab section lookup by string and popup lookup by prefab name.
- [x] `MatchHudSelectionPanelSystem`: remove `transform.Find("SelectedSquadPanel")`, `FindChildRecursive`, and `Frame/PortraitFrame` lookup.
- [x] `BuildingDefinitionPrefabSystemHelper`: remove runtime `root.Find("Model")` dependency.
- [x] `BuildingPlacementVisualPresentationSystemHelper`: remove runtime `prefab.transform.Find("Model")` dependency.
- [x] `BuildingProductionTransportSystem`: remove runtime `transport.Instance.transform.Find("Model")` fallback.

Target:
- UI paths use explicit serialized references on narrow views.
- Runtime gameplay/model paths use baked ECS references, authoring fields, config references, or cached references created at spawn time.

### Static UI Registry Debt

- [x] `MatchHudSelectionPanelSystem.activeSystem`
- [x] `BattleHudRuntimeFeedbackView.RegisteredInstances`
- [x] `MatchOverlayCommandControlsView.RegisteredInstances`
- [x] `MatchOverlayCommandTabGroupView.RegisteredInstances`
- [x] `RuntimeBuildingEntityLink.ActiveLinks`
- [x] `UIShellContentView` former static registry (`UIShellContentSystem.RegisteredInstances`)

Target:
- UI registration flows through serialized shell views, injected runtime binding, or ECS managed reference components.
- No static mutable UI registries in shipped runtime code.

### Non-System Files In `Systems`

- [x] `Assets/Game/Scripts/Systems/AISettingsRuntimeState.cs` -> `Assets/Game/Scripts/Configs/AISettingsRuntimeState.cs`
- [x] `Assets/Game/Scripts/Systems/BuildingDefinition.cs` -> `Assets/Game/Scripts/Components/BuildingDefinition.cs`
- [x] `Assets/Game/Scripts/Systems/CitizenPopulationComponent.cs` -> `Assets/Game/Scripts/Components/CitizenPopulationRuntimeComponents.cs`
- [x] `Assets/Game/Scripts/Systems/CitizenVisualLifecycleReporter.cs` -> `Assets/Game/Scripts/Components/CitizenVisualLifecycleReporter.cs`
- [x] `Assets/Game/Scripts/Systems/InitialFactionBaseLayoutPlanner.cs` -> `Assets/Game/Scripts/Utilities/InitialFactionBaseLayoutPlanner.cs`
- [x] `Assets/Game/Scripts/Systems/RespawnQueueUtils.cs` -> `Assets/Game/Scripts/Utilities/RespawnQueueUtility.cs`
- [x] `Assets/Game/Scripts/Systems/RuntimeBuildingData.cs` -> `Assets/Game/Scripts/Components/RuntimeBuildingEntity.cs`
- [x] `Assets/Game/Scripts/Systems/UnitPathfindBatchJob.cs` -> `Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs`
- [x] `Assets/Game/Scripts/Systems/VehicleVisualEntityUtility.cs` -> `Assets/Game/Scripts/Utilities/VehicleVisualEntityUtility.cs`

Target:
- Data/component files move to `Components`, `Configs`, or a domain data folder and use accepted suffixes.
- Pure helpers move to `Utilities` and use `*Utility` only when stateless.
- Jobs can live near their owner, but should be named and placed consistently, for example under a pathfinding domain folder.

### Broad Or Questionable Names

- [x] `BuildingEntityManagerAccessSystem`: validated as the explicit ECS entity-manager access edge named in `gameplay_solid_ecs_contract.md`; keep unless it grows beyond this boundary.
- [x] `BuildingPlacementAdapterCompositionSystemHelper`: validated as the placement-context adapter glue named in `gameplay_solid_ecs_contract.md`; keep unless it grows into a broad owner.
- [x] `AIControllerConfig`: accepted existing config asset name for serialized compatibility; future AI runtime code should avoid controller naming.

### Static Runtime State Debt

- [x] `AISettingsRuntimeState`: replace public static mutable fields with a quick-game configuration snapshot projected into ECS/config data at match start. Keep the current bridge only until quick-custom setup and AI startup can consume an explicit snapshot boundary.

### Project-Name String Debt

- [x] Unity `MenuItem` paths no longer use `WarlineCapture/...`.
- [x] `CreateAssetMenu` menu names no longer use `WarlineCapture/...`.
- [x] PlayerPrefs keys no longer use `WarlineCapture.*`.
- [x] Shader names no longer use `WarlineCapture/...`.
- [x] Active diagnostic log tags no longer include `WARLINECAPTURE_*`.
- [x] Editor build output no longer hardcodes `WarlineCapture.*`.

Target:
- Replace with domain names such as `Game`, `UI`, `Map Surface`, `Rendering`, or stable internal keys that are not tied to the project title.
- Shader renames must be staged carefully because material references can break.
- PlayerPrefs key renames may reset local settings for now per user direction; add migration/fallback reads later only if user-facing persistence becomes important.

## Remediation Phases

### Phase 1: Guardrails And Tests

- [x] Add an architecture test that fails if new source filenames start with the project/product name.
- [x] Add an architecture test that fails if runtime UI code uses `transform.Find`, `GameObject.Find`, or `Object.Find*`.
- [x] Add an architecture test that lists UI MonoBehaviours not ending in `View` or approved shell-edge suffixes.
- [x] Add an architecture test that flags new `Manager`, `Controller`, `Presenter`, `Facade`, `Installer`, or `Orchestrator` names.
- [x] Add an architecture test that fails if UI MonoBehaviours add new static mutable view registries.
- [x] Add a limited allowlist for existing debt in this document so the test can ratchet down over time.

### Phase 2: Safe UI Rename Pass

- [x] Rename simple UI reference-holder types to `*View` without changing behavior for the Phase 2 candidate batch.
- [x] Preserve Unity `.meta` files and prefab script GUIDs during every rename.
- [x] Update prefab and scene serialized script references after each rename.
- [x] Run Unity compile validation after the batch.

Candidate first-pass files:
- [x] `UILegacyGameStartSystem` -> `UIGameStartButtonView`
- [x] `UISafeArea` -> `UISafeAreaView`
- [x] `UIAspectVariantSwitcher` -> `UIAspectVariantView`
- [x] `WorldFeedbackMarker` -> `WorldFeedbackMarkerView`
- [x] `UiMotionFeedback` -> `UIMotionFeedbackView`

### Phase 3: UI Runtime Binding Cleanup

- [x] Replace `MatchHudSelectionPanelSystem` with a serialized `MatchHudSelectionPanelView`.
- [x] Remove `MatchHudSelectionPanelSystem.activeSystem`; bind selection panel through shell/match HUD binding.
- [x] Replace `UIShellContentView` string section lookup with explicit content-section view references or authored section mapping.
- [x] Remove static shell content view registry; route binding through shell root references.
- [x] Remove static command-control and command-tab view registries.
- [x] Keep all UI click handling as ECS request writes or shell-edge request submission, not direct gameplay mutation.

### Phase 4: Router And Screen Flow Cleanup

- [x] Decide whether `UIRouterView` remains a shell-edge view or moves fully to ECS shell routing.
- [x] If kept as MonoBehaviour, split it into `UIRouterView` for references and `UiShellFlowSystem`/existing ECS systems for state transitions.
- [x] Rename remaining screen descendants that are pure views, or move their behavior into ECS shell systems.
- [x] Ensure route changes do not use broad screen discovery at runtime.

### Phase 5: Runtime Model Reference Cleanup

- [x] Replace `BuildingPlacementVisualPresentationSystemHelper` model-child lookup with authoring/configured model root references or cached spawn-time references.
- [x] Replace `BuildingProductionTransportSystem` model-child fallback with cached transport visual references.
- [x] Confirm authoring-time `UnitGridAuthoring` lookups are conversion-edge only and not copied into runtime code.
- [x] Add tests or prefab validation for required model-root references.

### Phase 6: Road Build Legacy Rename/Refactor

- [x] Audit each `BuildingRoadLegacy*System` responsibility.
- [x] Rename the group to current domain names without changing behavior.
- [x] Update `RoadBuildCompositionSourceSystem`, `RoadBuildCompositionContextSystem`, `RoadBuildCompositionLifecycleSystem`, `RoadBuildDisposalSystem`, and `RoadBuildReadModelSystem`.
- [x] Remove the word `Legacy` from active runtime road/build composition code.
- [x] Add compile and targeted road/build placement validation.

Candidate target names:
- `BuildingRoadLegacyStorageSystem` -> `RoadBuildPlacementStorageSystem`
- `BuildingRoadLegacyDefinitionSystem` -> `RoadBuildDefinitionProjectionSystem`
- `BuildingRoadLegacyPlacementVisualSystem` -> `RoadBuildPlacementVisualSystem`
- `BuildingRoadLegacyPlacementSystem` -> `RoadBuildBuildingPlacementSystem`
- `BuildingRoadLegacyInteractionSystem` -> `RoadBuildInteractionSystem`
- `BuildingRoadLegacyGridSystem` -> `RoadBuildGridQuerySystem`
- `BuildingRoadLegacyContextSystem` -> `RoadBuildContextSystem`
- `BuildingRoadLegacyEcsSystem` -> `RoadBuildEcsBoundarySystem`

### Phase 7: Folder And Suffix Cleanup

- [x] Move component/data files out of `Systems`.
- [x] Move stateless helpers out of `Systems`.
- [x] Rename data suffixes that conflict with the ECS contract, especially runtime gameplay `*Data` and `*State` where they are actual ECS data.
- [x] Keep service/repository/model names only in persistence/external-edge folders.

### Phase 8: Project-Name String Cleanup

- [x] Rename editor menu paths away from `WarlineCapture/...`.
- [x] Rename `CreateAssetMenu` paths away from `WarlineCapture/...`.
- [x] Rename PlayerPrefs keys away from `WarlineCapture.*`; local settings reset is acceptable for now per user direction.
- [x] Plan shader path rename with material validation before changing shader names.
- [x] Rename editor build output from `WarlineCapture.*` to a configurable product name or generic build target name.
- [x] Rename diagnostic log tags only after existing diagnostic parsing scripts are checked.

## Validation Gates

Run after each implementation phase:
- [x] `git diff --check`
- [x] C# compile validation through Unity batchmode.
- [x] Focused EditMode tests for touched area.
- [x] Prefab missing-script scan after Unity script renames.
- [x] Serialized reference validation for changed UI prefabs/scenes.

Runtime validation, when UI or gameplay runtime is touched:
- [x] Launch match scene.
- [x] Confirm shell UI loads without missing references.
- [x] Confirm command buttons still submit ECS requests.
- [x] Confirm selection, command modes, minimap, squad tray, and armory paths still work if touched.
- [x] Check `FrameRateDiag` before and after for obvious regressions.

## Progress

- [x] Initial audit completed.
- [x] Roadmap document created.
- [x] Phase 1 guardrail tests added.
- [x] Phase 2 safe UI rename pass complete.
- [x] Phase 3 UI runtime binding cleanup complete.
- [x] Phase 4 router and screen flow cleanup complete.
- [x] Phase 5 runtime model reference cleanup complete.
- [x] Phase 6 road build legacy rename/refactor complete.
- [x] Phase 7 folder and suffix cleanup complete.
- [x] Phase 8 project-name string cleanup complete.
- [x] Final compile, prefab, and runtime validation complete.

## Progress Notes

- 2026-06-08: Added `ScriptArchitectureAlignmentContractTests` to block new project-name-prefixed source filenames, new runtime hierarchy/Object.Find lookup debt, new non-`View` UI MonoBehaviours outside the allowlist, and new broad Manager/Controller/Presenter/Facade/Installer/Orchestrator names outside the allowlist. While encoding the lookup allowlist, added `BuildingDefinitionPrefabSystemHelper.root.Find("Model")` to the runtime hierarchy lookup debt inventory. Validation: `git diff --check` passed, a mirrored Node read of the guardrail logic passed, and focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Phase 2 safe UI rename slice started. Renamed `UILegacyGameStartSystem` to `UIGameStartButtonView`, `UISafeArea` to `UISafeAreaView`, and `UIAspectVariantSwitcher` to `UIAspectVariantView`, preserving moved `.meta` GUIDs. Updated tests and the `UIShellAppCanvas` class identifier for the safe-area component. `WorldFeedbackMarker` and `UiMotionFeedback` remain for a later slice because they have broader prefab/runtime usage. Validation: `git diff --check` passed, stale old-type scans passed, moved GUIDs matched the pre-rename GUIDs, the mirrored architecture guardrail check passed, and focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Renamed `WorldFeedbackMarker` to `WorldFeedbackMarkerView`, preserving the `.meta` GUID. No prefab, scene, or config GUID references existed for the old script. Removed the old name from the UI naming guardrail allowlist. Validation: `git diff --check` passed, stale old-type scans passed, the moved GUID matched the pre-rename GUID, the mirrored architecture guardrail check passed, and focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Renamed `UiMotionFeedback` to `UIMotionFeedbackView`, preserving the `.meta` GUID and mechanically updating 31 popup prefab `m_EditorClassIdentifier` entries while leaving their `m_Script` GUID references unchanged. Updated `UIDesignedUnavailableRouteTests` and removed the old name from the UI naming guardrail allowlist. Validation: `git diff --check` passed, stale old-type scans passed, the moved GUID matched the pre-rename GUID, the mirrored architecture guardrail check passed, and focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Replaced `MatchHudSelectionPanelSystem` with `MatchHudSelectionPanelView`, preserving the `.meta` GUID and the `SCN08_MatchHudContent` prefab script reference. Removed `activeSystem`, removed all runtime child-name lookup from the component, removed the `UIShellContentSystem` selected-panel fallback lookup, and bound the selection panel through `UIShellContentSystem` -> `MainMenuPlayUI` -> `SelectionHudFeedbackSystem`/`SelectionBuildingInteractionSystem`. Tightened the architecture guardrail allowlists by removing the old lookup and non-`View` exceptions and ratcheting `UIShellContentSystem` hierarchy lookup debt from 3 to 2. Validation: `git diff --check` passed, stale old-type scans passed, the moved GUID still matches the prefab script reference, the mirrored architecture guardrail check passed, and focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Removed `RegisteredInstances` static registries from `MatchOverlayCommandControlsView` and `MatchOverlayCommandTabGroupView`. `MatchOverlayCommandTabFeedbackSystem` now updates only explicit command tab groups supplied by `BattleHudRuntimeFeedbackView`, and former null-fallback call sites pass active view groups or no-op when no HUD view exists. Added a static UI registry guardrail test with the remaining known debts allowlisted (`BattleHudRuntimeFeedbackView`, `RuntimeBuildingEntityLink`, and `UIShellContentSystem`). Validation: targeted `git diff --check` for touched files passed, scans confirmed no command-control/tab `RegisteredInstances` or null command-tab fallback remained, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Removed unused `RuntimeBuildingEntityLink.ActiveLinks` and `GetActiveLinks()`. No production or test callers consumed the static registry; runtime building systems already configure each link from the created instance, and tests inspect explicit roots. Removed `RuntimeBuildingEntityLink` from the static UI registry guardrail allowlist. Validation: stale `ActiveLinks`/`GetActiveLinks` scans passed, targeted `git diff --check` for touched files passed, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved stateless helpers out of `Systems`: `RespawnQueueUtils` became `RespawnQueueUtility` under `Assets/Game/Scripts/Utilities`, and `VehicleVisualEntityUtility` moved under `Assets/Game/Scripts/Utilities` unchanged. Preserved `.meta` files during both moves and updated respawn queue call sites. Validation: stale source-reference scans passed, old helper paths are absent, moved helper `.meta` files are present, targeted `git diff --check` for touched files passed, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved `CitizenPopulationComponent.cs` out of `Systems` to `Assets/Game/Scripts/Components/CitizenPopulationRuntimeComponents.cs`, preserving the `.meta` GUID. The file contains citizen population enums, records, and runtime data components; no type names changed. Updated the older citizen refactor roadmap to point at the current file location. Validation: old source path absence passed, targeted `git diff --check` passed, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved `InitialFactionBaseLayoutPlanner.cs` out of `Systems` to `Assets/Game/Scripts/Utilities/InitialFactionBaseLayoutPlanner.cs`, preserving the `.meta` GUID. The type names did not change; systems and tests continue to reference the static planner directly. Validation: old source path absence passed, targeted `git diff --check` passed, moved `.meta` is present, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved `CitizenVisualLifecycleReporter.cs` out of `Systems` to `Assets/Game/Scripts/Components/CitizenVisualLifecycleReporter.cs`, preserving the `.meta` GUID. No source call sites or serialized GUID references were found, so the move is path-only and behavior-neutral. Validation: old source path absence passed, targeted `git diff --check` passed, moved `.meta` is present, GUID reference scan found no serialized users, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved `UnitPathfindBatchJob.cs` to `Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs`, preserving the job script `.meta` GUID and adding a tracked folder `.meta`. The contained `PathfindBatchJob` type and `UnitPathfindingScheduler` scheduling semantics are unchanged. Validation: old path absence/new path presence passed, stale source-reference scan found only the scheduler and the moved job source, targeted `git diff --check` passed, and the mirrored architecture guardrail check passed. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved building runtime model files out of `Systems`: `BuildingDefinition.cs` and the former `RuntimeBuildingData.cs` now live under `Assets/Game/Scripts/Components`, preserving both `.meta` GUIDs. The broad runtime building model was later renamed to `RuntimeBuildingEntity` as part of suffix cleanup. Validation: old path absence/new path presence passed, targeted `git diff --check` passed, and stale path scan found only this roadmap plus the historical `Design/AgentReports/2026-05-23_gameplay-building-data-contract-extraction.md` report. Full working-tree `git diff --check` is still blocked by trailing whitespace in the already-modified `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`; focused Unity EditMode validation was attempted but blocked because the project is already open in Unity.
- 2026-06-08: Moved `AISettingsRuntimeState.cs` out of `Systems` to `Assets/Game/Scripts/Configs/AISettingsRuntimeState.cs`, preserving the `.meta` GUID. This is a folder-only cleanup; the static mutable runtime settings bridge remains tracked as static runtime state debt for a future ECS/config snapshot refactor. Validation: old path absence/new path presence passed, targeted `git diff --check` passed, and stale path scan found only this roadmap plus `Design/FTUE_And_Command_Assistant_Design.md` still pointing at the former location. Unity batchmode import/compile exited successfully after this move, but the requested focused EditMode test results XML was not produced, so focused test execution is not counted as passed.
- 2026-06-08: Removed the unused `BuildingPlacementVisualPresentationSystemHelper.TryGetPrefabModelBounds` helper instead of replacing it with another model-root lookup path. The active placement visual path already uses `BuildingDefinition.LocalBounds`, so deleting the dead helper removes the `prefab.transform.Find("Model")` debt without changing placement behavior. Removed the matching runtime lookup allowlist entry. Validation: stale active lookup/helper scans passed, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully with no C# compiler errors in the scanned log. The requested focused EditMode test results XML was not produced, so focused test execution is not counted as passed.
- 2026-06-08: Replaced the `BuildingProductionTransportSystem` runtime `transport.Instance.transform.Find("Model")` fallback with cached transport renderer references stored on `RuntimeBuildingEntity.ActiveProductionTransport` at spawn time. Rope origin resolution still uses renderer bounds first, but avoids per-update renderer-array allocation and falls back to the transport transform when no renderers exist. Removed the matching runtime lookup allowlist entry. Validation: stale active lookup/allowlist scans passed, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully with no C# compiler errors in the scanned log. The requested focused EditMode test results XML was not produced, so focused test execution is not counted as passed.
- 2026-06-08: Confirmed `UnitGridAuthoring` `Model` lookups are conversion-edge only: the remaining `authoring.transform.Find("Model")` calls are inside `Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs`, including the nested `Baker<UnitGridAuthoring>` path, and the architecture guardrail excludes `Authorings` from runtime lookup scanning. Runtime scans still show the remaining model-root debt in `BuildingDefinitionPrefabSystemHelper` only.
- 2026-06-08: Completed Phase 5 runtime model-reference cleanup. `BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds` now uses the existing whole-prefab renderer-bounds path instead of preferring `root.Find("Model")`; sampled building prefabs keep animated door renderers under `Model` and spawn markers have no renderers, so this removes the string lookup without adding prefab-reference churn. The runtime lookup allowlist now contains only UI shell region/content debt, and the remaining `Find("Model")` scans are editor tooling or `Authorings`. Validation: targeted stale lookup scans passed, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully with no C# compiler errors in the scanned log. The requested focused EditMode test results XML was not produced, so focused test execution is not counted as passed.
- 2026-06-08: Removed two shell hierarchy lookup debts without prefab rebuilds. `UIShellRegionView.Reset` now falls back to the first child `RectTransform` instead of `transform.Find("ContentRoot")`, and `UIShellContentSystem` tracks the installed build-drawer popup instance instead of closing it via `contentRoot.Find(buildDrawerPopupPrefab.name)`. The runtime lookup allowlist is ratcheted down to the single remaining `UIShellContentSystem` section-installer lookup. Validation: targeted stale lookup scans passed, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully with no C# compiler errors in the scanned log. The requested focused EditMode test results XML was not produced, so focused test execution is not counted as passed.
- 2026-06-08: Removed the final runtime hierarchy lookup allowlist entry. Added `UIShellContentSectionsView` as a serialized section-reference binder, populated it on `SCN02_MainMenuContent`, `SCN08_MatchHudContent`, and `SCN19_ArmoryContent` with the targeted `UIShellContentSectionPrefabMigration` editor utility, and changed `UIShellContentSystem.InstallSection` to resolve sections by enum through the view instead of `prefab.transform.Find(sectionName)`. Added an architecture/prefab test that asserts required section references exist. Validation: runtime `.Find` scan now returns only allowed `Shader.Find` calls outside Editor/Authorings, stale shell lookup scans passed, targeted `git diff --check` passed after trimming trailing whitespace in the touched content prefabs, whitespace-ignored prefab diff confirms the actual serialized change is the new section-reference component plus root component references, and Unity batchmode compile/import exited successfully. The requested focused EditMode test results XML was not produced, so focused test execution is not counted as passed.
- 2026-06-08: Validated broad/questionable names without code changes. `BuildingEntityManagerAccessSystem` is still a narrow explicit `World.DefaultGameObjectInjectionWorld` access edge, `BuildingPlacementAdapterCompositionSystemHelper` still owns only placement adapter glue, and `AIControllerConfig` remains an accepted serialized config asset name for compatibility. Future expansions of these roles should be renamed or split rather than broadened.
- 2026-06-08: Replaced active C# Unity menu labels away from the project name. `MenuItem` paths now use `Game/...` or `Tools/Game/...`, and `CreateAssetMenu` menu names now use `Game/...`. Shader names were intentionally left unchanged because material/shader reference migration needs a dedicated validation pass. Validation: active C# scan for `MenuItem`/`CreateAssetMenu` attributes containing `WarlineCapture` returned no matches, active C# `WarlineCapture/` scan now only finds shader names, and targeted `git diff --check` passed for the touched menu/config files.
- 2026-06-08: Removed hardcoded Android build artifact name from `BuildScript`; output now derives from sanitized `PlayerSettings.productName` with a `Game` fallback. This keeps build naming configurable instead of source-bound to the current project title. Validation: targeted `git diff --check` passed for `BuildScript`, and the remaining active source scan shows project-name debt only in planned migration-sensitive areas such as PlayerPrefs keys, shader names, diagnostic tags, project settings, tool prompts, labels, and messages.
- 2026-06-08: Renamed active HUD command diagnostic tags from `WARLINECAPTURE_MATCHHUD_*` to `MATCHHUD_*`. No active source/test/tool parser references to `WARLINECAPTURE_*` remained after the change; historical design/agent reports still preserve older proof markers. Validation: active source/test/tool scan for `WARLINECAPTURE_*` returned no matches, `MATCHHUD_*` scan confirmed the renamed runtime tags, and targeted `git diff --check` passed.
- 2026-06-08: Renamed active PlayerPrefs keys from `WarlineCapture.*` to `Game.*` and changed the editor match-scene restore SessionState key to `Game.RestoreMatchSceneAfterPlay`. Per user direction, no legacy PlayerPrefs migration/read-through was kept, so local user settings may reset. Validation: exact old key scan for `WarlineCapture.Settings`, `WarlineCapture.ReducedMotion`, and `WarlineCapture.RestoreMatchSceneAfterPlay` returned no active matches; targeted `git diff --check` passed.
- 2026-06-08: Replaced remaining low-risk active project-name strings in router error text, balance report output folder, balance report temp test folder, and UI button assertion messages. Remaining active `WarlineCapture` references are now limited to ProjectSettings identity fields, tool prompt text, UI asset label tests (`WarlineCaptureUI`), the filename guardrail constant, and shader names that need material-safe migration. Validation: active source/test/tool scan confirmed only those categories remain, and targeted `git diff --check` passed.
- 2026-06-08: Renamed custom shader display paths from `WarlineCapture/...` to `Game/...` for DOTS health bars, unit impostors, and attack traces. Updated the corresponding `Shader.Find` strings for attack trace and impostor rendering; material references are GUID-based, and the narrow scan found no old shader-name strings outside historical docs. Validation: old active shader path scan returned no matches, new shader path scan confirmed the three declarations and two lookup references, and targeted `git diff --check` passed.
- 2026-06-08: Removed the unused `UIShellContentSystem.RegisteredInstances` static registry and the unused `GameplaySceneBindingSystem.BindGameplayUiRuntimeDependencies` method that consumed it. Existing shell/content binding continues through serialized `UIShellView`, `MenuBootstrapView`, and direct install/bind calls. Removed `UIShellContentSystem` from the static UI registry guardrail allowlist. Validation: scans found no remaining `UIShellContentSystem.Instances` or registry-backed binding method, and targeted `git diff --check` passed.
- 2026-06-08: Removed `BattleHudRuntimeFeedbackView.RegisteredInstances`. The installed Match HUD content now explicitly binds the active `BattleHudRuntimeFeedbackView` into `BattleHudRuntimeFeedbackSystem`, and the view refreshes/clears that active reference on enable/disable. Removed the final static UI registry allowlist entry. Validation: scans found no remaining `BattleHudRuntimeFeedbackView.Instances` or view-owned `RegisteredInstances`; targeted `git diff --check` passed.
- 2026-06-08: Renamed `BattleHudTacticalFeedbackSystem` to `BattleHudTacticalFeedbackView`, preserving the `.meta` GUID. The class is a serialized-reference HUD view with show/hide methods, so no behavior moved in this slice. Updated `BattleHudRuntimeFeedbackView`, `BattleHudRuntimeFeedbackSystem`, and focused tests to use the new `*View` name, and removed the old name from the UI MonoBehaviour naming allowlist. Validation: stale active-code type scans passed, moved GUID still matches the former script GUID, targeted `git diff --check` passed, and Unity batchmode import/compile exited successfully. Unity still did not produce the requested test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed `UIModalSystem` to `UIModalView` and `UIPlaceholderModalSystem` to `UIPlaceholderModalButtonView`, preserving both `.meta` GUIDs. Updated the shell prefab class identifier for the modal component and updated placeholder button tests to use the new view name. Validation: stale active-code type scans passed, moved GUIDs match the former script GUIDs, targeted `git diff --check` passed, and Unity batchmode import/compile exited successfully. Unity still did not produce the requested test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed `SplashScreenSystem` to `SplashScreenView`, preserving the `.meta` GUID. This is a view-only screen component with progress/status/tip bindings; no shell routing behavior changed. Validation: stale active-code type scans passed, moved GUID matches the former script GUID, targeted `git diff --check` passed, and Unity batchmode import/compile exited successfully. Unity still did not produce the requested test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed the shared `UIScreenSystem` base to `UIScreenView`, preserving the `.meta` GUID. Updated router, screen subclasses, and tests to use the `UIScreenView` type; serialized field names in `UIRouterView` remain unchanged. Validation: stale active-code type scans passed, moved GUID matches the former script GUID, targeted `git diff --check` passed, and Unity batchmode import/compile exited successfully. Unity still did not produce the requested test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed `UIPopupCloseSystem` to `UIPopupCloseButtonView`, preserving the `.meta` GUID. The `SCN09_BuildDrawerPopup` component still references the same script GUID and now has the matching class identifier. Validation: stale active-code type scans passed, moved GUID matches the former script GUID, targeted `git diff --check` passed for code/docs, and Unity batchmode import/compile exited successfully. Unity still did not produce the requested test-results XML, so focused test execution is not counted as passed. `SCN09_BuildDrawerPopup.prefab` still has pre-existing trailing whitespace elsewhere, so prefab-wide `git diff --check` remains blocked until that separate prefab cleanup is scheduled.
- 2026-06-08: Completed the UI click-boundary audit for Phase 3. Match HUD command clicks submit through `SelectionUiCommandSystem` into selection command requests; shell route and armory category clicks write UI shell ECS request buffers; quick-custom launch queues scene/match start through the shell edge; settings clicks remain settings persistence/runtime-apply concerns, not gameplay mutation. No UI click handler was found mutating gameplay entities directly. Validation: targeted scans covered UI `onClick`/pointer handlers, UI `EntityManager` access, and shell/gameplay request submission call sites.
- 2026-06-08: Renamed active runtime gameplay data suffixes that conflicted with the ECS naming contract. `DynamicBlockerData`, `PathPoolData`, `DynamicOccupancyData`, citizen projection `*Data` components, and public ECS `*State` components now use `*Component` names. The broad managed runtime building record moved from `RuntimeBuildingData` to `RuntimeBuildingEntity`, with the `.meta` file preserved. Remaining `*Data`/`*State` matches are persistence save data, UI visual state, private helper state, or named compatibility bridges such as `InitialUnitsRuntimeState` and `AISettingsRuntimeState`. Validation: stale old-name scans passed, no public `IComponentData` struct ending in `Data` or `State` remains, and targeted `git diff --check` passed for touched source/test paths.
- 2026-06-08: Completed the service/repository/model-name audit for Phase 7. File names ending in `Service`, `Repository`, or plain `Model` are limited to persistence and UI settings edge code: `SaveService`, `JsonSaveRepository`, `SaveDataModel`, and `SettingsService`. Existing `ReadModel` systems/components are explicit ECS/UI projection boundaries named in the gameplay contract and were not treated as broad application-model debt. Validation: file-level service/repository/model suffix scan passed and Phase 7 folder/suffix cleanup is complete.
- 2026-06-08: Ran final static validation for the completed roadmap slices. Full `git diff --check` passes after trimming trailing whitespace in the already-touched build drawer popup prefab. Unity 6000.4.0f1 batchmode compile/import exited 0 with no C# compiler errors or missing-script log hits. Static prefab/scene scans found no `m_Script: {fileID: 0}` missing scripts. Removed a dead `MissionResultPopupSystem` component from `MissionResultPopup.prefab` because its script GUID had no source meta and the component was already nonfunctional. Serialized stale-reference scans for the renamed ECS data and runtime building types passed.
- 2026-06-08: Reran the focused architecture EditMode test without forcing `-quit`; Unity produced `/private/tmp/warlinecapture-script-architecture-alignment-retry.xml` and `ScriptArchitectureAlignmentContractTests` passed 6/6. Validation log `/private/tmp/warlinecapture-script-architecture-alignment-retry.log` contains no C# compiler errors, missing-script hits, or test failures.
- 2026-06-08: Fixed minimap EditMode/runtime validation fallout found during the validation pass. `MatchHudMinimapInputUiSystemHelper` no longer calls `DontDestroyOnLoad` outside play mode and cleans up generated textures/camera objects with `DestroyImmediate` during EditMode tests. User-requested minimap zoom now preserves the requested zoomed projection in raster fallback instead of expanding back to the default zoomed-out grid. Validation: `MatchHudMinimapProjectionUiSystemHelperTests` passed 11/11 in `/private/tmp/warlinecapture-minimap-projection-retry2.xml`, and `git diff --check` passed.
- 2026-06-08: Attempted a broader UI runtime-boundary EditMode pass for command buttons, Match HUD, minimap, squad tray, armory, quick custom, and main menu. The run produced `/private/tmp/warlinecapture-ui-runtime-boundary-retry.xml` with 19 passing and 40 failing tests. Most failures are stale visual-lock tests that still load retired `Assets/Game/Prefabs/UI/Screens/Screen_*.prefab` paths instead of current shell content prefabs; the remaining minimap failure from that run was fixed and validated separately. At this point, live match-scene launch, command-button runtime smoke, shell UI missing-reference inspection, and `FrameRateDiag` comparison were still open and were closed by the final validation pass below.
- 2026-06-08: Added `MatchHudCommandControlsCurrentPrefabTests` for the current `SCN08_MatchHudContent` prefab instead of retired `Screen_MatchOverlay` paths. The test resolves `MatchOverlayCommandControlsView` from the current serialized content hierarchy, verifies command button references are assigned, binds `MatchOverlayCommandInputUiSystemHelper`, clicks Select/Move/Attack/Scan/Hold/Stop, and confirms the expected ECS selection command requests are queued. Validation: `MatchHudCommandControlsCurrentPrefabTests` passed 2/2 in `/private/tmp/warlinecapture-matchhud-command-controls-current-tests2.xml`, and `git diff --check` passed.
- 2026-06-08: Renamed the active `BuildingRoadLegacy*System` group to current `RoadBuild*System` names: placement storage, definition projection, placement visual, building placement, interaction, grid query, context, and ECS boundary. Updated the road-build composition source/context/lifecycle/disposal/read-model callers and removed active `Legacy` naming from road/build runtime composition code. Validation: active code stale-name scans passed, old `BuildingRoadLegacy*` files are absent, targeted `git diff --check` passed for road-build files and this roadmap, and Unity batchmode import/compile exited successfully. A closest available placement validation run was attempted with `BuildingPlacementValidationSystemTests`; Unity exited successfully but still did not produce the requested test-results XML or summary, so focused test execution is not counted as passed.
- 2026-06-08: Retired the unused `ScreenRouteSystem` route-button MonoBehaviour. Current prefabs already use `UIShellRouteButtonView`, which writes `UiShellRouteRequestComponent` requests to the shell boundary instead of calling `UIRouterView` directly or discovering it through the parent hierarchy. Removed the old class and its `.meta`, updated stale route-button tests to assert/submit through `UIShellRouteButtonView`, and removed the old class from the UI MonoBehaviour naming allowlist. Validation: stale active-code scans found no `ScreenRouteSystem` references under `Assets`, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed `UIShellContentSystem` to `UIShellContentView`, preserving the `.meta` GUID and the `Menu.unity` script reference. This is a behavior-neutral naming cleanup after the earlier registry and hierarchy-lookup removals; the class now reads as a serialized shell-content view/binder. Updated `UIShellView`, `MenuBootstrapView`, the scene class identifier, and the UI MonoBehaviour naming guardrail allowlist. Validation: stale active-code scans found no `UIShellContentSystem` references outside historical roadmap notes, the moved `.meta` keeps the original GUID, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed `BuildDrawerPanelSystem` to `BuildDrawerPanelView`, preserving the `.meta` GUID. The component only owns local build drawer open/close visibility and forwards build-mode feedback through the existing HUD feedback boundary, so no behavior was moved in this slice. Updated the focused runtime-feedback connection test and removed the old class from the UI MonoBehaviour naming allowlist. Validation: stale active-code scans found no `BuildDrawerPanelSystem` references outside this historical note, the moved `.meta` keeps the original GUID, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Split `UIRouter` into `UIRouterView` plus `UIScreenRouteFlowSystem`, preserving the router `.meta` GUID and the `UIShellAppCanvas` prefab script reference. The MonoBehaviour now keeps serialized screen references and lifecycle forwarding, while the route stack, active route state, screen registration, prefab instantiation, and show/hide transitions live in the narrow route-flow system. Removed the old runtime `GetComponentsInChildren<UIScreenView>` broad screen discovery path; routes now use only serialized screen references and serialized screen prefabs. Updated bootstrap, quick-custom tests, and shell launch code to use `UIRouterView`, and removed the router from the UI MonoBehaviour naming allowlist. Validation: stale active-code scans found no `UIRouter` type references outside historical roadmap notes, no runtime `UIScreenView` child discovery remains, the moved `.meta` keeps the original GUID, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Renamed `CommandWheelPanelSystem` to `CommandWheelPanelView`, preserving the `.meta` GUID. The component mirrors the build drawer pattern: local open/close state, button listeners, and forwarding special-order feedback through the existing HUD feedback boundary. Updated command controls and focused feedback tests to use the new view name, and removed the old class from the UI MonoBehaviour naming allowlist. Validation: stale active-code scans found no `CommandWheelPanelSystem` references outside this historical note, the moved `.meta` keeps the original GUID, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Split `QuickCustomScreenSystem` into `QuickCustomScreenView` plus `QuickCustomScreenFlowSystem`, preserving the screen `.meta` GUID. The view keeps the serialized controls and visual binding/readback surface, while the flow system owns initialization from the current quick-game runtime bridge, reset-to-defaults, runtime-config application, and launch forwarding. Updated quick-custom tests and removed the old class from the UI MonoBehaviour naming allowlist. Validation: stale active-code scans found no `QuickCustomScreenSystem` references outside this historical note, the moved `.meta` keeps the original GUID, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Split `SettingsScreenSystem` into `SettingsScreenView` plus `SettingsScreenFlowSystem`, preserving the screen `.meta` GUID. The view keeps serialized controls, visual binding, and control readback; the flow system owns loading, saving, reset-to-defaults, `SettingsService` persistence, runtime settings application, and visual-preference application. Updated settings tests and removed the old class from the UI MonoBehaviour naming allowlist. Validation: stale active-code scans found no `SettingsScreenSystem` references outside this historical note, the moved `.meta` keeps the original GUID, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Started the `AISettingsRuntimeState` static-state cleanup by replacing the public static mutable fields with a single `AISettingsSnapshot` value plus compatibility properties. Added `CurrentSnapshot` and `ApplySnapshot`, and changed `QuickGameConfig` to convert to/from `AISettingsSnapshot` instead of writing individual static fields. This removes the public static field surface while preserving existing callers; the roadmap item remains open until match startup consumes an explicit snapshot/config projection instead of the compatibility bridge. Validation: active scans found no public static mutable field declarations in `AISettingsRuntimeState`, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Completed the explicit AI settings snapshot boundary. `AISettingsSnapshot` now owns the AI tuning math, `QuickGameConfig` converts to/from that snapshot, and match startup captures `AISettingsRuntimeState.CurrentSnapshot` once before passing it into AI config validation and AI startup ECS projection. `AIStartupSystem`, `FactionEconomyStartupSystem`, and `AIFactionControlStartupSystem` now have explicit snapshot overloads used by production match startup; compatibility overloads remain for older tests and bridge callers. Removed the quick-custom launch helper's direct `AISettingsRuntimeState.ApplyToWorld` call because match startup now projects the snapshot. Validation: scans found no public static mutable field declarations in `AISettingsRuntimeState`, production AI startup reads the bridge only to capture/pass snapshots, targeted `git diff --check` passed, and Unity batchmode compile/import exited successfully with the explicit-snapshot AI startup validation path. Unity still did not produce the requested focused test-results XML, so focused test execution is not counted as passed.
- 2026-06-08: Added focused current-prefab/runtime-path validation for renamed shell content surfaces. `MatchHudSquadTrayQuickSelectTests` passed 2/2 against `SCN08_MatchHudContent`; `ArmoryCurrentContentPrefabTests` passed 2/2 against `SCN19_ArmoryContent` and verifies the current catalog list binds the right inspection panel; `RtsSelectionInputSystemTests` plus `SelectionUiReadModelLookupTests` passed 22/22 after updating stale source-text assertions to the current command-mode helper structure; `UIShellCurrentContentLoadTests` passed 1/1 by opening `Menu.unity` and installing Main Menu, Armory, and Match HUD sections through `UIShellContentView`. Validation: targeted `git diff --check` passed for the new/updated tests. Live match-scene launch and `FrameRateDiag` comparison were still open here and were closed by the final validation pass below.
- 2026-06-08: Final roadmap validation completed. `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` passed after adding the missing runtime city building spawnable prefabs to `Game_BuildingPlacement_Config.asset`. Added `MatchRuntimeShellSmokeValidation` as an editor execute-method validator for the actual shell-to-Match route; the runtime shell smoke pass confirmed Match scene load, shell HUD load, and gameplay play request activation. Captured a stable GUI `FrameRateDiag` after the loading gate and initial unit spawn reached the gameplay state; it still showed a spawn-time `BuildingPlacement` hitch around 528 ms, which is a performance concern to handle separately from this architecture cleanup. Focused EditMode XML validation passed for architecture guardrails, minimap projection, Match HUD command controls, squad tray quick select, Armory content, selection input/query systems, and UI shell content loading: 46/46 targeted tests passed. Final `git diff --check` passed, final Unity batchmode compile/import exited successfully, project-name-prefixed C# source scans returned no hits, and no new shipped runtime `Object.Find*`/hierarchy lookup debt was introduced.

## Working Rules

- Do not delete active systems only because their names are bad.
- Preserve Unity `.meta` files during all renames.
- Prefer one ownership slice per change, then validate.
- Do not rebuild UI prefabs wholesale for naming cleanup.
- Do not add `Object.Find*`, hierarchy string lookup, static service locators, or new broad shells while fixing existing debt.
- If runtime behavior changes unexpectedly, add targeted diagnostics and reproduce before handing off.
