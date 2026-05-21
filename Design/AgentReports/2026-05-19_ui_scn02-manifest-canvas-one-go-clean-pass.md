# Lane
UI

# Task
SCN-02 Main Menu one-go manifest canvas setup using the approved standalone generated assets.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`
- `Design/VisualLockLayered/SCN-02_MainMenu/scn02_main_menu_layout.json`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_ManifestCanvasTest.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/ManifestCanvas/`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_3840x2160.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_vs_Target_Comparison.png`

# Contracts touched
- Added a deterministic layout manifest contract for SCN-02:
  `Design/VisualLockLayered/SCN-02_MainMenu/scn02_main_menu_layout.json`
- Runtime asset source is restricted to:
  `Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets`
- Runtime import output is isolated under:
  `Assets/Game/Art/UI/Generated/MainMenu/ManifestCanvas/`

# User-visible behavior
- Builds a fresh main-menu test canvas from one manifest instead of manual hardcoded panel iteration.
- Uses standalone high-resolution sprites only for the runtime layout.
- Keeps image aspect ratios with explicit `contain`/`cover`/`stretch` fit modes.
- Places settings gear, left-nav icons, badges, card icons, card art, top resource bar, deploy CTA, and live TMP text from manifest rects.

# Validation run
- `python3 Tools/UI/validate_scn02_manifest_layout.py`
- `python3 -m json.tool Design/VisualLockLayered/SCN-02_MainMenu/scn02_main_menu_layout.json`
- Manifest asset resolution check against standalone root: 60 images, 29 text blocks, 0 missing.
- `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs Design/VisualLockLayered/SCN-02_MainMenu/scn02_main_menu_layout.json`
- Unity3 licensing-workaround capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-manifestcanvas-unity3.log`
- Runtime forbidden-name scan on the new prefab/output folder for old iteration terms.
- Comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_3840x2160.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_vs_Target_Comparison.png --label SCN-02-manifest-canvas-clean`

# Validation result
- Unity3 build and capture passed.
- Manifest layout validation passed: standalone-only asset root, 60 images, 29 text blocks, 0 missing, content art below frames, operation meters above warning rows, icons set to aspect-preserving contain fit, old-source tokens rejected.
- Fresh capture generated:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_3840x2160.png`
- Review-size capture generated:
  `Design/AgentReports/Captures/SCN-02_MainMenu_ManifestCanvas_1672x941.png`
- Comparison generated with MSE `605.02`.
- New runtime prefab/output folder scan returned no references to old Option3/LayerCanvasTest/SourceAssetsBatch01/ImageGenFlat/LayeredOneGo/target-slice/full-preview/cleaned-candidate paths.

# Known gaps
- This is a clean organized baseline, not a pixel-perfect target lock.
- Some generated standalone sprite shapes still differ from the reference mockup, especially exact chrome proportions and card/profile frame styling.
- The builder still exposes the old editor method names for compatibility, but the runtime output path/prefab/captures are the new ManifestCanvas names.
- The one-go conversion now has guardrails for layer order and aspect preservation; further improvements should come from asset bounds/slot metadata rather than per-panel manual edits.

# Cross-lane impacts
- Art/Atlas can replace individual standalone PNGs without UI code changes if filenames stay stable.
- PM/QA can review the fresh `ManifestCanvas` capture without comparing against prior old iteration images.

# Next recommended task
Review `SCN-02_MainMenu_ManifestCanvas_1672x941.png` as the new clean baseline. If accepted, promote this manifest workflow from test prefab to `Screen_MainMenu.prefab` and wire real route buttons/bindings against the same layout manifest.
