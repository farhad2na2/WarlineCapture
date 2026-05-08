# PM Review: UI PREFAB-04 Assistant Button Production

Date: 2026-05-07

## Reviewed Handoff

- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production.md`

## Decision

Needs fixes before PM acceptance.

The technical validation is useful and should be preserved as an implementation checkpoint, but the captured assistant HUD entry is not yet acceptable as final AAA production quality.

## Accepted Parts

- Reusable prefab direction is correct: `PREFAB-04_AssistantButton` replaces the inline ARIA match-overlay entry.
- Runtime boundary is correct: UI does not execute gameplay directly and exposes state through `AssistantButtonView`.
- Five-state support is directionally correct: idle, recommendation, critical, takeover/control, muted.
- Focused tests passed according to the UI handoff:
  - `WarlineCaptureUiAssistantButtonTests`: 3/3
  - `WarlineCaptureUiMatchOverlayTests`: 18/18
- PM independently confirmed `WarlineCaptureUiMatchOverlayTests`: 18/18 in `Design/AgentReports/2026-05-07_pm_match-overlay-editmode-validation.md`.

## Required Fixes

- The assistant entry is too small and visually crowded in both 16:9 and 20:9 captures.
- The waveform, ARIA label, and state label compete inside the button instead of reading as a high-quality tactical assistant control.
- The button does not yet feel like a final AAA HUD lock aligned with the stronger existing WarlineCapture match HUD chrome.
- The asset-register rows must remain unapproved until revised captures are reviewed.

## Validation Evidence

Review captures:

- `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`
- `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`
- `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`
- `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`

## Cross-Lane Impact

Support/FTUE should not yet wire final live recommendation state into the assistant button as a locked UI surface. It may continue against the typed assistant service and command executor contracts, but final visible button binding should wait for UI revision.

Gameplay is not blocked by this UI visual fix.

## Next Task For UI

Revise `PREFAB-04_AssistantButton` and the match-overlay mount so the closed assistant HUD entry is readable, premium, and aligned with approved WarlineCapture HUD styling at both 16:9 and 20:9.

Minimum acceptance criteria:

- The closed assistant button reads instantly at gameplay scale.
- ARIA label, waveform, and state cue do not crowd or overlap.
- Five states remain visually distinct using non-color-only cues.
- Captures are regenerated for 16:9 and 20:9, both closed and assistant-open.
- `WarlineCaptureUiAssistantButtonTests` and `WarlineCaptureUiMatchOverlayTests` still pass.

## User Decision Needed

No immediate user decision. UI should fix before PM acceptance.
