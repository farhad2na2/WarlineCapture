# APH-509 Package Usage Inventory

This is deterministic, read-only static evidence. It does not approve package removal.
A candidate still requires isolated import, compile, test, Android build, and device validation.

## Coverage

- First-party source and asmdef references under `Assets/`.
- Text-serialized asset references resolved through package `.meta` GUID ownership.
- Built-in Cloth, Umbra, and Wind source/YAML signatures whose packages do not expose stable GUID ownership.
- Build automation under `Tools/CI`, `.github`, build-named editor files, and Jenkins.
- Editor workflows from editor source, package/assembly mentions, documentation, external IDE configuration, and SVG importer state.
- Manifest/lock state and reverse package dependencies.

## Summary

- Inventory valid: **true**
- Package removal authorized: **false**
- Total package graph entries: **68**
- Manifest-declared packages: **47**
- Embedded depth-zero manifest discrepancies: **1**
- Ordinary lock-only transitives: **20**
- Static-only candidate-unused declarations: **15**
- Unproven static blind spots: **2**

## Accepted Current Count Contract

- Total graph entries: `68`
- Manifest declarations: `47`
- Embedded depth-zero discrepancies: `1`
- Lock-only transitives: `20`
- Static-only candidates: `15`
- Static blind spots: `2`

## Audited Inputs

- Manifest SHA-256: `c650575e2642faa5b53753d5a5aab2c99450ae4c27f88fc4e622f474595b8289`
- Lock SHA-256: `1d236fe27a37ac281785ef94283f220b9135ca0ee73976117782a5b674ea935e`
- Upstream comparison ref: `origin/main`
- Worktree package inputs match upstream: `true`

## Candidate Removal Blockers

No candidate is approved for removal. Static absence only starts an isolated validation lane.

| Candidate | Removal authorized | Blocking evidence still required |
|---|---|---|
| `com.unity.collab-proxy` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.accessibility` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.adaptiveperformance` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.ai` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.cloth` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.screencapture` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.tilemap` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.unitywebrequesttexture` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.unitywebrequestwww` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.vehicles` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.video` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.wind` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.modules.xr` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.multiplayer.center` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |
| `com.unity.visualscripting` | false | static-zero-evidence-is-not-runtime-proof, isolated-package-resolution-not-run, isolated-import-and-compile-not-run, full-test-suite-not-run, release-android-build-delta-not-measured, release-device-smoke-not-run |

## Deterministic Evidence

| Package | State | Classification | Source | Serialized | Build | Editor | Ambiguous | Required by | Example |
|---|---|---|---:|---:|---:|---:|---:|---:|---|
| `com.sniveler-code.gpu-animation` | embedded-depth-zero-manifest-absent | usage-evidence-found | 12 | 137 | 0 | 14 | 0 | 0 | `Assets/Game/Prefabs/Generated/CharactersBaked/Animators/Animator_SM_Chr_Bombsuit_Male_01_CombinedSkinned_0.prefab` |
| `com.unity.2d.sprite` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.ai.assistant` |
| `com.unity.addressables` | manifest-declared | usage-evidence-found | 4 | 13 | 0 | 3 | 0 | 0 | `Assets/AddressableAssetsData/AddressableAssetSettings.asset` |
| `com.unity.ai.assistant` | manifest-declared | usage-evidence-found | 0 | 0 | 0 | 2 | 0 | 0 | `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs` |
| `com.unity.burst` | lock-only-transitive | usage-evidence-found | 71 | 0 | 1 | 4 | 0 | 5 | `Assets/Game/Scripts/Editor/BuildScript.cs` |
| `com.unity.collab-proxy` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.collections` | lock-only-transitive | usage-evidence-found | 332 | 0 | 8 | 115 | 0 | 3 | `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs` |
| `com.unity.entities` | manifest-declared | usage-evidence-found | 529 | 3 | 24 | 199 | 0 | 2 | `Assets/Game/Scenes/Match.unity` |
| `com.unity.entities.graphics` | manifest-declared | usage-evidence-found | 23 | 0 | 0 | 10 | 0 | 1 | `Assets/Game/Scripts/Components/FactionVisualComponents.cs` |
| `com.unity.ext.nunit` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.ide.rider` |
| `com.unity.ide.rider` | manifest-declared | unproven-static-blind-spot | 0 | 0 | 0 | 0 | 1 | 0 | `.gitignore` |
| `com.unity.ide.visualstudio` | manifest-declared | unproven-static-blind-spot | 0 | 0 | 0 | 0 | 1 | 0 | `.gitignore` |
| `com.unity.inputsystem` | manifest-declared | usage-evidence-found | 10 | 4 | 1 | 1 | 0 | 0 | `Assets/Game/Scenes/Menu.unity` |
| `com.unity.mathematics` | lock-only-transitive | usage-evidence-found | 359 | 0 | 8 | 120 | 0 | 2 | `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs` |
| `com.unity.modules.accessibility` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.adaptiveperformance` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.ai` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.androidjni` | manifest-declared | usage-evidence-found | 1 | 0 | 0 | 0 | 0 | 0 | `Assets/Game/Scripts/Systems/AndroidPerformanceRecorder.cs` |
| `com.unity.modules.animation` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 3 | `com.unity.modules.director` |
| `com.unity.modules.assetbundle` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 5 | `com.unity.addressables` |
| `com.unity.modules.audio` | manifest-declared | usage-evidence-found | 15 | 0 | 5 | 24 | 0 | 7 | `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs` |
| `com.unity.modules.cloth` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.director` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.timeline` |
| `com.unity.modules.hierarchy` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.entities` |
| `com.unity.modules.hierarchycore` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.modules.hierarchy` |
| `com.unity.modules.imageconversion` | manifest-declared | usage-evidence-found | 0 | 0 | 0 | 3 | 0 | 5 | `Assets/Game/Scripts/Editor/MatchHudCurrentOrderBannerPlayModeValidation.cs` |
| `com.unity.modules.imgui` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 6 | `com.unity.modules.hierarchy` |
| `com.unity.modules.jsonserialize` | manifest-declared | usage-evidence-found | 3 | 0 | 3 | 17 | 0 | 11 | `Assets/Game/Scripts/Balance/BalanceReportWriter.cs` |
| `com.unity.modules.particlesystem` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.entities.graphics` |
| `com.unity.modules.physics` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 7 | `com.unity.entities` |
| `com.unity.modules.physics2d` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.modules.tilemap` |
| `com.unity.modules.physicscore2d` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.modules.physics2d` |
| `com.unity.modules.screencapture` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.subsystems` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.modules.adaptiveperformance` |
| `com.unity.modules.terrain` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.render-pipelines.core` |
| `com.unity.modules.tilemap` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.ui` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 3 | `com.unity.modules.uielements` |
| `com.unity.modules.uielements` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 6 | `com.unity.ai.assistant` |
| `com.unity.modules.umbra` | manifest-declared | usage-evidence-found | 3 | 0 | 1 | 8 | 0 | 0 | `Assets/Editor/SkinnedPrefabCombinerWindow.cs` |
| `com.unity.modules.unityanalytics` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.entities` |
| `com.unity.modules.unitywebrequest` | manifest-declared | usage-evidence-found | 0 | 0 | 0 | 1 | 0 | 9 | `Assets/Game/Scripts/Editor/Narrative/FirstLaunchNarrativePerformanceValidation.cs` |
| `com.unity.modules.unitywebrequestassetbundle` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.addressables` |
| `com.unity.modules.unitywebrequestaudio` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.modules.unitywebrequestwww` |
| `com.unity.modules.unitywebrequesttexture` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.unitywebrequestwww` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.vectorgraphics` | manifest-declared | usage-evidence-found | 0 | 0 | 0 | 276 | 0 | 0 | `Assets/GUI/Buttons/Button_circle_brown.svg` |
| `com.unity.modules.vehicles` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.video` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.wind` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.modules.xr` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.multiplayer.center` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |
| `com.unity.nuget.mono-cecil` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 3 | `com.unity.ai.assistant` |
| `com.unity.nuget.newtonsoft-json` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.ai.assistant` |
| `com.unity.probuilder` | manifest-declared | usage-evidence-found | 0 | 6 | 1 | 2 | 0 | 0 | `Assets/Editor/ProBuilderShapeBakerWindow.cs` |
| `com.unity.profiling.core` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.addressables` |
| `com.unity.render-pipelines.core` | lock-only-transitive | usage-evidence-found | 30 | 19 | 1 | 27 | 0 | 4 | `Assets/Editor/SkinnedPrefabCombinerWindow.cs` |
| `com.unity.render-pipelines.universal` | manifest-declared | usage-evidence-found | 9 | 443 | 0 | 2 | 0 | 0 | `Assets/Game/Effects/Combat/Materials/Mat_Vfx_Dust_Alpha.mat` |
| `com.unity.render-pipelines.universal-config` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.render-pipelines.universal` |
| `com.unity.scriptablebuildpipeline` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.addressables` |
| `com.unity.searcher` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.shadergraph` |
| `com.unity.serialization` | manifest-declared | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.entities` |
| `com.unity.settings-manager` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 1 | `com.unity.probuilder` |
| `com.unity.shadergraph` | manifest-declared | usage-evidence-found | 0 | 321 | 0 | 0 | 0 | 2 | `Assets/Piloto Studio/Materials/Arcane/ArcaneRing_Runes.mat` |
| `com.unity.test-framework` | manifest-declared | usage-evidence-found | 13 | 0 | 31 | 274 | 0 | 5 | `Assets/Tests/Editor/AIBuildPlannerAllocationTests.cs` |
| `com.unity.test-framework.performance` | lock-only-transitive | dependency-graph-required | 0 | 0 | 0 | 0 | 0 | 2 | `com.unity.collections` |
| `com.unity.timeline` | manifest-declared | usage-evidence-found | 0 | 0 | 0 | 2 | 0 | 0 | `Design/Campaign_Narrative_And_Content_Redesign_Recommendations.md` |
| `com.unity.ugui` | manifest-declared | usage-evidence-found | 94 | 400 | 10 | 52 | 0 | 1 | `Assets/Game/Animations/UI/UIButton_Disabled.anim` |
| `com.unity.visualscripting` | manifest-declared | candidate-unused-static-only | 0 | 0 | 0 | 0 | 0 | 0 | `-` |

## Fail-Closed Limitations

- Binary assets, reflection, generated code, native plugins, shader includes, and untracked external editor services can evade static attribution.
- Explicit built-in component probes cover known Cloth, Umbra, and Wind source/YAML signatures; unknown signatures remain a fail-closed limitation.
- Namespace and workflow text matches are evidence to inspect, not proof that every reference is semantically required.
- A zero in all columns means only that this inventory found no static evidence. It never proves runtime safety.
- Lock reverse dependencies describe the current graph; Unity must resolve the lock after any isolated manifest experiment.

## Removal Gate

1. Change one manifest declaration in an isolated clean worktree; never hand-edit ordinary lock-only transitives.
2. Complete clean package resolution/import and require zero compile, import, missing-script, and missing-shader errors.
3. Run full EditMode/PlayMode coverage and the affected editor workflows.
4. Produce the release-equivalent Android build and compare BuildReport, warnings, assemblies, shaders, and size.
5. Run device startup, menu, Match, input, rendering, audio, networking/content, and thermal smoke coverage.
6. Retain the package when evidence is ambiguous; removal requires separate review and measured proof.

## Reproduction

```sh
python3 Tools/CI/aph509_package_usage_inventory.py --check
python3 Tools/CI/aph509_package_usage_inventory.py --write-report
python3 Tools/CI/aph509_package_usage_inventory.py --json
python3 -m unittest Tools.CI.tests.test_aph509_package_usage_inventory
```
