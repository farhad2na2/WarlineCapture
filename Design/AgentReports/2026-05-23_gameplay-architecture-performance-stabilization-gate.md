Lane
Gameplay

Task
Architecture and performance stabilization gate before continuing gameplay coding.

Files changed
- `Assets/Game/Scenes/Game.unity`
- `Assets/Tests/PlayMode/GameSceneIsolationPlayModeTests.cs`
- `Design/AgentReports/2026-05-23_gameplay-architecture-performance-stabilization-gate.md`

Contracts touched
- Gameplay architecture contract validation.
- Promoted default `Game` scene PlayMode isolation contract.
- Bootstrap scene-reference contract for `GameBootstrap.WorldCamera`, `GameBootstrap.GlobalVolume`, `GameBootstrap.DirectionalLight`, and `GameBootstrap.DecorationRoot`.

User-visible behavior
- `GameBootstrap` in `Assets/Game/Scenes/Game.unity` now has the existing `Global Volume` and `Directional light` scene objects bound, so promoted default gameplay startup receives the lighting/post-processing references expected by the scene smoke.

Validation run
- `git diff --check -- Assets/Game/Scenes/Game.unity Assets/Tests/PlayMode/GameSceneIsolationPlayModeTests.cs`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-stabilization-architecture-rerun.xml -logFile /private/tmp/warlinecapture-stabilization-architecture-rerun.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter GameSceneIsolationPlayModeTests.GameScene_PlayUsesPromotedDefaultCanvasWithoutOld2DRoute -testResults /private/tmp/warlinecapture-stabilization-game-scene-smoke-rerun.xml -logFile /private/tmp/warlinecapture-stabilization-game-scene-smoke-rerun.log`

Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed `70/70`.
- `GameSceneIsolationPlayModeTests.GameScene_PlayUsesPromotedDefaultCanvasWithoutOld2DRoute`: passed `1/1`.
- Initial attempted M01 production smoke was not applicable to the promoted default `Game` scene because that scene intentionally no longer contains `Chapter01MissionTacticalRuntimeBinder`.

Known gaps
- This was a stabilization smoke, not a full performance acceptance gate. The project still lacks an automated current-scene performance test that records frame-time average, p95, p99, max, GC allocation, and system timing against explicit budgets.
- The PlayMode smoke emitted `[PerfDiag] slowUpdate frame=11 total=41.7ms`, with the largest managed step samples from `Selection=12.4ms`, `BuildingPlacement=11.4ms`, and `CitizenPopulation=10.9ms`. Treat this as a startup diagnostic to track, not as a milestone FPS failure.
- The same short PlayMode run emitted a `[FrameRateDiag] fps=0.6` sample dominated by scene load/editor test startup. It is not a valid steady-state FPS measurement.

Cross-lane impacts
- QA can use the two result XML paths above as the current architecture/default-scene smoke evidence.
- Performance lane should add a true automated promoted-`Game` performance scenario before relying on p95/p99 claims.

Next recommended task
Add a focused promoted-`Game` PlayMode performance test that warms up after scene load, runs a fixed number of gameplay frames, records structured frame/GC/system metrics, and reports p95/p99 budgets separately from editor scene-load spikes.
