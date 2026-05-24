# WarlineCapture UIUX Runtime Shell Transition System Plan

## Purpose

Define the new runtime UI shell system that controls loading, main menu, match HUD, screen switching, and popup transitions from one central ECS-driven shell flow.

This plan replaces ad hoc per-screen transition behavior with a reusable shell. Screens provide content; ECS shell flow decides what should happen, and Unity shell views execute the animation.

## Recommendation

Build a persistent `WarlineCaptureRuntimeShell` prefab with:

- `UiShellFlowSystem` for flow decisions and command sequencing.
- `WarlineCaptureShellEcsBridgeView` for ECS-to-Canvas command execution.
- `WarlineCaptureShellView` for serialized region references.
- `WarlineCaptureUiMotionHostView` for tween execution.

Do not put transition logic inside individual screens. Each screen should expose region content and routing metadata. The shell flow should decide the motion sequence.

Do not add a tween package unless a later implementation step proves it is needed. The project already has Unity UI, `CanvasGroup`, shell routing, and screen controllers. A small in-project tween runner is enough for this first implementation and avoids a dependency/approval cycle.

## Shell Regions

The runtime shell should be divided into stable regions:

- `LoadingLayer`: full-screen loading screen and loading background.
- `HeaderRegion`: top header. In menu mode, this remains constant across menu screen changes.
- `LeftRegion`: left navigation or match left HUD content.
- `MiddleRegion`: main content area. This scales out/in during screen switches.
- `RightRegion`: commander/profile panel, contextual info, or match right HUD content.
- `FooterRegion`: match HUD footer or bottom command rail.
- `PopupLayer`: modal/result/threat/pause popups.

## Motion Rules

### Loading Exit

When game loading reaches 100%:

- Loading UI elements slide down to the bottom/offscreen.
- Loading background scales from `1` to `0`.
- Loading layer fades/gone after the transition finishes.
- The next shell mode begins only after the loading exit sequence completes.

### Main Menu Enter

Main menu opens in this order:

1. Header slides from offscreen top into place.
2. Left navigation slides from offscreen left into place.
3. Right commander/profile panels slide from offscreen right into place.
4. Middle content scales from `0` to `1` at center.

All movement should be eased, not linear.

### Main Menu Screen Switching

While in main menu mode:

- Header remains constant and does not animate out.
- Middle area scales from `1` to `0`, swaps content, then scales from `0` to `1`.
- Left area slides out left and back in only when the destination screen changes the left content.
- Right area slides out right and back in only when the destination screen changes the right content.
- The shell owns the swap timing so content cannot overlap or pop in early.

### Match Enter

When entering a match:

1. Show loading.
2. Exit menu regions:
   - Left panel slides left.
   - Right panel slides right.
   - Middle scales to `0`.
   - Header slides back to offscreen top.
3. Match HUD enters:
   - Header slides from offscreen top.
   - Left HUD slides from offscreen left.
   - Right HUD slides from offscreen right.
   - Footer slides from offscreen bottom.

### Result Popup

Result screen is a popup, not a full shell mode.

- Popup appears centered with scale `0` to `1`.
- Popup hide scales from `1` to `0` at center.
- Popup uses `PopupLayer` and does not mutate the shell regions underneath.
- After result confirmation, show loading, then return to main menu enter sequence.

## Flow Order

Expected runtime flow:

1. App starts.
2. Loading screen appears.
3. Loading reaches 100%.
4. Loading exits with bottom slide and background scale-out.
5. Main menu enters with header, left, right, middle sequence.
6. User switches menu screens; header remains stable, middle swaps through scale transitions, side regions swap only when needed.
7. User enters match.
8. Loading appears.
9. Menu exits.
10. Match HUD enters with header, left, right, footer sequence.
11. Match completes.
12. Result popup scales in.
13. Result popup scales out.
14. Loading appears.
15. Return to main menu enter sequence.

## Proposed Runtime Types

### `UiShellFlowSystem`

ECS system that owns shell state, transition sequencing, and route-to-shell-mode mapping.

Responsibilities:

- Start application shell flow.
- Show/hide loading.
- Enter menu shell.
- Switch menu screens.
- Enter match HUD shell.
- Show/hide popups.
- Prevent overlapping transitions.
- Expose hooks for gameplay loading/progress.
- Write presentation commands for Unity views to execute.

### `WarlineCaptureShellRegionView`

Serialized Unity view wrapper for one shell region.

Responsibilities:

- Store anchored on-screen position.
- Compute offscreen positions.
- Hold active content instance.
- Provide `SetContent`, `ClearContent`, `SetVisible`, and `ResetTransform`.
- Own `CanvasGroup` for fade/interactability.

### `WarlineCaptureUiMotionHostView`

Small internal tween utility.

Required tween primitives:

- Anchored position tween.
- Scale tween.
- Alpha tween.
- Parallel group.
- Sequence group.
- Cancellation token or transition id guard.

Required easing:

- Ease out cubic for enters.
- Ease in cubic for exits.
- Ease in-out cubic for screen swaps.
- Optional overshoot for popup scale-in, kept subtle.

### `WarlineCaptureShellScreenConfig`

Config asset describing which content prefab belongs in each shell region.

Fields:

- Route or screen id.
- Header content prefab, optional.
- Left content prefab, optional.
- Middle content prefab, optional.
- Right content prefab, optional.
- Footer content prefab, optional.
- Region behavior flags.

### `WarlineCapturePopupLayerView`

Serialized Unity view for popup instantiation and centered scale animation.

Responsibilities:

- Instantiate popup under `PopupLayer`.
- Scale/fade popup in.
- Scale/fade popup out.
- Block duplicate popup transitions.
- Return completion callbacks to flow code.

## Content Strategy

The current target-lock screens include many full-screen/static prefabs. The new shell needs region-ready content.

Recommended migration:

1. Start with adapter prefabs for a vertical slice.
2. Keep the existing visual assets and screen prefabs available for reference.
3. Extract or rebuild region content only where the shell needs animation:
   - Header content.
   - Left navigation/content.
   - Middle content.
   - Right content.
   - Footer content.
   - Popup content.
4. Avoid cropping full-screen screenshots into runtime panels.
5. Prefer existing layered/generated sprites and current UI builders when constructing region-ready prefabs.

## First Vertical Slice

Build the first version with:

- SCN-01 loading/splash.
- SCN-02 main menu.
- SCN-08 match HUD.
- POP-05 mission result popup.

This slice proves the full runtime motion path before converting all remaining screens.

Required sequence:

`Loading -> Main Menu -> Loading -> Match HUD -> Result Popup -> Loading -> Main Menu`

## Validation Requirements

Focused validation should include:

- Unity edit/build validation for new scripts and prefabs.
- Runtime play-mode or editor-driven transition smoke test.
- Capture of the full sequence as images or video.
- Checks that:
  - No transition overlaps another transition.
  - Header remains stable during menu screen switching.
  - Popup scales from center.
  - Loading exits only after reaching 100%.
  - Content does not pop before its region animation starts.
  - Region content returns to exact designed anchored positions after tween completion.

## Known Risks

- Full-screen target-lock prefabs cannot become clean shell regions without adapter/extraction work.
- Existing `WarlineCaptureRouter` may need a thin bridge rather than a replacement.
- Loading progress source must be defined: simulated progress for UI smoke tests, real progress for runtime scene/match loading.
- If multiple gameplay systems request route changes during loading or result flow, the shell director must serialize them.

## Next Planning Step

Create a step-by-step implementation plan that splits the work into small validated slices:

1. Add shell/tween primitives.
2. Build shell prefab.
3. Add loading transition.
4. Add main menu enter transition.
5. Add menu screen switching.
6. Add match enter transition.
7. Add popup scale presenter.
8. Wire vertical slice.
9. Capture and validate.
10. Expand to remaining screens.
