Gate:
M01 Critical Path Gate 2 - UI Visual Lock And Assistant Surface

Status: accepted

Reason:
The replacement `PREFAB-04_AssistantButton` target is no longer a flat state board. It presents the ARIA button inside a WarlineCapture match HUD/gameplay context, uses the existing dark military RTS chrome/cyan/amber language, includes five readable states, and provides a side-by-side style contact sheet against accepted nearby targets.

Validation accepted:
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png` exists at 1672x941 and was visually reviewed by PM.
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Style_ContactSheet.png` exists and shows alignment against `SCN-08_RTSBattleHUD`, `PREFAB-05_AssistantPanel`, `POP-10_AssistantTakeover`, and `SCN-03_CommanderProfile`.
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_CleanLandscape_Notes.md` correctly states this is a visual target only and keeps separated production assets unapproved.
- The UI report kept `aria.icon.waveform` and `aria.button.state_set` as missing/not reviewed/not started, which is correct.

Validation still needed:
- Separated production assets for `aria.icon.waveform` and `aria.button.state_set`.
- Reusable Unity prefab implementation for `PREFAB-04_AssistantButton`.
- Runtime/capture validation after the prefab is implemented.

Cross-lane notices:
- Support/FTUE can use the accepted button state vocabulary when documenting ARIA recommendation, critical, takeover/control, and muted states.
- Gameplay contracts are unchanged; UI must keep assistant execution behind typed intents.
- Art/register status must not advance to complete until separated assets are reviewed and runtime wiring exists.

Tracking updates:
No task-board files were edited in this heartbeat review. The PM should mark the visual-target portion of Gate 2 accepted in `Design/AgentTasks/M01_CRITICAL_PATH.md` during the next explicit task-board update.

Next gate/task:
UI next recommended task is to create separated `aria_waveform_icon.png` and `aria_button_state_set.png`, then implement `PREFAB-04_AssistantButton` as a reusable animated Unity prefab.
