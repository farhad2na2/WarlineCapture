# WarlineCapture UI/UX Phase 2 Immediate Implementation Plan

Date: 2026-05-02

Status: Phase 2 implementation completed on 2026-05-02. Shared component prefabs and bindable view scripts are in place, focused component and shell EditMode validation passes, and an Android APK build completed successfully from the CodexUnity project.

## Goal

Phase 2 turns the Phase 1 shell into a usable UI construction kit.

The goal is not to replace the legacy game UI yet. The goal is to create stable, reusable Canvas prefabs and small bindable view scripts that future screens can use without growing `MenuView.cs` or rebuilding one large scene canvas.

Phase 2 must preserve:

- Current `UI_Canvas / Panel_Main` behavior.
- Existing gameplay launch through the legacy menu.
- `WarlineCaptureUIBootstrap` defaulting to `UseLegacyMenu`.
- Android build compatibility.

## Phase 2 Scope

Build and validate shared UI components under:

```text
Assets/Game/Prefabs/UI/Components
Assets/Game/Prefabs/UI/Popups
Assets/Game/Scripts/UI/Components
Assets/Game/Scripts/UI/Popups
Assets/Tests/Editor
```

Phase 1 already created first-pass versions of:

```text
ModeCardView.prefab
ResourceCounterView.prefab
ActionButtonView.prefab
```

Phase 2 should upgrade these into stable components and add the next required prefab set.

## Component Inventory

### Existing Components To Harden

```text
ModeCardView
ResourceCounterView
ActionButtonView
```

Required upgrades:

- Stable child names matching the detailed UI/UX spec.
- Serialized references assigned on the view script.
- `Bind(...)` APIs with simple data structs where useful.
- 80 px minimum touch targets at 1920x1080 reference resolution.
- Default text uses an Oxanium font asset.
- No hard-coded gameplay behavior inside visual components.

### New Components To Create

```text
StatTileView
ObjectiveRowView
RewardItemView
PopupFrameView
SegmentedControlView
ToggleRowView
SliderRowView
```

These are the minimum shared parts needed before building Quick Custom, objectives, rewards, settings, and result screens.

## Target Prefab Contracts

### ModeCardView

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
```

Use for Saga, Operation, Quick Custom, future event cards, and dashboard cards.

### StatTileView

```text
StatTileView
  Icon
  LabelText
  ValueText
  DeltaText
```

Use for commander stats, operation dashboard metrics, match summary stats, and district state.

### ResourceCounterView

```text
ResourceCounterView
  Icon
  ValueText
  PlusButton
```

Use in top bars and results/reward surfaces. `PlusButton` can show a placeholder popup until economy/store is implemented.

### ObjectiveRowView

```text
ObjectiveRowView
  Icon
  LabelText
  ProgressText
  ProgressBar
    Fill
  CompleteIcon
```

Use for match HUD objective tracking, mission briefing objectives, and result objectives.

### RewardItemView

```text
RewardItemView
  Icon
  QuantityText
  RarityFrame
```

Use for mission result rewards, daily operation rewards, and unlock previews.

### ActionButtonView

```text
ActionButtonView
  Icon
  LabelText
  CostText
  LockRoot
  Button
```

Use for launch, confirm, build, train, upgrade, retry, and claim actions.

### PopupFrameView

```text
PopupFrameView
  Scrim
  Frame
    Header
      TitleText
      CloseButton
    BodyRoot
    ButtonRow
```

Use as the common frame for placeholder popups, warning popups, pause, confirmation, and future reward/result popups.

### SegmentedControlView

```text
SegmentedControlView
  SegmentRoot
```

Use for difficulty, graphics mode, match type, AI profile, and settings categories.

### ToggleRowView

```text
ToggleRowView
  LabelText
  DescriptionText
  Toggle
```

Use for binary settings.

### SliderRowView

```text
SliderRowView
  LabelText
  ValueText
  Slider
```

Use for volume, camera speed, game speed, spawn/resource tuning, and quick custom setup values.

## Runtime Script Pattern

Each view script should be small and UI-only:

```text
Assets/Game/Scripts/UI/Components/ModeCardView.cs
Assets/Game/Scripts/UI/Components/StatTileView.cs
Assets/Game/Scripts/UI/Components/ResourceCounterView.cs
Assets/Game/Scripts/UI/Components/ObjectiveRowView.cs
Assets/Game/Scripts/UI/Components/RewardItemView.cs
Assets/Game/Scripts/UI/Components/ActionButtonView.cs
Assets/Game/Scripts/UI/Popups/PopupFrameView.cs
Assets/Game/Scripts/UI/Components/SegmentedControlView.cs
Assets/Game/Scripts/UI/Components/ToggleRowView.cs
Assets/Game/Scripts/UI/Components/SliderRowView.cs
```

Rules:

- Serialized fields use lower camel case.
- Expose read-only properties only where tests or screen controllers need them.
- Provide `Bind(...)` methods.
- Do not call gameplay systems from component views.
- Do not use `FindObjectOfType`, `GameObject.Find`, or global lookups.
- Buttons expose `Button` references; screen controllers decide behavior.

## Editor Builder Updates

Extend:

```text
Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
```

Either rename later to `WarlineCaptureUiPrefabBuilder` or keep the current name for now to avoid churn.

Builder responsibilities in Phase 2:

- Regenerate all shared component prefabs.
- Assign all serialized references.
- Apply Oxanium font to all TMP text.
- Ensure touch target sizes.
- Leave screen prefab creation intact.
- Leave `WarlineCaptureUIBootstrap` defaulting to legacy.

## Tests

Add or extend EditMode tests:

```text
Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs
```

Required validation:

- Every Phase 2 component prefab exists.
- Required child names exist.
- TMP text fields use an Oxanium family font.
- Buttons have a target graphic.
- Primary buttons/cards are at least 80 px high at prefab reference size.
- Each `Bind(...)` method runs with placeholder data without exceptions.
- `PopupFrameView` starts closed or can close itself cleanly.
- No component prefab contains a screen router or gameplay bootstrap dependency.

Keep the existing `WarlineCaptureUiShellTests` focused on shell and screen routing.

## Immediate Work Order

1. Inventory current Phase 1 component prefab structure.
2. Add missing component view scripts.
3. Expand the builder to generate Phase 2 component prefabs.
4. Upgrade existing `ModeCardView`, `ResourceCounterView`, and `ActionButtonView` to the stable child contracts.
5. Add `PopupFrameView` and wire `WarlineCaptureModalController` to use it for placeholder popups.
6. Add `StatTileView`, `ObjectiveRowView`, and `RewardItemView`.
7. Add `SegmentedControlView`, `ToggleRowView`, and `SliderRowView`.
8. Add component prefab validation tests.
9. Regenerate prefabs through Unity batch mode in `WarlineCapture-CodexUnity`.
10. Run focused EditMode tests.
11. Run `git diff --check`.
12. Run Android APK build if the prefab/script surface changed significantly.

## Validation Commands

Use the CodexUnity project for batch validation:

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter WarlineCaptureUiComponentPrefabTests -testResults /private/tmp/warlinecapture-ui-phase2-components.xml -logFile /private/tmp/warlinecapture-ui-phase2-components.log
```

Existing shell tests should continue passing:

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter WarlineCaptureUiShellTests -testResults /private/tmp/warlinecapture-ui-phase2-shell.xml -logFile /private/tmp/warlinecapture-ui-phase2-shell.log
```

Build gate:

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -executeMethod BuildScript.BuildAndroid -buildType APK -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -logFile /private/tmp/warlinecapture-ui-phase2-android.log
```

## What Not To Do In Phase 2

- Do not make the new app shell the default menu.
- Do not remove or disable legacy `UI_Canvas`.
- Do not implement Quick Custom launch yet; that is the next screen phase.
- Do not migrate tactical HUD surfaces yet.
- Do not add gameplay dependencies to shared UI components.
- Do not use mockup images as final game art unless explicitly approved.
- Do not expand `MenuView.cs` for new product surfaces.

## Phase 2 Completion Definition

Phase 2 is complete when:

- All shared component prefabs exist with stable child names.
- All component scripts compile and expose clean `Bind(...)` methods.
- Component prefabs use Oxanium-family TMP fonts.
- Component prefab validation tests pass.
- Existing `WarlineCaptureUiShellTests` pass.
- `WarlineCaptureUIBootstrap` still defaults to `UseLegacyMenu`.
- `git diff --check` is clean.
- Android build still succeeds or reaches a clearly unrelated known build blocker.

Completion validation performed:

- `WarlineCaptureUiComponentPrefabTests`: 6 passed, 0 failed.
- `WarlineCaptureUiShellTests`: 11 passed, 0 failed.
- Android APK build: succeeded, output at `/Users/farhad/Projects/WarlineCapture-CodexUnity/Build/AndroidAPK/WarlineCapture.apk`.
- `git diff --check`: clean.

## Next Phase

Phase 3 should use these components to finish `Screen_MainMenu` as the full `SCN-02 Main Menu / Mode Select` screen:

- Top profile/resource bar.
- Left navigation placeholders.
- Mode cards using the hardened `ModeCardView`.
- Saga and Operation placeholder routes.
- Quick Custom route into the real setup screen.
