# AM-WP-027 - Phase 2 Exit Acceptance

Status: active, dependency-ready, and dispatchable. `AM-021` through `AM-024` are accepted, and `AM-025` owns this evidence/validation package. Release-device, thermal, cold/warm, sustained, and APH-506 certification checks remain deferred and outside Core acceptance.

Umbrella task: `AM-025`

Evidence inputs: accepted AM-021 ownership authority and AM-022 through AM-024 acceptance records.

## 1. Current Validation State And Risk

- The accepted AM-024 integrated baseline passes `157 / 157`; the current AM-025 contract inventory passes `170 / 170` after adding fail-closed closure-audit and blocker-binding checks.
- Canonical source growth currently reports five separately owned blockers: four FirstLaunch `*SystemHelper` paths and the operation-map-owned runtime-grid storage helper.
- AM-021 is accepted at 575 persistent resources: 553 explicit owners, 22 protected owners, and zero ownership gaps.
- `ArchitectureHardeningCloseoutValidationRunner.RunFocusedValidation` is the full Unity architecture entrypoint. `RunJenkinsArchitectureValidation` runs only two suites and is not an AM-025 substitute.
- `SceneLifecycleValidationRunner.Run` is a one-case request-queue smoke test, not lifecycle closeout. Generated-project/dotnet compilation is supporting evidence only; Unity compilation is canonical.
- Current operation-map, FirstLaunch/UI, and package-bound broad-contract failures remain visible external blockers to AM-025 acceptance; they are not retroactive blockers to accepted AM-021 through AM-024 evidence and cannot be silently excluded or converted into Core passes without owner resolution and accepted evidence.

Risks are partial-suite acceptance, alias validators being treated as canonical, stale or dirty evidence, missing identity/hash binding, suppressed external failures, threshold weakening, and release-deferred checks contaminating Core status.

## 2. Accepted Exit Ownership

- AM-025 freezes the exact suite list, thresholds, governed sources, exclusions, environment, and baseline before capture.
- Capture starts from a clean exact commit/tree after AM-021 through AM-024 acceptance. Every canonical Core suite must pass; external blockers remain blockers until their owner resolves them.
- The Phase 2 ownership delta compares the Phase 0 lifecycle inventory and AM-018 hazard inventory against final AM-021 ownership. It preserves `575` historical intake rows separately from the unrelated AM-021 total of `575` owned persistent resources. Every intake row requires one row-bound decision; reviewed genuine debt and unclassified counts must both reach zero.
- Focused allocation suites retain their accepted 180-warmup/300-measurement, exactly-zero recurring production allocation gates.
- Unity compilation with zero compiler errors is canonical. Python/evidence checks, deterministic regeneration, byte-identical projections, and `git diff --check` are mandatory.
- One focused review verifies commands, logs, arithmetic, hashes, ownership delta, residual risks, exclusions, and acceptance decision before the record is published.

No alias substitution, hidden skip, dirty capture, threshold relaxation, broad exclusion, release-lane activation, `SystemBase` migration, or production refactor is allowed in this evidence package.

## 3. Canonical Suite And Identity Contract

Required execution order:

1. verify AM-021 zero-gap authority and accepted AM-022, AM-023, and AM-024 records;
2. freeze policy and capture commit/tree/environment;
3. run `python3 -m unittest discover -s Tools/CI/tests -p 'test_architecture_*.py'`;
4. run `python3 Tools/CI/architecture_persistent_resource_ownership.py --check` and byte-identical ownership/lifecycle regeneration;
5. run canonical source-growth validation and require its 17-test pass marker;
6. run `ArchitectureHardeningCloseoutValidationRunner.RunFocusedValidation` and require its 23-suite pass marker;
7. run accepted lifecycle/recovery/stress/memory suites and governed focused allocation suites;
8. run final Unity compile and require successful exit plus `compilerErrors: 0`;
9. run evidence validators, Python syntax, and `git diff --check`;
10. publish ownership delta, focused review closure, acceptance record, and Progress Snapshot.

Every artifact records schema/task/result, baseline/capture/evidence commit and tree, ancestry, source/tool/test manifests with SHA-256 hashes, exact commands, environment, pass markers/counts, exclusions, residual risks, and review result. Missing or mismatched identity fails closed.

## 4. Exact File Allowlist

Allowed evidence/tool files:

- `Design/AgentReports/ArchitectureMaturity/am025_phase2_exit_policy.json`
- `Design/AgentReports/ArchitectureMaturity/am025_phase2_ownership_delta.json`
- `Design/AgentReports/ArchitectureMaturity/am025_phase2_ownership_delta.md`
- `Design/AgentReports/ArchitectureMaturity/am025_phase2_closure_audit.json`
- `Design/AgentReports/ArchitectureMaturity/am025_phase2_exit_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am025_acceptance_record.json`
- bounded `Design/AgentReports/ArchitectureMaturity/Logs/am025_*.log.gz`
- `Tools/CI/tests/test_architecture_phase2_exit_evidence.py`
- `Tools/CI/tests/test_architecture_phase2_acceptance.py`
- `Design/AgentReports/ArchitectureMaturity/am025_source_growth_validator_schema_amendment.json`
- `Tools/CI/tests/test_architecture_source_responsibility_guardrail_evidence.py` only for the reviewed immutable validator-amendment binding required after a canonical validator schema correction
- one narrow AM-025 evidence generator/runner under `Tools/CI/` if required
- existing architecture evidence generators only for an explicitly reviewed AM-025 schema extension
- AM-025 validation/evidence/Progress Snapshot records in this tracker

Read-only dependencies: all canonical validators/tests, accepted AM-021 through AM-024 evidence, Phase 0/AM-018 inventories, source-growth authority, ownership registry, and Unity project sources/configuration.

Production files are not allowlisted. `Assets/Tests/Editor/ProductionSourceGrowthArchitectureTests.cs` is allowlisted only for the reviewed `genericLifecycleAnchorSymbols` schema/DTO/comparison correction that exposes current blockers; it may not change source ceilings or approve exceptions. Any failing production or protected owner must be resolved in its own reviewed package before recapture.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, UI visual-lock, gameplay implementation, scenes, prefabs, packages, `ProjectSettings`, release/device/thermal/cold-warm/sustained work, APK/build artifacts, and the unrelated Arabic font asset.

## 5. Evidence And Ownership Delta Matrix

The exit bundle must prove:

1. AM-021 final ownership totals (`575` resources, `553` explicit, `22` protected, zero ownership gaps) and the separate AM-025 audit intake/remediation totals, with protected/deferred rows explicitly named;
2. AM-022 ten-case World lifecycle recovery acceptance;
3. AM-023 one-warm-up plus 10-measured production transition acceptance and bounded snapshots; the former 100-cycle stress remains deferred;
4. AM-024 one-warm-up plus five-measured structural-owner and governed-pool plateau; every exceeded Editor memory investigation ceiling and the deferred extended/release follow-up remain explicit, and raw Editor memory totals are not described as passed;
5. integrated Python architecture, persistent-resource check, source-growth, full Unity architecture, lifecycle, and focused allocation results;
6. Unity compile result and exact zero-error marker;
7. Phase 0/AM-018-to-AM-021 row-by-row ownership delta with no missing/duplicate row;
8. protected-path diff audit and deferred-release exclusion audit;
9. deterministic regeneration and exact file/tool/source hash verification;
10. focused review findings, resolutions, rereview result, residual risks, and next dependency-ready task.

Logs remain compressed and bounded; stdout contains summaries and first-failure context only.

## 6. Acceptance And Blocked Rules

AM-025 passes only when every prerequisite is accepted, AM-021 ownership gaps are zero, all `575` audit-intake rows have row-bound authority, genuine debt and unclassified counts are zero, all five source-growth blockers are closed by their owners, capture identity is clean and exact, ancestry/hashes resolve, all canonical Core suites pass, compiler errors are zero, governed recurring allocations are exactly zero, lifecycle counts and retained trends pass, deterministic outputs match, protected paths are absent from the owned diff, and focused review has no unresolved finding.

AM-025 remains unchecked if any prerequisite, counter, suite, marker, hash, log, review, or ownership row is missing; if an external Core failure remains; or if a release-deferred failure is improperly mixed into Core arithmetic. The tracker records exact failing suite/owner/evidence and next action. Thresholds, samples, suites, and failures are never suppressed or reclassified merely to close Phase 2.

On acceptance, update overall progress to `26 / 86` (`30.2%`) and Core progress to `26 / 68` (`38.2%`), set Phase 2 accepted, route the next task to AM-027 because audit-only AM-026 is already accepted, retain Release Certification at `0 / 18` deferred, and publish the final Phase 2 ownership delta and evidence identity.

## 7. Maximum Slices And Rollback

At most three independently stable commits after AM-024 acceptance:

1. frozen exit policy, deterministic ownership-delta/evidence generator, and focused tests;
2. canonical suite capture, bounded logs, and draft evidence;
3. evidence validation, focused review closure, acceptance record, tracker update, commit, and push.

Reject or roll back AM-025-owned evidence/tooling if it omits counters, uses aliases/partial suites, accepts dirty/stale identity, hides skips/failures, weakens thresholds, emits unbounded output, includes protected files, activates release certification, or reports acceptance with any unresolved finding. Never revert separately owned production work.
