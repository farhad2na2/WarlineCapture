# PREFAB-05_AssistantPanel Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/PREFAB-05_AssistantPanel/PREFAB-05_AssistantPanel_Landscape_Target.png`.
- Source background: `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png` blurred and dimmed for panel/popup presentation.
- Source design: `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`.
- Purpose: High-quality ARIA recommendation panel over blurred Match Overlay context.
- Direction: focused high-quality panel/popup target over WarlineCapture UI context. This is not a new gameplay HUD layout.
- Layering: flat mockup reference only; no new sliced/layered art pack is required for this visual target pass.

## Required Controls

- `AssistantTabs`
- `RecommendationChips`
- `ShowMeButton`
- `DoItButton`
- `StopButton`

## Implementation Notes

- Match WarlineCapture's dark military RTS HUD language, Oxanium typography, cyan edge light, and restrained amber action accents.
- Keep labels readable in the target. Unity implementation should use live TMP text.
- ARIA and commander portrait/icon art shown here is placeholder visual-lock content until approved in `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`.
