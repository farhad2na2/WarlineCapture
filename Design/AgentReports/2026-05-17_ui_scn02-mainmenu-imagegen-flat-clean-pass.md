# Lane
UI

# Task
SCN-02 MainMenu target-match cleanup iteration after user feedback that the prior chrome was messy, thick, and not close enough to the minimal target. Continued only the active `Design/AgentTasks/ui_current.md` priority.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/ImageGenFlat/**`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_20x9_vs_Target_Comparison.png`

# Contracts touched
- `Screen_MainMenu` prefab route buttons and real TMP/data bindings preserved.
- MainMenu sprite atlas packables updated to include `ImageGenFlat/FramesTrimmed`.
- No target slices, target composites, screenshots, contact sheets, or full mockup overlays used as runtime UI.
- Existing v2 Art/Atlas Unity destinations from the accepted SCN-02 v2 import remain the runtime content base; this pass adds imagegen-only flat chrome and opacity/layout cleanup on top of that import.

# User-visible behavior
- Main menu chrome is cleaner and less heavy:
  - background tactical map dimmed to reduce noisy glow;
  - top cyan trim reduced;
  - logo/resource/settings/profile/nav/card/feed/deploy frame opacity reduced;
  - 20:9 nav rows now use the same `ImageGenFlat` row chrome as standard aspect;
  - deploy chevrons and frame tone reduced.
- The result is visibly closer and less cluttered than the previous `ImageGenFlatPass`, but it is not a perfect region-by-region target lock.

# Validation run
- Unity build: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-imagegen-flat-clean-build.log`
- 16:9 capture: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-ui-scn02-imagegen-flat-clean-capture.log`
- 20:9 capture: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 -logFile /private/tmp/warlinecapture-ui-scn02-imagegen-flat-clean-capture-20x9.log`
- 16:9 compare: `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_vs_Target_Comparison.png --label SCN-02_MainMenu_ImageGenFlatCleanPass`
- 20:9 compare: `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_ImageGenFlatCleanPass_20x9`
- Focused tests: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-scn02-imagegen-flat-clean-tests-results.xml -logFile /private/tmp/warlinecapture-ui-scn02-imagegen-flat-clean-tests.log`
- Forbidden runtime target scan: `rg -n "TargetRoot|TargetSlice|target_slice|TargetMatchComposite|SCN02_MainMenu_Landscape_TargetComposite|SCN-02_MainMenu_Landscape_Target|layers_contact_sheet|scn02_complete_production_sprites_contact_sheet|MainMenu_Landscape_Visual_Target" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo Assets/Game/Art/UI/Generated/MainMenu/ImageGenFlat`
- Whitespace validation: `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Assets/Tests/Editor/UIMainMenuTests.cs Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`

# Validation result
- Unity prefab rebuild passed.
- 16:9 capture passed: `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_1672x941.png`.
- 20:9 capture passed: `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenFlatCleanPass_20x9.png`.
- 16:9 MSE: `615.91`, improved from current-task baseline `1077.03`, previous ImageGen opacity pass `726.72`, and first flat pass `676.55`.
- 20:9 MSE: `606.09`, improved from current-task baseline `1043.91`, previous ImageGen opacity pass `717.15`, and first flat pass `687.91`.
- `WarlineCaptureUiMainMenuTests`: passed `7/7`.
- Forbidden target-slice/composite scan: no matches.
- `git diff --check`: passed after normalizing generated Unity prefab trailing whitespace.

# Known gaps
| Region | Status | Owner |
|---|---|---|
| Background | Improved but still brighter/noisier than target floor glow and map balance. | UI can continue tone tweaks; art/content mismatch remains. |
| Masthead | Functional, but logo proportions and top resource grouping still differ from target. | UI-owned layout/TMP/chrome. |
| Top bar | Cleaner after opacity reduction, but generated frame silhouette still not identical. | Imagegen art/content limitation plus UI opacity. |
| Settings | Cleaner, still not exact target square/chrome silhouette. | Imagegen art/content limitation. |
| Commander profile | Less heavy, but profile frame corners and portrait treatment still differ. | Imagegen art/content limitation plus UI tone. |
| Left nav | Cleaner and dimmer; row silhouette still not exact target narrow chrome. | Imagegen art/content limitation. |
| Mode cards | Less cluttered; card art and generated corner marks remain visibly different from target. | Art/content mismatch; UI can only tune placement/tone. |
| Operation rows/meters | Legible and positioned, but target row balance is cleaner. | UI-owned layout/TMP/tone. |
| Deploy CTA | Dimmer and less heavy, but silhouette is still not exact target. | Imagegen art/content limitation plus UI tone. |
| 20:9 command feed | Lower-left placement retained; frame tone reduced. | UI-owned layout/tone. |

# Cross-lane impacts
- No other screen lanes were modified intentionally.
- This pass used imagegen-only generated chrome assets under `ImageGenFlat`; no deterministic/procedural new art was introduced as runtime UI.
- Source docs and other lane task files were not edited.

# Next recommended task
PM/QA should review the fresh `ImageGenFlatCleanPass` captures. If visual lock still requires exact chrome silhouette, Art/Atlas needs imagegen-only regenerated flat chrome assets that match the target geometry more tightly; if PM accepts continued UI-only iteration, the next UI pass should focus on darker card art/tactical map tone and smaller top/card text deltas without changing bindings.
