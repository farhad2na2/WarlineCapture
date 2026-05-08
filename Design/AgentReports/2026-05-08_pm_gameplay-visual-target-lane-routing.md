# PM Gameplay Visual Target Lane Routing

## Lane

PM

## Task

Route a new gameplay-only Visual Target package before final selected-readability approval, keeping gameplay references separate from UI target references while aligning style.

## Files changed

- `Design/AgentTasks/visual-target_current.md`
- `Design/AgentTasks/visual-target_pm_message.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-visual-target-lane-routing.md`

## Contracts touched

- Final selected-readability visual approval is paused until a gameplay visual target package exists.
- Gameplay visual targets must live under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- UI/HUD target references remain under `Design/VisualLock/` and `Design/VisualLockLayered/`.
- UI mockups may be referenced for alignment, but gameplay visual target files must not be mixed into UI target folders.

## User-visible behavior

No runtime behavior changed. This prevents future visual approval from depending only on repeated user feedback and runtime measurements.

## Validation run

- Inspected existing visual target structure under `Design/VisualLock/`, `Design/VisualLockLayered/`, and `Design/VisualReferences/`.
- Identified UI alignment references:
  - `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- Created gameplay-only target task and folder contract.

## Validation result

Routed. Visual Target is now the active owner before final selected-readability visual approval.

## Known gaps

- No new visual target PNGs/mockups are created yet.
- Existing selected-readability QA pass remains useful as a functional baseline, but it is not the final visual bar.

## Cross-lane impacts

- Visual Target owns the next report.
- Gameplay, Art/Atlas, Designer, UI, QA/HCI, and Support/FTUE wait for the gameplay visual target package.
- PM should not ask the user for final selected-readability approval until the target package is available.

## Next recommended task

Visual Target should write:

`Design/AgentReports/2026-05-08_visual-target_m01-selected-readability-package.md`
