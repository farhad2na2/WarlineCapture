# Android Jenkins Build Monitor - 2026-05-12

## Task Inputs

- `Design/AgentTasks/build-android_current.md`: missing
- `Design/AgentTasks/build-monitor_current.md`: missing

## Jenkins Build

- Job: `Android Unity Project`
- Build: `#36`
- URL: `http://192.168.2.101/job/Android%20Unity%20Project/36/`
- Commit: `b17f07f43a653836c6757f5a60ba1af1f70877ac`
- Result: `SUCCESS`
- Duration: `1672786 ms`

## Artifacts

- `Build/AndroidAPK/WarlineCapture.apk`
- `build.log`
- `TestResults/EditMode.log`
- `TestResults/EditMode.xml`
- `TestResults/PlayMode.log`
- `TestResults/PlayMode.xml`

## BuildGate

Final Jenkins line:

`[BuildGate][FINAL] EditMode tests FAILED with exit code 2; build was allowed to continue. See archived TestResults/EditMode.xml and TestResults/EditMode.log.`

Concise failure reasons:

- `Chapter01TacticalRuntimeBindingTests.GameScene_Chapter01TacticalRuntimeBinderIsWired`: scene YAML/mission definition assertion mismatch.
- `GameSubSceneIsolationValidationTests.ProductionAndLegacyScenesUseDistinctSubSceneAssets`: unexpected `NullReferenceException` log.
- `OperationUiBindingTests`: missing UI text nodes and null references in district/dashboard/shell flows.
- `SettingsPanelSceneValidationTests` and `ThreatWarningValidationTests`: `MenuView` block missing from scene YAML.
- `WarlineCaptureUiArmoryTests`: missing `InspectionPanel/SelectedArtImage` and unlock text mismatch.
- `WarlineCaptureUiCommandExchangeTests`: disabled purchase reason text mismatch.
- `WarlineCaptureUiShellTests`: missing `UI_Canvas` marker and missing splash prefab visual-lock structure.

## Notes

No source files or tests were changed. The Android APK build succeeded even though EditMode tests failed, matching the current Jenkins build-gate policy.
