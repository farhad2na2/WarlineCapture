# POP-05 Mission Result Variant Layer Pack Prompt V01

Use the active `VisualLockLayered V15 3D Green-Screen Workflow`.

Surface id: `POP-05_MissionResult`
Layer pack: `generated_variants_v01`
Purpose: variant-specific implementation source assets for `PartialSuccess`, `DefeatFailed`, and `Withdrawn`.

Existing shared pack:

- `layer_manifest.json`
- `layers/`
- `generated_v01/source/POP-05_Frames_Green.png`
- `generated_v01/source/POP-05_Icons_Green.png`

Do not regenerate or duplicate the shared POP-05 frame shell unless a state-specific asset cannot be made by tinting/reusing existing frames in Unity.

Target references:

- `reference/POP-05_MissionResult_PartialSuccess_Target.png`
- `reference/POP-05_MissionResult_Defeat_Target.png`
- `reference/POP-05_MissionResult_Withdrawn_Target.png`

Required source outputs:

1. Opaque full-screen no-UI background art:
   - `POP-05_PartialSuccess_Background_21x9_NoUI.png`
   - `POP-05_Defeat_Background_21x9_NoUI.png`
   - `POP-05_Withdrawn_Background_21x9_NoUI.png`
2. Opaque rectangular mission snapshot art:
   - `POP-05_PartialSuccess_MissionSnapshot.png`
   - `POP-05_Defeat_MissionSnapshot.png`
   - `POP-05_Withdrawn_MissionSnapshot.png`
3. Green-background icon/accent sheet:
   - `POP-05_ResultVariants_IconsAndAccents_Green.png`

Green-sheet required separated items:

- `pop05_variant_icon_failed_x`
- `pop05_variant_icon_warning_triangle`
- `pop05_variant_icon_abandoned_square`
- `pop05_variant_icon_extracted_arrow`
- `pop05_variant_icon_unknown_question`
- `pop05_variant_icon_disabled_lock`
- `pop05_variant_star_dim_large`
- `pop05_variant_star_partial_gold_large`
- `pop05_variant_marker_objective_abandoned`
- `pop05_variant_marker_squad_extracted`
- `pop05_variant_marker_civilian_unresolved`
- `pop05_variant_retry_chevrons`
- `pop05_variant_return_map_icon`
- `pop05_variant_main_menu_arrow`
- `pop05_variant_adjust_loadout_helmet`
- `pop05_variant_failure_header_warning_accent`
- `pop05_variant_withdraw_header_accent`
- `pop05_variant_partial_header_accent`
- `pop05_variant_disabled_reward_overlay`

Layer rules:

- No baked dynamic text, no numbers, no reward amounts, no route labels.
- Button backgrounds must not include labels.
- Icons must be separate from panels and rows.
- Backgrounds and mission snapshots are opaque rectangular art.
- Green sheet must use a flat pure `#00ff00` background with no texture, shadow, or gradient.
- Do not crop or cut target-lock references into implementation layers.
