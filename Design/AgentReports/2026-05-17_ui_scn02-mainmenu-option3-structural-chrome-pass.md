# Lane
UI

# Task
SCN-02 Main Menu option-3 structural chrome source freeze, extraction, import, prefab rebuild, and focused validation.

# Files changed
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/approved_option3/scn02_option3_structural_chrome_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/approved_option3/scn02_option3_full_layer_pack_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/approved_option3_structural_chrome_extracted_contact_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/{screen_shell_frame,commander_profile_panel_frame,mode_card_frame_large,top_resource_bar_frame_full,left_nav_row_frame,deploy_command_button_frame,command_feed_panel_frame,resource_counter_slot_frame}.png`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/**` via forced SCN-02 manifest copy
- `Assets/Game/Art/UI/Generated/MainMenu/ImageGenFlat/FramesTrimmed/{commander_profile_panel_frame,mode_card_frame,top_resource_bar_frame,left_nav_row_frame,deploy_command_button_frame,command_feed_panel_frame}.png`
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_{CardArt,FramesChrome,IconsButtons}.spriteatlas`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3StructuralChrome_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3StructuralChrome_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3StructuralChrome_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_Option3StructuralChrome_20x9_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 VisualLockLayered manifest layer contract for structural frame PNGs.
- Main Menu prefab sprite binding contract.
- Main Menu atlas/import contract.
- Preserved route buttons, live TMP text/data, and non-target-slice runtime rule.

# User-visible behavior
- Main Menu now uses the approved option-3 structural chrome frames for the key frame pieces instead of the rejected messy/thick chrome pass.
- The screen remains a layered Unity Canvas with live text/buttons, not a full target screenshot or composite overlay.

# Validation run
- `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply --force`
- Unity build: `WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen`
- Unity EditMode: `WarlineCaptureUiMainMenuTests`
- Unity capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual`
- Unity capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9`
- 16:9 comparison: `Tools/UI/compare_ui_capture_to_target.py`
- 20:9 comparison: `Tools/UI/compare_ui_capture_to_target.py`
- Forbidden runtime asset scan for target slices/composites/contact sheets.
- `git diff --check` on touched SCN-02/UI files.

# Validation result
- Forced SCN-02 manifest copy completed: 49 layer files copied.
- Main Menu prefab rebuild completed.
- `WarlineCaptureUiMainMenuTests`: 7 passed / 0 failed.
- 1672x941 capture generated: `Design/AgentReports/Captures/SCN-02_MainMenu_Option3StructuralChrome_1672x941.png`
- 20:9 capture generated: `Design/AgentReports/Captures/SCN-02_MainMenu_Option3StructuralChrome_20x9.png`
- 1672x941 comparison MSE: `614.82`.
- 20:9 comparison MSE: `610.47`.
- Forbidden runtime asset scan returned no matches.
- `git diff --check` passed after normalizing Unity prefab trailing whitespace.

# Known gaps
- This is still not a region-perfect target lock. The screen remains visibly busier and more saturated than the clean minimal chrome target.
- 16:9 MSE improves slightly from the prior imagegen flat-clean pass score cited in-thread (`615.91` to `614.82`).
- 20:9 MSE regresses slightly from the prior imagegen flat-clean pass score cited in-thread (`606.09` to `610.47`), though it remains much better than the older v2 placement baseline (`980.49`).
- Remaining mismatches are mostly UI-owned composition/tone/scale after structural chrome import: top bar still too heavy, card frames/art still too loud, nav/profile chrome still more crowded than the target, deploy CTA still heavier than target.

# Cross-lane impacts
- Art/Atlas does not need to regenerate structural chrome for this option-3 pass unless PM rejects the latest structural sheet.
- QA/HCI can inspect the two fresh captures, but should not mark SCN-02 target-lock complete.

# Next recommended task
UI should do one focused tone/placement cleanup pass using the approved option-3 structural chrome: reduce top-bar/card/nav opacity and glow, tighten target spacing, and reduce card/deploy visual weight before another capture/comparison cycle.
