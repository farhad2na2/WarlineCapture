# Match Intro Transition Plan

## Goal

Create a polished Match start sequence that hides late visual spawning and reveals the battlefield cinematically.

The desired player experience is:
- Loading remains visible until gameplay runtime and world spawning are actually ready.
- The loading panel exits with a smooth scale/fade motion instead of disappearing abruptly.
- The Match HUD uses the existing shell motion language: header slides from top, left from left, right from right, and footer from bottom.
- The world is initially covered by a black curtain.
- The gameplay camera performs a small settle/zoom motion while hidden or partially hidden.
- The black curtain fades out last, revealing the final camera framing and ready HUD.
- Player input unlocks only after the intro transition completes.

## Architecture Rules

- Do not rebuild UI prefabs wholesale.
- Do not mutate the gameplay camera directly from UI code.
- Camera intro movement must flow through the existing camera request / selection camera boundary, or through a new narrow camera-intro request system if needed.
- UI views remain serialized-reference binders.
- No runtime `Object.Find*`, `GameObject.Find`, hierarchy string lookup, static mutable view registries, or broad manager/controller shells.
- Match startup readiness must continue to use the existing loading/runtime gate. Do not replace real readiness with a fixed delay.
- The black world curtain should be a UI overlay, not a camera clear-color/render-pipeline trick.

## Current Baseline

- Match loading now uses staged startup progress.
- `MatchBootstrapSystem.GameplayStartComplete` waits for the runtime loading gate before `MatchStartSceneSystemHelper` marks match start complete.
- `MenuBootstrapSystem` holds `Match ready` briefly before exiting loading.
- Existing `UIShellView` / `UIMotionHostView` already supports region transitions and should remain the primary UI motion owner.
- Current smoke validation can fail under `-nographics` because the minimap render texture path cannot create/render targets in that mode. This is separate from the intro transition and should be accounted for during validation.

## Target Timeline

Default timings:

```text
0.00s  Loading reaches real world-ready state.
0.00s  Loading status shows "Match ready".
0.25s  Loading panel starts scale/fade out.
0.55s  Loading panel gone.
0.55s  Match HUD region slides start.
0.60s  Camera settle request starts behind black curtain.
0.75s  Black curtain fade-out starts.
1.35s  Curtain gone, HUD settled, input unlocks.
```

The exact values should be configurable on a small config/view field, but the first implementation may use constants near the shell transition code if that matches existing motion patterns.

## Implementation Steps

### Phase 1: Inspect Existing Shell Motion

- [x] Review `UIShellView`, `UIMotionHostView`, and loading/match HUD command sequence behavior.
- [x] Confirm how region enter/exit animations are currently defined for header, left, right, middle, footer, popup, and loading layer.
- [x] Identify whether loading layer exit already supports scale+alpha, or whether a new loading-exit step is needed.
- [x] Confirm Match HUD region entry currently uses the same slide directions expected by the user.

Progress notes:
- 2026-06-08: `UIShellView` already drives loading exit with parallel alpha-to-zero and scale-to-zero via `UIMotionHostView`; `EnterMatchHud` primes header/left/right/footer offscreen and enters them with existing region slide directions.

### Phase 2: Add Match Intro State/Data

- [x] Add a data component or shell-edge state for Match intro transition progress.
- [x] Track states such as `Inactive`, `WaitingForWorldReady`, `ExitingLoading`, `EnteringHud`, `FadingCurtain`, and `Complete`.
- [x] Keep this state in ECS/shell data where possible so loading, HUD motion, input lock, and camera requests can coordinate without static state.
- [x] Add short status labels that fit fixed strings safely.

Progress notes:
- 2026-06-08: Added `MatchIntroTransitionComponent` on the UI shell boundary. `UiShellFlowSystem` moves it from `WaitingForWorldReady` to `EnteringHud` to `Complete`, keeps input locked during Match entry, and unlocks after the shell command sequence completes. `FadingCurtain` is reserved for the curtain slice.

### Phase 3: Add Black Curtain View

- [x] Add a narrow `MatchIntroCurtainView` with serialized references to a root `GameObject` and `CanvasGroup`.
- [x] Add APIs only for visual state, such as `SetVisible`, `SetAlpha`, and `Configure`.
- [x] Wire the view by serialized reference on the shell or Match HUD content path.
- [x] Avoid prefab rebuild. Only add/assign the minimal required component/object if the curtain object does not already exist.
- [x] Ensure the curtain renders above the world and behind/above HUD according to desired visual layering. Preferred: black covers world during HUD slide, then fades to reveal both the world and settled HUD cleanly.

Progress notes:
- 2026-06-08: Added `MatchIntroCurtainView` and serialized `MatchIntroCurtain` scene object under `WarlineCaptureRuntimeShell`. The curtain is a full-screen black `Image` with a `CanvasGroup`, starts inactive/transparent, is referenced by `UIShellView`, and is layered after `MenuBackgroundRegion` but before header/left/middle/right/footer/popup/loading so the HUD can slide above it while the world remains hidden.

### Phase 4: Loading Exit Animation

- [x] Update loading exit sequence to use scale and alpha tween instead of immediate hide.
- [x] Match existing middle-panel motion style where possible.
- [x] Keep the loading object active until the exit animation completes.
- [x] Preserve the current real readiness gate and `Match ready` hold before exit begins.
- [x] Verify progress text does not keep updating after exit starts in a way that causes layout flicker.

Progress notes:
- 2026-06-08: Existing `AddExitLoadingSteps` keeps the loading region active while alpha and scale tween out. `MenuBootstrapSystem` still gates completion on staged match readiness plus the `Match ready` hold.

### Phase 5: Match HUD Slide Reveal

- [x] Confirm `EnterMatchHud` uses existing region slide motions.
- [x] If a region snaps in, update only that region transition to use the existing slide style.
- [x] Ensure header, left, right, and footer are synchronized with the intro timeline.
- [x] Keep Match HUD content installed behind the curtain before curtain fade-out.
- [x] Avoid changing the visible hierarchy names unless required.

Progress notes:
- 2026-06-08: `EnterMatchHud` already enters header, left, right, and footer together through `RegionEnterFactory`. With the curtain serialized on `UIShellView`, Match loading shows the curtain opaque, HUD regions slide in above it, and the curtain fade step runs after the HUD entry step.

### Phase 6: Camera Settle Request

- [x] Add a small camera-intro request path that does not mutate the camera directly from UI.
- [x] Preferred behavior: start slightly zoomed out, then ease into the default match camera zoom/focus.
- [x] Reuse `SelectionUiCameraSystemHelper` or existing camera request buffers if they can express smooth focus/zoom.
- [x] If existing systems cannot express this cleanly, add a narrow ECS request component consumed by the camera system.
- [x] Keep the camera move short and subtle so it feels cinematic without disorienting the player.

Progress notes:
- 2026-06-08: Reused the existing `RtsCameraRequestSystem` path in `RtsSelectionRuntimeCameraSystemHelper`. On first match play activation, the camera queues a slightly zoomed-out perspective start while the curtain is opaque, then sets zoom transition active so the existing smooth `UpdatePerspectiveMode` request eases back to normal height/FOV.

### Phase 7: Input Lock Until Intro Complete

- [x] Block world selection and command input while Match intro state is not `Complete`.
- [x] Use ECS/shell runtime state, not direct UI flags or static globals.
- [x] Keep menu/shell escape/pause behavior reasonable if available.
- [x] Confirm no move/attack/scan command can be issued during the pre-curtain Match intro window.

Progress notes:
- 2026-06-08: `SelectionUiCommandSystem` and `RtsSelectionRuntimeInputSystem` now consume a cached ECS lock delegate from `MatchIntroTransitionComponent`. World pointer input, queued move orders, and command-button requests are ignored while the shell-owned intro lock is active. Because the curtain fade is sequenced inside `EnterMatchHud`, `UiShellFlowSystem` does not mark the intro complete until the fade step finishes.
- 2026-06-08: Inspected shell route and popup request flow. The intro lock is only injected into gameplay selection/command input systems, while shell route buttons and popup requests continue through `UiShellFlowSystem`; no separate escape/pause gameplay handler is currently gated by the intro lock.

### Phase 8: Validation And Tuning

- [x] Run `git diff --check`.
- [x] Run Unity compile validation.
- [x] Run playmode/smoke validation in a graphics-capable editor session if the minimap render target still fails under `-nographics`.
- [x] Verify the sequence through graphics-capable runtime smoke:
  - loading reaches `Match ready`;
  - loading scales/fades out;
  - HUD slides in using existing region directions;
  - black curtain fades out last;
  - camera settles subtly;
  - no buildings visibly spawn after curtain is gone;
  - input unlocks only after the intro completes.
- [x] Check `FrameRateDiag` before/after and confirm the intro does not add new runtime stalls.

Progress notes:
- 2026-06-08: Previous slice validation passed `git diff --check` and Unity shadow-project C# compile under `-batchmode -nographics`. Focused `RtsSelectionInputSystemTests` command exited successfully, but Unity did not emit the requested XML result file at `/private/tmp/warlinecapture-rts-selection-input-tests.xml`; treat the added lock test as compile-validated until a graphics/editor test-runner pass confirms it.
- 2026-06-08: Curtain and camera-settle slices passed `git diff --check` and Unity shadow-project C# compile under `-batchmode -nographics` using `/private/tmp/warlinecapture-match-intro-curtain-compile.log` and `/private/tmp/warlinecapture-match-intro-camera-compile.log`. Visual/runtime tuning remains pending in a graphics-capable editor session.
- 2026-06-08: Added focused validation coverage. `RtsCameraSystemTests.RunMatchIntroValidation` passed with `[RtsCameraMatchIntroValidation] result=Passed tests=1`, confirming the first-play camera intro starts zoomed out and transitions through camera requests. `UIShellCurrentContentLoadTests.RunFocusedValidation` passed with `[UIShellCurrentContentLoadValidation] result=Passed tests=5`, including `MenuSceneShellSerializesMatchIntroCurtain` for serialized curtain references and layer ordering. Unity `-runTests` still did not emit XML for the direct filtered test invocation, so explicit `-executeMethod` validation is the reliable local gate for these slices.
- 2026-06-09: Graphics-capable shadow-project smoke passed with `MatchRuntimeShellSmokeValidation.RunFrameRateDiagnostics`. The validation reached `mode=MatchHud`, `route=Match`, `phase=MatchHudReady`, `transition=0`, `matchIntro=Complete`, `inputLocked=0`, `matchSceneLoaded=1`, `hudLoaded=1`, and `curtainHidden=1`. No low-FPS `[FrameRateDiag]` was emitted during the stable post-intro observation window. A `FreezeDetect` BuildingPlacement hitch was still logged before the loading gate became ready, so the heavy setup remains behind loading/curtain rather than appearing after the intro.
- 2026-06-09: Tuned camera intro visibility after user feedback that no zoom-out was visible. The first implementation started settling while the curtain was still opaque and used a subtle `+4` height / `+2` FOV offset. The camera now starts farther out (`+8` height / `+5` FOV), holds that zoom while the shell intro/input lock is active, and begins a slower request-driven settle only after `MatchIntroTransitionComponent` reaches `Complete`. `RtsCameraSystemTests.RunMatchIntroValidation` now covers both the original request path and the delayed-settle behavior with `[RtsCameraMatchIntroValidation] result=Passed tests=2`.

## Completion Criteria

- [x] Match loading never disappears before runtime world readiness.
- [x] Loading panel exits with a smooth scale/fade motion.
- [x] Match HUD regions use existing slide-in directions.
- [x] Black curtain fade hides late visual setup.
- [x] Camera settles through a proper camera request boundary.
- [x] Input is locked until the intro transition is complete.
- [x] Targeted compile and runtime validation pass, or any validation limitation is documented with a concrete reason.
