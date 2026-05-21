# WarlineCapture Handoff - Gameplay Demo Scene Visual Role Review

Date: 2026-05-19
Lane: Gameplay
Status: review complete; scene generation not started
Priority: scene direction groundwork

## Lane

Gameplay

## Task

Render/review the existing `Assets/Game/Scenes/Demo.unity`, mark town/highway/military-base style areas, understand prefab roles, and propose designed scene ideas before any automated rearrangement.

## Files changed

- `Assets/Game/Scripts/Editor/WarlineCaptureDemoSceneVisualAudit.cs`
- `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_topdown_2048.png`
- `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_existing_camera_1920x1080.png`
- `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_isometric_overview_1920x1080.png`
- `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_instance_role_map.json`
- `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_topdown_role_area_map.png`
- `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_role_summary.txt`
- `Design/AgentReports/2026-05-19_gameplay_demo-scene-visual-role-review.md`

## Contracts touched

- None. This is audit/review only.

## User-visible behavior

- None. No scene content was moved or generated.

## Validation run

- Unity batch render/data export:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureDemoSceneVisualAudit.Run -logFile /private/tmp/warlinecapture-demo-scene-visual-audit-graphics.log`
- Python role map postprocess from `demo_instance_role_map.json`.

## Validation result

- Demo scene role map generated from 7065 usable prefab instances.
- Batch camera renders are not visually reliable yet: the Demo scene renders as large flat terrain/material silhouettes from editor batch capture. I am not using those as art/composition proof.
- The data-backed role map is usable for layout understanding:
  - `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_topdown_role_area_map.png`
  - `Design/AgentReports/Captures/DemoSceneVisualAudit/demo_role_summary.txt`

## Area Read

- Dense town/village block: lower and lower-center clusters. This area is heavy on village buildings, doors/windows/cloth details, walls, props, rocks/grass, and tight street clutter.
- Highway/road spine: strong vertical road/runway-like spine through the center plus horizontal connectors through the middle. This is the best source for designed movement corridors.
- Military/base/industrial assets: center-right and right-side clusters. Strong signals include barracks, hangars, guard towers, barriers, razor wire, military beds/lockers, fuel/pipe assets, tanks/APCs/armored cars, jets/helicopters, and road/runway lights.
- Outlier/background terrain: wide perimeter assets should not be copied into gameplay. They should be treated as background/set dressing or excluded for flat-grid scenes.

## Role Summary

- Road: 526 instances.
- Building: 458 instances plus 626 building-detail instances.
- Military/industrial: 327 industrial, 294 cover, 909 vehicle, 45 destroyed vehicle.
- Environmental dressing: 1467 environment, 2191 prop, 72 debris.

## Scene Ideas

1. Base Gate Assault
   - Use barracks, guard towers, razor wire, road barriers, armored cars, and road lights.
   - Layout: straight road spine enters a fortified gate; player starts outside lower-left; enemy holds the gate and inner yard.
   - Gameplay role: clean choke point, readable cover rows, strong first mission combat lane.

2. Town Highway Ambush
   - Use the village block plus the central road/highway spine.
   - Layout: flat road cuts through compact town blocks; damaged cars and barrier rows create cover pockets.
   - Gameplay role: infantry movement tutorial with flanking alleys and visible objective patrol.

3. Military Convoy Intercept
   - Use armored cars, trucks, destroyed vehicles, road barriers, debris piles.
   - Layout: convoy stopped along the central road, with side buildings framing combat.
   - Gameplay role: strong readable objective line and natural cover without dense city clutter.

4. Refinery/Depot Raid
   - Use pipeline assets, fuel tanks, oil pumps, smoke stacks, barriers, utility props.
   - Layout: industrial yard on one side of the road, village or wall boundary on the other.
   - Gameplay role: medium-density tactical map with blocking industrial footprints and explosive objective hooks.

5. Runway Edge Sabotage
   - Use runway/road spine, hangars, jets/helicopters, road/runway lights, fuel props.
   - Layout: long clean sightline with hangar anchors and cover islands along the edge.
   - Gameplay role: vehicle/airbase mission with clear battlefield silhouette.

6. Destroyed Checkpoint
   - Use guard towers, barriers, destroyed vehicles, rubble, craters, power poles.
   - Layout: small checkpoint at a road crossing, damaged town pieces around it.
   - Gameplay role: compact encounter scene suited for a fast mission or tutorial step.

## Known gaps

- Need a real visual render pass from an interactive/editor camera or a corrected render pipeline capture; current batch renders are not composition proof.
- Need road socket extraction before generation: straight, corner, exit, edge, sidewalk, runway.
- Need flat-grid normalization per prefab: bottom-Y correction, X/Z footprint, blocker/walkable tag.
- Need contact sheets for candidate prefabs before using them in a generator.

## Cross-lane impacts

- Designer can choose which scene recipe maps to mission beats.
- Gameplay can build the generator around flat-grid role placement instead of random prefab scatter.
- Art can provide any missing base/town set dressing after the role kit is narrowed.

## Next recommended task

Build contact sheets and flat-grid prefab adapters for the candidate kit: road pieces, base gate pieces, barracks/hangars, village walls/buildings, barriers, vehicles, destroyed vehicles, industrial props, and debris. Then generate 3-5 seed layouts from one selected recipe.
