# Interpreting the next native packed-view diagnostic

This is a source trace, not a diagnosis of trial 01. The original strict opening
gate remains unchanged. Trial 02 must supply the actual database fields before
we select any implementation correction.

## Native ownership and publication

`OperationMapRenderVirtualizationInitializationSystem.OnUpdate` requires one
active map, one database and one state owner, with the database and state owner
being the same entity. It validates the packed readiness contract, database
identity, native slot census, metadata and active map generation. Successful
allocation writes Initialized=1, capacity and disabled slot counts. Allocation
alone is not a rendered opening view.

`ScheduleCameraRebuildIfRequired` requires exactly one RuntimeCameraSnapshot,
initialized canonical visual state and a valid native camera snapshot. A
missing/invalid camera can leave allocation initialized while no first rebuild
has occurred. Invalid database/camera-envelope contracts otherwise throw. It
does not infer camera state from Camera.main or global model counts.

`TryBuildCameraEnvelopes` projects the native camera through the packed cells'
actual vertical bounds, converts the footprint into database cell coordinates,
and clamps to the database grid. The required envelope adds one guard cell;
the materialized envelope adds two. Actual selection uses the materialized
envelope. This matters when comparing an apparently clear on-screen base with
the scenery that the allocator must still materialize outside the image.

`OperationMapRenderRequiredCellSelectionJob` traverses the existing blob cells
and selects coordinates inside that envelope. A valid envelope can select zero
cells without an error. The database itself must contain cells. No selected
cell count or global unit count is a stand-in for this per-envelope census.

`OperationMapRenderPlacementGatherJob` validates every selected cell's range,
deduplicates its placement indices, then filters each placement against the
native canonical visual state. A render-only placement must have owner -1 and
RequiredVisualState=Any. A stateful placement requires a valid state owner and
Intact/Destroyed state; only its currently matching visual is included.
Every included placement contributes its prototype's actual part rows and
pool buckets. Zero selected placements/rows is allowed by this algorithm.

`OperationMapRenderAssignmentJob` releases unneeded slots, retains matching
rows and assigns new rows in their exact pool buckets. A valid zero-row input
does not fabricate a slot or a failure. Exhausting a pool bucket increments
OverflowCount. Invalid selection/database/binding/capacity has a separate
internal failure enum.

`OperationMapRenderAssignmentPublishJob` handles two materially different paths:

- On successful selection/assignment, it stores the materialized envelope,
  increments RebuildCount, publishes ActiveSlotCount=retained+assigned,
  matching EnabledSlotCount, active cell/placement counts, overflow/deficit
  metrics and a nonzero command version. This can truthfully publish zero
  active slots.
- On a selection/assignment failure, it forces state/metrics OverflowCount to
  at least one and HighestDeficit to at least one, then returns without a new
  successful rebuild or command publication. Consequently OverflowCount=1
  alone does not prove a pool is undersized: it can also represent a validation
  failure. Do not increase fixed budgets based on that value.

`OperationMapRenderProxyApplySystem` validates one fixed command per fixed slot
and schedules the real proxy application. Its completion job sets
InitialViewApplied=1. The command buffer retains the fixed pool capacity even
when no slot is active. Thus a completed empty view can have initialized=1,
firstViewApplied=1, rebuilt=1, active=enabled=0, positive capacity and no overflow.

## What the diagnostic signatures establish

| Actual diagnostic signature | Meaning and next inspection |
| --- | --- |
| Database count greater than one | Ambiguous native ownership; inspect active map/scene/database owners, not camera zoom. |
| Missing readiness/state/metrics | Packed owner contract incomplete; inspect bake/subscene ownership. |
| Initialized=0 | Native packed initialization has not completed; inspect active map generation and initialization errors. |
| Initialized=1, RebuildCount=0 | Allocation exists without a successful published view; inspect unique valid runtime camera, canonical state and selection/assignment errors. |
| RebuildCount>0, InitialViewApplied=0 | Successful publication has not been applied; inspect fixed commands, version and proxy apply. |
| Either overflow nonzero | Fail closed; distinguish internal validation failure from actual bucket deficit before changing anything. |
| Active and enabled counts disagree | Published state/metrics inconsistent; inspect their actual command version and dependency/publication path. |
| Initialized/applied/rebuilt, zero overflow, active=enabled=0, sufficient capacity | Completed empty packed allocation is possible; current strict opening gate still fails its positive-slot invariant. Expected scenery remains unproven. |
| All existing predicates pass but failure branch emitted | Inspect exact captured frame/owner and any diagnostic/gate race; never reinterpret the failure as a pass. |

The current diagnostic records state.ActiveEnvelopeMin/Max, ActiveCellCount,
ActivePlacementCount, canonical state length, state-sync counts/revision,
commands length/version and camera snapshots. These narrow the branch but do
not enumerate the blob's selected cells or its eligible rows. For a completed
empty signature, independently count actual blob cells within the published
materialized envelope, their unique placements, current canonical matches,
prototype parts and per-bucket required rows. Compare those counts with the
published metrics, and retain the actual camera matrices and source identity.
Without that census we cannot call a close cleared-base image legitimate zero
expected scenery or identify an allocator omission.

Resident exceptions, dynamic gameplay units/buildings and separate model/impostor
systems are not counted by packed ActiveSlotCount. The diagnostic PNG may show
some visible actors while packed slots are zero. Conversely, positive packed
slots do not establish readable infantry or comfortable aircraft framing.
The rendering gate and the owner's camera acceptance remain separate.

No runtime predicate, scenery selection, fixed capacity, source geometry or
camera profile was edited by this trace. If the native fields prove a completed
empty view, preserve that failed evidence and propose an explicit source-backed
opening validation design for review rather than silently accepting zero slots.
