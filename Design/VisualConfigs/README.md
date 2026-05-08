# WarlineCapture Visual Configs

Date: 2026-05-05

This folder contains visual-only companion configs. These files own art and presentation references only: world asset paths, UI icon paths, portrait paths, damage states, animation ids, VFX/audio cue ids, silhouette rules, and art briefs.

## Files

- `WarlineCapture_Combat_Visual_Config_v0_1.json` - visual companion for the combat catalog in `../BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`.

## Rules

- Do not place costs, HP, damage, cooldowns, production time, upgrade cost, reward amounts, or economy tuning values in visual configs.
- Link back to balance data only through `visualCatalogId` and `entityId`.
- Visual entries can point at implemented runtime prefabs or concrete required production paths for future 2D isometric assets.
- Missing produced art is an asset-production task when the visual entry already has a concrete path, art brief, and silhouette rules.
