# SCN-13 Quick Custom Game Setup Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-13_QuickCustomGameSetup/SCN-13_QuickCustomGameSetup_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/WarlineCapture_UIUX_Codex_Package/warlinecapture_uiux_spec_assets/SCN-13_quick_custom_game_setup.jpg`.
- Recreate in Unity as real form controls, not a baked setup screen image.

## Implementation Notes

- Dropdowns, steppers, segmented controls, sliders, toggles, checkboxes, map preview, and launch CTA should be reusable prefabs.
- The setup screen should bind directly to `QuickGameConfig`.
- `LAUNCH MISSION` must validate config before entering the match.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Quick Custom Game Setup screen, matching the premium military RTS HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Dark tactical skirmish setup console with subtle low-poly map preview, graphite metal HUD panels, cyan highlights, blue selected controls, orange/yellow launch CTA.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite panels, cyan bevels, smooth shadows.
- Top header bar: left back button, title "CUSTOM GAME SETUP".
- Left configuration panel: "PRESET" dropdown, "Enemy Type" dropdown, "Enemy Count" stepper, "Difficulty" segmented buttons with "HARD" selected.
- Center configuration panel: sliders for "Starting Credits", "Income Multiplier", "Build Speed", "Aggression" with cyan tracks and numeric values.
- Right rules panel: "WIN CONDITION" dropdown, toggles/checkboxes for "Fog of War", "Intel Reveal", "Super Weapons", "Base Recovery", "Alliances".
- Far-right or lower-right map preview panel: title "MAP PREVIEW", low-poly desert city map thumbnail, map size label "Medium".
- Bottom-right primary CTA button: "LAUNCH MISSION" in orange/yellow military style.

Style requirements:
- Match accepted targets: dark beveled panels, cyan outlines, filled blue selected controls, orange/yellow CTA, smooth shadows, crisp readable typography.
- Dropdowns, steppers, segmented buttons, sliders, toggles, checkboxes, map preview, and launch button must look like separate Unity UI prefabs.
- No bright white borders, no hard block shadows, no cramped controls, no stretched UI, no watermark, no captions.
- Text must be legible and exactly as specified where quoted.
```
