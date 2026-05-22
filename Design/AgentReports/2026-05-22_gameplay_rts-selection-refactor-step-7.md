# RTS Selection Refactor Step 7

Lane: Gameplay
Task: Continue the `RTSSelectionSystem` architecture refactor by extracting selected/focused UI read-model logic into `SelectionUiQuerySystem` while preserving the existing public facade API.

Files changed:
- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/SelectionUiQuerySystem.cs`
- `Assets/Game/Scripts/Systems/SelectionUiQuerySystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/SelectionUiQuerySystemTests.cs`
- `Assets/Tests/Editor/SelectionUiQuerySystemTests.cs.meta`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`

Contracts touched: `GameplayArchitectureContractTests` now requires `RTSSelectionSystem` to delegate focused and selected UI read models to `SelectionUiQuerySystem`.
User-visible behavior: intended no behavior change. Existing UI callers still use `RTSSelectionSystem`, but label, description, health/capacity, passenger list, portrait pose/framing, and HUD selection status are now computed by `SelectionUiQuerySystem`.
Validation run: Unity 6000.4.0f1 batchmode EditMode tests in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
Validation result: passed `GameplayArchitectureContractTests` 35/35, `SelectionUiQuerySystemTests` 4/4, `BattleHudGameplayBridgeConnectionTests` 6/6, `UnitTransportValidationTests` 16/16, `MissileLauncherRadarAttackValidationTests` 5/5.
Known gaps: Remaining `RTSSelectionSystem` debt includes path request construction, transport pickup/disembark cell selection, attack order writes, camera state, and static runtime-state reads.
Cross-lane impacts: UI should see unchanged focused/selected unit display behavior. QA can use the same HUD, transport, and missile attack regression paths.
Next recommended task: move remaining path request construction from `RTSSelectionSystem` into `UnitMoveOrderSystem`.
