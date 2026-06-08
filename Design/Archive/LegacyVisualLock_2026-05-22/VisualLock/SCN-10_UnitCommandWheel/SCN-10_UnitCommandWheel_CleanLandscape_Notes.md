# SCN-10 Unit Command / Command Wheel Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-10_UnitCommandWheel/SCN-10_UnitCommandWheel_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-10_unit_command_wheel.jpg`.
- Recreate in Unity as a live battle overlay with real radial buttons and contextual selected-unit data.

## Implementation Notes

- Command wheel segments must be individual selectable controls.
- Available/disabled/selected command states should be data-driven from the selected unit capability set.
- Keep the battle HUD context visible but secondary while the command wheel is active.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Unit Command / Command Wheel overlay, matching the premium military RTS HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Active low-poly urban battlefield, selected helicopter/squad near center, tactical selection glow and command targeting hints. The game world should remain visible behind the radial UI.

UI layout:
- Full-screen battle context with HUD frame and separate overlay elements.
- Center-right radial command wheel with a selected unit icon in the center labeled "BLACK HAWK".
- Wheel segments labeled "MOVE", "ATTACK", "PATROL", "ROPE DROP", "EXTRACT", "STOP" with clear icon placeholders and one blue highlighted segment.
- Left contextual card: selected unit portrait/thumbnail, health bar, status text "READY", and short capability row.
- Bottom-left small squad tray remains visible with three compact unit cards.
- Bottom-right minimap remains visible but dimmed behind overlay.
- Small top hint strip: "Select command, then tap target".

Style requirements:
- Match accepted targets: dark graphite metal, cyan highlights, blue selected states, soft shadows, premium military mobile RTS typography.
- Radial wheel segments, selected unit card, hint strip, minimap, and squad cards must read as separate Unity UI prefabs.
- Command wheel should be touch-sized, clean, and not cramped.
- No bright white borders, no hard block shadows, no stretched UI, no watermark, no captions.
- Text must be legible and exactly as specified where quoted.
```
