# GC18 Authored Road Visual Scene

Lane: Gameplay
Task: Continue from GC17 by replacing rectangular road/yards presentation with authored polygon road, shoulder, runway, plaza, and tire-track visual surfaces while preserving the GC17 walkability masks.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc18AuthoredRoadVisualBuilder.cs`
- `Assets/Game/Scenes/Generated/GC18_AuthoredRoadVisual_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC18_AuthoredRoadVisual_2048/gc18_authored_road_visual_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC18_AuthoredRoadVisual_2048/gc18_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC18_AuthoredRoadVisual_2048/gc18_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC18_AuthoredRoadVisual_2048/gc18_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC18_AuthoredRoadVisual_2048/gc18_topdown_contract_visual_proof_2048x2048.png`

Contracts touched: GC17 walkability visual contract remains the source of truth; GC18 changes visual road/yards presentation only.
User-visible behavior: no shipped runtime behavior changed; generated scene gives a more authored-looking visual pass for PM review.
Validation run: Unity batchmode `WarlineCaptureGc18AuthoredRoadVisualBuilder.BuildGc18AuthoredRoadVisual2048`.
Validation result: passed authored-road visual validation.
Known gaps: This is still a generated visual pass, not final art quality. Building clusters remain sparse and must be densified with legal authored modules without occupying the colored walkable masks.
Cross-lane impacts: Art/Design can now review curved road/plaza composition separately from walkability proof.
Next recommended task: GC19 should densify city and military-base visual modules inside blocker footprints, using the GC18 road/plaza surfaces as the visual frame.

Counts:
- macro roads: 5
- soldier local zones: 12
- vehicle zones: 11
- blocker zones: 40
- visual placements: 36
- skipped road-conflict visuals: 12

Validation log:
- PASS: GC18 preserved the GC17 walkability masks while replacing rectangular macro-road visuals with authored polygon road, shoulder, runway, plaza, and tire-track surfaces. Visual placements: 36; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 5.

Skipped visuals:
- CityCentral_WestInner_Building_NW: skipped because blocker anchor overlaps macro road.
- CityCentral_WestInner_Building_SW: skipped because blocker anchor overlaps macro road.
- CityMarket_West_Building_SW: skipped because blocker anchor overlaps macro road.
- CityMarket_West_Building_SE: skipped because blocker anchor overlaps macro road.
- CentralTentBarracks_Static_SE: skipped because blocker anchor overlaps macro road.
- WestTentBarracks_Static_SE: skipped because blocker anchor overlaps macro road.
- Airfield_Apron_Static_SE: skipped because blocker anchor overlaps macro road.
- FuelUtility_East_Static_NW: skipped because blocker anchor overlaps macro road.
- FuelUtility_East_Static_SE: skipped because blocker anchor overlaps macro road.
- FuelUtility_East_VehicleStaticProp: skipped because blocker anchor overlaps macro road.
- Palm_MarketEdge_01: skipped because dressing footprint would overlap walkable space.
- Palm_SouthEdge_01: skipped because dressing footprint would overlap walkable space.
