# opmap-007 Phase 0 Camera And Minimap Ownership

Date: 2026-07-15
Task: `opmap-007`
Branch: `codex/opmap-007-phase0-camera-minimap-ownership`
Validated source revision (`origin/main`): `d9e2f1ba0e9f7df2d35abe60488fb1d44d5c91bf`
Workflow: pull request; implementation context does not merge or claim tracker acceptance.

## Result

Status: `NeedsDecision`

The read-only editor probe records `23` deterministic camera, camera-boundary, minimap-projection, initial-focus, objective, and assistant-focus ownership rows. The classification totals are `10` `ShellOwned`, `4` `MapOwned`, `2` `TemporaryCompatibility`, `5` `Mixed`, and `2` `Unresolved`. No row is classified as `SharedConfig`.

Seven rows deliberately remain decisions:

- initial camera focus production mixes scenario-derived placement with map-owned grid bounds;
- tactical-follow application bypasses the normal grid clamp while its pose is valid;
- grid bounds are projected into mutable shell-camera boundary state;
- the selection startup config can override the shell camera, although its current reference is null;
- expanded full-map minimap projection can include an out-of-bounds camera footprint;
- the objective read model writer remains unresolved because no writer was found in audited runtime sources;
- the objective camera-focus recommendation writer remains unresolved because no writer was found in audited runtime sources.

Every `Mixed` or `Unresolved` row uses `DecisionRequired` and names its decision owner. The report does not convert missing production behavior into a passing ownership claim.

## Presence Findings

- `initial-focus-producer`: `Present`; `InitialUnitsSpawnSystem::ProcessInitialBuildingCompletion(Unity.Entities.EntityManager,Unity.Entities.Entity,Unity.Entities.Entity,Game.Components.GridConfig,int,ref Game.Runtime.InitialUnitsSpawnSystem.InitialSpawnDiagnosticLogWriter)` writes the legacy one-shot focus state.
- `runtime-objective-writer`: `Unresolved`; no writer found in audited sources. Decision owner: mission runtime owner and assistant architecture owner.
- `objective-camera-focus-recommendation-producer`: `Unresolved`; no writer found in audited sources. Decision owner: mission UX owner and assistant architecture owner.

The candidate audit scans every runtime C# source under `Assets/Game/Scripts` before and after evidence capture. A new `MatchObjectiveRuntimeElement` or `AssistantRecommendationKind.CameraFocus` source reference fails closed; focused tests inject and reject both candidate classes.

## Determinism And Safety

- Output schema: `warline.operation-map.phase0-camera-minimap-ownership`, version `2`.
- Committed JSON SHA-256: `f9b2ef9b54d66a1ac1a4dfdc925bbf5bffe1eec6182bda7d69dff048219d9c66`.
- Two real Unity runs and the committed JSON are byte-identical at that hash.
- The probe pins exact SHA-256 values and required semantic tokens for all direct inputs, including accepted `opmap-002` and `opmap-004` evidence.
- The branch was rebased onto `origin/main` at the exact JSON source revision; every pinned source hash and required semantic token was revalidated there.
- Inputs are hashed before and after report construction. Any missing, changed, unordered, unsupported, volatile, local-path, or stale evidence fails closed.
- JSON validation rejects unknown fields at every object level before typed validation.
- Output parents are canonically resolved before project-containment checks, including symlinked parents. Publication carries that validated destination forward and performs every delete, temporary write, move, and read through the canonical parent and destination. It revalidates the requested and canonical parent identities immediately before replacement and fails closed if either changed; a deterministic Unix negative retargets the parent symlink at that exact point and proves neither target receives output.

## Validation

Unity: `6000.5.2f1`. All logs and generated test evidence are under `/private/tmp`.

Authoritative probe, run twice with independent outputs:

```text
WARLINE_OPERATION_MAP_PHASE0_CAMERA_MINIMAP_OWNERSHIP_REPORT_PATH=/private/tmp/opmap007-final-latest-runN.json \
Tools/CI/invoke_unity_macos.sh --project <worktree> --timeout 1800 \
  --log /private/tmp/opmap007-final-latest-runN.log -- \
  -nographics -quit \
  -executeMethod Game.Editor.OperationMapPhase0CameraMinimapOwnershipProbe.Run
```

Both exited `0` with `result=NeedsDecision rows=23 needsDecision=7 runtimeObjectiveWriter=Unresolved` and zero compiler errors. `cmp -s` passed for both run outputs and the committed JSON.

Focused negative and report-shape tests:

```text
Tools/CI/invoke_unity_macos.sh --project <worktree> --timeout 1800 \
  --log /private/tmp/opmap007-final-latest-focused.log -- \
  -nographics -runTests -testPlatform EditMode \
  -testFilter OperationMapPhase0CameraMinimapOwnershipProbeTests \
  -testResults /private/tmp/opmap007-final-latest-focused-results.xml
```

Result: `32 / 32` passed, zero failures, skips, or inconclusive tests. Coverage includes exact committed shape and hashes, candidate discovery, compiled declaration/signature validation, fully-qualified overload identities, strict unknown-field rejection, symlink containment and deterministic immediate pre-replace retargeting, valid/invalid publication races, concurrent atomic publication, missing sections, stale evidence, ordering drift, decision owners, external-only output, and unchanged inputs.

Architecture and naming gates:

- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed `9 / 9`; `/private/tmp/opmap007-final-latest-gate-non-ecs.log`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: passed `31 / 31`; `/private/tmp/opmap007-final-latest-gate-assembly.log`.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed `1 / 1`; `/private/tmp/opmap007-final-latest-gate-broad-shell.log`.

Final scoped git validation requires `git diff --check`, complete `origin/main...HEAD` inspection, exact six-file allowlist equality, and a clean worktree after commit.

## Files

- `Assets/Game/Scripts/Editor/OperationMapPhase0CameraMinimapOwnershipProbe.cs`
- `Assets/Game/Scripts/Editor/OperationMapPhase0CameraMinimapOwnershipProbe.cs.meta`
- `Assets/Tests/Editor/OperationMapPhase0CameraMinimapOwnershipProbeTests.cs`
- `Assets/Tests/Editor/OperationMapPhase0CameraMinimapOwnershipProbeTests.cs.meta`
- `Design/AgentReports/2026-07-15_opmap-007_phase0_camera_minimap_ownership.json`
- `Design/AgentReports/2026-07-15_opmap-007_phase0_camera_minimap_ownership.md`

## Known Limits

- This evidence does not decide the seven mixed or unresolved ownership rows and does not close a Phase 0 tracker checkbox.
- It inventories current ownership and migration disposition; it does not implement operation-map metadata, scene extraction, objective runtime production, camera behavior changes, or minimap policy changes.
- No scene, prefab, config, package, project setting, runtime source, accepted baseline report, or shared tracker is modified by this PR.
