# Android GPU Instancing Comparison

Date: 2026-07-11

## Decision

Accept Unity GPU Resident Drawer `InstancedDrawing` as an intermediate Android optimization. It improves GPU and render-thread cost on the tested scene without visible map loss, but it does not satisfy the 60 FPS ship gate. Keep GPU occlusion and small-mesh culling disabled until separate visual acceptance.

## Test Configuration

- Device: Xiaomi `24090RA29G` (`malachite`), Mali-G615 MC2, Android 16 / API 36
- Resolution: 2712 x 1220 landscape
- Unity: 6000.5.2f1, IL2CPP ARM64 development profiler APK
- Quality: Mobile, 0.50 render scale, no MSAA, target 60 FPS
- Scene: deterministic auto-start Match at the same camera position
- Map presentation: 525 additive shared-mesh chunks; rebake reused all scenes with `scenesWritten=0`
- Instanced APK SHA-256: `e230e52a40a45bbbc2afd82a52f83227f5cf519ee562afbd93f1edcadf48e57d`

## Stable Comparison

| Metric | Instanced drawing | Previous settings control | Result |
|---|---:|---:|---|
| FPS | 36.3 | 32.6 | +11.3% |
| Average frame | 27.87 ms | 30.80 ms | -9.5% |
| GPU frame | 21.69 ms | 28.82 ms | -24.7% |
| CPU main | 21.35 ms | 22.70 ms | -5.9% |
| CPU render | 3.07 ms | 5.24 ms | -41.4% |
| Draw calls | 75.0 | 75.2 | neutral |
| SetPass | 45.0 | 49.2 | -8.4% |
| Triangles | 1.046 M | 1.046 M | neutral |
| Total PSS | 2,630 MB | 2,555 MB | +75 MB |
| Graphics PSS | 1,134 MB | 1,089 MB | +45 MB |

The final instanced confirmation run averaged 37.4 FPS and 20.5 ms GPU across its last 15 diagnostic samples. Thermal status remained `0`; the control ran later on a warmer device, so the comparison supports direction but is not a cold-device final benchmark.

## Visual And Runtime Result

- Instanced captures are nonblank and retain terrain, buildings, props, units, HUD textures, command controls, and minimap.
- The control capture showed black/missing HUD texture regions; this is an additional reason not to restore Android static batching.
- Runtime marker: `GPU Resident Drawer created.`
- No shader errors, BRG/DOTS instancing errors, fatal exceptions, `SIGSEGV`, or missing-reference exceptions.
- Unity automatically retains incompatible renderers on the ordinary URP path; no custom fallback system was added.

## Configuration Accepted

- `Mobile_RPAsset`: GPU Resident Drawer mode `InstancedDrawing`
- GPU occlusion: disabled
- Small-mesh screen percentage: `0`
- Android static batching: disabled
- Dynamic batching: disabled
- BRG and standard instancing shader variants: Keep All
- Mobile renderer: Forward+

## Remaining Gates

- 60 FPS is still red; current Match remains GPU-bound.
- Development-build PSS is too high for product acceptance and requires release-device measurement.
- Full startup, package, memory, screenshot, and extended soak comparison remains open.
- Do not enable GPU occlusion, small-mesh culling, or remove fallback renderers without new visual evidence.

## Local Evidence

- `/private/tmp/warline-gpu-instancing-profiler-build-r2.log`
- `/private/tmp/warline-gpu-instancing-device.log`
- `/private/tmp/warline-gpu-instancing-final-device.log`
- `/private/tmp/warline-gpu-control-device.log`
- `/private/tmp/warline-gpu-instancing-final-match.png`
- `/private/tmp/warline-gpu-control-match.png`
