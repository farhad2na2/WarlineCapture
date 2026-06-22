# Phase 7 Agent E Handoff - Citizen Population State Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0197` - `CitizenPopulationStateSystem` - Retired/folded from disabled `SystemBase` wrapper into a plain citizen population state holder.

Files changed:
- `Assets/Game/Scripts/Systems/CitizenPopulationStateSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result ECS contracts changed.
- The existing helper type name and public state API stayed stable: dictionaries, scratch lists, counters, id allocation, store/read helpers, and visible-citizen id population methods are unchanged.
- `CitizenPopulationCompositionSystem` and focused tests already construct this state holder directly, so no composition resolver change was required.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`
- Inventory after regeneration: `261 total` ECS declarations, `128` production SystemBase/legacy declarations, `133` production ISystem declarations, `51.0%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-state-helper-fold-citizen.log -quit`: passed, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check -- Assets/Game/Scripts/Systems/CitizenPopulationStateSystem.cs Design/Architecture/systembase_to_isystem_inventory.md`: passed.

Risks:
- This remains a managed in-memory population state holder with dictionaries and scratch lists. The fold removes invalid ECS ownership but does not yet migrate citizen population storage into ECS-native data; that belongs to the later split rows for citizen projection, household registration, refugee, and population runtime work.
