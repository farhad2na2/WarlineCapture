# Android APK Build Report

- Task: `APH-500`
- Status: `complete`
- Exact commit: `2b19449570ccf1b78c43ea0154e83785ecb3031e`
- Dirty: `false`
- Unity: `6000.5.2f1`
- Build: `release APK`
- Target: `Android`
- Scripting backend: `IL2CPP`
- Target architecture: `ARM64`
- Frame Timing Stats: `enabled`
- Detailed BuildReport: `true`
- Artifact: `Build/AndroidAPK/WarlineCapture.apk`
- Artifact SHA-256: `31bb8749ec04f6be69d65a323a3e60956927aa7ebaa13a9af08c5c5ab4b4e74d`

## Size Accounting

| Measure | Bytes | Meaning |
|---|---:|---|
| Attributed packed assets | 650,446,703 | Sum of BuildReport packed entries with a normalized sourceAssetPath |
| Unattributed packed content | 312,073 | Sum of BuildReport packed entries without a sourceAssetPath |
| Packed file overhead | 860,590 | Sum of PackedAssets.overhead header bytes |
| Accounted packed files | 651,619,366 | Attributed + unattributed + packed file overhead |
| BuildReport summary total size | 3,049,162,850 | BuildSummary.totalSize for all build output |
| BuildReport summary unaccounted | 2,397,543,484 | Summary total minus accounted packed files; signed |
| Compressed package file length | 380,801,943 | APK/AAB artifact file length on disk |

Packed contributions and packed-file overhead come from `BuildReport.packedAssets`. The artifact file length is the compressed APK/AAB package size and is not a per-asset compressed-byte attribution.

## Top 100 Included Assets

- Distinct attributed assets: `6440`
- Rows reported: `100`
- Packed files: `554`
- Packed entries: `23416`

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
| 18 | 6,817,900 | 6.50 | UnityEngine.MonoBehaviour | `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset` |
| 19 | 5,592,564 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Air_Vehicle_Burnt.png` |
| 20 | 5,592,560 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_B.png` |
| 21 | 5,592,552 | 5.33 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Signs 1.png` |
| 22 | 5,592,544 | 5.33 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Soft_Normal.png` |
| 23 | 4,507,444 | 4.30 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_01_A_Combined 10.asset` |
| 24 | 4,194,448 | 4.00 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Desert_Albedo.png` |
| 25 | 4,194,448 | 4.00 | UnityEngine.Texture2D | `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Green_Albedo.png` |
| 26 | 3,870,252 | 3.69 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_calm_loop_01.wav` |
| 27 | 3,563,224 | 3.40 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Gold_A_Combined 3.asset` |
| 28 | 3,533,448 | 3.37 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_menu_loop_01.wav` |
| 29 | 3,457,940 | 3.30 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Frames/dialogue_frame_body.png` |
| 30 | 3,263,132 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_01_CombinedSkinned_19_lod0.asset` |
| 31 | 3,261,992 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_01_CombinedSkinned_25_lod0.asset` |
| 32 | 3,258,160 | 3.11 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_combat_loop_01.wav` |
| 33 | 3,073,976 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_02_CombinedSkinned_26_lod0.asset` |
| 34 | 3,071,860 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_CombinedSkinned_21_lod0.asset` |
| 35 | 2,815,140 | 2.68 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_CombinedSkinned_32_lod0.asset` |
| 36 | 2,697,856 | 2.57 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_02_CombinedSkinned_23_lod0.asset` |
| 37 | 2,532,284 | 2.41 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_CombinedSkinned_27_lod0.asset` |
| 38 | 2,518,736 | 2.40 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Ghillie_Male_01_CombinedSkinned_8_lod0.asset` |
| 39 | 2,512,476 | 2.40 | UnityEngine.MonoBehaviour | `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` |
| 40 | 2,497,864 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01_Damaged.png` |
| 41 | 2,497,864 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01_Emmision.png` |
| 42 | 2,497,860 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Plane_01_Damaged.png` |
| 43 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/PolygonMilitary_Camo_Netting_01.tga` |
| 44 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_01.png` |
| 45 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_02.png` |
| 46 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_03.png` |
| 47 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_04.png` |
| 48 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_05.png` |
| 49 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_08.png` |
| 50 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_09.png` |
| 51 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_10.png` |
| 52 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_01_A.png` |
| 53 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_02_A.png` |
| 54 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_03_A.png` |
| 55 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_04_A.png` |
| 56 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01.png` |
| 57 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_02.png` |
| 58 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_06.png` |
| 59 | 2,497,852 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Land_Vehicle_Master_Burnt_01.png` |
| 60 | 2,497,848 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Land_Vehicle_Master_01.png` |
| 61 | 2,497,840 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Air_Veh_Large_01.png` |
| 62 | 2,497,840 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_A.png` |
| 63 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Road_Texture.png` |
| 64 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Jet_01_A.png` |
| 65 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Jet_02_A.png` |
| 66 | 2,461,636 | 2.35 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_02_CombinedSkinned_7_lod0.asset` |
| 67 | 2,398,900 | 2.29 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/LowLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset` |
| 68 | 2,295,460 | 2.19 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_02_CombinedSkinned_12_lod0.asset` |
| 69 | 2,290,848 | 2.18 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31_lod0.asset` |
| 70 | 2,276,388 | 2.17 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_02_CombinedSkinned_29_lod0.asset` |
| 71 | 2,272,780 | 2.17 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_02_CombinedSkinned_20_lod0.asset` |
| 72 | 2,266,532 | 2.16 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Female_01_CombinedSkinned_5_lod0.asset` |
| 73 | 2,218,996 | 2.12 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_CombinedSkinned_24_lod0.asset` |
| 74 | 2,181,604 | 2.08 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_01_CombinedSkinned_9_lod0.asset` |
| 75 | 2,143,396 | 2.04 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_01_CombinedSkinned_22_lod0.asset` |
| 76 | 2,136,972 | 2.04 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Land_Vehicles_5_Combined.asset` |
| 77 | 2,025,228 | 1.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_03_CombinedSkinned_30_lod0.asset` |
| 78 | 2,011,172 | 1.92 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_05_CombinedSkinned_15_lod0.asset` |
| 79 | 1,883,944 | 1.80 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_01_CombinedSkinned_6_lod0.asset` |
| 80 | 1,829,928 | 1.75 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Bld_Hall_01.fbx` |
| 81 | 1,786,372 | 1.70 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Final/scn19_roster_card_default_frame.png` |
| 82 | 1,786,372 | 1.70 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Final/scn19_roster_card_locked_frame.png` |
| 83 | 1,786,372 | 1.70 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Final/scn19_roster_card_selected_frame.png` |
| 84 | 1,747,420 | 1.67 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Veh_Light_Armored_Car.asset` |
| 85 | 1,689,476 | 1.61 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_01_CombinedSkinned_11_lod0.asset` |
| 86 | 1,597,004 | 1.52 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_01_CombinedSkinned_28_lod0.asset` |
| 87 | 1,581,880 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/FirstLaunch/Commander/commander_portrait_choices.png` |
| 88 | 1,578,992 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` |
| 89 | 1,573,472 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_campaign_valley.png` |
| 90 | 1,573,472 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_operations_radar.png` |
| 91 | 1,573,472 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_skirmish_airbase.png` |
| 92 | 1,573,464 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait.png` |
| 93 | 1,536,504 | 1.47 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_Light_Armored_Car_01.fbx` |
| 94 | 1,533,116 | 1.46 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_04_CombinedSkinned_14_lod0.asset` |
| 95 | 1,521,828 | 1.45 | UnityEngine.Mesh | `Assets/BakedPoses/HumanM@DualGun_Aim01_Pose_0.50.asset` |
| 96 | 1,496,820 | 1.43 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_APC_Heavy_01.fbx` |
| 97 | 1,450,476 | 1.38 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_02_CombinedSkinned_10_lod0.asset` |
| 98 | 1,442,312 | 1.38 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Bld_OilTower_01.fbx` |
| 99 | 1,398,224 | 1.33 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/BatchTexture.asset` |
| 100 | 1,341,748 | 1.28 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_SUV_01.fbx` |
