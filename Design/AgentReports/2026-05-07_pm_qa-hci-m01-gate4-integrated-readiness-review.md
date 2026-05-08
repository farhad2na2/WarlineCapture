Gate:
Gate 4 QA/HCI M01 Smoke And Readability
Status:
needs fixes
Reason:
QA/HCI completed the integrated readiness review using the accepted UI capture matrix, accepted Gameplay log-health evidence, accepted Support/FTUE assistant handoffs, and current sprite-renderer visual evidence. The review found no new blocker inside the UI prefab/editor capture matrix itself, but Gate 4 cannot be accepted yet because the evidence still lacks a real player-route pass and safe-area/device validation.
Validation accepted:
- UI capture matrix evidence is usable for QA review: 1920x1080 and 2400x1080, eight required states, visible HUD/minimap/objective/assistant/result surfaces.
- Gameplay log-health remains accepted for focused graphics-enabled editor evidence: no reproduced NullReferenceException, RenderTexture.Create failed, EntitiesGraphicsSystemUtility stack, generic AI plan noise, FreezeDetect, PerfDiag, or RuntimeCitySpawner hitch in the accepted scanned log.
- Support/FTUE service-level readiness remains accepted for typed intents and live assistant context.
- QA correctly keeps current AI-generated tactical/unit art as review evidence only, not final art approval.
Validation still needed:
- End-to-end M01 player-route pass proving match start, squad selection, move, attack, invalid-command recovery, assistant open, assistant takeover/Stop ownership state, result popup, and route logs.
- Safe-area/device assumptions or actual device/safe-area evidence for mobile 20:9.
- Player-route confirmation of assistant ownership cancellation and result-flow Stop behavior.
- Watch for editor shutdown leak warnings, Animator warnings, frame/input stalls, freezes, or gameplay-owned log spam during the player-route pass.
Cross-lane notices:
- QA/HCI remains active and owns the next pass.
- UI is not blocked by this report unless QA/HCI finds a concrete UI-owned issue during the player-route/safe-area pass.
- Gameplay is not blocked by this report unless the player-route pass reproduces freezes, input stalls, runtime exceptions, severe FPS drops, or gameplay-owned log spam.
- Support/FTUE is not blocked by this report unless player-route ARIA recommendation, ownership, Stop, or result explanation behavior is misleading.
- Art/design final approval remains separate: final atlas/config packaging, hostile non-color readability, and `vfx.unit.destroyed.small` remain open.
Tracking updates:
- `Design/AgentTasks/qa-hci_current.md` now explicitly owns the player-route/safe-area Gate 4 pass.
- `Design/AgentTasks/M01_CRITICAL_PATH.md` now records the QA/HCI integrated readiness review as needs fixes and narrows the remaining Gate 4 blocker to the player-route/safe-area pass.
Next gate/task:
QA/HCI should run the player-route M01 Gate 4 capture/log pass at 1920x1080 and 2400x1080 with safe-area/device assumptions explicit, then write `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-pass.md`.
