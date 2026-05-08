Status: needs fixes
Topic:
Public M01 launch captures show brown tiny-world gameplay instead of readable production scene

Reviewed evidence:
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`
- `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_1920x1080_01_MatchStart.png`
- `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_01_MatchStart.png`

Finding:
The latest public-launch captures are not acceptable M01 production gameplay evidence. They show WarlineCapture HUD chrome, but the gameplay world is a mostly flat brown field with tiny centered content. This does not match the accepted gameplay camera/readability reference or the accepted HUD/gameplay composition targets.

Why it matters:
The user is seeing the same failure pattern the PM gate is meant to catch: technical route state and UI chrome can pass while the player-visible game remains unusable or visually off-direction. Hiding the old 3D prototype is not enough. The public launch must show the authored M01 tactical map/terrain, readable unit scale, HUD/objective context, and a camera composition that supports the first select/move/attack task.

Rejected evidence:
- Brown or blank world background.
- Tiny unreadable tactical content.
- Camera-only/world-only proof presented as manual-ready full-screen gameplay.
- Claims that `WarlineCaptureRoute.Match`, inactive `UI_Canvas`, or sprite renderer presence alone prove manual readiness.

Required next evidence:
- Full-screen public launch capture from `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch`.
- Full-screen public launch capture from Quick Custom/Test if that path is being kept as production-ready.
- HUD/objective/assistant context visible.
- Authored M01 tactical map/terrain visible, not a flat brown/blank field.
- Unit/target scale and camera framing comparable to `M01_SpriteRenderer_CloseCapture.png` and accepted integrated HUD captures.
- Explicit report of mission id, route, camera, map id, legacy 3D visibility, terrain visibility, and capture path.

Affected lanes:
Gameplay and UI.

Needs user decision:
No. This is an implementation and validation quality issue.

Next task update needed:
Done in `Design/AgentTasks/gameplay_current.md`, `Design/AgentTasks/ui_current.md`, and `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
