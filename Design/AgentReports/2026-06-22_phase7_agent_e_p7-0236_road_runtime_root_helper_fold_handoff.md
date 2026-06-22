# Phase 7 Agent E Handoff - P7-0236 RoadRuntimeRootSystem Helper Fold

Date: 2026-06-22

## Scope

- Inventory row: `P7-0236 RoadRuntimeRootSystem`
- Lane: Agent E road/city/citizen
- Disposition: `SplitThenConvert` helper fold
- Goal: remove the disabled `SystemBase` wrapper while preserving runtime road/building root composition behavior.

## Changes

- Folded `Assets/Game/Scripts/Systems/RoadRuntimeRootSystem.cs` from a disabled `SystemBase` into a plain managed helper.
- Updated `Assets/Game/Scripts/Systems/RoadBuildCompositionSourceSystem.cs` to construct the helper directly instead of resolving it through the default ECS world.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 progress trackers.

## Behavior Preserved

- Runtime root creation and disposal still use the existing `GameObject`, `Transform`, and `UnityEngine.Object.Destroy` boundary.
- Child root names remain unchanged: `RuntimeRoads`, `RuntimeAutobahns`, `RuntimeAutobahnConnectors`, `RuntimeDebugStraightRoads`, and `RuntimeBuildings`.
- Road build composition callers continue to request the same runtime root helper API.
- No new manager/controller/facade and no new updating `MonoBehaviour` loop were introduced.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-runtime-root-helper-fold-road-build-command.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor assembly compile passed.
- Inventory regeneration passed.
- `git diff --check` passed.
- Road build command validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- Phase 7 architecture guard passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS declarations: `208`
- Production non-UI rows: `200`
- Production UI rows: `8`
- Production `SystemBase`/legacy declarations: `74`
- Production `ISystem` declarations: `134`
- Production `ISystem` share: `64.4%`

## Follow-Up

- Continue Agent E with the remaining `26` open split/managed-exception candidates.
