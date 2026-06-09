# Match HUD Command Feedback Panel Plan

## Goal

Design the command feedback behavior for:

`Canvas (Environment) / SCN08_MatchHudContent / FooterContent / FeedbackPanel / Frame`

The panel contains:

- `Icon`
- `Feedback`

This is the general Match HUD command feedback strip. It should give immediate contextual guidance and result feedback for command buttons such as Move, Attack, Hold, Stop, Scan, Build, Return, Destroy, and Board.

The behavior should mirror the Build Drawer instruction strip pattern from `Design/Architecture/build_drawer_instruction_strip_plan.md`: a short message plus one of four severity icons.

## Architecture Rules

- Do not put gameplay policy in the UI view.
- Do not use runtime hierarchy lookup. Add serialized fields for `FeedbackPanel`, `Feedback`, and `Icon`.
- UI buttons emit ECS command intents only.
- Command systems validate selected units/buildings, target legality, capacity, resources, and cooldowns.
- Command systems publish command feedback through ECS/result data or a narrow injected HUD feedback context.
- The view only renders a resolved feedback model:
  - visible/hidden
  - message text
  - severity icon
  - optional timeout/sticky behavior
- This plan must align with:
  - `Design/Architecture/gameplay_solid_ecs_contract.md`
  - `Design/Architecture/match_hud_explicit_command_mode_plan.md`
  - `Design/Architecture/match_hud_attack_command_mode_plan.md`
  - `Design/Architecture/match_hud_selected_squad_panel_command_plan.md`

## Feedback Severities

Use four command feedback severities:

- `Neutral`: informational/default state.
- `Ready`: command mode is armed or command is available.
- `Warning`: command succeeded but changed state, cancelled something, or has a non-fatal limitation.
- `Error`: command cannot run or selected/targeted object is invalid.

## Icon Assets

The command feedback panel can reuse the existing Build Drawer instruction icon style initially, or use command-specific copies later if art direction requires it.

Initial implemented assets:

- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Info_512.png` for `Neutral`
- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Ready_512.png` for `Ready`
- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Warning_512.png` for `Warning`
- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Error_512.png` for `Error`

These are the same severity icons used by the Build Drawer instruction strip.

If the panel needs separate assets later, create:

- `Assets/Game/Art/UI/Icons/Icon_CommandFeedback_Info_512.png`
- `Assets/Game/Art/UI/Icons/Icon_CommandFeedback_Ready_512.png`
- `Assets/Game/Art/UI/Icons/Icon_CommandFeedback_Warning_512.png`
- `Assets/Game/Art/UI/Icons/Icon_CommandFeedback_Error_512.png`

Import settings:

- Texture type: Sprite.
- Sprite mode: Single.
- Wrap mode: Clamp.
- Mipmaps: Off.
- 512x512 project copy.

## Visibility Rules

- Hidden by default when no command mode or result message is active.
- Show as sticky while a target command mode is armed:
  - Move
  - Attack
  - Scan
  - Build placement
- Show briefly for immediate command results:
  - Hold
  - Stop
  - Return
  - Destroy
  - Board
  - Production/build failures routed to HUD
- Error feedback should stay visible long enough to read, then clear unless a command mode remains armed.
- When a command mode exits successfully, clear the panel unless a success message is explicitly requested.
- When an invalid target is clicked for a still-armed mode, keep the mode instruction after the error timeout.

## Command Feedback Model

Preferred model:

```csharp
public readonly struct MatchHudCommandFeedbackModel
{
    public readonly bool Visible;
    public readonly CommandFeedbackSeverity Severity;
    public readonly FixedString128Bytes Message;
    public readonly TacticalCommandMode CommandMode;
    public readonly float TimeoutSeconds;
    public readonly bool Sticky;
}
```

The exact storage can use ECS components/buffers or a shell-edge DTO, but runtime state must not be stored in a static active view registry.

## Default Messages

- No active command: hidden.
- Selection mode armed: `Select units or a building.` (`Neutral`)
- Selection changed with no command: hidden.
- Gameplay input locked during intro/loading: `Command unavailable during deployment.` (`Warning`)
- UI or command system not ready: `Command system is connecting.` (`Warning`)

## Move Command

### Unit Or Squad Selected

- Move button clicked and movable unit(s) selected: `Choose destination.` (`Ready`)
- Move destination accepted for one unit: `Moving {unitName}.` (`Ready`)
- Move destination accepted for multiple units: `Moving {count} units.` (`Ready`)
- Double-click destination accepted and Move remains armed: `Moving. Choose another destination.` (`Ready`)
- Destination blocked: `Route is blocked.` (`Error`)
- Destination unreachable: `Destination is unreachable.` (`Error`)
- Destination outside map: `Destination is outside the playable area.` (`Error`)
- Selected unit cannot move: `{unitName} cannot move.` (`Error`)

### Vehicle Or Aircraft Selected

- Ground vehicle ready: `Choose destination for {vehicleName}.` (`Ready`)
- Aircraft ready: `Choose landing or patrol destination for {aircraftName}.` (`Ready`)
- Transport vehicle ready: `Choose destination for {transportName}.` (`Ready`)
- Destination accepted: `Moving {vehicleName}.` (`Ready`)
- Aircraft destination accepted: `{aircraftName} moving to target area.` (`Ready`)
- Invalid landing destination: `No valid landing area there.` (`Error`)

### Building Selected

- Move clicked with selected building: `Buildings cannot move.` (`Error`)
- Building placement active: `Drag placement, then confirm.` (`Ready`)

## Attack Command

### Unit Or Squad Selected

- Attack clicked with attack-capable unit(s): `Tap hostile target.` (`Ready`)
- Attack accepted for one unit: `{unitName} attacking {targetName}.` (`Ready`)
- Attack accepted for squad/group: `{count} units attacking {targetName}.` (`Ready`)
- Clicked friendly target: `Cannot attack friendly units.` (`Error`)
- Clicked civilian or neutral target: `Target is not hostile.` (`Error`)
- Clicked non-attackable object: `Target cannot be attacked.` (`Error`)
- Target out of range but reachable: `Moving to attack range.` (`Ready`)
- Target unreachable: `Target is unreachable.` (`Error`)
- No valid target under click: `Tap a hostile target.` (`Error`)

### Vehicle Or Aircraft Selected

- Combat vehicle ready: `Tap hostile target for {vehicleName}.` (`Ready`)
- Attack helicopter ready: `Tap hostile target for {aircraftName}.` (`Ready`)
- Jet ready: `Tap strike target.` (`Ready`)
- Transport selected: `{transportName} has no attack command.` (`Error`)
- Unarmed vehicle selected: `{vehicleName} cannot attack.` (`Error`)
- Attack accepted: `{vehicleName} attacking {targetName}.` (`Ready`)

### Building Selected

- Armed defensive building ready: `Tap hostile target for {buildingName}.` (`Ready`)
- Defensive building attack accepted: `{buildingName} engaging {targetName}.` (`Ready`)
- Non-combat building selected: `{buildingName} cannot attack.` (`Error`)
- Target outside building range: `Target is outside weapon range.` (`Error`)

## Hold Command

### Unit, Squad, Vehicle, Or Aircraft Selected

- Hold accepted for one unit: `{unitName} holding position.` (`Ready`)
- Hold accepted for multiple units: `{count} units holding position.` (`Ready`)
- Combat vehicle hold accepted: `{vehicleName} holding position.` (`Ready`)
- Aircraft hold accepted in air: `{aircraftName} holding area.` (`Ready`)
- Transport hold accepted: `{transportName} holding position.` (`Ready`)
- No selection: `Select units to hold position.` (`Error`)
- Selected object cannot hold: `{name} cannot hold position.` (`Error`)

### Building Selected

- Defensive building hold accepted: `{buildingName} holding defensive area.` (`Ready`)
- Non-defensive building selected: `Buildings already hold position.` (`Neutral`)

## Stop Command

### Unit, Squad, Vehicle, Or Aircraft Selected

- Stop accepted for one unit: `{unitName} is idle.` (`Warning`)
- Stop accepted for multiple units: `{count} units stopped.` (`Warning`)
- Stop accepted for vehicle: `{vehicleName} stopped.` (`Warning`)
- Stop accepted for aircraft: `{aircraftName} stopping current order.` (`Warning`)
- No selection: `Select units to stop.` (`Error`)
- Nothing to stop: `{name} has no active order.` (`Neutral`)

### Building Selected

- Building production unaffected: `Buildings do not use Stop. Use Cancel in production.` (`Neutral`)
- Building-owned units stopped, if implemented later: `Stopped {count} units from {buildingName}.` (`Warning`)

## Scan Command

Scan is an intel command and does not require selected units.

- Scan clicked and available: `Tap scan area.` (`Ready`)
- Scan accepted: `Scanning area.` (`Ready`)
- Hidden enemies found: `Intel updated: {count} contacts revealed.` (`Ready`)
- Nothing found: `Scan complete. No contacts found.` (`Neutral`)
- Invalid scan area: `Cannot scan that area.` (`Error`)
- Scan on cooldown: `Scan cooling down.` (`Error`)
- Insufficient resources: `Insufficient resources for scan.` (`Error`)
- Mission/game mode has no scan: `Scan unavailable.` (`Error`)

If a unit/building is selected, keep it selected and show scan feedback without changing the selected panel.

## Build Command And Build Placement

- Build clicked: `Choose what to build, produce, or recruit.` (`Neutral`)
- Build Drawer opened: `Build menu opened.` (`Neutral`)
- Building item selected and ready: `Choose a location for {buildingName}.` (`Ready`)
- Placement valid: `Confirm placement for {buildingName}.` (`Ready`)
- Placement blocked: `Cannot place here: {reason}.` (`Error`)
- Building placed: `{buildingName} placed.` (`Ready`)
- Placement cancelled: `Placement cancelled.` (`Warning`)
- Not enough credits: `Insufficient credits.` (`Error`)
- Missing producer: `Required producer is missing.` (`Error`)

The Build Drawer instruction strip owns detailed Build Drawer guidance. The command feedback panel should show only short global command state/result messages.

## Return Command

Return is initiated from:

`Canvas (Environment) / SCN08_MatchHudContent / LeftContent / SelectedSquadPanel`

### Unit Or Squad Selected

- Return accepted for one unit: `{unitName} returning to base.` (`Ready`)
- Return accepted for multiple units: `{count} units returning to base.` (`Ready`)
- Unit has no assigned base: `{unitName} has no assigned base.` (`Error`)
- No selected unit: `Select units to return.` (`Error`)
- Unit already returning: `{unitName} is already returning.` (`Neutral`)

### Vehicle Or Aircraft Selected

- Vehicle return accepted: `{vehicleName} returning to base.` (`Ready`)
- Aircraft return accepted: `{aircraftName} returning to base.` (`Ready`)
- Transport return accepted: `{transportName} returning to pickup area.` (`Ready`)
- No return point: `{vehicleName} has no return point.` (`Error`)

### Building Selected

- Building-owned units return accepted: `Calling {count} units back to {buildingName}.` (`Ready`)
- No owned units: `{buildingName} has no assigned units.` (`Error`)
- No valid return target: `{buildingName} cannot receive returning units.` (`Error`)

## Destroy Command

Destroy is initiated from the selected panel. It is not the same as Attack.

### Unit, Vehicle, Or Aircraft Selected

- Destroy accepted for one unit: `Destroyed {unitName}.` (`Warning`)
- Destroy accepted for selected group: `Destroyed {count} selected units.` (`Warning`)
- No selected unit: `Select a unit or building to destroy.` (`Error`)
- Protected unit: `{unitName} cannot be destroyed.` (`Error`)
- Unit already destroyed: `{unitName} is already destroyed.` (`Neutral`)

### Building Selected

- Destroy accepted: `Destroyed {buildingName}.` (`Warning`)
- Protected building: `{buildingName} cannot be destroyed.` (`Error`)
- City/neutral building not commandable: `{buildingName} is not commandable.` (`Error`)
- No selected building: `Select a building to destroy.` (`Error`)

## Board Command

Board is initiated from the selected panel.

### Transport Vehicle Or Transport Aircraft Selected

- Board clicked with valid transport and one soldier: `Calling {unitName} to board {transportName}.` (`Ready`)
- Board clicked with valid transport and multiple soldiers: `Calling {count} soldiers to board {transportName}.` (`Ready`)
- Transport full: `{transportName} is full.` (`Error`)
- No eligible soldiers: `No eligible soldiers to board.` (`Error`)
- Transport cannot board right now: `{transportName} cannot board units now.` (`Error`)
- Selected object is not transport: `{name} is not a transport.` (`Error`)

### Soldier Or Non-Transport Vehicle Selected

- Board button should be hidden or disabled.
- If clicked through stale binding: `Select a transport to call boarding.` (`Error`)

### Building Selected

- One transport can fit all units: `Calling {count} units to board {transportName}.` (`Ready`)
- Multiple transports needed: `Boarding {count} units across {transportCount} transports.` (`Ready`)
- No building-owned units: `{buildingName} has no assigned units.` (`Error`)
- No transport nearby: `No transport with free capacity nearby.` (`Error`)
- Not enough capacity: `Not enough transport capacity.` (`Error`)

## Selection Cases

- No selection and command requires selection: `Select units or a building first.` (`Error`)
- Selected civilian/non-commandable unit: `{name} is not commandable.` (`Error`)
- Selected neutral/city building: `{name} is not commandable.` (`Error`)
- Mixed selected group with partial command support:
  - Move: `Moving {validCount} units. {skippedCount} cannot move.` (`Warning`)
  - Attack: `{validCount} units attacking. {skippedCount} cannot attack.` (`Warning`)
  - Hold: `{validCount} units holding. {skippedCount} ignored.` (`Warning`)
  - Stop: `Stopped {validCount} units. {skippedCount} had no order.` (`Warning`)

## Reason-Code Mapping

Initial mapping from existing reason codes:

- `None`: use command-specific success text.
- `NoSelection`: `Select units or a building first.` (`Error`)
- `TargetOutOfBounds`: `Target is outside the playable area.` (`Error`)
- `TargetBlocked`: `Route is blocked.` (`Error`)
- `TargetUnreachable`: `Target is unreachable.` (`Error`)
- `TargetNotEnemy`: `Target is not hostile.` (`Error`)
- `TargetNotAttackable`: `Target cannot be attacked.` (`Error`)
- `CommandUnavailable`: `Command unavailable.` (`Error`)
- `BuildUnavailable`: `Build command unavailable.` (`Error`)
- `CameraJumpUnavailable`: `Camera focus unavailable.` (`Warning`)
- `ScanUnavailable`: `Scan unavailable.` (`Error`)
- `ScanCooldown`: `Scan cooling down.` (`Error`)
- `InsufficientResources`: `Insufficient resources.` (`Error`)

Add new reason codes when implementing Return/Destroy/Board if needed:

- `NotCommandable`
- `NoAssignedBase`
- `NoEligiblePassengers`
- `TransportFull`
- `NoTransportCapacity`
- `ProtectedTarget`

## Implementation Steps

- [x] Step 01: Add this design document.
- [x] Step 02: Add `CommandFeedbackSeverity` and a command feedback model/result type.
- [x] Step 03: Add serialized `FeedbackPanel`, `Feedback`, and `Icon` references to the Match HUD feedback view.
- [x] Step 04: Assign the four severity icon sprites on `SCN08_MatchHudContent.prefab`.
- [x] Step 05: Replace hard-coded command feedback text with a command feedback formatter using this matrix.
- [x] Step 06: Route Move, Attack, Hold, Stop, Scan, and Build results into the command feedback model.
- [x] Step 07: Route selected-panel Return, Destroy, and Board results into the same command feedback model.
- [x] Step 08: Add tests for mode messages, success messages, rejected reason-code mapping, and missing prefab references.
- [x] Step 09: Run focused Unity validation and verify the panel is hidden by default.

## Validation Scenarios

- Select soldier, click Move: icon is Ready, message is `Choose destination.`
- Click Move with no selection: icon is Error, message asks for selection.
- Select rifle squad, click Attack: icon is Ready, message is `Tap hostile target.`
- Click friendly/civilian target in Attack mode: icon is Error, message says target is not hostile or cannot be attacked.
- Select transport, click Board: icon is Ready or Error based on eligible soldiers/capacity.
- Select building, click Return: icon is Ready if owned units exist, Error otherwise.
- Select building, click Destroy: icon is Warning on accepted destruction, Error if protected/not commandable.
- Click Scan with no selection: icon is Ready, message is `Tap scan area.`
- Stop/Hold with no selection: icon is Error.
- Feedback panel is hidden on match start and after command completion timeout.

## Progress Notes

- 2026-06-09: Design document created. Next step is implementation planning for the feedback model and serialized Match HUD references.
- 2026-06-09: Implemented severity-aware command feedback model, SCN08 feedback icon wiring, result severity mapping, selected-panel Return/Destroy messages, Hold/Stop messages, and focused EditMode coverage for ready/error/warning icon routing. Remaining: run focused Unity validation.
- 2026-06-09: Focused Unity EditMode validation passed: `MatchHudCommandFeedbackPanelTests` with 2/2 tests covering runtime severity icon routing and SCN08 prefab serialized references.
- 2026-06-09: Kept the command feedback panel on the same four BuildDrawer severity icons used by the Build Drawer instruction strip, added runtime diagnostics for SCN08 icon application, and corrected the serialized SCN08 icon Image default to enabled + neutral info.
