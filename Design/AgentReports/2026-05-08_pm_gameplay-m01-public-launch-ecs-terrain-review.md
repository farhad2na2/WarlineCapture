Status: accepted
Topic: Gameplay M01 public launch ECS terrain proof review
Docs reviewed:
- `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-ecs-terrain-contract-gap.md`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Finding:
The revised Gameplay handoff satisfies the current PM blocker for ECS-backed tactical terrain proof. The visible M01 ground presentation now has a named ECS source contract through `MissionRuntimeTerrainSurface` and `MissionRuntimeTerrainSurfaceRendererRuntime`, with a stable terrain runtime id derived from `terrain.iso.ch01.district_edge_01`, map id, mission id, world origin, visible world size, grid size, runtime plane, and orientation flag. The visible `Ground` SpriteRenderer is now explicitly linked as the ECS-driven presentation object for that terrain entity rather than accepted as independent world state.

Gameplay also removed the previously flagged broad child-component discovery from the touched M01 PlayMode validation path. The updated validation resolves the public route through router/provider references and verifies terrain backing through ECS queries and explicit loader references. Gameplay reports `Chapter01M01PlayModeValidationTests` 5/5 passing in `/Users/farhad/Projects/WarlineCapture-CodexUnity`, and the updated handoff reports a no-broad-lookup scan with no matches in the touched M01 test, tactical map loader, and terrain surface renderer system.

Why it matters:
This closes the PM-specific ECS terrain presentation proof blocker without forcing a larger Entities Graphics migration. It preserves the user rule that non-Canvas visible world presentation must be ECS-backed while allowing the current SpriteRenderer layer to remain a presentation object driven by ECS state.

Accepted scope:
- Public launch no longer appears to show the old 3D prototype, flat brown/tiny-world field, or upside-down map in the provided evidence.
- Tactical terrain is acceptable as a hybrid ECS-backed SpriteRenderer presentation for this M01 slice.
- Touched M01 validation no longer depends on broad child-component discovery.
- Gameplay used the assigned Gameplay workspace for focused validation.

Known gaps:
- This does not mark review art or AI-generated tactical/unit assets final-approved.
- This does not close real-device touch/camera ergonomics, marker/VFX readiness, final 1920x1080/2400x1080 eight-state QA matrix, or shutdown leak-warning classification.
- `WarlineCaptureGameLaunchUtility` and some unrelated UI screens still contain pre-existing broad lookup usage; those remain cleanup items but are not blockers for this handoff because they were not introduced by the Gameplay terrain proof.
- UI still owns final HUD/canvas composition if QA/HCI finds a UI-specific presentation issue.

Cross-lane impacts:
Gameplay can stop iterating on the current ECS terrain proof unless QA/HCI finds a new gameplay-owned regression. QA/HCI should rerun the affected public-launch/Gate 4 checks from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` using escalated/out-of-sandbox Unity batchmode if needed. UI should remain waiting unless QA/HCI identifies a HUD/canvas/capture-composition blocker.

Needs user decision:
No.

Next task update needed:
QA/HCI should validate the accepted Gameplay handoff. After QA/HCI reports, PM should refresh `gameplay_current.md` to the next Chapter 1 implementation task if no new gameplay blocker appears.
