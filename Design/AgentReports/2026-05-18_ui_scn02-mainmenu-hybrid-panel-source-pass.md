# Lane
UI

# Task
SCN-02 Main Menu hybrid source pass after PM/user review: use approved generated-sheet panel extractions for chrome/panels, keep sharp v2/generated icons, logo mark, card art, and live TMP as separate layers, and avoid baked full-screen/background composites.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/settings_button_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_row_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_frame_large.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/deploy_command_button_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/settings_button_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/left_nav_row_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/mode_card_frame_large.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/deploy_command_button_frame.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_HybridRingPanelsR2_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_HybridRingPanelsR2_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_HybridRingPanelsR2_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_HybridRingPanelsR2_20x9_vs_Target_Comparison.png`

# Contracts touched
- Main Menu panel/chrome contract now binds structural panel sprites from `LayeredOneGo` paths for brand shell, nav rows, card frames, profile frames, resource bar, settings, command feed, and deploy CTA.
- Sharp content remains layered separately: brand emblem, settings gear, resource icons, nav icons, card art, operation meters, footer icons, deploy chevrons, and live TMP text are not baked into panel backgrounds.
- Settings, left-nav, and mode-card panel frames were replaced with frame-only ring extractions from approved generated lock-layer panel sources. Baked centers were stripped before runtime use.
- Deploy CTA reverted to the hollow structural generated frame after the ring extraction proved too filled/heavy in capture.

# User-visible behavior
- Mode-card frames are thinner and cleaner than the prior R8 pass.
- Left-nav row chrome is less thick and less multi-border heavy.
- Settings button keeps the separate sharp gear and no longer has a bright baked gear underneath it.
- Deploy CTA keeps the cleaner hollow amber frame rather than a filled baked panel.
- The screen remains a layered Unity Canvas with real buttons and live text, not a full baked background/image overlay.

# Validation run
- Forced SCN-02 layer copy:
  `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply --force`
- Rebuilt Main Menu prefab:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-scn02-hybrid-ring-panels-r2-build.log`
- Captured 16:9:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-scn02-hybrid-ring-panels-r2-capture16.log`
- Captured 20:9:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 -logFile /private/tmp/warlinecapture-scn02-hybrid-ring-panels-r2-capture20x9.log`
- Compared 16:9 and 20:9 against SCN-02 target references with `Tools/UI/compare_ui_capture_to_target.py`.
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-scn02-hybrid-ring-panels-r2-tests.xml -logFile /private/tmp/warlinecapture-scn02-hybrid-ring-panels-r2-tests.log`
- Guard scan for target/screenshot/contact-sheet runtime references returned no matches.
- `git diff --check` passed for touched SCN-02 UI files after Unity prefab whitespace cleanup.

# Validation result
- Forced SCN-02 manifest copy completed: `49` layer files copied.
- 16:9 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_HybridRingPanelsR2_1672x941.png`
- 20:9 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_HybridRingPanelsR2_20x9.png`
- 16:9 comparison MSE: `516.07`
- 20:9 comparison MSE: `554.76`
- Previous R8 MSE: `549.34` 16:9, `586.32` 20:9.
- `WarlineCaptureUiMainMenuTests`: 7 passed, 0 failed.

# Known gaps
- Not target-lock complete.
- Top resource bar and logo shell still do not exactly match the target panel geometry.
- Commander profile still has frame/profile interior differences versus the reference.
- Operation card content remains more cyan and dense than the target.
- Deploy CTA is cleaner than the failed ring extraction but still heavier/brighter than the exact target.
- Exact target parity likely needs additional panel-specific source extraction or accepted chrome sprites for the remaining top/profile/deploy regions.

# Cross-lane impacts
- UI did not modify non-SCN-02 screens or other lane task files.
- This pass proves the hybrid source strategy is viable and improves measured mismatch without baking full backgrounds.
- Art/Atlas can stay held unless PM wants exact replacement chrome for the remaining top/profile/deploy mismatches.
- If the main Unity project is busy, UI can continue validation in `WarlineCapture-CodexUnity2` or `WarlineCapture-CodexUnity3`.

# Next recommended task
PM/user visual review of `SCN-02_MainMenu_HybridRingPanelsR2_1672x941.png` and `SCN-02_MainMenu_HybridRingPanelsR2_20x9.png`. If continuing UI iteration, next pass should apply the same hybrid extraction approach to top resource/logo/profile/deploy with stricter center stripping and region-specific opacity tuning.
