Gate:
Gate 4 QA/HCI M01 Smoke And Readability
Status:
blocked
Reason:
QA/HCI ran the available player-route automation and focused assistant/shell validation. The automated route, assistant runtime, and shell tests are green, and the earlier severe log-health risks did not reproduce. Gate 4 still cannot pass because QA/HCI could not produce route-driven screenshots for the eight required states and could not validate real or simulated safe-area/device cutouts.
Validation accepted:
- `Chapter01M01PlayModeValidationTests` passed 3/3.
- `WarlineCaptureUiAssistantRuntimeBindingTests` passed 7/7.
- `WarlineCaptureUiShellTests` passed 15/15.
- Capture dimensions for the accepted UI matrix remain correct.
- QA log scan did not reproduce `NullReferenceException`, `RenderTexture.Create failed`, `EntitiesGraphicsSystemUtility`, `AIProduction`, `AIBuild`, `AISquad`, `FreezeDetect`, `PerfDiag`, or `RuntimeCitySpawner` issues.
Validation still needed:
- Route-driven screenshots or equivalent player-route capture evidence for the same eight states at 1920x1080 and 2400x1080.
- Explicit safe-area/device assumptions, simulated cutout/inset proof, or real device evidence.
- Route-level evidence for touch/camera behavior, assistant ownership release, and result-flow Stop behavior.
- Continued watch of Animator warnings, preview-scene leak warnings, persistent allocation warnings, and any player-visible hitches.
Cross-lane notices:
- UI now owns the missing route-driven capture/safe-area tooling support. This is a UI tooling deliverable, not a QA waiting loop.
- QA/HCI should wait for the UI route-driven capture/safe-area tooling handoff, then rerun the player-route/safe-area pass.
- Gameplay has no new blocker unless the next player-route pass reproduces runtime/gameplay failures.
- Support/FTUE has no new blocker unless the next pass shows misleading ARIA recommendation, ownership, Stop, or result explanation behavior.
Tracking updates:
- `Design/AgentTasks/ui_current.md` moved to active for route-driven M01 capture/safe-area tooling support.
- `Design/AgentTasks/qa-hci_current.md` moved to waiting on the specific UI tooling handoff.
- `Design/AgentTasks/M01_CRITICAL_PATH.md` now records the QA/HCI player-route pass as blocked by missing route-driven capture/safe-area evidence.
Next gate/task:
UI should implement or expose route-driven M01 capture/safe-area tooling and write `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`. QA/HCI should write the next rerun to `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` after that handoff.
