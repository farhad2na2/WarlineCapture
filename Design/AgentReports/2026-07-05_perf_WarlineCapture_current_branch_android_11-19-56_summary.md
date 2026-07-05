# Android Profiler Capture Summary

Date: 2026-07-05 11:27:47 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture/ProfilerCaptures/WarlineCapture_2026-07-05_11-19-56.data.raw`
Profiler frames: `1..2000`
Scanned frames: `2000`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 16.78 ms (59.6 FPS) |
| P50 frame | 16.64 ms (60.1 FPS) |
| P95 frame | 17.52 ms (57.1 FPS) |
| P99 frame | 18.27 ms (54.7 FPS) |
| Max frame | 282.20 ms (3.5 FPS) |
| Frames over budget | 943/2000 |
| Avg CPU active | 8.03 ms |
| P95 CPU active | 6.78 ms |
| Avg GPU time | 5.31 ms |
| P95 GPU time | 5.53 ms |
| Total GC allocated | 12853044 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 33112.34 | 16.556 | 281.99 | 1 | 824.36 | 5.66 | 2000 | 1135584 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 26376.32 | 13.188 | 249.77 | 1 | 29.58 | 0.13 | 22133 | 0 |
| WaitForTargetFPS | Main Thread | 17477.17 | 8.739 | 14.23 | 376 | 17460.26 | 14.23 | 2000 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 6078.13 | 3.039 | 168.85 | 1 | 177.81 | 64.62 | 2000 | 433128 |
| Gfx.PresentFrame | Render Thread | 3363.87 | 1.682 | 4.56 | 939 | 863.66 | 1.43 | 2000 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 3113.08 | 1.557 | 8.17 | 1 | 365.79 | 1.84 | 12000 | 20838 |
| SimulationSystemGroup | Main Thread | 1613.51 | 0.807 | 8.18 | 1 | 4.57 | 0.25 | 2000 | 550 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 1600.20 | 0.800 | 8.17 | 1 | 312.36 | 1.49 | 2000 | 550 |
| PresentationSystemGroup | Main Thread | 853.04 | 0.427 | 5.92 | 1 | 11.09 | 0.14 | 2000 | 4324 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 831.90 | 0.416 | 5.91 | 1 | 69.27 | 1.95 | 2000 | 4324 |
| Canvas.RenderSubBatch | Render Thread | 817.38 | 0.409 | 1.50 | 315 | 812.32 | 0.79 | 107354 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 423.39 | 0.212 | 16.60 | 2 | 4.85 | 0.09 | 2000 | 542924 |
| BehaviourUpdate | Main Thread | 418.54 | 0.209 | 16.60 | 2 | 51.42 | 0.78 | 2000 | 542924 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 404.85 | 0.202 | 2.06 | 1 | 163.52 | 1.83 | 2000 | 40 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 354.12 | 0.177 | 38.21 | 1 | 84.80 | 0.20 | 2000 | 134676 |
| Gfx.SetRenderTarget | Render Thread | 348.33 | 0.174 | 3.75 | 1 | 347.86 | 3.75 | 10136 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 307.48 | 0.154 | 3.07 | 1 | 95.28 | 0.40 | 2000 | 14570 |
| Gfx.EndAsyncJobFrame | Main Thread | 194.67 | 0.097 | 0.31 | 1261 | 3.58 | 0.11 | 4001 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 179.55 | 0.090 | 3.28 | 1 | 16.34 | 0.13 | 2000 | 876 |
| Canvas.RenderSubBatch | Main Thread | 158.54 | 0.079 | 2.04 | 359 | 158.54 | 2.04 | 107354 | 0 |
| Default World Unity.Rendering.StructuralChangePresentationSystemGroup | Main Thread | 120.47 | 0.060 | 1.56 | 1428 | 12.67 | 0.34 | 2000 | 80 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 117.86 | 0.059 | 1.27 | 1 | 22.42 | 0.35 | 2000 | 2104 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 100.18 | 0.050 | 2.35 | 1276 | 100.15 | 2.35 | 2000 | 190 |
| UnityEngine.CoreModule.dll!UnityEngine.Experimental.Rendering::ScriptableRuntimeReflectionSystemWrapper.Internal_ScriptableRuntimeReflectionSystemWrapper_TickRealtimeProbes() [Invoke] | Main Thread | 95.35 | 0.048 | 0.35 | 653 | 10.97 | 0.10 | 2000 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 92.40 | 0.046 | 1.30 | 1020 | 92.40 | 1.30 | 134 | 0 |
| Default World Unity.Entities.UpdateWorldTimeSystem | Main Thread | 86.78 | 0.043 | 0.35 | 608 | 86.78 | 0.35 | 2000 | 44 |
| Default World Game.Rendering.UnitSelectionObjectOutlinePresentationSystem | Main Thread | 80.80 | 0.040 | 2.07 | 1842 | 80.80 | 2.07 | 2000 | 0 |
| Default World Unity.Entities.FixedStepSimulationSystemGroup | Main Thread | 80.36 | 0.040 | 0.56 | 1 | 75.27 | 0.55 | 2000 | 96 |
| Gfx.DrawDynamic | Render Thread | 73.86 | 0.037 | 0.32 | 1814 | 73.54 | 0.32 | 2134 | 0 |
| Default World Unity.Rendering.AddLODRequirementComponents | Main Thread | 71.90 | 0.036 | 1.52 | 1428 | 71.90 | 1.52 | 2000 | 0 |
| Default World Unity.Scenes.SceneSystemGroup | Main Thread | 58.40 | 0.029 | 2.58 | 1 | 16.69 | 0.10 | 2000 | 13400 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 53.34 | 0.027 | 0.49 | 1 | 32.33 | 0.32 | 2000 | 1232 |
| UnityEngine.UI.dll!UnityEngine.EventSystems::EventSystem.Update() [Invoke] | Main Thread | 38.81 | 0.019 | 0.33 | 1580 | 38.81 | 0.33 | 2000 | 218 |
| Default World Unity.Rendering.DeformationsInPresentation | Main Thread | 37.86 | 0.019 | 0.13 | 1629 | 19.08 | 0.12 | 2000 | 0 |
| Default World Unity.Rendering.HybridLightBakingDataSystem | Main Thread | 37.56 | 0.019 | 0.60 | 1 | 37.56 | 0.60 | 2000 | 1184 |
| Default World Unity.Rendering.MatrixPreviousInitializationSystem | Main Thread | 37.49 | 0.019 | 0.19 | 1 | 37.48 | 0.19 | 2000 | 40 |
| UnityEngine.InputModule.dll!UnityEngineInternal.Input::NativeInputSystem.NotifyBeforeUpdate() [Invoke] | Main Thread | 35.79 | 0.018 | 0.71 | 1 | 28.70 | 0.14 | 2000 | 976 |
| Default World Game.Runtime.UnitAttackOrderRequestSystem | Main Thread | 34.02 | 0.017 | 0.18 | 33 | 34.02 | 0.18 | 2000 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 30.56 | 0.015 | 0.16 | 1710 | 10.23 | 0.13 | 2000 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] | Main Thread | 29.34 | 0.015 | 0.12 | 61 | 29.34 | 0.12 | 2000 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 17477.14 | 8.739 | 14.23 | 376 | 17460.24 | 14.23 | 1998 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 1706.63 | 0.853 | 2.64 | 1396 | 1706.61 | 2.64 | 2000 | 0 |
| PlayerLoop | Main Thread | 33112.34 | 16.556 | 281.99 | 1 | 824.36 | 5.66 | 2000 | 1135584 |
| Inl_On Record Render Graph | Main Thread | 743.52 | 0.372 | 19.09 | 1 | 733.99 | 18.98 | 2000 | 47564 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: GameUICamera | Main Thread | 3455.47 | 1.728 | 61.11 | 1 | 421.71 | 20.11 | 2000 | 109096 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 3113.08 | 1.557 | 20.05 | 1 | 365.79 | 2.85 | 12000 | 20838 |
| StdRender.ApplyShader | Main Thread | 337.93 | 0.169 | 1.32 | 1392 | 323.87 | 1.31 | 19884 | 0 |
| Inl_RenderCameraStack | Main Thread | 5699.87 | 2.850 | 69.19 | 1 | 314.68 | 4.77 | 2000 | 116878 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 1592.03 | 0.796 | 3.44 | 1042 | 312.24 | 1.49 | 1999 | 0 |
| WaitForJobGroupID | Main Thread | 373.80 | 0.187 | 2.24 | 13 | 304.85 | 1.28 | 6524 | 0 |
| PlayerConnection.Poll | Main Thread | 302.71 | 0.151 | 0.77 | 1188 | 296.76 | 0.76 | 2000 | 0 |
| Inl_ScriptableRenderContext.Submit | Main Thread | 1714.47 | 0.857 | 3.12 | 359 | 286.82 | 0.69 | 1999 | 0 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 6596.93 | 3.298 | 171.92 | 1 | 232.03 | 0.73 | 2000 | 433128 |
| ClipperRegistry.Cull | Main Thread | 197.43 | 0.099 | 1.03 | 1 | 197.38 | 1.03 | 2000 | 806 |
| RenderLoop.Draw | Main Thread | 688.81 | 0.344 | 2.39 | 359 | 192.36 | 1.30 | 1999 | 0 |
| WaitForRenderJobs | Main Thread | 191.00 | 0.095 | 0.35 | 1261 | 191.00 | 0.35 | 3989 | 0 |
| SceneCulling | Main Thread | 581.61 | 0.291 | 1.02 | 1420 | 189.90 | 0.23 | 1998 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 6075.99 | 3.038 | 168.85 | 1 | 177.79 | 64.63 | 1999 | 433128 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 171.94 | 0.086 | 1.76 | 1 | 171.86 | 1.68 | 2000 | 1320 |
| UGUI.Rendering.EmitWorldScreenspaceCameraGeometry | Main Thread | 187.61 | 0.094 | 2.03 | 13 | 169.18 | 0.50 | 3996 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 404.85 | 0.202 | 2.06 | 1 | 163.52 | 1.83 | 2000 | 40 |
| RegisterMaterialsAndMeshes | Main Thread | 163.21 | 0.082 | 3.27 | 1 | 163.20 | 3.25 | 2000 | 798 |
| Canvas.RenderSubBatch | Main Thread | 158.52 | 0.079 | 2.04 | 359 | 158.52 | 2.04 | 107352 | 0 |
| Inl_BlitFinalToBackBuffer | Main Thread | 157.66 | 0.079 | 1.47 | 628 | 157.66 | 1.47 | 1998 | 0 |
| QueuePrepareIntegrateMainThreadObjects | Main Thread | 140.77 | 0.070 | 0.94 | 1544 | 140.77 | 0.94 | 1998 | 0 |
| Profiler.ScreenshotUpdate | Main Thread | 155.98 | 0.078 | 0.77 | 1410 | 137.29 | 0.75 | 1972 | 0 |
| Inl_UniversalRenderTotal | Main Thread | 5868.13 | 2.934 | 72.03 | 1 | 132.26 | 2.57 | 2000 | 127314 |
| Inl_ExecuteRenderGraph | Main Thread | 946.72 | 0.473 | 1.88 | 628 | 129.12 | 0.61 | 1998 | 0 |
| Inl_Setup Light Constants | Main Thread | 127.35 | 0.064 | 0.37 | 1993 | 127.35 | 0.37 | 1998 | 0 |
| Setup Camera Properties | Main Thread | 120.13 | 0.060 | 1.01 | 1147 | 120.13 | 1.01 | 3996 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 26346.74 | 13.173 | 250.07 | 1 | 26346.74 | 250.07 | 22134 | 0 |
| GfxDeviceVK.Present | Render Thread | 2036.90 | 1.018 | 3.66 | 939 | 2036.90 | 3.66 | 2000 | 0 |
| Gfx.PresentFrame | Render Thread | 3363.87 | 1.682 | 4.56 | 939 | 863.66 | 1.43 | 2000 | 0 |
| Canvas.RenderSubBatch | Render Thread | 817.38 | 0.409 | 1.50 | 315 | 812.32 | 0.79 | 107354 | 0 |
| AcquireNextFrame | Render Thread | 747.66 | 0.374 | 1.17 | 1833 | 747.66 | 1.17 | 2000 | 0 |
| ExecuteRenderGraph | Render Thread | 2837.00 | 1.418 | 3.24 | 1350 | 574.56 | 1.02 | 2000 | 0 |
| RenderLoop | Render Thread | 7392.73 | 3.696 | 31.85 | 1 | 539.76 | 3.23 | 21767 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 367.05 | 0.184 | 1.21 | 1982 | 367.05 | 1.21 | 4010 | 0 |
| Gfx.SetRenderTarget | Render Thread | 348.33 | 0.174 | 3.78 | 1 | 347.86 | 3.78 | 10136 | 0 |
| RenderLoop.Draw | Render Thread | 1057.09 | 0.529 | 2.41 | 359 | 168.60 | 1.44 | 2000 | 0 |
| BlitFinalToBackBuffer | Render Thread | 140.44 | 0.070 | 1.36 | 1797 | 140.17 | 1.36 | 2000 | 0 |
| GpuRecorder.FrameTick | Render Thread | 96.25 | 0.048 | 0.94 | 1632 | 96.25 | 0.94 | 2000 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 92.40 | 0.046 | 1.30 | 1020 | 92.40 | 1.30 | 134 | 0 |
| Profiler.FlushRenderCounters | Render Thread | 77.98 | 0.039 | 0.75 | 607 | 77.98 | 0.75 | 1999 | 0 |
| Gfx.DrawDynamic | Render Thread | 73.86 | 0.037 | 0.46 | 1 | 73.54 | 0.32 | 2134 | 0 |
| PlayerEndOfFrame | Render Thread | 162.03 | 0.081 | 21.16 | 1 | 44.55 | 20.00 | 2000 | 0 |
| Setup Camera Properties | Render Thread | 100.76 | 0.050 | 1.22 | 1876 | 35.58 | 0.37 | 4000 | 0 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 26376.32 | 13.188 | 250.10 | 1 | 29.58 | 0.13 | 22133 | 0 |
| DrawScreenSpaceUI | Render Thread | 47.50 | 0.024 | 0.29 | 1124 | 29.50 | 0.16 | 4000 | 0 |
| WaitForRenderJobs | Render Thread | 21.70 | 0.011 | 0.08 | 1977 | 21.70 | 0.08 | 4001 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 282.20 | 282.19 | 282.19 | 30.74 | 5.38 | 12205566 |
| 2 | 31.22 | 31.20 | 31.20 | 2.15 | 5.24 | 546854 |
| 1238 | 20.89 | 8.89 | 8.89 | 2.07 | 5.23 | 48 |
| 1766 | 19.41 | 8.50 | 8.50 | 2.22 | 5.59 | 48 |
| 1375 | 19.05 | 8.18 | 8.18 | 1.80 | 5.23 | 48 |
| 1419 | 19.02 | 9.06 | 9.06 | 1.88 | 5.60 | 48 |
| 1681 | 18.86 | 8.66 | 8.66 | 1.96 | 4.39 | 78 |
| 1557 | 18.76 | 8.55 | 8.55 | 1.88 | 4.93 | 48 |
| 1612 | 18.70 | 9.73 | 9.73 | 1.78 | 4.95 | 48 |
| 1158 | 18.63 | 8.22 | 8.22 | 2.09 | 4.94 | 48 |
| 1821 | 18.61 | 6.98 | 6.98 | 1.82 | 4.64 | 48 |
| 1273 | 18.58 | 7.63 | 7.63 | 2.05 | 5.58 | 48 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
