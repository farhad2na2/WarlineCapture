# M01 Step-By-Step Gameplay Mockups Source Notes

Date: 2026-05-14
Lane: Art/Atlas
Status: revised approval sample - imagegen reference pass

## Revision Summary

This revision applies the latest PM process rejection requiring a fresh imagegen visual pass rather than deterministic PNG patching. The two sample frames were regenerated as cohesive imagegen bitmap targets:

- `M01-01_TacticalStart_1920x1080.png`
- `M01-02_SquadSelected_1920x1080.png`
- `M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`

The imagegen source files retained under Codex generated images are:

- M01-01 generated source: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_03458752e3125a38016a060e66b2408191bbd3e0692ea215f4.png`
- M01-02 generated source: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_03458752e3125a38016a060dfbbf608191b5621d93b4ab0122.png`

The generated frames were resized to the required `1920x1080` project output size. No scripted marker/UI compositing, manual overlay drawing, or pixel-patch workflow was used to create the flattened gameplay frame artwork for this pass.

## Current Marker Revision

The rejected deterministic selected-marker pass has been replaced. The selected markers in `M01-02_SquadSelected_1920x1080.png` are now imagegen-native scene elements: four cyan segmented isometric foot rings with transparent centers, integrated terrain lighting, and no patched-on marker shapes.

This pass also restores the original reference's world-view readability treatment:

- M01-02 adds a selected-squad cyan shield icon above the player squad.
- M01-02 adds a segmented cyan selected-squad status/health bar above the player squad.
- M01-02 does not add an extra visible squad-name label above the soldiers.
- M01-01 and M01-02 both include permanent enemy-affiliation red foot rings and red segmented above-head health bars.
- Enemy red rings/health bars are not attack-target command markers; attack markers remain hidden until a future attack-preview state.

## Current HUD Revision

Both sample frames were regenerated with the HUD chrome integrated into the imagegen frame against the original VisualLock references:

- objective, threat feed, resource/top bar, squad tray, command bar, Build disabled treatment, and minimap frame use darker layered glass/metal, restrained cyan trim, inner highlights, and clearer panel depth
- command buttons use the requested family/order: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`
- Build remains disabled/secondary for M01 using `MissionDoesNotAllowBuild`, and no oversized Build block dominates the command rail
- all HUD text/icons remain runtime-separate implementation targets; the flattened PNGs are visual references only

## Applied Designer Fixes

- M01-01 and M01-02 preserve the same camera, zoom, world scale, player squad projection, enemy patrol projection, HUD layout, and minimap viewport direction.
- M01-01 has no selection ring, no command mode, no move/attack/objective/invalid markers, M01-only objective text, neutral/disabled command controls, and disabled Build.
- M01-02 adds cyan selection rings and selected squad/card state while keeping Move/Attack/Stop/Hold available but inactive.
- M01-02 now includes four visible imagegen-native per-soldier cyan selected marker rings, each aligned to an individual rifleman foot anchor on the isometric ground plane and matched to the clean VisualLock marker language.
- M01-02 now includes selected-squad world shield/status layers above the soldiers, separate from the bottom squad card.
- Build remains disabled for M01 with the canonical reason `MissionDoesNotAllowBuild`.
- Enemy red read is now explicit in the sample and metadata as permanent restrained unit-affiliation/health treatment, not an attack/target world marker.
- Player and enemy infantry are normalized to the same isometric projection scale. Any width difference in the layer rects is formation spread, not perspective shrink.

## Existing Reusable Source Assets

- Tactical plate candidate: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/TacticalMaps/m01_tactical_plate_a_pot_2048x1024.png`
- Player atlas: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
- Enemy atlas: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`
- Markers:
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/selection_ring.png`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/move_destination.png`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/attack_target.png`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/objective_focus.png`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/invalid_blocked.png`

## Runtime Implementation Notes

The flattened PNGs remain visual QA references only. They are not implementation source.

Before Gameplay implementation, Art/UI still needs to produce or approve:

- a clean no-HUD/no-unit camera plate or runtime terrain capture matching `CameraLock_M01_DefaultStart.json`
- native minimap source and viewport mapping from the same world bounds
- sliced HUD chrome for objective, log, resource, squad tray/cards, command bar/buttons, top controls, and minimap
- separate runtime TMP text/icons/counters/objective ticks/health values/button labels/reason codes
- final unit frame keys, pivots, formation offsets, feet anchors, and contact-shadow split
- final scale proof for player/enemy infantry in runtime source sprites, preserving the no-distance-shrink rule
- implementation validation that selected marker child layers follow the same per-soldier feet anchors during idle/run state transitions

## Held Scope

No remaining frames were generated. The optional M01-05 attack-preview proof remains held unless PM/user requests it. No runtime files, Unity assets, imports, source docs, or other lane task files were modified.
