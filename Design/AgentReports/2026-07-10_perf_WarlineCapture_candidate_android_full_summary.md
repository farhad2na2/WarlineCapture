# Android Profiler Capture Summary

Date: 2026-07-10 15:44:42 +02:00
Capture: `/Users/farhad/Projects/WarlineCapture/ProfilerCaptures/WarlineCapture_2026-07-10_15-40-candidate.data.raw`
Profiler frames: `1..2000`
Scanned frames: `2000`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 24.02 ms (41.6 FPS) |
| P50 frame | 21.55 ms (46.4 FPS) |
| P95 frame | 28.13 ms (35.5 FPS) |
| P99 frame | 38.86 ms (25.7 FPS) |
| Max frame | 3071.29 ms (0.3 FPS) |
| Frames over budget | 1833/2000 |
| Avg CPU active | 18.29 ms |
| P95 CPU active | 15.60 ms |
| Avg GPU time | 18.81 ms |
| P95 GPU time | 20.60 ms |
| Total GC allocated | 345813295 bytes |

## Render Counters

| Counter | Avg | P50 | P95 | Max |
|---|---:|---:|---:|---:|
| Draw calls | 0 | 0 | 0 | 0 |
| Batches | 0 | 0 | 0 | 0 |
| SetPass calls | 47 | 51 | 52 | 738 |
| Triangles | 951081 | 1059735 | 1060535 | 1162925 |
| Vertices | 1793128 | 1997887 | 2002425 | 2187991 |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 47845.10 | 23.923 | 3071.11 | 216 | 503.46 | 4.76 | 2000 | 328027592 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 28657.17 | 14.329 | 3068.85 | 216 | 31.98 | 0.34 | 27456 | 0 |
| Gfx.PresentFrame | Render Thread | 12862.07 | 6.431 | 53.13 | 193 | 420.87 | 1.36 | 1999 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 12803.61 | 6.402 | 58.28 | 221 | 218.67 | 3.66 | 12000 | 3508747 |
| WaitForTargetFPS | Main Thread | 11344.80 | 5.672 | 29.94 | 204 | 11308.67 | 29.93 | 2000 | 0 |
| SimulationSystemGroup | Main Thread | 8975.55 | 4.488 | 58.28 | 221 | 3.97 | 0.01 | 2000 | 3419686 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 8958.99 | 4.479 | 58.28 | 221 | 414.09 | 1.31 | 2000 | 3419686 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 7997.79 | 3.999 | 272.00 | 192 | 180.09 | 78.25 | 2007 | 1449700 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 6135.16 | 3.068 | 3061.42 | 216 | 5.21 | 0.04 | 2000 | 321978127 |
| BehaviourUpdate | Main Thread | 6129.95 | 3.065 | 3061.42 | 216 | 76.45 | 1.08 | 2000 | 321978127 |
| PresentationSystemGroup | Main Thread | 3273.24 | 1.637 | 27.25 | 221 | 7.63 | 0.07 | 2000 | 27506 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 3256.22 | 1.628 | 27.24 | 221 | 105.95 | 0.66 | 2000 | 27506 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 1408.65 | 0.704 | 56.93 | 221 | 153.68 | 3.03 | 1780 | 25836674 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 1309.90 | 0.655 | 8.41 | 221 | 39.45 | 0.27 | 2000 | 20770 |
| Default World Game.Runtime.UnitPathfindingSystem | Main Thread | 1168.86 | 0.584 | 9.14 | 241 | 1144.20 | 8.97 | 2000 | 2047124 |
| LateBehaviourUpdate | Main Thread | 573.41 | 0.287 | 35.96 | 240 | 27.74 | 0.16 | 2000 | 658384 |
| Canvas.RenderOverlays | Render Thread | 557.28 | 0.279 | 1.21 | 54 | 304.69 | 1.09 | 10635 | 0 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 542.56 | 0.271 | 20.64 | 1 | 124.11 | 0.31 | 2007 | 417340 |
| BuildingPlacementRuntimeTick.EnqueueMapBuildingPlacements | Main Thread | 527.72 | 0.264 | 34.92 | 221 | 406.01 | 28.35 | 20 | 21374822 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 505.10 | 0.253 | 2.16 | 979 | 120.42 | 1.02 | 2000 | 86 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 470.33 | 0.235 | 1.25 | 221 | 103.08 | 1.06 | 3823 | 3196 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 449.70 | 0.225 | 35.63 | 240 | 17.56 | 0.50 | 1761 | 658128 |
| Gfx.InitializeBuffer | Render Thread | 446.01 | 0.223 | 28.94 | 205 | 151.18 | 8.77 | 2640 | 0 |
| Canvas.RenderOverlays | Main Thread | 442.54 | 0.221 | 0.82 | 72 | 129.43 | 0.69 | 10641 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 365.25 | 0.183 | 56.62 | 214 | 56.35 | 0.29 | 2000 | 60161 |
| Gfx.CreateBufferResource | Render Thread | 350.36 | 0.175 | 34.30 | 203 | 349.65 | 34.30 | 3154 | 0 |
| Gfx.WaitForPresentOnGfxThread | Main Thread | 305.70 | 0.153 | 56.21 | 194 | 1.19 | 0.03 | 2000 | 0 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIUtility.BeginGUI() [Invoke] | Main Thread | 288.36 | 0.144 | 218.18 | 192 | 270.31 | 203.70 | 3618 | 722434 |
| Canvas.BuildBatch | Main Thread | 281.70 | 0.141 | 1.92 | 199 | 281.67 | 1.92 | 10691 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 278.68 | 0.139 | 1.98 | 1458 | 122.68 | 0.40 | 2000 | 40 |
| UnitImpostors.DrawCulled | Main Thread | 268.67 | 0.134 | 25.22 | 240 | 23.40 | 0.35 | 1761 | 656368 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 263.32 | 0.132 | 2.26 | 1490 | 95.50 | 1.92 | 2000 | 190 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 258.98 | 0.129 | 2.68 | 214 | 41.86 | 0.16 | 2000 | 876 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 258.12 | 0.129 | 1.80 | 1 | 134.80 | 1.56 | 2000 | 40 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 257.36 | 0.129 | 1.93 | 214 | 112.69 | 1.86 | 2000 | 120 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 243.52 | 0.122 | 1.53 | 891 | 112.97 | 0.35 | 2000 | 296 |
| GameplayRuntimeUpdate.Selection | Main Thread | 231.79 | 0.116 | 4.49 | 241 | 33.89 | 0.13 | 1760 | 331178 |
| BuildingPlacementRuntimeTick.UpdateActiveProductionTransports | Main Thread | 214.67 | 0.107 | 2.03 | 1977 | 1.25 | 0.06 | 622 | 1005200 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 212.60 | 0.106 | 13.72 | 221 | 199.18 | 11.07 | 2000 | 1856 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 196.43 | 0.098 | 0.36 | 221 | 97.35 | 0.28 | 2000 | 40 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WaitForTargetFPS | Main Thread | 11332.86 | 5.666 | 29.94 | 204 | 11306.42 | 29.93 | 1828 | 0 |
| Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] | Main Thread | 3245.65 | 1.623 | 3060.86 | 216 | 2506.15 | 2411.44 | 45 | 279844819 |
| Profiler.FlushMemoryCounters | Main Thread | 2323.93 | 1.162 | 3.07 | 1920 | 2323.73 | 3.07 | 2000 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 2569.32 | 1.285 | 7.23 | 274 | 1375.63 | 2.01 | 2002 | 386502 |
| WaitForJobGroupID | Main Thread | 3587.93 | 1.794 | 8.49 | 995 | 1165.58 | 2.96 | 54194 | 0 |
| Default World Game.Runtime.UnitPathfindingSystem | Main Thread | 1148.18 | 0.574 | 9.14 | 241 | 1123.52 | 8.97 | 353 | 2047124 |
| JobHandle.Complete | Main Thread | 3511.48 | 1.756 | 8.25 | 1477 | 630.84 | 2.72 | 156235 | 0 |
| Inl_On Record Render Graph | Main Thread | 815.30 | 0.408 | 22.03 | 1 | 604.18 | 18.93 | 2007 | 87350 |
| GC.Alloc | Main Thread | 591.39 | 0.296 | 560.99 | 216 | 591.39 | 560.99 | 8738557 | 320339282 |
| ThreatDetectionWarningSystem:ThreatScanJob (Burst) | Main Thread | 557.27 | 0.279 | 4.91 | 1379 | 557.27 | 4.91 | 134 | 0 |
| PlayerLoop | Main Thread | 47845.10 | 23.923 | 3071.11 | 216 | 503.46 | 4.76 | 2000 | 328027592 |
| SRPBatcher.Flush | Main Thread | 476.48 | 0.238 | 2.25 | 195 | 448.43 | 2.16 | 48548 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 8932.49 | 4.466 | 58.28 | 221 | 413.80 | 1.31 | 1998 | 3416994 |
| BuildingPlacementRuntimeTick.EnqueueMapBuildingPlacements | Main Thread | 527.72 | 0.264 | 34.92 | 221 | 406.01 | 28.35 | 20 | 21374822 |
| OnPerformCulling | Main Thread | 364.72 | 0.182 | 1.54 | 240 | 364.52 | 1.54 | 3581 | 1964 |
| Semaphore.WaitForSignal | Main Thread | 363.63 | 0.182 | 56.21 | 194 | 363.63 | 56.21 | 133 | 0 |
| TextureStreamingManager.UpdateRenderers | Main Thread | 337.53 | 0.169 | 1.44 | 1658 | 337.48 | 1.44 | 1807 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 3415.36 | 1.708 | 20.34 | 192 | 323.58 | 1.01 | 1806 | 72858 |
| ClipperRegistry.Cull | Main Thread | 320.47 | 0.160 | 6.38 | 177 | 320.42 | 6.38 | 2003 | 870 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 291.62 | 0.146 | 1.59 | 1313 | 291.62 | 1.59 | 1725 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 339.78 | 0.170 | 7.09 | 545 | 287.00 | 7.04 | 1699 | 1072428 |
| ExecuteRenderQueueJob | Main Thread | 281.71 | 0.141 | 5.76 | 195 | 281.71 | 5.76 | 1914 | 0 |
| Canvas.BuildBatch | Main Thread | 279.61 | 0.140 | 1.92 | 199 | 279.57 | 1.92 | 10584 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 290.98 | 0.145 | 18.31 | 274 | 264.85 | 4.87 | 1999 | 1317260 |
| SRPBRender.ApplyShader | Main Thread | 283.28 | 0.142 | 4.37 | 192 | 261.90 | 0.46 | 41364 | 0 |
| Inl_RenderCameraStack | Main Thread | 7643.37 | 3.822 | 271.92 | 192 | 244.82 | 4.59 | 2004 | 1116356 |
| Idle | Main Thread | 220.57 | 0.110 | 6.49 | 1477 | 220.57 | 6.49 | 122 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 12677.57 | 6.339 | 74.31 | 214 | 218.25 | 3.71 | 11970 | 3464637 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIUtility.BeginGUI() [Invoke] | Main Thread | 232.94 | 0.116 | 218.18 | 192 | 216.78 | 203.70 | 386 | 127746 |
| RegisterMaterialsAndMeshes | Main Thread | 210.75 | 0.105 | 2.67 | 214 | 210.61 | 2.65 | 1938 | 798 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 28625.99 | 14.313 | 3068.84 | 216 | 28625.99 | 3068.84 | 27497 | 0 |
| GfxDeviceVK.Present | Render Thread | 12072.14 | 6.036 | 51.33 | 193 | 12072.14 | 51.33 | 1999 | 0 |
| DrawBuffersBatchMode | Render Thread | 2208.08 | 1.104 | 22.12 | 243 | 2200.62 | 22.12 | 48521 | 0 |
| RenderLoop | Render Thread | 19644.69 | 9.822 | 255.01 | 192 | 930.95 | 24.55 | 28246 | 0 |
| ExecuteRenderGraph | Render Thread | 4654.43 | 2.327 | 232.00 | 192 | 523.12 | 9.50 | 2006 | 0 |
| RenderLoop.Draw | Render Thread | 443.39 | 0.222 | 2.87 | 1695 | 422.23 | 2.87 | 12552 | 0 |
| Gfx.PresentFrame | Render Thread | 12862.07 | 6.431 | 53.13 | 193 | 420.87 | 1.36 | 1999 | 0 |
| AcquireNextFrame | Render Thread | 387.48 | 0.194 | 1.54 | 38 | 387.48 | 1.54 | 1999 | 0 |
| Gfx.CreateBufferResource | Render Thread | 350.36 | 0.175 | 34.30 | 203 | 349.65 | 34.30 | 3154 | 0 |
| Canvas.RenderOverlays | Render Thread | 557.28 | 0.279 | 1.21 | 54 | 304.69 | 1.10 | 10635 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 274.91 | 0.137 | 1.21 | 193 | 274.91 | 1.21 | 4035 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 1598.80 | 0.799 | 6.25 | 195 | 213.38 | 0.99 | 17921 | 0 |
| ScheduleGeometryJobs | Render Thread | 214.62 | 0.107 | 1.75 | 1929 | 212.22 | 1.75 | 8900 | 0 |
| Gfx.InitializeBuffer | Render Thread | 446.01 | 0.223 | 28.94 | 205 | 151.18 | 8.77 | 2640 | 0 |
| Gfx.CreateTexture | Render Thread | 113.11 | 0.057 | 27.06 | 203 | 113.11 | 27.06 | 78 | 0 |
| Gfx.SetRenderTarget | Render Thread | 97.30 | 0.049 | 5.66 | 240 | 97.28 | 5.66 | 8557 | 0 |
| GpuRecorder.FrameTick | Render Thread | 94.15 | 0.047 | 0.29 | 472 | 94.15 | 0.29 | 1999 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 70.53 | 0.035 | 1.57 | 1680 | 70.53 | 1.57 | 134 | 0 |
| BlitFinalToBackBuffer | Render Thread | 68.15 | 0.034 | 1.44 | 483 | 67.83 | 1.44 | 1977 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 875.91 | 0.438 | 21.33 | 243 | 35.23 | 0.18 | 1813 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 216 | 3071.29 | 3071.28 | 3071.28 | 1.50 | 9.35 | 277548011 |
| 192 | 323.13 | 323.12 | 323.12 | 34.89 | 82.34 | 161522 |
| 1 | 272.91 | 272.89 | 272.89 | 35.65 | 6.18 | 15102672 |
| 214 | 219.07 | 219.01 | 219.01 | 2.05 | 17.37 | 2214343 |
| 221 | 171.98 | 171.95 | 171.95 | 7.00 | 19.96 | 11452520 |
| 274 | 109.37 | 109.34 | 109.34 | 5.12 | 24.48 | 4907348 |
| 204 | 98.84 | 16.16 | 16.16 | 3.55 | 19.30 | 1076 |
| 240 | 95.04 | 95.02 | 95.02 | 45.11 | 28.33 | 1700538 |
| 222 | 87.22 | 87.20 | 87.20 | 3.00 | 0.00 | 2632712 |
| 194 | 69.95 | 19.08 | 13.49 | 19.08 | 21.56 | 26300 |
| 223 | 52.33 | 52.31 | 52.31 | 2.47 | 20.83 | 880962 |
| 241 | 50.08 | 50.06 | 50.06 | 4.49 | 26.18 | 137728 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
