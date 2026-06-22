# Phase 7 Agent E Handoff - P7-0234 Road Runtime Generation Context Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0234` - `RoadRuntimeGenerationContextSystem` - `Retired/Folded`

Files changed:
- `Assets/Game/Scripts/Systems/RoadRuntimeGenerationContextSystem.cs`
- `Assets/Game/Scripts/Systems/RoadBuildCompositionSourceSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Summary:
- Folded `RoadRuntimeGenerationContextSystem` from a disabled `SystemBase` wrapper into a plain road runtime generation context helper.
- Preserved `Context`, `CreateContext`, `BeginDeferredRoadEcsSync`, and `EndDeferredRoadEcsSync`.
- Updated `RoadBuildCompositionSourceSystem` to direct-own the helper with `new RoadRuntimeGenerationContextSystem()` instead of resolving it through `World.GetOrCreateSystemManaged`.
- Removed the empty ECS lifecycle surface without introducing a replacement ECS shell, managed runtime owner, or new MonoBehaviour loop.

Contracts changed:
- None. Road runtime generation context construction is unchanged.

Inventory delta:
- Total ECS declarations: `267`.
- Production `SystemBase`/legacy declarations: `134`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `49.8%`.
- Production non-UI rows: `260`.
- Production UI rows: `7`.
- Agent E rows: `86`.
- Remaining `RetireFold` rows: `3`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-runtime-generation-context-helper-fold-road.log -quit`: passed, marker `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Risks:
- No runtime behavior change intended. This slice removed only the disabled `SystemBase` inheritance and empty lifecycle methods from a direct-owned context helper.
