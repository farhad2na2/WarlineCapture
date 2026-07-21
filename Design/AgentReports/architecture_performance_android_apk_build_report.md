# Android APK Build Report

- Task: `APH-500`
- Status: `complete`
- Exact commit: `b089517166fbf49489f1b85e573ed0d35d47436c`
- Dirty: `true`
- Unity: `6000.5.2f1`
- Build: `release APK`
- Target: `Android`
- Scripting backend: `IL2CPP`
- Target architecture: `ARM64`
- Frame Timing Stats: `enabled`
- Detailed BuildReport: `true`
- Artifact: `Build/AndroidAPK/WarlineCapture.apk`
- Artifact SHA-256: `0d70364c74553467b1d682c883c5d51021d09994e1d4c3fed3b924dfef969d55`

## Size Accounting

| Measure | Bytes | Meaning |
|---|---:|---|
| Attributed packed assets | 464,572,890 | Sum of BuildReport packed entries with a normalized sourceAssetPath |
| Unattributed packed content | 78,927 | Sum of BuildReport packed entries without a sourceAssetPath |
| Packed file overhead | 661,019 | Sum of PackedAssets.overhead header bytes |
| Accounted packed files | 465,312,836 | Attributed + unattributed + packed file overhead |
| BuildReport summary total size | 3,084,145,752 | BuildSummary.totalSize for all build output |
| BuildReport summary unaccounted | 2,618,832,916 | Summary total minus accounted packed files; signed |
| Compressed package file length | 476,851,204 | APK/AAB artifact file length on disk |

Packed contributions and packed-file overhead come from `BuildReport.packedAssets`. The artifact file length is the compressed APK/AAB package size and is not a per-asset compressed-byte attribution.

## Top 100 Included Assets

- Distinct attributed assets: `5670`
- Rows reported: `100`
- Packed files: `40`
- Packed entries: `21727`

| Rank | Packed bytes | MiB | Object types | Source asset path |
|---:|---:|---:|---|---|
| 1 | 22,369,788 | 21.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` |
| 2 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset` |
| 3 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset` |
| 4 | 16,777,348 | 16.00 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset` |
| 5 | 9,961,684 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` |
| 6 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` |
| 7 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` |
| 8 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` |
| 9 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` |
| 10 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` |
| 11 | 9,961,676 | 9.50 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` |
| 12 | 5,592,564 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Air_Vehicle_Burnt.png` |
| 13 | 5,592,552 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Signs 1.png` |
| 14 | 4,507,444 | 4.30 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_01_A_Combined 10.asset` |
| 15 | 3,870,252 | 3.69 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_calm_loop_01.wav` |
| 16 | 3,563,224 | 3.40 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Gold_A_Combined 3.asset` |
| 17 | 3,533,448 | 3.37 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_menu_loop_01.wav` |
| 18 | 3,457,940 | 3.30 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Frames/dialogue_frame_body.png` |
| 19 | 3,263,132 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_01_CombinedSkinned_19_lod0.asset` |
| 20 | 3,261,992 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_01_CombinedSkinned_25_lod0.asset` |
| 21 | 3,258,160 | 3.11 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_combat_loop_01.wav` |
| 22 | 3,073,976 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_02_CombinedSkinned_26_lod0.asset` |
| 23 | 3,071,860 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_CombinedSkinned_21_lod0.asset` |
| 24 | 2,815,140 | 2.68 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_CombinedSkinned_32_lod0.asset` |
| 25 | 2,697,856 | 2.57 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_02_CombinedSkinned_23_lod0.asset` |
| 26 | 2,532,284 | 2.41 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_CombinedSkinned_27_lod0.asset` |
| 27 | 2,518,736 | 2.40 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Ghillie_Male_01_CombinedSkinned_8_lod0.asset` |
| 28 | 2,497,864 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01_Emmision.png` |
| 29 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_03.png` |
| 30 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_05.png` |
| 31 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_01_A.png` |
| 32 | 2,497,852 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Land_Vehicle_Master_Burnt_01.png` |
| 33 | 2,497,848 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Land_Vehicle_Master_01.png` |
| 34 | 2,497,840 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Air_Veh_Large_01.png` |
| 35 | 2,497,840 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_A.png` |
| 36 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Road_Texture.png` |
| 37 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Jet_01_A.png` |
| 38 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Jet_02_A.png` |
| 39 | 2,461,636 | 2.35 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_02_CombinedSkinned_7_lod0.asset` |
| 40 | 2,398,900 | 2.29 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/LowLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset` |
| 41 | 2,295,460 | 2.19 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_02_CombinedSkinned_12_lod0.asset` |
| 42 | 2,290,848 | 2.18 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31_lod0.asset` |
| 43 | 2,276,388 | 2.17 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_02_CombinedSkinned_29_lod0.asset` |
| 44 | 2,272,780 | 2.17 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_02_CombinedSkinned_20_lod0.asset` |
| 45 | 2,266,532 | 2.16 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Female_01_CombinedSkinned_5_lod0.asset` |
| 46 | 2,218,996 | 2.12 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_CombinedSkinned_24_lod0.asset` |
| 47 | 2,181,604 | 2.08 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_01_CombinedSkinned_9_lod0.asset` |
| 48 | 2,143,396 | 2.04 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_01_CombinedSkinned_22_lod0.asset` |
| 49 | 2,136,972 | 2.04 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Land_Vehicles_5_Combined.asset` |
| 50 | 2,025,228 | 1.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_03_CombinedSkinned_30_lod0.asset` |
| 51 | 2,011,172 | 1.92 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_05_CombinedSkinned_15_lod0.asset` |
| 52 | 1,883,944 | 1.80 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_01_CombinedSkinned_6_lod0.asset` |
| 53 | 1,829,928 | 1.75 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Bld_Hall_01.fbx` |
| 54 | 1,786,372 | 1.70 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Final/scn19_roster_card_default_frame.png` |
| 55 | 1,786,372 | 1.70 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Final/scn19_roster_card_locked_frame.png` |
| 56 | 1,786,372 | 1.70 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Final/scn19_roster_card_selected_frame.png` |
| 57 | 1,747,420 | 1.67 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Veh_Light_Armored_Car.asset` |
| 58 | 1,689,476 | 1.61 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_01_CombinedSkinned_11_lod0.asset` |
| 59 | 1,597,004 | 1.52 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_01_CombinedSkinned_28_lod0.asset` |
| 60 | 1,581,880 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/FirstLaunch/Commander/commander_portrait_choices.png` |
| 61 | 1,578,992 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` |
| 62 | 1,573,472 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_campaign_valley.png` |
| 63 | 1,573,472 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_operations_radar.png` |
| 64 | 1,573,472 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_skirmish_airbase.png` |
| 65 | 1,573,464 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait.png` |
| 66 | 1,536,504 | 1.47 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_Light_Armored_Car_01.fbx` |
| 67 | 1,533,116 | 1.46 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_04_CombinedSkinned_14_lod0.asset` |
| 68 | 1,521,828 | 1.45 | UnityEngine.Mesh | `Assets/BakedPoses/HumanM@DualGun_Aim01_Pose_0.50.asset` |
| 69 | 1,450,476 | 1.38 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_02_CombinedSkinned_10_lod0.asset` |
| 70 | 1,398,224 | 1.33 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/BatchTexture.asset` |
| 71 | 1,341,196 | 1.28 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset` |
| 72 | 1,250,848 | 1.19 | UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_blackout_relay_preview_v01.png` |
| 73 | 1,250,840 | 1.19 | UnityEngine.Texture2D | `Built-in Texture2D: Splash Screen Unity Logo` |
| 74 | 1,222,320 | 1.17 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV04Imagegen/Sprites/scn01_v04_loading_panel_frame.png` |
| 75 | 1,213,328 | 1.16 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_03_CombinedSkinned_13_lod0.asset` |
| 76 | 1,169,480 | 1.12 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Pilot_Female_01_CombinedSkinned_17_lod0.asset` |
| 77 | 1,124,864 | 1.07 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_03_overview_panel_frame.png` |
| 78 | 1,098,196 | 1.05 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_APC_Heavy_01_Destroyed.fbx` |
| 79 | 1,095,068 | 1.04 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png` |
| 80 | 1,092,108 | 1.04 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_selected.png` |
| 81 | 1,087,624 | 1.04 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_amber.png` |
| 82 | 1,084,900 | 1.03 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_amber.png` |
| 83 | 1,078,464 | 1.03 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` |
| 84 | 1,064,140 | 1.01 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset` |
| 85 | 1,063,220 | 1.01 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset` |
| 86 | 1,062,544 | 1.01 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset` |
| 87 | 1,017,848 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_Large.png` |
| 88 | 1,017,848 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_disabled.png` |
| 89 | 1,017,848 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_hover.png` |
| 90 | 1,017,848 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_pressed.png` |
| 91 | 1,017,848 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_selected.png` |
| 92 | 1,017,840 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_blue.png` |
| 93 | 1,017,832 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame.png` |
| 94 | 1,017,136 | 0.97 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_blue.png` |
| 95 | 983,796 | 0.94 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Bombsuit_Male_01_CombinedSkinned_0_lod0.asset` |
| 96 | 972,220 | 0.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Veh_APC_Heavy.asset` |
| 97 | 969,884 | 0.92 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_Truck_01.fbx` |
| 98 | 966,304 | 0.92 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_02_commander_identity_panel_frame.png` |
| 99 | 958,636 | 0.91 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Pilot_Male_01_CombinedSkinned_18_lod0.asset` |
| 100 | 907,500 | 0.87 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Land_Vehicles_3_Combined.asset` |
