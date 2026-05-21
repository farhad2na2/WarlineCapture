# UI SCN-08 Battle HUD Target Implementation V4

Date: 2026-05-16
Status: ready for PM/QA review

## Lane

UI

## Task

P0 SCN-08/M01 Match HUD v4 visual-quality correction after PM partial acceptance of v3.

Latest handoff assessment:

- `Design/AgentReports/2026-05-16_pm_ui-scn08-v3-partial-accept-layout-rejected.md`: accepted for corrected M01 no-selection state; needs fixes for full visual quality and bottom HUD command/minimap layout.

Implemented v4 scope:

- Moved `CommandBar` to the SCN-08 layered target rail rect `676,744,704,164`.
- Rebuilt the rail art to fill the 704x164 rail instead of overflowing a compressed 522x124 root.
- Re-anchored STOP/HOLD/MOVE/ATTACK/SPECIAL command buttons to manifest-relative target slots inside the 704x164 rail.
- Fixed M01 no-selection runtime refresh so it reapplies anchor-based command-button rects instead of changing stretched `anchoredPosition`, which was shifting ATTACK/SPECIAL into the minimap at 1920x1080.
- Added focused tests proving ATTACK and SPECIAL do not overlap `MiniMapPanel` before and after M01 no-selection refresh.
- Added a v4 no-selection capture method that writes a unique evidence image.

Layered target used:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`

## Files changed

- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_v4_vs_SCN08_Target_Comparison.png`
- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v4.md`

## Contracts touched

- Preserved `Design/Architecture/gameplay_solid_ecs_contract.md`: UI still displays mission/runtime state and emits command affordances only; no gameplay policy moved into UI.
- M01 no-selection contract remains enforced: objective is `Destroy hostile patrol`, ARIA closed, Build hidden, selected/status panel hidden, command banner hidden, command wheel/drawer hidden, no command target marker layer, command buttons visible but neutral/disabled.
- SCN-08 layered HUD manifest now drives command rail/button rects for the bottom command surface.

## User-visible behavior

- At 1920x1080, STOP/HOLD/MOVE/ATTACK/SPECIAL are fully readable.
- ATTACK and SPECIAL no longer clip into or disappear behind the minimap in the M01 no-selection HUD.
- Bottom HUD separation is improved: squad cards, command rail, and minimap are distinct readable regions.
- M01 runtime flow capture now shows the same readable command rail composition.
- Build remains unavailable/hidden for M01.

## Validation run

- Prefab rebuild:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen -logFile /private/tmp/warlinecapture-ui-scn08-v4-build.log`
- Focused EditMode:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-ui-scn08-v4-match-overlay-results.xml -logFile /private/tmp/warlinecapture-ui-scn08-v4-match-overlay-tests.log`
- Fresh no-selection capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureM01NoSelectionEvidenceV4 -logFile /private/tmp/warlinecapture-ui-scn08-v4-no-selection-capture.log`
- Runtime flow capture, first attempt:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5 -logFile /private/tmp/warlinecapture-ui-scn08-v4-m01-v5-runtime-capture.log`
- Runtime flow capture, graphics retry:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV5 -logFile /private/tmp/warlinecapture-ui-scn08-v4-m01-v5-runtime-capture-gfx.log`
- Comparison montage:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_1920x1080.png --out Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_vs_Target_Comparison.png --label SCN08_M01_NoSelection_v4`
- Runtime comparison montage:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png --capture Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png --out Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_v4_vs_SCN08_Target_Comparison.png --label SCN08_M01_Runtime_v5_after_v4`
- Whitespace:
  `git diff --check -- Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`

## Validation result

- Prefab rebuild passed.
- `WarlineCaptureUiMatchOverlayTests` passed: 20/20.
- `git diff --check` passed for UI-touched files.
- Fresh v4 editor/prefab evidence captured:
  `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_1920x1080.png`
- Editor/prefab comparison generated:
  `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_vs_Target_Comparison.png`
  MSE: `1018.05`
- First runtime capture with `-nographics` produced a blank gray frame with `RenderTexture.Create failed`; this was retried without `-nographics`.
- Runtime graphics retry succeeded:
  `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- Runtime comparison generated:
  `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_v4_vs_SCN08_Target_Comparison.png`
  MSE: `1390.07`

Region checklist against SCN-08 target:

- Objective panel: M01 objective text/state correct; chrome uses layered one-go panel frame/fill; remaining typography/chrome gap is asset-quality/style tuning.
- Top resource bar: spacing and pause/settings controls retained from v3/v4 target alignment; no regression observed in capture.
- Pause/settings: larger target-aligned icon rects retained; no overlap.
- Threat feed: M01 row state correct; remaining mismatch is row art density and target threat-row background quality.
- Squad cards: visible and separated; disabled non-rifle cards preserved for SCN-08 density; remaining mismatch is missing target-quality portrait/card micro-detail.
- Command rail/buttons: v4 fixed; full STOP/HOLD/MOVE/ATTACK/SPECIAL visible and readable; no minimap overlap for ATTACK/SPECIAL by test and capture.
- Minimap: readable and separated from command buttons; remaining mismatch is generated minimap content/zoom style density versus target.
- Chrome/trim/shadows/transparency/typography/spacing/visual density: command rail spacing fixed; remaining global polish depends on missing layered art/data listed below.

## Known gaps

- Art-owned: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json` states the generated set does not include squad portraits, shield badges, rank chevrons, objective icons, minimap content, minimap zoom buttons, or threat row backgrounds. UI is still using existing fallback/separate assets for these.
- Art-owned: squad cards still cannot reach target-quality portrait/card micro-detail until target-quality per-card art exists.
- Art-owned/UI integration later: minimap density/style does not exactly match target because accepted replacement minimap content/zoom assets are not present in the SCN-08 generated set.
- UI-owned but lower than the P0 overlap bug: broader chrome/shadow/typography tuning can continue after PM review if the current v4 command readability fix is accepted as sufficient to release the next slice.
- Runtime capture caveat: `-nographics` runtime capture path can generate a blank frame due `RenderTexture.Create failed`; graphics batchmode retry produced valid evidence.

## Cross-lane impacts

- Art/Atlas owns missing SCN-08 target-quality replacement slices listed in the manifest note.
- Gameplay runtime M01 flow is not modified by this pass.
- QA/HCI can review v4 using the no-selection and runtime captures above.
- POP-05 and SCN-02 remain held until PM/user accepts or releases UI from the Match HUD lane.

## Next recommended task

PM/QA should review `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_1920x1080.png` and `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png` for acceptance of the bottom HUD readability/layout fix. If accepted, release UI to the next queued slice. If rejected for remaining visual polish, route Art/Atlas to provide the missing SCN-08 slices named above or keep UI on chrome/spacing-only refinement with those gaps explicitly accepted.
