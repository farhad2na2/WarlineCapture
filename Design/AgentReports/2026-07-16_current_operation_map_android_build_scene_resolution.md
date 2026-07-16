# Current Operation-Map Android Build-Scene Resolution

Date: 2026-07-16
Result: Passed
Scope: Current compatibility-map Android build-scene inclusion only

## Acceptance

- `StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject` resolves the enabled base scenes followed by exactly the current compatibility manifest's `514` owned chunk scenes.
- Every returned scene is unique and manifest ordered.
- Missing/stale manifests, unsupported schemas, stale canonical dependency hashes, missing or foreign chunk scenes, duplicate chunks, stale enabled generated scenes, missing `Match.unity`, and integrity mismatches fail closed.
- Both Android build pipelines call the same manifest resolver.
- No staged operation-map scene, future map, Addressables group, or unowned generated scene is included by this compatibility path.

## Accepted Identity

- Canonical source: `Assets/Game/Scenes/Match.unity`
- Manifest: `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset`
- Manifest schema: `1`
- Canonical dependency hash: `2b20b20900b73c1b456c04f9173c33a7`
- Presentation content hash: `9eebc7c8aa774d5f505cb684099d133a`
- Manifest SHA-256: `b389013b4753e73e65424e1ca06737507aa6fc6a6be429903b2da683b052ec01`
- Accepted chunk count: `514`

## Validation

The documented macOS licensing workaround was used in windowed batchmode:

```text
Tools/CI/invoke_unity_macos.sh \
  --project /Users/farhad/Projects/WarlineCapture-Clone \
  --log /private/tmp/opmap-android-resolver-windowed.log \
  --timeout 420 -- \
  -runTests -testPlatform EditMode \
  -testFilter StaticMapAndroidBuildSceneResolverTests \
  -testResults /private/tmp/opmap-android-resolver-windowed-results.xml
```

- Focused EditMode result: `23 / 23` passed; zero failed, skipped, or inconclusive.
- Unity exited successfully.
- No compiler-error marker was emitted.
- `git diff --check` passed for the documentation update.

## Boundaries

- This validates the existing schema-v1 compatibility build path only.
- It does not bake the staged operation-map scene.
- It does not implement or validate multi-map packaging, Addressables, loading/unloading, a generator, or future physical maps.
