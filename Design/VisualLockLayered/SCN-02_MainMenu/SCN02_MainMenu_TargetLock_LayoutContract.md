# SCN02 Main Menu Target-Lock Layout Contract

Date: 2026-05-27
Status: Initial measured contract for clean GameUI implementation

## Source

Active target reference:

```text
Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png
```

Measured source size:

```text
1671 x 941
```

Implementation base:

```text
4800 x 2160
```

## Conversion Policy

The target mockup is a 16:9-style composition. GameUI is authored at `4800x2160`, which is 20:9. The clean implementation must not simply scale the entire 16:9 image into the 20:9 canvas because that would leave the left and right gameplay shell areas floating away from the screen edges.

Use this policy:

- Vertical scale is based on target height: `2160 / 941 = 2.29543`.
- Header, left navigation, commander panel, and deploy CTA are edge-stable shell elements.
- Left-side elements convert from the left edge.
- Right-side elements convert from the right edge.
- Center mode cards convert by measured size, then are centered inside `MiddleContent`.
- Background uses cover/crop behavior across the full shell.
- At 16:9 capture widths, the shell should visually collapse toward the target composition.
- At 20:9 capture widths, the background reveals more horizontal area while left and right shell content stay edge anchored.

## Shell Sections

| Section | Shell rect at 4800x2160 | Anchor behavior |
| --- | --- | --- |
| `MenuBackgroundContent` | `x=0 y=0 w=4800 h=2160` | Full stretch |
| `HeaderContent` | `x=0 y=0 w=4800 h=280` | Top stretch |
| `LeftContent` | `x=0 y=280 w=720 h=1640` | Left edge |
| `MiddleContent` | `x=720 y=280 w=3360 h=1640` | Center flexible |
| `RightContent` | `x=4080 y=280 w=720 h=1640` | Right edge |

## Target Measurements

Coordinates are approximate hand-measured rectangles from the active target mockup. Source rects are in `1671x941` target pixels.

| Element | Source rect | 2160-height scaled rect | Owner |
| --- | --- | --- | --- |
| Header full bar | `0,0,1671,114` | `0,0,3836,262` | `HeaderContent` |
| Header logo block | `0,6,414,106` | `0,14,950,243` | `HeaderLogoPanel` |
| Header credits block | `414,6,314,106` | `950,14,721,243` | `CreditsPanel` |
| Header supplies block | `728,6,304,106` | `1671,14,698,243` | `SuppliesPanel` |
| Header command block | `1032,6,324,106` | `2369,14,744,243` | `CommandPanel` |
| Header actions block | `1356,6,313,106` | right anchored, `w=719 h=243` | `HeaderActionsPanel` |
| Left nav rail | `18,185,250,610` | `41,425,574,1400` | `LeftNavPanel` |
| Campaign row | `20,188,244,90` | `46,432,560,207` | `Nav_Campaign` |
| Operations row | `28,292,236,90` | `64,670,542,207` | `Nav_Operations` |
| Skirmish row | `28,396,236,90` | `64,909,542,207` | `Nav_Skirmish` |
| Store row | `28,501,236,90` | `64,1150,542,207` | `Nav_Store` |
| Commander row | `28,606,236,90` | `64,1391,542,207` | `Nav_Commander` |
| Settings row | `28,710,236,90` | `64,1630,542,207` | `Nav_Settings` |
| Comms panel | `18,812,250,122` | `41,1864,574,280` | `CommsStatusPanel` |
| Cards band | `292,320,949,429` | `w=2178 h=985` | `ModeCardsContainer` |
| Campaign card | `292,320,304,429` | `w=698 h=985` | `CampaignCard` |
| Operations card | `615,320,304,429` | `w=698 h=985` | `OperationsCard` |
| Skirmish card | `937,320,304,429` | `w=698 h=985` | `SkirmishCard` |
| Card thumbnail | `302,393,284,186` | `w=652 h=427` | `ThumbnailViewport` |
| Commander panel | `1342,158,313,646` | right anchored, `w=719 h=1483` | `CommanderPanel` |
| Commander portrait | `1378,229,230,221` | local panel child, `w=528 h=507` | `PortraitPanel` |
| Readiness row | `1369,552,258,57` | local panel child, `w=592 h=131` | `ReadinessPanel` |
| Locked row 1 | `1364,630,272,69` | local panel child, `w=624 h=158` | `SquadManagementRow` |
| Locked row 2 | `1364,711,272,69` | local panel child, `w=624 h=158` | `IntelReportRow` |
| Deploy CTA | `1153,802,497,115` | right anchored, `w=1141 h=264` | `DeployOperationButton` |

## 4800x2160 Placement Decisions

These are the authoring placements to use for the clean implementation.

| Element | 4800x2160 placement | Parent |
| --- | --- | --- |
| Background | `x=0 y=0 w=4800 h=2160`, cover/crop | `MenuBackgroundContent` |
| Header logo panel | left anchored, `x=0 y=14 w=950 h=243` | `HeaderContent` |
| Header resource area | fill between logo and actions | `HeaderContent` |
| Credits panel | `x=950 y=14 w=721 h=243` or flex slot | `HeaderResourceArea` |
| Supplies panel | `x=1671 y=14 w=698 h=243` or flex slot | `HeaderResourceArea` |
| Command panel | `x=2369 y=14 w=744 h=243` or flex slot | `HeaderResourceArea` |
| Header actions panel | right anchored, `right=0 y=14 w=719 h=243` | `HeaderContent` |
| Left nav panel | left anchored inside `LeftContent`, `x=41 y=145 w=574 h=1400` after subtracting header shell y | `LeftContent` |
| Comms panel | bottom-left inside `LeftContent`, `x=41 bottom=16 w=574 h=280` | `LeftContent` |
| Cards container | centered inside `MiddleContent`, `w=2178 h=985` | `MiddleContent` |
| Campaign card | `x=0 y=0 w=698 h=985` | `ModeCardsContainer` |
| Operations card | `x=740 y=0 w=698 h=985` | `ModeCardsContainer` |
| Skirmish card | `x=1480 y=0 w=698 h=985` | `ModeCardsContainer` |
| Commander panel | right anchored inside `RightContent`, `x=0 y=83 w=719 h=1483` after subtracting header shell y | `RightContent` |
| Deploy CTA | bottom-right, `x=-421 y=1561 w=1141 h=264` relative to `RightContent` if allowed to extend left like the target | `RightContent` |

The deploy CTA intentionally extends left from the right region in the target composition. If this causes routing or clipping problems, place it as a child of `RightContent` with overflow visible, not as a screen-root child.

## Local Hierarchy Rules

- Each frame is child of its owning panel.
- Each icon is child of its row, button, or card.
- Each text label is child of its local panel or row.
- Each thumbnail art image is child of its card thumbnail viewport.
- `CommanderPortraitButton` and deploy compatibility paths may exist, but visible art stays under `CommanderPanel` and `DeployOperationButton`.

## Acceptance Notes

The target visual has these important qualities:

- Header reads as one continuous command bar.
- Left nav rows are large, readable, and evenly spaced.
- Mode cards are medium height, not tall empty panels.
- Each mode card has strong title/header, thumbnail, description, and progress/footer structure.
- Commander panel contents are contained inside the right panel.
- Deploy CTA is large, gold, bottom-right, and aligned with the commander panel.
- Comms panel is bottom-left and visually attached to the command table/radio area.
