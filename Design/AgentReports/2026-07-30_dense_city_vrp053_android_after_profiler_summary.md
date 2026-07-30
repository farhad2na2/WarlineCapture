# Android Profiler Capture Summary

Date: 2026-07-30 23:07:43 +02:00
Capture: `C:\Users\zfoul\Projects\WarlineCapture\Build\AndroidDenseCandidateProfiler\vrp053\vrp053_after.raw`
Profiler frames: `300..799`
Scanned frames: `500`
Frame budget: `16.667ms`

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 93.94 ms (10.6 FPS) |
| P50 frame | 86.50 ms (11.6 FPS) |
| P95 frame | 110.68 ms (9.0 FPS) |
| P99 frame | 114.71 ms (8.7 FPS) |
| Max frame | 134.55 ms (7.4 FPS) |
| Frames over budget | 500/500 |
| Avg CPU active | 93.93 ms |
| P95 CPU active | 110.66 ms |
| Avg GPU time | 15.53 ms |
| P95 GPU time | 16.59 ms |
| Total GC allocated | 3835632 bytes |

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
| PlayerLoop | Main Thread | 46945.77 | 93.892 | 134.48 | 638 | 108.49 | 0.41 | 500 | 3835632 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 45012.08 | 90.024 | 128.97 | 638 | 5.83 | 0.03 | 8780 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 42562.70 | 85.125 | 55.95 | 300 | 57.40 | 0.11 | 3500 | 550950 |
| PresentationSystemGroup | Main Thread | 26751.84 | 53.504 | 55.95 | 300 | 1.41 | 0.00 | 500 | 40000 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 26748.84 | 53.498 | 55.95 | 300 | 26.30 | 0.10 | 500 | 40000 |
| Default World Game.Rendering.UnitFactionTintTargetBackfillSystem | Main Thread | 25861.10 | 51.722 | 53.86 | 300 | 25839.61 | 53.80 | 500 | 0 |
| SimulationSystemGroup | Main Thread | 15593.66 | 31.187 | 52.30 | 300 | 0.80 | 0.04 | 500 | 499628 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 15590.91 | 31.182 | 52.30 | 300 | 89.67 | 0.30 | 500 | 499628 |
| Default World Game.Rendering.UnitMassRenderSettingsSystem | Main Thread | 8658.95 | 17.318 | 19.85 | 769 | 8632.77 | 19.79 | 500 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 1721.43 | 3.443 | 7.01 | 638 | 14.42 | 0.07 | 500 | 401752 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 1127.41 | 2.255 | 5.96 | 340 | 49.12 | 0.22 | 500 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 578.75 | 1.157 | 36.18 | 638 | 0.73 | 0.01 | 500 | 2862930 |
| BehaviourUpdate | Main Thread | 578.02 | 1.156 | 36.17 | 638 | 27.94 | 0.43 | 500 | 2862930 |
| Gfx.PresentFrame | Render Thread | 448.69 | 0.897 | 1.74 | 344 | 116.97 | 0.53 | 500 | 0 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 341.62 | 0.683 | 0.97 | 361 | 9.44 | 0.05 | 500 | 0 |
| Canvas.RenderOverlays | Render Thread | 190.09 | 0.380 | 0.32 | 402 | 91.14 | 0.18 | 3500 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 132.59 | 0.265 | 3.75 | 638 | 48.55 | 3.62 | 2000 | 1712 |
| Canvas.RenderOverlays | Main Thread | 121.09 | 0.242 | 0.19 | 643 | 28.63 | 0.07 | 3500 | 0 |
| LateBehaviourUpdate | Main Thread | 117.59 | 0.235 | 0.36 | 343 | 6.92 | 0.06 | 500 | 20000 |
| GameplayRuntimeUpdate.Selection | Main Thread | 116.68 | 0.233 | 0.38 | 579 | 6.55 | 0.04 | 500 | 347488 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 103.99 | 0.208 | 34.74 | 638 | 19.91 | 0.16 | 500 | 2064462 |
| Canvas.BuildBatch | Main Thread | 102.46 | 0.205 | 0.31 | 453 | 102.46 | 0.31 | 3500 | 0 |
| GameplayRuntimeUpdate.MainMenu | Main Thread | 99.89 | 0.200 | 0.86 | 645 | 39.84 | 0.37 | 500 | 47078 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 92.83 | 0.186 | 0.88 | 596 | 19.15 | 0.10 | 500 | 0 |
| Default World Unity.Rendering.StructuralChangePresentationSystemGroup | Main Thread | 91.86 | 0.184 | 0.41 | 321 | 4.64 | 0.05 | 500 | 0 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 85.39 | 0.171 | 0.35 | 579 | 76.10 | 0.33 | 500 | 220000 |
| BuildingDefenseAttackSystem.TargetSelection | Main Thread | 68.12 | 0.136 | 0.52 | 301 | 68.12 | 0.52 | 3250 | 0 |
| Default World Unity.Rendering.UpdateHybridChunksStructure | Main Thread | 67.55 | 0.135 | 0.37 | 321 | 3.04 | 0.03 | 500 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 62.28 | 0.125 | 0.22 | 466 | 24.58 | 0.11 | 500 | 0 |
| BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState | Main Thread | 61.50 | 0.123 | 34.60 | 638 | 53.26 | 28.04 | 500 | 2037742 |
| MainMenuPlayUI.MinimapUpdate | Main Thread | 58.93 | 0.118 | 0.55 | 345 | 58.93 | 0.55 | 500 | 0 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 57.22 | 0.114 | 0.36 | 343 | 24.89 | 0.22 | 500 | 20000 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 49.81 | 0.100 | 0.25 | 741 | 29.63 | 0.12 | 500 | 0 |
| UnitSelectionMarkerSystem:CollectSelectionMarkerChangesJob (Burst) | Main Thread | 49.09 | 0.098 | 0.32 | 343 | 49.09 | 0.32 | 500 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 48.83 | 0.098 | 0.17 | 538 | 2.39 | 0.01 | 500 | 0 |
| Default World Game.Runtime.UnitAttackSystem | Main Thread | 46.49 | 0.093 | 0.20 | 568 | 41.42 | 0.18 | 500 | 20000 |
| Default World Game.Runtime.MatchHudMinimapMarkerSystem | Main Thread | 44.40 | 0.089 | 0.51 | 756 | 10.38 | 0.14 | 500 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 43.47 | 0.087 | 0.17 | 446 | 11.64 | 0.06 | 500 | 0 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 41.30 | 0.083 | 0.16 | 541 | 17.40 | 0.10 | 500 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 40.26 | 0.081 | 0.25 | 776 | 12.70 | 0.17 | 500 | 0 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Default World Game.Rendering.UnitFactionTintTargetBackfillSystem | Main Thread | 25861.10 | 51.722 | 53.86 | 300 | 25839.61 | 53.80 | 500 | 0 |
| Default World Game.Rendering.UnitMassRenderSettingsSystem | Main Thread | 8658.95 | 17.318 | 19.85 | 769 | 8632.77 | 19.79 | 500 | 0 |
| ThreatDetectionWarningSystem:ThreatScanJob (Burst) | Main Thread | 3580.28 | 7.161 | 22.81 | 634 | 3580.28 | 22.81 | 159 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 941.64 | 1.883 | 4.33 | 756 | 941.64 | 4.33 | 499 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 852.50 | 1.705 | 2.67 | 300 | 545.15 | 2.04 | 500 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 514.60 | 1.029 | 1.88 | 344 | 514.60 | 1.88 | 500 | 0 |
| WaitForJobGroupID | Main Thread | 5671.23 | 11.342 | 30.06 | 756 | 494.09 | 2.37 | 15803 | 0 |
| Idle | Main Thread | 222.23 | 0.444 | 24.10 | 517 | 222.23 | 24.10 | 36 | 0 |
| Default World Game.Runtime.StaticGridBlockerUpdateSystem | Main Thread | 224.47 | 0.449 | 0.66 | 693 | 222.08 | 0.65 | 500 | 0 |
| JobHandle.Complete | Main Thread | 5688.67 | 11.377 | 31.30 | 446 | 193.98 | 6.51 | 35454 | 0 |
| Inl_On Record Render Graph | Main Thread | 165.81 | 0.332 | 0.62 | 445 | 132.05 | 0.54 | 500 | 0 |
| SRPBatcher.Flush | Main Thread | 122.46 | 0.245 | 0.41 | 344 | 122.46 | 0.41 | 7000 | 0 |
| PlayerLoop | Main Thread | 46945.77 | 93.892 | 134.48 | 638 | 108.50 | 0.41 | 500 | 3835632 |
| Canvas.BuildBatch | Main Thread | 102.46 | 0.205 | 0.31 | 453 | 102.46 | 0.31 | 3500 | 0 |
| BuildingDefenseAttackSystem.TargetCollection | Main Thread | 99.75 | 0.199 | 0.55 | 553 | 99.47 | 0.55 | 250 | 0 |
| Game.Composition.dll!Game.Composition::MatchSceneView.OnGUI() [Invoke] | Main Thread | 107.18 | 0.214 | 0.35 | 344 | 98.58 | 0.32 | 1000 | 216000 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 15590.91 | 31.182 | 52.30 | 300 | 89.68 | 0.30 | 500 | 499628 |
| OnPerformCulling | Main Thread | 83.97 | 0.168 | 0.33 | 652 | 83.97 | 0.33 | 1000 | 0 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 85.39 | 0.171 | 0.35 | 579 | 76.10 | 0.33 | 500 | 220000 |
| CanvasRenderer.SyncTransform | Main Thread | 75.60 | 0.151 | 0.21 | 458 | 75.60 | 0.21 | 217500 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 759.00 | 1.518 | 5.25 | 638 | 72.24 | 0.24 | 500 | 1712 |
| BuildingDefenseAttackSystem.TargetSelection | Main Thread | 68.12 | 0.136 | 0.52 | 301 | 68.12 | 0.52 | 3250 | 0 |
| MainMenuPlayUI.MinimapUpdate | Main Thread | 58.91 | 0.118 | 0.55 | 345 | 58.91 | 0.55 | 330 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 42562.70 | 85.125 | 108.64 | 300 | 57.40 | 0.18 | 3500 | 550950 |
| Default World Game.Runtime.AICombatOrderSystem | Main Thread | 57.86 | 0.116 | 0.98 | 471 | 57.05 | 0.92 | 110 | 158 |
| SRPBRender.ApplyShader | Main Thread | 56.30 | 0.113 | 0.99 | 475 | 53.96 | 0.98 | 6000 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 52.27 | 0.105 | 0.23 | 537 | 52.27 | 0.23 | 500 | 0 |
| UpdateAllBatches | Main Thread | 226.12 | 0.452 | 0.71 | 361 | 50.26 | 0.16 | 500 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 50.43 | 0.101 | 0.33 | 741 | 49.64 | 0.31 | 499 | 332288 |
| UnitSelectionMarkerSystem:CollectSelectionMarkerChangesJob (Burst) | Main Thread | 49.09 | 0.098 | 0.32 | 343 | 49.09 | 0.32 | 500 | 0 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 45006.26 | 90.013 | 128.95 | 638 | 45006.26 | 128.95 | 8780 | 0 |
| DrawBuffersBatchMode | Render Thread | 781.06 | 1.562 | 2.38 | 432 | 781.06 | 2.38 | 7000 | 0 |
| GfxDeviceVK.Present | Render Thread | 259.02 | 0.518 | 1.18 | 344 | 259.02 | 1.18 | 500 | 0 |
| RenderLoop | Render Thread | 2013.88 | 4.028 | 10.17 | 339 | 172.77 | 2.23 | 8389 | 0 |
| ExecuteRenderGraph | Render Thread | 1312.66 | 2.625 | 8.62 | 339 | 135.29 | 0.50 | 500 | 0 |
| Gfx.PresentFrame | Render Thread | 448.69 | 0.897 | 1.74 | 344 | 116.97 | 0.53 | 500 | 0 |
| AcquireNextFrame | Render Thread | 102.55 | 0.205 | 0.33 | 350 | 102.55 | 0.33 | 500 | 0 |
| Canvas.RenderOverlays | Render Thread | 190.09 | 0.380 | 0.65 | 341 | 91.14 | 0.30 | 3500 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 61.00 | 0.122 | 0.25 | 780 | 61.00 | 0.25 | 1000 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 653.05 | 1.306 | 1.84 | 344 | 46.08 | 0.17 | 3500 | 0 |
| ScheduleGeometryJobs | Render Thread | 28.43 | 0.057 | 0.50 | 305 | 28.43 | 0.50 | 3000 | 0 |
| Gfx.SetRenderTarget | Render Thread | 23.25 | 0.046 | 0.20 | 433 | 23.25 | 0.20 | 2534 | 0 |
| BlitFinalToBackBuffer | Render Thread | 19.77 | 0.040 | 0.44 | 339 | 19.77 | 0.44 | 500 | 0 |
| Camera.RenderSkybox | Render Thread | 15.17 | 0.030 | 4.99 | 339 | 15.17 | 4.99 | 500 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 14.84 | 0.030 | 1.19 | 600 | 14.84 | 1.19 | 34 | 0 |
| RenderLoop.Draw | Render Thread | 12.62 | 0.025 | 0.09 | 448 | 12.58 | 0.09 | 500 | 0 |
| GpuRecorder.FrameTick | Render Thread | 11.69 | 0.023 | 0.26 | 617 | 11.69 | 0.26 | 500 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 212.83 | 0.426 | 1.00 | 432 | 9.76 | 0.07 | 500 | 0 |
| UI.RenderOverlays | Render Thread | 104.02 | 0.208 | 0.34 | 402 | 8.16 | 0.06 | 500 | 0 |
| Profiler.FlushRenderCounters | Render Thread | 6.36 | 0.013 | 0.02 | 795 | 6.36 | 0.02 | 500 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 638 | 134.55 | 134.52 | 134.52 | 3.96 | 16.53 | 1987888 |
| 300 | 120.64 | 120.63 | 120.63 | 4.39 | 19.27 | 2718 |
| 446 | 116.96 | 116.93 | 116.93 | 2.99 | 16.51 | 2758 |
| 553 | 116.13 | 116.12 | 116.12 | 2.80 | 16.29 | 38246 |
| 756 | 115.31 | 115.29 | 115.29 | 2.88 | 17.18 | 16556 |
| 380 | 114.71 | 114.70 | 114.70 | 3.09 | 19.12 | 14660 |
| 517 | 112.83 | 112.82 | 112.82 | 2.91 | 16.27 | 3046 |
| 589 | 112.02 | 112.01 | 112.01 | 2.79 | 16.34 | 2758 |
| 642 | 111.90 | 111.87 | 111.87 | 2.95 | 0.00 | 2718 |
| 717 | 111.84 | 111.82 | 111.82 | 3.01 | 16.77 | 2758 |
| 747 | 111.77 | 111.76 | 111.76 | 4.96 | 16.61 | 6942 |
| 499 | 111.72 | 111.70 | 111.70 | 3.06 | 0.00 | 2718 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
