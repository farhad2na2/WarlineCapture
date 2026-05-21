# Art/Atlas SCN-02 Target-Lock Asset Revisions

Date: 2026-05-17
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Revise the SCN-02 Main Menu assets that blocked target-lock after UI implemented the accepted production-sprite package.

Scope was limited to:

- `Design/VisualLockLayered/SCN-02_MainMenu/`
- this handoff report under `Design/AgentReports/`

No runtime code, Unity prefabs, `Assets/` imports, source docs, or other lane task files were modified.

## Handoff Assessment

- `Design/AgentReports/2026-05-17_pm_ui-scn02-art-needs-accepted-art-atlas-dispatch.md`: accepted as current P0 Art/Atlas routing.
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-production-sprite-implementation.md`: accepted as honest UI implementation evidence.
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-target-lock-art-needs.md`: accepted as the precise Art/Atlas asset blocker list.

## Result

The routed SCN-02 target-lock asset set has been revised from imagegen-sourced replacement sheets.

Manifest status:

- `ReadyForReview_TargetLockAssetRevisions_SCN02`

Focused contact evidence:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_target_lock_asset_revisions_contact_sheet.png`

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
| `mode_card_art_saga` | generic/insufficient city card crop | illustrated smoky city and armored convoy campaign scene |
| `mode_card_art_operation` | non-target operation card art | blue tactical holographic district/city-grid scene |
| `mode_card_art_quick_custom` | non-target base scene crop | mountain forward base with helicopter target-style scene |
| `commander_profile_portrait` | visible face portrait, unlike target | dark commander silhouette/profile scan treatment |
| `brand_emblem` | off-style crest/eagle treatment | angular silver/cyan Warline-style emblem |
| `icon_credits` | less target-like coin icon | stacked gold coin resource icon |
| `icon_materials` | less target-like material icon | stacked blue crate/materials icon |
| `icon_command_authority` | less target-like authority icon | gold shield/star command authority icon |
| `designed_unavailable_badge` | weak badge/lock treatment | target-style two-line `Designed Unavailable` badge with lock plate |
| `left_nav_icon_*` | oversized/off-style nav icons | neutral target-style mail/cart/calendar/ranking/antenna silhouettes |
| `deploy_command_chevrons` | oversized/brighter chevrons | smaller amber double chevron treatment |
| `deploy_command_glow_overlay` | very bright CTA glow | subtler amber glow overlay frame |

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected generated files:

- Card art triptych: `ig_0ae68e52b07447a2016a0998b7ab688198a82e4cdf627abf6e.png`
- Icon/badge atlas: `ig_0ae68e52b07447a2016a0998fb7c18819899bc41eb3a4a27fc.png`

Project source copies:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_card_art_triptych_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_icon_badge_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_icon_badge_atlas_alpha.png`

Deterministic tooling was used only after imagegen source selection for chroma-key alpha removal, crop extraction, resizing to package dimensions, metadata updates, contact-sheet packaging, inspection, and validation. A tiny amount of chroma-key residue in two card crops was neutralized as alpha/color cleanup.

No revised final runtime art was created from deterministic vector/HTML/CSS/scripted methods, target-reference panel crops, target composites, screenshots, comparison images, or contact sheets.

## Manifest Updates

Updated `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with:

- status `ReadyForReview_TargetLockAssetRevisions_SCN02`
- source entries for the target-lock card triptych and icon/badge atlas
- source entries for the target-lock revision contact sheet
- source generation provenance for each revised routed layer
- updated binding notes for revised card art, portrait, badge, nav icons, and optional deploy overlays
- updated target rect guidance for the larger commander profile scan and revised unavailable badge

## Files Changed

- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_target_lock_asset_revisions_contact_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_card_art_triptych_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_icon_badge_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_target_lock_icon_badge_atlas_alpha.png`
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
- Read PM dispatch and UI art-needs reports.
- Reviewed SCN-02 production-sprite runtime captures.
- Generated imagegen card-art triptych and icon/badge replacement atlas.
- Removed chroma-key background from the icon/badge atlas.
- Extracted and resized the routed replacement layers.
- Updated SCN-02 README and manifest.
- Built focused target-lock revision contact sheet.
- Parsed `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with `python3 -m json.tool`: passed.
- Verified manifest layer count remains `49`.
- Verified every manifest layer file exists: `missing 0`.
- Scanned revised routed layer PNGs for opaque chroma-green pixels: `REVISED_GREEN_REMAINING 0`.
- Ran `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py`: dry-run passed and mapped the revised assets to Unity destinations.
- Ran `git diff --check` for the SCN-02 package: passed.

## Validation Result

Ready for PM/user review.

- Required routed SCN-02 asset revisions delivered: yes
- Revised assets are imagegen-sourced: yes
- Manifest parses: yes
- Every manifest layer file exists: yes
- No chroma-green residue remains in revised routed layers: yes
- Target-reference panel crops used as final runtime art: no
- Target composites/screenshots/contact sheets used as final runtime art: no
- Deterministic final art created: no
- Runtime code changed: no
- Unity prefabs changed: no
- `Assets/` imports changed: no
- Other packages changed: no

## Next Owner

After PM/user accepts this Art/Atlas handoff, UI can run the final SCN-02 import/placement/capture pass using the revised layers.
