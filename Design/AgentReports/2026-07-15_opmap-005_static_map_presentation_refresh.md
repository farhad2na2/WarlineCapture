# opmap-005 Static Map Presentation Refresh

Date: 2026-07-15
Baseline: `3b7228292db7159c3c70025cf5d1676573721cd4`
Branch: `codex/opmap-005-static-map-presentation-refresh`
Workflow: pull request; implementation context does not merge.

## Result

Build #106 reported a stale static-map presentation manifest during Android APK resolution. Its exact revision and workspace state were not captured, so the root cause is unknown. The failure did not reproduce in a clean isolated worktree at the baseline above.

The accepted Phase 0 probe passed before regeneration with `514` chunks and `16,542` sources. Two authoritative Android-target bakes then passed with identical results:

- content hash: `9eebc7c8aa774d5f505cb684099d133a`
- sources: `16,542`
- chunks: `514`
- scenes written: `0`
- stale scenes deleted: `0`
- reuse rejection: `none`

Tracked and untracked source/product assets remained clean after each bake. Therefore the checked-in manifest, integrity ledger, generated scenes, and generated `.meta` files already match the current `Match.unity` dependency state. No generated asset was changed or committed.

## APK Verification

`Game.Editor.BuildScript.BuildAndroid -buildType APK` completed successfully after the Android-target no-op bakes.

- artifact: `Build/AndroidAPK/WarlineCapture.apk`
- file size: `442,924,488` bytes
- SHA-256: `38609e1a20a3d8663b72306d3ac0995df78ed2d2d172f1dd235360e0f048fbe3`
- Unity result: `Build succeeded`

Focused EditMode validation also passed `35 / 35` with zero failures, skips, or inconclusive tests. The suite covered `StaticMapAndroidBuildSceneResolverTests`, `StaticMapPresentationSceneWiringTests`, `StaticMapPresentationOwnershipTests`, and `StaticMapPresentationStructuralValidationTests`.

Tracked and untracked source/product assets remained clean after artifact verification. Ignored artifacts remained under Build, Addressables, log, and cache paths and are not committed. The APK at `Build/AndroidAPK/WarlineCapture.apk` was intentionally retained as build evidence; the other ignored files are transient build and cache artifacts.

## Commands And Logs

Read-only pre-bake probe:

```text
Tools/CI/invoke_unity_macos.sh --project <worktree> --timeout 1800 \
  --log /private/tmp/opmap-005-prebake-probe.log -- \
  -quit -executeMethod Game.Editor.OperationMapPhase0BaselineProbe.Run
```

Authoritative bake, run twice:

```text
Tools/CI/invoke_unity_macos.sh --project <worktree> --timeout 3600 \
  --log /private/tmp/opmap-005-android-bake-N.log -- \
  -buildTarget Android -quit \
  -executeMethod Game.Editor.StaticMapPresentationBaker.Bake
```

APK build:

```text
Tools/CI/invoke_unity_macos.sh --project <worktree> --timeout 7200 \
  --log /private/tmp/opmap-005-android-apk.log -- \
  -buildTarget Android -quit \
  -executeMethod Game.Editor.BuildScript.BuildAndroid -buildType APK
```

Focused EditMode tests:

```text
Tools/CI/invoke_unity_macos.sh --project <worktree> --timeout 1800 \
  --log /private/tmp/opmap-005-focused-tests.log -- \
  -runTests -testPlatform EditMode \
  -testFilter 'StaticMapAndroidBuildSceneResolverTests;StaticMapPresentationSceneWiringTests;StaticMapPresentationOwnershipTests;StaticMapPresentationStructuralValidationTests' \
  -testResults /private/tmp/opmap-005-focused-tests.xml
```

## Disposition

Because Build #106's exact revision and workspace state were not captured, the retained evidence does not establish its root cause and does not support attributing it to Jenkins-local cache state. The clean isolated-worktree run establishes only that the failure did not reproduce at the baseline above and that the committed static-map assets matched that baseline. Jenkins should rerun from a clean workspace at this baseline or later. If the resolver fails again, retain the exact Jenkins revision, manifest dependency hash, recomputed dependency hash, active build target, and workspace status before cleanup.
