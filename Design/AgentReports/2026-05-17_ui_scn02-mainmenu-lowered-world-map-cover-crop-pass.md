# Lane
UI

# Task
SCN-02 Main Menu target-match iteration after PM/user review: fix incorrect blue/black background behavior, make the lower world-map continents visible under the three mode cards, prevent stretched mode-card art, and continue chrome/settings-frame cleanup.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/main_menu_background_tactical_map.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Backgrounds/main_menu_background_tactical_map.png`
- `Assets/Game/Art/UI/Generated/MainMenu/ImageGenFlat/FramesTrimmed/main_menu_background_tactical_map.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3LoweredCoolWorldMap_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3LoweredCoolWorldMap_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3LoweredCoolWorldMap_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3LoweredCoolWorldMap_20x9_vs_Target_Comparison.png`

# Contracts touched
- Main Menu generated-layer import continues to use manifest-declared SCN-02 runtime layers only.
- Main Menu mode-card art contract changed from direct stretch/crop to masked cover-crop behavior with `AspectRatioFitter.AspectMode.EnvelopeParent`, so art does not stretch on wider card ratios.
- Main Menu focused editor tests updated to accept the current manifest-declared chrome sources for top resource bar, settings button, commander profile panel, and mode-card frames.

# User-visible behavior
- Background is no longer a blue full-screen wash.
- World-map continents are lowered and visible in the lower screen area behind/below the three game-mode panels.
- Lower background color is neutral graphite/cool gray instead of yellow/gold.
- Mode-card images preserve aspect and bleed under the mask instead of stretching.
- Settings button uses the compact generated frame asset, with reduced opacity so it is less blocky than the prior blue shell.
- Chrome is still not a perfect target match; several frames remain heavier/more segmented than the reference.

# Validation run
- Rebuilt Main Menu prefab:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-scn02-lowered-cool-map-build.log`
- Captured 16:9:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-scn02-lowered-cool-map-capture16.log`
- Captured 20:9:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 -logFile /private/tmp/warlinecapture-scn02-lowered-cool-map-capture20x9.log`
- Compared 16:9 against target with `Tools/UI/compare_ui_capture_to_target.py`.
- Compared 20:9 against target with `Tools/UI/compare_ui_capture_to_target.py`.
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-scn02-lowered-cool-map-tests-r2.xml -logFile /private/tmp/warlinecapture-scn02-lowered-cool-map-tests-r2.log`
- Guard scan for target/screenshot/contact-sheet runtime references returned no matches.
- `git diff --check` passed for touched SCN-02 UI files.

# Validation result
- 16:9 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_Option3LoweredCoolWorldMap_1672x941.png`
- 20:9 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_Option3LoweredCoolWorldMap_20x9.png`
- 16:9 comparison MSE: `569.86`
- 20:9 comparison MSE: `605.10`
- `WarlineCaptureUiMainMenuTests`: 7 passed, 0 failed.

# Known gaps
- Not target-lock complete.
- Frame chrome still differs from the target: settings, top bar, mode-card, nav row, and deploy frames remain heavier/more segmented than the clean target chrome.
- Operation card art remains more blue/cyan than the target operation panel.
- Main Menu card/layout spacing is closer after cover-crop, but title/emblem/body/footer positions still need a final alignment pass.
- 20:9 command-feed region still needs review after the lowered-map background change.

# Cross-lane impacts
- UI updated tests to match the new no-stretch card-art contract.
- Art/Atlas does not need to provide a background fix for this pass; UI produced the lowered graphite world-map treatment from the approved generated source.
- If PM rejects remaining chrome mismatch, Art/Atlas should generate a cleaner, exact SCN-02 chrome layer set for settings/top/card/nav/deploy frames; UI can continue placement from the current prefab.

# Next recommended task
PM/user visual review of `SCN-02_MainMenu_Option3LoweredCoolWorldMap_1672x941.png` and `SCN-02_MainMenu_Option3LoweredCoolWorldMap_20x9.png`. If accepted for continued UI iteration, next UI task should be a focused chrome/spacing pass on settings, top bar, three mode-card frames, left nav frames, and deploy CTA without changing other screens.
