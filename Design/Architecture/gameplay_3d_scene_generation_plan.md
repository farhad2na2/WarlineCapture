# Gameplay 3D Scene Generation Plan

## Goal

Create good-looking, flat-grid-compatible 3D gameplay scenes from the existing `Demo` and `Game` asset libraries without hand-placing every object.

The first target scene is a small tactical desert town with a highway, military checkpoint, and nearby base edge.

## Process

1. Freeze the first target scene: small desert town plus military checkpoint, highway, and base edge.
2. Build a prefab role and footprint catalog from the existing asset library.
3. Tag prefabs by role: road, town building, military building, wall, fence, cover, vehicle, wreck, industrial, landmark, and decoration.
4. Define scene grammar before placement: highway spine, side roads, town district, military checkpoint, base perimeter, objective zones, cover clusters, open combat lanes, and decorative-only zones.
5. Generate the flat gameplay grid first: walkable cells, blocked cells, road cells, cover cells, objective cells, and spawn zones.
6. Place structural pieces first: highway, town blocks, base perimeter, gates, hangars, barracks, tents, cliffs, and large rocks.
7. Place gameplay pieces second: barriers, sandbags, wrecked vehicles, small cover, checkpoint props, and objective markers.
8. Place decoration last: palms, crates, barrels, debris, lights, signs, and small props.
9. Run validation: reject blocked spawns, unreachable objectives, disconnected roads, excessive empty space, prefab overlaps, floating/sunken objects, off-plane objects, and excessive renderer density.
10. Capture proof images for every generated scene: top-down gameplay grid, top-down rendered view, perspective view, role-colored map, and walkability map.
11. Review generated scenes from proof images, then fix generator rules instead of nudging individual objects.
12. Promote passing scenes by saving the Unity scene, seed/config, report, and reusable template.

## First Scene Grammar

- Highway is the central landmark and divides the scene into town and military sides.
- Town sits west of the highway with clustered buildings, alleys, rock edges, palms, and courtyard gaps.
- Military base sits east of the highway with fenced perimeter, gate, barracks, tents, guard towers, and vehicles.
- Runway/airfield edge sits farther east as a secondary landmark.
- Industrial/fuel props sit southeast of the base as an objective pocket.
- Gameplay remains on a flat plane even when visual rocks/cliffs are used as boundaries.

## Non-Goals For First Pass

- Do not replace the current `Game` scene flow.
- Do not touch M01 V32 soldier runtime visual files.
- Do not hand-author a final production map directly in the Demo scene.
- Do not use terrain height as gameplay height.
