# AM-025 Feature-Readiness Architecture Closeout Tracker

Date: 2026-08-11
Status: Active. This child tracker closes the Phase 2 architecture gate before new feature implementation resumes.
Parent tracker: `post_hardening_architecture_maturity_tracker.md`
Parent checklist item: `AM-025`
Related work packages: `WorkPackages/am_wp_027_phase2_exit_acceptance.md`, `WorkPackages/am_wp_028_phase2_debt_reconciliation.md`

## 1. Purpose

Close the bounded architecture debt that remained protected while the operation-map and dense-city work was active. The completed map implementation is accepted evidence for ECS ownership, immutable baked data, bounded presentation, lifecycle teardown, rollback, and Android performance, but it does not automatically satisfy the whole-project Phase 2 exit gate.

This tracker is the feature-resume gate recommended after the map closeout:

1. finish `AM-025` now;
2. resume feature work after this child and the parent row are accepted;
3. pair later Phase 3-5 and Phase 7 maturity work with features that touch those domains;
4. add Phase 8 enforcement continuously;
5. leave release-only Phase 6 and Phase 9 deferred until their existing activation contract passes.

The child adds no checklist credit to the parent by itself. Only the accepted `AM-025` parent row advances the maturity tracker from `25 / 86` to `26 / 86` and the Core Architecture Lane from `25 / 68` to `26 / 68`.

## 2. Current Exact State

Starting pushed head: `87157342ab89cbff8ca9de3ec56d3710b93321dd` on `main` and `origin/main`.

The checked Windows wrapper compiled the current project and then correctly failed the focused source-growth gate because eight helper paths lack an exact bound exception at their present size:

| Path | Current size | Existing authority | Required disposition |
|---|---:|---|---|
| `Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs` | 927 lines / 34,174 bytes | none | Decompose the combined scene, Addressables, EntityScene, manifest, transition, rollback, and unload responsibilities before considering any bounded exception. |
| `Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs` | 395 / 15,565 | 295 / 11,398 | Reconcile material growth and extract responsibilities that no longer fit the accepted bootstrap boundary. |
| `Assets/Game/Scripts/Systems/BuildingProductionEntriesUiSystemHelper.cs` | 437 / 18,403 | 351 / 13,949 | Align with the forthcoming Phase 3 change-driven UI projection boundary; decompose material scope growth. |
| `Assets/Game/Scripts/Composition/OperationMapSceneReferenceSceneSystemHelper.cs` | 67 / 2,223 | none | Review the intentionally narrow once-per-transition scene-reference owner and register only if responsibility, tests, and exact ceilings justify it. |
| `Assets/Game/Scripts/Systems/RuntimeGridPersistentStorageUtilitySystemHelper.cs` | 199 / 8,699 | none | Reconcile the accepted AM-021 lifecycle owner with source-growth authority and register only after focused ownership/disposal review. |
| `Assets/Game/Scripts/Environment/RuntimeCityRoadVisualPrototypeSystemHelper.cs` | 286 / 10,976 | 280 / 10,568 | Review the small overrun against the accepted R&D-only presentation responsibility and set no unused headroom. |
| `Assets/Game/Scripts/Environment/RuntimeOperationMapVisualQualitySystemHelper.cs` | 198 / 8,170 | 185 / 7,730 | Review the small overrun against its bounded generation-time quality responsibility and set no unused headroom. |
| `Assets/Game/Scripts/Systems/BuildingDefinitionFootprintCloneSystemHelper.cs` | 72 / 4,230 | 70 / 4,084 | Review the two-line pure-clone change and set no unused headroom. |

Canonical failure evidence: `Build/Logs/am025-current-source-growth.log`, marker `[ProductionSourceGrowthArchitectureValidation] result=Failed`.

The current row-bound Phase 2 delta separately reports `9` genuine-debt rows grouped into `8` unique file/rule items:

- explicit World ownership for runtime-city readiness and road-build composition, including their overlapping global-World findings;
- the `GridAuthoring` World-query boundary;
- the map-surface runtime bootstrap World owner;
- road-preview pool lifecycle;
- runtime hierarchy discovery in algorithmic-aftermath presentation.

These findings must be regenerated at the current exact head before implementation. Historical counts are inputs, not permission to edit a path whose current evidence no longer matches.

## 3. Scope And Ownership

This tracker owns only the production, test, source-authority, evidence, and tracker paths required to close the exact current `AM-025` findings. Each implementation slice must declare its exact allowlist before editing and must preserve one transition/state owner.

Protected unless a slice explicitly claims and re-verifies the exact path:

- scenes, prefabs, generated EntityScenes, Addressables output, packages, and `ProjectSettings`;
- accepted dense-city logical/render data and proxy capacities;
- Android performance thresholds and accepted device evidence;
- audio, FirstLaunch, UI visual-lock, and unrelated feature paths;
- Jenkins/Unity installation paths and wrapper behavior;
- user-owned dirty generated reports and all `_Recovery` scenes.

The worktree must be clean at every accepted commit and after every push. Generated reports may change only when a tracker item explicitly owns their regeneration and validates their exact identity. Unity recovery scenes are never accepted implementation or evidence output and must not remain as untracked worktree state.

## 4. Status Rules

- `[ ]` pending
- `[~]` active; one bounded owner only
- `[x]` complete with committed evidence
- `[!]` blocked with exact evidence, owner, and next action
- Complete tasks in dependency order. Do not check a row from inspection or compilation alone.
- A production change requires focused behavior tests, the applicable architecture gate, Unity compilation with zero errors, deterministic evidence where relevant, `git diff --check`, and a protected-path diff audit.
- Never add an exception merely to make a validator pass. Every retained exception records owner, narrow responsibility, rationale, focused tests, exact current ceiling with no speculative headroom, approval task, review condition, and removal path.
- Use only the repository Unity wrappers with explicit logs, timeouts, and required pass markers. Missing markers, nonzero exits, timeouts, and project locks fail closed.
- Commit and push one stable bounded slice before advancing. Do not amend or squash accepted commits.

## 5. Checklist

### Phase A - Exact-Head Reconciliation

- [ ] `AMFR-001` Re-read `AGENTS.md`, both maturity work packages, both completed dense-map trackers, the parent maturity tracker, source-growth authority, current reports, `main`/`origin/main`, active Unity ownership, and preserved worktree state. Publish the exact slice allowlist and exclusions.
- [ ] `AMFR-002` Regenerate AM-007, AM-018, AM-021, AM-025 ownership-delta, closure-audit, and current source-growth inputs at one exact clean capture identity without changing production code.
- [ ] `AMFR-003` Reconcile every current finding with its accepted owner, tests, previous exception, map-closeout evidence, and removal/review condition. Reject stale, duplicate, retired, or already-resolved findings explicitly rather than silently dropping them.

**Exit:** Current evidence names every genuine-debt item and source-growth finding once, with no unclassified row and no stale blocker count.

### Phase B - Material Responsibility Drift

- [ ] `AMFR-004` Decompose `OperationMapSceneLoadingSceneSystemHelper.cs` into existing-contract-aligned operations and one narrow transition owner. Preserve load/unload ordering, handle ownership, failure unwind, generation matching, readiness publication, retry, sequential switch, and zero new polling loops.
- [ ] `AMFR-005` Reconcile `OperationMapRuntimeBootstrapSceneSystemHelper.cs` growth. Keep one metadata/blob publication owner, explicit owned-blob disposal, map-surface reuse, and no scene-loading or recurring-update authority.
- [ ] `AMFR-006` Reconcile `BuildingProductionEntriesUiSystemHelper.cs` growth with the Phase 3 source-version/projection-cache contract. Preserve produced-unit, pending-production, friendly-queue, and operation-map production behavior while separating unrelated read models rather than creating another coordinator.

**Exit:** The three material-growth paths have bounded responsibilities, characterization/equivalence tests, exact source authority, and passing focused architecture checks.

### Phase C - Narrow Helper Authority

- [ ] `AMFR-007` Review and either register or correct `OperationMapSceneReferenceSceneSystemHelper.cs`, proving exactly-one view resolution occurs once per transition with bounded reused storage and deterministic zero/multiple-view failure.
- [ ] `AMFR-008` Review and either register or correct `RuntimeGridPersistentStorageUtilitySystemHelper.cs`, proving one creator/resizer/disposer, World replacement safety, and no retained native allocation across teardown.
- [ ] `AMFR-009` Review the road-visual prototype helper overrun with focused R&D-root disposal and bounded-generation evidence; retain no speculative ceiling headroom.
- [ ] `AMFR-010` Review the visual-quality helper overrun with explicit cleanup input, deterministic generation-time-only behavior, and no steady-state work; retain no speculative ceiling headroom.
- [ ] `AMFR-011` Review the footprint-clone helper overrun and prove exact field preservation with only the authorized footprint override; retain no speculative ceiling headroom.

**Exit:** Every retained helper has exact, evidence-backed source authority and every rejected helper shape is corrected.

### Phase D - Phase 2 Genuine Debt

- [ ] `AMFR-012` Remove implicit global-World authority from runtime-city readiness and road-build composition. Supply the exact World/EntityManager through the owning lifecycle and prove replacement-World isolation.
- [ ] `AMFR-013` Correct or explicitly bound the `GridAuthoring` runtime-query boundary without introducing a service locator, shipping mutable authority, or per-frame discovery.
- [ ] `AMFR-014` Reconcile map-surface bootstrap World ownership with its created-World lifecycle, generation replacement, blob disposal, and shutdown behavior.
- [ ] `AMFR-015` Give the road-preview pool exact capacity, reuse, exhaustion, and disposal evidence without changing gameplay authority.
- [ ] `AMFR-016` Remove or explicitly bound algorithmic-aftermath runtime hierarchy discovery and allocations. Preserve deterministic presentation and disposal without making authoring GameObjects runtime truth.

**Exit:** The regenerated Phase 2 delta reports zero genuine-debt rows, zero unclassified rows, and no hidden or protected-path substitution.

### Phase E - AM-025 Acceptance

- [ ] `AMFR-017` Pass `ProductionSourceGrowthArchitectureTests.RunFocusedValidation` with the exact 17-test marker and no weakened rule, ceiling, or allowlist.
- [ ] `AMFR-018` Pass the complete `ArchitectureHardeningCloseoutValidationRunner.RunFocusedValidation` 23-suite architecture entrypoint and all required Python ownership/evidence checks.
- [ ] `AMFR-019` Pass the accepted World recreation, scene unload/reload, missing-singleton recovery, one-warm-up plus 10 measured transition cycles, structural/pool trend, and governed focused allocation suites.
- [ ] `AMFR-020` Complete canonical Unity compilation with zero errors; deterministic regeneration; exact artifact/source/tool hashes; protected-path audit; `git diff --check`; and focused independent review with no unresolved finding.
- [ ] `AMFR-021` Publish the AM-025 exit evidence and acceptance record, update the parent tracker to `26 / 86` overall and `26 / 68` Core, mark Phase 2 accepted, and identify `AM-027` as the next maturity task without starting it.
- [ ] `AMFR-022` Publish the feature-resume handoff: later UI, presentation/pooling, simulation, diagnostics, and enforcement work must consume relevant Phase 3-5/7/8 rows when a feature touches those domains; Phase 6 and 9 remain deferred.

**Exit:** This child is complete, `AM-025` is honestly accepted and pushed, the Core lane is green through Phase 2, and new feature planning may resume.

## 6. Validation And Evidence Contract

The parent work packages remain authoritative. This child does not replace their suite order, identity rules, evidence schema, allocation thresholds, lifecycle counts, or acceptance arithmetic.

Minimum final evidence:

1. exact pushed capture commit/tree and ancestry;
2. current deterministic AM-007/AM-018/AM-021/AM-025 reports;
3. zero ownership gaps, zero genuine debt, zero unclassified rows;
4. source-growth 17-test pass;
5. full architecture 23-suite pass;
6. accepted lifecycle, transition, pool/native trend, and focused allocation passes;
7. Unity compiler errors exactly zero;
8. exact hashes and bounded logs for every checked wrapper;
9. no protected or preserved file in any owned diff;
10. focused review with all findings resolved and rereviewed.

Map Android evidence may be referenced where its identity and scenario remain applicable. It cannot substitute for missing whole-project Core validation, and no Android reinstall or new release certification is required for this child.

## 7. Rollback And Stop Rules

Rollback the owned slice if behavior/equivalence tests fail, ownership becomes duplicated, source responsibility broadens, a pool becomes unbounded, a World survives replacement, native memory lacks disposal, evidence is dirty/stale, or a threshold/exception was weakened to pass.

Stop and request new authority only for a genuinely new protected/production asset edit, credential, physical device action, unavailable external resource, or scope expansion beyond `AM-025`. Licensing, wrapper, disk, and bounded tool interruptions follow the already authorized recovery contract in `AGENTS.md` and are not passive blockers.
