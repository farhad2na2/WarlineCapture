# APH-710 Building Production Command Entity Cache

Date: 2026-07-12

Baseline: `012feb64f`

Status: Stable partial slice; APH-710 remains active

## Result

The building production runtime created and disposed one ECS query for the camp
item queue and one for the production queue during every idle Match poll. The
previous capture attributed 11,960 bytes over 299 frames to each lookup.

`BuildingProductionCommandEntityCache` now owns both queue handles. It stores
the owning `World`, each resolved `Entity`, and an explicit negative-resolution
state. Warm existing-entity and absent-entity paths perform no query and allocate
no managed memory. Cold binding, world replacement, destroyed-entity recovery,
and adoption/repair of existing marker entities remain covered.

The broad production runner also exposed a latent buffer-safety issue in runtime
faction summary publication: the first dynamic buffer was retained while adding
the second buffer structurally invalidated it. Both buffers are now ensured before
either writable handle is retained. The frozen helper shrank below its line and
byte ceilings.

## Evidence

- Both recurring building production lookup rows are absent from the fresh
  180-warmup/300-frame raw Match capture.
- The accepted capture reports 145,792 player-relevant bytes, down from the
  previous warmed 187,690-byte capture. The unchanged 1,024-byte global gate
  remains red.
- An initial 386,100-byte capture was rejected after it proved that a simple
  positive entity cache still queried every frame while both queues were absent.
  The negative-cache correction removed those rows in the accepted rerun.
- Warm idle polling and warm existing-entity reads each allocate zero managed
  bytes across 300 iterations.
- One helper rebinds across world replacement, recovers destroyed cached
  entities, adopts existing queue markers, and restores missing buffers.

## Validation

- Building production request validation: passed `26/26`.
- Production source-growth architecture validation: passed `15/15`.
- Script architecture boundary validation: passed `31/31`.
- ECS/Burst hot-path architecture validation: passed `10/10`.
- `Game.Runtime.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.
- `git diff --check`: passed.

## Residual Risk

The global Match allocation gate remains red. Building placement command lookup,
transport helper construction, UI shell attribution, pathfinding, selection-panel
allocations, and hidden default-world lookups remain assigned to APH-710/711.
