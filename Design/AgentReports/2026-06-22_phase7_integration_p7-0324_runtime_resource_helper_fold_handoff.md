# Phase 7 Integration Handoff - P7-0324 RuntimeResourceSystem

Date: 2026-06-22

Slice:
`P7-0324` `RuntimeResourceSystem`

## Summary

- Folded `RuntimeResourceSystem` in `Assets/Game/Scripts/Systems/RuntimeResourceSystem.cs` out of ECS by removing its disabled `SystemBase` wrapper.
- Kept the existing direct-owned helper API used by building production, building placement, UI/build drawer, and citizen resource composition.
- Introduced no manager/controller/facade, no new `MonoBehaviour` update loop, and no UI Toolkit/Canvas work.

## Rationale

The inventory row was labeled `DirectConvert`, but the implementation was not a recurring ECS processor:

- `OnCreate` only set `Enabled = false`.
- `OnUpdate` was empty.
- Runtime behavior was plain helper state and direct-call methods: `SetInitialDollars`, `AddDollars`, `TrySpendDollars`, and `CreateCitizenResourceContext`.

Creating an `ISystem` shell for this would have been inheritance churn. Folding the disabled wrapper removes the legacy ECS declaration while preserving the direct-owned resource helper.

## Behavior Preserved

- Initial dollars still clamp to non-negative values.
- Adding dollars still ignores negative input.
- Spending dollars still succeeds for zero/negative cost, fails when insufficient, and decrements when sufficient.
- `CreateCitizenResourceContext` still reads and writes the same runtime dollar state.
- Building gameplay composition still uses the same runtime resource helper instance.

## Inventory Impact

- Total ECS declarations: `174`.
- Production `SystemBase`/legacy declarations: `38`.
- Production `ISystem` declarations: `136`.
- Current production `ISystem` share: `78.2%`.
- Inventory rows: `174 total`, `166 ProductionNonUI`, `8 ProductionUI`.
- Owner lanes: `AgentB 19`, `AgentC 12`, `AgentD 9`, `AgentE 10`, `AgentF 34`, `Integration 90`.
- Dispositions: `Converted 129`, `DirectConvert 2`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 9`, `UIOutOfScope 8`.
- Statuses: `Converted 129`, `Deferred 8`, `ManagedException 24`, `Open 13`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-integration-runtime-resource-helper-fold-building-composition.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Building gameplay composition focused validation passed: `/private/tmp/warline-phase7-integration-runtime-resource-helper-fold-building-composition.log` marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
