# Android Map Presentation Ownership

Date: 2026-07-11

## Decision

Accept validated Android presentation ownership as the runtime map-rendering path. Keep the legacy runtime combiner as a fail-closed fallback for a missing, malformed, or stale manifest. Do not close the overall map task until mesh Read/Write policy and the required 10-minute visual soak are complete.

## Implementation

- Android validates the presentation manifest before mutating canonical renderers.
- A valid manifest disables all 17,564 manifest-owned canonical `MeshRenderer` components and skips runtime mesh combination.
- Player builds first resolve the editor-baked sibling-index path, then use unique mesh/material/world-bounds identity when Unity player stripping changes sibling indices.
- A renderer can be claimed only once. Validation is transactional and restores original enabled states during Match teardown.
- Invalid source ranges, null sources/materials, missing assets, duplicate identities, and canonical mismatches select the legacy fallback without partial suppression.
- Colliders, scripts, authoring objects, `MeshFilter` components, and overlay source geometry remain in the canonical Match scene.

## Validation

- Focused ownership tests: 9/9 passed.
- Final integrated ownership/texture-guard/Android/architecture/source-growth matrix: 83/83 passed.
- Final static-map bake: 17,564 sources, 525 chunks, identical content hash `393591d2855b764bce260888e6f5fa20`, zero scene writes, and zero stale-scene deletions.
- Final profiler APK: 560 MB, SHA-256 `9c3ca24e837a3550fef8fc59e0ab5d3b45d413d6ed7517fefceb2f6e8c5aebde`.
- Device: Xiaomi `24090RA29G`, Mali-G615 MC2, Android 16 / API 36, 2712 x 1220, Mobile quality, 0.50 render scale, target 60 FPS.
- Runtime marker: `[StaticMapPresentationOwnership] result=Presentation suppressed=17564`.
- Legacy `[StaticMapBatching] result=Applied` marker is absent on the accepted run.
- Screenshot retains roads, terrain, walls, buildings, props, vehicles, units, HUD, minimap, and command controls.
- No fatal signal, shader error, BRG error, or missing-reference error was found.

## Device Comparison

| Metric | Warm legacy-fallback run | Ownership run | Result |
|---|---:|---:|---|
| Settled FPS | 34.6 | 43.4 | +25.3% |
| Submitted triangles | 1.047 M | 0.813 M | -22.4% |
| Submitted vertices | 1.96 M | 1.52 M | -22.6% |
| GPU frame | 27.6 ms | 22.2 ms | -19.8% |
| Render thread | 4.2 ms | 3.2 ms | -24.8% |
| Draw calls | 75 | 74.2 | neutral |
| SetPass | 45 | 41.2 | -8.4% |
| Runtime mesh-combine startup | 351-361 ms | skipped | removed |

The ownership run contains 25 diagnostic samples; the last ten are compared with the last ten samples from the warm fallback run. Thermal ordering was not randomized, so the geometry, ownership marker, startup-path removal, and visual evidence are stronger than small frame-time differences. Total PSS was 2,495 MB with 1,101 MB graphics PSS; this is 135 MB and 33 MB below the earlier accepted instanced development-build snapshot, but the captures occurred at different process ages and are not treated as an acceptance delta.

## Remaining Gates

- Disable Read/Write only for meshes proven unused by every accepted runtime path.
- Run the full 10-minute Android visual soak across top-down, oblique, low-ground, and gameplay camera views.
- Capture cold release startup, release memory, installed size, and thermal evidence.
- The 60 FPS target remains red on this reference device.

## Local Evidence

- `/private/tmp/warline-map-ownership-final-device.log`
- `/private/tmp/warline-map-ownership-final-match.png`
- `/private/tmp/warline-map-ownership-final-meminfo.txt`
- `/private/tmp/warline-map-ownership-final-build-r3.log`
- `/private/tmp/warline-map-ownership-focused-r5b.xml`
- `/private/tmp/warline-map-ownership-integrated.xml`
