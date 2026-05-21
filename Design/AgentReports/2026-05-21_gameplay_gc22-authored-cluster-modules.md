# GC22 Authored Cluster Modules Scene

Lane: Gameplay
Task: Continue from GC21 by instantiating selected Demo-authored cluster prefabs inside owned blocker masks, fitting them to mask bounds, and rejecting any cluster whose renderer bounds leak onto walkable roads/yards.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc22AuthoredClusterModuleBuilder.cs`
- `Assets/Game/Scenes/Generated/GC22_AuthoredClusterModules_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_authored_cluster_modules_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_topdown_contract_visual_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_topdown_generator_blueprint_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC22_AuthoredClusterModules_2048/gc22_topdown_module_mask_proof_2048x2048.png`

Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC22 adds fitted authored-cluster placements to the reusable module contract.
User-visible behavior: no shipped runtime behavior changed; generated scene now shows Demo-authored cluster prefabs fitted inside legal blocker masks.
Validation run: Unity batchmode `WarlineCaptureGc22AuthoredClusterModuleBuilder.BuildGc22AuthoredClusterModules2048`.
Validation result: passed authored-cluster validation.
Known gaps: Demo clusters are fitted conservatively into individual blocker masks, so this is visibly safer but not yet a full high-density district replacement.
Cross-lane impacts: Art/Design can review whether the selected Demo clusters are the right source modules before Gameplay expands to multi-mask cluster replacement.
Next recommended task: GC23 should expand from single-mask fitted cluster previews to multi-mask composed authored districts with explicit internal sub-masks for each child object.

Counts:
- fitted Demo-authored clusters: 7
- reusable masked modules: 7
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

Masked modules:
- TownBlockModule_NorthWest: district=NorthWestDenseTown; masks=8; replacement=Demo-authored village houses/shops
- MarketModule_West: district=WestMarketResidential; masks=4; replacement=Market village with cloth covers, alleys, walls
- PlayerBaseModule_SouthGate: district=SouthPlayerBase; masks=3; replacement=Military entry camp
- BarracksModule_CentralSpine: district=CentralBarracksSpine; masks=6; replacement=Barracks and tent service strip
- AirfieldModule_NorthEast: district=NorthEastAirfield; masks=3; replacement=Runway apron, hangar, control tower
- CommandDepotModule_CentralEast: district=CentralCommandLogistics; masks=6; replacement=Command depot and vehicle yard
- ArmorFuelModule_SouthEast: district=SouthEastArmorFuel; masks=6; replacement=Armor park and fuel utility

Authored clusters:
- GC22_TownBlockModule_NorthWest_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseSouthDepot_DemoAuthored.prefab; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW; scale=0.18
- GC22_MarketModule_West_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseBarracks_DemoAuthored.prefab; module=MarketModule_West; mask=CityMarket_West_Building_NW; scale=0.17
- GC22_PlayerBaseModule_SouthGate_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseSouthDepot_DemoAuthored.prefab; module=PlayerBaseModule_SouthGate; mask=SouthGate_PlayerCamp_Static_NW; scale=0.12
- GC22_BarracksModule_CentralSpine_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseBarracks_DemoAuthored.prefab; module=BarracksModule_CentralSpine; mask=WestTentBarracks_Static_NW; scale=0.12
- GC22_AirfieldModule_NorthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/RunwayApron_DemoAuthored.prefab; module=AirfieldModule_NorthEast; mask=Airfield_Apron_Static_NW; scale=0.15
- GC22_CommandDepotModule_CentralEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseCommand_DemoAuthored.prefab; module=CommandDepotModule_CentralEast; mask=CommandDepot_CentralEast_Static_NW; scale=0.14
- GC22_ArmorFuelModule_SouthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseMotorPool_DemoAuthored.prefab; module=ArmorFuelModule_SouthEast; mask=VehicleYard_SouthEast_Static_NW; scale=0.14

Validation log:
- PASS: GC22 instantiated 7 fitted Demo-authored clusters inside owned blocker masks, exported 7 reusable masked district modules across 7 named districts, preserved the GC17/GC18 walkability masks, and kept all legal visuals on blocker zones. Detail props: 102; primary blocker visuals: 30; total placements: 138; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 5.

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
