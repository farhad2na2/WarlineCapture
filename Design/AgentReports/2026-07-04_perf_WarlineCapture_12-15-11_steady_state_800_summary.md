# Android Profiler Capture Summary

Date: 2026-07-04 12:21:19 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-04_12-15-11.data`
Profiler frames: `800..1999`
Scanned frames: `1200`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 23.09 ms (43.3 FPS) |
| P50 frame | 22.17 ms (45.1 FPS) |
| P95 frame | 30.79 ms (32.5 FPS) |
| P99 frame | 39.42 ms (25.4 FPS) |
| Max frame | 56.42 ms (17.7 FPS) |
| Frames over budget | 1175/1200 |
| Avg CPU active | 17.69 ms |
| P95 CPU active | 17.11 ms |
| Avg GPU time | 19.91 ms |
| P95 GPU time | 22.28 ms |
| Total GC allocated | 2468058 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 27626.47 | 23.022 | 56.36 | 824 | 288.56 | 1.55 | 1200 | 2468058 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 13561.09 | 11.301 | 21.87 | 1801 | 14.92 | 0.13 | 13617 | 0 |
| Gfx.PresentFrame | Render Thread | 8724.85 | 7.271 | 28.32 | 822 | 262.66 | 9.75 | 1200 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 7344.61 | 6.121 | 14.27 | 1076 | 80.52 | 0.58 | 1200 | 576000 |
| WaitForTargetFPS | Main Thread | 6061.03 | 5.051 | 21.64 | 801 | 6042.94 | 21.63 | 1200 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 5931.81 | 4.943 | 7.43 | 1319 | 258.04 | 0.38 | 7200 | 1046400 |
| SimulationSystemGroup | Main Thread | 3936.32 | 3.280 | 7.43 | 1319 | 2.19 | 0.01 | 1200 | 1046400 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 3929.09 | 3.274 | 7.42 | 1319 | 196.27 | 0.35 | 1200 | 1046400 |
| PresentationSystemGroup | Main Thread | 1596.00 | 1.330 | 3.60 | 824 | 7.62 | 0.86 | 1200 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 1581.80 | 1.318 | 3.57 | 824 | 43.31 | 0.51 | 1200 | 0 |
| LateBehaviourUpdate | Main Thread | 1390.48 | 1.159 | 3.20 | 1554 | 18.78 | 0.12 | 1200 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 1310.53 | 1.092 | 3.14 | 1554 | 1309.39 | 3.14 | 1200 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 814.93 | 0.679 | 2.84 | 1416 | 24.11 | 0.17 | 1200 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 795.81 | 0.663 | 2.25 | 1555 | 1.73 | 0.01 | 1200 | 845658 |
| BehaviourUpdate | Main Thread | 794.08 | 0.662 | 2.25 | 1555 | 35.61 | 0.51 | 1200 | 845658 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 430.01 | 0.358 | 0.84 | 1823 | 83.26 | 0.23 | 2400 | 0 |
| Gfx.WaitForPresentOnGfxThread | Main Thread | 427.01 | 0.356 | 20.18 | 823 | 0.38 | 0.00 | 1200 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 357.17 | 0.298 | 2.37 | 1621 | 85.15 | 0.29 | 1200 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 334.11 | 0.278 | 1.00 | 1977 | 83.59 | 0.15 | 1200 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 198.77 | 0.166 | 1.09 | 1439 | 49.48 | 0.61 | 1200 | 144000 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 195.17 | 0.163 | 0.30 | 1660 | 195.17 | 0.30 | 1200 | 0 |
| Canvas.RenderOverlays | Render Thread | 190.45 | 0.159 | 0.29 | 948 | 185.65 | 0.28 | 1200 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 182.49 | 0.152 | 0.66 | 1153 | 83.87 | 0.20 | 1200 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 171.63 | 0.143 | 3.40 | 1319 | 63.16 | 0.21 | 1200 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 163.84 | 0.137 | 1.73 | 1863 | 80.72 | 0.25 | 1200 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 156.03 | 0.130 | 1.06 | 1802 | 30.10 | 0.30 | 1200 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 155.97 | 0.130 | 0.34 | 1517 | 26.49 | 0.18 | 1200 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 150.53 | 0.125 | 0.48 | 1802 | 51.14 | 0.11 | 1200 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 149.62 | 0.125 | 0.45 | 1126 | 78.10 | 0.17 | 1200 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 129.49 | 0.108 | 1.04 | 1951 | 63.08 | 0.17 | 1200 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 119.72 | 0.100 | 0.51 | 1952 | 18.17 | 0.06 | 1200 | 218016 |
| Canvas.RenderOverlays | Main Thread | 118.62 | 0.099 | 0.22 | 1952 | 65.67 | 0.17 | 1200 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 115.66 | 0.096 | 0.99 | 1723 | 7.69 | 0.87 | 1200 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 115.19 | 0.096 | 1.74 | 1585 | 50.68 | 0.18 | 1200 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 110.17 | 0.092 | 0.25 | 1151 | 110.17 | 0.25 | 1200 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 107.18 | 0.089 | 0.30 | 1802 | 49.93 | 0.15 | 1200 | 0 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 90.06 | 0.075 | 0.26 | 826 | 30.38 | 0.12 | 1200 | 0 |
| GameplayRuntimeUpdate.RoadBuild | Main Thread | 88.22 | 0.074 | 0.20 | 1315 | 74.61 | 0.18 | 1200 | 48000 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 83.05 | 0.069 | 0.94 | 824 | 5.74 | 0.07 | 1200 | 0 |
| Default World Unity.Rendering.LODRequirementsUpdateSystem | Main Thread | 77.79 | 0.065 | 0.45 | 1208 | 76.98 | 0.45 | 1200 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 6057.57 | 5.048 | 21.64 | 801 | 6040.58 | 21.63 | 1019 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 1803.71 | 1.503 | 2.66 | 1525 | 1803.71 | 2.66 | 1200 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 1310.53 | 1.092 | 3.14 | 1554 | 1309.39 | 3.14 | 1200 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 1972.84 | 1.644 | 2.71 | 1285 | 1210.88 | 1.85 | 1200 | 0 |
| SRPBatcher.Flush | Main Thread | 1251.49 | 1.043 | 3.38 | 1801 | 987.45 | 3.05 | 30000 | 0 |
| WaitForJobGroupID | Main Thread | 2718.44 | 2.265 | 10.84 | 1076 | 881.71 | 2.68 | 36463 | 0 |
| ExecuteRenderQueueJob | Main Thread | 633.20 | 0.528 | 1.94 | 1701 | 633.20 | 1.94 | 2990 | 0 |
| Semaphore.WaitForSignal | Main Thread | 425.48 | 0.355 | 20.17 | 823 | 425.48 | 20.17 | 74 | 0 |
| Inl_On Record Render Graph | Main Thread | 515.14 | 0.429 | 1.89 | 1910 | 379.86 | 1.75 | 1200 | 0 |
| JobHandle.Complete | Main Thread | 1848.85 | 1.541 | 4.91 | 1319 | 379.43 | 2.23 | 99048 | 0 |
| OnPerformCulling | Main Thread | 346.75 | 0.289 | 0.93 | 1823 | 346.75 | 0.93 | 2400 | 0 |
| PlayerLoop | Main Thread | 27626.47 | 23.022 | 56.36 | 824 | 288.56 | 1.55 | 1200 | 2468058 |
| SamplePerObjectReflectionProbes | Main Thread | 286.55 | 0.239 | 0.57 | 1409 | 286.55 | 0.57 | 1088400 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 5931.81 | 4.943 | 10.36 | 824 | 258.04 | 0.48 | 7200 | 1046400 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 3339.47 | 2.783 | 11.06 | 1076 | 254.08 | 0.53 | 1200 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 268.97 | 0.224 | 0.47 | 1802 | 227.73 | 0.40 | 1200 | 883200 |
| SRPBRender.ApplyShader | Main Thread | 235.39 | 0.196 | 0.63 | 1258 | 225.10 | 0.62 | 25200 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 210.76 | 0.176 | 1.95 | 1614 | 210.76 | 1.95 | 1194 | 0 |
| ClipperRegistry.Cull | Main Thread | 209.32 | 0.174 | 0.58 | 1569 | 209.32 | 0.58 | 1200 | 0 |
| RenderLoop.DrawSRPBatcher | Main Thread | 1559.25 | 1.299 | 4.15 | 1801 | 201.45 | 0.66 | 14400 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 3929.09 | 3.274 | 7.42 | 1319 | 196.27 | 0.35 | 1200 | 1046400 |
| CanvasRenderer.SyncTransform | Main Thread | 195.21 | 0.163 | 0.36 | 1962 | 195.21 | 0.36 | 446400 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 195.17 | 0.163 | 0.30 | 1660 | 195.17 | 0.30 | 1200 | 0 |
| RenderLoop.CleanupNodeQueue | Main Thread | 182.19 | 0.152 | 0.70 | 991 | 182.19 | 0.70 | 7200 | 0 |
| Inl_RenderCameraStack | Main Thread | 7140.92 | 5.951 | 14.06 | 1076 | 181.44 | 0.44 | 1200 | 576000 |
| Batch.DrawInstanced | Main Thread | 170.47 | 0.142 | 0.44 | 1802 | 170.47 | 0.44 | 7200 | 0 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 8218.02 | 6.848 | 31.55 | 823 | 149.98 | 1.07 | 1200 | 576000 |
| RegisterMaterialsAndMeshes | Main Thread | 124.65 | 0.104 | 0.21 | 1823 | 124.65 | 0.21 | 1172 | 0 |
| UpdateAllBatches | Main Thread | 432.41 | 0.360 | 0.78 | 824 | 117.62 | 0.23 | 1191 | 0 |
| TransformChangeSystem | Main Thread | 143.57 | 0.120 | 1.35 | 1505 | 113.82 | 1.32 | 23862 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 13546.17 | 11.288 | 21.85 | 1801 | 13546.17 | 21.85 | 13617 | 0 |
| GfxDeviceVK.Present | Render Thread | 8235.91 | 6.863 | 27.84 | 822 | 8235.91 | 27.84 | 1200 | 0 |
| DrawBuffersBatchMode | Render Thread | 3179.79 | 2.650 | 7.27 | 1529 | 3179.79 | 7.27 | 30000 | 0 |
| RenderLoop | Render Thread | 14208.69 | 11.841 | 33.63 | 822 | 521.15 | 6.94 | 14056 | 0 |
| ExecuteRenderGraph | Render Thread | 4764.84 | 3.971 | 9.38 | 823 | 408.04 | 0.92 | 1200 | 0 |
| RenderLoop.Draw | Render Thread | 275.25 | 0.229 | 0.86 | 1531 | 275.13 | 0.86 | 6000 | 0 |
| AcquireNextFrame | Render Thread | 263.75 | 0.220 | 3.11 | 823 | 263.75 | 3.11 | 1200 | 0 |
| Gfx.PresentFrame | Render Thread | 8724.85 | 7.271 | 28.32 | 822 | 262.66 | 9.75 | 1200 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 2840.39 | 2.367 | 6.90 | 1529 | 201.17 | 0.41 | 14400 | 0 |
| Canvas.RenderOverlays | Render Thread | 190.45 | 0.159 | 0.29 | 948 | 185.65 | 0.28 | 1200 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 181.38 | 0.151 | 0.36 | 1719 | 181.38 | 0.36 | 2400 | 0 |
| Gfx.SetRenderTarget | Render Thread | 59.05 | 0.049 | 0.53 | 1695 | 59.05 | 0.53 | 4880 | 0 |
| BlitFinalToBackBuffer | Render Thread | 48.28 | 0.040 | 0.22 | 1517 | 48.28 | 0.22 | 1200 | 0 |
| GpuRecorder.FrameTick | Render Thread | 44.89 | 0.037 | 0.21 | 1678 | 44.89 | 0.21 | 1200 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 35.90 | 0.030 | 1.02 | 1775 | 35.90 | 1.02 | 80 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 613.39 | 0.511 | 1.65 | 1581 | 33.36 | 0.14 | 1200 | 0 |
| ScheduleGeometryJobs | Render Thread | 30.70 | 0.026 | 1.07 | 826 | 30.70 | 1.07 | 1200 | 0 |
| UI.RenderOverlays | Render Thread | 213.75 | 0.178 | 0.35 | 1144 | 23.30 | 0.09 | 1200 | 0 |
| Gfx.DrawDynamic | Render Thread | 20.25 | 0.017 | 0.10 | 1910 | 20.25 | 0.10 | 1280 | 0 |
| Setup Camera Properties | Render Thread | 36.25 | 0.030 | 0.51 | 1607 | 18.83 | 0.11 | 2400 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 824 | 56.42 | 28.28 | 28.28 | 6.56 | 22.81 | 1832 |
| 805 | 51.98 | 15.95 | 15.95 | 5.18 | 27.13 | 1792 |
| 801 | 50.47 | 15.74 | 15.74 | 5.13 | 32.94 | 1792 |
| 816 | 50.09 | 17.16 | 17.16 | 5.32 | 27.23 | 1832 |
| 812 | 46.05 | 15.83 | 15.83 | 5.16 | 32.72 | 1832 |
| 868 | 43.75 | 16.11 | 16.11 | 4.86 | 20.70 | 1792 |
| 819 | 43.21 | 16.48 | 16.48 | 5.23 | 24.71 | 1792 |
| 823 | 42.95 | 22.75 | 22.75 | 9.93 | 34.00 | 1792 |
| 807 | 42.86 | 15.84 | 15.84 | 4.82 | 30.23 | 2288 |
| 877 | 42.39 | 18.36 | 18.36 | 5.65 | 31.83 | 1792 |
| 880 | 39.47 | 17.31 | 17.31 | 6.16 | 21.58 | 1792 |
| 1543 | 39.42 | 18.75 | 18.75 | 5.58 | 21.73 | 1832 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
