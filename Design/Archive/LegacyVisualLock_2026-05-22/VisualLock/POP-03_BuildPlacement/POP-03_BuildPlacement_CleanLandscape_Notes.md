# POP-03 Build Placement Visual Target

- Canvas: 1672 x 941.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/POP-03_build_placement.jpg`.
- Target type: popup/panel.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- This is not a crop, upscale, padding pass, or pixel promotion of the source JPG.
- Recreate in Unity as real layered Canvas UI: transparent frames/chrome, separate icons, TMP text, buttons, meters, rows, and content/fill layers.

Canonical target: `Design/VisualLock/POP-03_BuildPlacement/POP-03_BuildPlacement_Landscape_Target.png`

## Generation Prompt

```text
Use case: ui-mockup
Asset type: WarlineCapture landscape mobile game UI visual target mockup, 16:9, intended canonical canvas 1672x941.
Primary request: Create a new AAA-quality generated landscape target mockup for POP-03 Build Placement. This must be a new optimized landscape target in the accepted WarlineCapture premium military RTS HUD style, not a crop, upscale, or copy of the source reference.
Source intent reference: POP-03 appears after choosing a building from the Build Drawer, showing valid grid placement, footprint, cost, rotate, cancel, and confirm controls.
Scene/backdrop: Active low-poly RTS battlefield with a base construction area, green valid grid cells, a translucent ghost preview of a power plant, nearby roads and military structures, dimmed around the placement UI.
UI layout: Use a hybrid overlay/panel composition. Main world view visible full-screen. Bottom center compact dark graphite placement panel with cyan bevels and title exact text "PLACE BUILDING". Left thumbnail/card: "Power Plant" and small building render. Middle info: "Footprint 3 x 3", cost row with resource icons, status exact text "VALID PLACEMENT" in green. Right controls: square icon button for rotate, secondary "CANCEL", primary orange/gold "CONFIRM". Include a small close X and a subtle grid coordinate readout.
Style requirements: Match WarlineCapture accepted targets: dark graphite military HUD panels, cyan edge highlights, orange/gold CTA, green valid placement accents, soft shadows, crisp readable Oxanium-like typography. UI elements should be separable Unity Canvas parts. No watermark, no captions, no flat wireframe, no hard block shadows, no source-image border artifacts.
```
