# WarlineCapture M01 First Contact Production Contract

Date: 2026-05-07

2026-07-10 design dependency note: the tactical contract remains in force, but first-launch routing and narrative context now come from `First_Player_Experience_And_Story_Onboarding_Design.md`, `Campaign_Narrative_Bible.md`, and the amended Chapter 1 document. A fresh player reaches M01 from the cold open and diegetic identity flow, then sees the revoked-ARIA-credential debrief before the command-base menu. A later implementation-planning pass must reconcile this production contract in detail; this high-level pass does not add implementation steps.

2026-07-11 implementation hold: M01 planning may be studied, but M01 implementation and player-facing integration must not begin until `Design/NarrativeVision/FirstLaunch/IMPLEMENTATION_TRACKER.md` Phase 10R passes Gate 9R. That gate requires the second live FirstLaunch review to accept readable UI scale, clean speech-frame rendering, HUD-consistent controls, explicit Sahrin/Dalia/Samira/ARIA introductions, and layered runtime music/ambience/conflict/vehicle audio. This hold is independent of the later M01-camera and Android-device Gate 10 evidence.

2026-08-12 hold release: the project owner approved the current comic-style FirstLaunch dialogue/story presentation and instructed Codex to continue M01 under `Design/M01_FirstContact_Dense_City_High_Level_Design.md`, `Design/Architecture/m01_first_contact_dense_city_technical_architecture.md`, and the 43-item implementation tracker. Phase 10R / Gate 9R is therefore accepted and no longer blocks M01 production. This release does not relabel the retained July validation as a new run and does not close later Gate 10 camera-continuity, Android-device, or existing shell-regression evidence.

## Purpose

This is the implementation handoff for the first production tactical slice:

```text
MissionId: saga.ch01.m01.first_contact
ScenarioSetupId: scenario.ch01.m01.first_contact
OperationMapId: opmap.ch01.district_edge_01
PlanningCameraId: camera.ch01.m01.planning
MinimapProjectionId: minimap.ch01.m01.projection
```

Use this after:

- `AAA_Mobile_Game_Design_Document_v0_2.md`
- `Campaign_Narrative_Bible.md`
- `First_Player_Experience_And_Story_Onboarding_Design.md`
- `3D_SingleMap_Gameplay_Direction.md`
- `LargeScale_Grid_Movement_Design.md`
- `M01_Metric_Scale_Readability_Contract.md`
- `Level_And_Mission_Content_Plan.md`
- `UIUX_Gameplay_Element_Alignment.md`
- `SagaChapters/Saga_Chapter01_First_Response.md`

The goal is to make M01 implementable without guessing about map ids, UI feedback, metadata anchors, assets, tutorial targets, audio/VFX, or validation.

Movement scope note: the large-scale movement design does not expand M01 beyond select, move, attack, objective, and result. For M01 it only raises the acceptance bar: the first movement lesson must be readable through squad selection, a destination marker, attack marker/target highlight, invalid target feedback, HUD current-order state, operation-map camera bounds, and objective/result confirmation.

Metric scale/readability note: M01 public visual approval must follow `M01_Metric_Scale_Readability_Contract.md`. Soldiers should calibrate around a `1.8m` human anchor, visible building doors around a `2.3m` anchor, and buildings should scale from doors/footprints/road context rather than tiny decorative values. Selection must be subtle, grounded, and usable from the soldier body/formation footprint; point-command markers should read around two soldier footsteps wide; movement must show plausible infantry motion with correct idle/run animation; and public unit/building presentation must be ECS entity / atlas-backed rather than accepted through `SpriteRenderer`, `MeshRenderer`, or `MeshFilter` gameplay presentation.

## Locked Mission Contract

| Field | Value |
|---|---|
| `MissionId` | `saga.ch01.m01.first_contact` |
| `ScenarioSetupId` | `scenario.ch01.m01.first_contact` |
| `OperationMapId` | `opmap.ch01.district_edge_01` |
| `PlanningCameraId` | `camera.ch01.m01.planning` |
| `MinimapProjectionId` | `minimap.ch01.m01.projection` |
| Player-facing title | First Contact |
| Archetype | Patrol Intercept |
| Teaching goal | Select, move, attack, read objective, finish result. |
| Build rules | Build disabled. Show tutorial disabled reason if the Build button/drawer is visible. |
| Allowed commands | Select, Move, Attack, Stop, Hold. |
| Primary objective | Destroy hostile patrol. |
| Failure guard | Player command squad must survive. |

## Operation Map Definition Draft

The first implementation should create an `OperationMapDefinition` asset:

```text
Assets/Game/Data/OperationMaps/Chapter01/opmap.ch01.district_edge_01.asset
```

Initial authoring assumptions:

| Field | Draft Value |
|---|---|
| World source | 3D town corridor / civilian edge block operation-map scene. |
| Runtime world | One 3D operation map with roads, civilian frontage, command spawn, hostile patrol lane, camera states, and metadata. |
| Logical authoring grid | `64 x 36` cells over visible source art. |
| World origin | Operation-map origin defined by the scene/root metadata object. |
| Cell size | Define from scene scale contract; all metadata references grid cells or normalized coordinates. |
| Default camera | Battle camera focused on command squad and road corridor. |
| Planning camera | `camera.ch01.m01.planning`, zoomed to show route, civilian edge, and hostile contact area. |
| Minimap projection | `minimap.ch01.m01.projection`, same operation map with route/objective/viewport markers. |
| Camera bounds | Clamped to authored operation-map bounds, never empty scene edges. |
| Map theme | Small unstable civilian edge block, one road route, command point, light cover, no baked units/buildings. |

Until the final ground plate exists, use normalized map coordinates for authoring anchors:

```text
x: 0.0 left -> 1.0 right
y: 0.0 bottom -> 1.0 top
```

When the operation map is approved, convert these anchors to exact grid/world positions and inspect them in the metadata overlay.

## Required Operation-Map Metadata

### Walkable And Surface Areas

| Metadata Id | Type | Draft Placement | Gameplay Use |
|---|---|---|---|
| `walk.main_road` | Walkable + road | diagonal / horizontal road corridor across the center third of map | Primary movement path for friendly squad and enemy patrol. |
| `walk.road_shoulders` | Walkable + sidewalk/shoulder | edges along `walk.main_road` | Infantry fallback movement and cover-adjacent placement. |
| `walk.command_point_pad` | Walkable | near player spawn | Safe tutorial selection start. |
| `walk.cover_pullout_01` | Walkable | between player and patrol route | Tutorial move destination. |
| `block.map_edge` | Blocked | outside playable operation bounds | Prevents camera/path orders into empty or invalid areas. |
| `block.civilian_structures` | Blocked | decorative civilian walls/ruins/curbs if visible | Prevents pathing through visual obstacles. |
| `zone.civilian_edge` | Civilian zone | side/back edge near civilian block | Consequence/context marker; no M01 penalty unless later enabled. |

### Anchors

| Anchor Id | Kind | Normalized Draft | Required By |
|---|---|---:|---|
| `player_spawn.command_squad` | spawn | `(0.22, 0.52)` | friendly rifle squad spawn, FTUE select step |
| `camera.default_start` | camera | `(0.28, 0.52)` | match start camera |
| `tutorial.move_target.cover_01` | move target | `(0.42, 0.54)` | FTUE move step, destination marker |
| `enemy_spawn.patrol_start` | spawn | `(0.78, 0.54)` | hostile patrol spawn |
| `route.enemy_patrol_01.a` | route waypoint | `(0.78, 0.54)` | patrol route |
| `route.enemy_patrol_01.b` | route waypoint | `(0.68, 0.53)` | patrol route |
| `route.enemy_patrol_01.c` | route waypoint | `(0.58, 0.52)` | patrol route / contact point |
| `objective.destroy_patrol_group` | objective | `(0.64, 0.53)` | HUD objective jump, ARIA target, result condition |
| `threat.patrol_warning_01` | threat focus | `(0.70, 0.53)` | optional low-severity threat jump |
| `minimap.viewport_start` | minimap/camera | `(0.28, 0.52)` | HUD minimap viewport |

### Entity Footprints And Approach Rules

M01 has no production building placement. Still define runtime footprint metadata so attack/move logic uses the same pattern as later missions.

| Entity | Footprint | Approach Rule |
|---|---|---|
| `unit.player.rifle_squad_01` | dynamic unit footprint | spawn on nearest walkable cell to `player_spawn.command_squad`. |
| `unit.enemy.patrol_01` | dynamic unit footprint | spawn on nearest walkable road cell to `enemy_spawn.patrol_start`. |
| optional `decor.command_point` | non-attackable marker or decorative runtime prop | if used, static blocker only if visual footprint blocks road/sidewalk. |

## Scenario Setup Runtime Contract

| Runtime Need | Contract |
|---|---|
| Friendly start | Spawn one controllable rifle squad at `player_spawn.command_squad`. |
| Enemy start | Spawn one hostile patrol group at `enemy_spawn.patrol_start`. |
| Enemy behavior | Patrol or hold along `route.enemy_patrol_01` until engaged. |
| Objective group | `objective.destroy_patrol_group` resolves to `unit.enemy.patrol_01`. |
| Victory | Objective complete when the patrol group is destroyed. |
| Failure | Mission fails or blocks completion if the command squad is fully destroyed. |
| Camera start | Focus `camera.default_start`; player can pan only inside tactical camera bounds. |
| Build | Disabled with reason `MissionDoesNotAllowBuild`. |

## UI Command Contract

M01 must support both direct and explicit command flows, even if the direct flow is the primary mobile path.

Match HUD source of truth: `Match_HUD_And_Gameplay_Implementation_Spec.md`. Selection source of truth: `Match_Selection_Implementation_Spec.md`. M01 may keep `SELECT` visible but disabled/neutral while teaching direct squad selection; the player must be able to select by tapping the friendly squad in the world or the enabled rifle squad card.

| Player Action | Gameplay Result | Required UI Feedback | Owner Surface |
|---|---|---|---|
| Tap friendly squad | Select squad. | Selection ring, squad card selected, `BattleHud.SelectedEntityPanel` populated. | `SCN-08` |
| Selected squad + tap walkable ground | Issue move order. | Move marker at target, optional path preview, current order shows Move. | `SCN-08` |
| Tap `MOVE`, then tap walkable ground | Explicit move order. | `BattleHud.CommandModeBanner` shows Move until target/cancel. | `SCN-08` / `SCN-10` if wheel is open |
| Selected squad + tap enemy patrol | Issue attack order. | Attack marker/target highlight, current order shows Attack. | `SCN-08` |
| Tap `ATTACK`, then tap enemy patrol | Explicit attack order. | `BattleHud.CommandModeBanner` shows Attack until target/cancel. | `SCN-08` / `SCN-10` if wheel is open |
| Tap objective row | Camera focuses patrol objective anchor. | Objective row focus state, world objective marker pulse. | `SCN-08` |
| Tap minimap | Camera moves inside bounds. | Minimap ripple and viewport rectangle update. | `SCN-08` |
| Tap disabled Build | No build flow. | Reason toast: build unavailable in this tutorial. | `SCN-08` / `SCN-09` if visible |
| Victory | Result flow starts. | Objective complete tick, then `POP-05`. | `SCN-08`, `POP-05` |

## Command Result Reason Codes

Gameplay should return typed command results so UI, VFX, and audio can react consistently.

| Reason Code | When Used | UI Text Direction |
|---|---|---|
| `NoSelection` | Player taps ground/enemy with no controllable squad selected. | Select a squad first. |
| `TargetOutOfBounds` | Tap lands outside tactical playable bounds or in POT padding. | Target outside mission area. |
| `TargetBlocked` | Tap lands on blocked terrain. | Path blocked. |
| `TargetUnreachable` | No path to nearest valid cell. | Cannot reach that point. |
| `TargetNotEnemy` | Attack mode target is not hostile. | Choose an enemy target. |
| `TargetNotAttackable` | Target cannot be attacked. | Target cannot be attacked. |
| `CommandUnavailable` | Selected entity cannot perform command. | Command unavailable for this unit. |
| `MissionDoesNotAllowBuild` | Build pressed in M01. | Building unlocks in the next mission. |
| `CameraJumpUnavailable` | Minimap/objective/threat anchor missing. | No valid map focus. |

## FTUE / ARIA Contract

M01 FTUE steps must resolve to operation-map camera states, UI elements, runtime entities, or metadata anchors.

| Step Id | Target Type | Target Id | Notes |
|---|---|---|---|
| `ftue.m01.welcome` | planning camera | `camera.ch01.m01.planning` | Explain mission context. |
| `ftue.m01.deploy` | UI element | `SCN-06.DeployButton` | Show deploy button. |
| `ftue.m01.objectives` | UI element | `BattleHud.ObjectivePanel` | Highlight objective tracker after match starts. |
| `ftue.m01.select_squad` | runtime entity | `unit.player.rifle_squad_01` | Highlight squad using runtime entity, not map pixels. |
| `ftue.m01.move` | operation-map anchor | `tutorial.move_target.cover_01` | Do It issues move command. |
| `ftue.m01.attack` | runtime entity / operation-map objective anchor | `unit.enemy.patrol_01` / `objective.destroy_patrol_group` | Do It issues attack command. |
| `ftue.m01.complete` | popup | `POP-05_MissionResult` | Explain stars and rewards. |

ARIA must not click raw screen coordinates. All `Show Me` and `Do It` actions use typed command intents and these ids.

## Asset Manifest

### Required Before M01 Playable Implementation

| Asset Id | Type | Planned Path / Owner | Status Rule |
|---|---|---|---|
| `opmap.ch01.district_edge_01.scene` | 3D operation map | `Assets/Game/Scenes/OperationMaps/Chapter01/opmap_ch01_district_edge_01.unity` or equivalent scene/subscene | Required for playable validation once 3D map work begins. |
| `opmap.ch01.district_edge_01.metadata` | operation-map metadata | `Assets/Game/Data/OperationMaps/Chapter01/opmap.ch01.district_edge_01.asset` | Required for any playable validation. |
| `camera.ch01.m01.planning` | planning camera | Operation-map camera definition | Approved in Mission Briefing/Campaign context. |
| `minimap.ch01.m01.projection` | minimap projection | Operation-map minimap projection definition | Approved in Battle HUD with markers visible. |
| `unit.player.rifle_squad_01` | runtime 3D entity | Prefab/config-backed infantry presentation | Must match `M01_Metric_Scale_Readability_Contract.md`: about `1.8m` soldier scale, readable squad presentation, ECS-backed public presentation. |
| `unit.enemy.patrol_01` | runtime 3D entity | Prefab/config-backed hostile infantry presentation | Can reuse approved hostile infantry variant if readable. |
| `marker.selection.ring` | world overlay | UI/VFX marker atlas | Separate from unit sprite. |
| `marker.move.destination` | world overlay | UI/VFX marker atlas | Separate from ground art. |
| `marker.attack.target` | world overlay | UI/VFX marker atlas | Separate from ground art. |
| `marker.objective.focus` | world overlay | UI/VFX marker atlas | Used by objective row jump and ARIA. |
| `vfx.impact.light` | VFX | 3D VFX or temporary approved FX | Density-gated and readable at battle camera scale. |
| `vfx.unit.destroyed.small` | VFX | 3D VFX or temporary approved FX | Completes objective feedback. |

### Audio Event Requirements

| Event Id | Trigger |
|---|---|
| `Gameplay.Unit.Select.Infantry` | Friendly squad selected. |
| `Gameplay.Command.Move.Confirm` | Move order accepted. |
| `Gameplay.Command.Attack.Confirm` | Attack order accepted. |
| `Gameplay.Command.Invalid` | Command result is rejected. |
| `Gameplay.Objective.Update` | Patrol objective progress changes. |
| `Gameplay.Objective.Complete` | Patrol destroyed. |
| `Mission.Result.Victory` | Result flow begins. |
| `Tutorial.Highlight.Pulse` | ARIA highlights UI/world target. |
| `Tutorial.Step.Complete` | FTUE step completes. |

### VFX / Feedback Requirements

| Feedback Id | Trigger | Anchor |
|---|---|---|
| `feedback.selection_ring` | squad selected | selected runtime entity |
| `feedback.move_marker` | move command accepted | target walkable cell |
| `feedback.attack_marker` | attack command accepted | enemy runtime entity |
| `feedback.invalid_marker` | command rejected | attempted target cell/entity |
| `feedback.objective_marker` | objective focused | `objective.destroy_patrol_group` |
| `feedback.minimap_ripple` | minimap tap/jump | minimap projected target |

## Validation Checklist

### Design/Data Gate

- `MissionId`, `ScenarioSetupId`, `OperationMapId`, `PlanningCameraId`, and `MinimapProjectionId` are present.
- `OperationMapDefinition` has all required M01 anchors.
- Build is disabled with `MissionDoesNotAllowBuild`.
- FTUE step ids resolve to UI elements, runtime entities, operation-map camera states, or operation-map anchors.
- Asset register has rows for operation map, metadata, planning camera, minimap projection, runtime entities, markers, VFX, and audio.

### Visual Gate

- Operation map is reviewed at battle camera scale, not only from a planning overview.
- Infantry reads at about `1.8m` human scale against road/building/door context.
- Visible building doors read around `2.3m`, and buildings scale from doors/footprints/readability instead of tiny decor values.
- Enemy patrol reads as hostile without relying only on color.
- Selection is small, grounded, per-soldier or equivalent subtle formation treatment, and does not cover units or screen context.
- Point-command move/attack/target markers read around two soldier footsteps wide and do not cover the scene.
- Selection works from the soldier body/formation footprint, not only exact foot pixels.
- Idle animation is visible and correct.
- Movement shows plausible infantry run/move animation while units travel.
- Moving soldiers do not use crouched, sitting, hit, death, or artifact frames unless intentionally in that state.
- Public M01 unit/building visuals are ECS entity / atlas-backed and do not expose `SpriteRenderer`, `MeshRenderer`, or `MeshFilter` gameplay presentation as the accepted path.
- Move, attack, invalid, and objective markers are visible without covering units.
- HUD capture at 16:9 and 20:9 does not hide the selected squad, enemy patrol, objective tracker, or minimap.

### Metadata Gate

- Walkable cells match visible road/sidewalk.
- Blocked cells match visual obstacles and map edge.
- Patrol route stays on walkable road.
- Objective anchor is near the enemy patrol, not arbitrary screen center.
- Camera bounds prevent empty edge exposure.
- Minimap projection points to the same operation-map bounds.

### Playable Gate

- Tap friendly squad selects it.
- Tap walkable ground moves selected squad.
- Tap blocked/out-of-bounds ground returns a reason code and UI feedback.
- Tap enemy patrol attacks it.
- Enemy death completes `obj.ch01.m01.destroy_patrol`.
- Losing the command squad blocks or fails completion.
- Result popup opens with M01 stars/rewards.
- Replay rebuilds the same ScenarioSetup.

## Implementation Order

1. Create or stage the approved M01 3D operation-map scene, planning camera, and minimap projection rows.
2. Create `OperationMapDefinition` for `opmap.ch01.district_edge_01` with the draft anchors above.
3. Build the metadata overlay scene and verify anchor placement in the 3D operation map.
4. Update `SCN-08` UI to expose the M01-selected entity panel, command banner, markers, invalid toast, minimap bridge, and objective jump.
5. Wire gameplay command result reason codes to UI/VFX/audio.
6. Spawn friendly and hostile groups from metadata anchors.
7. Implement M01 objective completion and result route.
8. Run visual, metadata, and playable gates before M02 design or implementation begins.
