# Operation Map Editor Debug Overlays

Date: 2026-07-16

## Scope

Added a selection-scoped custom editor for `OperationMapSceneView`. In Scene
view it renders the operation-map definition's exact world, playable, and
camera bounds; typed camera positions/headings; typed anchors; distinctly
colored lane, runway, and helipad anchors; and the oriented minimap extent.

The inspector also exposes the existing bounded map-surface preview path for
surface heights and blocked cells. This composes the accepted surface/blocker
debug implementation instead of rescanning map geometry or manufacturing
spatial blocker positions from metadata counts.

The overlay is editor-only and active only while its view is selected. It adds
no runtime component, ECS system, MonoBehaviour update loop, loading behavior,
managed gameplay allocation, or player build content.

## Validation

- Focused geometry/color EditMode tests: `4 / 4` passed.
  - Results: `/private/tmp/opmap-scene-overlay-focused.xml`
  - Log: `/private/tmp/opmap-scene-overlay-focused.log`
- Source-growth and non-ECS architecture/naming gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-scene-overlay-architecture.xml`
  - Log: `/private/tmp/opmap-scene-overlay-architecture.log`
- Invalid bounds, minimap extents, orientation, and caller buffer capacity fail
  closed in focused coverage.
- Unity compiled the editor and test assemblies with zero C# compiler errors.
- `git diff --check` passed.
