# SCN-09 Build Placement Mode Implementation Spec

Status: Implementation handoff
Date: 2026-06-09
Owner: Design

## Purpose

This document defines the step-by-step implementation for the in-match building placement flow after the player chooses a building from the SCN-09 Build drawer and taps `PLACE`.

The player must never be left dragging a building with no obvious way to finish. Placement mode must always show a clear confirmation bar with `CANCEL`, optional `ROTATE`, and `CONFIRM`.

## Source References

- Visual reference: `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildPlacementMode_TargetLock_V01.png`
- Build drawer reference: `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_SingleActionQueue_OnExistingMatchHUD_TargetLock_V03.png`
- Build drawer notes: `Design/VisualLockLayered/SCN-09_BuildDrawer/README.md`
- Match HUD contract: `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- Building config source: `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_*_Config.asset`

## Design Contract

When the player taps `PLACE` in the Build drawer:

1. The Build drawer closes.
2. The match HUD remains visible.
3. A transparent building ghost appears on the map.
4. A footprint grid appears under the ghost.
5. The player can drag the ghost across valid ground.
6. A compact placement confirmation bar appears above the bottom command bar.
7. The player must confirm or cancel from that bar.

Tapping the world must not finalize the building by itself. World tapping and dragging only move the placement preview. `CONFIRM` is the only action that commits the building.

## Placement Confirmation Bar

The bar appears above the bottom command buttons while placement mode is active.

Required elements:

- Selected building name, for example `PLACE OIL REFINERY`
- Validity status, for example `VALID GROUND`, `BLOCKED`, `OUT OF BOUNDS`, `INSUFFICIENT CREDITS`
- Cost display, using the same currency icon and value shown in the Build drawer
- Duration display, using `productionDurationSeconds`
- `CANCEL` button
- `CONFIRM` button
- Instruction line: `DRAG TO POSITION, CONFIRM TO BUILD`

Optional element:

- `ROTATE` button, shown between `CANCEL` and `CONFIRM` when the building footprint supports rotation.

The floating rotate icon shown in the visual reference is not mandatory. Prefer the labeled `ROTATE` button in the placement bar because it is clearer and avoids confusion with a refresh button.

## Step-by-Step Flow

1. Player opens the Build drawer from the match HUD.
2. Player selects the `Buildings` tab.
3. Player selects a building card.
4. The selected building details panel updates with name, role, description, footprint, cost, time, requirements, placement rule, and `PLACE`.
5. Player taps `PLACE`.
6. System creates a `BuildPlacementRequest` from the selected building.
7. System closes the Build drawer.
8. System enters `BuildPlacementActive`.
9. System spawns a transparent ghost preview of the selected building.
10. System shows the footprint grid under the ghost.
11. System shows the placement confirmation bar.
12. Player drags or taps valid ground to move the ghost.
13. System snaps the ghost to the build grid.
14. System validates the current footprint every time position or rotation changes.
15. System updates ghost color, footprint color, and status text.
16. Player taps `ROTATE`, if available, to rotate the footprint 90 degrees clockwise.
17. Player taps `CONFIRM`.
18. If placement is valid and affordable, system commits the build request.
19. System removes ghost, footprint, and placement bar.
20. System returns to normal match HUD.
21. System shows construction or production feedback through the Build button badge/timer and Build drawer queue.

If the player taps `CANCEL`, the system removes the ghost, footprint, and placement bar. It should reopen the Build drawer with the same building still selected when technically practical. If reopening is not available yet, return to the normal match HUD and show `Placement canceled`.

## State Machine

Required states:

- `NormalMatchHud`
- `BuildDrawerOpen`
- `BuildPlacementActive`
- `BuildPlacementInvalid`
- `BuildPlacementConfirming`
- `ConstructionQueued`

Required transitions:

- `NormalMatchHud` -> `BuildDrawerOpen` when player taps Build.
- `BuildDrawerOpen` -> `BuildPlacementActive` when player taps `PLACE`.
- `BuildPlacementActive` -> `BuildPlacementInvalid` when the current footprint fails validation.
- `BuildPlacementInvalid` -> `BuildPlacementActive` when the current footprint becomes valid.
- `BuildPlacementActive` -> `ConstructionQueued` when player taps `CONFIRM` on valid placement.
- `BuildPlacementInvalid` -> `BuildPlacementInvalid` when player taps `CONFIRM`; show the failure reason and do not place.
- `BuildPlacementActive` or `BuildPlacementInvalid` -> `BuildDrawerOpen` or `NormalMatchHud` when player taps `CANCEL`.
- Any placement state -> `NormalMatchHud` when mission ends, player exits match, or a blocking modal takes over.

## Input Ownership

While placement mode is active:

- The placement mode owns world taps and drags.
- Tapping or dragging the world moves the ghost preview.
- Tapping UI controls must not move the ghost.
- Camera pan and zoom remain available.
- Unit selection is disabled.
- Move, Attack, Hold, Stop, Scan, Support, and Build drawer commands cannot issue unit orders.
- Build drawer cannot be reopened accidentally by world input.
- Pause/settings can still interrupt placement, but must clean up the ghost and footprint.

Recommended camera behavior:

- One-finger drag on the building ghost or ground moves the placement preview.
- Two-finger pinch zooms the camera.
- Two-finger drag pans the camera if supported.
- Edge pan can remain active if it does not fight placement dragging.

## Validity Rules

The validation service must check all of these before enabling a valid confirm:

- Building is unlocked.
- Player has enough required currency.
- Mission or custom-game rules allow this building.
- Footprint is inside the playable grid.
- Footprint does not overlap blockers.
- Footprint does not overlap another building.
- Footprint does not overlap reserved civilian/protected zones.
- Footprint is on a valid terrain type for that building.
- Footprint does not block required pathing if path-critical validation exists.
- Required prerequisite buildings exist, for example Oil Refinery requires Oil Pump if that rule is active.

Invalid state must show one clear reason at a time. Priority order:

1. `LOCKED`
2. `REQUIRES <PREREQUISITE>`
3. `INSUFFICIENT CREDITS`
4. `OUT OF BOUNDS`
5. `BLOCKED`
6. `INVALID TERRAIN`
7. `PROTECTED ZONE`
8. `PATH BLOCKED`

## Visual Feedback

Valid placement:

- Ghost material is readable but transparent.
- Footprint is green or blue-green.
- Status text reads `VALID GROUND`.
- `CONFIRM` is enabled.

Invalid placement:

- Ghost material shifts to red/orange warning tint.
- Footprint is red.
- Status text shows the reason.
- `CONFIRM` is disabled, or tapping it keeps placement active and shows the reason again.

Rotation:

- Rotate the ghost and footprint together.
- Re-run validation immediately after rotation.
- Do not rotate if the footprint is square and rotation has no visible effect, unless the building mesh orientation matters.

## Data Contract

Placement mode requires this data from the selected building:

- Stable config id
- Display name
- Prefab reference
- Footprint width
- Footprint height
- Cost
- `productionDurationSeconds`
- Unlock state
- Prerequisite list
- Placement category or terrain rule
- Build/production spawn behavior

`productionDurationSeconds` must be read from building config data, not hardcoded in UI.

## Commit Behavior

On valid `CONFIRM`:

1. Re-run validation server-side or gameplay-side before spending resources.
2. Spend the required resources.
3. Create construction entity or queued production item.
4. Store grid origin, rotation, building config id, cost, and duration.
5. Clear placement preview objects.
6. Return to normal match HUD.
7. Show feedback:
   - construction marker in world, or
   - Build button badge/timer, and
   - production queue row when Build drawer is reopened.

Do not leave the player inside placement mode after a successful confirm for the first implementation. Repeat placement can be added later as a separate `PLACE ANOTHER` feature.

## Cancel Behavior

`CANCEL` must always work.

On cancel:

1. Do not spend resources.
2. Do not add to production queue.
3. Destroy or hide the ghost preview.
4. Destroy or hide the footprint overlay.
5. Hide the placement confirmation bar.
6. Clear the active `BuildPlacementRequest`.
7. Reopen Build drawer with the previous item selected if the UI stack supports it.
8. Otherwise return to normal match HUD and show `Placement canceled`.

## Implementation Steps

1. Add a placement state enum to the match UI/gameplay flow.
2. Add a `BuildPlacementRequest` data object created from the selected Build drawer item.
3. Wire the Build drawer `PLACE` button to create the request and close the drawer.
4. Add a placement HUD view for the confirmation bar.
5. Add a ghost preview spawner that uses the selected building prefab or a simplified preview mesh.
6. Add a footprint overlay renderer using the building footprint dimensions.
7. Add grid snapping from pointer/raycast hit to grid origin.
8. Add placement validation service and ordered failure reasons.
9. Update ghost, footprint, status text, and confirm state from validation result.
10. Implement `ROTATE` as 90-degree clockwise rotation with immediate revalidation.
11. Implement `CANCEL` cleanup and optional Build drawer restore.
12. Implement `CONFIRM` with final validation, resource spend, and construction/queue creation.
13. Block unit command input while placement mode is active.
14. Allow camera zoom/pan without cancelling placement.
15. Add compact HUD production feedback after confirm.
16. Add capture tests for 16:9 and 20:9 safe-area layouts.

## QA Checklist

- Player can enter placement mode from the Build drawer.
- Build drawer closes when placement starts.
- Ghost and footprint appear immediately.
- Placement bar is visible and not hidden behind the command bar.
- `CANCEL` exits placement from every valid or invalid state.
- `CONFIRM` places only when the footprint is valid.
- Invalid confirm cannot spend resources.
- Dragging over UI controls does not move the ghost.
- Rotation changes footprint orientation and revalidates.
- Camera pan/zoom works without losing placement.
- Unit selection and unit commands are blocked while placement is active.
- Cost and duration match the selected building config.
- Production/construction feedback appears after confirm.
- Reopening the Build drawer shows the queue state.
- Layout works at 16:9, 20:9, and 21:9.

## Acceptance Criteria

The implementation is accepted when a player can select Oil Refinery, tap `PLACE`, drag the ghost to valid ground, rotate it if needed, confirm construction, see production feedback, and cancel safely at any point before confirmation without losing control of the match HUD.
