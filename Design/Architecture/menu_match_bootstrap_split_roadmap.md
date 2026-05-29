# Menu/Match Bootstrap Split Roadmap

## Goal

Make `Assets/Game/Scenes/Menu.unity` the persistent app scene and `Assets/Game/Scenes/Match.unity` the load/unload gameplay scene. Split current bootstrap responsibilities into thin scene views plus focused systems:

- `MenuBootstrapView`: MonoBehaviour scene reference holder only.
- `MenuBootstrapSystem`: persistent app/menu lifetime orchestration.
- `MatchSceneView`: MonoBehaviour match-scene reference holder only.
- `MatchBootstrapSystem`: match-scene startup/shutdown orchestration.

The current `GameBootstrap` must not be moved whole into Menu. It should be retired after its app-level and match-level responsibilities are extracted.

## Guardrails

- Preserve build entry order: `Menu.unity` first, `Match.unity` enabled after it.
- Preserve current match startup behavior until a step explicitly moves ownership.
- Do not change gameplay constants, spawn ordering, AI defaults, pathfinding budgets, render budgets, or runtime-city generation semantics.
- MonoBehaviours may only connect serialized scene references and forward lifecycle calls; gameplay/app logic belongs in systems.
- Scene load/unload must be request/response driven, not direct UI-to-scene static calls.
- Every step must keep either the old path or the new path working; no half-migrated runtime startup.

## Progress

- [x] Step 1: Rename `Game.unity` to `Match.unity` and preserve its scene GUID.
- [x] Step 2: Keep `Menu.unity` first in build settings and make `Match.unity` the enabled gameplay scene after it.
- [x] Step 3: Update source/test scene path constants from `Game.unity` to `Match.unity`.
- [ ] Step 4: Add scene lifecycle ECS request/response components.
- [ ] Step 5: Add `SceneLifecycleSystem` as the single additive load/unload boundary.
- [ ] Step 6: Add `MenuBootstrapView` to `Menu.unity` with only shell/router/camera/config references.
- [ ] Step 7: Add `MenuBootstrapSystem` and move persistent UI shell startup into it.
- [ ] Step 8: Route main-menu match entry through an ECS `LoadMatchSceneRequest`.
- [ ] Step 9: Add unload match request path for match exit/result return.
- [ ] Step 10: Add `MatchSceneView` to `Match.unity` with only match scene references currently held by `GameBootstrap`.
- [ ] Step 11: Add `MatchBootstrapSystem` with no behavior changes, initially driven by existing `GameBootstrap`.
- [ ] Step 12: Extract world camera, lighting, volume, root references from `GameBootstrap` into `MatchSceneView`.
- [ ] Step 13: Extract managed runtime system construction from `GameBootstrap` into `MatchBootstrapSystem`.
- [ ] Step 14: Extract match startup config projection from `GameBootstrap` into `MatchBootstrapSystem`.
- [ ] Step 15: Extract match runtime update entry point from `GameBootstrap` into `MatchBootstrapSystem`.
- [ ] Step 16: Extract match shutdown/cleanup into `MatchBootstrapSystem`.
- [ ] Step 17: Move persistent diagnostics/logging service setup to `MenuBootstrapSystem`.
- [ ] Step 18: Move persistent UI shell/menu setup to `MenuBootstrapSystem`.
- [ ] Step 19: Move app-level config/service registration to `MenuBootstrapSystem`.
- [ ] Step 20: Keep match-only configs in `MatchSceneView` or authored config assets referenced by `MatchBootstrapSystem`.
- [ ] Step 21: Replace direct play button match startup with scene lifecycle request.
- [ ] Step 22: Replace direct return-to-menu flow with match unload request and menu route response.
- [ ] Step 23: Add bootstrap transition state so duplicate load/unload requests are ignored safely.
- [ ] Step 24: Add tests proving `Menu.unity` is first enabled build scene and `Match.unity` is second.
- [ ] Step 25: Add tests proving `MenuBootstrapView` has no gameplay scene references.
- [ ] Step 26: Add tests proving `MatchSceneView` owns match references and contains no app/menu shell logic.
- [ ] Step 27: Add tests proving `GameBootstrap` no longer owns app-level responsibilities.
- [ ] Step 28: Remove `GameBootstrap` scene dependency once `MatchBootstrapSystem` owns match startup.
- [ ] Step 29: Rename/remove remaining `GameBootstrap` type only after all production and test references are migrated.
- [ ] Step 30: Add load smoke: boot `Menu.unity`, request match, verify `Match.unity` loads additively and gameplay starts.
- [ ] Step 31: Add unload smoke: exit match, verify `Match.unity` unloads and menu remains alive.
- [ ] Step 32: Add repeated load/unload smoke to catch stale ECS world, event, and object references.
- [ ] Step 33: Run focused performance comparison against current baseline.
- [ ] Step 34: Remove temporary compatibility allowances and old bootstrap wording from architecture tests/contracts.
- [ ] Step 35: Final validation gate in `WarlineCapture-CodexUnity1`.

## Validation Gate

Run these before marking the roadmap complete:

- EditMode architecture tests covering bootstrap naming, scene ownership, and MonoBehaviour view limits.
- `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` against `Match.unity`.
- PlayMode smoke: `Menu.unity` boots, loads `Match.unity`, starts gameplay, exits, unloads `Match.unity`, returns to menu.
- Repeated load/unload smoke for at least three cycles.
- FPS diagnostics baseline with one AI enemy faction enabled.

## Expected End State

- `Menu.unity` stays loaded for the app lifetime.
- `Match.unity` loads additively only when a match starts.
- `Match.unity` unloads when returning to menu.
- `GameBootstrap` is retired or reduced to a temporary compatibility shim with a scheduled deletion step.
- Bootstrap logic is in systems; scene MonoBehaviours are views/reference holders only.
