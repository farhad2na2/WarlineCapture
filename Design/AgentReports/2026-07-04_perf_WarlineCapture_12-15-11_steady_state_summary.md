# Android Profiler Capture Summary

Date: 2026-07-04 12:19:45 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-04_12-15-11.data`
Profiler frames: `300..1999`
Scanned frames: `1700`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 24.71 ms (40.5 FPS) |
| P50 frame | 22.43 ms (44.6 FPS) |
| P95 frame | 39.06 ms (25.6 FPS) |
| P99 frame | 50.24 ms (19.9 FPS) |
| Max frame | 68.27 ms (14.6 FPS) |
| Frames over budget | 1634/1700 |
| Avg CPU active | 17.61 ms |
| P95 CPU active | 18.14 ms |
| Avg GPU time | 20.41 ms |
| P95 GPU time | 0.00 ms |
| Total GC allocated | 3523148 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 41879.06 | 24.635 | 68.18 | 336 | 414.31 | 1.55 | 1700 | 3523148 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 17724.65 | 10.426 | 27.87 | 439 | 20.15 | 0.13 | 18544 | 0 |
| Gfx.PresentFrame | Render Thread | 16237.03 | 9.551 | 34.32 | 711 | 415.05 | 19.21 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 10563.58 | 6.214 | 17.70 | 439 | 108.91 | 0.58 | 1700 | 816000 |
| WaitForTargetFPS | Main Thread | 9686.68 | 5.698 | 29.71 | 437 | 9665.18 | 29.70 | 1700 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 8157.16 | 4.798 | 7.45 | 439 | 376.81 | 0.38 | 10200 | 1482400 |
| SimulationSystemGroup | Main Thread | 5385.03 | 3.168 | 7.45 | 439 | 3.04 | 0.01 | 1700 | 1482400 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 5375.14 | 3.162 | 7.44 | 439 | 267.55 | 0.61 | 1700 | 1482400 |
| Gfx.WaitForPresentOnGfxThread | Main Thread | 2389.26 | 1.405 | 24.86 | 712 | 0.65 | 0.00 | 1700 | 0 |
| PresentationSystemGroup | Main Thread | 2175.86 | 1.280 | 4.44 | 440 | 9.61 | 0.86 | 1700 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 2157.64 | 1.269 | 4.42 | 440 | 57.19 | 0.51 | 1700 | 0 |
| LateBehaviourUpdate | Main Thread | 1924.43 | 1.132 | 3.20 | 1554 | 24.72 | 0.12 | 1700 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 1817.61 | 1.069 | 3.14 | 1554 | 1816.01 | 3.14 | 1700 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 1178.15 | 0.693 | 5.69 | 784 | 2.48 | 0.01 | 1700 | 1224748 |
| BehaviourUpdate | Main Thread | 1175.67 | 0.692 | 5.69 | 784 | 51.81 | 0.51 | 1700 | 1224748 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 1128.18 | 0.664 | 2.84 | 1416 | 32.85 | 0.17 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 599.51 | 0.353 | 0.86 | 439 | 121.15 | 0.65 | 3400 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 490.08 | 0.288 | 2.37 | 1621 | 115.27 | 0.29 | 1700 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 449.54 | 0.264 | 1.00 | 1977 | 110.55 | 0.15 | 1700 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 297.33 | 0.175 | 1.09 | 1439 | 73.80 | 0.61 | 1700 | 206392 |
| Canvas.RenderOverlays | Render Thread | 278.89 | 0.164 | 0.31 | 772 | 271.74 | 0.30 | 1700 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 271.55 | 0.160 | 0.30 | 1660 | 271.55 | 0.30 | 1700 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 248.08 | 0.146 | 0.66 | 1153 | 113.10 | 0.22 | 1700 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 237.66 | 0.140 | 2.40 | 440 | 45.16 | 0.30 | 1700 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 235.46 | 0.139 | 3.40 | 1319 | 85.27 | 0.21 | 1700 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 224.92 | 0.132 | 1.73 | 1863 | 110.74 | 0.25 | 1700 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 212.08 | 0.125 | 1.29 | 510 | 70.88 | 0.21 | 1700 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 211.31 | 0.124 | 1.11 | 437 | 36.72 | 0.20 | 1700 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 208.75 | 0.123 | 0.45 | 1126 | 108.71 | 0.22 | 1700 | 0 |
| Canvas.RenderOverlays | Main Thread | 175.06 | 0.103 | 0.52 | 481 | 97.80 | 0.24 | 1700 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 174.86 | 0.103 | 1.04 | 1951 | 82.45 | 0.17 | 1700 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 171.96 | 0.101 | 0.51 | 1952 | 26.02 | 0.09 | 1700 | 318032 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 157.11 | 0.092 | 1.74 | 1585 | 67.33 | 0.18 | 1700 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 154.44 | 0.091 | 0.99 | 1723 | 9.89 | 0.87 | 1700 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 148.00 | 0.087 | 0.54 | 439 | 148.00 | 0.54 | 1700 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 144.51 | 0.085 | 2.05 | 437 | 65.04 | 0.19 | 1700 | 0 |
| GameplayRuntimeUpdate.RoadBuild | Main Thread | 128.89 | 0.076 | 0.36 | 456 | 109.26 | 0.20 | 1700 | 68000 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 127.12 | 0.075 | 0.36 | 440 | 42.14 | 0.12 | 1700 | 0 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 115.28 | 0.068 | 1.03 | 440 | 7.95 | 0.07 | 1700 | 0 |
| Default World Unity.Rendering.LODRequirementsUpdateSystem | Main Thread | 103.95 | 0.061 | 0.45 | 1208 | 102.76 | 0.45 | 1700 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 9679.03 | 5.694 | 29.71 | 437 | 9659.95 | 29.70 | 1308 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 2643.95 | 1.555 | 4.51 | 439 | 2643.95 | 4.51 | 1700 | 0 |
| Semaphore.WaitForSignal | Main Thread | 2387.15 | 1.404 | 24.85 | 712 | 2387.15 | 24.85 | 268 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 1817.61 | 1.069 | 3.14 | 1554 | 1816.01 | 3.14 | 1700 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 2630.59 | 1.547 | 4.58 | 439 | 1600.70 | 2.03 | 1700 | 0 |
| SRPBatcher.Flush | Main Thread | 1821.49 | 1.071 | 3.38 | 1801 | 1441.11 | 3.05 | 42500 | 0 |
| WaitForJobGroupID | Main Thread | 3858.77 | 2.270 | 10.84 | 1076 | 1242.69 | 2.68 | 51617 | 0 |
| ExecuteRenderQueueJob | Main Thread | 910.91 | 0.536 | 1.94 | 1701 | 910.91 | 1.94 | 4257 | 0 |
| JobHandle.Complete | Main Thread | 2598.88 | 1.529 | 4.91 | 1319 | 545.24 | 4.21 | 140371 | 0 |
| Inl_On Record Render Graph | Main Thread | 733.87 | 0.432 | 1.89 | 1910 | 540.50 | 1.75 | 1700 | 0 |
| OnPerformCulling | Main Thread | 478.36 | 0.281 | 0.93 | 1823 | 478.36 | 0.93 | 3400 | 0 |
| PlayerLoop | Main Thread | 41879.06 | 24.635 | 68.18 | 336 | 414.30 | 1.55 | 1700 | 3523148 |
| SamplePerObjectReflectionProbes | Main Thread | 412.49 | 0.243 | 1.23 | 313 | 412.49 | 1.23 | 1541900 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 8157.16 | 4.798 | 14.07 | 440 | 376.81 | 0.48 | 10200 | 1482400 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 4768.41 | 2.805 | 11.06 | 1076 | 358.20 | 0.83 | 1700 | 0 |
| SRPBRender.ApplyShader | Main Thread | 342.74 | 0.202 | 0.63 | 1258 | 327.76 | 0.62 | 35700 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 358.65 | 0.211 | 0.47 | 1802 | 300.78 | 0.40 | 1700 | 1251200 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 291.47 | 0.171 | 1.95 | 1614 | 291.47 | 1.95 | 1691 | 0 |
| RenderLoop.DrawSRPBatcher | Main Thread | 2264.80 | 1.332 | 4.15 | 1801 | 291.13 | 0.66 | 20400 | 0 |
| ClipperRegistry.Cull | Main Thread | 281.69 | 0.166 | 0.66 | 585 | 281.69 | 0.66 | 1700 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 271.55 | 0.160 | 0.30 | 1660 | 271.55 | 0.30 | 1700 | 0 |
| RenderLoop.CleanupNodeQueue | Main Thread | 269.24 | 0.158 | 0.70 | 991 | 269.24 | 0.70 | 10200 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 5375.14 | 3.162 | 7.44 | 439 | 267.55 | 0.61 | 1700 | 1482400 |
| CanvasRenderer.SyncTransform | Main Thread | 259.05 | 0.152 | 0.36 | 1962 | 259.05 | 0.36 | 632400 | 0 |
| Batch.DrawInstanced | Main Thread | 254.08 | 0.149 | 1.74 | 439 | 254.08 | 1.74 | 10200 | 0 |
| Inl_RenderCameraStack | Main Thread | 10287.80 | 6.052 | 17.51 | 439 | 249.50 | 0.46 | 1700 | 816000 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 13598.08 | 7.999 | 32.21 | 712 | 213.82 | 1.07 | 1700 | 816000 |
| RegisterMaterialsAndMeshes | Main Thread | 168.05 | 0.099 | 0.90 | 437 | 168.05 | 0.90 | 1668 | 0 |
| RenderLoop.Draw | Main Thread | 615.90 | 0.362 | 2.48 | 439 | 164.93 | 0.44 | 8500 | 0 |
| Inl_ScriptableRenderContext.Submit | Main Thread | 4911.81 | 2.889 | 8.95 | 439 | 159.00 | 0.46 | 1700 | 816000 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 17704.50 | 10.414 | 28.29 | 439 | 17704.50 | 28.29 | 18544 | 0 |
| GfxDeviceVK.Present | Render Thread | 15493.49 | 9.114 | 33.92 | 711 | 15493.49 | 33.92 | 1700 | 0 |
| DrawBuffersBatchMode | Render Thread | 4748.96 | 2.794 | 7.27 | 1529 | 4748.96 | 7.27 | 42500 | 0 |
| RenderLoop | Render Thread | 24375.63 | 14.339 | 39.78 | 711 | 719.83 | 6.94 | 19102 | 0 |
| ExecuteRenderGraph | Render Thread | 7127.51 | 4.193 | 9.41 | 439 | 638.22 | 1.21 | 1700 | 0 |
| Gfx.PresentFrame | Render Thread | 16237.03 | 9.551 | 34.32 | 711 | 415.05 | 19.21 | 1700 | 0 |
| RenderLoop.Draw | Render Thread | 402.30 | 0.237 | 0.86 | 1531 | 402.12 | 0.86 | 8500 | 0 |
| AcquireNextFrame | Render Thread | 384.29 | 0.226 | 3.11 | 823 | 384.29 | 3.11 | 1700 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 4257.25 | 2.504 | 6.90 | 1529 | 302.85 | 0.55 | 20400 | 0 |
| Canvas.RenderOverlays | Render Thread | 278.89 | 0.164 | 0.31 | 772 | 271.74 | 0.30 | 1700 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 264.03 | 0.155 | 0.36 | 1719 | 264.03 | 0.36 | 3400 | 0 |
| Gfx.SetRenderTarget | Render Thread | 84.55 | 0.050 | 0.65 | 436 | 84.55 | 0.65 | 6913 | 0 |
| BlitFinalToBackBuffer | Render Thread | 71.35 | 0.042 | 0.22 | 1517 | 71.35 | 0.22 | 1700 | 0 |
| GpuRecorder.FrameTick | Render Thread | 64.46 | 0.038 | 0.21 | 1678 | 64.46 | 0.21 | 1700 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 53.15 | 0.031 | 1.33 | 335 | 53.15 | 1.33 | 113 | 0 |
| ScheduleGeometryJobs | Render Thread | 51.09 | 0.030 | 4.01 | 336 | 51.09 | 4.01 | 1700 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 903.13 | 0.531 | 1.97 | 315 | 49.80 | 0.16 | 1700 | 0 |
| UI.RenderOverlays | Render Thread | 313.12 | 0.184 | 0.35 | 1144 | 34.23 | 0.09 | 1700 | 0 |
| Gfx.DrawDynamic | Render Thread | 30.16 | 0.018 | 0.10 | 1910 | 30.16 | 0.10 | 1813 | 0 |
| Setup Camera Properties | Render Thread | 53.09 | 0.031 | 0.51 | 1607 | 27.65 | 0.11 | 3400 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 336 | 68.27 | 25.16 | 25.16 | 11.62 | 38.59 | 2080 |
| 411 | 66.94 | 22.44 | 22.44 | 7.80 | 30.09 | 1792 |
| 437 | 63.65 | 31.86 | 31.86 | 6.60 | 0.00 | 2042 |
| 712 | 63.01 | 18.99 | 18.99 | 4.94 | 30.78 | 2288 |
| 339 | 62.57 | 24.06 | 24.06 | 6.79 | 30.43 | 1822 |
| 731 | 61.05 | 24.06 | 24.06 | 7.00 | 36.49 | 2032 |
| 824 | 56.42 | 28.28 | 28.28 | 6.56 | 22.81 | 1832 |
| 528 | 52.90 | 17.09 | 17.09 | 5.38 | 30.32 | 2328 |
| 519 | 52.81 | 18.59 | 18.59 | 5.21 | 31.07 | 1792 |
| 508 | 52.64 | 16.60 | 16.60 | 5.07 | 33.83 | 2062 |
| 606 | 52.36 | 16.54 | 16.54 | 5.00 | 30.12 | 1792 |
| 502 | 52.32 | 18.19 | 18.19 | 5.07 | 32.53 | 1832 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
