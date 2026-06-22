# Phase 7 Agent E Handoff - 2026-06-22 - P7-0206 Citizen Visible Unit Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0206` - `CitizenVisibleUnitSystem` - `Retired/Folded`

Files changed:
- `Assets/Game/Scripts/Systems/CitizenVisibleUnitSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- None.

Shared components/contracts/asmdefs/tests touched:
- None.

Generated inventory touched:
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- JSON sidecar emitted to `/private/tmp/warline-phase7-systembase-inventory.json`.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions created: `0`
- Retired/folded: `1`
- Current inventory: `58` production SystemBase/legacy declarations, `134` production ISystem declarations, `69.8%` production ISystem share.

Implementation notes:
- Removed the disabled empty `SystemBase` lifecycle wrapper from `CitizenVisibleUnitSystem`.
- Kept visible citizen sync, same-frame entity instantiation, component setup, move-command enqueueing, visible citizen removal, clear/dispose behavior, and citizen population callers unchanged.
- This is a helper fold, so the SystemBase denominator decreased and the ISystem numerator stayed unchanged.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` passed.
- `git diff --check` passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` passed with marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-visible-unit-helper-fold-citizen-visible-unit.log` passed with marker `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.

Blockers:
- None.

Deferred validation:
- None.

Coordination notes:
- Agent E open split candidates from the regenerated inventory are complete.
- Remaining Agent E-owned `SystemBase` rows are counted managed presentation/config/camera exceptions.
