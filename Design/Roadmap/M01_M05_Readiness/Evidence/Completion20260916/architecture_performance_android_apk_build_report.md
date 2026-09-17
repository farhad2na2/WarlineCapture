# Android APK Build Report

- Task: `APH-500`
- Status: `complete`
- Exact commit: `b70b529b40c9cb8689402824b29190a2443d420c`
- Dirty: `true`
- Unity: `6000.5.2f1`
- Build: `release APK`
- Target: `Android`
- Scripting backend: `IL2CPP`
- Target architecture: `ARM64`
- Frame Timing Stats: `enabled`
- Detailed BuildReport: `true`
- Artifact: `Build/AndroidAPK/WarlineCapture.apk`
- Artifact SHA-256: `6aa83f296e60f45cc4adb8ddd102c3b1f5fd2b9557bed7589d29ec77790b1173`

## Size Accounting

| Measure | Bytes | Meaning |
|---|---:|---|
| Attributed packed assets | 518,044,547 | Sum of BuildReport packed entries with a normalized sourceAssetPath |
| Unattributed packed content | 79,827 | Sum of BuildReport packed entries without a sourceAssetPath |
| Packed file overhead | 1,005,182 | Sum of PackedAssets.overhead header bytes |
| Accounted packed files | 519,129,556 | Attributed + unattributed + packed file overhead |
| BuildReport summary total size | 3,549,450,758 | BuildSummary.totalSize for all build output |
| BuildReport summary unaccounted | 3,030,321,202 | Summary total minus accounted packed files; signed |
| Compressed package file length | 634,020,483 | APK/AAB artifact file length on disk |

Packed contributions and packed-file overhead come from `BuildReport.packedAssets`. The artifact file length is the compressed APK/AAB package size and is not a per-asset compressed-byte attribution.

## Top 100 Included Assets

- Distinct attributed assets: `6424`
- Rows reported: `100`
- Packed files: `41`
- Packed entries: `33610`

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
| 14 | 4,988,208 | 4.76 | UnityEngine.Texture2D | `Assets/Synty/PolygonGeneric/Textures/SyntyLensDirt_01.png` |
| 15 | 4,720,692 | 4.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/Inbox/SCN15_NorthBridgeIntel_V3.png` |
| 16 | 4,720,684 | 4.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/Events/SCN16_ARIAFieldTrials_V3.png` |
| 17 | 4,720,204 | 4.50 | UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/SkirmishSetup/TargetLockV02/scn13_operation_preview_sahrin_v02.png` |
| 18 | 4,720,196 | 4.50 | UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png` |
| 19 | 4,720,188 | 4.50 | UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/MissionBriefing/SCN06_ForwardPost_V3.png` |
| 20 | 4,718,176 | 4.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/Portraits/SCN06_EnemyOfficer_V3.png` |
| 21 | 4,718,168 | 4.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/RewardUnlock/POP04_RangerSquad_V3.png` |
| 22 | 4,717,284 | 4.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/Backgrounds/SCN01_LoadingEnvironment_V3.png` |
| 23 | 4,507,444 | 4.30 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_01_A_Combined 10.asset` |
| 24 | 3,870,252 | 3.69 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_calm_loop_01.wav` |
| 25 | 3,563,224 | 3.40 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Gold_A_Combined 3.asset` |
| 26 | 3,533,448 | 3.37 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_menu_loop_01.wav` |
| 27 | 3,263,132 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_01_CombinedSkinned_19_lod0.asset` |
| 28 | 3,261,992 | 3.11 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_01_CombinedSkinned_25_lod0.asset` |
| 29 | 3,258,160 | 3.11 | UnityEngine.AudioClip | `Assets/Game/Audio/Music/music_match_combat_loop_01.wav` |
| 30 | 3,073,976 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_02_CombinedSkinned_26_lod0.asset` |
| 31 | 3,071,860 | 2.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_CombinedSkinned_21_lod0.asset` |
| 32 | 2,815,140 | 2.68 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_CombinedSkinned_32_lod0.asset` |
| 33 | 2,697,856 | 2.57 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_02_CombinedSkinned_23_lod0.asset` |
| 34 | 2,532,284 | 2.41 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_CombinedSkinned_27_lod0.asset` |
| 35 | 2,518,736 | 2.40 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Ghillie_Male_01_CombinedSkinned_8_lod0.asset` |
| 36 | 2,506,772 | 2.39 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset` |
| 37 | 2,497,864 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Vehicles_01_Emmision.png` |
| 38 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_03.png` |
| 39 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/PolygonMilitary_Land_Vehicles_05.png` |
| 40 | 2,497,856 | 2.38 | UnityEngine.Texture2D | `Assets/Synty/PolygonBattleRoyale/Textures/PolygonBattleRoyale_Texture_01_A.png` |
| 41 | 2,497,852 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Land_Vehicle_Master_Burnt_01.png` |
| 42 | 2,497,848 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Land_Vehicle_Master_01.png` |
| 43 | 2,497,840 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Air_Veh_Large_01.png` |
| 44 | 2,497,840 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_A.png` |
| 45 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Road_Texture.png` |
| 46 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Jet_01_A.png` |
| 47 | 2,497,836 | 2.38 | UnityEngine.Texture2D | `Assets/PolygonMilitary/Textures/Vehicles/Veh_Jet_02_A.png` |
| 48 | 2,461,636 | 2.35 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_02_CombinedSkinned_7_lod0.asset` |
| 49 | 2,398,900 | 2.29 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/LowLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset` |
| 50 | 2,295,460 | 2.19 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_02_CombinedSkinned_12_lod0.asset` |
| 51 | 2,290,848 | 2.18 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31_lod0.asset` |
| 52 | 2,276,388 | 2.17 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_02_CombinedSkinned_29_lod0.asset` |
| 53 | 2,272,780 | 2.17 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_02_CombinedSkinned_20_lod0.asset` |
| 54 | 2,266,532 | 2.16 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Female_01_CombinedSkinned_5_lod0.asset` |
| 55 | 2,218,996 | 2.12 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_CombinedSkinned_24_lod0.asset` |
| 56 | 2,181,604 | 2.08 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_01_CombinedSkinned_9_lod0.asset` |
| 57 | 2,143,396 | 2.04 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_01_CombinedSkinned_22_lod0.asset` |
| 58 | 2,136,972 | 2.04 | UnityEngine.Mesh | `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Land_Vehicles_5_Combined.asset` |
| 59 | 2,025,228 | 1.93 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_03_CombinedSkinned_30_lod0.asset` |
| 60 | 2,011,172 | 1.92 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_05_CombinedSkinned_15_lod0.asset` |
| 61 | 1,883,944 | 1.80 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_01_CombinedSkinned_6_lod0.asset` |
| 62 | 1,829,928 | 1.75 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Bld_Hall_01.fbx` |
| 63 | 1,747,420 | 1.67 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Veh_Light_Armored_Car.asset` |
| 64 | 1,689,476 | 1.61 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_01_CombinedSkinned_11_lod0.asset` |
| 65 | 1,597,004 | 1.52 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_01_CombinedSkinned_28_lod0.asset` |
| 66 | 1,581,880 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/FirstLaunch/Commander/commander_portrait_choices.png` |
| 67 | 1,579,448 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/M03RadarWarning/Final/M03-C01.png` |
| 68 | 1,579,012 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/CommanderScenes/SCN02_FieldCommander_01_Scene_V3.png` |
| 69 | 1,578,992 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` |
| 70 | 1,578,976 | 1.51 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/Narrative/M04Airlift/Final/M04-B01.png` |
| 71 | 1,578,488 | 1.51 | UnityEngine.Texture2D | `Assets/Game/Art/Narrative/M03RadarWarning/Final/M03-B01.png` |
| 72 | 1,578,488 | 1.51 | UnityEngine.Texture2D | `Assets/Game/Art/Narrative/M03RadarWarning/Final/M03-D01.png` |
| 73 | 1,578,488 | 1.51 | UnityEngine.Texture2D | `Assets/Game/Art/Narrative/M05BreachAssault/Final/M05-B01.png` |
| 74 | 1,578,488 | 1.51 | UnityEngine.Texture2D | `Assets/Game/Art/Narrative/M05BreachAssault/Final/M05-D03.png` |
| 75 | 1,576,000 | 1.50 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png` |
| 76 | 1,536,504 | 1.47 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_Light_Armored_Car_01.fbx` |
| 77 | 1,533,116 | 1.46 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_04_CombinedSkinned_14_lod0.asset` |
| 78 | 1,521,828 | 1.45 | UnityEngine.Mesh | `Assets/BakedPoses/HumanM@DualGun_Aim01_Pose_0.50.asset` |
| 79 | 1,450,476 | 1.38 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_02_CombinedSkinned_10_lod0.asset` |
| 80 | 1,398,224 | 1.33 | UnityEngine.Texture2D | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/BatchTexture.asset` |
| 81 | 1,341,196 | 1.28 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset` |
| 82 | 1,250,856 | 1.19 | UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/MissionResult/POP05_M01_OldMarket_ResultBackdrop_V3.png` |
| 83 | 1,250,840 | 1.19 | UnityEngine.Texture2D | `Built-in Texture2D: Splash Screen Unity Logo` |
| 84 | 1,213,328 | 1.16 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_03_CombinedSkinned_13_lod0.asset` |
| 85 | 1,180,788 | 1.13 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/V3Shared/IntelReveal/POP08_EvidenceAtlas_V3.png` |
| 86 | 1,169,480 | 1.12 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Pilot_Female_01_CombinedSkinned_17_lod0.asset` |
| 87 | 1,098,196 | 1.05 | UnityEngine.Mesh | `Assets/PolygonMilitary/Models/SM_Veh_APC_Heavy_01_Destroyed.fbx` |
| 88 | 1,078,464 | 1.03 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` |
| 89 | 1,076,360 | 1.03 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset` |
| 90 | 1,065,324 | 1.02 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset` |
| 91 | 1,064,140 | 1.01 | UnityEngine.Material, UnityEngine.MonoBehaviour, UnityEngine.Texture2D | `Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset` |
| 92 | 1,053,340 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_attack.png` |
| 93 | 1,052,076 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_support.png` |
| 94 | 1,051,360 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_scan.png` |
| 95 | 1,051,084 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_hold.png` |
| 96 | 1,050,820 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_move.png` |
| 97 | 1,050,736 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_stop.png` |
| 98 | 1,050,340 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_build.png` |
| 99 | 1,049,972 | 1.00 | UnityEngine.Sprite, UnityEngine.Texture2D | `Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned/v3_match_command_select.png` |
| 100 | 983,796 | 0.94 | UnityEngine.Mesh | `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Bombsuit_Male_01_CombinedSkinned_0_lod0.asset` |
