# Lane
UI

# Task
SCN-02 Main Menu v2 Art import and focused placement/capture pass.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureAspectVariantSwitcher.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureAspectVariantSwitcher.cs.meta`
- `Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/**`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_20x9_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-v2-placement-pass.md`

# Contracts touched
- SCN-02 Main Menu prefab now uses the accepted v2 manifest layers copied from `Design/VisualLockLayered/SCN-02_MainMenu`.
- Main Menu layout contract updated for the focused v2 placement pass:
  - 20:9 command feed moved to the lower-left target region.
  - top bar/settings geometry tightened.
  - commander profile frame/portrait/lower label tightened.
  - left-nav badge placement tightened and duplicate TMP badge overlay hidden so the v2 badge art is readable.
  - mode card title/icon/art/body/footer placement tightened.
  - Persistent Operation warning rows/meters tightened.
  - deploy CTA scale/chevron placement reduced toward target.
- Main Menu focused editor tests updated to assert the v2 placement contract.

# User-visible behavior
- The Main Menu now shows the accepted v2 card art, commander silhouette, resource icons, nav icons, unavailable badges, and deploy CTA art in runtime UI.
- The 20:9 command feed no longer sits inside the Quick Custom card region; it is relocated to the lower-left command-feed region.
- Route buttons and live TMP/data bindings remain real runtime UI.
- Runtime UI does not use target crops, target composites, screenshots, comparison images, contact sheets, or placeholder art.

# Validation run
- Forced v2 Art import:
  `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply --force`
- Unity worker rebuild:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-v2-placement-build.log`
- 1672x941 capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-ui-scn02-v2-placement-capture.log`
- 20:9 capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 -logFile /private/tmp/warlinecapture-ui-scn02-v2-placement-capture-20x9.log`
- 1672x941 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_vs_Target_Comparison.png --label SCN-02_MainMenu_V2PlacementPass`
- 20:9 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_V2PlacementPass_20x9`
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-scn02-v2-placement-tests-results.xml -logFile /private/tmp/warlinecapture-ui-scn02-v2-placement-tests.log`
- Forbidden runtime asset scan:
  `rg -n "TargetRoot|TargetSlice|target_slice|TargetMatchComposite|SCN02_MainMenu_Landscape_TargetComposite|SCN-02_MainMenu_Landscape_Target|layers_contact_sheet|scn02_complete_production_sprites_contact_sheet|MainMenu_Landscape_Visual_Target" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo`
- `git diff --check` on touched SCN-02 script/test/prefab/capture/report files.

# Validation result
- Forced import copied `49` manifest layer PNGs.
- Explicit v2 source/destination hash verification passed:
  - `mode_card_art_saga`: `c965e9c331051fef3e189b45c42318bba82b2b8c77351a70cbb430a94075e541`
  - `mode_card_art_operation`: `75a684e43cf1b2ac42c8bfa8525d315d007d82982d65888ed82a1156564f88e5`
  - `mode_card_art_quick_custom`: `104ca4a3ba89ca4b2b9e80a0af7d74c11f19c5c71a10d68cec10ae53e7615db7`
  - `commander_profile_portrait`: `8c4bf91299efb03b50cc9c75873bf4366573114d0534c703342d44ba4a7e2df1`
- Unity worker rebuild passed.
- Fresh 1672x941 capture produced:
  `Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_1672x941.png`
- Fresh 20:9 capture produced:
  `Design/AgentReports/Captures/SCN-02_MainMenu_V2PlacementPass_20x9.png`
- Fresh 1672x941 comparison produced MSE `1016.42`, improved from previous final-pass MSE `1077.03`.
- Fresh 20:9 comparison produced MSE `980.49`, improved from previous final-pass MSE `1043.91`.
- `WarlineCaptureUiMainMenuTests`: `7 passed / 0 failed`.
- Forbidden runtime asset scan returned no matches.
- `git diff --check`: passed.

# Region mismatch table
| Region | Current result | Remaining mismatch | Owner |
| --- | --- | --- | --- |
| Background | Improved | Runtime remains brighter with stronger center glow and less target-like bottom map framing. | UI for tone/placement if PM wants another polish pass. |
| Masthead | Improved | Brand area is closer, but logo/emblem still has different proportions and chrome density from target. | UI for minor rects; Art/Atlas if exact logo/emblem fidelity is required. |
| Top bar | Improved | Counters/settings tightened, but resource bar still reads heavier/brighter than target. | UI for rect/tone; Art/Atlas for exact chrome/icon fidelity. |
| Settings | Improved | Button is smaller and closer to target, but still brighter/thicker than target. | UI/Art split. |
| Commander profile | Improved | Portrait/frame/lower label are tighter; silhouette scan still differs from target background detail. | UI for final rects; Art/Atlas for exact scan content. |
| Left nav | Improved | Badge duplicate TMP clutter removed; row/badge readability improved. Rows still run taller/brighter than target. | UI. |
| Saga card | Improved | V2 art is much closer; title/body/footer layout still not exact and art composition still differs from target. | UI for layout; Art/Atlas if target-identical art is required. |
| Persistent Operation card | Improved | V2 map art and rows are better; row spacing/meter styling still not target-tight. | UI for rows; Art/Atlas for exact map content. |
| Quick Custom card | Improved | V2 art is closer; card body/footer and art crop still differ from target. | UI/Art split. |
| Operation detail rows | Improved | Warning rows and meters tightened, but label/value alignment and segment styling remain off. | UI. |
| Deploy CTA | Improved | Scale and chevrons reduced toward target, but glow remains too strong and button still reads brighter than target. | UI tone/rect, Art/Atlas if glow asset is still rejected. |
| 20:9 command feed | Improved | Panel moved from Quick Custom region to lower-left target region. Runtime left rail remains taller than target, so the feed competes with the last nav row and icon scale is still imperfect. | UI. |

# Known gaps
- This pass improves both required MSE scores but is still not a region-by-region target-lock match.
- The best final comparison scores are `1016.42` for 16:9 and `980.49` for 20:9.
- One attempted capture with `-nographics` failed because `CaptureScreenPrefab` requires a graphics device; the capture was rerun without `-nographics` and passed.
- The 20:9 command feed is now in the correct lower-left region, but the standard left-nav layout remains too tall for the 20:9 target. A true 20:9 target match likely needs a wide-specific left-rail layout variant, not just moving the command-feed panel.
- No missing accepted manifest layer was found.

# Cross-lane impacts
- PM: review the improved v2 captures before routing QA/HCI.
- QA/HCI: should not treat this as final target-lock acceptance unless PM accepts the remaining visual deltas.
- Art/Atlas: may still be needed if PM requires exact logo/emblem, resource icon/chrome, scan portrait, operation map, or CTA glow fidelity.
- Other lanes: no POP-05, SCN-08, Gameplay, or non-UI task/source docs were intentionally modified.

# Next recommended task
PM should visually review the v2 captures. If target-lock remains mandatory, route one narrow UI follow-up for a wide-specific 20:9 left-rail/feed layout variant plus final deploy/top-bar/card text polish, with Art/Atlas on standby only for exact-content deltas PM rejects after this v2 proof.
