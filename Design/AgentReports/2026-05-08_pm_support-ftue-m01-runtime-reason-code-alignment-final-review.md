Status: accepted
Topic:
Support/FTUE M01 runtime reason-code alignment final review

Reviewed handoff:
`Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`

Finding:
Support/FTUE updated the previously blocked reason-code handoff with passing final Unity validation from `WarlineCapture-CodexUnity3`. The runtime/test cleanup now uses canonical M01 reason-code names, and the legacy aliases flagged by QA/HCI no longer appear in `Assets/Game/Scripts` or `Assets/Tests` except for the unrelated framework exception text `ArgumentOutOfRangeException` in `MenuView.cs`.

Acceptance decision:
Accepted for `QAHCI-G4-012` implementation handoff. QA/HCI can rerun or close the reason-code blocker against this evidence.

Validation accepted:
- Static scan for legacy aliases: pass.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: 7/7 passed.
- `WarlineCaptureUiMatchOverlayTests`: 18/18 passed.
- `CommandIntentExecutorTests`: 14/14 passed.
- `M01AssistantCommandRuntimeTests`: 10/10 passed.
- Test result XML files exist under `/private/tmp/` for all four final focused runs.

Remaining risk:
The cleanup maps several generic invalid-target cases to `TargetNotAttackable`. That is acceptable for this canonical-name closure, but Gameplay/UI may still refine semantics later if runtime context can distinguish `TargetOutOfBounds`, `TargetNotEnemy`, or `TargetUnreachable` more precisely.

Cross-lane impacts:
- QA/HCI can treat `QAHCI-G4-012` as ready for recheck/closure.
- Gameplay/UI still own the public M01 launch-path evidence gap.
- Art/design or the implementing lane still owns marker/VFX readiness and final art approval.

Needs user decision:
No.

Next recommended task:
QA/HCI should update the Gate 4 blocker table for reason-code status during its next affected rerun. Gameplay/UI should continue the active public launch-path task from `Design/AgentTasks/*_current.md`.
