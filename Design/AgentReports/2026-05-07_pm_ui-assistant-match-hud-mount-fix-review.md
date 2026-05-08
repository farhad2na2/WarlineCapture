# PM Review: UI Assistant HUD Mount Capture Fix

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_ui_assistant-panel-match-hud-mount-fix.md`

## Decision

Accepted as the visual validation fix for the match HUD assistant mount.

## Validation Checked

- `/private/tmp/warlinecapture-assistant-panel-tests.xml`: `WarlineCaptureUiAssistantPanelTests` passed 7/7.
- `/private/tmp/warlinecapture-assistant-panel-controller-tests.xml`: `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4.
- `/private/tmp/warlinecapture-matchoverlay-tests.xml`: `WarlineCaptureUiMatchOverlayTests` passed 18/18.
- `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`: visible 16:9 HUD capture.
- `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`: visible 20:9 HUD capture.
- `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`: visible 16:9 open-panel capture.
- `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`: visible 20:9 open-panel capture.

## Accepted Behavior

- The capture helper now fails under `NullGfxDevice` instead of writing misleading blank gray frames.
- The ARIA entry button is visible between the objective tracker and threat feed.
- The assistant panel opens into a fixed centered/docked rect and no longer expands across the command HUD, objective panel, threat feed, or minimap surfaces in the reviewed captures.
- The panel remains placeholder-bound and keeps Show Me / Do It / Stop as UI callback seams only.

## Known Gaps Accepted

- No Android/touch validation was run.
- Assistant content is still placeholder data, not live `WarlineCaptureAssistantService` data.
- Show Me / Do It / Stop still do not execute gameplay typed intents, highlights, ownership, or control cancellation.

## Cross-Lane Notices

- Support/FTUE can now treat the match HUD assistant surface as visually accepted for service-data binding.
- Gameplay typed command ownership remains separate and accepted in `Design/AgentReports/2026-05-07_pm_gameplay-typed-command-hooks-review.md`.
- QA/HCI can use the visible captures as the UI baseline, while keeping active balance QA blocked until live assistant behavior and manual player-route validation exist.

## Next Recommended Task

Support/FTUE should connect `WarlineCaptureAssistantService` data and command-intent execution boundaries into the mounted panel. UI should then validate live recommendation binding and button enabled/disabled states.
