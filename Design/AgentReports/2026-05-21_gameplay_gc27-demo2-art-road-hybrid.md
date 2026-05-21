# GC27 Demo2 Art Road Hybrid Scene

Lane: Gameplay
Task: Continue from GC26 by preserving the source-scale compound layout and replacing the weak generated road/ground look with Demo2 PolygonBattleRoyale road, dirt, concrete, grass, wall, rubble, and detail assets.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc27Demo2ArtRoadHybridBuilder.cs`
- `Assets/Game/Scenes/Generated/GC27_Demo2ArtRoadHybrid_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_demo2_art_road_hybrid_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_scale_audit_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_topdown_contract_visual_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_topdown_generator_blueprint_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC27_Demo2ArtRoadHybrid_2048/gc27_topdown_module_mask_proof_2048x2048.png`

Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC27 adds a Demo2 art-road layer on top of GC26 source-scale compound envelopes without changing gameplay masks.
User-visible behavior: no shipped runtime behavior changed; generated scene now uses source-scale Demo2 road/ground/boundary/dressing prefabs over the existing flat gameplay plane.
Validation run: Unity batchmode `WarlineCaptureGc27Demo2ArtRoadHybridBuilder.BuildGc27Demo2ArtRoadHybrid2048`.
Validation result: passed Demo2 art-road hybrid validation.
Known gaps: Demo2 source-scale road modules are repeated along GC26 lane masks, so visual continuity still needs human review in the generated screenshots; this is not final navmesh/ECS integration.
Cross-lane impacts: Art/design can now compare the hybrid look against the Demo2 scene and target RTS reference while Gameplay keeps walkable/blocker masks stable.
Next recommended task: GC28 should visually review the captures, then replace any weak repeated road sections with authored intersections/compound entrance modules.

Counts:
- Demo2 road modules: 225
- Demo2 ground/concrete/grass modules: 5
- Demo2 boundary modules: 81
- Demo2 detail modules: 8
- fitted Demo-authored clusters: 37
- reusable masked modules: 7
- procedural districts: 7
- macro roads: 6
- soldier local zones: 12
- vehicle zones: 11
- blocker zones: 40
- visual placements: 155
- primary blocker visuals: 35
- dense blocker detail props: 116
- Demo2 art placements: 319
- skipped road-conflict visuals: 10

Procedural districts:
- NorthWestDenseTown: Civilian city blocks; style=MiddleEasternTown; density=0.78
- WestMarketResidential: Market streets and low houses; style=MarketVillage; density=0.72
- SouthPlayerBase: Player staging base; style=MilitaryBase; density=0.58
- CentralBarracksSpine: Source-scale barracks and service yards; style=MilitaryCamp; density=0.66
- NorthEastAirfield: Source-scale runway, hangars, apron; style=Airfield; density=0.62
- CentralCommandLogistics: Source-scale command and vehicle logistics; style=CommandDepot; density=0.70
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
- GC27_TownBlockModule_NorthWest_ChildPiece_0_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_0; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_1_TownBlock_Center_DemoAuthored_SM_Bld_Hall_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Hall_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_1; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_2_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_06; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_2; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_3_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_3; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_4_TownBlock_Center_DemoAuthored_SM_Bld_Shop_08: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_08; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_0; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_5_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_1; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_6_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_2; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_7_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_3; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_8_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_0; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_9_TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_1; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_10_TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_2; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_11_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_3; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_12_TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_0; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_13_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_1; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_14_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_2; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_15_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_3; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_16_TownBlock_Center_DemoAuthored_SM_Bld_Shop_07: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_07; module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_0; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_17_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_1; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_18_SM_Bld_Village_House_06 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::SM_Bld_Village_House_06 (3); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_2; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_19_TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed; module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_3; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_20_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_0; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_21_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_1; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_22_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_2; scale=1.00
- GC27_TownBlockModule_NorthWest_ChildPiece_23_TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_3; scale=1.00
- GC27_MarketModule_West_ChildPiece_0_TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_0; scale=1.00
- GC27_MarketModule_West_ChildPiece_1_TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_1; scale=1.00
- GC27_MarketModule_West_ChildPiece_2_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_2; scale=1.00
- GC27_MarketModule_West_ChildPiece_3_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1); module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_3; scale=1.00
- GC27_MarketModule_West_ChildPiece_4_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08; module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_0; scale=1.00
- GC27_MarketModule_West_ChildPiece_5_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_1; scale=1.00
- GC27_MarketModule_West_ChildPiece_6_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_2; scale=1.00
- GC27_MarketModule_West_ChildPiece_7_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_3; scale=1.00
- GC27_PlayerBaseModule_SouthGate_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseSouthDepot_DemoAuthored.prefab; module=PlayerBaseModule_SouthGate; mask=PlayerBaseModule_SouthGate_SourceScaleEnvelope; scale=1.00
- GC27_BarracksModule_CentralSpine_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseBarracks_DemoAuthored.prefab; module=BarracksModule_CentralSpine; mask=BarracksModule_CentralSpine_SourceScaleEnvelope; scale=1.00
- GC27_AirfieldModule_NorthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/RunwayApron_DemoAuthored.prefab; module=AirfieldModule_NorthEast; mask=AirfieldModule_NorthEast_SourceScaleEnvelope; scale=1.00
- GC27_CommandDepotModule_CentralEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseCommand_DemoAuthored.prefab; module=CommandDepotModule_CentralEast; mask=CommandDepotModule_CentralEast_SourceScaleEnvelope; scale=1.00
- GC27_ArmorFuelModule_SouthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseMotorPool_DemoAuthored.prefab; module=ArmorFuelModule_SouthEast; mask=ArmorFuelModule_SouthEast_SourceScaleEnvelope; scale=1.00

Demo2 art layer:
- GC27_Demo2_GrassPad_NorthWestTown_A: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Square_01.prefab; footprint=174.9m x 174.9m
- GC27_Demo2_GrassPad_WestMarket_A: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_01.prefab; footprint=128.9m x 128.9m
- GC27_Demo2_GrassPad_SouthBase_A: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_02.prefab; footprint=124.3m x 124.3m
- GC27_Demo2_Concrete_CommandPad_A: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Concrete_Base_01.prefab; footprint=170.3m x 170.3m
- GC27_Demo2_Concrete_AirfieldPad_A: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Port_Concrete_Slab_01.prefab; footprint=161.1m x 161.1m
- GC27_Demo2_WestNorthSouth_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_05: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_06: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_07: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_08: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_09: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_10: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_11: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_12: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_13: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_14: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_15: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_16: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_17: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_18: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_19: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_20: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_21: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_22: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_23: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_24: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_25: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_26: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_27: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_28: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_29: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_30: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_31: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_32: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_33: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_34: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_35: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_36: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_37: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_38: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_39: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_40: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_41: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_42: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_43: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_WestNorthSouth_44: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_05: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_06: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_07: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_08: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_09: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_10: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_11: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_12: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_13: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_14: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_15: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_16: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_17: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_18: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_19: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_20: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_21: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_22: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_23: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_24: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_25: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_26: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_27: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_28: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- GC27_Demo2_EastNorthSouth_29: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=96.6m x 64.4m
- ... 239 more Demo2 art placements omitted from report body; full list is in the JSON contract.

Scale audit:
- GC27_TownBlockModule_NorthWest_ChildPiece_0_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01: source scale 1.00; footprint 33.8m x 33.8m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_1_TownBlock_Center_DemoAuthored_SM_Bld_Hall_01: source scale 1.00; footprint 22.5m x 35.0m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_2_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06: source scale 1.00; footprint 21.5m x 12.8m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_3_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1): source scale 1.00; footprint 21.5m x 12.8m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_4_TownBlock_Center_DemoAuthored_SM_Bld_Shop_08: source scale 1.00; footprint 22.8m x 12.6m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_5_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1): source scale 1.00; footprint 18.8m x 17.3m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_6_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3): source scale 1.00; footprint 9.0m x 12.8m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_7_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2): source scale 1.00; footprint 28.1m x 22.1m; mask 47.6m x 48.8m.
- GC27_TownBlockModule_NorthWest_ChildPiece_8_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02: source scale 1.00; footprint 14.4m x 14.4m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_9_TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front: source scale 1.00; footprint 8.8m x 10.9m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_10_TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01: source scale 1.00; footprint 15.9m x 13.2m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_11_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1): source scale 1.00; footprint 11.3m x 13.0m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_12_TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1): source scale 1.00; footprint 14.1m x 11.7m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_13_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1): source scale 1.00; footprint 11.6m x 13.9m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_14_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3): source scale 1.00; footprint 8.5m x 8.6m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_15_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4): source scale 1.00; footprint 8.5m x 8.6m; mask 47.6m x 45.6m.
- GC27_TownBlockModule_NorthWest_ChildPiece_16_TownBlock_Center_DemoAuthored_SM_Bld_Shop_07: source scale 1.00; footprint 15.2m x 9.2m; mask 36.2m x 42.4m.
- GC27_TownBlockModule_NorthWest_ChildPiece_17_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2): source scale 1.00; footprint 8.5m x 8.6m; mask 36.2m x 42.4m.
- GC27_TownBlockModule_NorthWest_ChildPiece_18_SM_Bld_Village_House_06 (3): source scale 1.00; footprint 8.5m x 8.6m; mask 36.2m x 42.4m.
- GC27_TownBlockModule_NorthWest_ChildPiece_19_TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed: source scale 1.00; footprint 7.3m x 9.2m; mask 36.2m x 42.4m.
- GC27_TownBlockModule_NorthWest_ChildPiece_20_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC27_TownBlockModule_NorthWest_ChildPiece_21_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC27_TownBlockModule_NorthWest_ChildPiece_22_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC27_TownBlockModule_NorthWest_ChildPiece_23_TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1): source scale 1.00; footprint 10.7m x 10.6m; mask 36.2m x 39.5m.
- GC27_MarketModule_West_ChildPiece_0_TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01: source scale 1.00; footprint 33.8m x 33.8m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_1_TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01: source scale 1.00; footprint 22.5m x 35.0m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_2_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06: source scale 1.00; footprint 21.5m x 12.8m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_3_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1): source scale 1.00; footprint 21.5m x 12.8m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_4_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08: source scale 1.00; footprint 22.8m x 12.6m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_5_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1): source scale 1.00; footprint 18.8m x 17.3m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_6_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3): source scale 1.00; footprint 9.0m x 12.8m; mask 50.3m x 48.8m.
- GC27_MarketModule_West_ChildPiece_7_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2): source scale 1.00; footprint 28.1m x 22.1m; mask 50.3m x 48.8m.
- GC27_PlayerBaseModule_SouthGate_AuthoredCluster: source scale 1.00; footprint 95.6m x 167.8m; envelope 207.6m x 211.8m.
- GC27_BarracksModule_CentralSpine_AuthoredCluster: source scale 1.00; footprint 185.8m x 122.7m; envelope 275.5m x 628.1m.
- GC27_AirfieldModule_NorthEast_AuthoredCluster: source scale 1.00; footprint 64.0m x 185.5m; envelope 253.5m x 264.6m.
- GC27_CommandDepotModule_CentralEast_AuthoredCluster: source scale 1.00; footprint 180.8m x 119.4m; envelope 582.4m x 297.9m.
- GC27_ArmorFuelModule_SouthEast_AuthoredCluster: source scale 1.00; footprint 177.9m x 101.4m; envelope 469.2m x 394.6m.

Validation log:
- PASS: GC27 instantiated 37 source-scale Demo-authored clusters/modules, added Demo2 road/ground/boundary art (225/5/81/8), exported 7 reusable masked district modules across 7 named districts, preserved the GC17/GC18 walkability masks, and kept legal blocker visuals off macro roads. Detail props: 116; primary blocker visuals: 35; total placements: 155; art placements: 319; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 6.

Skipped visuals:
- CityCentral_WestInner_Building_NW: skipped because blocker anchor overlaps macro road.
- CityCentral_WestInner_Building_SW: skipped because blocker anchor overlaps macro road.
- CityMarket_West_Building_SW: skipped because blocker anchor overlaps macro road.
- CityMarket_West_Building_SE: skipped because blocker anchor overlaps macro road.
- Airfield_Apron_Static_SE: skipped because blocker anchor overlaps macro road.
- FuelUtility_East_VehicleStaticProp_Barrier_A: skipped because detail footprint would leak outside blocker.
- Rock_NorthEdge_01: skipped because dressing footprint would overlap walkable space.
- Rock_CommandEdge_01: skipped because dressing footprint would overlap walkable space.
- Palm_MarketEdge_01: skipped because dressing footprint would overlap walkable space.
- Palm_SouthEdge_01: skipped because dressing footprint would overlap walkable space.
