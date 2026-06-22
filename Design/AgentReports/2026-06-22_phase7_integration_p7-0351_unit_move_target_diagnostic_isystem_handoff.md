# Phase 7 Integration Handoff - P7-0351 UnitMoveTargetDiagnosticSystem

Date: 2026-06-22

Slice:
`P7-0351` `UnitMoveTargetDiagnosticSystem`

## Summary

- Converted `UnitMoveTargetDiagnosticSystem` in `Assets/Game/Scripts/Systems/UnitMoveTargetDiagnosticSystem.cs` from `SystemBase` to `ISystem`.
- Replaced the managed `Dictionary<Entity, int2>` and `List<Entity>` caches with persistent native containers and explicit disposal.
- Added `Assets/Game/Scripts/Editor/UnitMoveTargetDiagnosticValidationRunner.cs` to verify ECS construction and source-level conversion invariants.
- Introduced no manager/controller/facade, no new `MonoBehaviour` update loop, and no UI Toolkit/Canvas work.

## Behavior Preserved

- `[UpdateInGroup(typeof(SimulationSystemGroup))]` stayed unchanged.
- The system still disables itself when `SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace` is false.
- The player-owned `UnitTarget` change scan still reads `UnitTarget`, `UnitGrid`, and `Faction` through the same query.
- Move-target changes still log through `SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace`.
- Entity description and path request details still resolve through `EntityManager`.
- Stale cached targets are still pruned every 120 frames.

## Inventory Impact

- Total ECS declarations: `173`.
- Production `SystemBase`/legacy declarations: `36`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `79.2%`.
- Inventory rows: `173 total`, `165 ProductionNonUI`, `8 ProductionUI`.
- Owner lanes: `AgentB 19`, `AgentC 12`, `AgentD 9`, `AgentE 10`, `AgentF 34`, `Integration 89`.
- Dispositions: `Converted 130`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 9`, `UIOutOfScope 8`.
- Statuses: `Converted 130`, `Deferred 8`, `ManagedException 24`, `Open 11`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMoveTargetDiagnosticValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-unit-move-target-diagnostic-isystem.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Unit move target diagnostic focused validation passed: `/private/tmp/warline-phase7-integration-unit-move-target-diagnostic-isystem.log` marker `[UnitMoveTargetDiagnosticValidation] result=Passed tests=1`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration split-first rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
