# Gameplay Current Task

Date: 2026-05-09
Status: active
Priority: P0 make v2 soldier atlas import-ready before runtime integration

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
- PM decision: v2 art is not accepted for runtime integration yet. Gameplay owns import-readiness cleanup before any ECS runtime integration.

Do not implement runtime visuals from review boards. Wait for runtime PNGs under:

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

These must include the strategic map, tactical map plates, marker PNGs, player/enemy soldier atlases, building atlases, and manifests.

Use these Art/Atlas reports only after PM/user acceptance:

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`

Required cleanup before integration:

- Generate/source-control Unity `.meta` files for every v2 soldier runtime PNG and the v2 manifest files.
- Define explicit Unity importer settings for mobile: sprite/texture usage, alpha handling, compression, max size, mipmaps, filter mode, wrap mode clamp, and Android/iOS overrides.
- Add explicit per-frame or per-sequence pivot, foot anchor, contact bounds, and normalized sprite bounds metadata to `m01_soldier_animation_manifest_v2.json`.
- Preserve existing state/facing/frame order/fps/loop data.
- Keep player and enemy atlases separate for current M01 unless the cleanup proves a better split is required.
- Decide and document atlas layout policy: keep `4096x1792` with safe importer settings, repack to a padded/POT layout, or split by state/faction if required for mobile.
- Add gutter/extrusion or explicitly disable mipmap/bleeding risks; do not leave this implicit.
- Do not integrate v2 into live ECS gameplay until PM/user accepts the cleanup result.

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

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`

Use the standard WarlineCapture handoff format. Include the changed `.meta`, manifest, and atlas-layout files; the final importer policy; and whether any issue must go back to Art/Atlas.
