# BuildingPlacementSystem Retirement Audit

Date: 2026-05-24
Lane: Gameplay

## Current Status

`BuildingPlacementSystem` is retired and deleted.

Deleted files:
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs.meta`

Runtime composition constructs `BuildingGameplaySystem` through `BuildingGameplayCompositionSystem` and passes narrow systems, contexts, callbacks, and disposal through managed composition.

## Hard Rule

`BuildingPlacementSystem` must not exist.

No production code, editor test, playmode test, scene, prefab, asset, generated source, or compatibility wrapper may construct, serialize, type against, or reference the exact facade type.

Allowed related names:
- `BuildingPlacementSystemConfig`
- `BuildingPlacementSystemSceneConfigAsset`

These are config asset types, not the retired gameplay facade.

## Closed Deletion Gates

17. Inventory final facade blockers and freeze this audit. Complete.
18. Extract remaining runtime context factories from the facade. Complete.
19. Extract selection and query facade wrappers. Complete.
20. Extract config/init ownership into managed composition or narrow startup/config systems. Complete.
21. Replace production composition construction with `new BuildingGameplaySystem()`. Complete.
22. Migrate remaining editor validation tests to `BuildingGameplayTestHarness` or narrower systems. Complete.
23. Delete `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` and its `.meta`. Complete.
24. Remove facade allowlists and update the architecture contract to require zero facade references. Complete.
25. Run the validation gate: architecture tests, building runtime boundary tests, bootstrap/menu playmode smoke, and one focused runtime load validation.

## Ongoing Guard

Building behavior belongs in the owning narrow `*System` boundary. Do not recreate the facade as a wrapper, singleton, source generator output, nested type, test harness, or serialized component.
