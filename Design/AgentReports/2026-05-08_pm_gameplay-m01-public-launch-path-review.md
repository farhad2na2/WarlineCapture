Gate:
Gate 4 public M01 launch visible gameplay

Status:
needs fixes

Reviewed handoff:
`Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`

Reviewed captures:
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`

Reason:
The visual evidence is materially improved: authored-looking tactical terrain is visible, the HUD is present, the units are readable, and the old brown-field failure appears fixed in the reviewed captures. However, the handoff cannot be accepted under the current PM rules because the visible tactical world is not proven to be ECS-backed except for unit sprite presenters.

Validation accepted:
- Public launch captures now show HUD plus authored terrain instead of a flat brown/blank world.
- 16:9 captures exist at 1280x720.
- 20:9 captures exist at 1600x720.
- The handoff reports `Chapter01M01PlayModeValidationTests` passing 5/5 in `WarlineCapture-CodexUnity2`.
- Runtime legacy mesh suppression is implemented as an ECS system through `M01LegacyEcsRenderingSuppressionSystem`.
- Unit legacy model spawning is suppressed through ECS query exclusions using `MissionRuntimeSpritePresenterSuppressesLegacyModelTag`.

Validation still needed:
- Prove or fix ECS source-of-truth for every non-Canvas visible world object. The current diff still shows `TacticalMapRuntimeLoader` creating a visible `ground` GameObject/SpriteRenderer, which violates the new rule that only Canvas UI may be non-ECS GameObjects.
- Report whether terrain/map surfaces, markers, decor, and visible tactical objects are ECS-backed, not just command squad and hostile patrol sprites.
- Do not rely on standalone world GameObjects/SpriteRenderers as production tactical world proof. They may only be ECS-driven presentation objects for ECS entities.
- Include the 20:9 captures in the handoff `Files changed` and validation evidence. They exist on disk but are omitted from the gameplay report's files list.
- Resolve or explicitly justify the touched Unity test lookup usage: `Chapter01M01PlayModeValidationTests.cs` still contains `GetComponentInChildren` discovery in touched test code. The current workflow rejects new broad lookup patterns in tests/builders unless documented with an accepted blocker.
- Re-run validation from Gameplay's assigned workspace, `/Users/farhad/Projects/WarlineCapture-CodexUnity`, or report it blocked. The handoff's clean pass came from `WarlineCapture-CodexUnity2`, which is assigned to UI under the new workspace rule. If the Gameplay workspace exits with code 3, classify that as a validation blocker or get PM reassignment before using another lane's workspace.

Cross-lane notices:
- UI should not take ownership of terrain/map/world ECS conversion; that remains Gameplay.
- QA/HCI should not treat the current handoff as manual-ready until the ECS world-source rule is satisfied.
- The improved captures are useful visual progress, but they are not enough to override the ECS architecture requirement.

Needs user decision:
No. This is a Gameplay implementation/validation compliance issue.

Next gate/task:
Gameplay should revise the handoff to prove every visible non-Canvas world object in the M01 tactical slice is ECS-backed, or update the implementation so terrain/map/decor/markers are ECS source-of-truth with SpriteRenderer/GameObject visuals only as ECS-driven presentation. Then rerun focused validation in the assigned Gameplay workspace and include all 16:9/20:9 capture evidence in the report.
