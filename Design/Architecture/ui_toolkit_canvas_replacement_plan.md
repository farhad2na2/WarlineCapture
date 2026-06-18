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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

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

Status: [ ]

Tasks:

- Convert the commander/profile content screen to UI Toolkit.
- Bind tab row, portrait, title, subtitle, badge, stats, ability rows, and back action.
- Back action writes a route request to return to main menu content while preserving the persistent header.

Validation:

- Left/right/middle content swaps animate consistently with main menu rules.
- Header remains visible and unchanged.
- Text stays inside tabs, panels, and frames.

### Phase 9 - Result, Victory, Loss, And Other Popups

Status: [ ]

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

Status: [ ]

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
