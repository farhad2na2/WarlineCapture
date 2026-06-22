# Phase 7 Agent E Handoff - P7-0212 RoadBuildCompositionSystem Helper Fold

Date: 2026-06-22

## Scope

- Inventory row: `P7-0212 RoadBuildCompositionSystem`
- Lane: Agent E road/city/citizen
- Disposition: `SplitThenConvert` helper fold
- Goal: remove the disabled `SystemBase` wrapper while preserving road build composition startup and binding behavior.

## Changes

- Folded `Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs` from a disabled `SystemBase` into a plain managed helper.
- Kept the existing `Initialize`, `BindBuildingInteraction`, `BindMainMenu`, and `BindRuntimeGameplayFeatures` APIs unchanged.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 progress trackers.

## Behavior Preserved

- Managed gameplay startup still owns and initializes the same road build composition helper.
- Initialization still creates a `RoadBuildCompositionSourceSystem`, initializes lifecycle dependencies, and returns the same read-model/runtime-generation/runtime-update/GUI/dispose result delegates.
- Building interaction, main menu, runtime gameplay feature, and runtime grid blocker binding still forward through the same dependency binding path.
- No new manager/controller/facade and no new updating `MonoBehaviour` loop were introduced.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-composition-helper-fold-road-build-command.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor assembly compile passed.
- Inventory regeneration passed.
- `git diff --check` passed.
- Road build command validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- Phase 7 architecture guard passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS declarations: `205`
- Production non-UI rows: `197`
- Production UI rows: `8`
- Production `SystemBase`/legacy declarations: `71`
- Production `ISystem` declarations: `134`
- Production `ISystem` share: `65.4%`

## Follow-Up

- Continue Agent E with the remaining `23` open split/managed-exception candidates.
