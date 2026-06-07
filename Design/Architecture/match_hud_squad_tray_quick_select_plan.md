# Match HUD Squad Tray Quick-Select Plan

## Summary

Turn the five panels under `Canvas (Environment) / SCN08_MatchHudContent / FooterContent / SquadTray / Frame` into tactical quick-select buttons.

The tray should select useful unit groups without rebuilding the HUD hierarchy:

- Card 1 selects up to 4 soldiers.
- Card 2 selects up to 2 non-transport combat vehicles.
- Card 3 selects 1 non-transport helicopter.
- Card 4 selects 1 jet.
- Card 5 selects 1 APC or transport aircraft.

Clicking the currently selected card again cycles to another valid squad of the same type.

## Design Goals

- Keep the existing Match HUD visual hierarchy stable.
- Treat the cards as quick-select presets, not detailed unit status cards.
- Make selection deterministic and readable.
- Keep UI SOLID-compliant: the view emits card-click events, while gameplay systems own selection decisions.
- Do not mutate gameplay camera, transforms, or selection state directly from the UI view.
- Avoid rebuilding the full Match HUD prefab for this feature.

## User-Facing Behavior

- Each of the five card panels becomes a button.
- Only the active card uses the selected frame sprite.
- Inactive cards use the normal frame sprite.
- Clicking an inactive card selects the best matching squad for that category.
- Clicking the active card again cycles to the next valid squad for the same category.
- If no valid unit exists for a card, the card should give subtle disabled feedback and should not remain selected.
- Before applying a squad selection, any currently selected units or buildings are deselected through the existing selection path.

## Squad Rules

### Card 1: Soldiers

- Select up to 4 soldier units.
- Prefer soldiers currently inside the gameplay camera viewport.
- If no soldiers are in the viewport, select a nearby coherent cluster of soldiers roughly around viewport-scale distance.
- If fewer than 4 soldiers exist, select as many as available.
- Do not select more than 4.

### Card 2: Combat Vehicles

- Select up to 2 closest combat vehicles.
- Exclude transport vehicles.
- Exclude APCs if they are transport-capable.
- If fewer than 2 valid vehicles exist, select as many as available.

### Card 3: Attack Helicopter

- Select 1 closest helicopter.
- Exclude transport helicopters.

### Card 4: Jet

- Select 1 closest jet.

### Card 5: APC Or Transport Aircraft

- Select 1 APC or transport aircraft.
- Include transport helicopters or transport planes if they are represented as transport-capable units.

## Candidate Ranking

- Prefer units inside the gameplay camera viewport.
- Rank candidates by distance to viewport/camera center.
- For soldier groups, prefer a coherent cluster instead of four unrelated globally nearest units.
- For fallback soldier selection outside the viewport, find a good anchor soldier near the camera area, then choose nearby soldiers around that anchor.
- Cycling should avoid the previous selected entities when possible, then wrap predictably when alternatives are exhausted.

## UI Architecture

### MatchHudSquadTrayView

Add a serialized view component on the squad tray frame or equivalent stable parent.

Serialized references:

- Five card buttons.
- Five frame images.
- Five portrait roots.
- Normal frame sprite.
- Selected frame sprite.
- Optional feedback target for pulse/disabled feedback.

The runtime view must use serialized references and should not search the hierarchy in shipped code.

### Portrait Display

- Keep the authored/static category portrait on each squad card.
- Do not replace squad tray card portraits with selected-unit portraits.
- Selection feedback belongs to the card frame state and the existing selection HUD, not the tray portrait art.
- Do not add health/status overlays to the squad tray.

## Gameplay Architecture

### Squad Tray Selection System

Add a dedicated runtime system/service that:

- Receives squad card click requests from the UI view.
- Queries valid unit candidates from ECS state.
- Applies the category-specific selection rule.
- Clears current unit/building selection through existing selection APIs.
- Applies the new unit selection through the existing ECS selection state.
- Tracks active card and cycling state.

### Unit Classification

Classification should be based on existing runtime config/identity data where possible:

- Soldier: infantry soldier configs, excluding civilians/non-combatants.
- Combat vehicle: ground vehicle, non-air, non-transport.
- Attack helicopter: air helicopter, non-transport.
- Jet: air jet/plane, non-transport unless explicitly categorized otherwise.
- Transport: APC or unit with transport capacity / transport config identity.

If the current runtime entity data does not expose enough stable identity, add a lightweight runtime category/reference during unit spawn instead of relying on fragile name parsing everywhere.

## Implementation Checklist

- [x] Inspect existing selection APIs and runtime unit identity components.
- [x] Confirm exact prefab hierarchy and sprite assignments for `SquadCard1` through `SquadCard5`.
- [x] Add `MatchHudSquadTrayView` with serialized references.
- [x] Wire five existing card panels as buttons without changing the visible hierarchy.
- [x] Add squad-category enum and click request flow.
- [x] Add candidate query/classification helper.
- [x] Implement soldier viewport and fallback cluster selection.
- [x] Implement combat vehicle, helicopter, jet, and transport selection.
- [x] Implement cycling state and wrap behavior.
- [x] Apply selections through existing selection state APIs.
- [x] Update selected/normal frame visuals.
- [x] Preserve static/authored squad card portraits during selection.
- [x] Add subtle cycle/disabled feedback.
- [x] Add prefab serialization tests.
- [ ] Add selection query tests.
- [ ] Add cycling tests.
- [ ] Add static portrait preservation tests.
- [ ] Run targeted EditMode tests.
- [ ] Validate in Unity play mode.

## Progress Notes

- 2026-06-07: First implementation pass added `MatchHudSquadTrayView`, `MatchHudSquadTraySelectionSystem`, shell/runtime binding, prefab button wiring, and prefab serialization tests.
- 2026-06-07: Unity editor domain reload completed with no C# compile errors in the editor log. Batchmode EditMode validation was blocked because the project was already open in Unity.
- 2026-06-07: Corrected squad tray portrait behavior so card portraits remain static/authored; selection only changes the card frame state.

## Test Plan

- Prefab test confirms all five squad card buttons exist and are serialized on `MatchHudSquadTrayView`.
- Prefab test confirms normal and selected sprites are assigned.
- Selection tests cover:
  - Soldier selection from viewport.
  - Soldier fallback cluster when none are visible.
  - Fewer-than-required soldier counts.
  - Combat vehicle selection excluding transport vehicles.
  - Helicopter selection excluding transport helicopters.
  - Jet selection.
  - APC/transport aircraft selection.
- Cycling tests verify repeated clicks avoid previous selections when possible and wrap predictably.
- UI tests verify selected/normal frame switching.
- Static portrait tests verify selecting/cycling cards does not replace authored squad tray portraits.

## Validation Notes

- Manually click each card in play mode.
- Confirm buildings and previous unit selections are cleared before the new squad is selected.
- Confirm repeated clicks cycle squads instead of doing nothing.
- Confirm squad tray portraits remain authored/static and do not resize or shift the card layout.
- Confirm no full Match HUD rebuild was required for this feature.
