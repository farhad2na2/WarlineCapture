# APH-710 Transport Idle Helper Allocation

Date: 2026-07-12

Baseline: `4d81bd9cc`

Status: Stable partial slice; APH-710 remains active

## Result

`TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests` constructed
transport capacity, air pickup, move-order, and selection-state helpers whenever
the shared command queue entity existed, even when its request buffer contained
no pre-resolved transport command.

The method now performs an allocation-free request-kind scan and returns before
constructing helpers when no relevant command exists. Command-event behavior and
helper ownership remain unchanged. A warmed system with an empty command queue
allocates zero current-thread managed bytes across 300 updates.

## Evidence

- The fresh 180-warmup/300-frame Match capture contains neither the 23,920-byte
  selection-helper constructor row nor the 16,744-byte pre-resolved routing row.
- The two removed rows total 40,664 bytes per capture.
- Current player-relevant Match GC is 122,248 / 1,024 bytes, down from the
  preceding 161,180-byte capture. The unchanged global gate remains red.

## Validation

- Unit transport validation: passed `79/79`.
- Production source-growth architecture validation: passed `15/15`.
- Script architecture boundary validation: passed `31/31`.
- ECS/Burst hot-path architecture validation: passed `10/10`.
- `Game.Runtime.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.
- `git diff --check`: passed.

## Residual Risk

UI shell call-stack attribution, pathfinding allocations, intermittent selection
panel work, and hidden default-world lookups remain assigned to APH-710/711.
