# SCN02 Main Menu Clean Target-Lock Implementation Plan

Date: 2026-05-27
Status: Complete. The first implementation produced a cleaner structure but was reopened for visual mismatch; the second refinement pass completed the target-like header/card/progress/CTA corrections and was verified through the shadow Unity project.

## Rule

This plan has one definition of complete:

The GameUI capture must look clean and close to `reference/SCN-02_MainMenu_Landscape_Target.png`, and the prefab hierarchy must be correctly parented, anchored, and pivoted.

Do not mark a step complete just because code compiles, a prefab exists, or Unity validation passes. Visual quality is part of the requirement.

## Progress Tracker

Use this section as the only tracker for this work. Change `[PENDING]` to `[COMPLETE]` only when the described output exists and is verified.

- [x] `[COMPLETE]` Step 01 - Open the active target mockup and layer inventory only from `Design/VisualLockLayered/SCN-02_MainMenu/`; confirm no old generated Unity scene, old mockup folder, or V15B source is used.
- [x] `[COMPLETE]` Step 02 - Measure the target mockup layout into a written layout contract: header, left nav, center cards, commander panel, comms panel, deploy CTA, and background.
- [x] `[COMPLETE]` Step 03 - Convert the measured target layout from target/mockup coordinates into the GameUI base resolution `4800x2160`.
- [x] `[COMPLETE]` Step 04 - Define the final prefab section roots exactly: `MenuBackgroundContent`, `HeaderContent`, `LeftContent`, `MiddleContent`, and `RightContent`.
- [x] `[COMPLETE]` Step 05 - Define the final local hierarchy for each section before writing code, including panel roots, frame children, image children, text children, and hotspots.
- [x] `[COMPLETE]` Step 06 - Define anchor and pivot rules for every repeated element type: section roots, panels, frames, icons, thumbnails, text slots, masks, and buttons.
- [x] `[COMPLETE]` Step 07 - Implement `MenuBackgroundContent` as a full-shell persistent background using `scn02_background_art.png` with cover/crop behavior, not stretch.
- [x] `[COMPLETE]` Step 08 - Implement `HeaderContent` as a clean target-like top command bar with logo, resource slots, command slot, inbox, and settings all parented under local header panels.
- [x] `[COMPLETE]` Step 09 - Implement `LeftContent` as a clean left navigation rail with local nav row children, correct selected state, readable labels, icons, hotspots, and bottom comms panel.
- [x] `[COMPLETE]` Step 10 - Implement `MiddleContent` with target-like mode cards: correct card scale, thumbnail area, title area, description area, progress/footer area, and card hotspots.
- [x] `[COMPLETE]` Step 11 - Implement card thumbnail masks so each thumbnail art layer is a child of its card viewport and is cropped/revealed without distortion.
- [x] `[COMPLETE]` Step 12 - Implement `RightContent` with clean commander panel, portrait containment, commander identity, readiness row, locked rows, commander hotspot, and deploy CTA.
- [x] `[COMPLETE]` Step 13 - Preserve required route compatibility paths without allowing compatibility objects to create duplicate visible UI.
- [x] `[COMPLETE]` Step 14 - Build the SCN02 prefab through Unity using only the shadow sibling project `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 15 - Capture GameUI main menu screenshots through the shadow sibling project at `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160`.
- [x] `[COMPLETE]` Step 16 - Compare the `1920x1080` capture against the active target mockup for composition, spacing, typography scale, visual hierarchy, and readability.
- [x] `[COMPLETE]` Step 17 - Verify responsive behavior at all captured aspect ratios: header remains stable, side regions stay anchored, center content remains clean, and no text/panels overlap.
- [x] `[COMPLETE]` Step 18 - Verify hierarchy and transform rules mechanically: each panel owns its contents, centered children use centered anchors/pivots, and no target reference PNG or old source path is used as implementation art.
- [x] `[COMPLETE]` Step 19 - Verify menu behavior: header and background stay unchanged during menu navigation, and match HUD hides/replaces the menu frame.
- [x] `[COMPLETE]` Step 20 - Save final screenshots and a short verification report under `Design/AgentReports/Captures/GameUI/MainMenu/`.
- [x] `[COMPLETE]` Step 21 - Mark this plan complete only if the final capture is clean and target-like; otherwise leave the relevant step pending with the exact reason.
- [x] `[COMPLETE]` Step 22 - Refine the header so it reads closer to the target mockup as one continuous command bar: tighter resource spacing, fewer empty gaps, stable left/right anchoring, and readable values at `1920x1080`.
- [x] `[COMPLETE]` Step 23 - Refine the center mode cards to match the target rhythm: card position, thumbnail height, title/icon placement, description spacing, and footer/progress area.
- [x] `[COMPLETE]` Step 24 - Add target-like visible progress fills/segments to the mode cards instead of empty meter frames.
- [x] `[COMPLETE]` Step 25 - Refine the commander panel and deploy CTA proportions so the right side matches the target composition without crowding or oversized elements.
- [x] `[COMPLETE]` Step 26 - Rebuild `SCN02_MainMenuContent.prefab` through `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 27 - Re-capture `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160` through `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 28 - Recompare the new captures against the active target mockup and mark Steps 16, 17, 20, and 21 complete only if the result is visually clean and target-like.

## Reopened Visual Gap Notes

The first capture was structurally better than the old flat conversion, but it was not final target-lock quality.

- Header was refined with a continuous dark backing plate while keeping local left/resource/right anchoring.
- Center cards were lifted to better match the target rhythm.
- Mode cards now include visible segmented progress fills.
- Deploy CTA was raised to better match the target bottom-right rhythm.
- Final completion was based on fresh shadow-project captures and visual comparison, not only Unity validation.

## Source Of Truth

Use:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`

Do not use:

- Old generated main menu mockups
- Generated Unity target scenes as source of truth
- `Assets/Game/Art/UI/Generated/MainMenu`
- `Assets/Game/Art/UI/Generated/MainMenuAlt`
- V15B rejected assets
- The target reference PNG as sliced implementation art

The reference PNG is for visual comparison only. Implementation sprites must come from the approved layers.

## Target Layout Contract

Create this contract before implementation and keep it in this document or a linked report.

For each target region, record:

- target rect in source/mockup pixels
- converted rect in `4800x2160`
- owning GameUI section
- local parent panel
- anchor preset
- pivot
- child list
- visual acceptance notes

Required regions:

- Full-screen command-base background
- Header logo block
- Header credits block
- Header supplies block
- Header command block
- Header actions block
- Left nav rail
- Campaign nav row
- Operations nav row
- Skirmish nav row
- Store nav row
- Commander nav row
- Settings nav row
- Comms status panel
- Campaign mode card
- Operations mode card
- Skirmish mode card
- Commander panel
- Commander portrait area
- Readiness row
- Locked feature rows
- Deploy operation CTA

## Required Prefab Hierarchy

The final hierarchy must be local and panel-owned:

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
      Detail
  MiddleContent
    ModeCardsContainer
      CampaignCard
        Frame
        ThumbnailViewport
          ThumbnailArt
          MaskFrame
        Icon
        Title
        Description
        ProgressLabel
        ProgressValue
        ProgressMeter
        Hotspot
      OperationsCard
        Frame
        ThumbnailViewport
          ThumbnailArt
          MaskFrame
        Icon
        Title
        Description
        ProgressLabel
        ProgressValue
        ProgressMeter
        Hotspot
      SkirmishCard
        Frame
        ThumbnailViewport
          ThumbnailArt
          MaskFrame
        Icon
        Title
        Description
        ProgressLabel
        ProgressValue
        ProgressMeter
        Hotspot
  RightContent
    CommanderPanel
      Frame
      Title
      PortraitPanel
        Frame
        Portrait
      IdentityPanel
        Badge
        Name
        Level
      ReadinessPanel
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
      Label
      Chevrons
      Hotspot
```

Compatibility route objects may exist, but they must not create duplicate visible frames or labels.

## Anchor And Pivot Rules

Section roots:

- Stretch to their assigned shell region.
- Anchor min `(0, 0)`.
- Anchor max `(1, 1)`.
- Pivot `(0.5, 0.5)`.
- Offset min/max zero.

Panel roots:

- Anchor to the local parent area they belong to.
- Use edge anchors only for edge-stable groups.
- Use center anchors for centered groups.
- Never position a panel relative to the full screen unless its parent is the full-screen section.

Frame/image children:

- Parent must be the owning panel or viewport.
- Default anchor `(0.5, 0.5)`.
- Default pivot `(0.5, 0.5)`.
- Centered frame anchored position `(0, 0)`.

Text:

- TMP text only.
- Text is parented to a local slot inside the owning panel.
- Text slots can use stretch anchors inside their own panel, but not across unrelated panels.
- Text must be readable and not overlap visual frames.

Thumbnails/background:

- Background uses cover/crop full-shell behavior.
- Card thumbnails use viewport mask/crop behavior.
- Do not non-uniformly stretch image art.

Hotspots:

- Transparent hotspots are children of the visual control they activate.
- Hotspot rect must match the local visual control.

## Completion Gate

The work is complete only when all of these are true:

- The final screenshot is clean and close to the target mockup.
- Header looks like one intentional command bar, not scattered chunks.
- Left nav scale and typography match the target.
- Mode cards have the same visual weight as the target and no huge unintended empty areas.
- Commander panel contents are contained and readable.
- Deploy CTA is target-like and properly anchored.
- Background, header, left, middle, and right areas respond cleanly at 16:9 and 20:9.
- The hierarchy follows the required local parent ownership.
- `SCN01_LoadingContent.prefab` is untouched.
- Verification was done through `D:\Projects\WarlineCapture-CodexUnity1`.
