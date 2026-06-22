# UI Canvas Target Lock Art Direction Tracker

Purpose:
Update the existing Unity Canvas screens and popups to use the approved Target Lock art direction currently proven in the UI Toolkit work, while keeping the runtime on Canvas for performance and stability.

This is a Canvas visual migration tracker. It is not a UI Toolkit rewrite, not an ECS task, and not a gameplay behavior migration.

Last updated:
2026-06-22

Approved visual source:

- `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md`
- `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`
- Approved SCN-02 shared chrome baseline from the UI Toolkit main menu pass.
- Latest Target Lock reference mockups under `Design/VisualLockLayered/**/reference/`.

## Progress Snapshot

- Checklist progress: `0 / 144 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `144`.
- Current target: `Phase 0 - inventory active Canvas routes, prefabs, references, and performance baseline`.
- Active Canvas shell surfaces target-matched: `0 / 8`.
- Secondary Canvas popup surfaces target-matched: `0 / 10`.
- Shared Canvas chrome baseline status: `not started`.
- Button/selectable interaction standard status: `not started`.
- Responsive CanvasScaler validation status: `not started`.
- Performance validation status: `not started`.
- Shadow-project validation status: `not started`.
- Main-project validation status: `not started; use only when explicitly needed or requested`.

## Decision

Canvas is the preferred runtime target for this migration because the recent UI Toolkit Target Lock implementation is visually useful but has shown heavy frame cost on the main menu. This tracker ports the look, not the UI Toolkit runtime architecture.

The implementation should therefore favor:

- existing Canvas prefabs and scene bindings;
- sliced sprites, sprite states, Canvas Selectable transitions, and prefab variants;
- stable CanvasScaler behavior across aspect ratios;
- low rebuild cost and low overdraw;
- no per-frame visual scripts unless already present and justified.

## Active Canvas Scope

These are the active shell prefabs discovered at tracker creation:

| Surface | Canvas prefab | Reference source |
| --- | --- | --- |
| Shell | `Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab` | Approved shared shell/chrome contract |
| SCN-01 Loading | `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` | `Design/VisualLockLayered/SCN-01_SplashLoading/reference/SCN-01_SplashLoading_NewMainMenuArtDirection_TargetLock_V04.png` |
| SCN-02 Main Menu | `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` | `Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/reference/scn02c_target_lock_warline_capture_bright.png` |
| SCN-03 Commander Profile | `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab` | `Design/VisualLockLayered/SCN-03_CommanderProfile/reference/SCN-03_CommanderProfile_NewMainMenuArtDirection_TargetLock_V01.png` |
| SCN-08 Match HUD | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` | `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png` |
| SCN-08 Build Placement Bar | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab` | `Design/VisualLockLayered/SCN-08_BuildPlacementConfirmationBar/reference/SCN-08_BuildPlacementConfirmationBar_NewMainMenuArtDirection_TargetLock_V01.png` |
| SCN-09 Build Drawer Popup | `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab` | `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_NewMainMenuArtDirection_TargetLock_V03.png` |
| SCN-19 Armory | `Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab` | `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_NewMainMenuArtDirection_TargetLock_V04.png` |
| POP-05 Mission Result | `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab` | `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_NewMainMenuArtDirection_TargetLock_V01.png` |

Secondary Canvas popup prefabs discovered at tracker creation:

| Surface | Canvas prefab | Status rule |
| --- | --- | --- |
| Ability Upgrade Detail | `Assets/Game/Prefabs/UI/Popups/AbilityUpgradeDetailPopup.prefab` | Audit active usage before styling |
| Build Placement Panel | `Assets/Game/Prefabs/UI/Popups/BuildPlacementPanel.prefab` | Audit overlap with shell build placement bar |
| Confirm Raid | `Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab` | Audit active usage before styling |
| End Of Day Report | `Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab` | Audit active usage before styling |
| Intel Reveal | `Assets/Game/Prefabs/UI/Popups/IntelRevealPopup.prefab` | Audit active usage before styling |
| Legacy Mission Result | `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab` | Reconcile with shell POP-05 before styling |
| Pause Menu | `Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab` | Style if still active in match flow |
| Popup Frame | `Assets/Game/Prefabs/UI/Popups/PopupFrameView.prefab` | Prefer as shared popup chrome foundation |
| Reward Unlock | `Assets/Game/Prefabs/UI/Popups/RewardUnlockPopup.prefab` | Audit active usage before styling |
| Threat Alert | `Assets/Game/Prefabs/UI/Popups/ThreatAlertPopup.prefab` | Style if still active in match flow |

Settings and Inbox must be inventoried from the active route/popup bindings. If they are currently UI Toolkit-only, mark them `not applicable for Canvas` instead of inventing Canvas prefabs.

## Allowed Write Scope

Allowed by default:

- `Assets/Game/Prefabs/UI/**/*.prefab`
- `Assets/Game/Art/UI/**/*.png`
- `Assets/Game/Art/UI/**/*.png.meta`
- existing UI sprite/font/material assets under `Assets/Game/**/UI/**` when the asset is already used by Canvas UI;
- Canvas-only animation controllers or transition assets only when they already belong to the target UI prefab family;
- `Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`
- `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/**`
- narrowly scoped editor-only screenshot/validation tooling when needed for static Canvas preview evidence.

Forbidden unless separately approved:

- gameplay, ECS, composition, match logic, production logic, or route behavior changes;
- UI Toolkit UXML/USS changes as part of this Canvas migration;
- scene rewiring outside the target UI Canvas/prefab validation path;
- replacing live UI with a baked full-screen screenshot;
- adding new `Update`, `LateUpdate`, coroutine polling, or runtime visual controllers;
- changing data values to make a visual mockup look right;
- deleting UI Toolkit work or Canvas fallback assets.

## Shared Art Direction Rules

These rules override pixel-level mockup matching when they conflict:

- Reuse the approved SCN-02 main menu header/chrome for main-menu-adjacent Canvas screens.
- Reuse the approved SCN-02 left navigation style for main-menu-adjacent Canvas screens; only icons, labels, and active route change.
- Match HUD owns its own gameplay header and may differ from menu chrome.
- If a reference uses one large baked multi-section background, rebuild it as separate Canvas panels like the approved UI Toolkit SCN-02 right commander area.
- Every button-like or selectable control family must have visible default, hover/focus, selected/current, disabled, and pressed/impact states.
- Selected and hover states should be chrome-level state sprites or full-frame state treatments, not small translucent overlays.
- Repeated cards/buttons must use one template; a highlighted mockup card is a reusable state example, not a one-off layout.
- Text must be readable at all target aspects, and button captions must remain fully visible.
- Padding must be symmetrical inside repeated components unless the mockup and data justify an explicit exception.

## Canvas Performance Rules

Canvas migration is only successful if the UI remains cheap enough at runtime.

- Keep static backgrounds out of high-rebuild Canvas groups where practical.
- Do not place huge full-screen transparent images over the entire screen unless they are necessary and batched.
- Prefer sliced sprites over multiple stacked decorative images.
- Avoid nested LayoutGroups on hot, frequently updated panels unless the panel is small and measured.
- Avoid ContentSizeFitter/LayoutElement combinations that rebuild every frame.
- Split dynamic panels from static chrome so data updates do not dirty the whole screen.
- Use atlased sprites and compatible materials where possible.
- Use mipmaps only for large sprites that are scaled down materially; do not blur small icons.
- Record FPS and profiler observations before and after each major surface pass.
- Compare active Canvas FPS against the same scene with the target UI object disabled when investigating regressions.

## Validation Loop

Use this loop for every screen or popup:

1. Inspect the active Canvas prefab, runtime bindings, and current screenshot before editing.
2. Identify the matching UI Toolkit approved surface and Target Lock reference.
3. Classify mismatches as `sprite`, `9-slice`, `PPU`, `layout`, `padding`, `font`, `state`, `responsive`, `content`, `performance`, or `artifact`.
4. Fix sprite import, Pixel Per Unit, and 9-slice issues before compensating with layout.
5. Apply one coherent visual-only prefab/art slice.
6. Sync allowed files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` when available.
7. Validate static Canvas/Game View captures in the shadow project first.
8. Capture at least `4800x2160`, `1920x1080`, and one wide aspect used by the project when the screen is responsive.
9. Create focused crops for every major panel family, repeated card family, and button family.
10. Compare against the mockup and the approved UI Toolkit screen.
11. Run `git diff --check`.
12. Update this tracker with progress, artifact paths, and validation status.
13. Continue only when the current surface passes a full panel-by-panel visual audit or has a recorded user-approved exception.

## Phase 0 - Inventory, Baseline, And Safety

Goal:
Know exactly which Canvas surfaces are active, how they are bound, and what the current performance/visual baseline is before styling.

- [ ] Confirm all active Canvas shell content prefabs and popup prefabs from scene and route bindings.
- [ ] Confirm whether Settings and Inbox have active Canvas prefabs or are UI Toolkit-only.
- [ ] Inventory runtime-bound component scripts on every active Canvas prefab.
- [ ] Record which serialized field names and GameObject names must not be renamed.
- [ ] Inventory current CanvasScaler settings on menu and match canvases.
- [ ] Capture baseline 4800x2160 Canvas screenshots for all active shell surfaces.
- [ ] Capture baseline 1920x1080 Canvas screenshots for all active shell surfaces.
- [ ] Capture baseline wide-aspect Canvas screenshots for all active shell surfaces.
- [ ] Capture baseline screenshots for all active secondary popups.
- [ ] Capture current FPS for menu Canvas active vs Canvas disabled.
- [ ] Capture current FPS for match HUD Canvas active vs Canvas disabled.
- [ ] Record current draw calls, batches, and Canvas rebuild warnings where available.
- [ ] Create `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- [ ] Save baseline captures and notes under the Canvas visual match folder.
- [ ] Run `git diff --check` before implementation edits.

Acceptance:

- Active Canvas targets are known.
- Baseline visuals and performance are captured.
- No prefab editing starts from guesswork.

## Phase 1 - Shared Canvas Chrome And Asset Foundation

Goal:
Create the reusable Canvas art foundation before per-screen tuning.

- [ ] Map approved UI Toolkit SCN-02 header sprites to Canvas Image/Sliced Image usage.
- [ ] Map approved UI Toolkit SCN-02 left nav sprites to Canvas button templates.
- [ ] Map shared panel, card, chip, divider, tab, and square-button sprites.
- [ ] Identify which Target Lock art is already imported for Canvas and which needs import/meta tuning.
- [ ] Audit Pixel Per Unit for every shared Canvas chrome sprite.
- [ ] Audit 9-slice borders for every shared Canvas frame/button/card sprite.
- [ ] Enable mipmaps only for large scaled-down background/chrome sprites that need them.
- [ ] Confirm texture compression keeps thin Target Lock chrome sharp.
- [ ] Create or update a shared Canvas popup frame using `PopupFrameView` where active.
- [ ] Create or update a shared Canvas button state set: default, hover, selected, disabled, pressed.
- [ ] Create or update a shared Canvas card state set: default, hover, selected, disabled, pressed.
- [ ] Verify shared state sprites cover the whole chrome frame, not only inner content.
- [ ] Confirm static shared chrome can batch cleanly with existing Canvas materials.
- [ ] Save a shared chrome contact sheet under `_CanvasTargetLockVisualMatch/shared/`.
- [ ] Run `git diff --check`.

Acceptance:

- Shared visual primitives exist before screen-specific copies multiply.
- PPU and 9-slice decisions are recorded.

## Phase 2 - Shell, Header, Left Navigation, And Global Background

Goal:
Make the Canvas shell match the approved Target Lock visual language while preserving the shell structure.

- [ ] Update `UIShellAppCanvas.prefab` static background strategy without increasing menu overdraw unnecessarily.
- [ ] Port the approved SCN-02 logo/header treatment into Canvas shell/header regions.
- [ ] Port the approved SCN-02 left navigation background into Canvas.
- [ ] Update `MainMenuLeftNavButton.prefab` to use the shared Target Lock button states.
- [ ] Confirm menu-adjacent screens reuse the same header prefab/style.
- [ ] Confirm menu-adjacent screens reuse the same left navigation prefab/style.
- [ ] Keep Match HUD excluded from menu header/nav reuse.
- [ ] Validate left nav does not overlap the middle region at 4800x2160.
- [ ] Validate left nav does not overlap the middle region at 1920x1080.
- [ ] Validate header text/logo scale does not become oversized at lower resolutions.
- [ ] Capture shell/header/nav focused crops.
- [ ] Run `git diff --check`.

Acceptance:

- Shared shell chrome is visually consistent and responsive.
- Header/nav can be reused by later screen passes.

## Phase 3 - Menu Screens

Goal:
Update Canvas menu screens using the shared shell, header, and left nav baseline.

- [ ] SCN-02 Main Menu: update center mode cards to approved Target Lock card style.
- [ ] SCN-02 Main Menu: update right commander panel as separate live Canvas panels, not a baked multi-section image.
- [ ] SCN-02 Main Menu: update footer/deploy controls with full interaction states.
- [ ] SCN-02 Main Menu: validate readable text and clean panel alignment at all target aspects.
- [ ] SCN-03 Commander Profile: reuse shared header and left nav.
- [ ] SCN-03 Commander Profile: split profile/stat/loadout areas into clean panel sections.
- [ ] SCN-03 Commander Profile: update portrait, rank, stats, and action buttons.
- [ ] SCN-03 Commander Profile: validate repeated rows and action states.
- [ ] SCN-19 Armory: reuse shared header and left nav.
- [ ] SCN-19 Armory: update catalog cards with full default/hover/selected/disabled/pressed states.
- [ ] SCN-19 Armory: update right inspection panel as separate live sections.
- [ ] SCN-19 Armory: ensure right-side buttons are readable, large enough, and visible.
- [ ] SCN-19 Armory: validate tabs update visually without layout shifts.
- [ ] SCN-19 Armory: validate card portraits and selected detail imagery stay live.
- [ ] Capture focused crops for every menu panel family.
- [ ] Run `git diff --check`.

Acceptance:

- Menu screens look like one product family.
- No screen carries a one-off header or left navigation style.

## Phase 4 - Match HUD And Gameplay Canvas Surfaces

Goal:
Update gameplay Canvas surfaces without hurting runtime performance or gameplay bindings.

- [ ] SCN-08 Match HUD: inventory every runtime-bound HUD element name before editing.
- [ ] SCN-08 Match HUD: update unique gameplay header/resources/current-order area.
- [ ] SCN-08 Match HUD: update selected-unit/selection details panel.
- [ ] SCN-08 Match HUD: update objectives/status panels.
- [ ] SCN-08 Match HUD: update minimap and right quick-rail panels.
- [ ] SCN-08 Match HUD: update command buttons with visible hover/selected/focus/press impact states.
- [ ] SCN-08 Match HUD: update all squad cards from one repeated template.
- [ ] SCN-08 Match HUD: ensure selected squad state is a full chrome state, not a partial overlay.
- [ ] SCN-08 Match HUD: ensure squad card health/progress/value text never overlaps chrome.
- [ ] SCN-08 Match HUD: validate all HUD panels panel-by-panel before moving on.
- [ ] SCN-08 Build Placement Bar: update rail, preview, cost, time, rotate, cancel, and confirm controls.
- [ ] SCN-08 Build Placement Bar: validate the bar stays readable and anchored at all target aspects.
- [ ] SCN-09 Build Drawer Popup: update tabs, catalog cards, right detail, queue, and progress panels.
- [ ] SCN-09 Build Drawer Popup: ensure build progress panel is hidden by default and only shown when active.
- [ ] SCN-09 Build Drawer Popup: ensure tab changes update card portraits and selected detail imagery.
- [ ] SCN-09 Build Drawer Popup: validate scrolling content has no clipped card buttons.
- [ ] Capture focused crops for command buttons, squad cards, drawer cards, and build placement rail.
- [ ] Run `git diff --check`.

Acceptance:

- Gameplay UI remains live, readable, and performant.
- No runtime-bound names are renamed or removed.

## Phase 5 - Popups And Modal Surfaces

Goal:
Bring Canvas popups into the same Target Lock modal language.

- [ ] POP-05 Mission Result: reconcile shell popup vs legacy MissionResult popup usage.
- [ ] POP-05 Mission Result: update modal frame, result header, stat rail, objectives, rewards, casualties, score, and footer actions.
- [ ] POP-05 Mission Result: validate victory/defeat/neutral states.
- [ ] Pause Menu: update frame, mission info, settings, resume, retry, quit, and footer controls if active.
- [ ] Threat Alert: update alert frame, icon, severity state, message, and action controls if active.
- [ ] Confirm Raid: update confirmation frame, risk/reward rows, and confirm/cancel states if active.
- [ ] Reward Unlock: update reward card, icon/portrait, rarity state, and claim controls if active.
- [ ] Intel Reveal: update reveal panel, image, text hierarchy, and close/continue controls if active.
- [ ] End Of Day Report: update summary sections, stat rows, charts, rewards, and action controls if active.
- [ ] Ability Upgrade Detail: update detail panel, upgrade rows, requirements, and action controls if active.
- [ ] Build Placement Panel legacy popup: either retire as inactive or align with build placement shell style.
- [ ] PopupFrameView: make it the shared Target Lock modal foundation where feasible.
- [ ] Ensure every popup close button has hover/focus/pressed states.
- [ ] Ensure every destructive or confirm action has distinct but consistent state styling.
- [ ] Validate popup readability at 4800x2160.
- [ ] Validate popup readability at 1920x1080.
- [ ] Capture focused modal crops for every active popup.
- [ ] Run `git diff --check`.

Acceptance:

- Active popups share one premium modal language.
- Inactive legacy popups are documented before any styling work is skipped.

## Phase 6 - Interaction, Motion, And State Polish

Goal:
Make controls feel premium without adding runtime polling or layout instability.

- [ ] Audit every Button, Toggle, selectable card, tab, and row in active Canvas prefabs.
- [ ] Add default, highlighted/hover, pressed, selected/current, disabled, and focused visuals where supported.
- [ ] Use sprite-swap or color-tint transitions consistently per control family.
- [ ] Add subtle scale/impact animation only through existing Canvas selectable/animator mechanisms.
- [ ] Confirm hover/selected states cover the full chrome frame where the mockup shows frame replacement.
- [ ] Confirm state transitions do not move neighboring layout or cause overlap.
- [ ] Confirm selected/current state can move to any repeated item at runtime.
- [ ] Confirm disabled/locked state remains readable but clearly unavailable.
- [ ] Capture focused state contact sheets for button and card families.
- [ ] Run `git diff --check`.

Acceptance:

- Interactive states are visible, consistent, and reusable.
- No new MonoBehaviour update loop is introduced for visual polish.

## Phase 7 - Responsive Layout And CanvasScaler Pass

Goal:
Make Canvas visuals stay clean across the same aspect ranges the game uses.

- [ ] Record the existing CanvasScaler mode and reference resolution before any changes.
- [ ] Decide whether the Canvas reference should remain current settings or move to the Target Lock 4800x2160 authoring reference.
- [ ] Validate 4800x2160 layout for every active surface.
- [ ] Validate 1920x1080 layout for every active surface.
- [ ] Validate wide aspect layout for every active surface.
- [ ] Validate popup anchoring on menu and match scenes.
- [ ] Validate text does not become oversized at lower resolutions.
- [ ] Validate text does not become unreadably small at high resolutions.
- [ ] Validate left nav never overlaps middle content.
- [ ] Validate right panels and drawers stay inside the safe area.
- [ ] Validate HUD bottom tray/squad panels remain aligned and unclipped.
- [ ] Validate scroll views preserve usable viewport height.
- [ ] Save responsive comparison contact sheets.
- [ ] Run `git diff --check`.

Acceptance:

- Canvas behaves like a stable responsive UI, not a one-resolution mockup.

## Phase 8 - Performance And Regression Gates

Goal:
Prove the Canvas art migration does not recreate the UI Toolkit FPS problem.

- [ ] Measure menu FPS with Canvas active after shared shell pass.
- [ ] Measure menu FPS with Canvas disabled after shared shell pass.
- [ ] Measure menu FPS with Canvas active after all menu surfaces.
- [ ] Measure menu FPS with Canvas disabled after all menu surfaces.
- [ ] Measure match HUD FPS with Canvas active after HUD pass.
- [ ] Measure match HUD FPS with Canvas disabled after HUD pass.
- [ ] Inspect Canvas rebuild profiler markers on static menu screens.
- [ ] Inspect Canvas rebuild profiler markers on dynamic match HUD screens.
- [ ] Reduce overdraw from large transparent images where profiler/captures show cost.
- [ ] Split static and dynamic Canvas groups when dynamic updates dirty too much static chrome.
- [ ] Confirm large scaled art has appropriate mipmap/import settings.
- [ ] Confirm repeated cards/buttons are batched where practical.
- [ ] Confirm no runtime errors are introduced in editor logs.
- [ ] Run focused Unity validation in the shadow project when available.
- [ ] Run main-project validation only when explicitly needed or requested.
- [ ] Run `git diff --check`.

Acceptance:

- Canvas remains materially cheaper than the rejected heavy UI Toolkit menu path.
- No visual pass is accepted with unresolved runtime errors.

## Phase 9 - Final Audit And Handoff

Goal:
Finish with a traceable, reusable Canvas art system.

- [ ] Recount checklist progress and update this snapshot.
- [ ] Confirm every active Canvas surface has final screenshots.
- [ ] Confirm every active popup has final screenshots or documented inactive status.
- [ ] Confirm every button/selectable family has state evidence.
- [ ] Confirm every PPU/9-slice change is recorded.
- [ ] Confirm no forbidden files were edited.
- [ ] Confirm all `.meta` files are preserved.
- [ ] Run `git diff --check`.
- [ ] Record final validation status and remaining risks.
- [ ] Mark automation complete only after all active Canvas surfaces and validation gates are complete.

Acceptance:

- The Canvas UI carries the Target Lock art direction with stable performance.
- The tracker can be used later as a regression checklist.
