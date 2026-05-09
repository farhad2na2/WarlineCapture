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
