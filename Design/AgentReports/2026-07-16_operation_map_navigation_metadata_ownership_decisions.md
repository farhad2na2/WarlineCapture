# Operation Map Navigation Metadata Ownership Decisions

Date: 2026-07-16
Status: Accepted shared-foundation decisions
Evidence: `2026-07-15_opmap-008_phase0_navigation_metadata_ownership.json`
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Decision Rule

- The active operation map owns immutable spatial truth: grid dimensions/origin/cell size, surface/height data, authored static blocker inputs, and authored runway/helipad anchors.
- Runtime ECS systems own mutable simulation state derived from that truth: occupancy, blocker counts, faction passability, and changed bounds.
- Shared building definitions own prefab-local runway/helipad metadata for player-built or scenario-instantiated facilities.
- Runtime systems resolve either map-authored or prefab-local metadata into the same bounded ECS contract. Air movement policy consumes that contract but does not own map metadata.

This decision does not choose a map loader or generator.

## Resolved Evidence Rows

| Evidence authority | Accepted classification | Resolution | Required migration disposition |
|---|---|---|---|
| `Game.Components.DynamicBlockerComponent` | `Mixed` | Map metadata supplies grid identity and authored blocker baseline; runtime ECS owns mutable counts, blocked state, and faction pass rules. | Initialize against active-map metadata and clear/rebuild on map teardown or replacement. Air-unit passability remains explicit movement policy, not metadata ownership. |
| `Game.Components.DynamicOccupancyComponent` | `Mixed` | Map metadata supplies the grid domain; runtime ECS owns mutable unit occupancy derived from `UnitGrid` and footprints. | Recreate empty occupancy for each active map and never persist entity occupancy in map assets. |
| `Game.Components.StaticGridBlocker` | `Mixed` | Map owns authored static blocker identity/bounds; runtime ECS owns change detection and projected blocked cells. | Bind authored blockers through typed map metadata, then rebuild runtime cells when blocker state or bounds change. |
| `Game.Runtime.BuildingRunwaySystem` runway metadata | `Mixed` | Authored map airports use map-owned typed runway anchors. Player-built/scenario-instantiated airports use `SharedConfig` prefab-local runway start/end metadata. `BuildingRunwaySystem` remains shell/runtime resolution. | Publish both sources into one runway ECS contract with stable ids and source kind; reject missing/duplicate endpoints instead of hierarchy-name fallback. |

## Teardown And Performance Contract

1. Immutable map metadata is resolved once per active map and represented by bounded ECS data/blob references.
2. Mutable occupancy/blocker containers are allocated once, updated incrementally, and disposed or cleared exactly once during map teardown.
3. No per-frame scene search, asset lookup, hierarchy-name scan, managed allocation, or full-grid rebuild is introduced.
4. Runway and helipad lookup uses stable ids and cached ECS data. Aircraft systems do not query scene transforms in hot paths.

## Acceptance Consequences

The historical probe remains `NeedsDecision`; this document supplies all four decisions. No ownership row remains unresolved for ground-height, map-surface, grid, blocker, terrain, runway, or helipad metadata.

This closes ownership only. Active-map binding, typed runway/helipad publication, teardown, and incremental rebuild behavior remain implementation tasks under shared Phases 4-6.
