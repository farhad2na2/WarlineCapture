# PM Review - UI Assistant Runtime Binding Fix

Date: 2026-05-07
Lane reviewed: UI
Report reviewed: `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md`

## Status

Accepted.

## Reason

The fix closes the PM blockers from `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-review.md`. UI now validates live assistant presentation data, typed `Do It` execution, visible ownership status, player-input release through `AssistantRuntimeBinding.NotifyPlayerInputOutsideAssistant()`, and `M01.ResultExplain` Stop behavior without closing or acknowledging `POP-05_MissionResult`.

The capture set remains readable at 16:9 and 20:9, and the panel shows live objective recommendation data instead of placeholder content. The visible status line is acceptable for this gate; final wording/readability can still be checked during QA/HCI smoke.

## Validation Accepted

- `WarlineCaptureUiAssistantRuntimeBindingTests`: 7/7 passed.
- `WarlineCaptureUiAssistantPanelControllerTests`: 4/4 passed.
- `WarlineCaptureUiMatchOverlayTests`: 18/18 passed.
- Source grep found no banned screen-coordinate, child-name execution, or selected-entity-panel coupling in `AssistantRuntimeBinding.cs` / `AssistantPanelController.cs`.
- UI-scoped `git diff --check` passed.
- 16:9 and 20:9 closed/open assistant captures were regenerated and reported as RGBA with nonzero RGB variance.

## Validation Still Needed

- QA/HCI should include assistant recommendation, result-flow Stop, takeover ownership visibility, and player-input release in the integrated M01 smoke pass after Gameplay lands sprite-renderer close tactical evidence.
- World highlight rendering for `Show Me` remains outside this pass and should stay behind the typed preview/focus contract until separately assigned.

## Cross-Lane Notices

- UI: move to waiting/support status; do not start broad UI polish until QA reports a blocker or PM assigns a concrete task.
- Gameplay/Input: use `AssistantRuntimeBinding.NotifyPlayerInputOutsideAssistant()` when real player input outside the assistant panel should pause/cancel assistant-owned preview/takeover.
- QA/HCI: UI runtime binding is accepted; remaining readiness blocker is Gameplay sprite-renderer visual evidence.
- Support/FTUE: no new Support API is required.

## Tracking Updates

- Updated `Design/AgentTasks/M01_CRITICAL_PATH.md`.
- Updated `Design/AgentTasks/ui_current.md`.
- Updated `Design/AgentTasks/qa-hci_current.md`.
- No overall percentage change yet; QA/HCI has not run the integrated M01 smoke pass.

## Next Task

Gameplay continues `Design/AgentTasks/gameplay_current.md`. QA/HCI waits for gameplay sprite-renderer evidence, then runs M01 smoke/readability/performance checks.
