# M01 Selected Readability Gameplay Visual Target

## Ownership Boundary

This folder is for in-world gameplay visual targets only.

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

The first Visual Target handoff should produce or reference:

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
