# SCN-08 RTS Battle HUD One-Go Layer Export

This folder now contains a regenerated one-go layer export for the RTS battle HUD.

Status: `ReadyForReview_AlphaQualityFix_SCN08`

The 2026-05-16 Art/Atlas pass adds imagegen-sourced implementation slices for the SCN-08 pieces UI v4 identified as missing or below target quality: squad portraits/card art, shield/rank badges, objective icons, threat rows/icons, minimap content/viewport/zoom controls, clock icon, and command/minimap chrome.

Alpha-quality update: the SCN-08 slices were cleaned after UI v5 rejection to remove green chroma-key edge spill from objective/threat frames, squad cards, command rail/buttons/icons, minimap chrome/content, and related HUD trim. No runtime code, Unity prefabs, or `Assets/` imports were changed by this Art/Atlas package update.

- `layers/` contains the generated separated PNG assets for review.
- `layer_manifest.json` maps each PNG to its Unity Canvas role, target object path, proposed asset destination, import mode, and 9-slice border hints.
- `copy_layers_to_unity.py` is a dry-run helper for staging these PNGs under `Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo`.
- `generated_one_go/source/generated_layer_atlas_chromakey.png` is the original generated atlas.
- `generated_one_go/source/generated_layer_atlas_alpha.png` is the same atlas after chroma-key removal.
- `generated_one_go/source/imagegen_scn08_squad_badge_objective_threat_atlas_chromakey.png` is the selected imagegen atlas for squad, badge, objective, clock, and threat slices.
- `generated_one_go/source/imagegen_scn08_squad_badge_objective_threat_atlas_alpha.png` is the chroma-key alpha version of that selected imagegen atlas.
- `generated_one_go/source/imagegen_scn08_minimap_command_chrome_atlas_chromakey.png` is the selected imagegen atlas for minimap and command chrome slices.
- `generated_one_go/source/imagegen_scn08_minimap_command_chrome_atlas_alpha.png` is the chroma-key alpha version of that selected imagegen atlas.
- `generated_one_go/source/imagegen_scn08_complete_slices_contact_sheet.png` is the selected imagegen review contact sheet.
- `generated_one_go/source/imagegen_scn08_m01_command_select_correction_chromakey.png` is the selected imagegen command correction sheet for the M01 `SELECT` command.
- `generated_one_go/source/imagegen_scn08_m01_command_select_correction_alpha.png` is the chroma-key alpha version of that selected command correction sheet.
- `generated_one_go/command_select_correction_contact_sheet.png` is the focused command correction review sheet.
- `generated_one_go/alpha_quality_fix_contact_sheet.png` is the focused alpha-quality review sheet for cleaned imagegen-derived slices.
- `generated_one_go/layers_contact_sheet.png` is only a review sheet.
- `reference/SCN-08_RTSBattleHUD_Landscape_Target.png` is the previous target used as style reference.

New target-quality layers include:

- `layers/squad_portrait_rifle.png`
- `layers/squad_portrait_apc.png`
- `layers/squad_portrait_tank.png`
- `layers/squad_portrait_helicopter.png`
- `layers/shield_badge_cyan.png`
- `layers/objective_checked_square.png`
- `layers/time_clock_icon.png`
- `layers/threat_enemy_spotted_icon.png`
- `layers/threat_row_active_background.png`
- `layers/threat_row_normal_background.png`
- `layers/minimap_content.png`
- `layers/minimap_viewport_rect.png`
- `layers/minimap_zoom_plus_button.png`
- `layers/minimap_zoom_minus_button.png`
- `layers/command_select_icon.png`

## M01 Command Rule

M01 command order is `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.

`SPECIAL` is not part of the M01 command family. `layers/command_special_icon.png` may remain available for generic/non-M01 SCN-08 use, but UI must not bind it into M01.

Run the helper in dry-run mode:

```bash
python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py
```

Copy without overwriting existing Unity assets:

```bash
python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py --apply
```

The manifest intentionally keeps the generated files in a new `LayeredOneGo` asset root so the current Match HUD implementation is not overwritten until the canvas builder is updated and validated.
