# WarlineCapture UI/UX Phase 4 Immediate Implementation Plan

## Goal

Build the first usable parallel Settings and Accessibility screen without touching the legacy settings controls. This phase should make the new Codex UI route to a real settings screen, persist basic preferences, and establish the structure that later replaces the old menu settings safely.

## Execution Rule

Phase 4 should continue as screen-level vertical slices:

1. Match the target visual from the original mockup style.
2. Build real Canvas elements, not a flat target image.
3. Wire only the behavior needed to validate the slice.
4. Capture and compare at Android landscape aspects.
5. Optimize accepted art and controls before moving to the next slice.
6. Add tests so later changes cannot silently break hierarchy, visuals, import settings, raycasts, or persistence.

This means visual lock and implementation happen together. Do not postpone the visual match until after all Phase 4 behavior is wired, and do not create visual-only screens without route/runtime validation.

## Validation Steps

1. Add a small settings data model and `SettingsService` backed by `PlayerPrefs`.
2. Generate `Screen_Settings.prefab` with the target hierarchy from SCN-04:
   - `HeaderBar`
   - `BackButton`
   - `TitleText`
   - `TabStrip`
   - `SettingsScrollView`
   - `AudioSection`
   - `GraphicsSection`
   - `ControlsSection`
   - `AccessibilitySection`
   - `LanguageSection`
   - `FooterButtons`
3. Wire initial controls:
   - Master, Music, SFX volume sliders
   - Graphics quality segmented control
   - Frame rate segmented control
   - Camera sensitivity slider
   - High Contrast UI toggle
   - Large Text toggle
   - Colorblind mode dropdown
   - Language dropdown
4. Keep legacy gameplay speed and AI settings in the old UI for now.
5. Add EditMode tests for hierarchy, PlayerPrefs persistence, and settings screen bindings.
6. Regenerate prefabs through Unity batch mode and run focused EditMode tests.

## Phase 4.1 Scope

This first implementation slice does not yet apply full theme switching or localization at runtime. It stores values, updates labels and selected states, and applies safe runtime values where Unity supports it directly: master audio volume, target frame rate, quality level, and UI scale markers.

## Phase 4.1 Status

Completed:

- `Screen_Settings.prefab` exists as a parallel Codex Canvas screen.
- Settings uses the shared full-screen outer frame style also used by Splash/Loading.
- Audio sliders, graphics and frame-rate segmented controls, camera sensitivity, accessibility toggles, and language/colorblind dropdowns are real UI controls.
- Slider handles, toggles, dropdowns, tabs, and footer buttons visually follow the accepted Settings target style.
- Generated Settings art is atlas-ready with validated UI import settings.
- Decorative Settings graphics do not receive raycasts.
- Transparent placeholder `Image` components were removed from the generated Settings hierarchy.
- Focused EditMode validation covers Settings hierarchy, persistence, bindings, control geometry, sprite import settings, atlas labels, and raycast rules.
- `WarlineCaptureUiAccessibilityApplier` now provides the reusable Phase 4 bridge for large-text scaling and high-contrast surface color application. The app shell uses it to scale the routed content root, and Settings uses it for standalone visual application without double-scaling when hosted under the shell.

## Later Phase 4 Slices

- Add real theme variable switching for high contrast.
- Add large text scaling across the full app shell.
- Move existing gameplay speed and AI controls into `Screen_QuickCustomSetup`.
- Add pause-menu entry to the same Settings screen.
- Add localization string table integration once localization data exists.

## Next Phase 4 Slice

Continue with the smallest behavior slice that completes Settings as an app-level screen:

1. Expand high-contrast theme surfaces screen by screen as each screen is visually accepted.
2. Add a Settings entry point from the pause/menu overlay once that route exists.
3. Keep gameplay speed and AI controls in the old UI until `Screen_QuickCustomSetup` starts, then migrate them there in one focused slice.
