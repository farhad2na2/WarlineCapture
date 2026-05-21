# Art/Atlas M01 V5 Readability/Shadow Iteration

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; improved readability/shadows
Priority: P0

## Lane

Art/Atlas

## Task

Iterate M01 units after user review: soldier bodies were hard to distinguish from the background and shadows were not visible enough.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_asset_manifest_v5.json`
- Main production manifest pointer: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json` key `target_match_v5`
- Review README update: `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_assets_v5_contact.png`
- Composite: `Design/AgentReports/Captures/M01_TargetMatchV5StrongShadow_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV5StrongShadow_AssetPlacementReview_vs_Target_Comparison.png`
- Diff heatmap: `Design/AgentReports/Captures/M01_TargetMatchV5StrongShadow_AssetPlacementReview_vs_Target_DiffHeat.png`

## What Changed

- Retained the v3 clean plate because it still compares better than the v4 plate.
- Regenerated player and enemy units with brighter matte blue-grey planes, stronger helmet/shoulder readability, and less black-noise blending.
- Generated a separate imagegen-sourced shadow sheet instead of baking shadows into the body sprites.
- Strengthened the shadow alpha from the selected imagegen shadow source so shadows are visible in the placement composite.

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected v5 files:

- Player readable units: `ig_061caec3064fc95a016a09de5103388198a7d22ba3747b514f.png`
- Enemy readable units: `ig_061caec3064fc95a016a09debf179c819898dbe97f1f81e0be.png`
- Separate unit shadows: `ig_061caec3064fc95a016a09df3434b4819883a37a0701ecea4e.png`

## Comparison Metrics

- Full-frame MSE: `910.63`
- World crop MSE: `671.18`
- Player region MSE: `923.64`
- Enemy region MSE: `856.81`

The MSE is slightly worse than the v3 baseline because stronger unit readability and visible shadows intentionally diverge from darker target pixels. Visually, the requested readability issue is improved.

## Gameplay Binding Checklist

- Plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v3_source_1920x1080.png`
- Player atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/TargetMatchV5/player_rifle_squad_idle_facings_atlas_v5.png`
- Enemy atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/TargetMatchV5/enemy_patrol_idle_facings_atlas_v5.png`
- Strong shadow atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/TargetMatchV5/unit_shadow_facings_atlas_v5_strong.png`
- Markers: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/selection_ring_v5.png`, `selected_squad_status_v5.png`, `enemy_readability_ring_v5.png`, `enemy_health_bar_v5.png`
- Frame contract: four idle facings, 256x256 cells, atlas rects `[0,0,256,256]`, `[256,0,256,256]`, `[512,0,256,256]`, `[768,0,256,256]`, pivot/foot anchor `[128,210]`.
- Do not bind diagnostic target crops, comparison panels, diff heatmaps, or any pasted/cropped target mockup pixels as runtime art.

## Validation

- Parsed `m01_target_match_asset_manifest_v5.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared review files exist: passed.
- PNG dimensions sampled:
  - player atlas `1024x256`
  - enemy atlas `1024x256`
  - strong shadow atlas `1024x256`
  - contact sheet `720x872`
  - placement composite `1920x1080`
  - comparison panel `3840x1126`
- Scanned v5 transparent unit/shadow PNGs for opaque chroma-green residue: `M01_V5_GREEN_REMAINING 0`.
- `git diff --check` for the v5 package/report paths: passed.
- No Unity runtime code, prefabs, scenes, UI implementation, or `Assets/` imports modified.
- Final visual assets are imagegen-sourced; deterministic postprocess was limited to chroma cleanup, sprite splitting/resizing, shadow alpha tuning from imagegen shadow art, contact-sheet packaging, comparison packaging, and validation.

## Assessment

V5 is a better readability/shadow candidate than v3/v4, but still not target-perfect. The largest remaining target-match gap is the background plate composition, especially the exact road/building/corner layout behind the player and enemy regions.

## Handoff Status

Needs PM/user art approval before Gameplay binds this candidate. If accepted, route Gameplay to import the v5 unit/body/shadow/marker set through the existing ECS/runtime presentation path and regenerate the M01-01 target-match proof.
