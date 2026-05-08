# WarlineCapture Art Asset Requirements Register

Date: 2026-05-06

## Purpose

This is the project-level art approval checklist. The repo already had partial source lists in the combat visual config, Saga mission docs, UI visual-lock docs, economy/reward docs, visual feedback/VFX recommendations, and monetization catalog, but it did not have one consolidated list that can be reviewed, approved, and marked complete.

The editable checklist is the companion CSV:

- `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`

Use the CSV columns `approval_status` and `completion_status` for production tracking. Do not treat an asset as final just because a prototype/runtime prefab exists.

## Status Definitions

| Status | Meaning |
|---|---|
| `missing` | Required asset row has no matching file yet. |
| `exists_needs_review` | A file exists at the planned path, but needs art approval. |
| `not_reviewed` | No approval decision has been recorded. |
| `approved` | User/art direction approved. |
| `rejected` | Do not use; regenerate or replace. |
| `complete` | Approved and wired to the relevant Unity/runtime surface. |

## Current Findings

- There is no single complete production art checklist before this file. Existing docs are useful source catalogs, not an approval register.
- `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json` lists 154 combat visual entries. It defines planned UI icon/portrait paths, but none of the `Assets/Game/Art/UI/Generated/CombatCatalog/...` paths currently exist.
- Existing 3D prefabs, mid-LOD generated prefabs, golden spike images, UI mockups, and validation sprites should be treated as references/prototypes unless specifically approved in this register.
- Strategic/zoomed-out map art and tactical/zoomed-in gameplay maps are separate approval lanes. Use `WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md` as the shared contract.
- Any older four-island or large-map scene remains a reference/prototype unless a row explicitly marks it approved for a strategic preview/minimap use. Tactical gameplay maps still need separate native-resolution, unit-readable map art plus metadata.

## Register Summary

| Bucket | Rows |
|---|---:|
| combat.building | 150 |
| combat.buildingAbility | 2 |
| combat.character | 99 |
| combat.operationAbility | 8 |
| combat.seaVehicle | 18 |
| combat.supportAbility | 26 |
| combat.unitCommand | 24 |
| combat.upgradeTrack | 80 |
| combat.vehicle | 54 |
| economy.resource | 6 |
| economy.reward | 13 |
| saga.level | 7 |
| saga.mission | 75 |
| store.product | 25 |
| ui.assistant | 7 |
| ui.commanderIdentity | 13 |
| ui.surface | 60 |

## File Status Summary

| Current Status | Rows |
|---|---:|
| exists_needs_review | 52 |
| missing | 615 |

## Required Asset Families

### Combat Units And Vehicles

Every character, ground vehicle, air vehicle, and sea vehicle needs:

- final gameplay sprite atlas at actual in-game camera scale
- UI icon readable at 48px and 96px
- UI portrait/card art
- transparent alpha and consistent contact shadow
- approval capture at intended tactical zoom

The CSV includes all 33 character entries, 18 vehicle entries, and 6 sea-vehicle entries from the visual config. Sea vehicles are design-ready but have no production assets.

### Buildings

Every building needs:

- intact world sprite
- construction world sprite
- damaged world sprite
- destroyed world sprite
- build/UI icon

The CSV includes all 30 building entries from the visual config. Six are explicitly marked `needsProductionAsset`: Coastal Radar, Command Post, Dock, Field Workshop, Medical Station, and Naval Yard. The other implemented runtime references still need final approved 2D state art.

### Abilities, Commands, And Upgrades

Required:

- 18 ability/support/operation/building-action icon rows plus VFX package rows
- 9 command icon rows plus VFX/feedback rows
- 40 upgrade-track icons
- 40 four-tier upgrade badge sets

These are critical for Command Wheel, Battle HUD, Loadout, Armory, Store, Reward Unlock, and POP-09.

Feedback/VFX rows should be checked against `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md` so locked-state wiggles, validation chips, reward flyouts, popup/drawer motion assets, command markers, scan effects, damage feedback, and critical warning treatments are tracked as production assets instead of ad hoc polish.

### Economy, Rewards, Store

Required:

- resource icons for Credits, Materials, Fuel, Intel, Command Authority, Rush Tickets
- reward icon/tile treatments for CommanderXP, unlocks, BlueprintParts, GearModule, Cosmetic, OperationSupply, SagaStars, and Operation district metrics
- product card art for 25 monetization catalog entries

### Commander Identity And ARIA

Required commander identity assets:

- six free default commander portraits for first launch
- unlockable commander portrait slots for Saga, Operation, and founder/event identity
- commander portrait frames, selected/locked card states, and edit icon
- `POP-11 Commander Identity` layer pack and prefab visuals

Required ARIA assistant assets:

- ARIA portrait distinct from the player's commander portrait
- ARIA/radio waveform icon
- Assistant button state set: idle, recommendation, critical, takeover, muted
- Assistant panel frame, recommendation chip states, tutorial card frame, UI/world highlight ring, path preview, blocked-action pulse, and takeover banner

These assets are defined in `WarlineCapture_FTUE_And_Command_Assistant_Design.md`. `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` is the concrete M01 source for `PREFAB-05_AssistantPanel`, Show Me / Do It / Stop, typed highlight/path preview targets, and visible takeover cancellation requirements. Existing generated profile, menu, or assistant-panel target images are placeholders until reviewed in this register.

### Saga Missions And Levels

The register includes all 25 Saga mission slots from Chapter 1-5. Each mission needs key art, map preview, and minimap art. Chapter 1 additionally has concrete tactical level rows for its five maps:

- `level.ch01.district_edge_01`
- `level.ch01.forward_post_01`
- `level.ch01.convoy_approach_01`
- `level.ch01.landing_zone_01`
- `level.ch01.fortified_node_01`

Important production rule: these tactical maps should be authored at native AI/output size for the intended unit scale. Do not upscale strategic island art and do not use tiny repeat tiles as final gameplay terrain.

Chapter 1 tactical-map production must also follow `WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`: every map approval needs a matching metadata package for walkable cells, roads, sidewalks, blockers, build zones, spawn anchors, objective anchors, route anchors, minimap data, and validation scene. A ground image without metadata is `exists_needs_review` at most, not `complete`.

M01 First Contact production assets and metadata are enumerated in `WarlineCapture_M01_FirstContact_Production_Contract.md`; use that contract before marking any M01 tactical art, marker, VFX, minimap, preview, or metadata row complete.

Strategic / tactical asset approval rules:

- Mission key art and `MapPreviewArtId` rows are approved in Saga Map / Mission Briefing context.
- `MinimapArtId` rows are approved in Battle HUD context with runtime marker readability.
- `IsoMapId` / tactical ground rows are approved only at close gameplay camera scale with runtime soldiers, vehicles, buildings, selection rings, command markers, VFX, and HUD visible.
- Tactical metadata is required even if it is not a visual PNG. The checklist should track metadata packages, validation scenes, and authoring overlays as production assets because they control pathfinding, blockers, build placement, minimap jumps, threat jumps, objective jumps, FTUE highlights, VFX anchors, and audio emitters.

### UI Screens And Popups

The register includes SCN-01 through SCN-19, POP-01 through POP-09, and reusable prefab surfaces. For each surface, track both the layer pack and the Unity prefab. A layer pack or prefab existing is not the same as final approval; final approval requires rendered capture review.

Missing/weak UI layer-pack coverage found in this pass:

- SCN-01 Splash / Loading layer pack missing from `Design/VisualLockLayered`
- SCN-02 Main Menu layer pack missing from `Design/VisualLockLayered`
- SCN-04 Settings / Accessibility layer pack missing from `Design/VisualLockLayered`
- SCN-09 Build Drawer / Production layer pack missing from `Design/VisualLockLayered`
- SCN-13 Quick Custom Game Setup layer pack missing from `Design/VisualLockLayered`

## Immediate Approval Workflow

1. Open the CSV and filter `current_status=missing`.
2. Approve production by family, not random one-offs: first combat gameplay scale, then UI icons/portraits, then mission/map art, then store/reward art.
3. For any generated asset, set `approval_status=approved` only after visual review at the intended screen/camera scale.
4. Set `completion_status=complete` only after the asset is wired to the Unity prefab/scene/config that consumes it.
5. If an asset is rejected, keep the row and set `approval_status=rejected`; do not delete the tracking row.
