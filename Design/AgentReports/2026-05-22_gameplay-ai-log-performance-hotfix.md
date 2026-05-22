Lane
Gameplay

Task
Investigate and hotfix the renewed performance drop after the AI diagnostics boundary refactor.

Files changed
- `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs`
- `Assets/Game/Scripts/Systems/AILog.cs`
- `Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs`
- `Assets/Game/Scripts/Systems/AIProductionSystem.cs`
- `Assets/Game/Scripts/Systems/AISquadSystem.cs`
- `Assets/Game/Scripts/Systems/AITargetingSystem.cs`
- `Assets/Game/Scripts/Systems/AIEconomySystem.cs`
- `Assets/Game/Scripts/Systems/AIFactionControlSystem.cs`
- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/AgentReports/2026-05-22_gameplay-ai-log-performance-hotfix.md`

Contracts touched
- Added `HotAiSystemsMustGuardAILogMessageConstruction` to `GameplayArchitectureContractTests`.
- The guard prevents known AI hot systems from constructing `AILog` messages unless `AILog.IsEnabled` is nearby.

User-visible behavior
- No intended gameplay behavior change.
- When AI verbose logs are disabled, AI systems avoid per-frame diagnostic string construction and the AI log gate no longer reads ECS data per log call.

Validation run
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs Assets/Game/Scripts/Systems/AILog.cs Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs Assets/Game/Scripts/Systems/AIProductionSystem.cs Assets/Game/Scripts/Systems/AISquadSystem.cs Assets/Game/Scripts/Systems/AITargetingSystem.cs Assets/Game/Scripts/Systems/AIEconomySystem.cs Assets/Game/Scripts/Systems/AIFactionControlSystem.cs Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- Unity EditMode `AI`
- Unity EditMode `RuntimeDiagnosticsSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`

Validation result
- Diff whitespace check passed.
- `AI`: passed 20/20.
- `RuntimeDiagnosticsSystemTests`: passed 3/3.
- `GameplayArchitectureContractTests`: passed 43/43.

Known gaps
- This was a source-level hotfix plus focused Unity validation, not a full live FPS/profile capture.
- `AILog` remains a static compatibility facade. It is still legacy debt, but disabled AI logging is now cheap again.
- Startup AI config logging in `GameBootstrap` is still unguarded because it is not a steady-state hot path.

Cross-lane impacts
- QA should retest the previously observed 60 FPS to 20 FPS drop in the playable scene.
- Future AI hot-system log additions must be guarded by `AILog.IsEnabled` until the static facade is fully retired.

Next recommended task
Run the actual in-editor/device performance scenario that showed the FPS drop and confirm frame time returns to baseline. If it still drops, collect system timing and GC samples for `UnitPathfindingSystem`, `UnitRenderBudgetSystem`, AI systems, and presentation systems.
