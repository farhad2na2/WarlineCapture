# Art/Atlas SCN-08 RTSBattleHUD Complete Implementation Slices

Date: 2026-05-16
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Produce complete target-quality SCN-08 RTSBattleHUD implementation art/layer slices with imagegen-only visual production, scoped only to:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/`

Do not modify runtime code, Unity prefabs, POP-05, SCN-02, POP-11, POP-10, Operation screens, Gameplay art, or other VisualLockLayered packages.

## Handoff Assessment

- `Design/AgentReports/2026-05-16_pm_ui-scn08-v4-rejected-route-art-slices.md`: accepted as P0 Art/Atlas routing.
- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v4.md`: accepted as UI implementation context and missing-slice list.
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`: accepted as the approved visual direction to preserve.
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`: accepted as the package manifest to update.

## Imagegen Confirmation

Confirmed: the new SCN-08 visual assets, contact sheet, atlas sources, and flattened review imagery in this handoff are imagegen-sourced. Deterministic tooling was used only after imagegen selection for chroma-key alpha removal, crop extraction from selected imagegen atlases, metadata sizing, manifest updates, and validation.

No HTML/CSS screenshots, local compositing, manual vector shape drawing, scripted UI assembly, or pixel-patched deterministic art were used as final visual sources.

Selected imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected imagegen files:

- Squad/badge/objective/threat atlas: `ig_066f017118725f96016a081ff4d0908191991a90394ce59d70.png`
- Minimap/command chrome atlas: `ig_066f017118725f96016a082043cad881919b85bf190cb50e1e.png`
- Complete slices contact sheet: `ig_066f017118725f96016a08208fb32481918244cfb92123e633.png`

## Package Paths Revised

Updated package:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/generated_layer_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/generated_layer_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_squad_badge_objective_threat_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_squad_badge_objective_threat_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_minimap_command_chrome_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_minimap_command_chrome_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_complete_slices_contact_sheet.png`

New or replaced target-quality layer slices:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_portrait_rifle.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_portrait_apc.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_portrait_tank.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_portrait_helicopter.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_card_selected_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_card_normal_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/shield_badge_cyan.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/squad_rank_triple_chevron.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/objective_empty_square.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/objective_checked_square.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/objective_star_filled.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/time_clock_icon.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/threat_warning_icon.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/threat_enemy_spotted_icon.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/threat_row_active_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/threat_row_normal_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/minimap_content.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/minimap_frame.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/minimap_viewport_rect.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/minimap_zoom_plus_button.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/minimap_zoom_minus_button.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_rail_frame.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_rail_fill.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_button_normal_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_button_selected_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/build_button_selected_background.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/objective_panel_frame.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/threat_feed_panel_frame.png`

## Manifest Updates

- Manifest status changed to `ReadyForReview_ImagegenCompleteImplementationSlices_SCN08`.
- Manifest now records imagegen atlas/contact-sheet provenance and selected generated file names.
- New `sourceGeneration` block confirms imagegen source and states deterministic final visuals are not allowed.
- New and replaced layers include `sourceGeneration: imagegen_scn08_complete_implementation_slices`.
- Added layer entries for squad portraits, shield badge, checked objective, clock, enemy spotted icon, threat row backgrounds, minimap content, minimap viewport rectangle, and minimap zoom buttons.
- Updated sizes, 9-slice hints, and notes for replaced squad card, minimap, command rail/button, objective panel, and threat panel slices.

## Visual QA Notes

- Squad cards now have target-quality portrait/card art for Rifle Squad, APC, Tank, and Helicopter/Air Support.
- Selected and normal/disabled squad card chrome now include richer cyan/metal trim and health-bar wells.
- Objective icons now include empty checkbox, checked checkbox, and polished amber star goal icon.
- Threat feed support now includes active and normal row backgrounds plus amber warning and red enemy-spotted icons.
- Minimap support now includes tactical city map content, frame, viewport rectangle, plus zoom button, and minus zoom button.
- Command rail/button replacements preserve the v4 command/minimap layout separation while improving chrome depth and polish.
- Clock icon now matches the resource strip visual language.
- The contact sheet presents the full SCN-08 slice package and a flattened review preview for PM/user visual review.

## Validation Run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Checked latest Art/Atlas-relevant reports in `Design/AgentReports/`.
- Read the PM SCN-08 v4 rejection/routing report and UI v4 implementation handoff.
- Inspected the approved SCN-08 target reference and existing contact sheet.
- Generated imagegen atlases/contact sheet.
- Copied selected imagegen outputs into the SCN-08 package.
- Extracted alpha PNG layers from selected imagegen atlases.
- Updated README and layer manifest.
- Parsed `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json` with `python3 -m json.tool`: passed.
- Verified key PNG dimensions with `sips`.
- Ran dry-run copy helper:
  - `python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py`
- Ran whitespace validation:
  - `git diff --check -- Design/VisualLockLayered/SCN-08_RTSBattleHUD Design/AgentReports/2026-05-16_art-atlas_scn08-rtsbattlehud-complete-implementation-slices.md`

## Validation Result

Ready for PM/user review.

- Runtime code changed: no
- Unity prefabs changed: no
- `Assets/` imports changed: no
- Non-routed VisualLockLayered packages changed: no
- SCN-08 package revised: yes
- Imagegen-sourced atlas sources present: yes
- Imagegen-sourced contact sheet present: yes
- Required missing/low-quality slice groups covered: yes
- Layer manifest present and valid JSON: yes
- Dry-run copy helper recognizes new layer destinations: yes

## Known Gaps

- Final Unity import, prefab binding, and runtime proof remain held for UI after PM/user accepts these Art/Atlas slices.
- Some generated sprites include intentionally baked card-art details for target review; runtime labels and numeric values remain TMP/live data per manifest rules.
- This handoff does not change gameplay state, UI behavior, command logic, or M01 runtime flow.
