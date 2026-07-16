# Operation Map Runtime Bootstrap Contract

Date: 2026-07-16

## Scope

Added a load-strategy-neutral composition boundary that publishes one validated
`OperationMapDefinition` into the existing ECS lifecycle contract. This slice
does not select a map, load or unload scenes, use Addressables, generate map
content, or change `Match.unity`.

## Ownership

- `OperationMapRuntimeBootstrapSceneSystemHelper` receives an explicit `World`.
- The caller supplies approved operation-map, scenario, mission, and generation
  identities.
- The helper owns and disposes only the small immutable metadata blob it creates.
- `MapSurfaceComponent` remains owned by the existing map-surface bootstrap.
- Request/result buffers are initialized without adding a recurring update loop.
- Duplicate roots, stale generations, invalid definitions, and disposed worlds
  fail before replacing the currently published metadata.

## Validation

- Focused EditMode: `5 / 5` passed.
- Production source growth: `15 / 15` passed.
- Non-ECS naming architecture: `9 / 9` passed.
- Non-UI SystemBase migration: `19 / 19` passed.
- Unity compiler errors: `0`.
- Logs: `/private/tmp/opmap-bootstrap-focused.xml`,
  `/private/tmp/opmap-bootstrap-growth.log`,
  `/private/tmp/opmap-bootstrap-nonecs.log`, and
  `/private/tmp/opmap-bootstrap-nonui.log`.

## Progress Decision

This is enabling foundation only. The Phase 5 publication checklist remains
open until production composition publishes a selected map and teardown/failure
behavior is validated through that integration.
