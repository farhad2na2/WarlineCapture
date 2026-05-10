Status: accepted
Topic:
Gate 4 now requires the M01 golden playthrough before M02

Lane:
PM

Task:
Add explicit acceptance gates so agents cannot pass M01 through isolated evidence artifacts.

Files changed:
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_gate4-golden-playthrough-acceptance-reset.md`

Contracts touched:
- None changed. This enforces the existing M01 product direction before Gate 4.

User-visible behavior:
- M01 readiness now means the user can actually play the first mission loop:
  - launch from public path
  - see/select their rifle squad
  - move to tutorial cover
  - attack the hostile patrol
  - destroy/neutralize the patrol
  - reach objective/result popup
- M01 is explicitly infantry-only: one player rifle squad type and one enemy patrol type. Player vehicles, vehicle production, transport, base/build mechanics, and extra player unit types are out of scope before M02.

Validation run:
- Reviewed and updated the active task board and auto-continue protocol.

Validation result:
- Accepted as PM planning reset.
- Gate 4 cannot pass from screenshots, safe-area matrices, route wiring, isolated tests, editor-only scenes, or SpriteRenderer review captures.
- Every readiness report must now include `Golden playthrough impact`.

Known gaps:
- Gameplay still must implement/prove the actual golden playthrough from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- QA/HCI remains blocked until Gameplay lands and PM accepts that handoff.
- PM/user can still later decide marker/VFX temporary acceptance, but that decision cannot replace the golden playable path.

Cross-lane impacts:
- Gameplay is the active implementation lane.
- QA/HCI waits for the gameplay handoff, then reruns final Gate 4 against the golden path.
- UI and Support/FTUE stay waiting unless the gameplay handoff exposes concrete lane-owned regressions.
- M02-M05 remain blocked.

Next recommended task:
Gameplay completes `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` with golden playthrough impact, infantry-only scope proof, ECS animated atlas runtime proof, and validation results.
