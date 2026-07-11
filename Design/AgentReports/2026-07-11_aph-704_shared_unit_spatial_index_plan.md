# Shared Unit Spatial Index Benchmark Plan

Date: 2026-07-11
Tracker item: `APH-704`
Decision: benchmark in shadow mode before adopting.

## Current Query Owners

| Domain | Owner | Current behavior |
|---|---|---|
| Building defense | `BuildingDefenseAttackSystem` | Every 0.12 seconds, collects valid ground targets and scans the complete candidate array for each tower. |
| AI targeting | `AITargetingSystem` | Every 0.5 seconds, scans faction/grid/health chunks for every attacking squad. |
| Threat detection | `ThreatDetectionWarningSystem` | Scans target chunks for each detector; every frame at 32 targets or fewer, otherwise every 0.2 seconds. |
| Rectangle selection | `VisibleUnitSelectionCandidateSystem` | Rebuilds the complete player-unit snapshot every frame. |
| Managed camera selection | `VisibleUnitSelectionCameraSystemHelper` | Repeats candidate collection for camera filtering. |
| Click selection | `FocusableUnitLookupCameraSystemHelper` | Already uses a changed-filtered managed cell lookup; excluded from the first shared-index migration. |

## Baseline

- Match fixture: 733 units, 628 runtime buildings, `2.486 ms` average and `3.824 ms` p95, zero current-thread allocation.
- Building-defense acquisition: approximately `1.025 ms` combined (`0.121 ms` collection plus `0.904 ms` selection).
- Building benchmark: 32 towers and 740 candidates, with zero managed allocation after warmup.
- AI fixture: 160 enemy units, 48 threats, 64 buildings; `0.039 ms` average and `0.136 ms` p95, zero allocation.
- Visible selection: recent Android average `0.126 ms`, maximum `1.38 ms`.
- Threat detection has no isolated timing baseline; the benchmark must add one before adoption.

## Proposed Shadow Design

Add a Burst-compatible, read-only index built once per simulation frame after movement:

1. `Assets/Game/Scripts/Components/UnitSpatialIndexComponents.cs`
2. `Assets/Game/Scripts/Systems/UnitSpatialIndexBuildSystem.cs`
3. `Assets/Game/Scripts/Systems/UnitSpatialIndexQuery.cs`

The build owner must be an `ISystem`. It may not allocate managed memory, perform structural changes in the measured update, look up the default World, or add a new completion fence. Initial benchmarks compare 16-, 32-, and 64-cell buckets.

Each entry retains its original ECS query-order index in addition to entity, grid/world position, faction, health, and compact classification flags. Consumers continue to own their scoring rules. Preserving global source order is mandatory because bucket order would otherwise alter equal-distance and equal-score winners.

## Equivalence Contract

- Building defense preserves planar squared distance, inclusive range, exclusions, faction rules, four-slot order, and original-query-order ties.
- AI preserves Manhattan scoring, health/category/priority modifiers, neutral/dead filtering, and the first query result for equal scores.
- Threat detection preserves Chebyshev radius, exclusions, air/ground classification, approach/ETA calculation, previous-threat suppression, counts, and warning priority.
- Selection preserves player filtering, zero-unit clearing, global order, vehicle classification, and camera projection behavior.
- Every consumer observes same-frame movement and health changes; stale index reads are forbidden.

## Execution

1. Add fixed-edge-case and at least 100 seeded equivalence fixtures before changing a production consumer.
2. Add an alternating-order direct-versus-indexed shadow benchmark.
3. Freeze the best measured bucket size only after evidence.
4. Migrate building defense first, then threat detection, AI targeting, and rectangle-selection candidates.
5. Leave the existing click-selection lookup unchanged during this slice.
6. Run each existing domain suite after each migration and add Match profiler markers for index build/query cost.

## Adoption Gate

Adopt only when all conditions pass:

- Zero result mismatches across deterministic and 100-seed fixtures.
- Zero managed bytes after warmup.
- No new completion fence, structural sync point, or managed World lookup.
- Combined index-build plus indexed-consumer median and p95 improve by at least 10%.
- Building acquisition improves over `1.025 ms`.
- No individual consumer regresses by more than 5%.
- Frozen-fixture Match p95 remains at or below `3.824 ms`, subject only to the accepted same-revision variance series.
- Stable index memory remains at or below 256 KiB for 740 units, with no capacity growth after warmup.

If these gates fail, retain the direct-query implementation and record the shared index as a rejected candidate.
