# SCN-19 Armory Layered Regeneration Pack

Status: `FinalHighEndTargetAndLayerPackGenerated`

This package follows the SCN-08/SCN-14 one-go layer workflow. The flattened high-end reference target is available in `reference/`, and `layers/` contains Unity-consumable sprites for the Armory shell, roster cards, category tabs, resource frames, tier pips, buttons, icons, and content art.

## Required Output

- `reference/SCN-19_Armory_Landscape_Target.png` - final high-end reference target.
- `generated_one_go/source/generated_layer_atlas_chromakey.png` - chroma-key style atlas source.
- `generated_one_go/source/generated_layer_atlas_alpha.png` - alpha layer atlas.
- `generated_one_go/layers_contact_sheet.png` - layer review sheet.
- `layers/` - separated implementation PNGs.
- `layer_manifest.json` - layer contract, Unity destinations, sprite import settings, and 9-slice hints.
- `copy_layers_to_unity.py` - dry-run-first staging helper.

## Alignment Requirements

- Bind item cards from `PlayerInventory`, combat balance config ids, and visual config art.
- TMP text remains live; no item names, target ids, values, or disabled reasons are baked into reusable sprites.
- Upgrade CTAs remain disabled until inventory, upgrade application, GearModule spending, and validation services exist.
- Item detail opens `POP-09 Ability / Upgrade Detail`.
- No Tokens, gems, Intel Keys, SagaStars, or direct Operation metric grants.

## Generated Assets

Current generated layer count: 45.

Inspect `generated_one_go/layers_contact_sheet.png` before Unity import. Frame, tab, button, and card layers have 9-slice hints in `layer_manifest.json`.

## Generation Prompt

Use `prompts/high_end_target_and_layers.md`.

## Unity Staging

Dry run:

```bash
python3 Design/VisualLockLayered/SCN-19_Armory/copy_layers_to_unity.py
```

Copy without overwriting existing Unity assets:

```bash
python3 Design/VisualLockLayered/SCN-19_Armory/copy_layers_to_unity.py --apply
```
