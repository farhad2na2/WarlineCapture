# Architecture Performance Content Residency Baseline

- Task: `APH-008`
- Audio residency extension: `APH-400`
- Status: `complete`
- Baseline commit: `7084805d771142706f340e9f2e52a68570bcb72b`
- Generated UTC: `2026-07-10T09:49:14.2373020Z`
- Unity: `6000.5.2f1`
- Active build target: `Android`
- Scope: Enabled build scenes, Assets Resources content, PlayerSettings preloaded assets, and StreamingAssets, including transitive AssetDatabase dependencies.

## Summary

| Metric | Value |
|---|---:|
| Dependency roots | 12 |
| Included asset paths | 4,102 |
| Assets with source size | 4,102 |
| Known source bytes | `816,165,398` (778.36 MiB) |
| Assets with measured imported size | 2,682 |
| Known imported bytes | `2,471,900,934` (2,357.39 MiB) |
| Audio assets | 226 |
| Catalog-referenced audio clips | 226 |
| Catalog audio duration | 575.838 s |
| Catalog clips with compressed size | 226 / 226 |
| Known catalog compressed bytes | `50,966,162` (48.61 MiB) |
| Estimated catalog decoded bytes | `101,717,516` (97.01 MiB) |
| Texture assets | 639 |
| Streaming-enabled textures | 0 |
| Mesh assets | 1,817 |
| Read/write-enabled mesh assets | 901 |
| Animation texture assets | 3 |
| Animation texture payload | `50,331,648` (48.00 MiB) |

## Dependency Roots

| Kind | Asset path |
|---|---|
| PreloadedAsset | `Assets/Game/InputSystem_Actions.inputactions` |
| BuildScene | `Assets/Game/Scenes/Match.unity` |
| BuildScene | `Assets/Game/Scenes/Menu.unity` |
| ResourcesAsset | `Assets/Resources/Operation/OperationActionConfigSet.asset` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/LineBreaking Following Characters.txt` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/LineBreaking Leading Characters.txt` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/Style Sheets/Default Style Sheet.asset` |
| ResourcesAsset | `Assets/TextMesh Pro/Resources/TMP Settings.asset` |

## Largest Source Assets

| Asset | Type | Source | Imported | Dependency roots |
|---|---|---:|---:|---|
| `Assets/Game/Scenes/Match.unity` | SceneAsset | `58,822,096` (56.10 MiB) | Unavailable | BuildScene: `Assets/Game/Scenes/Match.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset` | Texture2D | `33,555,477` (32.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset` | Texture2D | `33,555,477` (32.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset` | Texture2D | `33,555,477` (32.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Camo_Netting_01.tga` | Texture2D | `16,777,260` (16.00 MiB) | `4,996,432` (4.76 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity` |
| `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_01_A_Combined 10.asset` | Mesh | `9,017,493` (8.60 MiB) | `9,015,528` (8.60 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/GeneratedCombinedMeshes/Model_PolygonMilitary_Mat_Gold_A_Combined 3.asset` | Mesh | `7,129,054` (6.80 MiB) | `7,127,088` (6.80 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_01_CombinedSkinned_19_lod0.asset` | Mesh | `6,542,874` (6.24 MiB) | `6,521,104` (6.22 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_01_CombinedSkinned_25_lod0.asset` | Mesh | `6,539,724` (6.24 MiB) | `6,518,900` (6.22 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_Alt_02_CombinedSkinned_26_lod0.asset` | Mesh | `6,163,985` (5.88 MiB) | `6,142,840` (5.86 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_CombinedSkinned_21_lod0.asset` | Mesh | `6,159,172` (5.87 MiB) | `6,138,680` (5.85 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_CombinedSkinned_32_lod0.asset` | Mesh | `5,645,481` (5.38 MiB) | `5,625,256` (5.36 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_Alt_02_CombinedSkinned_23_lod0.asset` | Mesh | `5,411,746` (5.16 MiB) | `5,390,608` (5.14 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` | MapSurfaceDataAsset | `5,314,368` (5.07 MiB) | Unavailable | BuildScene: `Assets/Game/Scenes/Match.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_01_CombinedSkinned_27_lod0.asset` | Mesh | `5,079,752` (4.84 MiB) | `5,059,536` (4.83 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Ghillie_Male_01_CombinedSkinned_8_lod0.asset` | Mesh | `5,052,461` (4.82 MiB) | `5,032,436` (4.80 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Male_02_CombinedSkinned_7_lod0.asset` | Mesh | `4,939,304` (4.71 MiB) | `4,918,172` (4.69 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/MidLOD/LowLOD_Unit_Chr_Soldier_Male_02_Alt_04.asset` | Mesh | `4,813,587` (4.59 MiB) | `4,792,752` (4.57 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Male_02_CombinedSkinned_12_lod0.asset` | Mesh | `4,605,989` (4.39 MiB) | `4,585,904` (4.37 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31_lod0.asset` | Mesh | `4,597,465` (4.38 MiB) | `4,576,616` (4.36 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Male_02_Alt_02_CombinedSkinned_29_lod0.asset` | Mesh | `4,567,982` (4.36 MiB) | `4,547,744` (4.34 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_01_Alt_02_CombinedSkinned_20_lod0.asset` | Mesh | `4,561,859` (4.35 MiB) | `4,540,424` (4.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Contractor_Female_01_CombinedSkinned_5_lod0.asset` | Mesh | `4,548,538` (4.34 MiB) | `4,528,008` (4.32 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Soldier_Female_02_CombinedSkinned_24_lod0.asset` | Mesh | `4,453,440` (4.25 MiB) | `4,432,944` (4.23 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/SM_Chr_Insurgent_Female_01_CombinedSkinned_9_lod0.asset` | Mesh | `4,378,679` (4.18 MiB) | `4,358,152` (4.16 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |

## Largest Measured Imported Assets

| Asset | Type | Source | Imported | Dependency roots |
|---|---|---:|---:|---|
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` | Texture2D | `1,725,988` (1.65 MiB) | `44,740,304` (42.67 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/PolygonMilitary/Textures/SkyBox.png` | Texture2D | `2,612,676` (2.49 MiB) | `39,147,843` (37.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset` | Texture2D | `33,555,477` (32.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset` | Texture2D | `33,555,477` (32.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset` | Texture2D | `33,555,477` (32.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01_Alt_01.png` | Texture2D | `311,442` (0.30 MiB) | `22,370,680` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01_Alt_02.png` | Texture2D | `296,637` (0.28 MiB) | `22,370,680` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_01.png` | Texture2D | `294,918` (0.28 MiB) | `22,370,680` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_02.png` | Texture2D | `340,115` (0.32 MiB) | `22,370,680` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01_Alt_01.png` | Texture2D | `296,076` (0.28 MiB) | `22,370,678` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01_Alt_02.png` | Texture2D | `314,991` (0.30 MiB) | `22,370,678` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_01.png` | Texture2D | `409,355` (0.39 MiB) | `22,370,678` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_02.png` | Texture2D | `295,319` (0.28 MiB) | `22,370,678` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_03.png` | Texture2D | `407,740` (0.39 MiB) | `22,370,678` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_04.png` | Texture2D | `314,882` (0.30 MiB) | `22,370,678` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Female_01.png` | Texture2D | `335,644` (0.32 MiB) | `22,370,676` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Female_01.png` | Texture2D | `306,458` (0.29 MiB) | `22,370,675` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Female_02.png` | Texture2D | `388,650` (0.37 MiB) | `22,370,675` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_01.png` | Texture2D | `373,434` (0.36 MiB) | `22,370,674` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_02.png` | Texture2D | `358,859` (0.34 MiB) | `22,370,674` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Male_01.png` | Texture2D | `402,489` (0.38 MiB) | `22,370,674` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Male_02.png` | Texture2D | `344,203` (0.33 MiB) | `22,370,674` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_01.png` | Texture2D | `300,344` (0.29 MiB) | `22,370,673` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_02.png` | Texture2D | `305,489` (0.29 MiB) | `22,370,673` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_03.png` | Texture2D | `335,588` (0.32 MiB) | `22,370,673` (21.33 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |

## Import States

### Build-Included Audio Load Types

| State | Asset count |
|---|---:|
| DecompressOnLoad | 217 |
| Streaming | 9 |

### Texture Mipmap and Streaming

| State | Asset count |
|---|---:|
| mipmaps=false, streaming=false | 508 |
| mipmaps=true, streaming=false | 131 |

### Mesh Read/Write

| State | Asset count |
|---|---:|
| disabled | 916 |
| enabled | 901 |

## Catalog-Referenced Audio Residency

This inventory includes only clips directly referenced by serialized `AudioEventCatalogConfig` assets. Unreferenced project audio is excluded.

Catalog assets:
- `Assets/Game/Audio/Events/AudioEventCatalogConfig.asset`

### Bus and Category Totals

| Bus | Category | Clips | Duration | Compressed | Estimated decoded |
|---|---|---:|---:|---:|---:|
| Alerts | Alerts | 4 | 3.920 s | `459,872` (0.44 MiB) | `691,488` (0.66 MiB) |
| Ambience | Ambience | 2 | 16.000 s | `0` (0.00 MiB) | `2,822,400` (2.69 MiB) |
| Music | Music | 7 | 53.000 s | `0` (0.00 MiB) | `9,349,200` (8.92 MiB) |
| SFX | Gameplay | 32 | 22.060 s | `2,928,598` (2.79 MiB) | `4,031,148` (3.84 MiB) |
| UI | UI | 18 | 9.400 s | `1,342,656` (1.28 MiB) | `1,658,160` (1.58 MiB) |
| Voice | Voice | 163 | 471.458 s | `46,235,036` (44.09 MiB) | `83,165,120` (79.31 MiB) |

### Catalog Clip Detail

| Bus | Category | Clip | Event ID(s) | Duration | Channels | Frequency | Import load type | Compressed | Estimated decoded |
|---|---|---|---|---:|---:|---:|---|---:|---:|
| Alerts | Alerts | `Assets/Game/Audio/Alerts/alert_base_breached_01.wav` | Alert.Base.Breached | 1.200 s | 1 | 44,100 Hz | DecompressOnLoad | `134,372` (0.13 MiB) | `211,680` (0.20 MiB) |
| Alerts | Alerts | `Assets/Game/Audio/Alerts/alert_threat_critical_01.wav` | Alert.Threat.Critical | 1.080 s | 1 | 44,100 Hz | DecompressOnLoad | `123,788` (0.12 MiB) | `190,512` (0.18 MiB) |
| Alerts | Alerts | `Assets/Game/Audio/Alerts/alert_threat_minor_01.wav` | Alert.Threat.Minor | 0.800 s | 1 | 44,100 Hz | DecompressOnLoad | `99,092` (0.09 MiB) | `141,120` (0.13 MiB) |
| Alerts | Alerts | `Assets/Game/Audio/Alerts/alert_unit_under_attack_01.wav` | Alert.Unit.UnderAttack | 0.840 s | 1 | 44,100 Hz | DecompressOnLoad | `102,620` (0.10 MiB) | `148,176` (0.14 MiB) |
| Ambience | Ambience | `Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav` | Ambience.Base.DistantLoop | 8.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `1,411,200` (1.35 MiB) |
| Ambience | Ambience | `Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav` | Ambience.City.DayLoop | 8.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `1,411,200` (1.35 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_briefing_loop_01.wav` | Music.Briefing.Loop | 10.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `1,764,000` (1.68 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_match_calm_loop_01.wav` | Music.Match.CalmLoop | 10.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `1,764,000` (1.68 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_match_combat_loop_01.wav` | Music.Match.CombatLoop | 10.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `1,764,000` (1.68 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_menu_loop_01.wav` | Music.Menu.Loop | 10.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `1,764,000` (1.68 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_result_defeat_01.wav` | Music.Result.Defeat | 4.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `705,600` (0.67 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_result_victory_01.wav` | Music.Result.Victory | 4.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `705,600` (0.67 MiB) |
| Music | Music | `Assets/Game/Audio/Music/music_splash_intro_01.wav` | Music.Splash.Intro | 5.000 s | 1 | 44,100 Hz | Streaming | `0` (0.00 MiB) | `882,000` (0.84 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_build_place_invalid_01.wav` | Gameplay.Build.Place.Invalid | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_build_place_valid_01.wav` | Gameplay.Build.Place.Valid | 0.600 s | 1 | 44,100 Hz | DecompressOnLoad | `81,452` (0.08 MiB) | `105,840` (0.10 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_attack_accepted_01.wav` | Gameplay.Command.Attack.Accepted | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_hold_accepted_01.wav` | Gameplay.Command.Hold.Accepted | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_move_accepted_01.wav` | Gameplay.Command.Move.Accepted | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_rejected_01.wav` | Gameplay.Command.Rejected | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_scan_accepted_01.wav` | Gameplay.Command.Scan.Accepted | 0.520 s | 1 | 44,100 Hz | DecompressOnLoad | `74,396` (0.07 MiB) | `91,728` (0.09 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_scan_targeting_01.wav` | Gameplay.Command.Scan.Targeting | 0.520 s | 1 | 44,100 Hz | DecompressOnLoad | `74,396` (0.07 MiB) | `91,728` (0.09 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_command_stop_returning_01.wav` | Gameplay.Command.Stop.Returning | 0.520 s | 1 | 44,100 Hz | DecompressOnLoad | `74,396` (0.07 MiB) | `91,728` (0.09 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_explosion_large_01.wav` | Gameplay.Explosion.Large | 1.150 s | 1 | 48,000 Hz | DecompressOnLoad | `138,930` (0.13 MiB) | `220,796` (0.21 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_explosion_small_01.wav` | Gameplay.Explosion.Small | 0.720 s | 1 | 48,000 Hz | DecompressOnLoad | `97,652` (0.09 MiB) | `138,240` (0.13 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_impact_bullet_01.wav` | Gameplay.Impact.Bullet | 0.160 s | 1 | 48,000 Hz | DecompressOnLoad | `43,892` (0.04 MiB) | `30,720` (0.03 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_objective_complete_01.wav` | Gameplay.Objective.Complete | 0.880 s | 1 | 44,100 Hz | DecompressOnLoad | `106,148` (0.10 MiB) | `155,232` (0.15 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_objective_failed_01.wav` | Gameplay.Objective.Failed | 0.880 s | 1 | 44,100 Hz | DecompressOnLoad | `106,148` (0.10 MiB) | `155,232` (0.15 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_objective_progress_01.wav` | Gameplay.Objective.Progress | 0.640 s | 1 | 44,100 Hz | DecompressOnLoad | `84,980` (0.08 MiB) | `112,896` (0.11 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_production_complete_01.wav` | Gameplay.Production.Complete | 0.800 s | 1 | 44,100 Hz | DecompressOnLoad | `99,092` (0.09 MiB) | `141,120` (0.13 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_production_queued_01.wav` | Gameplay.Production.Queued | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_aircraft_flyby_01.wav` | Gameplay.Unit.Aircraft.Flyby | 1.150 s | 1 | 48,000 Hz | DecompressOnLoad | `138,930` (0.13 MiB) | `220,796` (0.21 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_flight_01.wav` | Gameplay.Unit.Engine.Aircraft.Flight | 2.800 s | 1 | 48,000 Hz | DecompressOnLoad | `297,332` (0.28 MiB) | `537,600` (0.51 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_takeoff_01.wav` | Gameplay.Unit.Engine.Aircraft.Takeoff | 1.040 s | 1 | 44,100 Hz | DecompressOnLoad | `120,260` (0.11 MiB) | `183,456` (0.17 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_engine_helicopter_flight_01.wav` | Gameplay.Unit.Engine.Helicopter.Flight | 0.600 s | 1 | 44,100 Hz | DecompressOnLoad | `81,452` (0.08 MiB) | `105,840` (0.10 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_engine_vehicle_move_01.wav` | Gameplay.Unit.Engine.Vehicle.Move | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_select_air_01.wav` | Gameplay.Unit.Select.Air | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_select_infantry_01.wav` | Gameplay.Unit.Select.Infantry | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_select_vehicle_01.wav` | Gameplay.Unit.Select.Vehicle | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_vehicle_destroyed_01.wav` | Gameplay.Unit.Vehicle.Destroyed | 0.950 s | 1 | 48,000 Hz | DecompressOnLoad | `119,732` (0.11 MiB) | `182,400` (0.17 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_unit_vehicle_engine_01.wav` | Gameplay.Unit.Vehicle.Engine | 0.720 s | 1 | 44,100 Hz | DecompressOnLoad | `92,036` (0.09 MiB) | `127,008` (0.12 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_weapon_air_missile_launch_01.wav` | Gameplay.Weapon.AirMissile.Launch | 0.580 s | 1 | 48,000 Hz | DecompressOnLoad | `84,210` (0.08 MiB) | `111,356` (0.11 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_weapon_missile_flight_01.wav` | Gameplay.Weapon.Missile.Flight | 0.580 s | 1 | 44,100 Hz | DecompressOnLoad | `79,688` (0.08 MiB) | `102,312` (0.10 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_weapon_missile_launch_01.wav` | Gameplay.Weapon.Missile.Launch | 0.720 s | 1 | 48,000 Hz | DecompressOnLoad | `97,652` (0.09 MiB) | `138,240` (0.13 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_weapon_rifle_fire_01.wav` | Gameplay.Weapon.Rifle.Fire | 0.180 s | 1 | 48,000 Hz | DecompressOnLoad | `45,812` (0.04 MiB) | `34,560` (0.03 MiB) |
| SFX | Gameplay | `Assets/Game/Audio/Gameplay/game_weapon_vehicle_cannon_fire_01.wav` | Gameplay.Weapon.VehicleCannon.Fire | 0.550 s | 1 | 48,000 Hz | DecompressOnLoad | `81,332` (0.08 MiB) | `105,600` (0.10 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_button_disabled_tap_01.wav` | UI.Button.Disabled.Tap | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_button_negative_click_01.wav` | UI.Button.Negative.Click | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_button_primary_click_01.wav` | UI.Button.Primary.Click | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_button_secondary_click_01.wav` | UI.Button.Secondary.Click | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_card_locked_01.wav` | UI.Card.Locked | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_card_select_01.wav` | UI.Card.Select | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_drawer_close_01.wav` | UI.Drawer.Close | 0.520 s | 1 | 44,100 Hz | DecompressOnLoad | `74,396` (0.07 MiB) | `91,728` (0.09 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_drawer_open_01.wav` | UI.Drawer.Open | 0.600 s | 1 | 44,100 Hz | DecompressOnLoad | `81,452` (0.08 MiB) | `105,840` (0.10 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_feedback_toast_error_01.wav` | UI.Feedback.Toast.Error | 0.640 s | 1 | 44,100 Hz | DecompressOnLoad | `84,980` (0.08 MiB) | `112,896` (0.11 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_feedback_toast_positive_01.wav` | UI.Feedback.Toast.Positive | 0.640 s | 1 | 44,100 Hz | DecompressOnLoad | `84,980` (0.08 MiB) | `112,896` (0.11 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_popup_close_01.wav` | UI.Popup.Close | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_popup_open_01.wav` | UI.Popup.Open | 0.520 s | 1 | 44,100 Hz | DecompressOnLoad | `74,396` (0.07 MiB) | `91,728` (0.09 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_screen_back_01.wav` | UI.Screen.Back | 0.600 s | 1 | 44,100 Hz | DecompressOnLoad | `81,452` (0.08 MiB) | `105,840` (0.10 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_screen_forward_01.wav` | UI.Screen.Forward | 0.600 s | 1 | 44,100 Hz | DecompressOnLoad | `81,452` (0.08 MiB) | `105,840` (0.10 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_slider_tick_01.wav` | UI.Slider.Tick | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_tab_select_01.wav` | UI.Tab.Select | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_toggle_off_01.wav` | UI.Toggle.Off | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| UI | UI | `Assets/Game/Audio/UI/ui_toggle_on_01.wav` | UI.Toggle.On | 0.480 s | 1 | 44,100 Hz | DecompressOnLoad | `70,868` (0.07 MiB) | `84,672` (0.08 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_action_place_choose_footprint_01.wav` | VO.ARIA.Message.BuildDrawerActionPlaceChooseFootprint | 4.488 s | 1 | 44,100 Hz | DecompressOnLoad | `424,406` (0.40 MiB) | `791,684` (0.76 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_aircraft_01.wav` | VO.ARIA.Message.BuildDrawerEmptyAircraft | 3.552 s | 1 | 44,100 Hz | DecompressOnLoad | `341,852` (0.33 MiB) | `626,576` (0.60 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_buildings_01.wav` | VO.ARIA.Message.BuildDrawerEmptyBuildings | 3.336 s | 1 | 44,100 Hz | DecompressOnLoad | `322,800` (0.31 MiB) | `588,472` (0.56 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_default_01.wav` | VO.ARIA.Message.BuildDrawerEmptyDefault | 3.312 s | 1 | 44,100 Hz | DecompressOnLoad | `320,684` (0.31 MiB) | `584,240` (0.56 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_name_01.wav` | VO.ARIA.Message.BuildDrawerEmptyName | 2.040 s | 1 | 44,100 Hz | DecompressOnLoad | `208,460` (0.20 MiB) | `359,856` (0.34 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_select_item_01.wav` | VO.ARIA.Message.BuildDrawerEmptySelectItem | 4.224 s | 1 | 44,100 Hz | DecompressOnLoad | `401,122` (0.38 MiB) | `745,116` (0.71 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_soldiers_01.wav` | VO.ARIA.Message.BuildDrawerEmptySoldiers | 3.456 s | 1 | 44,100 Hz | DecompressOnLoad | `333,384` (0.32 MiB) | `609,640` (0.58 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_vehicles_01.wav` | VO.ARIA.Message.BuildDrawerEmptyVehicles | 3.408 s | 1 | 44,100 Hz | DecompressOnLoad | `329,150` (0.31 MiB) | `601,172` (0.57 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_connecting_01.wav` | VO.ARIA.Message.BuildDrawerFailureConnecting | 5.208 s | 1 | 44,100 Hz | DecompressOnLoad | `487,910` (0.47 MiB) | `918,692` (0.88 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_global_queue_full_01.wav` | VO.ARIA.Message.BuildDrawerFailureGlobalQueueFull | 6.288 s | 1 | 44,100 Hz | DecompressOnLoad | `583,166` (0.56 MiB) | `1,109,204` (1.06 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_invalid_selection_01.wav` | VO.ARIA.Message.BuildDrawerFailureInvalidSelection | 3.168 s | 1 | 44,100 Hz | DecompressOnLoad | `307,982` (0.29 MiB) | `558,836` (0.53 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_01.wav` | VO.ARIA.Message.BuildDrawerFailureMissingProducer | 4.248 s | 1 | 44,100 Hz | DecompressOnLoad | `403,238` (0.38 MiB) | `749,348` (0.71 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_named_01.wav` | VO.ARIA.Message.BuildDrawerFailureMissingProducerNamed | 4.752 s | 1 | 44,100 Hz | DecompressOnLoad | `447,692` (0.43 MiB) | `838,256` (0.80 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_not_enough_money_01.wav` | VO.ARIA.Message.BuildDrawerFailureNotEnoughMoney | 3.744 s | 1 | 44,100 Hz | DecompressOnLoad | `358,786` (0.34 MiB) | `660,444` (0.63 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_01.wav` | VO.ARIA.Message.BuildDrawerFailureQueueFull | 5.832 s | 1 | 44,100 Hz | DecompressOnLoad | `542,948` (0.52 MiB) | `1,028,768` (0.98 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_named_01.wav` | VO.ARIA.Message.BuildDrawerFailureQueueFullNamed | 5.688 s | 1 | 44,100 Hz | DecompressOnLoad | `530,246` (0.51 MiB) | `1,003,364` (0.96 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_global_queue_full_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortGlobalQueueFull | 3.840 s | 1 | 44,100 Hz | DecompressOnLoad | `367,252` (0.35 MiB) | `677,376` (0.65 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_missing_producer_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortMissingProducer | 2.904 s | 1 | 44,100 Hz | DecompressOnLoad | `284,666` (0.27 MiB) | `512,268` (0.49 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_not_enough_money_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortNotEnoughMoney | 2.472 s | 1 | 44,100 Hz | DecompressOnLoad | `246,564` (0.24 MiB) | `436,064` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortQueueFull | 3.432 s | 1 | 44,100 Hz | DecompressOnLoad | `331,268` (0.32 MiB) | `605,408` (0.58 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_named_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortQueueFullNamed | 2.928 s | 1 | 44,100 Hz | DecompressOnLoad | `286,782` (0.27 MiB) | `516,500` (0.49 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_requires_named_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortRequiresNamed | 2.376 s | 1 | 44,100 Hz | DecompressOnLoad | `238,096` (0.23 MiB) | `419,128` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_unavailable_01.wav` | VO.ARIA.Message.BuildDrawerFailureShortUnavailable | 2.736 s | 1 | 44,100 Hz | DecompressOnLoad | `269,848` (0.26 MiB) | `482,632` (0.46 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_unavailable_01.wav` | VO.ARIA.Message.BuildDrawerFailureUnavailable | 4.872 s | 1 | 44,100 Hz | DecompressOnLoad | `458,276` (0.44 MiB) | `859,424` (0.82 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_cannot_place_here_01.wav` | VO.ARIA.Message.BuildDrawerInstructionCannotPlaceHere | 3.792 s | 1 | 44,100 Hz | DecompressOnLoad | `363,020` (0.35 MiB) | `668,912` (0.64 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_place_pending_confirm_01.wav` | VO.ARIA.Message.BuildDrawerInstructionPlacePendingConfirm | 5.160 s | 1 | 44,100 Hz | DecompressOnLoad | `483,676` (0.46 MiB) | `910,224` (0.87 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_placement_invalid_01.wav` | VO.ARIA.Message.BuildDrawerPlacementInvalid | 2.376 s | 1 | 44,100 Hz | DecompressOnLoad | `238,096` (0.23 MiB) | `419,128` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_aircraft_01.wav` | VO.ARIA.Message.BuildDrawerReadyAircraft | 4.776 s | 1 | 44,100 Hz | DecompressOnLoad | `449,808` (0.43 MiB) | `842,488` (0.80 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_buildings_01.wav` | VO.ARIA.Message.BuildDrawerReadyBuildings | 4.488 s | 1 | 44,100 Hz | DecompressOnLoad | `424,406` (0.40 MiB) | `791,684` (0.76 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_default_01.wav` | VO.ARIA.Message.BuildDrawerReadyDefault | 2.280 s | 1 | 44,100 Hz | DecompressOnLoad | `229,628` (0.22 MiB) | `402,192` (0.38 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_soldiers_01.wav` | VO.ARIA.Message.BuildDrawerReadySoldiers | 4.584 s | 1 | 44,100 Hz | DecompressOnLoad | `432,874` (0.41 MiB) | `808,620` (0.77 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_vehicles_01.wav` | VO.ARIA.Message.BuildDrawerReadyVehicles | 4.656 s | 1 | 44,100 Hz | DecompressOnLoad | `439,224` (0.42 MiB) | `821,320` (0.78 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_production_queued_01.wav` | VO.ARIA.Message.BuildDrawerSuccessProductionQueued | 3.024 s | 1 | 44,100 Hz | DecompressOnLoad | `295,282` (0.28 MiB) | `533,436` (0.51 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_recruitment_queued_01.wav` | VO.ARIA.Message.BuildDrawerSuccessRecruitmentQueued | 3.024 s | 1 | 44,100 Hz | DecompressOnLoad | `295,282` (0.28 MiB) | `533,436` (0.51 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_building_placed_01.wav` | VO.ARIA.Message.BuildFeedbackBuildingPlaced | 2.088 s | 1 | 44,100 Hz | DecompressOnLoad | `212,694` (0.20 MiB) | `368,324` (0.35 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_drawer_not_ready_01.wav` | VO.ARIA.Message.BuildFeedbackDrawerNotReady | 2.592 s | 1 | 44,100 Hz | DecompressOnLoad | `257,148` (0.25 MiB) | `457,232` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_no_active_placement_01.wav` | VO.ARIA.Message.BuildFeedbackNoActivePlacement | 2.904 s | 1 | 44,100 Hz | DecompressOnLoad | `284,666` (0.27 MiB) | `512,268` (0.49 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_building_01.wav` | VO.ARIA.Message.BuildFeedbackPlaceBuilding | 1.968 s | 1 | 44,100 Hz | DecompressOnLoad | `202,110` (0.19 MiB) | `347,156` (0.33 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_on_valid_ground_01.wav` | VO.ARIA.Message.BuildFeedbackPlaceOnValidGround | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_placement_cancelled_01.wav` | VO.ARIA.Message.BuildFeedbackPlacementCancelled | 2.304 s | 1 | 44,100 Hz | DecompressOnLoad | `231,746` (0.22 MiB) | `406,428` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancel_unavailable_01.wav` | VO.ARIA.Message.BuildFeedbackProductionCancelUnavailable | 2.928 s | 1 | 44,100 Hz | DecompressOnLoad | `286,782` (0.27 MiB) | `516,500` (0.49 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_01.wav` | VO.ARIA.Message.BuildFeedbackProductionCancelled | 2.328 s | 1 | 44,100 Hz | DecompressOnLoad | `233,862` (0.22 MiB) | `410,660` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_named_01.wav` | VO.ARIA.Message.BuildFeedbackProductionCancelledNamed | 2.184 s | 1 | 44,100 Hz | DecompressOnLoad | `221,162` (0.21 MiB) | `385,260` (0.37 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_clear_unavailable_01.wav` | VO.ARIA.Message.BuildFeedbackProductionClearUnavailable | 2.808 s | 1 | 44,100 Hz | DecompressOnLoad | `276,198` (0.26 MiB) | `495,332` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_01.wav` | VO.ARIA.Message.BuildFeedbackProductionQueueCleared | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_sentence_01.wav` | VO.ARIA.Message.BuildFeedbackProductionQueueClearedSentence | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_empty_01.wav` | VO.ARIA.Message.BuildFeedbackProductionQueueEmpty | 2.688 s | 1 | 44,100 Hz | DecompressOnLoad | `265,614` (0.25 MiB) | `474,164` (0.45 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_requested_01.wav` | VO.ARIA.Message.BuildFeedbackProductionRequested | 3.456 s | 1 | 44,100 Hz | DecompressOnLoad | `333,384` (0.32 MiB) | `609,640` (0.58 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_rotated_90_01.wav` | VO.ARIA.Message.BuildFeedbackRotated90 | 2.784 s | 1 | 44,100 Hz | DecompressOnLoad | `274,082` (0.26 MiB) | `491,100` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_instruction_confirm_01.wav` | VO.ARIA.Message.BuildPlacementInstructionConfirm | 3.408 s | 1 | 44,100 Hz | DecompressOnLoad | `329,150` (0.31 MiB) | `601,172` (0.57 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_drag_to_position_01.wav` | VO.ARIA.Message.BuildPlacementStatusDragToPosition | 2.208 s | 1 | 44,100 Hz | DecompressOnLoad | `223,278` (0.21 MiB) | `389,492` (0.37 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_valid_ground_01.wav` | VO.ARIA.Message.BuildPlacementStatusValidGround | 2.136 s | 1 | 44,100 Hz | DecompressOnLoad | `216,928` (0.21 MiB) | `376,792` (0.36 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_default_01.wav` | VO.ARIA.Message.BuildPlacementTitleDefault | 1.968 s | 1 | 44,100 Hz | DecompressOnLoad | `202,110` (0.19 MiB) | `347,156` (0.33 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_fallback_subject_01.wav` | VO.ARIA.Message.BuildPlacementTitleFallbackSubject | 1.656 s | 1 | 44,100 Hz | DecompressOnLoad | `174,592` (0.17 MiB) | `292,120` (0.28 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_named_01.wav` | VO.ARIA.Message.BuildPlacementTitleNamed | 2.064 s | 1 | 44,100 Hz | DecompressOnLoad | `210,578` (0.20 MiB) | `364,092` (0.35 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_confirm_destroy_01.wav` | VO.ARIA.Message.ConfirmDestroy | 3.216 s | 1 | 44,100 Hz | DecompressOnLoad | `312,216` (0.30 MiB) | `567,304` (0.54 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_create_first_01.wav` | VO.ARIA.Message.CreateFirst | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_drag_building_to_final_position_01.wav` | VO.ARIA.Message.DragBuildingToFinalPosition | 2.568 s | 1 | 44,100 Hz | DecompressOnLoad | `255,030` (0.24 MiB) | `452,996` (0.43 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_blocked_civilian_zone_01.wav` | VO.ARIA.Message.MatchFeedbackBlockedCivilianZone | 3.792 s | 1 | 44,100 Hz | DecompressOnLoad | `363,020` (0.35 MiB) | `668,912` (0.64 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_tactical_map_not_ready_01.wav` | VO.ARIA.Message.MatchFeedbackTacticalMapNotReady | 2.784 s | 1 | 44,100 Hz | DecompressOnLoad | `274,082` (0.26 MiB) | `491,100` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_not_enough_money_01.wav` | VO.ARIA.Message.NotEnoughMoney | 2.184 s | 1 | 44,100 Hz | DecompressOnLoad | `221,162` (0.21 MiB) | `385,260` (0.37 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_soldier_singular_01.wav` | VO.ARIA.Message.SelectionFeedbackSoldierSingular | 1.848 s | 1 | 44,100 Hz | DecompressOnLoad | `191,526` (0.18 MiB) | `325,988` (0.31 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_count_01.wav` | VO.ARIA.Message.SelectionFeedbackSquadCount | 2.160 s | 1 | 44,100 Hz | DecompressOnLoad | `219,044` (0.21 MiB) | `381,024` (0.36 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_selected_01.wav` | VO.ARIA.Message.SelectionFeedbackSquadSelected | 2.304 s | 1 | 44,100 Hz | DecompressOnLoad | `231,746` (0.22 MiB) | `406,428` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_plural_01.wav` | VO.ARIA.Message.SelectionFeedbackUnitPlural | 1.728 s | 1 | 44,100 Hz | DecompressOnLoad | `180,942` (0.17 MiB) | `304,820` (0.29 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_singular_01.wav` | VO.ARIA.Message.SelectionFeedbackUnitSingular | 1.680 s | 1 | 44,100 Hz | DecompressOnLoad | `176,708` (0.17 MiB) | `296,352` (0.28 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_vehicle_singular_01.wav` | VO.ARIA.Message.SelectionFeedbackVehicleSingular | 1.800 s | 1 | 44,100 Hz | DecompressOnLoad | `187,292` (0.18 MiB) | `317,520` (0.30 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_cargo_drop_blocked_01.wav` | VO.ARIA.Message.TacticalAirdropCargoDropBlocked | 2.376 s | 1 | 44,100 Hz | DecompressOnLoad | `238,096` (0.23 MiB) | `419,128` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_emergency_drop_visual_missing_01.wav` | VO.ARIA.Message.TacticalAirdropEmergencyDropVisualMissing | 3.072 s | 1 | 44,100 Hz | DecompressOnLoad | `299,516` (0.29 MiB) | `541,904` (0.52 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_no_clear_landing_zone_01.wav` | VO.ARIA.Message.TacticalAirdropNoClearLandingZone | 3.192 s | 1 | 44,100 Hz | DecompressOnLoad | `310,100` (0.30 MiB) | `563,072` (0.54 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_parachute_visual_missing_01.wav` | VO.ARIA.Message.TacticalAirdropParachuteVisualMissing | 2.616 s | 1 | 44,100 Hz | DecompressOnLoad | `259,264` (0.25 MiB) | `461,464` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedAttackDescription | 2.376 s | 1 | 44,100 Hz | DecompressOnLoad | `238,096` (0.23 MiB) | `419,128` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedAttackTitle | 2.016 s | 1 | 44,100 Hz | DecompressOnLoad | `206,344` (0.20 MiB) | `355,624` (0.34 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedBoardDescription | 2.304 s | 1 | 44,100 Hz | DecompressOnLoad | `231,746` (0.22 MiB) | `406,428` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedBoardTitle | 1.944 s | 1 | 44,100 Hz | DecompressOnLoad | `199,994` (0.19 MiB) | `342,924` (0.33 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedBuildDescription | 2.592 s | 1 | 44,100 Hz | DecompressOnLoad | `257,148` (0.25 MiB) | `457,232` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedBuildTitle | 1.896 s | 1 | 44,100 Hz | DecompressOnLoad | `195,760` (0.19 MiB) | `334,456` (0.32 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedDestroyDescription | 2.592 s | 1 | 44,100 Hz | DecompressOnLoad | `257,148` (0.25 MiB) | `457,232` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedDestroyTitle | 2.088 s | 1 | 44,100 Hz | DecompressOnLoad | `212,694` (0.20 MiB) | `368,324` (0.35 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedHoldDescription | 2.976 s | 1 | 44,100 Hz | DecompressOnLoad | `291,016` (0.28 MiB) | `524,968` (0.50 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedHoldTitle | 2.040 s | 1 | 44,100 Hz | DecompressOnLoad | `208,460` (0.20 MiB) | `359,856` (0.34 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedMoveDescription | 2.712 s | 1 | 44,100 Hz | DecompressOnLoad | `267,732` (0.26 MiB) | `478,400` (0.46 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedMoveTitle | 1.944 s | 1 | 44,100 Hz | DecompressOnLoad | `199,994` (0.19 MiB) | `342,924` (0.33 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedReturnDescription | 2.616 s | 1 | 44,100 Hz | DecompressOnLoad | `259,264` (0.25 MiB) | `461,464` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedReturnTitle | 2.112 s | 1 | 44,100 Hz | DecompressOnLoad | `214,812` (0.20 MiB) | `372,560` (0.36 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedScanDescription | 2.832 s | 1 | 44,100 Hz | DecompressOnLoad | `278,316` (0.27 MiB) | `499,568` (0.48 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedScanTitle | 2.064 s | 1 | 44,100 Hz | DecompressOnLoad | `210,578` (0.20 MiB) | `364,092` (0.35 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_description_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedStopDescription | 3.096 s | 1 | 44,100 Hz | DecompressOnLoad | `301,632` (0.29 MiB) | `546,136` (0.52 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_title_01.wav` | VO.ARIA.Message.TacticalBannerAcceptedStopTitle | 2.016 s | 1 | 44,100 Hz | DecompressOnLoad | `206,344` (0.20 MiB) | `355,624` (0.34 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_description_01.wav` | VO.ARIA.Message.TacticalBannerModeAttackDescription | 2.736 s | 1 | 44,100 Hz | DecompressOnLoad | `269,848` (0.26 MiB) | `482,632` (0.46 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_title_01.wav` | VO.ARIA.Message.TacticalBannerModeAttackTitle | 2.016 s | 1 | 44,100 Hz | DecompressOnLoad | `206,344` (0.20 MiB) | `355,624` (0.34 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_01.wav` | VO.ARIA.Message.TacticalBannerModeBoardDescription | 2.448 s | 1 | 44,100 Hz | DecompressOnLoad | `244,446` (0.23 MiB) | `431,828` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_transport_to_passenger_01.wav` | VO.ARIA.Message.TacticalBannerModeBoardDescriptionTransportToPassenger | 2.616 s | 1 | 44,100 Hz | DecompressOnLoad | `259,264` (0.25 MiB) | `461,464` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_title_01.wav` | VO.ARIA.Message.TacticalBannerModeBoardTitle | 1.944 s | 1 | 44,100 Hz | DecompressOnLoad | `199,994` (0.19 MiB) | `342,924` (0.33 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_description_01.wav` | VO.ARIA.Message.TacticalBannerModeBuildDescription | 3.024 s | 1 | 44,100 Hz | DecompressOnLoad | `295,282` (0.28 MiB) | `533,436` (0.51 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_title_01.wav` | VO.ARIA.Message.TacticalBannerModeBuildTitle | 1.896 s | 1 | 44,100 Hz | DecompressOnLoad | `195,760` (0.19 MiB) | `334,456` (0.32 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_description_01.wav` | VO.ARIA.Message.TacticalBannerModeMoveDescription | 2.472 s | 1 | 44,100 Hz | DecompressOnLoad | `246,564` (0.24 MiB) | `436,064` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_title_01.wav` | VO.ARIA.Message.TacticalBannerModeMoveTitle | 1.944 s | 1 | 44,100 Hz | DecompressOnLoad | `199,994` (0.19 MiB) | `342,924` (0.33 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_description_01.wav` | VO.ARIA.Message.TacticalBannerModeScanDescription | 2.784 s | 1 | 44,100 Hz | DecompressOnLoad | `274,082` (0.26 MiB) | `491,100` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_title_01.wav` | VO.ARIA.Message.TacticalBannerModeScanTitle | 2.064 s | 1 | 44,100 Hz | DecompressOnLoad | `210,578` (0.20 MiB) | `364,092` (0.35 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_passenger_to_transport_01.wav` | VO.ARIA.Message.TacticalCommandBoardPromptPassengerToTransport | 2.448 s | 1 | 44,100 Hz | DecompressOnLoad | `244,446` (0.23 MiB) | `431,828` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_transport_to_passenger_01.wav` | VO.ARIA.Message.TacticalCommandBoardPromptTransportToPassenger | 3.672 s | 1 | 44,100 Hz | DecompressOnLoad | `352,436` (0.34 MiB) | `647,744` (0.62 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_select_unit_first_01.wav` | VO.ARIA.Message.TacticalCommandBoardSelectUnitFirst | 2.520 s | 1 | 44,100 Hz | DecompressOnLoad | `250,796` (0.24 MiB) | `444,528` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_selected_unit_cannot_board_01.wav` | VO.ARIA.Message.TacticalCommandBoardSelectedUnitCannotBoard | 2.856 s | 1 | 44,100 Hz | DecompressOnLoad | `280,432` (0.27 MiB) | `503,800` (0.48 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_tap_units_to_board_01.wav` | VO.ARIA.Message.TacticalCommandBoardTapUnitsToBoard | 2.400 s | 1 | 44,100 Hz | DecompressOnLoad | `240,212` (0.23 MiB) | `423,360` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_unavailable_01.wav` | VO.ARIA.Message.TacticalCommandBoardUnavailable | 2.784 s | 1 | 44,100 Hz | DecompressOnLoad | `274,082` (0.26 MiB) | `491,100` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_attack_01.wav` | VO.ARIA.Message.TacticalCommandInstructionAttack | 2.424 s | 1 | 44,100 Hz | DecompressOnLoad | `242,330` (0.23 MiB) | `427,596` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_board_01.wav` | VO.ARIA.Message.TacticalCommandInstructionBoard | 2.448 s | 1 | 44,100 Hz | DecompressOnLoad | `244,446` (0.23 MiB) | `431,828` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_build_01.wav` | VO.ARIA.Message.TacticalCommandInstructionBuild | 3.768 s | 1 | 44,100 Hz | DecompressOnLoad | `360,902` (0.34 MiB) | `664,676` (0.63 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_hold_01.wav` | VO.ARIA.Message.TacticalCommandInstructionHold | 3.024 s | 1 | 44,100 Hz | DecompressOnLoad | `295,282` (0.28 MiB) | `533,436` (0.51 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_move_01.wav` | VO.ARIA.Message.TacticalCommandInstructionMove | 2.304 s | 1 | 44,100 Hz | DecompressOnLoad | `231,746` (0.22 MiB) | `406,428` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_scan_01.wav` | VO.ARIA.Message.TacticalCommandInstructionScan | 2.352 s | 1 | 44,100 Hz | DecompressOnLoad | `235,980` (0.23 MiB) | `414,896` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_select_01.wav` | VO.ARIA.Message.TacticalCommandInstructionSelect | 2.736 s | 1 | 44,100 Hz | DecompressOnLoad | `269,848` (0.26 MiB) | `482,632` (0.46 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_special_01.wav` | VO.ARIA.Message.TacticalCommandInstructionSpecial | 2.520 s | 1 | 44,100 Hz | DecompressOnLoad | `250,796` (0.24 MiB) | `444,528` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_stop_01.wav` | VO.ARIA.Message.TacticalCommandInstructionStop | 3.528 s | 1 | 44,100 Hz | DecompressOnLoad | `339,734` (0.32 MiB) | `622,340` (0.59 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_build_unavailable_01.wav` | VO.ARIA.Message.TacticalCommandReasonBuildUnavailable | 2.376 s | 1 | 44,100 Hz | DecompressOnLoad | `238,096` (0.23 MiB) | `419,128` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_camera_jump_unavailable_01.wav` | VO.ARIA.Message.TacticalCommandReasonCameraJumpUnavailable | 2.856 s | 1 | 44,100 Hz | DecompressOnLoad | `280,432` (0.27 MiB) | `503,800` (0.48 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_command_unavailable_01.wav` | VO.ARIA.Message.TacticalCommandReasonCommandUnavailable | 2.424 s | 1 | 44,100 Hz | DecompressOnLoad | `242,330` (0.23 MiB) | `427,596` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_fuel_01.wav` | VO.ARIA.Message.TacticalCommandReasonInsufficientFuel | 2.328 s | 1 | 44,100 Hz | DecompressOnLoad | `233,862` (0.22 MiB) | `410,660` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_resources_01.wav` | VO.ARIA.Message.TacticalCommandReasonInsufficientResources | 2.712 s | 1 | 44,100 Hz | DecompressOnLoad | `267,732` (0.26 MiB) | `478,400` (0.46 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_passenger_01.wav` | VO.ARIA.Message.TacticalCommandReasonInvalidPassenger | 3.360 s | 1 | 44,100 Hz | DecompressOnLoad | `324,916` (0.31 MiB) | `592,704` (0.57 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_transport_01.wav` | VO.ARIA.Message.TacticalCommandReasonInvalidTransport | 4.128 s | 1 | 44,100 Hz | DecompressOnLoad | `392,654` (0.37 MiB) | `728,180` (0.69 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_disembark_cell_01.wav` | VO.ARIA.Message.TacticalCommandReasonNoDisembarkCell | 3.528 s | 1 | 44,100 Hz | DecompressOnLoad | `339,734` (0.32 MiB) | `622,340` (0.59 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_eligible_passengers_01.wav` | VO.ARIA.Message.TacticalCommandReasonNoEligiblePassengers | 3.792 s | 1 | 44,100 Hz | DecompressOnLoad | `363,020` (0.35 MiB) | `668,912` (0.64 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_selection_01.wav` | VO.ARIA.Message.TacticalCommandReasonNoSelection | 3.240 s | 1 | 44,100 Hz | DecompressOnLoad | `314,332` (0.30 MiB) | `571,536` (0.55 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_cooldown_01.wav` | VO.ARIA.Message.TacticalCommandReasonScanCooldown | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_unavailable_01.wav` | VO.ARIA.Message.TacticalCommandReasonScanUnavailable | 2.448 s | 1 | 44,100 Hz | DecompressOnLoad | `244,446` (0.23 MiB) | `431,828` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_blocked_01.wav` | VO.ARIA.Message.TacticalCommandReasonTargetBlocked | 2.184 s | 1 | 44,100 Hz | DecompressOnLoad | `221,162` (0.21 MiB) | `385,260` (0.37 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_attackable_01.wav` | VO.ARIA.Message.TacticalCommandReasonTargetNotAttackable | 2.664 s | 1 | 44,100 Hz | DecompressOnLoad | `263,498` (0.25 MiB) | `469,932` (0.45 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_enemy_01.wav` | VO.ARIA.Message.TacticalCommandReasonTargetNotEnemy | 2.544 s | 1 | 44,100 Hz | DecompressOnLoad | `252,914` (0.24 MiB) | `448,764` (0.43 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_out_of_bounds_01.wav` | VO.ARIA.Message.TacticalCommandReasonTargetOutOfBounds | 3.312 s | 1 | 44,100 Hz | DecompressOnLoad | `320,684` (0.31 MiB) | `584,240` (0.56 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_unreachable_01.wav` | VO.ARIA.Message.TacticalCommandReasonTargetUnreachable | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_full_01.wav` | VO.ARIA.Message.TacticalCommandReasonTransportFull | 2.256 s | 1 | 44,100 Hz | DecompressOnLoad | `227,512` (0.22 MiB) | `397,960` (0.38 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_passenger_missing_01.wav` | VO.ARIA.Message.TacticalCommandReasonTransportPassengerMissing | 3.648 s | 1 | 44,100 Hz | DecompressOnLoad | `350,318` (0.33 MiB) | `643,508` (0.61 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_hold_no_selection_01.wav` | VO.ARIA.Message.TacticalCommandUnavailableHoldNoSelection | 3.312 s | 1 | 44,100 Hz | DecompressOnLoad | `320,684` (0.31 MiB) | `584,240` (0.56 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_scan_no_selection_01.wav` | VO.ARIA.Message.TacticalCommandUnavailableScanNoSelection | 3.624 s | 1 | 44,100 Hz | DecompressOnLoad | `348,202` (0.33 MiB) | `639,276` (0.61 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_stop_no_selection_01.wav` | VO.ARIA.Message.TacticalCommandUnavailableStopNoSelection | 3.408 s | 1 | 44,100 Hz | DecompressOnLoad | `329,150` (0.31 MiB) | `601,172` (0.57 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_air_defense_auto_engage_01.wav` | VO.ARIA.Message.TacticalFeedbackAirDefenseAutoEngage | 4.704 s | 1 | 44,100 Hz | DecompressOnLoad | `443,458` (0.42 MiB) | `829,788` (0.79 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_boarding_transport_01.wav` | VO.ARIA.Message.TacticalFeedbackBoardingTransport | 2.304 s | 1 | 44,100 Hz | DecompressOnLoad | `231,746` (0.22 MiB) | `406,428` (0.39 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_active_01.wav` | VO.ARIA.Message.TacticalFeedbackCameraFollowActive | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_unavailable_01.wav` | VO.ARIA.Message.TacticalFeedbackCameraFollowUnavailable | 2.784 s | 1 | 44,100 Hz | DecompressOnLoad | `274,082` (0.26 MiB) | `491,100` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_building_01.wav` | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedBuilding | 2.664 s | 1 | 44,100 Hz | DecompressOnLoad | `263,498` (0.25 MiB) | `469,932` (0.45 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_unit_01.wav` | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnit | 2.688 s | 1 | 44,100 Hz | DecompressOnLoad | `265,614` (0.25 MiB) | `474,164` (0.45 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_units_01.wav` | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnits | 3.168 s | 1 | 44,100 Hz | DecompressOnLoad | `307,982` (0.29 MiB) | `558,836` (0.53 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_passengers_01.wav` | VO.ARIA.Message.TacticalFeedbackExitingPassengers | 2.496 s | 1 | 44,100 Hz | DecompressOnLoad | `248,680` (0.24 MiB) | `440,296` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_unit_01.wav` | VO.ARIA.Message.TacticalFeedbackExitingUnit | 2.136 s | 1 | 44,100 Hz | DecompressOnLoad | `216,928` (0.21 MiB) | `376,792` (0.36 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_follow_target_lost_01.wav` | VO.ARIA.Message.TacticalFeedbackFollowTargetLost | 2.472 s | 1 | 44,100 Hz | DecompressOnLoad | `246,564` (0.24 MiB) | `436,064` (0.42 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_holding_current_position_01.wav` | VO.ARIA.Message.TacticalFeedbackHoldingCurrentPosition | 2.544 s | 1 | 44,100 Hz | DecompressOnLoad | `252,914` (0.24 MiB) | `448,764` (0.43 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_loading_transport_01.wav` | VO.ARIA.Message.TacticalFeedbackLoadingTransport | 2.376 s | 1 | 44,100 Hz | DecompressOnLoad | `238,096` (0.23 MiB) | `419,128` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_missile_launched_01.wav` | VO.ARIA.Message.TacticalFeedbackMissileLaunched | 2.088 s | 1 | 44,100 Hz | DecompressOnLoad | `212,694` (0.20 MiB) | `368,324` (0.35 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_rts_camera_restored_01.wav` | VO.ARIA.Message.TacticalFeedbackRtsCameraRestored | 2.736 s | 1 | 44,100 Hz | DecompressOnLoad | `269,848` (0.26 MiB) | `482,632` (0.46 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_complete_01.wav` | VO.ARIA.Message.TacticalFeedbackScanComplete | 3.816 s | 1 | 44,100 Hz | DecompressOnLoad | `365,136` (0.35 MiB) | `673,144` (0.64 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_contacts_01.wav` | VO.ARIA.Message.TacticalFeedbackScanContacts | 2.424 s | 1 | 44,100 Hz | DecompressOnLoad | `242,330` (0.23 MiB) | `427,596` (0.41 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_one_contact_01.wav` | VO.ARIA.Message.TacticalFeedbackScanOneContact | 2.208 s | 1 | 44,100 Hz | DecompressOnLoad | `223,278` (0.21 MiB) | `389,492` (0.37 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_ordered_01.wav` | VO.ARIA.Message.TacticalFeedbackScanOrdered | 4.440 s | 1 | 44,100 Hz | DecompressOnLoad | `420,172` (0.40 MiB) | `783,216` (0.75 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_stopped_selected_units_01.wav` | VO.ARIA.Message.TacticalFeedbackStoppedSelectedUnits | 2.688 s | 1 | 44,100 Hz | DecompressOnLoad | `265,614` (0.25 MiB) | `474,164` (0.45 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_unit_returning_to_base_01.wav` | VO.ARIA.Message.TacticalFeedbackUnitReturningToBase | 2.616 s | 1 | 44,100 Hz | DecompressOnLoad | `259,264` (0.25 MiB) | `461,464` (0.44 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_units_returning_to_base_01.wav` | VO.ARIA.Message.TacticalFeedbackUnitsReturningToBase | 3.120 s | 1 | 44,100 Hz | DecompressOnLoad | `303,748` (0.29 MiB) | `550,368` (0.52 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_warning_air_attack_type_01.wav` | VO.ARIA.Message.WarningAirAttackType | 2.352 s | 1 | 44,100 Hz | DecompressOnLoad | `235,980` (0.23 MiB) | `414,896` (0.40 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_count_suffix_01.wav` | VO.ARIA.Message.WarningAttackCountSuffix | 2.256 s | 1 | 44,100 Hz | DecompressOnLoad | `227,512` (0.22 MiB) | `397,960` (0.38 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_seconds_01.wav` | VO.ARIA.Message.WarningAttackEtaSeconds | 4.752 s | 1 | 44,100 Hz | DecompressOnLoad | `447,692` (0.43 MiB) | `838,256` (0.80 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_suffix_01.wav` | VO.ARIA.Message.WarningAttackEtaSuffix | 2.784 s | 1 | 44,100 Hz | DecompressOnLoad | `274,082` (0.26 MiB) | `491,100` (0.47 MiB) |
| Voice | Voice | `Assets/Game/Audio/Voice/ARIA/aria_message_warning_ground_attack_type_01.wav` | VO.ARIA.Message.WarningGroundAttackType | 3.024 s | 1 | 44,100 Hz | DecompressOnLoad | `295,282` (0.28 MiB) | `533,436` (0.51 MiB) |

## Animation Texture Payload

| Asset | Dimensions | Format | Payload | Imported | Dependency roots |
|---|---:|---|---:|---:|---|
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset` | 2048 x 1024 | R16G16B16A16_SFloat | `16,777,216` (16.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset` | 2048 x 1024 | R16G16B16A16_SFloat | `16,777,216` (16.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |
| `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset` | 2048 x 1024 | R16G16B16A16_SFloat | `16,777,216` (16.00 MiB) | `33,555,440` (32.00 MiB) | BuildScene: `Assets/Game/Scenes/Match.unity`<br>BuildScene: `Assets/Game/Scenes/Menu.unity` |

## Measurement Boundaries

- Build inclusion is a deterministic dependency-root inventory, not a BuildReport. Exact APK/AAB contribution remains APH-500 work.
- Imported size is reported only for loaded Texture, AudioClip, and Mesh objects through Profiler.GetRuntimeMemorySizeLong; unsupported assets remain JSON null.
- Dependency inclusion does not prove simultaneous runtime residency or unload lifetime.
- Source size is the project/package file length. Native built-in resources without a project path are excluded.
- Catalog audio compressed size is Unity's imported storage-memory measurement from AudioUtil.GetSoundSize, not source WAV size or final APK/AAB contribution.
- Catalog audio decoded size is estimated as sample frames x channels x 4-byte PCM float samples; it excludes engine/object overhead and does not claim simultaneous residency.
