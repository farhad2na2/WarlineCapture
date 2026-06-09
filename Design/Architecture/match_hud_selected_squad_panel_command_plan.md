# Match HUD Selected Squad Panel Command Plan

## Goal

Upgrade `Canvas (Environment) / SCN08_MatchHudContent / LeftContent / SelectedSquadPanel` so the panel is a complete selection command surface, not only a portrait display.

When a unit, squad, vehicle, or building is selected, the panel must update:
- portrait
- title
- subtitle
- current order
- health bar
- health text
- character badge
- Return action
- Destroy action
- Board action

The implementation must follow `Design/Architecture/gameplay_solid_ecs_contract.md`: UI `*View` classes are raw serialized reference holders only, user actions enqueue ECS/data requests, and gameplay decisions stay in `*System` classes.

## Current Findings

- `Assets/Game/Scripts/UI/Components/MatchHudSelectionPanelView.cs` currently only references `selectedSquadPanel` and `selectedPortraitImage`.
- `SelectionHudFeedbackSystem` currently toggles the selected panel and portrait, but it does not publish a full panel read model.
- `SelectionUiCommandSystem` already queues selection commands and has `RequestDestroyFocusedUnit()`, but it does not expose Return or focused nearest-transport Board requests.
- `SelectionInputRequestComponents.cs` already contains `ReturnToBase`, `BoardTransport`, and `DestroyFocusedUnit` intent kinds.
- `FocusedUnitCommandSystem` already has `ReturnFocusedUnitToBase(...)` and `DestroyFocusedUnit(...)`.
- Existing transport ownership is split across `TransportBoardingCommandSystem`, `SelectionTransportCommandRequestSystem`, `UnitTransportBoardingQuerySystem`, `UnitTransportBoardingRuleSystem`, `UnitTransportCapacitySystem`, `UnitTransportApproachCellSystem`, and air/rope transport systems.

## Architecture Rules For This Feature

- Do not add new classes ending in `Controller`, `Presenter`, `Bridge`, `Manager`, or `Button`.
- Do not put gameplay policy in `MatchHudSelectionPanelView`.
- Do not use runtime hierarchy path lookup such as `transform.Find("Frame/Title")`.
- Add serialized fields for title/order/action references on the prefab.
- Button clicks must call narrow methods that enqueue ECS command intents; command behavior belongs in systems.
- Order text must be produced by a read-model system, not by the view.
- Result messages must flow through existing HUD feedback/result systems where possible.

## User Behavior Contract

### Unit, Squad, Or Vehicle Selection

- Title shows the selected unit/squad/vehicle name.
- Subtitle shows the selected unit/squad/vehicle role, type, or short description.
- Health bar and health text show current health for a focused unit/vehicle, or aggregated squad health when multiple units are selected.
- Badge updates for character/soldier selections.
- Badge is hidden for vehicles, transport vehicles, aircraft, and buildings.
- Current order shows a plain readable status:
  - `Idle`
  - `Moving`
  - `Returning to base`
  - `Boarding <transport name>`
  - `In transport`
  - `Engaging target`
  - `No active order`
- Return sends the selected unit or selected squad members to their assigned base/home.
- Destroy destroys the focused selected unit/vehicle through the gameplay lifecycle.
- Board is available only when the selected focused entity is a transport vehicle or aircraft.
- For transport vehicles or aircraft, Board calls the nearest eligible soldiers to board that selected transport.
- Non-transport vehicles, normal soldier units, and squads without a focused transport must not show or enable Board.

### Building Selection

- Title shows the selected building name.
- Subtitle shows the selected building role, type, or short description.
- Health bar and health text show current building health.
- Badge is hidden for buildings.
- Current order summarizes building-owned unit orders:
  - `No unit orders`
  - `<count> units returning`
  - `<count> units boarding <transport name>`
  - `<count> units boarding across <transport count> transports`
  - `Mixed unit orders`
- Return calls the units belonging to the building to return to that building/base.
- Destroy destroys the selected building through the building runtime destruction path.
- Board calls the units belonging to the building to board the nearest valid transport vehicle or aircraft.
- If one transport has enough free capacity, all eligible units board together.
- If one transport does not have enough capacity, units split across the nearest valid transports by capacity.
- If no transport can take them, show a clear rejected command message.

## Implementation Checklist

- [x] Create this trackable implementation plan.
- [x] Audit current `MatchHudSelectionPanelView`, `SelectionHudFeedbackSystem`, `SelectionUiCommandSystem`, and selection/transport command ownership.
- [x] Add a plain read model for the panel.
- [x] Expand `MatchHudSelectionPanelView` with serialized references only.
- [x] Add Unity `Button` components directly to the existing Return, Destroy, and Board panel roots.
- [x] Wire prefab references for title, subtitle, current order, health bar, health text, character badge, Return, Destroy, and Board.
- [x] Add UI action binding without gameplay policy in the view.
- [x] Add Return and Board request methods to `SelectionUiCommandSystem`.
- [x] Route Return, Destroy, and Board through ECS command-intent processing.
- [x] Implement selected-unit/squad Return command behavior.
- [ ] Implement selected-building Return command behavior.
- [x] Implement selected-transport Board command behavior that calls nearest eligible soldiers to board it.
- [x] Ensure non-transport vehicles, normal units, and non-transport squads do not expose Board.
- [ ] Implement selected-building nearest-transport Board command behavior with capacity split.
- [x] Ensure Destroy supports focused unit/vehicle and selected building through existing lifecycle systems.
- [x] Update current-order read model for units, squads, vehicles, and buildings.
- [x] Add command result messages for accepted and rejected Return/Destroy/Board actions.
- [ ] Add focused EditMode validation for panel model and missing prefab references.
- [x] Run focused compile/Unity validation.
- [ ] Mark completed items in this document as each implementation slice lands.

## Implementation Slice 2026-06-09

Completed:

- Added `MatchHudSelectionPanelView.Model` and serialized references for title, subtitle, current order, health fill/text, badge, and Return/Destroy/Board action surfaces.
- Bound Return/Destroy/Board panel-root buttons through `SelectionGameplayStartupSystem` to `SelectionUiCommandSystem` request methods.
- Added `RtsSelectionCommandIntentKind.BoardNearestSoldiers` so selected-transport boarding does not collide with the existing click-to-transport `BoardTransport` command path.
- Routed Return and selected-transport Board through `RtsSelectionFocusCommandSystem`.
- Added panel projection for focused units, squads, and selected buildings.
- Added focused unit/squad Return, focused unit Destroy, selected-tag unit/squad Destroy fallback, selected building Destroy, and focused transport Board-nearest-soldiers behavior.

Known gaps:

- Selected-building Return and selected-building Board with capacity splitting are not complete because `SelectionGameplayStartupSystem.Initialize(...)` currently receives only `BuildingPlacementInteractionSystem.Context`, which exposes selected-building label/delete but not the richer produced-unit ownership and building health data from `BuildingUiQuerySystem.Context`.
- Selected-building health currently displays `Health: -` until the richer building query context is passed into this selection startup path.
- Badge visibility is wired for character selections, but no new badge sprite resolver has been added; the existing prefab badge art is preserved when visible.

Validation status:

- Main Unity batch compile passed on 2026-06-09 using Unity `6000.4.0f1`.
- Log scan found no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Aborting batchmode` entries.
- Destroy fallback fix revalidated with main Unity batch compile on 2026-06-09; log scan found no C# errors or warnings.
- `WarlineCapture-CodexUnity2` remains unsuitable for this specific validation until its stale/mismatched source state is cleaned up.

## Step 1 - Panel Read Model

Add a small data model owned by systems, for example:

```csharp
public readonly struct MatchHudSelectionPanelModel
{
    public readonly bool Visible;
    public readonly Sprite Portrait;
    public readonly string Title;
    public readonly string Subtitle;
    public readonly string CurrentOrder;
    public readonly float Health01;
    public readonly string HealthText;
    public readonly Sprite BadgeSprite;
    public readonly bool BadgeVisible;
    public readonly bool ReturnEnabled;
    public readonly bool DestroyEnabled;
    public readonly bool BoardEnabled;
}
```

The exact type can live in UI/component code if it is purely visual data, or in selection read-model code if it needs ECS fixed-string storage. It must not execute gameplay decisions.

## Step 2 - View Reference Expansion

Extend `MatchHudSelectionPanelView` as a serialized-reference holder:

- `GameObject selectedSquadPanel`
- `Image selectedPortraitImage`
- `TMP_Text titleText`
- `TMP_Text subtitleText`
- `TMP_Text currentOrderText`
- `Image healthFillImage`
- `TMP_Text healthText`
- `GameObject badgeRoot`
- `Image badgeImage`
- `UnityEngine.UI.Button returnAction`
- `UnityEngine.UI.Button destroyAction`
- `UnityEngine.UI.Button boardAction`

The existing `ReturnButton`, `DestroyButton`, and `BoardButton` panel roots are the clickable surfaces. They must keep their `Button` components directly on those panel roots and must not use hidden child hotspots.

Allowed methods:

- `Apply(MatchHudSelectionPanelModel model)`
- `SetSelectionVisible(bool visible)`
- `SetSelectionPortrait(Sprite portraitSprite)`
- `BindActions(Action onReturn, Action onDestroy, Action onBoard)`
- `ClearActions()`

The view may set text, sprite, active state, and interactable state. It must not choose what command means.

## Step 3 - System-Owned Model Projection

Add a narrow `*System` such as `MatchHudSelectionPanelModelSystem`.

Responsibilities:

- Read focused unit/squad/building selection.
- Resolve portrait using the existing portrait resolver.
- Resolve title using `SelectionUiQuerySystem` or building query systems.
- Resolve subtitle using unit/building description, role, or type data.
- Resolve current order using ECS components.
- Resolve health ratio and health text using existing unit/building health components and query systems.
- Resolve character badge sprite for character/soldier selections.
- Hide badge for vehicles, transport vehicles, aircraft, and buildings.
- Resolve action availability.
- Push the model to `MatchHudSelectionPanelView`.

Do not duplicate gameplay mutation here. This system is read-model projection only.

## Step 4 - UI Command Requests

Extend `SelectionUiCommandSystem` with:

- `RequestReturnToBase()`
- `RequestBoardNearestTransport()`
- Existing `RequestDestroyFocusedUnit()` may be renamed only if needed, but avoid broad churn.

Each method:

- calls `CaptureUiClickSequence()` when appropriate
- respects `IsGameplayInputLocked()`
- queues an ECS command intent/request

## Step 5 - Return Command

Unit/squad Return:

- Use existing focused/selected unit data.
- Use `FocusedUnitCommandSystem.ReturnFocusedUnitToBase(...)` where applicable.
- For multi-selection, add a selected-unit return system if existing code only supports one focused unit.
- Accepted message examples:
  - `Returning <unit name> to base.`
  - `Returning 4 units to base.`
- Rejected message examples:
  - `No selected unit.`
  - `<unit name> has no assigned base.`

Building Return:

- Resolve selected building.
- Gather units belonging to that building using the existing production/ownership link.
- Issue return orders to those units.
- Accepted message examples:
  - `Calling 3 units back to <building name>.`
- Rejected message examples:
  - `<building name> has no assigned units.`

## Step 6 - Destroy Command

Unit/vehicle Destroy:

- Keep using existing focused-unit destruction where valid.
- Clear focus/selection after successful destruction.
- Publish a command result.

Building Destroy:

- Route through the building runtime destruction/lifecycle system, not direct UI object destruction.
- Clear selected building and update the panel after successful destruction.
- Publish a command result.

Message examples:

- `Destroyed <name>.`
- `No selected unit or building.`
- `<name> cannot be destroyed.`

## Step 7 - Board Command

Focused transport Board:

- Resolve the focused selected transport vehicle or aircraft.
- Reject if the focused entity is not a valid transport.
- Find nearest eligible player soldiers that are not already boarded and can board this transport.
- Use existing transport boarding rule/query/capacity systems.
- Respect the selected transport capacity.
- Issue boarding orders through the same ECS command/result path used by transport boarding.
- Hide or disable the Board action for normal units, non-transport vehicles, and squads without a focused transport.

Building Board:

- Resolve building-owned units.
- Find nearest valid transport vehicles/aircraft.
- Prefer one transport if it has enough capacity for all eligible units.
- Otherwise split units across nearest transports by free capacity.
- Reject if no transport has capacity.

Message examples:

- `Calling <unit name> to board <transport name>.`
- `Calling 5 soldiers to board <transport name>.`
- `Boarding 8 units across 2 transports.`
- `<name> is not a transport.`
- `No transport with free capacity nearby.`
- `No eligible soldiers to board.`

## Step 8 - Current Order Text

Add order text projection after command processing and when selection changes.

Unit order sources should check, in priority order:

1. Passenger/in-transport component.
2. Boarding target component.
3. Return-to-base/home movement component or return intent.
4. Attack/engage target component.
5. Move/path request component.
6. Idle fallback.

Building order sources should aggregate building-owned unit order states:

1. Return orders.
2. Boarding orders.
3. In-transport state.
4. Mixed active orders.
5. No active unit orders.

## Step 9 - Prefab Wiring And Validation

Wire the scene/prefab references:

- `Title` text under `SelectedSquadPanel`.
- `Subtitle` text under `SelectedSquadPanel`.
- `CurrentOrder` text under `SelectedSquadPanel`.
- health fill image under `SelectedSquadPanel`.
- health text under `SelectedSquadPanel`.
- `Frame / Badge` root and badge image under `SelectedSquadPanel`.
- `ReturnButton`
- `DestroyButton`
- `BoardButton`
- existing `PortraitFrame` image.

Add editor validation so missing references fail before runtime.

Validation should check:

- `MatchHudSelectionPanelView` has every serialized field assigned.
- Return/Destroy/Board click wiring is not duplicated after rebind.
- Hidden state on scene launch.
- Unit selection shows title/subtitle/order/portrait/actions.
- Building selection shows title/subtitle/order/portrait/actions.
- Unit, squad, vehicle, and building selections show correct health bar fill and health text.
- Character selections show the correct badge.
- Vehicle, aircraft, transport, and building selections hide the badge.

## Step 10 - Focused Validation Scenarios

Run these focused checks after implementation:

- Select one unit: panel visible, portrait/title/subtitle/order update, actions enabled.
- Select one unit: health bar and health text match current/max health.
- Select one character/soldier: badge is visible and updated.
- Select one vehicle/transport/aircraft: badge is hidden.
- Select squad: health bar and health text use the agreed aggregate health model.
- Select one building: panel visible, building title/subtitle/order/health update, actions enabled where valid.
- Select one building: badge is hidden.
- Clear selection: panel hidden.
- Return selected unit: unit receives return order and current order updates.
- Return selected building: building-owned units receive return orders and current order summarizes count.
- Board selected transport: nearest eligible soldiers are called to board the selected transport.
- Board selected non-transport unit/vehicle: Board is hidden or disabled.
- Board selected building with enough capacity in one transport: all eligible units target same transport.
- Board selected building without enough capacity in one transport: units split across transports.
- Board with no capacity: command rejected with proper message.
- Destroy selected unit/building: selection clears and panel hides or updates.

## Progress Log

| Date | Status | Notes |
| --- | --- | --- |
| 2026-06-09 | Planned | Documented architecture-aligned feature plan and current code findings. |
| 2026-06-09 | Partial prefab setup | Added `Button` components directly to `ReturnButton`, `DestroyButton`, and `BoardButton` panel roots in `SCN08_MatchHudContent.prefab`. Command wiring is still pending. |
