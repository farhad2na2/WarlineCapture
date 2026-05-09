# Gameplay Current Task

Date: 2026-05-09
Status: active
Priority: P0 integrate v2 soldier atlas into M01 ECS runtime and capture proof

## Assignment

The user approved the true-isometric AAA gameplay visual target package as the reference quality bar.

Approved reference package:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

Approved target files:

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Selected_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Move_Attack_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Enemy_Readability_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Scale_Board.png`

The Gameplay VisualLock board package is rejected as insufficient for implementation. Before Gameplay resumes, Art/Atlas must deliver the AI-generated ready-to-implement production asset pack:

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`

PM/user review status:

- Art/Atlas handoff is not accepted for Gameplay yet.
- The regenerated strategic map is approved.
- The animated soldier sprites from `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix.md` are rejected.
- Blocking issue: the run sequence appears to repeat the same pose, and the user says this may be true for all sequences.
- Art/Atlas has delivered v2 in `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`.
- Designer audit accepted v2 visually with minor notes in `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`.
- Gameplay audit found v2 is not yet ready for direct runtime import in `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`.
- Gameplay completed import-readiness cleanup in `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`.
- PM accepted the import-readiness cleanup for runtime integration. This is not final visual approval; final approval still requires runtime capture/video review.

Do not implement runtime visuals from review boards. Wait for runtime PNGs under:

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

These must include the strategic map, tactical map plates, marker PNGs, player/enemy soldier atlases, building atlases, and manifests.

Use these Art/Atlas reports only after PM/user acceptance:

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`

Required integration:

- Integrate `m01_soldier_animation_manifest_v2.json` as the ECS sprite animation source for the M01 player rifle squad and enemy patrol.
- Use the v2 player/enemy atlas files and frame metadata; do not fall back to v1/static/rejected soldier frames.
- Keep player and enemy atlases separate.
- Use manifest fps, loop flags, facing ids, state ids, pivot/foot-anchor/contact metadata, and frame order.
- Preserve ECS atlas-backed runtime direction. Do not introduce SpriteRenderer/MeshRenderer unit presentation.
- Capture runtime proof showing idle and run animation playback at the actual M01 camera scale. Include at least one selected player rifle squad view and one enemy patrol view if reachable.
- Report any visible scale, alpha speckle, edge bleed, sliding, wrong facing, or state-transition issues.

Do not start M02, vehicles, broad combat changes, or unrelated polish unless PM assigns the next concrete Gameplay task.

## Waiting On

Waiting on lane:
Gameplay

Owner of next action:
Gameplay

Can Gameplay continue fallback work? no. Integrate v2 soldier animation runtime and capture proof first.

## Required Validation

- Capture the M01 selected-readability runtime view from the public path or documented validation scene.
- Compare it against `M01_SelectedReadability_Gameplay_Target.png` and `M01_SelectedReadability_Isometric_Grid_Proof.png`.
- Compare it against the approved AI production asset pack.
- Include capture paths in the completion report.
- Explicitly state whether image/background/map/soldiers/markers match the approved reference style.

## Completion Report

Write:

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-runtime-integration.md`

Use the standard WarlineCapture handoff format. Include code/config files changed, validation commands, capture/video paths, and a clear recommendation for PM/user review.
