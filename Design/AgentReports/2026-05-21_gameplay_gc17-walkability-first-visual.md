# GC17 Walkability-First Visual Scene

Lane: Gameplay
Task: Build the next scene pass by placing visuals from the GC16 walkability contract, keeping macro roads and city/camp local walk zones as the source of truth.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc17WalkabilityFirstVisualBuilder.cs`
- `Assets/Game/Scenes/Generated/GC17_WalkabilityFirstVisual_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC17_WalkabilityFirstVisual_2048/gc17_walkability_visual_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC17_WalkabilityFirstVisual_2048/gc17_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC17_WalkabilityFirstVisual_2048/gc17_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC17_WalkabilityFirstVisual_2048/gc17_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC17_WalkabilityFirstVisual_2048/gc17_topdown_contract_visual_proof_2048x2048.png`

Contracts touched: GC16 walkability contract is promoted into visual placement input; buildings/props are anchored to blocker zones, not placed over walkable lanes.
User-visible behavior: no shipped runtime behavior changed; generated scene proves visuals can be built around walkability instead of the reverse.
Validation run: Unity batchmode `WarlineCaptureGc17WalkabilityFirstVisualBuilder.BuildGc17WalkabilityFirstVisual2048`.
Validation result: passed walkability-first visual validation.
Known gaps: This is a first walkability-first visual pass, not final art quality. Roads still need authored curved road meshes and local yards need richer dressing once the contract is accepted.
Cross-lane impacts: Design/Art should now compose high-quality modules around blocker/walkable masks instead of producing opaque city clusters with unknown internal walkability.
Next recommended task: replace rectangle placeholder roads/yards with authored art-road meshes while preserving the exported GC17 walkability visual contract.

Counts:
- macro roads: 5
- soldier local zones: 12
- vehicle zones: 11
- blocker zones: 40
- visual placements: 36
- skipped road-conflict visuals: 12

Validation log:
- PASS: GC17 placed 36 visuals from 40 blocker zones while preserving 12 soldier zones, 11 vehicle zones, and 5 macro roads.

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
