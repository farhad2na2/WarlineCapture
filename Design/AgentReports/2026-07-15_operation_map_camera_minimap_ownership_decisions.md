# Operation Map Camera And Minimap Ownership Decisions

Date: 2026-07-15
Status: Accepted shared-foundation decisions
Evidence: `2026-07-15_opmap-007_phase0_camera_minimap_ownership.json`
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Decision Rule

- The scenario owns semantic intent: initial focus purpose, objective id, recommendation meaning, and requested anchor id.
- The active operation map owns spatial truth: bounds, grid, projection extent, and typed anchor transforms.
- The shell owns policy and presentation: camera instance, movement/follow behavior, clamping, minimap projection, request transport, assistant scoring, and UI read models.
- Direct Unity object references that let shared config replace the shell camera are temporary compatibility, not map ownership.

This decision is loader- and generator-neutral.

## Resolved Evidence Rows

| Evidence subject | Accepted classification | Resolution | Required migration disposition |
|---|---|---|---|
| Initial focus producer | `Mixed` | Scenario selects a typed initial-focus anchor; the map resolves its transform; shell camera request logic applies it. | Replace direct scenario/grid world-position publication with typed anchor intent and active-map resolution. |
| Tactical-follow clamp policy | `Mixed` | Shell owns follow pose policy; map metadata owns legal camera/clearance bounds. | Validate/clamp follow poses against active-map metadata without changing tactical-follow ownership. |
| Camera boundary projection | `Mixed` | Map owns canonical bounds; shell projects them into mutable camera state. | Publish immutable active-map bounds and retarget the existing shell camera state only when map metadata changes. |
| Camera source and override resolution | `TemporaryCompatibility` | The active world camera is shell-owned. `RTSSelectionSystemConfig.WorldCamera` is a direct Unity-object override that must not become map data. | Preserve fallback now; remove the override after all call sites consume the composition-provided shell camera. |
| Expanded full-map bounds | `Mixed` | Map owns canonical minimap extent; shell owns viewport projection. The viewport must remain inside map bounds. | Clamp the camera footprint to canonical map metadata; never enlarge the full-map world extent to display out-of-bounds camera space. |
| Objective-to-assistant projection | `Mixed` | Mission runtime owns objective-state publication; scenario owns objective semantics; map owns an optional typed focus anchor; shell owns the ARIA read model. | Add/retain a mission-runtime objective writer independently of map loading, then resolve optional focus anchors through active-map metadata. |
| Objective recommendation projection | `Mixed` | Shell owns recommendation scoring/presentation; scenario defines the action meaning; map owns any spatial target anchor. | Keep recommendation logic map-neutral and carry a typed anchor id when `Show Me` or camera focus is available. |

## Acceptance Consequences

The original deterministic evidence correctly remains `NeedsDecision`; this document supplies those decisions without rewriting historical evidence. There are now no unresolved ownership decisions for the Phase 0 camera/minimap inventory.

This closes ownership only. It does not claim that typed anchors, active-map binding, objective writers, follow clamping, or minimap clamping are already implemented. Those changes remain tracked under the shared Phase 1, Phase 4, Phase 5-contract, and Phase 6 tasks and require focused behavior validation.
