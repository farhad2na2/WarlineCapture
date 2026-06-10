# Match HUD Transport Passenger Drawer Plan

Status: planning; implementation not started.
Updated: 2026-06-10

## Summary

Add a compact passenger drawer to the existing Match HUD selected panel so transport units can show who is onboard and expose explicit disembark actions.

The existing `Canvas (Environment) / SCN08_MatchHudContent / LeftContent / SelectedSquadPanel` remains the primary selected-unit panel. Transport selection must not replace or hide the current portrait, title, health, return, destroy, or board actions. Instead, transport-capable selections gain a small passenger capacity chip and an attached drawer that can be opened when the player wants details.

The drawer should reuse the same clean scroll-view pattern already present in:

`Canvas (Environment) / SCN09_BuildDrawerPopup / BuildDrawerRoot / DrawerFrame / RightPanel / ProductionPanel / ProductionPanelActive / Scroll View`

## User Behavior Contract

### Transport Selected

- Selected panel shows the normal selected transport content.
- A compact chip shows passenger count, for example `PASSENGERS 3/8`.
- Tapping the chip opens the passenger drawer.
- Tapping the chip again or tapping the drawer close button hides the drawer.
- Selecting another entity hides the drawer.

### Drawer Open

- Header shows `PASSENGERS 3/8`.
- If empty, the list shows a clear empty state: `NO PASSENGERS ONBOARD`.
- If passengers exist, each row shows:
  - passenger portrait
  - passenger name
  - passenger type/role
  - health bar and health text
  - individual `EXIT` button
- Footer shows:
  - `EXIT ALL`
  - `CLOSE`
- Visible rows should cap at roughly 3 rows and scroll for more passengers, matching mobile HUD density.

### Individual Exit

- Tapping a passenger row `EXIT` requests that passenger to disembark from the selected transport.
- The selected transport remains selected.
- The drawer stays open and updates as passengers exit.
- If disembark is invalid, show transient command feedback and keep the drawer open.

### Exit All

- Tapping `EXIT ALL` requests all valid passengers to disembark from the selected transport.
- The selected transport remains selected.
- The drawer stays open and updates to empty if all passengers exit.
- If only some passengers can exit, request valid exits, show a partial transient feedback message, and leave blocked passengers in the list.

## Visual Design

Use the SCN-08 target lock style from:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_16x9_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/scn08_selected_entity_panel_frame.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/scn08_health_bar_small_frame.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/scn08_small_square_button_frame.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/scn08_ability_chip_frame.png`

Recommended layout:

- Add the capacity chip inside or immediately attached to `SelectedSquadPanel`, below the selected title/order area and above the command buttons if space allows.
- Add the drawer as a narrow attached panel beside or below the selected panel, not as a modal popup.
- Drawer width should stay compact enough to avoid covering the command bar or center play space.
- Use the current black/gold frame language, yellow separators, green health, and red/amber disabled feedback.
- `EXIT` may be a small text button because this is a mobile HUD action and needs clear touch affordance.

## Architecture Rules

- Follow `Design/Architecture/gameplay_solid_ecs_contract.md`.
- UI views are passive serialized-reference holders only.
- No `Object.Find*`, `GameObject.Find`, runtime hierarchy string lookup, static mutable view registries, service locators, `Camera.main`, or direct gameplay mutation from UI.
- Do not rebuild UI prefabs wholesale.
- Add serialized references to the existing prefab in place.
- Reuse the Build Drawer scroll-view/item-view pattern where useful: serialized content root, serialized template item, pooled rows, and explicit row binding.
- UI clicks emit typed command intents/requests.
- Passenger read models are projected by systems from ECS state.
- Disembark execution belongs in transport command/request systems, not the view.

## Data Sources

Expected ECS/runtime sources:

| Data | Source/owner |
|---|---|
| Selected transport | Selection state / focused entity state |
| Capacity | `UnitTransportCapacitySystem` and transport capacity components/config |
| Passenger entities | `UnitTransportPassengerElement` dynamic buffer on transport |
| Passenger onboard state | `UnitTransportPassenger` on passenger |
| Passenger name/type | existing unit display info/config read model |
| Passenger portrait | existing portrait resolution, prefer card portrait then base portrait |
| Passenger health | existing unit health component/read model |
| Disembark validity | `SelectionTransportCommandRequestSystem` plus narrow transport query/rule systems |
| Rope/air disembark | `UnitTransportRopeDisembarkSystem` and existing air transport rules |

If any source is missing, add a narrow read-model helper or system. Do not let the UI inspect authoring components directly at runtime.

## Runtime Shape

### View Classes

Extend or add passive views:

- `MatchHudSelectionPanelView`
  - existing selected panel references remain
  - add serialized passenger chip root/button/label references
  - optionally hold a serialized `MatchHudTransportPassengerDrawerView`
- `MatchHudTransportPassengerDrawerView`
  - drawer root
  - header count label
  - empty-state root/label
  - scroll rect
  - viewport
  - content root
  - passenger row template
  - `EXIT ALL` button/label
  - `CLOSE` button/label
- `MatchHudTransportPassengerItemView`
  - root
  - portrait image
  - name text
  - role text
  - health fill image
  - health text
  - exit button
  - disabled/blocked state visuals

Allowed view behavior:

- Apply immutable read-model snapshots.
- Pool/reuse row instances under the serialized content root.
- Bind button callbacks supplied by the composition/binding layer.
- Show/hide roots and set text/images/interactable states.

Forbidden view behavior:

- ECS queries.
- Gameplay validation.
- Disembark mutation.
- Runtime hierarchy searches.

### Read Models

Add small immutable models:

```csharp
public readonly struct MatchHudTransportPassengersModel
{
    public readonly bool Visible;
    public readonly bool DrawerOpen;
    public readonly Entity Transport;
    public readonly int PassengerCount;
    public readonly int Capacity;
    public readonly bool ExitAllEnabled;
    public readonly FixedString64Bytes ExitAllDisabledReason;
    public readonly NativeList<MatchHudTransportPassengerItemModel> Passengers;
}
```

Implementation may use managed lists at the UI boundary if that matches existing UI code. The important contract is that the model is a snapshot and the view does not query gameplay state.

Each passenger item should include:

- stable passenger entity or request token
- portrait sprite
- display name
- role/type label
- health ratio
- health text
- exit enabled
- disabled reason

### Commands / Intents

Add or reuse explicit intents:

- `ToggleSelectedTransportPassengerDrawer`
- `CloseSelectedTransportPassengerDrawer`
- `DisembarkSelectedTransportPassenger`
- `DisembarkAllSelectedTransportPassengers`

The toggle/close may be UI-local state if no gameplay mutation is needed, but disembark requests must flow through ECS-aligned command systems.

Disembark request execution belongs in `SelectionTransportCommandRequestSystem` or a similarly narrow transport command system, consistent with the contract:

> transport boarding/disembark request consumption, boarding result marker payloads, focused transport disembark mutation, and transport command ECS result publication belong in `SelectionTransportCommandRequestSystem`.

## Feedback Rules

Use the existing Match HUD feedback lifetime system.

Persistent prompts are not needed just because the drawer is open. Drawer open state is self-explanatory.

Transient success:

- `Exiting unit.`
- `Exiting 3 units.`

Transient warnings/errors:

- `Transport is empty.`
- `Cannot exit while airborne.`
- `No safe exit point.`
- `Passenger cannot exit.`
- `Some passengers could not exit.`

Cancel/close should not show a feedback message.

## Implementation Plan

Mark each step complete only after its done criteria are verified.

- [ ] Step 01 - Inspect current selected panel and Build Drawer scroll-view structure
  - Inventory `SCN08_MatchHudContent.prefab` selected-panel hierarchy.
  - Inventory `SCN09_BuildDrawerPopup.prefab` production scroll-view references, template item behavior, masks, layout groups, and button wiring.
  - Done when the exact new serialized references and target prefab insertion point are documented in progress notes.

- [ ] Step 02 - Add transport passenger drawer UI plan to prefab without rebuilding
  - Add a compact passenger chip to the existing selected panel.
  - Add a drawer root with header, empty state, scroll view, content root, row template, `EXIT ALL`, and `CLOSE`.
  - Match SCN-08 target-lock frame styling and use the Build Drawer scroll-view setup as the layout reference.
  - Done when prefab diff is minimal and all new UI objects are explicitly named.

- [ ] Step 03 - Add passive view scripts
  - Extend `MatchHudSelectionPanelView` with serialized passenger chip/drawer references.
  - Add `MatchHudTransportPassengerDrawerView`.
  - Add `MatchHudTransportPassengerItemView`.
  - Done when scripts compile and contain only serialized references, apply methods, row pooling, and callback binding.

- [ ] Step 04 - Wire serialized prefab references
  - Assign chip, drawer, scroll, item template, labels, images, health bar, individual exit buttons, exit all, and close references.
  - Do not use runtime hierarchy lookup.
  - Done when prefab validation reports no missing references or missing scripts.

- [ ] Step 05 - Add selected-transport passenger read model
  - Project selected transport capacity and current passenger list from ECS state.
  - Resolve passenger name, role/type, portrait, health ratio, health label, and individual exit availability.
  - Hide the chip/drawer for non-transport selections.
  - Keep drawer state stable while the same transport remains selected.
  - Close drawer automatically when selection changes away from that transport.
  - Done when focused tests cover visible, empty, populated, and non-transport model states.

- [ ] Step 06 - Bind chip and close interactions
  - Chip toggles drawer open/closed.
  - Close hides drawer only.
  - Neither action mutates gameplay or changes selection.
  - Done when tests verify drawer open/close state and no command requests are emitted for close.

- [ ] Step 07 - Add individual exit request flow
  - Row `EXIT` emits an explicit disembark-passenger request.
  - Request carries selected transport and passenger identity.
  - ECS command system validates selection, transport, passenger membership, landed/exit eligibility, and safe exit cell.
  - Success queues the existing disembark behavior.
  - Failure emits transient feedback.
  - Done when tests cover valid exit, wrong transport/passenger, airborne transport, and no safe exit.

- [ ] Step 08 - Add Exit All request flow
  - `EXIT ALL` emits one bulk disembark request for the selected transport.
  - System expands request to valid passengers up to current passenger list.
  - Partial success is allowed and reports blocked passengers.
  - Empty transport disables button and/or shows transient `Transport is empty.` if tapped.
  - Done when tests cover empty, full success, partial blocked, and invalid transport.

- [ ] Step 09 - Update panel/drawer after disembark
  - Passenger list refreshes as ECS state changes.
  - Count chip updates immediately after state projection.
  - Drawer empty state appears when the last passenger exits.
  - Selected transport remains selected.
  - Done when tests verify `3/8 -> 2/8 -> 0/8` updates.

- [ ] Step 10 - Integrate feedback lifetime and command modes
  - Disembark messages are transient.
  - Closing drawer does not show feedback.
  - Opening drawer does not override active command prompts such as Board, Move, or Attack unless the command mode explicitly closes the drawer.
  - Switching active command modes closes the drawer if that avoids conflicting touch targets.
  - Done when tests verify feedback does not stick and Board/Move/Attack command visuals remain correct.

- [ ] Step 11 - Prefab and UI validation
  - Validate `SelectedSquadPanel`, passenger chip, drawer root, scroll view, viewport mask, row template, row buttons, `EXIT ALL`, and `CLOSE`.
  - Verify buttons are raycastable only on their visible button rects.
  - Verify drawer does not block bottom command buttons, minimap, or world panning outside its rect.
  - Done when prefab validation passes.

- [ ] Step 12 - Runtime validation in `WarlineCapture-CodexUnity1`
  - Select non-transport: chip hidden.
  - Select empty transport: chip shows `0/N`, drawer opens with empty state, `EXIT ALL` disabled.
  - Board soldiers into transport: chip count increases and rows appear.
  - Individual `EXIT` disembarks one passenger.
  - `EXIT ALL` disembarks all valid passengers.
  - Airborne/blocked exit shows transient error and keeps drawer state stable.
  - Run `git diff --check`.

## Test Plan

Add focused EditMode tests where practical:

- `MatchHudTransportPassengerDrawerView` applies empty model and hides row pool.
- Populated model creates/reuses rows without changing other row images when selecting a different row.
- Chip is hidden for non-transport selections.
- Chip count formats `current/capacity`.
- Drawer closes on selection change.
- Individual exit request contains the selected transport and passenger.
- Exit All request targets the selected transport only.
- Exit All skips invalid/non-member passengers.
- Disembark rejection uses transient feedback.
- No shipped code introduces forbidden hierarchy searches or direct gameplay mutation from UI.

## Open Questions

- Should individual passenger row tap select/focus the passenger after they exit, or should only the `EXIT` button be interactive in V1?
- Should `EXIT ALL` keep passengers near the transport in a defensive cluster, or use the existing rope/disperse behavior for all transport types?
- Should transport aircraft disembark be disabled while airborne, or should it use rope drop when a unit has valid rope-drop rules?
- Should the drawer remain open while Board mode is active, or should entering Board mode close the drawer to avoid crowded touch targets?

## Progress Notes

- 2026-06-10: Created plan. The implementation should reuse the Build Drawer production scroll-view pattern and route disembark through the existing transport command/request boundary.
