# Bounded world replay without Unity

`skirmish_world_replay.py` compiles the unchanged production scheduling, target acquisition, standard attack, direct damage, BA objective, clock and freeze kernels into a local .NET 8 executable. It also exposes shared squad targeting through `kind: "squad_target"`. It performs no downloads and launches no Unity processes.

This tool tests **synthetic stationary world configurations and captured snapshots**, not complete missions. Every report states `full_battle_simulated: false` and `mission_qualified: false`. Passing 120 synthetic configurations is not 120 mission wins or player readiness. Existing Campaign map reuse and native acceptance gates still apply.

## Build and run

Use an installed .NET 8 SDK and a fresh directory outside the repository:

```sh
rtk proxy python3 -B Tools/CI/skirmish_world_replay.py build \
  --project . --output /private/tmp/world-build \
  --schedule-receipt /private/tmp/verified-native-order-receipt.json
rtk proxy python3 -B Tools/CI/skirmish_world_replay.py run \
  --build /private/tmp/world-build --input /private/tmp/world-cases.json \
  --output /private/tmp/world-run
rtk proxy python3 -B Tools/CI/skirmish_world_replay_test.py \
  --build /private/tmp/world-build --output /private/tmp/world-tests
rtk proxy python3 -B Tools/CI/skirmish_squad_targeting_test.py \
  --build /private/tmp/world-build --output /private/tmp/squad-tests
```

Omit `--schedule-receipt` for a squad-only build. World cases then fail explicitly; they are never silently skipped. Build manifests pin every compiled source, native adapter dependencies, binaries and `profile.json`. Run checks hashes before and after execution, including all additional native source/package/settings pins from the receipt. Changed source, LFS pointer sources, incomplete manifests, altered binaries, invalid JSON shapes and unknown features fail closed. Inputs are retained with SHA256, stderr/stdout and report files.

The receipt must have exactly `schema_version`, `order`, `native_log`, `native_log_sha256`, `source_pins`, `settings_pins`. `schema_version` is 1. `order` is the JSON emitted after `[WorldSchedulingNativeOrder]` by the native suite: profile `IsolatedStationaryCombatScheduleV1`, scope `NativeSortedSubsetOnly`, `full_world: false`, and the nine sorted system names. The builder requires the original log hash, matching captured order, the fixed `[SharedWorldTargetingParity] result=Passed` marker, no failed marker, current native source hashes, and ProjectVersion/manifest/packages-lock hashes. Additional native test source/settings pins are retained. An arbitrary provenance string is insufficient. A receipt for a whole native sorted subset is **not evidence of full live-world system order**.

The validated native subset currently sorts projection → BA evaluation → occupancy → acquisition → engaged movement → attack → outcome, between Begin/EndSimulation ECB. The host executes those captured phases separately. Projection evaluates BA without surrender first, matching the native terminal precedence; the later BA stage considers surrender only if projection did not already produce a terminal result. Damage after projection becomes an objective fact on the next frame. A build embeds the captured profile and each world case must supply its exact SHA256.

## World input contract

The top level is `{"schema_version":1,"cases":[...]}`. Each case requires `id`, `kind: "world"`, `profile`, `profile_sha256`, `cell_size`, `actors`, `player_base`, `enemy_base`, `deadline_seconds`, `policy_profile: "unrestricted_visible_no_fog"`, `frames`, and nonempty `expect`. Expected objects may select fields, while expected arrays must match length and order. The Python fixture functions in `skirmish_world_replay_test.py` provide complete examples and can generate JSON after setting its `BUILD` path. They use synthetic positions and explicitly label that scope in benchmark output.

Actor fields are exact:

- `id`, positive native entity `index` and `version`, `kind` (`infantry` or `base`), health, nonzero byte faction, fixed `x/y/z`, captured `cell_x/cell_z`, zero `radius`.
- `has_unit_grid` must be true for infantry and false for target-only bases. Base cell facts are captured native WorldToCell observations, not a claim that the base has UnitGrid. The host does not invent grid conversion arithmetic.
- `combat_suppressed: false`, `has_animation_order: false`, `protected_records: false`; `has_engage` is distinct from `target: ""` (a null target component is retained).
- Cover eligibility/permille are explicit. Infantry have standard `weapon` facts and complete production `StandardAttackState`; initial animation timer must be zero. Bases have null weapon/acquisition and cannot engage or receive grid cover.
- Infantry `acquisition` includes one common grid width/height, aggro cells, auto-engage byte, attack-move/manual flags, hold position, no path-follow/request, nullable inactive scan, and a recent-attacker ID or empty string. Empty recent means the component is absent; a present RecentAttacker with null identity is outside this profile.

The profile requires HoldPosition, no active scan, single-cell zero combat radius, positive range, and aggro radius covering attack range. Retained or newly acquired targets must lie within half the weapon range. This conservative boundary avoids copying native chase/scan/leash logic into an approximate simulator. No movement is invented. Initial targets and recent attackers must resolve to explicit identities. All actors use default unrestricted visible policies and no paired fog; unsupported policy/structure/air flags are rejected. Native kernel tests cover broader policies independently.

Every frame requires `elapsed_time`, `delta_time`, `paused`, `surrender`, `has_gameplay_state`, `simulation_active` (0/1), and explicit `acquisition_order`, `candidate_orders`, `attack_order`, `damage_commit_order`. Time is finite/nondecreasing. Paused objective time and gameplay simulation activity are separate facts; the host does not infer one from the other. Native acquisition has its 0.12-second cadence and no pause gate. Native attack only checks finished Skirmish; a ready shot can fire at delta zero. Stationary engagement maintenance uses the shared positive-delta/gameplay admission predicate.

`candidate_orders` has an entry for every eligible acquirer: `cells` (key and ordered actor IDs), `buildings` (ordered IDs), and complete `policies` facts for every actor. All acquirers must use one common grid, identical per-cell enumeration order and identical building order. Native spatial hash order is supplied, never sorted or assumed stable. All live in-grid infantry and all target-only bases must be represented. Attacker counts are the immutable pre-acquisition snapshot.

The attack list contains exactly the actors with EngageTarget, including null-target components. All standard plans read the same pre-damage health snapshot, then shared predicted-health sequencing and explicit aggregate commit order determine HP. Acquisition changes and RecentAttacker removal are deferred to EndSimulation. Existing RecentAttacker values update immediately on damage; missing components are added at the attack system’s local ECB playback, before EndSimulation acquisition removals. Newly acquired units cannot fire during the acquisition frame. Unengaged timer recovery calls the same production helper as native attack.

## Boundaries and interpretation

The host derives designated BA base health, clock, terminal state and one frozen result. It stops at the first terminal result or unsupported lifecycle boundary and reports consumed/remaining frames. Mutually lethal attacks preserve both planned shots and report dead mobile actors without inventing teardown. A mobile death is `mobile_death_requires_native_teardown`; a planner clear requiring native cleanup is `engagement_clear_requires_native_cleanup`. Nonlethal damage that could require unshared counter-engagement is rejected. Shared unavailable-target clearing retains EngageTarget component presence.

Not simulated: full-world ordering, squad AI/world insertion, economy, spawns, construction, moving/vehicle/air actors, pathfinding or occupancy evolution, chase/rotation, animation ticking, death cleanup, fog updates, audio/VFX/UI, runtime asset binding, or normal-input playthroughs. Attack animation fields are returned as kernel metadata only; no animation-order component is supported. Other standalone routing/movement tools exercise their shared kernels separately; combining them requires a verified native observation order and their lifecycle adapters.

Input limits are 16 MiB, 256 cases, 128 actors, 2048 frames, and a two-million actor-pair/frame aggregate budget. Grid and range bounds further constrain acquisition work. Processes have the existing standalone runner timeout. The test suite includes two independent synthetic world cases and repeat equality, deferred acquisition, mutual lethal sequencing, paused delta-zero behavior, projection/surrender precedence, cadence/recovery, strict rejection, provenance and binary tamper controls, plus 120 repeated synthetic configurations. Retain failures alongside passing receipts.

The separate moving profile is documented in [skirmish_moving_world_replay.md](skirmish_moving_world_replay.md). Its receipt and limits do not replace this stationary profile.

Multi-unit request budgets, goal reservations, and manual retries use a separate captured profile; see [the multi-unit replay guide](skirmish_multi_unit_world_replay.md).
