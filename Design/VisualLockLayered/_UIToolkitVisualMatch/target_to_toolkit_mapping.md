# UI Toolkit Target-To-Toolkit Mapping

Purpose:
Map Target Lock reference mockup elements to existing UI Toolkit elements without changing shell or screen structure. This file supports the implement, UI Builder compare, shadow capture, difference classify, and reiterate loop in `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`.

Last updated:
2026-06-21

Scope status:

| Surface | Status | Notes |
| --- | --- | --- |
| SCN-02 Main Menu | In progress | Initial structure and asset mapping recorded. |
| Other surfaces | Not started | Do not begin until SCN-02 is handed to the user for verification and approved. |

## Global Structure Lock

The shell remains owned by `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml`.

| Shell element | Required role | May rename | May remove | May move structurally | Visual tuning allowed |
| --- | --- | --- | --- | --- | --- |
| `MenuBackgroundRegion` | Full-screen menu background slot | No | No | No | Yes |
| `HeaderRegion` | Header slot | No | No | No | Yes |
| `LeftRegion` | Left-side slot | No | No | No | Yes |
| `MiddleRegion` | Primary content slot | No | No | No | Yes |
| `RightRegion` | Right-side slot | No | No | No | Yes |
| `FooterRegion` | Footer slot | No | No | No | Yes |
| `MainMenuScreenSlot` | SCN-02 screen mount | No | No | No | Yes |
| `PopupScreenSlot` | Popup mount | No | No | No | Not in SCN-02 pass |
| `ModalOverlay` | Modal overlay root | No | No | No | Not in SCN-02 pass |

## Current UI Toolkit UXML Inventory

These files are in scope for visual-only UI Toolkit restyling, but SCN-02 is the only active implementation surface until user verification.

| UXML | Current loop status |
| --- | --- |
| `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml` | Structure lock only |
| `Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml` | In progress |
| `Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/SCN03_CommanderProfileContent.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_PassengerItemView.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildCatalogItemView.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionActiveItemView.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionQueueItemView.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml` | Not started |
| `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryItemView.uxml` | Not started |
| `Assets/Game/UI Toolkit/POP05_MissionResultPopup/POP05_MissionResultPopup.uxml` | Not started |
| `Assets/Game/UI Toolkit/POP06_SettingsPopup/POP06_SettingsPopup.uxml` | Needs target before visual-lock claim |
| `Assets/Game/UI Toolkit/POP07_InboxPopup/POP07_InboxPopup.uxml` | Needs target before visual-lock claim |

## SCN-02 Main Menu Reference

Canonical target:
`Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/reference/scn02c_target_lock_warline_capture_bright.png`

Active Toolkit files:

- `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml`
- `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss`

Main imported art folder:
`Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/`

Design layer source:
`Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/layers/`

### SCN-02 Region Structure

| UXML element | Region role | May rename | May remove | May move structurally | Visual tuning allowed |
| --- | --- | --- | --- | --- | --- |
| `SCN02_MainMenuContent` | Screen root | No | No | No | Yes |
| `MenuBackgroundContent` | Background | No | No | No | Yes |
| `HeaderContent` | Header | No | No | No | Yes |
| `LeftContent` | Left | No | No | No | Yes |
| `MiddleContent` | Middle | No | No | No | Yes |
| `RightContent` | Right | No | No | No | Yes |
| `FooterContent` | Footer | No | No | No | Yes |

### SCN-02 Element Mapping

| Target element | Toolkit element | USS class | Sprite/content source | Region | First QA status |
| --- | --- | --- | --- | --- | --- |
| Command table background | `BackgroundArt` | `.background-art` | `scn02c_background_command_table_no_ui.png` | Background | Needs runtime capture comparison |
| Dark readability overlay | `BackgroundArtOverlay` | `.background-art-overlay` | USS color overlay | Background | Needs opacity comparison |
| Header frame | `HeaderBackPlate` | `.header-back-plate` | `scn02c_header_bar_frame.png` | Header | Needs PPU and 9-slice crop check |
| Logo lockup | `Logo` | `.header-logo` | currently `scn01_v04_logo_lockup.png` | Header | Needs sprite-source decision; target layer has `scn02c_brand_logo_lockup.png` |
| Credits resource chip | `CreditsPanel/Frame` | `.resource-frame` | `scn02c_resource_chip_frame.png` | Header | Needs PPU and 9-slice crop check |
| Credits icon | `CreditsPanel/Icon` | `.credits-icon` | `scn02c_resource_crate_icon.png` | Header | Needs scale/centering check |
| Credits value | `CreditsPanel/Value` | `.resource-value` | Runtime text | Header | Needs font size check |
| Supplies resource chip | `SuppliesPanel/Frame` | `.resource-frame` | `scn02c_resource_chip_frame.png` | Header | Needs PPU and 9-slice crop check |
| Supplies icon | `SuppliesPanel/Icon` | `.supplies-icon` | `scn02c_resource_diamond_icon.png` | Header | Needs scale/centering check |
| Supplies value | `SuppliesPanel/Value` | `.resource-value` | Runtime text | Header | Needs font size check |
| Command resource chip | `CommandPanel/Frame` | `.resource-frame` | `scn02c_resource_chip_frame.png` | Header | Needs PPU and 9-slice crop check |
| Command icon | `CommandPanel/Icon` | `.command-icon` | `scn02c_resource_energy_icon.png` | Header | Needs scale/centering check |
| Header inbox button | `InboxButton` | `.header-icon-button` | `scn02c_header_square_button_frame_default.png`, `scn02c_mail_icon.png` | Header | Needs PPU and icon crop check |
| Header settings button | `SettingsButton` | `.header-icon-button` | `scn02c_header_square_button_frame_default.png`, `scn02c_settings_gear_icon.png` | Header | Needs PPU and icon crop check |
| Header menu button | `MenuButton` | `.header-icon-button` | `scn02c_header_square_button_frame_default.png`, `scn02c_menu_hamburger_icon.png` | Header | Needs PPU and icon crop check |
| Left campaign nav selected | `Nav_Campaign` | `.nav-item.nav-item-selected` | `scn02c_nav_button_frame_selected.png`, `scn02c_nav_campaign_target_icon.png`, `scn02c_nav_chevron_icon.png` | Left | Needs selected-state crop check |
| Left armory nav | `Nav_Armory` | `.nav-item` | `scn02c_nav_button_frame_default.png`, `scn02c_nav_armory_ammo_icon.png`, `scn02c_nav_chevron_icon.png` | Left | Needs normal-state crop check |
| Left supply nav | `Nav_Supply` | `.nav-item` | `scn02c_nav_button_frame_default.png`, `scn02c_nav_supply_crate_icon.png`, `scn02c_nav_chevron_icon.png` | Left | Needs normal-state crop check |
| Left command nav | `Nav_Command` | `.nav-item` | `scn02c_nav_button_frame_default.png`, `scn02c_nav_command_shield_icon.png`, `scn02c_nav_chevron_icon.png` | Left | Needs normal-state crop check |
| Left tech tree nav | `Nav_TechTree` | `.nav-item` | `scn02c_nav_button_frame_default.png`, `scn02c_nav_tech_tree_nodes_icon.png`, `scn02c_nav_chevron_icon.png` | Left | Needs normal-state crop check |
| Left profile nav | `Nav_Profile` | `.nav-item` | `scn02c_nav_button_frame_default.png`, `scn02c_nav_profile_tag_icon.png`, `scn02c_nav_chevron_icon.png` | Left | Needs normal-state crop check |
| Campaign card | `Card_Campaign` | `.mode-card.mode-card-selected` | backing/art/frame/label/badge/icon/star Target Lock sprites | Middle | Needs full card crop and PPU/9-slice check |
| Skirmish card | `Card_Skirmish` | `.mode-card.mode-card-blue` | backing/art/frame/label/badge/icon/star Target Lock sprites | Middle | Needs full card crop and PPU/9-slice check |
| Operations card | `Card_Operations` | `.mode-card.mode-card-amber` | backing/art/frame/label/badge/icon/star Target Lock sprites | Middle | Needs full card crop and PPU/9-slice check |
| Commander title panel | `CommanderTitlePanel` | `.commander-section-panel` | nav backing/frame reused | Right | Needs target crop check |
| Commander portrait | `CommanderPortraitPanel/Portrait` | `.commander-portrait` | `scn02c_commander_portrait.png` | Right | Needs scale and crop check |
| Commander identity panel | `IdentityPanel` | `.identity-panel` | nav backing/frame reused | Right | Needs text/spacing check |
| Commander progress panel | `ProgressPanel` | `.commander-progress-panel` | nav backing/frame reused | Right | Needs progress text/strip check |
| Commander readiness panel | `ReadinessPanel` | `.readiness-panel` | nav backing/frame reused | Right | Needs segment spacing check |
| Deploy CTA | `DeployOperationButton` | `.deploy-operation-button` | `scn02c_deploy_button_frame.png`, chevrons, star | Footer | Needs PPU, 9-slice, font, and footer placement crop check |

### SCN-02 First Known Audit Notes

| Area | Observation | First classification | Next action |
| --- | --- | --- | --- |
| Logo | USS uses Splash Loading `scn01_v04_logo_lockup.png`; SCN-02 layer pack includes `scn02c_brand_logo_lockup.png`. | `sprite` | Compare target; likely switch to SCN-02 brand logo if target crop confirms. |
| Frame/chrome PPU | Main SCN-02 frame sprites currently import with `spritePixelsToUnits: 100`. | `PPU` | Do not tune until UI Builder/runtime crop shows chrome too heavy or too thin. |
| Header frame | USS slice values match sprite border values `110/52`; slice scale is `0.28`. | `9-slice` | Check header crop before layout edits. |
| Resource chip | USS slice values match sprite border values `135/82`; slice scale is `0.2`. | `9-slice` | Check chip crop before layout edits. |
| Nav buttons | USS slice values match sprite border values `130/95`; slice scale is `0.22`. | `9-slice` | Check selected/normal nav crops. |
| Mode cards | USS slice values match sprite border values `72/104`; slice scale is `0.32`. | `9-slice` | Check cards for border weight and corner distortion. |
| Deploy button | USS slice left/right match sprite border, but USS top/bottom are `116/96` while meta border is `{x:155,y:96,z:155,w:116}`. | `9-slice` | Verify orientation visually; do not change until crop confirms distortion. |

### SCN-02 Existing Shadow Capture Tooling

Existing C# methods found by reference only; do not edit them in this loop:

- `WarlineCaptureUiPhase1PrefabBuilder.BuildMainMenuScreen`
- `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual`
- `WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9`

Likely shadow command shape, pending sync approval:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 \
  -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual \
  -logFile /private/tmp/warline-ui-target-lock-scn02-shadow-16x9.log
```

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 \
  -executeMethod WarlineCaptureUiPhase1PrefabBuilder.CaptureMainMenuVisual20x9 \
  -logFile /private/tmp/warline-ui-target-lock-scn02-shadow-20x9.log
```

Status:
Capture not run in this slice because syncing current allowed UI Toolkit/art files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` requires approval to write outside the current workspace.
