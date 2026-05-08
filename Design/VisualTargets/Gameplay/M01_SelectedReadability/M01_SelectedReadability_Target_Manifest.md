# M01 Selected Readability Gameplay Visual Target Manifest

Date: 2026-05-08
Owner: Art/Atlas
Approval: Approved by user as M01 gameplay reference quality bar.

## Target Files

- `M01_SelectedReadability_AAA_Isometric_AI_Source.png` - resized source frame from the AI-generated true-isometric gameplay mockup.
- `M01_SelectedReadability_AAA_AI_Source.png` - current source alias for package consumers.
- `M01_SelectedReadability_Gameplay_Target.png` - polished in-world M01 true-isometric gameplay target.
- `M01_SelectedReadability_Isometric_Grid_Proof.png` - isometric grid/axis proof showing parallel ground-plane axes.
- `M01_SelectedReadability_Scale_Board.png` - scale and grounding target using believable road/building/soldier relationships.
- `M01_SelectedReadability_Selected_Marker_Target.png` - selected-state target with consistent soldiers and cyan grounded contact rings.
- `M01_SelectedReadability_Move_Attack_Marker_Target.png` - move and attack marker target using amber and red ground-plane tactical feedback.
- `M01_SelectedReadability_Enemy_Readability_Target.png` - enemy readability target with restrained hostile markers and consistent lighting.
- `M01_SelectedReadability_Idle_Run_Pose_Guide.png` - high-quality pose/contact guidance from the same true-isometric gameplay mockup.
- `M01_SelectedReadability_Rejected_Bad_Examples.png` - compact rejected-case sheet for known failure modes, including non-isometric camera rejection.

## Source References Used

- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`
- Built-in image generation output copied into this package as `M01_SelectedReadability_AAA_Isometric_AI_Source.png`.
- Prompt intent: AAA true-isometric RTS M01 selected-readability gameplay mockup with orthographic/isometric camera feel, parallel 2:1 ground-plane axes, consistent soldiers, grounded selection rings, amber move marker, red hostile markers, no HUD, no text, no yellow square, no giant green marker, no buried/floating/squashed soldiers, and no cinematic perspective convergence.

## Acceptance Checks

- This package is the approved visual quality reference for image/background/map/soldiers/markers.
- Runtime capture should align to the target boards before another selected-readability approval request.
- Unit visuals must be ECS/atlas-backed and not accepted through visible renderer-wrapper GameObjects.
- Target/capture perspective should read as orthographic isometric: consistent parallel ground-plane axes, no vanishing point, no horizon, and no wide-angle cinematic convergence.
- Infantry scale should stay consistent across the squad and read correctly against road lanes, sidewalks, and buildings.
- Selected state should use subtle grounded cyan contact rings/brackets; reject yellow squares, half-buried feet, and giant selection blobs.
- Move marker should be an amber ground-plane destination cue that does not cover units or terrain context.
- Attack marker should use restrained red hostile ground-plane feedback around enemies, not over-tinting or floating marks.
- Enemy readability should preserve standing silhouettes, consistent lighting, and believable perspective.
- Bad-example cases remain rejected: non-isometric camera, huge green marker, yellow square, squashed soldier, half underground, red sitting artifact, and mixed-size squad.
