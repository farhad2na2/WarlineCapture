# Art/Atlas Current Task

Date: 2026-05-08
Status: active
Priority: P0 rejected temporary Gate 4 art: metric scale/readability package

## Assignment

The user rejected the temporary Gate 4 art review. Art/Atlas must produce a focused scale/readability package before the project can ask for review again.

Read first:

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

Do not broaden to M02, vehicles, base/build mechanics, unrelated unit sets, or final Chapter 1 art beyond the M01 evidence needed here.

## Required Work

- Define corrected M01 visible scale targets for infantry and visible M01 buildings/decor.
- Use metric anchors from the user: soldier about `1.8m`, building door about `2.3m`, and road/context size to avoid tiny or oversized assets.
- Explicitly reject the reviewed tiny values as insufficient: soldier around `0.1505`, building around `0.14`.
- Provide an art-side recommendation for automated scale roles/values, including building scale closer to door/readability needs, not tiny decor scale.
- Specify selected-state art treatment: small under each soldier or equivalent subtle grounded treatment; no huge screen-covering marker; no unclear blue marker.
- Confirm whether the current infantry atlas has enough run frames to animate movement. If not, block clearly on replacement run frames.
- Confirm destroyed/death remains an atlas state, not a separate `Destroyed` child dependency.

## Runtime Constraints

Art/Atlas assets must support Gameplay's ECS atlas presentation path:

- no visible Unity `SpriteRenderer` unit presentation
- no `MissionRuntimeSpriteRendererRuntime` dependency
- no visible legacy `Model` child dependency
- no separate `Destroyed` child runtime dependency
- atlas-backed idle/move/run/attack/damaged/destroyed state coverage

## Waiting On

Waiting on lane:
none

Owner of next action:
Art/Atlas

Can my lane still continue fallback work? yes, only the required work above.

## Cross-Lane Notes

- Designer owns the concise metric scale/readability contract.
- Gameplay owns runtime scale consumption, ECS-only presentation proof, marker implementation, movement speed, and run animation.
- QA/HCI waits until Art/Atlas, Designer, and Gameplay reports are present.
- PM owns acceptance and commit/push.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task
