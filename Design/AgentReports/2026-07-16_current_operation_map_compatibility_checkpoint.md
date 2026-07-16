# Current Operation-Map Compatibility Checkpoint

Date: 2026-07-16
Result: Passed
Checkpoint revision: `5c86a3ea2ed45f29d8f8c4bcbf4b7e056cf0b850`

## Accepted State

- `Assets/Game/Scenes/Match.unity` remains the complete, functional current
  match route. No map root or map-specific serialized reference has been
  removed from it.
- `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`
  remains a distinct-GUID staged map scene with no shell bootstrap/HUD policy.
- The canonical Match route still publishes exactly one active operation-map
  root and tears it down on return to Menu.
- The current schema-v1 static presentation product remains valid with 514
  chunks, 16,542 sources, content hash
  `9eebc7c8aa774d5f505cb684099d133a`, and dependency hash
  `c6334797e2ba64aabd6cf41377674c88`.
- Android build resolution accepts exactly the current compatibility
  manifest's owned chunks.

## Evidence

- Authored behavior and identity report:
  `2026-07-16_current_operation_map_authored_behavior_validation.md`.
- Menu -> Match -> Menu lifecycle: `1 / 1` passed in
  `/private/tmp/opmap-current-authored-lifecycle.xml`.
- Static structure/source hiding: `2 / 2` passed in
  `/private/tmp/opmap-current-authored-static-structural.xml`.
- Staged scene validation: `10 / 10` passed in
  `/private/tmp/opmap-current-authored-stager-isolated.xml`.
- Final Android resolver: `23 / 23` passed in
  `/private/tmp/opmap-current-authored-android-resolver-final.xml`.
- Refreshed Phase 0 ownership report remains `NeedsDecision` with its same four
  historical decisions; report SHA-256 is
  `18e2876ce420363be12672c79cbfdfde857b0b4c3a9cc75e7162bc6905291374`
  and shape/hash tests passed `26 / 26` in
  `/private/tmp/opmap-rollback-checkpoint-ownership-tests.xml`.

This checkpoint closes only the requirement to keep the original route
functional before cutover. It does not approve a staged-map presentation bake,
scene loading/unloading, Addressables, removal of Match map roots, or the atomic
shell cutover.

## Rollback Boundary

Revision `d5784dcfa` is the accepted pre-cutover checkpoint. The exact recovery
procedure is `Design/Architecture/operation_map_scene_split_rollback_recipe.md`.
Any later atomic cutover must preserve or be able to restore from this revision:

- canonical `Match.unity` and `MatchSubScene.unity` references;
- canonical building/vehicle placement configs and compatibility definition;
- static presentation manifest, integrity ledger, and 514 owned chunk scenes;
- current build settings and compatibility catalog binding.

The unrelated M01 runtime-generation prototype is outside this checkpoint.
