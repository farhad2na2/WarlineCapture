# M01 AI Production Asset Pack

## Purpose

This is the runtime folder for ready-to-implement M01 AI-generated production PNG assets.

Art/Atlas owns this folder until the production asset pack is delivered and approved.

## Required Runtime Asset Families

- `Strategic/` - big zoomed-out strategic/base-layout background matching `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`; do not generate Tehran, do not bake finished/destroyed buildings or shells, and cover a large operational base area with clear placement zones for separate refinery/fuel module, soldier tents/camp, vehicle motor pool, command/support pad, staging/training area, roads/service lanes, and defensive/perimeter space.
- `TacticalMaps/` - zoomed-in tactical map plates, including native AI source and POT-padded Unity-ready PNGs.
- `Markers/` - individual transparent marker PNG sprites plus atlas sheet/manifest.
- `Units/PlayerRifleSquad/` - transparent player rifle squad atlas frames.
- `Units/EnemyPatrol/` - transparent enemy patrol atlas frames.
- `Buildings/` - transparent building/prop atlas states: intact, damaged, destroyed.
- `Manifests/` - asset ids, atlas state ids, import usage, scale anchors, prompt/source notes, and approval status.

## Quality Rule

Assets must be AI-generated or AI-assisted at production quality. Do not use deterministic vector placeholders, review-board crops, or low-detail filler.

Zoom level, camera angle, composition, background density, soldier/building scale, marker footprint, and visual style must follow `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png` and the approved VisualLock package. Do not generate Tehran or invent a different city/camera direction.

Do not create smaller soldiers, smaller buildings, different building designs, or different soldier styles. Assets in this folder must preserve the approved visual family and scale relationships.

Do not combine player and enemy factions in one unit atlas. Each unit atlas must include complete idle, run, aim, shoot/fire, hit/damaged, and die/death animation frames for every required facing direction.

The strategic/base-layout map is rejected if it reads as a dense grid of small lots. It must include a review overlay/contact sheet that labels the large placement zones before separate assets are placed.
