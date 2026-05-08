# SCN-08 RTS Battle HUD One-Go Layer Export

This folder now contains a regenerated one-go layer export for the RTS battle HUD.

- `layers/` contains the generated separated PNG assets for review.
- `layer_manifest.json` maps each PNG to its Unity Canvas role, target object path, proposed asset destination, import mode, and 9-slice border hints.
- `copy_layers_to_unity.py` is a dry-run helper for staging these PNGs under `Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo`.
- `generated_one_go/source/generated_layer_atlas_chromakey.png` is the original generated atlas.
- `generated_one_go/source/generated_layer_atlas_alpha.png` is the same atlas after chroma-key removal.
- `generated_one_go/layers_contact_sheet.png` is only a review sheet.
- `reference/SCN-08_RTSBattleHUD_Landscape_Target.png` is the previous target used as style reference.

Run the helper in dry-run mode:

```bash
python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py
```

Copy without overwriting existing Unity assets:

```bash
python3 Design/VisualLockLayered/SCN-08_RTSBattleHUD/copy_layers_to_unity.py --apply
```

The manifest intentionally keeps the generated files in a new `LayeredOneGo` asset root so the current Match HUD implementation is not overwritten until the canvas builder is updated and validated.
