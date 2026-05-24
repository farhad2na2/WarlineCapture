# GameUI Scene Runtime Shell Implementation Plan

## Goal

Create a new isolated `GameUI` Unity scene for the runtime UI shell without modifying the existing game scene, legacy UI scene objects, or legacy router behavior.

The first implementation proves the shell workflow in a clean scene:

`Loading -> Main Menu -> Loading -> Match HUD -> Result Popup -> Loading -> Main Menu`

The scene is a UI runtime validation scene first. Game-scene integration is a later task after the shell is stable.

## Non-Goals

- Do not edit `Assets/Game/Scenes/Game.unity`.
- Do not edit existing gameplay scenes to load this scene.
- Do not remove or rewrite legacy `WarlineCaptureRouter`, `WarlineCaptureScreenController`, `WarlineCaptureModalController`, or existing target-lock design scenes.
- Do not continue old target-match cleanup loops.
- Do not create a full app router replacement in the first slice.

## Architecture Rules

- ECS owns shell state, route requests, transition sequencing, and presentation commands.
- Unity UI `*View` classes own Canvas references and animations only.
- Bootstrap composes references only.
- Screen/popup prefabs are content; they do not decide shell flow.
- No runtime `static Instance` or new singleton service locator.
- The new scene must be self-contained and safe to open without gameplay scene dependencies.

## Target Files

New scene:

- `Assets/Game/Scenes/GameUI.unity`

New runtime scripts:

- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellView.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellRegionView.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellEcsBridgeView.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureUiMotionHostView.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellButtonRequestView.cs`

New configs/prefabs:

- `Assets/Game/Data/UI/Shell/WarlineCaptureShellMotionConfig.asset`
- `Assets/Game/Data/UI/Shell/WarlineCaptureShellScreenConfig.asset`
- `Assets/Game/Data/UI/Shell/WarlineCapturePopupConfig.asset`
- `Assets/Game/Prefabs/UI/Shell/WarlineCaptureRuntimeShell.prefab`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab`

New editor tooling for deterministic scene/prefab creation:

- `Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameUiShellCapture.cs`

## Step 1 - Create The Isolated Scene Skeleton

Build `Assets/Game/Scenes/GameUI.unity` with only:

- One root `GameUIRoot`.
- One `EventSystem`.
- One screen-space Canvas for the shell.
- One UI camera only if needed by project conventions.
- One `WarlineCaptureRuntimeShell` root object.

Validation:

- Open/build scene in Unity batchmode.
- Confirm `Game.unity` and legacy design scenes are untouched.

## Step 2 - Add Shell Region Views

Create the shell region hierarchy:

- `LoadingLayer`
- `HeaderRegion`
- `LeftRegion`
- `MiddleRegion`
- `RightRegion`
- `FooterRegion`
- `PopupLayer`

Each region gets a `WarlineCaptureShellRegionView` with:

- Onscreen anchored position.
- Offscreen direction.
- `CanvasGroup`.
- Active content root.
- Strict reset method that restores designed position, alpha, and scale.

Validation:

- Editor test or capture helper verifies each region has a bound RectTransform and CanvasGroup.

## Step 3 - Add Motion Host

Implement `WarlineCaptureUiMotionHostView` as the only first-slice tween runner.

Required primitives:

- Anchored position tween.
- Scale tween.
- Alpha tween.
- Sequence.
- Parallel group.
- Transition id cancellation guard.

Required easing:

- Ease out cubic for enter.
- Ease in cubic for exit.
- Ease in-out cubic for swaps.
- Subtle popup overshoot only if it does not cause layout drift.

Validation:

- Unit/editor smoke runs a dummy RectTransform through slide, scale, alpha, and cancel paths.

## Step 4 - Add ECS Shell Boundary

Create the ECS boundary data and system:

- `UiShellStateComponent`
- `UiShellRouteRequestComponent`
- `UiShellLoadingProgressComponent`
- `UiShellPopupRequestComponent`
- `UiShellPresentationCommandComponent`
- `UiShellTransitionCompleteComponent`
- `UiShellFlowSystem`

First-slice behavior:

- On app start, command `ShowLoading`.
- When loading completes, command `ExitLoading` then `EnterMenu`.
- Menu launch request commands `ShowLoading`, `ExitMenu`, then `EnterMatchHud`.
- Result request commands `ShowPopup`.
- Result confirm commands `HidePopup`, `ShowLoading`, then `EnterMenu`.

Validation:

- Pure ECS command-sequence test checks emitted command order and transition locks.

## Step 5 - Add Unity/ECS Bridge View

Implement `WarlineCaptureShellEcsBridgeView`.

Responsibilities:

- Read shell presentation commands.
- Call `WarlineCaptureShellView` to execute animations.
- Write transition-complete events back to ECS with sequence ids.
- Reject stale completion callbacks.

It must not decide route policy or gameplay outcomes.

Validation:

- Simulated command buffer drives all regions once and writes completion events.

## Step 6 - Build Region Content Prefabs

Create region-ready content prefabs from the latest approved UI assets, not old screenshots.

Initial content:

- Loading content: logo, progress, loading background.
- Main menu content: header, left nav, middle cards, right commander/profile area.
- Match HUD content: header, left tactical panel, right controls, footer.
- Result popup: POP-05 result body.

Rules:

- Content must fit inside its assigned region.
- No content is allowed to touch panel chrome.
- Icons keep aspect ratio.
- Text is centered in its own slot.
- Popup content lives only under `PopupLayer`.

Validation:

- Layout validation checks visible bounds against safe rects for each prefab.

## Step 7 - Wire The GameUI Scene Flow

Add a scene-only smoke driver for the `GameUI` scene.

The smoke driver can be editor-only or debug-only and must not become gameplay policy.

Sequence:

1. Show loading at 0%.
2. Animate loading to 100%.
3. Exit loading.
4. Enter main menu.
5. Trigger match entry.
6. Enter match HUD.
7. Show result popup.
8. Hide result popup.
9. Return to loading.
10. Enter main menu again.

Validation:

- Capture stills at each stable state.
- Optional capture video/GIF of the transition sequence.

## Step 8 - Add Hard Layout Guards

Add automated guards before PM review:

- Region visible bounds cannot leave Canvas safe area unless explicitly allowed.
- Major regions cannot overlap.
- Content visible bounds must stay inside region safe rects.
- Icons must be centered by alpha-visible bounds, not transparent texture bounds.
- Text RectTransforms must fit within their slot.
- Header must remain stable during menu screen switching.
- Popup must scale around center.

Validation:

- Batchmode runs scene build plus layout guard.
- Capture output includes diagnostic overlay only as a separate debug image, never as the review screenshot.

## Step 9 - Capture And Compare

Capture:

- `GameUI_Loading_Stable`
- `GameUI_MainMenu_Stable`
- `GameUI_MatchHud_Stable`
- `GameUI_ResultPopup_Stable`
- `GameUI_ReturnedMainMenu_Stable`

Also capture transition samples if time allows:

- Header entering.
- Side regions entering.
- Middle scaling.
- Popup scaling.

Validation result must state:

- Unity command run.
- Log path.
- Scene path.
- Capture paths.
- Remaining visual gaps.

## Step 10 - Handoff

Write a WarlineCapture UI handoff report under `Design/AgentReports/`.

Required fields:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task

## Implementation Order

1. Create scene builder and scene skeleton.
2. Add shell region view and motion host.
3. Add ECS shell components and flow system.
4. Add bridge view.
5. Build simple placeholder content to validate motion.
6. Replace placeholders with latest approved region assets.
7. Add hard layout guards.
8. Capture stable states.
9. Write handoff report.

This order keeps the scene isolated and proves the shell motion before deeper runtime integration.
