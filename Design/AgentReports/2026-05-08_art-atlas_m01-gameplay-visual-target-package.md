# Art/Atlas M01 Gameplay Visual Target Package

## Lane

Art/Atlas

## Task

Create the user-reviewable M01 gameplay visual target package before final selected-readability approval.

## Files changed

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Scale_Board.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Selected_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Move_Attack_Marker_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Enemy_Readability_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Idle_Run_Pose_Guide.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Rejected_Bad_Examples.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Target_Manifest.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-target-package.md`

## Contracts touched

- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`

## User-visible behavior

No runtime behavior changed. This is a gameplay visual target/reference package for user approval and downstream capture comparison.

## Validation run

- Read the current Art/Atlas task, PM ownership handoff, user feedback gate, gameplay visual target README, and M01 contracts.
- Generated the seven target PNGs under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- Ran `identify Design/VisualTargets/Gameplay/M01_SelectedReadability/*.png`.
- Built and visually inspected a local contact sheet at `/private/tmp/m01_selected_readability_contact.png`.

## Validation result

Ready for PM/user approval.

The target package establishes:

- Full M01 gameplay target / paintover board.
- Soldier, road, building, and marker scale board.
- Selected-state marker target.
- Move/attack marker target.
- Enemy readability target.
- Idle/run pose contact sheet guidance.
- Rejected bad-example sheet naming huge green marker, yellow square, squashed soldier, crouch-run, red sitting artifact, and foot-only selection.

## QA acceptance checks

- Runtime captures should use ECS/atlas-backed unit presentation, not visible renderer-wrapper GameObjects.
- Infantry should read near the intended `0.15` scale and preserve sprite aspect ratio.
- Selected state should use small warm per-soldier grounded rings/brackets/contact marks; reject yellow square and foot-only selection.
- Move/attack markers should sit on the terrain and read around two soldier footsteps wide; reject huge green destination zones.
- Alive enemy patrol should use standing, walking, aiming, or firing silhouettes with restrained hostile tint; reject red sitting/death-row artifacts.
- Normal movement should sample valid walk/run rows only; reject crouch-run and adjacent-cell foot/top artifacts.

## User Review Steps

1. Open the seven PNG target files in `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
2. Check whether they set the intended visual bar for M01 selected-readability: unit scale, selected markers, move/attack markers, enemy readability, pose guide, and rejected examples.
3. Answer exactly `approve gameplay visual target package` or `reject gameplay visual target package with notes`.

## Known gaps

- The package is not accepted for downstream Gameplay/Art/QA use until PM/user explicitly approves it.
- This is a target/reference package, not runtime implementation or final gameplay capture evidence.

## Cross-lane impacts

- Gameplay, Designer, UI, QA/HCI, and Support/FTUE should remain blocked from final selected-readability approval until PM/user approves this visual target package.
- After approval, downstream lanes can compare runtime captures against these target boards.

## Next recommended task

PM/user should approve or reject the gameplay visual target package with notes.
