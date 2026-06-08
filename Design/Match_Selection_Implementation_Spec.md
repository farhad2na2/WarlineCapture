# WarlineCapture Match Selection Implementation Spec

Date: 2026-05-24

This is the canonical implementation contract for selecting units in `SCN-08 RTS Battle HUD`. Use this document when implementing, testing, reviewing, or explaining how a player selects units in a live match.

Parent match-screen source: `Match_HUD_And_Gameplay_Implementation_Spec.md` owns the full `SCN-08` match HUD contract for all buttons, panels, warnings, overlays, routes, and gameplay feedback. This file owns selection only.

Related sources:

- `UIUX_Gameplay_Element_Alignment.md` defines the high-level UI element roles.
- `Gameplay_UI_Integration_Handoff_Spec.md` defines `BattleHudGameplayBridge`.
- `M01_FirstContact_Production_Contract.md` defines the first tutorial mission exception rules.
- Runtime code currently lives under `RTSSelectionSystem`, `RtsSelectionInputSystem`, `SelectionStateSystem`, `SelectionUiQuerySystem`, `GamePointerInput`, and `BattleHudGameplayBridge`.

## Product Rule

Selection is a gameplay-owned state. The HUD can request selection mode or focus a squad card, but it must not own selected entities and must not write directly into child UI text or markers.

The required data flow is:

```text
Player input -> RTSSelectionSystem -> SelectionStateSystem / ECS refs -> SelectionUiQuerySystem -> BattleHudGameplayBridge -> HUD panels
```

The HUD update call is always one of:

```csharp
ApplySelection(displayName, status)
ClearSelection()
```

No gameplay system may update `SelectedEntityPanel/NameText`, command labels, selection rings, or command feedback by finding child UI paths directly.

## Selection Inputs

| Input | Required Result | Selection State | HUD Feedback | Notes |
|---|---|---|---|---|
| Tap/click friendly selectable unit in world | Select that unit or its owning squad/group. | Replace current selection with tapped unit/group. | Selection ring/highlight appears; selected panel calls `ApplySelection`; valid commands enable. | Primary mobile interaction. |
| Tap/click friendly squad card in squad tray | Select that squad/group. If the card is already selected, focus camera on that squad. | Replace current selection with squad card's ECS refs. | Squad card selected state; selected panel calls `ApplySelection`; second tap/focus behavior updates camera only. | Squad tray must call selection APIs, not mutate HUD directly. |
| Tap/click enemy with no selected controllable unit | Do not select enemy as controllable unit. | No selected player unit. | Reject with `NoSelection`; prompt player to select a squad first. | Enemy may show hover/intel only if a separate inspect mode exists. |
| Tap/click enemy with selected combat unit and no explicit `SELECT` mode | Issue direct attack if target is valid. | Keep current selection. | Attack target marker; command result feedback. | Direct attack is allowed after selection. |
| Tap/click walkable ground with selected movable unit and no explicit `SELECT` mode | Issue direct move order. | Keep current selection. | Move marker/path feedback; command result feedback. | Direct move is the primary mobile RTS path. |
| Tap/click empty ground with no selection and no explicit `SELECT` mode | No command. | No selected unit. | Optional low-priority `NoSelection` feedback only if the tap was interpreted as a command attempt. | Must not silently select nothing and then enable commands. |
| Click/tap `SELECT` HUD button | Enter explicit selection mode. | Clear active command targeting; preserve current selection until a new valid selection replaces it or an explicit clear-selection route clears it. | `SELECT` visual active state or selection-mode banner; map accepts tap/drag selection. | See state machine below. |
| Drag on battlefield while explicit selection mode is active | Box-select all eligible friendly units inside drag rectangle. | Replace current selection with eligible units in rectangle. | Live rectangle while dragging; selected rings after release; selected panel summarizes count/group. | Drag-select is available only after explicit `SELECT` unless a future gesture spec maps long-press to the same state machine. |
| Tap/click empty ground while explicit selection mode is active | Exit selection mode. | Preserve previous selection. | Selection-mode banner clears. | Must not issue a move and must not clear selection. Use an explicit clear-selection route for clearing. |
| Tap/click cancel/back while explicit selection mode is active | Exit selection mode. | Preserve existing selection unless the control is specifically a clear-selection route. | Selection-mode banner clears. | Must suppress the world click that triggered cancel. |
| Tap/click optional clear-selection affordance | Clear selected units. | No selected unit. | `ClearSelection`; command buttons disable or return to neutral. | There is no required dedicated `Deselect` button in the current HUD target. If a future route adds one, it must not issue a world command on the same input. |

## `SELECT` Button Contract

The `SELECT` button is not an auto-select button. It does not pick the nearest unit and it does not select all units by itself.

The `SELECT` button remains visible and selectable after a unit is selected, unless the current mission, tutorial step, modal, build placement, cutscene, or assistant takeover explicitly disables it. Selecting a unit directly by tapping it does not deactivate `SELECT`; it simply enables selected-unit commands such as `MOVE`, `ATTACK`, `HOLD`, `STOP`, and selected-unit abilities according to capability.

When enabled and clicked:

1. Suppress the current UI click from also becoming a world click.
2. Clear active command targeting mode: `MOVE`, `ATTACK`, `BUILD`, `SCAN`, `SUPPORT`, `SPECIAL`, `HOLD`, or `STOP` feedback must not remain active.
3. Set `SelectionModeActive = true`.
4. Show selection-mode feedback using the match HUD state, such as active `SELECT` chrome or a short command banner.
5. Wait for the next world selection input:
   - tap friendly unit selects that unit/group
   - drag rectangle selects all eligible friendly units in the rectangle
   - cancel/back exits selection mode
   - empty tap exits selection mode without issuing a move or clearing selection
6. After a valid selection or cancel, set `SelectionModeActive = false`.

When disabled:

- The button must have disabled/neutral visual state.
- The button must not enter selection mode.
- If the disabled state is visible to the player, a tooltip/toast/reason must explain why, unless the current tutorial step intentionally hides interaction.

## M01 First Contact Exception

M01 is the first tutorial mission. If the layout shows `SELECT`, it is visible but disabled while the player learns direct selection. If a narrower tutorial layout hides `SELECT`, no other visible selection-mode button may replace it.

M01 required behavior:

- `SELECT` button: disabled and neutral whenever visible.
- Primary select action: tap/click the friendly rifle squad in the world or tap its enabled squad card.
- Disabled `SELECT` click: no selection mode, no command, no world click leak.
- After selecting rifle squad: `ApplySelection("Rifle Squad" or authored display name, status)` updates the selected panel.
- Build and special controls remain disabled or hidden according to M01 scope.

M01 must not require the player to click `SELECT` before tapping the first squad.

## Clear Selection Rule

The current match HUD target does not require a dedicated on-screen `Deselect` button.

Default clear-selection rules:

- Tapping another friendly unit/card replaces the selection; it does not clear to no selection.
- Tapping the already selected squad card focuses the camera or confirms focus; it does not deselect.
- Tapping empty ground in normal mode does not deselect because selected movable units may treat ground taps as direct move orders.
- Tapping empty ground while explicit `SELECT` mode is active exits selection mode and preserves the previous selection.
- Back/cancel clears active command modes first. If no explicit command/selection/build/scan/support mode is active, the route may clear selection if the platform UX requires back-to-neutral behavior.
- Selection also clears when the selected unit/group is destroyed, becomes uncontrollable, leaves the map, mission ends, or a modal/result route resets match command state.

If a future design adds a visible `Deselect` affordance, it must call `ClearSelection()`, hide the selected panel, remove selection rings, disable selected-unit commands, and suppress the same input from issuing a world command.

## Selection State Machine

The selection state machine has these states:

| State | Meaning | Allowed Inputs | Exit |
|---|---|---|---|
| `NoSelection` | No controllable unit/group is selected. | Tap friendly unit, tap enabled squad card, tap enabled `SELECT`. | Friendly unit/card selects; `SELECT` enters explicit selection mode. |
| `Selected` | One or more controllable units/groups are selected. | Direct move, direct attack, command buttons, tap another friendly unit/card, `SELECT`, optional clear-selection route. | Clear-selection route clears; other friendly selection replaces; commands keep selection. |
| `SelectionModeActive` | HUD is waiting for tap/drag selection. | Tap friendly unit, drag rectangle, cancel/back, empty tap. | Valid select or cancel exits. Empty tap exits without move. |
| `MoveTargeting` | HUD is waiting for a move target after `MOVE`. | Tap walkable ground, cancel/back, `SELECT`, other command. | Valid target issues move; `SELECT` cancels move and enters selection mode. |
| `AttackTargeting` | HUD is waiting for attack target after `ATTACK`. | Tap valid enemy, cancel/back, `SELECT`, other command. | Valid target issues attack; `SELECT` cancels attack and enters selection mode. |
| `ScanTargeting` | HUD is waiting for a scan target after `SCAN`. | Tap valid area, cancel/back, `SELECT`, other command. | Valid target executes scan; `SELECT` cancels scan and enters selection mode. |
| `SupportTargeting` | HUD is waiting for support target after a support ability is chosen. | Tap valid area/unit, cancel/back, `SELECT`, other command. | Valid target executes support; `SELECT` cancels support and enters selection mode. |
| `BuildPlacement` | Build placement owns map clicks. | Placement confirm/cancel, `SELECT` only after exiting build. | Build cancel/confirm exits; selection input must not place buildings. |

Invalid mixed states are not allowed:

- `SelectionModeActive` and `MoveTargeting` cannot both be active.
- `SelectionModeActive` and `AttackTargeting` cannot both be active.
- `SelectionModeActive` and `ScanTargeting` cannot both be active.
- `SelectionModeActive` and `SupportTargeting` cannot both be active.
- UI clicks must never fall through as world move/attack/select actions.
- A disabled command button must never mutate selection or command state.

## Eligibility Rules

A world entity is selectable only if all required conditions are true:

- It belongs to the player or to a player-controllable allied group.
- It has a selectable/runtime entity reference.
- It is alive or in a controllable disabled-but-selectable state.
- It is not hidden by fog/intel rules that forbid command interaction.
- It is not a civilian, neutral prop, enemy-only intel marker, building ghost, projectile, VFX marker, or decorative mesh.
- The current mission does not explicitly ban selecting that unit class.

If multiple friendly selectable entities overlap under the tap:

1. Prefer currently highlighted/closest screen-space unit.
2. Prefer controllable combat units over passive support props.
3. Prefer squad representative/root over individual soldier mesh if the unit is represented as a squad.
4. Break ties by stable entity id in a repeatable order.

## Multi-Selection Rules

Box selection is allowed only in explicit selection mode. A future long-press gesture may enter explicit selection mode, but it must still use this same state machine and suppression rules.

Box selection must:

- Include only eligible friendly controllable units.
- Exclude enemies, civilians, neutral objects, dead units, building ghosts, and decorative props.
- Summarize the HUD selection as `{count} Units Selected`, `{count} Squads Selected`, or the dominant unit label.
- Enable only commands supported by every selected unit, or show mixed availability with clear disabled reasons.
- Keep individual selection rings on selected units, with group-level HUD summary.

If the box contains zero eligible units:

- Do not issue a move order.
- Exit selection mode.
- Preserve previous selection.

## HUD Enable Rules After Selection

After `ApplySelection`, command controls must use real selected-unit capability data.

| Control | Enabled When | Disabled Reason |
|---|---|---|
| `MOVE` | At least one selected unit can move and mission allows movement. | `NoSelection`, immobilized, mission restricted, invalid state. |
| `ATTACK` | At least one selected unit has an attack command and mission allows combat. | `NoSelection`, non-combat unit, disarmed, mission restricted. |
| `STOP` | Selected unit has active or interruptible orders. | `NoSelection`, no stoppable order. |
| `HOLD` | Selected unit can hold/defend position. | `NoSelection`, command unavailable. |
| `SCAN` | Mission scan rules allow scanning and required source/cooldown/charge/resource checks pass. Selection is not required by default. | Mission does not allow scan, scan unavailable, cooldown, no charges, insufficient resources, invalid target. |
| `SUPPORT` | Mission support rules allow support and at least one equipped support ability is available. Selection is not required by default unless the chosen support ability requires a selected unit or target. | Mission does not allow support, support unavailable, locked, cooldown, no charges, insufficient resources, invalid target. |
| `SPECIAL` | Selected unit/group has at least one available contextual ability. | Locked, cooldown, no charges, mission banned, invalid target requirement. |
| `BUILD` | Mission and selected context allow build/production. | Mission does not allow build, insufficient resources, no builder/producer, locked. |
| `SELECT` | Current mode supports explicit tap/drag selection. Remains enabled after direct unit selection unless blocked by mission/modal state. | Tutorial disabled, modal open, build placement owns input, cutscene/assistant takeover. |

Disabled controls must remain neutral and must not fire hidden gameplay effects.

## Input Suppression Rules

The implementation must guard against accidental double actions:

- A UI button press must call a suppression/capture path so the same pointer release does not issue move, attack, or select.
- Opening selection mode from `SELECT` must suppress the current world click.
- Selecting a unit from a world tap must suppress the release from becoming a move command.
- Canceling selection mode must suppress the cancel click from becoming a move command.
- Drag-select release must not also issue a move command.
- If the pointer began over blocking UI, no world action may fire on release.

## World Feedback

Selection feedback must be readable on the 3D operation map:

- Selected units receive a ground-anchored selection ring or equivalent readable highlight.
- Multi-selected units each show individual selection feedback.
- The selected HUD panel shows display name, status, and key combat state.
- Squad tray selected state mirrors the selected group.
- Selection feedback must not be baked into static HUD art.
- Selection feedback must not use 2D/isometric-only visual assumptions in production 3D scenes.

## Command Result Integration

Selection and commands must use typed bridge results.

Required bridge calls:

| Situation | Bridge Call |
|---|---|
| Valid selection | `ApplySelection(displayName, status)` |
| Selection cleared | `ClearSelection()` |
| Enter move targeting | `ApplyCommandMode(TacticalCommandMode.Move)` |
| Enter attack targeting | `ApplyCommandMode(TacticalCommandMode.Attack)` |
| Enter selection mode | Clear active command mode; selection-mode feedback is owned by the HUD controller. |
| Enter scan targeting | `ApplyCommandMode(TacticalCommandMode.Scan)` or equivalent typed mode. |
| Enter support targeting | `ApplyCommandMode(TacticalCommandMode.Support)` or equivalent typed mode. |
| Invalid command due no selection | `ApplyCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection))` |
| Invalid enemy target | `ApplyCommandResult(...TargetNotEnemy or TargetNotAttackable)` |
| Invalid ground target | `ApplyCommandResult(...TargetBlocked, TargetOutOfBounds, or TargetUnreachable)` |
| Invalid scan target/state | `ApplyCommandResult(...MissionDoesNotAllowScan, ScanUnavailable, TargetOutOfBounds, InsufficientResources, or AbilityOnCooldown)` |
| Invalid support target/state | `ApplyCommandResult(...MissionDoesNotAllowSupport, SupportUnavailable, TargetOutOfBounds, InsufficientResources, or AbilityOnCooldown)` |

## Implementation Ownership

| Responsibility | Owner |
|---|---|
| Read touch/mouse pointer | `GamePointerInput` |
| Track pointer press, drag, UI suppression, queued move | `RtsSelectionInputSystem` |
| Decide world selection, move, attack, hold, stop, scan, support targeting | `RTSSelectionSystem` and command-specific systems |
| Store focused/selected ECS refs | `SelectionStateSystem` |
| Build selected display name/status/read model | `SelectionUiQuerySystem` |
| Update match HUD selected panel and command feedback | `BattleHudGameplayBridge` |
| Render Canvas command buttons and selected panel | `Screen_MatchOverlay` controllers |
| Provide FTUE step gating and M01 exception state | M01/FTUE controllers |

## Acceptance Tests

At minimum, implementation or review should prove:

- Tap friendly unit selects it and calls `ApplySelection`.
- Tap squad card selects/focuses the matching squad and calls `ApplySelection`.
- Selection ring/highlight appears only for selected friendly controllable units.
- Tap selected unit's valid ground target issues move and keeps selection.
- Tap selected unit's valid enemy target issues attack and keeps selection.
- Tap enemy with no selection returns `NoSelection` and does not select enemy.
- Enabled `SELECT` enters `SelectionModeActive`.
- Disabled `SELECT` does nothing and cannot leak a world command.
- `SELECT` cancels active `MOVE`, `ATTACK`, `SCAN`, or `SUPPORT` targeting before entering selection mode.
- Drag in selection mode selects all eligible friendly units in the rectangle.
- Empty drag/tap in selection mode exits without issuing move.
- Empty tap in selection mode preserves the previous selection and does not deselect.
- There is no required dedicated `Deselect` HUD button in the current target; any future clear-selection affordance calls `ClearSelection()` and suppresses world input.
- UI clicks never fall through to world commands.
- M01 keeps `SELECT` disabled/neutral if the mission scope requires it, while direct squad/world selection remains available.

## Quick Answer

Player selection is:

```text
Tap friendly unit or squad card -> selection system stores selected ECS refs -> BattleHudGameplayBridge.ApplySelection updates HUD.
```

The `SELECT` HUD button is:

```text
Enter explicit selection mode -> next tap/drag on battlefield chooses units -> mode exits.
```

After direct unit selection:

```text
SELECT remains visible/selectable unless mission/modal state disables it. It is an optional precision-selection mode, not a prerequisite for normal tapping.
```

Clear selection:

```text
No dedicated Deselect button is required in the current HUD. Selection stays until replaced, cleared by back/cancel route, invalidated by gameplay, or reset by mission/modal/result flow.
```

In M01:

```text
SELECT button is disabled/neutral; player selects by tapping the friendly squad or squad card.
```

Squad tray note:

```text
The four roster cards are quick-select cards for active controllable groups, not command buttons. Current HUD slots are Rifle, APC, Tank, and Helicopter/Air Support; in M01 only Rifle is enabled.
```
