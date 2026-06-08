Status: needs fixes
Topic:
M01 command rejection reason codes are inconsistent across contracts
Docs reviewed:
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/Gameplay_UI_Integration_Handoff_Spec.md`
- `Design/AssistantRuntime_M01_Wiring_Plan.md`
- `Design/AssistantPanel_M01_Implementation_Contract.md`
Finding:
The M01 production contract defines reason codes such as `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, and `CameraJumpUnavailable`. The gameplay/UI integration handoff defines a different set: `InvalidTarget`, `BlockedRoute`, `OutOfRange`, `InsufficientResources`, `BuildModeUnavailable`, `AbilityOnCooldown`, and `TransportUnavailable`. The assistant runtime recovery table also references `BlockedRoute`, `InsufficientResources`, `AbilityOnCooldown`, and `TransportUnavailable`.
Why it matters:
Invalid-command recovery is part of the current Gate 4 route evidence. If gameplay emits one enum set while UI/ARIA/QA expect another, the invalid command toast, ARIA recovery recommendation, audio/VFX feedback, and QA assertions can drift or silently miss coverage. Agents would have to guess whether `TargetBlocked` equals `BlockedRoute`, whether `TargetOutOfBounds` equals `OutOfRange`, and whether `MissionDoesNotAllowBuild` equals `BuildModeUnavailable`.
Recommended fix:
Before accepting the final route-driven Gate 4 rerun, choose one canonical M01 reason-code enum and update the dependent docs/tasks. Recommended canonical M01 set from the production contract:
- `NoSelection`
- `TargetOutOfBounds`
- `TargetBlocked`
- `TargetUnreachable`
- `TargetNotEnemy`
- `TargetNotAttackable`
- `CommandUnavailable`
- `MissionDoesNotAllowBuild`
- `CameraJumpUnavailable`
Map or remove the handoff-only aliases (`InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`) and keep later-mission codes (`InsufficientResources`, `AbilityOnCooldown`, `TransportUnavailable`) out of M01 Gate 4 unless they are explicitly marked non-M01/future.
Affected lanes:
- Gameplay
- UI
- Support/FTUE
- QA/HCI
Needs user decision:
No if PM adopts the production-contract enum as canonical for M01. Yes only if the user wants different player-facing wording.
Next task update needed:
Yes. Assign a small contract cleanup before QA/HCI rerun, or require the UI route-driven tooling report to state exactly which command reason codes are captured and which canonical names QA should assert.
