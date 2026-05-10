Status: needs fixes
Topic:
M01 squad readability, selected state, and projectile/VFX scale are not explicit enough for AAA Gate 4

Lane:
PM

Task:
Route user-observed M01 visual feedback issues into the active Gate 4 plan.

Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-squad-selection-projectile-art-blocker.md`

Contracts touched:
- No source contract changed. Existing M01 contract already requires selection ring, move marker, attack marker, impact VFX, destroyed VFX, unit grounding, and visual readability. This report makes the acceptance criteria concrete.

User-visible behavior:
- User saw the rifle squad as four soldiers baked into one sprite. Expected: four distinct soldiers under one controllable squad identity, with readable individual presence/formational spacing.
- User expected selected states after selection. Expected: clear world selected state and HUD selected state.
- User saw enemy fire as oversized bullets. Expected: tactical-scale AAA projectile/impact VFX, not arcade-scale bullets.

Validation run:
- PM reviewed M01 production contract, FTUE design, current critical path, active Gameplay task, QA/HCI task, and recent reports.

Validation result:
- Needs fixes / not ready for final Gate 4. The broad idea was planned through M01 feedback/VFX/readability requirements, but the plan did not explicitly constrain four-soldier squad readability or projectile scale.
- Added active criteria so these cannot be treated as optional polish before Gate 4.

Known gaps:
- Gameplay/Art must provide or integrate a readable four-soldier squad presentation under `unit.player.rifle_squad_01`.
- Gameplay/UI/VFX must show selected state after selection with a world marker/ring/outline and HUD selected state.
- Gameplay/Art/VFX must resize/restyle enemy projectile or impact visuals to tactical AAA scale.
- If art approval is needed, Gameplay should provide the smallest approval package: squad idle/move/attack/damaged/death, selected state, and projectile/impact size comparison.

Cross-lane impacts:
- Gameplay remains active owner for unit presentation and projectile/VFX runtime scale unless PM splits out a dedicated art lane.
- QA/HCI must verify four-soldier readability, selected state, and projectile/impact scale before final Gate 4 pass.
- UI may own HUD selected-state issues if the world state is correct but HUD feedback is missing.
- Support/FTUE remains waiting unless selected-state or assistant guidance is misleading.

Next recommended task:
Gameplay should include squad readability, selected-state proof, and projectile/impact scale proof in the next update to `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`, or write a focused follow-up if art approval is required first.
