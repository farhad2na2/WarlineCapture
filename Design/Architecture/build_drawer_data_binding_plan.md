# Build Drawer Data Binding Plan

Status: planning only; do not rebuild prefabs for this pass.
Updated: 2026-06-09

## Scope

This document tracks the data/config binding needed for `Canvas (Environment) / SCN09_BuildDrawerPopup`.
The current popup opens and closes, but most visible fields are still static placeholders. The next implementation should wire the existing prefab through serialized references only; it should not regenerate or wholesale rebuild the UI prefab.

Primary design references:

- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/README.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

## Architecture Rules

- No shipped runtime hierarchy search, `Object.Find*`, `GameObject.Find`, or string path lookup.
- The popup view owns serialized UI references only.
- The popup view does not decide gameplay policy and does not mutate gameplay state directly.
- UI click handlers emit typed requests through the existing building/production boundary.
- A presenter/read-model translates configs and runtime queue state into UI snapshots.
- Dynamic item rows and queue rows use pooled item views under serialized content roots.
- The catalog must include only entries that can be requested from config. Non-requestable civilians, enemies, ambience, static-only map buildings, and debug entries stay hidden.

## Existing Data Sources

| Data | Current source |
|---|---|
| Requestable buildings | `BuildingPlacementSystemConfig.Spawnables` / configured spawnable definitions, filtered by `BuildingDefinitionAuthoring.ConfiguredCanRequest` or `BuildingDefinitionAuthoringConfig.CanRequest` |
| Requestable units | `UnitPrefabRegistryAuthoringConfig.UnitSpawnPrefabs`, filtered by `UnitGridAuthoring.CanRequest` or `UnitGridAuthoringConfig.CanRequest` |
| Building identity | `ConfiguredDisplayName`, `ConfiguredDescription`, `ConfiguredPortraitSprite`, `ConfiguredPortraitCardSprite`, `ConfiguredPortraitActionSprite` |
| Building placement | `ConfiguredFootprintCells`, `ConfiguredPrice`, `ConfiguredRole`, `ConfiguredIsWall` |
| Building production slots | `BuildingDefinitionAuthoring.ConfiguredProductionCount`, `GetProductionOrDefault(index)` |
| Unit identity | `ConfiguredDisplayName`, `ConfiguredDescription`, `PortraitSprite`, `PortraitCardSprite`, `PortraitActionSprite` |
| Unit production | `Price`, `ProductionDurationSeconds`, `ProductionTransportPrefab`, transport/runway flags |
| Unit category | `IsAirUnit`, `UsesVehicleMotion`, footprint, transport capacity, identity tokens |
| Queue runtime | `BuildingProductionQueueCompositionSystemHelper`, `BuildingProductionRequestBoundary`, queue context and producer building state |
| Resource runtime | Current dollar/credits source from `BuildingUiCommandSystem.Context.GetCurrentDollars`; other resources need confirmation |

## Category And Visibility Rules

| Drawer tab | Include rule | Primary CTA |
|---|---|---|
| Buildings | Prefab has `BuildingDefinitionAuthoring`; config says `CanRequest`; present in player buildable spawnables. | `PLACE` |
| Vehicles | Prefab has `UnitGridAuthoring`; config says `CanRequest`; `IsAirUnit == false`; vehicle-motion/vehicle identity is true; not classified as soldier/personnel. | `PRODUCE` |
| Aircrafts | Prefab has `UnitGridAuthoring`; config says `CanRequest`; `IsAirUnit == true`. | `PRODUCE` |
| Soldiers | Prefab has `UnitGridAuthoring`; config says `CanRequest`; not air; not vehicle-motion; personnel category. | `RECRUIT` |

Notes:

- The Armory type-label logic can be reused as a reference for labels such as `TRANSPORT VEHICLE`, `AIRCRAFT`, `TRANSPORT AIRCRAFT`, `TENT`, `WALL`, and `STRUCTURE`.
- Build drawer visibility should be stricter than Armory visibility. Armory can show inspectable configs; Build drawer shows only requestable configs.
- If a requestable config has no producer building available, it can remain visible but disabled with a typed reason such as `Requires Barracks` or `Requires Airport`.

## Popup Areas To Bind

### Root And Modal Behavior

Path: `SCN09_BuildDrawerPopup / BuildDrawerRoot / DrawerFrame`

Required bindings:

- Open state from the shell popup route.
- Close button remains wired to shell close.
- Closing the drawer clears build/placement mode and any drawer-only selected item.
- The drawer must block world input behind it.

### Category Tabs

Paths:

- `BuildingsTab`
- `VehiclesTab`
- `AircraftsTab`
- `SoldiersTab`
- Selected tab frame example: `Canvas (Environment) / SCN09_BuildDrawerPopup / BuildDrawerRoot / DrawerFrame / LeftPanel / Tabs / BuildingsTab / Frame`
- Unselected tab frame example: `Canvas (Environment) / SCN09_BuildDrawerPopup / BuildDrawerRoot / DrawerFrame / LeftPanel / Tabs / VehiclesTab / Frame`

Required bindings:

- Selected/unselected visual state.
- Selected tabs should reuse the current `BuildingsTab / Frame` sprite/style as the selected category visual.
- Unselected tabs should reuse the current `VehiclesTab / Frame` sprite/style as the normal category visual.
- Category display label.
- Visible item count for requestable entries.
- Disabled state and disabled reason if a category has no requestable entries or is unavailable in the current match.
- On click, change local drawer category and rebuild the item list from the read-model snapshot.

### Catalog Scroll Rect And Item Views

Paths:

- `Scroll View / Viewport / Content`
- Existing `ItemView`, `ItemView (1)`, etc. should become templates/pool items.
- Item frame path: `Canvas (Environment) / SCN09_BuildDrawerPopup / BuildDrawerRoot / DrawerFrame / LeftPanel / Scroll View / Viewport / Content / ItemView / Frame`

Each item view should bind:

- Thumbnail: prefer card portrait, then base portrait, then category fallback art.
- Display name.
- Type/role label.
- Short description or one-line role summary.
- Cost.
- Production/build time.
- Footprint for buildings.
- Requirement line.
- Disabled reason.
- Selected state: for units and buildings, swap the `Frame` image to `Assets/Game/Art/UI/Panels/scn09_build_card_frame_selected_check.png` when the item is selected, and restore the normal/unselected frame sprite when it is not selected.
- Affordability/queue availability state.

Interaction:

- Clicking an item selects/previews it. It should not immediately place or produce.
- The selected item detail panel owns the primary action.

### Selected Item Detail Panel

Paths include:

- `Preview`
- `Thumb`
- `Name`
- `Role`
- `Description`
- `CreditsCost`
- `SuppliesTinyCost`
- `CreditsTinyCost`
- `ProductionTimePanel / TimeText`
- `PlacementPanel`
- `RequirementsPanel`

Required bindings:

- Large preview image: prefer action portrait for units/aircraft/vehicles, then card portrait, then base portrait, then fallback.
- Display name and type label.
- Full configured description.
- Cost row from configured price and any extra resource costs.
- Time row from `ProductionDurationSeconds` for units; building placement/build duration needs a confirmed source.
- Placement row for buildings: footprint, placement category, and validity prompt.
- Requirements row: required producer building, runway requirement, unlock state, resource shortfall, queue capacity, or disabled reason.

### Primary Action Button

Paths:

- `BuildButton`
- `OrderButton` if this is the current visual primary action; implementation should identify which one is canonical and hide/disable the duplicate.

Required label by selected category:

- Buildings: `PLACE`
- Vehicles: `PRODUCE`
- Aircrafts: `PRODUCE`
- Soldiers: `RECRUIT`

Required state:

- Disabled when no item is selected.
- Disabled with reason when the selected config is not requestable, unaffordable, missing producer, blocked by runway/queue capacity, or otherwise unavailable.
- On click, emit a typed request:
  - Buildings: enter placement mode for selected prefab.
  - Vehicles/Aircrafts: enqueue production through producer building.
  - Soldiers: enqueue recruitment through producer building.
- Request failure must surface a typed feedback message; never silently fail.

### Production Queue Panel

Paths:

- `PRODUCTION QUEUE` label area.
- `ProductionPanel`
- `ProductionPanelActive`
- `ProductionItemView`
- `ProductionActiveItemView`
- `NoProduction`
- `PercentageCompleteText`
- `TimeText`
- `Numbers`
- `CancelButton`
- `RushButton`
- `ClearButton`
- `Slider`

Required bindings:

- Empty state when no active or queued production exists.
- Active item name, icon, producer, progress percent, ETA, and cancel availability.
- Queued item rows with icon, name, ETA/order, and cancel availability.
- Queue capacity text such as `4/6`, if a capacity system exists.
- Producer building label when queue is scoped to a selected/nearest/global producer.
- `CancelButton` emits a cancel request for the selected/active queue item.
- `RushButton` remains disabled unless rush tickets/resources and rules exist.
- `ClearButton` remains disabled unless bulk-cancel rules are confirmed.

### Resource And Requirement Labels

Current placeholder labels include credits, supplies, time, placement, footprint, and requirements.

Required bindings:

- Credits from `Price` / current player credits.
- Supplies/materials/fuel only if the economy source of truth has those costs for build drawer entries.
- Time from production/build time source.
- Requirements from producer/unlock/runway/queue/resource validation.

Open resource naming issue:

- The popup currently shows supplies-style fields. We need confirmation whether build drawer costs should use only credits for V1 or show credits plus supplies/fuel/materials.

## Suggested Runtime Shape

New or expanded UI classes:

- `BuildDrawerView`: serialized root references, category tabs, item content/template, detail fields, action button, queue content/template, queue controls.
- `BuildDrawerTabView`: one tab button plus selected/disabled/count visuals.
- `BuildDrawerItemView`: one catalog item row/card.
- `BuildDrawerQueueItemView`: one active or queued production row.
- `BuildDrawerReadModel`: immutable snapshot of categories, requestable catalog entries, selected item, queue state, and resource state.
- `BuildDrawerPresenterSystem`: maps configs/runtime state to `BuildDrawerReadModel`, applies it to the view, and translates UI clicks into typed requests.

Existing systems to integrate with:

- `BuildingUiCommandSystem`
- `BuildingProductionRequestBoundary`
- `BuildingProductionQueueCompositionSystemHelper`
- `BuildingPlacementSessionCompositionSystemHelper` / placement command boundary
- `UIShellContentView` popup binding

## Implementation Plan

Use this checklist as the progress tracker. Mark a step complete only after its done criteria are verified.

- [x] Step 01 - Prefab reference inventory
  - Inspect the existing `SCN09_BuildDrawerPopup` hierarchy and identify the exact serialized references needed for tabs, item templates, detail panel, queue rows, primary CTA, close button, and optional controls.
  - Done when the implementation knows every reference path and no prefab rebuild is required.

- [x] Step 02 - Add serialized view components
  - Add view classes for the existing popup hierarchy, such as `BuildDrawerView`, `BuildDrawerTabView`, `BuildDrawerItemView`, and `BuildDrawerQueueItemView`.
  - These classes should expose serialized references only and should not perform gameplay logic.
  - Done when the scripts compile and can represent the existing prefab without hierarchy search.

- [x] Step 03 - Wire existing prefab references in place
  - Add the new view components and assign serialized fields on the existing prefab.
  - Preserve current hierarchy, art, layout, and `.meta` files.
  - Done when Unity prefab validation reports no missing script/reference fields.

- [x] Step 04 - Build requestable-only catalog read model
  - Read buildings from configured spawnables and units from the unit prefab registry.
  - Include only configs where `CanRequest` is true.
  - Done when non-requestable civilians, enemy entries, static-only items, and debug entries are absent from the drawer data.

- [x] Step 05 - Categorize requestable catalog entries
  - Split entries into Buildings, Vehicles, Aircrafts, and Soldiers.
  - Use existing config signals first: `BuildingDefinitionAuthoring`, `UnitGridAuthoring.IsAirUnit`, vehicle motion/footprint, transport capacity, and identity fallback only where needed.
  - Done when each requestable entry appears in exactly one drawer tab.

- [x] Step 06 - Bind category tabs
  - Bind tab click, selected state, item count/availability, and disabled reason.
  - Selected tab visuals use the existing `BuildingsTab / Frame` visual; unselected tab visuals use the existing `VehiclesTab / Frame` visual.
  - Done when switching tabs updates the visible catalog without leaking world input.

- [x] Step 07 - Bind catalog item views
  - Pool/reuse item views under `LeftPanel / Scroll View / Viewport / Content`.
  - Bind thumbnail, name, type/role, short description, cost, time, footprint, requirements, disabled reason, and affordability.
  - Done when all visible item cards are config-backed and no static placeholder item text remains.

- [x] Step 08 - Bind item selection visuals
  - Clicking an item selects/previews it without issuing a gameplay command.
  - Selected unit/building item views set `ItemView / Frame` to `Assets/Game/Art/UI/Panels/scn09_build_card_frame_selected_check.png`.
  - Unselected item views restore the normal frame sprite.
  - Done when exactly one item per category selection shows the selected frame.

- [x] Step 09 - Bind selected item detail panel
  - Bind large preview, thumbnail, name, type label, full description, cost rows, time, placement/footprint, and requirements.
  - Done when selecting any catalog item fully updates the right/detail panel from config data.

- [x] Step 10 - Bind primary action button
  - Identify the canonical current CTA (`BuildButton` or `OrderButton`) and disable/hide the duplicate if needed.
  - Bind labels: Buildings `PLACE`, Vehicles `PRODUCE`, Aircrafts `PRODUCE`, Soldiers `RECRUIT`.
  - Bind enabled state and typed disabled reasons.
  - Done when no item means disabled, valid item means actionable, and invalid item explains why.

- [x] Step 11 - Route primary action requests
  - Buildings enter placement mode through the placement boundary.
  - Vehicles/Aircrafts enqueue production through the production request boundary.
  - Soldiers enqueue recruitment through the production request boundary.
  - Done when UI emits typed requests and never mutates gameplay state directly.

- [x] Step 12 - Bind production queue panel
  - Bind empty, active, queued, progress, ETA, producer, queue capacity, and cancelable states.
  - Done when active/queued production reflects runtime state and the empty message shows only when no queue exists.

- [x] Step 13 - Bind secondary queue controls
  - Wire `CancelButton` to typed cancel requests.
  - Keep `RushButton` and `ClearButton` disabled with reason text unless their gameplay rules are confirmed.
  - Done when secondary buttons never silently fail.

- [x] Step 14 - Add focused tests
  - Cover requestable filtering, category assignment, tab selected/unselected visuals, item selected frame, CTA label rules, disabled reasons, and request emission.
  - Done when focused EditMode tests pass.

- [x] Step 15 - Final Unity validation
  - Run prefab serialization validation and a compile/test pass.
  - Verify no missing references, no hierarchy-search regressions, and no prefab rebuild churn.
  - Done when validation passes and this document has progress notes updated.

## Progress Notes

- 2026-06-09: Planning document created. No implementation started.
- 2026-06-09: Step 01 complete. Existing popup surfaces inventoried: `BuildDrawerRoot / DrawerFrame`, `LeftPanel / Tabs`, `BuildingsTab`, `VehiclesTab`, `AircraftsTab`, `SoldiersTab`, `LeftPanel / Scroll View / Viewport / Content`, item templates `ItemView*`, detail images/texts (`Preview`, `Thumb`, `Name`, `Role`, `Description`, costs, time, placement, requirements), CTA buttons (`BuildButton`, `OrderButton`), queue surfaces (`ProductionPanel`, `ProductionPanelActive`, `ProductionItemView*`, `ProductionActiveItemView`, `NoProduction`, `Slider`, progress/time/count texts), queue controls (`CancelButton`, `RushButton`, `ClearButton`), and shell close binding through `UIPopupCloseView`. No prefab rebuild is required.
- 2026-06-09: Step 02 complete. Added serialized-only `BuildDrawerCategory`, `BuildDrawerView`, `BuildDrawerTabView`, `BuildDrawerItemView`, and `BuildDrawerQueueItemView` scripts with stable `.meta` files. Shadow Unity batch compile passed via `/private/tmp/warlinecapture-builddrawer-step02-shadow-compile.log`.
- 2026-06-09: Step 03 complete. Wired `BuildDrawerView` onto the existing popup root, added tab/item/queue view components in place, assigned selected tab frame, normal tab frame, selected item frame, item template, queue templates, CTA buttons, close button, and available detail/queue fields. Added Button components only to existing tab and item roots that lacked them, using their existing frame images as target graphics. Temporary editor binder was removed after use. Shadow Unity prefab import/compile passed via `/private/tmp/warlinecapture-builddrawer-step03-final-compile.log`.
- 2026-06-09: Steps 04 and 05 complete. Added `BuildDrawerCatalogQueryUiSystemHelper` with `BuildDrawerCatalogItem` and formatter rules. The catalog includes only `CanRequest` building/unit configs and categorizes requestable entries into Buildings, Vehicles, Aircrafts, and Soldiers. Added focused tests for requestable filtering, category assignment, action labels, and `CollectAll`. Shadow Unity focused validation passed via `/private/tmp/warlinecapture-builddrawer-step04-05-focused.log`.
- 2026-06-09: Steps 06 and 07 complete. Added `BuildDrawerCatalogRuntimeView` to bind category tabs, requestable counts, selected/normal tab visuals, and config-backed catalog rows. Static placeholder rows are hidden behind the template/pool path, and visible item rows now come from the requestable catalog. Shadow Unity focused validation, including the real `SCN09_BuildDrawerPopup` prefab, passed via `/private/tmp/warlinecapture-builddrawer-step06-07-focused.log`.
- 2026-06-09: Steps 08, 09, and 10 complete. Item clicks now only select/preview drawer entries, selected rows swap to `scn09_build_card_frame_selected_check.png`, the detail panel binds the selected config name/type/description/cost/time/placement/requirements and portraits, and the primary CTA label is category-derived (`PLACE`, `PRODUCE`, `RECRUIT`). `BuildButton` is the canonical CTA when both button references exist; `OrderButton` is hidden as the duplicate. Shadow Unity focused validation passed via `/private/tmp/warlinecapture-builddrawer-step08-10-final-focused.log`.
- 2026-06-09: Step 11 complete. `MatchBootstrapSystem` exposes the existing `BuildingUiCommandSystem.Context`, `MenuBootstrapSystem` passes it to `UIShellContentView`, and installed build drawer popups bind the presenter to the runtime command boundary. Primary CTA clicks now route through `BuildingUiCommandSystem.TryRequestCampItem`; building `PLACE` enters placement mode and closes the drawer, while unit `PRODUCE`/`RECRUIT` keeps the drawer open for queue feedback. Focused routing validation passed via `/private/tmp/warlinecapture-builddrawer-step11-focused.log`.
- 2026-06-09: Step 12 complete. The build drawer now binds friendly pending production from `BuildingUiQueryUiSystemHelper.GetFriendlyPendingProductionUiEntries`, displays empty/active/queued states, binds active/queued row names from the same catalog resolver as the item list, and updates summary progress/time/count. The read model carries producer names; the current prefab queue item components do not expose per-row producer text fields, so producer display is available when a field is later assigned. Focused prefab validation passed via `/private/tmp/warlinecapture-builddrawer-step12-focused.log`.
- 2026-06-09: Step 13 complete. Added a typed production cancel boundary to `BuildingUiCommandSystem` and implemented it in `BuildingUiContextCompositionSystemHelper` using validated building id plus pending production index. The drawer Cancel button now routes active production cancellation through that boundary; Rush and Clear remain disabled because their gameplay rules are not defined. Focused validation passed via `/private/tmp/warlinecapture-builddrawer-step13-focused.log`.
- 2026-06-09: Step 14 complete. `BuildDrawerCatalogQueryUiSystemHelperTests.RunFocusedValidation` now covers requestable filtering, category assignment, action labels, real popup tab/item binding, selected item frame swapping, detail/CTA binding, primary action request routing, production queue snapshot binding, and cancel request routing.
- 2026-06-09: Step 15 complete. Final static checks passed (`git diff --check` and no forbidden build drawer hierarchy-search calls), and final shadow Unity focused validation passed via `/private/tmp/warlinecapture-builddrawer-final-focused.log` with 10 checks.

## Acceptance Criteria

- Non-requestable configs do not appear in the drawer.
- Category tabs show only requestable catalog availability.
- Selected category tabs use the current `BuildingsTab / Frame` visual; unselected category tabs use the current `VehiclesTab / Frame` visual.
- Item cards are generated from configs, not static placeholder text.
- Clicking an item previews it and does not issue gameplay commands.
- Selected unit/building item views use `Assets/Game/Art/UI/Panels/scn09_build_card_frame_selected_check.png` on their `Frame` image; unselected item views restore the normal frame.
- Primary action text matches category: `PLACE`, `PRODUCE`, `PRODUCE`, `RECRUIT`.
- Primary action uses typed gameplay requests and reports typed failure reasons.
- Queue panel reflects real production state or shows the empty state.
- Close button hides the popup and exits build/placement mode.
- No prefab regeneration, runtime hierarchy search, or direct gameplay mutation from UI.

## Open Questions

1. Should build drawer V1 costs be credits-only, or should it also show supplies/fuel/materials when configs support them?
2. What is the canonical source for building build time? Current building configs expose price and footprint, but no obvious build-duration field.
3. Should vehicles, aircraft, and soldiers use a global production queue, or should the drawer select the nearest/first compatible producer building?
4. Should entries with missing producer buildings remain visible disabled, or be hidden until the producer exists?
5. Should `Rush All` and `Clear All` ship disabled in V1, or should we implement queue acceleration and bulk cancel now?
6. Should transport vehicles/transport aircraft be grouped with Vehicles/Aircrafts or split into a future Logistics category?
7. Should contractors and specialists appear under Soldiers, or should Soldiers be combat troops only?
8. Which button is the canonical primary CTA in the current prefab, `BuildButton` or `OrderButton`?
