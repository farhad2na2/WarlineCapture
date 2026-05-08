# Art/Atlas M01 AAA Gameplay Visual Target Package

## Lane

Art/Atlas

## Task

Replace the rejected M01 gameplay visual target package with an AAA-quality AI-assisted gameplay mockup target package.

## Files changed

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_AI_Source.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Scale_Board.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Selected_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Move_Attack_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Enemy_Readability_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Idle_Run_Pose_Guide.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Rejected_Bad_Examples.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Target_Manifest.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

## Contracts touched

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-visual-target-rejected.md`

## User-visible behavior

No runtime behavior changed. The target package now uses a polished AI-generated isometric gameplay mockup as the visual bar instead of the rejected placeholder-style collage package.

## Validation run

- Read the active Art/Atlas task, PM rejection report, PM Art/Atlas message, user feedback gate, gameplay visual target README, and UI Visual Lock references.
- Used the built-in image generation workflow to create a new AAA-quality in-world M01 gameplay mockup.
- Copied the generated source into `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_AI_Source.png`.
- Derived the selected-state, move/attack, enemy readability, scale/grounding, pose/contact, and rejected-case targets from the same source mockup to keep style, scale, lighting, and perspective consistent.
- Ran `identify Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_*.png`.
- Built and visually inspected `/private/tmp/m01_aaa_selected_readability_contact.png`.

## Validation result

Ready for PM/user approval.

The replacement directly addresses the rejection:

- Visual quality now matches an AAA RTS target style rather than placeholder boards.
- Soldiers come from one coherent generated scene with consistent proportions, lighting, and perspective.
- Grounding is shown through pavement contact, selection rings, and scale/grounding guides.
- The package avoids half-buried, floating, squashed, cut-off, and mixed-size soldiers in accepted targets.
- Rejected bad examples are compact and do not dominate the package.

## QA acceptance checks

- Compare future runtime captures against `M01_SelectedReadability_Gameplay_Target.png` for the overall in-world polish bar.
- Confirm soldiers remain consistent in scale, lighting, perspective, and foot contact.
- Confirm selected-state feedback uses subtle grounded cyan contact rings/brackets, not yellow squares or giant blobs.
- Confirm move feedback reads as an amber ground-plane destination cue and does not cover units.
- Confirm attack feedback reads as restrained red hostile ground-plane feedback, not floating or over-tinted marks.
- Confirm scale/grounding against road lanes, sidewalks, and buildings.
- Confirm known rejected cases stay absent: huge green marker, yellow square, squashed soldier, half underground, red sitting artifact, and mixed-size squad.

## User Review Steps

1. Open the seven review PNGs in `Design/VisualTargets/Gameplay/M01_SelectedReadability/`:
   - `M01_SelectedReadability_Gameplay_Target.png`
   - `M01_SelectedReadability_Scale_Board.png`
   - `M01_SelectedReadability_Selected_Marker_Target.png`
   - `M01_SelectedReadability_Move_Attack_Marker_Target.png`
   - `M01_SelectedReadability_Enemy_Readability_Target.png`
   - `M01_SelectedReadability_Idle_Run_Pose_Guide.png`
   - `M01_SelectedReadability_Rejected_Bad_Examples.png`
2. Check whether this replacement reaches the intended AAA gameplay target quality and fixes the prior rejection issues.
3. Answer exactly `approve gameplay visual target package` or `reject gameplay visual target package with notes`.

## Known gaps

- This is still a target/reference package, not runtime implementation.
- Downstream lanes must not treat it as the accepted visual bar until PM/user explicitly approves it.

## Cross-lane impacts

- Gameplay, Designer, UI, QA/HCI, and Support/FTUE remain blocked from final selected-readability approval until PM/user approves this replacement.
- After approval, downstream runtime captures should be compared against this package.

## Next recommended task

PM/user should approve or reject the replacement gameplay visual target package with notes.
