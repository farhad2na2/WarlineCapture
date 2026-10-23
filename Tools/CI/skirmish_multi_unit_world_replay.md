# Bounded multi-unit world replay

`skirmish_world_replay.py` accepts `multi_unit_world` cases under the separate `IsolatedMultiUnitCombatScheduleV1` profile. It composes production request selection, adaptive budgets, goal reservations, seeded goal search, route search, retry restoration, movement, engagement, damage, infantry death, and base-assault outcome kernels. No Unity process or package download is launched by the runner.

This profile requires a passing native capture containing all 15 specified systems, including `UnitManualMoveRetrySystem`. The older stationary and moving profiles retain their own captured memberships. Every case binds to the SHA-256 of the complete `profile.json`, including all supplied captures. Captured source/settings pins, compiled source bytes, output binaries, and the native log are verified; changes invalidate the build.

```sh
rtk proxy python3 -B Tools/CI/skirmish_world_replay.py build --project . \
  --output /private/tmp/multi-world-build \
  --schedule-receipt /absolute/path/stationary-receipt.json \
  --moving-schedule-receipt /absolute/path/moving-receipt.json \
  --multi-unit-schedule-receipt /absolute/path/multi-unit-receipt.json
rtk proxy python3 -B Tools/CI/skirmish_multi_unit_world_replay_test.py \
  --build /private/tmp/multi-world-build --output /private/tmp/multi-world-tests \
  --native-log /absolute/path/verified-native.log
```

Use new output directories for every run. Receipts use the same strict schema as the moving runner: `schema_version`, `order`, `native_log`, `native_log_sha256`, `source_pins`, and `settings_pins`. The new capture marker is `[MultiUnitWorldSchedulingNativeOrder]`; the required suite marker is `[SharedMultiUnitPathParity] result=Passed`. A failed marker or mismatched source, log, order, or binary fails closed. The combined suite marker is an accepted superset for the old profiles only when their own distinct capture markers and memberships are present.

## Input additions

Start with the schema in [the moving replay guide](skirmish_moving_world_replay.md). Change `kind` to `multi_unit_world` and `profile` to `IsolatedMultiUnitCombatScheduleV1`. Every infantry movement object adds:

```json
"retry": {"has_cooldown": false, "resume_frame": 0, "manual_group_member": false}
```

`manual` and the corresponding acquisition `manual_move` must agree. Initial requests and targets are ordinary input orders, never future positions or health. A cooldown is component membership distinct from its frame value. Long-distance metadata can arise from the shared manual routing fallback; initial hierarchical routes, vehicles, air, boarding, and campaign-guided movement remain unsupported.

Each frame adds `engine_frame`, `path_job_wall_seconds`, and `query_orders`. The query orders include the ten existing moving orders plus `manual_pathfinding` and `retry`. Each order supplies all initial IDs exactly once, with current component membership filtering derived by the host. These are explicit observed or synthetic enumeration facts; native archetype or parallel hash order is not inferred from entity IDs. The original top-level query order object also includes the two new names. Per-frame orders are authoritative during the frame.

`engine_frame` is a nonnegative, nondecreasing observed Unity frame counter independent of simulation delta and elapsed time. `path_job_completed` controls publication of computed paths. `path_job_wall_seconds` is the observed native interval used by the adaptive budget when a job completes; the host's execution speed does not substitute for it. Zero simulation delta does not imply paused retry restoration or acquisition. Synthetic controls must be identified as synthetic in map provenance.

The budget starts at the production value of one request. Shared admission visits manual requests first, then ordinary requests excluding manual-tagged actors, preserving supplied query order. It increases or decreases only through the actual production reducer. Reservations are shared across each admitted batch. The production reservation prepare behavior is preserved: equal grid sizes retain storage and advance the generation; wrap clears storage. Goal assignment uses native seeded ring ordering, placement/surface checks, footprint reservations, start-cell bypass, and desired-goal restoration. Goal placement deliberately uses the native nonmanual legality mode; route search uses the actor's actual manual state.

## Timing and boundaries

Pending paths are derived from the scheduled snapshot. They apply only after the supplied completion observation and only if the current request still matches. Cancellation discards the result. Result application can create a retry cooldown. Retry restoration reads all pre-playback component facts: clearing an expired cooldown cannot restore the request in the same update. Its commands play at the end of the retry system, before the captured pathfinding phase.

Supported grids remain at most 64 by 64, with single-cell infantry. Ordinary requested distances remain within the shared 32-cell bound; manual requests remain within the shared manual bound. The host rejects scheduler requests that require unsupported ignored-target occupancy or hierarchical segmentation. Static surface arrays are complete explicit facts. Native parallel hash iterator order is not reconstructed from insertion order. Multiple living units may share an acquisition cell only when every armed acquirer can evaluate at most one distinct eligible enemy identity from that cell, including ally engagement referrals. Repeated evaluations of the same identity are order-insensitive; multiple eligible identities reject the case. The old moving profile retains its stricter single-occupant guard.

Each result declares `full_battle_simulated=false` and `mission_qualified=false`. Synthetic squad route/combat/result cases and the 120-case benchmark exercise simulator integration; they do not qualify the 120 mission catalog, normal player input, UI, ARIA, economy, arbitrary AI behavior, vehicles, presentation, or device acceptance. Native trace comparisons report their exact captured subset and numerical tolerance separately from synthetic scenario outcomes. Failed attempts are retained.

The first native multi-unit trace serializes exact completion inputs and adaptive-budget snapshots, but Unity JSON omits the nested full budget-state structs. Its comparison reports `native_budget_full_state_serialized=false`; full-state reducer behavior is covered by the separate native kernel tests. Actual admitted prefixes are observed; unselected query-order tails in the replay adapter are explicitly synthetic, so the trace does not establish full native query enumeration equivalence.
