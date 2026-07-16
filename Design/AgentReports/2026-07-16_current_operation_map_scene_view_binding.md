# Current Operation Map Scene View Binding

Date: 2026-07-16

## Scope

Added one `Game.Composition.OperationMapSceneView` root to the staged current
operation-map scene. The serialized, non-updating view binds only:

- operation-map id and `OperationMapDefinition`;
- the staged `Map` root;
- the staged scene's `MapSurfaceAuthoring`;
- the distinct operation-map building and vehicle placement configs;
- the staged map-owned Entities `SubScene` component.

The view has getters and validation only. It has no `Update`, `LateUpdate`, or
`FixedUpdate`, no singleton/self-registration behavior, no hierarchy search,
and no loading or gameplay policy. The editor stager performs the one-time
scene search and rejects missing or duplicate views/components.

The canonical source scene and subscene remain unchanged:

- `Match.unity`: `dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`
- `MatchSubScene.unity`: `bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8`

## Validation

- Focused scene staging/view EditMode tests: `8 / 8` passed.
  - Results: `/private/tmp/opmap-scene-view-tests.xml`
  - Log: `/private/tmp/opmap-scene-view-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-scene-view-architecture.xml`
  - Log: `/private/tmp/opmap-scene-view-architecture.log`
- Unity compilation: zero compiler errors.
- No loader, Addressables, update loop, current Match binding, or runtime route
  changed.
