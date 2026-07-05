# TransportBoardingCommandSystem Phase 9 Inventory

Date: 2026-07-05

## Scope

This inventory covers `Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs` as the Phase 9 decomposition starting point from `Design/Architecture/architecture_performance_audit_followup_tracker.md`.

The slice is documentation-only. It does not change gameplay behavior, ECS ownership, UI wiring, prefabs, or transport timing.

## Current Shape

- File size: 4,058 lines.
- Runtime type: `TransportBoardingCommandSystem : ISystem`.
- Update order: before `UnitMoveOrderRequestSystem` and `UnitTransportBoardingSystem`.
- Primary command input: `RtsSelectionCommandIntentRequestElement`.
- Primary command output: `RtsSelectionCommandResultElement`.
- Main live state dependencies: `UnitGrid`, `UnitFootprint`, `UnitMove`, `UnitTransportCapacity`, `UnitTransportPassengerElement`, `UnitTransportBoardingTarget`, `UnitTransportPassenger`, `UnitTransportCargoPassenger`, `UnitAirComponent`, `UnitAirMovement`, `UnitTransportRopeDisembarkRequest`, `UnitTransportAirdropRequest`, `UnitTransportPlaneDoorOpenRequest`, `GridWalkable`, `DynamicBlockerComponent`, and `DynamicOccupancyComponent`.
- Existing instrumentation: editor allocation probes through `RuntimeDiagnosticsSystem.RecordEditorTransportBoardingUpdateAllocation` and `RecordEditorTransportBoardingCommandAllocation`.
- Diagnostics sink: `TransportBoardingDiagnosticLogComponent` buffer, flushed by the existing diagnostic log systems.

## Responsibility Map

| Area | Current methods | Current responsibility | Extraction risk |
|---|---|---|---|
| ECS system shell | `OnCreate`, `OnUpdate`, `EnsureEntityQueries`, `ProcessPreResolvedTransportRequests` | Own query lifetime and drain pre-resolved transport requests from ECS command buffers. | Low if kept as the only `ISystem` entry point. |
| Command dispatch | `ProcessCommandIntentRequests`, `IsTransportCommandIntent`, `IsPreResolvedTransportCommandIntent`, `AddCommandResult`, `Process*Request`, `To*CommandResultElement` | Convert RTS command intents into boarding/disembark command results. | Low/medium; stable extraction target because it is mostly routing and result shaping. |
| Selection and click resolution | `TryRequestBoardTransportOrderToClickedUnit`, `TryGetClickedOrNearbyBoardableTransport`, `TryFindNearbyBoardableTransport`, `CollectSelectedBoardingSourceEntities`, `TryResolveSelectedBoardTransport`, `CollectEntities` | Resolve the selected/focused transport or nearby clicked transport and collect selected passengers. | Medium; depends on selection cache semantics and UI pointer delegate contracts. |
| Boarding validation and capacity | `TryValidateBoardingTransport`, `ResolveTransportSlotAvailability`, `TryResolveBoardingPassengerKind`, `HasAvailableTransportBoardingSlot`, `CountTransportPassengerOccupancy`, `ResolveTransportPassengerCapacity`, `IsBoardablePlayerTransport`, `IsSoldierBoardingCandidate`, `IsPotentialVehicleCargoPassenger`, `IsCargoPlaneTransport` | Decide which transports/passengers are eligible and how many soldier/cargo slots remain. | Low; already heavily covered by focused tests and good candidate for first pure helper extraction. |
| Boarding order planning | `TryIssueBoardTransportOrderToTransport`, `TryIssueBoardSelectedTransportOrderToPassenger`, `TryIssueBoardNearestSoldierOrders`, `TryCreateTransportBoardingGoalOrder`, `BuildSelectedBoardingPassengerIgnoreSets`, `CollectNearestBoardingCandidates`, `CollectPathingLiveUnits` | Build passenger move goals, reserve approach cells, command air pickup landing if required, and apply boarding target state. | High; mixes selection, grid pathing, seat planning, move-order mutation, passenger state mutation, and diagnostics. Split after more test pinning. |
| Approach-cell search | `TryFindTransportBoardingGoal`, `TryFindPlaneRampApproachCell`, `ResolvePlaneRampApproachCell`, `TryFindTransportApproachCell`, `TryFindNearbyTransportApproachCell`, `IsTransportApproachPassable`, `ReserveFootprintCells`, scoring helpers | Choose boarding approach cells for ground transports, helicopters, and cargo-plane rear ramp loading. | Medium; algorithmic and mostly pure, but depends on live unit arrays and friendly-pass blocker semantics. Good second extraction after safety tests. |
| Air pickup support | `TryFindAirTransportPickupCellNearPassenger` plus calls into `UnitTransportAirPickupSystem` | Find a landing cell near passengers when an airborne helicopter is selected for boarding. | Medium; keep ownership with existing air pickup system where practical. |
| Disembark routing | `ProcessDisembarkTransportRequest`, `ProcessDisembarkTransportPassengerRequest`, `TryDisembarkTransport`, `TryDisembarkTransportPassenger`, `TryPlanPassengerDisembarkCells`, `TryFindTransportDisembarkCell`, `TryFindPlaneRampDisembarkCell`, `TryFindPlaneRampRolloutCell`, `CanPlaceDisembarkedFootprint`, `TryFindTransportRingCell` | Ground exit, selected passenger exit, plane rear-ramp exit, passenger placement, visibility restore, and rollout move orders. | High; mutates passengers, buffer state, visibility, movement, and plane-door requests. Extract only after tests pin partial-unload/remaining-passenger behavior. |
| Rope disembark | `IsRopeDisembarkTransport`, `StartRopeDisembarkTransport` | Convert helicopter transport disembark into a rope request and takeoff/hover state. | Medium; current behavior intentionally routes helicopter exit through rope flow. Better target is a small helper that only creates the request. |
| Plane airdrop/deploy | `TryStartPlaneAirdrop`, `TryIssueDeployDisembark`, `CanStartPlaneAirdrop`, `TryValidatePlaneAirdropPassengers`, `TryValidatePlaneAirdropPassenger`, `SetPlaneAirdropRequest`, `CountAirdropPassengers`, `ResolveLoadedPassengerKind`, `MarkDeployPassengersForAttack`, `RequestPlaneDoorOpen` | Start plane airdrop requests, validate landing/visual availability, count soldier vs cargo drops, and mark deploy-attack targets. | Medium/high; request creation can be split safely, but validation must stay aligned with `UnitTransportAirdropSystem`. |
| Diagnostics | `ShouldQueueTransportBoardingDiagnostics`, `EnsureTransportBoardingDiagnosticQueue`, `EnqueueTransportBoardingDiagnostic`, `DescribeTransportBoardingEntity`, `DescribeTransportAirState`, source-name helpers | Optional transport boarding diagnostic strings and queue setup. | Low if moved to a `SystemHelper` diagnostics helper with unchanged call sites; avoid adding a new Boundary/Presenter. |

## Existing Test Coverage

Focused editor coverage already exists in:

- `Assets/Tests/Editor/UnitTransportValidationTests.cs`
  - Ground personnel transport boarding.
  - Cargo plane soldier and vehicle boarding.
  - Rear-ramp approach cell behavior.
  - Full passenger/cargo capacity rejection.
  - Helicopter rejection of vehicle cargo.
  - Plane ground exit and blocked-ramp rejection.
  - Plane airdrop request/result/visual validation.
  - Plane airdrop pass and touchdown behavior.
  - Deploy-attack command flow.
  - Pre-resolved board/disembark command queue handling.
  - Air transport pickup and landed/airborne boarding gates.
  - Helicopter rope disembark one-at-a-time, straight-down, and multi-passenger free-cell placement.
  - Focused transport read-model passenger/cargo rows.
- `Assets/Tests/Editor/UnitTransportBoardingSystemExtractionTests.cs`
  - Transport capacity recognition.
  - Cargo capacity preservation.
  - Soldier boarding candidate classification.
  - Footprint reservation helper behavior.
- `Assets/Tests/Editor/ScenarioLab/TransportBoardingScenarioLabTests.cs`
  - TB-001 through TB-010 ScenarioLab coverage for vehicle, helicopter, plane, cargo, airdrop, and negative cases.
- `Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs`
  - Deterministic scene-level smoke coverage for helicopter boarding/exit, transport plane boarding, transport plane ground exit, and plane airdrop visuals.

## Recommended Decomposition Order

1. Extract diagnostics-only helper.
   - Target: `DescribeTransportBoardingEntity`, `DescribeTransportAirState`, `ShouldQueueTransportBoardingDiagnostics`, `EnsureTransportBoardingDiagnosticQueue`, `EnqueueTransportBoardingDiagnostic`.
   - Reason: lowest behavioral risk, no gameplay ownership change, and reduces noise in the main system.
   - Constraint: name as `TransportBoardingDiagnosticSystemHelper`, not Boundary/Presenter.

2. Extract pure capacity/passenger classification helper.
   - Target: passenger kind, cargo weight, capacity, occupancy, soldier/cargo candidate checks, cargo-plane checks.
   - Reason: already test-covered and reusable by selection preview/read-model code.
   - Constraint: keep authoritative capacity mutation in existing ECS systems; do not create managed state.

3. Extract approach-cell search helper.
   - Target: approach/ramp/ring cell search and footprint reservation.
   - Reason: algorithmic code can be tested independently before moving command mutation.
   - Constraint: preserve friendly-pass blocker semantics and live-unit occupancy filtering exactly.

4. Extract command-result shaping helper.
   - Target: request-kind checks plus `ToBoardingCommandResultElement`, `ToBoardAllCommandResultElement`, and `ToDisembarkCommandResultElement`.
   - Reason: mostly stable formatting and command-result contract.
   - Constraint: keep the ECS command buffer drain in the `ISystem`.

5. Split disembark planning from disembark mutation.
   - Target first: `TryPlanPassengerDisembarkCells`, plane ramp disembark/rollout search, and ring-cell search.
   - Target later: passenger buffer mutation, visibility restore, movement clear/reissue, plane door request.
   - Reason: planning is testable; mutation is behavior-sensitive.

6. Split plane airdrop request creation and validation.
   - Target first: airdrop validation and request data construction.
   - Keep: final `UnitTransportAirdropRequest` component mutation in a narrow ECS-owned system/helper edge until a later ECS command surface exists.

7. Leave boarding-order mutation in the system until after more measured data.
   - `TryIssueBoardTransportOrderToTransport`, `TryIssueBoardSelectedTransportOrderToPassenger`, and `TryIssueBoardNearestSoldierOrders` currently combine planning and mutation. Moving these too early risks regressions in the exact cases the user previously validated visually.

## Guardrails For Future Slices

- Do not add a new `Boundary` or `Presenter` class.
- Do not add MonoBehaviour gameplay loops or Canvas-owned transport state.
- Keep `TransportBoardingCommandSystem` as the ECS command entry point while extracting helpers.
- Prefer `SystemHelper` names for helper extractions, and keep helpers stateless where possible.
- Add or update focused tests before each extraction.
- After each extraction, run:
  - `git diff --check`
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - A focused Unity/EditMode test when the slice touches behavior.

## Next Slice Recommendation

Start with diagnostics extraction into `TransportBoardingDiagnosticSystemHelper`.

This is the smallest behavior-preserving slice because it moves logging and message formatting only. It does not alter boarding decisions, seat capacity, movement orders, passenger buffers, rope exit, airdrop requests, or visual ownership.
