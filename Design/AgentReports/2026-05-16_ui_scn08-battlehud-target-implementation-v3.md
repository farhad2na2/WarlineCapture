Lane:
UI

Task:
P0 reject UI v2 evidence; fix M01-01 SCN-08 HUD state to match routed target/rules. Source of truth was `Design/AgentTasks/ui_current.md`; target authority was `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png` and `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_vs_SCN08_Target_Comparison.png`
- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v3.md`

Contracts touched:
- M01 no-selection HUD now forcibly hides ARIA entry/panel dock, selected entity panel, command mode banner, invalid command toast, command wheel, build drawer, and world command marker layer.
- M01 objective evidence state is refreshed after the active M01 mission is set, then scoped to exactly `Destroy hostile patrol`.
- M01 no-selection command buttons render above the command rail, stay visible in order STOP, HOLD, MOVE, ATTACK, SPECIAL, are non-interactable, and use neutral command chrome instead of selected MOVE art.
- Build remains unavailable/hidden for M01 through the existing `MissionDoesNotAllowBuild` rule path.
- The editor capture path now includes a dedicated M01 no-selection evidence method instead of reusing a mixed-state matrix frame.

User-visible behavior:
- M01-01 no-selection HUD shows the correct objective text, no ARIA panel, no Build command, no selected squad status panel, no active command banner, and no UI command target markers.
- Disabled command buttons remain readable instead of being hidden behind the command rail or showing MOVE as the active command.
- Runtime M01 capture now shows the same no-selection HUD state on the game scene.

Validation run:
- Prefab builder:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-scn08-v3-build-main-final.log`
- Focused UI EditMode:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-ui-scn08-v3-match-overlay-results-neutral.xml -logFile /private/tmp/warlinecapture-ui-scn08-v3-match-overlay-tests-neutral.log`
- Dedicated M01 no-selection evidence capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureM01NoSelectionEvidence -logFile /private/tmp/warlinecapture-ui-scn08-v3-no-selection-capture-neutral.log`
- Runtime M01 V5 capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5 -logFile /private/tmp/warlinecapture-ui-scn08-v3-m01-v5-capture.log`
- Target comparison generation:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_SCN08_NoSelection_1920x1080.png --out Design/AgentReports/Captures/M01-01_SCN08_NoSelection_vs_Target_Comparison.png --label "M01-01 SCN-08 UI no-selection evidence"`
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png --out Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_vs_SCN08_Target_Comparison.png --label "M01-01 runtime vs SCN-08 HUD target"`
- Static/new lookup scan:
  `rg -n "FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|GameObject\\.Find|Transform\\.Find|Resources\\.FindObjectsOfTypeAll" Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Whitespace check:
  `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab Design/AgentReports/Captures/M01-01_SCN08_NoSelection_1920x1080.png`

Validation result:
- Prefab builder passed. Log shows `[WarlineCaptureUI] Match Overlay screen generated.` and `Exiting batchmode successfully now!`
- `WarlineCaptureUiMatchOverlayTests` passed 20/20. Results: `/private/tmp/warlinecapture-ui-scn08-v3-match-overlay-results-neutral.xml`.
- Dedicated no-selection evidence capture passed: `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_1920x1080.png`.
- Visual inspection of that evidence confirms: `Destroy hostile patrol`, ARIA closed, Build hidden, neutral/disabled command buttons, no active command banner, no selected entity panel, and no world command markers.
- Runtime V5 capture succeeded this run. Log line: `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`.
- Target comparison outputs generated:
  `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_vs_Target_Comparison.png` with MSE `1015.48`.
  `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_vs_SCN08_Target_Comparison.png` with MSE `1376.88`.
- `git diff --check` passed.
- Static scan found no lookup calls in `M01InfantryOnlyHudScopeController.cs` or `WarlineCaptureUiMatchOverlayTests.cs`. Existing `GameObject.Find` calls remain in `Chapter01M01PlayModeValidationTests.cs` at rejected-wrapper assertions and were not introduced by this patch.

Known gaps:
- Runtime capture still shows Gameplay-owned enemy readability/health overlays around enemy units. These are not the UI `WorldCommandMarkerLayer`; UI command target markers are hidden.
- Runtime battlefield composition, soldier placement, and tactical ground art remain Gameplay-owned for target-match acceptance.
- Command bar right-side buttons are partly occluded by the minimap at 1920x1080 because this pass preserved the existing SCN-08 rail/minimap footprint. UI can tighten this in a follow-up if PM wants a no-overlap crop pass.

Cross-lane impacts:
- QA/HCI can review the new no-selection evidence capture and runtime V5 capture for the PM-rejected state mismatch.
- Gameplay remains owner for runtime battlefield/soldier/readability alignment versus gameplay visual targets.
- UI has no remaining blocker for the specific PM v2 rejection: the evidence state now matches the M01-01 no-selection rules.

Next recommended task:
PM/QA should review `M01-01_SCN08_NoSelection_1920x1080.png` and `M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`; if accepted, route any remaining runtime battlefield/marker/readability deltas to Gameplay and any command-bar/minimap overlap polish back to UI as a separate focused crop pass.
