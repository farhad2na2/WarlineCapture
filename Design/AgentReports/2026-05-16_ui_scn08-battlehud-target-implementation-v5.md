# Lane
UI

# Task
P0 SCN-08/M01 Match HUD v5 integration with accepted Art slices.

Integrated the accepted SCN-08 Art/Atlas layer package and Select-command correction into the Match HUD. M01 now uses command order `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`; `SPECIAL` remains generic/non-M01 only and is hidden for M01.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/**`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v5_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v5_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_vs_Target_Comparison.png`

# Contracts touched
- SCN-08 layer manifest: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- Accepted Art handoffs:
  - `Design/AgentReports/2026-05-16_art-atlas_scn08-rtsbattlehud-complete-implementation-slices.md`
  - `Design/AgentReports/2026-05-16_art-atlas_scn08-select-command-correction.md`
  - `Design/AgentReports/2026-05-16_pm_art-atlas-scn08-select-accepted-ui-v5.md`
- M01 command rule: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`; `SPECIAL` not used for M01.
- M01 no-selection rule: `Destroy hostile patrol`, ARIA closed, Build hidden/unavailable, no selected squad panel, no command target marker layer, no active command mode.

# User-visible behavior
- Match HUD imports and binds accepted v5 slices for squad portraits, squad card chrome, shield/rank badges, objective/check icons, clock icon, threat rows, minimap content, minimap viewport, zoom buttons, command button chrome, top buttons, and rail/minimap/objective/threat frames.
- M01 no-selection HUD now shows the v5 command rail as `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`, all neutral/disabled, with `command_select_icon.png` on the first slot.
- `SPECIAL` is not visible in M01; it remains available for generic/non-M01 SCN-08 usage.
- Build remains hidden/unavailable in M01 no-selection.
- ARIA/assistant entry and panel are closed in M01 no-selection evidence.
- Squad strip stays visible for mockup density, with non-rifle cards visible but disabled for M01.

# Validation run
- Copied accepted layer slices:
  - `python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py --apply --force`
- Rebuilt Match Overlay prefab:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-scn08-v5-build-2.log`
- Focused EditMode suite:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-ui-scn08-v5-match-overlay-results-3.xml -logFile /private/tmp/warlinecapture-ui-scn08-v5-match-overlay-tests-3.log`
- Editor/prefab capture:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureM01NoSelectionEvidenceV5 -logFile /private/tmp/warlinecapture-ui-scn08-v5-editor-capture-gfx.log`
- Runtime capture:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5 -logFile /private/tmp/warlinecapture-ui-scn08-v5-runtime-capture-gfx.log`
- Comparisons:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v5_1920x1080.png --out Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v5_vs_Target_Comparison.png --label SCN08_M01_NoSelection_v5`
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png --out Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_vs_Target_Comparison.png --label SCN08_M01_Runtime_v5`
- Diff hygiene:
  - `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

# Validation result
- Prefab rebuild: pass.
- `WarlineCaptureUiMatchOverlayTests`: pass, 20/20.
- Editor/prefab capture: pass.
- Runtime route capture: pass; log reports `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED`.
- Editor comparison generated: `mse=1045.04`.
- Runtime comparison generated: `mse=1429.54`.
- Diff hygiene: pass.
- Note: the first editor capture attempt with `-nographics` failed because Unity was running with `NullGfxDevice`; reran without `-nographics` successfully. This is a capture environment constraint, not a HUD failure.

# Region checklist
- Objective panel: accepted v5 frame/fill, M01 objective `Destroy hostile patrol`, non-M01 objective rows hidden in no-selection.
- Top resource bar: accepted resource icons and new clock icon bound; pause/settings top buttons updated to accepted v5 chrome.
- Threat feed: accepted panel, row backgrounds, warning/enemy-spotted icons, readable row spacing.
- Squad cards: accepted portraits for rifle/APC/tank/air support, accepted normal/selected card chrome, shield/rank badges, air support no longer uses pending-art fallback label.
- Command rail: accepted rail/frame/button chrome; M01 order is `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`; all neutral/disabled in no-selection; no `SPECIAL` for M01.
- Minimap: accepted minimap content, frame, viewport rectangle, plus/minus zoom button art.
- Global chrome: accepted imagegen slices keep transparent outside corners and stronger target-quality body/well detail; sprite borders updated to manifest values.
- M01 no-selection: ARIA closed, Build hidden, selected entity panel hidden, command mode hidden, command wheel/build drawer hidden, world command marker layer hidden.

# Known gaps
- Runtime capture includes live Gameplay-owned soldiers, enemy readability rings, and health bars; those are not UI command target markers and are part of current gameplay runtime visibility proof.
- Some remaining target-vs-runtime MSE comes from the underlying gameplay map/camera content differing from the static SCN-08 target reference, not from the HUD slice binding.
- Build drawer still uses legacy iso thumbnails/time metric where outside the active M01 HUD requirement; tests now keep this contract separate from the v5 Match HUD portrait/clock bindings.

# Cross-lane impacts
- Art/Atlas accepted slices are now imported into Unity-generated HUD assets.
- Gameplay runtime capture path succeeded and shows current live ECS soldier/readability overlays under the updated HUD.
- PM/QA can review v5 evidence without waiting on a new Art package.

# Next recommended task
PM/QA review `2026-05-16_ui_scn08-battlehud-target-implementation-v5.md` and compare the fresh v5 editor/runtime evidence against `SCN-08_RTSBattleHUD_Landscape_Target.png`. If accepted, route the next UI priority through `Design/AgentTasks/ui_current.md`; do not start POP-05/SCN-02 until that file is updated.
