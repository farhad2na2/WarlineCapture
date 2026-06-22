# Phase 7 Agent E Handoff - P7-0233 RoadPreviewSystem Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0233` - `RoadPreviewSystem` - folded from a disabled `SystemBase` wrapper into a plain road preview helper.

Files changed:
- `Assets/Game/Scripts/Systems/RoadPreviewSystem.cs`
- `Assets/Game/Scripts/Systems/RoadBuildCompositionSourceSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No ECS request/result contract changed.
- `RoadPreviewSystem` remains the road preview helper API for clear, update, and dispose behavior.
- `RoadBuildCompositionSourceSystem` now owns the helper instance directly instead of resolving it from the ECS world.

Behavior preserved:
- Preview object pooling and release behavior.
- Preview material alpha copy behavior.
- Preview path rebuild and placement behavior.
- Single-cell end-preview behavior.
- Road composition update, clear, and disposal callers.

Counts after regeneration:
- Production `SystemBase`/legacy declarations: `64`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `67.7%`.
- Total ECS system declarations: `198`.
- Production non-UI rows: `190`.
- Production UI rows: `8`.
- Agent E remaining inventory rows: `16`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` - passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md` - passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-preview-helper-fold-road-build-command.log` - passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` - passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` - passed.

Risks:
- `RoadPreviewSystem` remains a managed Unity-object preview helper by design. It was not converted to `ISystem` because it owns `GameObject`, `Transform`, `Renderer`, and `Material` preview presentation state.
