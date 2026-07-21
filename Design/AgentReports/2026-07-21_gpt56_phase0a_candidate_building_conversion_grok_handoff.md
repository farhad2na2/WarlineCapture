# Phase 0A Candidate Building Conversion Handoff

## Lane

Support / ECS presentation migration

## Task

Implement the first protected ownership mutation for `opmap.skirmish.desert_base_01`: create an isolated candidate SubScene and convert the 432 accepted authored building placements to candidate ECS authoring without changing production ownership.

## Status

`ImplementationReadyMutationBlockedByHeadlessLicensing`

The transaction code and all affected assemblies compile. Unity did not execute the transaction because the repository-approved batch wrapper could not initialize the 6000.5 headless licensing client. No candidate scene exists yet. No accepted source, production definition, Addressables ownership, or presentation mode changed.

## Files changed

- `Assets/Game/Scripts/Components/OperationMapEntityPresentationComponents.cs`
- `Assets/Game/Scripts/Authorings/OperationMapEntityPresentationRootAuthoring.cs`
- `Assets/Game/Scripts/Authorings/OperationMapBuildingAuthoring.cs`
- `Assets/Game/Scripts/Systems/OperationMapBuildingDestroyedVisualSystem.cs`
- `Assets/Game/Scripts/Editor/OperationMapEntityPresentationMigrationEditor.cs`
- `Assets/Game/Scripts/Editor/OperationMapEntityPresentationCandidateSceneBuilder.cs`
- `Assets/Game/Scripts/Editor/OperationMapBuildingCandidateMigrationEditor.cs`
- focused Editor tests for the candidate transaction, building authoring, and destruction visual state
- `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Contracts touched

- Candidate scene is a new, independent asset at `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_candidate.unity`.
- The existing production-autoloaded SubScene is copied as the candidate seed and is never edited by the transaction.
- Candidate hierarchy is identity-transform and role-marked under `AuthoredOperationMapEntityPresentation`.
- Building joins use accepted placement indices and exact `GlobalObjectId` evidence only. Name/proximity classification is not used.
- Each candidate building owns one `IntactVisual` child and an optional `DestroyedVisual` child. Attached visual ownership remains inside those roots.
- Building simulation remains alive and statically blocked after destruction; the ECS transition swaps intact/destroyed visual-root scales without managed `Instantiate`/`Destroy`.
- Production remains `OperationMapPresentationKind.StaticSceneChunks`. No `EntityScene` cutover is permitted before Editor and Android acceptance.

## User-visible behavior

None. This is candidate-only migration scaffolding and does not alter the shipped map path.

## Validation run

- Compiled with Unity 6000.5 Bee response files: `Game.Components`, `Game.Configs`, `Game.Authoring`, `Game.Runtime`, `Game.Editor`, `Game.Tests.Editor`.
- Attempted the candidate transaction through `Tools/CI/invoke_unity_macos.sh`.
- Rechecked production presentation-kind source and protected source hashes.

## Validation result

- Assembly compile: passed.
- Candidate transaction execution: blocked before `executeMethod` by Unity licensing handshake failure, `Unsupported protocol version '1.18.1'`.
- Candidate asset: absent, as required after a failed transaction.
- Accepted scene SHA-256: `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`.
- Accepted SubScene SHA-256: `eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d`.
- Production cutover: zero.

## Exact continuation

When Unity execution is available, run these in order. Do not run step 2 unless step 1 logs `status=Created` and the candidate GUID differs from the accepted SubScene GUID.

1. Unity menu: `Tools > Warline Capture > Operation Map Migration > Create Protected Candidate Hierarchy`
2. Unity menu: `Tools > Warline Capture > Operation Map Migration > Populate Candidate Gameplay Buildings`

The equivalent repository-approved automation methods are:

- `Game.Editor.OperationMapEntityPresentationCandidateSceneBuilder.CreateProtectedCandidateHierarchy`
- `Game.Editor.OperationMapBuildingCandidateMigrationEditor.PopulateCandidateGameplayBuildings`

After execution, require all of the following before checking either tracker mutation item:

- candidate has a new GUID and is not referenced by the production scene;
- accepted scene, accepted SubScene, and static rollback package hashes are unchanged;
- exactly 432 `OperationMapBuildingAuthoring` owners exist;
- every owner has one intact root, at most one destroyed root, finite transform/footprint/health, and no Collider or Rigidbody;
- candidate baking reports expected gameplay/render-child counts and zero managed map visual components;
- focused Editor destruction validation passes;
- no production definition or canonical mode is changed.

## Known gaps

- The candidate scene mutation and bake have not run due to headless licensing.
- The 9,090 render-only owners are not copied yet.
- Runtime production/selection/runway/faction-material parity still needs candidate integration and acceptance; the first building authoring captures identity, combat, health, footprint, storage, defense, and production prefab data, but does not authorize production cutover.
- Fixed-camera parity, lifecycle validation, Addressables candidate layout, and Android acceptance remain open.

## Exact blocker

- Blocker: Unity 6000.5 headless licensing initialization fails before `executeMethod`.
- Failing message: `Unsupported protocol version '1.18.1'` followed by licensing initialization timeout/reconnect.
- Required command path: `Tools/CI/invoke_unity_macos.sh` per `AGENTS.md`.
- Owner lane: environment / Unity licensing, then ECS presentation migration.
- Can another lane continue: yes, Grok can continue non-mutating analysis or run the two Editor menu transactions in a licensed normal Editor. No lane may flip production ownership.

## Cross-lane impacts

- Grok/map work must keep accepted source hierarchies unchanged until candidate parity is green.
- Runtime/loading and Addressables lanes should not consume the candidate yet.
- Android validation remains user-triggered and must occur only after Editor acceptance.

## Next recommended task

Execute and validate the two candidate transactions, then implement/copy the 9,090 classified render-only owners into the same protected candidate hierarchy. Keep production on `StaticSceneChunks` throughout.
