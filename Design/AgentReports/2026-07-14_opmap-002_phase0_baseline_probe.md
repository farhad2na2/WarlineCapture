# opmap-002 Phase 0 Baseline Probe

Date: 2026-07-14
Task: `opmap-002`
Branch: `codex/opmap-002-phase0-baseline-probe`
Baseline: `6340921d0a46db6082a029ef431643d9ea63dc79`
Prior reviewed head: `c48590267d472be088988cc823e6b29e7a775d11`
Workflow: Pull request; implementation agent does not merge or claim tracker acceptance.

## Files

- `Assets/Game/Scripts/Editor/OperationMapPhase0BaselineProbe.cs`
- `Assets/Game/Scripts/Editor/OperationMapPhase0BaselineProbe.cs.meta`
- `Assets/Tests/Editor/OperationMapPhase0BaselineProbeTests.cs`
- `Assets/Tests/Editor/OperationMapPhase0BaselineProbeTests.cs.meta`
- `Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md`

## Behavior

`Game.Editor.OperationMapPhase0BaselineProbe.Run` is a static editor-only probe. It rejects dirty loaded scenes, captures the Editor scene setup, opens `Match.unity` and `MatchSubScene.unity` additively for inspection, invokes no baker or scene-wiring path, invokes no save API, and restores the captured setup in `finally`.

The probe derives and cross-validates the manifest schema, canonical source hash, presentation content hash, chunk/source ranges, integrity ledger through `StaticMapPresentationSceneIntegrity.TryLoadAndValidate`, generated scene and metadata file sets, and every per-file SHA-256. Its success schema requires the canonical main scene and subscene, complete object identities, nonempty required collections, count/set parity, ordered unique generated files, recomputed generated-file aggregates, ordered unique placement identities, and consistent grid/map-surface counts. Placement ordering is a total ordinal order over source/category/key, faction, occurrence counts, all transform values, yaw/rotation flags, and complete prefab identity. Audited tracker counts are not acceptance constants.

Publication fails closed. Any prior destination is removed before probe validation; success JSON is schema-validated in memory, written to a same-directory temporary file, read back and schema-validated, then atomically moved/replaced at the destination. Every failure cleans the temporary file, so stale `Passed` output cannot survive. On success it writes one UTF-8 JSON report to `/private/tmp/warline-operation-map-phase0-baseline.json`. `WARLINE_OPERATION_MAP_PHASE0_BASELINE_REPORT_PATH` is the only override. Relative paths, non-JSON paths, missing output directories, and every location at or below the canonical project root are rejected, including platform-appropriate case aliases.

## Validation

Validation workspace: `/Users/farhad/Projects/WarlineCapture-Worktrees/opmap-002-phase0-baseline-probe`
Unity: `6000.5.2f1`

Two full probe and compiler-validation runs used the same report path:

```text
WARLINE_OPERATION_MAP_PHASE0_BASELINE_REPORT_PATH=/private/tmp/opmap-002-phase0-revision-determinism.json \
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-Worktrees/opmap-002-phase0-baseline-probe \
  -executeMethod Game.Editor.OperationMapPhase0BaselineProbe.Run \
  -logFile /private/tmp/opmap-002-phase0-revision-probe-run1.log
```

The second run used `/private/tmp/opmap-002-phase0-revision-probe-run2.log`. Both exited `0` with `[OperationMapPhase0BaselineProbe] result=Passed chunks=514 sources=16542`; no C# compiler errors. JSON: `/private/tmp/opmap-002-phase0-revision-determinism.json`; preserved first output: `/private/tmp/opmap-002-phase0-revision-determinism-run1.json`. `cmp -s` passed and both SHA-256 values were `991096d3bdab9119c23fe6cc8d932fb3e9fb497b96fdebdc8d7badd0eb73025c`.

Focused EditMode tests:

```text
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/farhad/Projects/WarlineCapture-Worktrees/opmap-002-phase0-baseline-probe \
  -runTests -testPlatform EditMode \
  -testFilter OperationMapPhase0BaselineProbeTests \
  -testResults /private/tmp/opmap-002-phase0-revision-tests-final.xml \
  -logFile /private/tmp/opmap-002-phase0-revision-tests-final.log
```

Result: exit `0`; `8 total, 8 passed, 0 failed, 0 skipped`. Coverage includes SHA-256, aggregate ordering/duplicate rejection, whole-project/case-alias output safety, stale-success invalidation on forced validation and write failures, mandatory report identities/sections/counts/sets, unsupported report and integrity schemas, old-key placement collisions across faction/prefab/transform identities, full smoke invocation, scene-setup restoration, and before/after hashes for `Match.unity`, `MatchSubScene.unity`, the manifest, and the integrity ledger.

Architecture and naming guards:

```text
-testFilter "ProductionSourceGrowthArchitectureTests;NonEcsSystemConversionArchitectureTests"
-testResults /private/tmp/opmap-002-phase0-revision-architecture-tests.xml
-logFile /private/tmp/opmap-002-phase0-revision-architecture-tests.log
```

Result: non-ECS architecture/naming passed `9 / 9`. Production source growth passed `8 / 15` and failed `7 / 15` on pre-existing runtime source/baseline authorization debt outside this PR; the editor-only probe is excluded from that production inventory and no reported violation names either PR source file. The combined guard is recorded as failed, not as acceptance evidence.

Derived report evidence: manifest schema `1`; canonical dependency hash `0a587783351110d16353575d15d1b5cd` recomputed equal; content hash `9eebc7c8aa774d5f505cb684099d133a` recomputed equal; chunk size `32`; manifest/ledger/disk-scene/disk-meta counts all `514`; source count `16,542`; exact file-set parity passed; combined scene/meta aggregate SHA-256 `574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79`. Placement counts are `451` building and `29` vehicle. The grid and map surface both derive as `2048 x 1024`, origin `(0,0,0)`, cell size `1`, and `2,097,152` cells/surfaces.

Final scoped git validation includes `git diff --check`, complete `origin/main...HEAD`/working-tree allowlist inspection, `git status --short --branch`, remote-head verification, and an untracked-file check. No tracker acceptance is claimed.

## Known Limits

- This task captures repository and Unity Editor baseline structure. It does not supply device, APK, installed-size, memory, FPS, draw-call, or launch-time evidence required by the wider Phase 0 tracker.
- The first isolated Unity import changed `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`. That transient import churn was restored exactly to `HEAD` and is not task behavior or part of the PR.
- Failed validation iterations also left untracked `mono_crash.mem.*.blob` diagnostics at the worktree root. They were removed and are not part of the PR.
- The production source-growth guard has seven unrelated failures on existing runtime sources/baseline authorizations. This PR cannot repair them because those files and the shared baseline/tracker are outside its allowlist.
- Existing placement source-path ambiguity is reported as `configSourcePathOccurrenceCount` and `sceneMatchCount`; missing scene paths and exact duplicate placement identities still fail the probe.
- The report is a baseline artifact, not tracker acceptance and not authorization for scene extraction.
- No scene, config, manifest, integrity ledger, generated chunk, project setting, Addressables asset, package manifest, or runtime source is modified.
