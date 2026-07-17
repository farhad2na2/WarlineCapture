# AM-021 Runtime Grid Persistent Storage Ownership Handoff

## Result

The five split-owner persistent grid containers now have one explicit lifecycle owner:

| Container | Creation / resize owner | Disposal owner |
|---|---|---|
| `DynamicBlockerComponent.Counts` | `RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage` | `RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage` |
| `DynamicBlockerComponent.Blocked` | `RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage` | `RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage` |
| `DynamicBlockerComponent.FriendlyPassFactionIds` | `RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage` | `RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage` |
| `DynamicOccupancyComponent.Occupied` | `RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage` | `RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage` |
| `PathPoolComponent.Cells` | `RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage` | `RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage` |

Runtime initialization, bootstrap, duplicate-grid teardown, scenario-lab setup/teardown, and system shutdown delegate to that owner. Disposal clears component-held aliases, so repeated teardown cannot double-dispose. Same-size ensure preserves live state; resize replaces grid-sized storage and clears pooled path cells.

## Validation

- `RuntimeGridPersistentStorageUtilitySystemHelperTests`: 4/4 passed.
- `PersistentResourceOwnershipLifecycleTests`: 8/8 passed.
- `DynamicOccupancyRebuildSystemTests`: 2/2 passed.
- `RuntimeGridDeduplicationSystemTests`: 5/5 passed.
- `python3 -m unittest Tools.CI.tests.test_architecture_persistent_resource_ownership`: passed.
- External AM-021 regeneration: zero ownership gaps; output at `/private/tmp/am021-grid-ownership.json`.
- Unity 6000.5.2f1 compile: zero compiler errors across focused runs.
- `git diff --check`: passed.

Detailed Unity logs and NUnit XML are under `/private/tmp/am021-*-tests.log` and `/private/tmp/am021-*-tests.xml`.

The Post-Hardening Architecture Maturity task can regenerate and publish the canonical AM-021 artifacts after this commit lands.
