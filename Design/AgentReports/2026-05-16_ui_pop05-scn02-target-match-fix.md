# Lane
UI

# Task
POP-05 / SCN-02 target-match fix after PM visual rejection of `2026-05-16_ui_pop05-scn02-approved-target-implementation.md`.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Popups/MissionResultPopupController.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs`
- `Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/**`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_materials.png`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_intel.png`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_star_empty.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_TargetMatchFix_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_TargetMatchFix_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_TargetMatchFix_1672x941.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_TargetMatchFix_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 `Screen_MainMenu` prefab hierarchy now exposes target-lock nodes for `MastheadText`, `CommanderProfilePanel`, three vertical `ModeCard_*` cards, operation risk rows, and `DeployCommandButton`.
- POP-05 `MissionResultPopup` keeps live TMP/data binding paths for mission identity, rewards, objective completion, consequence row, and Replay/Continue buttons.
- `WarlineCaptureUiMainMenuTests` now validates the target composition instead of the rejected horizontal mode-row shell.
- `WarlineCaptureUiComponentPrefabTests` continues to validate Mission Result reward/objective binding and controller runtime data.

# User-visible behavior
- Main Menu now presents the Warline Capture masthead, top resource strip, Commander Profile block, designed-unavailable side routes, three large mode cards, Persistent Operation pressure/risk content, Quick Custom Game card, and Deploy Command CTA.
- Mission Result now presents a premium Victory screen with tactical background art, hero header, mission image/identity block, star cluster, objective completion row, reward cards, city consequence row, and styled Replay/Continue controls.
- Live TMP text remains separate from imagery for reusable/runtime data fields.

# Validation run
- `Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-scn02-target-fix-build.log`
- `Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMissionResultPopup -logFile /private/tmp/warlinecapture-pop05-target-fix-build.log`
- `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-scn02-target-fix-test-results.xml -logFile /private/tmp/warlinecapture-scn02-target-fix-tests.log`
- `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiComponentPrefabTests -testResults /private/tmp/warlinecapture-component-target-fix-test-results.xml -logFile /private/tmp/warlinecapture-component-target-fix-tests.log`
- `Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-scn02-target-fix-capture.log`
- `Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMissionResultPopupVisual -logFile /private/tmp/warlinecapture-pop05-target-fix-capture.log`
- `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_TargetMatchFix_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_TargetMatchFix_vs_Target_Comparison.png --label SCN-02_TargetMatchFix`
- `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png --capture Design/AgentReports/Captures/POP-05_MissionResult_TargetMatchFix_1672x941.png --out Design/AgentReports/Captures/POP-05_MissionResult_TargetMatchFix_vs_Target_Comparison.png --label POP-05_TargetMatchFix`

# Validation result
- Build Main Menu: passed.
- Build Mission Result: passed.
- `WarlineCaptureUiMainMenuTests`: passed, 7/7.
- `WarlineCaptureUiComponentPrefabTests`: passed, 17/17.
- Captures generated at 1672x941.
- SCN-02 comparison generated, MSE `1042.03`.
- POP-05 comparison generated, MSE `776.16`.

# Known gaps
- SCN-02 is now compositionally aligned to the target but is not a perfect pixel lock: the available SCN-02 layer pack does not include the target's exact full-screen illustrated world-map/background plate, exact masthead logo art, or exact CTA chrome as standalone non-text layers.
- POP-05 is now compositionally aligned to the target but is not a perfect pixel lock: the available POP-05 layer pack does not include a separate mission-thumbnail illustration matching the target, so the runtime mission image reuses the tactical background art crop.
- No SCN-08/M01 Match HUD final target-lock claim is made here.

# Cross-lane impacts
- PM/QA can review the fresh SCN-02 and POP-05 captures.
- Art/Atlas owns any remaining exact-background, exact-thumbnail, or exact-bespoke CTA/logo layer gaps needed for stricter pixel matching without baking live text into runtime UI.
- Gameplay and QA/HCI should remain held until PM/user accepts this UI handoff or routes fixes.

# Next recommended task
PM/user visual review of `SCN-02_MainMenu_TargetMatchFix_1672x941.png` and `POP-05_MissionResult_TargetMatchFix_1672x941.png`. If stricter pixel lock is required, route Art/Atlas for the missing standalone background/thumbnail/logo/CTA layers, then UI can bind those layers without changing live TMP ownership.
