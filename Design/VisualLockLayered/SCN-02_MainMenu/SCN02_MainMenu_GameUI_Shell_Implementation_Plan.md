# SCN02 Main Menu GameUI Shell Implementation Plan

Date: 2026-05-27
Status: Structural pass complete. Visual target-lock pass is still pending because the current GameUI capture does not yet match the target mockup quality.

## Goal

Rebuild `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` so the GameUI main menu matches the active layered target mockup while still behaving like a real responsive shell prefab.

The end result must be one prefab that scales correctly across 16:9, 20:9, and wider landscape aspect ratios. It must not be a flat scene-space recreation. Each visual element must belong to its owning shell region or panel, with local anchors, local pivots, and child transforms that make sense.

The main menu header and command-base background are persistent main-menu frame elements. They should stay unchanged while navigating between main-menu routes such as Main Menu, Commander Profile, Store, Settings, or future menu pages. They should disappear only when leaving menu mode, such as entering the match HUD.

## Progress Tracker

Use this section as the implementation checklist. Change `[PENDING]` to `[COMPLETE]` only after the step is implemented and verified.

- [x] `[COMPLETE]` Step 01 - Confirm active source files are `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`, `layers/`, and `layer_manifest.json`.
- [x] `[COMPLETE]` Step 02 - Add source guards so SCN02 generation cannot silently use old generated main menu mockups, generated Unity scenes, or rejected V15B assets.
- [x] `[COMPLETE]` Step 03 - Add a typed SCN02 layer asset map for every required header, background, nav, card, commander, and CTA sprite.
- [x] `[COMPLETE]` Step 04 - Add `MenuBackgroundRegion` or equivalent full-shell persistent menu background region behind header, left, middle, and right regions.
- [x] `[COMPLETE]` Step 05 - Update `WarlineCaptureShellContentPresenterView` so `EnterMenu` installs the menu background and header once, while body route changes do not clear or reinstall them.
- [x] `[COMPLETE]` Step 06 - Update match HUD entry so it clears or hides the menu background and replaces the menu header with match HUD header content.
- [x] `[COMPLETE]` Step 07 - Build `MenuBackgroundContent` using `scn02_background_art.png` as full-shell cover/crop art.
- [x] `[COMPLETE]` Step 08 - Rebuild `HeaderContent` from the active target layers, with logo, resources, command panel, inbox, and settings all parented under local header panels.
- [x] `[COMPLETE]` Step 09 - Rebuild `LeftContent` with `LeftNavPanel`, local nav rows, icons, TMP labels, hotspots, and bottom comms/status panel.
- [x] `[COMPLETE]` Step 10 - Rebuild `MiddleContent` with a centered `ModeCardsContainer` and three local mode card panels for Campaign, Operations, and Skirmish.
- [x] `[COMPLETE]` Step 11 - Rebuild each mode card so frame, thumbnail viewport, masked wide art, icon, TMP text, progress, and hotspot are children of the card root.
- [x] `[COMPLETE]` Step 12 - Rebuild `RightContent` with `CommanderPanel`, portrait panel, readiness row, locked rows, commander hotspot, and deploy CTA.
- [x] `[COMPLETE]` Step 13 - Preserve route compatibility paths such as `RightContent/CommanderPortraitButton` and any existing deploy button path until runtime code is intentionally updated.
- [x] `[COMPLETE]` Step 14 - Keep `SCN01_LoadingContent.prefab` excluded from regeneration and confirm it is untouched.
- [x] `[COMPLETE]` Step 15 - Run prefab structure validation: only expected section roots at the prefab root, no flat screen-space visual hierarchy, and children owned by their local panels.
- [x] `[COMPLETE]` Step 16 - Run transform validation: centered frames/icons/art use correct local pivots and anchors, and no implementation sprite references the target reference PNG or old generated mockup folders.
- [x] `[COMPLETE]` Step 17 - Open `GameUI.unity` only in the shadow sibling project `D:\Projects\WarlineCapture-CodexUnity1`, not the main Unity project, and capture the main menu at 1920x1080, 2400x1080, 3840x2160, and 4800x2160.
- [x] `[COMPLETE]` Step 18 - Verify header and background stay unchanged while navigating main-menu routes.
- [x] `[COMPLETE]` Step 19 - Verify match HUD hides the menu background and uses match HUD header/content instead of the main menu frame.
- [x] `[COMPLETE]` Step 20 - Compare the 1920x1080 capture against the active target mockup for composition, palette, panel ownership, and readability.
- [x] `[COMPLETE]` Step 21 - Save screenshots and notes under `Design/AgentReports/Captures/GameUI/MainMenu/`.
- [x] `[COMPLETE]` Step 22 - Mark completed tracker items and list any remaining visual differences or follow-up decisions.

## Visual Target-Lock Pass Tracker

This pass is required because the first implementation created the correct shell ownership and persistence model, but the current `GameUI_MainMenu_Stable.png` still looks visually misaligned compared with `reference/SCN-02_MainMenu_Landscape_Target.png`.

- [ ] `[PENDING]` Visual Step 01 - Rework header layout so it reads like the target: continuous top command bar, larger readable resource text, correct logo/resource/action proportions.
- [ ] `[PENDING]` Visual Step 02 - Rework left navigation row scale, icon placement, label typography, selected-state proportions, and comms panel to match the target.
- [ ] `[PENDING]` Visual Step 03 - Rework mode card size and vertical layout so cards are not oversized empty panels and match target thumbnail/text/progress composition.
- [ ] `[PENDING]` Visual Step 04 - Rework mode card thumbnail masks and progress/footer areas so thumbnail art, labels, and progress rows are locally contained and readable.
- [ ] `[PENDING]` Visual Step 05 - Rework commander panel proportions, portrait containment, readiness row, locked rows, and deploy CTA so the right side matches the target.
- [ ] `[PENDING]` Visual Step 06 - Rebuild SCN02 prefab through the shadow project only and capture 1920x1080, 2400x1080, 3840x2160, and 4800x2160.
- [ ] `[PENDING]` Visual Step 07 - Compare the new 1920x1080 capture against the target mockup and update the verification report with before/after notes.
- [ ] `[PENDING]` Visual Step 08 - Mark visual pass complete only if the GameUI capture is visually close enough to the target mockup, not merely structurally valid.

## Source Of Truth

Use these active target sources:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`

The reference PNG is only for visual comparison. It must not be cut, cropped, masked, or used as implementation sprite art. Implementation sprites must come from `layers/` and the approved source data described by `layer_manifest.json`.

Do not use these as source of truth:

- `Assets/Game/Art/UI/Generated/MainMenu`
- `Assets/Game/Art/UI/Generated/MainMenuAlt`
- Generated Unity scenes or prefabs for SCN02, including previous 16:9 or 20:9 converted scenes
- Rejected V15B layer requests
- Any flat hierarchy conversion where everything is anchored to the screen or top-left canvas

## Existing Runtime Shell Contract

The GameUI shell uses a 4800x2160 reference resolution.

Current shell regions:

| Region | Local top-left rect | Purpose |
| --- | --- | --- |
| `HeaderRegion` | `x=0, y=0, w=4800, h=280` | Global menu header, logo, resources, inbox/settings |
| `LeftRegion` | `x=0, y=280, w=720, h=1640` | Left navigation and comms/status |
| `MiddleRegion` | `x=720, y=280, w=3360, h=1640` | Route-specific middle menu content, such as main mode cards |
| `RightRegion` | `x=4080, y=280, w=720, h=1640` | Commander panel and deploy CTA |
| `FooterRegion` | `x=0, y=1920, w=4800, h=240` | Not required for this first SCN02 menu pass |

Add a persistent menu background layer to the shell:

| Region | Local top-left rect | Purpose |
| --- | --- | --- |
| `MenuBackgroundRegion` or `MenuFrameBackgroundRegion` | `x=0, y=0, w=4800, h=2160` | Main-menu-only command-base background, rendered behind header/left/middle/right regions |

This is better than making the background a child of `HeaderContent`. A header child that extends outside the header region works only by accident and makes layering harder to reason about. A dedicated background region is semantically correct: it is persistent like the header, full-screen like the target art, and easy to hide when the match HUD appears.

`SCN02_MainMenuContent.prefab` should expose exactly the expected section roots for the presenter:

- `MenuBackgroundContent`
- `HeaderContent`
- `LeftContent`
- `MiddleContent`
- `RightContent`

The presenter stretches each section root into its matching shell region. The prefab contents inside those section roots must therefore use local coordinates relative to the section, not absolute screen coordinates.

## Responsive Strategy

Build one responsive prefab, not separate prefabs or scenes per aspect ratio.

Use the shell regions as the stable outer layout:

- Menu background is anchored to the full shell and rendered behind all main-menu regions.
- Header is anchored to the top and spans the full shell width.
- Left navigation is anchored to the left edge under the header.
- Right commander area is anchored to the right edge under the header.
- Middle content fills the space between left and right.
- Footer remains unused unless a later pass moves comms or global actions there.

Persistence behavior:

- `EnterMenu` installs `MenuBackgroundContent` and `HeaderContent`.
- Main-menu route navigation swaps only `LeftContent`, `MiddleContent`, and `RightContent`.
- Header is not rebuilt for every route.
- Background is not rebuilt for every route.
- `EnterMatchHud` clears or hides `MenuBackgroundRegion`, replaces the header with match HUD header content, and installs match HUD regions.
- Loading/splash can remain independent and should not modify the hand-corrected loading prefab.

Inside each shell region:

- Fixed-edge panels keep their size and edge anchors.
- Center content may stretch or reveal more art horizontally.
- Wide art uses cover/crop behavior, never non-uniform stretch.
- Any child image centered inside a panel should use anchor `(0.5, 0.5)`, pivot `(0.5, 0.5)`, anchored position `(0, 0)`, and a size controlled by the parent panel's local layout.

Target aspect checks:

- 16:9: `1920x1080`, `3840x2160`
- 20:9: `2400x1080`, `4800x2160`
- 21:9 or similar ultrawide: enough to confirm header right actions and side rails stay stable

## Layer Mapping

Use these layer roles from `layers/`:

| Layer | Owning area | Notes |
| --- | --- | --- |
| `scn02_background_art.png` | `MenuBackgroundContent/BackgroundViewport` | Persistent main-menu background. Cover/crop full shell. Do not stretch. Hidden when match HUD appears. |
| `scn02_header_logo_panel_bg.png` | `HeaderContent/HeaderLogoPanel` | Left header panel background. |
| `scn02_brand_logo_lockup.png` | `HeaderContent/HeaderLogoPanel/Logo` | Child of logo panel, centered/scaled locally. |
| `scn02_header_resource_panel_bg.png` | `HeaderContent/HeaderResourceArea` | Resource panel frame or repeated local panels depending on visual match. |
| `scn02_resource_coin_badge.png` | Header resource item | Icon child of its resource item. |
| `scn02_resource_supplies_crate.png` | Header resource item | Icon child of its resource item. |
| `scn02_resource_command_shield.png` | Header command item | Icon child of command item. |
| `scn02_header_command_panel_bg.png` | Header command item | Separate panel if the target shows command as a different visual surface. |
| `scn02_header_right_actions_bg.png` | `HeaderContent/HeaderActionsPanel` | Right anchored panel for inbox and settings. |
| `scn02_icon_inbox_envelope.png` | Header inbox button | Icon child of local button. |
| `scn02_icon_settings_gear.png` | Header settings button | Icon child of local button. |
| `scn02_nav_button_selected_frame.png` | Left selected nav item | Campaign selected state for initial menu. |
| `scn02_nav_button_inactive_frame.png` | Left inactive nav items | Operations, Skirmish, Store, Commander, Settings. |
| `scn02_icon_campaign_crosshair.png` | Campaign nav/card | Icon child of local route row or card. |
| `scn02_icon_operations_pin.png` | Operations nav/card | Icon child of local route row or card. |
| `scn02_icon_skirmish_blades.png` | Skirmish nav/card | Icon child of local route row or card. |
| `scn02_icon_store_cart.png` | Store nav | Icon child of local route row. |
| `scn02_icon_commander_bust.png` | Commander nav/right panel | Icon child of local row/panel. |
| `scn02_comms_status_panel_frame.png` | `LeftContent/CommsStatusPanel` | Bottom-left status panel unless later moved to footer. |
| `scn02_mode_card_frame.png` | Each mode card | Card frame parented under `ModeCardsContainer`. |
| `scn02_mode_card_thumbnail_mask_frame.png` | Each mode card thumbnail viewport | Visual frame around masked art. |
| `scn02_campaign_thumbnail_art.png` | Campaign card art | Wide art inside masked viewport, cover/crop. |
| `scn02_operations_thumbnail_art.png` | Operations card art | Wide art inside masked viewport, cover/crop. |
| `scn02_skirmish_thumbnail_art.png` | Skirmish card art | Wide art inside masked viewport, cover/crop. |
| `scn02_mode_progress_meter_frame.png` | Mode card progress | Child of card footer. |
| `scn02_commander_panel_frame.png` | `RightContent/CommanderPanel` | Right panel frame. |
| `scn02_commander_portrait_frame.png` | `CommanderPanel/PortraitPanel` | Child of commander panel. |
| `scn02_commander_portrait_art.png` | `CommanderPanel/PortraitPanel/Portrait` | Centered/cropped locally inside portrait frame. |
| `scn02_readiness_segments.png` | Commander readiness row | Child of commander panel. |
| `scn02_locked_row_frame.png` | Locked feature rows | Child of commander panel rows container. |
| `scn02_icon_lock.png` | Locked rows | Icon child of locked row. |
| `scn02_deploy_cta_frame.png` | `RightContent/DeployOperationButton` | Bottom-right CTA frame. |
| `scn02_deploy_chevrons.png` | Deploy CTA | Local decorative child of CTA. |
| Trim sprites | Local panel decoration | Use only as children of the panel they decorate. |

## Required Prefab Hierarchy

The implementation should follow this shape. Names may be adjusted only if existing routing code requires a specific path.

```text
SCN02_MainMenuContent
  MenuBackgroundContent
    BackgroundViewport
      BackgroundArt
  HeaderContent
    HeaderLogoPanel
      Frame
      Logo
    HeaderResourceArea
      CreditsPanel
        Frame
        Icon
        Label
        Value
      SuppliesPanel
        Frame
        Icon
        Label
        Value
      CommandPanel
        Frame
        Icon
        Label
        Value
    HeaderActionsPanel
      Frame
      InboxButton
        Icon
        Hotspot
      SettingsButton
        Icon
        Hotspot
  LeftContent
    LeftNavPanel
      Nav_Campaign
        Frame
        Icon
        Label
        Hotspot
      Nav_Operations
        Frame
        Icon
        Label
        Hotspot
      Nav_Skirmish
        Frame
        Icon
        Label
        Hotspot
      Nav_Store
        Frame
        Icon
        Label
        Hotspot
      Nav_Commander
        Frame
        Icon
        Label
        Hotspot
      Nav_Settings
        Frame
        Icon
        Label
        Hotspot
    CommsStatusPanel
      Frame
      StatusIcon
      Label
  MiddleContent
    ModeCardsContainer
      CampaignCard
        Frame
        ArtViewport
          Art
          MaskFrame
        Icon
        Title
        Description
        Progress
          Frame
          Value
        Hotspot
      OperationsCard
        Frame
        ArtViewport
          Art
          MaskFrame
        Icon
        Title
        Description
        Progress
          Frame
          Value
        Hotspot
      SkirmishCard
        Frame
        ArtViewport
          Art
          MaskFrame
        Icon
        Title
        Description
        Progress
          Frame
          Value
        Hotspot
  RightContent
    CommanderPanel
      Frame
      PortraitPanel
        Frame
        Portrait
      Header
      Name
      Rank
      Readiness
        Label
        Segments
      LockedRowsContainer
        SquadManagementRow
          Frame
          LockIcon
          Label
        IntelReportRow
          Frame
          LockIcon
          Label
    CommanderPortraitButton
      Hotspot
    DeployOperationButton
      Frame
      Chevrons
      Label
      Hotspot
```

Keep compatibility aliases if runtime code already expects them. For example, if validation or routing expects `RightContent/DeployCommandButton`, preserve that object or make it an alias/child wrapper around the final `DeployOperationButton`.

## Anchor And Pivot Rules

Section roots:

- `MenuBackgroundContent`: stretch to the full persistent menu background region, anchor min `(0, 0)`, anchor max `(1, 1)`, pivot `(0.5, 0.5)`, offset min/max zero.
- `HeaderContent`: stretch to parent region, anchor min `(0, 0)`, anchor max `(1, 1)`, pivot `(0.5, 0.5)`, offset min/max zero.
- `LeftContent`: stretch to parent region, anchor min `(0, 0)`, anchor max `(1, 1)`, pivot `(0.5, 0.5)`, offset min/max zero.
- `MiddleContent`: stretch to parent region, anchor min `(0, 0)`, anchor max `(1, 1)`, pivot `(0.5, 0.5)`, offset min/max zero.
- `RightContent`: stretch to parent region, anchor min `(0, 0)`, anchor max `(1, 1)`, pivot `(0.5, 0.5)`, offset min/max zero.

Panel roots:

- Edge panels use edge anchors on their section root.
- Centered panels use center anchors on their section root or local container.
- A panel's frame is a child of that panel and is centered locally.
- Do not place a frame and its labels/icons as siblings under the section root when they are visually one panel.

Common child transform:

- Frame, icon, art, and text objects inside a panel should usually use pivot `(0.5, 0.5)`.
- If centered within the owning panel, they should use anchored position `(0, 0)` relative to that panel or to a named local slot.
- Text may use stretch anchors only inside a local text slot, not across unrelated parent panels.

Image rules:

- Use `Image.preserveAspect = true` for icon and frame sprites unless a nine-slice sprite is explicitly configured.
- Use cover/crop for background and thumbnail art.
- Wide thumbnail art must be inside `ArtViewport` with `RectMask2D` or `Mask`.
- Do not scale thumbnail art non-uniformly to fill card windows.

Button/hotspot rules:

- Transparent hit zones are children of the local visual control.
- A route button's clickable rect should match the visual panel, not an unrelated screen-space area.
- Keep the existing expected route paths until code is updated deliberately.

## Implementation Steps

### 1. Freeze Active Inputs

Confirm the implementation script and prefab generation path uses only the active SCN02 layered target inputs:

- `Design/VisualLockLayered/SCN-02_MainMenu/layers`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`

Add a short warning comment in the editor builder near the SCN02 source path that generated SCN02 Unity scenes and old generated mockup folders are not source of truth.

### 2. Build A Layer Asset Map

Create a typed lookup in the editor generation code for every layer required by this menu. Fail loudly if a required layer is missing.

Required groups:

- Header: logo panel, resource panel, command panel, right actions panel, brand logo, resource icons, inbox/settings icons
- Left: selected/inactive nav frames, route icons, comms frame
- Middle: background art, mode frames, thumbnail frame, mode thumbnail art, progress meter
- Right: commander frame, portrait frame, portrait art, readiness, locked row, lock icon, deploy frame, deploy chevrons

This prevents silent fallback to old mockups.

### 3. Normalize Local Coordinate Helpers

Add or update editor helper methods so all objects are created relative to a parent:

- `CreateSectionRoot(parent, name)`
- `CreatePanel(parent, name, anchorPreset, size, anchoredPosition)`
- `AddCenteredSprite(parent, name, sprite, size)`
- `AddCoverSprite(parent, name, sprite, viewportSize)`
- `AddText(parent, name, text, slotRect, style)`
- `AddHotspot(parent, name, slotRect, routeId)`
- `AddMaskedViewport(parent, name, size)`

These helpers should always set:

- `localScale = Vector3.one`
- predictable anchors
- predictable pivot
- no inherited screen-space offsets
- sibling order controlled by visual layering

### 4. Add Persistent Menu Background

Add a shell-level background region behind the existing content regions.

Presenter behavior:

- `InstallMainMenu()` installs `MenuBackgroundContent` into `MenuBackgroundRegion`.
- `InstallMenuRouteBody()` does not touch `MenuBackgroundRegion`.
- `InstallCommanderProfileBody()` and future menu body installers do not touch `MenuBackgroundRegion`.
- `InstallMatchHud()` clears `MenuBackgroundRegion`.

Background content:

- `MenuBackgroundContent` stretches to the full 4800x2160 background region.
- `BackgroundViewport` stretches to `MenuBackgroundContent`.
- `BackgroundArt` uses `scn02_background_art.png`.
- `BackgroundArt` uses cover/crop behavior so 21:9 art crops inward on 20:9 and 16:9.
- Do not place mode cards, nav, commander panel, or route controls inside this background layer.

### 5. Rebuild HeaderContent

Create the header as local panels inside `HeaderContent`.

Header layout intent:

- `HeaderLogoPanel` anchored left.
- `HeaderActionsPanel` anchored right.
- Resource and command panels fill the space between them without drifting over the edges.
- Right actions remain stable on 20:9 and 21:9.

Header child rules:

- `Logo` is child of `HeaderLogoPanel`, centered/scaled locally.
- Credits, Supplies, and Command are separate local items under `HeaderResourceArea`.
- Inbox and Settings buttons are children of `HeaderActionsPanel`.
- Header text is TMP and runtime-bindable, not baked into sprites.
- `HeaderContent` is installed when entering menu mode and remains unchanged when only the menu body route changes.

### 6. Rebuild LeftContent

Create a `LeftNavPanel` that owns all left navigation rows.

Initial state:

- Campaign selected.
- Operations, Skirmish, Store, Commander, Settings inactive.

Each nav row:

- Has its own local root.
- Frame is centered inside the row.
- Icon and label are children of the row.
- Hotspot is a child of the row and matches the row size.

Place `CommsStatusPanel` at the bottom of `LeftContent` unless the shell gains a footer-specific status area later.

### 7. Rebuild MiddleContent

Mode cards:

- Create `ModeCardsContainer` centered in `MiddleContent`.
- Create three card roots: `CampaignCard`, `OperationsCard`, `SkirmishCard`.
- Each card owns its frame, art viewport, icon, title, description, progress, and hotspot.
- Thumbnail art is a child of the card's `ArtViewport`.
- Thumbnail frame is a child of the same card, above the art.

Responsive behavior:

- Card spacing can scale slightly with middle width.
- Card size should remain visually stable and readable.
- At wider aspects, thumbnail art can reveal more width, while frame proportions remain stable.

### 8. Rebuild RightContent

Create `CommanderPanel` anchored near the top of `RightContent`.

Commander panel:

- Frame centered inside panel root.
- Portrait frame is child of commander panel.
- Portrait art is child of portrait frame or portrait viewport.
- Readiness segments are child of a local readiness row.
- Locked rows are children of `LockedRowsContainer`.

Create `DeployOperationButton` anchored bottom-right within `RightContent`.

Deploy button:

- Frame is local child.
- Chevrons are local child.
- Label is TMP and says `DEPLOY OPERATION`.
- Hotspot matches button size.

Preserve or intentionally adapt existing expected route object paths:

- `RightContent/CommanderPortraitButton`
- `RightContent/DeployCommandButton` if existing code still expects it

### 9. Preserve Loading Prefab

Do not regenerate or modify:

- `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab`

Keep the editor generation exclusion that skips SCN01 loading content.

### 10. Validate Prefab Structure

Add or run validation after generation:

- Root has the expected section children: `MenuBackgroundContent`, `HeaderContent`, `LeftContent`, `MiddleContent`, `RightContent`.
- No visual implementation child is directly under `SCN02_MainMenuContent` except section roots.
- No persistent background child is under `HeaderContent`, `LeftContent`, `MiddleContent`, or `RightContent`.
- No mode card child is directly under `MiddleContent` unless it is the card container.
- No commander portrait/image/button child is outside `RightContent`.
- No nav row child is outside `LeftContent`.
- No header resource/action child is outside `HeaderContent`.
- Common centered images have pivot `(0.5, 0.5)`.
- Centered frame children have anchored position near `(0, 0)` within their owning panel.
- No sprite reference points to old generated main menu/mockup folders.
- No sprite reference uses the target reference PNG as implementation art.
- Menu route body swaps do not clear or reinstall `MenuBackgroundRegion`.
- Match HUD entry clears or hides `MenuBackgroundRegion`.

### 11. Validate Aspect Ratios In Unity

Use the shadow Unity project for all Unity verification, even if the main project is open:

- `D:\Projects\WarlineCapture-CodexUnity1`

Do not open the main Unity project for verification captures during this task.

Open or generate `Assets/Game/Scenes/GameUI.unity`, then capture the menu in:

- 1920x1080
- 2400x1080
- 3840x2160
- 4800x2160
- Optional ultrawide check, such as 3440x1440

For each capture, verify:

- Header remains top aligned.
- Background remains behind all main-menu pages and does not change while navigating menu routes.
- Logo panel remains left aligned.
- Header actions remain right aligned.
- Resource panels do not overlap logo or actions.
- Left nav remains left aligned and under the header.
- Comms status remains bottom-left inside left region.
- Commander panel remains inside right region.
- Deploy CTA remains bottom-right inside right region.
- Mode cards remain centered in the middle region.
- Background art crops, not stretches.
- Wide thumbnail art crops/reveals, not stretches.
- Text remains readable and does not overlap frames.

### 12. Compare Against Target Mockup

Scale the target reference to 1920x1080 only for screenshot comparison.

Compare:

- Large composition: header, left rail, mode cards, commander panel, deploy CTA.
- Palette: military dark/olive/gold, not old teal/cyan.
- Major art direction: command base/town visual language.
- Button/hit-zone placement.

Do not expect exact pixel matching where responsive shell constraints intentionally differ, but do require the layout to preserve the target hierarchy, visual weight, and region ownership.

### 13. Produce Verification Report

Save captures and notes under:

```text
Design/AgentReports/Captures/GameUI/MainMenu/
```

Report should include:

- Source paths used.
- Prefab path rebuilt.
- Unity version/project path used.
- Capture resolutions.
- Known differences from target.
- Any follow-up needed for shell background support or route-code path cleanup.

## Acceptance Checklist

- `SCN02_MainMenuContent.prefab` matches the active target mockup composition.
- The prefab uses the active layered SCN02 assets only.
- The prefab is one responsive canvas content prefab, not one prefab or scene per aspect ratio.
- `HeaderContent`, `LeftContent`, `MiddleContent`, and `RightContent` are the only top-level visual sections.
- `MenuBackgroundContent` is the only persistent background section and renders behind main-menu regions.
- Header and background stay unchanged across main-menu route navigation.
- Header is replaced and background is hidden when entering match HUD.
- Header content is owned by `HeaderContent`.
- Nav and comms content are owned by `LeftContent`.
- Mode cards are owned by `MiddleContent`.
- Main-menu background is owned by `MenuBackgroundContent`.
- Commander and deploy CTA content are owned by `RightContent`.
- Frames, icons, portraits, thumbnails, labels, and hotspots are children of their owning panels.
- Centered panel children use correct local pivots and anchored positions.
- Wide art uses cover/crop behavior.
- TMP text is live/runtime-bindable.
- Existing route buttons still work or have preserved compatibility paths.
- `SCN01_LoadingContent.prefab` is untouched.
- GameUI opens after loading and shows the designed main menu header and aligned menu shell.

## Open Decisions

1. The current shell does not yet expose a dedicated persistent menu background region. Add it before rebuilding SCN02 instead of putting the background under a body route section.
2. The current shell header height is 280 px at 4800x2160, about 13% of height. This matches the visual contract's rough 12-15% range, but should be checked against captured screenshots.
3. Confirm whether route code still requires `DeployCommandButton` naming, or whether it can move cleanly to `DeployOperationButton`.
4. Confirm the final project-wide TMP font asset. Until then, use Oxanium as required by the visual contract where available.

## Recommended First Build Order

1. Add the source guard and layer asset map.
2. Add `MenuBackgroundRegion` and presenter support for persistent menu background behavior.
3. Build `MenuBackgroundContent` from `scn02_background_art.png`.
4. Rebuild `HeaderContent` because it is persistent across menu navigation.
5. Rebuild `LeftContent` and route hotspots.
6. Rebuild `RightContent`, preserving commander/deploy route compatibility.
7. Rebuild `MiddleContent` mode cards.
8. Run structure validation.
9. Run Unity screenshot validation across aspect ratios.
10. Tune local panel positions only inside their owning parent panels.
