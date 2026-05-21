# GC02 Demo Cluster City 2048

Lane: Gameplay
Task: Build a high-end 2048x2048 generated city scene by cloning authored Demo scene clusters with child decoration intact.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureDemoClusterCityBuilder.cs`
- `Assets/Game/Scenes/Generated/GC02_DemoClusterCity_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_overview_2048_map_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_city_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_base_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_city_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_convoy_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_ortho_city_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_ortho_convoy_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_60deg_city_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_60deg_convoy_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_40deg_city_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_40deg_convoy_units_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_professional_40deg_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_readability_32deg_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_gameplay_readable_35deg_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_unit_control_zoom_35deg_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_perspective_readable_3d_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_01_city_lane_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_02_highway_push_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_03_town_entry_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_04_town_market_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_05_base_approach_1920x1080.png`

Contracts touched: none. This is a generated design-target scene and does not change runtime ECS/game flow.
User-visible behavior: none in shipped flow yet; the scene is available for visual review under Generated scenes.
Validation run: Unity batchmode `WarlineCaptureDemoClusterCityBuilder.BuildGc02DemoClusterCity2048`.
Validation result: scene saved and five fresh RTS perspective lighting proof captures exported with shadowed key light, cooler fill, subtle fog, and URP post processing enabled on proof cameras.
Known gaps: interior large terrain is filtered so playable districts stay visually flat; some Demo base/runway source materials still read darker than the city district, so the base cluster needs a material/decal audit before PM acceptance. Next pass should add path/walkability overlays, road masks, and more city block variants.
Cross-lane impacts: Designer/PM can review scene composition; no UI/runtime source files are changed.
Next recommended task: convert accepted clusters into reusable city-block templates with footprint metadata and blocked/walkable masks.

Map size: 2048x2048 world units.

Clusters cloned:
- TownDistrict_A: cloned 2886 authored roots from Demo bounds center=(-38, 16, -58) size=(220, 90, 220) to target=(-420, 0, -260) scale=1.85
- TownDistrict_B: cloned 2886 authored roots from Demo bounds center=(-38, 16, -58) size=(220, 90, 220) to target=(-385, 0, 220) scale=1.65
- TownDistrict_C: cloned 2886 authored roots from Demo bounds center=(-38, 16, -58) size=(220, 90, 220) to target=(260, 0, -360) scale=1.55
- TownHighwayStrip: cloned 809 authored roots from Demo bounds center=(-10, 10, 40) size=(90, 80, 190) to target=(-55, 0, -45) scale=2.25
- MilitaryBase: cloned 1059 authored roots from Demo bounds center=(48, 8, 176) size=(130, 80, 140) to target=(410, 0, 245) scale=1.8
- RunwayEdge: cloned 220 authored roots from Demo bounds center=(96, 8, 188) size=(70, 60, 220) to target=(690, 0, 280) scale=2.05
- IndustrialObjective: cloned 186 authored roots from Demo bounds center=(85, 15, 430) size=(280, 120, 240) to target=(470, 0, -395) scale=1.45
- LongHighway: cloned 662 authored roots from Demo bounds center=(-11, 5, 0) size=(42, 35, 880) to target=(0, 0, 0) scale=2.1

Interior terrain/blocker roots rejected from playable clusters: 161

RTS proof units:
- blue infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-430, 0, -245) yaw=36
- blue infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-414, 0, -232) yaw=36
- blue infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-446, 0, -228) yaw=36
- blue infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-398, 0, -214) yaw=36
- blue infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-462, 0, -210) yaw=36
- blue infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-426, 0, -198) yaw=36
- red infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (-300, 0, -158) yaw=216
- red infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (-282, 0, -146) yaw=216
- red infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (-318, 0, -142) yaw=216
- red infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (-264, 0, -130) yaw=216
- red infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (-336, 0, -124) yaw=216
- red infantry: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (-300, 0, -110) yaw=216
- blue APC: Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab at (-455, 0, -295) yaw=38
- blue tank: Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab at (-398, 0, -270) yaw=36
- red armored car: Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab at (-285, 0, -210) yaw=218
- red APC: Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Fast.prefab at (-230, 0, -184) yaw=218
