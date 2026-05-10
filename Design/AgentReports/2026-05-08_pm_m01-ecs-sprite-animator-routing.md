Status: accepted
Topic:
Replace old ECS model animation output with ECS sprite-atlas animator

Lane:
PM

Task:
Capture the user's clarification that the previous ECS animation solution for `Model` must be replaced by a new ECS sprite animator built by Gameplay on top of the atlas.

Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-ecs-sprite-animator-routing.md`

Contracts touched:
- No source contract changed. This clarifies the runtime implementation required to satisfy M01's ECS animated atlas expectation.

User-visible behavior:
- Public M01 infantry should animate from sprite-atlas states, not from the old visible 3D `Model` or the old per-model GPU/material animation path.
- Movement, attack, damaged/hit, and death/destroyed visuals should come from the ECS sprite-atlas animator.

Validation run:
- PM reviewed source references:
  - `UnitGridAuthoring` bakes animation settings/order and model/destroyed child references.
  - `UnitAnimationIndexSystem` resolves ECS animation state and applies `MaterialAnimationIndex` to model visual roots.
  - `MissionRuntimeSpritePresenterSystem` already observes movement/attack/death state for M01 sprite presenter state.

Validation result:
- Accepted as PM routing.
- Gameplay should reuse useful ECS state inputs and timing/config where possible, but replace the output consumer for public M01 infantry with a sprite-atlas animator.
- Gate 4 should not pass while public M01 infantry animation depends on the old per-Model `MaterialAnimationIndex`/model visual-root path.

Known gaps:
- Gameplay must implement or adapt an ECS sprite-atlas animator that maps existing movement/combat/death state to atlas animation states/frames.
- The animator must not require a separate `Destroyed` child object.
- If atlas frames are missing or ambiguous, Gameplay must prepare the art approval package for the user.

Cross-lane impacts:
- Gameplay remains active owner.
- QA/HCI should verify the new ECS sprite-atlas animator path before final Gate 4.
- UI and Support/FTUE remain waiting unless the animator migration exposes concrete lane-owned issues.

Next recommended task:
Gameplay should update/follow up `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` proving public M01 infantry uses a new ECS sprite-atlas animator instead of the old Model animation output.
