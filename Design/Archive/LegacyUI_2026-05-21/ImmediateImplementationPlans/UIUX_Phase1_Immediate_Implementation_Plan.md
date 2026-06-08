# WarlineCapture UI/UX Phase 1 Immediate Implementation Plan

Date: 2026-05-02

Status: Phase 1 implementation completed on 2026-05-02. The parallel UI shell is installed disabled-by-default, the first real main menu screen exists, component prefabs are in place, focused EditMode validation passes, and an Android APK build completed successfully from the CodexUnity project.

## Goal

Start the UI/UX rewrite without breaking the current working game.

Phase 1 creates a new parallel Canvas-based UI system, fully coded and prefab-driven by Codex. It must coexist with the current scene hierarchy:

- Keep current `UI_Canvas / Panel_Main` working.
- Do not expand `MenuView.cs` for new product screens.
- Do not delete or disable the current menu/HUD until the replacement prefab is validated.
- Build the new UI as small prefabs and controllers, then replace one legacy surface at a time.

The first phase is not a visual polish phase. It is an architecture and validation phase.

## Target Parallel UI Root

Create a new root prefab:

```text
Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab
```

Prefab hierarchy:

```text
WarlineCaptureAppCanvas
  SafeAreaRoot
    HeaderBar
    ContentRoot
    FooterBar
    ModalOverlay
    TooltipLayer
```

The runtime scene should eventually contain this beside the old canvas:

```text
Game.unity
  UI_Canvas                  existing, do not break
  WarlineCaptureAppCanvas       new parallel UI root
```

During early validation, `WarlineCaptureAppCanvas` can be inactive or hidden behind a feature flag. The current `Button_Game` flow must remain usable until the new main menu launch path is proven.

## New Folder Layout

Create these folders first:

```text
Assets/Game/Scripts/UI/Shell
Assets/Game/Scripts/UI/Screens
Assets/Game/Scripts/UI/Popups
Assets/Game/Scripts/UI/Components
Assets/Game/Prefabs/UI/Shell
Assets/Game/Prefabs/UI/Screens
Assets/Game/Prefabs/UI/Popups
Assets/Game/Prefabs/UI/Components
Assets/Game/Configs/UI
```

Do not move existing UI scripts in Phase 1.

## Phase 1A - Code Skeleton

Purpose: introduce routing and screen lifecycle without touching current UI behavior.

Create scripts:

```text
Assets/Game/Scripts/UI/Shell/UIRoute.cs
Assets/Game/Scripts/UI/Shell/UIRouter.cs
Assets/Game/Scripts/UI/Shell/WarlineCaptureScreenController.cs
Assets/Game/Scripts/UI/Shell/WarlineCaptureModalController.cs
Assets/Game/Scripts/UI/Shell/UISafeArea.cs
Assets/Game/Scripts/UI/Shell/UIBootstrap.cs
Assets/Game/Scripts/UI/Shell/ScreenRouteButton.cs
```

Initial routes:

```text
Splash
MainMenu
Settings
QuickCustomSetup
Match
```

Rules:

- `WarlineCaptureRouter` owns active route and back stack.
- `WarlineCaptureScreenController` exposes `Show()`, `Hide()`, and optional `Bind()`.
- `WarlineCaptureModalController` owns one modal overlay root.
- `WarlineCaptureUiBootstrap` can instantiate the new canvas prefab, but default behavior must not hide the legacy UI yet.
- No dependency from new shell code back into `MenuView.cs`.

Validation:

- EditMode test can instantiate router and switch routes without exceptions.
- EditMode test confirms unknown/missing route fails clearly.
- Existing scene still opens with current legacy UI.

## Phase 1B - Shell Prefab

Purpose: create the new Canvas container as a prefab with stable names.

Create:

```text
Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab
```

Required components:

- `Canvas`
- `CanvasScaler`
- `GraphicRaycaster`
- `WarlineCaptureRouter`
- `WarlineCaptureModalController`
- `WarlineCaptureSafeArea`

Canvas settings:

- Render Mode: `Screen Space - Overlay`
- Canvas Scaler: `Scale With Screen Size`
- Reference Resolution: `1920 x 1080`
- Match: `0.5`
- Target layout: landscape

Required child names:

```text
SafeAreaRoot
HeaderBar
ContentRoot
FooterBar
ModalOverlay
TooltipLayer
```

Validation:

- Prefab loads from `Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab`.
- Required children exist.
- Canvas scaler values match the mobile reference resolution.
- `ModalOverlay` starts inactive or visually hidden.
- `ContentRoot` contains no embedded screens; screens are assigned as separate prefab references on `WarlineCaptureRouter`.
- No current `Game.unity` behavior changes yet.

## Phase 1C - Empty Screen Prefabs

Purpose: make routing visible but not functional yet.

Create minimal prefabs:

```text
Assets/Game/Prefabs/UI/Screens/Screen_Splash.prefab
Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab
Assets/Game/Prefabs/UI/Screens/Screen_Settings.prefab
Assets/Game/Prefabs/UI/Screens/Screen_QuickCustomSetup.prefab
Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab
```

Each screen prefab:

- Root has matching `WarlineCaptureScreenController`.
- Root has a stable `RectTransform`.
- Root fills `ContentRoot` unless intentionally a HUD overlay.
- Contains one temporary TMP title label for validation only.
- Is saved as its own prefab and instantiated by `WarlineCaptureRouter` under `ContentRoot`.

Validation:

- Router can instantiate and register all screen prefabs.
- Router shows exactly one screen under `ContentRoot`.
- Switching MainMenu -> Settings -> Back returns to MainMenu.
- Touch/click blocking is limited to the new canvas and does not block legacy gameplay while the new shell is inactive.

## Phase 1C.5 - Splash / Loading Screen With Tips

Purpose: pull `SCN-01 Splash / Loading` forward from the full UI/UX spec so the new parallel UI has a polished first screen early.

This is the first non-empty screen to implement because it is self-contained and easy to validate without touching gameplay.

Upgrade:

```text
Assets/Game/Prefabs/UI/Screens/Screen_Splash.prefab
```

Target hierarchy:

```text
Screen_Splash
  BackgroundImage
  LogoRoot
    LogoImage
    TitleText
  LoadingBar
    Fill
  StatusText
  TipText
```

Required behavior:

- Uses `Assets/Game/Textures/Logo.png` for `LogoImage`.
- Shows a status line such as `LOADING ASSETS...`.
- Shows one rotating loading tip in `TipText`.
- Supports progress binding from `0..1`.
- Supports a minimum visible duration so it does not flash instantly.
- Routes to `MainMenu` when bootstrap loading completes.
- Does not require a separate scene in Phase 1; it is instantiated into `WarlineCaptureAppCanvas/ContentRoot` at runtime.

Create support code/config:

```text
Assets/Game/Scripts/UI/Screens/SplashScreenController.cs
Assets/Game/Configs/UI/LoadingTips.asset
```

`LoadingTips.asset` should contain short player-facing tips sourced from the design direction, for example:

- Upgrade command-center systems before extended operations.
- Use roads to speed up base logistics.
- Scout before committing squads into hostile districts.
- Keep production and defense balanced during long attacks.

Validation:

- Splash prefab loads.
- Logo reference is assigned.
- `LoadingBar` can be set to `0`, `0.5`, and `1`.
- `TipText` is populated from `LoadingTips.asset`.
- After simulated load completion, router moves from `Splash` to `MainMenu`.
- Legacy UI still works when the parallel UI is disabled.

## Phase 1D - Scene Integration, Disabled by Default

Purpose: put the new system in the real scene without replacing the legacy UI.

Steps:

1. Add `WarlineCaptureUiBootstrap` to a new scene object:

```text
WarlineCaptureUIBootstrap
```

2. Assign `UIShellAppCanvas.prefab`.
3. Add a serialized flag:

```text
enableParallelUiOnStart = false
```

4. When false, bootstrap can instantiate the prefab inactive or skip instantiation.
5. When true, bootstrap instantiates the prefab beside `UI_Canvas`.

Validation:

- With flag false, the game behaves exactly like now.
- With flag true in editor, the new shell appears and route switching works.
- Existing `UI_Canvas / Panel_Main / Panel_Menu / Panel_Home / Button_Game` still starts gameplay.
- Android build must not depend on enabling the new shell yet.

## Phase 1E - Main Menu Shell, Still Parallel

Purpose: build the first real replacement screen without using it as default yet.

Create first real screen prefab:

```text
Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab
```

Hierarchy:

```text
Screen_MainMenu
  TopProfileBar
    LogoImage
    CommanderNameText
    SettingsButton
  ModeCardList
    ModeCard_Saga
    ModeCard_Operation
    ModeCard_QuickCustom
  BottomUtilityBar
```

Create component prefabs as needed:

```text
Assets/Game/Prefabs/UI/Components/ModeCardView.prefab
Assets/Game/Prefabs/UI/Components/ResourceCounterView.prefab
Assets/Game/Prefabs/UI/Components/ActionButtonView.prefab
```

Behavior:

- Saga card: opens placeholder popup.
- Operation card: opens placeholder popup.
- Quick Custom card: routes to `QuickCustomSetup`.
- Optional debug-only direct match button can call existing gameplay launch, but must be hidden or clearly marked for development.

Validation:

- `Assets/Game/Textures/Logo.png` is assigned to `LogoImage`.
- Three mode cards are visible in 1920x1080 landscape.
- Buttons have >= 80 px touch targets at reference resolution.
- No new code is added to `MenuView.cs`.

## Phase 1F - Cutover Gate

Only after Phase 1A-1E pass:

1. Add a project setting or serialized bootstrap flag to choose the starting UI:

```text
UseLegacyMenu
UseParallelCodexUi
```

2. Default remains `UseLegacyMenu`.
3. In editor/dev builds, enable `UseParallelCodexUi` for testing.
4. In release/Jenkins builds, keep legacy until the main menu and quick custom setup are complete.

Cutover criteria:

- New MainMenu can launch existing gameplay.
- Back/settings flow works.
- Android landscape safe area verified.
- Runtime log panel remains reachable from the existing debug path or a new popup.
- EditMode scene/prefab validation passes.
- PlayMode bootstrap smoke test passes.
- Android APK builds and is archived by Jenkins.

## Immediate Work Order

Implement in this exact order:

1. Create folders and route/shell scripts.
2. Add EditMode tests for route lifecycle.
3. Create `UIShellAppCanvas.prefab`.
4. Add prefab validation tests for shell hierarchy.
5. Create empty screen prefabs.
6. Wire router to empty screens.
7. Upgrade `Screen_Splash.prefab` into the loading screen with logo, progress, status, and tips.
8. Add loading-tip config and Splash route validation.
9. Add disabled-by-default scene bootstrap.
10. Validate legacy UI still works.
11. Build first real `Screen_MainMenu.prefab`.
12. Add mode-card component prefab.
13. Route mode cards to placeholders/QuickCustom.
14. Add cutover flag, still defaulting to legacy.

## What Not To Do In Phase 1

- Do not delete `UI_Canvas`.
- Do not remove current `Panel_Main` panels.
- Do not migrate the tactical HUD yet.
- Do not merge UI Toolkit removal into this phase.
- Do not expand `MenuView.cs` except for a tiny compatibility hook if absolutely required.
- Do not change gameplay systems for UI polish.
- Do not make the new shell default in Jenkins until Quick Custom launch is validated.

## First Phase Completion Definition

Phase 1 is complete when:

- The new parallel UI canvas exists as a prefab.
- The scene can optionally load it.
- Router can switch Splash, MainMenu, Settings, QuickCustomSetup, and MatchOverlay.
- Splash has a real loading layout with logo, loading bar, status text, and rotating tips.
- The first MainMenu prefab exists with mode cards.
- Existing gameplay launch is not broken.
- Tests validate hierarchy and route behavior.
- Android build still succeeds.

Completion validation performed:

- `WarlineCaptureUiShellTests`: 11 passed, 0 failed.
- Android APK build: succeeded, output at `/Users/farhad/Projects/WarlineCapture-CodexUnity/Build/AndroidAPK/WarlineCapture.apk`.
- `git diff --check`: clean.
