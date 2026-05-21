# Art/Atlas M01 V18 Direction-Locked Baked Soldiers

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; idle-only direction proof needs PM/user approval
Priority: P0

## Summary

V18 responds to the Gameplay/PM v17 direction blocker. The package provides direction-locked idle cells for the actual M01 screen-space read:

- `player_bottom_faces_up_screen`
- `enemy_top_faces_down_screen`

This is an idle-only proof package. Full direction-locked animation cycles remain blocked until the idle direction read is approved.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v18.json`
- Combined direction atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/m01_direction_locked_idle_body_shadow_atlas_v18.png`
- Player clean cell: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_clean_body_atlas_v18.png`
- Player baked-shadow cell: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_body_shadow_atlas_v18.png`
- Enemy clean cell: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/EnemyPatrol/enemy_patrol_idle_direction_locked_clean_body_atlas_v18.png`
- Enemy baked-shadow cell: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/EnemyPatrol/enemy_patrol_idle_direction_locked_body_shadow_atlas_v18.png`
- Direction contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v18_direction_locked_idle_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV18DirectionLockedIdle_AssetPlacementReview_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV18DirectionLockedIdle_vs_Target_Comparison.png`

## Imagegen Provenance

Source visuals are built-in-imagegen-derived clean soldier components from the V17/V5 source chain.

- Player source components: V17 player clean body atlas indices `56` and `84`, selected as the two back-read idle facings.
- Enemy source components: V17 enemy clean body atlas indices `0` and `28`, selected as the two front/down-read idle facings.

Postprocess was limited to direction-lock composition from clean source components, alpha preservation, pivot recentering, directional-dark treatment, baked horizontal-right shadow, atlas packing, contact sheets, and validation. No target mockup crops were pasted into delivered art.

## Direction Mapping

| Direction key | Faction | Atlas rect | Pivot | Screen-space read |
| --- | --- | --- | --- | --- |
| `player_bottom_faces_up_screen` | player rifle squad | `[0,0,256,256]` in player cell, `[0,0,256,256]` in combined atlas | `[128,210]` | bottom/player soldier faces up-screen toward tactical field |
| `enemy_top_faces_down_screen` | enemy patrol | `[0,0,256,256]` in enemy cell, `[256,0,256,256]` in combined atlas | `[128,210]` | top/enemy soldier faces down-screen toward player squad |

## Dimensions

- Player direction cell: `256x256`
- Enemy direction cell: `256x256`
- Combined atlas: `512x256`
- Direction contact sheet: `900x380`
- Placement proof: `1920x1080`
- Target comparison: `3840x1080`

## Validation

- Manifest parse: passed.
- Manifest-declared missing files: `0`.
- Alpha-missing declared unit PNGs: `0`.
- Player clean-cell validation: passed, one large component, bbox `[99,93,157,211]`.
- Enemy clean-cell validation: passed, one large component, bbox `[101,93,155,211]`.
- No merged/two-half-frame contamination in the delivered V18 idle cells.
- Shadows are baked into the V18 cells; no separate runtime shadow atlas is required.

## Shadow Assessment

The V18 idle cells retain the horizontal-right baked contact shadow direction from the corrected V17/V16 shadow family. The shadow is anchored under the feet and extends right, matching the user correction that shadows should not drift down-right.

## Gameplay Binding Checklist

- For player bottom squad direction proof, bind:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_body_shadow_atlas_v18.png`
  - direction key: `player_bottom_faces_up_screen`
  - frame rect: `[0,0,256,256]`
  - pivot: `[128,210]`
- For enemy top squad direction proof, bind:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/EnemyPatrol/enemy_patrol_idle_direction_locked_body_shadow_atlas_v18.png`
  - direction key: `enemy_top_faces_down_screen`
  - frame rect: `[0,0,256,256]`
  - pivot: `[128,210]`
- Combined atlas option:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV18/m01_direction_locked_idle_body_shadow_atlas_v18.png`
  - cell `0`: player bottom faces up-screen
  - cell `1`: enemy top faces down-screen
- Binding mode: idle-only, baked body+shadow.
- Do not bind V7 through V16 animation atlases.
- Do not treat V17 facings as final M01-01 direction approval.

## Remaining Blocker

Full animation cycles are not direction-locked in V18. If PM/user accepts the idle screen-space direction read, Art/Atlas must either:

- generate full direction-locked animation cycles for the accepted player/enemy screen-space directions, or
- route Gameplay to use V18 idle-only for the M01-01 no-selection proof while animation work remains held.

## Assessment

V18 is ready for PM/user direction approval. It specifically tests the screen-space reads requested by PM rather than relying on `NE/SE/SW/NW` compass labels.
