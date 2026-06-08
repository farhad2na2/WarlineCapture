Status: needs fixes
Topic: ECS-backed tactical terrain acceptance contract is underdefined
Docs reviewed:
- `Design/AgentTasks/pm_design-audit.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/Agent_Coordination_Workflow.md`
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/2D_Isometric_Production_Direction.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`

Finding:
The current Gameplay task correctly blocks standalone non-Canvas world GameObjects and requires the visible tactical terrain/map surface to be ECS-backed. However, the docs do not define the concrete ECS component/entity contract that makes a tactical ground SpriteRenderer or terrain presentation acceptable. There is no named component, entity id, owner system, validation assertion, or allowed presentation boundary for tactical terrain equivalent to the already established unit presenter/runtime contracts.

Why it matters:
Gameplay can satisfy the instruction in multiple incompatible ways: add a marker entity beside the existing SpriteRenderer, build a hybrid ECS-driven presentation component, move terrain fully into Entities Graphics, or just claim metadata backing without a strict runtime link. Without a named contract, QA/HCI and PM will keep rejecting or debating implementation proof, and agents may guess different ownership boundaries for terrain, minimap, blockers, camera bounds, and visual ground orientation.

Recommended fix:
Define the smallest explicit M01 terrain ECS presentation contract before the next Gameplay handoff is accepted. At minimum, the task/report should require:
- A named ECS component or tag that represents the visible tactical ground presentation owner.
- A stable terrain/map entity id tied to `iso.ch01.district_edge_01`.
- A runtime link proving the visible ground SpriteRenderer is an ECS-driven presentation object, not independent world state, or a direct ECS rendering alternative.
- Validation that the entity owns or references map id, sprite/art id, world bounds, camera bounds, orientation/up vector, minimap mapping, blocker metadata source, and ground renderer/presentation reference.
- A no-broad-lookup test path that resolves this terrain presentation through explicit bootstrap/binder/provider references or ECS queries, not hierarchy search.

Affected lanes:
Gameplay, QA/HCI, PM. UI is affected only as a consumer of final captures.

Needs user decision:
No. This is a technical contract clarification consistent with the user's existing rule that only Canvas UI may be non-ECS.

Next task update needed:
Yes. PM should update `Design/AgentTasks/gameplay_current.md` with the concrete terrain ECS presentation acceptance contract or ask Gameplay to propose the exact component/entity contract in its next handoff before implementing further broad changes.
