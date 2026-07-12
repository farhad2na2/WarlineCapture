# APH-710 Building Placement Command Entity Cache

Date: 2026-07-12

Baseline: `f2028be02`

Status: Stable partial slice; APH-710 remains active

## Result

`BuildingPlacementCommandRequestCompositionSystemHelper` created and disposed an
ECS query during every idle Match poll while no placement command queue existed.
The integrated baseline attributed 11,960 bytes across 299 of 299 frames to that
lookup.

`BuildingPlacementCommandEntityCache` now owns the queue's world/entity state and
an explicit negative-resolution state. Warm absent and present paths avoid query
construction. Cold binding, world replacement, destroyed-entity recovery,
existing-marker adoption, and missing-buffer repair remain covered.

The fresh 180-warmup/300-frame Match capture contains no placement lookup/cache
allocation row. It reports 161,180 total player-relevant bytes; this is cross-run
pathfinding variance and is not accepted as an improvement over the integrated
145,792-byte baseline. The unchanged 1,024-byte gate remains red.

## Integration Repair

Current `origin/main` initially blocked integration because commit `148b3cecf`
grew `FirstLaunchNarrativePlayer.cs` to 595 lines without reviewed-file coverage.
No budget or allowlist was added. Audio routing and presentation-model creation
were moved verbatim into two same-domain controller/factory types, reducing the
player to 487 lines. Narrative behavior and serialized view ownership are
unchanged.

## Validation

- Building placement command validation: passed `16/16`.
- Script architecture boundary validation: passed `31/31`.
- ECS/Burst hot-path architecture validation: passed `10/10`.
- First-launch narrative player validation: passed `5/5`.
- First-launch narrative presentation validation: passed `9/9`.
- Production source-growth architecture validation: passed `15/15`.
- `Game.Composition.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.
- `git diff --check`: passed.

## Residual Risk

The global Match GC gate remains red. Transport helper construction, UI shell
attribution, pathfinding, selection-panel allocations, and hidden default-world
lookups remain assigned to APH-710/711.
