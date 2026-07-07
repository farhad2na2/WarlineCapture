# Mobile Visual Quality Verification Plan

Date: 2026-07-07

## Scope
Verify whether the current mobile render tier is too aggressive visually before changing defaults. This is an evidence plan only; no `VisualQualityConfig.asset`, URP asset, scene, prefab, or gameplay behavior changed in this slice.

## Current Runtime Owner
- Config asset: `Assets/Game/Rendering/VisualQualityConfig.asset`
- Runtime applier: `Assets/Game/Scripts/Systems/VisualQualitySettingsSystem.cs`
- Scene binding: `Assets/Game/Scripts/Composition/MatchSceneView.cs` via the Match scene `visualQualityProfile` reference.
- Mobile URP asset: `Assets/Settings/Mobile_RPAsset.asset`
- Android default quality tier: `ProjectSettings/QualitySettings.asset` maps Android to quality index `1`, named `Mobile`.

## Current Mobile Settings
| Setting | Current value | Source |
|---|---:|---|
| `runtimeMode` | `2` (`High`) | `VisualQualityConfig.asset` |
| Low render scale override | `0.5` | `VisualQualityConfig.asset` |
| Medium/High render scale override | `0.5` | `VisualQualityConfig.asset` |
| Mobile URP `m_RenderScale` | `0.5` | `Assets/Settings/Mobile_RPAsset.asset` |
| Mobile quality `shadowDistance` | `16` | `ProjectSettings/QualitySettings.asset` |
| Mobile URP `m_ShadowDistance` | `16` | `Assets/Settings/Mobile_RPAsset.asset` |
| Mobile URP shadow cascades | `1` | `Assets/Settings/Mobile_RPAsset.asset` |
| Main light shadowmap | `512` | `Assets/Settings/Mobile_RPAsset.asset` |
| HDR | off | `Assets/Settings/Mobile_RPAsset.asset` |
| MSAA | off/effectively `1x` | `Assets/Settings/Mobile_RPAsset.asset` |
| Additional lights | off | `Assets/Settings/Mobile_RPAsset.asset` |
| Ground variation | off | `VisualQualityConfig.asset` |

## Recommended Candidate For Sign-off
Do not make this default until screenshots and Android metrics pass.

| Setting | Candidate value | Reason |
|---|---:|---|
| Medium/High render scale override | `0.75` | First visual recovery step from `0.5` while staying below full resolution. |
| Mobile URP render scale | `0.75` while testing | Match runtime override for direct asset capture. |
| Shadow distance | `48` | Inside the tracker recommendation of `40-60`; likely enough for RTS zoom readability. |
| Shadow cascades | `1` first, optional `2` second pass | Keep first test close to current GPU cost; test `2` only if shadow cutoff is visible. |
| Main light shadowmap | `512` first, optional `1024` second pass | Avoid increasing atlas cost until screenshots prove `512` is visibly insufficient. |
| HDR | off | Keep current mobile cost profile. |
| MSAA | off/effectively `1x` | Keep current mobile cost profile. |
| Additional lights | off | Keep current mobile cost profile. |
| Ground variation | unchanged until a separate visual sign-off | Avoid bundling terrain-material risk with render-scale/shadow evaluation. |

## Required Screenshot Set
Capture both the current tier and the recommended candidate from the same device and the same camera setup.

1. Gameplay zoom: Faction 1 area after match start, normal command camera, UI visible.
2. Max zoom-out: same area after pressing Zoom Out to the farthest allowed RTS zoom.
3. Night phase: deepest-night or closest available night-state view with the same camera area.

Screenshot acceptance criteria:
- Infantry silhouettes remain readable.
- Vehicle/building edges are not visibly mushy at gameplay zoom.
- Shadow cutoff is not distracting at normal zoom or max zoom-out.
- Night view is readable without flattening the scene.
- UI remains unchanged and legible.

## Android Metrics Required
Use a current-branch Android profiler APK and compare the recommended candidate against the existing accepted mobile evidence.

Metrics to record:
- Average frame time.
- P95 frame time.
- P99 frame time.
- P95 CPU active.
- P95 GPU time.
- Total GC bytes.
- Draw calls, batches, SetPass, triangles, and vertices when available.
- Thermal/app survival notes for long enough foreground runtime.

Existing exporter:
- `Game.Editor.ProfilerCaptureSummaryExporter.Export`
- Report title: `Android Profiler Capture Summary`
- Includes frame time, CPU active, GPU, GC, render counters, marker tables, and slow frames.

## Validation Commands
Build or refresh a profiler APK when the Android device path is available:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 1200 --log /private/tmp/warline-visual-quality-android-profiler-apk.log -- -quit -executeMethod Game.Editor.BuildScript.BuildAndroidProfilerApk
```

Run local editor baseline after any committed code/config change:

```bash
git diff --check
dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-visual-quality-performance-baseline.log -- -quit -executeMethod Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline
```

## Decision Gate
No default mobile visual-quality value should change until:
- Current and candidate screenshots are captured side by side.
- Android profiler summary exists for the candidate.
- Candidate p95/p99/GPU/GC remain acceptable against the accepted mobile baseline.
- User approves the visual tradeoff.

