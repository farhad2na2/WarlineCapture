# Menu/Match Bootstrap Split Roadmap

## Goal

Make `Assets/Game/Scenes/Menu.unity` the persistent app scene and `Assets/Game/Scenes/Match.unity` the load/unload gameplay scene. Split current bootstrap responsibilities into thin scene views plus focused systems:

- `MenuBootstrapView`: MonoBehaviour scene reference holder only.
- `MenuBootstrapSystem`: persistent app/menu lifetime orchestration.
- `MatchSceneView`: MonoBehaviour match-scene reference holder only.
- `MatchBootstrapSystem`: match-scene startup/shutdown orchestration.

`GameBootstrap` is retired. It must not be restored or moved whole into Menu; app-level and match-level responsibilities now live in the Menu/Match bootstrap systems above.

## Guardrails

- Preserve build entry order: `Menu.unity` first, `Match.unity` enabled after it.
- Preserve current match startup behavior until a step explicitly moves ownership.
- Do not change gameplay constants, spawn ordering, AI defaults, pathfinding budgets, render budgets, or runtime-city generation semantics.
- MonoBehaviours may only connect serialized scene references and forward lifecycle calls; gameplay/app logic belongs in systems.
- Scene load/unload must be request/response driven, not direct UI-to-scene static calls.
- Every step must keep either the old path or the new path working; no half-migrated runtime startup.
- The `Menu.unity` footer `DeployCommandButton` is the new player-facing start trigger. It must replace the old disabled `Match.unity` Canvas play button behavior without requiring that old Canvas to be active.
- Starting a match is a two-phase contract: first load/show the Match UI and Match scene, then issue the same gameplay-start request that the old Match Canvas Play button issued.

## Progress

- [x] Step 1: Rename `Game.unity` to `Match.unity` and preserve its scene GUID.
- [x] Step 2: Keep `Menu.unity` first in build settings and make `Match.unity` the enabled gameplay scene after it.
- [x] Step 3: Update source/test scene path constants from `Game.unity` to `Match.unity`.
- [x] Step 4: Add scene lifecycle ECS request/response components.
- [x] Step 5: Add `SceneLifecycleSystem` as the single additive load/unload boundary.
- [x] Step 6: Add `MenuBootstrapView` to `Menu.unity` with only shell/router/camera/config references.
- [x] Step 7: Add `MenuBootstrapSystem` and move persistent UI shell startup into it.
- [x] Step 8: Route `FooterContent/DeployCommandButton` from `Menu.unity` through an ECS `LoadMatchSceneRequest`.
- [x] Step 9: Add a match-start command/request that mirrors the old `Match.unity` Canvas Play button behavior by setting the runtime gameplay state to play requested only after Match UI/scene load is ready.
- [x] Step 10: Keep `FooterContent/DeployCommandButton` on the footer as the only required player-facing start button; the old `Match.unity` Canvas may stay disabled and must not be required for gameplay start.
- [x] Step 11: Add unload match request path for match exit/result return.
- [x] Step 12: Add `MatchSceneView` to `Match.unity` with only match scene references currently held by `GameBootstrap`.
- [x] Step 13: Add `MatchBootstrapSystem` with no behavior changes, initially driven by existing `GameBootstrap`.
- [x] Step 14: Extract world camera, lighting, volume, root references from `GameBootstrap` into `MatchSceneView`.
- [x] Step 15: Extract managed runtime system construction from `GameBootstrap` into `MatchBootstrapSystem`.
- [x] Step 16: Extract match startup config projection from `GameBootstrap` into `MatchBootstrapSystem`.
- [x] Step 17: Extract match runtime update entry point from `GameBootstrap` into `MatchBootstrapSystem`.
- [x] Step 18: Extract match shutdown/cleanup into `MatchBootstrapSystem`.
- [x] Step 19: Move persistent diagnostics/logging service setup to `MenuBootstrapSystem`.
- [x] Step 20: Move persistent UI shell/menu setup to `MenuBootstrapSystem`.
- [x] Step 21: Move app-level config/service registration to `MenuBootstrapSystem`.
- [x] Step 22: Keep match-only configs in `MatchSceneView` or authored config assets referenced by `MatchBootstrapSystem`.
- [x] Step 23: Replace direct play button match startup with scene lifecycle plus match-start requests.
- [x] Step 24: Replace direct return-to-menu flow with match unload request and menu route response.
- [x] Step 25: Add bootstrap transition state so duplicate load/unload/start requests are ignored safely.
- [x] Step 26: Add tests proving `Menu.unity` is first enabled build scene and `Match.unity` is second.
- [x] Step 27: Add tests proving `MenuBootstrapView` has no gameplay scene references.
- [x] Step 28: Add tests proving `MatchSceneView` owns match references and contains no app/menu shell logic.
- [x] Step 29: Add tests proving `FooterContent/DeployCommandButton` requests Match load and then gameplay start without using the old `Match.unity` Canvas.
- [x] Step 30: Add tests proving `GameBootstrap` no longer owns app-level responsibilities.
- [x] Step 31: Remove `GameBootstrap` scene dependency once `MatchBootstrapSystem` owns match startup.
- [x] Step 32: Rename/remove remaining `GameBootstrap` type only after all production and test references are migrated.
- [x] Step 33: Add load smoke: boot `Menu.unity`, press footer Deploy command, verify `Match.unity` loads additively and gameplay starts.
- [x] Step 34: Add unload smoke: exit match, verify `Match.unity` unloads and menu remains alive.
- [x] Step 35: Add repeated load/unload smoke to catch stale ECS world, event, and object references.
- [x] Step 36: Run focused performance comparison against current baseline.
- [x] Step 37: Remove temporary compatibility allowances and old bootstrap wording from architecture tests/contracts.
- [x] Step 38: Final validation gate in `WarlineCapture-CodexUnity1`.

Step 38 result:

- Focused Menu/Match PlayMode smoke passed: `/private/tmp/warlinecapture-menu-match-step38-playmode-rerun.xml` (`3/3` tests).
- Focused Menu bootstrap architecture contract passed: `/private/tmp/warlinecapture-menu-match-step38-menu-bootstrap-view.xml`.
- Runtime city Match scene smoke passed: `/private/tmp/warlinecapture-menu-match-step38-runtime-city-smoke.log`.
- FPS baseline was captured during Step 36 with footer Deploy path: `/private/tmp/warlinecapture-runtime-fps-probe.json`.
- Known validation noise: Unity batchmode still logs licensing/Xcode/usbmuxd messages and an Entities Graphics package `NullReferenceException` during object-root GC; the focused gameplay tests and smoke validations passed.

## Validation Gate

Run these before marking the roadmap complete:

- EditMode architecture tests covering bootstrap naming, scene ownership, and MonoBehaviour view limits.
- `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` against `Match.unity`.
- PlayMode smoke: `Menu.unity` boots, loads `Match.unity`, starts gameplay, exits, unloads `Match.unity`, returns to menu.
- Footer Deploy smoke: `Menu.unity` boots with the old `Match.unity` Canvas disabled, pressing `FooterContent/DeployCommandButton` loads Match UI/scene and sets gameplay `PlayRequested`.
- Repeated load/unload smoke for at least three cycles.
- FPS diagnostics baseline with one AI enemy faction enabled.

## Expected End State

- `Menu.unity` stays loaded for the app lifetime.
- `Match.unity` loads additively only when a match starts.
- `FooterContent/DeployCommandButton` replaces the old Match Canvas Play button and starts gameplay after Match UI/scene load completes.
- `Match.unity` unloads when returning to menu.
- `GameBootstrap` is retired, deleted, and guarded against restoration.
- Bootstrap logic is in systems; scene MonoBehaviours are views/reference holders only.
