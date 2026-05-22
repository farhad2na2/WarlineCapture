# WarlineCapture Handoff Report

## Lane

Gameplay

## Task

RTS selection refactor step 10: move attack target validation and attack order component writes out of `RTSSelectionSystem` and into `UnitTargetOrderSystem`.

## Files changed

- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/UnitTargetOrderSystemTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay_rts-selection-refactor-step-10.md`

## Contracts touched

- Extended `GameplayArchitectureContractTests.RtsSelectionSystemMustDelegateTargetOrderSlice` so attack validation and `EngageTarget` construction cannot drift back into `RTSSelectionSystem`.
- Updated the RTS selection responsibility audit to mark target-order writes as extracted.

## User-visible behavior

No intended behavior change. Explicit attack target mode, world-click attack orders, missile launcher radar attack, base-breach attack routing, and auto-attack reset should behave the same. `RTSSelectionSystem` still owns HUD markers/results, selection clearing, and camera flags; `UnitTargetOrderSystem` now owns target validation and attack component writes.

## Validation run

- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-step10-architecture.xml -logFile /private/tmp/warlinecapture-step10-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTargetOrderSystemTests -testResults /private/tmp/warlinecapture-step10-targetorder.xml -logFile /private/tmp/warlinecapture-step10-targetorder.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter MissileLauncherRadarAttackValidationTests -testResults /private/tmp/warlinecapture-step10-missile.xml -logFile /private/tmp/warlinecapture-step10-missile.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BattleHudGameplayBridgeConnectionTests -testResults /private/tmp/warlinecapture-step10-hudbridge.xml -logFile /private/tmp/warlinecapture-step10-hudbridge.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTransportValidationTests -testResults /private/tmp/warlinecapture-step10-transport.xml -logFile /private/tmp/warlinecapture-step10-transport.log`

## Validation result

- Passed: `GameplayArchitectureContractTests` 35/35.
- Passed: `UnitTargetOrderSystemTests` 5/5.
- Passed: `MissileLauncherRadarAttackValidationTests` 5/5.
- Passed: `BattleHudGameplayBridgeConnectionTests` 6/6.
- Passed: `UnitTransportValidationTests` 16/16.

## Known gaps

- `RTSSelectionSystem` still owns camera/focus behavior, clicked-entity lookup, HUD result application, marker spawning, and some general controllable-entity validation used by non-attack commands.
- `UnitTargetOrderSystem` is still a composed plain C# system object, not an ECS `ISystem`, matching the current facade extraction pattern.

## Cross-lane impacts

- QA should treat this as a behavior-preserving target-order refactor.
- Architecture lane can rely on the updated contract test to keep attack validation and `EngageTarget` writes out of the selection facade.
- UI lane should continue routing attack commands through the gameplay facade and avoid direct target component writes from views.

## Next recommended task

Continue the RTS selection split by moving camera/focus state into `RtsCameraSystem` or a shell-edge camera service fed by ECS camera request components.
