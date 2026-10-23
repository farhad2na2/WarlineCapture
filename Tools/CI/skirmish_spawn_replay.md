# Shared spawn, blocker and actor snapshot replay

This tool runs the exact production C# kernels in a standalone .NET 8 process. It checks deterministic placement search, seeded reservation sequencing, static-blocker updates and the health/weapon overlay on explicitly supplied actor component facts. It does **not** initialize or qualify an entire mission.

Use it after editing a spawn snapshot or draft, without starting Unity. Installed .NET 8 and Python 3 are required; the build clears NuGet sources and never downloads dependencies. All commands below use the repository RTK convention.

```sh
rtk proxy python3 Tools/CI/skirmish_spawn_replay.py build \
  --project . --output /private/tmp/spawn-build
rtk proxy python3 Tools/CI/skirmish_spawn_replay.py run \
  --build /private/tmp/spawn-build --input /private/tmp/spawn-input.json \
  --output /private/tmp/spawn-result
rtk proxy python3 Tools/CI/skirmish_spawn_replay_test.py \
  --build /private/tmp/spawn-build --output /private/tmp/spawn-tests \
  --native-log Logs/MacSharedSpawnInitialization20261024/native-r2.log
```

Choose a new output directory each time. Outputs are rejected inside Assets, Packages, ProjectSettings, Design and .git. The wrapper pins shared kernels, native adapters, relevant tools, Unity version and package manifests. It records compiled binaries, captured JSON and binary data hashes, then checks sources, inputs and binaries again after execution. Any changed source requires a fresh build. Missing LFS payloads, incomplete schemas, nonfinite values, oversized work or mismatched hashes fail closed.

## Snapshot kinds

The root JSON has exactly `schema_version: 1`, `artifacts: []` and `cases: []`. Each case has an `id`, `kind` and complete facts; an optional `expect` checks a partial result object, with exact array values. The executable schemas and runnable fixture constructors are in `skirmish_spawn_replay_test.py`. Native structs use their exact public C# field names; host fields use snake_case. Unknown and omitted fields are rejected.

| Kind | Inputs | Result and boundary |
| --- | --- | --- |
| `actor_initialization` | Complete existing-component `state`, scalar `overlay`, `shot_interval` | Shared health and weapon binding. Absent weapon components stay absent; this is not prefab baking or role selection. |
| `initial_search` | Full grid walkable/blocked/occupied/reserved facts, center, footprint, radius, live RNG state, air flag | Shared native ground/air search, final RNG state and reservation bytes/hash. |
| `surface_correction` | Full grid masks, reserved candidate, exact complete-footprint surface-admission mask, matching-surface facts | Shared release/retry behavior. Missing/ambiguous matching surfaces retain the original reservation exactly as native does. |
| `blockers` | Exact owner epoch/grid identity, count/blocked/friendly arrays and ordered new/existing/removed snapshots | Shared invalidation, opening geometry, overlap counts, friendly pass and deferred history/opening changes. |
| `placement_sequence` | Full grid runtime snapshot, optional blocker update, all existing footprints, ordered actors and complete surface profiles | Shared coordinate conversion, stable-ID seeded search, per-actor reservation rebuild, optional actor overlay and derived cells. Stops on the first failure. |

Boolean grid arrays are row-major `x + z * width`. Inline arrays are limited to 65,536 cells. Full maps use artifact references: `{"artifact":"walkable"}` with a root descriptor `{"id":"walkable","path":"/absolute/map.bin","sha256":"…","bytes":2097152,"encoding":"u8"}`. Counts use `i32-le`; bytes use `u8`. Every array must have the exact full-grid length. The wrapper snapshots binary inputs into the result directory, with both original and captured hashes. It never crops a map.

Limits include 256 cases, 16 million cells per grid, 512 MiB aggregate binary inputs, positive footprints up to 64×64, and explicit aggregate worst-case work limits. Large output arrays use SHA-256 summaries. Reduce requested work when rejected; changing the grid to a crop would change the tested problem.

## Exact semantics and supplied facts

Ground search tries the clamped preferred cell, 128 random pairs, perimeter rings and finally the entire grid. Air's initial preferred-cell rule differs intentionally. Seeded sequence replay uses the production UTF-8 stable-ID fold and installed Unity-compatible random algorithm; a live `initial_search` uses the supplied already-initialized RNG state. Each sequence member rebuilds reservation cells from all supplied existing `UnitGrid`/footprint facts plus prior successful derived placements. Each actor declares `has_footprint_component`: absence uses the native 1×1 search fallback but excludes that successful actor from the next reservation query. Existing membership and native order must be captured or explicitly synthetic; occupancy alone is not a substitute.

A surface profile is admission for its **complete exact footprint and vehicle category**, supplied by the native `CanTraverseFootprint` callback. A 1×1 infantry cell mask is not a proof for a vehicle or larger footprint. `NativeCapturedSnapshot` is a provenance label, not an authenticated receipt; native equivalence is established separately by the trace test and its verified wrapper evidence.

Blocker inputs preserve the three native query phases and caller order. New/removal history and opening structural changes play back after all three query phases; existing history writes are immediate. Friendly-pass overlap keeps the native last-write and removal-clear behavior. Epoch/revision values are unsigned 64-bit integers, including exhaustion; do not round them through JavaScript numbers. Blocker-only snapshots explicitly list unavailable walkable, occupied and reserved arrays instead of inventing them. Actor overlay comparisons use exact float32 bit patterns for native decimal JSON, and exact discrete state.

Sequence replay requires explicit `placement_mode`: `Untyped` requires each actor’s `typed_occupied` to be null; `CapturedTypedOccupancy` requires a complete actor-specific occupied mask that already combines actual occupancy with native typed-pad restrictions. The mask must preserve every actual occupied cell and is applied before that actor’s search and surface correction. The tool does not select pads or execute the typed-pad policy; `typed_pad_constraints_evaluated` remains false. Native observation must establish the supplied masks’ meaning. Real-export probes always use `Untyped`.

The sequence result names its position `ungrounded_cell_center`: actual surface grounding has not run. Full actor initialization requires the real prefab/registry, baked movement and other components, actual role resolution and current scene state. Starting buildings, resources/economy, ownership, full scenario hooks, runtime surface birth, cleanup on spawn failure and session launch remain outside this tool. `full_mission_initialized` and player-readiness claims remain false. A successful search does not prove a legal objective, route, role placement or normal-input mission win.

## Real Campaign map admission probes

First obtain a fresh verified export using the repository wrapper and `Game.Tests.Editor.SharedSpawnInitializationParityTests.RunFocusedValidation`, with `-skirmishOfflineExport <new output directory>`. The combined native suite exports and repeats the actual Campaign/configuration bundle. See `skirmish_mission_data.md` for the standalone export and verified-receipt contract. Changing pinned sources invalidates the export: there is no partial-source exception.

Then choose an **exact exported map key and typed anchor ID**, not a guessed map name:

```sh
rtk proxy python3 Tools/CI/skirmish_spawn_replay.py probe-export \
  --build /private/tmp/spawn-build \
  --bundle Logs/MacSharedSpawnInitialization20261024/export-r2 \
  --native-receipt /absolute/verified-export-receipt.json \
  --map-key EXACT_EXPORTED_KEY --anchor-id EXACT_EXPORTED_ANCHOR \
  --count 120 --output /private/tmp/spawn-map-probes
```

This first performs the entire strict mission-data integrity/source/receipt check. The map must be a current Campaign physical source with complete matching authored grid and decoded native surface facts. It then runs the shared placement sequence on the full grid for distinct stable IDs/seeds, using actual authored walkability and native 1×1 infantry admission, while clearly constructing **hypothetical empty runtime blockers and occupancy**. Probe actors explicitly assume a 1×1 footprint component; reserved cells accumulate from prior probe placements; no future cells are injected. The anchor supplies a geometric starting point only, not a validated scenario spawn request.

`probe-report.json` reports requested, placed and unplaced counts, all-requested-placed, dimensions, map/anchor IDs, build/input/export hashes and scope. The command fails if any requested placement fails, while retaining evidence. Runtime spawn qualification and actual prefab initialization remain false even if all 120 placements succeed. These are repeated full-map admission probes, not 120 initialized missions.

## Verified evidence, 2026-10-24

The final standalone suite passed 16 groups. Native fixture comparison matched 128 actor overlays, 18 placement/surface cases and 14 static-blocker update stages, including unsigned epoch exhaustion. Actor float fields were compared as float32 bit patterns; discrete state and every captured storage byte/count were exact. Native source/managed/Burst checks and the installed Unity random proof are separate evidence: the installed MathematicsModule comparison covered 1,000,000 random draws and 100,000 seed/hash cases.

The verified fresh native export is `Logs/MacSharedSpawnInitialization20261024/export-r2`, with `verified-receipt-r2.json` in its parent and native trace log `native-r2.log`. A full-grid probe used map key `0a2117ca560a44d4099b1fb448c92c0c`, anchor `anchor.ch01.m05.squad_a`, all 2,097,152 cells, and 120 distinct probe stable IDs/seeds. Both runs placed 120/120 with exact result equality. Replay engine times were 0.1885 and 0.1904 seconds; complete strict export verification, artifact capture and replay took 5.954 and 5.983 seconds. These figures describe hypothetical empty-overlay admission probes on one actual Campaign source, not 120 initialized missions or normal-input wins.
