# RTSSelectionSystem Responsibility Audit

## Purpose

`RTSSelectionSystem` is currently a legacy gameplay facade. Its single acceptable reason to change should become input-to-command orchestration, but today it changes for selection state, UI read models, move orders, transport boarding, targeting, camera behavior, HUD feedback, and gameplay command policy.

## Current Responsibility Buckets

### Selection State

- Focused unit entity.
- Selected move-entity cache.
- Selected tag clearing and focus refresh.
- Select-all and rectangle selection entry points.

Target owner: `SelectionStateSystem` plus ECS selected/focused components over time.

### Input And Drag Selection

- Pointer state.
- Drag rectangle lifetime.
- Selection hold activation.
- UI click suppression.

Target owner: keep temporarily in `RTSSelectionSystem` as the input facade, then move stable data to ECS request components.

### Move Orders

- Manual move target resolution.
- Formation offsets.
- Path request creation.
- Staggered group move behavior.
- Move order HUD result reporting.

Target owner: `UnitMoveOrderSystem`.

### Transport Boarding

- Boarding source selection.
- Capacity checks.
- Pickup, approach, disembark, and rope-drop cell selection.
- Boarding target component writes.

Target owner: `UnitTransportBoardingSystem`.

### Attack And Target Orders

- Attack target validation.
- Radar/missile target selection.
- Detector-radius checks.
- Engage target and combat command writes.

Target owner: `UnitTargetOrderSystem`.

### UI Read Models

- Focused unit label, description, health, capacity, passenger list, and status.
- Portrait pose and selected-unit framing data.
- HUD selection result text.

Target owner: `SelectionUiQuerySystem`.

### Camera Control

- Follow/focus target movement.
- Mode-specific camera transition state.
- Fullscreen/build mode camera settings.

Target owner: `RtsCameraSystem` or a shell-edge camera service fed by ECS camera request components.

## First Extraction Completed

`SelectionStateSystem` now owns:

- Focused unit storage.
- Selected move-entity cache storage.
- Cache eligibility for player move units.
- Cache mutation helpers used by `RTSSelectionSystem`.

`RTSSelectionSystem` remains the facade for behavior in this slice; future work should keep moving behavior out by bucket without adding new responsibilities to the facade.

## Second Extraction Completed

`UnitMoveOrderSystem` now owns:

- Manual move goal selection.
- Manual formation offsets.
- Reserved-goal and selected-current-footprint cell rules.

`UnitTransportBoardingSystem` now owns:

- Boardable player transport checks.
- Transport capacity normalization from source-prefab identity.
- Boarding click padding and landed-state policy.

`UnitTargetOrderSystem` now owns:

- Missile launcher radar target lookup.
- Friendly detector-radius checks.
- Target distance/classification helpers.
- Cleanup of accidental nearby air-selection move orders.

## Third Extraction Completed

`SelectionUiQuerySystem` now owns:

- Focused unit label, description, health, capacity, ownership, attack, and vehicle read models.
- Focused transport passenger UI lists.
- Focused and selected portrait pose/framing calculations.
- Focused unit UI status and HUD selection status text.

## Fourth Extraction Completed

`UnitMoveOrderSystem` now owns:

- Grouped manual move command component writes.
- Immediate move command component writes.
- Shared movement-order cleanup.
- Ground path request creation and staggered retry-cooldown scheduling for grouped move orders.
- Air-unit move command path-request removal.

## Fifth Extraction Completed

`UnitTransportBoardingSystem` now owns:

- Soldier boarding candidate policy.
- Air transport pickup landing-cell search.
- Transport approach-cell search and passability checks.
- Disembark ring-cell search.
- Boarding footprint reservation.
- Transport helicopter rope-disembark request setup.

## Sixth Extraction Completed

`UnitTargetOrderSystem` now owns:

- Attack target validation.
- Attack source validation.
- Selected-unit attack order component writes.
- Direct radar attack component writes.
- Base-breach attack order component writes.
- Commanded attack-order cleanup.

## Seventh Extraction Completed

`RtsCameraSystem` now owns:

- Camera drag-session state.
- Smooth camera focus target state.
- Smooth focus velocity state.
- Smooth focus target advancement and completion clearing.

## Eighth Extraction Completed

`RtsCameraSystem` now also owns:

- Camera mode transition state.
- Fullscreen iso target state.
- Perspective and fullscreen iso transition velocities.
- Camera pan and zoom transform writes.
- Perspective and fullscreen iso camera mode writes.
- Ground-center movement and viewport ground-plane ray queries.
- Ground-span camera mode fitting calculations.

`RTSSelectionSystem` remains the input/UI facade and still decides when camera actions are requested from runtime/UI state.

## Ninth Extraction Completed

`RtsSelectionInputSystem` now owns:

- Pointer drag origin/current/last-position state.
- UI-click and world-release suppression state.
- Selection-hold timing state.
- Live selection rectangle cache state.
- Deferred move-order click queue state.
- Last-known pointer position state.

`RTSSelectionSystem` still performs input orchestration and gameplay command dispatch; this slice only moved mutable input session state and small state-only helpers.

## Recommended Next Slices

1. Move `RTSSelectionSystem` input orchestration branches into `RtsSelectionInputSystem` or ECS request components once command side effects have narrower interfaces.
2. Replace static runtime state reads from `InitialUnitsRuntimeState` with ECS singleton request/state components.
