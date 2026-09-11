# Field guide aligned with the Build popup — 2026-09-11

The shared M3/M4 field guide now follows SCN09 Build Drawer instead of the previous cyan, horizontally tabbed layout. Reference: the user's Build popup screenshot from 2026-09-11 at 09:29:42.

## Delivered

- Build popup's 1448 × 909 frame within the 1672 × 941 reference composition, dark directional gradients, steel borders at 3 reference pixels, and gold selection palette.
- Square 72 × 72 X close control, matching the Build popup's two-stroke mark.
- Left category tabs at 216 × 164 for lessons, classes, and M3's radio archive. Selected tabs use a gold frame; inactive icons are muted; unavailable radio is dimmed. The archive remains accessible directly from Classes once unlocked.
- Footer navigation at 93 pixels high, with the existing green primary-action style. Search/filter controls are 84 pixels high. Existing mission content, localization, pause behavior, class residency, and M4 catalog binding remain in place.
- Reading content fits beside the left rail. Longer English/Farsi copy and accessibility large text expand vertically; class portraits and radio art retain their aspect ratio. Scrollbar anchors constrain the handle to its track.

## Editor validation

Approved macOS wrapper only, GUI licensing with Hub open; isolated project `/private/tmp/warline-campaign-return-qa`. No Android validation requested.

- Standard text: `RepairGuideAndRunUiValidation`, `/private/tmp/warline-guide-style-ui-final.log`, pass marker and exit 0.
- Large text: `RunDeliveredUiLargeValidation`, `/private/tmp/warline-guide-style-large.log`, pass marker and exit 0.
- Both live passes cover 12 lessons and 57 classes in English/Farsi, 16:9 and 20:9, visible text fit, ordered Persian digits, search/filter behavior, at most one resident class asset, frozen mission time/positions/health while reading, camera return and selection preservation, scrolling, radio navigation from Classes, and the actual X returning to Pause.
- Radio QA stages the established 45-second archive-available fact after validating pause behavior; it does not claim to test timed clue delivery. Initial radio QA correctly encountered the locked state. A subsequent probe compile error was corrected and reimported in the wrapper-owned Editor before the completed pass.
- Existing EditMode suites: `M03PresentationTests`, `MissionHudRoadRegressionTests`, and `ProductionSourceGrowthArchitectureTests`; 34/34 passed, zero failures/skips, exit 0; results `/private/tmp/warline-guide-style-tests.xml`.

Screenshots remain under `/private/tmp/warline-m03-editor-probe`; no evidence images are included in Design or Git. Only the shared guide prefab is rebuilt for this change; the Build popup remains the visual reference.
