# Operation Map Phase 0 Ownership Evidence

Task: `opmap-004`

Baseline revision: `3b7228292db7159c3c70025cf5d1676573721cd4`

## Scope

`Game.Editor.OperationMapPhase0OwnershipProbe` performs a read-only ownership inspection of exactly:

- 28 serialized `MatchSceneView` Unity-object references;
- 16 ordered roots in `Assets/Game/Scenes/Match.unity`;
- 3 ordered roots in `Assets/Game/Scenes/Match/MatchSubScene.unity`.

The probe does not invoke a baker, regenerate static presentation output, save a scene, or rescan the generated 514-scene presentation set. Presentation and placement totals are parsed as cross-referenced evidence from the accepted `opmap-002` report.

## Result

Status: `NeedsDecision`

The report contains 47 complete ownership rows and deliberately does not claim `Passed`. Seven rows require an explicit architecture or design decision:

- `dayNightConfig` and `directionalLight` references;
- `Start` and `End` Match roots;
- both Match directional-light roots;
- `InitialUnitsSpawnerAuthoring` in `MatchSubScene`.

Every row records deterministic current-target identities and collection cardinality. Every `Mixed` or `Unresolved` row also identifies a decision owner, rationale, evidence paths, and migration disposition. The evidence therefore supports review without closing either Phase 0 ownership checkbox.

## Determinism And Guardrails

- Output schema: `warline.operation-map.phase0-ownership`, version `1`.
- Committed JSON SHA-256: `33105709add01dc271f6ae01a9b12b8a53e7f195fd97585e31c0942a1c44360a`.
- Two real Unity probe runs produced byte-identical JSON.
- Paths in JSON are repository-relative; no timestamp, worktree path, session ID, or instance ID is recorded.
- Direct Match scene, MatchSubScene, `MatchSceneView`, tracker, and accepted `opmap-002` evidence hashes are checked against the exact baseline contract and again after inspection.
- Existing report and temporary output are invalidated before validation; publication uses a validated same-directory temporary file and fails closed.
- Loaded saved-scene setup is restored in `finally`; a single empty untitled batch-mode setup is normalized back to one empty scene, while mixed or non-empty untitled setups fail before inspection.

## Validation

Validation uses Unity `6000.5.2f1` with logs redirected to `/private/tmp`.

- Real ownership probe run: exit `0`, compiler errors `0`.
- Determinism: two runs byte-identical.
- Focused negative/shape tests: `15 / 15` passed. They cover field/root drift, ordering drift, unsupported baseline schema, empty baseline counts, missing or drifted hashes, invalid `Passed` status, classification-count drift, target-cardinality drift, missing decision owner, and fail-closed publication.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: passed.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed.
- `git diff --check` and the exact six-file allowlist: passed before commit; final clean-worktree verification is recorded in the PR handoff.
