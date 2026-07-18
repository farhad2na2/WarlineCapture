# AM-WP-028 - Phase 2 Debt Reconciliation

Status: active audit and remediation package under `AM-025`. It does not change checklist arithmetic and cannot accept Phase 2 by itself.

## 1. Purpose

The first AM-025 delta reported `575` open rows. That number is historical audit intake, not the number of AM-021 ownership gaps and not a valid production-remediation count. AM-021 separately owns `575` persistent resources with `553` explicit owners, `22` protected owners, and zero ownership gaps.

Bounded read-only audits reviewed the AM-025 intake as:

| Measure | Count |
|---|---:|
| Historical intake rows | 575 |
| Projected reviewed non-debt rows | 407 |
| Projected genuine-debt rows | 168 |
| Projected unclassified rows | 0 |
| Source-growth blockers | 5 |

The `407 / 168` split is a planning projection until the row-bound authority artifact is generated, validated, and hash-bound. It is not acceptance evidence and cannot reduce the gate by assertion.

## 2. Required Row Authority

Every historical intake row receives exactly one decision containing:

- source artifact and stable source key without line-number identity;
- decision: `resolved`, `protected-deferred`, or `genuine-debt`;
- reason code, rationale, authority, and evidence path/hash;
- current source path/hash when the source still exists;
- named protected owner ID when deferred;
- debt ID, owner domain, and remediation package for genuine debt.

The generator must reject missing or duplicate rows, unknown decisions, ambiguous matches, stale source hashes, count mismatches, renamed hazards without new-row accounting, fuzzy/path-only matches, or acceptance while any genuine debt remains.

## 3. Reconciliation Order

1. Regenerate current AM-007 and AM-018 scans at the exact capture commit/tree.
2. Reconcile exact retired identities and emit every new identity separately.
3. Match AM-021 resources only by category-compatible stable identity.
4. Classify non-persistent candidates only through explicit exclusion authority, never absence alone.
5. Classify arithmetic `+=` rows through parsed add-assignment structure, not text similarity.
6. Apply exact immutable-data, lifecycle-owner, protected-owner, and explicit-boundary decisions.
7. Emit genuine debt with stable debt IDs and group counts without losing row-level traceability.
8. Validate `historical = non-debt + protected/deferred + genuine debt` and require zero unclassified rows.

## 4. Remediation Lanes

Production remediation is serialized after row-bound evidence and occurs in separately owned packages:

| Lane | Projected debt | Ownership rule |
|---|---:|---|
| World lookup, hidden singleton, and runtime discovery | 12 hazard rows plus overlapping World-owner candidates | Architecture may change unprotected composition/runtime paths; audio and operation-map rows require owner handoff. |
| Mutable static state and caches | 115 hazard rows plus overlapping lifecycle candidates | Split immutable tables, tested subsystem-reset state, World-owned state, UI presentation caches, debug-only state, and genuine gameplay authority before edits. |
| Pools and lifecycle caches | 8 lifecycle rows | Add exact teardown/test authority or repair disposal; do not infer closure from a method name. |
| World-owner candidates | 33 lifecycle rows after exact boundary/protected classifications | Reconcile overlaps with the hazard lane and avoid double-counting one production defect as multiple remediation items. |
| Source growth | 5 helper paths | Four FirstLaunch paths and one operation-map path remain owner-controlled; shrink, consolidate, or publish superseding exact authority. |

Row count and unique debt-item count are reported separately because multiple lexical rows can map to one production fix.

## 5. Scope Safety

- Work directly on `main` with task-owned staging and stable commits.
- Preserve operation-map, FirstLaunch, audio, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, and unrelated dirty work unless the exact owner provides a handoff.
- Prefer unmanaged `ISystem`; do not introduce `SystemBase`, service locators, mutable gameplay registries, broad managers/controllers/providers, or second update owners.
- Naming must follow the project convention; no `*Controller`, `*Player`, vague `*Utility`, or generic ownership shell is introduced.
- Release-only Android, thermal, cold/warm, sustained, package, and certification work remains deferred.

## 6. Validation And Acceptance

Each remediation slice requires focused tests, applicable architecture gates, Unity compilation with zero compiler errors when C# changes, deterministic projection checks, `git diff --check`, and a protected-path diff audit.

AM-025 remains unchecked until:

- all `575` historical intake rows have one validated row-bound decision;
- genuine-debt and unclassified counts are zero;
- all five source-growth blockers are closed by accepted owner action;
- the canonical AM-WP-027 suite passes without exclusions or threshold relaxation.

