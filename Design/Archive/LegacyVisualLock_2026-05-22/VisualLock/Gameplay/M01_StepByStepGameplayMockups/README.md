# M01 Step-By-Step Gameplay Mockup Draft

Date: 2026-05-14
Owner: PM draft, pending Designer authority
Status: draft reference only; not design-approved; not implementation-ready

## Purpose

This folder captures a PM draft of the desired end-to-end M01 gameplay mockup sequence. It is an input for Designer review, not the source of truth for Art/Atlas, Gameplay, or QA/HCI. Designer must review it against the approved visual targets under `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/` and publish `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md` before Art creates mockup images.

## Approval Gate

1. Designer accepts, corrects, or replaces this draft in `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`.
2. Art/Atlas produces mockup images and contact sheets from the Designer report.
3. User approves the mockup images before they are added as accepted project visual lock.
4. Gameplay implementation and QA/HCI validation remain blocked until PM routes them after user approval.

Mission source:

- MissionId: `saga.ch01.m01.first_contact`
- ScenarioSetupId: `scenario.ch01.m01.first_contact`
- LevelId: `level.ch01.district_edge_01`
- IsoMapId: `iso.ch01.district_edge_01`
- Title: `First Contact`
- Archetype: `Patrol Intercept`
- Teaching goal: select, move, attack, read objective, finish result.

## Draft Runtime Locks For Designer Review

- Camera is true orthographic isometric with no horizon, vanishing point, wide-angle distortion, or cinematic perspective convergence.
- Tactical combat uses the tactical ground, not a baked strategic overview image.
- Tactical ground must not bake units, markers, objective pulses, minimap viewport, HUD, or tutorial UI into the map art.
- Production gameplay uses the XZ plane.
- Gameplay must wire tactical feedback through `BattleHudGameplayBridge`.
- Build is unavailable in M01. If visible, the Build control must show reason `MissionDoesNotAllowBuild`.
- Allowed commands are Select, Move, Attack, Stop, and Hold.
- ARIA Show Me and Do It must use typed command intents, never raw screen coordinates.
- Canonical command reason codes are `NoSelection`, `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, and `CameraJumpUnavailable`.

## Draft Anchors For Designer Review

| Anchor | Normalized Position | Usage |
| --- | --- | --- |
| `player_spawn.command_squad` | `(0.22, 0.52)` | Friendly squad initial position. |
| `camera.default_start` | `(0.28, 0.52)` | Tactical camera start and minimap viewport start. |
| `tutorial.move_target.cover_01` | `(0.42, 0.54)` | Required first move destination. |
| `enemy_spawn.patrol_start` | `(0.78, 0.54)` | Enemy patrol spawn. |
| `route.enemy_patrol_01.a` | `(0.78, 0.54)` | Patrol route start. |
| `route.enemy_patrol_01.b` | `(0.68, 0.53)` | Patrol route midpoint. |
| `route.enemy_patrol_01.c` | `(0.58, 0.52)` | Patrol route contact point. |
| `objective.destroy_patrol_group` | `(0.64, 0.53)` | Objective marker and attack focus. |
| `threat.patrol_warning_01` | `(0.70, 0.53)` | Threat warning pulse. |
| `minimap.viewport_start` | `(0.28, 0.52)` | Initial minimap viewport center. |

## Draft Required Step Frames

### M01-00 Strategic Briefing And Deploy

Required captures:

- `M01-00_StrategicBriefing_1920x1080.png`
- `M01-00_StrategicBriefing_2400x1080.png`

Player action: open M01 from the strategic flow and press Deploy.

Expected state: strategic preview shows `preview.ch01.first_contact`, mission title `First Contact`, primary objective "Destroy hostile patrol", and a deploy control. FTUE may show `ftue.m01.welcome` followed by `ftue.m01.deploy`.

Reject if: the screen jumps directly into combat without mission context, uses a tactical combat map as the strategic preview, or omits the primary objective.

### M01-01 Tactical Match Start

Required captures:

- `M01-01_TacticalStart_1920x1080.png`
- `M01-01_TacticalStart_2400x1080.png`

Player action: none after deploy completes.

Expected state: camera starts near `camera.default_start`, friendly squad `unit.player.rifle_squad_01` is at `player_spawn.command_squad`, enemy patrol `unit.enemy.patrol_01` starts near `enemy_spawn.patrol_start`, objective panel lists "Destroy hostile patrol", minimap uses `minimap.ch01.first_contact`, and no unit is selected.

Bridge/tutorial state: `ClearSelection`, `ClearCommandMode`, `SetWorldMarkersVisible(false)`, then FTUE target `BattleHud.ObjectivePanel` for `ftue.m01.objectives`.

Reject if: a unit starts selected, move or attack markers are visible, tactical UI is absent, or the minimap viewport does not match the camera start.

### M01-02 Squad Selected

Required captures:

- `M01-02_SquadSelected_1920x1080.png`
- `M01-02_SquadSelected_2400x1080.png`

Player action: tap or click the friendly squad.

Expected state: selection ring uses `marker.selection.ring`, selected squad panel is visible, command controls are enabled for the selected squad, objective panel remains visible, and no command mode banner is active.

Bridge/tutorial state: `ApplySelection(unit.player.rifle_squad_01)`, FTUE step `ftue.m01.select_squad`.

Reject if: selection is shown as a yellow square, selection detaches from the ground plane, the selected panel is missing, or selecting the squad also issues a move.

### M01-03 Move Preview

Required captures:

- `M01-03_MovePreview_1920x1080.png`
- `M01-03_MovePreview_2400x1080.png`

Player action: choose MOVE and aim at `tutorial.move_target.cover_01`.

Expected state: command banner says Move, destination marker uses `marker.move.destination`, projected path follows the isometric ground plane, selected squad remains selected, and current order is preview-only until commit.

Bridge/tutorial state: `ApplyCommandMode(Move)`, FTUE step `ftue.m01.move`.

Reject if: the move target is a giant green marker, the path floats above the ground, the squad deselects, or the banner uses a non-canonical command name.

### M01-04 Move Commit And Arrival

Required captures:

- `M01-04A_MoveCommit_1920x1080.png`
- `M01-04B_MoveInProgress_1920x1080.png`
- `M01-04C_MoveArrived_1920x1080.png`
- `M01-04A_MoveCommit_2400x1080.png`
- `M01-04B_MoveInProgress_2400x1080.png`
- `M01-04C_MoveArrived_2400x1080.png`

Player action: confirm the move destination.

Expected state: accepted command pulse appears at the target, squad runs from spawn toward `tutorial.move_target.cover_01`, arrival returns the squad to idle at the destination, selection persists, current order clears or returns to ready, and objective state is unchanged.

Bridge/tutorial state: `ApplyCommandResult(Move, accepted)`, then `ClearCommandMode`.

Reject if: movement teleports with no readable transition, the unit scale changes while moving, path feedback remains stuck after arrival, or the unit ends off-anchor.

### M01-05 Attack Preview

Required captures:

- `M01-05_AttackPreview_1920x1080.png`
- `M01-05_AttackPreview_2400x1080.png`

Player action: choose ATTACK and target `unit.enemy.patrol_01`.

Expected state: command banner says Attack, enemy patrol receives a restrained hostile target highlight, attack marker uses `marker.attack.target`, selected squad remains selected, and objective focus aligns with `objective.destroy_patrol_group`.

Bridge/tutorial state: `ApplyCommandMode(Attack)`, FTUE step `ftue.m01.attack`.

Reject if: enemy highlight turns the patrol into an unreadable red blob, target feedback covers the unit, or attack mode can target non-enemy ground without a rejection reason.

### M01-06 Attack Commit And Combat

Required captures:

- `M01-06A_AttackCommit_1920x1080.png`
- `M01-06B_CombatExchange_1920x1080.png`
- `M01-06C_PatrolDestroyed_1920x1080.png`
- `M01-06A_AttackCommit_2400x1080.png`
- `M01-06B_CombatExchange_2400x1080.png`
- `M01-06C_PatrolDestroyed_2400x1080.png`

Player action: confirm the attack target.

Expected state: accepted attack command starts combat, friendly squad uses aim/fire states, enemy patrol remains visible and alive during exchange, impact and destroyed VFX use `vfx.impact.smallarms` and `vfx.destroyed.light_unit`, patrol is defeated, objective completes only after defeat, and player squad survives.

Bridge/tutorial state: `ApplyCommandResult(Attack, accepted)`, then `ClearCommandMode`.

Reject if: a death or destroyed atlas state is used for an alive enemy, the enemy disappears without combat readability, the objective completes before defeat, or friendly squad death can complete the mission.

### M01-07 Invalid Command Recovery

Required captures:

- `M01-07_InvalidCommandRecovery_1920x1080.png`
- `M01-07_InvalidCommandRecovery_2400x1080.png`

Player action: attempt one invalid command after selection, such as Attack on non-enemy ground or Move to blocked ground.

Expected state: the selected squad remains selected, the command is rejected with one canonical reason code, invalid feedback is brief, command mode does not remain stuck, and no movement or attack is issued.

Bridge/tutorial state: `ApplyCommandResult(<Command>, rejected, <CanonicalReasonCode>)`.

Reject if: aliases such as `InvalidTarget`, `BlockedRoute`, `OutOfRange`, or `BuildModeUnavailable` appear, the selection is lost, or invalid feedback becomes a persistent world marker.

### M01-08 Objective And Minimap Focus

Required captures:

- `M01-08A_ObjectiveFocus_1920x1080.png`
- `M01-08B_MinimapFocus_1920x1080.png`
- `M01-08A_ObjectiveFocus_2400x1080.png`
- `M01-08B_MinimapFocus_2400x1080.png`

Player action: tap the objective row, then tap the minimap inside bounds.

Expected state: objective tap focuses `objective.destroy_patrol_group`, objective row shows focus, world objective marker pulses on the ground plane, minimap tap moves camera within bounds, viewport rectangle updates, and minimap ripple appears at the tap location.

Reject if: objective focus uses raw coordinates, camera jumps outside bounds, minimap does not update the viewport rectangle, or objective pulse is screen-space only.

### M01-09 ARIA Open And Show Me

Required captures:

- `M01-09A_AssistantOpen_1920x1080.png`
- `M01-09B_AssistantShowMe_1920x1080.png`
- `M01-09A_AssistantOpen_2400x1080.png`
- `M01-09B_AssistantShowMe_2400x1080.png`

Player action: open ARIA and press Show Me for the current FTUE step.

Expected state: ARIA panel does not hide the selected unit, objective panel, command controls, or critical markers. Show Me focuses or highlights the typed target for the current FTUE step without issuing the final command.

Reject if: ARIA clicks raw coordinates, blocks the objective or selected panel, or performs Do It behavior during Show Me.

### M01-10 ARIA Do It And Stop Recovery

Required captures:

- `M01-10A_AssistantDoIt_1920x1080.png`
- `M01-10B_StopRecovery_1920x1080.png`
- `M01-10A_AssistantDoIt_2400x1080.png`
- `M01-10B_StopRecovery_2400x1080.png`

Player action: press ARIA Do It for the active FTUE command, then issue Stop.

Expected state: Do It issues the same typed command intent a player could issue, Stop cancels the current order, command mode clears, path and target preview markers clear, and selection remains stable.

Reject if: Do It bypasses command validation, Stop deselects the squad, or stale move or attack markers remain on screen.

### M01-11 Objective Complete And Result

Required captures:

- `M01-11A_ObjectiveComplete_1920x1080.png`
- `M01-11B_ResultPopup_1920x1080.png`
- `M01-11A_ObjectiveComplete_2400x1080.png`
- `M01-11B_ResultPopup_2400x1080.png`

Player action: finish destroying the enemy patrol.

Expected state: objective row receives a complete tick, tactical state settles with the player squad alive, then result popup `POP-05_MissionResult` appears with M01 success.

Bridge/tutorial state: FTUE step `ftue.m01.complete`.

Reject if: result appears before objective completion, popup uses a non-M01 result surface, or victory is possible with the command squad dead.

## Required Contact Sheets

- `M01_StepByStepGameplay_ContactSheet_1920x1080.png`
- `M01_StepByStepGameplay_ContactSheet_2400x1080.png`

The contact sheets must show frames in numeric order from M01-00 through M01-11 and label each frame with its id.

## Acceptance Checklist

- Designer report exists and explicitly accepts, corrects, or replaces this draft.
- Art/Atlas mockup images exist only after Designer approval.
- User approves the mockup images before Gameplay implementation or project import begins.
- Approved captures match the Designer report and `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/`.
- When implemented later, contract ids, mission ids, scenario ids, level ids, ARIA intent ids, and command reason codes match `Design/M01_FirstContact_Production_Contract.md`.
- When implemented later, UI feedback routes through `BattleHudGameplayBridge`; no direct screen-coordinate scripting or raw overlay calls.
- When implemented later, every rejected command uses a canonical reason code and clears cleanly.
- When implemented later, tactical maps do not bake units, markers, HUD, minimap, or tutorial UI.
