# Art/Atlas SCN-02 Target-Lock Asset Revisions V2

Date: 2026-05-17
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Revise the SCN-02 Main Menu Art/Atlas assets after PM rejected UI's final target-lock pass as still visually off-target.

Scope was limited to:

- `Design/VisualLockLayered/SCN-02_MainMenu/`
- this handoff report under `Design/AgentReports/`

No runtime code, Unity prefabs, `Assets/` imports, source docs, or other lane task files were modified.

## Handoff Assessment

- `Design/AgentReports/2026-05-17_pm_ui-scn02-final-pass-rejected-art-reroute.md`: accepted as the active P0 PM reroute and current Art/Atlas blocker.
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-final-target-lock-pass.md`: accepted as honest UI evidence, but the pass remains rejected for target-lock acceptance per PM.
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-pm-art-asset-request.md`: accepted as the specific v2 Art/Atlas asset request list.

## Result

The routed SCN-02 target-lock asset set has been revised again from imagegen-sourced v2 replacement sheets.

Manifest status:

- `ReadyForReview_TargetLockAssetRevisionsV2_SCN02`

Focused contact evidence:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_target_lock_asset_revisions_v2_contact_sheet.png`

## Revised Layers

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
- `deploy_command_chevrons`
- `deploy_command_glow_overlay`

## Before/After Notes

| Layer | Before | After |
|---|---|---|
| `mode_card_art_saga` | v1 card was thematically correct but still not close enough to the target city/convoy/soldier read | wider smoky city battle scene with APC foreground, soldier group, aircraft silhouettes, fires, and cooler HUD lighting |
| `mode_card_art_operation` | v1 operation card lacked target density/perspective and bright central node structure | denser blue holographic district grid with brighter center cluster, layered towers, and orange threat points |
| `mode_card_art_quick_custom` | v1 base scene did not carry enough mountain-base layout or sky contrast | wider mountain forward-base scene with watchtower, modular base structures, helicopter silhouettes, and storm-sky contrast |
| `commander_profile_portrait` | v1 scan portrait was closer but still soft/less target-framed | harder framed dark commander silhouette with cyan scan grid and target-style panel treatment |
| `brand_emblem` | v1 emblem was angular but still too crest-like/off-target | sharper silver/cyan Warline mark with stronger masthead-scale silhouette |
| `icon_credits` | v1 coin icon was usable but not target-scale enough | stacked gold coin icon with clearer bevel, scale, and warm highlight |
| `icon_materials` | v1 material icon was usable but less target-like | stacked blue crate/materials icon with stronger crate silhouette and cyan lighting |
| `icon_command_authority` | v1 authority icon was usable but less target-like | gold shield/star icon with stronger target silhouette and bevel |
| `designed_unavailable_badge` | v1 badge read, but needed cleaner row-scale lock treatment | cleaner `Designed Unavailable` badge with right-side lock plate and tighter row readability |
| `left_nav_icon_*` | v1 nav symbols still varied in row weight and contrast | target-weight neutral mail/cart/calendar/ranking/antenna silhouettes sized for runtime rows |
| `deploy_command_chevrons` | v1 chevrons were still too large/bright | subtler amber double chevron sized closer to target CTA spacing |
| `deploy_command_glow_overlay` | v1 CTA glow remained too intense | subtler amber glow overlay sized for the target button treatment |

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected generated files:

- V2 card art strip: `ig_0ae68e52b07447a2016a09ab860bcc81988eb545f8bc902259.png`
- V2 icon/badge atlas: `ig_0ae68e52b07447a2016a09abc8126881988111930cc429cecf.png`

Project source copies:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_v2_card_art_strip_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_v2_icon_badge_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_v2_icon_badge_atlas_alpha.png`

Deterministic tooling was used only after imagegen source selection for source copy, chroma-key alpha removal, crop extraction, resizing to existing package dimensions, residual chroma cleanup, metadata updates, contact-sheet packaging, inspection, and validation.

No revised final runtime art was created from deterministic vector/HTML/CSS/scripted methods, target-reference panel crops, target composites, screenshots, comparison images, or contact sheets.

## Manifest Updates

Updated `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with:

- status `ReadyForReview_TargetLockAssetRevisionsV2_SCN02`
- source entries for the v2 target-lock card strip and icon/badge atlas
- source entries for the v2 target-lock revision contact sheet
- source generation provenance for each revised routed layer
- updated binding notes for v2 card art, portrait, badge, resource icons, nav icons, and optional deploy overlays
- no `target_slice_*` manifest references

## Files Changed

- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_target_lock_asset_revisions_v2_contact_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_v2_card_art_strip_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_v2_icon_badge_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_v2_icon_badge_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_saga.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_operation.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_quick_custom.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/commander_profile_portrait.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/brand_emblem.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/icon_credits.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/icon_materials.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/icon_command_authority.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/designed_unavailable_badge.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_inbox.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_store.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_events.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_ranking.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_command_feed.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/deploy_command_chevrons.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/deploy_command_glow_overlay.png`

## Validation Run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Checked latest `Design/AgentReports/` handoffs.
- Accepted PM reroute and UI final-pass evidence as the active v2 Art/Atlas blocker context.
- Generated imagegen v2 card-art strip and v2 icon/badge atlas.
- Copied selected imagegen sources into the SCN-02 package.
- Removed chroma-key background from the v2 icon/badge atlas.
- Extracted and resized the routed v2 replacement layers to existing package dimensions.
- Cleaned residual chroma pixels from revised routed layers.
- Updated SCN-02 README and manifest.
- Built focused target-lock v2 revision contact sheet.
- Parsed `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with `python3 -m json.tool`: passed.
- Verified manifest layer count remains `49`.
- Verified every manifest layer file exists: `missing 0`.
- Verified every manifest source file exists.
- Scanned revised routed layer PNGs for opaque chroma-green pixels: `REVISED_V2_GREEN_REMAINING 0`.
- Ran `rg -n "target_slice" Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`: no matches.
- Ran `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py`: dry-run passed and mapped the revised assets to Unity destinations.
- Ran `git diff --check` for the SCN-02 package and this report: passed.

## Validation Result

Ready for PM/user review.

- Required routed SCN-02 v2 asset revisions delivered: yes
- Revised assets are imagegen-sourced: yes
- Manifest parses: yes
- Every manifest layer file exists: yes
- Every manifest source file exists: yes
- No `target_slice_*` manifest references: yes
- No chroma-green residue remains in revised routed layers: yes
- Target-reference panel crops used as final runtime art: no
- Target composites/screenshots/contact sheets used as final runtime art: no
- Deterministic/vector/programmatic final art created: no
- Runtime code changed: no
- Unity prefabs changed: no
- `Assets/` imports changed: no
- Other lane task files changed: no

## Next Owner

PM/user review. If accepted, UI can run the final SCN-02 import/placement/capture pass using the revised v2 layers.
