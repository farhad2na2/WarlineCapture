# Gameplay Current Task

Date: 2026-05-09
Status: active
Priority: P0 integrate the full M01 AI production art pack into ECS runtime and capture proof

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
- User clarified that Gameplay must implement all new M01 AI production art assets, not only the v2 soldier atlases.

Do not implement runtime visuals from review boards. Wait for runtime PNGs under:

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

These must include the strategic map, tactical map plates, marker PNGs, player/enemy soldier atlases, building atlases, and manifests.

Use these Art/Atlas reports only after PM/user acceptance:

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`

Required integration:

- Read the direct PM message before continuing:
  - `Design/AgentTasks/gameplay_pm_message.md`
- Integrate the full M01 AI production runtime asset pack:
  - strategic/background map,
  - all tactical map plates,
  - building/prop atlases and states,
  - marker atlas and marker sprites,
  - v2 player rifle squad and enemy patrol soldier atlases,
  - production manifests and per-asset metadata.
- Use `m01_ai_production_asset_manifest.json` for strategic/background, tactical maps, buildings/props, and markers. Use `m01_soldier_animation_manifest_v2.json` for soldiers.
- Replace old/pre-production M01 visuals where equivalent AI production assets exist.
- Integrate `m01_soldier_animation_manifest_v2.json` as the ECS sprite animation source for the M01 player rifle squad and enemy patrol.
- Use the v2 player/enemy atlas files and frame metadata; do not fall back to v1/static/rejected soldier frames.
- Keep player and enemy atlases separate.
- Use manifest fps, loop flags, facing ids, state ids, pivot/foot-anchor/contact metadata, and frame order.
- Preserve ECS atlas-backed runtime direction. Do not introduce SpriteRenderer/MeshRenderer unit presentation.
- Capture runtime proof showing the AI production background/map, buildings, markers, and idle/run soldier animation playback at the actual M01 camera scale. Include at least one selected player rifle squad view and one enemy patrol view if reachable.
- Report any visible scale, alpha speckle, edge bleed, sliding, wrong facing, or state-transition issues.

Do not start M02, vehicles, broad combat changes, or unrelated polish unless PM assigns the next concrete Gameplay task.

## Waiting On

Waiting on lane:
Gameplay

Owner of next action:
Gameplay

Can Gameplay continue fallback work? no. Integrate the full M01 AI production art pack and capture proof first.

## Required Validation

- Capture the M01 selected-readability runtime view from the public path or documented validation scene.
- Compare it against `M01_SelectedReadability_Gameplay_Target.png` and `M01_SelectedReadability_Isometric_Grid_Proof.png`.
- Compare it against the approved AI production asset pack.
- Include capture paths in the completion report.
- Explicitly state whether image/background/map/soldiers/markers match the approved reference style.

## Completion Report

Write:

`Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`

Use the standard WarlineCapture handoff format. Include code/config files changed, asset manifests used, validation commands, capture/video paths, and a clear recommendation for PM/user review.
