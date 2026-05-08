Status: blocked pending Unity validation
Reviewed report:
- `Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`

Lane:
PM review

Summary:
Support/FTUE landed a report for `QAHCI-G4-012` runtime reason-code alignment. The report uses the standard WarlineCapture format and the scoped static scan supports the claim that the old legacy aliases are no longer present in `Assets/Game/Scripts` or `Assets/Tests`, aside from unrelated framework text such as `ArgumentOutOfRangeException`.

Acceptance decision:
Not accepted yet as blocker closure. The implementation remains in progress until focused Unity validation completes.

Validation accepted:
- Static legacy alias scan: passed for the legacy aliases flagged by QA/HCI.
- Diff hygiene: reported as passed.
- The changed code appears to move runtime/test usage toward canonical names such as `TargetBlocked`, `TargetNotAttackable`, `CommandUnavailable`, and `CameraJumpUnavailable`.

Validation still needed:
- Focused Unity EditMode validation must be rerun successfully for at least:
  - `WarlineCaptureUiAssistantRuntimeBindingTests`
  - `WarlineCaptureUiMatchOverlayTests`
  - `CommandIntentExecutorTests`
  - `M01AssistantCommandRuntimeTests`
- The report says Unity entered licensing reconnect/unsupported-protocol loops before tests started. That is a validation blocker, not a product acceptance pass.

Remaining risk:
Most previous generic invalid-target paths are now mapped to `TargetNotAttackable`. That is acceptable as a first canonical-name cleanup only if tests pass and QA/Gameplay do not require more granular runtime semantics for `TargetOutOfBounds`, `TargetNotEnemy`, or `TargetUnreachable`.

Cross-lane notices:
- QA/HCI should not mark `QAHCI-G4-012` closed yet.
- Gameplay/UI should review the canonical reason-code semantics after Unity validation, especially whether broad `TargetNotAttackable` emissions are sufficient for M01 invalid-command recovery.
- UI still owns `QAHCI-G4-011` safe-area profile closure.

Tracking updates:
No project-state or asset-register update yet.

Next task:
Support/FTUE or the owning implementation lane should rerun the focused Unity EditMode tests once Unity licensing is healthy, then update or replace the handoff report with the validation result.
