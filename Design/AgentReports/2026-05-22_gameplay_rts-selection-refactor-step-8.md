# WarlineCapture Handoff Report

## Lane

Gameplay

## Task

RTS selection refactor step 8: move remaining movement/path command construction out of `RTSSelectionSystem` and into `UnitMoveOrderSystem`.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs`
- `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/UnitMoveOrderSystemTests.cs`
- `Assets/Tests/Editor/UnitMoveOrderSystemTests.cs.meta`
- `Assets/Tests/Editor/UnitTransportValidationTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay_rts-selection-refactor-step-8.md`

## Contracts touched

- Gameplay SOLID/ECS architecture contract enforcement via `GameplayArchitectureContractTests.RtsSelectionSystemMustDelegateMoveOrderSlice`.
- RTS selection responsibility audit updated to mark move/path command construction as extracted.
- Transport validation reflection updated because air pickup preparation is now an instance helper instead of a static helper.

## User-visible behavior

No intended behavior change. RTS selection still issues group move, immediate move, transport boarding, air pickup, and disembark commands through the same UI flows. The ownership of movement command component writes moved from the facade into `UnitMoveOrderSystem`.

## Validation run

- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-step8-architecture.xml -logFile /private/tmp/warlinecapture-step8-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitMoveOrderSystemTests -testResults /private/tmp/warlinecapture-step8-moveorder.xml -logFile /private/tmp/warlinecapture-step8-moveorder.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BattleHudGameplayBridgeConnectionTests -testResults /private/tmp/warlinecapture-step8-hudbridge.xml -logFile /private/tmp/warlinecapture-step8-hudbridge.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTransportValidationTests -testResults /private/tmp/warlinecapture-step8-transport.xml -logFile /private/tmp/warlinecapture-step8-transport.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter MissileLauncherRadarAttackValidationTests -testResults /private/tmp/warlinecapture-step8-missile.xml -logFile /private/tmp/warlinecapture-step8-missile.log`

## Validation result

- Passed: `GameplayArchitectureContractTests` 35/35.
- Passed: `UnitMoveOrderSystemTests` 5/5.
- Passed: `BattleHudGameplayBridgeConnectionTests` 6/6.
- Passed: `UnitTransportValidationTests` 16/16 after updating the test reflection to the new instance helper.
- Passed: `MissileLauncherRadarAttackValidationTests` 5/5.

## Known gaps

- `RTSSelectionSystem` is still large and still owns transport pickup/disembark cell selection, attack order orchestration, and camera control state.
- `UnitMoveOrderSystem` is still a plain composed system object rather than an ECS `ISystem`; this preserves the current facade wiring while reducing responsibility drift.

## Cross-lane impacts

- QA should treat this as a refactor with no intended gameplay behavior change.
- UI lane should not call movement component writes directly from views; route through the gameplay facade or extracted systems.
- Architecture lane can use the updated contract test as the guard against reintroducing move-command writes into `RTSSelectionSystem`.

## Next recommended task

Continue the RTS selection split by extracting remaining transport pickup, approach, disembark, and rope-drop cell selection into `UnitTransportBoardingSystem`.
