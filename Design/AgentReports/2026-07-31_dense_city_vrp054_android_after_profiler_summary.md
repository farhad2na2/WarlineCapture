# Android Profiler Capture Summary

Date: 2026-07-31 13:11:46 +02:00
Capture: `C:\Users\zfoul\Projects\WarlineCapture\Build\AndroidDenseCandidateProfiler\vrp054\vrp054_after_resident_buffer.raw`
Profiler frames: `300..799`
Scanned frames: `500`
Frame budget: `16.667ms`

## Capture Provenance And VRP-054 Gate

- Device: paired wireless Xiaomi `24090RA29G`.
- Candidate profiler APK: `645455988` bytes, SHA-256 `36600231719B5A48805AF7E9C27374A78A5C3AF390F8A510CA4858396238B0F2`.
- Raw capture: `486810763` bytes, SHA-256 `805860C34E7DDD86EF801A462BC7B6E9D6B5C4780F2AF03B202F40909FE2D866`.
- Checked wrapper: `%TEMP%\warline-vrp054-resident-buffer-profiler-export.log`; pass marker `[ProfilerCaptureSummaryExporter] result=Passed frames=500 range=300..799`.
- Stationary runtime state at capture start: `placements=9721`, `parts=11299`, `resident=70710`, `slots=122/704`, `activeCells=25`, `activePlacements=114`, `retained=0`, `released=0`, `rebound=122`, `overflow=0`, `deficit=0`, `reason=1`, `commandVersion=1`.
- Visual evidence: `Build/AndroidDenseCandidateProfiler/vrp054/vrp054_after_resident_buffer.png`, SHA-256 `DCC85C6563917C635F87B653B00FF6462B8D22C63B8A5FB5ADC8E3C36BB6FEA7`; the post-capture camera view retains dense-city presentation, HUD, and minimap without an obvious gross hole. Exact historical camera-pose equivalence remains qualified because the retained before artifact did not serialize its pose.
- Compared with the accepted VRP-053 stationary profiler sample, CPU-main p95 improved from `110.66ms` to `35.88ms` (`67.6%`), average frame time improved from `93.94ms` to `21.62ms`, faction-tint backfill fell from `51.722ms/frame` to `2.239ms/frame`, and mass render settings fell from `17.318ms/frame` to `2.171ms/frame`.
- Gate decision: `VRP-054 accepted`. Active proxy state is nonzero, overflow/deficit remain zero, visual/state evidence passes at the available parity level, CPU-main p95 improves materially, and no new main-thread owner replaces the removed cost. This is not the final 60 FPS gate: `35.88ms` CPU-main p95 still exceeds the `16.667ms` Phase 9 target.

## Frame Time

| Metric | Value |
|---|---:|
| Avg frame | 21.62 ms (46.3 FPS) |
| P50 frame | 19.67 ms (50.8 FPS) |
| P95 frame | 35.89 ms (27.9 FPS) |
| P99 frame | 37.02 ms (27.0 FPS) |
| Max frame | 40.64 ms (24.6 FPS) |
| Frames over budget | 499/500 |
| Avg CPU active | 21.56 ms |
| P95 CPU active | 35.88 ms |
| Avg GPU time | 13.35 ms |
| P95 GPU time | 0.00 ms |
| Total GC allocated | 1444386 bytes |

## Render Counters

| Counter | Avg | P50 | P95 | Max |
|---|---:|---:|---:|---:|
| Draw calls | 0 | 0 | 0 | 0 |
| Batches | 0 | 0 | 0 | 0 |
| SetPass calls | 32 | 32 | 33 | 34 |
| Triangles | 786004 | 785846 | 787061 | 789429 |
| Vertices | 1423496 | 1423207 | 1425371 | 1429737 |

## Top Priority Markers By Total Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PlayerLoop | Main Thread | 10764.93 | 21.530 | 40.48 | 687 | 106.96 | 0.46 | 500 | 1444386 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 8881.53 | 17.763 | 35.49 | 563 | 8.65 | 0.55 | 8268 | 0 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 6738.39 | 13.477 | 26.75 | 508 | 55.97 | 0.18 | 3500 | 204988 |
| SimulationSystemGroup | Main Thread | 4778.47 | 9.557 | 26.75 | 508 | 0.80 | 0.00 | 500 | 164988 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 4775.28 | 9.551 | 26.74 | 508 | 108.34 | 0.78 | 500 | 164988 |
| PresentationSystemGroup | Main Thread | 1789.88 | 3.580 | 5.71 | 505 | 1.18 | 0.01 | 500 | 40000 |
| Default World Unity.Entities.PresentationSystemGroup | Main Thread | 1787.16 | 3.574 | 5.69 | 505 | 18.87 | 0.11 | 500 | 40000 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke] | Main Thread | 1685.67 | 3.371 | 11.35 | 798 | 15.72 | 0.08 | 500 | 400000 |
| Default World Game.Rendering.UnitFactionTintTargetBackfillSystem | Main Thread | 1119.72 | 2.239 | 3.46 | 430 | 1097.16 | 3.42 | 500 | 0 |
| Default World Game.Rendering.UnitMassRenderSettingsSystem | Main Thread | 1085.38 | 2.171 | 3.70 | 430 | 1058.50 | 3.63 | 500 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 993.73 | 1.987 | 6.02 | 506 | 48.11 | 0.18 | 500 | 0 |
| Gfx.PresentFrame | Render Thread | 518.84 | 1.038 | 5.77 | 331 | 116.47 | 1.63 | 500 | 0 |
| Update.ScriptRunBehaviourUpdate | Main Thread | 440.70 | 0.881 | 9.37 | 468 | 1.06 | 0.01 | 500 | 819398 |
| BehaviourUpdate | Main Thread | 439.64 | 0.879 | 9.37 | 468 | 30.42 | 0.22 | 500 | 819398 |
| Default World Unity.Rendering.EntitiesGraphicsSystem | Main Thread | 243.81 | 0.488 | 1.29 | 732 | 6.51 | 0.03 | 500 | 0 |
| Canvas.RenderOverlays | Render Thread | 161.65 | 0.323 | 0.47 | 462 | 76.77 | 0.30 | 3500 | 0 |
| LateBehaviourUpdate | Main Thread | 106.78 | 0.214 | 1.15 | 505 | 7.02 | 0.04 | 500 | 20000 |
| Canvas.RenderOverlays | Main Thread | 105.32 | 0.211 | 0.20 | 766 | 26.19 | 0.09 | 3500 | 0 |
| UnityEngine.CoreModule.dll!UnityEngine.Rendering::BatchRendererGroup.InvokeOnPerformCulling() [Invoke] | Main Thread | 102.42 | 0.205 | 1.76 | 504 | 23.12 | 1.67 | 2000 | 0 |
| Default World Game.Rendering.UnitSelectionMarkerSystem | Main Thread | 96.53 | 0.193 | 0.80 | 506 | 18.59 | 0.15 | 500 | 0 |
| GameplayRuntimeUpdate.Selection | Main Thread | 89.56 | 0.179 | 0.47 | 689 | 7.11 | 0.05 | 500 | 251296 |
| Canvas.BuildBatch | Main Thread | 87.15 | 0.174 | 0.55 | 506 | 87.15 | 0.55 | 3500 | 0 |
| Default World Game.Runtime.UnitRuntimeHealthBarSystem | Main Thread | 74.21 | 0.148 | 0.58 | 553 | 31.27 | 0.13 | 500 | 0 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 67.96 | 0.136 | 0.31 | 692 | 61.76 | 0.29 | 500 | 215200 |
| Default World Game.Runtime.UnitDeathSystem | Main Thread | 54.49 | 0.109 | 0.56 | 661 | 22.71 | 0.16 | 500 | 20000 |
| Default World Game.Runtime.VisibleUnitSelectionCandidateSystem | Main Thread | 50.97 | 0.102 | 0.26 | 535 | 27.51 | 0.20 | 500 | 0 |
| Default World Unity.Rendering.StructuralChangePresentationSystemGroup | Main Thread | 50.35 | 0.101 | 0.33 | 766 | 2.83 | 0.09 | 500 | 0 |
| Default World Game.Runtime.UnitAttackSystem | Main Thread | 49.40 | 0.099 | 1.20 | 440 | 43.49 | 1.18 | 500 | 20000 |
| Default World Game.Runtime.UnitManualMoveRetrySystem | Main Thread | 47.14 | 0.094 | 0.35 | 507 | 21.00 | 0.22 | 500 | 0 |
| UnitSelectionMarkerSystem:CollectSelectionMarkerChangesJob (Burst) | Main Thread | 46.38 | 0.093 | 0.24 | 442 | 46.38 | 0.24 | 498 | 0 |
| Default World Unity.Transforms.TransformSystemGroup | Main Thread | 45.91 | 0.092 | 2.58 | 505 | 3.33 | 0.07 | 500 | 0 |
| GameplayRuntimeLateUpdate.UnitImpostors | Main Thread | 45.45 | 0.091 | 0.78 | 505 | 2.26 | 0.02 | 500 | 0 |
| Default World Game.Runtime.UnitTransportAirdropSystem | Main Thread | 44.61 | 0.089 | 1.66 | 775 | 22.68 | 0.28 | 500 | 0 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 43.49 | 0.087 | 0.49 | 300 | 43.03 | 0.37 | 500 | 0 |
| Default World Unity.Entities.InitializationSystemGroup | Main Thread | 42.90 | 0.086 | 0.29 | 504 | 13.89 | 0.09 | 500 | 0 |
| Default World Game.Runtime.UnitAnimationIndexSystem | Main Thread | 42.62 | 0.085 | 0.32 | 506 | 13.75 | 0.10 | 500 | 0 |
| Default World Unity.Rendering.RegisterMaterialsAndMeshesSystem | Main Thread | 41.15 | 0.082 | 0.33 | 506 | 9.91 | 0.18 | 500 | 0 |
| GameplayRuntimeUpdate.MainMenu | Main Thread | 40.78 | 0.082 | 0.68 | 506 | 19.49 | 0.27 | 500 | 13094 |
| Default World Game.Runtime.UnitTransportPlaneDoorSystem | Main Thread | 40.15 | 0.080 | 0.20 | 689 | 12.05 | 0.05 | 500 | 0 |
| GameplayRuntimeUpdate.BuildingPlacement | Main Thread | 40.11 | 0.080 | 8.23 | 468 | 15.59 | 0.21 | 500 | 42496 |

## Top Main Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Default World Game.Rendering.UnitFactionTintTargetBackfillSystem | Main Thread | 1119.72 | 2.239 | 3.46 | 430 | 1097.16 | 3.42 | 500 | 0 |
| Default World Game.Rendering.UnitMassRenderSettingsSystem | Main Thread | 1085.38 | 2.171 | 3.70 | 430 | 1058.50 | 3.63 | 500 | 0 |
| LocalToWorldSystem:ComputeHierarchyLocalToWorldJob (Burst) | Main Thread | 807.69 | 1.615 | 3.36 | 688 | 807.69 | 3.36 | 492 | 0 |
| ThreatDetectionWarningSystem:ThreatScanJob (Burst) | Main Thread | 668.96 | 1.338 | 16.91 | 694 | 668.96 | 16.91 | 40 | 0 |
| WaitForJobGroupID | Main Thread | 2493.22 | 4.986 | 22.02 | 597 | 484.47 | 8.02 | 14694 | 0 |
| UGUI.Rendering.UpdateBatches | Main Thread | 731.52 | 1.463 | 2.93 | 506 | 483.74 | 2.00 | 500 | 0 |
| Profiler.FlushMemoryCounters | Main Thread | 477.27 | 0.955 | 2.03 | 684 | 477.27 | 2.03 | 500 | 0 |
| Idle | Main Thread | 202.20 | 0.404 | 17.06 | 518 | 202.20 | 17.06 | 38 | 0 |
| Default World Game.Runtime.StaticGridBlockerUpdateSystem | Main Thread | 204.12 | 0.408 | 0.64 | 692 | 201.95 | 0.64 | 500 | 0 |
| JobHandle.Complete | Main Thread | 2468.61 | 4.937 | 21.88 | 597 | 152.35 | 1.04 | 33832 | 0 |
| Inl_On Record Render Graph | Main Thread | 176.91 | 0.354 | 0.84 | 691 | 142.75 | 0.70 | 500 | 0 |
| SRPBatcher.Flush | Main Thread | 122.66 | 0.245 | 0.67 | 301 | 122.66 | 0.67 | 7000 | 0 |
| Default World Unity.Entities.SimulationSystemGroup | Main Thread | 4775.28 | 9.551 | 26.74 | 508 | 108.34 | 0.78 | 500 | 164988 |
| PlayerLoop | Main Thread | 10764.93 | 21.530 | 40.48 | 687 | 106.96 | 0.46 | 500 | 1444386 |
| Game.Composition.dll!Game.Composition::MatchSceneView.OnGUI() [Invoke] | Main Thread | 98.45 | 0.197 | 0.47 | 715 | 92.36 | 0.45 | 1000 | 216000 |
| Canvas.BuildBatch | Main Thread | 87.15 | 0.174 | 0.55 | 506 | 87.15 | 0.55 | 3500 | 0 |
| OnPerformCulling | Main Thread | 79.29 | 0.159 | 0.31 | 688 | 79.29 | 0.31 | 1000 | 0 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | Main Thread | 736.81 | 1.474 | 4.46 | 504 | 73.17 | 0.33 | 500 | 0 |
| CanvasRenderer.SyncTransform | Main Thread | 63.86 | 0.128 | 0.23 | 504 | 63.86 | 0.23 | 217734 | 0 |
| Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] | Main Thread | 62.44 | 0.125 | 0.34 | 518 | 62.44 | 0.34 | 500 | 0 |
| GameplayRuntimeUpdate.Selection.Camera | Main Thread | 67.96 | 0.136 | 0.31 | 692 | 61.76 | 0.29 | 500 | 215200 |
| UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] | Main Thread | 6738.39 | 13.477 | 30.73 | 508 | 55.97 | 0.25 | 3500 | 204988 |
| SRPBRender.ApplyShader | Main Thread | 53.80 | 0.108 | 0.22 | 301 | 51.67 | 0.21 | 6000 | 0 |
| Default World Game.Rendering.UnitHelicopterBladeSpinSystem | Main Thread | 993.62 | 1.987 | 6.02 | 506 | 48.05 | 0.18 | 499 | 0 |
| UnitSelectionMarkerSystem:CollectSelectionMarkerChangesJob (Burst) | Main Thread | 46.36 | 0.093 | 0.24 | 442 | 46.36 | 0.24 | 491 | 0 |
| Default World Game.Runtime.AIBuildPlannerSystem | Main Thread | 45.07 | 0.090 | 0.87 | 463 | 45.04 | 0.86 | 500 | 2948 |
| Default World Game.Runtime.AudioCooldownSystem | Main Thread | 52.62 | 0.105 | 0.32 | 430 | 44.77 | 0.30 | 497 | 19880 |
| Default World Game.Runtime.UnitAttackSystem | Main Thread | 49.40 | 0.099 | 1.20 | 440 | 43.49 | 1.18 | 500 | 20000 |
| Default World Game.Rendering.UnitRenderBudgetSystem | Main Thread | 43.27 | 0.087 | 0.49 | 300 | 42.81 | 0.37 | 495 | 0 |
| Inl_RenderCameraStack | Main Thread | 1647.03 | 3.294 | 11.27 | 798 | 41.29 | 0.20 | 500 | 400000 |

## Top Render Thread Markers By Self Time

| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Semaphore.WaitForSignal | Render Thread | 8872.88 | 17.746 | 35.57 | 563 | 8872.88 | 35.57 | 8268 | 0 |
| DrawBuffersBatchMode | Render Thread | 730.85 | 1.462 | 2.45 | 429 | 730.85 | 2.45 | 7000 | 0 |
| GfxDeviceVK.Present | Render Thread | 323.77 | 0.648 | 5.40 | 331 | 323.77 | 5.40 | 500 | 0 |
| RenderLoop | Render Thread | 1968.30 | 3.937 | 9.10 | 429 | 158.38 | 2.34 | 7808 | 0 |
| ExecuteRenderGraph | Render Thread | 1198.66 | 2.397 | 4.15 | 687 | 125.73 | 0.77 | 500 | 0 |
| Gfx.PresentFrame | Render Thread | 518.84 | 1.038 | 5.77 | 331 | 116.47 | 1.63 | 500 | 0 |
| AcquireNextFrame | Render Thread | 85.50 | 0.171 | 0.44 | 534 | 85.50 | 0.44 | 500 | 0 |
| Canvas.RenderOverlays | Render Thread | 161.65 | 0.323 | 0.99 | 462 | 76.77 | 0.46 | 3500 | 0 |
| GfxDeviceVK.ExecuteCommandList | Render Thread | 56.14 | 0.112 | 0.42 | 690 | 56.14 | 0.42 | 1000 | 0 |
| RenderLoop.DrawSRPBatcher | Render Thread | 612.01 | 1.224 | 2.20 | 429 | 45.10 | 0.30 | 3500 | 0 |
| ScheduleGeometryJobs | Render Thread | 40.31 | 0.081 | 0.65 | 506 | 40.31 | 0.65 | 3000 | 0 |
| GpuRecorder.FrameTick | Render Thread | 22.45 | 0.045 | 0.42 | 526 | 22.45 | 0.42 | 500 | 0 |
| Gfx.SetRenderTarget | Render Thread | 21.59 | 0.043 | 0.10 | 419 | 21.59 | 0.10 | 2534 | 0 |
| BlitFinalToBackBuffer | Render Thread | 17.38 | 0.035 | 0.31 | 316 | 17.38 | 0.31 | 500 | 0 |
| Gfx.RequestAsyncReadbackData | Render Thread | 14.73 | 0.029 | 0.98 | 780 | 14.73 | 0.98 | 34 | 0 |
| RenderLoop.Draw | Render Thread | 12.01 | 0.024 | 0.36 | 313 | 12.00 | 0.36 | 500 | 0 |
| Shadows.DrawSRPBatcher | Render Thread | 199.53 | 0.399 | 0.92 | 534 | 9.84 | 0.04 | 500 | 0 |
| Camera.RenderSkybox | Render Thread | 9.05 | 0.018 | 0.13 | 381 | 9.05 | 0.13 | 500 | 0 |
| Gfx.WaitForGfxCommandsFromMainThread | Render Thread | 8881.53 | 17.763 | 35.59 | 563 | 8.65 | 0.56 | 8268 | 0 |
| UI.RenderOverlays | Render Thread | 89.09 | 0.178 | 0.48 | 462 | 7.78 | 0.06 | 500 | 0 |

## Slowest Frames

| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 687 | 40.64 | 40.61 | 40.61 | 4.74 | 0.00 | 2088 |
| 563 | 39.66 | 39.64 | 39.64 | 2.47 | 16.92 | 2048 |
| 508 | 39.38 | 39.36 | 39.36 | 2.97 | 17.21 | 2472 |
| 310 | 38.13 | 38.11 | 38.11 | 4.18 | 17.14 | 2008 |
| 597 | 37.52 | 37.51 | 37.51 | 2.50 | 0.00 | 2088 |
| 518 | 37.02 | 37.00 | 37.00 | 2.64 | 16.99 | 2472 |
| 567 | 36.72 | 36.71 | 36.71 | 2.51 | 0.00 | 2088 |
| 490 | 36.55 | 36.54 | 36.54 | 2.38 | 17.01 | 2088 |
| 704 | 36.51 | 36.49 | 36.49 | 2.92 | 17.76 | 2472 |
| 300 | 36.42 | 36.40 | 36.40 | 3.69 | 0.00 | 2008 |
| 577 | 36.25 | 36.24 | 36.24 | 2.71 | 16.73 | 2472 |
| 724 | 36.22 | 36.21 | 36.21 | 2.43 | 16.79 | 2088 |

## Notes

- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.
- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.
