# Current-source Skirmish simulation

This CLI hosts the current shared/native S002 gameplay policies over explicit finite engine/data adapters. It does not launch Unity or build a player. The supported execution profiles are S002, seeds 104731 and 1, Standard/Regular, fresh config-initialized ground BaseAssault, with the qualified installed macOS Mono numeric lane (`--optimize=-float32`). Other mission/selection/seed profiles and consumed unsupported domains fail explicitly.

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
S002 resolve/build/run supports only the finite selections in admitted_profiles.json; S026 author diagnostics do not grant runtime admission.

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

## Fast smoke before a full simulation

Use the same entry point for one ordered report:

```sh
rtk proxy python3 -B Tools/CI/run_skirmish_source_simulation.py smoke \
  --repo <source-repository> --data-repo <asset-repository> \
  --mission S002 --seed 104731 --data-bundle <pinned-bundle.json> \
  --build-receipt <checked-source-build/build.json> \
  --objective-build <checked-objective-build> --output <fresh-smoke-output> --timeout 120
```

Both build arguments are optional. Missing producers build once; supplied producers
must still match their recorded sources, binaries and current selected source.
The existing .NET8 objective producer runs its original BA/FC/BT/CE rules against
1,800 synthetic probes generated from the actual 120-entry catalog/360-row matrix.
Those facts do not represent 1,800 battles. The source producer separately runs
original authored-definition, matrix, all-size static-budget and selected compiler
diagnostics without a World. Standalone S002 smoke ends with one ordinary five-tick
startup. Known startup failures propagate. No Unity or player build launches.

`smoke-report.json` lists each stage, field, reason, evidence, duration and a repro
command. Cheap independent checks are collected; failed dependencies skip later
work. The report also lists unimplemented physical/infrastructure coverage. It
does not load historical temporary placement/route/runtime-owner success flags.
Add a small current-source stage to the finite `STAGES` list when those adapters
are available. Exit 0 means `SmokePassed` for the declared required checks only;
exit 1 is `Failed`, and exit 2 is `Blocked` or unsupported. `mission_ready`,
`validation_passed`, `source_fit_verified`, `native_acceptance` and full-outcome
claims remain false. A passed smoke is never mission acceptance.

For S026/104755, use `smoke --authoring-input <actual-draft.json>` instead of a
runtime bundle. The existing native author command preserves original compiler
reasons; current missing world binding/source-fit returns a blocked smoke, without
allocating a mission World. Unsupported runtime families remain explicit.

Ordinary full `run` now performs the same config/objective preflight automatically,
reusing its checked source build. It defers startup to the actual full worker to
avoid a second world initialization. With an existing build receipt, its report
is `<run-output>-smoke/smoke-report.json`. Without one, cheap input/CSV checks run
first, the one source build is created by preflight, and the report is
`<output>/smoke/smoke-report.json` with the full worker under `<output>/run`.
A failing preflight does not build dependent producers or launch the worker. Explicit `run --max-ticks` retains its diagnostic-prefix semantics and does
not recurse through smoke. Smoke owns only its launched process group on timeout.

Qualification scope: the integrated source build/native-config/startup lane is
currently qualified on macOS with the recorded installed Mono numeric profile.
Catalog/matrix CSV checks are Python data checks; the existing objective producer
uses installed .NET8 and has portable source-only semantics. This does not grant
Windows source-world numerical parity or a fallback runtime. No runtime is
silently substituted when the qualified source-world lane is unavailable.

The checked-build qualification command completed S002 smoke in 10.77 seconds
(including 8.99 seconds ordinary startup and 0.41 seconds for 1,800 synthetic rule
probes). These are functional wall observations, not a speed benchmark or a
full-mission claim. Genuine S026 returned Blocked on its original compiler gate.


## Shared compiled ground mission context

The existing S002 controller/approach now consume the same native Runtime
`GroundMissionContextFactory` compiled by the source tool. The factory reruns the
original setup compiler and matches the real session and a unique canonical
BattleCatalog row. Requested dispatch 4, selected original BaseAssault resource
index 0, playable transport index 4 and canonical map DB remain separate fields.
The resulting observation scope is LogicalPublicData; it cannot grant rendered
observation, publication, owner capability or new-mission admission.

Initialization must include `canonical_battle_catalog` as an explicit absolute
`path`/`sha256` descriptor. Prepare that actual asset through the existing
source-config graph reader with the catalog as an additional input. Its typed
catalog node, raw asset hash and graph source pin must match the descriptor;
there is no guessed resource fallback. Existing graph node values and source
pins remain required. This is current configuration input, not a new data bank,
mission binding or readiness flag. S002-only entry guards and the original
native AriaS002 wrapper remain in force. S026 needs its genuine compiler and
source-fit gates before a separate admission change.


## Selected metadata root and complete production publication

The S002 source profile now requires three explicit initialization descriptors. Each is an exact `path` / `sha256` pair:

- `selected_operation_map`: the actual map selected by the native preset/fallback/catalog chain. Its unique typed node must be in the same `source_config_graph` as the mission and canonical battle catalog.
- `operation_map_rotation_reference`: the qualified native Quaternion.Euler reference. The provider rejects unknown raw Euler keys or incompatible source methods. It does not assume identity rotations.
- `building_request_metadata`: a qualified `CurrentSourceBuildingRequestMetadataSupplement`, including native configured-provider indices and requestability. Construction and scene lists remain distinct; missing values do not receive defaults.

Keep the existing `canonical_battle_catalog`, baked map, road, unit-production and other descriptors. The graph remains bounded at 64 nodes; this qualified S002 closure has 36.

Use the data-only preparation entry below. The older unused data wrapper in `generate_current.py` is unsupported; `generate.py` remains the source resolver entry and `prepare_inputs.py` is the supported data entry.

```sh
python3 Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/prepare_inputs.py \
  --repo /absolute/current/source-repo --data-repo /absolute/current/data-repo \
  --output /absolute/new/prepared-directory \
  --graph /absolute/existing/source-config-graph.json --graph-sha GRAPH_SHA256 \
  --initialization /absolute/existing/initialization.json --initialization-sha INITIALIZATION_SHA256 \
  --bundle /absolute/existing/data-bundle.json --bundle-sha BUNDLE_SHA256 \
  --rotation-reference /absolute/qualified/metadata-reference.json --rotation-reference-sha ROTATION_SHA256 \
  --building-request-metadata /absolute/qualified/supplement.json --building-request-metadata-sha SUPPLEMENT_SHA256
```

The output directory must be new. The command uses the existing safe source reader, verifies the actual serialized selection chain, preserves existing input descriptors and compares both generated C# files with the qualified source-only generator. It writes `source-config-graph.json`, `initialization.json`, `data-bundle.json` and `preparation-proof.json`. It starts no World and grants no mission admission. Supply its data bundle to the ordinary `run` or `smoke` command documented above.

First use still requires qualified baked/native inputs. The rotation reference must come from the existing native OperationMap metadata reference exporter through the repository's checked Unity wrapper; the CLI does not manufacture native quaternion results. The qualified configured-building supplement combines the admitted R6 selected-prefab/definition reference with current original configured-provider selection over `Assets/Game/Configs/Skirmish/Construction.asset` and `Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset`. It retains source/selector/prefab pins, both original list indices and explicit/initialized native requestability. This preparation entry consumes that qualified supplement; it does not invent one from names or prices. The reviewed S002 supplement has SHA256 `413c28c09e82c639fad78ec60c00fbf3608cffb7e5ab1d5663ca0cc45253f553`.

Actual startup order is road synchronization, selected-source validation, original initial facility/bank/actor and owner registration, configured metadata binding without publication, selected metadata-root creation, then the original first publication and three bootstrap owner steps before ARIA initialization. The numeric facility roots remain sourced from the original PositionBuildingObject/prefab TRS path; the metadata root does not fabricate a physical scene frame. Startup evidence records root identity/generation, actual readiness flags, every publication buffer count and ordered spawn-point float bits.

The current-source regression used S002/104731 Standard/Regular with normal preflight, autonomous ARIA, original clocks and actual command results. It observed original Victory/MainBaseDestroyed at tick 12267. This is source-world coverage, not rendered normal-input/device acceptance, S026 readiness or a 10x speed claim. Metadata publication keeps PhysicalSourceValidated unset. Existing-root reuse, positive unbound runtime runway/overlay geometry, owned boarding/deploy/disembark and active scan remain explicit unsupported frontiers. Original no-runway branches execute normally. Return command mechanics are qualified separately; no Return command is injected into the regression.


## Finite seed1 metadata compatibility

`admitted_profiles.json` is the single strict schema for S002 Regular difficulty 2 / Standard size 1, seeds 104731 and 1. The owned Python loader admits `skirmish_source_profiles.py` before callers, binds the JSON bytes, and generates the finite `SourceAdmittedProfiles` C# guard. Unknown schema versions, keys, runtime labels, selections and seeds fail closed. Each build receipt and data bundle must match the requested mission/seed; normal startup checks initialization selection before allocating its World.

For seed1, prepare a new initialization from the same authored graph/static map descriptors, set selection.seed to 1, and include this exact immutable qualification descriptor:

```json
"metadata_compatibility": {
  "path": "<qualified-native-output>/data/two-seed-metadata.json",
  "sha256": "d9b2dbfc12a8cfd12e385a3b3fa9266fd96a3d05939bc244992fafc6e21e1e54"
}
```

The qualified native wrapper capture independently compiled seed 104731/setup 3657809016 and seed 1/setup 3589425631 and observed equal building/unit/configured-provider metadata. `SourceMetadataCompatibility` checks its exact capture hash, reached factory source and script-meta pins, six owner assemblies and metas, conservative native asset closure, provider ordering, recipes and actual target setup. The historical metadata keeps captured seed 104731 provenance. Only immutable authoring metadata crosses contexts; current original compiler/placement/owners create seed 1 positions, banks, entities and runtime state afresh. Omitting the compatibility descriptor retains the default strict same-context reader behavior; the descriptor cannot admit another seed, target setup or map.

Pin the new initialization into a data bundle with mission S002 and seed 1. Use a new seed 1 build receipt, then the ordinary command (no forced terminal):

```sh
python3 -B Tools/CI/run_skirmish_source_simulation.py run \
  --repo <repository> --package-root <package> --mission S002 --seed 1 \
  --build-receipt <seed1-build>/build.json --data-bundle <seed1-bundle.json> \
  --output <fresh-output> --timeout 600
```

Add `--max-ticks 5` only for an explicit diagnostic prefix. Ordinary full runs retain required preflight, autonomous ARIA, measured path budgets and original clocks. The qualified seed 1 source run observed original Victory/MainBaseDestroyed at tick 12572, setup 3589425631, with 4 deliveries. Source outcomes remain separate from native normal-input/UI/result-save/return and real player/device acceptance. Reusable profile regressions and the noWorld metadata reader fixture are documented in `Tests/README.md`.
