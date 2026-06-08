# Lane
UI

# Task
SCN-02 Main Menu final target-lock import, placement, rebuild, capture, and comparison pass after accepted Art/Atlas target-lock asset revisions.

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
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 Main Menu prefab now imports and composes the accepted revised manifest layers from `LayeredOneGo`.
- Main Menu sprite atlas membership includes the revised SCN-02 layered content, frames, overlays, icons, and buttons.
- Main Menu focused editor test contract now expects the accepted revised commander profile portrait dimensions: `180x150`.
- Wide-aspect UI contract keeps the 20:9-only command-feed objects behind `WarlineCaptureAspectVariantSwitcher`.

# User-visible behavior
- The Main Menu now renders the revised Art/Atlas card art, commander silhouette portrait, brand/resource icons, left-nav icons, unavailable badges, and deploy CTA overlays.
- Existing route buttons and live TMP text remain in the runtime prefab.
- No target mockup, target composite, comparison image, screenshot, contact sheet, placeholder, or target-slice image is used as runtime UI.
- The screen is visually improved but still does not match the approved target region by region.

# Validation run
- Forced revised asset import:
  `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply --force`
- Unity worker rebuild:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-final-target-lock-build.log`
- 1672x941 capture:
  Unity `CaptureMainMenuVisual`, log `/private/tmp/warlinecapture-ui-scn02-final-target-lock-capture.log`
- 20:9 capture:
  Unity `CaptureMainMenuVisual20x9`, log `/private/tmp/warlinecapture-ui-scn02-final-target-lock-capture-20x9.log`
- 1672x941 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_vs_Target_Comparison.png --label SCN-02_MainMenu_FinalTargetLockPass`
- 20:9 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_FinalTargetLockPass_20x9`
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-scn02-mainmenu-final-target-lock-results.xml -logFile /private/tmp/warlinecapture-ui-scn02-mainmenu-final-target-lock-tests.log`
- Forbidden runtime asset scan:
  `rg -n "TargetRoot|TargetSlice|target_slice|TargetMatchComposite|SCN02_MainMenu_Landscape_TargetComposite|SCN-02_MainMenu_Landscape_Target|layers_contact_sheet|scn02_complete_production_sprites_contact_sheet|MainMenu_Landscape_Visual_Target" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo`
- `git diff --check` on the touched SCN-02 script, test, prefab, and final capture artifacts.

# Validation result
- Forced copy completed and imported `49` manifest PNG layers into `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo`.
- Explicit overwrite verification passed for revised routed source/destination pairs:
  - `mode_card_art_saga`: `31153cf8ef66acc90272df98fb0e73d2aeda3376e030ecb161e2b72be47b2ea1`
  - `mode_card_art_operation`: `484015aa9f3815e040172cc3c1f4f880ede32d95eb3bebbe1eecd01c5d3a43af`
  - `mode_card_art_quick_custom`: `be7f9a4da4fc39b3d583257d78beffff562c061c031431bf9ace3ee07cd4f3b5`
  - `commander_profile_portrait`: `dcf82056f1996152aaf369c187226b6488adbfb80d99a2e7a9108e7e6a3a4a81`
- Unity worker rebuild completed successfully.
- Fresh 1672x941 capture produced: `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_1672x941.png`.
- Fresh 20:9 capture produced: `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9.png`.
- Fresh 1672x941 comparison produced: `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_vs_Target_Comparison.png`, MSE `1077.03`.
- Fresh 20:9 comparison produced: `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9_vs_Target_Comparison.png`, MSE `1043.91`.
- `WarlineCaptureUiMainMenuTests`: `7 passed / 0 failed`.
- Forbidden runtime asset scan returned no matches.
- `git diff --check`: passed after trimming Unity-generated trailing whitespace in `Screen_MainMenu.prefab`.

# Region mismatch table
| Region | Current result | Remaining mismatch | Owner |
| --- | --- | --- | --- |
| Background | Improved | Runtime map/glow is brighter and framed differently; target has darker, wider world-map emphasis and cleaner bottom depth. | UI for exposure/placement tuning. |
| Masthead | Improved | Runtime logo panel is bulkier and emblem/text treatment still differs from target. | Art/Atlas for exact emblem/logo content; UI for frame scale and text spacing. |
| Top bar | Improved | Resource slots are too large/high-contrast and not the exact target proportions; settings cluster is larger and farther right. | UI for rects; Art/Atlas if exact target icon/chrome fidelity is required. |
| Settings | Implemented | Gear/frame are functional but oversized and brighter than target. | UI. |
| Commander profile | Improved | Revised silhouette is present, but portrait framing, panel height, and lower label spacing differ from target. | UI for placement; Art/Atlas if PM wants target-identical scan background detail. |
| Left nav | Improved | Rows are wider/brighter than target, badge/lock placement is cramped, and typography is smaller than target. | UI for rect/TMP placement; Art/Atlas if badge/lock art needs another exact pass. |
| Saga card | Improved | Revised art is present but composition differs from target scene; card title/icon/description placement and card scale remain off. | Art/Atlas for target-matching card art content; UI for title/body/footer layout. |
| Persistent Operation card | Improved | Revised blue map art is present but perspective/content differs; warning rows and meters are not target-tight. | Art/Atlas for map content; UI for operation row layout. |
| Quick Custom card | Improved | Revised base/mountain art is present but target has a wider base scene with aircraft composition; command-feed overlay intrudes in 20:9. | Art/Atlas for card art; UI for wide command-feed placement. |
| Operation detail rows | Improved | Rows exist, but icon size, meter length, row dividers, and label/value alignment are still off. | UI. |
| Deploy CTA | Improved | CTA remains too bright/tall and chevrons are much larger/closer to the edge than target. | UI for scale/tone; Art/Atlas if subtler chevron/glow assets are required. |
| 20:9 command feed | Not matched | Target places a large command-feed panel at lower left; runtime currently keeps a small panel inside/near the Quick Custom card area. | UI placement/layout. |

# Known gaps
- This is not target-lock complete. The final captures do not visually match the approved references region by region.
- PM should not send this directly to QA/HCI as accepted target-lock. It is ready for PM review as an honest final UI pass after revised-asset import.
- Additional UI iteration is needed for exact rects, TMP sizing, command-feed 20:9 placement, operation rows, deploy CTA scale/tone, top bar sizing, and left-nav badge placement.
- Additional Art/Atlas work is needed if PM requires closer-than-current card art, brand emblem/logo, scan portrait background, resource icons, badge/lock, and CTA chevron/glow fidelity. The current revised assets are improved but still not target-identical.
- No missing manifest-declared layer was found, and no blocked command remains.

# Cross-lane impacts
- PM: decision needed on whether to route one more supervised UI placement pass, route Art/Atlas for target-identical content revisions, or accept this as a non-target-lock visual improvement.
- Art/Atlas: likely owns the next improvement for card art composition, brand/resource icon exactness, badge/lock styling, and CTA chevron/glow if PM wants a closer target match.
- QA/HCI: should wait for PM acceptance or a new UI/Art iteration; current result is not a region-by-region target match.
- Other lanes: no POP-05, SCN-08, Gameplay, PM task file, or source doc work was intentionally modified.

# Next recommended task
PM should route a narrow follow-up instead of broad autonomous target-lock work:

1. UI-owned pass: move the 20:9 command feed to the lower-left target position, then tighten left-nav badge placement, operation rows, deploy CTA scale/tone, top-bar rects, and card TMP/footer placement.
2. Art/Atlas-owned pass: if PM wants a true target-lock result, provide closer target-matching card art, logo/emblem, resource icons, badge/lock, commander scan background, and CTA chevron/glow layers before the next UI capture pass.
