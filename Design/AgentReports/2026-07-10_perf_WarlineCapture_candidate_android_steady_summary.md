# Android Profiler Capture Summary

Date: 2026-07-10 15:45:40 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture/ProfilerCaptures/WarlineCapture_2026-07-10_15-40-candidate.data.raw`
Profiler frames: `300..1999`
Scanned frames: `1700`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 22.17 ms (45.1 FPS) |
| P50 frame | 21.61 ms (46.3 FPS) |
| P95 frame | 26.80 ms (37.3 FPS) |
| P99 frame | 31.13 ms (32.1 FPS) |
| Max frame | 39.07 ms (25.6 FPS) |
| Frames over budget | 1645/1700 |
| Avg CPU active | 16.77 ms |
| P95 CPU active | 16.77 ms |
| Avg GPU time | 20.23 ms |
| P95 GPU time | 20.87 ms |
| Total GC allocated | 7612760 bytes |

## Render Counters

| Counter | Avg | P50 | P95 | Max |
|---|---:|---:|---:|---:|
| Draw calls | 0 | 0 | 0 | 0 |
| Batches | 0 | 0 | 0 | 0 |
| SetPass calls | 51 | 51 | 52 | 54 |
| Triangles | 1058417 | 1060533 | 1060535 | 1065985 |
| Vertices | 1995230 | 1999483 | 1999487 | 2025443 |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 37539.52 | 22.082 | 38.83 | 487 | 420.55 | 1.46 | 1700 | 7612760 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 20707.59 | 12.181 | 28.82 | 1586 | 28.18 | 0.34 | 24083 | 0 |
| Gfx.PresentFrame | Render Thread | 11836.29 | 6.963 | 20.43 | 1261 | 354.61 | 1.36 | 1700 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 11717.46 | 6.893 | 15.95 | 1586 | 179.01 | 0.36 | 10200 | 3309146 |
| WaitForTargetFPS | Main Thread | 9132.49 | 5.372 | 19.25 | 1261 | 9108.86 | 19.24 | 1700 | 0 |
| SimulationSystemGroup | Main Thread | 8343.46 | 4.908 | 15.95 | 1586 | 3.44 | 0.01 | 1700 | 3308176 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 8328.94 | 4.899 | 15.93 | 1586 | 367.57 | 1.31 | 1700 | 3308176 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 6683.04 | 3.931 | 8.67 | 1801 | 89.54 | 0.76 | 1700 | 816000 |
| PresentationSystemGroup | Main Thread | 2972.41 | 1.748 | 4.11 | 1683 | 6.71 | 0.07 | 1700 | 970 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 2957.43 | 1.740 | 4.09 | 1683 | 93.85 | 0.37 | 1700 | 970 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 2026.67 | 1.192 | 5.75 | 733 | 4.38 | 0.04 | 1700 | 3487478 |
| BehaviourUpdate | Main Thread | 2022.30 | 1.190 | 5.75 | 733 | 67.43 | 1.08 | 1700 | 3487478 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 1235.23 | 0.727 | 2.66 | 1323 | 36.05 | 0.13 | 1700 | 0 |
| Default World Game.Runtime.UnitPathfindingSystem | Main Thread | 1132.79 | 0.666 | 7.07 | 1681 | 1109.31 | 7.02 | 1700 | 2006056 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 793.35 | 0.467 | 4.55 | 733 | 143.67 | 1.15 | 1700 | 1362304 |
| LateBehaviourUpdate | Main Thread | 517.88 | 0.305 | 0.85 | 1967 | 26.17 | 0.16 | 1700 | 0 |
| Canvas.RenderOverlays | Render Thread | 513.26 | 0.302 | 0.44 | 481 | 266.16 | 0.28 | 10200 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 484.44 | 0.285 | 2.16 | 979 | 113.94 | 1.02 | 1700 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 441.23 | 0.260 | 1.14 | 610 | 94.34 | 1.06 | 3400 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 434.38 | 0.256 | 1.39 | 564 | 111.53 | 0.16 | 1700 | 64 |
| Canvas.RenderOverlays | Main Thread | 407.14 | 0.239 | 0.39 | 1686 | 111.87 | 0.13 | 10200 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 399.74 | 0.235 | 0.76 | 1967 | 16.47 | 0.07 | 1700 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 266.75 | 0.157 | 1.98 | 1458 | 117.23 | 0.40 | 1700 | 0 |
| Canvas.BuildBatch | Main Thread | 252.49 | 0.149 | 1.50 | 998 | 252.49 | 1.50 | 10200 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 248.83 | 0.146 | 0.90 | 664 | 46.11 | 0.22 | 1700 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 244.45 | 0.144 | 1.15 | 761 | 106.16 | 0.37 | 1700 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 242.91 | 0.143 | 2.26 | 1490 | 82.63 | 1.11 | 1700 | 0 |
| UnitImpostors.DrawCulled | Main Thread | 235.19 | 0.138 | 0.52 | 471 | 22.25 | 0.09 | 1700 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 233.20 | 0.137 | 1.53 | 891 | 107.98 | 0.24 | 1700 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 227.95 | 0.134 | 0.30 | 1002 | 38.97 | 0.16 | 1700 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 219.77 | 0.129 | 1.15 | 901 | 32.68 | 0.12 | 1700 | 305136 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 214.67 | 0.126 | 1.38 | 1587 | 114.47 | 1.26 | 1700 | 0 |
| BuildingPlacementRuntimeTick.UpdateActiveProductionTransports | Main Thread | 214.56 | 0.126 | 2.03 | 1977 | 1.25 | 0.06 | 619 | 1005200 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 187.25 | 0.110 | 0.33 | 1686 | 92.78 | 0.19 | 1700 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 186.89 | 0.110 | 0.49 | 894 | 90.41 | 0.18 | 1700 | 0 |
| Default World Unity.Rendering.UpdatePresentationSystemGroup | Main Thread | 181.36 | 0.107 | 1.27 | 471 | 10.68 | 0.32 | 1700 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 181.27 | 0.107 | 1.28 | 337 | 176.54 | 1.15 | 1700 | 0 |
| Default World Game.Runtime.UnitGridMovementSystem | Main Thread | 176.36 | 0.104 | 0.60 | 1459 | 169.32 | 0.60 | 1700 | 0 |
| UnitImpostors.BuildMatrices | Main Thread | 152.22 | 0.090 | 0.36 | 1783 | 152.22 | 0.36 | 1737 | 0 |
| Default World Game.Runtime.UnitAttackSystem | Main Thread | 151.06 | 0.089 | 0.55 | 1144 | 147.08 | 0.54 | 1700 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 9128.13 | 5.369 | 19.25 | 1261 | 9107.31 | 19.24 | 1576 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 2063.07 | 1.214 | 3.07 | 1920 | 2063.07 | 3.07 | 1700 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 2299.00 | 1.352 | 3.84 | 976 | 1271.98 | 2.01 | 1700 | 64 |
| Default World Game.Runtime.UnitPathfindingSystem | Main Thread | 1113.19 | 0.655 | 7.07 | 1681 | 1089.70 | 7.02 | 345 | 2006056 |
| WaitForJobGroupID | Main Thread | 3364.17 | 1.979 | 8.49 | 995 | 1071.87 | 2.23 | 50732 | 0 |
| JobHandle.Complete | Main Thread | 3338.29 | 1.964 | 8.25 | 1477 | 589.68 | 2.19 | 149373 | 0 |
| ThreatDetectionWarningSystem:ThreatScanJob (Burst) | Main Thread | 527.61 | 0.310 | 4.91 | 1379 | 527.61 | 4.91 | 127 | 0 |
| Inl_On Record Render Graph | Main Thread | 662.81 | 0.390 | 0.97 | 1801 | 488.17 | 0.59 | 1700 | 0 |
| PlayerLoop | Main Thread | 37539.52 | 22.082 | 38.83 | 487 | 420.55 | 1.46 | 1700 | 7612760 |
| SRPBatcher.Flush | Main Thread | 439.34 | 0.258 | 1.13 | 610 | 415.34 | 1.12 | 45897 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 8328.94 | 4.899 | 15.93 | 1586 | 367.57 | 1.31 | 1700 | 3308176 |
| OnPerformCulling | Main Thread | 346.89 | 0.204 | 0.59 | 1898 | 346.89 | 0.59 | 3400 | 0 |
| TextureStreamingManager.UpdateRenderers | Main Thread | 314.93 | 0.185 | 1.44 | 1658 | 314.88 | 1.44 | 1700 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 3156.73 | 1.857 | 5.73 | 1111 | 302.25 | 0.51 | 1700 | 0 |
| ClipperRegistry.Cull | Main Thread | 283.94 | 0.167 | 1.29 | 564 | 283.94 | 1.29 | 1700 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 281.49 | 0.166 | 1.59 | 1313 | 281.49 | 1.59 | 1665 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 333.70 | 0.196 | 7.09 | 545 | 281.04 | 7.04 | 1649 | 1070920 |
| ExecuteRenderQueueJob | Main Thread | 258.90 | 0.152 | 1.19 | 426 | 258.90 | 1.19 | 1792 | 0 |
| Canvas.BuildBatch | Main Thread | 252.40 | 0.148 | 1.50 | 998 | 252.40 | 1.50 | 10188 | 0 |
| SRPBRender.ApplyShader | Main Thread | 259.21 | 0.152 | 0.38 | 300 | 245.45 | 0.37 | 39097 | 0 |
| Idle | Main Thread | 213.97 | 0.126 | 6.49 | 1477 | 213.97 | 6.49 | 113 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 213.37 | 0.126 | 1.22 | 454 | 212.91 | 1.22 | 1700 | 217600 |
| Inl_RenderCameraStack | Main Thread | 6463.89 | 3.802 | 8.56 | 1801 | 205.15 | 0.59 | 1699 | 815520 |
| BuildingProductionRuntimeTick.UpdateActiveProductionTransports | Main Thread | 197.14 | 0.116 | 2.02 | 1977 | 195.89 | 2.01 | 181 | 925536 |
| RegisterMaterialsAndMeshes | Main Thread | 183.06 | 0.108 | 0.23 | 603 | 183.06 | 0.23 | 1644 | 0 |
| Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] | Main Thread | 204.40 | 0.120 | 0.44 | 1612 | 179.19 | 0.42 | 1673 | 66920 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 11703.63 | 6.884 | 19.29 | 1586 | 178.93 | 0.40 | 10194 | 3303562 |
| RenderLoop.Draw | Main Thread | 582.92 | 0.343 | 0.88 | 1004 | 174.66 | 0.26 | 11944 | 0 |
| UpdateAllBatches | Main Thread | 649.60 | 0.382 | 1.41 | 1997 | 171.39 | 1.16 | 1655 | 0 |
| TransformChangeSystem | Main Thread | 214.57 | 0.126 | 0.88 | 1181 | 170.88 | 0.85 | 60281 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 20679.41 | 12.164 | 29.01 | 1586 | 20679.41 | 29.01 | 24083 | 0 |
| GfxDeviceVK.Present | Render Thread | 11166.47 | 6.569 | 20.00 | 1261 | 11166.47 | 20.00 | 1700 | 0 |
| DrawBuffersBatchMode | Render Thread | 2056.04 | 1.209 | 6.93 | 1695 | 2055.80 | 6.93 | 45897 | 0 |
| RenderLoop | Render Thread | 17018.48 | 10.011 | 24.07 | 331 | 793.36 | 2.13 | 24891 | 0 |
| ExecuteRenderGraph | Render Thread | 3973.82 | 2.338 | 11.07 | 1695 | 451.61 | 2.87 | 1700 | 0 |
| RenderLoop.Draw | Render Thread | 400.90 | 0.236 | 2.87 | 1695 | 399.93 | 2.87 | 11944 | 0 |
| Gfx.PresentFrame | Render Thread | 11836.29 | 6.963 | 20.43 | 1261 | 354.61 | 1.36 | 1700 | 0 |
| AcquireNextFrame | Render Thread | 327.09 | 0.192 | 1.30 | 1304 | 327.09 | 1.30 | 1700 | 0 |
| Canvas.RenderOverlays | Render Thread | 513.26 | 0.302 | 0.84 | 481 | 266.16 | 0.42 | 10200 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 233.74 | 0.137 | 0.72 | 1930 | 233.74 | 0.72 | 3400 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 1486.00 | 0.874 | 5.68 | 1695 | 201.16 | 0.99 | 16997 | 0 |
| ScheduleGeometryJobs | Render Thread | 192.42 | 0.113 | 1.75 | 1929 | 192.42 | 1.75 | 8500 | 0 |
| GpuRecorder.FrameTick | Render Thread | 81.48 | 0.048 | 0.29 | 472 | 81.48 | 0.29 | 1700 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 60.31 | 0.035 | 1.57 | 1680 | 60.31 | 1.57 | 114 | 0 |
| Gfx.SetRenderTarget | Render Thread | 58.84 | 0.035 | 0.91 | 578 | 58.84 | 0.91 | 6914 | 0 |
| BlitFinalToBackBuffer | Render Thread | 54.94 | 0.032 | 1.44 | 483 | 54.94 | 1.44 | 1700 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 812.67 | 0.478 | 3.24 | 1335 | 33.03 | 0.09 | 1700 | 0 |
| UI.RenderOverlays | Render Thread | 309.16 | 0.182 | 0.48 | 481 | 28.25 | 0.06 | 1700 | 0 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 20707.59 | 12.181 | 29.03 | 1586 | 28.18 | 0.34 | 24083 | 0 |
| Profiler.FlushRenderCounters | Render Thread | 27.84 | 0.016 | 0.35 | 1944 | 27.84 | 0.35 | 1700 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 487 | 39.07 | 19.59 | 19.59 | 3.96 | 20.61 | 9033 |
| 1141 | 37.13 | 16.56 | 16.56 | 4.62 | 21.08 | 1808 |
| 883 | 37.11 | 15.20 | 15.20 | 3.86 | 20.82 | 1808 |
| 331 | 35.16 | 15.69 | 15.69 | 4.75 | 22.54 | 1072 |
| 996 | 34.90 | 23.05 | 23.05 | 4.21 | 0.00 | 1808 |
| 1681 | 34.21 | 31.50 | 31.50 | 4.89 | 26.16 | 10446 |
| 1816 | 33.89 | 18.22 | 18.22 | 4.15 | 20.43 | 11245 |
| 336 | 33.78 | 20.97 | 20.97 | 4.40 | 22.94 | 8908 |
| 1586 | 33.20 | 33.19 | 33.19 | 2.73 | 21.13 | 12254 |
| 1261 | 32.84 | 13.58 | 13.58 | 3.25 | 22.43 | 1808 |
| 300 | 32.78 | 19.64 | 19.64 | 5.52 | 22.01 | 1112 |
| 931 | 32.62 | 13.15 | 13.15 | 3.29 | 20.73 | 1848 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
