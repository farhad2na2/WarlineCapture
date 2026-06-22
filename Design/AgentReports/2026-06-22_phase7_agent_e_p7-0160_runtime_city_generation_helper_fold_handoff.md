# Phase 7 Agent E Handoff - P7-0160 RuntimeCityGenerationSystem Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0160` - `RuntimeCityGenerationSystem` - folded from a disabled `SystemBase` wrapper into a plain runtime-city generation helper.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No ECS request/result contract changed.
- `RuntimeCityGenerationSystem` remains the runtime-city generation helper API for `State` and `TryBegin`.
- `RuntimeCityCompositionSystem` now owns the generation helper directly instead of resolving it from the ECS world.

Behavior preserved:
- Generation state ownership.
- City-generation coroutine orchestration.
- Deferred road ECS sync begin/end ordering.
- Deferred building spawn side-effect begin/end ordering.
- City-list and random-state lifetime.
- Bulk building routine stepping.
- Static minimap event publication.
- Generation completion and lifecycle handoff.

Counts after regeneration:
- Production `SystemBase`/legacy declarations: `62`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `68.4%`.
- Total ECS system declarations: `196`.
- Production non-UI rows: `188`.
- Production UI rows: `8`.
- Agent E remaining inventory rows: `14`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` - passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md` - passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-generation-helper-fold-runtime-city-generation.log` - passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` - passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` - passed.

Notes:
- The first runtime-city Unity validation attempt failed because the parallel architecture validation held a Bee artifact lock on `Game.Tests.Editor.dll`. The same runtime-city validation was rerun by itself and passed.

Risks:
- `RuntimeCityGenerationSystem` remains a managed coroutine helper by design. It owns generation coroutine orchestration and managed collection state, so it was folded out of ECS instead of converted to an unmanaged `ISystem`.
