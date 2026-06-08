# Lane
UI

# Task
SCN-02 Main Menu v2 imagegen-only clean chrome iteration after user rejected the prior busy/thick panel treatment.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/ImageGenHollow/`
- `Assets/Game/Art/UI/Generated/MainMenu/ImageGenClean/` (first imagegen source attempt retained for audit; runtime uses `ImageGenHollow/FramesTrimmed`)
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_20x9_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 Main Menu frame/chrome source contract changed from accepted `LayeredOneGo` frame sprites to user-authorized imagegen-only `ImageGenHollow/FramesTrimmed` chrome sprites.
- `WarlineCaptureUiMainMenuTests` now verifies the imagegen hollow frame paths and the FramesChrome atlas includes `ImageGenHollow/FramesTrimmed`.
- Route buttons, live TMP labels, mode-card child click targets, and accepted content art remain intact.

# User-visible behavior
- Main Menu panels now use one generated hollow chrome frame per region instead of the previous stacked/busy panel frames.
- Top bar, masthead, commander profile, left nav, mode cards, deploy CTA, settings, and 20:9 command feed are cleaner and less multi-border heavy.
- Runtime still does not perfectly match the target mockup: generated chrome remains more beveled/chunky than the target's narrow flat rails.

# Validation run
- Unity build:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-imagegen-opacity-build.log`
- 16:9 capture:
  `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual`
- 20:9 capture:
  `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9`
- 16:9 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_vs_Target_Comparison.png --label SCN-02_MainMenu_ImageGenOpacityPass`
- 20:9 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_ImageGenOpacityPass_20x9`
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-scn02-imagegen-opacity-tests-r2-results.xml -logFile /private/tmp/warlinecapture-ui-scn02-imagegen-opacity-tests-r2.log`
- Forbidden target-slice/composite scan:
  `rg -n "TargetRoot|TargetSlice|target_slice|TargetMatchComposite|SCN02_MainMenu_Landscape_TargetComposite|SCN-02_MainMenu_Landscape_Target|layers_contact_sheet|scn02_complete_production_sprites_contact_sheet|MainMenu_Landscape_Visual_Target" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo Assets/Game/Art/UI/Generated/MainMenu/ImageGenHollow`
- `git diff --check` on touched script/test/prefab/atlas files.

# Validation result
- Build passed.
- 16:9 capture produced: `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_1672x941.png`
- 20:9 capture produced: `Design/AgentReports/Captures/SCN-02_MainMenu_ImageGenOpacityPass_20x9.png`
- 16:9 MSE: `726.72`.
- 20:9 MSE: `717.15`.
- `WarlineCaptureUiMainMenuTests`: `7 passed / 0 failed`.
- Forbidden target-slice/composite scan returned no matches.
- `git diff --check` passed after stripping Unity prefab trailing whitespace.

# Known gaps
| Region | Result | Owner |
| --- | --- | --- |
| Background | Still close enough structurally but brighter/denser than target bottom map glow. | Art/UI tone |
| Masthead | Cleaner single imagegen frame, but still chunkier than target chrome and logo region is taller/heavier. | Imagegen art/UI placement |
| Top bar | Resource frame is cleaner but generated bevels are heavier than target and text sits slightly high against chrome. | Imagegen art/UI placement |
| Settings | Uses imagegen hollow frame; shape still not exact target octagonal button. | Imagegen art |
| Commander profile | Cleaner than prior pass, but generated frame corner weight remains heavier than target. | Imagegen art |
| Left nav | No more multi-border old shell, but generated row shape has heavier side caps than target. | Imagegen art |
| Mode cards | Single hollow frame now overlays card edges; still not target-lock because the generated frame is visibly thicker and more beveled than target. | Imagegen art |
| Operation detail rows | Existing meters/text preserved; still not exact target spacing and warning rows remain lower/compact. | UI placement |
| Deploy CTA | Improved from molten old CTA, but generated button is still more angular and thicker than target. | Imagegen art/UI placement |
| 20:9 command feed | Lower-left feed remains present and cleaner, but frame is larger/heavier than target. | UI placement/imagegen art |

# Cross-lane impacts
- Art/Atlas no longer has to deliver layered assets for this pass because user allowed UI to produce art with imagegen only.
- PM/QA should not mark SCN-02 target-lock accepted from this pass. It is a measurable improvement, not a region-perfect match.
- The imagegen-only constraint means further chrome matching requires more imagegen prompt iteration, not procedural sprite drawing.

# Next recommended task
PM should review the `ImageGenOpacityPass` captures and decide whether the current imagegen-only chrome direction is acceptable for gameplay UI polish. If exact target lock is still required, route one more imagegen-only art iteration focused specifically on ultra-flat, narrow, non-beveled chrome rails before any further UI placement work.
