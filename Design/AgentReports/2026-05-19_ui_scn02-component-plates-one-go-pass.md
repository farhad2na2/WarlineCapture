# Lane
UI

# Task
SCN-02 Main Menu component-plate canvas pass using the fresh approved main-menu asset set.

# Files changed
- `Tools/UI/build_scn02_component_plates.py`
- `Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/component_plates_20260519/assets/`
- `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_ComponentCanvasTest.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas/`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_vs_Target_Comparison.png`

# Contracts touched
- Added a component-plate layout contract:
  `Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json`
- Fresh source roots only:
  `Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets`
  `Design/VisualLockLayered/SCN-02_MainMenu/component_plates_20260519/assets`
- Runtime output is isolated under:
  `Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas/`

# User-visible behavior
- Main menu is now assembled from medium component plates for logo/header/profile/nav/cards/deploy instead of many small manual layers.
- Text remains live TMP for counters, labels, disabled-state copy, descriptions, warning text, and deploy CTA.
- Settings gear, resource icons, left-nav icon/badge/lock groups, card art, card chrome, footer icons, and deploy chevrons are baked into stable plates to reduce drift.
- Latest pass trims transparent padding from icons before fitting, insets major plates from the canvas border, tightens the left-nav stack, enlarges nav icons, and uses the target profile panel source baked into the profile plate.

# Validation run
- `python3 Tools/UI/build_scn02_component_plates.py`
- `python3 -m json.tool Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json`
- Component layout asset resolution check: 14 images, 27 text blocks, 0 missing.
- Unity3 licensing-workaround capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-componentcanvas-unity3-pass4.log`
- Runtime forbidden-name scan on component prefab/output/layout for old iteration terms.
- `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs Tools/UI/build_scn02_component_plates.py Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json`
- Target comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_vs_Target_Comparison.png --label SCN-02-component-canvas`

# Validation result
- Unity3 build and capture passed.
- Component layout JSON is valid.
- Component layout asset resolution passed: 14 images, 27 text blocks, 0 missing.
- Runtime forbidden-name scan returned no references to the previous rejected/legacy SCN-02 output names.
- Fresh review capture generated:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_1672x941.png`
- Comparison generated with MSE `619.45`.

# Known gaps
- This is a clean component-plate baseline, not a pixel-perfect lock.
- The fresh generated card/header/profile art still differs from the original mockup in exact chrome geometry and some icon semantics.
- Background/card proportions are clean enough for a fast one-go pass, but still require a dedicated accepted visual lock if PM wants exact target matching.
- Builder method names remain `BuildLayerCanvasTest` and `CaptureLayerCanvasTest` for existing automation compatibility, while output paths are `ComponentCanvas`.

# Cross-lane impacts
- Art can replace any component plate or source PNG by filename without changing UI code.
- PM/QA should review the ComponentCanvas capture, not previous rejected SCN-02 captures.
- Engineering can promote the component layout to the real main menu once PM accepts the clean baseline.

# Next recommended task
PM/QA review `SCN-02_MainMenu_ComponentCanvas_1672x941.png` as the new baseline. If accepted, wire this component-plate prefab into the real SCN-02 route and bind menu actions/counters.
