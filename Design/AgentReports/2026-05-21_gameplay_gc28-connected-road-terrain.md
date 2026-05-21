# GC28 Connected Road Terrain Scene

Lane: Gameplay
Task: Continue from GC27 by fixing disconnected road visuals and unclean terrain, while preserving the source-scale compound layout and the flat walkability/blocker contract.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc28ConnectedRoadTerrainBuilder.cs`
- `Assets/Game/Scenes/Generated/GC28_ConnectedRoadTerrain_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_connected_road_terrain_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_scale_audit_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_topdown_contract_visual_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_topdown_generator_blueprint_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC28_ConnectedRoadTerrain_2048/gc28_topdown_module_mask_proof_2048x2048.png`

Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC28 changes only generated art-road/terrain presentation on top of GC26/GC27 source-scale compound envelopes without changing gameplay masks.
User-visible behavior: no shipped runtime behavior changed; generated scene now uses a connected scaled Demo2 road graph, cleaner terrain beds, and source-scale building/compound visuals over the existing flat gameplay plane.
Validation run: Unity batchmode `WarlineCaptureGc28ConnectedRoadTerrainBuilder.BuildGc28ConnectedRoadTerrain2048`.
Validation result: passed connected road/terrain validation.
Known gaps: This is still a generated prototype scene, not final art. Road modules are scaled for 2048m readability while buildings remain source scale 1; final road art should use authored compound entrances and larger continuous road meshes if Art provides them.
Cross-lane impacts: Art/design can review whether the connected road graph and cleaner terrain read closer to the target RTS reference before Gameplay turns the contract into runtime movement data.
Next recommended task: GC29 should increase visual density around compounds and improve authored road-to-compound entrances after PM/art accepts the connected road/terrain direction.

Counts:
- connected road edges: 20
- connected road nodes: 6
- Demo2 road modules: 88
- Demo2 ground/concrete/grass modules: 6
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
- Demo2 art placements: 183
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
- GC28_TownBlockModule_NorthWest_ChildPiece_0_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_0; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_1_TownBlock_Center_DemoAuthored_SM_Bld_Hall_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Hall_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_1; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_2_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_06; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_2; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_3_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_3; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_4_TownBlock_Center_DemoAuthored_SM_Bld_Shop_08: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_08; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_0; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_5_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_1; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_6_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_2; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_7_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_3; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_8_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_0; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_9_TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_1; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_10_TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_2; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_11_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_3; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_12_TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_0; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_13_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_1; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_14_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_2; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_15_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_3; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_16_TownBlock_Center_DemoAuthored_SM_Bld_Shop_07: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_07; module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_0; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_17_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_1; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_18_SM_Bld_Village_House_06 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::SM_Bld_Village_House_06 (3); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_2; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_19_TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed; module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_3; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_20_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_0; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_21_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_1; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_22_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_2; scale=1.00
- GC28_TownBlockModule_NorthWest_ChildPiece_23_TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_3; scale=1.00
- GC28_MarketModule_West_ChildPiece_0_TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_0; scale=1.00
- GC28_MarketModule_West_ChildPiece_1_TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_1; scale=1.00
- GC28_MarketModule_West_ChildPiece_2_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_2; scale=1.00
- GC28_MarketModule_West_ChildPiece_3_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1); module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_3; scale=1.00
- GC28_MarketModule_West_ChildPiece_4_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08; module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_0; scale=1.00
- GC28_MarketModule_West_ChildPiece_5_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_1; scale=1.00
- GC28_MarketModule_West_ChildPiece_6_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_2; scale=1.00
- GC28_MarketModule_West_ChildPiece_7_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_3; scale=1.00
- GC28_PlayerBaseModule_SouthGate_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseSouthDepot_DemoAuthored.prefab; module=PlayerBaseModule_SouthGate; mask=PlayerBaseModule_SouthGate_SourceScaleEnvelope; scale=1.00
- GC28_BarracksModule_CentralSpine_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseBarracks_DemoAuthored.prefab; module=BarracksModule_CentralSpine; mask=BarracksModule_CentralSpine_SourceScaleEnvelope; scale=1.00
- GC28_AirfieldModule_NorthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/RunwayApron_DemoAuthored.prefab; module=AirfieldModule_NorthEast; mask=AirfieldModule_NorthEast_SourceScaleEnvelope; scale=1.00
- GC28_CommandDepotModule_CentralEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseCommand_DemoAuthored.prefab; module=CommandDepotModule_CentralEast; mask=CommandDepotModule_CentralEast_SourceScaleEnvelope; scale=1.00
- GC28_ArmorFuelModule_SouthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseMotorPool_DemoAuthored.prefab; module=ArmorFuelModule_SouthEast; mask=ArmorFuelModule_SouthEast_SourceScaleEnvelope; scale=1.00

Demo2 art layer:
- GC28_Demo2_GrassPad_NorthWestTown_A_FallbackSurface: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Square_01.prefab; footprint=349.8m x 349.8m
- GC28_Demo2_GrassPad_NorthWestTown_B_FallbackSurface: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_02.prefab; footprint=265.1m x 265.1m
- GC28_Demo2_GrassPad_WestMarket_A_FallbackSurface: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_01.prefab; footprint=232.0m x 232.0m
- GC28_Demo2_GrassPad_SouthBase_A_FallbackSurface: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_02.prefab; footprint=211.2m x 211.2m
- GC28_Demo2_Concrete_CommandPad_A_FallbackSurface: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Concrete_Base_01.prefab; footprint=340.6m x 340.6m
- GC28_Demo2_Concrete_AirfieldPad_A_FallbackSurface: category=ground; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Port_Concrete_Slab_01.prefab; footprint=338.3m x 338.3m
- GC28_Demo2_WestNorthSouth_North_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_North_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_North_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_North_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_Mid_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_Mid_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_Mid_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_Mid_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_South_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_South_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_South_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_South_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_South_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_WestNorthSouth_South_05: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_North_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_North_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_North_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_North_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_North_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_05: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_06: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_EastNorthSouth_South_07: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthCityToAirfield_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthCityToAirfield_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthCityToAirfield_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthCityToAirfield_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthConnectorToEastSpine_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthConnectorToEastSpine_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthConnectorToEastSpine_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_NorthConnectorToEastSpine_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralCityToCommand_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralCityToCommand_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralCityToCommand_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralConnectorToEastSpine_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralConnectorToEastSpine_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralConnectorToEastSpine_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CentralConnectorToEastSpine_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CommandToAirfieldEast_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CommandToAirfieldEast_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CommandToAirfieldEast_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CommandToAirfieldEast_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CommandToAirfieldEast_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_CommandToAirfieldEast_05: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_A_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_A_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_A_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_A_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_B_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_B_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_B_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_B_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_C_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_C_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_C_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_C_03: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_SouthPlayerToFuel_C_04: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab; footprint=250.4m x 162.0m
- GC28_Demo2_RoadT_WestNorth: category=road_node; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_T_01.prefab; footprint=331.4m x 331.4m
- GC28_Demo2_RoadT_WestCentral: category=road_node; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_T_01.prefab; footprint=331.4m x 331.4m
- GC28_Demo2_RoadCross_EastCommand: category=road_node; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Cross_01.prefab; footprint=331.4m x 331.4m
- GC28_Demo2_RoadCorner_WestSouth: category=road_node; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Corner_01.prefab; footprint=331.4m x 331.4m
- GC28_Demo2_RoadCorner_EastSouth: category=road_node; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Corner_02.prefab; footprint=331.4m x 331.4m
- GC28_Demo2_RoadEnd_NorthAirfield: category=road_node; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_End_01.prefab; footprint=331.4m x 331.4m
- GC28_Demo2_DirtServiceLane_CentralBarracks_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- GC28_Demo2_DirtServiceLane_CentralBarracks_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- GC28_Demo2_DirtServiceLane_CentralBarracks_02: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- GC28_Demo2_DirtServiceLane_WestBarracks_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- GC28_Demo2_DirtServiceLane_WestBarracks_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- GC28_Demo2_DirtServiceLane_ArmorFuel_00: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- GC28_Demo2_DirtServiceLane_ArmorFuel_01: category=road; prefab=Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_DirtRoad_Straight_01.prefab; footprint=211.2m x 136.7m
- ... 103 more Demo2 art placements omitted from report body; full list is in the JSON contract.

Scale audit:
- GC28_TownBlockModule_NorthWest_ChildPiece_0_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01: source scale 1.00; footprint 33.8m x 33.8m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_1_TownBlock_Center_DemoAuthored_SM_Bld_Hall_01: source scale 1.00; footprint 22.5m x 35.0m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_2_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06: source scale 1.00; footprint 21.5m x 12.8m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_3_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1): source scale 1.00; footprint 21.5m x 12.8m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_4_TownBlock_Center_DemoAuthored_SM_Bld_Shop_08: source scale 1.00; footprint 22.8m x 12.6m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_5_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1): source scale 1.00; footprint 18.8m x 17.3m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_6_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3): source scale 1.00; footprint 9.0m x 12.8m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_7_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2): source scale 1.00; footprint 28.1m x 22.1m; mask 47.6m x 48.8m.
- GC28_TownBlockModule_NorthWest_ChildPiece_8_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02: source scale 1.00; footprint 14.4m x 14.4m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_9_TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front: source scale 1.00; footprint 8.8m x 10.9m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_10_TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01: source scale 1.00; footprint 15.9m x 13.2m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_11_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1): source scale 1.00; footprint 11.3m x 13.0m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_12_TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1): source scale 1.00; footprint 14.1m x 11.7m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_13_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1): source scale 1.00; footprint 11.6m x 13.9m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_14_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3): source scale 1.00; footprint 8.5m x 8.6m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_15_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4): source scale 1.00; footprint 8.5m x 8.6m; mask 47.6m x 45.6m.
- GC28_TownBlockModule_NorthWest_ChildPiece_16_TownBlock_Center_DemoAuthored_SM_Bld_Shop_07: source scale 1.00; footprint 15.2m x 9.2m; mask 36.2m x 42.4m.
- GC28_TownBlockModule_NorthWest_ChildPiece_17_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2): source scale 1.00; footprint 8.5m x 8.6m; mask 36.2m x 42.4m.
- GC28_TownBlockModule_NorthWest_ChildPiece_18_SM_Bld_Village_House_06 (3): source scale 1.00; footprint 8.5m x 8.6m; mask 36.2m x 42.4m.
- GC28_TownBlockModule_NorthWest_ChildPiece_19_TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed: source scale 1.00; footprint 7.3m x 9.2m; mask 36.2m x 42.4m.
- GC28_TownBlockModule_NorthWest_ChildPiece_20_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC28_TownBlockModule_NorthWest_ChildPiece_21_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC28_TownBlockModule_NorthWest_ChildPiece_22_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC28_TownBlockModule_NorthWest_ChildPiece_23_TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1): source scale 1.00; footprint 10.7m x 10.6m; mask 36.2m x 39.5m.
- GC28_MarketModule_West_ChildPiece_0_TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01: source scale 1.00; footprint 33.8m x 33.8m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_1_TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01: source scale 1.00; footprint 22.5m x 35.0m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_2_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06: source scale 1.00; footprint 21.5m x 12.8m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_3_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1): source scale 1.00; footprint 21.5m x 12.8m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_4_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08: source scale 1.00; footprint 22.8m x 12.6m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_5_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1): source scale 1.00; footprint 18.8m x 17.3m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_6_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3): source scale 1.00; footprint 9.0m x 12.8m; mask 50.3m x 48.8m.
- GC28_MarketModule_West_ChildPiece_7_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2): source scale 1.00; footprint 28.1m x 22.1m; mask 50.3m x 48.8m.
- GC28_PlayerBaseModule_SouthGate_AuthoredCluster: source scale 1.00; footprint 95.6m x 167.8m; envelope 207.6m x 211.8m.
- GC28_BarracksModule_CentralSpine_AuthoredCluster: source scale 1.00; footprint 185.8m x 122.7m; envelope 275.5m x 628.1m.
- GC28_AirfieldModule_NorthEast_AuthoredCluster: source scale 1.00; footprint 64.0m x 185.5m; envelope 253.5m x 264.6m.
- GC28_CommandDepotModule_CentralEast_AuthoredCluster: source scale 1.00; footprint 180.8m x 119.4m; envelope 582.4m x 297.9m.
- GC28_ArmorFuelModule_SouthEast_AuthoredCluster: source scale 1.00; footprint 177.9m x 101.4m; envelope 469.2m x 394.6m.

Validation log:
- PASS: GC28 built a connected Demo2 road/terrain pass with 20 connected road edges, 6 road nodes, Demo2 road/ground/boundary/detail art (88/6/81/8), 37 source-scale Demo-authored clusters/modules, 7 reusable masked district modules across 7 named districts, and preserved the GC17/GC18 walkability masks. Detail props: 116; primary blocker visuals: 35; total placements: 155; art placements: 183; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 6.

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
