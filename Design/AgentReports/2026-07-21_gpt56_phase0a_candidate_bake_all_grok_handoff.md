# Phase 0A Candidate Bake All Handoff

Date: 2026-07-21  
Lane: GPT 5.6 implementation to Grok continuation  
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Task

Add the first candidate-only transactional Bake All gate for the existing-map EntityScene migration without changing production ownership.

## Files changed

- `Assets/Game/Scripts/Editor/OperationMapEntitySceneCandidateBakeAll.cs`
- `Assets/Game/Scripts/Editor/OperationMapEntitySceneCandidateBakeAll.cs.meta`
- `Assets/Tests/Editor/OperationMapEntitySceneCandidateBakeAllTests.cs`
- `Assets/Tests/Editor/OperationMapEntitySceneCandidateBakeAllTests.cs.meta`
- Existing editor menu declarations previously rooted at `Tools/Warline Capture` now use `Game/Maps` or `Game/Operation Maps`.
- This tracker and handoff report.

## Contracts touched

- Production definition remains `StaticSceneChunks`.
- Candidate definition remains `EntityScene`.
- Accepted operation-map scene, accepted SubScene, production definition/runtime binding, production Addressables groups, and frozen static rollback root are protected by pre/post fingerprints.
- Candidate SubScene, definition, and runtime binding are restored to their pre-run bytes after any stage failure.
- No production Addressables mutation or production cutover is allowed.

## User-visible behavior

- No runtime behavior changed.
- Candidate command: `Game > Operation Maps > EntityScene Migration > Bake All Candidate EntityScene`.
- The obsolete `Tools > Warline Capture` top-level menu is removed.

## Validation run

- `git diff --check`: passed.
- Unity compile/import: `/private/tmp/warline-phase0a-integration-compile.log`, passed with exit code 0 and no compiler errors.
- Focused coordinator tests: `/private/tmp/warline-phase0a-bake-all-focused-fix.log`, 8/8 passed.
- Complete candidate transaction: `/private/tmp/warline-phase0a-bake-all-run4.log`, passed.
- Settled no-op repeat: `/private/tmp/warline-phase0a-bake-all-run5.log`, passed.
- Candidate SubScene, definition, runtime binding, and runtime-binding `.meta` are byte-identical between settled runs.
- Accepted operation-map scene SHA-256 remains `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`.
- Accepted SubScene SHA-256 remains `eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d`.

## Validation result

`CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`

Unity licensing IPC was recovered with explicit project-owner approval. Two integration defects were fixed during execution:

- batch execution with no initially loaded scene now leaves one clean active empty scene instead of restoring an invalid zero-scene setup;
- rebuilding an existing candidate runtime-binding scene preserves its `.meta` GUID instead of deleting/recopying the asset and rewriting the candidate definition on every run.

The successful report contains 432 buildings, 22 vehicles, 9,090 render-only owners, 3 presentation roots, 14,212 render meshes, 1,841 shared dependencies, and `productionCutover=0`.

## Exact blocker

- Candidate Bake All: unblocked and complete.
- Production cutover: blocked by exact Editor-to-runtime transform parity, fixed-camera parity, Editor gameplay/lifecycle acceptance, and user-triggered Android acceptance.
- Owner lane: GPT continues transform-parity implementation; Grok may continue visual parity and review after the transform evidence exists.
- Another lane can continue: yes, but no lane may flip production ownership or retire static rollback content.

## Known gaps

- Exact source-to-candidate-to-baked-to-Addressables transform and transformed-bounds parity is not implemented yet.
- Fixed-camera visual parity remains open.
- Editor gameplay/lifecycle and Android acceptance remain open.
- Production cutover and static-package retirement remain prohibited.

## Cross-lane impacts

- Grok should use the new `Game/Operation Maps/EntityScene Migration` menu location.
- No lane should restore the removed `Tools/Warline Capture` navigation root.
- No lane should flip production to `EntityScene` before visual, Editor, and Android acceptance.

## Next recommended task

1. Add deterministic source/candidate/baked transform and transformed-renderer-bounds parity records.
2. Fail Bake All on hierarchy, pivot, non-uniform scale, `Parent`, `PostTransformMatrix`, duplicate, missing, or runtime-offset mismatches.
3. Capture fixed-camera parity only after numeric transform parity passes.
4. Run Editor gameplay/lifecycle acceptance.
5. Ask the project owner for the Android candidate validation; do not trigger Android builds from this lane.
