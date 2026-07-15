# Android APK Build Report

- Task: `APH-500`
- Status: `complete`
- Exact commit: `17566e35605b3925c96df5d86f173f382251f12a`
- Dirty: `false`
- Unity: `6000.5.2f1`
- Build: `release APK`
- Target: `Android`
- Scripting backend: `IL2CPP`
- Target architecture: `ARM64`
- Detailed BuildReport: `true`
- Artifact: `Build/AndroidAPK/WarlineCapture.apk`
- Artifact SHA-256: `7cf40efabd5e2c31298762745f4b68ad235a1b3f661065845a9c4d9487f26a0a`

## Size Accounting

| Measure | Bytes | Meaning |
|---|---:|---|
| Attributed packed assets | 830,252,375 | Sum of BuildReport packed entries with a normalized sourceAssetPath |
| Unattributed packed content | 312,073 | Sum of BuildReport packed entries without a sourceAssetPath |
| Packed file overhead | 860,406 | Sum of PackedAssets.overhead header bytes |
| Accounted packed files | 831,424,854 | Attributed + unattributed + packed file overhead |
| BuildReport summary total size | 3,348,817,847 | BuildSummary.totalSize for all build output |
| BuildReport summary unaccounted | 2,517,392,993 | Summary total minus accounted packed files; signed |
| Compressed package file length | 505,130,133 | APK/AAB artifact file length on disk |

Packed contributions and packed-file overhead come from `BuildReport.packedAssets`. The artifact file length is the compressed APK/AAB package size and is not a per-asset compressed-byte attribution.

## Top 100 Included Assets

- Distinct attributed assets: `6432`
- Rows reported: `100`
- Packed files: `554`
- Packed entries: `23408`

| Rank | Packed bytes | MiB | Object types | Source asset path |
|---:|---:|---:|---|---|
| 1 | 22,369,788 | 21.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` |
| 2 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset` |
| 3 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset` |
| 4 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset` |
| 5 | 16,777,336 | 16.00 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/SkyBox.png` |
| 6 | 9,961,684 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` |
| 7 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` |
| 8 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` |
| 9 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` |
| 10 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png` |
| 11 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png` |
| 12 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` |
| 13 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png` |
| 14 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` |
| 15 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` |
| 16 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png` |
| 17 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png` |
| 18 | 8,389,212 | 8.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_campaign_valley.png` |
| 19 | 8,389,212 | 8.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_operations_radar.png` |
| 20 | 8,389,212 | 8.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_skirmish_airbase.png` |
| 21 | 8,389,204 | 8.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait.png` |
| 22 | 8,388,512 | 8.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` |
| 23 | 6,817,900 | 6.50 | UnityEngine.MonoBehaviour | `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset` |
| 24 | 5,803,944 | 5.54 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png` |
| 25 | 5,800,984 | 5.53 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_selected.png` |
| 26 | 5,772,760 | 5.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_amber.png` |
| 27 | 5,770,036 | 5.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_amber.png` |
| 28 | 5,592,564 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Air_Vehicle_Burnt.png` |
| 29 | 5,592,560 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_B.png` |
| 30 | 5,592,552 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Signs 1.png` |
| 31 | 5,592,544 | 5.33 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Soft_Normal.png` |
| 32 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_Large.png` |
| 33 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_disabled.png` |
| 34 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_hover.png` |
| 35 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_pressed.png` |
| 36 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_selected.png` |
| 37 | 5,392,628 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_blue.png` |
| 38 | 5,392,620 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame.png` |
| 39 | 5,391,924 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_blue.png` |
| 40 | 4,716,792 | 4.50 | UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_operations_thumbnail_art.png` |
| 41 | 4,716,788 | 4.50 | UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_campaign_thumbnail_art.png` |
| 42 | 4,716,788 | 4.50 | UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_skirmish_thumbnail_art.png` |
| 43 | 4,580,084 | 4.37 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_frame.png` |
| 44 | 4,579,892 | 4.37 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_selected_frame.png` |
| 45 | 4,574,844 | 4.36 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_selected_panel_frame.png` |
| 46 | 4,519,516 | 4.31 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_selected.png` |
| 47 | 4,507,444 | 4.30 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_01_A_Combined 10.asset` |
| 48 | 4,356,512 | 4.15 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_panel_frame.png` |
| 49 | 4,245,052 | 4.05 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_objectives_panel_frame.png` |
| 50 | 4,194,448 | 4.00 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Desert_Albedo.png` |
| 51 | 4,194,448 | 4.00 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Green_Albedo.png` |
| 52 | 4,164,172 | 3.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_skirmish_crossed_weapons_icon.png` |
| 53 | 3,947,060 | 3.76 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_backing_default.png` |
| 54 | 3,947,052 | 3.76 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_default.png` |
| 55 | 3,870,252 | 3.69 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_calm_loop_01.wav` |
| 56 | 3,698,604 | 3.53 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_amber.png` |
| 57 | 3,620,132 | 3.45 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_selected.png` |
| 58 | 3,596,808 | 3.43 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_blue.png` |
| 59 | 3,563,224 | 3.40 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Gold_A_Combined 3.asset` |
| 60 | 3,533,448 | 3.37 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_menu_loop_01.wav` |
| 61 | 3,457,940 | 3.30 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Frames/dialogue_frame_body.png` |
| 62 | 3,392,364 | 3.24 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_rect_button_selected_frame.png` |
| 63 | 3,360,216 | 3.20 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_settings_gear_icon.png` |
| 64 | 3,293,096 | 3.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_operations_star_icon.png` |
| 65 | 3,275,800 | 3.12 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_campaign_target_icon.png` |
| 66 | 3,263,132 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_01_CombinedSkinned_19_lod0.asset` |
| 67 | 3,261,992 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_01_CombinedSkinned_25_lod0.asset` |
| 68 | 3,258,160 | 3.11 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_combat_loop_01.wav` |
| 69 | 3,245,852 | 3.10 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_command_shield_icon.png` |
| 70 | 3,201,756 | 3.05 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_button_frame.png` |
| 71 | 3,201,264 | 3.05 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_button_selected_frame.png` |
| 72 | 3,073,976 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_02_CombinedSkinned_26_lod0.asset` |
| 73 | 3,071,860 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_CombinedSkinned_21_lod0.asset` |
| 74 | 2,963,504 | 2.83 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_rect_button_frame.png` |
| 75 | 2,948,332 | 2.81 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_wide_rail_frame.png` |
| 76 | 2,903,164 | 2.77 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_supply_crate_icon.png` |
| 77 | 2,903,164 | 2.77 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_resource_crate_icon.png` |
| 78 | 2,815,140 | 2.68 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_CombinedSkinned_32_lod0.asset` |
| 79 | 2,744,340 | 2.62 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_resource_diamond_icon.png` |
| 80 | 2,697,856 | 2.57 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_02_CombinedSkinned_23_lod0.asset` |
| 81 | 2,532,284 | 2.41 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_CombinedSkinned_27_lod0.asset` |
| 82 | 2,518,736 | 2.40 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Ghillie_Male_01_CombinedSkinned_8_lod0.asset` |
| 83 | 2,512,476 | 2.40 | UnityEngine.MonoBehaviour | `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` |
| 84 | 2,497,864 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01_Damaged.png` |
| 85 | 2,497,864 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01_Emmision.png` |
| 86 | 2,497,860 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Plane_01_Damaged.png` |
| 87 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Camo_Netting_01.tga` |
| 88 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_01.png` |
| 89 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_02.png` |
| 90 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_03.png` |
| 91 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_04.png` |
| 92 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_05.png` |
| 93 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_08.png` |
| 94 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_09.png` |
| 95 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_10.png` |
| 96 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_01_A.png` |
| 97 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_02_A.png` |
| 98 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_03_A.png` |
| 99 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_04_A.png` |
| 100 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01.png` |
