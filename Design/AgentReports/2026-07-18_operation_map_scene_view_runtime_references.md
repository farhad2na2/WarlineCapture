# Operation Map Scene View Runtime References

Date: 2026-07-18
Result: Passed

## Scope

Extended the staged `OperationMapSceneView` with the map-owned references that
the existing Match bootstrap requires after additive loading:

- decoration `CombinedMeshBaker`;
- decoration root;
- building authoring root;
- vehicle authoring root;
- existing map root, surface, placement configs, definition, and subscene.

The view validates scene ownership, exact parent relationships for building and
vehicle roots, and the decoration baker/root identity. The editor stager binds
the exact `Decorations`, `Map/Buildings`, and `Map/Vehicles` hierarchy
objects and fails closed on drift.

The staged definition was regenerated from its authoritative compatibility
source so its current lazy local-content references are retained. No Match
scene or runtime lifecycle changed.

## Validation

- Staged scene ownership/reference/placement suite: `10 / 10` passed.
- Addressables source-scene loader lifecycle against the updated staged view:
  `5 / 5` passed.
- Unity compilation: zero C# compiler errors.
- `git diff --check`: passed after normalizing Unity YAML trailing spaces.
- Scene diff: four serialized reference bindings only.
