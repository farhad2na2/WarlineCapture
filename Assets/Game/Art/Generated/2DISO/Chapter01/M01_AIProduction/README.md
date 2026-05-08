# M01 AI Production Asset Pack

## Purpose

This is the runtime folder for ready-to-implement M01 AI-generated production PNG assets.

Art/Atlas owns this folder until the production asset pack is delivered and approved.

## Required Runtime Asset Families

- `Strategic/` - big zoomed-out strategic/background map matching the approved `M01_SelectedReadability_*` isometric reference style; do not generate Tehran.
- `TacticalMaps/` - zoomed-in tactical map plates, including native AI source and POT-padded Unity-ready PNGs.
- `Markers/` - individual transparent marker PNG sprites plus atlas sheet/manifest.
- `Units/PlayerRifleSquad/` - transparent player rifle squad atlas frames.
- `Units/EnemyPatrol/` - transparent enemy patrol atlas frames.
- `Buildings/` - transparent building/prop atlas states: intact, damaged, destroyed.
- `Manifests/` - asset ids, atlas state ids, import usage, scale anchors, prompt/source notes, and approval status.

## Quality Rule

Assets must be AI-generated or AI-assisted at production quality. Do not use deterministic vector placeholders, review-board crops, or low-detail filler.

Zoom level, camera angle, composition, background density, soldier/building scale, marker footprint, and visual style must follow the approved `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_*` reference package. Do not generate Tehran or invent a different city/camera direction.

Do not create smaller soldiers, smaller buildings, different building designs, or different soldier styles. Assets in this folder must preserve the approved visual family and scale relationships.
