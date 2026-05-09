# Gameplay Current Task

Date: 2026-05-09
Status: active
Priority: P0 audit Art/Atlas v2 soldier atlas runtime suitability before acceptance

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
- Gameplay must not integrate v2 yet. First audit whether it is technically ready for runtime import.

Do not implement runtime visuals from review boards. Wait for runtime PNGs under:

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

These must include the strategic map, tactical map plates, marker PNGs, player/enemy soldier atlases, building atlases, and manifests.

Use these Art/Atlas reports only after PM/user acceptance:

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`

Do a focused Gameplay/runtime audit before acceptance. Check:

- whether all sprite paths and manifest entries exist and are usable,
- whether player and enemy atlases should remain separate or be split further by faction/state/facing for mobile memory, streaming, sprite rect stability, and ECS animator lookup,
- whether the `4096x1792` atlases are acceptable for mobile or need POT/padding/layout changes,
- whether all sprites appear at consistent scale across faction, facing, and state,
- whether pivots, foot anchors, transparent padding, contact bounds, and frame rects are appropriate for ECS atlas animation,
- whether atlas metadata is sufficient for runtime frame order, fps, loop flags, and state/facing lookup,
- whether the package avoids SpriteRenderer/MeshRenderer assumptions and fits the ECS atlas-backed runtime direction,
- exact recommendation before integration: accept, accept with import notes, needs Art fixes, needs manifest/layout fixes, or blocked.

Do not start M02, vehicles, broad combat changes, or unrelated polish unless PM assigns the next concrete Gameplay task.

## Waiting On

Waiting on lane:
Gameplay

Owner of next action:
Gameplay

Can Gameplay continue fallback work? no. Complete this audit only; do not integrate v2 until PM/user accepts the audit outcome.

## Required Validation

- Capture the M01 selected-readability runtime view from the public path or documented validation scene.
- Compare it against `M01_SelectedReadability_Gameplay_Target.png` and `M01_SelectedReadability_Isometric_Grid_Proof.png`.
- Compare it against the approved AI production asset pack.
- Include capture paths in the completion report.
- Explicitly state whether image/background/map/soldiers/markers match the approved reference style.

## Completion Report

Write:

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`

Use the standard WarlineCapture handoff format.
