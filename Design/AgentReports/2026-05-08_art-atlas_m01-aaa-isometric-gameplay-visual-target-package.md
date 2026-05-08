# Art/Atlas M01 AAA Isometric Gameplay Visual Target Package

## Lane

Art/Atlas

## Task

Replace the non-isometric AAA gameplay target package with a true-isometric AAA gameplay visual target package.

## Files changed

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_Isometric_AI_Source.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_AI_Source.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Scale_Board.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Selected_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Move_Attack_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Enemy_Readability_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Idle_Run_Pose_Guide.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Rejected_Bad_Examples.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Target_Manifest.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

## Contracts touched

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/AgentReports/2026-05-08_pm_art-atlas-non-isometric-rejection-routing.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

## User-visible behavior

No runtime behavior changed. This package replaces the non-isometric AAA target package with true-isometric gameplay visual targets while preserving the accepted high-quality direction.

## Validation run

- Read the active Art/Atlas task, PM non-isometric rejection routing, PM Art/Atlas message, user feedback gate, gameplay visual target README, and UI Visual Lock references.
- Used the built-in image generation workflow to create a new AAA isometric RTS gameplay mockup with orthographic camera intent, parallel ground-plane axes, and no cinematic perspective convergence.
- Copied the generated source into `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_Isometric_AI_Source.png`.
- Rebuilt the gameplay, selected-state, move/attack, enemy readability, scale/grounding, pose/contact, rejected-case, and isometric grid proof boards from the same source.
- Ran `identify Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_*.png`.
- Built and visually inspected `/private/tmp/m01_aaa_isometric_selected_readability_contact.png`.

## Validation result

Ready for PM/user approval.

The replacement directly addresses the latest rejection:

- Preserves the accepted AAA quality direction.
- Uses a stricter isometric RTS camera source with road, sidewalk, wall, and building edges reading as parallel ground-plane axes.
- Adds `M01_SelectedReadability_Isometric_Grid_Proof.png` to make the isometric check explicit.
- Keeps soldiers consistent in style, scale, lighting, grounding, and perspective.
- Keeps tactical markers ground-plane aligned: cyan selected contact, amber move destination, and red hostile markers.
- Keeps bad examples compact and includes non-isometric camera as a rejected case.

## QA acceptance checks

- Compare future runtime captures against `M01_SelectedReadability_Gameplay_Target.png` for overall quality and composition.
- Compare perspective against `M01_SelectedReadability_Isometric_Grid_Proof.png`: ground-plane axes should remain parallel with no vanishing point, horizon, or wide-angle convergence.
- Confirm soldiers stay consistent in scale, lighting, perspective, and foot contact.
- Confirm selected feedback uses subtle grounded cyan contact rings/brackets, not yellow squares or giant blobs.
- Confirm move feedback reads as an amber ground-plane destination cue and does not cover units.
- Confirm attack feedback reads as restrained red hostile ground-plane feedback, not floating or over-tinted marks.
- Confirm known rejected cases stay absent: non-isometric camera, huge green marker, yellow square, squashed soldier, half underground, red sitting artifact, and mixed-size squad.

## User Review Steps

1. Open the seven core review PNGs in `Design/VisualTargets/Gameplay/M01_SelectedReadability/`:
   - `M01_SelectedReadability_Gameplay_Target.png`
   - `M01_SelectedReadability_Isometric_Grid_Proof.png`
   - `M01_SelectedReadability_Scale_Board.png`
   - `M01_SelectedReadability_Selected_Marker_Target.png`
   - `M01_SelectedReadability_Move_Attack_Marker_Target.png`
   - `M01_SelectedReadability_Enemy_Readability_Target.png`
   - `M01_SelectedReadability_Idle_Run_Pose_Guide.png`
2. Check whether the quality still looks AAA and whether the camera reads as true isometric gameplay: parallel road/building axes, no vanishing point, no wide-angle/cinematic perspective.
3. Answer exactly `approve gameplay visual target package` or `reject gameplay visual target package with notes`.

## Known gaps

- This is still a target/reference package, not runtime implementation.
- Downstream lanes must not treat it as the accepted visual bar until PM/user explicitly approves it.

## Cross-lane impacts

- Gameplay, Designer, UI, QA/HCI, and Support/FTUE remain blocked from final selected-readability approval until PM/user approves this replacement.
- After approval, downstream runtime captures should be compared against this package.

## Next recommended task

PM/user should approve or reject the true-isometric gameplay visual target package with notes.
