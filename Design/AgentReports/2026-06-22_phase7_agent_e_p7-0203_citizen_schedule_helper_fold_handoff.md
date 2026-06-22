# Phase 7 Agent E Handoff - 2026-06-22 - P7-0203 CitizenScheduleSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0203` - `CitizenScheduleSystem` - `Retired/Folded`

Scope:
- Folded `CitizenScheduleSystem` from a disabled `SystemBase` wrapper into a plain citizen schedule helper.
- Preserved schedule phase calculation, weekday/weekend/refugee status policy, scheduled target building selection, and citizen runtime callers.
- Replaced `World.GetOrCreateSystemManaged<CitizenScheduleSystem>()` with plain helper construction in `CitizenPopulationCompositionSystem.Result`.

Files changed:
- `Assets/Game/Scripts/Systems/CitizenScheduleSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- None. No ECS request/result component or public behavior contract changed.

Counts after inventory regeneration:
- Total ECS system declarations: `233`
- Production `SystemBase`/legacy declarations: `99`
- Production `ISystem` declarations: `134`
- Current production `ISystem` share: `57.5%`
- Production non-UI rows: `225`
- Production UI rows: `8`
- Agent E owner rows: `51`
- DirectConvert dispositions: `16`
- Open rows: `77`

Agent E counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `46`

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-schedule-helper-fold-visible-unit.log`: passed, marker `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenMovementCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-citizen-schedule-helper-fold-movement.log`: passed, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log`: passed, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Notes:
- No Agent C/D/F coordination needed for this helper-only citizen schedule fold.
- Next low-risk Agent E candidate from the regenerated inventory: `P7-0216 RoadBuildDependencySystem` or `P7-0217 RoadBuildDisposalSystem`.
