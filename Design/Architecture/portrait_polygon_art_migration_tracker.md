# Polygon Portrait Art Migration Tracker

Date: 2026-07-11

Status: Runtime migration complete. All 238 Polygon portraits are generated, imported, assigned, and technically validated; a final in-game visual spot-check remains.

## Goal

Replace the legacy realistic portrait library with portraits that use the approved FirstLaunch Direction B Match-Aligned POLYGON art direction while preserving exact gameplay identity, Unity sprite references, runtime behavior, and mobile performance.

This tracker covers:

- 74 entity definitions: 33 characters, 18 vehicles/aircraft, and 23 buildings.
- Three portrait roles per entity: Primary, Card, and Action.
- 11 Match HUD manual-selection fallback portraits.
- 5 Match HUD squad-tray portraits.
- 238 final target images when all missing variants are completed.

## Authority

- Style lock: `Design/NarrativeVision/FirstLaunch/ArtReview/StyleCandidates/DirectionB_MatchAligned/PRODUCTION_STYLE_LOCK.md`
- Approved reference summary: `Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt/Evidence/FINAL_ART_REFERENCE_SUMMARY.png`
- Character portrait references:
  - `Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_samira.png`
  - `Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_dalia.png`
- Exact gameplay identity references: `Assets/Game/Art/UI/Portraits/Generated/References/`
- Existing primary manifest: `Design/Architecture/portrait_sprite_generation_manifest.md`
- Existing Card/Action manifest: `Design/Architecture/portrait_card_action_generation_manifest.md`
- Runtime selection rules: `Design/Architecture/match_hud_selection_summary_panel_plan.md`

## Status Key

- `[ ]` Not started.
- `[~]` Generated candidate; review pending.
- `[x]` Approved, imported, assigned, and validated.
- `[!]` Blocked or requires correction.
- `N/A` Deliberately not applicable.

## Progress Summary

| Workstream | Total | Generated | Approved | Runtime replaced | Validated |
|---|---:|---:|---:|---:|---:|
| Entity Primary | 74 | 74 | 74 | 74 | 74 |
| Entity Card | 74 | 74 | 74 | 74 | 74 |
| Entity Action | 74 | 74 | 74 | 74 | 74 |
| Multi-selection fallbacks | 11 | 11 | 11 | 11 | 11 |
| Squad-tray portraits | 5 | 5 | 5 | 5 | 5 |
| **Total** | **238** | **238** | **238** | **238** | **238** |

Pre-migration legacy coverage discovered by audit:

- 72 assigned Primary sprites.
- 72 assigned Card sprites.
- 64 assigned Action sprites.
- Hall and Helipad do not have a complete three-role portrait set.
- Eight active buildings do not have Action sprites.
- Five purpose-built mixed/category summary images exist; six fallback slots reuse unrelated legacy icons or TargetLock art.

## Non-Negotiable Style Contract

- Preserve visibly faceted low-poly geometry and simplified POLYGON proportions.
- Preserve the prefab silhouette, colors, weapon category, attachments, rotor count, wing planform, launcher geometry, and building shape.
- Use flat color-block materials with minimal texture detail.
- Use the Match palette: pale sand, tan canvas, olive, muted green, charcoal, gray concrete, and restrained cyan/gold UI-compatible accents.
- Enrichment comes from composition, lighting, shadows, set dressing, and low-poly atmosphere, not photoreal surface rendering.
- No real flags, national markings, readable writing, insignia, logos, watermarks, gore, religious landmarks, or current-conflict references.
- No invented weapon, vehicle attachment, building annex, aircraft payload, or faction marking.
- Do not use a legacy realistic portrait as identity authority unless a documented source-prefab defect makes the generated prefab reference structurally misleading. In that case, use the legacy portrait only to recover the approved clean silhouette, record the exception, and validate every required part explicitly.
- `Unit_Veh_Jet_01` is one such documented exception. Its live and package prefabs contain shared `Jet_02` flap/tail renderer parts, so the chroma reference exposes a stacked multi-wing silhouette. New portraits must use a clean twin-tail strike-jet anatomy: one left/right main wing, one left/right horizontal tailplane, and exactly two vertical fins.
- `Building_Hall` is a source-identity exception to the generic no-religious-landmark generation rule. The exact live prefab and approved chroma reference are themselves the pink domed four-tower structure. Preserve that source identity; do not invent additional symbols or redesign it into an unrelated civic building.

## Output Contracts

### Primary

- Final size: `512x512` PNG with alpha.
- Transparent background and transparent corners.
- Character: readable bust or three-quarter figure; weapon category visible when configured.
- Vehicle/aircraft/building: complete silhouette at a consistent three-quarter gameplay-compatible angle.
- No floor, cast shadow, contact shadow, environment, text, or frame.
- Generate on flat chroma key, remove key locally, validate alpha and edge despill.

### Card

- Final size: `512x512` opaque RGB PNG.
- Calm identity/readiness composition.
- Match-aligned low-poly environment appropriate to the entity.
- Subject remains dominant and readable at small HUD sizes.
- No text or baked UI frame.

### Action

- Final size: `512x512` opaque RGB PNG.
- Distinct action from the Card pose/composition.
- Maintain pose diversity across the complete portrait set. Do not default repeatedly to kneeling behind a parapet and firing.
- Rotate role-appropriate staging such as advancing, street crossing, doorway entry, reloading, signaling, sprinting, dismounting, standing fire, prone observation, casualty aid, equipment operation, or withdrawal.
- Before accepting an Action candidate, compare it with the preceding completed Action portraits and reject materially repetitive pose/camera/background combinations.
- Character action must match configured weapon and role.
- Vehicle/aircraft action must retain the entire critical silhouette.
- Building action depicts operational activity, alert state, production, defense, or environmental response without changing the building identity.
- No projectile/UI overlays baked into the portrait unless the projectile is a natural scene element and does not obscure identity.

### Group Portraits

- Final size: `512x512` opaque RGB PNG.
- Clear category composition at thumbnail size.
- Use the same low-poly Match world and material vocabulary.
- No text, numerical counts, flags, insignia, or UI frame.
- Mixed portraits must show every named category clearly; do not imply a category through a background-only silhouette.

## GUID And Replacement Policy

- Approval candidates live outside `Assets/` and cannot replace runtime art before approval.
- After approval, replace image bytes at the existing runtime path wherever a valid assigned sprite already exists.
- Preserve every existing `.meta` file and GUID.
- Never bulk-delete or recreate `.meta` files.
- Add new files/GUIDs only for currently missing roles, Hall, Helipad, or a category slot that has no dedicated portrait asset.
- Re-open every config after import and verify Primary/Card/Action GUIDs.
- Verify prefab overrides still resolve to the config sprites.
- Git history is the archive for legacy realistic pixels; do not duplicate the full legacy library elsewhere in the runtime project.

## Approval Batch 01

Review output root:

`Design/VisualLockLayered/PortraitPolygonMigration/ApprovalBatch01/`

No file in this folder is a runtime source until the user approves it.

| ID | Candidate | Role | Identity source | State | User decision |
|---|---|---|---|---|---|
| `PPA-01` | Heavy Gunner Primary | Primary/alpha | `Unit_Chr_Soldier_Male_01.prefab` render | `[x]` | Approved |
| `PPA-02` | Heavy Gunner Card | Card | Same prefab render | `[x]` | Approved |
| `PPA-03` | Heavy Gunner Action | Action | Same prefab render | `[x]` | Approved |
| `PPA-04` | Strike Jet Primary | Primary/alpha | Clean legacy twin-tail silhouette + prefab audit + style lock | `[x]` | R4 approved |
| `PPA-05` | Strike Jet Card | Card | Clean legacy twin-tail silhouette + explicit ground-state contract | `[x]` | R4 approved |
| `PPA-06` | Strike Jet Action | Action | Clean legacy twin-tail silhouette + explicit climb-state contract | `[x]` | R4 approved |
| `PPA-07` | Barracks Primary | Primary/alpha | `PortraitReference_Building_Barrack_ChromaGreen.png` | `[x]` | Approved |
| `PPA-08` | Barracks Card | Card | Same prefab reference | `[x]` | Approved |
| `PPA-09` | Barracks Action | Action | Same prefab reference | `[x]` | Approved |
| `PPA-10` | Rifle Squad Group | Group/manual selection | Character prefab references | `[x]` | Approved |
| `PPA-11` | Mixed Force Group | Group/mixed selection | Character, vehicle, and aircraft references | `[x]` | Approved |
| `PPA-12` | Transport Group | Group/squad tray + transport fallback style | Transport vehicle/aircraft references | `[x]` | Approved |

Approval gate:

- [x] All 12 candidates generated.
- [x] Contact sheet created in fixed row order.
- [x] Transparent candidates have validated alpha previews and transparent corners.
- [x] Each candidate inspected at full resolution, `128x128`, and `64x64`.
- [x] User approves the batch or requests targeted revisions.
- [x] Approved prompt/style deltas are recorded before bulk generation.
- [x] No runtime sprite replaced before this gate passes.

## Entity Inventory

Each row represents three final images. Existing runtime GUID/path mappings remain authoritative in the config YAML and existing `.meta` files. `P`, `C`, and `A` track the new Polygon Primary, Card, and Action replacements.

### Characters

| Config | Display role | Source reference | P | C | A |
|---|---|---|---|---|---|
| `Prefab_UnitGrid_Chr_Bombsuit_Male_01_Config.asset` | Bomb Suit Specialist | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset` | Civilian Female I | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Civilian_Female_02_Config.asset` | Civilian Female II | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Civilian_Male_01_Config.asset` | Civilian Male I | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Civilian_Male_02_Config.asset` | Civilian Male II | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Contractor_Female_01_Config.asset` | Contractor Female / compact rifle | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Contractor_Male_01_Config.asset` | Contractor Male / pistol | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Contractor_Male_02_Config.asset` | Contractor Male / rifle | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset` | Ghillie Rocketeer | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset` | Female Rifle Fighter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset` | Female Pistol Fighter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Male_01_Config.asset` | Male RPG Fighter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Male_02_Config.asset` | Male Machine Gunner | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset` | Male Compact Rifle Fighter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Male_04_Config.asset` | Male Marksman | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Insurgent_Male_05_Config.asset` | Male Rifle Fighter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Leader_Male_01_Config.asset` | Field Commander | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Pilot_Female_01_Config.asset` | Pilot Female | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Pilot_Male_01_Config.asset` | Pilot Male | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_01_Config.asset` | Female Rifleman | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_02_Config.asset` | Female Marksman II | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Female_01_Config.asset` | Female Marksman I | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_01_Config.asset` | Female Rifleman Alt | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset` | Female Breacher | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Female_02_Config.asset` | Female Rifleman II | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_01_Config.asset` | Male Marksman I | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_02_Config.asset` | Advanced Rifleman | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset` | Heavy Gunner | Approved batch-01 corrected render | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_01_Config.asset` | Male Pistol Specialist | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_02_Config.asset` | Male Rifleman Alt II | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_03_Config.asset` | Male Pistol Specialist III | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset` | Male Rifleman IV | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Config.asset` | Male Rifleman II | Existing prefab reference | `[x]` | `[x]` | `[x]` |

### Vehicles And Aircraft

| Config | Display role | Source reference | P | C | A |
|---|---|---|---|---|---|
| `Prefab_UnitGrid_Veh_APC_Fast_Config.asset` | Fast APC | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_APC_Heavy_Config.asset` | Heavy APC | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_APC_Slow_Config.asset` | Armored APC | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Drone_Config.asset` | Recon Drone | Manual clean-airframe contract; all prefab/runtime portraits rejected for duplicated child meshes and incorrect upright tail | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Helicopter_Attack_Config.asset` | Attack Helicopter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Helicopter_Attack_Small_Config.asset` | Light Attack Helicopter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset` | Transport Helicopter | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Jet_01_Config.asset` | Strike Jet | Approved clean twin-tail exception | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Jet_02_Config.asset` | Fighter Jet | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset` | Light Armored Car | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset` | Air Missile Launcher | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset` | Ground Missile Launcher | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Plane_Transport_Config.asset` | Transport Plane | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Radar_Tank.asset` | Radar Tank | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Tank_USA_Config.asset` | Battle Tank | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Truck_Canopy.asset` | Canopy Truck | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Truck_Tanker.asset` | Tanker Truck | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_UnitGrid_Veh_Truck_Tray.asset` | Cargo Truck | Existing prefab reference | `[x]` | `[x]` | `[x]` |

### Buildings

| Config | Display role | Source reference | P | C | A |
|---|---|---|---|---|---|
| `Prefab_BuildingDefinition_Airport_Config.asset` | Airport | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Ammunition_Depot_Config.asset` | Ammunition Depot | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Building_Barrack_Config.asset` | Barracks | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Building_Satelite_Dish_Config.asset` | Satellite Dish | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Fuel_Bladder_Config.asset` | Fuel Bladder | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_GuardTower_Big_Config.asset` | Heavy Guard Tower | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_GuardTower_Config.asset` | Guard Tower | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Hall_Config.asset` | City Hall | Existing reference; all runtime roles missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Helipad_Config.asset` | Helipad | Exact binary-FBX mesh/UV reference extract; all runtime roles missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_House_Config.asset` | House | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_OilPump_Config.asset` | Oil Pump | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_OilRefinery_Big_Config.asset` | Large Oil Refinery | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_OilRefinery_Config.asset` | Oil Refinery | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Portaloo__Config.asset` | Portable Toilet | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Road_Barrier_Config.asset` | Road Barrier | Existing prefab reference | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Shop_Config.asset` | Shop | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Tent_Contractor_Config.asset` | Contractor Tent | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Tent_Expert_Config.asset` | Expert Tent | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Tent_Refugee_Config.asset` | Refugee Tent | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Tent_Regular_Config.asset` | Soldier Tent | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Wall_Dirt_Straight_Config.asset` | Dirt Wall | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_Wall_Fence_Straight_Config.asset` | Fence Wall | Action currently missing | `[x]` | `[x]` | `[x]` |
| `Prefab_BuildingDefinition_WaterTank_Config.asset` | Water Tank | Action currently missing | `[x]` | `[x]` | `[x]` |

## Match HUD Group Inventory

### Manual Selection And Fallbacks

| Runtime slot | Current sprite | Problem | New state |
|---|---|---|---|
| Generic squad | `Generated/MatchHUD/TargetLockV02/scn08_v02_selected_squad_group_portrait.png` | Legacy realistic style | `[x]` |
| Soldiers | `Generated/MatchHUD/TargetLockV02/scn08_v02_squad_rifle_portrait.png` | Content does not represent the required soldier-only fallback reliably | `[x]` |
| Vehicles | `Portraits/Secondary/SelectionSummary_VehicleSquad_512.png` | Legacy realistic style | `[x]` |
| Aircraft | `Icons/scn08_icon_support_parachute.png` | Unrelated icon, not an aircraft portrait | `[x]` |
| Transports | `Icons/scn08_command_board_vehicle.png` | Generic icon, not a transport group portrait | `[x]` |
| Buildings | `Icons/scn09_icon_build_tools.png` | Tool icon, not a building group portrait | `[x]` |
| Mixed force | `Icons/scn09_icon_squad_group.png` | Generic icon, not a mixed-force portrait | `[x]` |
| Soldier + vehicle | `Portraits/Secondary/SelectionSummary_MixedSoldierVehicle_512.png` | Legacy realistic style | `[x]` |
| Soldier + aircraft | `Portraits/Secondary/SelectionSummary_MixedSoldierAircraft_512.png` | Legacy realistic style | `[x]` |
| Vehicle + aircraft | `Portraits/Secondary/SelectionSummary_MixedVehicleAircraft_512.png` | Legacy realistic style | `[x]` |
| Soldier + vehicle + aircraft | `Portraits/Secondary/SelectionSummary_MixedSoldierVehicleAircraft_512.png` | Legacy realistic style | `[x]` |

### Squad Tray

| Runtime slot | Current sprite | New state |
|---|---|---|
| Rifle Squad | `Generated/MatchHUD/SquadTray/SquadTray_Card1_RifleSquad.png` | `[x]` |
| Armor | `Generated/MatchHUD/SquadTray/SquadTray_Card2_CombatVehicles.png` | `[x]` |
| Gunship | `Generated/MatchHUD/SquadTray/SquadTray_Card3_AttackHelicopter.png` | `[x]` |
| Jet Wing | `Generated/MatchHUD/SquadTray/SquadTray_Card4_FighterJet.png` | `[x]` |
| Transport | `Generated/MatchHUD/SquadTray/SquadTray_Card5_Transport.png` | `[x]` |

## Known Runtime Corrections

- [x] In `MatchHudSelectionPanelView.ResolveFallbackPortraitSprite`, change Transports fallback order from vehicle-first to transport-first.
- [x] Assign dedicated Polygon sprites to all 11 fallback fields in `SCN08_MatchHudContent.prefab`.
- [x] Replace all five squad-tray portrait sprites.
- [x] Add all group portraits to an appropriate runtime sprite atlas.
- [x] Verify no fallback field still references a generic icon after migration.
- [x] Verify Hall and Helipad resolve non-null Primary/Card/Action sprites.

## Generation Rules Per Asset

1. Resolve config and prefab.
2. Verify exact display role and weapon/attachment identity.
3. Inspect the exact low-poly prefab reference.
4. Include the FirstLaunch approved style summary as style-only reference.
5. Generate one role-specific candidate, not three loosely related variants in one image.
6. Inspect full resolution and thumbnail reductions.
7. Reject photoreal surfaces, invented attachments, incorrect weapons, cropped critical silhouette, flags, text, or repeated generic backgrounds.
8. Record prompt, input references, date, tool/model when available, revision, and decision.
9. Preserve rejected evidence only when it explains a correction; do not mix rejected images into runtime folders.

## Provenance Record

| Candidate ID | Revision | Tool path | Input references | Prompt recorded | SHA-256 | Decision |
|---|---:|---|---|---|---|---|
| `PPA-01` | R1 | Built-in imagegen | Heavy Gunner prefab + style lock | `[x]` | `13e1805c0ebecca464ec5a0b1a0c2936e55ce339d6a5710af53884eb46d11224` | Imported and validated |
| `PPA-02` | R1 | Built-in imagegen | Heavy Gunner prefab + style lock | `[x]` | `92b2e491ea2319044aea1e964a2d35dd95e57e9bea7aa2f1b2c57a3c3130c012` | Imported and validated |
| `PPA-03` | R1 | Built-in imagegen | Heavy Gunner prefab + style lock | `[x]` | `44ba158fe1daab8b2f1355d787cbd2ded21f0f1ac5f400c7b747bb62dfb19376` | Imported and validated |
| `PPA-04` | R4 | Built-in imagegen | Clean twin-tail silhouette + prefab audit + style lock | `[x]` | `44f79b36c6b66d8476edf4b6354ca4f44c94931b4c4fa9318f5ece97937d97f4` | Imported and validated |
| `PPA-05` | R4 | Built-in imagegen | Clean twin-tail silhouette + ground-state contract + style lock | `[x]` | `bce2268862242103552bc52c55894b37bb93190b6f097972803fd1709ac235a6` | Imported and validated |
| `PPA-06` | R4 | Built-in imagegen | Clean twin-tail silhouette + climb-state contract + style lock | `[x]` | `d1e104ea38f25c632d08354463169d993910b5ae17bc389e03206f9061285638` | Imported and validated |
| `PPA-07` | R1 | Built-in imagegen | Barracks prefab + style lock | `[x]` | `6e1360f6816beda1e80dfadc105d750f46c06f8e6fe08f46d0df7b5463b43706` | Imported and validated |
| `PPA-08` | R1 | Built-in imagegen | Barracks prefab + style lock | `[x]` | `362945c9731368026ad42a3d8f568b8c4db9ae7c5f1cc295828b99c18c5461a6` | Imported and validated |
| `PPA-09` | R1 | Built-in imagegen | Barracks prefab + style lock | `[x]` | `d4bde9354c773e461657f912852761205bdb159ccb658b0a3fb3cf64ac6a49e7` | Imported and validated |
| `PPA-10` | R1 | Built-in imagegen | Infantry references + style lock | `[x]` | `090894080f75c66d96680376f31c294d7419af4601548c8ab406f25a4bff1997` | Imported and validated |
| `PPA-11` | R1 | Built-in imagegen | Infantry/vehicle/aircraft references + style lock | `[x]` | `adebc0128c588624444ad8e25f869ab2b1400b11264ab0806151620bcc8955cf` | Imported and validated |
| `PPA-12` | R1 | Built-in imagegen | Transport references + style lock | `[x]` | `098877adf4c571066975c2c624256919e4709b5274b0bc81719a0fa6080d4520` | Imported and validated |

## Approval Batch 01 Prompt Record

Shared constraints for all 12 prompts:

- Use case: `stylized-concept`.
- Intended use: square mobile RTS portrait reviewed at `512x512`, `128x128`, and `64x64`.
- The gameplay reference defines subject identity only.
- The FirstLaunch references define rendering style only.
- Visibly faceted low-poly geometry, simplified POLYGON proportions, flat color-block materials, restrained texture detail, and Match palette.
- No photoreal surfaces, no real flags, no insignia, no readable writing, no logo, no watermark, no UI frame, and no invented equipment.

### PPA-01 Heavy Gunner Primary

```text
Create a square transparent-cutout source portrait of the exact heavy machine gun infantry identity from the gameplay reference. Convert the subject into the approved FirstLaunch Match-aligned POLYGON rendering style: visibly faceted low-poly face, uniform, helmet, protective glasses, vest, gloves, boots, and machine gun; simplified blocky proportions; flat olive, tan, charcoal, and muted metal materials. Show a readable three-quarter character pose with the complete machine-gun category unmistakable. Use a perfectly flat solid #00ff00 chroma-key background with no floor, shadow, gradient, texture, reflection, or lighting variation. Keep generous padding and crisp separation. Do not use green in the subject. No text, frame, flag, insignia, logo, or watermark.
```

### PPA-02 Heavy Gunner Card

```text
Create a square mobile RTS Card portrait of the exact heavy machine gun infantry identity from the gameplay reference, rendered in the approved FirstLaunch Match-aligned POLYGON style. Calm ready stance in a low-poly forward-base supply lane with tan tents, charcoal crates, concrete barriers, and sparse desert terrain. Subject dominates the frame and the machine gun remains fully readable. Faceted geometry, simplified proportions, flat color-block materials, restrained dawn light, no photoreal textures. No firing, no text, no UI frame, no real flag, no insignia, no logo, no watermark.
```

### PPA-03 Heavy Gunner Action

```text
Create a square mobile RTS Action portrait of the exact heavy machine gun infantry identity from the gameplay reference, rendered in the approved FirstLaunch Match-aligned POLYGON style. Distinct action: braced behind a low concrete barrier, firing the configured heavy machine gun across a low-poly base perimeter. Keep the weapon category, helmet, glasses, vest, uniform colors, and body identity consistent with the reference. Use visibly faceted geometry, simplified blocky proportions, flat materials, restrained muzzle flash and dust, and readable silhouette. No gore, text, UI frame, real flag, insignia, logo, watermark, or photoreal surface detail.
```

### PPA-04 Strike Jet Primary

```text
Create a square transparent-cutout source portrait of one clean twin-tail strike jet. The nose must be visibly pitched upward 8-12 degrees relative to the horizontal canvas, with the tail lower, in a gentle ascent from lower-left to upper-right. Use exactly one left/right main wing, one left/right horizontal tailplane, and exactly two vertical fins. No canards, duplicated surfaces, stores, pylons, landing gear, or wheels. Use a front-side three-quarter view close to eye level, not steep top-down. Render in the approved FirstLaunch Match-aligned POLYGON style. Use a perfectly flat solid #00ff00 chroma-key background with no floor, shadow, gradient, texture, reflection, or lighting variation. No text, markings, flag, insignia, logo, or watermark.
```

### PPA-05 Strike Jet Card

```text
Create a square mobile RTS Card portrait of one clean twin-tail strike jet parked on a concrete airbase apron. Show a complete weight-bearing tricycle landing gear: one nose strut/wheel and two main struts/wheels, all visible, round, correctly sized, touching the ground, and carrying the aircraft weight. Keep the fuselage level or slightly nose-up with realistic ground clearance; never floating, nose-down, or toy-like. Use exactly one left/right main wing, one left/right horizontal tailplane, and exactly two vertical fins. Use a low human eye-level three-quarter view so all gear positions and contact shadows are readable. Render in the approved FirstLaunch Match-aligned POLYGON style. No text, markings, flag, insignia, logo, or watermark.
```

### PPA-06 Strike Jet Action

```text
Create a square mobile RTS Action portrait of one clean twin-tail strike jet immediately after takeoff in a decisive climb. The nose must be visibly pitched upward 15-18 degrees relative to the runway/horizon, with the tail lower and flight path rising from lower-left to upper-right. Landing gear must be completely retracted with no struts or wheels visible. Use exactly one left/right main wing, one left/right horizontal tailplane, and exactly two vertical fins. No canards, duplicated surfaces, weapons, pylons, stores, smoke, or flames. Use a front-side three-quarter view close to eye level with the runway receding beneath it. Render in the approved FirstLaunch Match-aligned POLYGON style. No text, markings, flag, insignia, logo, or watermark.
```

### PPA-07 Barracks Primary

```text
Create a square transparent-cutout source portrait of the exact barracks building in the gameplay reference. Preserve the long raised rectangular structure, gabled roof, window rhythm, central double door, steps, supports, colors, and proportions. Render it in the approved FirstLaunch Match-aligned POLYGON style with visibly faceted low-poly geometry, flat tan, gray, charcoal, and muted wood materials, and minimal texture detail. Readable elevated three-quarter angle with the full building inside frame. Use a perfectly flat solid #00ff00 chroma-key background with no ground, shadow, gradient, texture, reflection, props, or lighting variation. Do not use green in the building. No text, sign, flag, insignia, logo, or watermark.
```

### PPA-08 Barracks Card

```text
Create a square mobile RTS Card portrait of the exact barracks building in the gameplay reference, rendered in the approved FirstLaunch Match-aligned POLYGON style. Calm operational scene in a faceted desert response base with the barracks dominant, a few low-poly crates, barrier segments, utility props, and connected compacted ground. Preserve the roof, windows, double door, steps, supports, colors, and full silhouette. Flat materials and restrained morning light. No photoreal textures, extra annex, text, sign, flag, insignia, logo, or watermark.
```

### PPA-09 Barracks Action

```text
Create a square mobile RTS Action portrait of the exact barracks building in the gameplay reference, rendered in the approved FirstLaunch Match-aligned POLYGON style. Distinct operational action: alert mobilization at dusk, open doorway light, a few low-poly soldiers moving out, supply crates and subtle dust, with the building still dominant and completely recognizable. Preserve the exact roof, windows, double-door entrance, steps, supports, colors, and dimensions. No structural damage, fire, photoreal textures, text, sign, flag, insignia, logo, or watermark.
```

### PPA-10 Rifle Squad Group

```text
Create a square mobile RTS manual-selection group portrait showing a coherent four-person rifle squad in the approved FirstLaunch Match-aligned POLYGON style. Use the supplied gameplay character references for clothing, helmets, equipment, weapon categories, colors, and simplified proportions. Arrange the squad in a readable layered command-HUD composition at a faceted desert-base checkpoint; every soldier must be visible and the group must remain legible at 64x64. Flat materials, restrained tactical readiness, no photoreal skin or cloth. No vehicle, aircraft, text, number, UI frame, real flag, insignia, logo, or watermark.
```

### PPA-11 Mixed Force Group

```text
Create a square mobile RTS mixed-force selection portrait in the approved FirstLaunch Match-aligned POLYGON style. Clearly show all three required categories in one coherent composition: two low-poly infantry in the foreground, one exact-reference armored vehicle in the middle ground, and one exact-reference strike jet visible in the sky. Use an elevated three-quarter desert-base staging area with flat faceted terrain, barriers, tents, and sparse props. Each category must remain independently recognizable at 64x64. No photoreal materials, extra category, text, number, UI frame, real flag, insignia, logo, or watermark.
```

### PPA-12 Transport Group

```text
Create a square mobile RTS transport-group portrait in the approved FirstLaunch Match-aligned POLYGON style. Clearly show a coordinated logistics group using the exact supplied references: one canopy troop truck in the foreground, one transport helicopter lifting behind it, and one transport aircraft in distant climb. Use a faceted desert-base loading zone with tan tents, crates, runway edge, and flat simplified materials. All three transport silhouettes must be complete and readable at thumbnail size. No combat firing, photoreal materials, text, number, UI frame, real flag, insignia, logo, or watermark.
```

## Import And Assignment Gate

- [x] Candidate is explicitly approved.
- [x] Final dimensions and color mode match its role contract.
- [x] Alpha candidate has transparent corners and no chroma fringe.
- [x] Existing runtime path and GUID are recorded before replacement.
- [x] Existing `.meta` file remains byte-identical unless an intentional import-setting correction is documented.
- [x] Unity imports without errors.
- [x] Config references resolve to the intended sprite.
- [x] Prefab reference resolves through the config.
- [x] Atlas includes the sprite where applicable.

## Runtime Validation Gate

- [x] Build drawer character, vehicle, aircraft, and building card references.
- [x] Single-selected character, vehicle, aircraft, transport, and building references.
- [x] Passenger drawer character card references.
- [x] Squad tray: all five slots.
- [x] Manual soldiers.
- [x] Manual vehicles.
- [x] Manual aircraft.
- [x] Manual transports.
- [x] Manual buildings.
- [x] Every mixed-selection combination.
- [x] Missing-sprite fallback.
- [ ] Target game resolution and both supported landscape aspect families.
- [x] Thumbnail readability at `128x128` and `64x64`.
- [x] No configured blank portrait while a selection panel is visible.
- [x] Unity compiler has zero errors.
- [x] Focused EditMode tests pass.
- [ ] Runtime screenshot contact sheet approved.

## Batch Order After Approval

1. Characters in identity/weapon-risk batches.
2. Ground vehicles.
3. Aircraft and helicopters.
4. Buildings.
5. Missing Hall/Helipad sets and eight missing building Action roles.
6. Manual-selection group portraits.
7. Squad-tray portraits.
8. Atlas/import audit.
9. Runtime visual validation and final stale-reference scan.

## Progress Log

- 2026-07-11: Repository audit completed. Found 74 target entity definitions, 222 entity-role targets, and 16 group surfaces.
- 2026-07-11: User authorized creation of this tracker and generation of approval batch 01 only.
- 2026-07-11: Generated all 12 approval candidates and created full-resolution, `128x128`, and `64x64` review sheets. Primary candidates have RGBA output and transparent corners.
- 2026-07-11: Rejected Strike Jet R1/R2 because the prefab reference propagated stacked extra wing/tail surfaces. Prefab audit found no active Destroyed child; the live and original package prefabs themselves contain shared `Jet_02` flap/tail renderer parts.
- 2026-07-11: Rejected Strike Jet R3 because both flight poses read nose-down and the parked Card omitted landing gear. R4 was generated as three independent physical states: gentle clean climb, weight-bearing parked tricycle gear, and decisive gear-retracted takeoff climb.
- 2026-07-11: User approved all 12 batch-01 candidates, including the corrected Strike Jet R4 set. The batch now defines the production art lock for remaining generation.
- 2026-07-11: Production character batch 02 generated Bomb Suit Specialist, Civilian Female I, and Civilian Female II Primary/Card/Action triplets. Primary alpha corners and `64x64` readability were validated; runtime import remains deferred.
- 2026-07-11: Production character batch 03 generated Civilian Male I, Civilian Male II, and Contractor Female Primary/Card/Action triplets. A landscape Civilian Male I Action attempt was rejected and regenerated as a compliant square composition.
- 2026-07-11: Production character batch 04 generated Contractor Male I (pistol), Contractor Male II (rifle), and Ghillie Rocketeer Primary/Card/Action triplets. Weapon categories, launcher geometry, Primary alpha corners, and square format were validated.
- 2026-07-11: Production character batch 05A generated Insurgent Female I rifle Primary/Card/Action portraits. Rifle identity, square format, and Primary alpha corners were validated.
- 2026-07-11: Production character batch 05B generated Insurgent Female II pistol Primary/Card/Action portraits. Pistol identity, square format, and Primary alpha corners were validated.
- 2026-07-11: Production character batch 05C generated Insurgent Male I RPG Primary/Card/Action portraits. Launcher geometry, clear Action backblast zone, square format, and Primary alpha corners were validated.
- 2026-07-11: Production character batch 06A generated Insurgent Male II machine-gunner Primary/Card/Action portraits. Long-barrel/feed identity, square format, and Primary alpha corners were validated.
- 2026-07-11: Production character batch 06B generated Insurgent Male III compact-rifle Primary/Card/Action portraits. Short weapon silhouette, square format, and Primary alpha corners were validated.
- 2026-07-11: Production character batch 06C generated Insurgent Male IV marksman Primary/Card/Action portraits. Long scoped-rifle identity, square format, and Primary alpha corners were validated.
- 2026-07-11: Production character batch 06D generated Insurgent Male V standard-rifle Primary/Card/Action portraits. An initial role-worded prompt was safety-filtered before generation; neutral strategy-game wording succeeded without changing identity or output requirements. Square format and Primary alpha corners were validated.
- 2026-07-12: Production character batch 07A generated Field Commander Primary/Card/Action portraits. Command identity, pistol category, square format, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 07B generated Pilot Female Primary/Card/Action portraits. Initial Card/Action attempts were rejected for inventing a helmet and oxygen mask; corrected versions preserve the mustard headscarf, visible face, harness, pistol identity, square format, and Primary alpha corners.
- 2026-07-12: Production character batch 07C generated Pilot Male Primary/Card/Action portraits. The initial Action was rejected for inventing a helmet; the corrected version preserves short hair, visible face, flight harness, pistol identity, square format, and Primary alpha corners.
- 2026-07-12: Production character batch 08A generated Soldier Female I Alt I rifle Primary/Card/Action portraits. Uniform variant, headset/goggles, rifle identity, square format, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 08B generated Soldier Female I Alt II marksman Primary/Card/Action portraits. Long scoped-rifle identity, helmet/goggles/NVG variant, square format, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 08C generated Soldier Female I marksman Primary/Card/Action portraits. Suppressed scoped-rifle identity, black visor/helmet/mic, square format, distinct overwatch/action compositions, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 09A generated Soldier Female II Alt I rifle Primary/Card/Action portraits. Boonie-hat/glasses and classic service-rifle identity, square format, route-planning Card, street-crossing Action, HUD-scale readability, and Primary alpha corners were validated. Added a global Action pose-diversity acceptance rule after review feedback.
- 2026-07-12: Production character batch 09B generated Soldier Female II Alt II breacher Primary/Card/Action portraits. Sunglasses/headset/red shoulder-marker and compact-carbine identity, square format, seated equipment-check Card, doorway-entry Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 09C generated Soldier Female II rifle Primary/Card/Action portraits. Helmet-top goggles/visible-face and long service-rifle identity, square format, perimeter-radio Card, prone-fire Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 10A generated Soldier Male I Alt I marksman Primary/Card/Action portraits. Black helmet/visor/lower-face-mask, green vest-cartridge, and suppressed scoped-rifle identity, square format, scope-adjustment Card, vehicle-hood-rest Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 10B generated Soldier Male I Alt II advanced-rifle Primary/Card/Action portraits. Raised NVG/full respirator and suppressed optic-rifle identity, square format, night wrist-status Card, stair-descent Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 11A generated Soldier Male II Alt I pistol-specialist Primary/Card/Action portraits. Wide field-hat/aviators/beard/shoulder-cartridge and single-pistol identity, square format, range-inspection Card, fast-draw Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 11B generated Soldier Male II Alt II rifle Primary/Card/Action portraits. Low black visor/full-beard/red shoulder-loop and long service-rifle identity, square format, perimeter-gate Card, barrier-vault Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 11C generated Soldier Male II Alt III dual-pistol Primary/Card/Action portraits. Red headband/wraparound glasses/beard/chest-cartridge and paired-pistol identity, square format, magazine-loading Card, column-lean dual-pistol Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 11D generated Soldier Male II Alt IV rifle Primary/Card/Action portraits. Tan helmet/clear glasses/gray beard/radio antenna/green vest-cartridge and compact-rifle identity, square format, relay-junction Card, embankment-descent Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production character batch 11E generated Soldier Male II grenadier Primary/Card/Action portraits. Tan helmet/dark visor/gray lower-face-mask/radio and integrated rifle-launcher identity, square format, launcher-loading Card, standing grenade-launch Action, HUD-scale readability, and Primary alpha corners were validated. Character entity generation is now complete; the next unchecked entity is Fast APC.
- 2026-07-12: Production vehicle batch 12A generated Fast APC Primary/Card/Action portraits. Compact four-wheel/sloped-rear/short-cab/bull-bar/headlight/roof-hatch identity, square format, motor-pool Card, high-speed road-bend Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 12B generated Heavy APC Primary/Card/Action portraits. Initial Primary and Action attempts were rejected for invented duplicate turret/barrel geometry. Corrected eight-wheel/boat-bow/high-hull/single-turret/single-autocannon identity, square format, depot Card, shallow-water-crossing Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 12C generated Armored APC Primary/Card/Action portraits. An initial wheel-count inference was corrected after alpha-inspecting the prefab reference; accepted candidates preserve the six-station near track, paired continuous tracks, squat troop hull, sloped front, roof hatches/low cupola, and unarmed identity. Square format, track-service Card, rubble-climb Action, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 13A generated Recon Drone Primary/Card/Action portraits. Audit found the MidLOD prefab renders the complete `Model` mesh together with separately extracted flap, wheel, and tail child meshes, producing destroyed-model-like overlap; legacy portraits also retained an incorrect central upright tail. All prefab/runtime portrait geometry was therefore rejected. The final triplet uses a manual clean-airframe contract: one main wing pair, exactly two diagonal V-tail surfaces, no central upright fin, one rear pusher propeller, one sensor nose, and three landing-gear legs when deployed. Corrected transparent Primary, runway-readiness Card, reconnaissance-orbit Action, HUD-scale readability, and alpha corners were validated.
- 2026-07-12: Production vehicle batch 13B generated Attack Helicopter Primary/Card/Action portraits. The contaminated raw prefab capture was excluded; accepted candidates preserve the tandem cockpit, single four-blade main rotor, single tail rotor, skids, short weapon wings, paired rocket pods, and chin gun identity. Square format, helipad-readiness Card, banked rocket-pass Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 13C generated Light Attack Helicopter Primary/Card/Action portraits. A first Action attempt was rejected for photorealistic style drift and a second for ambiguous reverse projectile direction. Accepted candidates preserve the compact bubble cockpit, slim tail boom, single four-blade main rotor, single tail rotor, skids, paired compact rocket pods, and nose sensor identity. Square format, forward-pad Card, distinct evasive canyon-climb Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 13D generated Transport Helicopter Primary/Card/Action portraits. Accepted candidates preserve the long troop cabin, multi-window rounded cockpit, twin overhead engine housings, single four-blade main rotor, single tail rotor, horizontal stabilizer, tricycle wheel gear, and unarmed identity. Square format, cargo-apron readiness Card, distinct sling-load Action with one pallet and coherent cables, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 13E generated Fighter Jet Primary/Card/Action portraits. Accepted candidates preserve the long pointed nose, tandem canopy, paired side intakes, twin engines/exhausts, exactly two vertical tails, conventional swept wings/tailplanes, tricycle gear, and modest missile load. Square format, correctly wheel-supported and nose-up readiness Card, distinct gear-retracted climbing-turn Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14A generated Light Armored Car Primary/Card/Action portraits. Accepted candidates preserve the compact four-wheel armored body, split windshield, side firing ports, brush guard, shielded roof machine gun, and single gunner identity. Square format, checkpoint Card, distinct suspension-loaded drainage-crossing Action without firing, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14B generated Air Missile Launcher Primary/Card/Action portraits. Accepted candidates preserve the armored three-pane cab, three-axle/six-wheel chassis, rear launcher frame, and exact six-missile ready load. Square format, radar-site readiness Card, coherent launch Action with five missiles remaining and one departing along the rack axis, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14C generated Ground Missile Launcher Primary/Card/Action portraits. Accepted candidates preserve the armored two-pane cab, three-axle/six-wheel chassis, and single opaque rectangular launcher pod with hydraulic elevation arms. Square format, elevated firing-bay Card, distinct lowered-and-locked rapid-relocation Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14D generated Transport Plane Primary/Card/Action portraits. Two Card attempts were rejected for five-engine and asymmetric 3+1 engine errors. Accepted candidates preserve the bulbous cargo fuselage, high wings, exactly four engines arranged two per wing, single high T-tail, wingtip fins, and coherent heavy landing gear. Square format, symmetric cargo-apron Card, distinct nose-up takeoff Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14E generated Radar Tank Primary/Card/Action portraits. Accepted candidates preserve the unarmed tracked hull, six near-side road wheels, three front windows, tall central mast, one large top dish, one smaller side dish, rectangular antenna panels, and whip antennas. Square format, communications-site calibration Card, distinct ridge-crest pivot Action with raised mast, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14F generated Battle Tank Primary/Card/Action portraits. Accepted candidates preserve the wide tracked hull, slab side skirts, eight near-side road wheels, angular turret, single long main gun, rear bustle, smoke launcher bank, roof optics, and antennas. Square format, armored-lane readiness Card, distinct berm-crossing Action without firing, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14G generated Canopy Truck Primary/Card/Action portraits. Accepted candidates preserve the forward-control cab, split windshield, left exhaust stack, brush guard, two-axle/four-wheel chassis, ribbed drop-side bed, and arched canvas canopy. Square format, depot-loading Card with coherent rear access, distinct shallow-ford Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14H generated Tanker Truck Primary/Card/Action portraits. Accepted candidates preserve the forward-control cab, split windshield, left exhaust stack, brush guard, two-axle/four-wheel chassis, angular fuel tank, top hatches, and lower pump equipment. Square format, contained field-refueling Card, distinct controlled relocation Action without leaks or fire, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production vehicle batch 14I generated Cargo Truck Primary/Card/Action portraits. Accepted candidates preserve the forward-control cab, split windshield, left exhaust stack, brush guard, two-axle/four-wheel chassis, open ribbed drop-side tray, and secured cargo load. Square format, warehouse-loading Card, distinct high rear-view switchback Action with taut cargo straps, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15A generated Airport Primary/Card/Action portraits. Accepted candidates preserve the five-level concrete control tower, cantilevered wraparound balcony, slanted green-glass cabin, roof mast/dish/beacon array, and attached marked runway segment. Square format, daytime operational Card, distinct illuminated night-operations Action with a distant coherent fighter takeoff, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15B generated Ammunition Depot Primary/Card/Action portraits. The first Action was rejected for unsafe exposed shells outdoors. Accepted candidates preserve the corrugated Quonset shell, exposed front arch ribs, high side vent strip, concrete foundation, striped sliding door, and ordered interior ammunition storage. Square format, secured daytime Card, distinct sealed-pallet night dispatch Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15C generated Satellite Dish Primary/Card/Action portraits. Accepted candidates preserve the large concave rectangular panel reflector, feed box and triangular arm, rotating pedestal, open lattice tower, braced equipment platform/control boxes/ladder, and attached four-wheel module. Square format, stabilized daytime communications Card, distinct low-elevation night tracking Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15D generated Fuel Bladder Primary/Card/Action portraits. Accepted candidates preserve the low rectangular flexible bladder, bulged top/sloping folded sides, top fill cap, corner outlet, perimeter mat, anchor weights, and tie straps. Square format, protected pump-site Card, distinct dusk tanker-transfer Action without leaks or fire, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15E generated Heavy Guard Tower Primary/Card/Action portraits. Accepted candidates preserve the four-leg dark steel tower, three X-braced levels, bolted gussets, exterior ladder, wide railed deck, and red-white-charcoal enclosed cabin. Square format, daylight perimeter Card, distinct night-alert Action using separate ground floodlights rather than invented tower equipment, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15F generated Guard Tower Primary/Card/Action portraits. Accepted candidates preserve the four-leg timber structure, two X-braced levels, internal ladder, green plank observation half-walls, open viewing bays, and flat overhanging roof. Square format, quiet daytime checkpoint Card, distinct sunset sentinel patrol Action with one binocular observer and no weapon, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-12: Production building batch 15G generated City Hall Primary/Card/Action portraits from the sole prefab reference because all runtime roles were missing. Accepted candidates preserve the pink symmetrical hall, one large central golden dome, two smaller domes, exactly four tall corner towers, pointed-arch wings, parapets, and blue entrance. Square format, restored daytime civic Card, distinct illuminated night secured-objective Action, complete silhouette, HUD-scale readability, and Primary alpha corners were validated.
- 2026-07-13: Production building batch 15H generated Helipad Primary/Card/Action portraits after extracting the exact Unity FBX vertices, UVs, and texture-atlas markings into a non-runtime reference render. Accepted candidates preserve the low chamfered tan platform, four-panel gray landing slab, one white H, and exactly eight red-white perimeter bars. The first Action was rejected because the helicopter occluded one bar; the corrected dusk lift-off composition exposes all eight bars and preserves the approved unarmed transport-helicopter identity. Square format, distinct isolated/daytime/lift-off roles, complete silhouette, HUD-scale readability, and Primary alpha corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15I generated House Primary/Card/Action portraits from the active `SM_Bld_Village_House_01` prefab reference. Accepted candidates preserve the compact asymmetrical two-level flat-roof form, half-footprint upper tower, recessed parapet terrace, narrow openings, lintel/support-block rhythm, raised plinth, and weathered beige-gray plaster with exposed dark masonry. Square format, distinct isolated/occupied-frontage/household-move-in roles, complete silhouette, HUD-scale readability, and Primary alpha corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15J generated Oil Pump Primary/Card/Action portraits from the active animated `SM_Prop_Pipline_OilPump_01` assembly. Accepted candidates preserve the single perforated horsehead, curved walking beam, rear counterweight, triangular lattice tower, circular yellow-railed service platform, gray motor/red flywheel, taut drive belt, concrete skid, motor-side rail, and connected left wellhead/pipe run. The Action uses a mechanically coherent downstroke with the horsehead/polished rod lowered and counterweight raised, rather than repeating the static Card phase. Square format, complete thin-part silhouette, HUD-scale readability, and Primary alpha corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15K generated Large Oil Refinery Primary/Card/Action portraits from the active custom low-profile prefab assembly rather than the generic tower-heavy legacy interpretation. Accepted candidates preserve one dark circular process ring, exactly two pale capped columns, the sparse overhead conduit, one horizontal segmented pressure vessel/pedestal/nozzle, the separate left platform with exactly four short tanks, and the separate raised beige control house with all baked sign text removed. The first Primary was rejected for excessive horizontal compression at HUD scale; R2 uses a steeper isometric layout and passes `64x64` readability. The Action adds one contained tanker dispatch with one coherent hose and no spill, while keeping the refinery dominant. Square format and Primary alpha corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15L generated standard Oil Refinery Primary/Card/Action portraits from the active compact `SM_Bld_GasTower_02` prefab assembly, keeping it visually distinct from the Large Oil Refinery. Accepted candidates preserve exactly one pale cylindrical storage tower, dark base band and broad roof rim, one front ladder and roof hatch, one lower-left pipe/pump assembly, and one raised barrel rack, with all baked sign text removed. The Card places the same one-tower structure in a sparse secured daylight fuel yard. The Action shows a distinct controlled small-batch transfer: one operator turns the pump valve while one coherent short hose fills exactly one secured drum trolley, with no tanker, spill, smoke, or fire. Square format, full-resolution structure, `64x64` readability, and transparent Primary corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15M generated Portable Toilet Primary/Card/Action portraits from the active `SM_Prop_Portaloo_01` body and separate opened door rather than copying the closed-door legacy portraits. Accepted candidates preserve the tall tan molded shell, faceted arched roof and gray rim, rear-left roof vent, exactly four recessed left-side panels, rounded gray door frame, coherent opened ribbed door/hinge/latch, two-slot service plate without text, and thick base plinth. The Card places the single cabin in a tidy daylight camp sanitation corner. The distinct Action shows exactly one sanitation operator disinfecting the open doorway with one pressure wand and one coherent hose connected to one compact wheeled clean-water sprayer, with no waste, spill, or gross imagery. Square format, full-resolution structure, `64x64` readability, and transparent Primary corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15N generated Road Barrier Primary/Card/Action portraits from the active three-mesh `SM_Prop_Road_Barrier_01` assembly. Accepted candidates preserve exactly two separated charcoal posts and footings, the right motorized pivot/control box, one diagonal rear brace, the left receiver cap, and one long rectangular boom. The first Primary was rejected because it invented an eighth pale band at the pivot. The corrected Primary and Card retain the prefab's 70-degree raised state and exactly seven arm sections from pivot to tip: red/pale/red/pale/red/pale/red. The distinct Action shows the same seven-section arm lowering toward the receiver while exactly one checkpoint operator uses the control box, with no vehicle or combat. Square format, complete thin-part silhouette, `64x64` readability, and transparent Primary corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15O generated Shop Primary/Card/Action portraits from the active `SM_Bld_Shop_01` mesh, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve the asymmetric L-shaped weathered-plaster structure, two-story main block, continuous triangular-cutout roof parapet, exactly two upper brown windows with layered pointed-arch hoods and matching perforated balcony rails, exactly two open salmon-framed storefront bays, and one lower right annex with one narrow doorway. Primary keeps the bays empty like the mesh; Card stocks only the two bays with civilian goods. The distinct dusk Action shows exactly one shopkeeper sliding one sealed supply crate down one short plank to exactly one resident's two-wheel handcart, with no military actors, crowd, signage, or text. Square format, full-resolution structure, `64x64` readability, and transparent Primary corners were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15P generated Contractor Tent Primary/Card/Action portraits from the active `SM_Bld_Tent_01 (8)`, front-door, cover, foundation, and guy-rope assembly, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve the long tan semi-cylindrical canvas shell, repeated faceted cover segments and wavy valance, dark raised foundation, centered open doorway, exactly two narrow front windows, exactly three shallow steps, and coherent side ropes/stakes, with no invented side windows or annexes. The Card adds a sparse contractor work area with exactly two closed tool cases, one vise workbench, one cable coil, and one floodlight. The distinct blue-hour Action shows exactly two contractors assembling one short metal support frame: one braces it while the other tightens a visible bolted joint with a hand wrench, avoiding the repeated pointing and idle poses rejected by the global Action-diversity rule. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, and transparent Primary corners were validated, with zero opaque chroma-green pixels after matte removal. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15Q generated Expert Tent Primary/Card/Action portraits from the active shared tent body plus `SM_Bld_Tent_Entrance_01`, its outward-open gray entrance door, and `SM_Prop_Airvent_02 (2)`, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve the very long tan faceted canvas shell, continuous dark raised foundation, single projecting box-shaped vestibule, exactly one narrow front-side window, open gray door, shallow steps with paired handrails, repeated side ropes/stakes, and exactly one low roof vent, keeping this identity visibly distinct from the Contractor Tent. The Card adds one low tripod antenna, exactly two closed instrument cases, one cable reel, and one compact radio workstation. The distinct blue-hour Action shows exactly two unarmed specialists performing linked but different tasks: one seated operator tunes the rugged console while one crouched technician connects its coherent cable at the tripod antenna base, with no pointing, idle, or combat pose. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, and transparent Primary corners were validated, with zero opaque chroma-green pixels after matte removal. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15R generated Refugee Tent Primary/Card/Action portraits from the dedicated replacement `Model` mesh used by `Tent_Refugee`, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve the very long low gray-lavender tunnel shell, tan arched front, centered open fabric doorway, exactly two narrow front windows, exactly three capped rolled-canvas shades, thin dark base beam, and repeated short support feet. The set deliberately excludes inherited tent features absent from the dedicated mesh: ropes, stakes, solid foundation, vestibule, metal door, steps, handrails, roof vent, and side windows. The Card adds one blanket pallet, exactly two capped water containers, one sealed food-supply box, and one entry mat. The distinct blue-hour Action shows exactly two unarmed adults preparing shelter: one kneels inside the doorway to spread one blanket over a folding cot while the other lowers one water container onto the entry mat, with no idle, pointing, crowd, military, or distress pose. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, and transparent Primary corners were validated, with zero opaque chroma-green pixels after matte removal. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15S generated Soldier Tent Primary/Card/Action portraits from the active `SM_Bld_Tent_01 (2)`, closed `SM_Bld_Tent_Door_01`, and `SM_Prop_Airvent_02` assembly, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve the very long tan faceted shell, dark raised foundation, centered closed canvas door, exactly two narrow front windows, exactly three steps without handrails, repeated side ropes/stakes, one low roof vent, and the exact fixed front-left utility silhouette: one separate gray pad, one tan mesh-sided diagonally braced cabinet, and one dark horizontal cylindrical unit on two supports connected toward the tent. The Card adds exactly two closed footlockers, one folded cot, and two rolled sleeping mats on one bench. The distinct blue-hour Action shows exactly two unarmed soldiers in different readiness tasks: one seated soldier laces a worn boot while one standing soldier fastens the compression buckles on a backpack resting on a bench, with no pointing, idle, kneeling, or combat pose. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, and transparent Primary corners were validated, with zero opaque chroma-green pixels after matte removal. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15T generated Dirt Wall Primary/Card/Action portraits from the active four-part stacked barrier assembly, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve exactly two long tan earth-filled HESCO/gabion tiers, the centered back-set upper tier, repeated dark square wire-mesh cells, the low wide straight silhouette, and one coherent crown assembled from the prefab's two joined razor-wire sections. The Card installs the complete wall across a sparse daylight base perimeter without foreground occlusion. The distinct blue-hour Action shows exactly two unarmed engineers in different mechanically coherent tasks: one kneels to tighten a lower mesh seam with pliers while the other works from a two-step platform to fasten one top razor-wire support clamp, with no pointing, idle, repeated gesture, combat, or damage. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, transparent Primary corners, and zero visible chroma-green pixels were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15U generated Fence Wall Primary/Card/Action portraits from the active `SM_Prop_Fence_04` mesh, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve exactly five capped square steel posts on visible base plates, exactly four equal framed diamond-mesh bays, substantial top and bottom panel rails, and exactly three taut parallel security wires above the top rail, with no razor loops, extra bays, diagonal braces, gate hardware, sensors, or damage. The Card installs the complete single segment on a narrow concrete mounting strip beside a sparse daylight base service road. The distinct blue-hour Action demonstrates the wall's route-control function without another worker pose: exactly one unarmed compact four-wheel patrol vehicle makes a controlled turn behind the intact fence while its headlights project the diamond-mesh shadow onto the foreground. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, transparent Primary corners, and zero visible chroma-green pixels were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Production building batch 15V generated Water Tank Primary/Card/Action portraits from the sole active `SM_Bld_WaterTank_01` mesh, including a new Action candidate because the runtime Action slot is currently empty. Accepted candidates preserve exactly one tall dark desaturated olive cylindrical tank, its faceted domed roof and centered low cap, exactly four horizontal reinforcing rings, the heavy square timber platform, exactly four timber corner legs, connected bolted X braces on the visible faces, and the substantial lower perimeter beam. Primary and Card exclude ladders, railings, permanent external plumbing, extra tanks, and metal-lattice substitutions absent from the mesh. The distinct blue-hour Action avoids another character pose and shows one temporary hose gravity-filling the open first of exactly three water cans secured on one handcart, with the other two cans sealed, one clean stream terminating inside the opening, and no pump, spill, overflow, or tank damage. All three candidates are square `1254x1254`; full-resolution structure, `64x64` readability, transparent Primary corners, and zero visible chroma-green pixels were validated. All 222 entity-role portraits are now generated; runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Match HUD batch 16A reconciled the three approved group assets that were already included in the generated totals but not marked in the runtime-slot inventory. PPA-10 is now recorded as the Soldiers manual-selection candidate, PPA-11 as the Mixed Force manual-selection candidate, and PPA-12 as the Transport squad-tray candidate. Byte-identical accepted copies were placed under `ProductionCandidates/MatchHUD`; all remain square `1254x1254` and retain their validated `64x64` category reads. Counts were unchanged. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Match HUD batch 16B generated the Generic Squad manual-selection fallback as a type-neutral base-personnel detachment rather than duplicating the rifle-only or mixed-force candidates. The accepted square `1254x1254` composition uses exactly four established roster identities: one field leader in gray command uniform and peaked cap, one headset-equipped contractor woman with a secured muzzle-down rifle, one headscarf-wearing pilot woman carrying a closed helmet, and one unarmed civilian man with a sealed supply pouch. All figures remain independently visible with coherent equipment, distinct poses, no duplicate people, no vehicle or aircraft category, and no readable text, flag, or insignia. Full-resolution structure and `64x64` readability were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Match HUD batch 16C generated the three pure-category manual-selection fallbacks. Vehicles uses exactly one Battle Tank, one unarmed Fast APC, and one gunner-equipped Light Armored Car in a separated motor-pool formation, preserving the accepted vehicle geometry without duplicate turrets, guns, wheels, or units. Aircraft uses exactly one twin-tail Fighter Jet, one four-blade Attack Helicopter, and one clean Recon Drone; R1 was rejected because the drone's rear propeller/tail silhouette could read as the previously rejected central upright fin, while accepted R2 clearly separates exactly two pale diagonal V-tail surfaces from the small rear pusher propeller and contains no center fin. Transports uses exactly one four-wheel Canopy Truck, one unarmed tricycle-wheel Transport Helicopter with a coherent four-blade rotor, and one symmetric four-engine T-tail Transport Plane in a no-cargo dawn departure, visibly distinct from the approved sling-load squad-tray composition. All three candidates are square RGB `1254x1254`; full-resolution geometry and `64x64` category readability were validated. Runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Match HUD batch 16D completed all five remaining manual-selection fallbacks with exact accepted entity identities and distinct category combinations. Buildings uses exactly one Barracks, one trailer-mounted Satellite Dish, and one Water Tank; Soldier + Vehicle uses exactly two rifle soldiers and one unarmed four-wheel Fast APC; Soldier + Aircraft uses exactly two rifle soldiers and one complete four-blade Attack Helicopter; Vehicle + Aircraft uses exactly one single-gun Battle Tank and one clean twin-tail Fighter Jet; Soldier + Vehicle + Aircraft uses exactly one Heavy Gunner, one single-gun Battle Tank, and one complete four-blade Attack Helicopter in a composition materially different from the approved broad Mixed Force portrait. All five candidates are square RGB `1254x1254`; exact subject counts, full-resolution geometry, and `64x64` category readability were validated. Manual-selection fallback generation is now complete at `11/11`; runtime Assets and GUIDs remain unchanged.
- 2026-07-13: Match HUD batch 16E generated the four remaining squad-tray portraits. Rifle Squad uses exactly four distinct accepted rifle-soldier identities in a staggered moving formation with non-repeating observation/movement poses; Armor uses exactly one single-gun Battle Tank and one four-wheel Light Armored Car with one coherent shielded-roof gunner; Gunship uses exactly one complete tandem-cockpit Attack Helicopter with one four-blade main rotor, one tail rotor, skids, paired rocket pods, and chin gun in a dawn lift-off; Jet Wing uses exactly two clean Fighter Jets in echelon, each with twin engines, exactly two vertical tails, one conventional wing pair, one horizontal-tail pair, and retracted gear. All four candidates are square RGB `1254x1254`; exact subject counts, full-resolution geometry, and `64x64` squad-role readability were validated. All `238/238` target images are now generated and locally validated as approval candidates. Runtime Assets and GUIDs remain unchanged, and controlled replacement remains blocked pending approval.
- 2026-07-13: The user approved controlled runtime replacement. Seven early portrait-shaped Card/Action candidates were rejected and replaced with square R2 outputs before import: Bomb Suit Specialist Card/Action, Civilian Female I Card/Action, Civilian Female II Card/Action, and Civilian Male I Card. Full-resolution and `64x64` correction contact sheets passed review.
- 2026-07-13: The deterministic runtime map resolved exactly `238` targets: `222` entity-role portraits and `16` Match HUD portraits. All outputs were normalized to `512x512`; all `74` Primary sprites are RGBA and all `164` Card/Action/group sprites are opaque. Existing target `.meta` SHA-256 values remained byte-identical for all `220` pre-existing runtime assets. Eighteen previously missing role/group assets were created with new Unity metadata.
- 2026-07-13: `PolygonPortraitRuntimeMigration.ApplyAndValidate` imported the new assets, assigned all missing config roles and Match HUD fallbacks, rebuilt both portrait atlases, and validated all `238` runtime assignments. The Unity batch log completed with `[PolygonPortraitRuntimeMigration] Applied and validated 238 Polygon portrait assignments.` and no compiler errors.
- 2026-07-13: Focused EditMode validation passed: Selection Summary `20/20`, UI Shell `14/14`, and Squad Tray Selection `3/3`. Sequential `dotnet build` checks for `Game.Runtime.csproj`, `Game.Editor.csproj`, and `Game.Tests.Editor.csproj` completed with zero errors.
- 2026-07-13: Runtime source contact sheets for Primary, Card, Action, and Match HUD group portraits were generated and inspected. The City Hall triplet was retained after confirming that its pink domed four-tower silhouette exactly matches the sole active Hall prefab/reference; this is a source-identity exception, not generation leakage. Final in-game landscape screenshots remain the only open visual approval gate.
