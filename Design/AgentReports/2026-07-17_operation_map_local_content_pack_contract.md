# Operation Map Local Content-Pack Contract

Date: 2026-07-17

## Scope

Approved the normative local Addressables group contract and introduced typed
catalog/content-pack delivery data without creating groups or changing runtime
loading behavior.

## Accepted Initial Contract

- Catalog group: `Operation Maps - Catalog`.
- Explicit shared group: `Operation Maps - Shared`.
- Per-map groups: `Operation Map - Local - <slug> - Core` and
  `Operation Map - Local - <slug> - Presentation`.
- Initial physical-map count: exactly one,
  `opmap.skirmish.desert_base_01`.
- Initial delivery kind: `BuiltInLocal`.
- Content-pack identity: `opmap-pack.skirmish.desert_base_01`.
- Content version/hash must exactly match the referenced map definition.

## Validation

- `OperationMapCatalogConfigTests`: `4 / 4` passed.
- Runtime-bootstrap catalog integration: `10 / 10` passed.
- Phase 0 ownership regression after manifest refresh: `157 / 157` passed.
- Architecture contract: `50 / 58` passed; the eight failures are existing
  project-wide assembly/runtime/UI guardrail debt and do not name the new
  `Game.Configs` source.
- Accepted static-presentation rebake: `514` chunks reused, `0` scenes
  written, `0` scenes deleted.
- Zero compiler errors.
- `git diff --check` passed.
- Log: `/private/tmp/opmap-content-pack-tests-final.log`.
- Results: `/private/tmp/opmap-content-pack-tests-final.xml`.
- Runtime-bootstrap results: `/private/tmp/opmap-content-pack-bootstrap-tests.xml`.
- Manifest refresh: `/private/tmp/opmap-content-pack-bake-final.log`.
- Phase 0 results: `/private/tmp/opmap-content-pack-phase0-final.xml`.
- Architecture results: `/private/tmp/opmap-content-pack-architecture.xml`.

No Addressables setting, group, scene, generated chunk, or runtime loader was
changed in this slice.
