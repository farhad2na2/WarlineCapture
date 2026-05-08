# WarlineCapture UI/UX Implementation Detailed Spec

Date: 2026-05-01

## Purpose

This document turns the AAA mobile GDD and UI/UX screen spec into concrete implementation steps for the current Unity project.

It is intentionally staged. The current project has working tactical gameplay and CI build coverage, so each step should keep the game buildable and testable.

For interaction polish, use `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md` as the shared implementation checklist for UI motion, locked/invalid feedback, reward flyouts, popup/drawer transitions, tactical command markers, critical warning feedback, and paired audio cues from `Design/WarlineCapture_Audio_Design_Guidelines.md`.

For FTUE, contextual help, assistant recommendations, tutorial cards, highlights, and assistant takeover UI, use `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md` together with `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`.

## Current Implementation Inventory

### Active Visual Direction

- Production art direction: premium 2D isometric mobile RTS.
- Direction doc: `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
- Visual references: `Design/VisualReferences`
- Unity imported golden assets: `Assets/Game/Art/Generated/2DISO`
- Manual Unity spike scene: `Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity`

### Parallel Work Boundary

UI implementation may continue independently from the 2D isometric gameplay/art pipeline.

UI implementation should avoid editing:

- `Design/WarlineCapture_2D_Isometric_*`
- `Design/VisualReferences`
- `Assets/Game/Art/Generated/2DISO`
- `Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity`
- `Assets/Game/Scripts/Editor/WarlineCaptureIso2DSpikeBuilder.cs`

2D isometric gameplay/art implementation should avoid editing UI prefabs, `Design/VisualLock`, `Design/VisualLockLayered`, and `WarlineCapture_UIUX_*` docs unless the change is specifically about HUD/battlefield integration.

Shared integration should happen at the Match HUD layer: UI owns the Canvas HUD composition; 2D iso gameplay/art owns the battlefield render, sprite sorting, tactical overlays, and capture/report validation behind the HUD.

### Scenes and Build

- Enabled build scene: `Assets/Game/Scenes/Game.unity`
- Disabled test scene: `Assets/Game/Scenes/Test.unity`
- Android build entry: `Assets/Game/Scripts/Editor/BuildScript.cs`
- Current Android build output path:
  - APK: `Build/AndroidAPK/WarlineCapture.apk`
  - AAB: `Build/AndroidAAB/WarlineCapture.aab`

### Current UI Systems

Legacy Canvas:

- Scene root: `UI_Canvas / Panel_Main`
- Main controller: `Assets/Game/Scripts/UI/MenuView.cs`
- Menu panels:
  - `Panel_Menu`
  - `Panel_Game`
  - `Panel_Camp`
  - `Panel_Stats`
  - `Panel_Settings`
  - `Panel_Confirm`
  - `Panel_Warning`
  - `Panel_Log`
- Existing controls:
  - `Button_Game`
  - `Button_Stats`
  - `Button_Back`
  - `Button_Settings`
  - camp category buttons
  - gameplay speed and AI settings dropdowns
  - minimap/fullscreen map objects
  - runtime log panel toggled from `Panel_FPS`

UI Toolkit:

- Main document reference: `GameBootstrap.mainMenuDocument`
- Controller: `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`
- UXML/USS:
  - `Assets/Game/UI/MainMenu.uxml`
  - `Assets/Game/UI/MainMenu.uss`
  - `Assets/Game/UI/Panels/BuildModePanel.uxml`
  - `Assets/Game/UI/Panels/BuildToolMenuPanel.uxml`
  - `Assets/Game/UI/Panels/DayNightTimePanel.uxml`
  - `Assets/Game/UI/Panels/FullscreenMapPanel.uxml`
  - `Assets/Game/UI/Panels/MinimapPanel.uxml`
  - `Assets/Game/UI/Panels/ResourcesBarPanel.uxml`
  - `Assets/Game/UI/Panels/SelectionModePanel.uxml`
  - `Assets/Game/UI/Panels/UnitCommandInfoPanel.uxml`
  - `Assets/Game/UI/Panels/UnitCommandMenuPanel.uxml`
  - `Assets/Game/UI/Panels/UnitPortraitPanel.uxml`
  - `Assets/Game/UI/Panels/ZoomControlsPanel.uxml`

### Current Gameplay Systems Available for UI Binding

- `GameBootstrap`
- `InitialUnitsRuntimeState`
- `AISettingsRuntimeState`
- `GameRuntimeStats`
- `ThreatWarningRuntimeState`
- `GameStrings`
- `RTSSelectionSystem`
- `BuildingPlacementSystem`
- `RoadBuildSystem`
- `DayNightSystem`
- `ThreatDetectionWarningSystem`
- ECS systems for AI, production, combat, movement, transport, citizens, and health bars

## Architecture Decision

Use a hybrid approach during migration:

- Keep the existing Canvas UI alive for current gameplay until each tactical surface is replaced.
- Build the new routed app shell as Canvas prefabs first, because the current screen/popup specs use GameObject hierarchy names and TextMeshPro, and existing tests already inspect the scene YAML.
- Continue using existing UI Toolkit panels only where they are already working for runtime tactical controls, then either wrap or migrate them phase by phase.

Longer term, choose one primary UI technology:

- Canvas + TMP is recommended for the near-term because the current UI, imported HUD prefabs, runtime log panel, and scene validation tests are already Canvas-based.
- UI Toolkit can remain for specialized runtime HUD panels until replaced.

## Screen-by-Screen Delivery Rule

New UI work should be delivered as vertical screen slices. Do not build all screens visually first and do not wire all screens functionally with visual lock deferred. Each screen should reach a usable, validated state before the next one starts.

Before a visible UI element is implemented, it must be checked against `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`. An element is not ready for production prefab work unless it has a documented gameplay purpose, route/effect or read-only/decorative role, gameplay data source, state rule, and feedback state. Elements without runtime systems must be explicitly marked `DesignedUnavailable`, `Locked`, `DevOnly`, or `ReadOnly`.
Assistant-specific visible elements must also satisfy `WarlineCapture_FTUE_And_Command_Assistant_Design.md`, especially the rules for explicit player permission, visible takeover ownership, and player-input cancellation.

For every screen:

- Start from the original design references under `Design/WarlineCapture_UIUX_Codex_Package/warlinecapture_uiux_spec_assets`.
- Create or select a landscape target that preserves the original style and hierarchy.
- Build a real Canvas prefab from separate replaceable parts. Do not use the target as one runtime background with invisible buttons.
- Reuse shared UI kit pieces when the target uses the same style: outer screen frame, tabs, animated buttons, sliders, toggles, dropdowns, masked art crops, and Oxanium TMP text.
- Keep every interactive element as a real Unity control with normal, highlighted, pressed, selected, and disabled states where applicable.
- Validate that every visible UI element appears in the gameplay element alignment contract.
- Apply the shared feedback primitives from `WarlineCapture_Visual_Feedback_VFX_Recommendations.md` for accepted, selected, locked, disabled, invalid, reward, popup, drawer, and tactical HUD states instead of inventing per-screen one-offs.
- Capture at common Android landscape aspects and compare visually before expanding behavior.
- Run focused EditMode tests for hierarchy, route wiring, sprite references, import settings, decorative raycasts, and accidental transparent placeholder graphics.
- Optimize the accepted screen before moving on: atlas labels, SpriteAtlas membership, UI sprite import settings, and disabled raycasts for decorative `Graphic` components.

## Folder Layout

Create these folders as implementation begins:

```text
Assets/Game/Scripts/UI/Shell
Assets/Game/Scripts/UI/Screens
Assets/Game/Scripts/UI/Popups
Assets/Game/Scripts/UI/Components
Assets/Game/Scripts/Modes
Assets/Game/Scripts/Objectives
Assets/Game/Scripts/Progression
Assets/Game/Scripts/Persistence
Assets/Game/Prefabs/UI/Shell
Assets/Game/Prefabs/UI/Screens
Assets/Game/Prefabs/UI/Popups
Assets/Game/Prefabs/UI/Components
Assets/Game/Configs/Modes
Assets/Game/Configs/Missions
Assets/Game/Configs/Rewards
```

Do not move existing scripts in the first phase. Add wrappers and new code around them to avoid breaking serialized scene references.

## Phase 1 Detailed Tasks - App Shell and Router

### New Types

Create:

- `WarlineCaptureRoute.cs`
- `WarlineCaptureRouter.cs`
- `WarlineCaptureScreenController.cs`
- `WarlineCaptureModalController.cs`
- `WarlineCaptureSafeArea.cs`
- `ScreenRouteButton.cs`

Suggested route enum:

```csharp
public enum WarlineCaptureRoute
{
    Splash,
    MainMenu,
    CommanderProfile,
    Settings,
    SagaMap,
    MissionBriefing,
    Loadout,
    Match,
    OperationDashboard,
    DistrictDetail,
    QuickCustomSetup
}
```

Router responsibilities:

- Own active route.
- Show one screen root at a time.
- Maintain optional back stack.
- Provide `GoTo(route)`, `Back()`, `ShowModal(id, payload)`, `CloseModal()`.
- Block gameplay input when modal overlay is active.

### Scene Hierarchy

Add under `UI_Canvas / Panel_Main` or a new root next to it:

```text
AppShell
  SafeAreaRoot
    HeaderBar
    ContentRoot
      Screen_Splash
      Screen_MainMenu
      Screen_Settings
      Screen_QuickCustomSetup
      Screen_MatchOverlay
    FooterBar
    ModalOverlay
    TooltipLayer
```

During migration, `AppShell` can coexist with existing `Panel_Menu` and `Panel_Game`.

### Tests

Add EditMode scene validation:

- `AppShellExistsInGameScene`
- `AppShellHasSafeAreaHeaderContentFooterModalTooltip`
- `RouterReferencesMainScreens`

Exit criteria:

- Game opens without null reference errors.
- Existing `Button_Game` still starts the current match.
- New router can show Main Menu and Settings in editor play mode.

## Phase 2 Detailed Tasks - Shared UI Components

### Component Prefabs

Create prefabs with stable child names:

```text
ModeCardView
  Background
  ArtImage
  TitleText
  SubtitleText
  ProgressText
  LockRoot
  NotificationBadge
  Button

StatTileView
  Icon
  LabelText
  ValueText
  DeltaText

ResourceCounterView
  Icon
  ValueText
  PlusButton

ObjectiveRowView
  Icon
  LabelText
  ProgressText
  ProgressBar
  CompleteIcon

RewardItemView
  Icon
  QuantityText
  RarityFrame

ActionButtonView
  Icon
  LabelText
  CostText
  LockRoot
  Button

PopupFrameView
  Scrim
  Frame
  Header
  TitleText
  CloseButton
  BodyRoot
  ButtonRow
```

### Component Scripts

Create:

- `ModeCardView.cs`
- `StatTileView.cs`
- `ResourceCounterView.cs`
- `ObjectiveRowView.cs`
- `RewardItemView.cs`
- `ActionButtonView.cs`
- `PopupFrameView.cs`

Each view should expose a `Bind(...)` method and avoid hard-coded mockup values except editor placeholder text.

### Visual Rules

- Use `Assets/Game/Textures/Logo.png` for brand/logo.
- Use accepted WarlineCapture UI kit buttons, frames, icons, controls, and rank badges from visual-locked screens. Imported Synty HUD assets may remain as legacy/reference assets where already used, but new gameplay, map, unit, and battlefield imagery must follow the 2D isometric art bible.
- Use mockup JPGs only as references.
- Canvas Scaler should use `Scale With Screen Size`, reference `1920x1080`, landscape.
- Respect safe area padding.
- Minimum mobile touch size: 80 px at 1920x1080 reference, larger for primary CTAs.

### Tests

Add prefab validation:

- Required child names exist.
- Buttons have target graphics.
- Text fields are TMP where expected.
- `Bind` can run with placeholder data without exception.

## Phase 3 Detailed Tasks - Main Menu / Mode Select

### Current Gap

Current `Panel_Menu` is not the target mode select screen. It mainly exposes direct game start, stats, camp, and settings.

### Target

Implement `SCN-02 Main Menu / Mode Select`.

Hierarchy:

```text
Screen_MainMenu
  TopProfileBar
    LogoImage
    CommanderAvatar
    CommanderNameText
    LevelText
    ResourceCounterList
    SettingsButton
  LeftNav
    ProfileButton
    InboxButton
    StoreButton
    EventsButton
    RankingButton
  ModeCardList
    ModeCard_Saga
    ModeCard_Operation
    ModeCard_QuickCustom
  BottomUtilityBar
    ChatButton
    SocialButton
    CommanderButton
```

### Behavior

- Saga card routes to `SagaMap`.
- Operation card routes to `OperationDashboard`.
- Quick Custom card routes to `QuickCustomSetup`.
- Settings button routes to `Settings`.
- Profile routes to `CommanderProfile`.
- Inbox routes to `SCN-15 Inbox` designed-unavailable shell.
- Store routes to `SCN-14 Command Exchange` with purchases disabled until wallet/catalog/receipt/reward services are implemented.
- Events routes to `SCN-16 Events` designed-unavailable shell.
- Ranking routes to `SCN-17 Ranking` designed-unavailable shell.
- Chat/Social routes to `SCN-18 Command Feed` designed-unavailable shell.
- Do not use silent placeholder toasts for these side-nav actions; every visible nav button must open a designed shell, disabled/empty state, or documented unavailable state.

### Data

Create simple placeholder services:

- `PlayerProfileState`
- `ResourceWalletState`
- `ModeUnlockState`

Initial values can be constants until persistence is added.

### Migration

- Keep `Button_Game` available as debug fallback during development.
- Once Quick Custom is implemented, replace direct `Button_Game` start with `Quick Custom -> Launch Mission`.

### Tests

- Scene has three mode cards.
- Quick Custom card routes to setup.
- Settings button opens settings.
- Inbox, Store, Events, Ranking, and Chat/Social buttons route to their designed shells instead of silent placeholder toasts.
- Side-nav shell routes preserve stable route ids: `SCN-14`, `SCN-15`, `SCN-16`, `SCN-17`, and `SCN-18`.
- Operation Armory routes to `SCN-19` and every ability/upgrade detail link opens `POP-09`.
- Logo image references `Assets/Game/Textures/Logo.png`.

### Side-Nav Shell Backlog

The updated gameplay alignment adds route surfaces that must be represented in the UI plan even before their gameplay services are complete.

| Surface | Trigger | First implementation state |
|---|---|---|
| `SCN-14 Command Exchange` | Store / Black Market | Designed catalog shell with purchases disabled. |
| `SCN-15 Inbox` | Inbox | Empty/message-category shell with reward-claim and Operation-report sections. |
| `SCN-16 Events` | Events | Empty event-calendar shell with next-event rule. |
| `SCN-17 Ranking` | Ranking | Local/account category shell without network leaderboard dependency. |
| `SCN-18 Command Feed` | Chat/Social | Local system-feed shell. |
| `SCN-19 Armory` | Operation Dashboard Armory, Profile Upgrades, Loadout/Store detail links | Final high-end layered target exists; owned roster/upgrades shell with disabled upgrade CTAs until services exist. |

Each shell still follows the normal target-to-canvas workflow: generate/verify a landscape target, create a VisualLockLayered pack, map target elements to Canvas objects, add route/source-mapping tests, capture 16:9 and 20:9, and compare against target before acceptance.

`SCN-19 Armory` is no longer only a reserved route. Use `Design/VisualLock/SCN-19_Armory/SCN-19_Armory_Landscape_Target.png` and `Design/VisualLockLayered/SCN-19_Armory/layer_manifest.json` as the implementation gate before prefab work.

## Phase 4 Detailed Tasks - Settings and Accessibility

### Current Gap

Current settings include gameplay speed and AI configuration. Target settings include audio, graphics, controls, notifications, accessibility, and language.

### Target Hierarchy

```text
Screen_Settings
  HeaderBar
    BackButton
    TitleText
  TabStrip
    Tab_General
    Tab_Controls
    Tab_Notifications
    Tab_Accessibility
  SettingsScrollView
    AudioSection
    GraphicsSection
    ControlsSection
    AccessibilitySection
    LanguageSection
  FooterButtons
    ResetButton
    ApplyButton
```

### New Types

- `SettingsService`
- `AudioSettingsModel`
- `GraphicsSettingsModel`
- `ControlsSettingsModel`
- `NotificationSettingsModel`
- `AccessibilitySettingsModel`
- `LocalizationSettingsModel`

### Initial Controls

- Master volume slider.
- Music volume slider.
- SFX volume slider.
- Graphics quality segmented control.
- Frame rate segmented control: 30, 60, Auto.
- Camera sensitivity slider.
- High Contrast UI toggle.
- Large Text toggle.
- Colorblind mode dropdown.
- Language dropdown.

### Preserve Current Controls

Move current gameplay speed and AI controls into:

- `Screen_QuickCustomSetup`, for match setup.
- Optional hidden debug tab in Settings, editor/development only.

### Tests

- Settings persists values to `PlayerPrefs`.
- Large text changes relevant scale variable.
- High contrast changes theme variable or root class.
- Existing AI dropdown tests are updated only after controls move.

### Current Vertical Slice Status

- `Screen_Settings.prefab` is implemented as a parallel Codex Canvas screen.
- The screen reuses the shared Splash/Settings outer frame instead of a one-off border.
- Sliders, toggles, dropdowns, segmented controls, tabs, and footer buttons are real controls with generated art instead of baked mockup UI.
- Settings generated art is atlas-ready and validated by EditMode tests.
- Decorative graphics should not receive raycasts; interactive `Selectable` graphics keep their raycasts.
- Transparent placeholder `Image` components should not be reintroduced.
- `WarlineCaptureUiAccessibilityApplier` is the reusable accessibility bridge for Phase 4. The shell uses it for large-text scaling of routed content, and Settings uses it to apply high-contrast background state and standalone large-text scaling.

### Remaining Phase 4 Work

- Apply the stored settings beyond first-pass local persistence where appropriate.
- Add high-contrast target surfaces to each screen as those screens are visually accepted.
- Add pause-menu entry to the same Settings screen.
- Move gameplay speed and AI tuning controls out of legacy Settings and into `Screen_QuickCustomSetup`.
- Add localization string table integration once localization data exists.

## Phase 5 Detailed Tasks - Quick Custom Game Setup

### Current Gap

Existing AI settings are buried in `Panel_Settings`. The target has a dedicated custom setup screen.

### Target Hierarchy

```text
Screen_QuickCustomSetup
  HeaderBar
    BackButton
    TitleText
  PresetDropdown
  ConfigForm
    EnemyTypeDropdown
    EnemyCountStepper
    DifficultySegmented
    StartingMoneySegmented
    IncomeMultiplierSliderOrDropdown
    BuildSpeedSegmented
    UnitProductionSpeedSegmented
    AttackGroupSizeSegmented
    AttackFrequencySegmented
    AggressionSegmented
    ExpansionSegmented
    TargetPriorityDropdown
    PlayerAutoToggle
  RulesPanel
    WinConditionDropdown
    FogOfWarToggle
    IntelRevealToggle
    StartingResourcesDropdown
  MapPreviewPanel
    PreviewImage
    MapNameText
    SeedInput
  LaunchButton
```

### Data Model

Create:

- `QuickGameConfig`
- `QuickGamePreset`
- `WinConditionConfig`
- `MapDefinition`

Initial `QuickGameConfig` maps directly to `AISettingsRuntimeState`.

### Launch Flow

1. Validate config.
2. Apply AI values to `AISettingsRuntimeState`.
3. Save current `QuickGameConfig` in a runtime launch payload.
4. Route to `Match`.
5. Call `GameBootstrap.BeginGameplay()`.

### Tests

- Setting values updates `QuickGameConfig`.
- Launch applies `AISettingsRuntimeState`.
- Enemy count maps to existing 1 to 3 enemy support.
- Back returns to Main Menu without starting gameplay.

## Phase 6 Detailed Tasks - Tactical HUD

### Current Gap

The current tactical HUD is functional but split across Canvas and UI Toolkit. It does not match the final information architecture.

The current tactical-map production gap is tracked in `WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`. The first concrete implementation target is `WarlineCapture_M01_FirstContact_Production_Contract.md`. Phase 6 must add the missing selected-entity panel, command mode banner, world command marker layer, invalid command feedback, and minimap camera bridge before the Chapter 1 M01 playable slice depends on the new close-up AI tactical map.

### Target Hierarchy

```text
MatchHUDCanvas
  TopHUD
    ObjectivePanel
    ThreatFeedPanel
    ResourceBar
  BottomHUD
    SquadTray
    CommandBar
    MiniMapPanel
    BuildToggle
  ContextOverlay
    CommandWheelCanvas
    BuildDrawerCanvas
  ModalOverlay
```

### Existing Systems to Bind

- Resources: `BuildingPlacementSystem` faction resources and citizen stats.
- Selection: `RTSSelectionSystem`.
- Build/production: `BuildingPlacementSystem`.
- Road tools: `RoadBuildSystem`.
- Minimap: existing `MenuView` and `MainMenuPlayUI` minimap logic.
- Threats: `ThreatWarningRuntimeState`.
- Runtime log: existing `MenuView` log panel.

### Objective Tracker

Before full objective runtime exists, bind a placeholder:

- Primary: Defeat hostile forces.
- Secondary: Protect civilian population.
- Bonus: Keep unit losses low.

Later replace with `ObjectiveRuntimeState`.

### Squad Tray

Initial implementation can use focused/selected unit data from `RTSSelectionSystem`.

Show:

- Unit/squad name.
- Count.
- Health.
- Status.
- Transport occupancy for vehicles/helicopters.

### Command Bar / Wheel

Start with command bar because it maps to existing controls:

- Select.
- Select all.
- Move.
- Attack.
- Hold/Stop.
- Build.
- Exit/rope drop when transport is focused.

Add radial command wheel later for long-press interactions.

The close-up tactical-map control contract must support both direct and explicit commands:

- tap friendly unit -> select and show selected entity panel
- selected unit + tap walkable ground -> move
- selected unit + tap enemy unit/building -> attack
- `MOVE` command -> explicit move target mode
- `ATTACK` command -> explicit attack target mode
- invalid target -> visible reason through HUD feedback
- minimap, objective row, or threat alert jump -> camera focus inside tactical map bounds

### Build Drawer

Wrap existing build/camp request functionality into `BuildDrawerPanel`.

Initial tabs:

- Infantry
- Vehicles
- Air
- Defense
- Economy

Use existing building/unit catalogs from configs.

### Tests

- Existing movement/build/selection PlayMode tests still pass.
- HUD input blocks world clicks when touching buttons.
- Runtime log panel stays inactive by default and toggles from FPS panel.
- Focused tests cover `BattleHud.SelectedEntityPanel`, `BattleHud.CommandModeBanner`, `BattleHud.WorldCommandMarkerLayer`, `BattleHud.InvalidCommandToast`, and `BattleHud.MinimapCameraBridge`.
- M01-focused tests verify selected rifle squad panel binding, Move/Attack command mode feedback, invalid command reason display, disabled Build reason, minimap/objective camera jump, and `POP-05` result binding for `saga.ch01.m01.first_contact`.

## Phase 7 Detailed Tasks - Popups

### Popup Prefabs

Create:

- `ThreatAlertPopup`
- `ConfirmRaidPopup`
- `BuildPlacementPanel`
- `RewardUnlockPopup`
- `MissionResultPopup`
- `EndOfDayReportPopup`
- `PauseMenuPopup`
- `IntelRevealPopup`
- `AbilityUpgradeDetailPopup`

### Modal Controller

`WarlineCaptureModalController` should:

- Show one blocking modal at a time by default.
- Support non-blocking toast/threat feed for alerts.
- Own scrim click behavior.
- Pause gameplay only for pause/options/result popups.
- Provide close reason callbacks.

### First Popup to Implement

Implement in this order:

1. `PauseMenuPopup`
2. `ThreatAlertPopup`
3. `BuildPlacementPanel`
4. `MissionResultPopup`
5. `RewardUnlockPopup`
6. `AbilityUpgradeDetailPopup`
7. Operation-specific popups later

### Tests

- Popup opens under `ModalOverlay`.
- Close button closes.
- Blocking popup prevents tactical world click.
- Pause popup pauses/resumes simulation.
- `AbilityUpgradeDetailPopup` binds target id, unlock moment, availability, disabled reason, and visual art from ability/upgrade configs.

## Phase 8 Detailed Tasks - Objectives and Results

### New Types

Create:

- `GameModeDefinition`
- `ScenarioSetup`
- `MissionConfig`
- `ObjectiveConfig`
- `ObjectiveType`
- `ObjectiveRuntimeState`
- `ObjectiveManager`
- `StarGoalConfig`
- `MissionResultData`
- `MissionResultBuilder`

### Objective Types

Initial enum:

```csharp
public enum ObjectiveType
{
    DestroyAllEnemies,
    SurviveDuration,
    ProtectCivilianCount,
    BuildStructure,
    CaptureOrDestroyBuilding,
    KeepUnitLossesBelow,
    ReachResourceAmount
}
```

### Runtime Binding

Use current systems where possible:

- Enemy destruction from ECS unit death/combat state.
- Civilian count from `CitizenPopulationSystem`.
- Buildings built from `GameRuntimeStats`.
- Resources from `BuildingPlacementSystem`.
- Unit losses from `GameRuntimeStats`.

### Result Flow

On match end:

1. Stop/pause gameplay simulation.
2. Build `MissionResultData`.
3. Show `MissionResultPopup`.
4. Apply rewards.
5. Route back to source mode screen.

### Tests

- Objective manager evaluates simple win/loss.
- Mission result shows victory and defeat variants.
- Star goals produce expected count.

## Phase 9 Detailed Tasks - Saga Campaign

### New Data

Create:

- `SagaProgress`
- `ChapterConfig`
- `SagaMissionNodeConfig`
- `MissionRewardConfig`

### Screens

`SCN-05 Saga Map`:

```text
Screen_SagaMap
  HeaderBar
  ChapterSelector
  MapViewport
  MissionNodeContainer
  NodeInfoPanel
  ChapterRewardPanel
  FooterBar
```

`SCN-06 Mission Briefing`:

```text
Screen_MissionBriefing
  HeaderBar
  MissionImagePanel
  BriefingText
  ObjectivePanel
  StarGoalsPanel
  EnemyIntelPanel
  RewardPanel
  StartMissionButton
```

`SCN-07 Loadout`:

```text
Screen_Loadout
  HeaderBar
  UnitRosterGrid
  SelectedUnitsPanel
  SupportSlotsPanel
  GearPanel
  MissionSummaryPanel
  DeployButton
```

### Initial Content

Chapter 1 content is owned by `SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`. UI implementation must bind nodes from that chapter document instead of hard-coded demo labels.

| Mission | Player-Facing Title | Required UI Surfaces |
|---|---|---|
| M01 | First Contact | SCN-05, SCN-06, SCN-08, POP-05, POP-07, PREFAB-01, PREFAB-02 |
| M02 | Establish The Base | SCN-05, SCN-06, SCN-08, SCN-09, POP-03, POP-05, POP-07, PREFAB-01, PREFAB-02, PREFAB-03 |
| M03 | Radar Warning | SCN-05, SCN-06, SCN-08, SCN-09, POP-01, POP-05, POP-07, PREFAB-01, PREFAB-02, PREFAB-03 |
| M04 | Airlift | SCN-05, SCN-06, SCN-07, SCN-08, SCN-10, POP-01, POP-05, POP-07, PREFAB-01, PREFAB-02 |
| M05 | Breach Assault | SCN-05, SCN-06, SCN-07, SCN-08, SCN-10, POP-01, POP-04, POP-05, POP-07, PREFAB-01, PREFAB-02 |

All five Chapter 1 missions have complete design specs. Implementation can still be sequenced as a vertical slice, but every visible Saga node must bind to a completed mission design, locked-state reason, objective set, reward preview, level/map preview, and validation state.

Current implementation note:

- `ChapterOneMissionCatalog` is the runtime code source for the five Chapter 1 route-ready mission configs.
- `ObjectiveManager` evaluates mission objectives from `GameRuntimeStats.Snapshot`.
- `GameRuntimeStats.Snapshot` now exposes all initial Phase 8 objective inputs: enemy kills, elapsed mission seconds, protected civilians, buildings built, captured/destroyed buildings, own losses, and resources earned.
- `MissionResultBuilder` creates `MissionResultData` for `POP-05 Mission Result`.
- `WarlineCaptureMissionSession` tracks the active mission and return route. `StartMissionButton` and `DeployButton` seed this state, and Deploy launches the current legacy gameplay path.
- `SagaMapScreenController` binds `Screen_SagaMap/NodeInfoPanel` from Chapter 1 mission node metadata, mission config objectives, and local completion/star progress. It also refreshes node locked/available/selected visuals from Saga progress, keeps locked nodes selectable for info only, and starts unlocked mission nodes into the briefing flow.
- `SagaProgressStore` persists local mission completion and best stars.
- `SaveService` provides split JSON files for profile, Saga, Operation, Settings, and Quick Custom data under the planned save scope.
- `MissionResultPopupController` binds runtime result data and granted reward rows into the generated `MissionResultPopup` prefab, and `WarlineCaptureMatchResultFlow` completes active missions from gameplay victory snapshots, shows `POP-05`, saves Saga best stars, and returns to the mission's configured route.
- `MatchObjectivePanelController` binds `Screen_MatchOverlay/ObjectivePanel` to the active mission session at runtime. It preserves the target-mockup fallback labels when no mission is active, then swaps in live primary objective progress and the first star-goal progress once a session exists.
- `RewardService` applies the first reward-service slice for completed missions: Commander XP, Credits, Unit/Building/Support/Cosmetic unlocks, BlueprintParts duplicate fallback, profile result counters, Saga save progress, saved operation supplies, and targeted Operation district trust/security/intel/infrastructure rewards. Chapter 1 mission configs now include reward configs, `Screen_MissionBriefing` previews those configs, and `WarlineCaptureMatchResultFlow` grants them through `SaveService` when an active mission completes. Mission Briefing, generated fallback reward text, and Mission Result reward rows now format Operation Supply and district-targeted Operation trust/security/intel/infrastructure rewards. Every Chapter 1 mission now carries an authored Operation outcome reward tied to North Bridge, Old Market, or Port Breach, including operation supply plus district metric gains. Operation-launched sessions prioritize Operation reward rows in briefing/result surfaces; Saga-launched sessions keep default XP/credits/unlock ordering. `ProgressionService` now provides the first commander XP table, level calculation, and account-stat accumulation from mission results. `RewardTrackService` adds commander-level milestone nodes, persisted claimed-node ids, eligibility checks, and first claim grants. `MissionHistoryService` archives recent mission result summaries into saved profile data for profile history surfaces.
- `OperationService` provides default operation districts, Resources-backed configurable action simulation for Patrol/Scan/Aid/Raid/Repair/Evacuate/Build Outpost, district-specific action modifiers, raid mission-routing intent, operation supply deltas, secondary trust/security/infrastructure/enemy-influence/heat/civilian-risk district consequences, typed pending event rows, saved intel evidence rows, authored threshold alert rules, and end-of-day pressure. `WarlineCaptureOperationRuntime` now loads the authored `OperationActionConfigSet`, then loads and saves this operation state through `SaveService`. `OperationDashboardScreenController`, `DistrictDetailScreenController`, and `WarlineCaptureOperationModalFlow` bind `SCN-11` / `SCN-12` plus first-slice Operation popups to this live state: dashboard cards select districts, End Day applies pressure and opens `POP-06`, Scan mutates intel and opens `POP-08`, Raid opens `POP-02`, and confirmation seeds Breach Assault into the briefing path. `Screen_DistrictDetail` now exposes the six-action Operation ActionGrid for Patrol, Drone Scan, Raid, Repair, Evacuate, and Build Outpost. Dashboard/detail cards now use a shared secondary-metric text contract so trust, security, infrastructure, enemy influence, heat, civilian risk, stability, and intel appear consistently before final visual-lock art treatment. Raid confirmation displays heat/civilian-risk/security/trust values directly, and End Day reports trust/security/heat/civilian-risk averages. `OperationIntelArchive` centralizes latest/count/read queries for saved evidence rows; `POP-08` displays the latest selected-district evidence and marks it read when View Intel is pressed. `OperationInboxScreenController`, `OperationEventsScreenController`, and `OperationCommandFeedScreenController` bind `SCN-15` / `SCN-16` / `SCN-18` to the saved Operation event ledger and intel archive while preserving their visual-lock fallback text; those feeds now display event category/severity/source-metric metadata and evidence confidence/read metadata from the typed ledger/archive. Remaining Operation work is final visual-lock UI presentation for every secondary metric and production content expansion.
- `CommanderProfileScreenController` now gives `SCN-03 Commander Profile` its first runtime profile binding pass: wallet counters, commander name, derived level/XP progress, unlock collection count, win/loss history, account combat totals, saved recent mission report rows, reward-track eligibility, claimable reward-track row buttons with modal detail/claim feedback, local Overview/Upgrades/History/Cosmetics/Stats/Settings tab content, and a first-claim CTA bind from saved `PlayerProfileSaveData` while preserving the layered visual-lock shell.

### Tests

- Locked nodes cannot start.
- Unlocked node opens briefing.
- Briefing Start opens loadout.
- Deploy starts match with mission payload.
- Result updates `SagaProgress`.

## Phase 10 Detailed Tasks - Persistent Operation

### New Data

Create:

- `OperationSaveState`
- `DistrictState`
- `DistrictMetricSet`
- `OperationDaySummary`
- `OperationEvent`
- `KnownThreatEstimate`
- `IntelEvidenceItem`
- `OperationActionRequest`
- `OperationActionResult`

### Screens

`SCN-11 Operation Dashboard`:

```text
Screen_OperationDashboard
  HeaderBar
  RegionMapViewport
  MetricSidebar
  DailyBriefingPanel
  WarningList
  BottomActionBar
```

`SCN-12 District Detail`:

```text
Screen_DistrictDetail
  HeaderBar
  DistrictImagePanel
  StatListPanel
  IntelPanel
  KnownThreatPanel
  RecentActivityPanel
  ActionGrid
```

### Actions

Initial action behavior:

- Patrol: reduce threat slightly, may reveal activity.
- Drone Scan: increase intel confidence.
- Aid: increase trust/stability, spend resources.
- Raid: requires confirmation, may launch mission.
- Repair: increase infrastructure, spend resources.
- Evacuate: reduce civilian risk, lower trust if overused.
- Build Outpost: improve security/readiness, spend resources.

### Persistence

Start with JSON save under `Application.persistentDataPath`.

Later add cloud save if required.

### Tests

- New operation creates valid district states.
- Actions mutate expected metrics.
- Save/load roundtrip preserves operation day and district metrics.
- Raid confirmation routes to mission briefing or match.

## Phase 11 Detailed Tasks - Profile and Progression

### New Data

Create:

- `PlayerProfileState`
- `CommanderXP`
- `AccountStats`
- `UnitUnlockCollection`
- `RewardTrackProgress`

### Screen

`SCN-03 Commander Profile`:

```text
Screen_CommanderProfile
  HeaderBar
  PortraitPanel
  ProfileSummaryPanel
  TabBar
  StatsPanel
  RewardTrackPanel
```

### Behavior

- Back returns to caller.
- Tabs switch local content only.
- Reward nodes open reward detail popup.

### Tests

- Profile route opens from Main Menu.
- Stats bind from placeholder state.
- Back returns to Main Menu.

## Phase 12 Detailed Tasks - Splash / Loading

### Screen

`SCN-01 Splash / Loading`:

```text
Screen_Splash
  BackgroundImage
  LogoRoot
    LogoImage
    TitleText
  LoadingBar
  StatusText
  TipText
```

### Behavior

- Shows on app launch.
- Loads required assets/configs.
- Routes to Main Menu when ready.
- May use a minimum visible duration for polish.

### Data

- Loading tips table through `GameStringsConfig`.
- Optional async load progress.

### Tests

- Splash uses logo.
- Splash routes to Main Menu when loading completes.

## Android Build and Quality Requirements

Keep Android settings aligned with mobile landscape:

- Landscape orientation.
- IL2CPP.
- ARM64.
- Debug signing until release signing is ready.
- Min SDK should remain compatible with target devices.
- Target SDK should be set deliberately before store submission.
- Use `Assets/Game/Textures/Logo.png` for app icon/logo surfaces where appropriate.

Before release:

- Audit `BuildScript.BuildAndroid` for target SDK, versioning, symbols, app bundle, and store signing.
- Ensure tests do not compile into player assemblies.
- Keep CI gates in order: EditMode, PlayMode, Android build.

## Validation Plan

Run these gates after each phase:

1. Targeted EditMode tests for changed UI or data.
2. Full EditMode suite.
3. Targeted PlayMode smoke test.
4. Android build when route/gameplay flow changes.

Add tests as features land:

- Scene hierarchy validation tests.
- Prefab child-name validation tests.
- Route transition tests.
- Settings persistence tests.
- Quick game config mapping tests.
- Objective evaluation tests.
- Save/load persistence tests.

## Implementation Checklist

### First Practical Milestone

- Add `AppShell` hierarchy.
- Add router and route enum.
- Add Main Menu mode select screen.
- Use logo image.
- Add Quick Custom screen with current AI settings.
- Launch current gameplay from Quick Custom.

### Second Practical Milestone

- Add Objective Tracker placeholder.
- Add Threat Feed panel around existing threat state.
- Re-layout resources, minimap, build, and command controls.
- Add Pause popup.
- Keep runtime log panel accessible from FPS panel.

### Third Practical Milestone

- Add Objective Manager.
- Add Mission Result popup.
- Add Reward popup.
- Add simple mission config and result route.

### Fourth Practical Milestone

- Add Saga Map, Briefing, and Loadout.
- Add Chapter 1 mission configs.
- Save stars/progress locally.

### Fifth Practical Milestone

- Add Operation Dashboard and District Detail.
- Add operation save model.
- Add district actions and end-of-day report.

## Known Risks

- `MenuView.cs` is large and owns many unrelated UI responsibilities. Avoid expanding it further; new screens should use smaller controllers.
- Current UI is split between Canvas and UI Toolkit. New architecture must not duplicate input handling indefinitely.
- Replacing tactical HUD all at once risks breaking selection, build placement, minimap, and world-click blocking.
- Campaign and operation modes require data/persistence systems that do not exist yet.
- Mockups include placeholder text and art; final implementation must use in-project assets and localized string keys.

## Recommended Next Implementation Step

Start with the first practical milestone:

1. Create router/app shell scripts.
2. Add `AppShell` under the current `UI_Canvas`.
3. Build `Screen_MainMenu` with logo and three mode cards.
4. Build `Screen_QuickCustomSetup` by moving current AI/gameplay speed settings into a dedicated setup screen.
5. Route Launch Mission into the existing `GameBootstrap.BeginGameplay()` path.

This gives an immediate visible upgrade, uses systems already present, and avoids blocking on Saga or Operation persistence.
