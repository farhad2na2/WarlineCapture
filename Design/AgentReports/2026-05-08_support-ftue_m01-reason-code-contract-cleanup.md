Lane: Support/FTUE
Task: Aligned M01 invalid-command and assistant recovery documentation to the canonical production-contract reason-code enum.
Files changed:
- `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/AgentReports/2026-05-08_support-ftue_m01-reason-code-contract-cleanup.md`
Contracts touched:
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-command-reason-code-mismatch.md`
User-visible behavior:
- No runtime behavior changed.
- M01 invalid-command recovery docs now consistently use the production-contract reason codes for UI, ARIA, and QA Gate 4 assertions.
Validation run:
- Documentation grep for stale aliases and canonical reason-code references:
  - `rg -n "InvalidTarget|BlockedRoute|OutOfRange|InsufficientResources|BuildModeUnavailable|AbilityOnCooldown|TransportUnavailable" Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
  - `rg -n "TargetOutOfBounds|TargetBlocked|TargetUnreachable|TargetNotEnemy|TargetNotAttackable|CommandUnavailable|MissionDoesNotAllowBuild|CameraJumpUnavailable|NoSelection" Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
Validation result:
- The active M01 assistant/handoff contracts now list canonical reason codes: `NoSelection`, `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, and `CameraJumpUnavailable`.
- Earlier aliases remain only in explicit deprecation/out-of-scope notes and are no longer the active M01 recovery table.
- No Unity validation was required because only design contracts changed.
Known gaps:
- Runtime enum/code may still need implementation verification if gameplay or UI source still emits legacy aliases.
- QA/HCI route-driven rerun still has not landed.
Cross-lane impacts:
- UI and QA/HCI should assert canonical M01 reason codes in Gate 4 route-driven capture/safe-area evidence.
- Gameplay should emit or map to canonical M01 reason codes for invalid-command feedback.
- Support/FTUE remains waiting unless UI or QA/HCI reports a concrete assistant behavior/API issue.
Next recommended task:
- UI should continue route-driven capture/safe-area tooling and include canonical M01 reason-code evidence in the handoff.
