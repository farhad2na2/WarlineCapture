# PM UI SCN-08 v4 Rejected; Route Art Slices

Date: 2026-05-16
Owner: PM
Status: rejected; Art/Atlas routed
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v4.md`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v4_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`

## Decision

Reject UI v4 as final Match HUD completion.

Accepted only:

- v4 fixed the command rail/minimap overlap.
- STOP/HOLD/MOVE/ATTACK/SPECIAL are readable at 1920x1080.
- M01 no-selection state remains correct.

Rejected:

- The HUD still does not match the approved SCN-08 mockup quality and cleanliness.
- Squad cards, portraits, card micro-detail, threat row art, minimap content, minimap zoom controls, badges/icons, chrome depth, and global density remain below target quality.
- The SCN-08 manifest confirms missing generated target-quality assets: time/clock icon, squad portraits, shield badges, rank chevrons, objective icons, minimap content, minimap zoom buttons, and threat row backgrounds.

## Routing

Current owner:
Art/Atlas

Art/Atlas must deliver:

- `Design/AgentReports/2026-05-16_art-atlas_scn08-rtsbattlehud-complete-implementation-slices.md`

Scope:

- Work only in `Design/VisualLockLayered/SCN-08_RTSBattleHUD/`.
- Preserve the approved SCN-08 reference and visual direction.
- Provide target-quality imagegen-sourced implementation slices/contact sheets/manifest updates for the missing or low-quality HUD pieces.
- Do not modify runtime code or Unity prefabs.
- Do not touch POP-05, SCN-02, POP-11, POP-10, Operation screens, Gameplay art, or other VisualLockLayered packages.

After PM/user accepts Art/Atlas SCN-08 slices, UI must produce:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v5.md`

## Held

UI SCN-08 v5 implementation, POP-05/SCN-02 implementation, Gameplay, QA/HCI, Support/FTUE, Designer, and non-routed Art packages.
