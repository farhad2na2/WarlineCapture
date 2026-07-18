# Operation Map Match Loader Composition

Date: 2026-07-18
Result: Passed; compatibility-safe, thin-shell activation pending

## Scope

Integrated `OperationMapSceneLoadingSceneSystemHelper` into the existing
`MatchSceneView` lifecycle without adding another `MonoBehaviour` update
owner.

- The current full Match scene keeps its synchronous compatibility path.
- A stripped Match shell with missing map-owned references starts the selected
  catalog definition's Addressables source-scene load.
- The existing `MatchSceneView.Update` polls loading and binds bootstrap only
  after the loaded scene view validates.
- Bootstrap-facing map surface, placement, building, vehicle, decoration, and
  batching properties project from the loaded map view when present.
- Pending source loads are released even if gameplay never binds.
- Bound gameplay tears down before the source-scene handle is released.
- Menu static-presentation binding waits while map source content is loading.

The current `Match.unity` remains unmodified, so this code path is not active
in production yet.

## Validation

- Affected operation-map bootstrap and static-presentation composition:
  `38 / 38` passed.
- Source-scene loader lifecycle: previously passed `5 / 5`.
- Unity compilation: zero C# compiler errors.
- `git diff --check`: passed.

The Phase 5 source-load checkbox remains open until the stripped shell activates
this route in PlayMode and failure/readiness ECS publication is validated.
