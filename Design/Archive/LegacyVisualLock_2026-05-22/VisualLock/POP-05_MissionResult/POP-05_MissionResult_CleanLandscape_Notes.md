# POP-05 Mission Result Visual Target

- Canvas: 1672 x 941.
- Source reference: `Design/WarlineCapture_UIUX_Codex_Package/warlinecapture_uiux_spec_assets/POP-05_mission_result.jpg`.
- Target type: popup.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- This is not a crop, upscale, padding pass, or pixel promotion of the source JPG.
- Recreate in Unity as real layered Canvas UI: transparent frames/chrome, separate icons, TMP text, buttons, meters, rows, and content/fill layers.

Canonical target: `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png`

## Generation Prompt

```text
Use case: ui-mockup
Asset type: WarlineCapture landscape mobile game UI visual target mockup, 16:9, intended canonical canvas 1672x941.
Primary request: Create a new AAA-quality generated landscape target mockup for POP-05 Mission Result. This must be a new optimized landscape target in the accepted WarlineCapture premium military RTS HUD style, not a crop, upscale, or copy of the source reference.
Source intent reference: POP-05 appears at the end of Saga, Operation, or Quick Custom matches, showing victory/defeat, stars, stats, rewards, replay, and continue.
Scene/backdrop: Dimmed premium 2D isometric battlefield aftermath at dusk, friendly armored vehicles and city base in background, subtle celebratory light shafts, modal scrim.
UI layout: Large centered result modal occupying roughly 62 percent width and 70 percent height. Dark graphite beveled frame, cyan highlights, gold victory accents. Header exact text "VICTORY" with emblem and three gold stars. Subheader "Downtown Breakthrough" and "Duration 08:42  |  Difficulty Hard". Middle left stats grid: "Enemies Defeated 42", "Units Lost 3", "Buildings Captured 2", "Civilians Saved 18". Middle right reward grid: Commander XP, Credits, Materials, Rush Tickets, and Ranger Squad parts. Include objective checklist rows with check icons. Bottom buttons: secondary "REPLAY" and primary gold "CONTINUE". No Close X; Continue owns exit.
Style requirements: Match WarlineCapture accepted targets: dark military HUD panels, cyan bevels, orange/gold CTA, smooth shadows, crisp readable Oxanium-like typography, premium 2D isometric RTS context. Elements must look separable for Unity Canvas. No watermark, no captions, no flat wireframe, no hard block shadows.
```
