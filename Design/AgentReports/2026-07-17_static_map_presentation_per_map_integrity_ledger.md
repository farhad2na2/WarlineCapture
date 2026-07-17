# Static Presentation Per-Map Integrity Ledger

Date: 2026-07-17

## Result

Passed. Static-presentation integrity ownership is now derived from the operation-map id. The current compatibility map owns:

`Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationSceneIntegrity.json`

`StaticMapPresentationBakeInput`, the baker, baseline probe, and Android build resolver pass the selected operation-map id and exact ledger path through validation. A ledger path owned by another map fails closed with `integrity-ledger-owner-changed`.

## Validation

- Focused EditMode fixtures: `89 / 89` passed, zero compiler errors. Result: `/private/tmp/opmap-per-map-integrity-tests-escalated-3.xml`; log: `/private/tmp/opmap-per-map-integrity-tests-escalated-3.log`.
- Ownership and architecture gates: `85 / 85` passed, zero compiler errors. Result: `/private/tmp/opmap-per-map-integrity-final-tests.xml`; log: `/private/tmp/opmap-per-map-integrity-final-tests.log`.
- Canonical bake: passed with `sources=16542`, `chunks=514`, `scenesWritten=0`, `staleScenesDeleted=0`, and `reuseRejectionReason=none`. Log: `/private/tmp/opmap-per-map-integrity-noop-bake.log`.
- `git diff --check`: passed.

No source scene, generated chunk scene, `.meta`, runtime loading behavior, Addressables setting, or second physical map was changed.
