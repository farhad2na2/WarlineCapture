# Operation Map Phase 0 Ownership Evidence

Task: `opmap-004`

Baseline revision: `3b7228292db7159c3c70025cf5d1676573721cd4`

## Scope

`Game.Editor.OperationMapPhase0OwnershipProbe` performs a read-only ownership inspection of exactly:

- 29 serialized `MatchSceneView` Unity-object references, including the loader-neutral compatibility operation-map catalog added on 2026-07-16;
- 16 ordered roots in `Assets/Game/Scenes/Match.unity`;
- 3 ordered roots in `Assets/Game/Scenes/Match/MatchSubScene.unity`.

The probe does not invoke a baker, regenerate static presentation output, save a scene, or rescan the generated 514-scene presentation set. Presentation and placement totals are parsed as cross-referenced evidence from the accepted `opmap-002` report.

## Result

Status: `NeedsDecision`

The report contains 47 complete ownership rows and deliberately does not claim `Passed`. Four rows require an explicit architecture or design decision:

- the `dayNightConfig` reference;
- `Start` and `End` Match roots;
- `InitialUnitsSpawnerAuthoring` in `MatchSubScene`.

The `directionalLight` reference and both Match directional-light roots are `ShellOwned`, matching the normative shell-owned lighting boundary.

Every row records deterministic current-target identities and collection cardinality. Every `Mixed` or `Unresolved` row also identifies a decision owner, rationale, evidence paths, and migration disposition. The evidence therefore supports review without closing either Phase 0 ownership checkbox.

## Determinism And Guardrails

- Output schema: `warline.operation-map.phase0-ownership`, version `1`.
- Committed JSON SHA-256: `e1080bd90e88140d8151755b7ef6086c02d8683b7d277708004797893fc3c49b`.
- Two real Unity probe runs produced byte-identical JSON.
- Paths in JSON are repository-relative; no timestamp, worktree path, session ID, or instance ID is recorded.
- Direct Match scene, MatchSubScene, `MatchSceneView`, tracker, and accepted `opmap-002` evidence hashes are checked against the exact baseline contract and again after inspection.
- Existing report and temporary output are invalidated before validation; publication uses a validated same-directory temporary file and fails closed.
- Loaded saved-scene setup is restored in `finally`; a single empty untitled batch-mode setup is normalized back to one empty scene, while mixed or non-empty untitled setups fail before inspection.

## Validation

Validation uses Unity `6000.5.2f1` with logs redirected to `/private/tmp`.

- Real ownership probe runs: exit `0`, compiler errors `0`; logs `/private/tmp/opmap004-revision-probe-1.log` and `/private/tmp/opmap004-revision-probe-2.log`.
- Determinism: both runs and the committed JSON are byte-identical at the SHA-256 above.
- Focused negative/shape tests: `26 / 26` passed in `/private/tmp/opmap004-revision-focused-results.xml` with log `/private/tmp/opmap004-revision-focused.log`. They pin exact row targets, types, evidence, rationale, disposition, decision owner, baseline totals and aggregate, ordering, hashes, status/count integrity, fail-closed publication, and ownership-default handling for an empty configured environment value.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed (`9` checks), `/private/tmp/opmap004-revision-gate-non-ecs.log`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: passed (`31` checks), `/private/tmp/opmap004-revision-gate-assembly-boundary.log`.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed (`1` check), `/private/tmp/opmap004-revision-gate-broad-shell.log`.
- `git diff --check` and the exact six-file allowlist: passed before commit; final clean-worktree verification is recorded in the PR handoff.
