# Phase 0 Canvas Inventory

Date:
2026-06-22

Purpose:
Record active Canvas targets, protected bindings, and current CanvasScaler settings before Target Lock Canvas visual work begins.

## Runtime UI Mode

- `Assets/Game/Data/UI/RuntimeUiConfig.asset` is set to `mode: 0`.
- `mode: 0` maps to `RuntimeUiMode.Canvas`.
- `MenuBootstrapView.ApplyRuntimeUiMode()` uses this setting to enable the Canvas path and disable the UI Toolkit shell root/document.

## Active Canvas Route And Modal Bindings

`Assets/Game/Scenes/Menu.unity` binds `MenuBootstrapView` to:

- `runtimeUiConfig`
- `uiCanvas`
- `uiToolkitDocument`
- `uiToolkitShellRoot`
- `uiToolkitShellView`

`UIShellContentView` active route prefabs:

- `loadingContentPrefab` -> `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`
- `mainMenuContentPrefab` -> `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab`
- `armoryContentPrefab` -> `Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab`
- `matchHudContentPrefab` -> `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `buildDrawerPopupPrefab` -> `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`
- `buildPlacementConfirmationBarPrefab` -> `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab`

`UIShellAppCanvas.prefab` modal flow prefabs:

- `missionResultPopupPrefab` -> `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`
- `confirmRaidPopupPrefab` -> `Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab`
- `endOfDayReportPopupPrefab` -> `Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab`
- `intelRevealPopupPrefab` -> `Assets/Game/Prefabs/UI/Popups/IntelRevealPopup.prefab`

No active Canvas Settings or Inbox prefab was found under `Assets/Game/Prefabs/UI`. Settings and Inbox are currently UI Toolkit-only popup assets on `UiToolkitShellView`.

## CanvasScaler Inventory

Menu scene runtime canvas:

- File: `Assets/Game/Scenes/Menu.unity`
- Canvas object: `GameUICanvas`
- `m_UiScaleMode: 1`
- `m_ReferencePixelsPerUnit: 100`
- `m_ReferenceResolution: {x: 4800, y: 2160}`
- `m_ScreenMatchMode: 1`
- `m_MatchWidthOrHeight: 0.5`
- `m_DynamicPixelsPerUnit: 1`

Canvas shell prefab source:

- File: `Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab`
- `m_UiScaleMode: 1`
- `m_ReferencePixelsPerUnit: 100`
- `m_ReferenceResolution: {x: 1672, y: 941}`
- `m_ScreenMatchMode: 0`
- `m_MatchWidthOrHeight: 0.5`
- `m_DynamicPixelsPerUnit: 1`

Design implication:

- The live Menu scene currently has a Target Lock-friendly `4800x2160` Canvas reference, while the prefab source still has an older `1672x941` reference.
- Do not tune visual sizes from prefab preview alone without checking the scene override.
- Before changing CanvasScaler values, capture 4800x2160, 1920x1080, and wide aspect comparisons.

## Shadow Canvas Baseline Evidence

Main menu/deploy UI fallback:

- `shadow_canvas_menu_fallback_1280x720.png`
- `shadow_canvas_menu_fallback_1280x720.log`
- `shadow_canvas_menu_fallback_1920x1080.png`
- `shadow_canvas_menu_fallback_1920x1080.log`
- `shadow_canvas_menu_fallback_4800x2160.png`
- `shadow_canvas_menu_fallback_4800x2160.log`
- `shadow_canvas_menu_fallback_2400x1080.png`
- `shadow_canvas_menu_fallback_2400x1080.log`

Route/popup captures from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:

- `shadow_canvas_loading_route_4800x2160.png`
- `shadow_canvas_loading_route_4800x2160.log`
- `shadow_canvas_loading_route_1920x1080.png`
- `shadow_canvas_loading_route_1920x1080.log`
- `shadow_canvas_loading_route_2400x1080.png`
- `shadow_canvas_loading_route_2400x1080.log`
- `shadow_canvas_armory_route_4800x2160.png`
- `shadow_canvas_armory_route_4800x2160.log`
- `shadow_canvas_armory_route_1920x1080.png`
- `shadow_canvas_armory_route_1920x1080.log`
- `shadow_canvas_armory_route_2400x1080.png`
- `shadow_canvas_armory_route_2400x1080.log`
- `shadow_canvas_match_route_4800x2160.png`
- `shadow_canvas_match_route_4800x2160.log`
- `shadow_canvas_match_route_1920x1080.png`
- `shadow_canvas_match_route_1920x1080.log`
- `shadow_canvas_match_route_2400x1080.png`
- `shadow_canvas_match_route_2400x1080.log`
- `shadow_canvas_build_drawer_popup_4800x2160.png`
- `shadow_canvas_build_drawer_popup_4800x2160.log`
- `shadow_canvas_build_drawer_popup_1920x1080.png`
- `shadow_canvas_build_drawer_popup_1920x1080.log`
- `shadow_canvas_build_drawer_popup_2400x1080.png`
- `shadow_canvas_build_drawer_popup_2400x1080.log`
- `shadow_canvas_build_placement_bar_4800x2160.png`
- `shadow_canvas_build_placement_bar_4800x2160.log`
- `shadow_canvas_build_placement_bar_1920x1080.png`
- `shadow_canvas_build_placement_bar_1920x1080.log`
- `shadow_canvas_build_placement_bar_2400x1080.png`
- `shadow_canvas_build_placement_bar_2400x1080.log`
- `shadow_canvas_modal_mission_result_4800x2160.png`
- `shadow_canvas_modal_mission_result_4800x2160.log`
- `shadow_canvas_modal_confirm_raid_4800x2160.png`
- `shadow_canvas_modal_confirm_raid_4800x2160.log`
- `shadow_canvas_modal_end_of_day_report_4800x2160.png`
- `shadow_canvas_modal_end_of_day_report_4800x2160.log`
- `shadow_canvas_modal_intel_reveal_4800x2160.png`
- `shadow_canvas_modal_intel_reveal_4800x2160.log`
- `shadow_canvas_modal_ability_upgrade_detail_4800x2160.png`
- `shadow_canvas_modal_ability_upgrade_detail_4800x2160.log`
- `shadow_canvas_modal_build_placement_panel_4800x2160.png`
- `shadow_canvas_modal_build_placement_panel_4800x2160.log`
- `shadow_canvas_modal_pause_menu_4800x2160.png`
- `shadow_canvas_modal_pause_menu_4800x2160.log`
- `shadow_canvas_modal_popup_frame_4800x2160.png`
- `shadow_canvas_modal_popup_frame_4800x2160.log`
- `shadow_canvas_modal_reward_unlock_4800x2160.png`
- `shadow_canvas_modal_reward_unlock_4800x2160.log`
- `shadow_canvas_modal_threat_alert_4800x2160.png`
- `shadow_canvas_modal_threat_alert_4800x2160.log`
- `shadow_canvas_perf_mainmenu_active.log`
- `shadow_canvas_perf_mainmenu_disabled.log`
- `shadow_canvas_perf_match_active.log`
- `shadow_canvas_perf_match_disabled.log`

Commander Profile reachability note:

- `SCN03_CommanderProfileContent.prefab` exists as Canvas content, but it is not installed by the active `UIShellContentView` route system.
- UI Toolkit owns the live commander profile route through `UIRoute.CommandFeed`.
- The legacy Canvas `UIRouterView.screenPrefabs` GUID references in `UIShellAppCanvas.prefab` and `Menu.unity` do not resolve to asset `.meta` files under `Assets`, so this path is not reliable capture evidence.

Open evidence gates:

- None for Phase 0 tracker baseline. Real Game View/device FPS and Frame Debugger draw-call proof remain separate profiling work if needed.

## Protected Serialized Fields

Do not rename or disconnect these fields during visual-only prefab work.

Shell and routing:

- `UIShellContentView.shellView`
- `UIShellContentView.loadingContentPrefab`
- `UIShellContentView.mainMenuContentPrefab`
- `UIShellContentView.armoryContentPrefab`
- `UIShellContentView.matchHudContentPrefab`
- `UIShellContentView.buildDrawerPopupPrefab`
- `UIShellContentView.buildPlacementConfirmationBarPrefab`
- `UIShellContentSectionsView.sections`
- `UIShellContentSectionsView.SectionReference.sectionId`
- `UIShellContentSectionsView.SectionReference.sectionRoot`
- `UIShellRegionView.regionId`
- `UIShellRegionView.regionRoot`
- `UIShellRegionView.contentRoot`
- `UIShellRegionView.canvasGroup`
- `UIRouterView.initialRoute`
- `UIRouterView.contentRoot`
- `UIRouterView.screens`
- `UIRouterView.screenPrefabs`

Menu and route buttons:

- `MainMenuNavigationView.tabs`
- `MainMenuNavigationTabView.tabId`
- `MainMenuNavigationTabView.button`
- `MainMenuNavigationTabView.frame`
- `MainMenuNavigationTabView.label`
- `UIShellRouteButtonView.intent`
- `UIShellRouteButtonView.route`
- `UIShellRouteButtonView.pushHistory`

Armory:

- `ArmoryContentListView.unitPrefabRegistryConfig`
- `ArmoryContentListView.buildingPlacementConfig`
- `ArmoryContentListView.contentRoot`
- `ArmoryContentListView.itemTemplate`
- `ArmoryCatalogItemView.selectionButton`
- `ArmoryCatalogItemView.frameImage`
- `ArmoryCatalogItemView.defaultFrameSprite`
- `ArmoryCatalogItemView.selectedFrameSprite`
- `ArmoryCatalogItemView.titleText`
- `ArmoryCatalogItemView.typeText`
- `ArmoryRightContentView.inspectionPanel`
- `ArmoryInspectionPanelView` text/stat/capability fields

Match HUD:

- `MatchHudSelectionPanelView`
- `MatchHudFooterContentView.commandControls`
- `MatchHudFooterContentView.runtimeFeedback`
- `MatchHudFooterContentView.minimap`
- `MatchHudFooterContentView.squadTray`
- `MatchOverlayCommandControlsView.selectButton`
- `MatchOverlayCommandControlsView.moveButton`
- `MatchOverlayCommandControlsView.attackButton`
- `MatchOverlayCommandControlsView.scanButton`
- `MatchOverlayCommandControlsView.buildButton`
- `MatchOverlayCommandControlsView.holdButton`
- `MatchOverlayCommandControlsView.stopButton`
- `MatchOverlayCommandControlsView.commandWheelStopButton`
- `MatchOverlayCommandControlsView.commandWheelPanel`
- `MatchOverlayCommandControlsView.commandTabGroup`
- `MatchOverlayCommandTabGroupView.tabs`
- `MatchOverlayCommandTabGroupView.defaultSelectedIndex`
- `MatchHudMinimapView.mapImage`
- `MatchHudMinimapView.mapRect`
- `MatchHudMinimapView.viewportRect`
- `MatchHudMinimapView.zoomInButton`
- `MatchHudMinimapView.zoomOutButton`
- `MatchHudMinimapView.markerRoot`
- `MatchHudRightQuickRailView.buildButton`

Build drawer and placement:

- `BuildDrawerView.drawerRoot`
- `BuildDrawerView.closeButton`
- `BuildDrawerView.tabs`
- `BuildDrawerView.selectedTabFrameSprite`
- `BuildDrawerView.normalTabFrameSprite`
- `BuildDrawerView.itemContentRoot`
- `BuildDrawerView.itemTemplate`
- `BuildDrawerView.selectedItemFrameSprite`
- `BuildDrawerView.previewImage`
- `BuildDrawerView.thumbnailImage`
- `BuildDrawerView.nameText`
- `BuildDrawerView.roleText`
- `BuildDrawerView.descriptionText`
- `BuildDrawerView.creditsCostText`
- `BuildDrawerView.suppliesCostText`
- `BuildDrawerView.productionTimeText`
- `BuildDrawerView.placementText`
- `BuildDrawerView.requirementsText`
- `BuildDrawerView.buildButton`
- `BuildDrawerView.orderButton`
- `BuildDrawerView.primaryActionLabelText`
- `BuildDrawerView.productionPanel`
- `BuildDrawerView.productionPanelActive`
- `BuildDrawerView.noProductionView`
- `BuildDrawerView.queueContentRoot`
- `BuildDrawerView.queuedItemTemplate`
- `BuildDrawerView.activeItemView`
- `BuildDrawerView.queueProgressSlider`
- `BuildDrawerView.cancelButton`
- `BuildDrawerView.rushButton`
- `BuildDrawerView.clearButton`
- `BuildDrawerItemView.selectionButton`
- `BuildDrawerItemView.frameImage`
- `BuildDrawerItemView.thumbnailImage`
- `BuildDrawerQueueItemView.cancelButton`
- `BuildDrawerQueueItemView.progressSlider`
- `BuildPlacementConfirmationBarView.root`
- `BuildPlacementConfirmationBarView.titleText`
- `BuildPlacementConfirmationBarView.statusText`
- `BuildPlacementConfirmationBarView.costText`
- `BuildPlacementConfirmationBarView.durationText`
- `BuildPlacementConfirmationBarView.instructionText`
- `BuildPlacementConfirmationBarView.cancelButton`
- `BuildPlacementConfirmationBarView.rotateButton`
- `BuildPlacementConfirmationBarView.confirmButton`

Modal and popups:

- `UIModalView.modalOverlay`
- `UIModalView.placeholderContent`
- `UIModalView.placeholderTitleText`
- `UIModalView.placeholderBodyText`
- `UIModalView.closeButton`
- `UIPopupCloseView.closeButton`
- `UIPopupCloseView.popupRoot`
- `UIPopupCloseView.commandModeToClear`
- `UIPopupCloseButtonView.closeView`
- `UIPopupCloseButtonView.runtimeFeedbackView`
- `UIPopupMotionView.motionRoot`
- `UIPopupMotionView.canvasGroup`
- `WarlineCaptureMatchResultFlow.missionResultPopupPrefab`
- `WarlineCaptureOperationModalFlow.confirmRaidPopupPrefab`
- `WarlineCaptureOperationModalFlow.endOfDayReportPopupPrefab`
- `WarlineCaptureOperationModalFlow.intelRevealPopupPrefab`

## Protected GameObject Names

Preserve names used as runtime or visual binding landmarks:

- `SCN01_LoadingContent`
- `SCN02_MainMenuContent`
- `HeaderContent`
- `HeaderBackPlate`
- `HeaderLogoPanel`
- `HeaderResourceArea`
- `LeftContent`
- `LeftNavPanel`
- `MiddleContent`
- `RightContent`
- `FooterContent`
- `MenuBackgroundContent`
- `SettingsButton`
- `InboxButton`
- `SCN03_CommanderProfileContent`
- `SCN08_MatchHudContent`
- `CommandRail`
- `CommandButtons`
- `CommandFocus`
- `SquadTray`
- `SquadCard1`
- `SquadCard2`
- `SquadCard3`
- `SquadCard4`
- `SquadCard5`
- `MinimapPanel`
- `RightQuickRail`
- `BuildCommand`
- `SCN08_BuildPlacementConfirmationBar`
- `DetailsPanel`
- `CancelButton`
- `ConfirmButton`
- `SCN09_BuildDrawerPopup`
- `BuildDrawerRoot`
- `BuildPanel`
- `LeftPanel`
- `RightPanel`
- `BuildingsTab`
- `BuildButton`
- `CloseButton`
- `SCN19_ArmoryContent`
- `InspectionPanel`
- `ArmoryTitleBlock`
- `MissionResultPopup`
- `MissionImage`
- `MissionImageScrim`
- `MissionIdentityBlock`
- `MissionNameText`
- `ConfirmRaidPopup`
- `EndOfDayReportPopup`
- `IntelRevealPopup`

## Pending Phase 0 Evidence

- Active reachable shell/surface baselines are captured at `4800x2160`, `1920x1080`, and `2400x1080`.
- Active modal baselines are captured at `4800x2160`.
- Secondary/reference popup baselines or usage decisions are captured at `4800x2160`.
- Commander Profile is documented as a Canvas prefab target that is not installed by the active Canvas route system.
- Shadow batchmode FPS smoke baselines are captured for Main Menu and Match HUD with Canvas active vs disabled.
- Unity render `ProfilerRecorder` counters returned `0.0` for draw calls, batches, SetPass, triangles, and vertices in batchmode, so draw-call proof remains a Game View/Frame Debugger task if needed.
- Captured performance logs did not emit Canvas rebuild warnings. Unity domain reload `RebuildCommonClasses` lines are editor startup noise, not Canvas rebuild warnings.

## Shadow Capture Evidence

2026-06-22:

`CanvasMenuFallbackValidation.Run` was extended in editor-only tooling to support environment-configured screenshot path and render resolution:

- `WARLINE_CANVAS_SCREENSHOT_PATH`
- `WARLINE_CANVAS_SCREENSHOT_WIDTH`
- `WARLINE_CANVAS_SCREENSHOT_HEIGHT`

Shadow project:

- `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

Captured Canvas main menu/deploy UI evidence:

| Resolution | Result | Luma | Screenshot | Log |
| --- | --- | --- | --- | --- |
| 1280x720 | Passed | 0.103 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_menu_fallback_1280x720.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_menu_fallback_1280x720.log` |
| 1920x1080 | Passed | 0.092 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_menu_fallback_1920x1080.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_menu_fallback_1920x1080.log` |
| 4800x2160 | Passed | 0.111 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_menu_fallback_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_menu_fallback_4800x2160.log` |

Scope limit:

- These captures validate the Canvas main menu/deploy UI visibility path only.
- They do not complete the all-route screenshot gate for loading, commander profile, armory, match HUD, build placement, build drawer, mission result, confirm raid, end-of-day report, or intel reveal.

Additional Phase 0 route, modal, and reference popup captures are listed above in the artifact inventory. Secondary/reference popup capture results:

| Surface | Result | Luma | Screenshot | Log |
| --- | --- | --- | --- | --- |
| Ability Upgrade Detail | Passed | 0.995 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_ability_upgrade_detail_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_ability_upgrade_detail_4800x2160.log` |
| Build Placement Panel | Passed | 0.679 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_build_placement_panel_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_build_placement_panel_4800x2160.log` |
| Pause Menu | Passed | 0.514 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_pause_menu_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_pause_menu_4800x2160.log` |
| Popup Frame | Passed | 0.049 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_popup_frame_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_popup_frame_4800x2160.log` |
| Reward Unlock | Passed | 0.928 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_reward_unlock_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_reward_unlock_4800x2160.log` |
| Threat Alert | Passed | 0.617 | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_threat_alert_4800x2160.png` | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_modal_threat_alert_4800x2160.log` |

Shadow batchmode Canvas performance smoke baselines:

| Surface | Canvas | Samples | Avg ms | FPS | P95 ms | Canvas render events | Render counters | Log |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Main Menu | Active | 240 | 0.434 | 2303.0 | 0.513 | 329 | Batchmode recorder returned `0.0` for draw/batch counters | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_perf_mainmenu_active.log` |
| Main Menu | Disabled | 240 | 0.678 | 1475.1 | 1.303 | 329 | Batchmode recorder returned `0.0` for draw/batch counters | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_perf_mainmenu_disabled.log` |
| Match HUD | Active | 240 | 0.611 | 1637.4 | 0.916 | 329 | Batchmode recorder returned `0.0` for draw/batch counters | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_perf_match_active.log` |
| Match HUD | Disabled | 240 | 0.890 | 1124.2 | 1.121 | 329 | Batchmode recorder returned `0.0` for draw/batch counters | `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/shadow_canvas_perf_match_disabled.log` |

Performance scope limit:

- These values are editor batchmode smoke numbers for future Canvas prefab styling regressions.
- They should not be compared to the user's interactive Game View FPS reports.
- Use the Unity Profiler or Frame Debugger in Game View/device profiling for authoritative draw calls, batches, rebuild cost, and real FPS.
