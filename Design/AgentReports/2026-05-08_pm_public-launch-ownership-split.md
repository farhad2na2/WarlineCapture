Status: advisory
Topic:
Split public M01 launch blocker ownership between UI canvas and Gameplay world

Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`

Finding:
The public M01 launch blocker had become a blended UI/GamePlay task, which allowed both lanes to work on the same visible failure without a crisp ownership split. The correct split is:

- UI owns the canvas over the world: route buttons, router state, HUD/objective/threat/assistant/command surfaces, safe-area layout, and full-screen player-facing capture composition.
- Gameplay owns the world under the HUD: mission runtime, tactical map loader output, authored terrain visibility, old-world suppression, unit/target world scale, camera bounds, and gameplay camera framing.

Why it matters:
The brown-field/tiny-world failure is not solved by UI making the HUD visible, and it is not solved by Gameplay hiding legacy roots while the full-screen player composition remains wrong. Public launch readiness requires both lanes' surfaces in the same evidence set.

Recommended fix:
Both agents should continue from their current task files with explicit boundaries:

- UI should not modify tactical world/map/camera runtime logic to fix brown-field gameplay. If the gameplay world is wrong, UI should report the Gameplay-owned blocker and keep its own canvas/capture composition ready.
- Gameplay should not modify HUD layout, assistant surfaces, safe-area layout, or UI capture chrome to fix the launch blocker. If the world is correct but HUD/capture composition is wrong, Gameplay should report the UI-owned blocker.
- Each handoff must state what that lane changed, what the other lane still owns, and whether the submitted evidence is a true full-screen player composition.

Affected lanes:
Gameplay, UI, QA/HCI, PM

Needs user decision:
No.

Next task update needed:
Done in `Design/AgentTasks/ui_current.md`, `Design/AgentTasks/gameplay_current.md`, and `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
