# Operation Map Sequential Lifecycle

Date: 2026-07-18
Scope: two consecutive production Match lifecycles using the single local-Addressables map
Result: passed

## Evidence

- `Aph805MenuMatchMenuLifecyclePlayModeTests`: 2/2 passed.
- `OperationMapRuntimeBootstrapSceneSystemHelperTests`: 12/12 passed.
- Unity compilation produced no C# compiler errors.
- `git diff --check`: passed.
- Unity validation used the documented out-of-sandbox macOS licensing path.

## Accepted Sequence

Each production cycle performs:

1. Menu shell requests Match.
2. Match shell loads additively.
3. `opmap_skirmish_desert_base_01` loads through retained local Addressables handles.
4. Exactly one operation-map ECS root is published in the existing World.
5. Return drains static presentation, stops Match gameplay, restores render ownership, and clears map ECS state.
6. The operation-map source scene unload completes before the Match shell unloads.

The second cycle repeats the sequence in the same World. Between cycles, the source scene is not loaded, the operation-map root count is zero, destroyed Match references are cleared, and the later load publishes exactly one root again.

This validates sequential shell-routed scenario/map transitions for the accepted one-physical-map rollout. Direct in-match hot swapping is not part of the current product route.
