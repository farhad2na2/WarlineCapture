# First-Launch Art Source Audit

Date: 2026-07-10

Status: Sufficient to begin style and continuity generation; exact production M01 scene is absent

## Character Anchors

| Role | Config | Unity render reference | Existing realistic reference |
|---|---|---|---|
| Major Dalia Rahim | `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset` | `Assets/Game/Art/UI/Portraits/Generated/References/Characters/PortraitReference_Unit_Chr_Soldier_Female_02_Alt_02_ChromaGreen.png` | `Assets/Game/Art/UI/Portraits/Generated/Portrait_Unit_Chr_Soldier_Female_02_Alt_02_AI_RealisticSMG_ChromaGreen.png` |
| Engineer Samira Haddad | `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset` | `Assets/Game/Art/UI/Portraits/Generated/References/Characters/PortraitReference_Unit_Chr_Civilian_Female_01_ChromaGreen.png` | `Assets/Game/Art/UI/Portraits/Generated/Portrait_Unit_Chr_Civilian_Female_01_AI_RealisticCivilian_ChromaGreen.png` |
| Optional Commander battlefield proxy | `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Leader_Male_01_Config.asset` | `Assets/Game/Art/UI/Portraits/Generated/References/Characters/PortraitReference_Unit_Chr_Leader_Male_01_ChromaGreen.png` | `Assets/Game/Art/UI/Portraits/Generated/Portrait_Unit_Chr_Leader_Male_01_AI_RealisticMetalPistol_ChromaGreen.png` |

Strict multi-angle geometry references:

- Dalia: `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_02.png`. She has a headset/ear protection, sunglasses, tied-back dark hair, and no helmet.
- Samira: `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_01.png`. She has a mustard hijab, dark jacket, deep-blue trousers, muted green belt, and no weapon or military equipment.
- Existing realistic portraits are secondary only and may not override Unity model geometry, equipment, clothing, or headwear.

The Commander proxy is not a fixed player face. Pre-identity panels use hands, silhouette, back view, or first-person framing. The identity surface requires six free portraits plus a neutral fallback.

## Faction And Civilian Anchors

First-contact JRC squad:

- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_02_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_01_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_01_Config.asset`

First-contact Ash Line patrol:

- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset`

`Prefab_UnitGrid_Chr_Insurgent_Male_05_Config.asset` is reserved exclusively for Nadir Qassem and may not appear as an anonymous first-contact patrol member.

The M01 first-contact patrol is deliberately limited to the Male 03 courier/raider, Female 01 rifle-cell commander, and Female 02 sidearm/logistics operative. The Male 02 heavy gunner is reserved for later escalation.

Opening civilians/responders:

- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset` for Samira
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_02_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Male_01_Config.asset`
- `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Male_02_Config.asset`

- Hostility must read through carried weapons, formation, conduct, route, and tactical context. No ethnicity, clothing family, gender, or neighborhood is a hostility signal.

## Environment And Product Anchors

| Purpose | Reference |
|---|---|
| M01 world detail and tactical scale | `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_AI_Source.png` |
| Current gameplay visual-target board | `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png` |
| ARIA command-system visual language | `Design/VisualLockLayered/POP-13_ARIACommandAssistant/reference/POP-13_ARIACommandAssistant_TargetLock_V01.png` |
| Command-table/headquarters material quality | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` |
| Brand/loading material direction | `Design/VisualLockLayered/SCN-01_SplashLoading/reference/SCN-01_SplashLoading_NewMainMenuArtDirection_TargetLock_V04.png` |

## M01 Handoff Finding

`Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset` exists, but its referenced ground sprite GUID does not resolve to a repository asset. The production `opmap.ch01.district_edge_01` 3D scene and exact handoff camera required by the design documents do not exist.

Art-first resolution:

- Use the existing M01 visual target for material density, tactical scale, and gameplay readability.
- Design Old Market as a lived civilian edge district, not the generic ruined block shown in the old target.
- Treat the approved `FL-P18` final panel as the binding camera, landmark, road, lighting, and composition contract for later M01 3D implementation.
- Do not claim pixel/geometry continuity with a missing scene.

## Narrative Interpretation

The high-level Bible/catalog originally defer full proof of the revoked ARIA credential until Chapter 1 M05. The newer first-player experience and M01 production dependency require an early credential clue after M01.

Art resolution: `FL-P20` shows only a fragmentary revoked-credential trace embedded in unusually precise patrol orders. M05 remains the complete, authenticated proof and Protocol Fragment reveal.

## Generation Readiness

- Ready: living Sahrin, crisis, ARIA terminal, Dalia, Samira, JRC, Ash Line, civilian, and command-base direction work.
- Ready with art-first authority: Old Market geography and handoff composition.
- Not reusable as final text: any generated interface labels, regional writing, insignia, flags, or logos.
