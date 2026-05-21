# GC19 Dense Blocker Visual Scene

Lane: Gameplay
Task: Continue from GC18 by densifying city and military-base blocker footprints with legal detail props while preserving the GC17/GC18 walkability masks and authored road frame.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc19DenseBlockerVisualBuilder.cs`
- `Assets/Game/Scenes/Generated/GC19_DenseBlockerVisual_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_dense_blocker_visual_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC19_DenseBlockerVisual_2048/gc19_topdown_contract_visual_proof_2048x2048.png`

Contracts touched: GC17 walkability visual contract remains the source of truth; GC19 adds visuals only inside blocker zones.
User-visible behavior: no shipped runtime behavior changed; generated scene gives denser city/base blocker clusters for PM review.
Validation run: Unity batchmode `WarlineCaptureGc19DenseBlockerVisualBuilder.BuildGc19DenseBlockerVisual2048`.
Validation result: passed dense-blocker visual validation.
Known gaps: This is still a generated visual pass, not final art quality. Whole authored Demo modules are still not promoted because their internal walkability is opaque.
Cross-lane impacts: Art/Design can review whether blocker footprints are large enough for the target visual density without sacrificing RTS walkability.
Next recommended task: GC20 should either enlarge/reshape accepted blocker footprints for better city massing or convert selected Demo-authored modules into explicit blocker masks.

Counts:
- macro roads: 5
- soldier local zones: 12
- vehicle zones: 11
- blocker zones: 40
- visual placements: 138
- primary blocker visuals: 30
- dense blocker detail props: 102
- skipped road-conflict visuals: 12

Validation log:
- PASS: GC19 preserved the GC17/GC18 walkability masks and densified legal blocker zones with 102 additional blocker detail props plus 30 primary blocker visuals. Total visual placements: 138; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 5.

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
