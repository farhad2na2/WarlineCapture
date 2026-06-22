# Phase 7 Agent E Handoff - P7-0224 Road Build Read Model Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0224` - `RoadBuildReadModelSystem` - `Retired/Folded`

Files changed:
- `Assets/Game/Scripts/Systems/RoadBuildReadModelSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Summary:
- Folded `RoadBuildReadModelSystem` from a disabled `SystemBase` wrapper into a plain road build read-model helper.
- Preserved the public read-only properties used by selection/camera startup, plus `Configure` and `Clear`.
- Preserved the direct-owned road composition call path through `RoadBuildCompositionSourceSystem`, `RoadBuildCompositionLifecycleSystem`, and `RoadBuildCompositionContextSystem`.
- Removed the empty ECS lifecycle surface without introducing a replacement ECS shell, managed runtime owner, or new MonoBehaviour loop.

Contracts changed:
- None. Road build read-model access remains through the same public type and properties.

Inventory delta:
- Total ECS declarations: `268`.
- Production `SystemBase`/legacy declarations: `135`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `49.6%`.
- Production non-UI rows: `261`.
- Production UI rows: `7`.
- Agent E rows: `87`.
- Remaining `RetireFold` rows: `4`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-read-model-helper-fold-road.log -quit`: passed, marker `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Risks:
- No runtime behavior change intended. This slice removed only the disabled `SystemBase` inheritance and empty lifecycle methods from a direct-owned read-model helper.
