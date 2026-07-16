# Static Map Schema-v1 Read Compatibility

Date: 2026-07-16
Status: Accepted current-map compatibility contract

## Implemented

- Added `StaticMapPresentationManifest.MinimumReadableSchemaVersion = 1` and
  an allocation-free `IsSchemaReadable` range check.
- Updated the runtime chunk index, canonical-renderer ownership resolver,
  editor scene wiring, and Android build-scene resolver to accept every schema
  in the explicit readable range `1..CurrentSchemaVersion`.
- Kept baker/output-ownership migration checks strict. This slice does not
  advance the schema or reinterpret generated content.

## Current Compatibility Evidence

- Current schema remains `1`.
- Canonical manifest remains schema `1`, with `514` chunks.
- The real manifest builds a `514`-entry runtime chunk index at chunk size `32`.
- Manifest SHA-256 remained
  `3940dcac3d42c703f47cf11f134b183c4554f9944629925f7b38957e08d93746`.
- No manifest, integrity ledger, generated scene, generated `.meta`, source
  scene, subscene, config asset, prefab, or build-setting file changed.

## Validation

- Focused manifest/index/ownership/wiring/Android-resolver EditMode tests:
  `37 / 37` passed, `/private/tmp/opmap-schema1-final-tests.xml`.
- Source-growth and non-ECS naming gates: `24 / 24` passed,
  `/private/tmp/opmap-schema1-architecture.xml`.
- Unity compilation: zero compiler errors.
- `git diff --check`: passed.

This preserves current static-presentation readability without selecting a map
loader, adding Addressables, implementing remote content, or starting a future
schema migration.
