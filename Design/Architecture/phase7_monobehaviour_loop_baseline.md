# Phase 7 MonoBehaviour Loop Baseline

Purpose:
Capture the existing MonoBehaviour runtime loop surface before Phase 7 domain conversions. The Phase 7 architecture guard fails if a new loop key appears outside this baseline.

Generated: `2026-06-22T14:05:57Z`.
Command: `python3 Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py --root Assets/Game/Scripts --output Design/Architecture/phase7_monobehaviour_loop_baseline.md`.
Source root: `Assets/Game/Scripts`.
Source commit: `833214b6`.
Rows: `41`.

## Baseline

| Key | Path | Type | Method | Line | Scope |
| --- | --- | --- | --- | ---: | --- |
| `Assets/Game/Scripts/Composition/MatchSceneView.cs\|MatchSceneView\|LateUpdate` | `Assets/Game/Scripts/Composition/MatchSceneView.cs` | `MatchSceneView` | `LateUpdate` | 102 | `ProductionNonUI` |
| `Assets/Game/Scripts/Composition/MatchSceneView.cs\|MatchSceneView\|Update` | `Assets/Game/Scripts/Composition/MatchSceneView.cs` | `MatchSceneView` | `Update` | 87 | `ProductionNonUI` |
| `Assets/Game/Scripts/Composition/MenuBootstrapView.cs\|MenuBootstrapView\|Update` | `Assets/Game/Scripts/Composition/MenuBootstrapView.cs` | `MenuBootstrapView` | `Update` | 157 | `ProductionNonUI` |
| `Assets/Game/Scripts/Composition/UiToolkitMatchHudMinimapSurface.cs\|UiToolkitMatchHudMinimapSurface\|LateUpdate` | `Assets/Game/Scripts/Composition/UiToolkitMatchHudMinimapSurface.cs` | `UiToolkitMatchHudMinimapSurface` | `LateUpdate` | 75 | `ProductionNonUI` |
| `Assets/Game/Scripts/Effects/MissileTrailVfxView.cs\|MissileTrailVfxView\|Update` | `Assets/Game/Scripts/Effects/MissileTrailVfxView.cs` | `MissileTrailVfxView` | `Update` | 66 | `ProductionNonUI` |
| `Assets/Game/Scripts/Effects/UnitAttackImpactVfxView.cs\|UnitAttackImpactVfxView\|Update` | `Assets/Game/Scripts/Effects/UnitAttackImpactVfxView.cs` | `UnitAttackImpactVfxView` | `Update` | 74 | `ProductionNonUI` |
| `Assets/Game/Scripts/Rendering/TerrainLodHeightSwitch.cs\|TerrainLodHeightSwitch\|Update` | `Assets/Game/Scripts/Rendering/TerrainLodHeightSwitch.cs` | `TerrainLodHeightSwitch` | `Update` | 56 | `ProductionNonUI` |
| `Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs\|RuntimeBuildingEntityLink\|Update` | `Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs` | `RuntimeBuildingEntityLink` | `Update` | 40 | `ProductionNonUI` |
| `Assets/Game/Scripts/UI/Components/FeedbackToastView.cs\|FeedbackToastView\|Coroutine:Animate` | `Assets/Game/Scripts/UI/Components/FeedbackToastView.cs` | `FeedbackToastView` | `Coroutine:Animate` | 87 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/FeedbackToastView.cs\|FeedbackToastView\|Coroutine:ShowRoutine` | `Assets/Game/Scripts/UI/Components/FeedbackToastView.cs` | `FeedbackToastView` | `Coroutine:ShowRoutine` | 76 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/MatchHudObjectivesElapsedView.cs\|MatchHudObjectivesElapsedView\|Update` | `Assets/Game/Scripts/UI/Components/MatchHudObjectivesElapsedView.cs` | `MatchHudObjectivesElapsedView` | `Update` | 19 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs\|MatchHudSquadTrayView\|Update` | `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs` | `MatchHudSquadTrayView` | `Update` | 100 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/ResourceFlyoutView.cs\|ResourceFlyoutView\|Coroutine:FlightRoutine` | `Assets/Game/Scripts/UI/Components/ResourceFlyoutView.cs` | `ResourceFlyoutView` | `Coroutine:FlightRoutine` | 59 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs\|UIMotionFeedbackView\|Coroutine:AnimateScale` | `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs` | `UIMotionFeedbackView` | `Coroutine:AnimateScale` | 258 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs\|UIMotionFeedbackView\|Coroutine:FlashOnly` | `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs` | `UIMotionFeedbackView` | `Coroutine:FlashOnly` | 343 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs\|UIMotionFeedbackView\|Coroutine:PulseScale` | `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs` | `UIMotionFeedbackView` | `Coroutine:PulseScale` | 246 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs\|UIMotionFeedbackView\|Coroutine:ScaleAndFade` | `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs` | `UIMotionFeedbackView` | `Coroutine:ScaleAndFade` | 296 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs\|UIMotionFeedbackView\|Coroutine:SlideAndFade` | `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs` | `UIMotionFeedbackView` | `Coroutine:SlideAndFade` | 317 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs\|UIMotionFeedbackView\|Coroutine:Wiggle` | `Assets/Game/Scripts/UI/Components/UIMotionFeedbackView.cs` | `UIMotionFeedbackView` | `Coroutine:Wiggle` | 274 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Components/WorldFeedbackMarkerView.cs\|WorldFeedbackMarkerView\|Coroutine:ShowRoutine` | `Assets/Game/Scripts/UI/Components/WorldFeedbackMarkerView.cs` | `WorldFeedbackMarkerView` | `Coroutine:ShowRoutine` | 65 | `ProductionUI` |
| `Assets/Game/Scripts/UI/MenuDiagnosticsView.cs\|MenuDiagnosticsView\|Update` | `Assets/Game/Scripts/UI/MenuDiagnosticsView.cs` | `MenuDiagnosticsView` | `Update` | 52 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationView.cs\|ArmoryCategoryNavigationView\|Update` | `Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationView.cs` | `ArmoryCategoryNavigationView` | `Update` | 50 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Screens/ArmoryContentListView.cs\|ArmoryContentListView\|Update` | `Assets/Game/Scripts/UI/Screens/ArmoryContentListView.cs` | `ArmoryContentListView` | `Update` | 32 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs\|BuildDrawerCatalogRuntimeView\|Update` | `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs` | `BuildDrawerCatalogRuntimeView` | `Update` | 58 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs\|BuildPlacementConfirmationBarView\|Update` | `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs` | `BuildPlacementConfirmationBarView` | `Update` | 111 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIAspectVariantView.cs\|UIAspectVariantView\|Update` | `Assets/Game/Scripts/UI/Shell/UIAspectVariantView.cs` | `UIAspectVariantView` | `Update` | 18 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs\|UIGameUiSmokeDriverView\|Coroutine:AnimateLoadingToComplete` | `Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs` | `UIGameUiSmokeDriverView` | `Coroutine:AnimateLoadingToComplete` | 60 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs\|UIGameUiSmokeDriverView\|Coroutine:RunLoadingGate` | `Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs` | `UIGameUiSmokeDriverView` | `Coroutine:RunLoadingGate` | 40 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs\|UIGameUiSmokeDriverView\|Coroutine:WaitForBoundary` | `Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs` | `UIGameUiSmokeDriverView` | `Coroutine:WaitForBoundary` | 76 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs\|UIMotionHostView\|Coroutine:ParallelChildRoutine` | `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs` | `UIMotionHostView` | `Coroutine:ParallelChildRoutine` | 269 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs\|UIMotionHostView\|Coroutine:SequenceRoutine` | `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs` | `UIMotionHostView` | `Coroutine:SequenceRoutine` | 237 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs\|UIMotionHostView\|Coroutine:TrackedRoutine` | `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs` | `UIMotionHostView` | `Coroutine:TrackedRoutine` | 232 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs\|UIMotionHostView\|Coroutine:TweenAlpha` | `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs` | `UIMotionHostView` | `Coroutine:TweenAlpha` | 355 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs\|UIMotionHostView\|Coroutine:TweenAnchoredPosition` | `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs` | `UIMotionHostView` | `Coroutine:TweenAnchoredPosition` | 275 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs\|UIMotionHostView\|Coroutine:TweenScale` | `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs` | `UIMotionHostView` | `Coroutine:TweenScale` | 315 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIPopupMotionView.cs\|UIPopupMotionView\|Coroutine:TweenRoutine` | `Assets/Game/Scripts/UI/Shell/UIPopupMotionView.cs` | `UIPopupMotionView` | `Coroutine:TweenRoutine` | 117 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UISafeAreaView.cs\|UISafeAreaView\|Update` | `Assets/Game/Scripts/UI/Shell/UISafeAreaView.cs` | `UISafeAreaView` | `Update` | 18 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs\|UIShellContentView\|Update` | `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` | `UIShellContentView` | `Update` | 115 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs\|UIShellEcsPresentationSystem\|Update` | `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs` | `UIShellEcsPresentationSystem` | `Update` | 46 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Shell/UIShellLoadingProgressView.cs\|UIShellLoadingProgressView\|Update` | `Assets/Game/Scripts/UI/Shell/UIShellLoadingProgressView.cs` | `UIShellLoadingProgressView` | `Update` | 35 | `ProductionUI` |
| `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs\|UiToolkitShellView\|LateUpdate` | `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs` | `UiToolkitShellView` | `LateUpdate` | 1665 | `ProductionUI` |
