# PM Review: UI Assistant Panel Match HUD Mount

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_ui_assistant-panel-match-hud-mount.md`

## Decision

Needs fixes before acceptance.

The code/test portion appears promising, but the visual capture validation is not acceptable because both reported capture files render as blank gray frames.

## Validation Checked

- `/private/tmp/warlinecapture-assistant-panel-tests.xml`: `WarlineCaptureUiAssistantPanelTests` passed 6/6.
- `/private/tmp/warlinecapture-assistant-panel-controller-tests.xml`: `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4.
- `/private/tmp/warlinecapture-matchoverlay-tests.xml`: `WarlineCaptureUiMatchOverlayTests` passed 18/18.
- `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`: exists at 1672x941, but renders as a blank gray frame.
- `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`: exists at 2400x1080, but renders as a blank gray frame.

## Blocking Issue

The report says capture validation was produced for 16:9 and 20:9, but the artifacts do not show the match HUD, ARIA entry button, assistant panel, objective tracker, threat feed, command HUD, or any occlusion/readability evidence.

For UI work, an existing file is not enough. The capture must prove the mounted surface is visible and readable. This matters because WarlineCapture has already seen deterministic/mock UI passes that can satisfy structural tests while failing visual/HCI quality.

## Required Fix

UI should fix the capture path or capture setup and resubmit this handoff with:

- A visible 16:9 capture showing the match HUD with the ARIA entry button.
- A visible 20:9 capture showing the same placement under mobile/tall aspect constraints.
- If possible, a second open-panel capture proving the assistant panel does not occlude command HUD, objective tracker, threat feed, or result surface anchors.
- A short note explaining why the previous captures were blank and what changed.

## Cross-Lane Notices

- Support/FTUE can continue implementing recommendation data against `AssistantPanelController`, but should not assume the match HUD visual mount is accepted until UI resubmits valid captures.
- QA/HCI should treat this as a major UI validation gap, not a gameplay blocker.
- Gameplay typed command hooks are unaffected.

## Next Recommended Task

UI should pause further assistant UI expansion and fix the visual capture/readability proof for `Screen_MatchOverlay` assistant mount.
