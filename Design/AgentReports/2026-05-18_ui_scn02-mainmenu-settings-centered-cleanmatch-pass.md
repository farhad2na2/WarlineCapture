# Lane
UI

# Task
SCN-02 Main Menu focused target-match iteration after PM/user review: center the settings gear, make the settings button border larger, preserve the cleaner current chrome/background/card pass, and validate the runtime prefab without using target composites or screenshot overlays.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Tests/Editor/WarlineCaptureUiMainMenuTests.cs`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/settings_button_frame.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/settings_button_frame.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_20x9_vs_Target_Comparison.png`

# Contracts touched
- Main Menu settings button now uses the manifest-declared generated `settings_button_frame.png` as the sliced button frame, with a separate live settings gear icon centered inside it.
- Main Menu card-art contract remains masked cover-crop via `AspectRatioFitter.AspectMode.EnvelopeParent`, so the three mode art panels do not stretch.
- Main Menu focused editor tests continue to enforce generated-layer usage, non-raycast decorative graphics, route safety, and the expected settings icon child.

# User-visible behavior
- Settings button border is larger than the previous pass and sits closer to the target top-right scale.
- Settings gear icon is centered inside the button frame instead of drifting within the shell.
- The lower black/graphite world-map background remains visible under the mode cards.
- The current cleaner mode-card cover-crop, lower-opacity chrome, tighter left-nav stack, and reduced deploy tone remain in place.
- This is still not a perfect target-lock match; the overall chrome styling and several generated frame details remain visibly different from the reference.

# Validation run
- Rebuilt Main Menu prefab:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-scn02-cleanmatch-pass-r8-build.log`
- Captured 20:9:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 -logFile /private/tmp/warlinecapture-scn02-cleanmatch-pass-r8-capture20x9.log`
- Captured 16:9:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-scn02-cleanmatch-pass-r8-capture16.log`
- Compared 16:9 against target with `Tools/UI/compare_ui_capture_to_target.py`.
- Compared 20:9 against target with `Tools/UI/compare_ui_capture_to_target.py`.
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-scn02-cleanmatch-pass-r8-tests.xml -logFile /private/tmp/warlinecapture-scn02-cleanmatch-pass-r8-tests.log`
- Guard scan for target/screenshot/contact-sheet runtime references returned no matches.
- `git diff --check` passed for touched SCN-02 UI files after Unity prefab whitespace cleanup.

# Validation result
- 16:9 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_1672x941.png`
- 20:9 capture: `Design/AgentReports/Captures/SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_20x9.png`
- 16:9 comparison MSE: `549.34`
- 20:9 comparison MSE: `586.32`
- Previous comparable R4 MSE: `550.91` 16:9, `587.84` 20:9.
- `WarlineCaptureUiMainMenuTests`: 7 passed, 0 failed.

# Known gaps
- Not target-lock complete.
- Settings button is improved, but the generated settings frame still differs from the exact target chrome thickness/detail.
- Top bar, commander profile, left nav, mode-card frames, operation detail rows, and deploy CTA still do not perfectly match the clean minimal target chrome.
- Some generated icons/frames remain busier and higher-contrast than the mockup.
- The current generated operation art remains more cyan/blue than the target operation panel.

# Cross-lane impacts
- UI did not modify other lane task files or non-SCN-02 screens.
- Art/Atlas is not blocked by this pass, but an exact target-lock chrome layer set would still be required if PM wants visual parity beyond UI placement/tone adjustments.
- If Unity project lock contention returns, UI can use `WarlineCapture-CodexUnity2` or `WarlineCapture-CodexUnity3` as fallback validation workspaces.

# Next recommended task
PM/user visual review of `SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_1672x941.png` and `SCN-02_MainMenu_CleanMatchPassR8_SettingsCentered_20x9.png`. If this remains rejected, route the next UI task as a focused exact-chrome replacement/placement pass for settings, top bar, left nav, mode-card frames, operation rows, and deploy CTA only.
