# SCN-07 Loadout / Squad Prep Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-07_LoadoutSquadPrep/SCN-07_LoadoutSquadPrep_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-07_loadout_squad_prep.jpg`.
- Recreate in Unity as separate unit cards, support slots, gear cards, mission summary, and deploy CTA.

## Implementation Notes

- Unit cards should be reusable and data-bound to roster entries.
- Support slots need available/locked/selected states.
- Gear cards need rarity and lock state support.
- `DEPLOY 10` should validate the selected loadout before launch.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Loadout / Squad Prep screen, matching the premium military RTS HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Dark mobile RTS pre-mission staging interface, hangar/armory atmosphere, subtle low-poly military equipment silhouettes, graphite panels and cyan highlights.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite panels, cyan bevels, smooth shadows.
- Top header bar: left back button, title "LOADOUT / SQUAD PREP", right home icon button, compact mission power line "Power Recommended 55,000".
- Left main panel: "SELECTED UNITS" with four large unit cards: Rifle Squad, APC, Tank, Helicopter. Each card has a replaceable low-poly thumbnail, count badge, health/power mini line, and rarity border.
- Center panel: "SUPPORT SLOTS" with three support ability slots, one locked slot with lock icon.
- Lower center panel: "RECOMMENDED GEAR" with gear/crate cards and rarity colored corner accents.
- Right panel: "MISSION SUMMARY" with objectives, star goals, enemy rating, and a large orange/yellow CTA button "DEPLOY 10".

Style requirements:
- Match accepted targets: dark beveled metal, cyan outlines, blue selected states, yellow/orange CTA and reward accents, crisp readable typography.
- Cards, slots, gear, mission summary, and deploy button must look like separate Unity UI pieces.
- No bright white borders, no hard block shadows, no cramped controls, no stretched UI, no watermark, no captions.
- Text must be legible and exactly as specified where quoted.
```
