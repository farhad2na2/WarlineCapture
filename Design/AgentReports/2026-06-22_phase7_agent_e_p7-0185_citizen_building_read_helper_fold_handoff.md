# Phase 7 Agent E Handoff - Citizen Building Read Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0185` - `CitizenBuildingReadSystem` - Retired/folded from disabled `SystemBase` wrapper into a plain citizen building read helper.

Files changed:
- `Assets/Game/Scripts/Systems/CitizenBuildingReadSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result ECS contracts changed.
- The existing helper type name and public read API stayed stable: runtime building list refresh, role list accessors, focus position lookup, destroyed/refugee settings lookup, approach-cell helpers, and nearest-building helpers are unchanged.
- `CitizenPopulationCompositionSystem` and focused tests already construct this helper directly, so no composition resolver change was required.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`
- Inventory after regeneration: `260 total` ECS declarations, `127` production SystemBase/legacy declarations, `133` production ISystem declarations, `51.2%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-building-read-helper-fold-citizen.log -quit`: passed, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check -- Assets/Game/Scripts/Systems/CitizenBuildingReadSystem.cs Design/Architecture/systembase_to_isystem_inventory.md`: passed.

Risks:
- This helper still bridges managed building runtime query data into citizen simulation callers. The fold removes false ECS ownership only; later citizen/building split rows still need proper ECS-native building snapshots if the architecture wants to remove this managed read bridge entirely.
