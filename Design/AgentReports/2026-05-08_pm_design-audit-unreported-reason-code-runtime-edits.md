Status: blocked
Topic:
Runtime reason-code edits exist without a handoff report

Evidence reviewed:
- `git status --short --branch`
- `git diff --stat`
- `Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs`
- `Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs`
- `Assets/Game/Scripts/Tutorial/Assistant/M01AssistantCommandRuntime.cs`
- `Assets/Game/Scripts/Tutorial/Recommendations/M01AssistantRecommendationProvider.cs`
- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Assets/Tests/Editor/Tutorial/CommandIntentExecutorTests.cs`
- `Assets/Tests/Editor/Tutorial/M01AssistantCommandRuntimeTests.cs`

Finding:
The workspace now contains unreported runtime and test edits that appear to align `TacticalCommandReasonCode` with the canonical M01 reason-code set. A scan of `Assets/Game/Scripts` and `Assets/Tests` no longer shows the legacy aliases from the QA blocker (`InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`, `InsufficientResources`, `AbilityOnCooldown`, `TransportUnavailable`) in the checked runtime/test code.

However, no agent handoff report has landed for this code change yet. PM cannot accept the fix or route QA/HCI to rerun the reason-code blocker until the owning lane reports files changed, contracts touched, validation run/result, known gaps, cross-lane impacts, and the next recommended task.

Why it matters:
This looks like a useful fix for `QAHCI-G4-012`, but it touches shared gameplay/UI/support behavior and tests. Accepting it without a report would hide the owner, validation proof, and any remaining mapping decisions.

Required handoff:
The owning lane should write a report, likely one of:

- `Design/AgentReports/2026-05-08_gameplay_m01-canonical-reason-code-runtime-alignment.md`
- `Design/AgentReports/2026-05-08_support-ftue_m01-canonical-reason-code-runtime-alignment.md`

The report must include:
- The final canonical enum set used by runtime.
- Confirmation that old aliases are removed or intentionally mapped.
- Tests run and pass/fail results.
- Whether UI/ARIA text now consumes canonical names end to end.
- Any cross-lane impacts for QA/HCI rerun.

Affected lanes:
- Gameplay
- Support/FTUE
- UI
- QA/HCI

Needs user decision:
No.

Next task update needed:
No task-file edit required yet. The owning agent should finish validation and land the handoff report before PM accepts this blocker closure.
