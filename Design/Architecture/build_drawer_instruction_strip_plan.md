# Build Drawer Instruction Strip Plan

## Goal

Bind the existing Build Drawer instruction strip to runtime state so it gives clear, contextual guidance for the active tab, selected item, resources, producer availability, placement state, and queue actions. The strip lives at:

`Canvas (Environment) / SCN09_BuildDrawerPopup / BuildDrawerRoot / DrawerFrame / LeftPanel / InstructionStrip / Frame`

The text object is `Instruction`, and the icon object is `Icon`.

## Architecture Constraints

- Do not rebuild the prefab wholesale.
- Do not use `Object.Find*`, `GameObject.Find`, or runtime hierarchy string lookup.
- Keep `BuildDrawerView` as a serialized view and renderer only.
- Keep policy and instruction selection in `BuildDrawerCatalogPresenterView`.
- Use `BuildingUiCommandSystem` and existing query/command context for resources, producer availability, placement state, queue actions, and command failures.
- Generated icons must be project assets under `Assets/Game/Art/UI/Icons`, imported as Sprites with clamp wrapping.

## Instruction Severities

- `Neutral`: informational state, info icon.
- `Ready`: action is available, ready/check icon.
- `Warning`: non-fatal queue or empty-tab state, warning icon.
- `Error`: action is blocked, error icon.

## Icon Assets

Generate four clean 512x512 tactical UI icons with green/dark military UI backgrounds, no text, no watermark:

- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Info_512.png`
- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Ready_512.png`
- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Warning_512.png`
- `Assets/Game/Art/UI/Icons/Icon_BuildDrawer_Error_512.png`

Import settings:

- Texture type: Sprite.
- Sprite mode: Single.
- Wrap mode: Clamp.
- Mipmaps: Off.
- Alpha source from input when present.
- Keep exact 512x512 project copy.

## Message Matrix

### Default / Empty Selection

- No selected item: `Select an item to place, produce, or recruit.` (`Neutral`)
- Empty Buildings tab: `No requestable buildings are configured.` (`Warning`)
- Empty Vehicles tab: `No requestable vehicles are configured.` (`Warning`)
- Empty Aircraft tab: `No requestable aircraft are configured.` (`Warning`)
- Empty Soldiers tab: `No requestable soldiers are configured.` (`Warning`)

### Buildings

- Selected and available: `PLACE: choose a location for {itemName}.` (`Ready`)
- Not enough credits: `Need {missingCredits} more credits to place {itemName}.` (`Error`)
- Placement active, valid: `Place {itemName}: drag to position, then confirm.` (`Ready`)
- Placement active, blocked: `Cannot place here: {placementStatus}.` (`Error`)
- Primary action success: `Place {itemName}: choose a valid footprint.` (`Ready`)
- Primary action failure: `Cannot place {itemName}: {reason}.` (`Error`)

### Vehicles

- Selected and available: `PRODUCE: add {itemName} to the vehicle queue.` (`Ready`)
- Missing producer: `Cannot produce {itemName}: requires {producerName}.` (`Error`)
- Missing producer without known name: `Cannot produce {itemName}: no compatible vehicle producer is available.` (`Error`)
- Not enough credits: `Need {missingCredits} more credits to produce {itemName}.` (`Error`)
- Success: `{itemName} added to production queue.` (`Ready`)

### Aircraft

- Selected and available: `PRODUCE: add {itemName} to the aircraft queue.` (`Ready`)
- Missing producer: `Cannot produce {itemName}: requires {producerName}.` (`Error`)
- Missing producer without known name: `Cannot produce {itemName}: no compatible air producer is available.` (`Error`)
- Not enough credits: `Need {missingCredits} more credits to produce {itemName}.` (`Error`)
- Success: `{itemName} added to production queue.` (`Ready`)

### Soldiers

- Selected and available: `RECRUIT: add {itemName} to the training queue.` (`Ready`)
- Missing producer: `Cannot recruit {itemName}: requires {producerName}.` (`Error`)
- Missing producer without known name: `Cannot recruit {itemName}: no compatible training building is available.` (`Error`)
- Not enough credits: `Need {missingCredits} more credits to recruit {itemName}.` (`Error`)
- Success: `{itemName} added to recruitment queue.` (`Ready`)

### Queue

- Queue empty: `Production queue is empty.` (`Neutral`)
- Active queue exists: `Producing {itemName}. Cancel current or clear queue if needed.` (`Neutral`)
- Cancel succeeds: `Cancelled {itemName}.` (`Warning`)
- Clear all succeeds: `Production queue cleared.` (`Warning`)
- Cancel unavailable: `Production cancel unavailable.` (`Error`)
- Clear unavailable: `Production clear unavailable.` (`Error`)

## Implementation Checklist

- [x] Step 01: Add this plan document.
- [x] Step 02: Add `BuildDrawerInstructionSeverity` and `BuildDrawerView.ApplyInstruction(...)`.
- [x] Step 03: Serialize `Instruction`, `Icon`, and severity icon sprites on `SCN09_BuildDrawerPopup.prefab`.
- [x] Step 04: Generate and import the four instruction icon assets.
- [x] Step 05: Add presenter-side instruction resolver for tabs, selected item, resources, producer availability, and placement state.
- [x] Step 06: Route primary action success/failure, cancel, and clear queue results into the instruction strip.
- [x] Step 07: Add focused prefab/presenter tests for serialized refs, icon refs, default tab messages, missing producer, insufficient credits, and queue actions.
- [x] Step 08: Run focused Unity validation and `git diff --check`.

## Progress Notes

- 2026-06-09: Step 01 complete. Plan created. Next step is the view API and prefab serialization.
- 2026-06-09: Step 02 complete. Added `BuildDrawerInstructionSeverity`, view instruction fields, and `BuildDrawerView.ApplyInstruction(...)`. Text/icon prefab references are assigned; icon sprite asset slots remain pending icon generation/import.
- 2026-06-09: Steps 03-04 complete. Generated four 512x512 instruction icons, imported them as Sprites, and assigned the icon sprite references on `SCN09_BuildDrawerPopup.prefab`.
- 2026-06-09: Steps 05-07 complete. Presenter now resolves selected-item, missing-producer, insufficient-credit, placement, primary-action, cancel, and clear-queue instruction messages. Focused tests cover prefab wiring and blocked instruction states.
- 2026-06-09: Step 08 complete. `git diff --check` passed, icon dimensions verified at 512x512, forbidden lookup scan passed for touched runtime files, and focused Unity validation passed on a refreshed `/private/tmp` project copy with 21/21 tests.
- 2026-06-09: Regenerated `Icon_BuildDrawer_Ready_512.png` on a magenta key background and replaced the project PNG in place so the green check/ring stays intact after transparency keying.
- 2026-06-09: Re-ran focused Unity validation after the Ready icon replacement. Build drawer validation passed again with 21/21 tests.
- 2026-06-09: Adjusted instruction rendering so severity changes only swap the `Icon` sprite; instruction text color remains controlled by the prefab text style.
- 2026-06-09: Fixed prefab wiring so `BuildDrawerView.instructionIcon` points to `InstructionStrip/Frame/Icon` instead of the active production thumbnail image. Added a regression test that verifies the instruction text/icon are siblings and that all four severity calls swap the visible icon sprite.
