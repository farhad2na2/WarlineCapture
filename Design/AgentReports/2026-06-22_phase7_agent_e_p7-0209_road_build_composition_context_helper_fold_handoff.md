# Phase 7 Agent E Handoff - P7-0209 RoadBuildCompositionContextSystem Helper Fold

Date: 2026-06-22

## Scope

- Inventory row: `P7-0209 RoadBuildCompositionContextSystem`
- Lane: Agent E road/city/citizen
- Disposition: `SplitThenConvert` helper fold
- Goal: remove the disabled `SystemBase` wrapper while preserving road build context factory behavior.

## Changes

- Folded `Assets/Game/Scripts/Systems/RoadBuildCompositionContextSystem.cs` from a disabled `SystemBase` into a plain managed helper.
- Kept the existing context factory API and all road composition call sites unchanged.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 progress trackers.

## Behavior Preserved

- Road footprint, runtime generation, read-model, interaction, input, command, delete prompt, disposal, ECS, visual, mutation, and placement context creation stayed unchanged.
- `RoadBuildCompositionSourceSystem` still owns the helper instance directly.
- Road composition callers still use the same `RoadBuildCompositionContextSystem` methods.
- No new manager/controller/facade and no new updating `MonoBehaviour` loop were introduced.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-composition-context-helper-fold-road-build-command.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor assembly compile passed.
- Inventory regeneration passed.
- `git diff --check` passed.
- Road build command validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- Phase 7 architecture guard passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS declarations: `206`
- Production non-UI rows: `198`
- Production UI rows: `8`
- Production `SystemBase`/legacy declarations: `72`
- Production `ISystem` declarations: `134`
- Production `ISystem` share: `65.0%`

## Follow-Up

- Continue Agent E with the remaining `24` open split/managed-exception candidates.
