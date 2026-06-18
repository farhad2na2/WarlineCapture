# UI Toolkit Canvas Phase 0 Inventory

Last updated: 2026-06-18

Progress source: `Design/Architecture/ui_toolkit_canvas_replacement_plan.md`

## Scope

This document is the Phase 0 parity inventory and missing-asset manifest for the UI Toolkit Canvas replacement. It records the current Canvas runtime surfaces, current UI Toolkit counterparts, route/command ownership, and gaps that must be resolved before runtime binding work.

Canvas is the behavior, text, and feature reference only. New UI Toolkit screens must use the new art direction assets and must not pull sprites from the old Canvas art direction.

## Active Canvas Surfaces

| Surface | Canvas prefab | Current runtime ownership | UI Toolkit counterpart | Status |
| --- | --- | --- | --- | --- |
| Shell | `Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab` | `UIShellView`, `UIShellContentView`, `UIShellEcsPresentationSystem` | `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml` | Counterpart exists |
| Loading | `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` | `UIShellContentView.InstallLoading`, shell loading route | `Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uxml` | Counterpart exists |
| Main Menu | `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` | `UIShellContentView.InstallMainMenu`, quick custom bindings, game start bindings | `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml` | Counterpart exists |
| Match HUD | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` | `UIShellContentView.InstallMatchHud`, `MainMenuPlayUI`, selection/build/minimap systems | `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml` | Counterpart exists |
| Build Drawer Popup | `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab` | `UIShellContentView.InstallBuildDrawerPopup`, `BuildDrawerCatalogRuntimeView`, build systems | `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml` | Counterpart exists |
| Build Placement Bar | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab` | `BuildPlacementConfirmationBarView`, building placement systems | `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uxml` | Counterpart exists |
| Armory | `Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab` | `UIShellContentView.InstallArmoryBody`, armory catalog systems | `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml` | Counterpart exists |
| Commander/Profile | `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab` | Main-menu sub-route content | None found under `Assets/Game/UI Toolkit` | Missing UI Toolkit surface |
| Mission Result | `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab` | Popup/result route flow | None found under `Assets/Game/UI Toolkit` | Missing UI Toolkit surface |

## Runtime Binding Inventory

Current Canvas shell behavior is centered in managed MonoBehaviour classes:

- `UIShellView`: runs Canvas region transitions and executes presentation command sequences.
- `UIShellContentView`: installs Canvas prefabs/sections, binds runtime dependencies, owns `Update()` for command control refresh, opens/closes build drawer, and wires build placement, match HUD, armory, minimap, selection, and popup close behavior.
- `UIShellEcsPresentationSystem`: MonoBehaviour edge that reads `UiShellRuntimeGateway` presentation commands and calls `UIShellView.ExecuteCommandSequence`.
- `MainMenuPlayUI`: managed UI runtime boundary for minimap update, feedback lifetime, pointer blocking, command state, build drawer visibility, selection panel binding, squad tray binding, and build placement bar binding.
- `UiShellBoundarySystem`, `UiShellFlowSystem`, and `UiShellArmoryCategorySystem`: existing ECS shell data and route/request flow. These are the right direction for request/read-model ownership.

Migration rule:

- Do not copy `UIShellContentView.Update()` or `MainMenuPlayUI.Update()` behavior into UI Toolkit views.
- Move calculations and route/request state into ECS read-model/request systems where practical.
- Keep only a thin managed UI Toolkit apply edge for `VisualElement` mutation.

## Surface Parity Checklists

### Shell

Must preserve:

- Safe area root, header/content/footer/modal/loading/tooltip layers.
- Loading layer above menus and popups when active.
- Popup scale show/hide.
- Header/left/right/footer slide motions and middle scale motions.
- UI pointer blocking before world input.
- Route requests through ECS, not direct gameplay mutation.

Current UI Toolkit names found:

- `SafeAreaRoot`, `HeaderBar`, `ContentRoot`, `MenuBackgroundRegion`, `LoadingLayer`, `HeaderRegion`, `LeftRegion`, `MiddleRegion`, `RightRegion`, `FooterRegion`, `ModalOverlay`, `TooltipLayer`.

Initial gaps:

- No `RuntimeUiConfig` mode switch found yet.
- No UI Toolkit managed apply system found yet.
- UI Toolkit shell is static UXML only; runtime mounting and pointer gate are not wired.

### Loading

Must preserve:

- Logo, command-system label, loading status, percent, progress track/fill/ticks, spinner, bottom status text.
- Fake progress starts at `0`, reaches `100`, then routes onward when enabled.
- Initial loading can be disabled by config.
- Loading remains topmost over menus and popups while active.

Current UI Toolkit names found:

- `Brand_LogoLockup`, `CommandSystem_Text`, `LoadingPanel_Status`, `LoadingPanel_Percent`, `Progress_Frame`, `Progress_Fill`, `BottomStatus_Spinner`, `BottomStatus_Text`.

Initial gaps:

- Needs ECS loading progress read model binding.
- Needs initial-loading-disable config parity.
- Needs runtime topmost behavior validation.

### Main Menu

Must preserve:

- Persistent header, resources, inbox/settings/menu actions.
- Left navigation routes: campaign, armory, supply, command, tech tree, profile.
- Middle mode cards and deploy action.
- Commander/profile click route and header persistence during sub-screen swaps.
- Tab selected state from ECS/read model.

Current UI Toolkit names found:

- `HeaderContent`, `CreditsPanel`, `SuppliesPanel`, `CommandPanel`, `InboxButton`, `SettingsButton`, `MenuButton`, `Nav_Campaign`, `Nav_Armory`, `Nav_Supply`, `Nav_Command`, `Nav_TechTree`, `Nav_Profile`, `Card_Campaign`, `Card_Skirmish`, `Card_Operations`, `CommanderPanel`, `DeployOperationButton`.

Initial gaps:

- Needs route/request binding for all buttons.
- Needs current resource/readiness read model binding.
- Needs profile route to swap left/middle/right while preserving header.

### Match HUD

Must preserve:

- Header logo/current order/resource strip/menu.
- Objectives panel and elapsed timer.
- Selected squad panel hidden at match init, shown for selected unit/building/squad/mixed selection.
- Selected squad title, subtitle, portrait, badge, health, order, return/destroy/board actions, and passenger drawer.
- Five squad tray buttons with portraits and selected state.
- Command rail actions: Select, Move, Attack, Hold, Stop, Build, Scan, Support.
- Build drawer opens from command rail or right rail; Build remains selected while popup is open.
- Select toggles selection mode and deselects when drag selection completes.
- Runtime feedback panel and action buttons.
- Right rail, threat/jump panel, minimap, zoom/focus controls.
- UI clicks block world selection under the UI.

Current UI Toolkit names found:

- `SelectedSquadPanel`, `ReturnButton`, `DestroyButton`, `BoardButton`, `PassengerChip`, `TransportPassengerDrawer`, `ExitAllButton`, `SquadCard1` through `SquadCard5`, `SelectCommand`, `MoveCommand`, `AttackCommand`, `HoldCommand`, `StopCommand`, `BuildCommand`, `ScanCommand`, `SupportCommand`, `FeedbackPanel`, `BoardAllButton`, `CancelButton`, `MinimapPanel`, `ZoomIn`, `ZoomOut`, `ZoomFocus`, `RightBuildCommand`, `RightSupportCommand`, `JumpButton`, `PauseButton`, `SettingsButton`.

Initial gaps:

- Needs command request/read-model split before callbacks are wired.
- Needs retained passenger row binding from ECS read model.
- Needs feedback action parity and pointer blocking parity.
- Needs validation that command rail visual state does not use old-art assets.

### Build Drawer Popup

Must preserve:

- Popup close only closes popup, never routes to main menu.
- Catalog tabs, catalog scroll, production queue scroll, active production row, build/rush/clear/close actions.
- Build button remains selected while popup is open.
- Scroll bars hidden where the Canvas target hides them.
- Catalog item rows and queue rows retained/reused, not recreated every refresh.

Current UI Toolkit names found:

- `AircraftsTab`, `VehiclesTab`, `SoldiersTab`, `BuildingsTab`, `CatalogScrollView`, `ItemView` templates, `ProductionActiveItemView`, `ProductionItemView`, `BuildButton`, `RushButton`, `ClearButton`, `CloseButton`.

Initial gaps:

- Needs ECS build request and production request binding.
- Needs retained row pool implementation.
- Needs popup ownership so close clears Build selected state without route mutation.

### Build Placement Confirmation Bar

Must preserve:

- Title, valid/invalid status, cost, duration, cancel, rotate, confirm, instruction.
- Blocks world input over its rect.
- Appears only during active placement.
- Does not overlap command rail.

Current UI Toolkit names found:

- `Title`, `Status`, `Cost`, `Duration`, `CancelButton`, `RotateButton`, `ConfirmButton`, `Instruction`.

Initial gaps:

- Needs building placement request bindings.
- Needs pointer block integration.
- Needs active placement read model.

### Armory

Must preserve:

- Persistent header resources and menu action.
- Left tabs: units, vehicles, aircraft, buildings, upgrades.
- Filter/sort controls.
- Roster/item scroll with default, selected, locked, rarity, portrait, title, state, level, type, stat segments.
- Right details panel with selected item, stats, upgrade/equip/close actions.
- Bottom tabs.

Current UI Toolkit names found:

- `Nav_Units`, `Nav_Vehicles`, `Nav_Aircraft`, `Nav_Buildings`, `Nav_Upgrades`, `FilterDropdown`, `SortDropdown`, `Scroll_View`, `ItemView`, `ItemView_FastApc`, `ItemView_ReconDrone`, `ItemView_BombSuit`, `ItemView_HeavyTank`, `ItemView_AttackHelicopter`, `ItemView_RocketArtillery`, `ItemView_SniperTeam`, `UpgradeButton`, `EquipButton`, `CloseButton`, `ArmoryTab`, `WorkshopTab`, `DoctrineTab`, `DepotTab`, `OfficersTab`.

Initial gaps:

- Needs item template/read-model binding.
- Needs selected/default/locked USS state binding.
- Needs details panel binding against current Canvas behavior.
- Reference metadata says blocked in `reference/README.md` even though `SCN-19_Armory_NewMainMenuArtDirection_TargetLock_V04.png` exists. Resolve this metadata mismatch before closing the Armory visual gate.

### Commander/Profile

Must preserve:

- Main menu header remains persistent.
- Back action returns to main menu content.
- Tabs, portrait, identity, badge, stats, rewards/history/armory actions.

Initial gaps:

- No UI Toolkit surface found.
- No new-art reference was confirmed in this pass.
- Add to missing-new-asset manifest before Phase 8.

### Mission Result And Other Popups

Must preserve:

- Result/victory/loss popup center scale show/hide.
- Confirm result routes through loading back to main menu.
- Loading covers popups during route transitions.

Initial gaps:

- No UI Toolkit result popup found.
- No new-art result popup reference confirmed in this pass.
- Settings/mail popups also need explicit parity inventory before implementation.

## New-Art Static URL Audit

Static scan run on 2026-06-18:

- Checked every `.uxml` and `.uss` under `Assets/Game/UI Toolkit`.
- Counted `236` total USS `url(...)` references.
- Found `0` references matching the known old-art markers:
  - `Generated/MainMenu/LayeredOneGo`
  - `Generated/MainMenuAlt`
  - `Art/UI/Final`
  - `VisualLockLayered/SCN-02_MainMenu`
  - `TargetLockV01`
  - `LegacyVisualLock`
  - `Generated/MatchHUD/TargetLockV01`

Enforced validation:

- `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs` enforces UI Toolkit UXML import, USS import, USS `url(...)` reference resolution, old-art marker blocking, `RuntimeUiConfig` default mode, Canvas fallback smoke, and isolated UI Toolkit shell smoke.
- Unity batch validation passed with `[UiToolkitCanvasMigrationValidation] result=Passed tests=7` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Future runtime sprite binding assets still need to be added to this same validation contract once those assets exist.

## Missing-New-Asset Manifest

| Surface | Missing or unresolved new-art item | Required before |
| --- | --- | --- |
| Commander/Profile | UI Toolkit reference mockup, separated layers/sprites, UXML/USS counterpart | Phase 8 runtime binding |
| Mission Result/Victory/Loss | UI Toolkit reference mockups, separated layers/sprites, UXML/USS counterparts | Phase 9 runtime binding |
| Settings/Mail popups | New-art target references and UI Toolkit surfaces if they remain active routes/actions | Phase 9 runtime binding |
| Armory | Reference README status says blocked while PNG exists; metadata must be corrected or regenerated | Phase 7 visual gate |
| Build Placement Bar | Reference README status says blocked while PNG exists; metadata must be corrected or regenerated | Phase 6 visual gate |
| All Toolkit runtime bindings | Future runtime sprite refs must be added to `UiToolkitCanvasMigrationValidationTests` when those binding assets exist | Screen runtime binding phases |

## Phase 0 Result

Complete. Continue with Phase 1 by mounting `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml` through a `UIDocument`.
