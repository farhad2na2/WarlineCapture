# M4 ARIA layout correction — 2026-09-14

The M4 extraction navigation row (Guide plus Team, Landing, or Departure) retained an absolute vertical position while the tutorial body and primary actions resized to their content. The row was absent from the ARIA height calculation. The next-click overlay also placed a large caption above ARIA’s action without reserving space, covering the instructions.

## Changes

- Bind M4 navigation into the shared ARIA layout in the delivered HUD and the M4 authoring path.
- Reserve space for visible utility rows before measuring the instruction viewport, place them beneath the primary actions, and include their visibility in layout invalidation.
- Keep the existing 72-unit touch target height, row spacing, cyan HUD styling, independent minimap, and scrolling for unusually long instructions.
- Retain the yellow next-click outline and localized caption on ARIA actions. Use a compact 32-unit-high caption fitted to the button width and reserve a 44-unit text-to-action gap. Controls outside ARIA retain their existing larger captions.
- Align both button rows to the same two equal-width columns, 20-unit column gap and 72-unit height; Team/Landing/Departure align exactly beneath Show Me.

## Regression coverage

The previous presentation test activated M3 utilities even when testing M4 copy. It now activates the correct mission row and the appropriate Team/Landing/Departure control, covering all 12 lessons in both languages and text sizes at 16:9 and 20:9. Geometry checks cover the text viewport, primary actions, utilities, panel boundary and minimap clearance. The live M4 launch probe checks the actual visible navigation row before each bilingual HUD capture.

## Validation results

- Shared HUD layout: **passed** 192 presentations (M3/M4 × 12 lessons × EN/FA × normal/large text × 16:9/20:9), plus empty/short/long content, independent minimap docking and non-blocking tutorial outline/caption and equal-width button column tests. Marker: `[AriaUtilityLayout] result=Passed`.
- Architecture: **passed**, nine fixtures, 139 checks, zero failures. Marker: `[MissionReadinessArchitecture] result=Passed fixtures=9 passed=139 failed=0`.
- Live M4 Editor launch: **passed** the normal opening camera tour, visible tutorial/navigation geometry and non-truncated next-click captions in both languages, all 12 guide topics and 57 classes in both languages, and guide pause/resume. Marker: `[M04EditorProbe] result=Passed`.
- Inspected all four live HUD captures: EN/FA at 1920×1080 and 2400×1080. Full first-lesson instructions remain visible; Continue/Show Me sit above Guide/Team; the compact yellow Continue lesson caption stays visible on the highlighted button without covering the instructions; the panel encloses both rows and clears the minimap.

Logs: `/private/tmp/warline-m04-caption-layout.log`, `/private/tmp/warline-m04-caption-architecture.log`, `/private/tmp/warline-m04-caption-live-final.log`. Screenshots: `/private/tmp/warline-m04-editor-probe/hud-{en,fa}-{16x9,20x9}.png`. The PNG dimensions were verified directly; Editor `Screen.width/height` diagnostics can report the docked window rather than the capture resolution.

This was an Editor UI regression pass, not a new full-mission completion audit or Android device test. Captures remain outside the Design folder.
