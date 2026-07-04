# Android Profiler Capture Summary

Date: 2026-07-04 10:59:10 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-04_10-48-22.data`
Profiler frames: `0..899`
Scanned frames: `900`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 26.74 ms (37.4 FPS) |
| P50 frame | 24.71 ms (40.5 FPS) |
| P95 frame | 38.18 ms (26.2 FPS) |
| P99 frame | 61.03 ms (16.4 FPS) |
| Max frame | 86.52 ms (11.6 FPS) |
| Frames over budget | 900/900 |
| Avg CPU active | 22.41 ms |
| P95 CPU active | 23.76 ms |
| Avg GPU time | 22.02 ms |
| P95 GPU time | 0.00 ms |
| Total GC allocated | 1924188 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 23934.21 | 26.594 | 86.04 | 205 | 270.12 | 2.09 | 900 | 1924188 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 13859.90 | 15.400 | 52.28 | 205 | 17.58 | 0.99 | 16067 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 6588.53 | 7.321 | 43.62 | 205 | 49.16 | 0.36 | 900 | 432000 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 6109.70 | 6.789 | 25.90 | 197 | 234.61 | 2.68 | 5400 | 784800 |
| Gfx.PresentFrame | Render Thread | 5409.65 | 6.011 | 19.22 | 106 | 231.54 | 1.92 | 900 | 0 |
| SimulationSystemGroup | Main Thread | 3982.44 | 4.425 | 21.40 | 194 | 2.35 | 0.06 | 900 | 784800 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 3974.01 | 4.416 | 21.39 | 194 | 196.99 | 4.03 | 900 | 784800 |
| WaitForTargetFPS | Main Thread | 3793.57 | 4.215 | 17.20 | 106 | 3780.04 | 17.20 | 900 | 0 |
| PresentationSystemGroup | Main Thread | 1687.42 | 1.875 | 25.91 | 197 | 6.45 | 0.06 | 900 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 1674.91 | 1.861 | 25.89 | 197 | 44.83 | 0.17 | 900 | 0 |
| LateBehaviourUpdate | Main Thread | 1383.30 | 1.537 | 4.06 | 192 | 16.52 | 0.28 | 900 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 1036.32 | 1.151 | 11.08 | 218 | 2.18 | 0.07 | 900 | 707388 |
| BehaviourUpdate | Main Thread | 1034.14 | 1.149 | 11.07 | 218 | 45.88 | 2.60 | 900 | 707388 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 798.04 | 0.887 | 5.03 | 211 | 22.42 | 0.14 | 900 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 351.73 | 0.391 | 2.61 | 36 | 81.36 | 1.83 | 900 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 349.71 | 0.389 | 6.09 | 213 | 114.02 | 5.87 | 1800 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 255.81 | 0.284 | 7.49 | 195 | 74.90 | 7.35 | 900 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 246.31 | 0.274 | 9.17 | 218 | 55.78 | 1.22 | 900 | 108000 |
| Canvas.RenderOverlays | Render Thread | 222.59 | 0.247 | 8.97 | 211 | 176.75 | 1.24 | 900 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 218.40 | 0.243 | 3.51 | 207 | 36.12 | 0.60 | 900 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 201.09 | 0.223 | 1.09 | 471 | 81.85 | 0.62 | 900 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 194.22 | 0.216 | 1.51 | 458 | 194.22 | 1.51 | 900 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 179.06 | 0.199 | 2.28 | 141 | 55.73 | 0.46 | 900 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 176.28 | 0.196 | 6.58 | 195 | 70.74 | 0.52 | 900 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 171.13 | 0.190 | 1.13 | 803 | 59.04 | 0.39 | 900 | 0 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 163.68 | 0.182 | 13.01 | 194 | 6.90 | 0.09 | 900 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 156.92 | 0.174 | 0.94 | 221 | 79.01 | 0.34 | 900 | 0 |
| Canvas.RenderOverlays | Main Thread | 151.82 | 0.169 | 9.04 | 211 | 99.52 | 8.57 | 900 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 149.63 | 0.166 | 3.85 | 210 | 34.32 | 3.76 | 900 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 139.65 | 0.155 | 4.16 | 53 | 20.69 | 0.24 | 900 | 176160 |
| Canvas.BuildBatch | Main Thread | 135.73 | 0.151 | 3.10 | 193 | 135.73 | 3.10 | 900 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 132.36 | 0.147 | 3.20 | 406 | 54.31 | 0.32 | 900 | 0 |
| Default World Unity.Transforms.LocalToWorldSystem | Main Thread | 129.57 | 0.144 | 12.95 | 194 | 128.39 | 12.94 | 900 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 126.86 | 0.141 | 1.60 | 213 | 5.70 | 0.05 | 900 | 0 |
| Gfx.WaitForPresentOnGfxThread | Main Thread | 113.69 | 0.126 | 8.56 | 741 | 0.39 | 0.00 | 900 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 112.62 | 0.125 | 1.07 | 207 | 46.23 | 0.26 | 900 | 0 |
| GameplayRuntimeUpdate.RoadBuild | Main Thread | 103.80 | 0.115 | 0.69 | 213 | 89.04 | 0.67 | 900 | 36000 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 101.74 | 0.113 | 1.40 | 83 | 101.74 | 1.40 | 900 | 0 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 100.14 | 0.111 | 1.29 | 179 | 30.80 | 0.24 | 900 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 95.95 | 0.107 | 0.62 | 222 | 42.53 | 0.49 | 900 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 3788.45 | 4.209 | 17.20 | 106 | 3776.73 | 17.20 | 702 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 1489.67 | 1.655 | 10.19 | 187 | 1489.67 | 10.19 | 900 | 0 |
| Game.Composition.dll!Game.Composition::MatchSceneView.LateUpdate() [Invoke] | Main Thread | 1366.78 | 1.519 | 4.04 | 192 | 1365.51 | 4.03 | 900 | 0 |
| SRPBatcher.Flush | Main Thread | 1256.40 | 1.396 | 16.23 | 205 | 1001.26 | 15.94 | 22500 | 0 |
| WaitForJobGroupID | Main Thread | 2408.82 | 2.676 | 7.08 | 431 | 956.63 | 4.00 | 25574 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 1653.24 | 1.837 | 9.64 | 195 | 939.69 | 2.42 | 900 | 0 |
| JobHandle.Complete | Main Thread | 1959.25 | 2.177 | 8.70 | 195 | 519.14 | 8.69 | 74347 | 0 |
| ExecuteRenderQueueJob | Main Thread | 411.56 | 0.457 | 1.87 | 3 | 411.56 | 1.87 | 2375 | 0 |
| Inl_On Record Render Graph | Main Thread | 401.11 | 0.446 | 6.86 | 209 | 284.40 | 1.24 | 900 | 0 |
| SamplePerObjectReflectionProbes | Main Thread | 275.74 | 0.306 | 1.40 | 209 | 275.74 | 1.40 | 816300 | 0 |
| PlayerLoop | Main Thread | 23934.21 | 26.594 | 86.04 | 205 | 270.12 | 2.09 | 900 | 1924188 |
| OnPerformCulling | Main Thread | 235.69 | 0.262 | 6.17 | 213 | 235.69 | 6.17 | 1800 | 0 |
| Batch.DrawInstanced | Main Thread | 235.65 | 0.262 | 4.57 | 192 | 235.65 | 4.57 | 5400 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 6109.70 | 6.789 | 31.89 | 197 | 234.61 | 2.98 | 5400 | 784800 |
| SRPBRender.ApplyShader | Main Thread | 244.34 | 0.271 | 2.23 | 190 | 233.00 | 2.20 | 18900 | 0 |
| RenderLoop.DrawSRPBatcher | Main Thread | 1588.76 | 1.765 | 17.85 | 205 | 216.57 | 1.80 | 10800 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 3974.01 | 4.416 | 21.39 | 194 | 196.99 | 4.03 | 900 | 784800 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 194.22 | 0.216 | 1.51 | 458 | 194.22 | 1.51 | 900 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 193.99 | 0.216 | 2.33 | 431 | 193.99 | 2.33 | 801 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 2666.76 | 2.963 | 12.69 | 209 | 182.62 | 0.96 | 900 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 221.27 | 0.246 | 0.76 | 214 | 181.25 | 0.68 | 900 | 662400 |
| CanvasRenderer.SyncTransform | Main Thread | 160.69 | 0.179 | 1.32 | 200 | 160.69 | 1.32 | 334056 | 0 |
| ClipperRegistry.Cull | Main Thread | 144.94 | 0.161 | 0.72 | 214 | 144.94 | 0.72 | 896 | 0 |
| TransformChangeSystem | Main Thread | 174.20 | 0.194 | 1.53 | 21 | 142.48 | 1.48 | 18619 | 0 |
| RenderLoop.CleanupNodeQueue | Main Thread | 139.35 | 0.155 | 0.53 | 0 | 139.35 | 0.53 | 5292 | 0 |
| Game.UI.Runtime.dll!Game.UI.Runtime::UIShellLoadingProgressView.Update() [Invoke] | Main Thread | 131.36 | 0.146 | 0.46 | 52 | 131.18 | 0.46 | 898 | 43104 |
| Inl_ScriptableRenderContext.Submit | Main Thread | 3493.43 | 3.882 | 36.84 | 205 | 121.90 | 8.67 | 899 | 431520 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 7027.80 | 7.809 | 44.58 | 205 | 115.08 | 1.87 | 897 | 430560 |
| Canvas.BuildBatch | Main Thread | 113.56 | 0.126 | 3.10 | 193 | 113.56 | 3.10 | 453 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 113.50 | 0.126 | 0.84 | 193 | 112.97 | 0.84 | 893 | 135736 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 13842.32 | 15.380 | 72.81 | 205 | 13842.32 | 72.81 | 16067 | 0 |
| GfxDeviceVK.Present | Render Thread | 4979.76 | 5.533 | 18.55 | 106 | 4979.76 | 18.55 | 900 | 0 |
| DrawBuffersBatchMode | Render Thread | 2757.36 | 3.064 | 15.69 | 196 | 2757.36 | 15.69 | 22500 | 0 |
| RenderLoop | Render Thread | 10577.77 | 11.753 | 34.25 | 205 | 473.94 | 3.29 | 11632 | 0 |
| ExecuteRenderGraph | Render Thread | 4487.19 | 4.986 | 29.39 | 209 | 336.74 | 1.31 | 900 | 0 |
| RenderLoop.Draw | Render Thread | 322.66 | 0.359 | 7.69 | 209 | 258.90 | 1.72 | 4500 | 0 |
| AcquireNextFrame | Render Thread | 236.08 | 0.262 | 9.41 | 209 | 236.08 | 9.41 | 900 | 0 |
| Gfx.PresentFrame | Render Thread | 5409.65 | 6.011 | 19.22 | 106 | 231.54 | 1.92 | 900 | 0 |
| Canvas.RenderOverlays | Render Thread | 222.59 | 0.247 | 8.97 | 211 | 176.75 | 1.24 | 900 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 2606.93 | 2.897 | 17.14 | 205 | 172.80 | 0.72 | 10800 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 158.32 | 0.176 | 1.28 | 93 | 158.32 | 1.28 | 1800 | 0 |
| ScheduleGeometryJobs | Render Thread | 49.40 | 0.055 | 6.02 | 3 | 49.40 | 6.02 | 900 | 0 |
| Gfx.SetRenderTarget | Render Thread | 48.82 | 0.054 | 3.53 | 218 | 48.82 | 3.53 | 3660 | 0 |
| BlitFinalToBackBuffer | Render Thread | 44.31 | 0.049 | 0.61 | 189 | 43.12 | 0.61 | 900 | 0 |
| GpuRecorder.FrameTick | Render Thread | 40.04 | 0.044 | 6.00 | 212 | 40.04 | 6.00 | 900 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 30.73 | 0.034 | 1.02 | 97 | 30.73 | 1.02 | 60 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 516.75 | 0.574 | 5.32 | 187 | 25.19 | 0.11 | 900 | 0 |
| WaitForRenderJobs | Render Thread | 21.98 | 0.024 | 1.39 | 73 | 21.98 | 1.39 | 1800 | 0 |
| Profiler.FlushRenderCounters | Render Thread | 20.29 | 0.023 | 2.75 | 207 | 20.29 | 2.75 | 900 | 0 |
| UI.RenderOverlays | Render Thread | 243.71 | 0.271 | 9.09 | 211 | 19.20 | 0.15 | 900 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 205 | 86.52 | 86.49 | 86.49 | 7.17 | 36.61 | 2376 |
| 209 | 69.58 | 69.55 | 69.55 | 15.89 | 28.01 | 2376 |
| 218 | 69.15 | 69.12 | 69.12 | 10.56 | 27.75 | 2376 |
| 190 | 66.68 | 66.64 | 66.64 | 8.97 | 27.65 | 2120 |
| 212 | 65.30 | 65.23 | 65.23 | 7.02 | 27.78 | 2090 |
| 194 | 64.64 | 64.59 | 64.59 | 7.03 | 26.80 | 2174 |
| 204 | 63.52 | 63.49 | 63.49 | 13.93 | 27.27 | 1840 |
| 207 | 62.75 | 62.72 | 62.72 | 10.08 | 0.00 | 2376 |
| 200 | 61.86 | 61.82 | 61.82 | 7.76 | 31.27 | 1840 |
| 199 | 61.03 | 60.99 | 60.99 | 15.95 | 26.82 | 1840 |
| 211 | 60.43 | 60.40 | 60.40 | 7.40 | 26.89 | 2406 |
| 206 | 60.27 | 60.24 | 60.24 | 6.44 | 28.25 | 2110 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
