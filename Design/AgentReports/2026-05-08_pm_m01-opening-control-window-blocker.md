Status: needs fixes
Topic:
M01 opening combat kills the player before first control

Lane:
PM

Task:
Route the user-observed M01 first-control failure to the owning lane before M02 expansion.

Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-opening-control-window-blocker.md`

Contracts touched:
- None changed. Existing contracts already require this behavior before M02:
  - `Design/AgentTasks/M01_CRITICAL_PATH.md`
  - `Design/M01_FirstContact_Production_Contract.md`
  - `Design/FTUE_And_Command_Assistant_Design.md`

User-visible behavior:
- Current observed behavior is not acceptable for M01: the enemy can shoot and kill player units immediately after launch, before the player understands they have soldiers or can order movement.
- Expected behavior is that M01 first teaches selection and movement, then attack/objective/result.

Validation run:
- PM reviewed the M01 critical path and production/FTUE contracts.

Validation result:
- Needs fixes. This was planned before M02, not after.
- `Design/AgentTasks/M01_CRITICAL_PATH.md` blocks M02-M05 expansion until Gate 4 has no blocker findings.
- `Design/M01_FirstContact_Production_Contract.md` defines the M01 teaching goal as select, move, attack, read objective, and finish result.
- The same contract states the enemy patrol should patrol or hold along `route.enemy_patrol_01` until engaged.
- The user-observed immediate death prevents the select/move teaching goal and should fail Gate 4.

Known gaps:
- Need Gameplay implementation proof that the player has a readable first-control window before lethal enemy fire.
- Need validation that movement uses tactical walkable metadata/pathing and that select, move, attack, and result flow remain reachable after the fix.
- Need confirmation that unit animation behavior is acceptable for the current sprite/animator stack.

Cross-lane impacts:
- Gameplay is now active on `Design/AgentTasks/gameplay_current.md`.
- QA/HCI should pause final Gate 4 recommendation until the Gameplay fix report lands.
- UI and Support/FTUE remain waiting unless the gameplay fix exposes a concrete UI or assistant issue.
- M02 remains blocked until this and the remaining Gate 4 closeout items pass or are explicitly waived.

Next recommended task:
Gameplay should fix and report `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`.
