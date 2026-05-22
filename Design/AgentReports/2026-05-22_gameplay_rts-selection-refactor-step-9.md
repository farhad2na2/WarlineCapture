# WarlineCapture Handoff Report

## Lane

Gameplay

## Task

RTS selection refactor step 9: move transport pickup, approach, disembark, and rope-drop rules out of `RTSSelectionSystem` and into `UnitTransportBoardingSystem`.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/UnitTransportValidationTests.cs`
- `Assets/Tests/Editor/UnitTransportBoardingSystemExtractionTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay_rts-selection-refactor-step-9.md`

## Contracts touched

- Extended `GameplayArchitectureContractTests.RtsSelectionSystemMustDelegateTransportBoardingSlice` so pickup-cell, approach-cell, disembark-cell, and rope-disembark request setup cannot drift back into `RTSSelectionSystem`.
- Updated the RTS selection responsibility audit to mark the transport spatial/boarding extraction complete.

## User-visible behavior

No intended behavior change. Transport boarding, air pickup landing, ground/air boarding approach goals, focused transport disembark, and helicopter rope disembark should behave the same. The rules now live in `UnitTransportBoardingSystem`; `RTSSelectionSystem` remains the UI/input orchestration facade.

## Validation run

- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-step9-architecture.xml -logFile /private/tmp/warlinecapture-step9-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTransportBoardingSystemExtractionTests -testResults /private/tmp/warlinecapture-step9-transport-extraction.xml -logFile /private/tmp/warlinecapture-step9-transport-extraction.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTransportValidationTests -testResults /private/tmp/warlinecapture-step9-transport.xml -logFile /private/tmp/warlinecapture-step9-transport.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BattleHudGameplayBridgeConnectionTests -testResults /private/tmp/warlinecapture-step9-hudbridge.xml -logFile /private/tmp/warlinecapture-step9-hudbridge.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter MissileLauncherRadarAttackValidationTests -testResults /private/tmp/warlinecapture-step9-missile.xml -logFile /private/tmp/warlinecapture-step9-missile.log`

## Validation result

- Passed: `GameplayArchitectureContractTests` 35/35.
- Passed: `UnitTransportBoardingSystemExtractionTests` 4/4.
- Passed: `UnitTransportValidationTests` 16/16.
- Passed: `BattleHudGameplayBridgeConnectionTests` 6/6.
- Passed: `MissileLauncherRadarAttackValidationTests` 5/5.

## Known gaps

- `RTSSelectionSystem` still owns attack order orchestration, some clicked-target routing, HUD command feedback, and camera/focus behavior.
- `UnitTransportBoardingSystem` now owns both runtime boarding execution and boarding spatial rules; a later split can separate pure transport query/rule helpers if the system becomes too large.

## Cross-lane impacts

- QA should treat this as a behavior-preserving refactor of transport command ownership.
- Architecture lane can use the updated contract test to prevent new transport spatial rules from being added back to the selection facade.
- UI lane should continue routing transport orders through the gameplay facade rather than writing boarding/path components from views.

## Next recommended task

Continue the RTS selection split by moving remaining attack target validation and attack order writes into `UnitTargetOrderSystem`.
