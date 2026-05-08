# M01 Selected Readability Gameplay Visual Target

## Ownership Boundary

This folder is for in-world gameplay visual targets only. Art/Atlas owns the target package in this folder.

Use this folder for:

- soldier/unit scale and silhouettes,
- building/road/door scale references,
- in-world selected-state markers,
- in-world move/attack target markers,
- enemy readability,
- animation pose/contact-sheet guidance,
- bad-example sheets for rejected runtime visuals.

Do not use this folder for:

- HUD panels,
- command rail layout,
- squad cards,
- buttons,
- menus,
- screen-space popups,
- UI layer exports.

UI/HUD visual targets remain in:

- `Design/VisualLock/`
- `Design/VisualLockLayered/`

## UI Alignment References

Gameplay targets must visually align with the existing UI target language without becoming UI targets:

- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Target_State_Manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`

## Required Package

The first Art/Atlas visual target handoff should produce or reference:

- `M01_SelectedReadability_Gameplay_Target.png`
- `M01_SelectedReadability_Scale_Board.png`
- `M01_SelectedReadability_Selected_Marker_Target.png`
- `M01_SelectedReadability_Move_Attack_Marker_Target.png`
- `M01_SelectedReadability_Enemy_Readability_Target.png`
- `M01_SelectedReadability_Idle_Run_Pose_Guide.png`
- `M01_SelectedReadability_Rejected_Bad_Examples.png`
- `M01_SelectedReadability_Target_Manifest.md`

## Acceptance Rule

Gameplay, Art/Atlas, and QA/HCI cannot claim final selected-readability visual acceptance until their captures are compared against this gameplay target package.

The package itself also requires explicit PM/user approval. Until the user approves the gameplay visual target package, downstream lanes must not treat it as the accepted visual bar.

Every target package report must include a short `User Review Steps` section naming the exact files to open and the exact approve/reject decision needed.

## Rejected Package

The first package was rejected by the user for low quality, inconsistent soldier scale, poor grounding, placeholder assets, and not matching the AAA AI-generated quality bar of the UI Visual Lock targets.

Rejected report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-target-package.md`

Replacement report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

The replacement must be a polished AAA gameplay mockup target, not a placeholder collage or board with inconsistent pasted assets.
