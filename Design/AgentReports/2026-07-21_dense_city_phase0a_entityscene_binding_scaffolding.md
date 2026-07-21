# Phase 0A EntityScene Binding / Load / Streamer Skip Scaffolding

Date: 2026-07-21  
Lane: Grok continuation  
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Status

`EntitySceneBindingScaffoldingReady` — production ownership unchanged (`StaticSceneChunks`).

## What landed

1. `OperationMapEntityScenePresentationPolicy` — fail-closed EntityScene detection, static-manifest/streamer skip, and binding validation (SubScene required, renderer-free map root, empty placements allowed).
2. `OperationMapSceneView.TryValidate` — empty legacy placements accepted only when presentation kind + canonical mode are both `EntityScene`; mismatched kind/mode fails closed.
3. `OperationMapSceneLoadingSceneSystemHelper` — EntityScene loads source scene only; uses `EntitySceneSkippedPresentationManifestOperation`; rejects a bound static-manifest GUID; ready path sets `Manifest = null`.
4. `StaticMapPresentationOwnership.Initialize` — already clean-skips for EntityScene (`EntitySceneSkipStaticOwnership`).
5. `MenuBootstrapCompositionSystemHelper` — skips static streamer bind for EntityScene matches and treats presentation preload as ready.

## Tests

Non-batchmode Unity EditMode:

`/private/tmp/dense-city-entityscene-scaffold-tests-gui4.xml` — **19/19 passed**

Filter:
`OperationMapEntityScenePresentationPolicyTests|OperationMapSceneLoadingSceneSystemHelperTests|StaticMapPresentationStreamerTests.EntitySceneMatch_SkipsStaticStreamerBind`

## Hard stops still in force

- Do not flip production definition / canonical mode to `EntityScene`
- Do not mutate accepted source/static package/Addressables production ownership
- Fixed-camera visual parity still required before production cutover

## Next

1. Candidate Addressables layout (runtime binding + entity-scene ownership, zero current-map static chunk entries)
2. Capture fixed-camera visual parity evidence against the candidate
3. Bake All fail-closed EntityScene stages
4. Editor + Android acceptance before any production `EntityScene` flip
