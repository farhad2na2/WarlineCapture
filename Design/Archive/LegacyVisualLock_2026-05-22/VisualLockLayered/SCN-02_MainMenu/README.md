# SCN-02 Main Menu Layered Regeneration Pack

Status: `ReadyForReview_TargetLockAssetRevisionsV2_SCN02`

This package creates the SCN-02 Main Menu layered target-lock pack with canonical wallet resources and designed-unavailable route states. The target-lock reference bitmaps and layer contact sheet in this pass are imagegen-sourced and replace the rejected deterministic pass from `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions.md`.

## Required Canonical Content

- Top strip resources: `Credits`, `Materials`, `Command Authority`
- Mode cards: `Saga Campaign`, `Persistent Operation`, `Quick Custom Game`
- Persistent Operation copy frames district/city operation pressure
- Non-live routes visibly marked DesignedUnavailable: `Inbox`, `Store`, `Events`, `Ranking`, `Command Feed`
- Commander profile uses `commander_profile_portrait` as a finished production portrait if `PlayerProfileState` portrait data is not live

## Review Targets

- 16:9 target: `reference/SCN-02_MainMenu_Landscape_Target.png`
- 20:9 target: `reference/SCN-02_MainMenu_20x9_Target.png`
- Imagegen selected reference copies: `generated_one_go/source/imagegen_selected_reference_16x9.png`, `generated_one_go/source/imagegen_selected_reference_20x9.png`
- Imagegen selected contact sheet copy: `generated_one_go/source/imagegen_layers_contact_sheet_source.png`
- Imagegen commander profile portrait source: `generated_one_go/source/imagegen_scn02_commander_profile_portrait_chromakey.png`
- Commander profile portrait alpha: `generated_one_go/source/imagegen_scn02_commander_profile_portrait_alpha.png`
- Layer contact sheet: `generated_one_go/layers_contact_sheet.png`

TMP text remains live in implementation. Frames, mode cards, resource icons, route badges, fills, commander portrait, and dynamic data areas are separated as final named implementation slices in `layer_manifest.json`.

## Complete Production Sprite Update

The 2026-05-17 Art/Atlas update adds a full SCN-02 production sprite set so UI can build the Main Menu from reusable layered assets instead of placeholders, old shell art, deterministic substitutes, full-target composites, or target-reference panel slices.

New imagegen-sourced production sources:

- `generated_one_go/source/imagegen_scn02_complete_production_sprite_atlas_chromakey.png`
- `generated_one_go/source/imagegen_scn02_complete_production_sprite_atlas_alpha.png`
- `generated_one_go/source/imagegen_scn02_tactical_map_background.png`
- `generated_one_go/scn02_complete_production_sprites_contact_sheet.png`

New layer groups include:

- tactical map background
- brand panel frame and emblem
- full top resource bar frame with divisions and settings dock
- target-scale commander profile panel frame
- target-scale left nav row frame and route icons
- large mode card frame, header emblems, footer badges, operation warning/meter/divider sprites
- dedicated amber Deploy Command frame, chevrons, and glow overlay
- 20:9 Command Feed panel frame and icon
- trim, glow, and shadow overlays for state/depth polish

## Target-Lock Asset Revision

The 2026-05-17 Art/Atlas target-lock revision replaces the routed fidelity blockers from the UI production-sprite pass:

- `mode_card_art_saga`
- `mode_card_art_operation`
- `mode_card_art_quick_custom`
- `commander_profile_portrait`
- `brand_emblem`
- `icon_credits`
- `icon_materials`
- `icon_command_authority`
- `designed_unavailable_badge`
- `left_nav_icon_inbox`
- `left_nav_icon_store`
- `left_nav_icon_events`
- `left_nav_icon_ranking`
- `left_nav_icon_command_feed`
- optional deploy CTA chevrons/glow polish

Revision sources:

- `generated_one_go/source/imagegen_scn02_target_lock_card_art_triptych_chromakey.png`
- `generated_one_go/source/imagegen_scn02_target_lock_icon_badge_atlas_chromakey.png`
- `generated_one_go/source/imagegen_scn02_target_lock_icon_badge_atlas_alpha.png`
- `generated_one_go/scn02_target_lock_asset_revisions_contact_sheet.png`

These revised runtime sprites are imagegen-sourced. Deterministic tooling was limited to chroma-key alpha removal, crop extraction, resizing, metadata, contact-sheet packaging, and validation.

## Target-Lock Asset Revision V2

The 2026-05-17 v2 Art/Atlas revision responds to the PM/UI reroute after UI's final target-lock pass was rejected as still off-target. It keeps the existing layer ids and dimensions, and replaces the routed card art, commander portrait, brand/resource/nav icons, designed-unavailable badge, deploy chevrons, and deploy glow with closer target-scale imagegen selections.

V2 revision sources:

- `generated_one_go/source/imagegen_scn02_target_lock_v2_card_art_strip_chromakey.png`
- `generated_one_go/source/imagegen_scn02_target_lock_v2_icon_badge_atlas_chromakey.png`
- `generated_one_go/source/imagegen_scn02_target_lock_v2_icon_badge_atlas_alpha.png`
- `generated_one_go/scn02_target_lock_asset_revisions_v2_contact_sheet.png`

These v2 runtime sprites are imagegen-sourced. Deterministic tooling was limited to source copy, chroma-key alpha removal, crop extraction, resizing to existing layer dimensions, residual chroma cleanup, metadata, contact-sheet packaging, and validation. No target crop, screenshot, comparison image, contact sheet, vector substitute, scripted composite, or deterministic final-art source was used as runtime art.

## Unity Staging

```bash
python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py
```

Default helper mode is dry-run. Do not import into `Assets/` until PM/user approval.
