# SCN08 Match HUD Target-Lock Layout Contract

Date: 2026-05-27
Base resolution: `4800 x 2160`
Canonical target size: `2400 x 1080`
Scale: `2.0`

## Shell Regions

- `HeaderContent`: `x=0 y=0 w=4800 h=280`
- `LeftContent`: `x=0 y=280 w=720 h=1640`
- `MiddleContent`: unused by match HUD shell content for now; battlefield remains clear.
- `RightContent`: `x=4080 y=280 w=720 h=1640`
- `FooterContent`: `x=0 y=1920 w=4800 h=240`

## Header

- Header backing plate: stretch to `HeaderContent`.
- Command banner: top center-left, child of `HeaderContent`, local rect `x=1865 y=18 w=700 h=150`.
- Resource strip: child of `HeaderContent`, local rect `x=2640 y=18 w=1610 h=150`.
- Menu button: child of `HeaderContent`, top-right local rect `right=24 y=18 w=150 h=150`.
- Pause/settings quick buttons: child of `HeaderContent`, local rects near the right edge if not duplicated in `RightContent`.

## Left Content

- Objective panel: child of `LeftContent`, top anchored, local rect `x=16 y=18 w=670 h=520`.
- Selected squad panel: child of `LeftContent`, top anchored under objectives, local rect `x=16 y=570 w=690 h=1000`.
- Objective rows are children of the objective panel.
- Portrait, health bar, order row, and ability chips are children of the selected squad panel.

## Right Content

- Threat/JUMP toast: child of `RightContent`, top anchored, local rect `x=-860 y=18 w=840 h=160`; it may extend left from the right content region like the target.
- Right quick rail: child of `RightContent`, right anchored, local rect `x=530 y=180 w=170 h=760`.
- Minimap panel: child of `RightContent`, bottom anchored, local rect `x=-220 y=830 w=900 h=760`; it may extend left from the right content region like the target.
- Minimap markers and zoom buttons are children of the minimap panel or its controls group.

## Footer

- Squad tray: child of `FooterContent`, bottom-left anchored, local rect `x=18 y=-38 w=1500 h=270`; it can extend above the footer region to match the target bottom HUD.
- Command rail: child of `FooterContent`, bottom-centered, local rect `x=1900 y=-12 w=1660 h=250`; command buttons are children of this rail.
- Footer children may extend upward from the footer region to match the target, but must remain parented to `FooterContent`.

## Battlefield Overlay Markers

- Non-interactive world markers may be added as children of `FooterContent` or `RightContent` only if needed for target-like capture.
- They must not become a full-screen flat mockup layer.
- The central battlefield must remain visually open.

## Anchor Rules

- Top-left panels use `anchorMin=(0,1)`, `anchorMax=(0,1)`, `pivot=(0,1)` when they must stay at a visible edge.
- Top-right panels use `anchorMin=(1,1)`, `anchorMax=(1,1)`, `pivot=(1,1)`.
- Centered children inside panels use `anchorMin=(.5,.5)`, `anchorMax=(.5,.5)`, `pivot=(.5,.5)`.
- Sprite art belongs inside its frame/viewport parent.
- Text belongs inside the owning row/button/panel.
