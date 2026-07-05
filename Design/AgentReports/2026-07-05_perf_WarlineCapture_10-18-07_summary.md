# Android Profiler Capture Summary

Date: 2026-07-05 10:20:06 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-05_10-18-07.data`
Profiler frames: `0..1585`
Scanned frames: `1586`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 19.17 ms (52.2 FPS) |
| P50 frame | 18.71 ms (53.4 FPS) |
| P95 frame | 21.96 ms (45.5 FPS) |
| P99 frame | 25.24 ms (39.6 FPS) |
| Max frame | 32.05 ms (31.2 FPS) |
| Frames over budget | 1577/1586 |
| Avg CPU active | 16.68 ms |
| P95 CPU active | 21.78 ms |
| Avg GPU time | 18.24 ms |
| P95 GPU time | 20.95 ms |
| Total GC allocated | 3191274 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 30481.09 | 19.219 | 185.32 | 0 | 408.13 | 1.21 | 1586 | 3191274 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 18220.62 | 11.488 | 193.83 | 0 | 19.61 | 0.72 | 20805 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 8366.48 | 5.275 | 6.48 | 208 | 375.05 | 0.46 | 9517 | 1383032 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 7268.74 | 4.583 | 10.97 | 641 | 70.98 | 0.29 | 1586 | 761280 |
| Gfx.PresentFrame | Render Thread | 6780.23 | 4.275 | 13.68 | 1570 | 326.52 | 0.68 | 1587 | 0 |
| SimulationSystemGroup | Main Thread | 5536.36 | 3.491 | 6.48 | 208 | 2.65 | 0.04 | 1586 | 1382992 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 5526.63 | 3.485 | 6.46 | 208 | 284.71 | 0.42 | 1586 | 1382992 |
| WaitForTargetFPS | Main Thread | 3964.87 | 2.500 | 12.62 | 1570 | 3944.05 | 12.61 | 1587 | 0 |
| PresentationSystemGroup | Main Thread | 2285.61 | 1.441 | 3.26 | 1498 | 10.75 | 0.06 | 1586 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 2265.14 | 1.428 | 3.25 | 1498 | 65.59 | 0.15 | 1586 | 0 |
| LateBehaviourUpdate | Main Thread | 1921.67 | 1.212 | 3.08 | 306 | 28.61 | 0.16 | 1586 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 1809.39 | 1.141 | 1.60 | 1402 | 1807.90 | 1.59 | 1586 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 1152.39 | 0.727 | 2.53 | 152 | 37.14 | 1.13 | 1586 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 1047.32 | 0.660 | 2.66 | 1112 | 2.64 | 0.26 | 1586 | 1046962 |
| BehaviourUpdate | Main Thread | 1044.68 | 0.659 | 2.66 | 1112 | 47.88 | 0.16 | 1586 | 1046962 |
| Canvas.RenderOverlays | Render Thread | 488.27 | 0.308 | 0.89 | 261 | 283.04 | 0.82 | 7935 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 481.31 | 0.303 | 2.47 | 129 | 123.84 | 1.26 | 1586 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 444.60 | 0.280 | 0.76 | 475 | 106.38 | 0.17 | 1586 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 420.78 | 0.265 | 1.16 | 1229 | 92.91 | 0.55 | 3172 | 0 |
| Canvas.RenderOverlays | Main Thread | 317.55 | 0.200 | 0.38 | 532 | 109.37 | 0.26 | 7930 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 271.60 | 0.171 | 1.80 | 1439 | 128.11 | 0.86 | 1586 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 260.04 | 0.164 | 0.28 | 33 | 260.04 | 0.28 | 1586 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 247.26 | 0.156 | 1.90 | 55 | 94.89 | 0.72 | 1586 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 242.24 | 0.153 | 0.74 | 161 | 51.63 | 0.41 | 1586 | 190320 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 219.56 | 0.138 | 0.57 | 800 | 37.15 | 0.17 | 1586 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 215.89 | 0.136 | 0.87 | 312 | 74.00 | 0.21 | 1586 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 211.93 | 0.134 | 0.64 | 640 | 103.78 | 0.21 | 1586 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 200.64 | 0.127 | 0.31 | 208 | 104.83 | 0.20 | 1586 | 0 |
| Canvas.BuildBatch | Main Thread | 196.72 | 0.124 | 1.28 | 640 | 196.72 | 1.28 | 7930 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 188.51 | 0.119 | 1.59 | 228 | 36.01 | 0.10 | 1586 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 185.03 | 0.117 | 0.45 | 421 | 95.02 | 0.40 | 1586 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 164.31 | 0.104 | 0.64 | 204 | 23.50 | 0.07 | 1586 | 260800 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 164.13 | 0.103 | 0.97 | 695 | 9.73 | 0.22 | 1586 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 159.33 | 0.100 | 0.74 | 696 | 72.77 | 0.70 | 1586 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 148.38 | 0.094 | 0.33 | 280 | 71.46 | 0.29 | 1586 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 145.23 | 0.092 | 0.23 | 1555 | 145.23 | 0.23 | 1586 | 0 |
| GameplayRuntimeUpdate.RoadBuild | Main Thread | 122.77 | 0.077 | 0.40 | 1254 | 104.94 | 0.38 | 1586 | 63440 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 121.59 | 0.077 | 0.95 | 642 | 9.00 | 0.06 | 1586 | 0 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 109.48 | 0.069 | 0.23 | 641 | 37.50 | 0.11 | 1586 | 0 |
| Default World Unity.Rendering.StructuralChangePresentationSystemGroup | Main Thread | 108.17 | 0.068 | 1.82 | 715 | 10.16 | 0.30 | 1586 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 3957.28 | 2.495 | 12.62 | 1570 | 3943.19 | 12.61 | 1532 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 2543.32 | 1.604 | 2.91 | 515 | 2543.32 | 2.91 | 1586 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 1809.39 | 1.141 | 1.60 | 1402 | 1807.90 | 1.59 | 1586 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 2844.04 | 1.793 | 3.30 | 641 | 1743.58 | 2.25 | 1586 | 0 |
| WaitForJobGroupID | Main Thread | 2998.66 | 1.891 | 4.58 | 1121 | 1027.92 | 2.68 | 44530 | 0 |
| SRPBatcher.Flush | Main Thread | 1133.50 | 0.715 | 3.45 | 641 | 904.38 | 3.29 | 38064 | 0 |
| JobHandle.Complete | Main Thread | 2425.23 | 1.529 | 3.79 | 1013 | 485.25 | 1.23 | 127634 | 0 |
| ExecuteRenderQueueJob | Main Thread | 479.40 | 0.302 | 1.20 | 837 | 479.40 | 1.20 | 2752 | 0 |
| Inl_On Record Render Graph | Main Thread | 443.70 | 0.280 | 0.80 | 1465 | 425.33 | 0.79 | 1586 | 0 |
| PlayerLoop | Main Thread | 30481.09 | 19.219 | 185.32 | 0 | 408.13 | 1.21 | 1586 | 3191274 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 8366.48 | 5.275 | 9.47 | 641 | 375.05 | 0.54 | 9517 | 1383032 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 410.14 | 0.259 | 0.60 | 201 | 354.82 | 0.55 | 1586 | 1167296 |
| OnPerformCulling | Main Thread | 327.86 | 0.207 | 1.23 | 1229 | 327.86 | 1.23 | 3172 | 0 |
| ClipperRegistry.Cull | Main Thread | 294.12 | 0.185 | 0.32 | 517 | 294.12 | 0.32 | 1586 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 5526.63 | 3.485 | 6.46 | 208 | 284.71 | 0.42 | 1586 | 1382992 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 277.04 | 0.175 | 2.19 | 1013 | 277.04 | 2.19 | 1572 | 0 |
| Default World Game.Rendering.UnitRenderVisualExclusivitySystem | Main Thread | 260.04 | 0.164 | 0.28 | 33 | 260.04 | 0.28 | 1586 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 3120.05 | 1.967 | 5.10 | 640 | 255.15 | 0.55 | 1586 | 0 |
| SamplePerObjectReflectionProbes | Main Thread | 252.73 | 0.159 | 0.27 | 32 | 252.73 | 0.27 | 991250 | 0 |
| SRPBRender.ApplyShader | Main Thread | 243.42 | 0.153 | 0.75 | 641 | 228.07 | 0.73 | 31720 | 0 |
| CanvasRenderer.SyncTransform | Main Thread | 228.03 | 0.144 | 0.29 | 1041 | 228.03 | 0.29 | 589992 | 0 |
| Batch.DrawInstanced | Main Thread | 225.84 | 0.142 | 0.44 | 641 | 225.84 | 0.44 | 9516 | 0 |
| Canvas.BuildBatch | Main Thread | 196.72 | 0.124 | 1.28 | 640 | 196.72 | 1.28 | 7930 | 0 |
| RenderLoop.DrawSRPBatcher | Main Thread | 1425.70 | 0.899 | 3.97 | 641 | 185.15 | 0.46 | 20618 | 0 |
| UpdateAllBatches | Main Thread | 625.77 | 0.395 | 1.88 | 1154 | 179.55 | 0.50 | 1585 | 0 |
| RegisterMaterialsAndMeshes | Main Thread | 177.94 | 0.112 | 0.55 | 800 | 177.94 | 0.55 | 1586 | 0 |
| RenderLoop.Draw | Main Thread | 565.42 | 0.357 | 0.90 | 641 | 177.21 | 0.51 | 9516 | 0 |
| RenderLoop.CleanupNodeQueue | Main Thread | 175.96 | 0.111 | 0.34 | 1007 | 175.96 | 0.34 | 9516 | 0 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 7757.73 | 4.891 | 11.30 | 641 | 171.08 | 0.60 | 1586 | 761280 |
| TransformChangeSystem | Main Thread | 211.85 | 0.134 | 1.91 | 1097 | 169.08 | 1.89 | 49409 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 18201.01 | 11.476 | 193.81 | 0 | 18201.01 | 193.81 | 20805 | 0 |
| GfxDeviceVK.Present | Render Thread | 6120.02 | 3.859 | 13.23 | 1570 | 6120.02 | 13.23 | 1587 | 0 |
| DrawBuffersBatchMode | Render Thread | 2863.76 | 1.806 | 7.44 | 532 | 2863.76 | 7.44 | 38088 | 0 |
| RenderLoop | Render Thread | 12414.28 | 7.827 | 17.57 | 370 | 608.91 | 1.14 | 21361 | 0 |
| ExecuteRenderGraph | Render Thread | 4729.66 | 2.982 | 8.50 | 532 | 514.85 | 0.86 | 1587 | 0 |
| AcquireNextFrame | Render Thread | 343.26 | 0.216 | 0.94 | 959 | 343.26 | 0.94 | 1587 | 0 |
| Gfx.PresentFrame | Render Thread | 6780.23 | 4.275 | 13.68 | 1570 | 326.52 | 0.68 | 1587 | 0 |
| RenderLoop.Draw | Render Thread | 290.35 | 0.183 | 0.68 | 641 | 288.66 | 0.58 | 9522 | 0 |
| Canvas.RenderOverlays | Render Thread | 488.27 | 0.308 | 1.71 | 261 | 283.04 | 0.88 | 7935 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 264.07 | 0.167 | 0.35 | 159 | 264.07 | 0.35 | 3174 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 2426.89 | 1.530 | 7.21 | 532 | 151.00 | 3.76 | 20631 | 0 |
| ScheduleGeometryJobs | Render Thread | 103.94 | 0.066 | 0.57 | 873 | 103.94 | 0.57 | 6348 | 0 |
| GpuRecorder.FrameTick | Render Thread | 69.62 | 0.044 | 3.80 | 1391 | 69.62 | 3.80 | 1587 | 0 |
| BlitFinalToBackBuffer | Render Thread | 67.50 | 0.043 | 0.18 | 702 | 67.50 | 0.18 | 1587 | 0 |
| Gfx.SetRenderTarget | Render Thread | 54.05 | 0.034 | 0.19 | 332 | 54.05 | 0.19 | 6454 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 49.65 | 0.031 | 1.32 | 1344 | 49.65 | 1.32 | 106 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 633.96 | 0.400 | 0.90 | 641 | 35.21 | 0.07 | 1587 | 0 |
| UI.RenderOverlays | Render Thread | 330.35 | 0.208 | 0.90 | 261 | 33.25 | 0.07 | 1587 | 0 |
| Gfx.DrawDynamic | Render Thread | 23.63 | 0.015 | 0.08 | 849 | 23.63 | 0.08 | 1693 | 0 |
| WaitForRenderJobs | Render Thread | 20.94 | 0.013 | 0.51 | 903 | 20.94 | 0.51 | 3174 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 641 | 32.05 | 32.03 | 32.03 | 4.11 | 21.37 | 1870 |
| 880 | 28.07 | 19.36 | 19.36 | 5.31 | 21.29 | 1832 |
| 1169 | 27.92 | 16.03 | 16.03 | 3.84 | 17.87 | 1792 |
| 870 | 27.73 | 23.79 | 23.79 | 4.50 | 20.22 | 1792 |
| 640 | 27.53 | 27.50 | 27.50 | 4.06 | 21.80 | 2288 |
| 884 | 27.37 | 20.24 | 20.24 | 5.11 | 20.65 | 1792 |
| 1570 | 27.31 | 14.69 | 14.69 | 3.52 | 17.79 | 2032 |
| 370 | 27.20 | 15.86 | 15.86 | 4.05 | 18.10 | 1832 |
| 516 | 26.20 | 26.16 | 26.16 | 6.29 | 17.99 | 2366 |
| 1452 | 26.04 | 15.85 | 15.85 | 4.07 | 18.04 | 1832 |
| 770 | 25.84 | 14.83 | 14.83 | 3.58 | 17.41 | 1792 |
| 652 | 25.57 | 19.42 | 19.42 | 4.03 | 18.94 | 1870 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
