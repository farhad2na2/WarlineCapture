# Phase 7 Agent E Handoff - P7-0211 Road Build Composition Source Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0211` - `RoadBuildCompositionSourceSystem` - `Retired/Folded`

Files changed:
- `Assets/Game/Scripts/Systems/RoadBuildCompositionSourceSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Summary:
- Folded `RoadBuildCompositionSourceSystem` from a disabled `SystemBase` wrapper into a plain road build composition source helper.
- Preserved child-system source fields, resolver state, and direct `new RoadBuildCompositionSourceSystem()` ownership from `RoadBuildCompositionSystem`.
- Removed the empty ECS lifecycle surface without introducing a replacement ECS shell, managed runtime owner, or new MonoBehaviour loop.

Contracts changed:
- None. Road composition still owns and wires the same helper fields.

Inventory delta:
- Total ECS declarations: `271`.
- Production `SystemBase`/legacy declarations: `138`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `49.1%`.
- Production non-UI rows: `264`.
- Production UI rows: `7`.
- Agent E rows: `90`.
- Remaining `RetireFold` rows: `7`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-composition-source-helper-fold-road.log -quit`: passed, marker `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- No runtime behavior change intended. This slice removed only the disabled `SystemBase` inheritance and empty lifecycle methods from a direct-owned composition source helper.
