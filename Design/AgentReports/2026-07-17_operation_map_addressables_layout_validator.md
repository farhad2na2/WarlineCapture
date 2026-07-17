# Operation Map Addressables Layout Validator

## Scope

Added a fail-closed editor validator for the single approved local operation-map package. It verifies exact owned groups, entry counts, local bundle schemas and packing modes, stable addresses, required labels, deterministic presentation partitions, catalog delivery, manifest ownership, duplicate/foreign addresses, and dependency-owned Entities subscene handling.

## Result

- Compatibility layout: passed.
- Strict complete layout: correctly fails because `operation-map/opmap.skirmish.desert_base_01/minimap-raster` is not yet authored.
- Focused EditMode tests: `2 / 2` passed.
- Phase 0 ownership regression: `157 / 157` passed after evidence refresh.
- Source-growth architecture gate: `13 / 17`; four unchanged upstream narrative ratchet failures, with no validator-path violation.
- Compiler errors: `0`.
- `git diff --check`: passed.
- Log: `/private/tmp/opmap-addressables-validator-tests.log`.
- Results: `/private/tmp/opmap-addressables-validator-tests.xml`.
- Phase 0 results: `/private/tmp/opmap-addressables-validator-phase0-tests.xml`.
- Architecture results: `/private/tmp/opmap-addressables-validator-architecture.xml`.

The validator checklist row is complete. The stable-address and package-completion rows remain open until the real map-specific minimap raster is assigned and validated.
