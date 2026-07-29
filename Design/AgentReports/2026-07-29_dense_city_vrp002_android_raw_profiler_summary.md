# Android Profiler Capture Summary

Date: 2026-07-29 09:39:35 +02:00
Capture: `C:\Users\zfoul\Projects\WarlineCapture\Build\AndroidDenseCandidateProfiler\vrp002_dense_city.raw`
Profiler frames: `300..799`
Scanned frames: `500`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 71.73 ms (13.9 FPS) |
| P50 frame | 66.45 ms (15.0 FPS) |
| P95 frame | 85.10 ms (11.8 FPS) |
| P99 frame | 87.75 ms (11.4 FPS) |
| Max frame | 109.68 ms (9.1 FPS) |
| Frames over budget | 500/500 |
| Avg CPU active | 71.71 ms |
| P95 CPU active | 85.08 ms |
| Avg GPU time | 12.59 ms |
| P95 GPU time | 18.46 ms |
| Total GC allocated | 3425624 bytes |

## Render Counters

| Counter | Avg | P50 | P95 | Max |
|---|---:|---:|---:|---:|
| Draw calls | 0 | 0 | 0 | 0 |
| Batches | 0 | 0 | 0 | 0 |
| SetPass calls | 32 | 32 | 33 | 33 |
| Triangles | 785846 | 785846 | 785848 | 785848 |
| Vertices | 1423207 | 1423207 | 1423211 | 1423211 |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 35840.95 | 71.682 | 109.62 | 791 | 95.81 | 0.53 | 500 | 3425624 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 34179.29 | 68.359 | 104.98 | 791 | 7.21 | 0.85 | 8423 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 32120.22 | 64.240 | 42.88 | 443 | 50.81 | 0.11 | 3500 | 222142 |
| PresentationSystemGroup | Main Thread | 20168.87 | 40.338 | 42.88 | 443 | 0.91 | 0.00 | 500 | 40000 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 20166.64 | 40.333 | 42.88 | 443 | 22.13 | 0.09 | 500 | 40000 |
| Default World Game.Rendering.UnitFactionTintTargetBackfillSystem | Main Thread | 19472.86 | 38.946 | 41.65 | 443 | 19455.33 | 41.62 | 500 | 0 |
| SimulationSystemGroup | Main Thread | 11776.78 | 23.554 | 39.10 | 475 | 0.63 | 0.00 | 500 | 170820 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 11774.47 | 23.549 | 39.09 | 475 | 72.61 | 0.27 | 500 | 170820 |
| Default World Game.Rendering.UnitMassRenderSettingsSystem | Main Thread | 6534.13 | 13.068 | 13.91 | 575 | 6512.61 | 13.83 | 500 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 1467.96 | 2.936 | 6.06 | 791 | 13.90 | 0.06 | 500 | 401752 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 899.82 | 1.800 | 3.51 | 504 | 42.29 | 0.20 | 500 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 458.57 | 0.917 | 30.97 | 791 | 0.55 | 0.00 | 500 | 2781730 |
| BehaviourUpdate | Main Thread | 458.02 | 0.916 | 30.97 | 791 | 22.84 | 0.17 | 500 | 2781730 |
| Gfx.PresentFrame | Render Thread | 384.79 | 0.770 | 1.97 | 504 | 99.76 | 0.92 | 500 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 289.76 | 0.580 | 0.89 | 474 | 8.96 | 0.08 | 500 | 0 |
| Canvas.RenderOverlays | Render Thread | 154.34 | 0.309 | 0.33 | 366 | 73.51 | 0.23 | 3500 | 0 |
| Canvas.RenderOverlays | Main Thread | 102.65 | 0.205 | 0.18 | 479 | 24.90 | 0.07 | 3500 | 0 |
| LateBehaviourUpdate | Main Thread | 95.40 | 0.191 | 0.31 | 418 | 5.49 | 0.08 | 500 | 20000 |
| GameplayRuntimeUpdate.Selection | Main Thread | 89.20 | 0.178 | 0.31 | 742 | 5.73 | 0.06 | 500 | 316000 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 82.82 | 0.166 | 3.12 | 791 | 18.28 | 2.91 | 2000 | 1712 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 81.64 | 0.163 | 29.68 | 791 | 15.55 | 0.09 | 500 | 2077526 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 79.09 | 0.158 | 0.46 | 698 | 16.90 | 0.07 | 500 | 0 |
| Canvas.BuildBatch | Main Thread | 78.66 | 0.157 | 0.26 | 472 | 78.66 | 0.26 | 3500 | 0 |
| GameplayRuntimeUpdate.MainMenu | Main Thread | 69.18 | 0.138 | 0.69 | 567 | 29.70 | 0.24 | 500 | 45710 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 65.13 | 0.130 | 0.29 | 742 | 58.09 | 0.27 | 500 | 220000 |
| BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState | Main Thread | 51.94 | 0.104 | 29.54 | 791 | 44.14 | 23.99 | 500 | 2051812 |
| BuildingDefenseAttackSystem.TargetSelection | Main Thread | 50.14 | 0.100 | 0.27 | 445 | 50.14 | 0.27 | 3250 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 48.62 | 0.097 | 0.23 | 678 | 18.56 | 0.08 | 500 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 46.79 | 0.094 | 0.23 | 477 | 20.77 | 0.12 | 500 | 20000 |
| UnityEngine.UIModule.dll!UnityEngine::Canvas.SendWillRenderCanvases() [Invoke] | Main Thread | 43.53 | 0.087 | 0.31 | 619 | 26.76 | 0.13 | 500 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 40.88 | 0.082 | 0.14 | 760 | 2.13 | 0.04 | 500 | 0 |
| UnitSelectionMarkerSystem:CollectSelectionMarkerChangesJob (Burst) | Main Thread | 40.23 | 0.080 | 0.33 | 698 | 40.23 | 0.33 | 500 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 39.96 | 0.080 | 0.43 | 311 | 23.31 | 0.12 | 500 | 0 |
| MainMenuPlayUI.MinimapUpdate | Main Thread | 38.50 | 0.077 | 0.45 | 567 | 38.50 | 0.45 | 500 | 0 |
| Default World Game.Runtime.UnitAttackSystem | Main Thread | 38.36 | 0.077 | 0.18 | 504 | 34.46 | 0.17 | 500 | 20000 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 36.89 | 0.074 | 0.23 | 504 | 8.79 | 0.16 | 500 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 36.09 | 0.072 | 0.21 | 566 | 11.75 | 0.06 | 500 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 34.26 | 0.069 | 0.14 | 658 | 34.26 | 0.14 | 500 | 0 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 33.65 | 0.067 | 0.45 | 679 | 8.18 | 0.09 | 500 | 0 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 33.29 | 0.067 | 0.21 | 473 | 10.00 | 0.07 | 500 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Default World Game.Rendering.UnitFactionTintTargetBackfillSystem | Main Thread | 19472.86 | 38.946 | 41.65 | 443 | 19455.33 | 41.62 | 500 | 0 |
| Default World Game.Rendering.UnitMassRenderSettingsSystem | Main Thread | 6534.13 | 13.068 | 13.91 | 575 | 6512.61 | 13.83 | 500 | 0 |
| ThreatDetectionWarningSystem:ThreatScanJob (Burst) | Main Thread | 2605.51 | 5.211 | 17.33 | 727 | 2605.51 | 17.33 | 156 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 754.83 | 1.510 | 3.10 | 504 | 754.83 | 3.10 | 498 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 478.38 | 0.957 | 1.58 | 472 | 478.38 | 1.58 | 500 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 701.06 | 1.402 | 1.92 | 472 | 456.34 | 1.33 | 500 | 0 |
| WaitForJobGroupID | Main Thread | 4335.10 | 8.670 | 23.94 | 487 | 363.45 | 4.96 | 15350 | 0 |
| Idle | Main Thread | 197.08 | 0.394 | 20.22 | 394 | 197.08 | 20.22 | 29 | 0 |
| Default World Game.Runtime.StaticGridBlockerUpdateSystem | Main Thread | 169.52 | 0.339 | 0.48 | 567 | 167.82 | 0.46 | 500 | 0 |
| JobHandle.Complete | Main Thread | 4275.45 | 8.551 | 23.47 | 487 | 148.54 | 2.16 | 40913 | 0 |
| Inl_On Record Render Graph | Main Thread | 139.78 | 0.280 | 0.47 | 641 | 111.57 | 0.35 | 500 | 0 |
| SRPBatcher.Flush | Main Thread | 109.45 | 0.219 | 0.40 | 482 | 109.45 | 0.40 | 7000 | 0 |
| PlayerLoop | Main Thread | 35840.95 | 71.682 | 109.62 | 791 | 95.81 | 0.53 | 500 | 3425624 |
| Game.Composition.dll!Game.Composition::MatchSceneView.OnGUI() [Invoke] | Main Thread | 89.62 | 0.179 | 0.32 | 791 | 82.92 | 0.30 | 1000 | 216000 |
| BuildingDefenseAttackSystem.TargetCollection | Main Thread | 81.17 | 0.162 | 0.51 | 503 | 81.01 | 0.51 | 250 | 0 |
| Canvas.BuildBatch | Main Thread | 78.66 | 0.157 | 0.26 | 472 | 78.66 | 0.26 | 3500 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 11774.47 | 23.549 | 39.09 | 475 | 72.61 | 0.27 | 500 | 170820 |
| OnPerformCulling | Main Thread | 64.39 | 0.129 | 0.23 | 608 | 64.39 | 0.23 | 1000 | 0 |
| FrustumCullingJob (Burst) | Main Thread | 59.82 | 0.120 | 0.54 | 491 | 59.82 | 0.54 | 430 | 0 |
| CanvasRenderer.SyncTransform | Main Thread | 59.03 | 0.118 | 0.20 | 630 | 59.03 | 0.20 | 217500 | 0 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 65.13 | 0.130 | 0.29 | 742 | 58.09 | 0.27 | 500 | 220000 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 583.06 | 1.166 | 4.36 | 791 | 57.24 | 0.18 | 500 | 1712 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 32120.22 | 64.240 | 81.22 | 475 | 50.81 | 0.19 | 3500 | 222142 |
| BuildingDefenseAttackSystem.TargetSelection | Main Thread | 50.14 | 0.100 | 0.27 | 445 | 50.14 | 0.27 | 3250 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 46.52 | 0.093 | 0.26 | 365 | 46.52 | 0.26 | 500 | 0 |
| UpdateOldEntitiesGraphicsChunksJob (Burst) | Main Thread | 44.50 | 0.089 | 0.22 | 474 | 44.50 | 0.22 | 500 | 0 |
| SRPBRender.ApplyShader | Main Thread | 46.14 | 0.092 | 0.16 | 474 | 44.30 | 0.16 | 6000 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 898.72 | 1.797 | 3.51 | 504 | 42.24 | 0.20 | 499 | 0 |
| UnitSelectionMarkerSystem:CollectSelectionMarkerChangesJob (Burst) | Main Thread | 40.15 | 0.080 | 0.33 | 698 | 40.15 | 0.33 | 496 | 0 |
| UpdateAllBatches | Main Thread | 194.08 | 0.388 | 0.55 | 564 | 39.35 | 0.13 | 500 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 34172.08 | 68.344 | 104.96 | 791 | 34172.08 | 104.96 | 8423 | 0 |
| DrawBuffersBatchMode | Render Thread | 670.13 | 1.340 | 1.98 | 474 | 670.13 | 1.98 | 7000 | 0 |
| GfxDeviceVK.Present | Render Thread | 218.78 | 0.438 | 1.58 | 504 | 218.78 | 1.58 | 500 | 0 |
| RenderLoop | Render Thread | 1729.98 | 3.460 | 5.03 | 480 | 149.60 | 1.03 | 7905 | 0 |
| ExecuteRenderGraph | Render Thread | 1122.56 | 2.245 | 3.53 | 474 | 115.12 | 0.42 | 500 | 0 |
| Gfx.PresentFrame | Render Thread | 384.79 | 0.770 | 1.97 | 504 | 99.76 | 0.92 | 500 | 0 |
| AcquireNextFrame | Render Thread | 91.03 | 0.182 | 0.57 | 499 | 91.03 | 0.57 | 500 | 0 |
| Canvas.RenderOverlays | Render Thread | 154.34 | 0.309 | 0.60 | 472 | 73.51 | 0.32 | 3500 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 53.97 | 0.108 | 0.29 | 380 | 53.97 | 0.29 | 1000 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 555.02 | 1.110 | 1.53 | 479 | 40.41 | 0.32 | 3500 | 0 |
| ScheduleGeometryJobs | Render Thread | 28.48 | 0.057 | 0.24 | 473 | 28.48 | 0.24 | 3000 | 0 |
| Gfx.SetRenderTarget | Render Thread | 20.22 | 0.040 | 0.23 | 316 | 20.22 | 0.23 | 2534 | 0 |
| BlitFinalToBackBuffer | Render Thread | 17.62 | 0.035 | 0.08 | 592 | 17.62 | 0.08 | 500 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 12.48 | 0.025 | 1.47 | 420 | 12.48 | 1.47 | 34 | 0 |
| GpuRecorder.FrameTick | Render Thread | 12.28 | 0.025 | 0.33 | 387 | 12.28 | 0.33 | 500 | 0 |
| RenderLoop.Draw | Render Thread | 11.59 | 0.023 | 0.09 | 480 | 11.58 | 0.09 | 500 | 0 |
| Camera.RenderSkybox | Render Thread | 9.63 | 0.019 | 0.38 | 343 | 9.63 | 0.38 | 500 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 190.54 | 0.381 | 1.02 | 697 | 8.42 | 0.03 | 500 | 0 |
| UI.RenderOverlays | Render Thread | 85.35 | 0.171 | 0.35 | 366 | 7.56 | 0.06 | 500 | 0 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 34179.29 | 68.359 | 104.98 | 791 | 7.21 | 0.86 | 8423 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 791 | 109.68 | 109.65 | 109.65 | 3.50 | 16.78 | 1989116 |
| 475 | 90.97 | 90.95 | 90.95 | 2.99 | 17.76 | 2472 |
| 679 | 90.82 | 90.81 | 90.81 | 2.39 | 16.46 | 37654 |
| 457 | 88.99 | 88.98 | 88.98 | 2.54 | 16.54 | 14028 |
| 487 | 88.05 | 88.03 | 88.03 | 2.66 | 17.03 | 2472 |
| 571 | 87.75 | 87.74 | 87.74 | 2.53 | 16.05 | 3092 |
| 472 | 87.59 | 87.56 | 87.56 | 3.19 | 17.39 | 2088 |
| 793 | 87.42 | 87.38 | 87.38 | 2.50 | 16.38 | 2472 |
| 658 | 87.02 | 87.01 | 87.01 | 2.47 | 16.70 | 2088 |
| 394 | 86.60 | 86.59 | 86.59 | 3.43 | 17.09 | 2088 |
| 343 | 86.54 | 86.53 | 86.53 | 2.77 | 17.11 | 17016 |
| 490 | 86.54 | 86.53 | 86.53 | 2.63 | 0.00 | 2088 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
