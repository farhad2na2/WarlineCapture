Gate:
M01 Reason-Code Contract Cleanup
Status:
needs fixes
Reason:
The documentation cleanup correctly makes the M01 design contracts agree on the production-contract reason-code names, but it does not yet make the runnable project agree. `BattleHudGameplayBridge.TacticalCommandReasonCode`, gameplay/UI runtime call sites, and focused tests still use legacy aliases such as `InvalidTarget`, `BlockedRoute`, `OutOfRange`, and `BuildModeUnavailable`.
Validation accepted:
- `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md` now names the M01 production-contract reason codes.
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md` now uses the same canonical M01 recovery table.
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` now states the earlier aliases are deprecated for M01 Gate 4 assertions.
Validation still needed:
- Update or map `TacticalCommandReasonCode` in runtime code so the canonical M01 values exist and display the production-contract player-facing strings.
- Update M01 assistant/runtime/gameplay call sites and tests that still emit/assert `InvalidTarget`, `BlockedRoute`, `OutOfRange`, or `BuildModeUnavailable`.
- Rerun focused assistant/runtime/match overlay validation after code changes.
Cross-lane notices:
- Gameplay/UI own the runtime enum and command-result emission/mapping.
- Support/FTUE owns assistant recovery semantics only after the runtime enum names are available.
- QA/HCI should not assert canonical reason codes in the final Gate 4 rerun until the runtime enum/code cleanup lands or an explicit mapping layer is documented.
Next gate/task:
Assign a focused Gameplay/UI contract-code cleanup before final QA/HCI rerun: canonicalize or map `TacticalCommandReasonCode` to `NoSelection`, `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, and `CameraJumpUnavailable`.
