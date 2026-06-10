# UI Match HUD Selection Summary Validation Blocker

## Lane
UI

## Task
Implement and validate Match HUD `SelectedSquadPanel` multi-selection summary behavior.

## Files changed
- `Assets/Game/Scripts/Systems/SelectionSummaryQuerySystem.cs`
- `Assets/Game/Scripts/Systems/SelectionSummaryQuerySystem.cs.meta`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs`
- `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `Assets/Tests/Editor/BattleHudRuntimeFeedbackSystemConnectionTests.cs`
- `Design/Architecture/match_hud_selection_summary_panel_plan.md`

## Contracts touched
- Match HUD selected squad panel view remains a raw serialized-reference view.
- Selection summary computation stays in system-style code.
- Runtime code does not discover child UI by hierarchy strings.
- No new `Controller`, `Presenter`, `Bridge`, or `Button` class names were added.

## User-visible behavior
- Multi-selection panel now uses category-aware copy such as `2 SOLDIERS`, `MIXED SQUAD`, `MIXED FORCE`.
- Multi-selection health aggregates selected entity health.
- Mixed selected orders display `Mixed orders`.
- Panel portrait falls back to serialized category sprites instead of showing a blank portrait.
- Single focused units and selected buildings also receive fallback portraits if their configured portrait is missing.

## Validation run
- `git diff --check`
- Main workspace direct Unity compiler response validation:
  - `Assembly-CSharp`
  - `Assembly-CSharp-Editor`
- Main workspace Unity Test Runner:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter BattleHudRuntimeFeedbackSystemConnectionTests -testResults /private/tmp/selection-summary-editmode.xml -logFile /private/tmp/selection-summary-editmode.log`
- Main workspace Unity Test Runner retry with fresh output paths:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter BattleHudRuntimeFeedbackSystemConnectionTests -testResults /private/tmp/selection-summary-editmode-main-retry.xml -logFile /private/tmp/selection-summary-editmode-main-retry.log`
- Documented UI workaround Unity Test Runner:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter BattleHudRuntimeFeedbackSystemConnectionTests -testResults /private/tmp/selection-summary-editmode-unity2.xml -logFile /private/tmp/selection-summary-editmode-unity2.log`

## Validation result
Updated after user restarted Unity licensing: focused selection-summary validation passed on the main project.

`git diff --check` passed.

Main workspace `Assembly-CSharp` and `Assembly-CSharp-Editor` compiler response validation passed.

The first sandboxed main-workspace Test Runner attempt failed before tests because Unity Package Manager IPC socket creation was blocked:
- `Error: listen EPERM: operation not permitted /tmp/Unity-Upm-74275.sock`

The out-of-sandbox main-workspace rerun entered repeated Unity licensing reconnect loops before tests started:
- `HandshakeResponse reported an error`
- `ResponseStatus: Unsupported protocol version '1.18.1'`
- `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad-6000.4.0"`
- no `/private/tmp/selection-summary-editmode.xml` was produced.

The fresh main-workspace retry with the documented out-of-sandbox workaround also entered the same licensing loop before assembly reload/test execution:
- log: `/private/tmp/selection-summary-editmode-main-retry.log`
- no `/private/tmp/selection-summary-editmode-main-retry.xml` was produced.
- stopped stuck Unity process `83090` after repeated reconnect/timeout lines.

The documented UI workaround in `/Users/farhad/Projects/WarlineCapture-CodexUnity2` was attempted out-of-sandbox and also stalled before tests started with the same licensing-loop symptom:
- log: `/private/tmp/selection-summary-editmode-unity2.log`
- no `/private/tmp/selection-summary-editmode-unity2.xml` was produced.

Unity2 generated compiler response files are also stale and do not include `Assets/Game/Scripts/Systems/SelectionSummaryQuerySystem.cs`, so Unity2 direct compiler validation cannot be used until that workspace refreshes successfully.

After Unity licensing was restarted manually, a focused main-project run passed:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter SelectionSummaryQuerySystemTests -testResults /private/tmp/selection-summary-query-tests.xml -logFile /private/tmp/selection-summary-query-tests.log`
- result: passed, 5/5 tests
- XML: `/private/tmp/selection-summary-query-tests.xml`
- log: `/private/tmp/selection-summary-query-tests.log`

The broad legacy `BattleHudRuntimeFeedbackSystemConnectionTests` fixture still fails in shared `SetUp` because `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab` is missing. The new selection-summary coverage was moved into isolated `SelectionSummaryQuerySystemTests` so it does not depend on that missing legacy overlay prefab.

## Known gaps
- Focused EditMode tests have not executed through Unity Test Runner.
- Manual play-mode smoke is not complete:
  - select one soldier
  - select one building
  - select multiple soldiers by rectangle
  - select mixed soldiers/vehicles
  - select squad tray soldier and vehicle/transport cards
- Visual confirmation is still needed that the fallback portraits fit the panel frame at 16:9 and 20:9.

## Cross-lane impacts
- No source-doc or other lane task files were modified.
- UI validation is blocked by Unity licensing/session startup, not by a known code compile failure in the main workspace.
- PM/Tools may need to refresh or repair the Unity licensing client/session for Unity 6000.4.0f1, or refresh `WarlineCapture-CodexUnity2` so its generated compiler response files include newly synced source.

## Next recommended task
PM/Tools should unblock Unity batchmode by repairing the Unity licensing client protocol mismatch or refreshing the assigned UI workaround workspace. After that, rerun:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter BattleHudRuntimeFeedbackSystemConnectionTests -testResults /private/tmp/selection-summary-editmode-unity2.xml -logFile /private/tmp/selection-summary-editmode-unity2.log`
