# Lane
UI

# Task
P0 visual target-match implementation v5 for `SCN-02_MainMenu` and `POP-05_MissionResult` after PM accepted the Art/Atlas no-placeholder package.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_1672x941.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_vs_Target_Comparison.png`

# Contracts touched
- Accepted Art/Atlas handoffs assessed as accepted/actionable:
  - `Design/AgentReports/2026-05-16_art-atlas_pop05-scn02-implementation-ready-no-placeholders.md`
  - `Design/AgentReports/2026-05-16_pm_art-atlas-pop05-scn02-no-placeholder-accepted-ui-v5.md`
- `SCN-02_MainMenu` now imports/copies all accepted manifest layers from `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`.
- `SCN-02_MainMenu` now serializes and displays:
  - `commander_profile_portrait`
  - `mode_card_art_saga`
  - `mode_card_art_operation`
  - `mode_card_art_quick_custom`
- Removed the v4 null-sprite/blank-region workaround for SCN-02 commander/profile and mode-card art regions.
- `POP-05_MissionResult` now imports/copies the accepted `icon_star_empty` layer in addition to the existing accepted POP-05 layer set.
- Removed stale generated POP-05 icons no longer declared by the accepted manifest:
  - `icon_supply_crate`
  - `icon_unlock_fragments`
- No runtime use of full-screen target composites or target screenshots was added.

# User-visible behavior
- SCN-02 commander avatar/profile avatar are visible again using accepted production portrait art.
- SCN-02 Saga Campaign, Persistent Operation, and Quick Custom Game cards display accepted production card art instead of blank/null regions or generated fallback card art.
- POP-05 continues to use live TMP/controller-driven mission result content with the accepted layered background, modal chrome, emblem, stars, stat/reward/consequence/objective rows, and buttons.
- This is an implementation handoff with visual proof. It is not a 100% target-lock completion claim.

# Validation run
- Unity prefab builds in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-mainmenu-build-v5-codexunity1.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMissionResultPopup -logFile /private/tmp/warlinecapture-ui-pop05-build-v5-codexunity1.log`
- Unity captures in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-ui-mainmenu-capture-v5-codexunity1.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMissionResultPopupVisual -logFile /private/tmp/warlinecapture-ui-pop05-capture-v5-codexunity1.log`
- Direct comparisons:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_vs_Target_Comparison.png --label SCN-02_MainMenu_VisualTargetMatchImplementationV5`
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png --capture Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_1672x941.png --out Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_vs_Target_Comparison.png --label POP-05_MissionResult_VisualTargetMatchImplementationV5`
- Runtime asset scan:
  - `rg -n "placeholder|TargetMatchCompositeOverlay|SCN02_MainMenu_Landscape_TargetComposite|POP05_MissionResult_Landscape_TargetComposite|MainMenu_CardArt_|MainMenu_Resource_|icon_supply_crate|icon_unlock_fragments" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo`
- Focused EditMode tests:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-mainmenu-tests-v5-results-rerun2.xml -logFile /private/tmp/warlinecapture-ui-mainmenu-tests-v5-rerun2.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiComponentPrefabTests -testResults /private/tmp/warlinecapture-ui-component-tests-v5-results-rerun2.xml -logFile /private/tmp/warlinecapture-ui-component-tests-v5-rerun2.log`
- Hygiene:
  - `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Tests/Editor/UIMainMenuTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_1672x941.png Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_1672x941.png`

# Validation result
- SCN-02 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_1672x941.png`
- SCN-02 comparison: `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV5_vs_Target_Comparison.png`
- SCN-02 comparison score: `mse=988.98` (`v4` was `mse=1253.70`)
- POP-05 capture: `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_1672x941.png`
- POP-05 comparison: `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV5_vs_Target_Comparison.png`
- POP-05 comparison score: `mse=776.39`
- Runtime asset scan: no matches.
- `WarlineCaptureUiMainMenuTests`: 7 passed / 0 failed.
- `WarlineCaptureUiComponentPrefabTests`: 17 passed / 0 failed.
- `git diff --check`: passed for touched UI source/test/prefab/capture files.

# Remaining mismatch table
| Surface | Region | Current result | Remaining mismatch / blocker | Owner |
|---|---|---|---|---|
| SCN-02_MainMenu | Commander/profile | Accepted portrait art is visible in top bar and profile panel. | Position, scale, and surrounding profile chrome still do not fully match the approved target composition. | UI |
| SCN-02_MainMenu | Mode cards | Accepted Saga, Operation, and Quick Custom art is visible; no null/blank/fallback card art remains. | Card proportions, art crop placement, text block density, risk rows, and CTA treatment still diverge from the target. | UI |
| SCN-02_MainMenu | Header/resource strip | Uses accepted manifest frames/icons and live TMP values. | Header spacing, resource strip density, settings/plus chrome, and masthead/logo treatment still need target-coordinate polish. | UI |
| SCN-02_MainMenu | Footer/side routes/deploy CTA | Uses accepted manifest frames and designed-unavailable badges. | Side route spacing, footer tactical treatment, and Deploy Command CTA do not yet lock to the target. | UI |
| POP-05_MissionResult | Overall popup | All accepted manifest layers are imported/copied, including `icon_star_empty`; stale undeclared generated reward icons removed. | Capture score did not improve from v4. The popup needs a deeper layout pass against the target: hero header, mission identity block, reward grid, consequence row, and button proportions. | UI |
| POP-05_MissionResult | Art sufficiency | Current accepted layer set is usable and wired. | If PM expects exact target chrome beyond the declared slices, Art/Atlas may need more granular slices, but no missing accepted layer blocked this v5 pass. | UI first; Art/Atlas only if PM requests additional slices |
| SCN-08_RTSBattleHUD / M01 Match HUD | Exact target-lock | Not changed in this v5 pass. Existing v6 remains accepted only for narrow HUD fixes. | Full target-lock remains separate from this SCN-02/POP-05 pass. | PM/UI/Gameplay as previously routed |

# Known gaps
- SCN-02 improved but is not yet a 100% visual target-lock match.
- POP-05 still needs a deeper UI layout pass; v5 only completed the accepted-manifest import cleanup and stale-layer removal.
- No sandbox/product approval blocker remains. The next work is implementation, not waiting on another lane.

# Cross-lane impacts
- PM/QA should review the new captures and comparisons, but should not treat v5 as final target-lock acceptance.
- Art/Atlas no-placeholder package was accepted and consumed by UI. Art/Atlas is not currently blocking UI unless PM requests new or more granular slices.
- Gameplay is not touched by this pass.

# Next recommended task
UI should continue with a v6 target-coordinate polish pass for SCN-02 first, then POP-05, using the v5 comparison images as the mismatch guide and preserving the accepted no-placeholder layer usage.
