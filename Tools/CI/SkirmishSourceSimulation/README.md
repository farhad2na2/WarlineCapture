# Current-source Skirmish simulation

This CLI hosts the current shared/native S002 gameplay policies over explicit finite engine/data adapters. It does not launch Unity or build a player. The initial supported profile is S002, seed 104731, fresh config-initialized ground BaseAssault, with the qualified installed macOS Mono numeric lane (`--optimize=-float32`). Other missions/seeds and consumed unsupported domains fail explicitly.

The source manifest links current repository and embedded/package sources directly. Original methods/types in adapter closures regenerate from current native source by explicit member and structural bindings. Original native whole-file hashes are receipts, not routine-edit admission locks. Unsupported method/API shapes fail before emitting a qualified build. Five relevant authoring/getter/factory source guards remain operational baked-metadata cache checks: edits there may require metadata regeneration. Ordinary shared gameplay changes compile directly; this is not a claim that every source edit universally avoids data refresh. Owned engine/data/host adapter role labels are declarations and review scope; they are not standalone proof of policy equivalence.

External validated data bundles are inputs. Their JSON descriptors may point to external baked data; executable C#/Python/DLL source dependencies must come from the repository/package or the fresh build output. No physical map derivatives are created. Current Campaign geometry is reused.

## One command

From the repository root after placing this package's `Tools/CI` files in it:

```sh
python3 Tools/CI/run_skirmish_source_simulation.py run \
  --mission S002 --seed 104731 \
  --data-bundle /absolute/path/bundle.json \
  --output /absolute/new/output
```

The command resolves current sources, rebuilds the CLR placement/data facade, compiles the actual entry, then runs one retained source world. Build and run outputs are under `output/build` and `output/run`. It uses the installed version from `ProjectSettings/ProjectVersion.txt`, current package/reference DLLs, and installed Roslyn; these are required and pinned, not built via Unity.

For an explicitly bounded diagnostic, add `--max-ticks 5`. A completed prefix is **not** mission validation: its receipt sets `diagnostic_prefix_completed=true`, `validation_passed=false`, and `full_outcome_observed=false`. The normal default run must observe a source terminal outcome; reaching the authored bound, an ARIA stop, a timeout, drift, or expected-Victory loss is non-green. `SourceOutcomeObserved` describes observation separately from the expected Victory check. Native UI/render/input, result-save and return acceptance are not inferred.

`--repo` binds the compiled source authority. An explicit `--data-repo` may name a separate repository asset root for current scene/profile/config data; both roots and their pins are recorded, and no C# is compiled from that data root. This supports an exact published-source snapshot with an independently admitted data bundle.

A build may be reused with `--build-receipt /absolute/build/build.json`; the runner uses that build's recorded Mono/engine/reference selection and rejects changed pins. `build` and `resolve` subcommands are also available. All outputs must be new isolated directories. Timeouts terminate only the owned CLI worker.

## Receipts and clocks

Source/helper/manifest/template hashes are read from exact bytes, checked for conflicting epochs, and rechecked before emission/build and around execution. Runtime receipts list actual loaded assemblies, locations, MVIDs and hashes before the world and after outcome/failure. Common before/after modules must remain unchanged; ordinary late-loaded modules have recorded post-load hashes, not a pre-load equality claim. Current repository Match scene/view-profile data is pinned before the world, bound to the consumed paths, and rechecked afterward; this is current-config admission rather than historical asset equality. External input hashes remain distinct from executable sources. Compilation, bounded prefixes and actual source outcomes are separate evidence.

Game-rule clocks retain the driver's existing generated simulation/fixed-delta inputs. Native path-budget feedback still consumes measured host stopwatch durations. Those timings can affect scheduling; config plus seed alone does not establish deterministic replay. No zero/virtual replacement of performance timing or native-wall parity is claimed.

The logical ARIA contact/receipt controller is an explicit data-mode transport over the current shared S002 strategic policy and actual public fog/objective/group/paid-production facts. It does not invent rendered/pixel gates, treat inspection as an order, fabricate command acceptance, or publish a synthetic outcome. Actual source command results and paid queue/delivery state remain authoritative.

## Current candidate status

Do not treat this README or `resolve` as qualification. The staged package is runnable only after its manifest has zero unresolved entries, its actual entry compiles, and its run receipt records the appropriate result. Historical Victory evidence remains a separate pinned reference. The recorded source linearization satisfies admitted source edges; it is not a claim that native Unity observed the same total order.


## Passive failure diagnostics

After a run, summarize the existing traces without launching the simulator:

```sh
python3 Tools/CI/skirmish_trace_summary.py \
  --aria /absolute/run/aria-trace.jsonl \
  --validator /absolute/run/validator-state.jsonl \
  --result /absolute/run/result.json \
  --receipt /absolute/run/run-receipt.json \
  --out /absolute/new/trace-summary.json
```

The compact report lists actual executed command acceptance/reasons and recent commands, repeated consecutive actions, stationary actors with a different pending path/request goal, and input hashes/errors. Repeat thresholds default to three executed actions within five seconds; `--repeat` and `--repeat-window` can change those diagnostic thresholds. Non-executed waits do not count as commands. Unknown receipt acceptance stays unknown.

`analyzer_success` means the input analysis completed, separately from `source_result` and the mission outcome. Warnings are observations to investigate; they do not prove a gameplay bug or full conservation, and they do not establish native acceptance. The analyzer never changes gameplay clocks or commands and refuses to overwrite existing output/evidence. Missing/invalid traces produce explicit analysis errors. `--receipt` is optional for older source attempts, but a missing validator trace cannot establish stationary-actor analysis.


### Reservation upper-bound gate

`checks.reservation_owner_upper_bounds` is an upper-bound-only diagnostic: it does not prove accepted-unit counts or full conservation. It can be `Passed` only when the optional run receipt contains exactly one normalized path-suffix pin for `SkirmishProductionContracts.cs` with the expected source hash. A missing, changed, or conflicting pin makes this check `Unsupported`, not a gameplay failure. Process exit `0` is completed analysis without a proven check failure, `1` is a proven reservation upper-bound failure, and `2` is malformed or missing input.

## Faster local builds and navigation initialization

Build input pins are captured as a batch. Each input is still hashed from its bytes; conflicting epochs and changes during capture are rejected. Complete input verification remains before compilation, after compilation and around execution. A local 525-source build comparison measured 50.9 seconds before batching and 5.0 seconds afterward; these are observed build times, not a full-mission speed claim.

The optional [authored navigation facts cache](NAVIGATION_FACTS.md) avoids repeatedly decoding the large baked navigation JSON. Its preparation command emits a separate bundle, which can be passed to the ordinary runner. Only the eight admitted authored component types are cached; world entities, commands, resources, production and gameplay state are freshly created. Source, type-schema and raw-input changes invalidate the cache. The raw input path remains supported. Cached startup and bounded prefixes do not establish a mission outcome.

Owned Python helper byte fidelity and the qualified protocol test entry are documented in [OWNED_PYTHON_SOURCES.md](OWNED_PYTHON_SOURCES.md). Existing passive diagnostics and native acceptance scopes above are unchanged.


## Focused native prefab reference data

A newly consumed prefab may need its imported model bounds captured once. The existing reference exporter accepts an explicit actual catalog, seed and comma-separated configured building keys; omitting these arguments retains the S002/104731 eight-building selection. Geometry is optional and records the original runtime definition, original `BuildingModelBounds` result, renderer mesh bounds and root-relative transforms. It does not initialize a mission or build a player.

On macOS use the required GUI-licensing wrapper with Unity Hub open:

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 600 --log /absolute/new/export.log -- \
  -quit -executeMethod Game.Editor.SkirmishBakedActorSnapshotExporter.RunBuildingReferenceData \
  -skirmishBuildingReferenceData /absolute/new/export \
  -skirmishBuildingReferenceCatalog S005 -skirmishBuildingReferenceSeed 104734 \
  -skirmishBuildingReferenceKeys Building_Helipad -skirmishBuildingReferenceGeometry
```

Require exit zero and `[SkirmishAuthoredBuildingDefinitions] result=Passed` with the expected count and zero unsupported values. Recheck the emitted source pins. Configured footprint and evaluated/final footprint are separate fields. Metadata is captured in the selected native catalog's recipe context: rebuild production recipes for the actual target mission; do not copy S005 recipes into S026. Mission placement, runtime outcome and native acceptance remain separate checks.

## Inspect a new mission data draft

`author` uses the current native `Configure`, `TryCompile` and public
`TryCaptureMissionLogicalAuthoringInputs` APIs before allocating any simulation world:

```sh
python3 Tools/CI/run_skirmish_source_simulation.py author \
  --repo /path/to/source-repo --data-repo /path/to/asset-repo \
  --output /path/to/fresh-output --mission S026 --seed 104755 \
  --authoring-input /path/to/s026-authoring-input.json
```

The initial supported authoring input is an **unmeasured S026/104755 draft**.
`DraftInspected` with exit 0 means the native compiler rejected the draft and its
original reasons were recorded. It explicitly means `validation_passed=false`,
`source_fit_verified=false` and `world_allocated=false`; it does not accept the
mission, modify the catalog, publish an asset or enable autonomous S026 execution.
The existing S002/104731 resolve/build/run admission is unchanged.

Input schema 1 has exact fields `schema_version`, `kind`
(`SkirmishMissionAuthoringDraft`), `catalog_id`, `seed`, `size`, `difficulty`,
`graph`, `profiles`, `campaign_source_path`, `layout`, `setup_matrix`, and optional
`binding`. Each file descriptor contains exactly `path` and `sha256`. The graph is
the existing pinned source-config graph. `profiles` maps exactly `objective`,
`army`, `start`, `economy`, `upgrades`, `intel`, `roles` to real asset paths in that
graph. `layout` is the finite native config field object with explicit
`bindingValueFormat=2`; measured deployments and bind-all remain false. Unknown
fields, duplicate fields, trailing JSON, changed pins and unsupported profiles or
formats reject. Positive source-fit input is explicitly unsupported until complete
hash-bound fixed-budget placement, service/egress, route and packing facts exist.

The receipt distinguishes the S002 finite **tool compilation profile** from the
actual S026 authoring identity; compiling that closure does not initialize an S002
world. Source code comes only from `--repo`; assets come from `--data-repo`.
The original compiler reasons, current input hashes and actual loaded-runtime
manifests are retained. An optional `--build-receipt` reuses a checked build from
the exact same source root. This command does not bypass baked-data metadata
cache guards or provide native UI, renderer, device or player readiness evidence.

## Capture the existing map metadata reference

The approved DenseCity source has a focused one-time native reference probe. It
calls the original complete map metadata factory and records raw float bits for
its eight anchors and one camera, including exact Euler/quaternion pairs. It
captures source-asset data only; no mission, World or native root state is sampled.

```sh
WARLINE_OPMAP_METADATA_REFERENCE_OUTPUT=/private/tmp/fresh-map-reference/metadata-reference.json \
  Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/fresh-map-reference.log -- \
  -quit -executeMethod Game.Tests.Editor.OperationMapMetadataReferenceProbe.Run
```

Keep Hub open and require wrapper exit zero, the exact
`[OperationMapMetadataReference] result=Passed rotations=9 anchors=8 cameras=1`
marker, and the fresh output. Reverify recorded asset/source, assembly, method IL
and Editor identity pins. The probe rejects a changed coded source asset; it is
not a general quaternion implementation or mission-readiness certificate. Reuse
these reference values only within their source/producer read domain.
