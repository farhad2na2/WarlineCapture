# SCN-06 Mission Briefing Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-06_MissionBriefing/SCN-06_MissionBriefing_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-06_mission_briefing.jpg`.
- Recreate in Unity as separate mission image, objectives, star goals, enemy intel, rewards, and CTA parts.

## Implementation Notes

- The mission key art must remain replaceable per mission.
- Objective, star-goal, enemy-intel, and reward rows should be reusable prefabs.
- `START MISSION` is a real interactive CTA, not baked text.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Mission Briefing screen, matching the premium military RTS HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target, not an exact crop of the source reference.

Scene/backdrop: Tactical mission command table interface, low-poly urban combat key art panel, graphite metal UI shell, cyan highlights, restrained orange/yellow CTA accents.

UI layout:
- Full-screen futuristic HUD frame with dark graphite/black panels, cyan edge highlights, subtle bevels, and smooth shadows.
- Top header bar: left back button, title "MISSION BRIEFING", mission label "3-6 DOWNTOWN BREAKTHROUGH".
- Large left mission image panel: premium 2D isometric city street combat key art with soldiers, armored vehicle, smoke, and urban ruins, framed as a replaceable image.
- Center briefing panel: title "BRIEFING" with a short readable paragraph: "Break through the downtown blockade and secure the command route.".
- Right column with stacked panels: "OBJECTIVES" list, "STAR GOALS" list with star icons, "ENEMY INTEL" tiles for Infantry, Armor, Air.
- Bottom reward strip: "REWARDS PREVIEW" with Commander XP, Credits, Materials, Rush Tickets, and explicit unlock icons.
- Bottom-right primary CTA button: "START MISSION" in orange/yellow military style.

Style requirements:
- Match accepted targets: dark beveled panels, cyan outlines, blue selected accents, yellow/orange rewards and CTA, crisp readable typography, AAA mobile military strategy polish.
- The image, objective rows, intel tiles, reward icons, and CTA must visually read as separate Unity UI parts.
- No bright white borders, no hard black block shadows, no cramped text, no stretched UI, no watermark, no captions.
- Text must be legible and exactly as specified where quoted.
```
