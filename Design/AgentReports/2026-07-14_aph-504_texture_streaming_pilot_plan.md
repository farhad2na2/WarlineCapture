# APH-504 Texture Streaming Pilot Candidate Plan

- Evidence date: `2026-07-15`
- Status: `candidate-plan-valid-rollout-blocked`
- Analyzed revision: `cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- Selector valid: `true`
- Pilot ready for importer mutation: `false`
- Importer mutation authorized: `false`
- Pilot expansion authorized: `false`
- Unity and Android runs: `none`

## Decision

The read-only selector derives up to 2 candidates from the current tracked TextureImporter inventory intersected with positive historical Android BuildReport rows. No asset path or APH-502 revision is embedded in the selector. Importer mutation is authorized only when every precondition below is true; expansion remains a separate rejected decision.

## Current Repository Evidence

- Tracked TextureImporter count: `3536`
- Importer inventory SHA-256: `5a6585672870e2787cefeea309de73448761dcedba6dc21bd4f240b24ae1e1a2`
- Historical BuildReport candidate intersection: `93`
- Manifest packages: `47`
- Locked packages: `68`
- Package inventory SHA-256: `e83851e7f6938399d76382bc4b79bb9f103b62517e1c36e4acdc920e5ac3b48c`
- Strict importer parse errors: `2384`

## Proposed Candidate Set

| Texture | Decision | Category | Historical AAB bytes | Reasons |
|---|---|---|---:|---|
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png` | excluded | UI | 6291540 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait.png` | excluded | UI | 6292056 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame.png` | excluded | UI | 5392620 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_Large.png` | excluded | UI | 5392636 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_disabled.png` | excluded | UI | 5392636 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_hover.png` | excluded | UI | 5392636 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_pressed.png` | excluded | UI | 5392636 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait_frame_selected.png` | excluded | UI | 5392636 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame.png` | excluded | UI | 3324024 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_disabled.png` | excluded | UI | 3324040 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_hover.png` | excluded | UI | 3324032 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_pressed.png` | excluded | UI | 3324040 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_button_frame_selected.png` | excluded | UI | 3324040 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_campaign_valley.png` | excluded | UI | 6292064 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_operations_radar.png` | excluded | UI | 6292064 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_art_skirmish_airbase.png` | excluded | UI | 6292064 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_amber.png` | excluded | UI | 5770036 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_blue.png` | excluded | UI | 5391924 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_selected.png` | excluded | UI | 5800984 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_amber.png` | excluded | UI | 5772760 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_blue.png` | excluded | UI | 5392628 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png` | excluded | UI | 5803944 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_amber.png` | excluded | UI | 3698604 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_blue.png` | excluded | UI | 3596808 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_selected.png` | excluded | UI | 3620132 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_operations_star_icon.png` | excluded | UI | 3293096 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_skirmish_crossed_weapons_icon.png` | excluded | UI | 4164172 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_backing_default.png` | excluded | UI | 3947060 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_default.png` | excluded | UI | 3947052 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_selected.png` | excluded | UI | 4519516 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_campaign_target_icon.png` | excluded | UI | 3275800 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_command_shield_icon.png` | excluded | UI | 3245852 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_settings_gear_icon.png` | excluded | UI | 3360216 | current-semantic-category-not-world |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_objectives_panel_frame.png` | excluded | UI | 4245052 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_rect_button_selected_frame.png` | excluded | UI | 3392364 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_selected_panel_frame.png` | excluded | UI | 4574844 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_frame.png` | excluded | UI | 4580084 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_selected_frame.png` | excluded | UI | 4579892 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_rifle_portrait.png` | excluded | UI | 4719236 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_button_frame.png` | excluded | UI | 3201756 | current-semantic-category-not-world, mipmaps-not-enabled:0 |
| `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Desert_Albedo.png` | excluded | world albedo | 4194448 | pilot-cap-reached:2 |
| `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Green_Albedo.png` | excluded | world albedo | 4194448 | pilot-cap-reached:2 |
| `Assets/Game/Rendering/Textures/Stylized/T_GroundStylized_Soft_Normal.png` | excluded | world normal/mask | 5592544 | pilot-cap-reached:2 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Bombsuit_Male_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_01.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Female_02.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Male_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Civilian_Male_02.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Female_01.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Male_01.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Contractor_Male_02.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Ghillie_Male_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Female_01.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Female_02.png` | excluded | impostor/atlas | 11184968 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_02.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_03.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_04.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Insurgent_Male_05.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Leader_Male_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Pilot_Female_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Pilot_Male_01.png` | excluded | impostor/atlas | 11184960 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01_Alt_01.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_01_Alt_02.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_01.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Female_02_Alt_02.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01_Alt_01.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_01_Alt_02.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02.png` | excluded | impostor/atlas | 11184964 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_01.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_02.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_03.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02_Alt_04.png` | excluded | impostor/atlas | 11184972 | asset-or-meta-changed-since-historical-aab, current-semantic-category-not-world, ignore-mipmap-limit-not-disabled:1, streaming-baseline-not-disabled:1 |
| `Assets/PolygonMilitary/Textures/Air_Vehicle_Burnt.png` | excluded | world albedo | 5592564 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` | proposed | world albedo | 22369788 | pilot-rank:1, texture-family-representative:numbered-texture:01, world-albedo, current-tracked-importer-inventory, historical-aab-positive-inclusion, asset-and-meta-unchanged-since-historical-aab, mipmaps-enabled, explicit-streaming-baseline-disabled |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` | excluded | world normal/mask | 9961684 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` | excluded | world albedo | 9961676 | texture-family-quota-filled:numbered-texture:01 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` | excluded | world albedo | 9961676 | texture-family-quota-filled:numbered-texture:01 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` | proposed | world albedo | 9961676 | pilot-rank:2, texture-family-representative:numbered-texture:02, world-albedo, current-tracked-importer-inventory, historical-aab-positive-inclusion, asset-and-meta-unchanged-since-historical-aab, mipmaps-enabled, explicit-streaming-baseline-disabled |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` | excluded | world albedo | 9961676 | pilot-cap-reached:2 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` | excluded | world albedo | 9961676 | pilot-cap-reached:2 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/Signs 1.png` | excluded | world albedo | 5592552 | default-max-size-below-source:2048<4096, ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/SkyBox.png` | excluded | world albedo | 16777336 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/Vehicles/Veh_Heli_01_B.png` | excluded | world albedo | 5592560 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |

## Evidence Disposition

- Historical AAB revision: `a527e151e9e43a491ba30f4c19a0320dc54faf5c`; dirty=`false`; exported assets=`100/6104`.
- Historical positive rows prove prior inclusion only; they do not prove current-revision inclusion or absence.
- Scoped tracked inputs clean: `false`.
- Control-input hashes unchanged during collection: `true`.
- Current complete texture BuildReport accepted: `false`; errors=`complete-texture-export-marker-not-true, complete-texture-export-not-array, revision-mismatch:a527e151e9e43a491ba30f4c19a0320dc54faf5c->cbf6fd48846b40dd086faa0feb364fce0462a1bf, selected-texture-absent-from-complete-build-export:Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png, selected-texture-absent-from-complete-build-export:Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png`.
- Current content-residency inventory accepted: `false`; errors=`revision-mismatch:7084805d771142706f340e9f2e52a68570bcb72b->cbf6fd48846b40dd086faa0feb364fce0462a1bf`.
- APH-505 visual evidence accepted: `false`; path=`Design/AgentReports/architecture_performance_texture_streaming_visual_evidence.json`; errors=`aph505-evidence-unavailable`.
- APH-506 performance evidence accepted: `false`; path=`Design/AgentReports/architecture_performance_texture_streaming_performance_evidence.json`; errors=`aph506-evidence-unavailable`.

## Mobile Configuration

- Streaming active: `1`
- Add all cameras: `1`
- Streaming memory budget: `256 MiB`
- Global texture mip limit: `1`
- Maximum streaming level reduction: `2`
- Maximum file I/O requests: `1024`

The 256 MiB value is an observed bounded configuration, not an accepted product budget. The global mip limit of 1 prevents full source mip preservation for nearby views while the proposed importers keep `ignoreMipmapLimit: 0`.

## Unresolved Evidence

- `aph505-evidence-unavailable`
- `aph506-evidence-unavailable`
- `complete-texture-export-marker-not-true`
- `complete-texture-export-not-array`
- `historical-aab-revision-mismatch:a527e151e9e43a491ba30f4c19a0320dc54faf5c->cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- `historical-aab-top-table-incomplete:100/6104`
- `precondition-failed:aph502FinalBucketsAccepted`
- `precondition-failed:aph505VisualEvidenceAccepted`
- `precondition-failed:aph506PerformanceEvidenceAccepted`
- `precondition-failed:currentRevisionCompleteTextureBuildEvidence`
- `precondition-failed:currentRevisionContentResidencyEvidence`
- `precondition-failed:fullResolutionNearbyTexturesPreserved`
- `precondition-failed:scopedTrackedInputsClean`
- `precondition-failed:trackedWorktreeClean`
- `revision-mismatch:7084805d771142706f340e9f2e52a68570bcb72b->cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- `revision-mismatch:a527e151e9e43a491ba30f4c19a0320dc54faf5c->cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- `selected-texture-absent-from-complete-build-export:Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png`
- `selected-texture-absent-from-complete-build-export:Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png`
- `tracked-worktree-dirty`

## Mutation Preconditions

- `selectorValid`: `true`
- `trackedWorktreeClean`: `false`
- `scopedTrackedInputsClean`: `false`
- `controlInputsStable`: `true`
- `currentRevisionCompleteTextureBuildEvidence`: `false`
- `currentRevisionContentResidencyEvidence`: `false`
- `aph502FinalBucketsAccepted`: `false`
- `mobileStreamingConfigurationValid`: `true`
- `fullResolutionNearbyTexturesPreserved`: `false`
- `aph505VisualEvidenceAccepted`: `false`
- `aph506PerformanceEvidenceAccepted`: `false`
- `textureImporterInventoryStable`: `true`
- `packageInventoryStable`: `true`

## Acceptance Boundary

The selector contract is accepted when candidate discovery is deterministic and read-only. Importer mutation remains fail-closed until a clean same-revision complete texture BuildReport and residency inventory accept APH-502, the Mobile tier preserves full-resolution nearby mips, APH-505 supplies accepted near/medium/far before-and-after visual pairs, and APH-506 supplies an accepted 600-second memory and I/O measurement for the exact candidate paths and revision. The APH-505 and APH-506 JSON contracts are validated at the evidence paths listed above. No APH-504 report can authorize expansion.

## Reproduction

```sh
PYTHONPYCACHEPREFIX=/tmp/aph504-pyc python3 -m unittest \
  Tools.CI.tests.test_aph504_texture_streaming_pilot_selector -v
PYTHONPYCACHEPREFIX=/tmp/aph504-pyc python3 \
  Tools/CI/aph504_texture_streaming_pilot_selector.py --check
```
