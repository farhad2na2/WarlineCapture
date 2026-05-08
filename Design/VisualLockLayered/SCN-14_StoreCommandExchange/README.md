# SCN-14 Store / Command Exchange Layered Regeneration Pack

Status: `HighEndTargetAndLayerAtlasGenerated`

This package follows the proven `SCN-08_RTSBattleHUD` one-go layer export workflow. The current reference image is the high-end regenerated Store / Command Exchange visual target, and `layers/` contains extracted candidate implementation sprites from the generated alpha atlas.

## Required Output

- `reference/SCN-14_Store_CommandExchange_Target.png` - regenerated high-end reference target.
- `generated_one_go/source/generated_layer_atlas_chromakey.png` - generated layer atlas source.
- `generated_one_go/source/generated_layer_atlas_alpha.png` - alpha-cleaned atlas.
- `generated_one_go/layers_contact_sheet.png` - layer review sheet.
- `layers/` - separated candidate implementation PNGs.
- `layer_manifest.json` - layer contract, Unity destinations, and 9-slice hints.
- `copy_layers_to_unity.py` - dry-run-first staging helper.

## Alignment Requirements

- Remove `Tokens`, `120 Tokens`, and `Intel Keys`.
- Use only canonical resources/rewards from `WarlineCapture_Economy_Reward_Design.md`.
- Store grants must be deterministic and must not directly grant SagaStars or Operation metric deltas.
- Product content follows `Design/Monetization/WarlineCapture_Monetization_Store_Catalog.md`.
- TMP text remains live text in Unity; reusable sprites must not bake labels or values.

## Generated Assets

Current generated layer count: 28.

The atlas and extracted layers are high-quality candidates. Before Unity import, inspect the layer contact sheet and each PNG for alpha edges, unwanted baked text, and 9-slice suitability.

## Generation Prompt

Use `prompts/high_end_target_and_layers.md`.

## Unity Staging

Dry run:

```bash
python3 Design/VisualLockLayered/SCN-14_StoreCommandExchange/copy_layers_to_unity.py
```

Copy without overwriting existing Unity assets:

```bash
python3 Design/VisualLockLayered/SCN-14_StoreCommandExchange/copy_layers_to_unity.py --apply
```
