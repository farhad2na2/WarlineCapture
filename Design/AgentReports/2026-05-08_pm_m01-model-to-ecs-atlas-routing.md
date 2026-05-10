Status: accepted
Topic:
Route M01 unit presentation from old prefab Model to ECS animated atlas

Lane:
PM

Task:
Capture the user's implementation direction and update the active lane plan.

Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-model-to-ecs-atlas-routing.md`

Contracts touched:
- No product contract changed. This clarifies how to satisfy the existing M01 runtime sprite/entity contract.

User-visible behavior:
- M01 should not show old 3D unit `Model` children or a final SpriteRenderer proxy.
- Existing unit prefab/authoring identity may remain for ids, stats, footprint/pathing metadata, combat, and selection.
- The visible `Model` part for M01 infantry should be replaced or bypassed by ECS-owned animated sprite-atlas presentation.

Validation run:
- Reviewed active Gameplay task, M01 critical path, and current Gameplay handoff state.

Validation result:
- Accepted as PM routing.
- Gate 4 should not accept the temporary `MissionRuntimeSpriteRendererRuntime`/SpriteRenderer adapter as final public M01 infantry presentation.
- Gameplay should preserve useful ECS/prefab identity and replace the visible Model presentation path with ECS/DOTS-compatible animated atlas rendering.

Known gaps:
- Final or milestone-approved infantry atlas frames may need user art approval.
- Required review set should be minimal: player rifle squad and hostile patrol idle/move/attack/damaged/death or destroyed, with scale/grounding/contact shadow aligned to M01.
- Gameplay must prove the public M01 path no longer relies on the old visible `Model` path or temporary SpriteRenderer adapter for infantry presentation.

Cross-lane impacts:
- Gameplay remains active owner.
- QA/HCI remains waiting until Gameplay reports the presentation replacement and PM accepts it.
- UI and Support/FTUE remain waiting unless the Gameplay change exposes concrete lane-owned issues.
- User is available for art approval if Gameplay needs to present atlas variants.

Next recommended task:
Gameplay should update `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` or write a focused follow-up showing the old `Model` presentation replaced by ECS animated atlas presentation in the public M01 path, plus any art approval package needed.
