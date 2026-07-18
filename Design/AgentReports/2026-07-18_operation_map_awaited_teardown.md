# Operation Map Awaited Teardown

Date: 2026-07-18
Scope: normal Match-to-menu teardown for the single local-Addressables operation map
Result: passed; sequential map switching remains open

## Change

- Retain and poll the Addressables source-scene unload operation instead of issuing an unobserved unload.
- Drain static presentation before stopping map gameplay and clearing runtime metadata.
- Await source-scene unload before queuing Match shell unload.
- Release source load, source unload, and manifest handles on success and failure.
- Keep terminal unload failure visible until the explicit reset path.

## Validation

- `OperationMapSceneLoadingSceneSystemHelperTests`: 12/12 passed, including pending unload, successful release, and failed-unload cleanup.
- `StaticMapPresentationSceneWiringTests` and related presentation tests: 33/33 passed with zero compiler errors. Unity produced the successful XML before a native worker shutdown crash.
- Menu -> Match -> menu PlayMode lifecycle: 1/1 passed in 48.741 seconds with exit code 0, exercising the real local Addressables unload path.
- `git diff --check`: passed.
- Final architecture rerun did not start because Unity licensing initialization timed out; the process was stopped after the project wrapper's reset attempt. The previously accepted architecture gate for the adjacent loader slice was 9/9, but it is not claimed as fresh evidence for this slice.

## Remaining Acceptance

- Prove sequential map switching and the same deterministic teardown order during switch.
- Complete renderer restoration, typed load failure reasons, and full map-specific state clearing evidence.
- Rerun the architecture/naming gate when the Unity licensing channel is available.
