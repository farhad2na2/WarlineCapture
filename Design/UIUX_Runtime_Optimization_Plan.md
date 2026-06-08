# WarlineCapture UI/UX Runtime Optimization Plan

## Current Main Menu Baseline

- Generated main menu artwork is split by runtime purpose:
  - `Atlas_MainMenu_IconsButtons`: buttons, icons, and commander portrait.
  - `Atlas_MainMenu_FramesChrome`: 9-sliced frames, rails, bars, masks, and chrome.
  - `Atlas_MainMenu_CardArt`: large mode-card artwork.
- The landscape visual target remains an authoring reference only and must not be packed into runtime atlases.
- Decorative `Graphic` components should not receive raycasts. Only interactive `Selectable` graphics should keep `raycastTarget` enabled.

## Current Settings Baseline

- Settings generated artwork follows the same atlas/import validation model as Main Menu.
- Shared screen chrome is reused where the mockups use the same outer-frame language. The Splash/Settings outer frame should stay shared unless a target clearly requires a different frame.
- Settings controls are real UI elements, not baked into the background:
  - tabs and footer buttons
  - segmented controls
  - sliders with fixed circular handles
  - toggles with separate track/fill/handle art
  - dropdowns with consistent height and chrome
- Transparent placeholder `Image` components should not be used as layout-only objects.
- Decorative Settings `Graphic` components should not receive raycasts.

## Import And Atlas Rules

- Generated UI textures must use `TextureImporterType.Sprite`, single-sprite mode, no mipmaps, clamp wrap mode, and bilinear filtering.
- Android import override should use ASTC compression for generated main menu art unless a specific texture proves visually unacceptable on-device.
- Every generated runtime UI texture should carry the common label `WarlineCaptureUI` plus one screen/family atlas label, for example:
  - `Atlas_MainMenu_IconsButtons`
  - `Atlas_MainMenu_FramesChrome`
  - `Atlas_MainMenu_CardArt`
  - `Atlas_Settings_Buttons`
  - `Atlas_Settings_Controls`
  - `Atlas_Settings_Frames`
- SpriteAtlas assets should be included in the build, disable rotation and tight packing, and use enough padding to avoid edge bleeding on mobile GPUs.

## Reusability Direction

1. Keep visual-target extraction centralized in `WarlineCaptureUiPhase1PrefabBuilder` until all first-pass screens are visually locked.
2. Promote repeated structures into reusable builders after two or more screens need the same behavior:
   - top bars and resource counters
   - footer/utility bars
   - nav tab buttons
   - 9-sliced panel frames
   - animated button state setup
3. Prefer prefab variants only when designers need direct scene-level editing. For generated screens, keep source-of-truth logic in code and validate with EditMode tests.

## Next Optimization Passes

1. Add screen-specific atlases using the same grouping approach as Main Menu.
2. Split large card/background art from small icons and chrome to avoid forcing small sprites onto oversized atlas pages.
3. Verify draw calls in Unity Frame Debugger on Android after each screen family is converted.
4. Reduce extra masks only after the visual lock is stable. Do not remove masks that enforce non-stretching or edge clipping unless the replacement matches the mockup.
5. Add automated checks for new screen atlases, texture import settings, and accidental runtime use of visual target images.

## Per-Screen Optimization Gate

Apply this gate immediately after a screen is visually accepted:

1. Confirm no visual target image is used as a runtime screen background.
2. Confirm repeated chrome uses shared 9-sliced sprites where possible.
3. Confirm generated runtime textures have `WarlineCaptureUI` and exactly the intended atlas family label.
4. Confirm Android texture override is set for generated runtime UI sprites.
5. Confirm decorative `Graphic.raycastTarget` is disabled.
6. Confirm interactive `Selectable` graphics keep valid target graphics and states.
7. Confirm no transparent placeholder `Image` components were left behind by the builder.
8. Run focused EditMode tests and `git diff --check`.
