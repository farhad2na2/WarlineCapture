# Lane
UI

# Task
P0 SCN-08/M01 Match HUD v6 reimport corrected alpha slices.

Reimported the accepted Art/Atlas SCN-08 alpha-quality fix and regenerated Match HUD evidence. M01 command order remains `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`; `SPECIAL` remains generic/non-M01 only and is hidden for M01.

# Files changed
- `Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/**`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_vs_Target_Comparison.png`

# Contracts touched
- Accepted Art/Atlas alpha-quality fix:
  - `Design/AgentReports/2026-05-16_art-atlas_scn08-alpha-quality-fix.md`
  - `Design/AgentReports/2026-05-16_pm_art-atlas-scn08-alpha-quality-accepted-ui-v6.md`
- SCN-08 manifest: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- M01 command rule: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`; `SPECIAL` excluded from M01.
- M01 no-selection rule: `Destroy hostile patrol`, ARIA closed, Build hidden/unavailable, no selected squad panel, no command target marker layer, no active command mode.

# User-visible behavior
- Corrected alpha-clean SCN-08 layer PNGs are now copied into the Unity HUD asset paths.
- Objective, threat feed, squad cards, command rail, minimap, top/resource chrome, command icons, minimap viewport, zoom buttons, and portraits render without the previous green chroma-key edge spill.
- M01 no-selection still presents `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD` in order, all neutral/disabled.
- `SPECIAL` is still hidden for M01.
- Build, ARIA, selected entity panel, command mode banner, command wheel, build drawer, and world command marker layer remain hidden in M01 no-selection evidence.

# Validation run
- Reimport/copy corrected layers:
  - `python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py --apply --force`
- Rebuilt Match Overlay prefab:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-scn08-v6-build.log`
- Focused EditMode suite:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-ui-scn08-v6-match-overlay-results.xml -logFile /private/tmp/warlinecapture-ui-scn08-v6-match-overlay-tests.log`
- Editor/prefab capture:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureM01NoSelectionEvidenceV6 -logFile /private/tmp/warlinecapture-ui-scn08-v6-editor-capture-gfx.log`
- Runtime capture:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV6 -logFile /private/tmp/warlinecapture-ui-scn08-v6-runtime-capture-gfx.log`
- Comparisons:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_1920x1080.png --out Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_vs_Target_Comparison.png --label SCN08_M01_NoSelection_v6`
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png --out Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_vs_Target_Comparison.png --label SCN08_M01_Runtime_v6`
- Chroma scan:
  - Scanned 68 integrated HUD PNG/capture files for opaque pure green chroma-key pixels using threshold `alpha >= 16`, `green >= 240`, `red <= 20`, `blue <= 20`.
- Diff hygiene:
  - `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

# Validation result
- Corrected layer copy: pass, 48 layer files copied.
- Prefab rebuild: pass.
- `WarlineCaptureUiMatchOverlayTests`: pass, 20/20.
- Editor/prefab capture: pass.
- Runtime route capture: pass; log reports `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED`.
- Editor comparison generated: `mse=1039.93`.
- Runtime comparison generated: `mse=1423.46`.
- Pure green chroma-key scan: pass, `PURE_GREEN_CHROMA_PIXELS 0` across integrated HUD slices plus v6 editor/runtime captures.
- Visual review: previously visible green edge spill is no longer present around objective panel, threat feed, squad cards, command rail, and minimap frames.
- Diff hygiene: pass.

# Region checklist
- Objective panel: corrected alpha-clean frame/fill; M01 objective remains `Destroy hostile patrol`; no green outline spill observed.
- Threat feed: corrected panel frame/fill, active row, and warning icon; no green outline spill observed.
- Squad cards: corrected selected/normal chrome, portraits, shield badge, and rank badge; no green outline spill observed; non-rifle cards remain disabled in M01.
- Command rail/buttons: corrected rail frame/fill and command button states; `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD` order preserved; `SPECIAL` hidden for M01.
- Minimap: corrected frame/fill/content, viewport rectangle, plus/minus zoom buttons; no green outline spill observed.
- Top/resource chrome: corrected resource/top button slices remain bound; clock/resource icons render cleanly.
- M01 no-selection: ARIA closed, Build hidden, selected entity panel hidden, command mode hidden, command wheel/build drawer hidden, world command marker layer hidden.

# Known gaps
- Runtime capture still includes Gameplay-owned live soldiers, enemy readability rings, and health bars. These are not UI command target markers and are outside this alpha-slice correction.
- Remaining target comparison MSE is primarily from the runtime/gameplay map/camera content differing from the static SCN-08 target reference and from v6 preserving current UI composition rather than creating a new art target.
- Broad green-threshold scanning can detect intentional squad health-bar green. The reported artifact proof uses a pure chroma-key threshold to target the rejected green key spill.

# Cross-lane impacts
- Art/Atlas alpha-quality fix is now imported into Unity HUD assets.
- Gameplay runtime route capture continues to work with the updated HUD.
- PM/QA can review v6 evidence for acceptance before UI starts POP-05/SCN-02.

# Next recommended task
PM/QA review `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v6.md` and the v6 captures/comparisons. If accepted, update `Design/AgentTasks/ui_current.md` before routing UI to POP-05, SCN-02, or any other screen.
