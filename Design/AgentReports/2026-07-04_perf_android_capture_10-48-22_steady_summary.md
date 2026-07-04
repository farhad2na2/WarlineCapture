# Android Profiler Capture Summary

Date: 2026-07-04 11:03:27 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-04_10-48-22.data`
Profiler frames: `900..1999`
Scanned frames: `1100`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 26.40 ms (37.9 FPS) |
| P50 frame | 24.46 ms (40.9 FPS) |
| P95 frame | 39.87 ms (25.1 FPS) |
| P99 frame | 55.07 ms (18.2 FPS) |
| Max frame | 66.84 ms (15.0 FPS) |
| Frames over budget | 1099/1100 |
| Avg CPU active | 19.41 ms |
| P95 CPU active | 17.35 ms |
| Avg GPU time | 21.86 ms |
| P95 GPU time | 25.51 ms |
| Total GC allocated | 2340914 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 28930.50 | 26.300 | 66.72 | 1263 | 301.91 | 1.43 | 1100 | 2340914 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 13629.94 | 12.391 | 26.23 | 1462 | 14.07 | 0.07 | 12699 | 0 |
| Gfx.PresentFrame | Render Thread | 9879.00 | 8.981 | 30.49 | 1267 | 256.83 | 0.48 | 1100 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 6976.32 | 6.342 | 15.60 | 1138 | 59.11 | 0.30 | 1100 | 528000 |
| WaitForTargetFPS | Main Thread | 6744.15 | 6.131 | 26.12 | 1263 | 6730.28 | 26.11 | 1100 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 6171.82 | 5.611 | 12.21 | 1462 | 265.73 | 3.79 | 6600 | 959200 |
| SimulationSystemGroup | Main Thread | 4096.77 | 3.724 | 12.22 | 1462 | 2.47 | 0.02 | 1100 | 959200 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 4084.57 | 3.713 | 8.42 | 1462 | 205.89 | 0.54 | 1100 | 959200 |
| LateBehaviourUpdate | Main Thread | 1648.19 | 1.498 | 2.71 | 1462 | 16.12 | 0.11 | 1100 | 0 |
| PresentationSystemGroup | Main Thread | 1632.04 | 1.484 | 4.51 | 1464 | 6.15 | 0.07 | 1100 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 1620.00 | 1.473 | 4.49 | 1464 | 44.22 | 0.65 | 1100 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 1118.97 | 1.017 | 3.55 | 1791 | 2.21 | 0.05 | 1100 | 853714 |
| BehaviourUpdate | Main Thread | 1116.76 | 1.015 | 3.55 | 1791 | 45.81 | 0.22 | 1100 | 853714 |
| Gfx.WaitForPresentOnGfxThread | Main Thread | 955.44 | 0.869 | 18.93 | 1268 | 0.54 | 0.00 | 1100 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 863.37 | 0.785 | 3.38 | 987 | 22.78 | 0.16 | 1100 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 391.53 | 0.356 | 2.63 | 1281 | 85.67 | 0.31 | 1100 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 362.94 | 0.330 | 1.17 | 1823 | 77.51 | 0.93 | 2200 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 273.31 | 0.248 | 0.67 | 1894 | 74.99 | 0.21 | 1100 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 255.43 | 0.232 | 1.53 | 1136 | 60.78 | 0.66 | 1100 | 132000 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 229.07 | 0.208 | 0.56 | 940 | 229.07 | 0.56 | 1100 | 0 |
| Canvas.RenderOverlays | Render Thread | 205.57 | 0.187 | 0.58 | 1131 | 200.34 | 0.57 | 1100 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 193.91 | 0.176 | 0.63 | 1136 | 40.38 | 0.42 | 1100 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 190.82 | 0.173 | 0.50 | 995 | 83.35 | 0.22 | 1100 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 178.81 | 0.163 | 0.64 | 1669 | 61.85 | 0.34 | 1100 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 176.60 | 0.161 | 4.24 | 1182 | 58.89 | 0.42 | 1100 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 164.67 | 0.150 | 0.70 | 923 | 78.69 | 0.20 | 1100 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 164.54 | 0.150 | 0.40 | 902 | 82.46 | 0.18 | 1100 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 152.36 | 0.139 | 0.99 | 1519 | 28.63 | 0.35 | 1100 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 149.06 | 0.136 | 0.43 | 1687 | 22.47 | 0.11 | 1100 | 212000 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 129.58 | 0.118 | 0.32 | 1900 | 56.95 | 0.14 | 1100 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 122.86 | 0.112 | 0.84 | 1917 | 50.38 | 0.28 | 1100 | 0 |
| Canvas.RenderOverlays | Main Thread | 118.45 | 0.108 | 0.26 | 1103 | 62.97 | 0.15 | 1100 | 0 |
| GameplayRuntimeUpdate.RoadBuild | Main Thread | 114.06 | 0.104 | 0.36 | 1464 | 97.12 | 0.34 | 1100 | 44000 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 111.86 | 0.102 | 1.20 | 1464 | 6.59 | 0.38 | 1100 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 106.30 | 0.097 | 0.25 | 1872 | 106.30 | 0.25 | 1100 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 103.01 | 0.094 | 0.31 | 997 | 44.46 | 0.16 | 1100 | 0 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 101.07 | 0.092 | 0.45 | 1462 | 32.52 | 0.15 | 1100 | 0 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 90.80 | 0.083 | 1.11 | 993 | 6.43 | 0.02 | 1100 | 0 |
| Default World Unity.Rendering.LODRequirementsUpdateSystem | Main Thread | 75.37 | 0.069 | 1.09 | 1464 | 74.37 | 1.09 | 1100 | 0 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 72.66 | 0.066 | 0.24 | 902 | 68.05 | 0.24 | 1100 | 88000 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 6739.90 | 6.127 | 26.12 | 1263 | 6727.40 | 26.11 | 895 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 1693.84 | 1.540 | 3.52 | 1138 | 1693.84 | 3.52 | 1100 | 0 |
| Game.Composition.dll!Game.Composition::MatchSceneView.LateUpdate() [Invoke] | Main Thread | 1632.07 | 1.484 | 2.70 | 1462 | 1630.57 | 2.69 | 1100 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 1745.82 | 1.587 | 4.47 | 1464 | 1036.47 | 1.55 | 1100 | 0 |
| WaitForJobGroupID | Main Thread | 2820.75 | 2.564 | 7.00 | 1182 | 1006.61 | 3.67 | 33318 | 0 |
| SRPBatcher.Flush | Main Thread | 1320.70 | 1.201 | 2.64 | 1135 | 1002.08 | 1.98 | 27500 | 0 |
| Semaphore.WaitForSignal | Main Thread | 954.27 | 0.868 | 18.93 | 1268 | 954.27 | 18.93 | 115 | 0 |
| ExecuteRenderQueueJob | Main Thread | 535.63 | 0.487 | 2.06 | 908 | 535.63 | 2.06 | 2856 | 0 |
| JobHandle.Complete | Main Thread | 2062.12 | 1.875 | 6.30 | 1182 | 430.79 | 2.43 | 90863 | 0 |
| SamplePerObjectReflectionProbes | Main Thread | 343.59 | 0.312 | 1.18 | 1135 | 343.59 | 1.18 | 997700 | 0 |
| Inl_On Record Render Graph | Main Thread | 454.73 | 0.413 | 1.83 | 1137 | 338.32 | 1.56 | 1100 | 0 |
| PlayerLoop | Main Thread | 28930.50 | 26.300 | 66.72 | 1263 | 301.91 | 1.43 | 1100 | 2340914 |
| OnPerformCulling | Main Thread | 285.43 | 0.259 | 0.52 | 1703 | 285.43 | 0.52 | 2200 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 6171.82 | 5.611 | 15.12 | 1462 | 265.73 | 4.29 | 6600 | 959200 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 238.24 | 0.217 | 2.38 | 1281 | 238.24 | 2.38 | 1094 | 0 |
| SRPBRender.ApplyShader | Main Thread | 248.07 | 0.226 | 0.76 | 1416 | 235.13 | 0.74 | 23100 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 229.07 | 0.208 | 0.56 | 940 | 229.07 | 0.56 | 1100 | 0 |
| RenderLoop.DrawSRPBatcher | Main Thread | 1668.63 | 1.517 | 3.23 | 1135 | 224.26 | 1.08 | 13200 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 2960.14 | 2.691 | 7.89 | 1138 | 212.65 | 0.72 | 1100 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 4084.57 | 3.713 | 8.42 | 1462 | 205.89 | 0.54 | 1100 | 959200 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 246.20 | 0.224 | 0.42 | 1136 | 200.47 | 0.36 | 1100 | 809600 |
| Batch.DrawInstanced | Main Thread | 186.34 | 0.169 | 1.31 | 1138 | 186.34 | 1.31 | 6600 | 0 |
| CanvasRenderer.SyncTransform | Main Thread | 185.74 | 0.169 | 1.21 | 1643 | 185.74 | 1.21 | 409200 | 0 |
| RenderLoop.CleanupNodeQueue | Main Thread | 169.74 | 0.154 | 0.65 | 1841 | 169.74 | 0.65 | 6600 | 0 |
| ClipperRegistry.Cull | Main Thread | 158.89 | 0.144 | 0.30 | 991 | 158.89 | 0.30 | 1100 | 0 |
| Game.UI.Runtime.dll!Game.UI.Runtime::UIShellLoadingProgressView.Update() [Invoke] | Main Thread | 147.60 | 0.134 | 0.36 | 1845 | 147.37 | 0.36 | 1100 | 52800 |
| Inl_RenderCameraStack | Main Thread | 6825.78 | 6.205 | 15.43 | 1138 | 139.33 | 0.26 | 1100 | 528000 |
| ThreatDetectionWarningSystem:ThreatScanJob (Burst) | Main Thread | 130.02 | 0.118 | 1.39 | 911 | 130.02 | 1.39 | 138 | 0 |
| FactionVisualSystem:UpdateSnivelerTintJob (Burst) | Main Thread | 129.41 | 0.118 | 0.30 | 1540 | 129.41 | 0.30 | 1032 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 127.31 | 0.116 | 0.26 | 1366 | 126.68 | 0.25 | 1100 | 167200 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 13615.88 | 12.378 | 27.23 | 1462 | 13615.88 | 27.23 | 12699 | 0 |
| GfxDeviceVK.Present | Render Thread | 9399.32 | 8.545 | 29.65 | 1267 | 9399.32 | 29.65 | 1100 | 0 |
| DrawBuffersBatchMode | Render Thread | 3261.19 | 2.965 | 7.89 | 1353 | 3261.19 | 7.89 | 27500 | 0 |
| RenderLoop | Render Thread | 15452.44 | 14.048 | 37.87 | 1267 | 530.02 | 1.76 | 13326 | 0 |
| ExecuteRenderGraph | Render Thread | 4852.57 | 4.411 | 10.23 | 1353 | 413.29 | 1.41 | 1100 | 0 |
| RenderLoop.Draw | Render Thread | 293.40 | 0.267 | 3.02 | 986 | 293.31 | 3.02 | 5500 | 0 |
| AcquireNextFrame | Render Thread | 271.94 | 0.247 | 0.67 | 1909 | 271.94 | 0.67 | 1100 | 0 |
| Gfx.PresentFrame | Render Thread | 9879.00 | 8.981 | 30.49 | 1267 | 256.83 | 0.48 | 1100 | 0 |
| Canvas.RenderOverlays | Render Thread | 205.57 | 0.187 | 0.58 | 1131 | 200.34 | 0.57 | 1100 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 2922.38 | 2.657 | 7.57 | 1353 | 190.59 | 0.60 | 13200 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 182.72 | 0.166 | 0.32 | 1462 | 182.72 | 0.32 | 2200 | 0 |
| Gfx.SetRenderTarget | Render Thread | 53.14 | 0.048 | 1.19 | 1147 | 53.14 | 1.19 | 4473 | 0 |
| BlitFinalToBackBuffer | Render Thread | 46.04 | 0.042 | 0.11 | 1403 | 46.04 | 0.11 | 1100 | 0 |
| GpuRecorder.FrameTick | Render Thread | 40.12 | 0.036 | 0.19 | 1891 | 40.12 | 0.19 | 1100 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 33.42 | 0.030 | 0.71 | 1702 | 33.42 | 0.71 | 73 | 0 |
| ScheduleGeometryJobs | Render Thread | 33.19 | 0.030 | 2.00 | 1146 | 33.19 | 2.00 | 1100 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 587.66 | 0.534 | 1.42 | 986 | 29.11 | 0.53 | 1100 | 0 |
| UI.RenderOverlays | Render Thread | 227.45 | 0.207 | 0.62 | 1131 | 21.88 | 0.09 | 1100 | 0 |
| Profiler.FlushRenderCounters | Render Thread | 21.55 | 0.020 | 0.11 | 1407 | 21.55 | 0.11 | 1100 | 0 |
| Gfx.DrawDynamic | Render Thread | 19.65 | 0.018 | 0.13 | 1268 | 19.65 | 0.13 | 1173 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 1263 | 66.84 | 22.92 | 22.92 | 6.56 | 38.74 | 1840 |
| 1268 | 66.40 | 25.15 | 25.15 | 7.56 | 29.45 | 1840 |
| 1195 | 58.78 | 19.23 | 19.23 | 5.51 | 0.00 | 1870 |
| 1129 | 57.81 | 18.55 | 18.55 | 5.49 | 0.00 | 2120 |
| 1180 | 57.51 | 17.96 | 17.96 | 5.18 | 34.51 | 2080 |
| 1355 | 57.21 | 19.17 | 19.17 | 5.63 | 35.34 | 2376 |
| 1257 | 57.16 | 20.82 | 20.82 | 6.42 | 39.16 | 1840 |
| 1283 | 57.06 | 17.86 | 17.86 | 5.22 | 29.45 | 2080 |
| 1274 | 56.81 | 18.39 | 18.39 | 5.03 | 34.43 | 2366 |
| 1174 | 55.59 | 17.60 | 17.60 | 5.13 | 34.07 | 1840 |
| 1449 | 55.25 | 20.56 | 20.56 | 5.61 | 31.62 | 2576 |
| 1200 | 55.07 | 18.15 | 18.15 | 5.10 | 29.07 | 2376 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
