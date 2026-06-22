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

- Checklist progress: `40 / 144 complete (27.8%)`.
- In progress: `0`.
- Remaining open: `104`.
- Current target: `Phase 2 - shell/header/global background Canvas chrome pass`.
- Active Canvas shell/modal surfaces target-matched: `0 / 12`.
- Secondary/reference Canvas popup surfaces target-matched: `1 / 6`.
- Secondary/reference Canvas popup baseline captured or decisioned: `6 / 6`.
- Shared Canvas chrome baseline status: `asset map and contact sheet complete; left-nav state seed applied to active Main Menu and Armory Canvas nav instances; Main Menu mode-card state seed applied; PopupFrameView shared modal seed and UIShellAppCanvas placeholder modal fallback seed applied; shared chrome material audit confirms default UI materials on seeded chrome`.
- Button/selectable interaction standard status: `left-nav route button, Main Menu mode-card, PopupFrame close-button, and shell placeholder close-button state seeds applied; broader button/card/selectable audit pending`.
- Responsive CanvasScaler validation status: `not started`.
- Performance validation status: `Phase 0 shadow batchmode baselines captured for Main Menu and Match HUD with Canvas active vs disabled; render counter recorder returned zero draw/batch values in batchmode, so real Game View/device profiling remains separate`.
- Shadow-project validation status: `Canvas main menu/deploy UI fallback validation passed at 1280x720, 1920x1080, 2400x1080, and 4800x2160; 4800x2160, 1920x1080, and 2400x1080 captures passed for reachable Loading, Armory, Match HUD, Build Drawer, and Build Placement Bar surfaces; 4800x2160 captures passed for active Mission Result, Confirm Raid, End Of Day Report, and Intel Reveal modal prefabs; 4800x2160 captures passed for secondary/reference Ability Upgrade Detail, Build Placement Panel, Pause Menu, Popup Frame, Reward Unlock, and Threat Alert popup prefabs; Main Menu and Match HUD Canvas active/disabled performance baselines passed; shared left-nav state seed 4800x2160 Main Menu and Armory captures passed; Main Menu and Armory left-nav overlap validation passed at 1920x1080; Main Menu header/logo scale validation passed at 1280x720; Main Menu route smoke passed after mode-card state seed; PopupFrame target-lock seed 4800x2160 modal capture passed; Main Menu route smoke passed after UIShellAppCanvas placeholder modal fallback seed in /Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Main-project validation status: `RuntimeUiConfig is now Canvas by default; no main-project capture validation yet`.

Recent slice notes:

- Applied the shared card state seed to the existing SCN-02 Main Menu mode cards without renaming runtime-bound objects: the transparent hotspot remains the raycast target, while each Button now targets the visible full-frame card Image for default/hover/pressed/selected/disabled sprite swapping.
- Applied the shared popup foundation to `PopupFrameView.prefab`: sliced Target Lock panel frame, header bar, close button frame, and full-frame close button hover/pressed/selected states.
- Applied the same Target Lock modal fallback seed to the inline `UIShellAppCanvas.prefab` placeholder modal: sliced panel frame, rectangular close button frame, and full-frame close button hover/pressed/selected states.
- Confirmed seeded nav, card, and popup chrome Images still use the default UI material (`m_Material: {fileID: 0}`); real draw/batch profiling remains a Phase 8 validation gate.
- Saved shadow validation artifacts under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/shared/`: `shadow_scn02_mode_card_state_seed_4800x2160.png`, `shadow_popup_frame_target_lock_seed_4800x2160.png`, and `shadow_scn02_shell_placeholder_seed_4800x2160.png`.
- Verified the shared left-navigation reuse contract for the Canvas Phase 2 pass: SCN-02 Main Menu and SCN-19 Armory use the same seeded left-nav style, while SCN-08 Match HUD remains excluded from menu nav/header reuse.
- Captured focused Phase 2 shadow evidence under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/phase2/`: Main Menu and Armory left-nav overlap at `1920x1080`, Main Menu header/logo scale at `1280x720`, and header/nav crop artifacts.
- Kept the header treatment/header-reuse checklist open because the current Canvas header still needs a dedicated pass before it is claimed as the final shared header baseline.

## Decision

Canvas is the preferred runtime target for this migration because the recent UI Toolkit Target Lock implementation is visually useful but has shown heavy frame cost on the main menu. This tracker ports the look, not the UI Toolkit runtime architecture.

The implementation should therefore favor:

- existing Canvas prefabs and scene bindings;
- sliced sprites, sprite states, Canvas Selectable transitions, and prefab variants;
- stable CanvasScaler behavior across aspect ratios;
- low rebuild cost and low overdraw;
- no per-frame visual scripts unless already present and justified.

## Active Canvas Scope

These are the active Canvas shell and modal prefabs confirmed from scene and prefab bindings:

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
| POP-05 Mission Result | `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab` | `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_NewMainMenuArtDirection_TargetLock_V01.png` |
| Confirm Raid | `Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab` | Use shared Target Lock modal style until a specific reference exists |
| End Of Day Report | `Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab` | Use shared Target Lock modal style until a specific reference exists |
| Intel Reveal | `Assets/Game/Prefabs/UI/Popups/IntelRevealPopup.prefab` | Use shared Target Lock modal style until a specific reference exists |

Commander Profile reachability note:

- `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab` exists as a Canvas prefab and remains in this migration scope for art-direction parity.
- It is not currently installed by `UIShellContentView`; the live Canvas content system exposes Loading, Main Menu, Armory, Match HUD, Build Drawer, and Build Placement Bar only.
- The current `UIRouterView.screenPrefabs` entries in `UIShellAppCanvas.prefab` and `Menu.unity` reference GUIDs that are not present as asset `.meta` files under `Assets`, so the legacy router path is not reliable for SCN-03 capture.
- UI Toolkit mounts Commander Profile through `UIRoute.CommandFeed`; Canvas has no equivalent active route install path at Phase 0.

Secondary or reference Canvas popup prefabs discovered during Phase 0 inventory:

| Surface | Canvas prefab | Status rule |
| --- | --- | --- |
| Ability Upgrade Detail | `Assets/Game/Prefabs/UI/Popups/AbilityUpgradeDetailPopup.prefab` | Audit active usage before styling |
| Build Placement Panel | `Assets/Game/Prefabs/UI/Popups/BuildPlacementPanel.prefab` | Audit overlap with shell build placement bar |
| Pause Menu | `Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab` | Style if still active in match flow |
| Popup Frame | `Assets/Game/Prefabs/UI/Popups/PopupFrameView.prefab` | Prefer as shared popup chrome foundation |
| Reward Unlock | `Assets/Game/Prefabs/UI/Popups/RewardUnlockPopup.prefab` | Audit active usage before styling |
| Threat Alert | `Assets/Game/Prefabs/UI/Popups/ThreatAlertPopup.prefab` | Style if still active in match flow |
| Reference POP-05 shell prefab | `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab` | Not bound by Canvas runtime at Phase 0; keep as visual/reference material unless later wired |

Settings and Inbox were checked during Phase 0. No active Canvas `Settings` or `Inbox` prefab was found under `Assets/Game/Prefabs/UI`; both are currently UI Toolkit-only popup assets in `UiToolkitShellView`.

## Phase 0 Inventory Notes

2026-06-22 slice 01:

- `Assets/Game/Data/UI/RuntimeUiConfig.asset` is set to `mode: 0`, so `MenuBootstrapView.ApplyRuntimeUiMode()` enables the Canvas path by default and disables the UI Toolkit shell root/document.
- `Assets/Game/Scenes/Menu.unity` binds `MenuBootstrapView` to `RuntimeUiConfig`, `uiCanvas`, `uiToolkitDocument`, `uiToolkitShellRoot`, and `uiToolkitShellView`.
- `UIShellContentView` scene fields confirm the active Canvas route prefabs: loading, main menu, armory, match HUD, build drawer popup, and build placement confirmation bar.
- `UIShellAppCanvas.prefab` confirms additional active Canvas modal bindings through `WarlineCaptureMatchResultFlow` and `WarlineCaptureOperationModalFlow`: mission result, confirm raid, end-of-day report, and intel reveal.
- `UIShellAppCanvas.prefab` currently uses CanvasScaler Scale With Screen Size, reference resolution `1672x941`, screen match mode `MatchWidthOrHeight`, match `0.5`, and reference pixels per unit `100`.
- Active Canvas runtime-bound component classes found on shell/content/modal prefabs: `ArmoryCatalogItemView`, `ArmoryInspectionPanelView`, `ArmoryRightContentView`, `BattleHudRuntimeFeedbackView`, `BuildDrawerCatalogRuntimeView`, `BuildDrawerItemView`, `BuildDrawerQueueItemView`, `BuildDrawerView`, `BuildPlacementConfirmationBarView`, `MainMenuNavigationView`, `MatchHudFooterContentView`, `MatchHudMinimapView`, `MatchHudObjectivesElapsedView`, `MatchHudRightQuickRailView`, `MatchHudSelectionPanelView`, `MatchHudSquadTrayView`, `MatchHudTransportPassengerDrawerView`, `MatchHudTransportPassengerItemView`, `MatchOverlayCommandControlsView`, `MatchOverlayCommandTabGroupView`, `UIAccessibilityApplier`, `UIModalView`, `UIPopupCloseButtonView`, `UIPopupCloseView`, `UIRouterView`, `UISafeAreaView`, `UIShellContentSectionsView`, `UIShellLoadingProgressView`, `UIShellRouteButtonView`, `WarlineCaptureMatchResultFlow`, `WarlineCaptureOperationModalFlow`, and `WarlineCaptureShellResultConfirmButtonView`.
- Runtime-bound section/component names must be preserved during visual work, especially shell region sections, `SCN09_BuildDrawerPopup`, build drawer tabs/cards/queue/detail controls, `SCN08_MatchHudContent`, command controls, squad tray, minimap, right quick rail, `SCN19_ArmoryContent`, armory catalog and inspection panel roots, and modal popup roots.

2026-06-22 slice 02:

- Baseline inventory artifact created at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/phase0_canvas_inventory.md`.
- Protected serialized fields and GameObject names are recorded there before any Canvas prefab visual edits.
- CanvasScaler inventory is recorded there. The Menu scene runtime canvas uses `4800x2160`, while the `UIShellAppCanvas.prefab` source still uses `1672x941`; future size tuning must validate against the live scene canvas, not prefab preview alone.

2026-06-22 slice 03:

- `CanvasMenuFallbackValidation.Run` now accepts editor-only screenshot path and resolution environment variables while preserving the old defaults.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for validation.
- Shadow Canvas main menu/deploy UI validation passed at `1280x720` (`luma=0.103`), `1920x1080` (`luma=0.092`), and `4800x2160` (`luma=0.111`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: these captures prove the Canvas main menu/deploy UI path only; the all-active-surface screenshot gates remain open.

2026-06-22 slice 04:

- `CanvasMenuFallbackValidation.RunRouteCapture` added as editor-only validation tooling.
- The route capture helper accepts `WARLINE_CANVAS_ROUTE`, `WARLINE_CANVAS_POPUP`, `WARLINE_CANVAS_SCREENSHOT_PATH`, `WARLINE_CANVAS_SCREENSHOT_WIDTH`, and `WARLINE_CANVAS_SCREENSHOT_HEIGHT`.
- The helper drives only existing Canvas presentation methods: menu route body swaps, Match HUD command presentation, and Build Drawer popup install. It does not change runtime gameplay/UI behavior.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow route capture passed for `Armory` at `4800x2160` (`luma=0.112`), `Match` at `4800x2160` (`luma=0.055`), and `Match + BuildDrawer` at `4800x2160` (`luma=0.106`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Loading, Commander Profile, Build Placement Bar, secondary/reference popups, 1920x1080 route captures, wide-aspect captures, and FPS/rebuild baselines remain open.

2026-06-22 slice 05:

- `CanvasMenuFallbackValidation.RunRouteCapture` now supports the existing Canvas `ShowLoading` command through `WARLINE_CANVAS_ROUTE=Splash`.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow route capture passed for `Splash`/SCN-01 Loading at `4800x2160` (`luma=0.375`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Commander Profile, Build Placement Bar, secondary/reference popups, 1920x1080 route captures beyond Main Menu, wide-aspect captures, and FPS/rebuild baselines remained open after this slice.

2026-06-22 slice 06:

- `CanvasMenuFallbackValidation.RunRouteCapture` now supports `WARLINE_CANVAS_OVERLAY=BuildPlacementBar`.
- The overlay capture binds a fake editor-only `IBuildingUiCommand` to the existing `BuildPlacementConfirmationBarView` after Match HUD install, so the placement bar can render for baseline evidence without entering gameplay placement or changing runtime behavior.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow overlay capture passed for `Match + BuildPlacementBar` at `4800x2160` (`luma=0.068`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Commander Profile reachability, secondary/reference popups, 1920x1080 route captures beyond Main Menu, wide-aspect captures, and FPS/rebuild baselines remain open.

2026-06-22 slice 07:

- Commander Profile reachability was audited before capture work continued.
- `SCN03_CommanderProfileContent.prefab` remains a Canvas art-direction target, but it is not mounted by the current Canvas `UIShellContentView`; UI Toolkit owns the live `CommandFeed` commander profile route.
- Legacy `UIRouterView.screenPrefabs` GUID references in `UIShellAppCanvas.prefab` and `Menu.unity` do not resolve to asset `.meta` files under `Assets`, so the legacy Canvas router cannot be used as authoritative capture evidence.
- Shadow route capture passed at `1920x1080` for `Splash`/SCN-01 Loading (`luma=0.357`), `Armory` (`luma=0.092`), `Match` (`luma=0.044`, using the lower static-HUD threshold), `Match + BuildDrawer` (`luma=0.089`), and `Match + BuildPlacementBar` (`luma=0.055`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: secondary/reference popups, wide-aspect captures, and FPS/rebuild baselines remain open.

2026-06-22 slice 08:

- Shadow wide-aspect captures passed at `2400x1080` for main menu/deploy UI (`luma=0.103`), `Splash`/SCN-01 Loading (`luma=0.377`), `Armory` (`luma=0.160`), `Match` (`luma=0.059`), `Match + BuildDrawer` (`luma=0.109`), and `Match + BuildPlacementBar` (`luma=0.068`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: secondary/reference popups and FPS/rebuild baselines remain open.

2026-06-22 slice 09:

- `CanvasMenuFallbackValidation.RunRouteCapture` now supports `WARLINE_CANVAS_MODAL` for editor-only popup prefab screenshot baselines.
- Supported modal keys are `MissionResult`, `ConfirmRaid`, `EndOfDayReport`, `IntelReveal`, `AbilityUpgradeDetail`, `BuildPlacementPanel`, `PauseMenu`, `PopupFrame`, `RewardUnlock`, and `ThreatAlert`.
- The modal capture helper instantiates the configured popup prefab under the active Canvas in PlayMode only; it does not change runtime route, gameplay, ECS, or prefab behavior.
- Shadow modal captures passed at `4800x2160` for `MissionResult` (`luma=0.986`), `ConfirmRaid` (`luma=0.976`), `EndOfDayReport` (`luma=0.954`), and `IntelReveal` (`luma=0.966`).
- High luma is expected from the current baseline because these modal prefabs are still mostly light placeholder styling; Target Lock styling remains future work.
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: secondary/reference popup captures or usage decisions and FPS/rebuild baselines remain open.

2026-06-22 slice 10:

- Secondary/reference popup usage was audited before styling work.
- `AbilityUpgradeDetailPopup`, `BuildPlacementPanel`, and `PopupFrameView` are not directly installed by the active Canvas `UIShellContentView` route/popup path.
- `Pause`, `ThreatAlert`, and `RewardUnlock` exist in `UiShellPopupKind`/ECS popup requests, but the current Canvas content view only installs the `BuildDrawer` popup prefab directly; these popup prefabs therefore need a wiring/usage decision before final art polish is treated as runtime-active.
- All six secondary/reference popup prefab baselines were still captured for visual decision material in the shadow project at `4800x2160`: `AbilityUpgradeDetail` (`luma=0.995`), `BuildPlacementPanel` (`luma=0.679`), `PauseMenu` (`luma=0.514`), `PopupFrame` (`luma=0.049`), `RewardUnlock` (`luma=0.928`), and `ThreatAlert` (`luma=0.617`).
- `PopupFrame` uses a lower screenshot luma threshold because it is intentionally sparse shared chrome; this only affects editor-only baseline validation.
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: FPS measurements and Canvas rebuild/draw-call baselines remain open before visual prefab edits begin.

2026-06-22 slice 11:

- `CanvasMenuFallbackValidation.RunCanvasPerformanceBaseline` was added as editor-only baseline tooling.
- The helper opens the Menu scene, forces Canvas mode, installs either Main Menu or Match HUD, and measures a fixed warmup/sample window with the runtime Canvas active or disabled.
- Shadow performance baselines passed with `90` warmup frames and `240` sample frames: Main Menu Canvas active (`avgMs=0.434`, `fps=2303.0`, `p95Ms=0.513`), Main Menu Canvas disabled (`avgMs=0.678`, `fps=1475.1`, `p95Ms=1.303`), Match HUD Canvas active (`avgMs=0.611`, `fps=1637.4`, `p95Ms=0.916`), and Match HUD Canvas disabled (`avgMs=0.890`, `fps=1124.2`, `p95Ms=1.121`).
- These are relative editor batchmode smoke baselines, not real Game View/device FPS. The values are intentionally recorded only to catch large regressions during Canvas prefab styling.
- Unity render `ProfilerRecorder` counters returned `0.0` for draw calls, batches, SetPass, triangles, and vertices in this batchmode path; draw-call proof must come from Game View/Frame Debugger if needed.
- No Canvas rebuild warnings were emitted in the captured performance logs; Unity domain reload `RebuildCommonClasses` lines are editor startup noise and not Canvas rebuild warnings.
- Logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Phase 0 evidence gates are complete; next work can begin with shared Canvas chrome and sprite-state foundation.

2026-06-22 slice 12:

- Shared Canvas chrome mapping was created at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/shared/canvas_shared_chrome_asset_map.md`.
- The map ties approved SCN-02 UI Toolkit sprites to Canvas usage for header frames, logo, resource chips, header icon buttons, left navigation, card frames, label plates, HUD panels, rectangular buttons, square buttons, Build Drawer tabs, and Build Drawer card highlights.
- Import audit notes confirm the SCN-02 shared menu chrome is already imported under `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/`, HUD/shared utility chrome under `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/`, and Build Drawer chrome under `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/`.
- PPU audit note: SCN-02 shared chrome is consistently `spritePixelsToUnits: 100`, while several HUD button/panel sprites intentionally import at `300`; do not normalize these globally until Canvas screenshots prove the mismatch causes distortion.
- 9-slice audit note: SCN-02 frame assets already have usable sprite borders; some Build Drawer tab/card sprites have zero borders and must be used fixed-size or border-tuned before heavy stretching.
- Scope note: no runtime, scene, prefab, or UI Toolkit files were changed in this slice; only mapping documentation was added.

2026-06-22 slice 13:

- `Assets/Game/Prefabs/UI/Components/MainMenuLeftNavButton.prefab` was updated as the shared Canvas left-nav state seed.
- The state seed now uses `scn02c_nav_button_frame_default.png` for the normal/disabled frame and `scn02c_nav_button_frame_selected.png` for highlighted, pressed, and selected states.
- The Button target graphic now points at the full `Frame` Image instead of the transparent removed `Hotspot` Image, so hover/selected states replace the whole chrome frame.
- Active left-nav instances in `SCN02_MainMenuContent.prefab` and `SCN19_ArmoryContent.prefab` were updated as well because both screen prefabs remove the source `Hotspot` object and add their own Button components on the instance root.
- Existing static selected-route intent was preserved by replacing old `scn02_nav_button_selected_frame.png` overrides with the approved `scn02c_nav_button_frame_selected.png`; old inactive-frame overrides were replaced with `scn02c_nav_button_frame_default.png`.
- Import decision: no sprite import changes were needed in this slice. The SCN-02 nav and mode-card frames already have mipmaps enabled for scaled chrome, while thinner header/resource/HUD button frames keep mipmaps disabled and default uncompressed texture settings for sharp edges.
- Shared chrome contact sheet saved at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/shared/canvas_shared_chrome_contact_sheet.png`.
- Shadow validation passed for the shared left-nav state seed at `4800x2160` on Main Menu (`shadow_scn02_left_nav_state_seed_4800x2160.png`, `luma=0.122`) and Armory (`shadow_scn19_left_nav_state_seed_4800x2160.png`, `luma=0.140`) in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `git diff --check` passed after the prefab and tracker updates.
- Scope note: only Canvas prefabs, tracker markdown, and visual evidence artifacts changed; no runtime C#, scene, gameplay, ECS, route behavior, or UI Toolkit files were edited.

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

- [x] Confirm all active Canvas shell content prefabs and popup prefabs from scene and route bindings.
- [x] Confirm whether Settings and Inbox have active Canvas prefabs or are UI Toolkit-only.
- [x] Inventory runtime-bound component scripts on every active Canvas prefab.
- [x] Record which serialized field names and GameObject names must not be renamed.
- [x] Inventory current CanvasScaler settings on menu and match canvases.
- [x] Capture baseline 4800x2160 Canvas screenshots for all active shell surfaces.
- [x] Capture baseline 1920x1080 Canvas screenshots for all active shell surfaces.
- [x] Capture baseline wide-aspect Canvas screenshots for all active shell surfaces.
- [x] Capture baseline screenshots for all active secondary popups.
- [x] Capture current FPS for menu Canvas active vs Canvas disabled.
- [x] Capture current FPS for match HUD Canvas active vs Canvas disabled.
- [x] Record current draw calls, batches, and Canvas rebuild warnings where available.
- [x] Create `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- [x] Save baseline captures and notes under the Canvas visual match folder.
- [x] Run `git diff --check` before implementation edits.

Acceptance:

- Active Canvas targets are known.
- Baseline visuals and performance are captured.
- No prefab editing starts from guesswork.

## Phase 1 - Shared Canvas Chrome And Asset Foundation

Goal:
Create the reusable Canvas art foundation before per-screen tuning.

- [x] Map approved UI Toolkit SCN-02 header sprites to Canvas Image/Sliced Image usage.
- [x] Map approved UI Toolkit SCN-02 left nav sprites to Canvas button templates.
- [x] Map shared panel, card, chip, divider, tab, and square-button sprites.
- [x] Identify which Target Lock art is already imported for Canvas and which needs import/meta tuning.
- [x] Audit Pixel Per Unit for every shared Canvas chrome sprite.
- [x] Audit 9-slice borders for every shared Canvas frame/button/card sprite.
- [x] Enable mipmaps only for large scaled-down background/chrome sprites that need them.
- [x] Confirm texture compression keeps thin Target Lock chrome sharp.
- [x] Create or update a shared Canvas popup frame using `PopupFrameView` where active.
- [x] Create or update a shared Canvas button state set: default, hover, selected, disabled, pressed.
- [x] Create or update a shared Canvas card state set: default, hover, selected, disabled, pressed.
- [x] Verify shared state sprites cover the whole chrome frame, not only inner content.
- [x] Confirm static shared chrome can batch cleanly with existing Canvas materials.
- [x] Save a shared chrome contact sheet under `_CanvasTargetLockVisualMatch/shared/`.
- [x] Run `git diff --check`.

Acceptance:

- Shared visual primitives exist before screen-specific copies multiply.
- PPU and 9-slice decisions are recorded.

## Phase 2 - Shell, Header, Left Navigation, And Global Background

Goal:
Make the Canvas shell match the approved Target Lock visual language while preserving the shell structure.

- [ ] Update `UIShellAppCanvas.prefab` static background strategy without increasing menu overdraw unnecessarily.
- [ ] Port the approved SCN-02 logo/header treatment into Canvas shell/header regions.
- [x] Port the approved SCN-02 left navigation background into Canvas.
- [x] Update `MainMenuLeftNavButton.prefab` to use the shared Target Lock button states.
- [ ] Confirm menu-adjacent screens reuse the same header prefab/style.
- [x] Confirm menu-adjacent screens reuse the same left navigation prefab/style.
- [x] Keep Match HUD excluded from menu header/nav reuse.
- [x] Validate left nav does not overlap the middle region at 4800x2160.
- [x] Validate left nav does not overlap the middle region at 1920x1080.
- [x] Validate header text/logo scale does not become oversized at lower resolutions.
- [x] Capture shell/header/nav focused crops.
- [x] Run `git diff --check`.

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
- [x] PopupFrameView: make it the shared Target Lock modal foundation where feasible.
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
