# Lane
UI

# Task
SCN-02 Main Menu production-sprite implementation from accepted Art/Atlas package.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Shell/UIAspectVariantSwitcher.cs`
- `Assets/Game/Scripts/UI/Shell/UIAspectVariantSwitcher.cs.meta`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/**`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 runtime prefab now composes the accepted manifest-declared layers only for the Main Menu surface.
- Main Menu sprite import/atlas contract now includes `LayeredOneGo/Buttons`, `LayeredOneGo/Backgrounds`, and `LayeredOneGo/Overlays`.
- Main Menu editor validation now asserts the production-sprite contract instead of the older shell/crop contract.
- Added `WarlineCaptureAspectVariantSwitcher` so the 20:9 command feed layer can activate only on wide captures/devices.

# User-visible behavior
- Main Menu background, masthead, top resource bar, settings, commander profile, left nav, three mode cards, operation detail rows, deploy CTA, and 20:9 command feed are rebuilt from accepted SCN-02 production sprites.
- Existing route buttons remain real interactive controls: settings, inbox, store, events, ranking, command feed, saga, operation, quick custom, and deploy.
- Rejected target composites, target slices, contact sheets, and flattened screenshots are not used by the runtime prefab.

# Validation run
- `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply`
- Unity worker build: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-production-sprites-build.log`
- 1672x941 capture: Unity `CaptureMainMenuVisual`, log `/private/tmp/warlinecapture-ui-scn02-production-sprites-capture.log`
- 20:9 capture: Unity `CaptureMainMenuVisual20x9`, log `/private/tmp/warlinecapture-ui-scn02-production-sprites-capture-20x9.log`
- Compare 1672x941: `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_vs_Target_Comparison.png --label SCN-02_MainMenu_ProductionSpriteImplementation`
- Compare 20:9: `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_ProductionSpriteImplementation_20x9`
- Focused tests: Unity EditMode `-testFilter WarlineCaptureUiMainMenuTests`, latest results `/private/tmp/warlinecapture-ui-scn02-mainmenu-tests-iter3-results.xml`
- Forbidden runtime asset scan: `rg` over `Screen_MainMenu.prefab` and `LayeredOneGo` for target slices/composites/contact sheets.
- `git diff --check` on touched script/test files.

# Validation result
- Build passed.
- Fresh 1672x941 capture produced: `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_1672x941.png`.
- Fresh 20:9 capture produced: `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9.png`.
- Third-pass 1672x941 comparison produced MSE `1172.77` (improved from `1456.64`).
- Third-pass 20:9 comparison produced MSE `1156.73` (improved from `1347.87`).
- `WarlineCaptureUiMainMenuTests`: 7 passed / 0 failed.
- Forbidden runtime asset scan returned no matches.
- `git diff --check`: passed.

# Region mismatch table
| Region | Status | Remaining mismatch | Owner |
| --- | --- | --- | --- |
| Background | Improved | Tactical map layer fills runtime canvas and oversized bottom trim was removed; residual global tone/detail deltas remain. | UI if PM requires pixel-level exposure/overlay tuning. |
| Masthead | Improved | Brand panel/emblem were scaled closer to target; asset style still differs from target logo treatment. | Art/Atlas for logo content, UI for final kerning. |
| Top bar | Improved | Full resource bar height and resource slots were tightened toward target coordinates; icon art/style still differs from target. | Art/Atlas for icon content, UI for final spacing. |
| Settings | Implemented | Uses accepted settings frame and gear; position/scale may need final target-lock adjustment. | UI |
| Commander profile | Improved | Panel/label moved closer to target; portrait content is still the accepted commander image rather than target silhouette. | Art/Atlas for portrait content, UI for final label placement. |
| Left nav | Improved | Rows were expanded to target rail size and designed-unavailable text is now visible; badge/lock icon treatment still differs. | UI, with Art/Atlas if badge art needs a lock variant. |
| Saga card | Improved | Card uses target-scale rect and a larger art window; card art content still does not match the illustrated target. | Art/Atlas for card art fidelity, UI for final text placement. |
| Persistent Operation card | Improved | Card now uses target-scale rect and operation meter rows; row panel styling/content still differs from target. | UI for row layout, Art/Atlas for operation art fidelity. |
| Quick Custom card | Improved | Card uses target-scale rect and a larger art window; card art content still does not match the illustrated target. | Art/Atlas for card art fidelity, UI for final text placement. |
| Deploy CTA | Improved | CTA moved/scaled to target area and subtitle hidden; accepted glow/chevron content remains brighter/larger than target. | UI for placement, Art/Atlas if chevron/glow style is rejected. |
| 20:9 command feed | Implemented | Wide-only panel and icon activate in 20:9 capture; exact target placement remains approximate. | UI |

# Known gaps
- This is a production-sprite implementation pass, not a pixel-perfect acceptance claim. The third-pass comparisons still report MSE `1172.77` and `1156.73`.
- The largest remaining blockers appear asset-content driven: mode card art differs from the illustrated target, commander portrait differs from the target silhouette, and top bar/resource icons differ from the target's specific art treatment.
- One attempted parallel 20:9 capture failed because Unity cannot open the same project twice; capture was rerun sequentially and passed.
- Existing unrelated dirty capture files under `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/` were already present in the worktree and were not part of this UI pass.

# Cross-lane impacts
- Art/Atlas: no missing accepted SCN-02 manifest layer was found. If PM rejects the visual quality of the mode card art itself, Art/Atlas owns that source-layer correction.
- QA/PM: fresh captures and comparison images are ready for review.
- Other lanes: no POP-05, SCN-08, Gameplay, PM, or source task files were intentionally modified.

# Next recommended task
PM/QA should review the two fresh captures and comparison images. If target-lock is still required, assign a narrow UI follow-up with exact rect/tone corrections by region, starting with commander profile lower labels, operation detail rows, and deploy CTA placement.
