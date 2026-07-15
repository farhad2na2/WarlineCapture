# Operation Map Shell And Scene-Root Ownership Decisions

Date: 2026-07-16
Status: Accepted shared-foundation decisions
Evidence: `2026-07-15_opmap-004_phase0_ownership_baseline.json`
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Decision Rule

- The Match shell owns bootstrap, cameras, HUD/runtime composition, lifecycle, and reusable presentation policy.
- The operation map owns physical world roots, authored spatial metadata, map surface, map lighting/probe content, and map-specific subscene content.
- Shared configuration assets define reusable policy; operation-map/scenario metadata selects them without taking ownership of shell systems.
- Scenario data owns starting force composition and semantic spawn intent; the map owns typed spatial spawn anchors.
- Unreferenced legacy transforms move with the map until typed metadata proves them redundant. They never become shell dependencies.

This decision is independent of loader and generator technology.

## Resolved Evidence Rows

| Evidence identity | Accepted classification | Resolution | Required migration disposition |
|---|---|---|---|
| `Game.Composition.MatchSceneView::dayNightConfig` | `SharedConfig` | The asset defines reusable day/night policy. Shell presentation applies it; map/scenario metadata may select an environment profile. | Keep the config outside physical map ownership and replace direct scene serialization only when loader-neutral active-map metadata can select it. |
| `Assets/Game/Scenes/Match.unity::Start[2]` | `TemporaryCompatibility` | Bare unreferenced transform at map-space `(0,0,1024)` is a map-scoped legacy extent marker, not shell or scenario state. | Move with the staged map, preserve through parity, then remove only after typed bounds/anchor validation proves no consumer requires it. |
| `Assets/Game/Scenes/Match.unity::End[3]` | `TemporaryCompatibility` | Bare unreferenced transform at map-space `(2048,0,0)` is a map-scoped legacy extent marker, not shell or scenario state. | Move with the staged map, preserve through parity, then remove only after typed bounds/anchor validation proves no consumer requires it. |
| `Assets/Game/Scenes/Match/MatchSubScene.unity::InitialUnitsSpawnerAuthoring[1]` | `Mixed` | Scenario owns starting-unit definitions; map owns typed spawn anchors; runtime ECS owns deterministic instantiation. | Preserve current authoring for compatibility, then migrate composition to scenario ids plus map anchors without embedding unit rosters in map assets. |

## Atomic Split Consequences

1. Only rows already classified `MapOwned` plus the two legacy map markers may move into the staged operation map before cutover.
2. Shell-owned roots and `MatchSceneView` runtime/composition references remain in `Match.unity`.
3. Shared config assets remain referenced in place and are not duplicated into the map folder.
4. Mixed scenario/map authoring remains compatibility data until typed spawn contracts and parity tests exist.
5. No source root is deleted from the current Match path before staged map, rollback, camera, minimap, surface, placement, and presentation parity pass.

## Acceptance Consequences

The historical deterministic report remains `NeedsDecision`; this record supplies all four decisions. All `28` `MatchSceneView` fields, `16` Match roots, and `3` MatchSubScene roots now have accepted ownership/disposition.

This closes inventory only. It does not authorize moving roots or stripping `Match.unity`; that remains Phase 4 work behind the atomic cutover gate.
