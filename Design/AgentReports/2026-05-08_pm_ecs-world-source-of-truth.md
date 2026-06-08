Status: advisory
Topic:
M01 tactical world must remain ECS source-of-truth, not standalone SpriteRenderer gameplay

Docs updated:
- `Design/AgentTasks/gameplay_current.md`
- `Design/Agent_Coordination_Workflow.md`

Decision:
Gameplay must preserve WarlineCapture as an ECS-first tactical project. Only Canvas UI is allowed to be non-ECS GameObjects. The M01 world under the HUD must be driven by ECS entities/components, authored tactical metadata, and mission runtime systems. `SpriteRenderer` and GameObject presentation objects are allowed only as ECS-driven visual objects for ECS entities.

Required rule:
- Do not implement M01 terrain/map surfaces, gameplay units, decor, markers, objectives, command state, health, damaged/destroyed state, or result readiness as standalone GameObjects or screenshot-only SpriteRenderers.
- Every non-Canvas visible world object must trace back to ECS source-of-truth data such as `MissionRuntimeEntityId`, `MissionRuntimeSpritePresenter`, `LocalTransform`, `UnitGrid`, `UnitHealth`, selection/command components, and tactical metadata.
- If a visible non-Canvas world sprite/object exists without a corresponding ECS entity/source-of-truth, the handoff is blocked even if the screenshot looks better.
- UI may consume/capture the rendered result, but UI must not create gameplay-world stand-ins to satisfy visual proof.

Affected lanes:
Gameplay, UI, QA/HCI, PM

Needs user decision:
No.

Next task update needed:
Done.
