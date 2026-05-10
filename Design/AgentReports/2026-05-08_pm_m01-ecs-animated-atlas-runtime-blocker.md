Status: needs fixes
Topic:
M01 visible units still depend on SpriteRenderer review/runtime bridge instead of final ECS animated atlas presentation

Lane:
PM

Task:
Route the user's runtime presentation expectation to Gameplay before Gate 4/M02.

Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-ecs-animated-atlas-runtime-blocker.md`

Contracts touched:
- None changed. Existing contracts already require runtime entity ids and Chapter 1 unit atlas presentation:
  - `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
  - `Design/AgentTasks/M01_CRITICAL_PATH.md`

User-visible behavior:
- The user sees SpriteRenderer-based unit presentation in the playable game.
- Expected behavior is that each soldier/unit is an ECS runtime entity with animated sprite-atlas presentation, pathing-aware movement, and correct idle/move/attack/death visual states.

Validation run:
- PM reviewed current M01 critical path, production contract, and prior sprite-presenter/renderer handoffs.

Validation result:
- Needs fixes / not final accepted. Prior `SpriteRenderer` reports were accepted only as implementation/review evidence for the current M01 slice, not as final approval for runtime unit presentation.
- `M01_SpriteRenderer_CloseCapture.png` is explicitly review-art evidence, not final art/runtime approval.
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md` defines `unit.player.rifle_squad_01` and `unit.enemy.patrol_01` as runtime sprite/entity rows backed by the Chapter 1 units atlas.
- The user's expectation upgrades this from known follow-up to active Gate 4 blocker: visible public M01 units must not look like design-target SpriteRenderer proxies.

Known gaps:
- Gameplay must prove public M01 launch uses ECS runtime entities for player and enemy units.
- Gameplay must prove or implement atlas-backed animated states for idle, move, attack, and hit/death/destroyed where currently required.
- If Unity `SpriteRenderer` remains as a temporary ECS-driven presentation adapter, Gameplay must document that it is not the gameplay source of truth and not final infrastructure.

Cross-lane impacts:
- Gameplay owns the fix/proof in `Design/AgentTasks/gameplay_current.md`.
- QA/HCI should not pass Gate 4 visual/runtime readiness until the Gameplay handoff lands.
- UI and Support/FTUE remain waiting unless the Gameplay fix exposes a concrete UI or assistant issue.
- PM/user owns any explicit waiver that accepts a temporary SpriteRenderer adapter for this milestone.

Next recommended task:
Gameplay should include ECS animated atlas runtime proof in `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`, alongside the opening-control-window fix.
