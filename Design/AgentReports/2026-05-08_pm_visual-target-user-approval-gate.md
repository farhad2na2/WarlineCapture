# PM Visual Target User Approval Gate

## Lane

PM

## Task

Make explicit that the M01 gameplay visual target package requires user approval before downstream lanes move forward.

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
- `Design/AgentReports/2026-05-08_pm_visual-target-user-approval-gate.md`

## Contracts touched

- Visual Target package is not accepted until PM/user approves it.
- Downstream Gameplay, Art/Atlas, UI, Designer, QA/HCI, and Support/FTUE must wait for the visual target report and user approval.
- Visual Target report must include short user review steps.

## User-visible behavior

The user will be asked to review the target visuals before agents use them as the final visual bar.

## Validation run

- Read `Design/AgentTasks/visual-target_current.md`.
- Read `Design/AgentTasks/visual-target_pm_message.md`.
- Read `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`.
- Updated waiting lanes to require PM/user approval after the Visual Target report lands.

## Validation result

Routed. User approval is now explicit.

## Known gaps

- The visual target package is still pending.

## Cross-lane impacts

- Visual Target owns the package.
- PM/user owns approval after the package lands.
- All implementation/validation lanes wait.

## Next recommended task

Visual Target should write:

`Design/AgentReports/2026-05-08_visual-target_m01-selected-readability-package.md`

The report must include target file paths and short user review steps.
