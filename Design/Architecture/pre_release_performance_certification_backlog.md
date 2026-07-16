# Pre-Release Performance Certification Backlog

## Purpose

Preserve the release-only evidence obligations deferred from the Architecture and Performance Hardening program on 2026-07-16. These items are not passed, waived, or deleted. They are intentionally inactive while WarlineCapture is in early development and its maps, content, rendering, and UX continue to change.

The completed early-development tracker remains `architecture_performance_hardening_implementation_tracker.md`. This backlog becomes the source of truth only after activation.

## Activation Criteria

Activate this backlog when all of the following are true:

1. A beta or release-candidate milestone has been declared.
2. The target Android device tiers and 30/60 FPS product targets are frozen.
3. The operation map, core HUD, graphics tiers, Day/Night presentation, and content set used for certification are stable enough that captures will not immediately become obsolete.
4. A clean ARM64 IL2CPP development artifact and release artifact can be built from one exact revision.
5. The team can reserve the reference device for uninterrupted, unplugged collection.

Do not reactivate this backlog merely because an engineering task needs a quick smoke test. Short compiler, architecture, PlayMode, GC, and performance regressions remain part of normal development CI.

## Deferred Scope

| Source task | Pre-release obligation | Existing deliverable retained |
|---|---|---|
| `APH-311` | Separate 10-minute Android 30 FPS and 60 FPS sessions | tier configuration and historical device baselines |
| `APH-501` | Set installed-size and absolute runtime memory/category budgets from accepted device evidence | APK/AAB ceilings and fail-closed budget schema |
| `APH-502` | Accept final included/excluded texture categories from a complete same-revision BuildReport/residency inventory | deterministic classifier and provisional 3,464-importer inventory |
| `APH-504` | Authorize or reject the representative mip-streaming pilot using measured quality and memory evidence | fail-closed pilot selector; no importer mutation authorized |
| `APH-505` | Capture and review identical near/medium/far streaming comparisons | capture contract |
| `APH-506` | Run the 10-minute camera pan/zoom memory and I/O collection | strict device collector and focused contracts |
| `APH-508` | Measure generated animation texture residency, CPU-copy retention, and unload behavior | six-texture static audit and risk report |
| `APH-509` | Prove any package removal in an isolated import/compile/test/build/device slice | deterministic package-usage inventory; no package removed |
| `APH-510` | Produce accepted same-revision package, residency, frame, startup, and I/O category deltas | comparison tool and package-size improvement evidence |
| `APH-601` | Record exact accepted-map CPU/GPU mesh memory and peak startup allocation | startup instrumentation and structural measurements |
| `APH-609` | Complete normalized canonical/chunked/GRD metrics and visual review | current-map evidence matrix and accepted short device comparison |
| `APH-803` | Qualify a clean Android development artifact with launch, sustained, memory, and thermal evidence | fail-closed gate, recorder, collector, schemas, and tests |
| `APH-804` | Qualify a clean Android release artifact for the 30 FPS tier | package-bound release gate, timing-capable artifact contract, collector, and tests |
| `APH-809` | Fill and approve the graphics tier, Day/Night, map, and streaming visual matrix | 26-slot fail-closed visual matrix tooling |
| `APH-902` | Publish final same-device development/release reports including thermal sessions | report schemas and prior diagnostic evidence |

## Execution Order

1. Freeze one exact certification revision and regenerate current BuildReports and residency inventories.
2. Resolve known visual blockers: 23:00 readability and near-map black/missing surfaces.
3. Complete streaming and map visual comparisons before endurance collection.
4. Build clean development and release artifacts from the same revision.
5. Run development qualification, then release 30 FPS qualification, then the separate 60 FPS observation.
6. Run category residency, camera pan/zoom, startup, and installed-size measurements.
7. Set or ratchet absolute budgets only from accepted evidence.
8. Publish the same-device certification report and update production-readiness status.

## Non-Negotiable Gates

- Never treat a diagnostic, dirty, thermally throttled, foreground-lost, timingless, or visually incomplete run as accepted.
- Never raise a package, frame, GC, memory, or visual budget merely to make a candidate pass.
- Preserve exact revision, artifact hash, build profile, device identity, thermal, foreground, crash, and screenshot evidence.
- Keep 30 FPS release acceptance separate from the 60 FPS high-end target.
- Do not change operation-map content from this backlog; consume the operation-map owner's accepted output.

## Current Status

`Inactive - deferred during early development by explicit user approval on 2026-07-16.`
