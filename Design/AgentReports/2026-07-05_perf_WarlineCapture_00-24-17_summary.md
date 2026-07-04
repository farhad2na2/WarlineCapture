# Android Profiler Capture Summary

Date: 2026-07-05 00:27:12 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-05_00-24-17.data`
Profiler frames: `0..1999`
Scanned frames: `2000`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 23.15 ms (43.2 FPS) |
| P50 frame | 21.91 ms (45.6 FPS) |
| P95 frame | 30.19 ms (33.1 FPS) |
| P99 frame | 34.07 ms (29.4 FPS) |
| Max frame | 47.03 ms (21.3 FPS) |
| Frames over budget | 1994/2000 |
| Avg CPU active | 17.84 ms |
| P95 CPU active | 16.77 ms |
| Avg GPU time | 21.82 ms |
| P95 GPU time | 22.23 ms |
| Total GC allocated | 4118494 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 46166.37 | 23.083 | 46.92 | 200 | 514.27 | 1.30 | 2000 | 4118494 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 23897.31 | 11.949 | 32.77 | 716 | 27.17 | 0.68 | 26128 | 0 |
| Gfx.PresentFrame | Render Thread | 13795.50 | 6.898 | 32.56 | 934 | 370.44 | 0.80 | 2000 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 11877.82 | 5.939 | 13.95 | 1518 | 124.36 | 0.27 | 2000 | 960000 |
| WaitForTargetFPS | Main Thread | 10515.61 | 5.258 | 20.23 | 394 | 10490.51 | 20.22 | 2000 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 9891.56 | 4.946 | 13.23 | 715 | 429.74 | 0.36 | 12000 | 1744000 |
| SimulationSystemGroup | Main Thread | 6467.50 | 3.234 | 13.24 | 715 | 3.66 | 0.04 | 2000 | 1744000 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 6455.46 | 3.228 | 13.22 | 715 | 329.65 | 0.59 | 2000 | 1744000 |
| PresentationSystemGroup | Main Thread | 2745.40 | 1.373 | 4.61 | 715 | 12.59 | 0.06 | 2000 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 2722.50 | 1.361 | 4.59 | 715 | 73.11 | 0.14 | 2000 | 0 |
| LateBehaviourUpdate | Main Thread | 2359.67 | 1.180 | 9.22 | 716 | 31.30 | 0.10 | 2000 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 2228.36 | 1.114 | 9.03 | 716 | 2225.99 | 9.02 | 2000 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 1409.32 | 0.705 | 2.92 | 53 | 44.25 | 0.43 | 2000 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 1375.21 | 0.688 | 3.55 | 1356 | 3.14 | 0.05 | 2000 | 1414494 |
| BehaviourUpdate | Main Thread | 1372.07 | 0.686 | 3.55 | 1356 | 64.90 | 1.23 | 2000 | 1414494 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 694.43 | 0.347 | 1.03 | 715 | 134.11 | 0.93 | 4000 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 587.82 | 0.294 | 5.75 | 1078 | 142.60 | 1.70 | 2000 | 0 |
| Canvas.RenderOverlays | Render Thread | 578.69 | 0.289 | 2.01 | 238 | 333.92 | 1.93 | 10000 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 548.70 | 0.274 | 0.71 | 1044 | 138.39 | 0.16 | 2000 | 0 |
| Canvas.RenderOverlays | Main Thread | 387.87 | 0.194 | 0.27 | 1140 | 133.58 | 0.18 | 10000 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 336.99 | 0.168 | 0.85 | 716 | 75.85 | 0.48 | 2000 | 243328 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 316.69 | 0.158 | 0.48 | 715 | 316.69 | 0.48 | 2000 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 314.74 | 0.157 | 1.23 | 1778 | 146.07 | 0.30 | 2000 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 293.03 | 0.147 | 1.91 | 1498 | 108.72 | 0.22 | 2000 | 0 |
| Canvas.BuildBatch | Main Thread | 272.20 | 0.136 | 2.20 | 718 | 272.20 | 2.20 | 10000 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 271.33 | 0.136 | 4.03 | 1519 | 51.56 | 0.33 | 2000 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 265.36 | 0.133 | 0.70 | 716 | 133.03 | 0.43 | 2000 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 264.21 | 0.132 | 0.56 | 1173 | 45.35 | 0.17 | 2000 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 259.93 | 0.130 | 1.85 | 1851 | 86.80 | 0.12 | 2000 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 243.42 | 0.122 | 0.60 | 715 | 127.47 | 0.47 | 2000 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 208.39 | 0.104 | 0.30 | 811 | 100.86 | 0.15 | 2000 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 197.22 | 0.099 | 1.01 | 1451 | 10.87 | 0.05 | 2000 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 196.84 | 0.098 | 0.45 | 716 | 30.58 | 0.07 | 2000 | 366336 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 188.08 | 0.094 | 2.45 | 445 | 82.13 | 0.20 | 2000 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 175.09 | 0.088 | 0.21 | 1765 | 175.09 | 0.21 | 2000 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 170.70 | 0.085 | 4.05 | 1519 | 75.69 | 0.13 | 2000 | 0 |
| GameplayRuntimeUpdate.RoadBuild | Main Thread | 154.23 | 0.077 | 0.41 | 1519 | 130.32 | 0.38 | 2000 | 80000 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 144.38 | 0.072 | 3.60 | 715 | 46.92 | 0.12 | 2000 | 0 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 140.23 | 0.070 | 1.14 | 717 | 9.24 | 0.06 | 2000 | 0 |
| Default World Unity.Rendering.LODRequirementsUpdateSystem | Main Thread | 132.69 | 0.066 | 0.94 | 1451 | 131.39 | 0.94 | 2000 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 10514.31 | 5.257 | 20.23 | 394 | 10489.65 | 20.22 | 1937 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 3079.28 | 1.540 | 8.31 | 1518 | 3079.28 | 8.31 | 2000 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 3683.53 | 1.842 | 4.01 | 718 | 2276.68 | 2.88 | 2000 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 2228.36 | 1.114 | 9.03 | 716 | 2225.99 | 9.02 | 2000 | 0 |
| SRPBatcher.Flush | Main Thread | 2237.62 | 1.119 | 4.19 | 1518 | 1789.10 | 3.94 | 50000 | 0 |
| WaitForJobGroupID | Main Thread | 4391.66 | 2.196 | 8.41 | 715 | 1456.27 | 8.15 | 57774 | 0 |
| ExecuteRenderQueueJob | Main Thread | 1022.15 | 0.511 | 1.19 | 1396 | 1022.15 | 1.19 | 4951 | 0 |
| JobHandle.Complete | Main Thread | 3012.90 | 1.506 | 9.38 | 715 | 614.54 | 5.27 | 161092 | 0 |
| Inl_On Record Render Graph | Main Thread | 635.93 | 0.318 | 0.87 | 1371 | 610.66 | 0.85 | 2000 | 0 |
| OnPerformCulling | Main Thread | 560.32 | 0.280 | 1.09 | 447 | 560.32 | 1.09 | 4000 | 0 |
| PlayerLoop | Main Thread | 46166.37 | 23.083 | 46.92 | 200 | 514.26 | 1.30 | 2000 | 4118494 |
| SamplePerObjectReflectionProbes | Main Thread | 488.72 | 0.244 | 0.50 | 447 | 488.72 | 0.50 | 1814000 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 9891.56 | 4.946 | 18.21 | 715 | 429.74 | 0.47 | 12000 | 1744000 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 5128.22 | 2.564 | 7.46 | 742 | 409.43 | 1.19 | 2000 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 448.67 | 0.224 | 3.49 | 1979 | 374.64 | 0.71 | 2000 | 1472000 |
| RenderLoop.DrawSRPBatcher | Main Thread | 2737.56 | 1.369 | 4.17 | 1518 | 346.14 | 0.51 | 24000 | 0 |
| ClipperRegistry.Cull | Main Thread | 342.73 | 0.171 | 0.51 | 937 | 342.73 | 0.51 | 2000 | 0 |
| SRPBRender.ApplyShader | Main Thread | 360.88 | 0.180 | 0.64 | 1518 | 339.19 | 0.62 | 42000 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 330.40 | 0.165 | 1.94 | 712 | 330.40 | 1.94 | 1978 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 6455.46 | 3.228 | 13.22 | 715 | 329.65 | 0.59 | 2000 | 1744000 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 316.69 | 0.158 | 0.48 | 715 | 316.69 | 0.48 | 2000 | 0 |
| RenderLoop.CleanupNodeQueue | Main Thread | 311.77 | 0.156 | 0.37 | 110 | 311.77 | 0.37 | 12000 | 0 |
| CanvasRenderer.SyncTransform | Main Thread | 304.13 | 0.152 | 0.37 | 69 | 304.13 | 0.37 | 744000 | 0 |
| Batch.DrawInstanced | Main Thread | 302.43 | 0.151 | 1.19 | 937 | 302.43 | 1.19 | 12000 | 0 |
| Canvas.BuildBatch | Main Thread | 272.14 | 0.136 | 2.20 | 718 | 272.14 | 2.20 | 9995 | 0 |
| Inl_RenderCameraStack | Main Thread | 11555.03 | 5.778 | 13.80 | 1518 | 268.40 | 0.28 | 1999 | 959520 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 12724.54 | 6.362 | 25.62 | 935 | 236.99 | 0.62 | 1999 | 959520 |
| TransformChangeSystem | Main Thread | 264.81 | 0.132 | 0.76 | 93 | 213.11 | 0.74 | 64194 | 0 |
| RegisterMaterialsAndMeshes | Main Thread | 213.07 | 0.107 | 0.53 | 1173 | 213.07 | 0.53 | 1995 | 0 |
| RenderLoop.Draw | Main Thread | 717.50 | 0.359 | 1.47 | 937 | 207.82 | 0.30 | 10000 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 23870.14 | 11.935 | 32.81 | 716 | 23870.14 | 32.81 | 26128 | 0 |
| GfxDeviceVK.Present | Render Thread | 13059.47 | 6.530 | 32.21 | 934 | 13059.47 | 32.21 | 2000 | 0 |
| DrawBuffersBatchMode | Render Thread | 5036.03 | 2.518 | 8.96 | 712 | 5036.03 | 8.96 | 50000 | 0 |
| RenderLoop | Render Thread | 22459.26 | 11.230 | 36.66 | 934 | 807.73 | 1.55 | 27280 | 0 |
| ExecuteRenderGraph | Render Thread | 7429.96 | 3.715 | 9.99 | 712 | 679.48 | 3.18 | 2000 | 0 |
| AcquireNextFrame | Render Thread | 422.83 | 0.211 | 1.40 | 778 | 422.83 | 1.40 | 2000 | 0 |
| RenderLoop.Draw | Render Thread | 382.77 | 0.191 | 2.92 | 752 | 382.40 | 2.92 | 10000 | 0 |
| Gfx.PresentFrame | Render Thread | 13795.50 | 6.898 | 32.56 | 934 | 370.44 | 0.80 | 2000 | 0 |
| Canvas.RenderOverlays | Render Thread | 578.69 | 0.289 | 3.94 | 238 | 333.92 | 1.99 | 10000 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 290.16 | 0.145 | 0.52 | 778 | 290.16 | 0.52 | 4000 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 4377.78 | 2.189 | 5.50 | 45 | 224.10 | 0.37 | 24000 | 0 |
| ScheduleGeometryJobs | Render Thread | 154.10 | 0.077 | 2.42 | 1392 | 154.10 | 2.42 | 8000 | 0 |
| Gfx.SetRenderTarget | Render Thread | 87.73 | 0.044 | 4.65 | 1518 | 87.73 | 4.65 | 8134 | 0 |
| BlitFinalToBackBuffer | Render Thread | 76.46 | 0.038 | 0.27 | 608 | 76.46 | 0.27 | 2000 | 0 |
| GpuRecorder.FrameTick | Render Thread | 75.44 | 0.038 | 0.11 | 171 | 75.44 | 0.11 | 2000 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 59.41 | 0.030 | 1.79 | 1113 | 59.41 | 1.79 | 134 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 940.16 | 0.470 | 6.26 | 712 | 41.41 | 0.24 | 2000 | 0 |
| UI.RenderOverlays | Render Thread | 388.44 | 0.194 | 2.02 | 238 | 37.49 | 0.14 | 2000 | 0 |
| WaitForRenderJobs | Render Thread | 33.21 | 0.017 | 0.87 | 1562 | 33.21 | 0.87 | 4000 | 0 |
| Gfx.DrawDynamic | Render Thread | 31.56 | 0.016 | 0.09 | 1518 | 31.56 | 0.09 | 2134 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 200 | 47.03 | 16.66 | 16.66 | 4.84 | 25.14 | 1792 |
| 935 | 46.62 | 16.75 | 16.75 | 5.41 | 0.00 | 2288 |
| 394 | 46.48 | 15.40 | 15.40 | 4.87 | 28.49 | 1832 |
| 172 | 46.36 | 16.12 | 16.12 | 4.42 | 29.52 | 2328 |
| 79 | 45.47 | 16.69 | 16.69 | 4.96 | 29.18 | 1792 |
| 466 | 43.47 | 20.77 | 20.77 | 5.39 | 22.98 | 1792 |
| 446 | 42.89 | 26.24 | 26.24 | 6.92 | 22.79 | 1832 |
| 169 | 42.12 | 15.54 | 15.54 | 4.70 | 31.14 | 1792 |
| 715 | 39.49 | 39.47 | 39.47 | 4.49 | 27.00 | 1792 |
| 716 | 38.03 | 38.02 | 38.02 | 4.12 | 26.05 | 2288 |
| 49 | 37.60 | 18.53 | 18.53 | 5.03 | 28.94 | 1896 |
| 46 | 37.02 | 18.38 | 18.38 | 5.53 | 32.11 | 1792 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
