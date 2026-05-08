# POP-09 Ability / Upgrade Detail Layered Regeneration Pack

Status: `FinalHighEndTargetAndLayerPackGenerated`

This package follows the SCN-08/SCN-14 one-go layer workflow. The flattened high-end reference target is available in `reference/`, and `layers/` contains Unity-consumable sprites for the modal shell, scrim, art panel, detail rows, effect cards, buttons, icons, progress bars, and target preview art.

## Required Output

- `reference/POP-09_AbilityUpgradeDetail_Landscape_Target.png` - final high-end reference target.
- `generated_one_go/source/generated_layer_atlas_chromakey.png` - chroma-key style atlas source.
- `generated_one_go/source/generated_layer_atlas_alpha.png` - alpha layer atlas.
- `generated_one_go/layers_contact_sheet.png` - layer review sheet.
- `layers/` - separated implementation PNGs.
- `layer_manifest.json` - layer contract, Unity destinations, sprite import settings, and 9-slice hints.
- `copy_layers_to_unity.py` - dry-run-first staging helper.

## Alignment Requirements

- Accept either `AbilityConfig` or `UpgradeTrackConfig`.
- Bind target id, unlock moment, availability, prerequisite, effect rows, cooldown, charges, parts progress, GearModule requirement, and disabled reason from configs.
- TMP text remains live; no target ids, values, costs, or requirements are baked into reusable sprites.
- The popup is shared by SCN-06, SCN-07, SCN-08, SCN-10, SCN-14, SCN-19, POP-04, and POP-08.
- No Tokens, gems, Intel Keys, SagaStars, or direct Operation metric grants.

## Generated Assets

Current generated layer count: 26.

Inspect `generated_one_go/layers_contact_sheet.png` before Unity import. Modal, row, card, and button layers have 9-slice hints in `layer_manifest.json`.

## Generation Prompt

Use `prompts/high_end_target_and_layers.md`.

## Unity Staging

Dry run:

```bash
python3 Design/VisualLockLayered/POP-09_AbilityUpgradeDetail/copy_layers_to_unity.py
```

Copy without overwriting existing Unity assets:

```bash
python3 Design/VisualLockLayered/POP-09_AbilityUpgradeDetail/copy_layers_to_unity.py --apply
```
