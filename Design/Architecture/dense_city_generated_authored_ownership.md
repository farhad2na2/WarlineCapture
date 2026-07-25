# Dense City Generated And Authored Ownership

Status: Authoring ownership contract

This document defines which dense-city content is disposable, which content is persistent, and how to preserve a hand edit across regeneration. It supplements `dense_city_author_workflow.md` and `operation_map_authored_ecs_workflow.md`.

## Ownership Matrix

| Content | Owner | Persistent across regeneration | Author action |
|---|---|---:|---|
| Handmade landmark, mission prop, authored road correction, typed anchor, or deliberate exclusion | Accepted authored source, normally beneath `AuthoredCityOverrides` | Yes | Edit the accepted source through reviewed authored ownership |
| Existing military base, handmade city, runway, resources, mountains, roads, and accepted mission content | Protected accepted authored roots | Yes | Do not move, rename, disable, delete, or overlap |
| Legacy building and vehicle placement rows | Protected placement configs | Yes until retirement is accepted | Change the authoritative row/prefab, then rebuild candidate ownership |
| Generated surface/proxy records | `Generated_GiantDenseMiddleEasternCity_MapBakeSource` in the candidate operation-map scene | No | Change generator/config/override inputs and regenerate |
| Generated gameplay-building presentation | `Generated_GiantDenseMiddleEasternCity_EntityPresentation/GameplayBuildings` | No | Change generator/config/source prefab and regenerate |
| Generated independent presentation | `Generated_GiantDenseMiddleEasternCity_EntityPresentation/RenderOnly` | No | Change generator/config/source prefab and regenerate |
| Candidate scenes and candidate-generated assets | Candidate transaction | No; derived from protected inputs | Never treat as the source of truth |
| Bake/parity/budget/layout reports | Their validator or Bake All transaction | No; deterministic evidence | Review, regenerate through the owning command, never hand-edit |
| Static manifest/chunks/integrity rollback package | Frozen production rollback ownership | Yes until separately authorized cleanup | Never mutate during candidate work |

## Disposable Generated Roots

One logical generation owns exactly two marked roots in distinct candidate scenes.

Map-bake source:

```text
Generated_GiantDenseMiddleEasternCity_MapBakeSource
  BakeSources
    Terrain
    Roads
    Bridges
    Ramps
    Blockers
```

Entity presentation:

```text
Generated_GiantDenseMiddleEasternCity_EntityPresentation
  GameplayBuildings
    Buildings
    CivicAndMarket
  RenderOnly
    Infrastructure
    Vegetation
    Props
    Horizon
```

Both roots carry the same generation id, generator schema/version, deterministic seed, and generation hash. Replacement is transactional: generation rejects an existing marked root unless the owning transaction is replacing the complete pair.

Everything beneath these roots is disposable, including renamed children, adjusted transforms, disabled renderers, added props, removed props, material overrides, and manually reparented objects. A saved hand edit beneath a generated root is not protected work.

Generated attachments such as roofs, interiors, shop dressing, signs, awnings, and tents belong to their declared building visual state. They must not be moved into `RenderOnly` to preserve them.

## Persistent Authored Ownership

`AuthoredCityOverrides` is the persistent dense-city correction domain. An override uses `DenseCityAuthoredOverrideAuthoring` with:

- a unique, trimmed stable id of at most 128 characters and no whitespace/control characters;
- finite local center and size, with strictly positive size;
- a finite transform with non-zero scale;
- at least one explicit exclusion flag:
  - presentation;
  - surface;
  - blockers;
- serialized bounds only, with no collider anywhere beneath the override.

Overrides reserve or exclude generation space. They do not silently make arbitrary child renderers valid runtime ECS presentation. A persistent visual must also follow the authored ECS role/identity contract in `operation_map_authored_ecs_workflow.md`.

Protected authored content outside the override domain remains immutable during generation. Readiness compares stable identity, hierarchy path, active state, transform, renderer mesh/material identity, and bounds. Generation fails instead of moving, hiding, renaming, deleting, or overlapping protected content.

## Preserve A Hand Edit

Before regeneration, classify the desired change.

### Change the procedural family

Examples: all road modules, building palette, vegetation density, deterministic placement rule, material family, or generator-wide scale.

1. Change the reviewed generator code, config asset, or persistent source prefab/material.
2. Keep the seed and RNG call order unchanged unless the change is an explicit generator schema/version migration.
3. Add or update focused deterministic/semantic coverage.
4. Recreate and realize the complete candidate.

Do not edit one generated instance and expect the change to propagate.

### Preserve one deliberate exclusion or correction

Examples: keep a plaza clear, reserve an objective area, protect an authored road connection, or suppress generated blockers near a landmark.

1. Create or update a stable `DenseCityAuthoredOverrideAuthoring` in the approved authored domain.
2. Set exact finite bounds and only the required exclusion flags.
3. Give the override a stable id that describes ownership rather than display text.
4. Remove any temporary generated-instance edit.
5. Recreate and realize the candidate.
6. Require protected-overlap and readiness validation to pass.

Do not use colliders as exclusion volumes.

### Preserve one visual or gameplay object

Examples: a handmade landmark, mission prop, authored building, vehicle, objective, runway marker, or camera/minimap anchor.

1. Move or recreate the source object beneath an approved persistent authored owner before regeneration.
2. Give runtime presentation the correct authored ECS root and stable source identity.
3. For a building or vehicle, update the authoritative placement/config and migration input rather than cloning a candidate owner.
4. Add an authored override when generated presentation/surface/blocker space must be reserved around it.
5. Delete the temporary generated copy.
6. Rebuild the candidate and prove one-to-one identity plus transform/bounds parity.

Do not reparent an object directly out of a generated candidate root and call it authored; the accepted source and ownership record must be updated.

## Validation After Ownership Changes

Run the candidate workflow in `dense_city_author_workflow.md` and require:

- exactly one marked root for each generated role;
- no duplicate override stable id;
- valid finite override bounds and at least one exclusion;
- no collider beneath an override;
- no protected authored identity, hierarchy, active-state, transform, mesh/material, or bounds drift;
- no generated/protected overlap outside the named semantic exception;
- every generated renderer under its explicit building or render-only owner;
- every persistent renderer under a valid authored ECS owner;
- accepted source/config bytes changed only when the reviewed authoring step intended them to change;
- candidate outputs and evidence regenerated from the new source fingerprint.

## Forbidden Shortcuts

- Editing candidate scenes as source content.
- Saving a disposable preview over an accepted source/candidate.
- Preserving a generated child by renaming, disabling, or reparenting it.
- Inferring ownership from names, folders, prefab names, bounds, or renderer categories.
- Adding colliders/Rigidbodies to generated or override ownership.
- Hand-editing deterministic reports, hashes, counts, or manifests.
- Mutating frozen static rollback artifacts to make a candidate gate pass.
- Switching production presentation ownership because Editor-only candidate validation passed.
