# Phase 7 Agent E Handoff - P7-0238 RuntimeGridBootstrapSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0238` - `RuntimeGridBootstrapSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeGridBootstrapSystem` from a disabled `SystemBase` wrapper into a plain runtime-grid bootstrap helper.
- `MatchBootstrapSystem` now owns the helper directly and passes `world.EntityManager` explicitly.
- Preserved grid config projection, runtime grid entity resolution, grid walkable/road/sidewalk/dirt buffers, dynamic blocker storage, dynamic occupancy storage, and path-pool setup.
- Updated the affected runtime-grid focused test to instantiate the helper directly and pass `_entityManager`.

Files changed:
- `Assets/Game/Scripts/Systems/RuntimeGridBootstrapSystem.cs`
- `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
- `Assets/Tests/Editor/RuntimeGridDeduplicationSystemTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No ECS request/result contracts changed.
- The helper call contract now requires an explicit `EntityManager`, making ECS mutation ownership visible at the composition call site.

Counts after inventory regeneration:
- Total ECS system declarations: `225`.
- Production `SystemBase`/legacy declarations: `91`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `59.6%`.
- Production non-UI rows: `217`.
- Production UI rows: `8`.
- Agent E owner rows remaining: `43`.
- DirectConvert rows remaining: `8`.
- Open rows remaining: `69`.

Agent E counts:
- Converted to ISystem: `0`.
- Split passive/managed boundaries: `0`.
- Managed SystemBase exceptions: `0`.
- Retired/folded helpers: `54`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeGridDeduplicationSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-grid-bootstrap-helper-fold-runtime-grid.log`: passed, marker `[RuntimeGridDeduplicationFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-grid-bootstrap-helper-fold-road-command.log`: passed, marker `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Coordination notes:
- No Agent C/D/F contract changes were needed.
- No remaining Agent E `DirectConvert` rows were found in the regenerated inventory; remaining Agent E rows are split/managed-exception candidates.
- No MonoBehaviour ticking, manager/controller/facade, or broad replacement `ISystem` was introduced.

Risks:
- Low. Runtime grid bootstrap remains composition-owned and one-shot; affected runtime-grid and road-build validations passed.
