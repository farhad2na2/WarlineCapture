Lane:
UI

Task:
P1 route-driven M01 capture and simulated safe-area tooling handoff for QA/HCI Gate 4 rerun.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_01_MatchStart.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_02_SquadSelected.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_03_MoveFeedback.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_04_AttackFeedback.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_05_InvalidCommandRecovery.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_06_AssistantOpen.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_07_AssistantTakeoverStop.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_08_ResultPopup.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_01_MatchStart.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_02_SquadSelected.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_03_MoveFeedback.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_04_AttackFeedback.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_05_InvalidCommandRecovery.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_06_AssistantOpen.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_07_AssistantTakeoverStop.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_2400x1080_08_ResultPopup.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_CaptureMatrix_ContactSheet.png
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/safe_area_1920x1080.json
- Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/safe_area_2400x1080.json
- Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md

Contracts touched:
- Fulfilled the UI-owned handoff requested by Design/AgentTasks/ui_current.md without editing AgentTasks.
- Added editor-only menu command `WarlineCapture/UI/Capture M01 Route Safe Area Matrix`.
- Capture path now instantiates `WarlineCaptureAppCanvas.prefab`, routes through `WarlineCaptureRouter` to `WarlineCaptureRoute.Match`, then configures the same eight M01 states.
- Safe-area evidence is simulated by insetting `SafeAreaRoot` before route capture; production `WarlineCaptureSafeArea` runtime behavior was not changed.
- No runtime UI API, prefab route id, gameplay command API, asset-register status, or production source contract was changed.

User-visible behavior:
No production runtime behavior changed. The project now has reproducible editor tooling for route-driven M01 HUD screenshots with simulated landscape safe-area insets.

Validation run:
- Unity route-driven capture: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureM01RouteDrivenSafeAreaMatrix -logFile /private/tmp/warlinecapture-m01-route-safe-area-capture.log`
- Image health/contact sheet: Python/Pillow verified 16 state captures, expected dimensions, full alpha, nonzero RGB variance, and generated `M01_RouteSafeArea_CaptureMatrix_ContactSheet.png`.
- Visual review: contact sheet plus representative 2400x1080 assistant takeover/Stop and result popup captures.
- Unity EditMode: `WarlineCaptureUiShellTests`, results `/private/tmp/warlinecapture-route-safe-area-shell-results.xml`.
- Unity EditMode: `WarlineCaptureUiMatchOverlayTests`, results `/private/tmp/warlinecapture-route-safe-area-matchoverlay-results.xml`.
- Unity EditMode: `WarlineCaptureUiAssistantRuntimeBindingTests`, results `/private/tmp/warlinecapture-route-safe-area-assistant-runtime-results.xml`.
- Scoped diff hygiene: `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`.
- Runtime banned-call scan: not applicable to this pass because no runtime UI files were changed. The new route/capture traversal is editor-only tooling.

Validation result:
Passed for the UI tooling handoff. `WarlineCaptureUiShellTests` passed 15/15, `WarlineCaptureUiMatchOverlayTests` passed 18/18, and `WarlineCaptureUiAssistantRuntimeBindingTests` passed 7/7. The route-driven evidence folder contains the required eight states at both 1920x1080 and 2400x1080, plus a contact sheet and safe-area manifests.

Safe-area assumptions:
- `1920x1080`: simulated landscape safe area, left 64 px, right 64 px, top 32 px, bottom 24 px.
- `2400x1080`: simulated landscape safe area, left 112 px, right 112 px, top 44 px, bottom 28 px.
- The simulated outer margins are visible in the captures and represent excluded cutout/rounded-corner regions. This is not real Android/device validation.

Capture route:
- The captures instantiate `WarlineCaptureAppCanvas.prefab`.
- `WarlineCaptureRouter.Initialize()` and `WarlineCaptureRouter.GoTo(WarlineCaptureRoute.Match, false)` drive the visible route.
- Match state presentation reuses the accepted M01 HUD state configuration for match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup.

Known gaps:
- This is route-driven editor evidence, not a human device pass.
- Safe area is simulated, not sourced from a physical Android device or platform cutout API.
- The tooling validates UI route surfaces and safe-area layout evidence; it does not prove touch camera drag/pinch ergonomics, device thermal behavior, or real input latency.
- Current AI-generated tactical/unit art remains review evidence only. No asset-register rows were advanced.

Cross-lane impacts:
- QA/HCI is unblocked to rerun `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` using `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/`.
- Gameplay has no new action unless QA/HCI reproduces route/runtime freezes, input stalls, gameplay-owned exceptions, or command behavior issues.
- Support/FTUE has no new action unless QA/HCI finds misleading ARIA recommendation, ownership, Stop, Show Me, or result-explanation behavior.
- PM/art approval remains separate for final atlas/config packaging, hostile non-color readability, and `vfx.unit.destroyed.small`.

Next recommended task:
QA/HCI should rerun the Gate 4 player-route/safe-area review and write `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`.
