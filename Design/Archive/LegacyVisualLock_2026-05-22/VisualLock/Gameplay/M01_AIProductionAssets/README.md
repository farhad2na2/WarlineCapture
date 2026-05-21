# M01 AI Production Assets Review Mirror

## Purpose

This folder mirrors the M01 ready-to-implement AI-generated production asset pack for review.

Runtime assets must live under:

`Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

This review mirror should contain contact sheets, direct PNG previews, prompt/source notes, and manifests only. It must not replace the runtime asset folder.

## Required Review Coverage

Art/Atlas must provide high-quality AI-generated or AI-assisted assets for:

- big zoomed-out strategic/base-layout background matching `VL_M01_TacticalMap_Target.png`; no Tehran, no closed walled compound/fortress/island base, no concept switch away from the previous city-like strategic map, no finished/destroyed buildings or shells baked into reserved zones, not a dense grid of small lots, and large enough for separate refinery/fuel module, soldier tents/camp, soldier vehicle motor pool, command/support pad, staging/training area, roads/service lanes, and defensive/perimeter space inside an open city/urban-road-grid context,
- M01 zoomed-in tactical map plates,
- marker PNG sprites,
- player rifle squad sprite atlas frames,
- enemy patrol sprite atlas frames,
- building PNG atlas states,
- scale and import manifests.

## Rejected Output Types

Do not use:

- deterministic vector marker boards,
- placeholder crops from a concept image,
- low-detail diagrams,
- stretched or upscaled source images,
- board-only VisualLock references with no runtime PNG assets,
- Tehran-map outputs or any new city/camera/zoom direction that does not follow the approved `M01_SelectedReadability_*` reference package,
- smaller soldiers, smaller buildings, different building designs, or different soldier styles than the approved reference package,
- player and enemy/faction variants combined in one unit atlas,
- partial unit animation sets missing idle, run, aim, shoot/fire, hit/damaged, or die/death for any required facing direction,
- static one-frame-per-state soldier sheets posing as animation,
- soldier frames rotated or angled differently from `VL_M01_TacticalMap_Target.png`,
- strategic/base-layout maps without an annotated overlay/contact sheet labeling refinery/fuel zone, tents/camp zone, vehicle motor-pool zone, command/support zone, staging/training zone, perimeter/defense lanes, roads, and city-block continuity,
- strategic/base-layout maps that read as a closed walled compound, fortress, island base, or isolated military installation instead of the previous city-like map direction.

## M01 Gameplay Target-Match V3 Package

The 2026-05-17 Art/Atlas v3 package responds to the Gameplay v5 blocker report by adding a focused target-match source set for M01-01/M01-02 battlefield binding:

- clean no-HUD/no-unit tactical start plate
- POT-padded tactical plate candidate
- player rifle squad idle facing atlas with baked per-frame contact shadows
- enemy patrol idle facing atlas at the same projection scale with restrained red accents
- cyan selected-ring and selected-squad status overlays
- red enemy foot-readability and segmented health-bar overlays
- focused contact sheet and binding manifest

V3 manifest:

- `Manifests/m01_target_match_asset_manifest_v3.json`

V3 contact sheet:

- `ContactSheets/m01_target_match_assets_v3_contact.png`

All v3 final visual assets are imagegen-sourced. Deterministic tooling was limited to source copy, chroma-key alpha removal, crop extraction, resizing into existing sprite-cell contracts, POT padding, contact-sheet packaging, metadata, inspection, and validation. Do not use target mockup crops, pasted screenshots, comparison images, or deterministic/vector/programmatic substitutes as runtime art.

## M01 Gameplay Target-Match V5 Readability/Shadow Candidate

The 2026-05-17 v5 package responds to user review that the soldiers were too hard to distinguish from the tactical background and that the contact shadows were not visible enough.

- retains the v3 clean tactical plate because the v4 plate compared worse
- regenerates player/enemy idle units with stronger local contrast and less muddy background blending
- provides a separate imagegen-sourced unit shadow atlas, including a stronger readability candidate
- keeps marker/readability overlays grouped with the v5 candidate
- includes placement review, target comparison, diff heatmap, and binding metadata

V5 manifest:

- `Manifests/m01_target_match_asset_manifest_v5.json`

V5 contact sheet:

- `ContactSheets/m01_target_match_assets_v5_contact.png`

V5 is a review candidate, not an approved target-perfect lock. The stronger shadows improve visual readability but score slightly worse in pixel MSE because they diverge from the target pixels. Do not route Gameplay to bind v5 until PM/user accepts this Art/Atlas candidate.

## M01 Soldier Animation V5 Candidate

The 2026-05-17 animation v5 package extends the v5 readability direction into full player and enemy animation atlases:

- four facings: NE, SE, SW, NW
- six states: idle, run, aim, fire, damaged, death
- 112 frames per faction using the previous 4096x1792, 256px-cell atlas layout
- separate 112-frame strong contact-shadow atlas with matching frame rects and pivots

V5 animation manifest:

- `Manifests/m01_soldier_animation_manifest_v5.json`

V5 animation contact sheet:

- `ContactSheets/m01_soldier_animation_v5_contact.png`

Shadows are separate from the body atlases so Gameplay can tune opacity independently during target-match review. Body sprites should be rendered above the shadow atlas frames using the same atlas rect and `[128,210]` foot pivot.
