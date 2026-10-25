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
