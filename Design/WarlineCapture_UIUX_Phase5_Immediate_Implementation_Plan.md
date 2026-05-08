# WarlineCapture UI/UX Phase 5 Immediate Implementation Plan

## Goal

Build the first real `Screen_QuickCustomSetup` vertical slice so the Main Menu no longer starts gameplay directly from the Quick Custom card. The screen should expose the existing AI tuning knobs in a player-facing setup flow and launch the current tactical game path safely.

## Completed Slice

- `Screen_QuickCustomSetup.prefab` now replaces the previous placeholder screen.
- Main Menu `ModeCard_QuickCustom/Button` routes to `QuickCustomSetup` instead of using direct legacy game start.
- `QuickGameConfig` maps the initial Quick Custom controls to `AISettingsRuntimeState`.
- `QuickCustomScreenController` binds UI controls, reads selected setup values, applies them to runtime AI settings, and launches the existing gameplay path.
- `WarlineCaptureGameLaunchUtility` centralizes the shared "enable legacy canvas, request game start, hide router" launch behavior.
- Focused EditMode tests validate hierarchy, controller wiring, Main Menu routing, config-to-AI mapping, and controller bind/apply behavior.

## First Screen Contract

The current first pass includes:

- Preset dropdown.
- Enemy type dropdown.
- Enemy count minus/value/plus stepper, matching the mockup control family.
- Difficulty, starting money, build speed, production speed, attack group size, attack frequency, aggression, and expansion segmented controls.
- Income multiplier slider.
- Target priority dropdown.
- Player Auto AI toggle.
- Win condition, fog of war, intel reveal, and starting resources rule controls.
- Map preview with map name and seed input.
- Launch Mission button with dedicated gold CTA chrome and Bold label.
- Replaceable icon sprites for economy/gameplay rows, rules, and map stats.

## Next Slice

1. Visually lock `SCN-13 Quick Custom Game Setup` against `Design/VisualLock/SCN-13_QuickCustomGameSetup/SCN-13_QuickCustomGameSetup_Landscape_Target.png`.
2. Replace first-pass Settings-style layout with target-matched Quick Custom panels while keeping the existing controller bindings.
3. Add per-screen generated art and atlas/import validation for Quick Custom.
4. Keep future Quick Custom polish on target-specific control families: dropdowns, difficulty segmented buttons, enemy steppers, map stat cards, and CTA buttons should not reuse generic panel borders.
5. Add persistence for the last Quick Custom setup.
6. Add a payload-based launch path once gameplay mode infrastructure starts, while keeping the current `BeginGameplay()` compatibility path.
