# ISO-04 Terrain Visual Target

Date: 2026-05-05

This folder contains the polished terrain visual target for WarlineCapture's 2D isometric battlefield direction.

## Files

- `ISO04_TerrainVisualTarget.png`
  - High-quality terrain-only visual target for road, curb, plaza, panel, lane-marking, and surface-detail style.

## Purpose

ISO-04 exists because the ISO-03 clean terrain pass was useful for technical road connectivity, but too plain to judge the intended mockup-quality result.

Use ISO-04 to approve the visual direction first. After that, derive the modular Unity tile kit from this look instead of treating the plain ISO-03 tiles as final art.

## Acceptance Questions

- Do the roads feel clean, connected, and professional enough for the target mobile RTS style?
- Do the curbs, panel seams, road markings, drains, and surface wear match the premium mockup direction?
- Is the terrain detailed enough to look high quality, but quiet enough to sit behind units and HUD?
- Should this become the source direction for the modular terrain tile kit?

## Rule

This is a visual target, not an imported modular gameplay tile set. Do not wire it into runtime or Jenkins. Use it to guide the next modular terrain asset batch.
