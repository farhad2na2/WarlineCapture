# GC25 Scale Reference District Scene

Lane: Gameplay
Task: Continue from GC24 by validating building, road, vehicle, and character scale using source prefab scale instead of fitting authored art down to masks.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc25ScaleReferenceDistrictBuilder.cs`
- `Assets/Game/Scenes/Generated/GC25_ScaleReferenceDistrict_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_scale_reference_district_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_scale_audit_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_topdown_contract_visual_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_topdown_generator_blueprint_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC25_ScaleReferenceDistrict_2048/gc25_topdown_module_mask_proof_2048x2048.png`

Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC25 changes visual placement policy so source-authored buildings, modules, soldiers, and vehicles are kept at scale 1 where possible.
User-visible behavior: no shipped runtime behavior changed; generated scene now exposes true source-scale relationships between soldiers, buildings, vehicles, roads, and district envelopes.
Validation run: Unity batchmode `WarlineCaptureGc25ScaleReferenceDistrictBuilder.BuildGc25ScaleReferenceDistrict2048`.
Validation result: passed source-scale validation.
Known gaps: source-scale prefabs reveal that some district envelopes/masks are still too sparse for target-quality city density; roads are still generated surfaces, not final art road modules.
Cross-lane impacts: Design should size future masks/envelopes around source-scale prefabs instead of relying on per-object scale fitting.
Next recommended task: GC26 should replace generated surface strips with real Polygon/Demo road, wall, curb, terrain, and compound modules while preserving source scale 1.

Counts:
- fitted Demo-authored clusters: 34
- reusable masked modules: 7
- procedural districts: 7
- macro roads: 5
- soldier local zones: 12
- vehicle zones: 11
- blocker zones: 40
- visual placements: 138
- primary blocker visuals: 30
- dense blocker detail props: 102
- skipped road-conflict visuals: 15

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
- GC25_TownBlockModule_NorthWest_ChildPiece_0_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_0; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_1_TownBlock_Center_DemoAuthored_SM_Bld_Hall_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Hall_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_1; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_2_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_06; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_2; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_3_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NW_ChildSlot_3; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_4_TownBlock_Center_DemoAuthored_SM_Bld_Shop_08: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_08; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_0; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_5_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_1; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_6_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_2; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_7_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_NE_ChildSlot_3; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_8_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_0; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_9_TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_1; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_10_TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01; module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_2; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_11_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SW_ChildSlot_3; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_12_TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_0; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_13_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_1; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_14_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_2; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_15_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4); module=TownBlockModule_NorthWest; mask=CityCore_NorthWest_Building_SE_ChildSlot_3; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_16_TownBlock_Center_DemoAuthored_SM_Bld_Shop_07: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_07; module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_0; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_17_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_1; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_18_SM_Bld_Village_House_06 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::SM_Bld_Village_House_06 (3); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_2; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_19_TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed; module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_NE_ChildSlot_3; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_20_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_0; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_21_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_1; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_22_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_2; scale=1.00
- GC25_TownBlockModule_NorthWest_ChildPiece_23_TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_Center_DemoAuthored.prefab::TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1); module=TownBlockModule_NorthWest; mask=CityCentral_WestInner_Building_SE_ChildSlot_3; scale=1.00
- GC25_MarketModule_West_ChildPiece_0_TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_0; scale=1.00
- GC25_MarketModule_West_ChildPiece_1_TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_1; scale=1.00
- GC25_MarketModule_West_ChildPiece_2_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06; module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_2; scale=1.00
- GC25_MarketModule_West_ChildPiece_3_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1); module=MarketModule_West; mask=CityMarket_West_Building_NW_ChildSlot_3; scale=1.00
- GC25_MarketModule_West_ChildPiece_4_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08: prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08; module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_0; scale=1.00
- GC25_MarketModule_West_ChildPiece_5_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_1; scale=1.00
- GC25_MarketModule_West_ChildPiece_6_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_2; scale=1.00
- GC25_MarketModule_West_ChildPiece_7_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2): prefab=Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_WestMarket_DemoAuthored.prefab::TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2); module=MarketModule_West; mask=CityMarket_West_Building_NE_ChildSlot_3; scale=1.00
- GC25_PlayerBaseModule_SouthGate_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseSouthDepot_DemoAuthored.prefab; module=PlayerBaseModule_SouthGate; mask=PlayerBaseModule_SouthGate_SourceScaleEnvelope; scale=1.00
- GC25_ArmorFuelModule_SouthEast_AuthoredCluster: prefab=Assets/Game/Prefabs/Generated/GC04Modules/BaseMotorPool_DemoAuthored.prefab; module=ArmorFuelModule_SouthEast; mask=ArmorFuelModule_SouthEast_SourceScaleEnvelope; scale=1.00

Scale audit:
- GC25_TownBlockModule_NorthWest_ChildPiece_0_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_01: source scale 1.00; footprint 33.8m x 33.8m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_1_TownBlock_Center_DemoAuthored_SM_Bld_Hall_01: source scale 1.00; footprint 22.5m x 35.0m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_2_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06: source scale 1.00; footprint 21.5m x 12.8m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_3_TownBlock_Center_DemoAuthored_SM_Bld_Shop_06 (1): source scale 1.00; footprint 21.5m x 12.8m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_4_TownBlock_Center_DemoAuthored_SM_Bld_Shop_08: source scale 1.00; footprint 22.8m x 12.6m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_5_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (1): source scale 1.00; footprint 18.8m x 17.3m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_6_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (3): source scale 1.00; footprint 9.0m x 12.8m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_7_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_01 (2): source scale 1.00; footprint 28.1m x 22.1m; mask 47.6m x 48.8m.
- GC25_TownBlockModule_NorthWest_ChildPiece_8_TownBlock_Center_DemoAuthored_SM_Bld_GasTower_02: source scale 1.00; footprint 14.4m x 14.4m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_9_TownBlock_Center_DemoAuthored_SM_Veh_Helicopter_Transport_01_Destroyed_Front: source scale 1.00; footprint 8.8m x 10.9m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_10_TownBlock_Center_DemoAuthored_SM_Bld_GasStation_01: source scale 1.00; footprint 15.9m x 13.2m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_11_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_03 (1): source scale 1.00; footprint 11.3m x 13.0m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_12_TownBlock_Center_DemoAuthored_SM_Bld_Destroyed_01 (1): source scale 1.00; footprint 14.1m x 11.7m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_13_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (1): source scale 1.00; footprint 11.6m x 13.9m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_14_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (3): source scale 1.00; footprint 8.5m x 8.6m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_15_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (4): source scale 1.00; footprint 8.5m x 8.6m; mask 47.6m x 45.6m.
- GC25_TownBlockModule_NorthWest_ChildPiece_16_TownBlock_Center_DemoAuthored_SM_Bld_Shop_07: source scale 1.00; footprint 15.2m x 9.2m; mask 36.2m x 42.4m.
- GC25_TownBlockModule_NorthWest_ChildPiece_17_TownBlock_Center_DemoAuthored_SM_Bld_Village_House_06 (2): source scale 1.00; footprint 8.5m x 8.6m; mask 36.2m x 42.4m.
- GC25_TownBlockModule_NorthWest_ChildPiece_18_SM_Bld_Village_House_06 (3): source scale 1.00; footprint 8.5m x 8.6m; mask 36.2m x 42.4m.
- GC25_TownBlockModule_NorthWest_ChildPiece_19_TownBlock_Center_DemoAuthored_SM_Veh_Tank_Russian_01_Destroyed: source scale 1.00; footprint 7.3m x 9.2m; mask 36.2m x 42.4m.
- GC25_TownBlockModule_NorthWest_ChildPiece_20_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (3): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC25_TownBlockModule_NorthWest_ChildPiece_21_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (4): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC25_TownBlockModule_NorthWest_ChildPiece_22_TownBlock_Center_DemoAuthored_SM_Bld_Shop_11 (1): source scale 1.00; footprint 9.5m x 12.2m; mask 36.2m x 39.5m.
- GC25_TownBlockModule_NorthWest_ChildPiece_23_TownBlock_Center_DemoAuthored_SM_Bld_Shop_05 (1): source scale 1.00; footprint 10.7m x 10.6m; mask 36.2m x 39.5m.
- GC25_MarketModule_West_ChildPiece_0_TownBlock_WestMarket_DemoAuthored_SM_Bld_GasTower_01: source scale 1.00; footprint 33.8m x 33.8m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_1_TownBlock_WestMarket_DemoAuthored_SM_Bld_Hall_01: source scale 1.00; footprint 22.5m x 35.0m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_2_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06: source scale 1.00; footprint 21.5m x 12.8m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_3_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_06 (1): source scale 1.00; footprint 21.5m x 12.8m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_4_TownBlock_WestMarket_DemoAuthored_SM_Bld_Shop_08: source scale 1.00; footprint 22.8m x 12.6m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_5_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (1): source scale 1.00; footprint 18.8m x 17.3m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_6_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (3): source scale 1.00; footprint 9.0m x 12.8m; mask 50.3m x 48.8m.
- GC25_MarketModule_West_ChildPiece_7_TownBlock_WestMarket_DemoAuthored_SM_Bld_Village_House_01 (2): source scale 1.00; footprint 28.1m x 22.1m; mask 50.3m x 48.8m.
- GC25_PlayerBaseModule_SouthGate_AuthoredCluster: source scale 1.00; footprint 95.6m x 167.8m; envelope 177.4m x 183.1m.
- GC25_ArmorFuelModule_SouthEast_AuthoredCluster: source scale 1.00; footprint 116.1m x 175.7m; envelope 211.3m x 201.0m.

Validation log:
- PASS: GC25 instantiated 34 source-scale Demo-authored clusters/modules, exported 7 reusable masked district modules across 7 named districts, preserved the GC17/GC18 walkability masks, and kept legal visuals off macro roads. Detail props: 102; primary blocker visuals: 30; total placements: 138; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 5.

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
- BarracksModule_CentralSpine: rejected authored cluster Assets/Game/Prefabs/Generated/GC04Modules/BaseBarracks_DemoAuthored.prefab because fitted renderer bounds overlap macro road space.
- AirfieldModule_NorthEast: rejected scale-1 authored cluster Assets/Game/Prefabs/Generated/GC04Modules/RunwayApron_DemoAuthored.prefab because renderer bounds exceed envelope AirfieldModule_NorthEast_SourceScaleEnvelope.
- CommandDepotModule_CentralEast: rejected authored cluster Assets/Game/Prefabs/Generated/GC04Modules/BaseCommand_DemoAuthored.prefab because fitted renderer bounds overlap macro road space.
