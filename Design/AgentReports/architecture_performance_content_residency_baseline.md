# Architecture Performance Content Residency Baseline

- Task: `APH-008`
- Status: `complete`
- Baseline commit: `7084805d771142706f340e9f2e52a68570bcb72b`
- Generated UTC: `2026-07-09T21:12:28.6355540Z`
- Unity: `6000.5.2f1`
- Active build target: `Android`
- Scope: Enabled build scenes, Assets Resources content, PlayerSettings preloaded assets, and StreamingAssets, including transitive AssetDatabase dependencies.

## Summary

| Metric | Value |
|---|---:|
| Dependency roots | 12 |
| Included asset paths | 4,099 |
| Assets with source size | 4,099 |
| Known source bytes | `815,664,568` (777.88 MiB) |
| Assets with measured imported size | 2,680 |
| Known imported bytes | `2,471,396,094` (2,356.91 MiB) |
| Audio assets | 226 |
| Texture assets | 637 |
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

### Audio Load Types

| State | Asset count |
|---|---:|
| DecompressOnLoad | 217 |
| Streaming | 9 |

### Texture Mipmap and Streaming

| State | Asset count |
|---|---:|
| mipmaps=false, streaming=false | 506 |
| mipmaps=true, streaming=false | 131 |

### Mesh Read/Write

| State | Asset count |
|---|---:|
| disabled | 916 |
| enabled | 901 |

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
