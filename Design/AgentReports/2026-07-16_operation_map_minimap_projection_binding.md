# Operation Map Minimap Projection Binding

Date: 2026-07-16

## Scope

The existing `MatchHudMinimapDataSourceAdapter` now prefers the minimap origin
and exact float projection extents from the single active operation-map
metadata blob. The projection is accepted only when the active-map and
metadata generations match and the blob data is finite and positive.

The existing `MatchHudMinimapInputUiSystemHelper` already caches its resolved
grid and captured raster. Consequently, compact and full-map raster capture,
markers, viewport projection, and focus conversion reuse the active-map
extents without a new update loop, per-frame entity query, managed allocation,
or raster rebuild policy.

Missing, stale, invalid, or rotated active-map projection metadata retains the
current `GridConfig` path. Rotation remains an explicit later extension because
the current raster feature-area filtering is axis aligned; silently applying
only part of a rotated projection would be incorrect. Both registered current
map definitions use the accepted zero-degree `2048 x 1024` projection.

## Validation

- Focused minimap adapter and projection EditMode tests: `22 / 22` passed.
  - Results: `/private/tmp/opmap-minimap-focused.xml`
  - Log: `/private/tmp/opmap-minimap-focused.log`
- Final adapter uniqueness/negative/fallback rerun: `5 / 5` passed.
  - Results: `/private/tmp/opmap-minimap-adapter-unique.xml`
  - Log: `/private/tmp/opmap-minimap-adapter-unique.log`
- Combined minimap and strict source-growth rerun: `37 / 37` passed.
  - Results: `/private/tmp/opmap-minimap-final.xml`
  - Log: `/private/tmp/opmap-minimap-final.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-minimap-architecture.xml`
  - Log: `/private/tmp/opmap-minimap-architecture.log`
- Active-map projection precedence, exact fractional extents, unique-root
  enforcement, rotated/stale fallback, and missing-active-map compatibility
  fallback are covered.
- Unity compilation completed with zero compiler errors.
- `git diff --check` passed.
