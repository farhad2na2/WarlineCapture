# Phase 7 Agent E Handoff - Citizen Status Transition Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0204` - `CitizenStatusTransitionSystem` - Retired/folded from disabled `SystemBase` wrapper into a plain citizen status transition helper.

Files changed:
- `Assets/Game/Scripts/Systems/CitizenStatusTransitionSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result ECS contracts changed.
- The existing helper type name, static call surface, instance call surface, and `StoreCitizenAction` delegate stayed stable.
- `CitizenPopulationCompositionSystem` now creates the status transition helper directly instead of resolving a disabled managed ECS system from the default world.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`
- Inventory after regeneration: `262 total` ECS declarations, `129` production SystemBase/legacy declarations, `133` production ISystem declarations, `50.8%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-status-transition-helper-fold-citizen.log -quit`: passed, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check -- Assets/Game/Scripts/Systems/CitizenStatusTransitionSystem.cs Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs Design/Architecture/systembase_to_isystem_inventory.md`: passed.

Risks:
- The helper intentionally keeps its historical `*System` type name so existing call sites, test setup, and delegate references remain stable. It no longer inherits `SystemBase`, has no ECS lifecycle, and is manually owned by citizen population composition.
