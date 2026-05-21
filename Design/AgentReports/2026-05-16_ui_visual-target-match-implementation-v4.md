# Lane
UI

# Task
P0 visual target-match implementation v4 for `SCN-02_MainMenu` and `POP-05_MissionResult`, after PM rejection of v3 placeholder/fallback art.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs`
- `Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/designed_unavailable_badge.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/footer_status_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/mode_card_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/profile_block_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/resource_counter_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/screen_shell_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/side_route_button_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/top_resource_strip_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_command_authority.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_credits.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_materials.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/settings_gear_icon.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV4_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV4_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV4_1672x941.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV4_vs_Target_Comparison.png`

# Contracts touched
- Removed visible runtime use of SCN-02 `commander_profile_placeholder`.
- Removed visible/runtime use of SCN-02 mode-card fallback/generated art:
  - `Assets/Game/Art/UI/Generated/MainMenu/Cards/MainMenu_CardArt_Saga.png`
  - `Assets/Game/Art/UI/Generated/MainMenu/Cards/MainMenu_CardArt_Operation.png`
  - `Assets/Game/Art/UI/Generated/MainMenu/Cards/MainMenu_CardArt_QuickCustom.png`
- Stopped copying rejected SCN-02 manifest layers whose ids/paths are placeholder or placeholder-scale:
  - `commander_profile_placeholder`
  - `mode_card_art_saga`
  - `mode_card_art_operation`
  - `mode_card_art_quick_custom`
- `SCN-02_MainMenu` mode-card art clips now serialize `null` sprites and are inactive until Art/Atlas delivers approved production slices.
- `SCN-02_MainMenu` commander avatar/profile avatar now serialize `null` sprites until Art/Atlas delivers approved production profile art.
- `SCN-02_MainMenu` resource icons now use manifest-declared `LayeredOneGo` icons instead of old shell resource icons.
- `SCN-02_MainMenu` plus/settings button backgrounds no longer serialize old shell button plates; settings keeps only the manifest-declared gear icon.
- `SCN-02_MainMenu` Deploy Command background now uses a manifest-declared frame rather than the POP-05 button asset.
- POP-05 remains built from manifest-declared `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json` assets plus live TMP/controllers.

# User-visible behavior
- SCN-02 no longer displays placeholder/fallback card art or placeholder commander art. Those regions are intentionally blank/blocked rather than faked.
- SCN-02 keeps live TMP text, routing buttons, resource values, resource icons, mode-card route buttons, and the Deploy Command CTA.
- POP-05 remains a live runtime popup with manifest assets, live TMP mission/reward/objective/consequence rows, Replay, and Continue.
- This is a blocker handoff, not a visual-complete claim.

# Validation run
- PM rejection read:
  - `Design/AgentReports/2026-05-16_pm_ui-v3-placeholder-fallback-rejected.md`
- Art/Atlas routing read:
  - `Design/AgentReports/2026-05-16_pm_art-atlas-pop05-scn02-no-placeholder-reopen.md`
- Unity prefab builds in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-mainmenu-build-v4-codexunity1.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMissionResultPopup -logFile /private/tmp/warlinecapture-ui-pop05-build-v4-codexunity1.log`
- Unity captures in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-ui-mainmenu-capture-v4-codexunity1.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMissionResultPopupVisual -logFile /private/tmp/warlinecapture-ui-pop05-capture-v4-codexunity1.log`
- Direct comparisons:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV4_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV4_vs_Target_Comparison.png --label SCN-02_MainMenu_VisualTargetMatchImplementationV4`
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png --capture Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV4_1672x941.png --out Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV4_vs_Target_Comparison.png --label POP-05_MissionResult_VisualTargetMatchImplementationV4`
- No-placeholder/fallback runtime asset scan:
  - `rg -n "placeholder|MainMenu_CardArt|MainMenu_Resource_|TargetMatchCompositeOverlay|SCN02_MainMenu_Landscape_TargetComposite|POP05_MissionResult_Landscape_TargetComposite" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo`
- Focused EditMode tests:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-mainmenu-tests-v4-results.xml -logFile /private/tmp/warlinecapture-ui-mainmenu-tests-v4.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiComponentPrefabTests -testResults /private/tmp/warlinecapture-ui-component-prefab-tests-v4-results.xml -logFile /private/tmp/warlinecapture-ui-component-prefab-tests-v4.log`
- Hygiene:
  - `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`

# Validation result
- No-placeholder/fallback runtime asset scan: no matches.
- SCN-02 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV4_1672x941.png`
- SCN-02 comparison: `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV4_vs_Target_Comparison.png`
- SCN-02 comparison score: `mse=1253.70`
- POP-05 capture: `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV4_1672x941.png`
- POP-05 comparison: `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV4_vs_Target_Comparison.png`
- POP-05 comparison score: `mse=776.39`
- `WarlineCaptureUiMainMenuTests`: 7 passed / 0 failed.
- `WarlineCaptureUiComponentPrefabTests`: 17 passed / 0 failed.
- `git diff --check`: passed for touched UI source/test/prefab files.

# Remaining mismatch table
| Surface | Region | Current result | Remaining mismatch / blocker | Owner |
|---|---|---|---|---|
| SCN-02_MainMenu | Commander profile avatar | Visible placeholder art removed; sprites are null. | `Design/VisualLockLayered/SCN-02_MainMenu/layers/commander_profile_placeholder.png` is disallowed by PM because the id/path contains `placeholder`. Need production commander/profile fallback art with a non-placeholder id/path and manifest update. | Art/Atlas |
| SCN-02_MainMenu | Saga mode card art | Fallback/generated card art removed; `ArtClip` inactive and sprite null. | `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_saga.png` is placeholder-scale/wrong-quality for target-lock. Need approved target-quality production slice and manifest update. | Art/Atlas |
| SCN-02_MainMenu | Persistent Operation mode card art | Fallback/generated card art removed; `ArtClip` inactive and sprite null. | `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_operation.png` is placeholder-scale/wrong-quality for target-lock. Need approved target-quality production slice and manifest update. | Art/Atlas |
| SCN-02_MainMenu | Quick Custom Game mode card art | Fallback/generated card art removed; `ArtClip` inactive and sprite null. | `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_quick_custom.png` is placeholder-scale/wrong-quality for target-lock. Need approved target-quality production slice and manifest update. | Art/Atlas |
| SCN-02_MainMenu | Top resource/plus/settings/CTA chrome | Old shell/fallback plates removed or replaced with manifest frames where possible. | Exact target-lock still needs manifest-declared production slices for plus button chrome, settings button chrome, Deploy Command CTA chrome/chevrons, brand logo plate, world-map/footer tactical layer, and top resource plate treatment. | Art/Atlas |
| POP-05_MissionResult | Header/hero | Uses current manifest assets and live TMP. | Target-lock still needs production slices matching the premium winged Victory chrome, mission thumbnail block, logo/sidebar treatment, and target star composition. Required Art/Atlas report is missing: `Design/AgentReports/2026-05-16_art-atlas_pop05-scn02-implementation-ready-no-placeholders.md`. | Art/Atlas |
| POP-05_MissionResult | Rewards/objective/consequence | Uses current manifest assets and live TMP. | Target-lock still needs production reward card/item art and chrome matching the approved target. Required Art/Atlas report is missing. | Art/Atlas |
| SCN-08_RTSBattleHUD / M01 Match HUD | Exact target-lock | Not changed in this v4 pass. Existing v6 remains accepted only for narrow HUD fixes. | Target state and Gameplay battlefield/camera/unit composition remain unresolved for a 100% target-lock claim. | PM for target state; Gameplay for battlefield/camera/unit composition; UI for HUD coordinates after routing |

# Known gaps
- This is an explicit blocker handoff, not a visual-complete claim.
- UI removed disallowed placeholder/fallback/generic/old-shell visible assets from the current SCN-02 target-lock path where possible.
- UI cannot produce target-lock SCN-02/POP-05 without the Art/Atlas no-placeholder implementation-ready package requested by PM.
- Missing required file/report/command: `Design/AgentReports/2026-05-16_art-atlas_pop05-scn02-implementation-ready-no-placeholders.md`
- Owner lane for unblock: Art/Atlas.
- Whether another lane can continue: Art/Atlas can continue now. UI should not retry exact SCN-02/POP-05 target-lock until Art/Atlas publishes the required report and PM/user accepts it.

# Cross-lane impacts
- Art/Atlas is actively routed by PM and owns the next action.
- QA/HCI should not validate SCN-02/POP-05 as target-lock complete.
- Gameplay is not blocked by this UI cleanup, but SCN-08 exact target-lock remains separately dependent on Gameplay/PM target state.

# Next recommended task
Art/Atlas should deliver `Design/AgentReports/2026-05-16_art-atlas_pop05-scn02-implementation-ready-no-placeholders.md` with production, no-placeholder SCN-02 and POP-05 layers plus manifest updates. After PM/user accepts that Art/Atlas handoff, UI should produce v5 by importing only those approved layers, rebuilding prefabs, and capturing fresh target comparisons.
