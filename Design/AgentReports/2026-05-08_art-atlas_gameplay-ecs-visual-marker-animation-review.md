Lane:
Art/Atlas

Task:
Assess Gameplay's M01 ECS visual, marker, animation, and scale reset against the Art/Atlas selected-readability rejection package.

Files changed:
- `Design/AgentReports/2026-05-08_art-atlas_gameplay-ecs-visual-marker-animation-review.md`

Contracts touched:
- No source/runtime contract files changed by Art/Atlas.
- Art/Atlas confirms Gameplay's report implements the current Art/Atlas package direction from:
  - `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`
  - `Design/AgentReports/2026-05-08_pm_selected-readability-lane-handoffs-review.md`

User-visible behavior:
No runtime behavior changed by Art/Atlas in this pass. Gameplay reports that public M01 now uses ECS visual entities, individual soldier atlas slices, smaller `0.15` infantry scale, selected-state ring textures instead of yellow squares, and small move/attack command markers.

Validation run:
- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`.
- Reviewed `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`.
- Reviewed `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`.

Validation result:
accepted for Art/Atlas scope; still waiting on QA/HCI feedback regression gate.

Handoff assessment:
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`: accepted for Art/Atlas scope.
- Gameplay consumed the Art/Atlas marker assets:
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/selection_ring.png`
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/move_destination_ring.png`
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/attack_target_ring.png`
- Gameplay kept M01 infantry-only: player rifle squad and enemy patrol only.

Art/Atlas findings:
- Public unit visuals no longer rely on the rejected runtime `GameObject` atlas wrapper root. Gameplay reports ECS render entities with `MaterialMeshInfo`, `LocalToWorld`, and `MissionRuntimeEcsVisualTag`.
- The infantry visual scale is now `0.15`, matching the user-observed target direction from the rejection package.
- Player squad still resolves to individual `Unit_Chr_Soldier_Male_02` standing/run/aim/hit cells instead of the grouped `infantry_squad.png` source.
- Selected-state markers now use the Art/Atlas `selection_ring` texture with warm/amber tint and small per-soldier scale; this addresses the yellow placeholder square at the art-style level.
- Move/attack marker assets are now used as small world-space markers. QA/HCI still needs to validate that no huge green target marker appears in public capture.
- Alive enemy patrol state is reported as using alive soldier atlas states, with legacy destroyed/red artifact visuals suppressed. QA/HCI still needs to validate this in the user feedback matrix.

Known gaps:
- QA/HCI feedback regression gate is still missing.
- This Art/Atlas review did not run Unity or inspect fresh capture/video evidence.
- Final atlas art remains temporary; final multi-frame idle/run/walk loops are still missing.
- Final enemy red-accent patrol variant remains missing.
- Final impact VFX and final destroyed/death VFX remain missing.

Cross-lane impacts:
- QA/HCI can now validate Gameplay's implementation against the full user feedback matrix.
- PM should not request user review until QA/HCI closes or blocks every feedback row.
- Gameplay remains owner for any implementation fix QA/HCI finds.
- Art/Atlas has no further action unless QA/HCI or PM reports a concrete art/frame/marker asset issue.

Next recommended task:
QA/HCI should write `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`, including the selected-readability user feedback matrix and fresh evidence for ECS visuals, marker size, animation rows, `0.15` scale/aspect, enemy alive-state clarity, and selected hit affordance.

Waiting on lane:
QA/HCI

Waiting on exact file/report/asset/command:
- `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

Owner of next action:
QA/HCI

Can my lane still continue fallback work? no
