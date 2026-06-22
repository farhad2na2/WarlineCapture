# Phase 7 Agent E Handoff - 2026-06-22 - P7-0198 CitizenPopulationTotalsSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0198` - `CitizenPopulationTotalsSystem` - `Retired/Folded`

Scope:
- Folded `CitizenPopulationTotalsSystem` from a disabled `SystemBase` wrapper into a plain citizen population totals helper.
- Preserved totals calculation, citizen data checks, household data checks, read-model refresh behavior, and citizen composition callers.
- Replaced `World.GetOrCreateSystemManaged<CitizenPopulationTotalsSystem>()` with plain helper construction in `CitizenPopulationCompositionSystem.Result`.
- Removed the stale `new` modifier from `CitizenPopulationRuntimeUpdateSystem.Update()` after the previous helper fold.

Files changed:
- `Assets/Game/Scripts/Systems/CitizenPopulationTotalsSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationRuntimeUpdateSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- None. No ECS request/result component or public behavior contract changed.

Counts after inventory regeneration:
- Total ECS system declarations: `235`
- Production `SystemBase`/legacy declarations: `101`
- Production `ISystem` declarations: `134`
- Current production `ISystem` share: `57.0%`
- Production non-UI rows: `227`
- Production UI rows: `8`
- Agent E owner rows: `53`
- DirectConvert dispositions: `18`
- Open rows: `79`

Agent E counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `44`

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-totals-helper-fold-visible-unit.log`: passed, marker `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-population-totals-helper-fold-movement.log`: passed, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Notes:
- An initial parallel Unity validation attempt failed because concurrent Unity instances contended for Bee/ILPP output files. The same validations were rerun sequentially and passed.
- No Agent C/D/F coordination needed for this helper-only citizen totals fold.
- Next low-risk Agent E candidate from the regenerated inventory: `P7-0202 CitizenResourceSystem`.
