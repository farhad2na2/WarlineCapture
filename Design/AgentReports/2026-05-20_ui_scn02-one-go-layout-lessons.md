# Lane
UI

# Task
Document SCN-02 one-go conversion lessons and the replacement safe-area layout strategy.

# Files changed
- `Design/AgentReports/2026-05-20_ui_scn02-one-go-layout-lessons.md`
- `Tools/UI/build_scn02_component_plates.py`
- `Tools/UI/validate_scn02_component_layout.py`
- `Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_slot_report.json`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_ComponentCanvasTest.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas/`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_Diagnostics.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 component layout now carries `layoutPolicy`, image `role`, and text `container` metadata.
- The outer shell is treated as a safe-area rule, not as a rendered runtime plate, because the transparent functional panels allow full-screen shell chrome to show through and visually collide.
- Functional panels are classified as `functional-panel` and are validated against shell safe-area and peer overlap rules.
- The runtime layout now omits `screen_shell_frame.png`; the shell remains a spacing constraint only.
- Component plates now emit local slot metadata before Unity assembly, so content inside each panel is checked against that panel's chrome padding before the full-screen canvas is built.

# User-visible behavior
- Previous approach placed visual plates without machine-checked collision rules, so obvious overlaps could ship in a capture.
- New approach treats the screen as constrained layout: panels must not overlap each other, panels must stay inside shell safe area, and live text must remain inside its declared panel container with padding.
- Fourth iteration changes the solver behavior from shrinking content to growing affected panel lanes first. Nav rows, top resource cells, operation warning rows, and deploy label lanes now reserve larger content areas before Unity assembly.
- Icons are placed with alpha-bounds optical centering so transparent padding in source PNGs does not shift the visible icon away from its panel center.
- Fifth iteration replaces composite component plates with a frame-first layout. Panel frames, icons, badges, locks, meters, art, and text are now separate layout entries so Unity can control them as child objects instead of inheriting baked plate mistakes.
- Each functional panel declares a `safeRect`; child sprites and live text must fit inside that safe rect, and active children are checked for overlap and minimum gaps.
- Diagnostic overlay is generated for engineering review only; it is not a product review capture.

# Validation run
- `python3 Tools/UI/build_scn02_component_plates.py`
- `python3 Tools/UI/validate_scn02_component_layout.py`
- `PYTHONPYCACHEPREFIX=/private/tmp/warline_pycache python3 -m py_compile Tools/UI/validate_scn02_component_layout.py Tools/UI/build_scn02_component_plates.py`
- Unity3 licensing-workaround capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-framefirst-final-unity3.log`
- `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_vs_Target_Comparison.png --label SCN-02-frame-first-final`
- `git diff --check -- Tools/UI/build_scn02_component_plates.py Tools/UI/validate_scn02_component_layout.py Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_slot_report.json Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_ComponentCanvasTest.prefab Design/AgentReports/2026-05-20_ui_scn02-one-go-layout-lessons.md`

# Validation result
- Component layout validator passed: 12 functional panels, 29 live text boxes, frame-first child sprites, no rendered shell image, diagnostic overlay generated for engineering review.
- Component-local slot validation passed: 12 component slot specs, 0 slot failures.
- Text validator also enforces the fourth-iteration font-size floor, so overlap fixes cannot silently shrink labels below the UI scale.
- Safe-rect validation now covers child sprites as well as text, and active children in the same panel are checked for overlaps and minimum gaps.
- Unity3 build and capture passed.
- Fresh review capture generated:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_1672x941.png`
- Diagnostic overlay generated:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_Diagnostics.png`
- Comparison generated with MSE `621.04`; pixel distance is secondary here because this pass prioritizes clean editable layout over baked visual matching.
- `git diff --check` passed.

# Known gaps
- This validator checks rectangular layout constraints; it does not yet inspect actual glyph overflow from TMP after Unity font rendering.
- It validates container geometry, not pixel-perfect target matching.
- Live content placed into component plates now has component-local slot checks, but baked pixels inside generated component artwork are still trusted once the asset is accepted.
- Frame-first layout removes most baked internal composition dependency, but generated source sprites still differ from the target mockup style and proportions.
- The profile frame and mode card frames are still approximate art sources; exact target matching needs better frame assets or deterministic target-derived frames.

# Cross-lane impacts
- PM/QA should review the clean capture; the diagnostic overlay is for UI engineering when a layout fails or needs audit.
- Art can still replace component sprites, but UI will reject layouts that collide with safe areas or panel containers.

# Next recommended task
Review the fresh clean capture. Use the diagnostic overlay only for engineering audit, not as a product-facing result.
