# UI Toolkit Canvas Replacement Plan

## Goal

Replace the current runtime Canvas shell with a UI Toolkit shell without losing the existing ECS/SOLID boundaries, screen flow, command behavior, or target-lock visual quality.

The migration starts with the already converted UI Toolkit assets:

- `Assets/Game/UI Toolkit/SCN01_LoadingContent`
- `Assets/Game/UI Toolkit/SCN02_MainMenuContent`
- `Assets/Game/UI Toolkit/SCN08_MatchHudContent`
- `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup`
- `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar`
- `Assets/Game/UI Toolkit/SCN19_ArmoryContent`
- `Assets/Game/UI Toolkit/UIShellAppCanvas`

The Canvas prefabs stay as fallback until each UI Toolkit screen reaches behavior and visual parity.

## Progress Dashboard

Last updated: 2026-06-20

Overall progress: 81% - 90 / 111 tracked items complete

Current phase: Phase 8 - Commander/Profile

Current focus:

- Convert the commander/profile content screen to UI Toolkit.

Completed phases:

- Phase 0 - Inventory And Feature Switch
- Phase 1 - UI Toolkit Shell Foundation
- Phase 2 - Loading Screen
- Phase 3 - Main Menu
- Phase 4 - Match HUD
- Phase 5 - Build Popup
- Phase 6 - Build Placement Confirmation Bar
- Phase 7 - Armory

Blocked:

- None.

| Phase | Status | Progress | Done / Total | Completion evidence |
| --- | --- | ---: | ---: | --- |
| Phase 0 - Inventory And Feature Switch | Complete | 100% | 15 / 15 | `Design/Architecture/ui_toolkit_canvas_phase0_inventory.md`; Unity compile log `/private/tmp/warline-ui-toolkit-runtime-ui-config-compile.log`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 1 - UI Toolkit Shell Foundation | Complete | 100% | 14 / 14 | `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml`; `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uss`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Game/Scripts/Composition/MenuBootstrapView.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 2 - Loading Screen | Complete | 100% | 9 / 9 | `Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uxml`; `Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uss`; `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 3 - Main Menu | Complete | 100% | 11 / 11 | `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml`; `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`; `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`; `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 4 - Match HUD | Complete | 100% | 13 / 13 | `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml`; `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_PassengerItemView.uxml`; `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uss`; `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`; `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; `Design/AgentReports/2026-06-19_ui-toolkit-match-hud-phase4-handoff.md`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 5 - Build Popup | Complete | 100% | 12 / 12 | `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml`; `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildCatalogItemView.uxml`; `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionQueueItemView.uxml`; `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionActiveItemView.uxml`; `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`; `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; `Design/AgentReports/2026-06-19_ui-toolkit-build-popup-phase5-handoff.md`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 6 - Build Placement Confirmation Bar | Complete | 100% | 8 / 8 | `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uxml`; `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uss`; `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`; `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildPlacementReadModelSystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; `Design/AgentReports/2026-06-19_ui-toolkit-build-placement-phase6-handoff.md`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 7 - Armory | Complete | 100% | 8 / 8 | `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml`; `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryItemView.uxml`; `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uss`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`; `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`; `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`; `Design/AgentReports/2026-06-20_ui-toolkit-armory-phase7-handoff.md`; UI Toolkit validation log `/private/tmp/warline-ui-toolkit-validation-execmethod.log` |
| Phase 8 - Commander/Profile | Not started | 0% | 0 / 6 | Pending |
| Phase 9 - Result, Victory, Loss, And Other Popups | Not started | 0% | 0 / 7 | Pending |
| Phase 10 - Remove Canvas Runtime Dependency | Not started | 0% | 0 / 8 | Pending |

## Progress Update Rules

Every heartbeat or implementation update must update this document before reporting completion.

- Update `Last updated`.
- Update `Overall progress` as completed tracked items divided by total tracked items.
- Update the phase table row for any touched phase.
- Mark phase status as `Not started`, `In progress`, `Blocked`, or `Complete`.
- Update each touched phase's `Progress`, `Done / Total`, and `Completion evidence`.
- Add completed step bullets under that phase's `Completed steps`.
- If blocked, write the exact blocker under that phase's `Blocked` field and keep the dashboard `Blocked` field in sync.
- If a phase adds or removes tracked task/validation bullets, update the phase total and the overall total.
- A phase is `Complete` only when every task and validation bullet in that phase is done and evidence is linked.

## Architecture Target

UI Toolkit is an application edge, not gameplay logic.

- ECS owns shell route state, popup state, command requests, gameplay requests, and read models.
- UI Toolkit `*View` MonoBehaviours own serialized `UIDocument`, `VisualTreeAsset`, style references, element lookup, event registration, and visual updates only.
- UI Toolkit event callbacks enqueue ECS request components or write to a narrow shell request boundary. They must not call gameplay logic directly.
- ECS systems must not manipulate `VisualElement`, `UIDocument`, `PanelSettings`, `TemplateContainer`, `StyleBackground`, or hierarchy names.
- Views must not own per-frame ECS reads or gameplay decisions. They expose cached UI references and narrow apply methods only.
- A managed UI presentation system reads ECS read-model data on the main thread and applies it to `*View` objects. Gameplay policy stays in unmanaged ECS systems.
- Runtime gameplay assemblies must not depend on concrete UI Toolkit runtime assemblies.

## Read-Model Split Requirement

The migration value is not that UI Toolkit makes UI rendering Burst-compatible. UI Toolkit object access is managed and cannot run inside Burst jobs. The migration value is that the current Canvas path mixes UI state calculation with object mutation; this rewrite must split those responsibilities instead of recreating the same pattern with `VisualElement`.

Target split:

- Burst-compatible `ISystem` code computes route state, popup state, command state, selected-state summaries, health/order text data, action availability, and other UI read models using unmanaged ECS data only.
- ECS components and buffers store the computed UI read models and one-frame UI action requests.
- `*View` MonoBehaviours hold serialized/cached UI Toolkit references, register UI callbacks, and expose narrow apply methods. They do not decide gameplay state.
- A narrow managed presentation system in `PresentationSystemGroup` reads ECS read models and calls the views to update labels, styles, classes, sprites, visibility, and progress fills.

This same read-model split is technically possible with Canvas too. UI Toolkit is the migration opportunity where the split becomes mandatory so old Canvas-style mixed systems do not get copied forward.

Managed UI apply example:

```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public sealed partial class UiToolkitShellApplySystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Main-thread managed UI apply. Not Burst-compatible because this touches UI Toolkit objects.
        // Read ECS read-model components.
        // Apply to UiToolkitShellView / child views.
    }
}
```

Rules for this managed apply system:

- It may read ECS read-model components and managed shell/view references.
- It may call `UiToolkitShellView` and child `*View` apply methods.
- It must not contain gameplay policy, command validation, pathing, selection logic, build rules, or direct gameplay mutation.
- It must stay thin and mechanical; when logic grows, move that logic into an `ISystem` read-model or request-processing system.
- It is allowed to be `SystemBase` because UI Toolkit object access is managed. This exception does not justify new managed gameplay systems.
- `*View` MonoBehaviours must not add `Update`, `LateUpdate`, coroutines, polling loops, or gameplay timers. If a view needs per-frame refresh, the refresh belongs in the managed presentation system.

## Naming And Contract Guardrails

- Do not add new runtime class names containing or ending in `Controller`, `Presenter`, `Bridge`, `Manager`, or `Button`.
- Use `*View` only for raw UI Toolkit reference holders and visual binding.
- Use `*Config` for ScriptableObject runtime UI configuration.
- Use `*Component` for ECS components and dynamic buffer elements.
- Place new runtime code under explicit assembly boundaries. Do not fall back into the default assembly.
- Do not add direct `UnityEngine.UI`, `TMPro`, `Canvas`, `CanvasGroup`, or `RectTransform` dependencies to the new UI Toolkit runtime.
- Do not bake replaceable UI into a single full-screen image. Sprites, panels, icons, text, portraits, progress bars, and item templates remain separate elements.
- Do not delete Canvas prefabs until the UI Toolkit route passes parity validation and fallback removal is approved.

## New Art Direction Asset Rule

UI Toolkit screens in this migration are based on the new target-lock mockups and new art direction, not on the old Canvas visual style.

- Use only new-art-direction assets for UI Toolkit screens.
- Do not reference old art-direction sprites, old visual-lock folders, or old Canvas-era sprite sheets from new UI Toolkit UXML/USS/runtime bindings.
- Canvas remains the behavior/text/feature parity reference, not the visual asset source.
- During migration, update UI Toolkit labels, icon meanings, button names, panel availability, and runtime bindings to match the current Canvas HUD/features.
- If the current Canvas has a text, icon, state, or panel that the new UI Toolkit mockup/assets do not yet cover, create or request the missing new-art-direction asset through the imagegen workflow.
- Imagegen-created assets must follow the project workflow: generate/reference mockup, generate individual layers on green background when needed, clean-cut green to full transparency, clamp empty space, import as sprites, tune Pixel Per Unit and slice values, and validate in UI Builder/Game View.
- Do not solve missing assets by pulling old sprites into the new UI Toolkit screen.

## Pre-Migration Entry Gates

Do not start runtime migration work until these gates are complete for the target screen or popup:

- Canvas parity inventory exists for the surface: current Canvas hierarchy, visible panels, button names, click behavior, labels, icons, states, item templates, scroll areas, popup ownership, and route effects.
- UI Toolkit mapping exists for the surface: UXML element name, USS class, read-model field, request kind, and missing-asset status for every Canvas parity item.
- New-art-direction reference mockup is saved under `Design/VisualLockLayered/<SurfaceId>/reference/`.
- New-art-direction assets are available for every required panel/icon/state, or a missing-asset imagegen task is written before implementation continues.
- No UXML/USS/runtime binding references old art folders, old target locks, or Canvas-era sprite sheets.
- Read-model components and UI action request kinds are defined before wiring callbacks.
- The managed apply path is defined before adding runtime refresh logic. Views may cache/apply; systems own timing and state.

## Proposed Runtime File Shape

New UI Toolkit runtime edge:

- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitRegionView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitPopupLayerView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitScreenSlotView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitInputGateView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitElementCacheView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`

New UI Toolkit configs:

- `Assets/Game/Data/UI/RuntimeUiConfig.asset`
- `Assets/Game/Scripts/UI/Toolkit/RuntimeUiConfig.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitScreenConfig.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitMotionConfig.cs`

New ECS shell data:

- `UiShellModeComponent`
- `UiRouteRequestComponent`
- `UiPopupRequestComponent`
- `UiActionRequestComponent`
- `UiLoadingProgressComponent`
- `UiPointerBlockComponent`
- `UiTransitionRequestComponent`
- `UiTransitionCompleteComponent`

New ECS/read-model data:

- `UiMainMenuReadModelComponent`
- `UiMatchHudReadModelComponent`
- `UiSelectionSummaryReadModelComponent`
- `UiBuildDrawerReadModelComponent`
- `UiBuildPlacementReadModelComponent`
- `UiArmoryReadModelComponent`
- `UiCommanderReadModelComponent`
- `UiResultReadModelComponent`

The final names can adjust to existing local naming, but they must preserve the same ownership split.

## Migration Phases

### Phase 0 - Inventory And Feature Switch

Status: Complete
Progress: 100% - 15 / 15 tracked items complete
Current step: Phase complete. Continue with Phase 1 shell foundation.
Completed steps:

- Inventoried active Canvas prefabs, runtime views, command bindings, popups, routes, and scene references in `Design/Architecture/ui_toolkit_canvas_phase0_inventory.md`.
- Created surface parity checklists covering labels, icon semantics, button names, states, scroll templates, popup close behavior, and route effects.
- Compared current UI Toolkit UXML counterparts against Canvas parity and recorded initial missing elements/gaps.
- Audited current UI Toolkit UXML/USS static `url(...)` references for known old-art markers; found `0` old-marker references.
- Created the first missing-new-asset manifest inside `Design/Architecture/ui_toolkit_canvas_phase0_inventory.md`.
- Added `RuntimeUiConfig` with `Canvas` and `UiToolkit` modes.
- Added `Assets/Game/Data/UI/RuntimeUiConfig.asset` defaulting to `Canvas`.
- Added a guarded `MenuBootstrapView`/`MenuBootstrapSystem` startup branch that can enable a serialized `UIDocument` shell while leaving Canvas as the default path.
- Ran Unity batch compile/import validation with no `CS` errors or warnings found in `/private/tmp/warline-ui-toolkit-runtime-ui-config-compile.log`.
- Confirmed the parity checklist and missing-new-asset manifest exist.
- Confirmed current UI Toolkit UXML/USS has no known old-art marker references.
- Added `UiToolkitCanvasMigrationValidationTests` to enforce UI Toolkit UXML import, USS import, USS `url(...)` resolution, old-art marker blocking, the default `RuntimeUiConfig` mode, Canvas fallback smoke, and isolated UI Toolkit shell smoke.
- Ran the UI Toolkit migration validation through Unity with `[UiToolkitCanvasMigrationValidation] result=Passed tests=7` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Confirmed current Canvas mode keeps the Canvas fallback enabled and the UI Toolkit shell disabled.
- Confirmed isolated UI Toolkit mode enables the `UIDocument` shell and disables the Canvas fallback without destroying it.

Blocked: Unity validation for the Build Drawer retained catalog/queue binding pass is temporarily blocked because process `92047` has `/Users/farhad/Projects/WarlineCapture` open in the Unity editor. Missing command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UiToolkitCanvasMigrationValidationTests.RunBatchValidation -logFile /private/tmp/warline-ui-toolkit-validation-execmethod.log`. Owner lane: UI. Another lane can continue only if it does not require main-project Unity batchmode validation.

Tasks:

- Inventory every active Canvas prefab, runtime view, command binding, popup, route, and scene reference.
- For each active Canvas surface, create a parity checklist covering text labels, icon semantics, button names, selected/disabled states, scroll item templates, popup close behavior, and route effects.
- Compare each UI Toolkit UXML/USS surface against the Canvas parity checklist and list missing elements before writing runtime code.
- Audit every UI Toolkit UXML/USS `url(...)`, runtime sprite reference, and generated asset path to ensure it uses only the new art direction.
- Create a missing-new-asset manifest for any Canvas feature that lacks an equivalent new-art icon, panel, state, or portrait. Resolve those through imagegen before binding that feature.
- Add `RuntimeUiConfig` with a simple mode: `Canvas` or `UiToolkit`.
- Keep the default mode on `Canvas` until Phase 2 passes.
- Add a runtime startup path that can mount a `UIDocument` shell without deleting the Canvas shell.
- Add import validation for UXML/USS and sprite references.
- Add a no-old-art validation check for UI Toolkit paths and runtime binding assets.

Validation:

- Unity compile has no errors and no new warnings.
- Current Canvas UI still runs unchanged when `RuntimeUiConfig` is set to `Canvas`.
- UI Toolkit shell can be instantiated in isolation with no gameplay behavior.
- Parity checklist and missing-asset manifest exist for the next phase surface.
- No UI Toolkit UXML/USS references old-art paths.

### Phase 1 - UI Toolkit Shell Foundation

Status: Complete
Progress: 100% - 14 / 14 tracked items complete
Current step: Phase complete. Continue with Phase 2 loading screen reconciliation.
Completed steps:

- Added the `Game.UI.Toolkit` assembly for the managed UI Toolkit edge.
- Added `UiToolkitShellView` as the raw `UIDocument`/`VisualTreeAsset` holder and shell UXML mount point.
- Connected `MenuBootstrapView.ApplyRuntimeUiMode()` to mount `UiToolkitShellView` only in UI Toolkit mode and clear its cache in Canvas mode.
- Added focused validation that mounts `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml` through a `UIDocument`, caches `UIShellAppCanvas`, and confirms `SafeAreaRoot` exists.
- Bound required shell regions by name on `UiToolkitShellView`: `SafeAreaRoot`, `HeaderBar`, `ContentRoot`, `FooterBar`, `ModalOverlay`, and `TooltipLayer`.
- Added focused validation that all required shell regions bind by name and that `ClearCache()` clears the region state.
- Added explicit screen slots to `UIShellAppCanvas.uxml` for loading, main menu, match, armory, commander/profile, result, and popups.
- Cached required screen slots by name on `UiToolkitShellView`.
- Added focused validation that all required screen slots bind by name and that `ClearCache()` clears slot state.
- Added reusable USS motion states for visible, fade-out, directional slide-out, scale-out, popup-visible, and popup-hidden transitions.
- Added `UiToolkitShellView.ApplyShellMotion()` and `RemoveShellMotion()` as narrow class-application helpers without per-frame polling or gameplay logic.
- Added focused validation for required motion USS classes and stale-class removal when swapping motion states.
- Moved `LoadingLayer` out of normal `ContentRoot` and into the top-level safe-area overlay stack above `ModalOverlay` and `TooltipLayer`.
- Cached `LoadingLayer` as a required shell region on `UiToolkitShellView`.
- Added focused validation that `ModalOverlay` draws above normal content, `LoadingLayer` draws above content, footer, popups, and tooltip overlays, and both overlay layers remain hidden by default.
- Added pointer-block query methods to `UiToolkitShellView` for gameplay, placement, and raycastable UI suppression.
- Added structural filtering so empty full-screen shell containers and screen slots do not block world input by themselves.
- Added focused validation that concrete UI content and visible loading/popup/header overlays block world clicks while hidden overlays and empty screen slots do not.
- Added `UiToolkitShellApplySystem` as the managed UI Toolkit apply edge in `PresentationSystemGroup`.
- Kept the apply system thin for this phase: it reads existing shell state/loading read models from `UiShellRuntimeGateway` and does not contain gameplay policy or direct `VisualElement` access.
- Added focused validation that the apply system is a `PresentationSystemGroup` `SystemBase`, that the UI Toolkit assembly has explicit `Unity.Entities`/`Unity.Collections` references, and that UI Toolkit `*View` classes do not own `Update`, `LateUpdate`, or coroutines.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=16` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiToolkitShellApplySystem` managed shell-view reference storage plus `ConfigureShellView` and `ClearShellView` methods so the presentation edge can receive the mounted shell without reading or writing `VisualElement` directly.
- Wired `MenuBootstrapView.ApplyRuntimeUiMode()` to register the mounted `UiToolkitShellView` with `UiToolkitShellApplySystem` in UI Toolkit mode and clear the reference in Canvas mode through `World.DefaultGameObjectInjectionWorld`.
- Added focused validation for the shell reference path and bootstrap wiring.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=17` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that the UI Toolkit shell scaffold stays fluid and full-screen instead of clamping to a fixed mockup resolution.
- Added 16:9 and 20:9 shell aspect smoke checks covering top-level shell regions, header/footer visibility, and placeholder popup clipping.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=19` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that runtime ECS-style systems do not read or write UI Toolkit objects directly, including `VisualElement`, `UIDocument`, `PanelSettings`, `TemplateContainer`, or `StyleBackground`.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=20` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Closed Phase 1 after confirming all shell foundation tasks and validation gates have focused coverage: shell mount, region binding, screen slots, motion states, overlay order, pointer blocking, managed apply ownership, shell reference path, 16:9/20:9 fluid layout, ECS/UI Toolkit boundary, and no UI Toolkit view frame polling.

Blocked: None.

Tasks:

- Mount `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml` through a `UIDocument`.
- Bind shell regions by name: `SafeAreaRoot`, `HeaderBar`, `ContentRoot`, `FooterBar`, `ModalOverlay`, `TooltipLayer`.
- Add explicit screen slots for loading, main menu, match, armory, commander/profile, result, and popups.
- Implement style-based motion primitives for slide, scale, fade, and popup scale.
- Make loading and popup layers always render above normal content.
- Add pointer-block propagation from UI Toolkit to ECS input suppression.
- Add `UiToolkitShellApplySystem` in `PresentationSystemGroup` as the only per-frame managed UI apply owner.
- Keep `UiToolkitShellView` and child `*View` classes as reference holders and event-callback registration/apply surfaces only; no `Update` or gameplay polling.
- Establish the managed shell reference path used by `UiToolkitShellApplySystem`, such as a narrow managed ECS reference component or an existing scene view reference.

Validation:

- Shell renders in 16:9 and 20:9.
- Pointer clicks on UI Toolkit elements do not pass through to world selection.
- Loading and popup layers sort above all content.
- No ECS system reads or writes `VisualElement` directly.
- No `*View` in the UI Toolkit path owns `Update`, `LateUpdate`, or gameplay timers.

### Phase 2 - Loading Screen

Status: Complete
Progress: 100% - 9 / 9 tracked items complete
Current step: Phase complete. Continue with Phase 3 main menu reconciliation.
Completed steps:

- Reconciled Loading Canvas behavior/text/features against the UI Toolkit loading surface before binding.
- Confirmed the behavior source is `UIShellLoadingProgressView`: it reads `UiShellLoadingProgressModel` from `UiShellRuntimeGateway`, applies `Progress01` to the fill width, applies `Status` to status text, and formats percent as `0%` through `100%`.
- Confirmed the route/source-of-truth is ECS shell state: `UiShellFlowSystem` shows/exits `LoadingLayer`; `MenuBootstrapSystem` computes actual loading progress, match-load deferral, the 2 second minimum visible loading window, match-ready hold, and menu return loading.
- Confirmed the current Canvas loading prefab exposes `UIShellLoadingProgressView` with `Progress_Fill`, `LoadingPanel_Percent`, and `LoadingPanel_Status`.
- Confirmed the UI Toolkit loading UXML exposes binding names for `Brand_LogoLockup`, `CommandSystem_Text`, `LoadingPanel_Status`, `LoadingPanel_Percent`, `Progress_Frame`, `Progress_Fill`, `BottomStatus_Spinner`, and `BottomStatus_Text`.
- Added focused validation that the UI Toolkit loading UXML keeps the required Canvas parity binding names, approved default text, separate progress fill element, and the approved `TargetLockV04Imagegen` new-art asset set.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=21` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiToolkitShellView` loading-screen asset mounting so `SCN01_LoadingContent.uxml` can be cloned into `LoadingScreenSlot` in UI Toolkit mode without replacing normal content or popup content.
- Added focused validation that the configured loading UXML mounts under the shell loading slot, exposes the progress/percent binding elements, and does not duplicate on repeated shell mounts.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=22` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Bound the mounted loading UXML elements on `UiToolkitShellView`: `LoadingBody`, `Background`, `Brand_LogoLockup`, `CommandSystem_Text`, `LoadingPanel_Frame`, `LoadingPanel_Status`, `LoadingPanel_Percent`, `Progress_Frame`, `Progress_Fill`, `BottomStatus_Spinner`, and `BottomStatus_Text`.
- Added `UiToolkitShellView.ApplyLoadingProgress()` as the retained UI Toolkit loading apply surface. It clamps `Progress01`, formats `0%` through `100%`, applies fallback status text, updates the retained progress-fill width, and does not recreate UI elements.
- Wired `UiToolkitShellApplySystem` to call `ApplyLoadingProgress(lastLoadingProgress)` from the existing `UiShellLoadingProgressModel` read path.
- Added `overflow: hidden` to the loading progress track so the retained fill can safely animate from empty to full inside the frame.
- Added focused validation that loading content mounts at runtime `0%`, exposes all required binding elements, applies a mid-progress read model, clamps completed progress to `100%`, and keeps the bottom status static until spinner animation binding is explicitly added.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=23` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Updated `MenuBootstrapSystem` so UI Toolkit mode no longer exits before `sceneLifecycleSystem`, `matchStartSystem`, deferred match-load feedback, and `UpdateActualLoadingProgress` run. UI Toolkit mode now skips only Canvas presentation and Canvas runtime UI binding.
- Mirrored fresh shell reset for UI Toolkit initialization so the isolated UI Toolkit path does not inherit stale loading state.
- Extended `UiToolkitShellApplySystem` to consume shell presentation commands and enqueue transition completions through `UiShellRuntimeGateway`, matching the Canvas transition-completion contract without adding gameplay policy.
- Added `UiToolkitShellView.ApplyPresentationCommands()` for loading shell commands so `ShowLoading` reveals `LoadingLayer` and `ExitLoading` hides it through shell classes.
- Added focused validation that UI Toolkit loading commands show/hide the loading layer, apply visible/fade motion classes, and that `MenuBootstrapSystem.Update` keeps loading progress active before skipping Canvas-only binding in UI Toolkit mode.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=25` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that mounted loading content remains hidden and non-blocking on the default menu boot path while still initializing the retained UI to runtime `0%`.
- Added focused validation that a visible UI Toolkit loading layer draws above visible popups and tooltip overlays and blocks underlying popup/menu/world input.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=27` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that mounted UI Toolkit loading labels are populated, not under a hidden ancestor, and not resolved to `display: none` when `ShowLoading` reveals the loading layer.
- Added focused validation that UI Toolkit loading can show, apply completed progress at `100%`, then process `ExitLoading` while preserving the ECS shell route transition contract through `UiShellFlowSystem` and `UiToolkitShellApplySystem`.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=29` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Blocked: None.

Loading reconciliation map:

| Canvas/reference item | UI Toolkit element | Runtime source | Binding rule |
| --- | --- | --- | --- |
| Root loading content | `SCN01_LoadingContent` / `LoadingBody` | `UiShellCommandKind.ShowLoading` on `UiShellRegionId.LoadingLayer` | Mount into `UiToolkitShellView.LoadingScreenSlot`; never replace normal content or popup content. |
| Background and logo | `Background`, `Brand_LogoLockup`, `CommandSystem_Text` | Static new-art loading assets | Keep as visual-only elements. No runtime gameplay state. |
| Main status | `LoadingPanel_Status` | `UiShellLoadingProgressModel.Status` | Empty status falls back to `Preparing command interface` or approved Toolkit fallback text; apply only in the managed UI Toolkit presentation edge. |
| Percent | `LoadingPanel_Percent` | `UiShellLoadingProgressModel.Progress01` | Clamp `0..1`, round to integer percent, display `0%..100%`; initial runtime apply must override the static `68%` mockup value. |
| Progress fill | `Progress_Fill` | `UiShellLoadingProgressModel.Progress01` | Use retained element width/scale, not recreated elements; starts at `0` and reaches `100` under existing loading progress logic. |
| Bottom status | `BottomStatus_Text`, `BottomStatus_Spinner` | Static text plus loading active state | Text defaults to `LOADING REQUIRED DATA`; spinner is visual-only until animation binding is explicitly added. |
| Topmost behavior | `LoadingLayer` and `LoadingScreenSlot` | `UiShellFlowSystem` / `UiShellView` equivalent commands | Loading layer stays above content, popups, and tooltip layers; already enforced in Phase 1 shell order validation. |
| Initial loading disabled | `UiShellMode.None` begins with `EnterMenu` | `UiShellFlowSystem` initial mode rule | Do not show loading on game start; only show when route requests enter match or return to menu through loading. |

Phase 2 binding gaps:

- None.

Tasks:

- Reconcile Loading UXML text, logo, status, progress behavior, topmost behavior, and disable-initial-loading behavior against the current Canvas flow before binding.
- Replace `SCN01_LoadingContent.prefab` with `SCN01_LoadingContent.uxml` in UI Toolkit mode.
- Bind loading title, status text, progress bar frame, fill, logo, backdrop, and spinner.
- Progress reads from `UiLoadingProgressComponent` or the existing loading read model.
- Fake loading starts at `0` and smoothly reaches `100` over the configured duration.
- Preserve current flow: no initial loading screen if the current game config disables it; when loading is shown, it remains topmost.

Validation:

- Text is visible in UI Builder and in play mode.
- Progress bar visually starts at `0`, reaches `100`, then route transition begins.
- Loading covers menus and popups while active.

### Phase 3 - Main Menu

Status: Complete
Progress: 100% - 11 / 11 tracked items complete
Current step: Phase complete. Continue with Phase 4 Match HUD reconciliation.
Completed steps:

- Reconciled Main Menu Canvas behavior/text/features against the UI Toolkit main menu surface before binding.
- Confirmed the UI Toolkit surface exposes required named regions and actions: `HeaderContent`, `CreditsPanel`, `SuppliesPanel`, `CommandPanel`, `InboxButton`, `SettingsButton`, `MenuButton`, `Nav_Campaign`, `Nav_Armory`, `Nav_Supply`, `Nav_Command`, `Nav_TechTree`, `Nav_Profile`, `Card_Campaign`, `Card_Skirmish`, `Card_Operations`, `CommanderPanel`, and `DeployOperationButton`.
- Confirmed the Canvas behavior source is split between `UIShellContentView` section installation and runtime binding (`BindQuickCustomScreens`, `BindGameStartButtons`) while route decisions remain in `UiShellFlowSystem`.
- Added focused validation that the Main Menu UXML keeps required binding names, actionable elements are UI Toolkit buttons, default labels exist, and the USS uses the approved `MainMenuBrightCommand` new-art set plus the current `TargetLockV04Imagegen` logo lockup.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=30` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiToolkitShellView` support for a configured `SCN02_MainMenuContent.uxml` asset and a retained Main Menu template under `MainMenuScreenSlot`.
- Added UI Toolkit presentation handling for `EnterMenu`, `SwapMenuMiddle`, and `ExitMenu` so the Main Menu UXML can be shown/hidden by shell commands without gameplay logic in the view.
- Added focused validation that the Main Menu UXML mounts once under `MainMenuScreenSlot`, exposes deploy/commander/header bindings after mount, survives repeated shell mounts without duplication, clears through `ClearCache`, and responds to Main Menu presentation commands.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=32` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Bound the mounted Main Menu action surface in `UiToolkitShellView`: header actions, left navigation, mode cards, commander/profile panel, and deploy now submit through `UiShellRuntimeGateway.TryEnqueueRouteRequest`.
- Kept Main Menu callbacks as request-boundary work only. They enqueue shell route intents and do not call gameplay systems, selection, build rules, pathing, or Canvas code.
- Added focused validation that every Phase 3 Main Menu action target exists as a UI Toolkit button and submits the expected shell route request. Current placeholder route mappings are explicit until dedicated routes exist: Supply -> `LoadoutSquadPrep`, Tech Tree -> `Events`, Profile/Commander -> `CommandFeed`, Skirmish -> `QuickCustomSetup`.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=33` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added explicit Main Menu route-state application in `UiToolkitShellView` so `SwapMenuMiddle` updates route-selected nav/card classes while preserving the mounted `HeaderContent` instance.
- Added focused validation that Main Menu sub-route swaps do not recreate the UXML tree, keep the same header and header action bindings, select `Nav_Armory` for the Armory route, and restore `Nav_Campaign` for the root Main Menu route.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=33` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added explicit commander/profile sub-route state for `CommandFeed`: the profile route selects `Nav_Profile`, applies the profile route class, reveals `CommanderProfileScreenSlot`, and keeps the same mounted header/header action instances.
- Added focused validation that returning to the root Main Menu hides `CommanderProfileScreenSlot`, clears Profile selection, restores Campaign selection, and preserves the same header instance.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=33` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Updated `UiToolkitShellApplySystem` so Main Menu selected nav/card state is mechanically applied from `UiShellStateModel.ActiveRoute` when the ECS shell read model reports `UiShellMode.MainMenu`.
- Added focused validation that changes the fake shell read model from `CommandFeed` to `Armory` without presentation commands and verifies Profile/Armory selected state, route classes, and `CommanderProfileScreenSlot` visibility update through the managed apply edge.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=34` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused Main Menu header geometry validation for the header logo, resource icons, plus icons, action icon safe rects, and header button frame slices.
- The validation now guards that header resource/plus icons stay vertically centered, action icons use symmetric safe insets, header action hit areas remain close to their visible square frames, and the logo uses scale-to-fit instead of cropping.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=35` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that `InboxButton` and `SettingsButton` use the shared header action hit rect, direct frame/icon children, no button text, no per-button positional overrides, transparent zero-padding button reset, and frame/icon geometry tied to the visible square frame.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=36` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that the three Main Menu game-mode cards keep portrait art wells padded inside the chrome, title labels centered inside label plates, badges/icons centered inside their junction frame, bottom decoration inside the label plate, direct child structure intact, and large target-matched title text.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=37` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiShellCommanderProfileComponent` / `UiShellCommanderProfileModel` and routed it through `UiShellRuntimeGateway`, `UiShellEcsGateway`, `UiToolkitShellApplySystem`, and `UiToolkitShellView.ApplyMainMenuCommanderProfile`.
- Moved the Main Menu commander portrait image onto the semantic `commander-portrait-default` class so the read model can select portrait classes without old-art sprites or view-side gameplay decisions.
- Added focused validation that the commander/profile panel applies name, subtitle, and portrait class from the read model through the managed UI Toolkit apply edge, with approved defaults for empty read-model values.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=38` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiShellMainMenuResourcesComponent` / `UiShellMainMenuResourcesModel` and routed resource values through `UiShellRuntimeGateway`, `UiShellEcsGateway`, `UiToolkitShellApplySystem`, and `UiToolkitShellView.ApplyMainMenuResources`.
- Bound `CreditsPanel`, `SuppliesPanel`, and `CommandPanel` value labels from read-model data with approved defaults for empty read-model values.
- Reset Main Menu resource read-model defaults from `MenuBootstrapSystem.ResetShellForFreshMenuScene` so menu boot does not retain stale runtime values.
- Added focused validation that Main Menu resource labels apply from the ECS shell read model through the managed UI Toolkit apply edge and fall back to approved defaults when empty.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=39` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Blocked: None.

Main Menu reconciliation map:

| Canvas/reference item | UI Toolkit element | Runtime source | Binding rule |
| --- | --- | --- | --- |
| Root main menu content | `SCN02_MainMenuContent` | `UiShellCommandKind.EnterMenu` / `UiShellCommandKind.SwapMenuMiddle` | Mount into the main menu screen slot or shell regions in UI Toolkit mode; do not flatten into one image. |
| Background | `MenuBackgroundContent`, `BackgroundArt`, `BackgroundArtOverlay` | Static new-art MainMenuBrightCommand background | Visual-only; never use old Canvas-era background sprites. |
| Persistent header | `HeaderContent`, `HeaderLogoPanel`, `HeaderResourceArea`, `HeaderActionsPanel` | Shell route state plus resource read model | Header remains persistent across Main Menu sub-routes; only labels/resources/icons update from read models. |
| Resources | `CreditsPanel`, `SuppliesPanel`, `CommandPanel` | `UiShellMainMenuResourcesModel` / `UiShellMainMenuResourcesComponent` | Update values mechanically in the managed UI Toolkit apply edge; no gameplay policy in the view. |
| Header actions | `InboxButton`, `SettingsButton`, `MenuButton` | UI action request boundary | Callbacks enqueue UI/shell requests only; settings/mail assets must stay new-art direction. |
| Left navigation | `Nav_Campaign`, `Nav_Armory`, `Nav_Supply`, `Nav_Command`, `Nav_TechTree`, `Nav_Profile` | Route request/read-model state | Selected state comes from ECS read model or shell route, not local-only class state. |
| Mode cards | `Card_Campaign`, `Card_Skirmish`, `Card_Operations` | Main menu selection/read model | Cards remain live buttons with separate art, frame, title, badge, and selected state elements. |
| Deploy | `DeployOperationButton` | `UiShellRouteIntent.EnterMatch` | Enqueue route request for Main Menu exit, loading, then Match HUD enter. |
| Commander/profile | `CommanderPanel` and child identity/progress/readiness elements | Commander/profile read model and route request | Click enqueues profile route/screen swap; header remains unchanged while left/middle/right content swaps. |

Tasks:

- Reconcile Main Menu UXML buttons, labels, resources, settings/mail actions, profile action, deploy route, and current header behavior against the Canvas prefab before binding.
- Replace `SCN02_MainMenuContent.prefab` with `SCN02_MainMenuContent.uxml` in UI Toolkit mode.
- Bind persistent header, left navigation, middle game mode panels, deploy action, profile/commander region, resources, settings, and mail.
- Header stays persistent during main-menu sub-screen swaps.
- Deploy action writes a route request: main menu exit, loading, match HUD enter.
- Commander/profile click writes a route request: middle/left/right content swap, header remains.
- Preserve tab selected state through ECS read model, not local-only visual state.

Validation:

- Header icons are centered inside their sections.
- Settings and mail hit areas match their visible frames.
- Game mode panels keep visible text and portraits inside safe padding.
- Commander/profile region uses the correct portrait and title/subtitle read model.

### Phase 4 - Match HUD

Status: Complete
Progress: 100% - 13 / 13 tracked items complete
Current step: Complete.
Completed steps:

- Reconciled Match HUD Canvas behavior/text/features against the UI Toolkit Match HUD surface before binding.
- Confirmed the UI Toolkit Match HUD exposes required named regions and actions: `HeaderContent`, `CurrentOrderBanner`, `ResourceStrip`, `SelectedSquadPanel`, `ReturnButton`, `DestroyButton`, `BoardButton`, `PassengerChip`, `TransportPassengerDrawer`, `Scroll_View`, `ExitAllButton`, `CloseButton`, `ThreatJumpPanel`, `JumpButton`, `RightQuickRail`, `PauseButton`, `SettingsButton`, `RightBuildCommand`, `RightSupportCommand`, `SquadCard1` through `SquadCard5`, `CommandRail`, `SelectCommand`, `MoveCommand`, `AttackCommand`, `HoldCommand`, `StopCommand`, `BuildCommand`, `ScanCommand`, `SupportCommand`, `MinimapPanel`, `ZoomIn`, `ZoomOut`, `ZoomFocus`, `FeedbackPanel`, `BoardAllButton`, and `CancelButton`.
- Confirmed `SCN08_PassengerItemView.uxml` exists as the retained passenger row template and exposes `Portrait`, `Name`, `Role`, `HealthFrame`, `HealthFill`, `Health`, and `ExitButton` bindings.
- Added focused validation that the Match HUD UXML keeps the Canvas parity binding names, every actionable surface is a UI Toolkit `Button`, five squad cards are present, command labels are present, passenger scrollbars remain hidden, the passenger template exists, and the USS uses the approved new-art `TargetLockV02` asset set.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=40` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added configured `SCN08_MatchHudContent.uxml` asset support to `UiToolkitShellView` and mounted it once under `MatchScreenSlot`.
- Cached `MatchHudContentRoot`, validated required root/command bindings, and kept the retained Match HUD hidden until an `EnterMatchHud` presentation command arrives.
- Added UI Toolkit presentation handling for `EnterMatchHud` and `ExitMatchHud` so shell commands reveal/hide the retained Match HUD slot without gameplay logic in the view.
- Added focused validation for Match HUD mount, no duplicate mount on repeated shell mount, hidden-before-command state, enter/exit motion classes, and `ClearCache` cleanup.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=41` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiActionKind` / `UiActionRequestModel` and the ECS `UiActionRequestComponent` dynamic buffer for UI Toolkit action requests.
- Extended `UiShellBoundarySystem` and `UiShellEcsGateway` so Match HUD UI Toolkit callbacks enqueue typed action requests on the shell boundary instead of calling gameplay logic directly.
- Bound the mounted Match HUD action surfaces in `UiToolkitShellView`: selected actions, passenger drawer actions, threat jump, right rail, five squad cards, command rail, minimap controls, and feedback actions.
- Added focused validation that every Match HUD action target exists, is registered by the mounted view, and submits the expected `UiActionKind` plus payload through `UiShellRuntimeGateway`.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=42` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiActionRequestSystem` as an ECS `ISystem` that consumes Match HUD `UiActionRequestComponent` values and writes existing `RtsSelectionCommandIntentRequestElement` requests for Select, Move, Attack, Hold, Stop, Scan/Support, Return, Destroy, Board, Board All, and Cancel Feedback.
- Added `BuildDrawer` to the shell popup contract and routed Build/RightBuild action requests to `UiShellPopupRequestComponent` without calling Canvas popup code.
- Routed Match menu/settings/pause action requests to existing shell route or popup request buffers.
- Preserved Canvas-equivalent UI-click suppression by updating `RtsSelectionInputStateComponent` and clearing pending move requests before command intents are queued.
- Added focused validation that the action request processor is an `ISystem`, does not touch UI Toolkit or Canvas objects, and maps Match HUD action kinds to existing ECS command/popup contracts.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=43` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudSelectionPanelModel` as the sprite-free UI Toolkit shell contract for selected panel visibility, title, subtitle, order, health, badge visibility, and action availability.
- Extended `UiShellEcsGateway` so the mounted UI Toolkit shell reads the existing `FocusedUnitUiReadModelComponent` as a hidden/visible selected-panel read model without adding a dependency on gameplay systems.
- Updated `UiToolkitShellApplySystem` to apply Match HUD selection read-model data only when the shell state is `MatchHud`, keeping it as the thin managed UI Toolkit apply edge.
- Cached `SelectedSquadPanel`, title/subtitle/order labels, health fill/text, badge, and Return/Destroy/Board actions in `UiToolkitShellView`, and added `ApplyMatchHudSelection()` for mechanical visual updates.
- Added focused validation that a fake Match HUD selection read model shows the selected panel, applies title/subtitle/order/health/action state, hides vehicle badges, and hides the panel again when the read model is empty.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=44` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudCommandStateModel` as the sprite-free UI Toolkit shell contract for active tactical command mode and Build drawer visibility.
- Added `UiShellActivePopupComponent` and updated ECS popup flow so the shell boundary records the currently visible popup without a managed UI dependency.
- Extended `UiShellEcsGateway` to combine `RtsSelectionInputStateComponent.ActiveCommandMode` with active `BuildDrawer` popup state for the Match HUD command read model.
- Updated `UiToolkitShellApplySystem` so Match HUD command-state apply is independent from selected-panel data and still only runs on the thin managed UI Toolkit edge.
- Cached bottom command rail and right quick rail command elements in `UiToolkitShellView`, clearing UXML defaults on mount and applying selected classes for active command mode plus Build drawer open state.
- Added focused validation that Move command selection, Build drawer selected state, and selected-state clearing apply correctly on the mounted Match HUD surface.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=45` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudPassengerDrawerModel` / `UiMatchHudPassengerRowModel` as the sprite-free shell contract for passenger chip visibility, drawer visibility, passenger counts, and retained row data.
- Added `UiMatchHudPassengerDrawerStateComponent` on the shell boundary and updated `UiActionRequestSystem` so Toggle/Close/Exit All passenger actions update ECS state without touching UI Toolkit objects.
- Extended `UiShellEcsGateway` to read the existing `FocusedUnitUiReadModelComponent` and `FocusedUnitPassengerUiReadModelElement` buffer into the Match HUD passenger drawer model.
- Updated `UiToolkitShellApplySystem` to apply passenger drawer read-model data only while the shell state is `MatchHud`.
- Cached passenger chip, drawer, header, empty state, and retained passenger rows in `UiToolkitShellView`, and added `ApplyMatchHudPassengerDrawer()` for count labels, row text, row health, and visibility classes.
- Added focused validation that the mounted Match HUD passenger drawer shows/hides from the read model, updates counts, fills retained passenger rows, hides unused rows, and clears when hidden.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=46` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudSquadTrayModel` / `UiMatchHudSquadTrayCardModel` as the sprite-free shell contract for five retained squad cards, selected slot, titles, health text, health fill, and visibility.
- Added `UiMatchHudSquadTrayStateComponent` on the shell boundary and updated `UiActionRequestSystem` so squad slot UI actions update selected-slot ECS state without touching UI Toolkit objects.
- Extended `UiShellEcsGateway` to expose the Match HUD squad tray model, currently preserving the five Canvas-equivalent slot identities while keeping selection state ECS-owned.
- Updated `UiToolkitShellApplySystem` to apply squad tray read-model data only while the shell state is `MatchHud`.
- Cached the five retained squad cards in `UiToolkitShellView`, and added `ApplyMatchHudSquadTray()` for selected class, title, health text/fill, and card visibility.
- Added focused validation that the mounted Match HUD squad tray applies selected slot state, card titles, health text/fill, hidden cards, and default clearing.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=47` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudHeaderModel` as the shell contract for Match HUD order text, squad text, credits, fuel, supply, and civilian risk values.
- Added ECS-owned `UiMatchHudHeaderComponent` defaults on the shell boundary so header/resource text is no longer sourced from UXML placeholders.
- Extended `UiShellEcsGateway` and `UiToolkitShellApplySystem` so Match HUD header/resource data is read from ECS and applied only through the thin managed UI Toolkit edge.
- Cached `CurrentOrderBanner` and `ResourceStrip` labels in `UiToolkitShellView`, and added `ApplyMatchHudHeader()` for mechanical label updates.
- Added focused validation that the mounted Match HUD header and resource labels apply fake read-model values and restore defaults.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=48` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudStatusSurfacesModel` as the shell contract for objectives, elapsed time, threat jump, and tactical feedback visibility/actions.
- Added ECS-owned `UiMatchHudStatusSurfacesComponent` defaults on the shell boundary so objectives/threat/feedback text no longer comes from UXML placeholders.
- Extended `UiShellEcsGateway` and `UiToolkitShellApplySystem` so objectives/threat/feedback state is read from ECS and applied only through the thin managed UI Toolkit edge.
- Cached `ObjectivesPanel`, `ThreatJumpPanel`, and `FeedbackPanel` labels/actions in `UiToolkitShellView`, and added `ApplyMatchHudStatusSurfaces()` for mechanical text, class, visibility, and enabled-state updates.
- Added focused validation that mounted Match HUD objectives, objective icon classes, threat jump text/enabled state, and feedback action visibility/enabled state apply fake read-model values and restore defaults.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=49` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiMatchHudMinimapModel` / `UiMatchHudMinimapMarkerModel` as the shell contract for retained minimap viewport, marker positions, marker visibility, and zoom/focus action availability.
- Added ECS-owned `UiMatchHudMinimapComponent` defaults on the shell boundary so minimap marker and viewport state no longer comes from USS-only placeholder values.
- Extended `UiShellEcsGateway` and `UiToolkitShellApplySystem` so minimap state is read from ECS and applied only through the thin managed UI Toolkit edge.
- Cached `MinimapPanel`, `Viewport`, retained minimap markers, and zoom/focus controls in `UiToolkitShellView`, and added `ApplyMatchHudMinimap()` for mechanical percent-position, visibility, and enabled-state updates.
- Added focused validation that mounted Match HUD minimap viewport style, marker positions/visibility, and zoom/focus enabled state apply fake read-model values and restore defaults.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=50` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Ran final static audit for SCN08 old-art references, ECS/UI separation, forbidden class names, TargetLockV02 asset usage, and UI action processor shape.
- Wrote Match HUD Phase 4 handoff at `Design/AgentReports/2026-06-19_ui-toolkit-match-hud-phase4-handoff.md`.

Blocked: None.

Match HUD reconciliation map:

| Canvas/reference item | UI Toolkit element | Runtime source | Binding rule |
| --- | --- | --- | --- |
| Root Match HUD content | `SCN08_MatchHudContent` | `UiShellCommandKind.EnterMatchHud` / `UiShellMode.MatchHud` | Mount into `MatchScreenSlot`; keep as live UI elements, not a flattened mockup. |
| Header/status | `HeaderContent`, `LogoPanel`, `CurrentOrderBanner`, `ResourceStrip`, `MenuButton` | Match HUD read model and shell route state | Apply resource/order values mechanically in the managed UI Toolkit apply edge. |
| Objectives | `ObjectivesPanel`, objective rows, `Elapsed` | Match objective read model | Text/icon state comes from ECS read model; no view-side mission logic. |
| Selected squad panel | `SelectedSquadPanel`, `Badge`, `Title`, `Subtitle`, `Portrait`, `HealthFill`, `HealthText`, `OrderValue` | Selection summary read model | Hidden when selection is empty; single/multi/squad/building values come from ECS read model. |
| Selected actions | `ReturnButton`, `DestroyButton`, `BoardButton` | UI action requests | Buttons enqueue request components only; availability comes from read model. |
| Passenger drawer | `PassengerChip`, `TransportPassengerDrawer`, `Scroll_View`, `Content`, `SCN08_PassengerItemView.uxml`, `ExitAllButton`, `CloseButton` | Transport passenger read model and UI action requests | Retain item rows/templates; hidden scrollbars match Canvas; actions enqueue passenger requests. |
| Threat jump | `ThreatJumpPanel`, `JumpButton` | Alert/threat read model and route/focus request | Jump enqueues focus request; panel data comes from read model. |
| Right rail | `PauseButton`, `SettingsButton`, `RightBuildCommand`, `RightSupportCommand` | UI action requests and popup route state | Build/support semantics stay distinct from bottom command aliases. |
| Squad tray | `SquadCard1` through `SquadCard5` | Squad tray read model | Five retained cards; selected/portrait/health/text state comes from ECS read model. |
| Command rail | `SelectCommand`, `MoveCommand`, `AttackCommand`, `HoldCommand`, `StopCommand`, `BuildCommand`, `ScanCommand`, `SupportCommand` | `UiActionRequestComponent` plus command-mode read model | Selected state is read-model driven; Select toggles selection mode; Build remains selected while popup is open. |
| Minimap | `MinimapPanel`, `Map`, `Viewport`, `ZoomIn`, `ZoomOut`, `ZoomFocus` | Minimap read model and UI requests | Pointer over minimap controls blocks world selection. |
| Feedback | `FeedbackPanel`, `Feedback`, `BoardAllButton`, `CancelButton` | Tactical feedback read model and action requests | Feedback actions enqueue ECS requests and never route to Main Menu directly. |

Tasks:

- Reconcile Match HUD UXML against the current Canvas HUD before binding, including selected squad panel, passenger drawer, passenger item template, five squad buttons, command rail, feedback panel/actions, right rail, minimap, and all button names.
- Replace `SCN08_MatchHudContent.prefab` with `SCN08_MatchHudContent.uxml` in UI Toolkit mode.
- Bind header, selected squad panel, minimap, command rail, squad tray, passenger drawer, feedback panel, and right quick rail.
- Convert command rail interactions to `UiActionRequestComponent` values for Select, Move, Attack, Scan, Support, Build, and other commands.
- Build action opens the Build Drawer popup and remains visually selected until the popup closes or another bottom command is selected.
- Select action toggles selection mode and deselects when drag selection completes.
- Selected squad panel reads title, subtitle, portrait, badge, current order, health bar, and action availability from ECS read models.
- Multi-selection summary portraits and labels use the generated cinematic combination sprites.
- Minimap and command interactions must not fall through to world selection.

Validation:

- Select, Move, Attack, Scan, Support, Build, debug fire, and selection mode still behave as current Canvas UI.
- Selected squad panel deactivates on match init and activates only when selection read model is non-empty.
- Single character, vehicle, aircraft, building, squad, and mixed selections display correct portrait and text.
- Command rail selected state stays in sync with command mode.

### Phase 5 - Build Popup

Status: Complete
Progress: 100% - 12 / 12 tracked items complete
Current step: Complete.
Completed steps:

- Reconciled Build Drawer UXML against the current Canvas popup before binding, including tabs, catalog scroll, retained catalog item template, production scroll, active production template, queued production template, build/rush/clear/close actions, detail labels, and hidden scrollbars.
- Added focused validation that the Build Drawer popup and all retained templates expose the required Canvas parity binding names and actionable UI Toolkit `Button` surfaces.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=51` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added configured `SCN09_BuildDrawerPopup.uxml` asset support to `UiToolkitShellView` and mounted it once under `PopupScreenSlot`.
- Cached the Build Drawer popup root, build panel, production panel, catalog scroll, production scroll, Build, Rush, Clear, and Close actions while keeping the popup hidden until popup presentation wiring is added.
- Added focused validation for Build Drawer popup mount, no duplicate mount on repeated shell mount, hidden-before-command state, and `ClearCache` cleanup.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=52` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Cached Build Drawer retained catalog item slots, production queue rows, active production row, detail labels, cost labels, icons, thumbnails, queue images, and action surfaces in `UiToolkitShellView`.
- Strengthened focused validation for retained catalog/queue/detail/icon bindings and `ClearCache` cleanup.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=52` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiBuildDrawerModel`, retained catalog item rows, retained queue rows, and active-production read models as sprite-free UI Toolkit shell contracts.
- Added `UiToolkitShellView.ApplyBuildDrawer()` to refresh cached Build Drawer templates in place, including visibility, labels, action enabled states, active production progress, catalog rows, and queue rows.
- Added focused validation that populated and reduced Build Drawer snapshots update retained rows without creating or destroying catalog/queue template instances.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=53` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiActionKind.CloseBuildDrawer`, registered the UI Toolkit Build Drawer `CloseButton` to enqueue it, and processed it in `UiActionRequestSystem`.
- Close now captures the UI click, hides only `UiShellPopupKind.BuildDrawer`, and enqueues `CancelActiveCommandMode` so Build selected state clears without routing to Main Menu.
- Added focused validation that Build Drawer close goes through the UI action boundary, does not enqueue a route request, and maps to `UiShellPopupIntent.Hide`.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=54` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiActionKind.BuildCatalogItem`, retained catalog row callbacks in `UiToolkitShellView`, and `UiBuildCatalogRequestComponent` on the UI shell ECS boundary.
- `UiActionRequestSystem` now maps Build Drawer catalog row clicks to ECS build catalog requests with the retained row payload and a request id while suppressing underlying world clicks.
- Added focused validation that retained catalog rows submit the expected payloads and that the ECS action request system owns the catalog request buffer path.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=55` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added typed Build Drawer production UI actions for Rush, Clear, active-production cancel, and queued-row cancel.
- Added `UiBuildProductionRequestComponent` on the UI shell ECS boundary so production queue surfaces enqueue ECS production requests instead of calling gameplay directly from the view.
- `UiActionRequestSystem` now maps production UI actions to `UiBuildProductionActionKind` values with queue slot payloads and request ids while suppressing underlying world clicks.
- Added focused validation that retained production surfaces submit the expected actions and that the ECS action request system owns the production request buffer path.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=56` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `ShowPopup` and `HidePopup` presentation handling in `UiToolkitShellView` so the Build Drawer popup and modal overlay are driven by ECS shell presentation commands.
- Added focused validation that `ShowPopup` reveals the Build Drawer popup with popup motion, `HidePopup` hides it, and the Build command/right Build command remain selected only while the Build Drawer read model is open.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=57` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiActionKind.BuildDrawerPrimaryBuild`, an unregisterable Build Drawer primary Build callback, and `UiBuildPrimaryRequestComponent` on the UI shell ECS boundary.
- `UiActionRequestSystem` now maps the primary Build action to an ECS request with a request id while suppressing underlying world clicks.
- Added focused validation that the primary Build button submits through the UI action boundary and that the ECS action request system owns the primary Build request buffer path.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=58` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added Build Drawer ECS read-model state on the UI shell boundary: detail component, active-production component, retained catalog row buffer, and retained queue row buffer.
- Added `UiShellRuntimeGateway.TryReadBuildDrawer` and `UiShellEcsGateway.TryReadBuildDrawer` so fixed-string ECS state maps to `UiBuildDrawerModel` at the presentation boundary.
- Updated `UiToolkitShellApplySystem` to read and apply Build Drawer snapshots through the thin managed UI Toolkit edge while Match HUD is active.
- Added focused validation that populated and empty Build Drawer ECS read-model snapshots apply through the runtime gateway into retained UI Toolkit rows.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=59` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Wrote Build Popup Phase 5 handoff at `Design/AgentReports/2026-06-19_ui-toolkit-build-popup-phase5-handoff.md`.
- Marked Phase 5 complete and advanced the tracker to Phase 6 - Build Placement Confirmation Bar.

Blocked: None.

Tasks:

- Reconcile Build Drawer UXML against the current Canvas popup before binding, including both scroll areas, item templates, close behavior, build/rush/clear actions, queue states, active production row, and hidden scrollbars.
- Replace `SCN09_BuildDrawerPopup.prefab` with `SCN09_BuildDrawerPopup.uxml` in UI Toolkit mode.
- Bind build catalog scroll, production queue scroll, active production row, close action, rush action, clear action, build action, labels, costs, and icons.
- Use retained item templates, not recreate/destroy item rows every refresh.
- Close action only closes the popup and clears the Build command selected state. It must not route to main menu.
- Build catalog actions enqueue ECS build request components.
- Production queue actions enqueue ECS production request components.

Validation:

- Two scroll areas work and vertical bars are hidden where the Canvas target hides them.
- Item portraits sit inside frames, not on top of chrome.
- Close only closes the popup.
- Build button remains selected while the popup is open.
- Opening the popup never changes match route.

### Phase 6 - Build Placement Confirmation Bar

Status: Complete
Progress: 100% - 8 / 8 tracked items complete
Current step: Phase complete.
Completed steps:

- Reconciled Build Placement Confirmation Bar UXML against the current Canvas bar before binding, including title, status, cost, duration, instruction, confirm/cancel/rotate actions, dedicated frame element, and explicit pointer blocking over the active bar only.
- Added focused validation that the Build Placement Confirmation Bar UXML exposes Canvas parity binding names, actionable UI Toolkit `Button` surfaces, separate action styles, frame styling, and pointer-event handling.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=60` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added configured `SCN08_BuildPlacementConfirmationBar.uxml` asset support to `UiToolkitShellView` and mounted it once under `MatchScreenSlot` in UI Toolkit mode.
- Cached the Build Placement Confirmation Bar title, status, cost, duration, instruction, cancel, rotate, and confirm bindings while keeping the bar hidden until read-model binding is added.
- Added focused validation for Build Placement Confirmation Bar mount, no duplicate mount on repeated shell mount, hidden-before-read-model state, and `ClearCache` cleanup.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=61` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiBuildPlacementConfirmationBarModel`, ECS boundary state, runtime gateway mapping, and `UiToolkitShellApplySystem` presentation binding for title, status, cost, duration, instruction, confirm, cancel, rotate, and valid/invalid visual state.
- Added Build Placement Confirmation Bar confirm/cancel/rotate callbacks in `UiToolkitShellView` that enqueue typed UI actions and stop pointer propagation.
- Mapped `BuildPlacementConfirm`, `BuildPlacementCancel`, and `BuildPlacementRotate` in `UiActionRequestSystem` to the existing `BuildingUiPlacementCommandRequestElement` queue rather than adding a parallel gameplay path.
- Added focused validation for read-model application, action submission, and placement command request wiring.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=62` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added `UiBuildPlacementReadModelSystem` as an `ISystem` producer that mirrors the existing `IBuildingUiCommand` active placement state into `UiBuildPlacementConfirmationBarComponent`.
- Wired UI Toolkit match bootstrap to configure and clear `UiBuildPlacementReadModelSource` from the loaded `MatchBootstrapSystem.BuildingUiCommandContract`.
- Added focused validation to keep the Build Placement producer as an `ISystem`, verify active placement visibility, and confirm Canvas-compatible status, cost, and duration formatting.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=63` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Guarded Build Placement confirm, cancel, and rotate callbacks so disabled UI Toolkit actions keep blocking pointer propagation but do not enqueue stale gameplay requests.
- Added focused validation that Build Placement actions only enqueue through enabled Toolkit elements and continue mapping to the existing `BuildingUiPlacementCommandRequestElement` request queue.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=63` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added fixed-width status, cost, duration, and single-line feedback slots to `SCN08_BuildPlacementConfirmationBar.uss` so changing placement state updates in-place instead of shifting layout.
- Added valid and invalid placement visual states that recolor status and confirm affordance without replacing UXML elements or changing bar structure.
- Added focused validation for valid-to-invalid feedback transitions, fixed feedback slots, clipped single-line text, and confirm disabling while cancel/rotate remain available.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=63` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that the Build Placement Confirmation Bar stays above the Match HUD footer and command rail band while the full-screen root remains pointer-transparent outside the active bar.
- Wrote Phase 6 handoff report: `Design/AgentReports/2026-06-19_ui-toolkit-build-placement-phase6-handoff.md`.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=63` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Blocked: None.

Tasks:

- Reconcile Build Placement Confirmation Bar UXML against the current Canvas bar before binding, including title, cost, confirm/cancel/rotate actions, valid/invalid feedback, and pointer blocking.
- Replace `SCN08_BuildPlacementConfirmationBar.prefab` with its UXML version.
- Bind confirm, cancel, rotate, cost, title, and feedback state.
- Actions enqueue existing building placement request components.
- The bar appears only during active placement and blocks world clicks over its rect.

Validation:

- Confirm, cancel, and rotate preserve current gameplay behavior.
- Placement feedback updates without layout shifts.
- Bar does not overlap Match HUD command rail.

### Phase 7 - Armory

Status: Complete
Progress: 100% - 8 / 8 tracked items complete
Current step: Phase complete.
Completed steps:

- Added focused validation that `SCN19_ArmoryContent.uxml` exposes Canvas-parity Armory bindings for header, left category nav, middle catalog, retained item template, right inspection panel, bottom tabs, filter/sort controls, and upgrade/equip/close actions.
- Added focused validation that `SCN19_ArmoryItemView.uxml` remains an actionable retained item template with selected/default/locked state binding surfaces.
- Added focused validation that the Armory stylesheet uses the generated new-art-direction Armory asset set and does not reference stale Armory target locks.
- Added `UiToolkitShellView` support for a dedicated `SCN19_ArmoryContent.uxml` asset, retained mounting into `ArmoryScreenSlot`, hidden-by-default startup state, Armory route reveal, and Main Menu route return hiding.
- Added focused validation that the Armory route uses the dedicated Armory screen when configured instead of leaving the Canvas/Main Menu body as the only route target.
- Added retained Armory runtime bindings for the roster `ScrollView`, item rows, category buttons, inspection labels, bottom tabs, and action surfaces.
- Added Armory item selection state handling so unlocked rows swap selected/default USS state and locked rows reject selection without losing the previous selected item.
- Added thin managed apply-edge support for the ECS Armory category read model; category clicks enqueue through `UiShellRuntimeGateway.TryEnqueueArmoryCategory`.
- Added focused validation for Armory retained bindings, category requests, selected/default/locked item behavior, and apply-system category synchronization.
- Added `SCN19_ArmoryItemView.uxml` retained bindings for subtitle, primary/secondary stat rows, and action availability state without introducing old art-direction assets.
- Added Armory item-template USS support for subtitle, stat rows, available action marker, and locked action marker using generated Armory assets.
- Added focused validation that the Armory item template exposes locked/selected/default, rarity, portrait/art, title, subtitle, stat rows, progress, level/type, and action availability bindings.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=68` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that the Armory roster scroll/catalog uses retained item rows: repeated category read-model applies and item selections preserve catalog child count and row object references.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=69` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that Armory selected/default/locked runtime classes map to the generated SCN19 roster frame sprites, selection restores default state on the previous row, and inspection labels follow the selected item.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=70` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Added focused validation that every locked Armory retained row keeps locked text/badge/frame state, rejects selection/action trigger attempts, preserves the previous valid selection, and does not mutate inspection labels or enqueue UI actions.
- Ran Unity validation with `[UiToolkitCanvasMigrationValidation] result=Passed tests=71` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Wrote Phase 7 handoff report: `Design/AgentReports/2026-06-20_ui-toolkit-armory-phase7-handoff.md`.

Blocked: None.

Tasks:

- Reconcile Armory UXML against the current Canvas Armory behavior before binding, but keep the new-art-direction header and screen assets. Add any missing item/state icons through imagegen instead of old sprites.
- Replace `SCN19_ArmoryContent.prefab` with `SCN19_ArmoryContent.uxml` in UI Toolkit mode.
- Bind roster scroll, item template, selected/default/locked frame state, bottom tabs, details panel, and actions.
- Item selection updates `Frame` sprite state equivalent through USS classes.
- Item template supports locked, selected, default, rarity, portrait, title, subtitle, stat rows, and action availability.

Validation:

- Scroll view recycles cleanly or uses retained item rows without per-frame allocation.
- Selecting an item swaps to selected visual state.
- Locked rows use locked state and cannot trigger unavailable actions.

### Phase 8 - Commander/Profile

Status: Not started
Progress: 0% - 0 / 6 tracked items complete
Current step: Convert the commander/profile content screen to UI Toolkit.
Completed steps:

- None yet.

Blocked: None.

Tasks:

- Convert the commander/profile content screen to UI Toolkit.
- Bind tab row, portrait, title, subtitle, badge, stats, ability rows, and back action.
- Back action writes a route request to return to main menu content while preserving the persistent header.

Validation:

- Left/right/middle content swaps animate consistently with main menu rules.
- Header remains visible and unchanged.
- Text stays inside tabs, panels, and frames.

### Phase 9 - Result, Victory, Loss, And Other Popups

Status: Not started
Progress: 0% - 0 / 7 tracked items complete
Current step: Convert result popups, victory/loss screens, mission result panels, settings/mail popups, and any remaining shell overlays.
Completed steps:

- None yet.

Blocked: None.

Tasks:

- Convert result popups, victory/loss screens, mission result panels, settings/mail popups, and any remaining shell overlays.
- All popups scale from center on show and scale back to center on hide.
- Popup layer always renders above loading only when loading is inactive; loading overrides popups when loading is active.
- Result confirm writes a route request to loading, then main menu.

Validation:

- Popup show/hide motion is shared and consistent.
- Result confirm does not directly mutate gameplay state outside ECS route/result requests.
- Loading covers popups during route transitions.

### Phase 10 - Remove Canvas Runtime Dependency

Status: Not started
Progress: 0% - 0 / 8 tracked items complete
Current step: Replace runtime Canvas shell references in startup/composition with UI Toolkit references when `RuntimeUiConfig` is `UiToolkit`.
Completed steps:

- None yet.

Blocked: None.

Tasks:

- Replace runtime Canvas shell references in startup/composition with UI Toolkit references when `RuntimeUiConfig` is `UiToolkit`.
- Remove direct runtime dependencies on Canvas views for migrated screens.
- Keep old Canvas prefabs only as archived fallback until user approves deletion.
- Replace any UI-bound `SystemBase` that only exists to manipulate Canvas with UI Toolkit read-model/request flow or remove it.
- Convert newly unmanaged, stateless UI request systems to `ISystem` where practical.

Validation:

- Runtime scan finds no active `UnityEngine.UI`, `TMPro`, `Canvas`, `CanvasGroup`, or `RectTransform` dependency in the UI Toolkit shell path.
- Architecture tests pass for forbidden names, assembly boundaries, GC rules, and ECS/UI ownership.
- The remaining managed UI Toolkit edge is classified as intentional managed boundary.

## Visual Quality Gates

Each screen must pass these before moving to the next phase:

- Accepted new-art-direction reference mockup exists under the screen's `Design/VisualLockLayered/<SurfaceId>/reference/` folder.
- 16:9 screenshot comparison against the accepted target.
- 20:9 screenshot comparison against the accepted target.
- No panel overlaps.
- No content touching panel chrome unless the target explicitly does it.
- Icons centered by visible alpha bounds, not transparent texture bounds.
- Text centered and fully inside its safe rect.
- Scroll item portraits inside frames, not floating over frames.
- Loading and popup sort order correct.
- No old mockup screenshots or obsolete assets used as runtime UI.
- Every required Canvas behavior/text/icon/state is represented by a new-art UI Toolkit element or by an explicit missing-asset task.

## Performance Gates

- No recurring per-frame allocations from element lookup, template cloning, string formatting, LINQ, closures, or list recreation in hot UI refresh paths.
- Cache queried `VisualElement` handles once during binding.
- Use retained row pools for catalogs, queues, roster items, squad cards, and passenger rows.
- Gate string formatting behind changed read-model values.
- Push calculations into Burst-compatible `ISystem` read-model builders whenever they only touch unmanaged ECS data.
- Keep `UiToolkitShellApplySystem` as the thin managed copy-to-UI layer; do not move read-model calculation into it.
- Profile Match HUD and Build Popup open state after warmup.
- Document any residual UI Toolkit managed allocation under `Design/AgentReports`.

## Test And Validation Plan

Run validation in the main project when available. Use the documented shadow Unity project only when the main editor is locked.

Required validation:

- Unity compile: no errors and no new warnings.
- UI Toolkit asset import validation for every UXML/USS file.
- No-old-art validation: UI Toolkit UXML/USS/runtime bindings must not reference old art-direction folders, old visual-lock folders, or Canvas-era sprite sheets.
- Read-model split validation: gameplay/UI state calculations live in `ISystem` or pure data helpers where unmanaged, while `SystemBase` is limited to managed UI Toolkit apply.
- View contract validation: UI Toolkit `*View` classes do not contain `Update`, `LateUpdate`, gameplay polling, route decisions, command validation, or direct gameplay mutation.
- Runtime shell smoke: Loading -> Main Menu -> Loading -> Match HUD -> Build Popup -> Build Placement -> Result -> Loading -> Main Menu.
- Input smoke: UI clicks do not select world units/buildings under the UI.
- Route smoke: profile/back, deploy, result confirm.
- Match HUD smoke: select mode, move mode, build popup, scan/support positions, debug fire.
- Build Drawer smoke: open, close, build request, queue display, retained rows.
- Screenshot captures: 16:9 and 20:9 for Loading, Main Menu, Match HUD, Build Popup, Armory.
- Architecture tests: forbidden names, assembly boundaries, ECS contract, Burst/hot-path classifications.
- GC warmup profile for Match HUD and Build Popup.

## Risks And Mitigations

- UI Toolkit animation differs from Canvas transforms. Mitigate with one shared motion system using style translate, scale, opacity, and transition ids.
- UI Toolkit text rendering may differ from TextMesh Pro. Mitigate by validating font assets, font size, line height, and UI Builder visibility per screen.
- Sprite slicing and USS backgrounds may not match Image.Type.Sliced exactly. Mitigate by checking each panel against safe rects and replacing only specific panel styles.
- Scroll views can allocate if rebuilt repeatedly. Mitigate with retained rows and changed-value refresh.
- Mixed Canvas and UI Toolkit can cause input conflicts during migration. Mitigate with `RuntimeUiConfig` and one active runtime shell mode at a time.
- Canvas parity can accidentally pull old visual assets into the new UI Toolkit path. Mitigate by treating Canvas as behavior/text reference only and running no-old-art validation before each phase closes.
- Managed UI apply code can grow into another mixed Canvas-style system. Mitigate by reviewing each added branch: if it calculates gameplay/UI policy from ECS state, move it to an unmanaged read-model/request system.

## Completion Criteria

The Canvas replacement is complete when:

- `RuntimeUiConfig` defaults to `UiToolkit`.
- Loading, Main Menu, Match HUD, Build Popup, Build Placement Bar, Armory, Commander/Profile, and result popups all run through UI Toolkit.
- Existing Canvas shell prefabs are no longer referenced by active runtime startup.
- Canvas assets remain only as archived fallback or are deleted after approval.
- The UI Toolkit shell passes route, input, visual, architecture, and GC validation.
- UI-bound `SystemBase` classes that became unnecessary are removed or converted according to the ECS/Burst roadmap.
- Remaining UI Toolkit `SystemBase` code is only the documented managed presentation edge and does not contain gameplay or UI policy calculation.
