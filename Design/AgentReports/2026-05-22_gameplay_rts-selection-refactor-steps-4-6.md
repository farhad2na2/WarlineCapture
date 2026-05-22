# RTS Selection Refactor Steps 4-6

Lane: Gameplay
Task: Continue the `RTSSelectionSystem` architecture refactor steps 4-6 by extracting move-order, transport-boarding, and target-order helper slices without changing shipped behavior.

Files changed:
- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs`
- `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs.meta`
- `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs`
- `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs`
- `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/UnitMoveOrderSystemTests.cs`
- `Assets/Tests/Editor/UnitMoveOrderSystemTests.cs.meta`
- `Assets/Tests/Editor/UnitTransportBoardingSystemExtractionTests.cs`
- `Assets/Tests/Editor/UnitTransportBoardingSystemExtractionTests.cs.meta`
- `Assets/Tests/Editor/UnitTargetOrderSystemTests.cs`
- `Assets/Tests/Editor/UnitTargetOrderSystemTests.cs.meta`
- `Assets/Tests/Editor/UnitTransportValidationTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`

Contracts touched: `GameplayArchitectureContractTests` now requires `RTSSelectionSystem` to delegate selection-state, move-order, transport-boarding, and target-order slices. The responsibility audit was updated with the completed second extraction.
User-visible behavior: intended no behavior change. `RTSSelectionSystem` remains the command facade, but no longer owns the extracted helper rules.
Validation run: Unity 6000.4.0f1 batchmode EditMode tests in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
Validation result: passed `GameplayArchitectureContractTests` 34/34, `UnitMoveOrderSystemTests` 2/2, `UnitTransportBoardingSystemExtractionTests` 2/2, `UnitTargetOrderSystemTests` 2/2, `MissileLauncherRadarAttackValidationTests` 5/5, `UnitTransportValidationTests` 16/16.
Known gaps: Remaining `RTSSelectionSystem` debt includes path request construction, transport pickup/disembark cell selection, attack order writes, UI read models, camera state, and static runtime-state reads.
Cross-lane impacts: none expected for Art/Design/UI. QA should see unchanged selection, movement, missile attack, and transport behavior.
Next recommended task: continue the refactor by moving remaining path request construction into `UnitMoveOrderSystem`, then transport pickup/disembark cell selection into `UnitTransportBoardingSystem`.
