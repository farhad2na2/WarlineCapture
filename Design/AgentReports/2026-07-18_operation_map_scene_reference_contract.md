# Operation Map Scene Reference Contract

Date: 2026-07-18
Result: Passed; runtime loader integration pending

## Scope

Added `Game.Composition.OperationMapSceneReferenceSceneSystemHelper` as the
single managed composition boundary for resolving an `OperationMapSceneView`
from an already-loaded additive scene.

The helper:

- rejects invalid or unloaded scenes;
- requires exactly one scene-local `OperationMapSceneView`;
- rejects a requested operation-map id mismatch;
- never caches a scene view across calls;
- reuses bounded root and component scratch lists.

It does not load or unload content, own an update loop, publish ECS state, or
change `Match.unity`.

## Validation

- Focused Unity validation: `5 / 5` passed.
- Warm repeated lookup: `0 B` managed allocation after capacity warmup.
- Unity compilation: zero C# compiler errors.
- `git diff --check`: passed.
- Production source-growth runner: blocked before evaluating the new helper by
  an unrelated current-main guardrail schema mismatch:
  `Unknown=[genericLifecycleAnchorSymbols]`.

The Phase 5 scene-resolution checkbox remains open until the Addressables
source-scene loader invokes this helper only after a successful retained-handle
load.
