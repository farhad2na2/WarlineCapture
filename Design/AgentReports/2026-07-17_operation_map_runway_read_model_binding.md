# Operation Map Runway Read Model Binding

## Scope

The existing `BuildingFactionRunwayReadModel` publication now consumes immutable runway anchors from the active loader-neutral operation-map metadata. This slice does not add map loading, Addressables, scene changes, editor generation, or helipad slot behavior.

## Runtime Behavior

- Active-map runway anchors are validated before publication for faction/lane identity, duplicate ownership, finite geometry, positive half-length, and runtime-grid containment.
- Valid anchors publish exact takeoff, landing, center, direction, faction, lane identity, and cells through the existing runway read-model buffer.
- A faction with active-map runway metadata no longer receives a duplicate building-derived fallback runway.
- Factions without map runway metadata keep the current building-derived compatibility path.
- No-active-map behavior is unchanged.
- Active-map generation participates in the existing building read-model dirty signature, so activation, replacement, and teardown republish without a new update loop.
- Invalid active metadata fails closed without publishing partial map runway rows.

## Architecture And Performance

- Lookup and projection are pure static utility work over the existing immutable `OperationMapBlob` and ECS buffer.
- `FixedList512Bytes<byte>` tracks map-owned factions without managed allocation.
- No manager, controller, facade, service locator, update-loop `MonoBehaviour`, delivery dependency, or broad replacement system was introduced.
- General signature hashing moved to `BuildingRuntimeSignatureUtility`, keeping the frozen composition owner within its source-growth budget.
- Helipad reservation and occupancy remain under `BuildingFactionProductionSpawnPointReadModel`; this runway slice does not duplicate mutable slot state into map metadata.

## Validation

- Focused runway projection tests: `5 / 5` passed.
- Affected metadata, ownership, naming, and source-growth EditMode suite: `119 / 119` passed.
- Unity compilation completed with no C# compiler errors.
- Camera/minimap ownership evidence regenerated twice byte-identically: SHA-256 `de9d5b30b69eff284306bcfcb0b1f989864b0e1804432f22c7c0ddfc88406776`.
- Navigation ownership evidence regenerated twice byte-identically: SHA-256 `849a9f1a4d4a325f48f86c301f08f93054cb02a136db01150a6b89560196d037`.
- `git diff --check` passed.
- No new scene-wide lookup API usage exists in the touched C# files.

## Remaining Work

The Phase 6 runway/helipad checklist item remains open. Runway read-model ownership is integrated, but taxi/takeoff/return/landing acceptance and the separate active-map helipad gameplay consumer path still require focused runtime validation before the combined item can close.
