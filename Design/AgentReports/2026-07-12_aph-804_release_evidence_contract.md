# APH-804 Android Release Evidence Contract

Date: 2026-07-12
Status: Stable release-recorder, artifact-binding, and serialized device-collection tooling; a clean release artifact and physical-device acceptance remain required.

## Result

- Extracted the strict Android evidence validator into a shared Python core while preserving the APH-803 public module, CLI marker, profile semantics, and tests.
- Added an APH-804 release-specific profile, strict evidence schema, CLI wrapper, and focused tests.
- The release profile pins the Xiaomi `24090RA29G`, ARM64 IL2CPP release APK, Mobile quality, and a requested/attested 60 FPS configuration.
- The exact tokenized launch sequence is `-warlineAutoStartMatch`, `-warlineAndroidPerformanceGate`, `APH-804`, `-warlinePerformanceFrameRate`, `60`. Profiler, development, and debug arguments are rejected.
- APH-804 blocks on p95 frame time strictly below `33 ms`, release APK size at or below `463,359,198` bytes, clean revision/artifact identity, 60-second warmup, 600-second foreground capture, at least 9,000 structured frames, process survival, zero fatal markers, and uncontaminated thermal/cooling evidence.
- The `<25 ms` p95 high-end target is reported as a separate non-blocking observation.
- P99, startup p95, installed size, and absolute memory are required measurements but retain non-blocking `measurement-required` limits until separate accepted evidence approves budgets.
- Release evidence must attest a non-development, non-script-debugging, non-profiler recorder mode and include GC, memory, battery, CPU/GPU timing, batches, SetPass, triangles, vertices, installed size, APK size, thermal snapshots, raw log, and a coherent hashed screenshot.
- Generalized the existing APH-803 recorder into one `AndroidPerformanceRecorder` with explicit development and release modes. APH-803 keeps its filename and JSON shape; APH-804 writes `aph804_android_release_recorder.json` with the exact release-only shape required by the gate.
- The existing `PerformanceDiagnosticsSystemHelper` remains the only per-frame owner. It forwards its existing render counters; the recorder owns only one opt-in GC allocation counter, preallocated timing buffers, one-second Android PSS sampling, and start/end battery sampling.
- APH-804 applies an unsaved Mobile/60 runtime override through `SettingsService`; it does not overwrite the player's persisted settings or write `Application.targetFrameRate` directly.
- Release capture fails closed if development/script-debugging/profiler/marker provenance is dirty, CPU/GPU timing is missing, GC/render counters are unavailable, battery data is unavailable or increases, or Android PSS cannot be measured. Profiler activation is latched throughout capture rather than checked only at startup.
- The Android build-report boundary now rejects `Development`, `AllowDebugging`, `ConnectWithProfiler`, and `EnableDeepProfilingSupport` in addition to requiring `DetailedBuildReport`.
- Jenkins now runs a release-artifact contract stage immediately after a requested APK build. It binds the contract to the actual APK SHA-256 and byte length, exact clean revision, canonical build report, release/Android/IL2CPP/ARM64 identity, and package-size ceiling before any deployment stage.
- Added one serialized Python ADB collection runner for the pinned release device. It rejects the artifact before ADB access unless the APK, exact clean revision, canonical BuildReport, hash, byte size, profile path, and package ceiling agree.
- The runner verifies one exact online device, pinned hardware/OS/display properties, a non-debuggable ARM64 installation, device-side APK hash, installed size, and the exact Unity GameActivity launch arguments.
- Collection performs exactly five cold starts with package-data clearing, five warm process starts without clearing, then one foreground sustained run with an unplugged battery, before/during/after thermal snapshots, stable PID/activity checks, a 60-second warmup plus 600-second recorder capture, final screenshot, verbatim recorder pull, raw log hashing, evidence assembly, and the existing release-gate validation.

## Fail-Closed Boundary

Contract generation reports `acceptanceReady=false` until a release-mode structured recorder and validated release-device evidence are supplied. Development recorder output cannot satisfy APH-804. Evidence validation requires artifact files and verifies their hashes, APK size/path, screenshot dimensions/package, recorder identity, raw crash markers, build type, exact launch tokens, and all profile thresholds.

The release runtime recorder, real-artifact Jenkins binding, and serialized ADB collection tooling are implemented. Product acceptance remains blocked until a clean package-compliant APK is collected on the unplugged reference device.

## Package Regression Audit

- Accepted clean APK: `463,359,198` bytes.
- Current dirty/stale APK: `552,481,264` bytes.
- Exact regression: `89,122,066` bytes (`84.99 MiB`, `19.23%`).
- Attributed packed assets decreased by `275,691,720` bytes while the BuildReport summary remainder increased by `350,151,978` bytes. The report therefore cannot honestly assign the APK regression to a top-100 asset row.
- Generated UI (`218,001,776` packed bytes in the dirty top 100), thirteen 4K world textures (`141,909,908` bytes), animation textures (`50,332,044` bytes), audio, and package manifests were unchanged between the compared artifact commits and are not demonstrated causes.
- Dirty combined static-map output is the leading candidate because it is the only new positively attributed family and replaced highly compressible impostor content. The current generated-mesh directory postdates the APK, so its source size cannot be used as an artifact delta.
- Required next proof is a clean current-revision release APK using the shared-mesh presentation resolver, followed by complete included-asset and ZIP-entry comparison. No map path, texture, animation asset, audio clip, or package is approved for deletion from this audit alone.

## Files

- `Tools/CI/android_performance_evidence_gate.py`
- `Tools/CI/android_development_performance_gate.py`
- `Tools/CI/tests/test_android_development_performance_gate.py`
- `Tools/CI/android_release_30fps_reference_device_profile.json`
- `Tools/CI/android_release_performance_evidence.schema.json`
- `Tools/CI/android_release_performance_gate.py`
- `Tools/CI/tests/test_android_release_performance_gate.py`
- `Assets/Game/Scripts/Systems/AndroidPerformanceRecorder.cs`
- `Assets/Game/Scripts/Systems/AndroidPerformanceRecorder.Reporting.cs`
- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystemHelper.cs`
- `Assets/Game/Scripts/Composition/AndroidPerformanceRuntimeSettings.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs`
- `Assets/Tests/Editor/AndroidPerformanceRecorderTests.cs`
- `Assets/Game/Scripts/Editor/AndroidBuildReportGenerator.cs`
- `Assets/Tests/Editor/AndroidBuildReportGeneratorTests.cs`
- `Tools/CI/InvokeAndroidReleasePerformanceContract.ps1`
- `Tools/CI/tests/test_android_release_performance_ci_contract.py`
- `Tools/CI/android_release_device_collection.py`
- `Tools/CI/tests/test_android_release_device_collection.py`
- `Jenkinsfile.groovy`

## Validation

- Development, release, and APH-803 CI source-contract tests: `41/41` passed.
- Python compilation passed for the shared core, both gate wrappers, and focused tests.
- Release profile and schema parse as JSON.
- Two APH-804 contract generations were byte-identical.
- Direct APH-804 CLI marker: `[APH-804 AndroidReleaseGate] result=ContractGenerated`.
- Generated contract reports `acceptanceReady=false`, ten declared startup runs, release-recorder attestation requirements, and all required collections.
- Scoped `git diff --check` passed.
- Focused Unity recorder/settings/serialization/allocation validation passed `16/16` with zero compiler errors. Log: `/private/tmp/warline-aph804-recorder-focused-final.log`.
- Existing diagnostics allocation regression passed `4/4`. Log: `/private/tmp/warline-aph804-diagnostics-allocation.log`.
- Naming/non-ECS architecture validation passed `9/9`. Log: `/private/tmp/warline-aph804-non-ecs-architecture.log`.
- Assembly-boundary validation passed `31/31`. Log: `/private/tmp/warline-aph804-assembly-boundary.log`.
- Android build-report provenance validation passed `8/8`. Log: `/private/tmp/warline-aph804-build-report-provenance.log`.
- Combined development/release CI, gate, and device-collection tests passed `62/62`; Python compilation and scoped whitespace checks passed.
- The device collector has `12/12` focused tests covering exact launch tokens, thermal/battery parsing, cold/warm startup semantics, stale-recorder rejection, PID continuity, bounded timeout behavior, verbatim pull, installed-size parsing, pre-ADB provenance rejection, and evidence compatibility with the existing gate.
- Direct current-artifact preflight fails before ADB access with `APK size 552481264 exceeds profile maximum 463359198`, as required.
- The current local APK is rejected evidence: its report is dirty/stale and `552,481,264` bytes exceeds the `463,359,198`-byte ceiling by `89,122,066` bytes. The Jenkins stage will fail closed on an equivalent artifact.
- PowerShell is unavailable on this macOS host, so the first direct wrapper execution remains owned by the Windows Jenkins agent; source-contract tests cover stage ordering, arguments, artifact/report identity, hashing, limits, output, archival, and the prohibition on Unity/ADB invocation.
- Both new recorder source files remain below 500 lines. The touched helpers remain within their ratchets: diagnostics is `907 lines / 39,098 bytes` versus `909 / 39,101`, while Menu composition is `917 / 37,959`, 17 bytes above `HEAD` but below its approved `932 / 38,015` ceiling.
- The tactical-materials config/startup changes and resource-hauler movement integration now have exact bounded decisions; initial spawning, vehicle route clearance, building hauling, and Commander shell lifecycle were extracted below their frozen ceilings. Focused behavior and non-ECS architecture validation pass. The global source-growth gate now reports only `AudioPlaybackPresentationSystemHelper.cs`, which is explicitly owned by another workstream and was not changed here. Log: `/private/tmp/warline-source-growth-integrated-extractions.log`.
- The Phase 7 inventory now covers all 197 ECS declarations without increasing the 24 `SystemBase` count. Its full gate advances to 26 runtime-loop registrations outside APH-804: 24 Scenario Lab coroutines, one resource-exchange popup `Update`, and the separately owned audio `Update`. APH-804 adds no ECS declaration or loop. Log: `/private/tmp/warline-non-ui-system-inventory-reconciled-r2.log`.

## Next Slice

First validate and integrate the current 525-to-514 static-map rebake: the committed map manifest is stale for the current Match dependency hash, so the release resolver correctly refuses a clean build. Then produce a clean current-revision release APK under the package ceiling using the shared-mesh presentation path, run the serialized collector on the unplugged reference device, and validate its five cold starts, five warm starts, 60-second warmup, 600-second sustained run, and hashed recorder/log/screenshot/thermal evidence before approving any measurement-required limits.
