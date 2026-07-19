# Operation Map UI Identity Read Model

Date: 2026-07-19
Result: Passed

## Scope

- Added `UiMatchIdentityReadModelComponent` to the existing unmanaged UI shell contract boundary.
- Added Burst `UiMatchIdentityReadModelSystem` to copy `OperationMapId`, `ScenarioId`, and `MissionId` from the canonical `ActiveOperationMapComponent`.
- The system writes only when identity changes, increments a stable version, and clears the read model after operation-map unload.
- No managed presentation loop, controller, service, provider, or map-specific UI branch was introduced.

## Validation

- `UiMatchIdentityReadModelSystemTests`: 3 passed, 0 failed.
- Shell-boundary creation, publication, identity change, unchanged no-op, and unload clearing are covered.
- 512 unchanged updates allocate 0 managed bytes on the current thread.
- Naming-convention guardrail: 1 passed, 0 failed.
- Burst hot-path architecture guardrail: 10 passed, 1 existing unrelated failure (`HotPathArraySnapshotDebtMustNotIncrease`, current `1`, ceiling `0`). The new system uses no array snapshot.
- Unity log: `/private/tmp/opmap-ui-identity-final.log`.
- NUnit XML: `/private/tmp/opmap-ui-identity-final.xml`.
- `git diff --check`: passed before final validation.
