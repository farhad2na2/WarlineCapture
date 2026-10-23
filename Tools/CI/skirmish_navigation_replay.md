# Shared navigation replay without Unity

`skirmish_navigation_replay.py` compiles the production occupancy, path placement, bounded A* and routing retry kernels byte for byte into .NET 8. It replays actor lifecycle changes and route requests against the resulting occupancy counts. No Unity Editor, package restore feed, physical map generation or replacement navigation algorithm is used.

## Run

Use new output directories on each run. An installed .NET 8 SDK and RTK are required.

```sh
rtk proxy python3 -B Tools/CI/skirmish_navigation_replay.py build --project . --output /private/tmp/navigation-build
rtk proxy python3 -B Tools/CI/skirmish_navigation_replay_test.py --build /private/tmp/navigation-build --output /private/tmp/navigation-tests
rtk proxy python3 -B Tools/CI/skirmish_navigation_replay.py run --build /private/tmp/navigation-build --input /private/tmp/navigation-tests/navigation-examples.json --output /private/tmp/navigation-replay
```

Tests write `examples.json` (occupancy), `navigation-examples.json` (composed navigation), and `routing-examples.json` (routing retry/state control) as reusable schemas. Build and replay directories contain source/binary hashes, captured input, complete subprocess logs and JSON reports. Source or binary drift invalidates a replay; rebuild after any pinned source changes. Inputs and outputs are never written to Assets, Packages, ProjectSettings or `.git`.

## Trace contract

Root: `schema_version: 1`, `cases: [...]`. Each case requires `id`, `width`, `height`, `steps`, and a nonempty `expect` object. Optional `map` becomes mandatory for any placement or path query. Arrays use row-major `z * width + x` order. Expectation objects may specify a subset of output fields, but cannot be empty; arrays compare exactly in order.

Occupancy steps:

- `add` / `update`: `entity` (nonempty string), `cell_x`, `cell_z`, `size_x`, `size_z`; optional `manual_group` flag (0/1). Footprints use the production center-cell rule and clipping. Updates preserve manual-group membership unless explicitly changed.
- `remove`: `entity`. Duplicate adds and missing removes/updates fail. Removing and re-adding a name receives a fresh internal identity, so stale membership is not reused.
- `snapshot`: records full counts, occupied mask and member count.

The replay maintains both occupancy counts/mask and the same live member footprints/identities consumed by placement. It never substitutes the mask alone for production vehicle soft-blocker, manual-group or ignored-entity rules. Every added actor is explicitly a member of the navigation snapshot; Unity ECS inclusion/exclusion filters and change detection are validated separately by native tests.

`map` requires complete `walkable` and `dynamic_blocked` 0/1 arrays, plus `friendly_pass` faction IDs (0..255). No omitted cell or unknown map is assumed clear.

Placement/path queries require `kind`, registered `entity`, `target_x`, `target_z`, `vehicle` and `manual_move` booleans, and `faction` (0..255). Optional `ignored_entity` must name a different registered actor. `placement` reports `allowed` using the production policy.

A `path` also requires `search_padding` (0..512), `max_expansions` (0..30000), `heuristic_weight` (1..1000), and `surface`:

- `provenance`: nonempty source reference; synthetic controls must identify themselves as synthetic.
- `cell_pathable`, `footprint_pathable`: complete 0/1 arrays evaluated for this specific requester, footprint and movement class.
- `cardinal_cost`, `diagonal_cost`: complete positive traversal cost arrays (1..10000).

Surface arrays must come from the corresponding Campaign snapshot/production surface policy for real mission checks. The provenance string documents a supplied claim; this tool does not authenticate it, load a mission catalog, derive those arrays, or establish source-map fit. Input hashes preserve exactly what was supplied. Synthetic maps in the tests are algorithm controls, not new game maps.

The shared start validator and one bounded A* attempt return ordered cells, failure code, expansion metadata and a monotonically increasing scratch epoch. Scratch arrays are reused between requests in the same case. Requests do not move actors; apply explicit `update` steps at the actual movement cadence. Failed searches return no invented fallback route. The production direct-path fast path preserves its original sampling, including the adjacent-cell shortcut. Its corner behavior differs from the A* neighbor expansion, whose diagonal orthogonal checks are retained. A route is therefore evidence of this production algorithm's result, not an additional geometric safety guarantee.

## Routing retries and preserved state

`kind: "route"` composes the same occupancy, placement and surface facts with the production routing coordinator. It requires all `path` fields, `heuristic_weight: 1`, and a strict `routing` object:

- `desired_goal_x`, `desired_goal_z`: original requested destination, separate from the assigned `target_x/z` (for segmented routes).
- `goal_search_radius` (0..20), `max_candidate_attempts` (0..32): explicit alternate-goal limits.
- `cheap_mode` (0..255), `has_modes`, `skip_alternate_search`: raw mode and availability/skip flags. Only available mode 3 enables the weighted failure-9 retry.
- `initial_state`: every production metadata field: `GoalX`, `GoalZ`, `FailureCode`, `ExpansionCount`, `AlternateAttempts`, `Status`, `Segmented`. The initial assigned goal must equal `target_x/z`; status and segmented are 0/1. Metadata is not silently reset.

`manual_move` remains the common query flag; it enables the production manual segmented progress fallback after exhausted/disabled alternate search. The original policy tries normal search first, a weight-2 detailed-hauler retry only after failure 9, legal alternate goals in square-ring order, then manual progress. The shared observer reports each attempted goal, epoch, heuristic weight, found/failure result and expansion metadata. No offline copy of the retry policy decides the result.

Every route reserves the native 128-epoch slot; weighted search uses base+1 and legal alternate attempt N uses base+N+1. Subsequent route or bounded-path queries cannot reuse these scratch epochs. Counts and ordered cells are retained exactly. Successful initial/weighted routes preserve previous alternate/segmented metadata where the original job does. A successful manual fallback can retain the failed search's `FailureCode` and `ExpansionCount`; use `Status`, `Segmented` and attempt evidence together rather than treating retained failure metadata as a new failure.

The runner accepts explicit search budgets for small algorithm controls. The native adapter still supplies its fixed production limits; a synthetic 15-expansion vehicle fixture is not evidence for the production 12,000-expansion detailed-hauler workload. Real replay inputs must supply the actual native search padding, budget, modes and goal limits. Surface provenance remains caller-supplied, not authenticated. Route queries charge actual placement work against the aggregate replay work budget, including retries and fallback candidates.

The routing controls include a 15×15 synthetic wall detour where normal search exhausts its 15-expansion budget and the weighted retry succeeds in 13 expansions, the exact compressed path and attempt order, alternative-goal assignment, fallback cells and preserved metadata, unavailable/raw mode distinctions, exhausted weighted retry, early invalid-start metadata, repeat epochs and a route followed by a bounded search. These controls extend the earlier suite; they are not mission completions.

## Scope and evidence

The suite checks independently recomputed occupancy after 4,000 seeded lifecycle changes; exact hand-computed detours and repeated search epochs; surface-cost choices; vehicle turnpoint compression; occupied goal removal; manual groups; friendly gates; ignored actors; vehicle soft/hard blockers and boundaries; start/goal/surface failures; bounded expansion and search windows; deterministic bulk replay; malformed input; source/binary tampering; and output isolation.

Limits: 16 MiB input, 10,000 cases/steps, 512 by 512 grids, aggregate estimated work/output budgets, 120-second subprocess timeout. Duplicate keys/IDs, unsupported fields, partial map/surface arrays, wrong types and unsafe lifecycle operations fail closed. Conservative work limits can reject large traces; split those into smaller independently pinned runs rather than weakening checks.

Reports always retain `native_runs: 0`, `full_battle_simulated: false` and `mission_qualified: false`. This is shared occupancy + placement + bounded search + production retry/fallback orchestration, with explicit surface facts. Initial goal assignment/reservations, hierarchical planning and segment construction, path publication/scheduling, surface export, targeting, AI/economy and vehicle/air motion remain outside this runner. It complements the shared combat/infantry movement replay; it does not yet run a complete autonomous mission or replace native UI/ARIA, normal-input and device acceptance.

## Verified candidate, 2026-10-23

The final standalone suite passed nine groups: 104 independent occupancy traces,
24 route/placement controls and 720 synthetic cases repeated identically in
0.0884/0.0896 seconds. These measure algorithm replay, not completed missions.

Mac Support ran the checked GUI wrapper and passed 41 combined methods, including
256 seeded search runs, 24 named search runs, 4096 raster controls, 8192 placement
cases, 4096 occupancy bounds and 1024 occupancy delta sequences. Actual Burst
execution was verified; original and extracted search paths, failure metadata
and scratch arrays matched exactly. All 99 source/settings pins were unchanged.
Entry: `Game.Tests.Editor.GridPathSearchKernelTests.RunCombinedValidation`.
Required marker: `[SharedNavigationCombatParity] result=Passed`.

Native receipt: `Logs/MacSharedNavigation20261023/result-r3.json`; full log:
`Logs/MacSharedNavigation20261023/native-r3.log`, SHA-256
`5399456e967e8b708951d8095a761dceea0374c4bbe4d16762843394a260558d`.
The failed missing-import attempt and original-writer-address failure are retained.
The adapter now pins the original writer only during synchronous search and keeps
stream safety checks enabled. Four previously attributed Unity.Entities global
buffer shutdown allocation warnings remain unresolved and unsuppressed. Native
UI/ARIA and device acceptance are separate gates.
