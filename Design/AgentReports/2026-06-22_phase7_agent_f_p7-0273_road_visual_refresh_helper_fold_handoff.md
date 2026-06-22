# Phase 7 Agent F Handoff - 2026-06-22 - P7-0273 Road Visual Refresh Helper Fold

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:
- `P7-0273` - `RoadVisualRefreshSystem` - `Retired/Folded`

Files changed:
- `Assets/Game/Scripts/Systems/RoadVisualRefreshSystem.cs`
- `Assets/Game/Scripts/Systems/RoadBuildCompositionSourceSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
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
- Current inventory: `55` production SystemBase/legacy declarations, `134` production ISystem declarations, `70.9%` production ISystem share.

Implementation notes:
- Removed the disabled empty `SystemBase` lifecycle wrapper from `RoadVisualRefreshSystem`.
- `RoadBuildCompositionSourceSystem` now direct-owns the helper with `new RoadVisualRefreshSystem()`.
- Road visual refresh, road-grid ECS sync, dirty chunk rebuild, special-road object rebuild, and road visual resolution calls stayed unchanged.
- This is a helper fold, so the SystemBase denominator decreased and the ISystem numerator stayed unchanged.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-road-visual-refresh-helper-fold-road-build-command.log` passed with marker `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` passed with marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

Blockers:
- None.

Deferred validation:
- None.

Coordination notes:
- `P7-0273` was inventoried as `DirectConvert`, but implementation review showed a disabled static-helper wrapper with no update/query behavior. Folding to a plain helper preserved the architecture contract better than forcing a broad empty `ISystem` shell.
- Remaining Agent F open rows are visual split/direct candidates.
