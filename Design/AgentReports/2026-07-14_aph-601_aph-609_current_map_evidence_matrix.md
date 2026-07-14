# APH-601 / APH-609 Audited Map-Artifact Evidence Matrix

Date: 2026-07-14  
Scope: read-only evidence reconciliation for `APH-601` and `APH-609`  
Result: **not acceptance-ready**

## Audited Baseline And Release Acceptance Key

| Key | Audited baseline / required acceptance value |
|---|---|
| Audited map-artifact baseline revision | `d5b2ddeb8166010bbe5337e00243e74015ee4e94` |
| Release acceptance revision | Eventual exact clean release revision; must be a descendant of the audited baseline and reproduce the dependency hash, content hash, and `514/16,542` counts below |
| Canonical scene | `Assets/Game/Scenes/Match.unity` |
| Canonical dependency hash | `0a587783351110d16353575d15d1b5cd` |
| Presentation content hash | `9eebc7c8aa774d5f505cb684099d133a` |
| Presentation layout | `514` chunks, `16,542` sources, `32 m` chunks |
| Audited-worktree build artifact | None present |
| Acceptance device | Xiaomi `24090RA29G` / `malachite`, Android 16, 2712 x 1220 |
| Release conditions | Clean ARM64 IL2CPP release APK, Mobile quality, requested 60 FPS, thermal status `0`, cooling value `0` |

The serialized manifest at the audited baseline supplies the identity and counts above. This audit did not run Unity, so canonical dependency parity, scene integrity, and runtime resolver acceptance remain unverified. A later repository revision is acceptable only when it is the exact clean release revision, is a descendant of the audited baseline, and reproduces these same manifest identities and counts.

## Fail-Closed Rules

1. Evidence closes a row only when the exact clean release revision is a descendant of the audited baseline and its manifest dependency hash, content hash, counts, APK hash, build type, device, thermal state, and camera condition match the row's acceptance key.
2. All `525`-chunk / `17,564`-source captures are **historical-only and explicitly rejected for `514`-chunk release acceptance**. They may support direction or regression hypotheses, but not completion.
3. Development/profiler PSS, startup, and frame results cannot substitute for clean release measurements.
4. Metrics captured at different process ages, camera poses, thermal ordering, revisions, or APK hashes are not accepted as deltas.
5. A single coherent screenshot cannot satisfy the required top-down, oblique, low-ground, and gameplay-camera visual matrix.

## Evidence Matrix

| Required metric | Revision / manifest key | Build type / hash | Thermal state | Camera | Device | Existing evidence | Acceptance status | Next collection action |
|---|---|---|---|---|---|---|---|---|
| `APH-601`: startup duration | Clean descendant release revision; `9eebc7c...`; `514/16,542` | No baseline-bound APK | Cold, status `0`, cooling `0` | Deterministic gameplay camera | Target Xiaomi | Rejected 96 m candidate reached static-map startup in `3.24 s` and gameplay-ready in `6.60 s`; older ownership run only proves legacy combine `351-361 ms -> skipped`. Both use other artifacts/layouts. | **Missing** | Build the exact clean descendant release APK; collect five cold and five warm process-to-Match-ready samples through the release collector. |
| `APH-601`: renderer scan, eligible/skipped counts | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Status `0` | Startup path | Target Xiaomi | Historical runtime batching reported `4,409` scanned renderers and older bake/ownership evidence used `525/17,564`. The audited manifest statically records `16,542` sources but does not prove runtime scan/suppression counts. | **Missing** | Record release resolver/ownership startup markers and verify no legacy batching marker on the exact release artifact. |
| `APH-601`: generated vertices / generated mesh payload | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Status `0` | Startup path | Target Xiaomi | Rejected transient candidates generated `9.48-9.84 M` vertices. The accepted design generates no mesh assets, but no release measurement binds zero incremental generated meshes/vertices to this manifest. | **Missing** | Capture release ownership and mesh-allocation evidence; require zero accepted-path generated mesh assets and zero legacy combination. |
| `APH-601`: CPU mesh memory | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Status `0` | Settled Match | Target Xiaomi | No exact baseline-matching release CPU mesh-memory measurement. Development total/PSS values are not mesh-specific. | **Missing** | Capture mesh-category CPU memory before Match, at Match-ready, and after settling on the exact artifact. |
| `APH-601`: GPU mesh memory | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Status `0` | Settled Match | Target Xiaomi | No exact baseline-matching release GPU mesh-memory measurement. Historical graphics PSS is not a mesh-memory substitute. | **Missing** | Capture mesh-category GPU memory at the same three lifecycle points and bind it to the APK and manifest hashes. |
| `APH-601`: peak startup allocation | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Status `0` | Process launch through Match-ready | Target Xiaomi | The release recorder tracks sustained-run peak allocated memory after warmup, not peak startup allocation. | **Missing** | Add or use bounded startup-window allocation capture covering process launch through presentation preload and Match-ready. |
| `APH-601`: foreground load, settled visual, and 60 FPS | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Cold, status `0`, cooling `0` | Gameplay camera | Target Xiaomi | Older `525`-chunk runs loaded visibly and survived; best reported results remained about `43-46 FPS`. Evidence is stale and the 60 FPS condition is red. | **Missing / prior failure** | Run the exact release artifact foreground, verify stable PID and visible Match, then collect thermally clean sustained frame evidence. |
| `APH-609`: draw calls, batches, SetPass, triangles, vertices, CPU/GPU frame time | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Cold baseline; status `0`, cooling `0` | Fixed gameplay camera | Target Xiaomi | Historical `525`-chunk GRD evidence reports roughly `74-75` draws, `41-45` SetPass, `0.813-1.046 M` triangles, and improved GPU/render-thread cost. Comparison order was not thermally randomized. | **Rejected for release acceptance** | Use one exact release artifact and fixed camera; collect canonical/fallback and shared-mesh/GRD legs with randomized cold ordering and identical duration. |
| `APH-609`: release memory | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Before/during/after status `0`, cooling `0` | Fixed gameplay camera | Target Xiaomi | Development runs reported `2,445-2,630 MB` total PSS and `1,090-1,134 MB` graphics PSS. Reports explicitly reject these as release-memory acceptance. | **Missing** | Collect release peak allocated, Mono, resident-set, Android PSS, and lifecycle snapshots from the exact APK. |
| `APH-609`: APK and installed size | Clean descendant release revision; `9eebc7c...`; `514/16,542` | No baseline-bound APK; old release APK `cb18f21...`, revision `5a49ab8...`, `463,359,198` bytes | N/A | N/A | Target Xiaomi for installed size | The tracked release package report belongs to another revision and cannot bind the audited map artifact. No APK/AAB exists in this worktree. | **Missing** | Produce the exact clean descendant release APK, verify SHA-256 and `<=463,359,198` bytes, install it, and record installed size. |
| `APH-609`: cold/warm startup | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Cold device, status `0`, cooling `0` | Auto-start Match | Target Xiaomi | The serialized collector supports five cold and five warm starts, but no baseline-matching release result exists. | **Missing; tooling ready** | Run the existing release collector after exact artifact provenance passes. |
| `APH-609`: normalized canonical/runtime-batched versus shared-mesh/GRD comparison | Same clean descendant release revision and gameplay content for both legs | Exact, explicitly identified comparison artifacts | Randomized cold ordering; status `0`, cooling `0` | Identical pose, FOV, resolution, quality | Same target device | Historical control and candidate captures differ in artifact, layout, process age, and thermal ordering. | **Missing** | Define two fail-closed comparison legs and collect them consecutively under identical settings without mixing report sources. |
| `APH-609`: visual matrix and pixel review | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact comparison artifacts | Status `0` | Top-down, oblique, low-ground, gameplay camera | Target Xiaomi | The soak passed one tested camera; instancing/ownership reports include isolated control/final screenshots. Existing tooling/records do not supply all four paired views. | **Missing** | Capture paired PNGs for all four fixed poses; record hashes and review terrain, roads, structures, interiors, vegetation, props, lighting, seams, and culling holes. |
| `APH-609`: sustained survival / thermal integrity | Clean descendant release revision; `9eebc7c...`; `514/16,542` | Exact release APK required | Status `0`, cooling `0` throughout | Gameplay plus required visual poses | Target Xiaomi | The `762.363 s` development soak survived and passed tested-camera integrity, but HAL skin reached status `3`; it is explicitly invalid for release performance acceptance. | **Missing for release** | Run the 60-second warmup plus 600-second unplugged release collection and reject any thermal contamination, PID loss, crash marker, or incoherent screenshot. |

## Evidence Sources

- `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset`
- `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json`
- `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`
- `Design/AgentReports/2026-07-10_perf_WarlineCapture_candidate_android_full_summary.md`
- `Design/AgentReports/2026-07-10_perf_WarlineCapture_candidate_android_steady_summary.md`
- `Design/AgentReports/2026-07-11_gpu_instancing_android_comparison.md`
- `Design/AgentReports/2026-07-11_map_presentation_ownership_android.md`
- `Design/AgentReports/2026-07-11_aph-611_android_map_soak.md`
- `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`
- `Design/AgentReports/architecture_performance_android_apk_build_report.json`
- `Design/AgentReports/architecture_performance_android_apk_build_report.md`
- `Tools/CI/android_release_30fps_reference_device_profile.json`
- `Tools/CI/android_release_device_collection.py`

## Handoff

The non-Unity reconciliation is complete. Neither `APH-601` nor `APH-609` can close from existing evidence. The next accepted evidence must use an exact clean release revision descended from the audited baseline, reproduce the audited `514`-chunk manifest dependency/content hashes and `16,542` sources, pass structural validation, and bind every device capture to the exact release revision, manifest, artifact, device, thermal, and camera keys above.
