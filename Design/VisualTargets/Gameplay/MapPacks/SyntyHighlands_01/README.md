# SyntyHighlands_01 Map Pack

Date: 2026-05-24

Status: Candidate v1 for gameplay/editor validation.

This pack is a 2024x2024 top-down Synty/POLYGON-style terrain and mask set for the active 3D single-map WarlineCapture direction.

## Files

| File | Use |
|---|---|
| `base_visual.png` | Visible terrain/reference plate. Place under the 3D operation map or use as a terrain painting guide. |
| `blocker_mask.png` | Pathfinding blocker reference. Black is walkable; white is blocked. |
| `tree_density_mask.png` | Tree/scrub placement density. Black is none; brighter values mean denser placement. |
| `rock_density_mask.png` | Rock/boulder/cliff placement density. Black is none; brighter values mean denser placement. |
| `height_mask.png` | Terrain height reference. Dark is low/flat; light is high/mountain/cliff. |
| `map_pack_manifest.json` | Machine-readable notes for editor tooling. |

## Layout Intent

- Large city/town reserve: center/center-left, open and mostly flat.
- Northwest military base reserve: open flat pad for future tents, command structures, vehicles, helipad, and barricades.
- Southeast military base reserve: second open flat pad far from the northwest base.
- Movement lanes: broad open paths connecting both base reserves and the city reserve.
- Natural blockers: mountain ridges, cliffs, boulder fields, dense trees, and jungle/woodland belts placed inside the edge, not as a perfect border.

## Gameplay Rules

- Treat `blocker_mask.png` as the pathfinding source of truth.
- Treat `base_visual.png` as visual/reference only.
- Do not bake gameplay buildings, units, selection rings, health bars, objectives, UI, or VFX into this map image.
- Keep city/base reserves clear during tree/rock spawning unless a later designer-authored zone overrides it.
- Use editor-time generation first, save generated metadata, and validate with debug overlays before runtime use.

See `Design/3D_Operation_Map_Texture_Mask_Workflow.md` for thresholds and sampling rules.

