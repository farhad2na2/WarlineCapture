# Art/Atlas Current Task

Date: 2026-05-08
Status: active
Priority: P0 rejected gameplay visual target package; create AAA Visual Lock quality AI-generated M01 gameplay mockup targets

## Assignment

The user rejected the M01 gameplay visual target package as low quality and inconsistent.

Rejected package:

- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-target-package.md`

Create a new AAA-quality gameplay visual target package that matches the production level of the existing UI Visual Lock targets.

Read first:

- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-visual-target-rejected.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- UI Visual Lock quality references:
  - `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`

## Required Behavior

- Produce high-quality AI-generated or AI-assisted gameplay target mockups, not placeholder collages.
- The target must look like an AAA production visual target, comparable in polish to the UI Visual Lock targets.
- All soldiers must be consistent in style, proportions, lighting, scale, grounding, and perspective.
- No soldier may be half buried, floating, squashed, cut off, or different-size without a clear perspective reason.
- The target must show an in-world M01 selected-readability gameplay scene, not a UI board pretending to be gameplay.
- Keep gameplay target files under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- Do not put gameplay target files under `Design/VisualLock/` or `Design/VisualLockLayered/`.
- Use UI Visual Lock references only for quality/style alignment.
- Include:
  - polished full gameplay visual target,
  - polished selected-state target,
  - polished move/attack marker target,
  - polished enemy readability target,
  - scale/grounding target using believable road/building/soldier relationships,
  - pose/animation target or high-quality contact guidance,
  - rejected bad-example sheet if useful, but do not let bad examples dominate the package.
- Include short user review steps.
- User approval is required before downstream lanes move forward.

## Waiting On

Waiting on lane:
none

Owner of next action:
Art/Atlas

Can my lane still continue fallback work? no. Replace the rejected package first.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

Use the standard WarlineCapture handoff format and include all target file paths, plus a short `User Review Steps` section.
