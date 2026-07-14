# opmap-002 Phase 0 Baseline Probe

Date: 2026-07-14
Task: `opmap-002`
Branch: `codex/opmap-002-phase0-baseline-probe`
Baseline: `6340921d0a46db6082a029ef431643d9ea63dc79`
Workflow: Pull request; implementation agent does not merge or claim tracker acceptance.

## Files

- `Assets/Game/Scripts/Editor/OperationMapPhase0BaselineProbe.cs`
- `Assets/Game/Scripts/Editor/OperationMapPhase0BaselineProbe.cs.meta`
- `Assets/Tests/Editor/OperationMapPhase0BaselineProbeTests.cs`
- `Assets/Tests/Editor/OperationMapPhase0BaselineProbeTests.cs.meta`
- `Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md`

## Behavior

`Game.Editor.OperationMapPhase0BaselineProbe.Run` is a static editor-only probe. It rejects dirty loaded scenes, captures the Editor scene setup, opens `Match.unity` and `MatchSubScene.unity` additively for inspection, invokes no baker or scene-wiring path, invokes no save API, and restores the captured setup in `finally`.

The probe derives and cross-validates the manifest schema, canonical source hash, presentation content hash, chunk/source ranges, integrity ledger, generated scene and metadata file sets, and every per-file SHA-256. It reports deterministic aggregate hashes, build settings, scene hierarchy summaries, all serialized `MatchSceneView` object references, placement identities and source paths, and grid/map-surface identity and dimensional consistency. Audited tracker counts are not acceptance constants.

On success it writes one UTF-8 JSON report to `/private/tmp/warline-operation-map-phase0-baseline.json`. `WARLINE_OPERATION_MAP_PHASE0_BASELINE_REPORT_PATH` is the only override. Relative paths, non-JSON paths, missing output directories, and paths under project `Assets`, `Packages`, or `ProjectSettings` are rejected. Validation failure throws before report writing.

## Validation

Validation workspace: `/Users/farhad/Projects/WarlineCapture-Worktrees/opmap-002-phase0-baseline-probe`
Unity: `6000.5.2f1`

Full probe and compiler validation:

```text
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-Worktrees/opmap-002-phase0-baseline-probe \
  -executeMethod Game.Editor.OperationMapPhase0BaselineProbe.Run \
  -logFile /private/tmp/opmap-002-phase0-probe-determinism.log
```

Result: exit `0`; `[OperationMapPhase0BaselineProbe] result=Passed chunks=514 sources=16542`; no C# compiler errors. JSON: `/private/tmp/warline-operation-map-phase0-baseline.json`. JSON SHA-256 on two consecutive default-path runs: `0344c181a9f40cc3b5529846a6454db8465c49669fd4be85c905040370290bca` both times.

Focused EditMode tests:

```text
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/farhad/Projects/WarlineCapture-Worktrees/opmap-002-phase0-baseline-probe \
  -runTests -testPlatform EditMode \
  -testFilter OperationMapPhase0BaselineProbeTests \
  -testResults /private/tmp/opmap-002-phase0-tests.xml \
  -logFile /private/tmp/opmap-002-phase0-tests.log
```

Result: exit `0`; `5 total, 5 passed, 0 failed, 0 skipped`. Coverage includes SHA-256, aggregate ordering/duplicate rejection, output safety, report shape, full smoke invocation, scene-setup restoration, and before/after hashes for `Match.unity`, `MatchSubScene.unity`, the manifest, and the integrity ledger.

Derived report evidence: manifest schema `1`; canonical dependency hash `0a587783351110d16353575d15d1b5cd` recomputed equal; content hash `9eebc7c8aa774d5f505cb684099d133a` recomputed equal; chunk size `32`; manifest/ledger/disk-scene/disk-meta counts all `514`; source count `16,542`; exact file-set parity passed; combined scene/meta aggregate SHA-256 `574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79`. Placement counts are `451` building and `29` vehicle. The grid and map surface both derive as `2048 x 1024`, origin `(0,0,0)`, cell size `1`, and `2,097,152` cells/surfaces.

Scoped git validation: `git diff --cached --check`; `git diff --cached --name-only`; `git status --short --branch`. Result: passed after removing `.meta` trailing whitespace; exactly the five allowlisted files are staged, with no out-of-allowlist tracked or untracked change. No tracker acceptance is claimed.

## Known Limits

- This task captures repository and Unity Editor baseline structure. It does not supply device, APK, installed-size, memory, FPS, draw-call, or launch-time evidence required by the wider Phase 0 tracker.
- The first isolated Unity import changed `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`. That transient import churn was restored exactly to `HEAD` and is not task behavior or part of the PR.
- Failed validation iterations also left untracked `mono_crash.mem.*.blob` diagnostics at the worktree root. They were removed and are not part of the PR.
- Existing placement source-path ambiguity is reported as `configSourcePathOccurrenceCount` and `sceneMatchCount`; missing scene paths and exact duplicate placement identities still fail the probe.
- The report is a baseline artifact, not tracker acceptance and not authorization for scene extraction.
- No scene, config, manifest, integrity ledger, generated chunk, project setting, Addressables asset, package manifest, or runtime source is modified.
