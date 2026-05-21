# Art/Atlas M01 Soldier Animation V5

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Lane

Art/Atlas

## Task

Generate full player/enemy animation cycles in all required directions from the v5 readability direction.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v5.json`
- Main production manifest pointer: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json` key `soldier_animation_v5`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_v5_contact.png`
- Player atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v5.png`
- Enemy atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/enemy_patrol_animation_atlas_v5.png`
- Separate shadow atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/AnimationV5/unit_shadow_animation_atlas_v5_strong.png`

## Animation Contract

- Facings: `NE`, `SE`, `SW`, `NW`
- States and frames: idle `4`, run `8`, aim `3`, fire `4`, damaged `3`, death `6`
- Frame count: `112` frames per faction
- Atlas layout: `4096x1792`, `256x256` cells, `16` columns, `7` rows
- Pivot/foot anchor: `[128,210]`
- Shadows: separate atlas, same frame rects and pivots as body atlases

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected v5 animation source sheets:

- Player: `ig_035d3737ed5552e2016a09eb46dafc8198b2834fd58db945a3.png`
- Enemy: `ig_035d3737ed5552e2016a09eb9e4a4c8198ac79efaadac2018f.png`
- Shadows: reused v5 strong imagegen-sourced shadow atlas `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/TargetMatchV5/unit_shadow_facings_atlas_v5_strong.png`

Deterministic postprocess was limited to workspace copy, chroma-key alpha cleanup, grid slicing, resizing/normalization into 256px cells, atlas packing, contact-sheet generation, metadata, and validation.

## Validation

- Parsed `m01_soldier_animation_manifest_v5.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared review files exist: passed.
- Player atlas dimensions: `4096x1792`.
- Enemy atlas dimensions: `4096x1792`.
- Shadow atlas dimensions: `4096x1792`.
- Contact sheet dimensions: `2048x2808`.
- Green residue scan on final unit/shadow frames and atlases: `0` files.
- Exact adjacent duplicate frame pairs: `0`.
- Low-motion sequences: `2` enemy idle sequences have low but non-zero adjacent delta and need visual review.
- `git diff --check` for the v5 animation package/report paths: passed.
- No Unity runtime code, prefabs, scenes, UI implementation, or `Assets/` imports modified.
- No target mockup crop, pasted screenshot, comparison panel, or deterministic/vector/programmatic placeholder was used as final runtime art.

## Assessment

The v5 animation package is complete enough for Art/PM review and Gameplay binding after approval. Shadows are intentionally separate rather than baked into the body atlases, because v5 readability feedback requires shadow opacity to remain tunable without repainting unit bodies.

Remaining risk: the generated source sheet provides full cycles, but two enemy idle directions have subtle motion and should be reviewed in motion before acceptance.
