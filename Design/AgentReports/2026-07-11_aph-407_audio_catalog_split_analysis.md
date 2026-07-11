# APH-407 Persistent Audio Catalog Split Analysis

## Recommendation

**DECLINE opening a catalog-split implementation now.** APH-405 accepted the Android pilot and APH-406 promoted the policy to all Voice clips. Re-evaluate only if full-policy Android residency still misses the accepted memory target.

## Evidence Boundary

- APH-401 capture revision: `8e1f21c2a4326ff08371d621e808f38f79b2b197`; captures are marked dirty and predate the APH-404 pilot.
- APH-400 inventory revision: `7084805d771142706f340e9f2e52a68570bcb72b`; it supplies clip duration/import/size classifications, not post-pilot Android residency.
- Runtime ownership was inspected at the current source revision and is hash-recorded in the companion JSON.
- All 163 current Voice importer metas were inspected and match the accepted on-demand compressed policy; the original eight remain frozen as the APH-405 evidence set.
- Measured residency values are Unity Editor runtime clip memory, not Android release-device memory.

## Current Ownership

- One 234-event serialized catalog exists.
- The persistent Menu scene owns the only `AudioPlaybackPresentationRuntimeView` and its catalog reference.
- Match loads additively beneath Menu; Match has no independent audio catalog owner.
- The runtime view and bridge each accept/cache one catalog.

## Quantified Residency

Before controlled Menu playback, 225 of 234 clips were loaded and catalog clips occupied **43.34 MiB**.

| Proposed catalog | Clips | Compressed inventory | Estimated decoded inventory | Measured Menu baseline runtime | Loaded clips |
|---|---:|---:|---:|---:|---:|
| Core/Menu | 23 | 1.30 MiB | 7.13 MiB | 0.81 MiB | 18 |
| Match | 48 | 3.87 MiB | 11.32 MiB | 2.69 MiB | 44 |
| Voice | 163 | 44.24 MiB | 79.31 MiB | 39.84 MiB | 163 |

If only Core/Menu clip references were resident in Menu, the measured classification upper bound is **0.81 MiB**, avoiding **42.53 MiB (98.12%)**. This is an upper bound, not a proven implementation result: it excludes catalog/dictionary overhead and does not prove Unity unloads split dependencies.

Voice accounts for 163 clips and the dominant measured baseline. The eight pilot clips represent 2.01 MiB compressed inventory versus 3.56 MiB estimated decoded PCM. APH-405 recorded passing first-play, repeated-play, glitch-counter, and post-load residency evidence for that set; full-policy Android residency remains the next measurement.

## Dependency And Lifecycle Risks

- The persistent Menu scene owns the only runtime view while Match loads additively; scene-owned Match catalogs require explicit registration and teardown.
- The bridge caches exactly one catalog and one hash map; multiple catalogs need atomic precedence, duplicate-ID, and hash-collision contracts.
- Voice crosses Menu and Match features, so a Voice catalog has no natural single-scene lifetime.
- A serialized split alone does not prove AudioClip payload unload; active sources and Unity asset dependencies can retain clips.
- On-demand catalog loading can turn accepted ECS requests into missing-event races or first-play stalls.
- Catalog builders, parity checks, residency capture, and validation currently assume one 234-event catalog.

## Decision Gate

Do not open implementation from this analysis. Reopen APH-407 only if full-policy Android residency shows that the accepted Voice importer policy still misses the same-device memory target. A reopened slice must first specify catalog acquisition, request queuing while loading, duplicate-event precedence, Match teardown, Voice cross-scene ownership, active-source completion, unload proof, and Android first-play/audible regression gates.

A split should be declined permanently if full Voice importer rollout meets the memory and latency targets, because Match-only non-Voice clips account for only a small persistent baseline relative to Voice and the split would add runtime ownership complexity for marginal incremental gain.

## Reproduction

```sh
python3 Tools/CI/aph407_audio_catalog_split_analysis.py --check
python3 -m unittest Tools.CI.tests.test_aph407_audio_catalog_split_analysis
```
