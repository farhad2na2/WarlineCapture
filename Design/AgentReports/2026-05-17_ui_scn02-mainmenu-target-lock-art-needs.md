# Lane
UI

# Task
End SCN-02 Main Menu UI iteration and report exact Art/Atlas needs for target-lock.

# Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Tests/Editor/UIMainMenuTests.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas`
- `Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas`
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-production-sprite-implementation.md`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9_vs_Target_Comparison.png`

# Contracts touched
- SCN-02 Main Menu runtime composition now uses accepted manifest-declared layers, live TMP text, and real route buttons.
- UI validation contract was updated for the SCN-02 production-sprite hierarchy.
- No target slices, target composites, screenshots, contact sheets, or mockup-derived runtime assets were used.

# User-visible behavior
- Main Menu is closer to the target mockup after three UI-owned iteration passes.
- Latest comparison scores:
  - 1672x941 MSE: `1172.77`
  - 20:9 MSE: `1156.73`
- UI cannot honestly claim target-lock match with the current accepted sprite content.

# Validation run
- Unity build: `WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen`
- Unity captures: `CaptureMainMenuVisual`, `CaptureMainMenuVisual20x9`
- Target comparisons:
  - `Tools/UI/compare_ui_capture_to_target.py` against `SCN-02_MainMenu_Landscape_Target.png`
  - `Tools/UI/compare_ui_capture_to_target.py` against `SCN-02_MainMenu_20x9_Target.png`
- Focused tests: Unity EditMode `WarlineCaptureUiMainMenuTests`
- Forbidden runtime asset scan over `Screen_MainMenu.prefab` and `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo`

# Validation result
- Build passed.
- `WarlineCaptureUiMainMenuTests`: 7 passed / 0 failed.
- Forbidden runtime asset scan: clean.
- Fresh captures and comparisons are available under `Design/AgentReports/Captures/`.
- Remaining mismatch is dominated by Art/Atlas sprite content, not UI geometry alone.

# Exact PM / Art-Atlas needs
Owner lane: Art/Atlas.

UI needs revised SCN-02 manifest layers that visually match the target mockup content, not only valid placeholder-safe production sprites:

| Layer id | Current Unity destination | Needed correction |
| --- | --- | --- |
| `mode_card_art_saga` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_saga.png` | Replace with illustrated city/armored convoy scene matching the Saga Campaign card in `SCN-02_MainMenu_Landscape_Target.png`. Current sprite reads as generic placeholder-style block art. |
| `mode_card_art_operation` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_operation.png` | Replace with blue tactical city-grid/hologram scene matching the Persistent Operation card. Current sprite does not match target content. |
| `mode_card_art_quick_custom` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_quick_custom.png` | Replace with mountain/base/helicopter scene matching the Quick Custom Game card. Current sprite does not match target content. |
| `commander_profile_portrait` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/commander_profile_portrait.png` | Replace with target-style dark commander silhouette/profile scan treatment, or PM-approved final portrait that matches the target region. Current face portrait materially differs from the mockup. |
| `brand_emblem` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/brand_emblem.png` | Replace with the angular target Warline emblem treatment. Current eagle/crest emblem differs from target logo. |
| `icon_credits` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_credits.png` | Replace with stacked coin icon matching the target top resource strip. |
| `icon_materials` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_materials.png` | Replace with stacked crate/materials icon matching the target top resource strip. |
| `icon_command_authority` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_command_authority.png` | Replace with shield/star icon matching the target top resource strip. |
| `designed_unavailable_badge` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/designed_unavailable_badge.png` | Provide target badge treatment with readable two-line Designed Unavailable label and lock plate region matching the mockup. Current badge frame requires UI text overlay and lacks the target lock treatment. |
| `left_nav_icon_inbox` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/left_nav_icon_inbox.png` | Ensure icon silhouette/scale matches target left-nav mail icon. |
| `left_nav_icon_store` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/left_nav_icon_store.png` | Ensure icon silhouette/scale matches target cart icon. |
| `left_nav_icon_events` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/left_nav_icon_events.png` | Ensure icon silhouette/scale matches target calendar/star icon. |
| `left_nav_icon_ranking` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/left_nav_icon_ranking.png` | Ensure icon silhouette/scale matches target ranking bars icon. |
| `left_nav_icon_command_feed` | `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/left_nav_icon_command_feed.png` | Ensure icon silhouette/scale matches target command-feed antenna icon. |

# Known gaps
- UI can continue minor rect, font, and spacing tweaks after Art/Atlas revisions, but another UI-only pass will have diminishing returns.
- Current accepted card art, portrait, brand emblem, and resource icons materially differ from the target pixels.
- A true target-lock claim should wait for revised Art/Atlas layers and one final UI placement/capture pass.

# Cross-lane impacts
- Art/Atlas is the required unblock owner for target-lock parity.
- PM should decide whether current SCN-02 is acceptable as a production-sprite implementation or whether Art/Atlas must revise the listed layers before QA target-lock review.
- UI should not continue autonomous SCN-02 matching until PM accepts this as good enough or Art/Atlas supplies corrected layers.

# Next recommended task
PM dispatch Art/Atlas to revise the listed SCN-02 layers against:
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`

After Art/Atlas handoff, UI should run one final placement pass, rebuild `Screen_MainMenu.prefab`, capture 1672x941 and 20:9, and regenerate comparison images.
