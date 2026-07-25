# Operation-Map Authored ECS Workflow

Status: Candidate workflow documented; production remains `StaticSceneChunks`.

This document covers existing-map ECS authoring, building and vehicle ownership, SubScene editing, the frozen static rollback package, and the rules for a later EntityScene production cutover or rollback. The dense-city generation sequence is documented separately in `dense_city_author_workflow.md`.

## 1. Source, Candidate, And Runtime Boundaries

The accepted authoring sources are:

- operation-map scene: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`;
- accepted entity-authoring SubScene: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity`;
- accepted building placements: `Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset`;
- accepted vehicle placements: `Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset`.

The migration candidate is:

- `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_candidate.unity`.

The candidate is derived output, not the source of truth. Do not hand-edit it to preserve a change. Apply an approved source/config change, rebuild the candidate transactionally, then rerun readiness and parity.

The runtime player must load only the thin runtime binding plus the accepted EntityScene package after cutover. Source scene YAML, source SubScene YAML, authoring MonoBehaviours, and the candidate hierarchy are never runtime presentation owners.

## 2. Existing-Map Authored ECS Editing

Use the accepted operation-map scene for map-owned surface/proxy authoring, overrides, typed anchors, minimap/camera metadata, and other non-presentation map content. Use the accepted SubScene for existing ECS presentation authoring only when the tracker authorizes the source edit.

The SubScene presentation hierarchy has exactly one `AuthoredOperationMapEntityPresentation` root and three role roots:

```text
AuthoredOperationMapEntityPresentation
  GameplayBuildings
  GameplayVehicles
  RenderOnly
```

Each role root has one `OperationMapEntityPresentationRootAuthoring` with:

- operation-map id `opmap.skirmish.desert_base_01`;
- one closed role: `GameplayBuildings`, `GameplayVehicles`, or `RenderOnly`;
- schema version `1`;
- the same current migration record-set SHA-256;
- the complete expected-count contract on the gameplay-building root.

Every presentation owner must have one stable source identity and the nearest correct role root. Do not infer ownership from a GameObject name, folder name, renderer type, prefab name, or spatial location.

Surface/proxy authoring stays in the operation-map scene. Every proxy mesh has exactly one nearest `MapBakeGroupAuthoring` with one of the existing roles:

- `IgnoredDecoration`;
- `Terrain`;
- `Road`;
- `Bridge`;
- `Ramp`;
- `Blocker`.

The entity-presentation SubScene must not contain `MapBakeGroupAuthoring` proxy ownership.

## 3. Existing Building Authoring

The legacy building-placement config remains the protected migration input until its retirement is explicitly accepted. Each placement must resolve one-to-one to one candidate `OperationMapBuildingAuthoring`; do not add a second candidate building by hand.

An authored building requires:

- the correct operation-map id;
- one valid source `GlobalObjectId` and no generated dense-city stable id;
- a unique non-negative placement index matching the protected placement row;
- a finite, non-zero transform;
- one `BuildingDefinitionAuthoring`;
- one immediate intact visual root;
- an optional immediate destroyed visual root that is separate from the intact hierarchy;
- every renderer beneath exactly one declared visual state;
- no nested building owner;
- `RubbleRemainsBlocked` unless a separately approved deterministic blocker policy exists.

Roof props, interior dressing, shop signs/awnings, and tent content belong beneath the appropriate building visual state. They are not independent render-only owners.

Populate/rebuild existing candidate buildings with:

`Game > Operation Maps > EntityScene Migration > Populate Candidate Gameplay Buildings`

Then run entity-presentation readiness and candidate bake validation. Any placement-count, identity, transform, visual-state, or managed-runtime-link mismatch rejects the candidate.

## 4. Existing Vehicle Authoring

The legacy vehicle-placement config remains the protected migration input until retirement is accepted. Each row maps one-to-one to one candidate gameplay vehicle beneath `GameplayVehicles`.

An authored vehicle remains a real gameplay `UnitGridAuthoring` root. Its configured model root carries the accepted `OperationMapEntityPresentationIdentityAuthoring`; baking that model marks the gameplay entity with `OperationMapAuthoredVehiclePresentation`. Do not replace the gameplay authoring root with a visual-only clone.

The placement config owns the authoritative root position/rotation. Candidate migration may copy the accepted visual hierarchy, including child overrides, beneath the gameplay root, but must preserve exact source/candidate matrix and transformed-renderer-bounds parity.

Populate/rebuild existing candidate vehicles with:

`Game > Operation Maps > EntityScene Migration > Populate Candidate Gameplay Vehicles`

Reject missing gameplay authoring, duplicate placement identity, a visual-only vehicle owner, lost child overrides, or any mismatch against the protected placement row.

## 5. Render-Only Existing Content

Render-only owners live beneath `RenderOnly` and carry one `OperationMapEntityPresentationIdentityAuthoring`. Preserve the accepted transform parent chain, including inherited shear and negative/non-uniform scale; do not approximate a world matrix by decomposing and reapplying it.

Populate/rebuild render-only owners with:

`Game > Operation Maps > EntityScene Migration > Populate Candidate Render-Only Owners`

If an older candidate has the known transform-hierarchy defect, use only the reviewed repair command:

`Game > Operation Maps > EntityScene Migration > Repair Candidate Render-Only Transform Hierarchies`

Both paths require exact owner and renderer matrix/bounds parity before saving.

## 6. SubScene Candidate Workflow

1. Record the current source/config revision and inspect the worktree.
2. Run `Game > Operation Maps > EntityScene Migration > Create Protected Candidate Hierarchy`.
3. Populate gameplay buildings, gameplay vehicles, and render-only owners through the migration commands.
4. Run `Validate Accepted Source Physics`.
5. Run `Validate Entity Presentation Readiness`.
6. Run `Bake And Validate Candidate Entity Presentation`.
7. Run `Validate Candidate Runtime Physics`.
8. Run `Validate Candidate Presentation Budget`.
9. Run `Bake All Candidate EntityScene`.
10. Review the candidate Bake All, bake, parity, layout, shared-art, and budget reports.

Candidate creation must preserve independent asset GUIDs, protected source bytes, production definition/Addressables ownership, and the frozen rollback package. A failed transaction restores only candidate-owned paths and then revalidates protected production state.

## 7. Frozen Static Rollback Artifacts

The frozen current-map rollback root is:

`Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01`

The broader pre-split rollback ownership set also includes the static manifest/integrity files, generated static scenes, Match shell/SubScene, placement configs, and `ProjectSettings/EditorBuildSettings.asset` listed in `operation_map_scene_split_rollback_recipe.md`.

Rules:

- never regenerate, delete, relabel, move, or reserialize the frozen package during candidate work;
- candidate preflight and postflight snapshots must match it byte-for-byte;
- do not remove the 514 static scenes or production labels until the tracker authorizes the separate cleanup commit;
- retain the exact pre-cutover commit range and path ledger needed for a repository revert.

## 8. EntityScene Cutover

Cutover is not authorized by candidate success alone. Before changing production:

1. complete Editor lifecycle, packed runtime, build-layout, offline package, Android device, performance, visual, navigation, and two-cycle teardown gates;
2. start from a clean reviewed revision and record the pre-cutover SHA;
3. capture the exact production path ledger;
4. require the production definition to reference the accepted thin binding and EntityScene package with zero current-map static manifest/chunk ownership;
5. require Addressables and player build scenes to exclude both source YAML hierarchies;
6. keep shell routing, definition, Addressables layout, and build ownership in one reviewable cutover range;
7. run the complete post-cutover Editor and Android acceptance matrix before static cleanup.

`Game > Operation Maps > Cut Over Current Match Shell` is the transactional Match shell split command. It is not, by itself, approval to switch the operation-map production presentation kind or delete the static rollback package.

## 9. Rollback

### Candidate failure

Use the candidate transaction's recorded rollback. Confirm:

- `rollbackApplied=1` when candidate-owned bytes changed;
- protected production revalidation passed;
- source/SubScene, definition, Addressables settings, and frozen rollback bytes are unchanged;
- stale parity/budget success evidence remains invalid.

Fix the cause and recreate the candidate. Never copy a failed candidate over an accepted source.

### Committed production cutover failure

Follow `operation_map_scene_split_rollback_recipe.md`:

1. use the recorded contiguous cutover commit range;
2. run `git revert --no-commit <first-cutover-sha>^..<last-cutover-sha>`;
3. compare the resulting name-status list with the recorded path ledger;
4. commit the reviewed revert;
5. rerun the authoritative baseline probe, static ownership/integrity tests, two no-op static bakes, Editor gameplay parity, and risk-required Android validation.

Do not use a repository-wide reset/clean, recreate evidence manually, or delete unlisted paths.
