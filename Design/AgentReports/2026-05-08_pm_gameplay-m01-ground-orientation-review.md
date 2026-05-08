Status: needs fixes
Topic: Gameplay M01 public launch ground orientation handoff review
Docs reviewed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-path-review.md`
- `Design/AgentReports/2026-05-08_pm_manual-test-m01-ground-upside-down.md`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Finding:
The updated Gameplay handoff is accepted for visual progress on the user-reported upside-down ground issue: the current 16:9 and 20:9 public-launch captures show readable authored terrain, road direction, HUD, squad, enemy patrol, and no obvious upside-down tactical plate. Gameplay also reports `Chapter01M01PlayModeValidationTests` 5/5 passing in the assigned Gameplay workspace.

The handoff is still not accepted for Gate 4/manual readiness. The active Gameplay task requires every non-Canvas visible world object to be ECS-backed. Current evidence still depends on `TacticalMapRuntimeLoader.GroundRenderer`, a visible standalone `SpriteRenderer` GameObject for the tactical ground. The handoff does not prove an ECS source-of-truth/entity backing for the visible terrain/map surface. The touched PlayMode validation also still contains broad child-component discovery (`GetComponentInChildren`) in `Chapter01M01PlayModeValidationTests.cs`, which violates the current no scene-wide/broad lookup policy for touched tests/builders.

Why it matters:
The orientation bug appears improved, but accepting this now would weaken the architecture decision the user explicitly set: only Canvas UI may be non-ECS GameObjects. If the tactical ground remains an independent world GameObject/SpriteRenderer, future map state, blockers, minimap, camera, unit anchors, and gameplay metadata can drift from what the player sees. The broad lookup in tests also lets validation pass through hierarchy discovery instead of explicit references/provider contracts, which is the failure pattern we are trying to remove.

Recommended fix:
Gameplay should keep the current visual orientation fix but revise the implementation/evidence so the visible tactical terrain/map surface has an ECS-backed source-of-truth. Acceptable evidence can be an ECS entity/component contract that owns the tactical ground presentation and proves the SpriteRenderer is only an ECS-driven visual presentation object, or a direct ECS rendering approach if that is the chosen architecture. The revised handoff must explicitly state how terrain/map surfaces, units, decor, markers, objectives, commands, health/destroyed state, camera/minimap bounds, and blockers are ECS-backed.

Gameplay should also remove or justify all new/touched broad lookup usage from `Chapter01M01PlayModeValidationTests.cs`, especially `GetComponentInChildren` helper discovery, and rerun the focused PlayMode validation from `/Users/farhad/Projects/WarlineCapture-CodexUnity` using escalated/out-of-sandbox batchmode if Unity licensing requires it.

Affected lanes:
Gameplay, QA/HCI, UI.

Needs user decision:
No.

Next task update needed:
Gameplay should continue the current task and not move on to M02-M05 or final art. QA/HCI should wait for the revised Gameplay handoff before marking public launch/manual HCI ready.
