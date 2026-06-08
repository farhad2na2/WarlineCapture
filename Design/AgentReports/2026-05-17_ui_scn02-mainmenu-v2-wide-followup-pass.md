# Lane
UI

# Task
SCN-02 Main Menu follow-up pass to improve target match after the v2 placement handoff, focused on the 20:9 left-rail and command-feed mismatch.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Shell/UIAspectVariantSwitcher.cs`
- `Assets/Game/Scripts/UI/Shell/UIAspectVariantSwitcher.cs.meta`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_20x9_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-v2-wide-followup-pass.md`

# Contracts touched
- `WarlineCaptureAspectVariantSwitcher` now supports `standardOnlyObjects` and `wideOnlyObjects`.
- `Screen_MainMenu` now hides the standard left rail on wide aspect and shows a dedicated `WideLeftNav` inside `WideAspectOnlyRoot`.
- The wide-only command feed remains in the lower-left target region.
- Standard 16:9 composition remains unchanged from the v2 placement pass.

# User-visible behavior
- On 20:9, the left rail no longer uses the vertically stretched standard rail. It switches to a wide-specific rail with compressed profile/nav layout, leaving clearer room for the lower-left command-feed panel.
- On 16:9, the standard Main Menu presentation remains visually unchanged from the v2 placement pass.
- Route buttons and TMP text remain live runtime UI.

# Validation run
- Unity worker rebuild:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen -logFile /private/tmp/warlinecapture-ui-scn02-v2-wide-followup-build.log`
- 1672x941 capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual -logFile /private/tmp/warlinecapture-ui-scn02-v2-wide-followup-capture.log`
- 20:9 capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 -logFile /private/tmp/warlinecapture-ui-scn02-v2-wide-followup-capture-20x9.log`
- 1672x941 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_1672x941.png --out Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_vs_Target_Comparison.png --label SCN-02_MainMenu_V2WideFollowup`
- 20:9 comparison:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_20x9.png --out Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_20x9_vs_Target_Comparison.png --label SCN-02_MainMenu_V2WideFollowup_20x9`
- Focused tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMainMenuTests -testResults /private/tmp/warlinecapture-ui-scn02-v2-wide-followup-tests-results.xml -logFile /private/tmp/warlinecapture-ui-scn02-v2-wide-followup-tests.log`
- Forbidden runtime asset scan:
  `rg -n "TargetRoot|TargetSlice|target_slice|TargetMatchComposite|SCN02_MainMenu_Landscape_TargetComposite|SCN-02_MainMenu_Landscape_Target|layers_contact_sheet|scn02_complete_production_sprites_contact_sheet|MainMenu_Landscape_Visual_Target" Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo`
- `git diff --check` on touched SCN-02 files.

# Validation result
- Unity worker rebuild passed.
- Fresh 1672x941 capture produced:
  `Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_1672x941.png`
- Fresh 20:9 capture produced:
  `Design/AgentReports/Captures/SCN-02_MainMenu_V2WideFollowup_20x9.png`
- 1672x941 comparison MSE: `1016.42`, unchanged from v2 placement pass and still improved versus previous final-pass `1077.03`.
- 20:9 comparison MSE: `965.32`, improved from v2 placement pass `980.49` and previous final-pass `1043.91`.
- `WarlineCaptureUiMainMenuTests`: `7 passed / 0 failed`.
- Forbidden runtime asset scan returned no matches.
- `git diff --check`: passed.

# Known gaps
- This is still not a target-lock-complete claim.
- 20:9 is improved, but the command-feed panel/icon and left rail still do not exactly match the reference proportions.
- Remaining 16:9 mismatch is mostly unchanged from the prior v2 pass: top bar/chrome weight, card text/footer alignment, deploy glow intensity, and exact art/chrome fidelity.

# Cross-lane impacts
- PM: this is ready for review as another measurable UI improvement pass.
- QA/HCI: should wait for PM acceptance before treating SCN-02 as target-lock ready.
- Art/Atlas: no new missing asset blocker found in this pass.
- Other lanes: no POP-05, SCN-08, Gameplay, or source task files were intentionally modified.

# Next recommended task
PM should review the updated 20:9 capture. If more matching is required, the next UI pass should focus on deploy CTA glow/tone and card footer/body text alignment; if PM rejects remaining chrome/art fidelity, route Art/Atlas only for those exact source-layer deltas.
