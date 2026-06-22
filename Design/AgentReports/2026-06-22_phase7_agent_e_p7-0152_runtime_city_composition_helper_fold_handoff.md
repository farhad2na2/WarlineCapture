# Phase 7 Agent E Handoff - P7-0152 RuntimeCityCompositionSystem Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0152` - `RuntimeCityCompositionSystem` - folded from a disabled `SystemBase` wrapper into a plain runtime-city composition helper.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Assets/Game/Scripts/Composition/GameplayFeatureStartupSystem.cs`
- `Assets/Game/Scripts/Editor/RuntimeCitySpawnerStep13Validation.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No ECS request/result contract changed.
- `RuntimeCityCompositionSystem` remains the runtime-city composition API for startup configuration, lifecycle update, manual generation, read-model publication, house-prefab checks, and disposal.
- `GameplayFeatureStartupSystem` now owns a direct helper instance instead of resolving `RuntimeCityCompositionSystem` from the ECS world.
- `RuntimeCitySpawnerStep13Validation` now instantiates the helper directly for the disabled-city validation path.

Behavior preserved:
- Runtime-city config projection.
- Startup blocker description and spawn-on-start gating.
- Lifecycle ticking and read-model publication.
- Manual city generation entry point.
- Child boundary composition for generation, spawn bridges, road bridges, visuals, minimap events, and disposal.
- No-city validation behavior for `cityCount == 0`.

Counts after regeneration:
- Production `SystemBase`/legacy declarations: `63`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `68.0%`.
- Total ECS system declarations: `197`.
- Production non-UI rows: `189`.
- Production UI rows: `8`.
- Agent E remaining inventory rows: `15`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` - passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md` - passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-composition-helper-fold-runtime-city-generation.log` - passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` - passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` - passed.

Risks:
- `RuntimeCityCompositionSystem` remains a managed composition boundary by design. It owns runtime-city config, GameObject prefab lists, visual/root handoff, bridge wiring, and managed child-boundary orchestration, so it was folded out of ECS instead of converted to an unmanaged `ISystem`.
