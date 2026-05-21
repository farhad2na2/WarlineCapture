# Art/Atlas M01 V28 Target Scale Hard Shadow Full Baked Atlases

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; needs PM/user visual approval
Priority: P0

## Summary

V28 responds to the latest user rejection of overly solid/soft shadows and lower-quality soldier shine. It uses a fresh built-in-imagegen soldier source with darker glossy armor, then repacks that source at target scale with baked non-solid, harder horizontal-right boot shadows.

V26 and the first V27 proof should not be routed as final. V26 remained too miniature/cutout-like against the target, and V27 was oversized in the placement proof. V28 is the current review candidate.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v28.json`
- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v28.png`
- Player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v28.png`
- Player idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v28.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v28.png`
- Enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v28.png`
- Enemy idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v28.png`
- Player numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v28_player_animation_atlas_numbered_grid.png`
- Enemy numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v28_enemy_animation_atlas_numbered_grid.png`
- Combined contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v28_target_scale_hard_shadow_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV28TargetScaleHardShadowIdlePlacement_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV28TargetScaleHardShadowIdlePlacement_vs_Target_Comparison.png`
- Target-aligned crop review: `Design/AgentReports/Captures/M01_TargetMatchV28TargetScaleHardShadowIdlePlacement_TargetAlignedCropReview.png`

## Imagegen Provenance

- Built-in imagegen original: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_0254c1f907ed28fc016a0b2c9ea5a88199b404ce515507ccd0.png`
- Workspace chroma source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV27/m01_v27_direction_locked_animation_source_chromakey.png`
- Workspace alpha source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV27/m01_v27_direction_locked_animation_source_alpha.png`

Postprocess:

- chroma-key alpha cleanup from the imagegen source
- target-scale atlas packing into the existing `256x256`, `16x7`, `112` frame layout
- dark glossy tone grade preserving source-derived armor shine
- non-solid harder baked contact shadows in every frame
- no artificial helmet dots, head glints, square shadow boxes, or separate runtime shadow atlas

No target mockup crops were pasted into delivered art.

## Validation

- Manifest parse: passed.
- Manifest-declared missing files: `0`.
- Source pose extraction: `12` player poses and `12` enemy poses.
- Clean body cell failures: `0` player, `0` enemy.
- Player animation atlas: `4096x1792` RGBA.
- Enemy animation atlas: `4096x1792` RGBA.
- Idle/facing atlases: `1024x256` RGBA each.
- Contact sheets: generated.
- Placement proof and target comparison: generated.

## Visual Assessment

- Player direction reads as `player_bottom_faces_up_screen`.
- Enemy direction reads as `enemy_top_faces_down_screen`.
- Soldier bodies are darker and glossier than V25/V26, with source-derived specular highlights instead of drawn-on head artifacts.
- V28 scale is reduced from the oversized first V27 proof and is closer to the target screen-space read.
- Shadows are baked into the atlases, connected at the boots, horizontal-right, and harder than the previous soft streaks without becoming solid black blocks.

Remaining approval risk:

- The body style is closer to the target mockup but still comes from a generated source, so PM/user visual approval is required before Gameplay binding.
- The proof health bars/rings are only review overlays; delivered art is the body+shadow atlas.

## Gameplay Binding Checklist

- Bind player animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v28.png`
- Bind enemy animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v28.png`
- Optional idle/facing atlases:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v28.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v28.png`
- Cell size: `256x256`.
- Atlas columns: `16`.
- Atlas rows: `7`.
- Frames per unit: `112`.
- Pivot: `[128,212]`.
- Direction keys: `screen_locked_A`, `screen_locked_B`, `screen_locked_C`, `screen_locked_D`.
- Direction mapping is screen-space locked: all player keys read `player_bottom_faces_up_screen`; all enemy keys read `enemy_top_faces_down_screen`.
- Do not bind V18 through V27 as final.
- Do not bind V7 through V16 animation atlases.
- Do not bind separate TargetMatchV5 soldier shadow atlas.

## Routing

V28 is ready for PM/user visual review. If accepted, route Gameplay to bind the V28 body+shadow atlases and regenerate the runtime proof.
