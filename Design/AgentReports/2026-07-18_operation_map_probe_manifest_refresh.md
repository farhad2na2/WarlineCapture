# Operation Map Probe And Manifest Refresh

Date: 2026-07-18
Status: Passed

## Scope

- Rebound the Phase 0 baseline probe to the thin Match shell, shared runtime subscene, and extracted current operation-map scene.
- Rebound camera/minimap ownership evidence to the extracted map-owned grid source.
- Refreshed the current operation-map static-presentation manifest after the canonical scene dependency hash changed.
- Preserved the existing 514 presentation scenes without rewriting chunk content.

## Validation

- Static presentation bake: passed; 16,542 sources, 514 chunks, 1 reused scene, 0 scene writes, 0 stale deletions.
- Camera/minimap ownership probe tests: 33/33 passed.
- Baseline probe tests: 8/8 passed.
- Complete `OperationMap` suite before the scene-setup assertion correction: 382/383 passed; the only failure was the corrected semantic scene-restoration assertion.
- `Game.Editor.csproj`: 0 errors.
- `Game.Tests.Editor.csproj`: 0 errors.
- `git diff --check`: passed.

## Build Integrity

- Manifest canonical scene dependency hash changed from `ee1807c0d119e18c4bbd6f21f4092254` to `0bada26f767a6251f00d5e5ff34d19c9`.
- Manifest content hash remained `667418e5a560e053b1487960764ccdd1`.
- No map scene, chunk scene, placement config, or user-authored font asset was modified by this slice.
