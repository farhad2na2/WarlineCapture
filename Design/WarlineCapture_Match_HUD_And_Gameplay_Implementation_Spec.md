# WarlineCapture Match HUD And Gameplay Implementation Spec

Date: 2026-05-24

This is the canonical implementation contract for the live match screen: `SCN-08 RTS Battle HUD` and its match-owned overlays. It covers player controls, panels, warnings, HUD state, command feedback, build/production drawer, command wheel, minimap/camera jumps, assistant hooks, pause/result routing, and the gameplay data each visible element must use.

Use this document before answering or implementing match-screen behavior. Lower-level child specs may add detail, but they cannot contradict this document.

Child specs:

- `WarlineCapture_Field_Logistics_Oil_Fuel_Design.md` - Oil/Fuel field logistics loop, Build Drawer resource rules, and tactical HUD fuel display rules.
- `WarlineCapture_Match_Selection_Implementation_Spec.md` - exact rules for unit selection, `SELECT`, squad cards, drag-select, input suppression, and M01 selection exceptions.
- `WarlineCapture_M01_FirstContact_Production_Contract.md` - M01-specific mission, FTUE, and tutorial-scope restrictions.
- `WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md` - current `BattleHudGameplayBridge` API and bridge wiring.
- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md` - high-level UI element matrix for all screens.

## Scope

This spec owns:

- `SCN-08 RTS Battle HUD`
- `SCN-09 Build Drawer / Production` when opened from the match HUD
- `SCN-10 Unit Command / Command Wheel` when opened from the match HUD
- `POP-07 Pause / Options` when opened during match
- `POP-05 Mission Result` when entered from match completion
- non-blocking match warnings, toasts, command banners, world markers, assistant match hooks, minimap/camera focus, and tutorial highlights

This spec does not own Main Menu, Campaign Map, Operations Dashboard, Store, Commander profile, or Settings except where those routes are entered from pause/result.

## Non-Negotiable Rules

1. Every visible match UI item must have a runtime owner, gameplay purpose, enabled/disabled rule, and feedback rule.
2. Canvas UI may request actions, but gameplay systems own gameplay state.
3. Gameplay systems must update the HUD through typed bridge/controller calls, not by writing directly to child object paths.
4. UI clicks must never leak into world clicks.
5. Disabled visible buttons must not mutate state and must expose an explicit disabled reason when the player can interact with them.
6. Match HUD art must be separated into background/chrome, icons, text, fills, meters, and markers. No gameplay text, progress, lock icon, star, health bar, or state icon may be baked into static panel art.
7. Production match presentation is full 3D single-map RTS. Do not add 2.5D/isometric-only assumptions to match behavior.

## Runtime Ownership Map

| Area | Runtime Owner | UI Owner | Notes |
|---|---|---|---|
| Pointer/touch input | `GamePointerInput`, `RtsSelectionInputSystem` | HUD buttons capture/suppress UI clicks | Touch and mouse use the same command semantics. |
| Selection | `RTSSelectionSystem`, `SelectionStateSystem`, `SelectionUiQuerySystem` | `BattleHudGameplayBridge`, selected panel, squad tray | See child selection spec. |
| Move/attack/hold/stop | `RTSSelectionSystem`, command-specific systems | Command bar, command wheel, command banner, world markers | Commands produce typed success/reject feedback. |
| Scan/support | intel, ability, support-call, cooldown/charge, and resource systems | Command bar, support/scan targeting overlay, command banner, world markers | `SCAN` reveals/updates battlefield intel; `SUPPORT` calls off-map or support abilities. |
| Build/production | `BuildingPlacementSystem`, `RuntimeBuildingSystem`, production services | Build button, build drawer, placement popup/panel | M01 disables build with reason. |
| Camera/minimap | camera system, minimap bridge/controller | minimap panel, objective/threat jump affordances | Jumps clamp to 3D map bounds. |
| Objectives/results | objective runtime, `WarlineCaptureMatchResultFlow` | objective panel, result popup | Match completion routes to `POP-05`. |
| Threat/warnings | `ThreatWarningRuntimeState`, objective/civilian systems | threat feed, toast, warning rows, world markers | Warnings must be typed by severity and source. |
| Assistant/FTUE | ARIA services, FTUE state, typed command intents | assistant entry, assistant panel dock, highlight layer | Assistant cannot click raw coordinates. |
| Pause/options | match route/session state | pause button, pause popup | Pause owns simulation pause/resume. |

## Match State Machine

| State | Meaning | HUD Behavior | Exit |
|---|---|---|---|
| `LoadingMatch` | Match scene/session loading. | Show loading/progress outside SCN-08 or blocked HUD shell. | Runtime ready. |
| `IntroFTUE` | Tutorial or intro overlay owns attention. | HUD visible only as instructed by FTUE; non-target controls disabled or blocked. | FTUE step complete/cancel. |
| `NoSelection` | No controllable unit selected. | Selected panel hidden; direct move/attack disabled or reject `NoSelection`; objective/minimap/pause remain available. | Tap friendly unit/card or explicit select mode. |
| `Selected` | One or more controllable units selected. | Selected panel visible; command buttons enable by capability. | Clear-selection route, new selection, death, mission end. |
| `SelectionModeActive` | HUD awaits tap/drag selection. | `SELECT` active feedback; move/attack targeting cleared; world tap/drag selects. | Valid selection, empty tap, cancel. |
| `MoveTargeting` | HUD awaits valid move target. | Move banner/marker preview visible; attack/build targeting cleared. | Valid target, cancel, another command, `SELECT`. |
| `AttackTargeting` | HUD awaits valid enemy target. | Attack banner/target highlight visible; move/build targeting cleared. | Valid target, cancel, another command, `SELECT`. |
| `ScanTargeting` | HUD awaits a valid scan area. | Scan radius/preview visible; move/attack/build targeting cleared. | Valid scan target, cancel, another command, `SELECT`. |
| `SupportMenuOpen` | Player is choosing a support ability. | Support choices own input; world clicks blocked until an ability enters targeting or menu closes. | Ability chosen, close, cancel, pause/result. |
| `SupportTargeting` | HUD awaits a valid support target. | Support radius/target preview visible; command targeting cleared. | Valid support target, cancel, another command, `SELECT`. |
| `BuildDrawerOpen` | Build/production drawer is open. | Drawer blocks world clicks; command targeting paused/cleared. | Close, build item chosen, pause/result. |
| `BuildPlacement` | Player is placing a building/structure. | Placement ghost/validity overlay owns map clicks; command selection cannot place. | Confirm, cancel, invalid reject. |
| `CommandWheelOpen` | Radial command wheel owns command selection. | Wheel blocks world clicks except its defined command target flow. | Segment chosen, close, cancel. |
| `Paused` | Pause popup owns input and simulation pause. | HUD dimmed/blocked; no world actions. | Resume, restart, settings, abandon. |
| `ResultShown` | Mission result owns input. | HUD blocked; result popup owns continue/replay. | Continue/replay/route. |

Invalid mixed states:

- `SelectionModeActive`, `MoveTargeting`, `AttackTargeting`, `ScanTargeting`, `SupportTargeting`, and `BuildPlacement` cannot be active at the same time.
- A modal popup and active world targeting cannot both accept input.
- A UI click can never also produce a world command on the same press/release.

## SCN-08 Required Hierarchy

The match overlay must expose stable object ids for tests and runtime binding:

```text
Screen_MatchOverlay
  ObjectivePanel
  ThreatFeedPanel
  ResourceBar / match resource strip when mission supports it
  PauseButton
  AssistantLayer
  SquadTray
  CommandBar
  BuildButton
  MiniMapPanel
  WorldCommandMarkerLayer
  SelectedEntityPanel
  CommandModeBanner
  InvalidCommandToast
  CommandWheelCanvas
  BuildDrawerCanvas
```

Names may be nested for layout, but public ids must remain discoverable through controllers/tests.

## Main HUD Element Contract

| Element | Purpose | Runtime Data | Interaction | Enabled / Visible Rule | Feedback |
|---|---|---|---|---|---|
| `ObjectivePanel` | Show active primary objective, star goals, progress, failure state. | `MissionConfig`, `ObjectiveRuntimeState`, active `ScenarioSetup`. | Tap objective row focuses objective anchor or opens detail if available. | Visible during active match. Rows hidden only if no objective exists. | Objective pulse, camera jump, reject `CameraJumpUnavailable` if anchor missing. |
| `ThreatFeedPanel` | Show live warnings and recent tactical events. | `ThreatWarningRuntimeState`, AI alerts, civilian risk, objective changes. | Tap actionable warning focuses source or opens detail. | Visible when mission supports warnings; may show empty/quiet state. | Severity color, short text, optional sound/VFX. |
| `ResourceBar` | Show match resources only when relevant. | `FactionResources`, population/capacity, tactical Credits, Oil/Fuel when active. | Read-only unless a resource detail route exists. | Hide or collapse in M01 if unused; show in base/build/fuel-logistics missions. | Resource delta/flyout on change. |
| `PauseButton` | Open pause/options. | Current match route/session state. | Opens `POP-07`. | Enabled during active match except non-interruptible loading/result transition. | Simulation pauses; HUD blocked by modal. |
| `AssistantLayer/AssistantEntryButton` | Open ARIA recommendation/help during match. | FTUE/recommendation state, assistant context provider. | Opens assistant panel dock or accepts typed recommendation actions. | Visible only when assistant is enabled for route/step. | Never clicks raw coordinates; uses typed intents. |
| `SquadTray` | Show controllable squads/groups. | selected/available unit groups, health, status, transport occupancy. | Tap squad card selects/focuses squad. | Cards enabled for alive/available controllable groups. Disabled cards show reason/lock. | Selected frame, health changes, unavailable state. |
| `CommandBar` | Primary command buttons. | selected unit capabilities, command state, mission restrictions. | Buttons request command modes/actions. | Visible during match; individual buttons enable by capability. | Active/disabled/pressed state; no silent inert buttons. |
| `BuildButton` | Open build/production flow. | build catalog, resources, mission allowed catalog. | Opens `BuildDrawerCanvas` or placement flow. | Enabled only when mission allows build and producer/build context is valid. | Disabled reason uses `MissionDoesNotAllowBuild`, resources, unlock, or no producer. |
| `MiniMapPanel` | Show tactical map overview and camera viewport. | minimap projection, operation-map bounds, camera state, known threats/objectives. | Tap/click map jumps camera; zoom buttons adjust minimap/camera if supported. | Visible once minimap data exists; viewport rect hidden only if unavailable. | Ripple/focus marker; reject `CameraJumpUnavailable` on missing data. |
| `WorldCommandMarkerLayer` | Render selection rings, move markers, attack markers, path previews, objective highlights. | selection state, move/attack orders, objective/threat anchors. | No direct UI input unless a marker is explicitly interactive. | Hidden when no markers exist or modal blocks world. | Markers are separate layers, never baked into world art. |
| `SelectedEntityPanel` | Show selected unit/group details. | selection read model: name, status, health, ability state, order state. | Tap/long-press may open unit detail/command wheel if route supports it. | Hidden in `NoSelection`; visible in `Selected`. | Calls `ApplySelection` / `ClearSelection`. |
| `CommandModeBanner` | Show active targeting/command mode. | `TacticalCommandMode`. | Tap cancel/back exits mode if a cancel affordance exists. | Visible only during active explicit command/selection/build/scan/support/special modes. | Text must match mode: move, attack, hold, stop, build, scan, support, special, select. |
| `InvalidCommandToast` | Explain rejected commands. | `TacticalCommandResult` and reason code. | Non-blocking; may auto-dismiss. | Visible only after rejected command or disabled button explanation. | Canonical reason text; no vague errors. |

## Squad Tray Four Quick-Select Cards

The four bottom roster cards are not command buttons. They are quick-select cards for the player's active controllable groups. They exist so the player can select/focus useful groups without hunting for them on the 3D map.

Assignment model:

- Campaign/Operations mission: the mission/loadout author defines the four important quick groups.
- Custom/Skirmish: the game dynamically recommends the four most useful command groups from the units currently on the field and the current tactical situation.

Canonical current card ids:

| Card Id | Player-Facing Meaning | Default Example | Click Result | Why It Exists | Enabled Rule | Disabled Rule |
|---|---|---|---|---|---|---|
| `SquadTray/Squad_Rifle` | Primary infantry squad slot. | Rifle Squad / command squad. | Select squad; second tap may focus camera. | Baseline command group for M01 and early missions. | Enabled when at least one alive/controllable infantry squad exists. | Disabled/hidden if no infantry squad is deployed; in M01 this is the only enabled card. |
| `SquadTray/Squad_APC` | Transport / light vehicle slot. | APC or troop carrier. | Select vehicle; second tap may focus camera; command wheel can expose load/unload. | Teaches mixed infantry/vehicle command and transport state. | Enabled when deployed vehicle is controllable and alive. | Disabled with reason such as `NotDeployed`, `Locked`, `Destroyed`, or `MissionUnavailable`. |
| `SquadTray/Squad_Tank` | Armor / heavy vehicle slot. | Tank or heavy armor group. | Select armor group; second tap may focus camera. | Gives fast access to high-value combat unit in larger battles. | Enabled when deployed armor is controllable and alive. | Disabled with explicit reason; do not show as a fake usable button. |
| `SquadTray/Squad_Helicopter` | Air/support slot. | Helicopter / air support group. | Select air/support unit; command wheel can expose transport, extract, or support commands. | Keeps air/rapid-response units accessible on large 3D maps. | Enabled when deployed air/support unit is controllable and mission allows it. | Disabled in M01 and missions without air/support; show lock/unavailable state if visible. |

Population rules:

- The four slots are a layout maximum for the current HUD target, not a requirement that every mission must deploy four unit types.
- Production data must populate cards from the active quick-group assignment. Do not hard-code Rifle/APC/Tank/Helicopter if the mission, loadout, or Skirmish recommendation uses different prefab-catalog units.
- Empty slots must either be hidden/collapsed or shown disabled with an explicit reason. A visible empty-looking card with no explanation is not allowed.
- If there are more than four controllable groups, the tray should show priority groups plus a paging/expand affordance or a command-group list in a later spec.
- Card portraits, health bars, rank/role icons, lock icons, and selected frames must be separate UI layers, not baked into the card background.

Campaign/Operations assignment rules:

- The mission, selected loadout, or operation event may author the four cards directly.
- A card selects only the group assigned to that slot, not every unit of that type on the entire map.
- Example: if the authored APC group contains three APCs, the APC card selects those three. Ten unrelated APCs elsewhere on the map are not selected unless they are part of that assigned command group.
- Authored quick groups should prioritize tutorial relevance, objective-critical units, transport units, high-value combat units, and support units that need fast access.

Custom/Skirmish dynamic recommendation rules:

- Skirmish has no authored mission quick-group list, so the match runtime assigns the four cards dynamically.
- The recommendation system should evaluate current controllable groups on the field, not the entire unit catalog.
- Recommended groups should prioritize:
  - currently selected or recently commanded group
  - groups under attack or near danger
  - groups near the active objective, enemy pressure, or civilian-risk area
  - high-value groups such as armor, air/support, transport, builders, repair, anti-air, or scouts
  - idle groups that need player attention
- The card selects the recommended group assigned to that slot, not all units of the same class globally.
- If the player has ten APCs split across the map, the runtime may recommend the most relevant APC group, such as the one under attack or closest to the objective. It must not automatically select all ten unless the player or runtime has explicitly formed them into one command group.
- If the player has airplanes instead of helicopters, the air/support slot should display the airplane group name and portrait. The slot meaning is `Air/Support`, not strictly helicopter.
- Temporary off-map abilities such as airstrike support are not squad-tray cards unless they represent a persistent controllable group. One-shot/off-map support belongs under `SPECIAL` or support command UI.
- If a recommendation changes, the UI must avoid surprising the player: update the card label/portrait clearly, and do not replace a card while the player is pressing it or while that group is actively selected.
- Later player pinning can lock a chosen group into a card. Pinned cards must not be replaced by dynamic recommendation until unpinned, destroyed, or mission state invalidates the group.

Dynamic card states:

| State | Meaning | UI Requirement |
|---|---|---|
| `Recommended` | Runtime chose this group because it is currently useful. | Show normal card with live group name/portrait/status. |
| `Pinned` | Player locked this group into the slot. | Show pin/lock indicator as a separate icon layer. |
| `UnderThreat` | Assigned group is taking damage or near critical threat. | Highlight card with warning accent and optional threat pulse. |
| `Unavailable` | Assigned group died, left map, is locked, or is no longer controllable. | Disable card with reason; do not silently select nothing. |
| `Empty` | Runtime has no useful group for this slot. | Hide/collapse or show disabled empty state with clear reason. |

Why soldiers and vehicles appear together:

- WarlineCapture is a mixed-arms 3D RTS. The tray is organized by controllable groups, not by button type.
- Infantry, transport, armor, and air/support cards give the player fast selection on a large map.
- The command bar below/near it issues actions to the selected card/unit; the squad tray only chooses who receives commands.

M01 tutorial rule:

- M01 may show the four-card layout for visual continuity, but only `Squad_Rifle` is enabled.
- `Squad_APC`, `Squad_Tank`, and `Squad_Helicopter` must be disabled/neutral and must not imply the player can deploy or command them in M01.
- If disabled cards are visible in M01, they need clear visual disabled treatment and optional reason feedback such as `Unlocks later`, `Not deployed`, or `Unavailable in tutorial`.

## Command Bar Buttons

Default visibility and clickability:

- The command bar remains visible during normal match play for layout stability and learnability.
- With no selected unit, `SELECT` remains visible and enabled when the current match allows explicit selection mode.
- With no selected unit, `MOVE`, `ATTACK`, `HOLD`, `STOP`, and selected-unit `SPECIAL` commands remain visible but disabled. If the disabled surface is tap-interactive, it must explain `NoSelection` with player-facing text such as `Select a squad first.`
- `BUILD`, `SCAN`, and `SUPPORT` may be enabled without a selected unit when the mission, resources, cooldown, charges, and target rules allow them. They are not unit-selection commands.
- Disabled visible buttons must not mutate command, selection, scan, support, or build state.

| Button | Click Result | Requires Selection | Enabled When | Disabled / Reject Reason | Required Feedback |
|---|---|---|---|---|---|
| `SELECT` | Enter explicit selection mode. | No | Match accepts explicit selection input. | Tutorial disabled, modal open, build placement owns input. | Active select state; current UI click suppressed. |
| `MOVE` | Enter move targeting; next valid ground tap issues move. | Yes | At least one selected unit can move. | `NoSelection`, immobilized, mission restricted, invalid state. | `ApplyCommandMode(Move)`, move banner/path preview. |
| `ATTACK` | Enter attack targeting; next valid enemy tap issues attack. | Yes | Selected unit has valid attack capability. | `NoSelection`, `TargetNotEnemy`, `TargetNotAttackable`, non-combat. | `ApplyCommandMode(Attack)`, target highlight. |
| `STOP` | Cancel the selected unit/group's current interruptible order immediately. Moving units stop where they are; attacking units stop attacking if the order can be interrupted; patrol/queued orders are cleared if stoppable. | Yes | Selection has active/interruption-capable order. | `NoSelection`, no stoppable order, command unavailable. | Immediate stop result or `ApplyCommandMode(Stop)` if the implementation requires confirmation; clear active targeting and update order/status text. |
| `HOLD` | Issue/toggle hold-position behavior for the selected unit/group. The unit stays near its current position and defends instead of chasing enemies far away. | Yes | Selected unit can hold/defend. | `NoSelection`, command unavailable, unit cannot hold. | Immediate hold result or `ApplyCommandMode(Hold)` if confirmation/targeting is required; show hold state on selected panel/card. |
| `SCAN` | Enter scan targeting or execute a mission-authored scan. The next valid map tap reveals/updates intel in that area: hidden enemies, suspect buildings, traps, patrol hints, objective clues, civilian risk, or minimap markers. | No by default | Mission allows scan and scan source/cooldown/charges/resources are valid. | `MissionDoesNotAllowScan`, `ScanUnavailable`, insufficient resources, cooldown, charges empty, target invalid/out of bounds. | `ApplyCommandMode(Scan)`, scan radius/preview, intel reveal marker/feed row, resource/cooldown update. |
| `SUPPORT` | Open support ability choices or enter support targeting for a selected support action. Examples: airstrike, smoke, med drone, supply drop, repair drone, evacuation, artillery, recon drone. | No by default; ability may require a selected target/unit | Mission allows support and at least one support ability is equipped/available. | `MissionDoesNotAllowSupport`, `SupportUnavailable`, locked, cooldown, no charges, insufficient resources, invalid target. | Open support menu or `ApplyCommandMode(Support)`, show support target preview, spend resources/charge only on accepted execution. |
| `SPECIAL` | Use selected contextual ability or open command wheel/detail. | Usually | Selected unit has available special ability. | Locked, cooldown, no charges, mission banned, invalid target. | Opens `SCN-10` or starts special targeting with reason text. |
| `BUILD` / build toggle | Open build drawer or build placement. | No, unless builder-selected mission requires it. | Mission allows build and catalog/context valid. | `MissionDoesNotAllowBuild`, insufficient resources, locked, no producer. | Open `BuildDrawerCanvas` or show disabled reason. |

M01 exception: `SELECT` may be visible but disabled/neutral; `SPECIAL` and `BUILD` are disabled/hidden according to M01 scope. M01 selection happens through direct world/squad-card selection.

`SCAN`, `SUPPORT`, and `SPECIAL` separation:

- `SCAN` is for information. It asks: what is hidden or uncertain in this area?
- `SUPPORT` is for off-map or auxiliary help. It asks: what external help do I want to call into this area?
- `SPECIAL` is for the selected unit/group's own contextual ability. It usually depends on the selected unit.
- Temporary one-shot support abilities should not appear as squad-tray quick-select cards unless they represent persistent controllable units.

## Direct World Commands

| Player Action | Required Behavior |
|---|---|
| Tap friendly unit/card | Select/focus unit or squad. |
| Selected unit + tap walkable ground | Issue direct move if no explicit selection/build/modal state owns input. |
| Selected combat unit + tap valid enemy | Issue direct attack if no explicit selection/build/modal state owns input. |
| No selection + tap ground/enemy | Reject with `NoSelection`; do not issue hidden command. |
| Scan targeting + tap valid area | Execute scan, spend scan cost/charge if accepted, reveal/update intel, then exit scan targeting unless the scan mode explicitly supports repeat use. |
| Support targeting + tap valid area/unit | Execute selected support action, spend cost/charge if accepted, show support marker/effect, then exit support targeting unless repeat use is explicitly allowed. |
| Tap UI while over HUD/popup | UI handles input; no world command. |

Detailed selection behavior is owned by `WarlineCapture_Match_Selection_Implementation_Spec.md`.

## Build Drawer / Production

`BuildDrawerCanvas` is part of match-owned UI when opened from `BuildButton`.

| Element | Purpose | Runtime Data | Interaction | Rule |
|---|---|---|---|---|
| `BuildDrawerCanvas/Scrim` | Block world input behind drawer. | Modal/drawer state. | Tap outside only closes if route explicitly allows. | Must suppress world click. |
| `HeaderBar/CloseButton` | Close drawer and return to HUD. | Drawer state. | Close drawer. | Always enabled once drawer is open. |
| Category tabs | Filter build/production catalog. | allowed catalog, unlocks, mission restrictions. | Select category. | Disabled tabs show no available items/locked reason. |
| Build item row | Preview unit/building/structure. | unit/building definitions, costs, build time, capacity. | Tap item or build button. | Enabled only if affordable/unlocked/allowed and producer/context valid. |
| Build item `BuildButton` | Start production or placement. | production queue or placement service. | Queue unit or enter placement. | Reject with typed reason; never silently fail. |
| Production queue row | Show queued/in-progress production. | queue state, progress, ETA. | Cancel if cancelable. | Progress fill separate from background art. |
| Queue cancel | Cancel queued production. | queue entry, refund rules. | Removes queue item and refunds per rules. | Disabled for non-cancelable entries. |
| Capacity panel | Show production/population capacity. | capacity/cap values. | Read-only. | Hide only if mission has no capacity system. |
| `RushAllButton` | Accelerate production where allowed. | rush tickets/resources, mission rules. | Spend and accelerate queue. | Disabled if no tickets/no rush/no active queue. |

Build placement mode must clear command targeting, block selection/move/attack world commands, show placement validity, and route invalid placement through typed feedback.

## Command Wheel

`CommandWheelCanvas` is an expanded command surface for selected units.

| Element | Purpose | Runtime Data | Interaction | Rule |
|---|---|---|---|---|
| Scrim | Own wheel focus and block world click. | Wheel open state. | Tap outside closes only if configured. | No world click leak. |
| Selected entity card | Confirm command target unit. | selected read model. | Read-only or opens unit details. | Must match selected HUD panel. |
| Move segment | Start move targeting. | movement capability. | Same as `MOVE`. | Disabled with reason if not movable. |
| Attack segment | Start attack targeting. | combat capability. | Same as `ATTACK`. | Disabled with reason if no attack. |
| Stop/Hold segment | Stop or hold. | command capability/order state. | Same as command bar. | Immediate command or reject. |
| Extract / Load / Unload / Rope Drop | Transport actions. | transport/boarding state. | Start typed transport command. | Disabled unless transport conditions valid. |
| Patrol / Breach / Special segments | Advanced context actions. | ability/skill availability. | Start typed command/targeting. | Disabled with explicit reason. |
| Close button | Close wheel. | Wheel state. | Return to HUD. | Does not clear selection. |

Wheel segments and command-bar buttons must share the same capability/reason-code model.

## Minimap And Camera Jumps

| Input | Required Behavior |
|---|---|
| Tap minimap playable area | Convert minimap position to operation-map coordinate and focus camera inside bounds. |
| Tap minimap outside known projection | Reject `CameraJumpUnavailable`; do not move camera. |
| Zoom in/out | Adjust camera/minimap zoom within tier bounds. |
| Tap objective row | Focus objective anchor if available. |
| Tap threat feed actionable warning | Focus warning source if available, otherwise open detail/tooltip if designed. |

Camera jumps must never leave the playable 3D operation-map bounds. Missing anchors are defects unless explicitly marked unavailable with reason feedback.

## Objective, Threat, Warning, And Civilian Feedback

| Feedback Type | Source | Surface | Required Rule |
|---|---|---|---|
| Primary objective progress | objective runtime | Objective panel | Shows current value, target, complete/fail state. |
| Star goal progress | objective/star runtime | Objective panel | Shows progress or hidden if mission has no star goal. |
| Objective failure risk | objective runtime | Objective panel + threat feed | Warning severity escalates before failure when possible. |
| Enemy threat alert | AI/threat runtime | Threat feed, world marker, optional toast | Includes source, severity, and camera-focus target if actionable. |
| Civilian risk warning | civilian/operation runtime | Threat feed, warning toast, result consequence | Must distinguish risk, casualty, evacuation, and neutral tutorial outcome. |
| Base/building under attack | building/combat runtime | Threat feed, world marker, audio cue | Focusable if anchor exists. |
| Invalid command | command result | Invalid toast | Uses canonical reason code text. |
| Resource/capacity warning | build/economy runtime | Build drawer, resource bar, toast | Names missing resource/capacity. |
| Tutorial prompt | FTUE runtime | assistant/tutorial card/highlight layer | Blocks or highlights only intended controls. |

Warning severity:

| Severity | Use | UI Treatment |
|---|---|---|
| `Info` | Mission start, neutral update, completed minor step. | Low emphasis; no blocking. |
| `Caution` | Enemy spotted, path issue, low resource. | Yellow/gold accent; optional sound. |
| `Critical` | Unit dying, civilian risk, base under attack, objective failure risk. | Red/high contrast; camera focus available. |
| `Blocked` | Player command cannot proceed. | Invalid toast with typed reason. |

## Pause And Result Routing

| Route | Trigger | Owns Simulation? | Required Buttons |
|---|---|---|---|
| `POP-07 Pause / Options` | Pause button, platform back/menu. | Pauses match until resume/route. | Resume, Restart if supported, Settings, Abandon/Exit with confirmation. |
| `POP-05 Mission Result` | Victory, partial success, defeat, withdrawal, or operation-resolved result flow. | Match ended or frozen. | Continue/retry/return route, Replay if supported, reward/stat rows. |
| Settings from pause | Pause menu action. | Simulation remains paused. | Back returns to pause, not directly to live match unless explicitly resumed. |
| Abandon/Exit | Pause menu action. | Ends or leaves match after confirmation. | Confirmation required if progress can be lost. |

Result popup must follow `Design/WarlineCapture_Mission_Result_State_Spec.md`. It must show objective outcome, failure/success reason, stars, combat stats, civilian/district consequence, rewards, and next route. It must not show fake rewards or baked values; all reward rows bind to result/reward services.

## Assistant And FTUE Match Rules

ARIA/FTUE may:

- highlight HUD controls and world targets
- open assistant panel dock
- recommend the next valid command
- execute typed `Show Me` or `Do It` command intents

ARIA/FTUE must not:

- click raw screen coordinates
- bypass command capability checks
- issue commands while player has a modal/pause/result state open
- hide cancel/resume affordances during assistant control takeover
- mutate selection or command state outside the same runtime APIs used by player input

M01 FTUE target ids are owned by `WarlineCapture_M01_FirstContact_Production_Contract.md`.

## Canonical Command Result Reason Codes

| Reason Code | Used When | Text Direction |
|---|---|---|
| `NoSelection` | Command requires selected controllable unit. | Select a squad first. |
| `TargetOutOfBounds` | Target is outside operation-map bounds. | Target outside mission area. |
| `TargetBlocked` | Terrain/cell/object blocks command. | Path blocked. |
| `TargetUnreachable` | No path or command route exists. | Cannot reach that point. |
| `TargetNotEnemy` | Attack target is not hostile. | Choose an enemy target. |
| `TargetNotAttackable` | Target cannot be attacked by selected unit. | Target cannot be attacked. |
| `CommandUnavailable` | Capability/order not available. | Command unavailable for this unit. |
| `MissionDoesNotAllowBuild` | Build pressed in a no-build mission. | Building unlocks in a later mission or this mission does not allow building. |
| `MissionDoesNotAllowScan` | Scan pressed in a no-scan mission. | Scanning is not available in this mission. |
| `MissionDoesNotAllowSupport` | Support pressed in a no-support mission. | Support is not available in this mission. |
| `ScanUnavailable` | Scan source, charge, cooldown, or target is invalid. | Scan unavailable. |
| `SupportUnavailable` | Support ability, charge, cooldown, source, or target is invalid. | Support unavailable. |
| `CameraJumpUnavailable` | Objective/minimap/threat focus anchor missing or invalid. | No valid map focus. |
| `InsufficientResources` | Resource/currency/capacity shortfall. | Name missing resource and amount if known. |
| `AbilityOnCooldown` | Special ability cooldown active. | Show cooldown remaining. |
| `TransportUnavailable` | Load/unload/extract cannot proceed. | Explain capacity, range, or passenger requirement. |

Reason codes should be enums or typed ids, not free-text-only errors.

## M01 First Contact Restrictions

M01 is a tutorial-scoped match and intentionally does not expose all match HUD functions.

| Item | M01 Behavior |
|---|---|
| Objective panel | Shows destroy hostile patrol objective and star goal. |
| Threat feed | Shows mission start and relevant combat/tutorial updates only. |
| Squad tray | `Squad_Rifle` enabled; `Squad_APC`, `Squad_Tank`, and `Squad_Helicopter` disabled/neutral if visible, with no click-through command behavior. |
| `SELECT` | Disabled/neutral if visible; player selects directly through world squad or squad card. |
| `MOVE` | Disabled until rifle squad selected; then usable/direct move taught. |
| `ATTACK` | Disabled until rifle squad selected; then usable/direct or explicit attack taught. |
| `STOP` / `HOLD` | Visible according to target; enabled only when selection/order state supports them. |
| `SCAN` | Hidden or disabled unless the M01 tutorial explicitly teaches scanning. |
| `SUPPORT` | Hidden or disabled unless the M01 tutorial explicitly teaches a support ability. |
| `SPECIAL` | Hidden or disabled; no M01 special command. |
| `BUILD` | Hidden or disabled; reason `MissionDoesNotAllowBuild`. |
| Build drawer | Not entered from M01. |
| Command wheel | Hidden unless specifically used by a later FTUE step. |
| Minimap/objective jump | May be enabled if anchors exist; otherwise returns `CameraJumpUnavailable`. |
| Result | Routes to `POP-05` with M01 result/reward data. |

## Acceptance Tests

Focused match implementation must prove:

- Every public HUD object id exists or is intentionally route-scoped.
- Objective panel binds active mission objective data.
- Threat feed shows typed warning rows and supports camera focus/reject behavior.
- Pause button opens `POP-07`, blocks world input, and resumes correctly.
- Squad card selection updates selection state and selected panel.
- Command buttons obey selected-unit capability and disabled reasons.
- Direct move/attack and explicit move/attack share validation and feedback.
- `SELECT` behavior follows `WarlineCapture_Match_Selection_Implementation_Spec.md`.
- `SCAN` enters scan targeting only when mission, resource, cooldown, charge, and target rules allow it; otherwise it returns typed disabled/reject feedback.
- `SUPPORT` opens support choices or support targeting only when mission, equipped support ability, resource, cooldown, charge, and target rules allow it; otherwise it returns typed disabled/reject feedback.
- Build button opens build drawer only when mission/build context allows it.
- Build drawer controls bind catalog/queue/capacity data and block world input.
- Command wheel segments share the same capability/reason-code model as command bar.
- Minimap/objective/threat camera jumps clamp to map bounds and reject missing anchors.
- Invalid command toast uses canonical reason code text.
- World markers are separate visual layers and update from gameplay state.
- Assistant `Show Me` / `Do It` uses typed intents and can be canceled.
- M01 restrictions match the table above.
- Result flow opens `POP-05` with real objective/stats/reward/consequence data.

## Quick Answer

The match screen is governed by this hierarchy:

```text
SCN-08 Match HUD
  Objective / Threat / Resources / Pause
  Squad Tray / Selected Panel
  Command Bar / Command Wheel
  Build Button / Build Drawer
  Minimap / World Markers / Warnings
  Assistant / FTUE
  Pause Popup / Result Popup
```

Selection is one child system. The full match HUD contract is this file; use the child selection spec only for the detailed select/tap/drag rules.
