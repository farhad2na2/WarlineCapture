Status: accepted
Topic:
Migrate existing prefab Model/Destroyed structure to ECS atlas presentation

Lane:
PM

Task:
Capture the user's clarification that existing unit/building prefabs/configs remain valuable, but their visible child presentation structure must change.

Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_m01-prefab-model-destroyed-migration.md`

Contracts touched:
- No source contract changed. This clarifies how M01 must satisfy the existing runtime sprite/entity and destroyed-feedback requirements.

User-visible behavior:
- Existing prefabs/configs remain the authoring/data source for units and buildings.
- The old child `Model` presentation is not the public M01 runtime visual for infantry.
- The old child `Destroyed` dependency should be removed from M01 infantry runtime because destroyed/death is part of the same animated atlas state machine.

Validation run:
- PM inspected active tasks and source references.
- `UnitGridAuthoring` currently still looks for `Model` and `Destroyed`, and adds `UnitDestroyedVisualReference` when the child exists.
- `UnitModelSpawnSystem` still supports `UnitDestroyedVisualReference` and model-instance spawning.

Validation result:
- Accepted as PM routing.
- Gameplay must preserve useful prefab/config identity while replacing/bypassing visible `Model` and removing/bypassing separate `Destroyed` child usage for M01 infantry.
- A final Gate 4 pass must not depend on visible old `Model`, temporary SpriteRenderer adapter, or separate `Destroyed` child prefab/object for M01 infantry.

Known gaps:
- Gameplay must decide whether the migration is M01-scoped first or a broader unit/building presentation migration. PM recommendation: scope implementation to M01 infantry first, but avoid patterns that make the broader prefab/config migration harder.
- If art frames are missing, Gameplay should present the smallest atlas approval package to the user rather than continue with placeholder infrastructure.

Cross-lane impacts:
- Gameplay remains active owner.
- QA/HCI should verify no separate `Destroyed` child dependency remains for M01 infantry before final Gate 4.
- UI and Support/FTUE remain waiting unless the migration exposes concrete UI/assistant issues.

Next recommended task:
Gameplay should update or follow up `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` proving M01 infantry no longer uses old visible `Model`, temporary SpriteRenderer adapter, or separate `Destroyed` child runtime presentation.
