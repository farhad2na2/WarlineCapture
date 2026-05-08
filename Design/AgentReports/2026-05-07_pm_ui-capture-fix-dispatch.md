# PM Dispatch: UI Capture Fix

Date: 2026-05-07

## Trigger

`Design/AgentReports/2026-05-07_ui_assistant-panel-match-hud-mount.md` was reviewed in `Design/AgentReports/2026-05-07_pm_ui-assistant-match-hud-mount-review.md`.

## Decision

UI match HUD mount remains unaccepted until visual capture validation is fixed.

## Task Updates

- `Design/AgentTasks/ui_current.md` now directs UI to fix and resubmit visible 16:9 and 20:9 capture/readability proof.
- `Design/AgentTasks/support-ftue_current.md` now warns that Support/FTUE may continue recommendation-service work against `AssistantPanelController`, but should not treat the match HUD mount as visually accepted.
- `Design/AgentTasks/qa-hci_current.md` now tracks the UI capture fix as a major HCI/readability gate.

## Cross-Lane State

- Gameplay: unaffected; continue assistant typed command hooks.
- UI: fix capture/render proof before expanding assistant UI further.
- Support/FTUE: continue first recommendation-service slice, keep UI mount dependency marked pending visual acceptance.
- QA/HCI: watch the resubmitted captures and keep active balance QA blocked.
