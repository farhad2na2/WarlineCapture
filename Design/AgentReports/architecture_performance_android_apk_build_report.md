# Android APK Build Report

- Task: `APH-500`
- Status: `complete`
- Exact commit: `5a49ab8f010674ca8b364af1245fe2902401b305`
- Dirty: `false`
- Unity: `6000.5.2f1`
- Build: `release APK`
- Target: `Android`
- Scripting backend: `IL2CPP`
- Target architecture: `ARM64`
- Detailed BuildReport: `true`
- Artifact: `Build/AndroidAPK/WarlineCapture.apk`
- Artifact SHA-256: `cb18f212d09ebde206884fd608e94441ce4f34fdc5800017067275f892824f20`

## Size Accounting

| Measure | Bytes | Meaning |
|---|---:|---|
| Attributed packed assets | 1,185,382,640 | Sum of BuildReport packed entries with a normalized sourceAssetPath |
| Unattributed packed content | 78,351 | Sum of BuildReport packed entries without a sourceAssetPath |
| Packed file overhead | 571,665 | Sum of PackedAssets.overhead header bytes |
| Accounted packed files | 1,186,032,656 | Attributed + unattributed + packed file overhead |
| BuildReport summary total size | 3,556,896,910 | BuildSummary.totalSize for all build output |
| BuildReport summary unaccounted | 2,370,864,254 | Summary total minus accounted packed files; signed |
| Compressed package file length | 463,359,198 | APK/AAB artifact file length on disk |

Packed contributions and packed-file overhead come from `BuildReport.packedAssets`. The artifact file length is the compressed APK/AAB package size and is not a per-asset compressed-byte attribution.

## Top 100 Included Assets

- Distinct attributed assets: `6104`
- Rows reported: `100`
- Packed files: `40`
- Packed entries: `18618`

| Rank | Packed bytes | MiB | Object types | Source asset path |
|---:|---:|---:|---|---|
| 1 | 22,369,788 | 21.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` |
| 2 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset` |
| 3 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset` |
| 4 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset` |
| 5 | 16,777,336 | 16.00 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/SkyBox.png` |
| 6 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01_Alt_01.png` |
| 7 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01_Alt_02.png` |
| 8 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_01.png` |
| 9 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_02.png` |
| 10 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01_Alt_01.png` |
| 11 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01_Alt_02.png` |
| 12 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_01.png` |
| 13 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_02.png` |
| 14 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_03.png` |
| 15 | 11,184,972 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_04.png` |
| 16 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_01.png` |
| 17 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_02.png` |
| 18 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Female_01.png` |
| 19 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Male_01.png` |
| 20 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Male_02.png` |
| 21 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Female_01.png` |
| 22 | 11,184,968 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Female_02.png` |
| 23 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Bombsuit_Male_01.png` |
| 24 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Male_01.png` |
| 25 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Male_02.png` |
| 26 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Ghillie_Male_01.png` |
| 27 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_01.png` |
| 28 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_02.png` |
| 29 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_03.png` |
| 30 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_04.png` |
| 31 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_05.png` |
| 32 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Leader_Male_01.png` |
| 33 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Pilot_Female_01.png` |
| 34 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01.png` |
| 35 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02.png` |
| 36 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01.png` |
| 37 | 11,184,964 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02.png` |
| 38 | 11,184,960 | 10.67 | UnityEngine.Texture2D | `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Pilot_Male_01.png` |
| 39 | 9,961,684 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` |
| 40 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` |
| 41 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` |
| 42 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` |
| 43 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png` |
| 44 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png` |
| 45 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` |
| 46 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png` |
| 47 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` |
| 48 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` |
| 49 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png` |
| 50 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png` |
| 51 | 6,292,064 | 6.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_campaign_valley.png` |
| 52 | 6,292,064 | 6.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_operations_radar.png` |
| 53 | 6,292,064 | 6.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_skirmish_airbase.png` |
| 54 | 6,292,056 | 6.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait.png` |
| 55 | 6,291,540 | 6.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` |
| 56 | 5,803,944 | 5.54 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png` |
| 57 | 5,800,984 | 5.53 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_selected.png` |
| 58 | 5,772,760 | 5.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_amber.png` |
| 59 | 5,770,036 | 5.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_amber.png` |
| 60 | 5,592,564 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Air_Vehicle_Burnt.png` |
| 61 | 5,592,560 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_B.png` |
| 62 | 5,592,552 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Signs 1.png` |
| 63 | 5,592,544 | 5.33 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Soft_Normal.png` |
| 64 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_Large.png` |
| 65 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_disabled.png` |
| 66 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_hover.png` |
| 67 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_pressed.png` |
| 68 | 5,392,636 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_selected.png` |
| 69 | 5,392,628 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_blue.png` |
| 70 | 5,392,620 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame.png` |
| 71 | 5,391,924 | 5.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_blue.png` |
| 72 | 4,719,236 | 4.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_rifle_portrait.png` |
| 73 | 4,580,084 | 4.37 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_frame.png` |
| 74 | 4,579,892 | 4.37 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_selected_frame.png` |
| 75 | 4,574,844 | 4.36 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_selected_panel_frame.png` |
| 76 | 4,519,516 | 4.31 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_selected.png` |
| 77 | 4,507,444 | 4.30 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_01_A_Combined 10.asset` |
| 78 | 4,245,052 | 4.05 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_objectives_panel_frame.png` |
| 79 | 4,194,448 | 4.00 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Desert_Albedo.png` |
| 80 | 4,194,448 | 4.00 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Green_Albedo.png` |
| 81 | 4,164,172 | 3.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_skirmish_crossed_weapons_icon.png` |
| 82 | 3,947,060 | 3.76 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_backing_default.png` |
| 83 | 3,947,052 | 3.76 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_default.png` |
| 84 | 3,698,604 | 3.53 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_amber.png` |
| 85 | 3,620,132 | 3.45 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_selected.png` |
| 86 | 3,596,808 | 3.43 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_blue.png` |
| 87 | 3,563,224 | 3.40 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Gold_A_Combined 3.asset` |
| 88 | 3,392,364 | 3.24 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_rect_button_selected_frame.png` |
| 89 | 3,360,216 | 3.20 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_settings_gear_icon.png` |
| 90 | 3,324,040 | 3.17 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_disabled.png` |
| 91 | 3,324,040 | 3.17 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_pressed.png` |
| 92 | 3,324,040 | 3.17 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_selected.png` |
| 93 | 3,324,032 | 3.17 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_hover.png` |
| 94 | 3,324,024 | 3.17 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame.png` |
| 95 | 3,293,096 | 3.14 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_operations_star_icon.png` |
| 96 | 3,275,800 | 3.12 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_campaign_target_icon.png` |
| 97 | 3,263,132 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_01_CombinedSkinned_19_lod0.asset` |
| 98 | 3,261,992 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_01_CombinedSkinned_25_lod0.asset` |
| 99 | 3,245,852 | 3.10 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_command_shield_icon.png` |
| 100 | 3,201,756 | 3.05 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_button_frame.png` |
