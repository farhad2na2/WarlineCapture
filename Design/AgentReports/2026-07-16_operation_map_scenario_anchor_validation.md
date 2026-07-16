# Operation Map Scenario Anchor Validation

Date: 2026-07-16

## Scope

Added a loader-neutral scenario contract for required operation-map anchors.
`ScenarioSetupConfig` now carries bounded anchor id/kind requirements, and the
existing aggregate contract validation rejects missing anchors, mismatched
kinds, duplicate requirements, invalid ids, and `None` kinds.

Compatibility scenarios may keep an empty requirement list. No scene,
operation-map asset, presentation output, loading policy, or runtime update
loop changed.

## Architecture

- Serialized authoring remains in `Game.Configs`.
- Anchor kinds reuse the unmanaged `Game.Components.OperationMapAnchorKind`.
- Validation is deterministic, ordinal, allocation-free after config loading,
  and runs outside gameplay hot paths.
- The scenario owns required anchor identities; the operation map owns anchor
  transforms and kinds.

## Validation

- Focused identity/contract EditMode tests: `33 / 33` passed.
  - Results: `/private/tmp/opmap-scenario-anchor-tests.xml`
  - Log: `/private/tmp/opmap-scenario-anchor-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-scenario-anchor-architecture.xml`
  - Log: `/private/tmp/opmap-scenario-anchor-architecture.log`
- Unity compilation: zero compiler errors.
- `git diff --check`: passed.

The documented `Tools/CI/invoke_unity_macos.sh` wrapper was used after a raw
sandboxed Unity attempt entered the known Licensing Client IPC reconnect loop.
