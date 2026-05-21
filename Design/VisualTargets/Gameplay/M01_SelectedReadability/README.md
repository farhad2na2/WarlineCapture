# M01 Selected Readability Gameplay Visual Target

Status: legacy 2D/isometric gameplay reference. Do not use this folder as the active visual target for the current 3D single-map direction.

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

- `Design/VisualLockLayered/`

## UI Alignment References

Gameplay targets must visually align with the existing UI target language without becoming UI targets:

- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
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

## Non-Isometric Rejection

The AAA replacement quality direction was accepted by the user, but the package was rejected because it was not isometric.

Rejected report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

Next replacement report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

The next replacement must keep the AAA quality bar while using true isometric gameplay perspective: orthographic/isometric camera feel, consistent parallel ground-plane axes, no cinematic perspective convergence, no wide-angle camera look, and an isometric grid/axis proof or annotation.

## Approved Visual Quality Reference

The user approved the true-isometric package as the M01 gameplay reference quality bar.

Approved report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

Approved reference files:

- `M01_SelectedReadability_Gameplay_Target.png`
- `M01_SelectedReadability_Isometric_Grid_Proof.png`
- `M01_SelectedReadability_Scale_Board.png`
- `M01_SelectedReadability_Selected_Marker_Target.png`
- `M01_SelectedReadability_Move_Attack_Marker_Target.png`
- `M01_SelectedReadability_Enemy_Readability_Target.png`
- `M01_SelectedReadability_Idle_Run_Pose_Guide.png`

Approval note:

- The user likes this direction and wants it as reference quality. Image/background/map/soldiers/markers must all be in this style and quality.

Runtime captures must be compared against this package before visual approval.

## Gameplay VisualLock Follow-Up

Before runtime implementation proceeds, the approved visual direction must be expanded into a gameplay VisualLock package under:

- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/`

This gameplay VisualLock must cover:

- strategic map visual style,
- tactical isometric map/background style,
- road, sidewalk, building, and map tile treatment,
- player and enemy atlas style,
- idle/run/aim/fire/death/destroyed atlas states,
- selection, move, attack, enemy, objective, and hover markers,
- scale/grounding rules for soldiers, doors, roads, buildings, and markers.

Expected Art/Atlas report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`
