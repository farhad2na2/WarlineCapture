# Android Profiler Capture Summary

Date: 2026-07-05 11:30:10 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture/ProfilerCaptures/WarlineCapture_2026-07-05_11-19-56.data.raw`
Profiler frames: `300..1999`
Scanned frames: `1700`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 16.64 ms (60.1 FPS) |
| P50 frame | 16.64 ms (60.1 FPS) |
| P95 frame | 17.57 ms (56.9 FPS) |
| P99 frame | 18.30 ms (54.6 FPS) |
| Max frame | 20.89 ms (47.9 FPS) |
| Frames over budget | 797/1700 |
| Avg CPU active | 8.00 ms |
| P95 CPU active | 9.03 ms |
| Avg GPU time | 5.31 ms |
| P95 GPU time | 5.74 ms |
| Total GC allocated | 84780 bytes |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 27899.73 | 16.412 | 20.69 | 1238 | 707.58 | 1.63 | 1700 | 84780 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 22126.65 | 13.016 | 16.77 | 1238 | 25.62 | 0.13 | 18784 | 0 |
| WaitForTargetFPS | Main Thread | 14674.85 | 8.632 | 14.23 | 376 | 14660.10 | 14.23 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 5090.59 | 2.994 | 5.31 | 505 | 97.87 | 0.55 | 1700 | 0 |
| Gfx.PresentFrame | Render Thread | 2913.50 | 1.714 | 4.56 | 939 | 749.04 | 1.43 | 1700 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 2682.97 | 1.578 | 3.44 | 1042 | 311.67 | 1.17 | 10200 | 0 |
| SimulationSystemGroup | Main Thread | 1392.29 | 0.819 | 3.45 | 1042 | 3.99 | 0.25 | 1700 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 1380.79 | 0.812 | 3.44 | 1042 | 270.34 | 1.49 | 1700 | 0 |
| PresentationSystemGroup | Main Thread | 736.52 | 0.433 | 2.68 | 1842 | 9.62 | 0.14 | 1700 | 0 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 718.21 | 0.422 | 2.67 | 1842 | 60.36 | 1.95 | 1700 | 0 |
| Canvas.RenderSubBatch | Render Thread | 706.79 | 0.416 | 1.50 | 315 | 702.43 | 0.79 | 91800 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 349.99 | 0.206 | 0.65 | 1391 | 4.14 | 0.01 | 1700 | 84780 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 349.54 | 0.206 | 0.59 | 1393 | 140.41 | 0.45 | 1700 | 0 |
| BehaviourUpdate | Main Thread | 345.85 | 0.203 | 0.65 | 1391 | 43.84 | 0.30 | 1700 | 84780 |
| Gfx.SetRenderTarget | Render Thread | 297.89 | 0.175 | 1.19 | 1876 | 297.48 | 1.19 | 8614 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 265.57 | 0.156 | 0.54 | 1287 | 82.94 | 0.40 | 1700 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 263.97 | 0.155 | 0.50 | 1997 | 73.19 | 0.20 | 1700 | 0 |
| Gfx.EndAsyncJobFrame | Main Thread | 168.23 | 0.099 | 0.31 | 1261 | 3.11 | 0.11 | 3400 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 153.50 | 0.090 | 0.27 | 1495 | 14.22 | 0.13 | 1700 | 0 |
| Canvas.RenderSubBatch | Main Thread | 130.62 | 0.077 | 2.04 | 359 | 130.62 | 2.04 | 91800 | 0 |
| Default World Unity.Rendering.StructuralChangePresentationSystemGroup | Main Thread | 104.58 | 0.062 | 1.56 | 1428 | 11.06 | 0.34 | 1700 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 101.29 | 0.060 | 0.22 | 1122 | 19.09 | 0.13 | 1700 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 85.50 | 0.050 | 2.35 | 1276 | 85.50 | 2.35 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Experimental.Rendering::ScriptableRuntimeReflectionSystemWrapper.Internal_ScriptableRuntimeReflectionSystemWrapper_TickRealtimeProbes() [Invoke] | Main Thread | 81.97 | 0.048 | 0.35 | 653 | 9.44 | 0.10 | 1700 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 81.43 | 0.048 | 1.30 | 1020 | 81.43 | 1.30 | 114 | 0 |
| Default World Unity.Entities.UpdateWorldTimeSystem | Main Thread | 75.42 | 0.044 | 0.35 | 608 | 75.42 | 0.35 | 1700 | 0 |
| Default World Game.Rendering.UnitSelectionObjectOutlinePresentationSystem | Main Thread | 70.45 | 0.041 | 2.07 | 1842 | 70.45 | 2.07 | 1700 | 0 |
| Default World Unity.Entities.FixedStepSimulationSystemGroup | Main Thread | 69.24 | 0.041 | 0.46 | 1193 | 64.88 | 0.46 | 1700 | 0 |
| Gfx.DrawDynamic | Render Thread | 63.37 | 0.037 | 0.32 | 1814 | 63.37 | 0.32 | 1814 | 0 |
| Default World Unity.Rendering.AddLODRequirementComponents | Main Thread | 62.63 | 0.037 | 1.52 | 1428 | 62.63 | 1.52 | 1700 | 0 |
| Default World Unity.Scenes.SceneSystemGroup | Main Thread | 48.57 | 0.029 | 0.39 | 1287 | 14.55 | 0.10 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 45.42 | 0.027 | 0.12 | 1202 | 27.51 | 0.10 | 1700 | 0 |
| UnityEngine.UI.dll!UnityEngine.EventSystems::EventSystem.Update() [Invoke] | Main Thread | 33.62 | 0.020 | 0.33 | 1580 | 33.62 | 0.33 | 1700 | 0 |
| Default World Unity.Rendering.DeformationsInPresentation | Main Thread | 32.80 | 0.019 | 0.13 | 1629 | 16.53 | 0.12 | 1700 | 0 |
| Default World Unity.Rendering.MatrixPreviousInitializationSystem | Main Thread | 32.58 | 0.019 | 0.16 | 1882 | 32.58 | 0.16 | 1700 | 0 |
| Default World Unity.Rendering.HybridLightBakingDataSystem | Main Thread | 32.05 | 0.019 | 0.15 | 1329 | 32.05 | 0.15 | 1700 | 0 |
| UnityEngine.InputModule.dll!UnityEngineInternal.Input::NativeInputSystem.NotifyBeforeUpdate() [Invoke] | Main Thread | 30.42 | 0.018 | 0.13 | 1824 | 24.79 | 0.13 | 1700 | 0 |
| Default World Game.Runtime.UnitAttackOrderRequestSystem | Main Thread | 29.27 | 0.017 | 0.17 | 1378 | 29.27 | 0.17 | 1700 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 26.44 | 0.016 | 0.16 | 1710 | 8.79 | 0.13 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] | Main Thread | 25.33 | 0.015 | 0.11 | 1424 | 25.33 | 0.11 | 1700 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 14674.85 | 8.632 | 14.23 | 376 | 14660.10 | 14.23 | 1700 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 1479.99 | 0.871 | 2.64 | 1396 | 1479.99 | 2.64 | 1700 | 0 |
| PlayerLoop | Main Thread | 27899.73 | 16.412 | 20.69 | 1238 | 707.58 | 1.63 | 1700 | 84780 |
| Inl_On Record Render Graph | Main Thread | 626.06 | 0.368 | 1.57 | 505 | 617.90 | 1.57 | 1700 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: GameUICamera | Main Thread | 2933.40 | 1.726 | 3.65 | 628 | 347.57 | 0.49 | 1700 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 2682.97 | 1.578 | 4.29 | 1042 | 311.67 | 1.47 | 10200 | 0 |
| StdRender.ApplyShader | Main Thread | 288.37 | 0.170 | 1.32 | 1392 | 276.76 | 1.31 | 17000 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 1380.79 | 0.812 | 3.44 | 1042 | 270.34 | 1.49 | 1700 | 0 |
| Inl_RenderCameraStack | Main Thread | 4849.99 | 2.853 | 5.18 | 505 | 268.08 | 1.71 | 1700 | 0 |
| PlayerConnection.Poll | Main Thread | 264.74 | 0.156 | 0.77 | 1188 | 259.52 | 0.76 | 1701 | 0 |
| WaitForJobGroupID | Main Thread | 307.02 | 0.181 | 1.78 | 1350 | 253.56 | 1.28 | 5436 | 0 |
| Inl_ScriptableRenderContext.Submit | Main Thread | 1464.85 | 0.862 | 3.12 | 359 | 246.09 | 0.69 | 1700 | 0 |
| PostLateUpdate.FinishFrameRendering | Main Thread | 5534.19 | 3.255 | 6.05 | 628 | 199.77 | 0.73 | 1700 | 0 |
| ClipperRegistry.Cull | Main Thread | 169.40 | 0.100 | 0.43 | 1997 | 169.40 | 0.43 | 1700 | 0 |
| WaitForRenderJobs | Main Thread | 165.10 | 0.097 | 0.35 | 1261 | 165.10 | 0.35 | 3392 | 0 |
| SceneCulling | Main Thread | 502.66 | 0.296 | 1.02 | 1420 | 163.90 | 0.23 | 1700 | 0 |
| RenderLoop.Draw | Main Thread | 581.86 | 0.342 | 2.39 | 359 | 162.87 | 1.30 | 1700 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 147.82 | 0.087 | 0.27 | 1529 | 147.82 | 0.27 | 1700 | 0 |
| UGUI.Rendering.EmitWorldScreenspaceCameraGeometry | Main Thread | 147.71 | 0.087 | 0.50 | 1550 | 147.71 | 0.50 | 3400 | 0 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 349.54 | 0.206 | 0.59 | 1393 | 140.41 | 0.45 | 1700 | 0 |
| RegisterMaterialsAndMeshes | Main Thread | 139.28 | 0.082 | 0.26 | 1495 | 139.28 | 0.26 | 1700 | 0 |
| Inl_BlitFinalToBackBuffer | Main Thread | 136.70 | 0.080 | 1.47 | 628 | 136.70 | 1.47 | 1700 | 0 |
| Canvas.RenderSubBatch | Main Thread | 130.62 | 0.077 | 2.04 | 359 | 130.62 | 2.04 | 91800 | 0 |
| Profiler.ScreenshotUpdate | Main Thread | 139.15 | 0.082 | 0.77 | 1410 | 123.53 | 0.75 | 1695 | 0 |
| QueuePrepareIntegrateMainThreadObjects | Main Thread | 121.93 | 0.072 | 0.94 | 1544 | 121.93 | 0.94 | 1700 | 0 |
| Inl_ExecuteRenderGraph | Main Thread | 819.64 | 0.482 | 1.88 | 628 | 112.01 | 0.61 | 1700 | 0 |
| Inl_UniversalRenderTotal | Main Thread | 4992.72 | 2.937 | 5.26 | 505 | 111.45 | 0.53 | 1700 | 0 |
| Inl_Setup Light Constants | Main Thread | 109.93 | 0.065 | 0.37 | 1993 | 109.93 | 0.37 | 1700 | 0 |
| Setup Camera Properties | Main Thread | 101.85 | 0.060 | 1.01 | 1147 | 101.85 | 1.01 | 3400 | 0 |
| CustomRenderTextures.Update | Main Thread | 100.82 | 0.059 | 0.19 | 1041 | 100.82 | 0.19 | 1684 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 22101.03 | 13.001 | 16.86 | 1238 | 22101.03 | 16.86 | 18784 | 0 |
| GfxDeviceVK.Present | Render Thread | 1763.90 | 1.038 | 3.66 | 939 | 1763.90 | 3.66 | 1700 | 0 |
| Gfx.PresentFrame | Render Thread | 2913.50 | 1.714 | 4.56 | 939 | 749.04 | 1.43 | 1700 | 0 |
| Canvas.RenderSubBatch | Render Thread | 706.79 | 0.416 | 1.50 | 315 | 702.43 | 0.79 | 91800 | 0 |
| AcquireNextFrame | Render Thread | 643.25 | 0.378 | 1.17 | 1833 | 643.25 | 1.17 | 1700 | 0 |
| ExecuteRenderGraph | Render Thread | 2450.46 | 1.441 | 3.24 | 1350 | 496.54 | 1.02 | 1700 | 0 |
| RenderLoop | Render Thread | 6351.37 | 3.736 | 7.96 | 1305 | 462.69 | 1.44 | 18481 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 317.68 | 0.187 | 1.21 | 1982 | 317.68 | 1.21 | 3400 | 0 |
| Gfx.SetRenderTarget | Render Thread | 297.89 | 0.175 | 1.36 | 1876 | 297.48 | 1.36 | 8614 | 0 |
| RenderLoop.Draw | Render Thread | 914.89 | 0.538 | 2.41 | 359 | 145.99 | 1.44 | 1700 | 0 |
| BlitFinalToBackBuffer | Render Thread | 120.68 | 0.071 | 1.36 | 1797 | 120.68 | 1.36 | 1700 | 0 |
| GpuRecorder.FrameTick | Render Thread | 82.88 | 0.049 | 0.94 | 1632 | 82.88 | 0.94 | 1700 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 81.43 | 0.048 | 1.30 | 1020 | 81.43 | 1.30 | 114 | 0 |
| Profiler.FlushRenderCounters | Render Thread | 68.75 | 0.040 | 0.75 | 607 | 68.75 | 0.75 | 1700 | 0 |
| Gfx.DrawDynamic | Render Thread | 63.37 | 0.037 | 0.32 | 1814 | 63.37 | 0.32 | 1814 | 0 |
| Setup Camera Properties | Render Thread | 85.83 | 0.050 | 1.22 | 1876 | 30.68 | 0.37 | 3400 | 0 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 22126.65 | 13.016 | 16.87 | 1238 | 25.62 | 0.13 | 18784 | 0 |
| DrawScreenSpaceUI | Render Thread | 40.92 | 0.024 | 0.29 | 1124 | 25.36 | 0.16 | 3400 | 0 |
| WaitForRenderJobs | Render Thread | 18.86 | 0.011 | 0.08 | 1977 | 18.86 | 0.08 | 3400 | 0 |
| PlayerEndOfFrame | Render Thread | 110.87 | 0.065 | 1.78 | 1200 | 10.24 | 0.54 | 1700 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
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
| 1519 | 18.55 | 10.14 | 10.14 | 2.37 | 5.26 | 48 |
| 966 | 18.51 | 8.77 | 8.77 | 2.04 | 5.22 | 48 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
