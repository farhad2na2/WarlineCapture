# Lane

UI

# Task

SCN-02_MainMenu v2 Art import and focused placement pass, additional iteration after user requested another pass.

# Files changed

- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_20x9_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-v2-another-pass.md`

# Contracts touched

- `Screen_MainMenu.prefab` keeps real route buttons for Settings, Saga, Operation Dashboard, Quick Custom, Inbox, Store/Command Exchange, Events, Ranking, Command Feed, and Deploy.
- Runtime TMP text remains live and interactive button targets are preserved.
- 20:9 behavior continues through `WarlineCaptureAspectVariantSwitcher`; this pass only adjusted the built prefab output and builder placement.
- Accepted manifest layers only; no target slices, target composites, screenshots, contact sheets, or full mockup overlays were used as runtime UI.

# User-visible behavior

- Mode card artwork now starts higher and extends closer to target card image bands.
- Operation warning rows were pulled upward to better align under the operation card art.
- Top strip, resource chrome, profile chrome, left nav chrome, mode card chrome, and deploy glow were toned down toward the target.
- Deploy chevrons now face right instead of using the delivered vertical chevron orientation.
- 20:9 command feed remains in the lower-left target area.

# Validation run

- Forced v2 source layer match verified by SHA-256:
  - `mode_card_art_saga.png`: `c965e9c331051fef3e189b45c42318bba82b2b8c77351a70cbb430a94075e541`
  - `mode_card_art_operation.png`: `75a684e43cf1b2ac42c8bfa8525d315d007d82982d65888ed82a1156564f88e5`
  - `mode_card_art_quick_custom.png`: `104ca4a3ba89ca4b2b9e80a0af7d74c11f19c5c71a10d68cec10ae53e7615db7`
  - `commander_profile_portrait.png`: `8c4bf91299efb03b50cc9c75873bf4366573114d0534c703342d44ba4a7e2df1`
- Unity build:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-v2-another-pass-r2-build.log`
- Runtime/editor captures:
  - `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual`
  - `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9`
- Target comparisons:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_vs_Target_Comparison.png --label SCN-02_MainMenu_V2AnotherPassR2`
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_V2AnotherPassR2_20x9`
- Focused EditMode tests:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-scn02-v2-another-pass-tests-results.xml -logFile /private/tmp/warlinecapture-ui-scn02-v2-another-pass-tests.log`
- Forbidden runtime asset scan:
  - `rg -n "TargetRoot|TargetSlice|target_slice|TargetMatchComposite|SCN02_MainMenu_Landscape_TargetComposite|SCN-02_MainMenu_Landscape_Target|layers_contact_sheet|scn02_complete_production_sprites_contact_sheet|MainMenu_Landscape_Visual_Target" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo`
- Whitespace check:
  - `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Game/Scripts/UI/Shell/WarlineCaptureAspectVariantSwitcher.cs Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_1672x941.png Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_20x9.png Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_vs_Target_Comparison.png Design/AgentReports/Captures/SCN-02_MainMenu_V2AnotherPassR2_20x9_vs_Target_Comparison.png`

# Validation result

- Unity build: passed.
- 1672x941 capture: generated.
- 20:9 capture: generated.
- 16:9 MSE: `946.11`, improved from previous pass `1010.67` and original final pass `1077.03`.
- 20:9 MSE: `915.50`, improved from previous pass `960.10` and original final pass `1043.91`.
- Focused EditMode tests: passed, `7/7`.
- Forbidden runtime asset scan: passed, no matches.
- `git diff --check`: passed after trimming Unity prefab whitespace.

# Known gaps

SCN-02 is closer but still not target-lock complete.

| Region | Result | Owner |
| --- | --- | --- |
| Background | Accepted layer is present; lower map glow still reads brighter than target under deploy/card area. | Art/Atlas content if exact target luminance is required |
| Masthead | Logo panel and wordmark are closer, but emblem/brand proportions and chrome intensity still differ. | UI placement plus Art/Atlas chrome content |
| Top bar | Resource strip was toned down; slot frames still differ from target shape and intensity. | UI placement/tone, possibly Art/Atlas frame content |
| Settings | Button is placed and routed; frame/gear proportions remain heavier than target. | UI placement/tone |
| Commander profile | Frame, portrait, and label are closer; portrait crop and frame glow do not exactly match target. | Art/Atlas portrait/frame content plus minor UI placement |
| Left nav | Badges/locks/text are functional and closer; row chrome is still brighter and more segmented than target. | UI tone plus Art/Atlas frame content |
| Saga card | Art band and footer moved closer; source art composition still does not match target crop exactly. | Art/Atlas card art content |
| Operation card | Art band/risk rows moved closer; operation map art and warning row styling still differ. | Art/Atlas content plus minor UI placement |
| Quick Custom card | Art band and footer improved; accepted art composition still differs from target. | Art/Atlas card art content |
| Operation detail rows | Rows are closer vertically; dividers/meters are still thinner/different than target. | UI placement plus Art/Atlas meter/frame content |
| Deploy CTA | Glow was reduced and chevrons face right; button is still too hot/yellow and chevron asset does not match target triple-arrow proportions. | Art/Atlas deploy asset content; UI tone if another pass is requested |
| 20:9 command feed | Lower-left placement remains correct; icon/frame proportions still differ from target. | Art/Atlas content plus minor UI placement |

# Cross-lane impacts

- Art/Atlas: current accepted v2 layers are imported and verified, but exact visual lock still needs asset-level revisions for card art composition/crops, deploy CTA frame/glow/chevrons, and some chrome/frame intensity.
- QA/HCI: can review this as improved evidence, but should not mark SCN-02 target-lock accepted yet.
- PM: this is ready for PM decision on whether to keep iterating UI tone/placement or send remaining deltas back to Art/Atlas.

# Next recommended task

PM should route remaining SCN-02 deltas to Art/Atlas for target-exact card art composition/crops, deploy CTA/chevron asset, and chrome intensity revisions. UI can do one more small placement/tone pass afterward, but the current remaining mismatch is no longer mainly route/TMP/prefab wiring.
