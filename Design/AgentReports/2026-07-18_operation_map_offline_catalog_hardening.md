# Operation Map Offline Catalog Hardening

Date: 2026-07-18
Result: Passed foundation; Android device acceptance pending

## Change

- Local Addressables configuration now disables remote catalog generation,
  startup catalog updates, and unique bundle ids.
- The layout validator fails closed if those settings drift.
- The content-build gate parses generated `settings.json` and requires exactly
  one catalog under `Addressables.RuntimePath`; HTTP and HTTPS catalog locations
  are rejected.

## Validation

- Focused EditMode tests: `17 / 17` passed.
- Local Addressables content build: passed; deterministic report remained a no-op.
- Generated runtime settings use
  `{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin` and set
  `m_DisableCatalogUpdateOnStart` to `true`.
- Fresh local-bundle Editor lifecycle PlayMode test: `1 / 1` passed.
- Zero C# compiler errors and `git diff --check` passed.

No Android device was attached. Device offline launch, teardown, and sequential
reload remain open and this evidence does not close the Phase 2A offline-launch
checkbox.
