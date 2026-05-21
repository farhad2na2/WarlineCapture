# WarlineCapture UI/UX Implementation High-Level Spec

Date: 2026-05-01

## Source Material

- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
- `Design/Archive/LegacyUI_2026-05-21/WarlineCapture_UIUX_Codex_Package/warlinecapture_uiux_spec_assets/*` as archived layout/content reference only
- Current Unity project state under `Assets/Game`

## Current Project State

WarlineCapture currently has one enabled build scene:

- `Assets/Game/Scenes/Game.unity`

The current game already contains a strong tactical simulation foundation:

- RTS unit selection, movement, attack, transport boarding, air movement, rope disembark, base breach, health bars, minimap, road/build placement, production, resources, day/night, citizens, AI economy, AI production, AI squads, AI targeting, AI combat, radar/satellite threat warning, player auto mode, and Android CI build support.

The current UI is split between two systems:

- Legacy Unity Canvas hierarchy under `UI_Canvas / Panel_Main`, controlled mostly by `Assets/Game/Scripts/UI/MenuView.cs`.
- UI Toolkit runtime HUD panels under `Assets/Game/UI/*.uxml`, controlled by `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`.

The current screens are practical but not yet aligned with the target AAA mobile shell:

- Existing main menu is mostly a start/game button and camp/stats/settings surfaces.
- Existing tactical HUD has many required systems, but not the final layout or reusable prefab structure from the design spec.
- Existing settings mostly expose gameplay speed and AI tuning, not full audio, graphics, controls, language, and accessibility.
- Existing warning/log/debug panels are functional but not yet formal popup prefabs.
- Campaign, Operations, Mission Briefing, Loadout, Mission Result, Reward, End of Day, Intel, and Skirmish are not fully implemented as production mode screens.

## Target Product Structure

The design documents define three user-facing modes built on one shared simulation:

1. Campaign
   - Curated mission nodes, chapter progression, star goals, rewards, briefing, loadout, result flow.

2. Operations
   - District dashboard, operation days, city state meters, intel confidence, trust/security/infrastructure, district actions, hidden hostile network abstraction.

3. Skirmish
   - Fast skirmish setup using existing AI knobs, map seed/preset, resources, win condition, fog/intel toggles, and launch.

All modes should route through a shared app shell:

- `SafeAreaRoot`
- `HeaderBar`
- `ContentRoot`
- `FooterBar`
- `ModalOverlay`
- `TooltipLayer`

Tactical play should use a persistent match HUD:

- `TopHUD(ObjectivePanel, ThreatFeedPanel, ResourceBar)`
- `BottomHUD(SquadTray, CommandBar, MiniMapPanel, BuildToggle)`
- `ContextOverlay(CommandWheelCanvas)`
- `ModalOverlay`

The battlefield presentation behind the HUD should follow the full 3D single-map direction in `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`. UI visual-lock work should continue under `Design/VisualLock` and `Design/VisualLockLayered`, while gameplay-facing content should use 3D operation-map captures and the prefab catalog under `Assets/Game/Configs/Prefabs`.

## Parallel Work Boundary

UI work and 3D operation-map gameplay/art work can proceed in separate chats as long as ownership stays clear.

UI work owns:

- `Design/VisualLock`
- `Design/VisualLockLayered`
- `Design/WarlineCapture_UIUX_*`
- UI prefabs under `Assets/Game/Prefabs/UI`
- UI runtime code under `Assets/Game/Scripts/UI`
- UI sprite atlases and generated UI art under `Assets/Game/Art/UI`

3D operation-map gameplay/art work owns:

- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Assets/Game/Configs/Prefabs`
- 3D operation-map scenes, metadata, camera states, and performance validation
- generated or captured 3D gameplay-facing art used behind HUD targets

The main coordination point is `SCN-08 RTS Battle HUD`: the UI chat can keep building Canvas HUD components, while the 3D operation-map lane supplies the battlefield/camera/overlay capture that sits behind that HUD.

## Implementation Strategy

Do not replace the working game scene in one large change. Build a routed UI layer around the current tactical scene and migrate feature surfaces one at a time.

Do not complete all visual-lock work first and do not complete all functionality first with visuals postponed. The UI should advance as screen-by-screen vertical slices. A screen is not considered ready to hand off until it has a landscape target, a real decomposed Canvas prefab, basic route/runtime wiring, multi-aspect captures, focused tests, and a small optimization pass.

Mockups and generated visual targets are references, not runtime shortcuts. The production UI must be separate Canvas elements: 9-sliced panels, masked art crops, icons, TMP text, and real `Button`, `Toggle`, `Slider`, `Dropdown`, and `ScrollRect` controls. Replaceable elements such as portraits, resources, logos, icons, and text must not be baked into large background images.

Shared chrome rule: buttons, tabs, segmented controls, dropdowns, and launch actions must use thin clean control chrome, not the heavier section/panel border art. Future screen builders should reuse the accepted Settings/Skirmish control style, keep controls below section-title divider lines, keep dropdown rects separated from their labels, use `Oxanium-Bold SDF` for page titles and large CTA labels, and use `Oxanium-Light SDF` for normal screen/control text. Numeric steppers, difficulty groups, map stat cards, icon plates, and CTA buttons are separate control families and should not be collapsed into a generic segmented-control treatment.

The recommended path is:

1. Stabilize architecture and naming.
2. Build shared shell and reusable prefab/view components.
3. Replace the main menu with mode selection.
4. Add Skirmish because it maps directly to existing AI/runtime settings.
5. Formalize tactical HUD overlays and popups around existing systems.
6. Add objective/result/reward infrastructure.
7. Add Campaign screens and first mission data.
8. Add Operations screens and save model.
9. Polish accessibility, localization keys, Android safe area, and visual consistency.

## Screen Vertical Slice Gate

Each remaining screen should pass this gate before the next screen starts:

1. Choose or generate the mobile landscape visual target from the original design references.
2. Decompose the target into real Unity UI parts instead of one full-screen background.
3. Build or update the prefab using shared UI kit pieces where possible.
4. Wire route navigation and the minimum runtime data needed for that screen.
5. Confirm every visible UI element has a gameplay contract in `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`, including route/effect, gameplay data, enable rule, and locked/designed-unavailable/read-only state where needed.
6. Capture at `1920x1080` and at least one wide phone aspect such as `2400x1080`.
7. Compare against the target and fix visible mismatches before expanding scope.
8. Apply optimization: atlas labels, import settings, 9-slice reuse, disabled decorative raycasts, and removal of transparent placeholder graphics.
9. Add or update EditMode validation for hierarchy, sprites, import settings, raycasts, gameplay element contracts, and interaction wiring.

Current accepted pattern:

- Main Menu established the target-to-real-canvas conversion method.
- Splash/Loading established the shared outer screen frame and branded loading treatment.
- Settings/Accessibility reuses the shared frame and now has real controls, matched sliders/toggles/dropdowns, atlas-ready generated art, and optimization tests.

## Phase 0 - Baseline Protection

Goal: keep the current build and tests stable while UI work begins.

Tasks:

- Keep `Assets/Game/Scenes/Game.unity` as the only enabled build scene until routing is ready.
- Preserve current `MenuView` gameplay button flow so Android remains launchable.
- Add scene validation tests for new root names before wiring logic.
- Add smoke tests for route transitions without entering gameplay simulation.
- Keep current `BuildScript.BuildAndroid` passing before and after each phase.

Exit criteria:

- EditMode tests pass.
- PlayMode bootstrap tests pass.
- Android build reaches at least UnityLinker/ManagedStripped successfully.

## Phase 1 - UI App Shell and Routing

Goal: introduce the target screen routing model without breaking existing gameplay.

Create a new runtime UI foundation:

- `WarlineCaptureRoute`
- `WarlineCaptureRouter`
- `WarlineCaptureScreenController`
- `WarlineCaptureModalController`
- `WarlineCaptureSafeArea`
- `WarlineCaptureViewModel` base pattern or simple screen-specific models

Initial routes:

- `Splash`
- `MainMenu`
- `Settings`
- `SkirmishSetup` as the player-facing route, backed by `QuickCustomSetup` only where runtime compatibility still requires it
- `Match`

Keep routes in the existing `Game.unity` first. Separate scenes are reserved for proven load-time or memory requirements.

Exit criteria:

- App opens to Splash or Main Menu.
- Main Menu can route to existing gameplay.
- Back navigation is deterministic.
- Modal overlay can block input and close cleanly.

## Phase 2 - Shared Visual Components

Goal: convert the design spec into reusable Unity prefabs/components.

Create reusable prefabs:

- `ModeCardView`
- `StatTileView`
- `ResourceCounterView`
- `ObjectiveRowView`
- `RewardItemView`
- `ActionButtonView`
- `PopupFrameView`
- `SegmentedControlView`
- `ToggleRowView`
- `SliderRowView`

Create tactical prefabs:

- `ObjectiveTrackerPanel`
- `SquadTrayPanel`
- `BuildDrawerPanel`
- `CommandBarPanel`
- `ThreatFeedPanel`
- `MissionResultPopup`
- `ThreatAlertPopup`
- `PauseMenuPopup`

Use the accepted WarlineCapture UI kit and generated visual-lock HUD art from the current target inventory. Use 3D operation-map captures and config-backed roster presentation for gameplay, map, unit, minimap, and battlefield imagery. Use mockup images only as implementation references, not in-game final art unless explicitly approved.

Exit criteria:

- Prefabs exist under a clear UI folder.
- Prefabs use stable child names from the design spec.
- Test or editor validation can load required prefab references.

## Phase 3 - Main Menu / Mode Select

Goal: implement `SCN-02 Main Menu / Mode Select`.

Replace the current first screen with:

- Top profile/resource bar.
- Three primary mode cards: Campaign, Operations, Skirmish.
- Secondary navigation: profile, settings, inbox/events/ranking placeholders.
- Footer utility bar placeholder.

Initial behavior:

- Campaign and Operations may show designed-unavailable states until their screens are ready.
- Skirmish opens the real setup screen. Runtime routes may retain QuickCustom naming until migration.
- Existing direct Play button can remain as a debug shortcut during development, hidden in release builds.

Exit criteria:

- Android landscape layout fits 16:9 and common phone safe areas.
- `Assets/Game/Textures/Logo.png` is used as the game logo.
- Mode cards route correctly.

## Phase 4 - Skirmish Setup

Goal: implement `SCN-13 Skirmish Setup` using existing AI settings.

Map current runtime settings into player-facing controls:

- Enemy Type
- Enemy Count
- Difficulty
- Starting Credits (tactical Money)
- Income Multiplier
- Build Speed
- Unit Production Speed
- Attack Group Size
- Attack Frequency
- Aggression
- Expansion
- Target Priority
- Player Auto AI

Add initial match rules as simple stored fields:

- Map preset
- Win condition
- Fog of war placeholder
- Intel reveal placeholder
- Starting resources

Exit criteria:

- Launch Mission applies the selected settings to `AISettingsRuntimeState`.
- Existing `GameBootstrap.BeginGameplay()` starts with the selected config.
- Settings are visible and usable on Android landscape.

## Phase 5 - Tactical HUD Upgrade

Goal: align the existing in-game HUD with `SCN-08`, `SCN-09`, and `SCN-10`.

Preserve existing tactical systems:

- `RTSSelectionSystem`
- `BuildingPlacementSystem`
- `RoadBuildSystem`
- `ThreatDetectionWarningSystem`
- minimap logic
- production and resource logic

Restructure presentation:

- Top-left objective tracker.
- Top-right resources.
- Threat feed below objective or top center.
- Bottom-left squad tray.
- Bottom-center command bar.
- Bottom-right minimap/build toggle.
- Build drawer overlay.
- Contextual command wheel or command strip.

Exit criteria:

- Existing select/move/attack/build/minimap flows continue working.
- The HUD no longer depends on oversized debug controls as primary mobile controls.
- Runtime log panel remains available through FPS panel tap for Android debugging.

## Phase 6 - Objectives, Mission Results, Rewards

Goal: add the core data needed by Saga and Operation.

Create data/runtime systems:

- `GameModeDefinition`
- `ScenarioSetup`
- `MissionConfig`
- `ObjectiveConfig`
- `ObjectiveRuntimeState`
- `MissionResultData`
- `RewardConfig`
- `RewardGrantResult`

Initial objectives:

- Destroy enemies.
- Survive duration.
- Protect civilians.
- Capture or destroy building.
- Build required structure.
- Keep unit losses below threshold.

Exit criteria:

- A match can produce win/loss result data.
- `POP-05 Mission Result` can show victory/defeat, stats, stars, and rewards.
- Reward popup can show placeholder grants without persistence first.

## Phase 7 - Campaign

Goal: implement the first campaign route around existing tactical missions.

Screens:

- `SCN-05 Campaign Map`
- `SCN-06 Mission Briefing`
- `SCN-07 Loadout / Squad Prep`

Initial content:

- Chapter 1 with 3 to 5 mission nodes.
- One mission can reuse current default tactical setup.
- Mission briefing pulls real objectives and rewards.
- Loadout starts as a simplified roster/selected units view and can deepen later.

Exit criteria:

- Main Menu -> Campaign -> Briefing -> Loadout -> Match -> Result -> Campaign loop works.
- Stars and completion are stored locally.

## Phase 8 - Operations

Goal: implement the strategic operation layer after the tactical/result loop is stable.

Screens:

- `SCN-11 Operation Dashboard`
- `SCN-12 District Detail / Actions`
- `POP-02 Confirm Raid`
- `POP-06 End of Day Report`
- `POP-08 Intel Reveal`

Data:

- `OperationSaveState`
- `DistrictState`
- `OperationEvent`
- `IntelEvidenceItem`
- `OperationActionRequest`

Exit criteria:

- District meters display and update.
- Patrol/Scan/Aid/Raid actions work as simple simulations.
- Raid can generate a mission route.
- End day saves and reloads state.

## Phase 9 - Settings, Accessibility, Localization

Goal: complete `SCN-04 Settings & Accessibility`.

Add settings models:

- Audio
- Graphics quality
- Frame rate
- Controls
- Notifications
- Accessibility
- Language

Accessibility:

- Large Text
- High Contrast UI
- Colorblind mode
- Reduced motion

Localization:

- Move player-facing labels to keys through `GameStringsConfig` or a future localization service.
- Keep English source values next to keys for authoring clarity.

Exit criteria:

- Settings persist with PlayerPrefs or save model.
- UI scales cleanly with Large Text.
- Critical state is not color-only.

## Phase 10 - Polish and Release Readiness

Goal: prepare for a polished Android landscape build.

Tasks:

- Safe area validation on phones/tablets.
- Canvas Scaler standardization.
- Visual consistency pass using accepted WarlineCapture UI kit assets and 3D single-map compatibility checks for gameplay-facing screens.
- Loading screen polish.
- Input blocking audit for modals/drawers.
- Performance pass for UI allocations and runtime image generation.
- Build settings audit for Android landscape, icon/logo, target SDK, IL2CPP, stripping, and debug signing.

Exit criteria:

- All tests pass.
- Android APK/AAB builds in Jenkins.
- Core route loop is playable without editor-only controls.

## Priority Order

Recommended implementation order:

1. Main Menu / Mode Select shell.
2. Skirmish setup.
3. Tactical HUD layout pass.
4. Objective tracker and mission result popup.
5. Campaign Map, Briefing, Loadout.
6. Settings/accessibility completion.
7. Operations dashboard and district actions.
8. Reward/intel/end-of-day polish.

This order gives the player a coherent first screen and makes the existing tactical game configurable before adding large campaign systems.
