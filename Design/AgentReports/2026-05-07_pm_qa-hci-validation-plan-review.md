# PM Review: QA/HCI M01 Validation Plan

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_qa-hci_m01-validation-plan.md`

## Decision

Accepted as the QA/HCI readiness plan and third-project smoke baseline.

Active M01 balance QA remains blocked.

## Validation Checked

- `/private/tmp/warlinecapture-codexunity2-m01-playmode-results.xml`: `Chapter01M01PlayModeValidationTests` passed 3/3.
- `/private/tmp/warlinecapture-codexunity2-m01-runtime-editmode-results.xml`: `Chapter01M01PlayableRuntimeTests` passed 7/7.
- `/private/tmp/warlinecapture-codexunity2-m01-tactical-binding-results.xml`: `Chapter01TacticalRuntimeBindingTests` passed 4/4.
- `/private/tmp/warlinecapture-codexunity2-assistant-panel-controller-results.xml`: `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4.

The QA agent correctly avoided final balance conclusions and framed `/Users/farhad/Projects/WarlineCapture-CodexUnity2` as a smoke baseline plus future manual HCI gate.

## Blocking / Major Risks

- The PlayMode run passed but logged repeated `NullReferenceException` entries from Unity Entities graphics/resource GC paths during the M01 scene run. This does not fail the current automated tests, but it should block active balance QA until gameplay confirms whether it is benign editor noise or a real runtime cleanup/lifecycle issue.
- The run also logged `RenderTexture.Create failed`, preview-scene leak warnings, persistent allocation leak warnings, and a `RuntimeCitySpawner=2064.9ms` hitch. These are major readiness risks for manual HCI and balance timing.
- Manual player-operated validation has not happened yet.
- Assistant runtime and HUD mount are still incomplete, so QA cannot validate ARIA Show Me / Do It / Stop behavior in the real player route yet.

## Cross-Lane Notices

- Gameplay: after the current M01 PlayMode validation report lands, include a log-hygiene/readiness note for the third-project warnings and hitches. If they are known Unity editor noise, document that; otherwise schedule cleanup before balance QA.
- UI: next assistant work must mount the panel in the real match HUD/app shell and provide capture validation so QA can judge occlusion and readability.
- Support/FTUE: next runtime implementation must expose typed assistant recommendations and interruption/cancel behavior before QA can run the Full Guidance pass.
- Art/design: unit, marker, and target readability still need real capture review; automated smoke tests are not enough for AAA visual-readability approval.

## Next Recommended Task

Refresh the QA/HCI lane after the current completed task is reviewed:

- Do not start active balancing yet.
- Continue as a watcher lane: monitor new gameplay/UI/support handoffs and update the QA gate only when implementation changes affect M01 manual HCI validation.
- When gameplay reports M01 PlayMode validation complete, re-run the third-project smoke set and compare logs for regressions.
