# WarlineCapture Balance Configs

Date: 2026-05-05

This folder contains design-facing machine-readable balance configs. These files own gameplay and economy values only: ids, costs, stats, cooldowns, unlock gates, producer relationships, upgrade tiers, and balance tags.

## Files

- `WarlineCapture_Combat_Balance_Config_v0_1.json` - canonical combat catalog for units, buildings, skills, abilities, and upgrade tracks.

## Rules

- Do not place world art paths, icon paths, portrait paths, VFX paths, color palettes, or visual prompts in balance configs.
- Link to visual data only through `visualCatalogId`.
- Every ability must include `availability` and `implementationSpec` blocks with unlock moment, modes, UI surfaces, runtime owner, precondition, locked/disabled state, and validation test names.
- Every upgrade track must include `availability`, `implementationSpec`, and `resolvedItemIds` blocks with unlock moment, source reward types, store eligibility, apply window, runtime owner, target resolution, and validation test names.
- Any `UnitUnlock`, `BuildingUnlock`, `SupportAbilityUnlock`, `GearModule`, or `BlueprintParts` reward target must resolve to an id in this folder or a future sibling catalog.
- Keep long-running tuning checks as report-producing balance probes; keep data shape checks as fast automated tests.
