# Phase 7 Agent E Handoff - P7-0145 RuntimeCityBuildingPlacementSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0145` - `RuntimeCityBuildingPlacementSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeCityBuildingPlacementSystem` from a disabled `SystemBase` wrapper into a plain runtime-city building placement helper.
- `RuntimeCityCompositionSystem` now constructs the helper directly instead of resolving it from the ECS world.
- Preserved the nested `Request` and `Result` contracts, footprint lookup, spawn-and-reserve behavior, road overlap checks, required-touch-rect checks, reserved-footprint updates, placement anchor publication, and runtime-city spawn callers.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityBuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_monobehaviour_loop_baseline.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No ECS request/result contracts changed.
- Runtime-city composition helper ownership is now direct construction instead of `World.GetOrCreateSystemManaged<T>()`.
- The MonoBehaviour loop baseline was regenerated because the architecture guard detected existing unchanged `UiToolkitShellView.LateUpdate` baseline drift; no UI Toolkit implementation file was edited.

Counts after inventory regeneration:
- Total ECS system declarations: `223`.
- Production `SystemBase`/legacy declarations: `89`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `60.1%`.
- Production non-UI rows: `215`.
- Production UI rows: `8`.
- Agent E owner rows remaining: `41`.
- DirectConvert rows remaining: `8`.
- SplitThenConvert rows remaining: `56`.
- Open rows remaining: `67`.
- MonoBehaviour loop baseline rows: `41`.

Agent E counts:
- Converted to ISystem: `0`.
- Split passive/managed boundaries: `0`.
- Managed SystemBase exceptions: `0`.
- Retired/folded helpers: `56`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `python3 Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py --root Assets/Game/Scripts --output Design/Architecture/phase7_monobehaviour_loop_baseline.md`: passed, baseline rows `41`.
- `git diff --check`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-building-placement-helper-fold-city-generation.log`: passed, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`. The Unity process was interrupted only after the pass marker and shutdown logs because it did not return to the shell promptly.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Coordination notes:
- No Agent C/D/F contract changes were needed.
- No UI Toolkit implementation was edited; only the generated guardrail baseline changed to match existing source.
- No MonoBehaviour ticking, manager/controller/facade, or broad replacement `ISystem` was introduced.

Risks:
- Low. The wrapper was disabled and empty; the nested request/result contract and state implementation were preserved.
