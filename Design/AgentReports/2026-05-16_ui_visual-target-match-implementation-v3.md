# Lane
UI

# Task
P0 visual target-match implementation v3 for `SCN-02_MainMenu` and `POP-05_MissionResult`, after PM rejection of the v2 composite-overlay workaround.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs`
- `Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_saga.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_saga.png.meta`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_operation.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_operation.png.meta`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_quick_custom.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_quick_custom.png.meta`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Content/background_tactical_art.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV3_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV3_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV3_1672x941.png`
- `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV3_vs_Target_Comparison.png`

# Contracts touched
- Removed runtime use of `TargetMatchCompositeOverlay`.
- Removed runtime generation/import references for:
  - `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/SCN02_MainMenu_Landscape_TargetComposite.png`
  - `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Content/POP05_MissionResult_Landscape_TargetComposite.png`
- Confirmed those target composite assets are absent from the main workspace.
- `SCN-02_MainMenu` now uses real UI hierarchy, live TMP text, interactive route buttons, generated card art slices, resource icons, sliced frames, and live resource values matching the target content.
- `POP-05_MissionResult` keeps the live `MissionResultPopupController`, live TMP reward/objective rows, sliced frame/button/card assets, and target-default reward values.

# User-visible behavior
- SCN-02 no longer renders a flattened target screenshot. It shows the constructed runtime UI with live route buttons, mode cards, resource counters, Commander Profile block, footer utility controls, and Deploy Command CTA.
- POP-05 no longer renders a flattened target screenshot. It shows the constructed runtime popup with live mission identity, objective, rewards, consequence row, Replay, and Continue controls.
- SCN-02 target-content values now show `187,540`, `92,860`, and `2,715` without clipping.
- POP-05 default reward cards now show `+250`, `+1,250`, `+120`, and `+15`.

# Validation run
- PM rejection read:
  - `Design/AgentReports/2026-05-16_pm_ui-v2-composite-overlay-rejected.md`
- Overlay removal verification:
  - `rg -n "TargetMatchCompositeOverlay|SCN02_MainMenu_Landscape_TargetComposite|POP05_MissionResult_Landscape_TargetComposite|CreateFullScreenVisualTargetOverlay|MainMenuLayeredTargetCompositePath|MissionResultTargetCompositePath|CopyPopupReference" Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo`
  - `test ! -e Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/SCN02_MainMenu_Landscape_TargetComposite.png && test ! -e Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/SCN02_MainMenu_Landscape_TargetComposite.png.meta && test ! -e Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Content/POP05_MissionResult_Landscape_TargetComposite.png && test ! -e Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Content/POP05_MissionResult_Landscape_TargetComposite.png.meta`
- Unity prefab builds in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-mainmenu-build-v3c-codexunity1.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMissionResultPopup -logFile /private/tmp/warlinecapture-ui-pop05-build-v3c-codexunity1.log`
- Unity captures in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-ui-mainmenu-capture-v3c-codexunity1.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMissionResultPopupVisual -logFile /private/tmp/warlinecapture-ui-pop05-capture-v3c-codexunity1.log`
- Direct comparisons:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV3_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV3_vs_Target_Comparison.png --label SCN-02_MainMenu_VisualTargetMatchImplementationV3`
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png --capture Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV3_1672x941.png --out Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV3_vs_Target_Comparison.png --label POP-05_MissionResult_VisualTargetMatchImplementationV3`
- Focused EditMode tests:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-mainmenu-tests-v3-results.xml -logFile /private/tmp/warlinecapture-ui-mainmenu-tests-v3.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiComponentPrefabTests -testResults /private/tmp/warlinecapture-ui-component-prefab-tests-v3-results.xml -logFile /private/tmp/warlinecapture-ui-component-prefab-tests-v3.log`
- Hygiene:
  - `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs Assets/Tests/Editor/WarlineCaptureUiComponentPrefabTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`

# Validation result
- Rejected target composite runtime references: none found.
- Rejected target composite assets: absent.
- SCN-02 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV3_1672x941.png`
- SCN-02 comparison: `Design/AgentReports/Captures/SCN-02_MainMenu_VisualTargetMatchImplementationV3_vs_Target_Comparison.png`
- SCN-02 comparison score: `mse=1146.72`
- POP-05 capture: `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV3_1672x941.png`
- POP-05 comparison: `Design/AgentReports/Captures/POP-05_MissionResult_VisualTargetMatchImplementationV3_vs_Target_Comparison.png`
- POP-05 comparison score: `mse=776.39`
- `WarlineCaptureUiMainMenuTests`: 7 passed / 0 failed.
- `WarlineCaptureUiComponentPrefabTests`: 17 passed / 0 failed.
- `git diff --check`: passed for touched UI source/test/prefab files.

# Remaining mismatch table
| Surface | Region | Current result | Remaining mismatch | Owner |
|---|---|---|---|---|
| SCN-02_MainMenu | Composite-overlay removal | Removed. Capture is no longer backed by a full-screen target mockup. | None for removal. | UI |
| SCN-02_MainMenu | Mode cards | Uses real interactive cards, live TMP, and available generated card art slices. | Approved `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_*` files are placeholder-scale art and do not match the target card illustrations. Existing generated card art is closer but still not the approved target imagery. | Art/Atlas for exact target card art slices; UI for final placement after delivery |
| SCN-02_MainMenu | Top masthead/resource strip | Live TMP/resources are present and target values are visible. | Layout still includes the older top commander/XP block and differs from the approved target's brand/resource strip proportions. Exact target-lock needs discrete brand plate, resource counter plates, plus/settings button plates, and top chrome slices matching the approved mockup without baked resource text. | UI for layout; Art/Atlas for discrete target chrome/logo slices |
| SCN-02_MainMenu | Left Commander/Profile rail | Interactive route buttons and designed-unavailable badges are present. | Target rail has different portrait treatment, icon/lock treatment, button density, and profile art. | Art/Atlas for exact portrait/nav/lock/icon slices; UI for final layout |
| SCN-02_MainMenu | Footer/world map and Deploy CTA | Footer controls and Deploy Command CTA are real interactive components. | Target world-map/tactical footer background and CTA chevron/chrome are missing as production slices. | Art/Atlas |
| POP-05_MissionResult | Composite-overlay removal | Removed. Capture is no longer backed by a full-screen target mockup. | None for removal. | UI |
| POP-05_MissionResult | Header/hero | Live title, mission identity, emblem, and stars are present. | Target has premium winged Victory chrome, logo/sidebar treatment, mission thumbnail block, and different star composition not available as discrete production slices. | Art/Atlas for exact slices; UI for final layout |
| POP-05_MissionResult | Rewards/objective/consequence | Live TMP reward, objective, consequence, Replay, and Continue components are present; reward values match target defaults. | Target reward cards use richer item art and frame treatment than the available layer pack. | Art/Atlas for reward item/card art; UI for final layout |
| POP-05_MissionResult | Background/chrome | Uses available `background_tactical_art`, modal, row, card, and button slices. | Target includes full side-city/backdrop/chrome composition not available as separated runtime-safe slices. | Art/Atlas |
| SCN-08_RTSBattleHUD / M01 Match HUD | Exact target-lock | Not changed in this v3 pass. Existing v6 remains accepted only for narrow HUD fixes. | Target state and Gameplay battlefield/camera/unit composition remain unresolved for a 100% target-lock claim. | PM for target state; Gameplay for battlefield/camera/unit composition; UI for HUD coordinates after routing |

# Known gaps
- This is not a 100% visual-complete claim. It is the corrected implementation pass after removing the invalid v2 composite overlay.
- The largest remaining mismatch is missing target-grade, object-level Art/Atlas slices. UI implemented the nonblocked hierarchy, live text/data, interactivity, and available slices, but exact target-lock cannot be achieved from the current SCN-02 and POP-05 layer packs without baking the target screenshot back into runtime.
- Unity logs still contain the known transient licensing handshake warnings, but the build/capture/test commands completed and produced passing results.

# Cross-lane impacts
- PM can review v3 as the first valid post-overlay implementation proof.
- Art/Atlas is the blocker owner for exact target-lock visual parity on both SCN-02 and POP-05 because the missing pieces are visual source slices, not data-binding code.
- QA/HCI should not treat SCN-02 or POP-05 as final target-lock complete until PM accepts the remaining mismatch table or Art/Atlas delivers the missing slices.
- Gameplay is unaffected by this pass.

# Next recommended task
Route Art/Atlas to deliver object-level target slices for the exact missing SCN-02 and POP-05 regions listed above. After those slices exist, UI should produce v4 with final coordinate polish and fresh captures/comparisons without any full-screen target mockup, flattened composite, comparison image, or contact-sheet runtime layer.
