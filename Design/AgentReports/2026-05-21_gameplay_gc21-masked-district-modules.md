# GC21 Masked District Modules Scene

Lane: Gameplay
Task: Continue from GC20 by converting district intent into reusable masked district modules. Each module names its district, intended authored replacement style, and the exact blocker masks it owns so Demo-quality clusters can be swapped in without breaking RTS walkability.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc21MaskedDistrictModuleBuilder.cs`
- `Assets/Game/Scenes/Generated/GC21_MaskedDistrictModules_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_masked_district_modules_contract.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_rts_dense_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_topdown_contract_visual_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_topdown_generator_blueprint_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC21_MaskedDistrictModules_2048/gc21_topdown_module_mask_proof_2048x2048.png`

Contracts touched: GC17/GC18 walkability visual contract remains the source of truth; GC21 adds reusable module ownership over blocker masks.
User-visible behavior: no shipped runtime behavior changed; generated scene now includes module-mask proof for replacing placeholder visuals with authored Demo-quality modules.
Validation run: Unity batchmode `WarlineCaptureGc21MaskedDistrictModuleBuilder.BuildGc21MaskedDistrictModules2048`.
Validation result: passed masked-module validation.
Known gaps: This pass exports module contracts and proof overlays, but it still uses the existing placeholder scene visuals. It does not yet instantiate full Demo clusters.
Cross-lane impacts: Art/Design can review module boundaries before Gameplay promotes full Demo clusters into these masks.
Next recommended task: GC22 should instantiate selected Demo-authored clusters inside the GC21 module masks and reject any cluster whose internal geometry leaks onto walkable roads/yards.

Counts:
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

Validation log:
- PASS: GC21 exported 7 reusable masked district modules across 7 named districts, preserved the GC17/GC18 walkability masks, and kept all legal visuals on blocker zones. Detail props: 102; primary blocker visuals: 30; total placements: 138; blocker zones: 40; soldier zones: 12; vehicle zones: 11; macro roads: 5.

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
