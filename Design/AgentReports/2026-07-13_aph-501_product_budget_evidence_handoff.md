# APH-501 Product Budget Evidence Handoff

Date: 2026-07-13
Status: Blocked on accepted release-device evidence; no product limit changed.

## Scope And Decision

This bounded non-Unity slice audited the tracked product-budget configuration, immutable Android BuildReport evidence, the current APK/report pair, the Android development and release evidence gates, the release-device collector, and their focused Python tests.

The APK and AAB limits remain valid. No evidence currently supports an absolute installed-size, peak allocated memory, texture memory, mesh memory, audio memory, or graphics-driver memory limit. Those limits must remain fail-closed `null`; assigning a numeric value now would fabricate product authority.

## Accepted Package Budgets

The tracked package limits resolve to immutable clean BuildReport evidence and pass all provenance checks.

| Budget | Accepted limit | Artifact revision | Artifact SHA-256 | Evidence commit | Result |
|---|---:|---|---|---|---|
| APK | 463,359,198 bytes | `5a49ab8f010674ca8b364af1245fe2902401b305` | `cb18f212d09ebde206884fd608e94441ce4f34fdc5800017067275f892824f20` | `a527e151e9e43a491ba30f4c19a0320dc54faf5c` | Accepted |
| AAB | 426,399,778 bytes | `a527e151e9e43a491ba30f4c19a0320dc54faf5c` | `c03558f2e093277949edf56ba8efd34d347e8f2396be594f8f88bdec5c57ac29` | `ddfca3b27c089da512925643933d68ae414cba43` | Accepted |

For both artifacts, the evidence blob at the pinned evidence commit is clean, complete, ARM64, IL2CPP, Android release, generated with `DetailedBuildReport`, matches the configured artifact revision/size/hash, and contains the required top-100 included-asset rows.

## Current Artifact Is Not Budget Authority

The local `Build/AndroidAPK/WarlineCapture.apk` and current working report agree at:

- Size: 552,481,264 bytes.
- SHA-256: `9b0102ea1b828334eed868309b76f4c646fb148062697edd90592351017bbe5e`.
- Build report revision: `4c05a2da10bf5117ca592cf8daac05459ab3b74c`.
- Build report dirty state: `true`.

This APK exceeds the accepted limit by 89,122,066 bytes (84.99 MiB, 19.23%). Its dirty provenance and package regression independently reject it as APH-501 evidence. It must not replace or loosen the accepted APK budget.

## Remaining Budget State

| Budget | Current state | Evidence available | Exact blocker |
|---|---|---|---|
| Installed size | `measurement-required`, limit `null` | The APH-804 collector can measure the installed package code path with `du -sb` and binds it to the device-side APK hash. | No accepted `aph804_android_release_evidence.json` exists. The available APK fails clean-provenance and package-size preflight. |
| Peak allocated memory | Relative 10% reduction target active; absolute release limit `null` | The release recorder/gate can capture peak allocated, peak Mono, and peak resident-set memory during the bounded Match run. | No accepted clean-release device capture exists. The historical 1,054-1,075 MB same-device baseline is sufficient only for the existing relative target, not a new absolute release ceiling. |
| Texture memory | `measurement-required`, limit `null` | BuildReport proves package contribution, not simultaneous runtime residency. | The APH-804 recorder has no texture residency field and no same-artifact Memory Profiler capture supplies `textureMemoryBytes`. |
| Mesh memory | `measurement-required`, limit `null` | BuildReport and generated-mesh inventory can prove inclusion/source size only. | The APH-804 recorder has no mesh residency field and no same-artifact Memory Profiler capture supplies `meshMemoryBytes`. |
| Audio memory | `measurement-required`, limit `null` | APH-401 editor captures describe audio residency behavior. | Existing APH-401 reports are dirty Editor captures, not clean release-device evidence, and do not satisfy same-artifact provenance plus representative playback coverage. |
| Graphics-driver memory | `measurement-required`, limit `null` | The release lane records overall process memory and graphics API provenance. | It does not record `graphicsDriverMemoryBytes`; no accepted same-artifact Memory Profiler/device capture exists. |

## Evidence-Lane Finding

The APH-804 non-Unity release contract is sufficient to collect and validate installed size plus overall peak memory once a compliant APK exists. It intentionally leaves installed size and absolute memory non-blocking and measurement-required. It does not provide the four resource-category memory measurements required by APH-501, so those categories need an additional same-revision Memory Profiler/device evidence artifact rather than a guessed conversion from BuildReport bytes or process PSS.

No release evidence/result/recorder JSON is currently present in the repository or ignored output inventory under the canonical APH-804 filenames.

## Contract Drift Found

The tracked budget JSON is schema version `4`, while `PerformanceProductBudgetValidator.ExpectedSchemaVersion` and `PerformanceProductBudgetValidatorTests.TrackedConfig_HasStrictSchemaAndApprovedThresholds` still require version `3`.

This is a deterministic validator/test drift, not evidence for changing any budget. A separate Unity Editor validation slice must align the validator and focused test with schema 4, then run `PerformanceProductBudgetValidatorTests.RunFocusedValidation`. This report does not touch those Unity files.

## Required Unblocking Sequence

1. Produce a clean ARM64 IL2CPP release APK with a matching detailed BuildReport at or below 463,359,198 bytes.
2. Run the serialized APH-804 collector on the pinned Xiaomi `24090RA29G` while unplugged: five cold starts, five warm starts, 60-second warmup, 600-second foreground Match sample, and at least 9,000 structured frames.
3. Accept the resulting exact-revision, exact-APK evidence only after the release gate passes. This supplies installed size and overall peak allocated/Mono/resident-set measurements.
4. Capture texture, mesh, audio, and graphics-driver residency for the same revision, APK, device, release build type, scenario, warmup, and sample window using Memory Profiler/device evidence. Include representative playback coverage for audio and the graphics API for driver memory.
5. Review and explicitly accept those measurements before replacing any `null` limit. Keep the existing APK/AAB ratchets unchanged unless a separately approved budget decision tightens them.
6. Align the Unity product-budget validator/test schema expectation from 3 to the tracked schema 4 and rerun its focused validation.

## Non-Unity Validation

Command:

```text
python3 -m unittest -v \
  Tools.CI.tests.test_android_development_performance_gate \
  Tools.CI.tests.test_android_release_performance_gate \
  Tools.CI.tests.test_android_release_device_collection
```

Result: 46/46 tests passed.

The immutable APK/AAB evidence audit also passed every clean-build identity, package type, release mode, target/backend/architecture, detailed-report, revision, byte-size, SHA-256, and top-100 row check.

## Tracker-Ready Handoff

- APH-501 remains active.
- Completed authority: tracked APK and AAB budgets.
- Blocked authority: installed size, absolute peak allocated memory, texture memory, mesh memory, audio memory, and graphics-driver memory.
- Blocking condition: no accepted clean-release APH-804 device capture; no same-artifact category residency capture.
- Next dependency-ready work: clean package-compliant release APK and serialized device collection, followed by same-artifact category residency capture.
- No budget, runtime behavior, asset, scene, prefab, generated map, audio runtime, FirstLaunch file, or tracker entry changed in this slice.
