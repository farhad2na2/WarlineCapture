# M01 Step-By-Step Gameplay Spec

## Sources Reviewed

- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/README.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/M01_StepByStepGameplayMockup_Manifest.json`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/README.md`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_GridProof.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_StrategicMap_Target.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_MapTiles_RoadSidewalkBuildings.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
- `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/VL_M01_ScaleGrounding_Rules.png`

## Design Authority Decision

Decision: correct PM draft

Reason: The PM draft has the correct M01 teaching sequence and required frame set, but it is not design authority as-is. This Designer spec keeps the existing gameplay and UI visual targets unchanged while tightening the camera, unit pose, survival, HUD, ARIA, minimap, recovery, and VisualLock acceptance requirements Art must show. The draft is corrected into an Art-ready mockup brief only; it is not a Gameplay implementation route.

VisualLock alignment notes: All mockups must match the approved true-isometric AAA VisualLock package under `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/`. Tactical gameplay uses the approved orthographic isometric city ground with no horizon, no vanishing point, no baked units, no baked UI, no baked markers, no yellow-square selection, no giant green move marker, and no red death/destroyed sprite used as a living enemy. Friendly and enemy units must preserve the approved RTS scale, grounded feet, consistent lighting, readable silhouettes, and restrained affiliation colors. The existing PM draft path uses `Gameplay`, while some approved VisualLock references use `GamePlay`; this report treats the actual reviewed folders as source and does not require folder renames.

## Step Table For Art

| Step ID | Gameplay beat | Player input/action | Camera/framing | Units/poses/facing/selection/survival | HUD/panels/minimap/assistant/log | Visual feedback/FX/ARIA | Transition/timing/recovery | Art mockup notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| M01-00 | Strategic briefing and deploy | Player opens M01 from strategic flow and presses Deploy. | Strategic map/mission preview, not tactical combat camera. Use approved strategic map style. | No tactical units visible as controllable gameplay pieces. | Mission title `First Contact`, primary objective `Destroy hostile patrol`, deploy control, optional FTUE `ftue.m01.welcome` then `ftue.m01.deploy`. | No tactical markers, no command modes, no combat FX. | Pressing Deploy transitions to tactical loading/start. | Show mission context before combat. Reject direct combat start or tactical map reused as strategic preview. |
| M01-01 | Tactical match start | No input after deploy completes. | True orthographic isometric camera centered near `camera.default_start` `(0.28, 0.52)` with minimap viewport matching start. | Friendly `unit.player.rifle_squad_01` at `player_spawn.command_squad` `(0.22, 0.52)`, idle, facing into the playable lane; enemy `unit.enemy.patrol_01` near `(0.78, 0.54)`, idle/patrol-ready, alive; no unit selected. | Objective panel lists `Destroy hostile patrol`; command panel is neutral/disabled until selection; minimap shows `minimap.ch01.first_contact`; assistant closed; log may show mission start. | `ClearSelection`, `ClearCommandMode`, `SetWorldMarkersVisible(false)`; no move/attack/objective markers yet; FTUE may target `BattleHud.ObjectivePanel`. | Stable start frame before tutorial prompts advance. | Establish baseline scale, tactical ground, HUD layout, and no-selection state. |
| M01-02 | Squad selected | Player taps/clicks friendly squad. | Camera remains stable; selected squad remains clearly visible, not hidden by UI. | Friendly squad selected, idle/ready, grounded, selection ring attached to ground plane; enemy alive in distance with no target highlight. | Selected squad panel appears; command controls Select/Move/Attack/Stop/Hold availability is readable; objective panel remains visible; minimap viewport unchanged; assistant closed; log can show selection. | `ApplySelection(unit.player.rifle_squad_01)`; selection uses `marker.selection.ring`, not a yellow square. | Selection feedback appears immediately without issuing a move. | Show readable selected state and command readiness without changing gameplay state. |
| M01-03 | Move preview | Player chooses MOVE and aims at `tutorial.move_target.cover_01` `(0.42, 0.54)`. | Camera remains true isometric and includes squad, route, and destination. | Friendly squad stays selected at origin/near current position, idle/ready; enemy remains alive and unselected. | Command panel/banner shows `Move`; selected panel and objective panel remain visible; minimap unchanged; assistant closed; log can show move preview. | Destination marker uses approved `marker.move.destination`; projected path lies on XZ/isometric ground; no accepted pulse yet. | Preview-only state until confirm; cancelling must clear path and marker. | Reject floating paths, oversized green markers, or deselection on command choice. |
| M01-04A | Move commit | Player confirms move destination. | Same framing as preview with origin, path, and target visible. | Friendly squad begins from selected state, alive; enemy remains alive, unselected. | Command panel shows move accepted/current order; objective unchanged; minimap viewport stable; assistant closed; log can show accepted Move. | `ApplyCommandResult(Move, accepted)`; brief accepted pulse at destination; path remains readable. | Commit pulse should be brief, then transition into movement. | Show accepted command without teleporting the unit. |
| M01-04B | Move in progress | No new input; move is executing. | Camera tracks or holds so squad, path, and destination stay readable without perspective drift. | Friendly squad uses run pose along path, selected ring follows/grounds cleanly; enemy alive, not targeted. | Current order indicates Move or movement-in-progress; objective remains incomplete; minimap viewport updates only if camera moves. | Path and subtle movement feedback remain ground-aligned. | Movement must show readable travel, not instant relocation; no scale changes mid-run. | Use approved run atlas state and scale-grounding rules. |
| M01-04C | Move arrived | No new input; movement completes at cover target. | Camera settles with squad at `tutorial.move_target.cover_01`. | Friendly squad alive, selected, idle/ready at destination; enemy alive near patrol/contact lane. | Command mode clears or returns ready; selected panel remains; objective unchanged; minimap consistent; log can show arrival/order complete. | `ClearCommandMode`; path and destination preview clear after arrival. | Arrival must recover to ready state with stable selection. | Reject stuck path marker or off-anchor arrival. |
| M01-05 | Attack preview | Player chooses ATTACK and targets `unit.enemy.patrol_01`. | Frame includes selected friendly squad, enemy patrol, and objective focus lane. | Friendly squad selected and alive, aim-ready or idle; enemy alive, visible, restrained hostile highlight, not obscured. | Command panel/banner shows `Attack`; selected panel and objective panel visible; minimap still readable; assistant closed; log can show attack preview. | `ApplyCommandMode(Attack)`; attack marker uses `marker.attack.target`; objective focus aligns with `objective.destroy_patrol_group` `(0.64, 0.53)`. | Preview-only until confirm; invalid ground target would reject with canonical reason. | Reject red blob enemies or target marker covering the unit silhouette. |
| M01-06A | Attack commit | Player confirms enemy target. | Camera frames both squad and patrol with enough tactical ground around them. | Friendly squad selected, alive, transitions to aim/fire; enemy patrol alive and targeted. | Command panel shows attack accepted/current order; objective still incomplete; minimap visible; log can show accepted Attack. | `ApplyCommandResult(Attack, accepted)`; brief accepted attack pulse; no objective completion yet. | Attack accepted pulse transitions into combat exchange. | Do not use death/destroyed art before the enemy is actually defeated. |
| M01-06B | Combat exchange | No new input; attack resolves. | Camera remains stable and readable during combat. | Friendly squad alive, aim/fire poses; enemy patrol alive during exchange, hit reactions allowed but silhouette remains readable. | Objective remains active; selected panel remains; command state can show engaged/attack order; minimap stable; log can show combat event. | Small-arms muzzle/impact FX use `vfx.impact.smallarms`; restrained hit flashes, no screen-filling effects. | Combat should show cause and effect over a short readable beat before destruction. | Preserve small RTS unit scale and approved lighting during fire/impact. |
| M01-06C | Patrol destroyed | Combat finishes. | Camera holds on defeated patrol and surviving squad. | Friendly squad alive and selected; enemy patrol defeated using valid death/destroyed state only now. | Objective row can update to complete only after defeat; selected panel remains; minimap remains visible; log can show patrol destroyed. | `vfx.destroyed.light_unit` may appear after defeat; objective marker can complete/pulse. | Completion follows defeat, not before; no enemy disappearance without readable defeat. | Victory cannot be shown if player squad is dead. |
| M01-07 | Invalid command recovery | After selection, player attempts one invalid command such as Attack on non-enemy ground or Move to blocked ground. | Camera stays where command is attempted; invalid target is visible if applicable. | Friendly squad remains selected, alive, and stationary; enemy state does not change. | Command panel shows rejection then recovers; objective unchanged; minimap unchanged; assistant closed; log shows one canonical reason code. | `ApplyCommandResult(<Command>, rejected, <CanonicalReasonCode>)`; use only `NoSelection`, `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, or `CameraJumpUnavailable`. | Invalid feedback is brief; command mode clears or returns to safe ready; no movement/attack is issued. | Reject aliases `InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`, lost selection, or persistent invalid marker. |
| M01-08A | Objective focus | Player taps objective row. | Camera begins or has just focused toward `objective.destroy_patrol_group` `(0.64, 0.53)`. | Friendly squad alive/selected if selection carried; enemy state depends on sequence timing but objective target remains readable. | Objective row shows focus state; command panel remains usable; minimap visible; assistant closed; log can show objective focus. | World objective marker pulses on ground plane with approved marker family. | Focus pulse is short and readable; no raw coordinate jump. | Objective feedback must be world-grounded, not screen-space only. |
| M01-08B | Minimap focus | Player taps minimap inside bounds. | Tactical camera moves within allowed map bounds; viewport rectangle updates. | Visible units keep correct scale after camera move; no pose/survival changes caused by minimap tap. | Minimap shows tap ripple and updated viewport; objective and selected panels remain readable; log can show camera focus if needed. | Camera focus must use typed/minimap intent, not raw arbitrary screen-coordinate scripting. | Camera movement is quick but not disorienting; recovery keeps HUD stable. | Reject out-of-bounds jumps or unchanged minimap viewport. |
| M01-09A | ARIA open | Player opens ARIA. | Camera remains on current tactical state; critical unit/marker area not obscured. | Friendly squad and enemy/objective remain readable according to current sequence state. | ARIA panel opens; it must not hide selected unit, command controls, objective panel, minimap, or critical markers; log remains available/readable. | Assistant affordances visible; no command issued. | Panel entrance should not change gameplay state. | The assistant is support UI, not a modal that covers tactical proof points. |
| M01-09B | ARIA Show Me | Player presses Show Me for current FTUE step. | Camera may focus/highlight the typed target for the active FTUE step while keeping HUD usable. | Units remain alive/selected according to current step; Show Me does not alter survival or issue final command. | ARIA remains open; objective/command panels/minimap remain readable; log can show assistant hint. | Show Me highlights/focuses typed target; no raw coordinate click; no Do It behavior. | Highlight is temporary and recoverable. | Art must distinguish Show Me guidance from actual command execution. |
| M01-10A | ARIA Do It | Player presses Do It for the active FTUE command. | Camera frames the relevant typed command target. | Friendly squad remains selected/alive; target state changes only through the same validation a player command would use. | ARIA remains readable without hiding command result; command panel shows issued intent/result; objective panel/minimap remain visible; log shows typed command result. | Do It emits the same typed command intent as player input and must pass validation; no bypass. | If accepted, transition mirrors the matching Move or Attack command; if rejected, use canonical reason. | Mockup must make Do It look like a valid command path, not a script shortcut. |
| M01-10B | Stop recovery | Player issues Stop after an active order. | Camera remains stable and shows squad recovery. | Friendly squad alive, selected, stops current order and returns to idle/ready; enemy/objective state does not advance from Stop alone. | Command mode clears; selected panel remains; objective unchanged unless already complete; minimap stable; log can show Stop. | Path/target preview markers clear; no stale move/attack markers. | Stop recovery should be immediate and leave player in a safe ready state. | Reject Stop deselecting squad or leaving stale command feedback. |
| M01-11A | Objective complete | Player finishes destroying the patrol. | Camera frames surviving squad, defeated patrol/objective area, and HUD objective completion. | Friendly squad alive; enemy patrol defeated; selection may persist on squad. | Objective row receives complete tick; command panel ready/cleared; minimap visible; assistant can be closed; log can show objective complete; FTUE `ftue.m01.complete`. | Objective completion feedback is restrained and ground/HUD aligned. | Completion appears only after defeat and before result popup. | This is the final tactical success state before result. |
| M01-11B | Result popup | No extra tactical input after objective complete, or player accepts completion flow if required. | Tactical background can remain visible behind result surface without changing isometric composition. | Friendly squad survival is implied/visible where background remains; enemy defeated. | Result popup `POP-05_MissionResult` appears with M01 success; objective is complete; command UI may dim behind popup. | No new combat FX; result surface should be readable and M01-specific. | Result appears after objective completion only. | Reject non-M01 result surface or victory with dead command squad. |

## Required Mockup Frames

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-00_StrategicBriefing_1920x1080.png` - Strategic mission briefing at 16:9 showing `First Contact`, `Destroy hostile patrol`, approved strategic-map style, and Deploy.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-00_StrategicBriefing_2400x1080.png` - Wide strategic mission briefing with the same content and no tactical combat start.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png` - Tactical start with no selection, friendly spawn, enemy patrol spawn, objective panel, minimap viewport at camera start, and no world markers.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_2400x1080.png` - Wide tactical start preserving the same no-selection and no-marker state.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png` - Friendly squad selected with approved ground selection ring, selected panel, enabled command controls, and no command banner.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_2400x1080.png` - Wide squad-selected state with HUD and selection readability intact.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-03_MovePreview_1920x1080.png` - Move preview showing Move banner, ground-aligned path, approved destination marker, selected squad, and no accepted pulse.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-03_MovePreview_2400x1080.png` - Wide move preview with route and HUD fully readable.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-04A_MoveCommit_1920x1080.png` - Move accepted pulse at destination with selected squad beginning the order.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-04B_MoveInProgress_1920x1080.png` - Friendly squad running along the isometric path with stable scale and grounded selection.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-04C_MoveArrived_1920x1080.png` - Friendly squad idle/ready at cover destination with command mode cleared and objective unchanged.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-04A_MoveCommit_2400x1080.png` - Wide move accepted pulse and beginning movement.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-04B_MoveInProgress_2400x1080.png` - Wide movement-in-progress proof with route readability.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-04C_MoveArrived_2400x1080.png` - Wide arrived state with selection and HUD stability.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-05_AttackPreview_1920x1080.png` - Attack preview with selected squad, alive enemy patrol, restrained hostile highlight, attack marker, and objective focus alignment.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-05_AttackPreview_2400x1080.png` - Wide attack preview preserving enemy readability and HUD state.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-06A_AttackCommit_1920x1080.png` - Attack accepted state before damage, with enemy still alive and objective incomplete.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-06B_CombatExchange_1920x1080.png` - Readable small-arms exchange with friendly aim/fire poses and enemy alive during impacts.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-06C_PatrolDestroyed_1920x1080.png` - Patrol defeated after combat, friendly squad alive, objective ready to complete.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-06A_AttackCommit_2400x1080.png` - Wide attack accepted state before damage.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-06B_CombatExchange_2400x1080.png` - Wide combat exchange preserving unit scale and effects restraint.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-06C_PatrolDestroyed_2400x1080.png` - Wide patrol destroyed state with objective completion timing clear.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-07_InvalidCommandRecovery_1920x1080.png` - Invalid command rejection with selected squad retained, one canonical reason code, and no issued order.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-07_InvalidCommandRecovery_2400x1080.png` - Wide invalid-command recovery showing the same clean rejection and recovery.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-08A_ObjectiveFocus_1920x1080.png` - Objective row focus and world objective pulse grounded at the objective area.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-08B_MinimapFocus_1920x1080.png` - Minimap tap, camera focus within bounds, tap ripple, and updated viewport rectangle.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-08A_ObjectiveFocus_2400x1080.png` - Wide objective focus with HUD and world marker visible.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-08B_MinimapFocus_2400x1080.png` - Wide minimap focus with viewport update visible.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-09A_AssistantOpen_1920x1080.png` - ARIA open without blocking selected unit, command controls, objective panel, minimap, or critical markers.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-09B_AssistantShowMe_1920x1080.png` - ARIA Show Me highlighting the typed FTUE target without executing the command.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-09A_AssistantOpen_2400x1080.png` - Wide ARIA open state preserving tactical readability.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-09B_AssistantShowMe_2400x1080.png` - Wide Show Me guidance state without command execution.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-10A_AssistantDoIt_1920x1080.png` - ARIA Do It issuing a typed validated command intent with visible HUD result.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-10B_StopRecovery_1920x1080.png` - Stop recovery with selection retained, command mode cleared, and stale markers removed.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-10A_AssistantDoIt_2400x1080.png` - Wide Do It state showing assistant, command result, and tactical target.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-10B_StopRecovery_2400x1080.png` - Wide Stop recovery with clean HUD and marker state.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-11A_ObjectiveComplete_1920x1080.png` - Objective complete tick after patrol defeat with player squad alive.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-11B_ResultPopup_1920x1080.png` - `POP-05_MissionResult` M01 success popup after objective completion.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-11A_ObjectiveComplete_2400x1080.png` - Wide objective complete state with surviving squad and defeated patrol context.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-11B_ResultPopup_2400x1080.png` - Wide M01 result popup after confirmed objective completion.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_ContactSheet_1920x1080.png` - Contact sheet showing all 1920x1080 frames in numeric order from M01-00 through M01-11 with frame IDs.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_ContactSheet_2400x1080.png` - Contact sheet showing all 2400x1080 frames in numeric order from M01-00 through M01-11 with frame IDs.

## VisualLock Constraints

- Camera: every tactical frame must be true orthographic isometric with parallel ground-plane axes, no horizon, no vanishing point, no cinematic perspective convergence, and no wide-angle distortion.
- Map usage: use the tactical gameplay ground, not a baked strategic overview image, for M01-01 through M01-11.
- Map art separation: tactical ground must not bake units, markers, objective pulses, minimap viewport, HUD, tutorial UI, command paths, or ARIA overlays into the map art.
- Production plane: gameplay-facing mockups must respect XZ-plane movement and ground-aligned marker placement.
- Visual families: strategic map, tactical map, map tiles, player squad, enemy patrol, atlas states, markers, and scale/grounding must match `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/`.
- Unit scale: friendly and enemy soldiers must use one readable RTS scale relative to roads, doors, sidewalks, walls, and cover; reject floating, half-buried, squashed, cut-off, or perspective-rescaled units.
- Alive/dead state: idle, run, aim, and fire states are the only allowed living combat poses; death/destroyed states are only allowed after the patrol is defeated.
- Friendly read: player squad should keep restrained blue/cyan affiliation and coherent armor material without oversized UI glow.
- Enemy read: enemy patrol should use restrained hostile red markers or accents; do not over-tint the unit into a red blob.
- Marker read: selection, move, attack, enemy, objective, and hover markers must be ground-plane tactical feedback aligned to isometric axes; reject yellow squares, giant green markers, filled blobs, and screen-space icons pretending to be ground FX.
- HUD continuity: objective panel, command panel, selected unit panel, minimap, assistant, and log must stay readable and must not cover the tactical proof point of the frame.
- Build lock: Build is unavailable in M01; if any Build control appears, it must show `MissionDoesNotAllowBuild`.
- Command lock: allowed commands are Select, Move, Attack, Stop, and Hold.
- Reason-code lock: rejected command mockups may show only canonical reason codes: `NoSelection`, `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, and `CameraJumpUnavailable`.
- Rejected alias lock: do not show `InvalidTarget`, `BlockedRoute`, `OutOfRange`, or `BuildModeUnavailable`.
- ARIA lock: Show Me and Do It must use typed command intents; they must not be represented as raw screen-coordinate clicks.
- Result lock: result popup appears only after `Destroy hostile patrol` completes and the player squad survives.

## Art/Atlas Acceptance Checklist

- All required image filenames listed in this report exist under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`.
- Both required contact sheets exist and show frames in numeric order with visible frame IDs.
- Each tactical frame matches true orthographic isometric camera rules and the approved VisualLock visual quality.
- Strategic briefing uses approved strategic-map style and does not jump directly into combat.
- Tactical start shows no selected unit, no move/attack markers, objective panel visible, and minimap viewport matching camera start.
- Selection uses the approved ground selection ring and keeps command controls, selected panel, objective panel, and minimap readable.
- Move preview, commit, movement, and arrival show a grounded path, approved destination marker, readable travel, stable scale, persistent selection, and clean command recovery.
- Attack preview, commit, combat, and destroyed frames keep the enemy alive until defeat, avoid red-blob readability loss, and complete the objective only after patrol destruction.
- Invalid command recovery shows one canonical reason code, keeps selection, issues no order, and clears brief invalid feedback.
- Objective focus and minimap focus use typed/focused interactions, grounded objective pulse, bounded camera movement, tap ripple, and updated minimap viewport.
- ARIA open, Show Me, Do It, and Stop recovery preserve tactical readability, avoid raw screen-coordinate behavior, validate commands, and clear stale markers.
- Objective complete and result popup happen in order, with `POP-05_MissionResult` shown only after the player squad survives and the patrol is defeated.
- No frame bakes HUD, tutorial UI, units, markers, objective pulses, or minimap viewport into the map art.
- No frame lowers quality below the approved AAA isometric VisualLock package.
- Any deviation from this report is routed back to Designer/PM before Gameplay or QA/HCI work starts.

## Routing And Approval

Designer deliverable: complete

Next lane after Designer delivery: Art/Atlas

Implementation lanes held until approved mockups exist: Gameplay, QA/HCI

User approval required before project import or Gameplay implementation: yes
