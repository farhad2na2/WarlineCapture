# Operation Map Contract Validation

Date: 2026-07-16

## Scope

Added one load-strategy-neutral validation utility for already-loaded small
`OperationMapDefinition` and `ScenarioSetupConfig` assets plus immutable expected
evidence supplied by composition, registration, or an authoritative build probe.

## Contract

- Requires at least one non-null operation-map definition.
- Rejects null scenario/evidence collections and null entries.
- Enforces ordinal uniqueness for operation-map and scenario ids.
- Reuses definition validation for identity, bounds, cameras, minimap, anchors,
  duplicate anchors, versions, and lowercase SHA-256 fields.
- Requires exactly one unique evidence record per definition.
- Rejects validly formatted but stale schema, content version, source identity,
  content hash, or generated-metadata hash evidence.
- Rejects scenarios whose operation-map id does not resolve.
- Performs no asset search, scene access, loading, Addressables work, or update.
- Allocates zero bytes on the warmed successful path.

## Validation

- Focused EditMode: `7 / 7` passed.
- Production source growth: `15 / 15` passed.
- Non-ECS naming gate: `9 / 9` passed during the slice.
- Unity compiler errors: `0`.
- `git diff --check`: passed.
- Logs: `/private/tmp/opmap-contract-validation.xml`,
  `/private/tmp/opmap-contract-validation.log`,
  `/private/tmp/opmap-contract-growth.log`, and
  `/private/tmp/opmap-contract-nonecs.log`.

The utility validates evidence but does not decide who produces it. Current-map
registration and the eventual selected loader must provide authoritative values
without changing this validation contract.
