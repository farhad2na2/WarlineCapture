# Operation Map Hand-Edit Bake

## Scope

- Source scene: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`
- Source commit: `a0e3a3b6b` (`Map`)
- Editor command: `Game/Operation Maps/Bake Current Map (All)`
- Batch entry point: `Game.Editor.OperationMapCurrentMapBaker.Run`

## Baked Output

- Building placements: 814
- Vehicle placements: 22
- Surface cells: 2,097,152
- Static presentation sources: 42,127
- Static presentation chunks: 501
- Presentation content hash: `e138fba4ea5bc7576378bd2e5297a6f8`
- Stale chunks removed during refresh: 99
- Local Addressables map bundles: 127
- Local Addressables map bytes: 145,245,659

## Validation

- One-button bake: passed all 8 stages.
- Deterministic presentation rerun: 501 chunks, 0 scene writes, 0 stale deletions.
- Addressables layout: 501 manifest chunks, 501 generated scenes, 501 presentation entries.
- Focused EditMode tests: 60 passed, 0 failed.
- Current staged spatial-binding validator: passed.
- `git diff --check`: passed.

Detailed transient logs and reports are stored under `/private/tmp`:

- `opmap-bake-current-map-all-final.log`
- `operation-map-current-map-bake-report.json`
- `opmap-map-edit-focused-tests.xml`
- `opmap-map-edit-spatial-validator.log`
