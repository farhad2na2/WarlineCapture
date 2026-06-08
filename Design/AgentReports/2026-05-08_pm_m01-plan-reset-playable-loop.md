Status: accepted
Topic:
M01 plan reset around the real playable loop

Lane:
PM

Task:
Correct the lane plan after user review showed agents were optimizing evidence artifacts instead of the actual Chapter 1 M01 playable goal.

Files changed:
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-plan-reset-playable-loop.md`

Contracts touched:
- No source contract changed. The reset enforces the existing product intent from:
  - `Design/M01_FirstContact_Production_Contract.md`
  - `Design/FTUE_And_Command_Assistant_Design.md`
  - `Design/AgentTasks/M01_CRITICAL_PATH.md`

User-visible behavior:
- M01 must be judged as a playable mission, not as a set of isolated captures.
- The user must be able to launch M01, see their soldiers, select/move them, observe pathing-aware movement and animation, survive long enough to learn, attack the patrol, and reach the objective/result flow.
- Visible public M01 units must be ECS runtime entities with animated sprite-atlas presentation. SpriteRenderer/design-target capture evidence is not final runtime acceptance.

Validation run:
- Reviewed active lane tasks and M01 critical path after user feedback:
  - immediate enemy fire kills the player before first control
  - visible SpriteRenderer-style unit presentation does not match expected ECS animated atlas units

Validation result:
- Needs fixes at the product level, but this PM routing reset is accepted.
- Gameplay is now the only active implementation lane because it owns the playable-loop blocker.
- QA/HCI is moved to waiting until Gameplay lands and PM accepts the playable-loop fix.
- UI remains waiting unless the gameplay fix exposes a concrete UI regression.
- Support/FTUE remains waiting unless the gameplay fix exposes a concrete assistant/FTUE regression.

Known gaps:
- Gameplay must implement/prove:
  - first-control survival window
  - select/move/attack/result reachable from public M01
  - tactical metadata/pathing-backed movement
  - ECS runtime entity ownership for visible units
  - animated atlas-backed idle/move/attack/death or destroyed visual states
- QA/HCI must rerun final Gate 4 only after that Gameplay handoff is accepted.
- PM/user may still need to decide marker/VFX temporary acceptance and final packaging after the playable loop is real.

Cross-lane impacts:
- Stop spending agent cycles on final safe-area/capture closeout until the playable loop is fixed.
- Stop treating `M01_SpriteRenderer_CloseCapture.png` as readiness evidence beyond review-art scale/readability reference.
- Keep M02-M05 blocked.

Next recommended task:
Gameplay completes `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`. PM reviews that report before reactivating QA/HCI final Gate 4.
