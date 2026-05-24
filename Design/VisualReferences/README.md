# WarlineCapture Visual References

Date: 2026-05-05

This folder holds current visual references that support the 3D RTS direction.

## Active Production Direction

WarlineCapture's gameplay-facing art direction is a stylized 3D tactical RTS using authored Unity scenes, 3D units, 3D buildings, and explicit walkable area contracts.

Scene art provides the terrain visuals. Separate metadata provides gameplay truth: walkable zones, blockers, road graph, sockets, spawns, objectives, minimap, and camera bounds.

## Folders

- Current gameplay references should be tied to the active 3D `Game` scene work, scene builder handoffs, or `Design/VisualLock`.
- Historical 2D isometric references were removed from active folders. If needed for audit, use `Design/Archive/Legacy2D_2026-05-21`.

## Removed Attempts

The 2D isometric scene, macro-tile, atlas, and sprite-presenter attempts were removed from the active project because they do not match the selected 3D RTS production direction.

Do not recreate those paths unless a future decision explicitly reopens them.

## Rule

Generated visual references are direction locks, not automatic production assets. Production acceptance requires Unity validation for scale, readability, metadata alignment, memory, and gameplay compatibility.
