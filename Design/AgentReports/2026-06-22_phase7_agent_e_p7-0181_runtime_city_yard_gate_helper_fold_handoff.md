# Phase 7 Agent E Handoff - P7-0181 Runtime City Yard Gate Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0181` - `RuntimeCityYardGateSystem` - `Retired/Folded`

Classification note:
- Inventory previously listed this row as `DirectConvert`, but implementation inspection showed an empty disabled `SystemBase` wrapper around a plain helper state and two deterministic helper methods. The behavior-preserving first safe slice was therefore to fold it out of ECS instead of creating a non-updating `ISystem` shell.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityYardGateSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Summary:
- Folded `RuntimeCityYardGateSystem` from a disabled `SystemBase` wrapper into a plain runtime-city yard-gate helper.
- Preserved `RuntimeCityYardGateState`, `YardSide`, `GetCenteredOpeningStart`, `GetPreferredYardGateSide`, and `State`.
- Updated `RuntimeCityCompositionSystem` to direct-own the helper with `new RuntimeCityYardGateSystem()` instead of resolving it through `World.GetOrCreateSystemManaged`.
- Removed the empty ECS lifecycle surface without introducing a replacement ECS shell, managed runtime owner, or new MonoBehaviour loop.

Contracts changed:
- None. Yard-gate calculations and runtime-city composition state access are unchanged.

Inventory delta:
- Total ECS declarations: `266`.
- Production `SystemBase`/legacy declarations: `133`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `50.0%`.
- Production non-UI rows: `259`.
- Production UI rows: `7`.
- Agent E rows: `85`.
- DirectConvert rows: `40`.
- Remaining `RetireFold` rows: `3`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-yard-gate-helper-fold-city.log -quit`: passed, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Risks:
- No runtime behavior change intended. This slice removed only the disabled `SystemBase` inheritance and empty lifecycle methods from a direct-owned yard-gate helper.
