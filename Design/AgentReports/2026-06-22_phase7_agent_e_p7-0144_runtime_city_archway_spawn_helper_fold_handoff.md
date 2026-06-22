# Phase 7 Agent E Handoff - P7-0144 RuntimeCityArchwaySpawnSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0144` - `RuntimeCityArchwaySpawnSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeCityArchwaySpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime-city archway spawn helper.
- `RuntimeCityCompositionSystem` now constructs the helper directly instead of resolving it from the ECS world.
- Preserved central archway placement, prefab-list selection, plot spacing checks, reserved-footprint mutation, and runtime-city decoration spawn callers.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityArchwaySpawnSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No ECS request/result contracts changed.
- Runtime-city composition helper ownership is now direct construction instead of `World.GetOrCreateSystemManaged<T>()`.

Counts after inventory regeneration:
- Total ECS system declarations: `224`.
- Production `SystemBase`/legacy declarations: `90`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `59.8%`.
- Production non-UI rows: `216`.
- Production UI rows: `8`.
- Agent E owner rows remaining: `42`.
- DirectConvert rows remaining: `8`.
- SplitThenConvert rows remaining: `57`.
- Open rows remaining: `68`.

Agent E counts:
- Converted to ISystem: `0`.
- Split passive/managed boundaries: `0`.
- Managed SystemBase exceptions: `0`.
- Retired/folded helpers: `55`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-archway-spawn-helper-fold-city-generation.log`: passed, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Coordination notes:
- No Agent C/D/F contract changes were needed.
- No MonoBehaviour ticking, manager/controller/facade, or broad replacement `ISystem` was introduced.

Risks:
- Low. The wrapper was disabled and empty; the state class already owned the behavior.
