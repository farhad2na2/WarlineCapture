# GC20 Procedural Authoring Scene

Lane: Gameplay
Task: Continue from GC19 by adding a Unity-native procedural authoring contract: named districts, generator settings, walkable roads/yards, blocker-only visual placement, and proof captures suitable for PM review before replacing modules.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc20ProceduralAuthoringBuilder.cs`
- `Assets/Game/Scenes/Generated/GC20_ProceduralAuthoring_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_procedural_authoring_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_topdown_contract_visual_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC20_ProceduralAuthoring_2048/gc20_topdown_generator_blueprint_2048x2048.png`

Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC20 adds procedural district metadata and keeps visuals constrained to blocker zones.
User-visible behavior: no shipped runtime behavior changed; generated scene now includes a city-generator-style contract that explains what each district is supposed to become.
Validation run: Unity batchmode `WarlineCaptureGc20ProceduralAuthoringBuilder.BuildGc20ProceduralAuthoring2048`.
Validation result: passed procedural authoring validation.
Known gaps: This is still a generated authoring pass, not final art quality. Full Demo modules are still not promoted until their internal blocker/walkable masks are explicit.
Cross-lane impacts: Design can review district roles/densities before Gameplay converts selected Demo-quality clusters into reusable masked modules.
Next recommended task: GC21 should convert selected Demo-authored clusters into reusable district modules with explicit blocker masks, then replace the GC20 placeholder blocker visuals.

Counts:
- procedural districts: 7
- macro roads: 5
- soldier local zones: 12
- vehicle zones: 11
- blocker zones: 40
- visual placements: 138
- primary blocker visuals: 30
- dense blocker detail props: 102
- skipped road-conflict visuals: 12

Procedural districts:
- NorthWestDenseTown: Civilian city blocks; style=MiddleEasternTown; density=0.78
- WestMarketResidential: Market streets and low houses; style=MarketVillage; density=0.72
- SouthPlayerBase: Player staging base; style=MilitaryBase; density=0.58
- CentralBarracksSpine: Barracks and service yards; style=MilitaryCamp; density=0.66
- NorthEastAirfield: Runway, hangars, apron; style=Airfield; density=0.62
- CentralCommandLogistics: Command and vehicle logistics; style=CommandDepot; density=0.70
- SouthEastArmorFuel: Armor parking and fuel utility; style=ArmorFuelDepot; density=0.74

Validation log:
- PASS: GC20 exported a procedural authoring contract with 7 named districts, preserved the GC17/GC18 walkability masks, and kept all legal visuals on blocker zones. Detail props: 102; primary blocker visuals: 30; total placements: 138; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 5.

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
