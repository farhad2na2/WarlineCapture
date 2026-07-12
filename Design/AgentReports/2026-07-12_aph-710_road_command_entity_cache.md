# APH-710 Road Command Entity Cache

Date: 2026-07-12

Baseline: `93c8df15b`

Status: Stable partial slice; APH-710 remains active

## Result

`RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity`
created and disposed an ECS query during every road-build runtime poll. APH-007,
APH-213, and the current pre-change capture measured this owner at 11,960 bytes
across 299 of 299 frames.

The helper now delegates queue ownership to `RoadBuildCommandEntityCache`. The
cache stores only the owning `World` and queue `Entity`; it does not retain a
native query and therefore requires no disposal lifecycle. A query is created
only on cold binding or exceptional recovery. The fast path verifies world
identity, entity existence, queue marker presence, and required buffers.

The existing helper shrank below its source-growth ceiling. No public command
API, request/result semantics, processing order, road behavior, or ECS system
ownership changed.

## Evidence

- Road command focused validation passes `11/11`.
- A warmed helper performs 300 empty command polls with zero current-thread
  managed allocation.
- One helper rebinds across two worlds without sharing request IDs or entities.
- Destroying the cached entity creates and repairs a replacement.
- An existing queue entity without buffers is adopted and both buffers are
  restored.
- The fresh 180-warmup/300-frame raw capture contains no
  `RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity` row.
- Previously removed scene-root, audio, and tactical-camera rows remain absent.
- The warmed capture reports 187,690 player-relevant bytes. A first 206,812-byte
  run was rejected as comparison evidence because unrelated pathfinding and
  build-production activity was higher; both runs omit the targeted road row.
  The unchanged 1,024-byte global gate remains red.

## Validation

- `RoadBuildCommandRequestValidation`: passed `11/11`.
- `ProductionSourceGrowthArchitectureValidation`: passed `15/15`.
- `ScriptArchitectureBoundaryValidation`: passed `31/31`.
- `EcsBurstHotPathArchitectureValidation`: passed `10/10`.
- `Game.Runtime.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.
- `git diff --check`: passed.

## Residual Risk

Building camp/production/placement command queries, transport helper
construction, UI shell attribution, pathfinding diagnostics, intermittent
selection-panel allocations, and hidden default-world lookups remain. APH-710
stays active until each owner is removed or explicitly reassigned.
