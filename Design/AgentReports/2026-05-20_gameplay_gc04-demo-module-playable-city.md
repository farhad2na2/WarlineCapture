# GC04 Demo Module Playable City 2048

Lane: Gameplay
Task: Convert accepted Demo scene clusters into reusable city/base modules and place them around explicit GC03-style walkable roads.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc04DemoModuleCityBuilder.cs`
- `Assets/Game/Scenes/Generated/GC04_DemoModulePlayableCity_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_demo_module_catalog.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_topdown_modules_walkability_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_town_modules_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_town_market_modules_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_base_modules_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_industrial_modules_1920x1080.png`

Contracts touched: Gameplay playable scene generation workflow contract.
User-visible behavior: none in shipped flow; generated GC04 scene is available for visual/design review.
Validation run: Unity batchmode `WarlineCaptureGc04DemoModuleCityBuilder.BuildGc04DemoModulePlayableCity2048`.
Validation result: passed Demo module placement and walkability validation.
Known gaps: modules are cloned from Demo bounds and flattened for grid gameplay, but they are not yet authored as reusable prefabs with designer-authored internal blocked/walkable masks.
Cross-lane impacts: PM/Design can review whether these Demo-derived modules are the right visual basis before we convert them into reusable prefabs.
Next recommended task: promote accepted GC04 modules into prefab assets with explicit internal blocked masks and module sockets for roads/objectives.

Modules placed: 16
Interior terrain/blocker roots rejected from playable modules: 288

Validation log:
- PASS: GC04 placed Demo-authored modules into legal footprints; explicit walkable roads, spawns, objectives, and proof soldiers remain connected.

Module log:
- TownBlock_SW_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-800, -620) size=(145.6, 145.6) role=town scale=0.58
- TownBlock_SouthCenter_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-600, -620) size=(136.8, 136.8) role=town scale=0.54
- TownBlock_SouthEast_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-400, -620) size=(136.8, 136.8) role=town scale=0.54
- TownBlock_WestMid_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-800, -220) size=(141.2, 141.2) role=town scale=0.56
- TownBlock_Center_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-600, -220) size=(141.2, 141.2) role=town scale=0.56
- TownMarket_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-400, 180) size=(150, 150) role=town scale=0.6
- TownBlock_WestMarket_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-800, 180) size=(136.8, 136.8) role=town scale=0.54
- TownBlock_NorthCenter_DemoAuthored: cloned 2892 Demo roots into legal module footprint=center=(-600, 580) size=(136.8, 136.8) role=town scale=0.54
- TownNorth_DemoAuthored: cloned 808 Demo roots into legal module footprint=center=(-800, 580) size=(82.8, 154.8) role=town scale=0.72
- BaseBarracks_DemoAuthored: cloned 1061 Demo roots into legal module footprint=center=(340, -260) size=(114.2, 121.6) role=base scale=0.74
- BaseMotorPool_DemoAuthored: cloned 1061 Demo roots into legal module footprint=center=(760, -260) size=(109, 116) role=base scale=0.7
- BaseSouthDepot_DemoAuthored: cloned 1061 Demo roots into legal module footprint=center=(340, -740) size=(103.8, 110.4) role=base scale=0.66
- BaseCommand_DemoAuthored: cloned 1061 Demo roots into legal module footprint=center=(560, 240) size=(111.6, 118.8) role=base scale=0.72
- BaseNorthDepot_DemoAuthored: cloned 1061 Demo roots into legal module footprint=center=(340, 500) size=(106.4, 113.2) role=base scale=0.68
- RunwayApron_DemoAuthored: cloned 220 Demo roots into legal module footprint=center=(780, 500) size=(61.4, 154.4) role=base scale=0.62
- IndustrialObjective_DemoAuthored: cloned 186 Demo roots into legal module footprint=center=(560, -760) size=(130, 114) role=industrial scale=0.4
