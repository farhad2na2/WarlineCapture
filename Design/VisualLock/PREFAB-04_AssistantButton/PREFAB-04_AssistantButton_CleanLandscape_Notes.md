# PREFAB-04_AssistantButton Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png`.
- Style validation contact sheet: `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Style_ContactSheet.png`.
- Source background intent: blurred WarlineCapture match HUD/gameplay context with objective tracker, threat feed, resource bar, squad tray, command bar, minimap, and isometric battlefield visible as defocused context.
- Source design: `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`.
- Purpose: AAA target-lock mockup for the persistent ARIA assistant button in real Match Overlay context.
- Direction: in-game HUD-context visual target with the assistant button as the first-read subject. This is not a production prefab, not a generic state board, and not a new gameplay HUD layout.
- Layering: flat visual target reference only; the production button still needs a separated state set and reusable waveform icon before art-register approval.

## Required Controls

- `IdleState`
- `RecommendationState`
- `CriticalState`
- `TakeoverState`
- `MutedState`

## Implementation Notes

- Match WarlineCapture's dark military RTS HUD language, Oxanium typography, cyan edge light, and restrained amber action accents.
- Keep labels readable in the target. Unity implementation should use live TMP text and should not bake state labels into final reusable button art unless PM approves.
- The target intentionally shows five button states in gameplay context: idle, recommendation available, critical, takeover/control, and muted.
- State differentiation must not rely only on hue. Preserve non-color cues in production: steady waveform, pulse dot/chevron, warning notch/triangle, command/control bracket or lock cue, and muted slash/sleep cue.
- ARIA waveform/icon art shown here is placeholder visual-lock content until reviewed and approved in `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`.
- Keep register rows for `aria.icon.waveform` and `aria.button.state_set` as `missing` / `not_reviewed` / `not_started` until a separated production asset set exists and PM explicitly approves status changes.
- Style-alignment review should compare this target against `SCN-08_RTSBattleHUD`, `PREFAB-05_AssistantPanel`, `POP-10_AssistantTakeover`, and `SCN-03_CommanderProfile` using the contact sheet. The target should read as a match-HUD resident surface, not standalone sci-fi art.
