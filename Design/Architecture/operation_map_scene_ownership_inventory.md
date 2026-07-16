# Operation Map Scene Ownership Inventory

Date: 2026-07-16
Status: Accepted shared-foundation inventory

## Purpose

This document finalizes the Phase 0 evidence into the ownership contract used by
the non-destructive `Match.unity` split. It does not move, duplicate, delete, or
rebind any Unity object or asset.

## Accepted Evidence

- `../AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.md`
- `../AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.md`
- `../AgentReports/2026-07-15_opmap-007_phase0_camera_minimap_ownership.md`
- `../AgentReports/2026-07-15_opmap-008_phase0_navigation_metadata_ownership.md`
- `../AgentReports/2026-07-15_operation_map_static_presentation_ownership.md`
- `../AgentReports/2026-07-15_operation_map_camera_minimap_ownership_decisions.md`
- `../AgentReports/2026-07-16_operation_map_navigation_metadata_ownership_decisions.md`
- `../AgentReports/2026-07-16_operation_map_shell_root_ownership_decisions.md`
- `../AgentReports/2026-07-16_operation_map_placement_ownership_decisions.md`

The historical probe reports remain immutable `NeedsDecision` evidence. The
accepted decision records above resolve every reported `Mixed` or `Unresolved`
row and supply the migration disposition.

## Split Rules

| Classification | Split disposition |
|---|---|
| `ShellOwned` | Remains in `Match.unity` or reusable shell/runtime code. Never copied into the operation-map scene. |
| `MapOwned` | Eligible for the staged operation-map scene or map registration. Preserve object identity, asset GUIDs, and parity evidence. |
| `SharedConfig` | Remains a shared referenced asset/type. Never duplicated merely to place it under a map folder. |
| `TemporaryCompatibility` | Remains functional until its typed replacement passes parity and the atomic cutover occurs. |
| Resolved `Mixed` | Split by the accepted decision row. Immutable authored map data moves to typed map metadata; mutable runtime/scenario/shell state remains with its existing owner. |

## Final Ownership Boundaries

- Shell: runtime composition, world camera and camera policy, HUD/UI, bounded
  static-presentation indexing/streaming, runtime occupancy, dynamic state, and
  gameplay request processing.
- Map: geometry and authored map roots, canonical bounds, minimap extent,
  surface/grid identity, static blockers, map-authored runway/helipad anchors,
  static-presentation product instances, and current placement configs.
- Shared config: reusable policy/config types and assets, including day/night
  policy and prefab-local runway/helipad metadata for built content.
- Scenario: starting-unit definitions, objective semantics, restrictions, and
  the ids of typed map anchors needed by a scenario.
- Compatibility: direct `MatchSceneView` map references, legacy Start/End extent
  transforms, current initial-unit authoring, hardcoded bake/wiring/build entry
  points, and direct current-manifest binding until parity-backed cutover.

## Placement And Presentation Invariants

- Preserve all 451 building and 29 vehicle placement identities.
- Duplicate hierarchy paths are compatibility evidence, not unique identity;
  migration uses the complete accepted placement identity tuple.
- Preserve the current manifest, integrity ledger, generated scenes, and source
  renderer suppression as one atomic map-owned product set.
- Shared source art and binary assets remain referenced; they are not copied.

## Cutover Gate

No source root or compatibility reference may be removed until the staged map
passes scene, placement, authored-conversion, camera, minimap, surface, blocker,
runway/helipad, static-presentation, Android build, and rollback parity. The
original `Match.unity` remains functional until that atomic cutover.
