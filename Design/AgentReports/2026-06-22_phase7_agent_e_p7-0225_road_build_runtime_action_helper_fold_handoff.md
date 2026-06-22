# Phase 7 Agent E Handoff - P7-0225 RoadBuildRuntimeActionSystem Helper Fold

Date: 2026-06-22

## Scope

- Inventory row: `P7-0225 RoadBuildRuntimeActionSystem`
- Lane: Agent E road/city/citizen
- Disposition: `SplitThenConvert` helper fold
- Goal: remove the disabled `SystemBase` wrapper while preserving road build runtime action behavior.

## Changes

- Folded `Assets/Game/Scripts/Systems/RoadBuildRuntimeActionSystem.cs` from a disabled `SystemBase` into a plain managed helper.
- Updated `Assets/Game/Scripts/Systems/RoadBuildCompositionSourceSystem.cs` to construct the helper directly instead of resolving it through the default ECS world.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 progress trackers.

## Behavior Preserved

- Runtime action state creation still uses the same `RoadBuildRuntimeActionSystem.State` type.
- Command processing still consumes pending road build commands through the configured `EntityManager` delegate.
- Runtime input update still routes through the interaction context and world camera.
- IMGUI delete prompt routing still delegates to `RoadDeletePromptSystem.OnGui`.
- Road composition source callers still use the same helper and static methods.
- No new manager/controller/facade and no new updating `MonoBehaviour` loop were introduced.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-runtime-action-helper-fold-road-build-command.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor assembly compile passed.
- Inventory regeneration passed.
- `git diff --check` passed.
- Road build command validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- Phase 7 architecture guard passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS declarations: `204`
- Production non-UI rows: `196`
- Production UI rows: `8`
- Production `SystemBase`/legacy declarations: `70`
- Production `ISystem` declarations: `134`
- Production `ISystem` share: `65.7%`

## Follow-Up

- Continue Agent E with the remaining `22` open split/managed-exception candidates.
