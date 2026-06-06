# Match HUD Minimap Migration Plan

## Summary

- Move minimap behavior into the new Match HUD at `FooterContent/MinimapPanel/Map`.
- Keep runtime UI SOLID-compliant: serialized `*View` references, no shipped hierarchy searches, no gameplay policy in UI views, and no direct camera mutation from UI.
- Render the static map background through a cached capture path only on match start or static minimap invalidation.
- Update camera viewport and input as lightweight runtime overlay behavior.

## Key Changes

- Add `MatchHudMinimapView` to the Match HUD minimap panel with explicit references to the map image, viewport rect, zoom buttons, and optional marker root.
- Add standalone minimap projection utilities for world-to-map, map-to-world, and camera viewport rect projection.
- Bind the loaded Match HUD minimap through the same shell runtime dependency path used by Match HUD command controls.
- Route minimap interactions through existing gameplay systems:
  - Zoom buttons set `RuntimeGameplayStateSystem.ZoomInHeld` and `ZoomOutHeld`.
  - Map click and viewport drag convert map coordinates to world space and call `SelectionUiCameraSystem.MoveCameraGroundCenterTo`.
  - UI suppresses the next world click when it consumes minimap input.
- Build the static background by rendering the gameplay area from a hidden orthographic capture camera into a cached texture, then assigning a generated sprite to the existing `Map` image.

## Implementation Steps

1. Create this architecture document.
2. Add `MatchHudMinimapView`, `MatchHudMinimapInputSystem`, and `MatchHudMinimapProjectionSystem`.
3. Extend shell/gameplay binding so `MainMenuPlayUI` can bind loaded minimap views.
4. Wire the checked-in Match HUD prefab and the prefab builder with serialized minimap references.
5. Add editor and projection tests for prefab wiring and coordinate math.
6. Run targeted EditMode tests and Unity validation.

## Test Plan

- Editor tests verify `FooterContent/MinimapPanel/Map`, `Viewport`, `ZoomIn`, and `ZoomOut` exist and are serialized on `MatchHudMinimapView`.
- Projection tests cover world-to-normalized, normalized-to-world, viewport rect sizing, clamping, and invalid grid/camera cases.
- Runtime binding tests verify minimap views bind through shell content without cross-region references.
- Smoke validation verifies the map image receives a generated background and viewport overlay updates without rebuilding the background every frame.

## Assumptions

- V1 zoom buttons control gameplay camera zoom, matching existing HUD semantics.
- Dragging the viewport rect recenters the gameplay camera; tapping the map outside the rect also recenters.
- Minimap-only pan/zoom is out of scope for this first pass.
- The `Map` GameObject path remains stable and keeps the existing `Image` component for this pass; the generated background is assigned as a cached sprite.
