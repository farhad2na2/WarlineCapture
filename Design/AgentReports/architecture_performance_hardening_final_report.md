# Architecture and Performance Hardening Final Report

## Closeout Status

The Architecture and Performance Hardening program is complete for WarlineCapture's early-development phase as of 2026-07-16.

- Checklist dispositions: `107 / 107`
- Accepted tasks: `92 / 107`
- Explicitly deferred pre-release certifications: `15 / 107`
- Active or unowned tasks: `0`
- Baseline revision: `b66453e979d847c3f05d61155266b166650b8df5`
- Closeout working baseline after disjoint operation-map integration: `1548ee09318305f885b90b9c9e122782f9e7c62f`

The deferred tasks are not passes or waivers. Their release-only obligations are preserved in `Design/Architecture/pre_release_performance_certification_backlog.md`.

## Rebaseline Decision

Repeated 10-minute thermal sessions, five-cold/five-warm launch qualification, same-artifact development/release certification, and the complete visual matrix are appropriate near beta or release-candidate stabilization. Running them repeatedly while maps, rendering, content, and UX are still changing creates obsolete evidence and slows higher-value architecture work.

The user therefore approved an early-development closeout focused on code quality, ownership, deterministic validation, short regression performance, GC discipline, and fail-closed release tooling. Long-form certification is deferred until its activation criteria are met.

## Ratings

| Area | Baseline | Closeout | Assessment |
|---|---:|---:|---|
| Overall architecture | `6.5` | `8.8` | Strong ECS ownership and guardrails; further oversized-owner decomposition remains maturity work |
| ECS and runtime ownership | not separately rated | `9.2` | ECS/Burst, update-owner, naming, and assembly gates are green |
| Modularity and dependency boundaries | not separately rated | `8.5` | Large responsibilities were decomposed and source-growth budgets now prevent silent regression |
| Lifecycle and resource safety | not separately rated | `8.6` | Menu/Match lifecycle, listener, teardown, and native-resource paths have deterministic coverage |
| Maintainability and testability | not separately rated | `8.7` | Compiler matrix, architecture suites, focused fixtures, and critical PlayMode flows are established |
| Performance engineering | `6.0` | `8.2` | Editor frame/GC and short Android diagnostics improved; sustained release certification remains open |
| Android production evidence | not separately rated | `7.0` | Strict build/device gates exist, but clean sustained acceptance is intentionally deferred |

The `8.8` architecture score is an early-development engineering rating, not a release-readiness claim. The project cannot claim fully certified Android performance until the pre-release backlog passes.

## Accepted Architecture Evidence

- Integrated architecture closeout: `23 / 23`.
- Source-growth and oversized-owner guard: `15 / 15`.
- ECS/Burst hot-path architecture: `10 / 10` with no snapshot debt.
- Assembly boundary validation: `31 / 31` in the recorded integrated slices.
- Broad naming and non-ECS ownership rules reject new `Manager`, `Controller`, `Provider`, broad `Player`, and similar prohibited ownership names.
- All 12 first-party assemblies built with zero errors at the accepted integrated closeout.
- Critical Menu -> Match -> Menu, selection/move/attack, placement/production, boarding/disembark, and resource-exchange PlayMode flows pass `5 / 5`.
- Static-map structural parity passes `2 / 2` without this closeout modifying operation-map-owned files.

## Accepted Performance Evidence

- Canonical Editor Match fixture: 733 units and 628 buildings over 557 measured frames.
- Frame time: `7.20 ms` average, `11.48 ms` p95, `13.16 ms` p99.
- Measured allocation in the frame benchmark: zero.
- Steady-state player-relevant GC: `292 / 1,024` bytes over 300 measured frames after 180 warmup frames.
- GPU Resident Drawer comparison improved stable device FPS from `32.6` to `36.3`, GPU time from `28.82` to `21.69 ms`, render thread from `5.24` to `3.07 ms`, and SetPass from `49.2` to `45.0` in the recorded short comparison.
- The clean timing-capable release APK is `380,801,943` bytes, `82,557,255` bytes below the accepted APK ceiling, with exact clean provenance and hash/report agreement.

## Deferred Certification Evidence

The last clean release diagnostic completed 600 seconds and collected 27,856 CPU timing samples and 27,785 GPU timing samples. It measured approximately `21.54 ms` average frame time, `32.79 ms` p95, `98.24 ms` p99, and `18,978.95 ms` process-to-Match-ready time. It is rejected because required release profiler counters were unavailable and the recorder emitted `complete=false`.

No further endurance rerun is required during early development. The diagnostic is retained only to inform the pre-release collector contract.

## Residual Risks

1. Sustained 30 FPS release acceptance and the separate 60 FPS high-end observation are not certified.
2. Installed size and absolute peak texture, mesh, audio, graphics-driver, and total memory budgets remain measurement-required.
3. The complete graphics tier, Day/Night, static-map, and mip-streaming visual matrix is not accepted. Known findings include dark 23:00 readability and black/missing surfaces in a near-map diagnostic capture.
4. Mip streaming remains fail-closed and must not expand until visual, memory, and I/O evidence passes.
5. Six generated animation textures retain a concrete CPU-copy/residency risk; precision and unload behavior are not yet proven safe to change.
6. Candidate-unused packages remain installed because no isolated removal slice has completed import, compile, test, build, and device validation.
7. Final current-map CPU/GPU mesh memory and normalized canonical/chunked/GRD comparisons remain owned by pre-release certification and the operation-map output available at that time.

## Conclusion

The hardening program materially improved WarlineCapture's architecture and engineering controls. Runtime ownership, ECS/Burst discipline, naming, assembly boundaries, source growth, lifecycle safety, deterministic tests, Editor frame performance, GC, and package governance are all substantially stronger than the audit baseline.

The correct current claim is: **architecture hardening complete for early development; Android release performance certification deferred**. Release readiness must remain false until `pre_release_performance_certification_backlog.md` is activated and accepted.
