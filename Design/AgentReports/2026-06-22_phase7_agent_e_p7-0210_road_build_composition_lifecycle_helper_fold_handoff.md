# Phase 7 Agent E Handoff - P7-0210 RoadBuildCompositionLifecycleSystem Helper Fold

Date: 2026-06-22

## Scope

- Inventory row: `P7-0210 RoadBuildCompositionLifecycleSystem`
- Lane: Agent E road/city/citizen
- Disposition: `SplitThenConvert` helper fold
- Goal: remove the disabled `SystemBase` wrapper while preserving road build composition lifecycle behavior.

## Changes

- Folded `Assets/Game/Scripts/Systems/RoadBuildCompositionLifecycleSystem.cs` from a disabled `SystemBase` into a plain managed helper.
- Kept the existing lifecycle helper API and behavior: `Init`, `BindDependencies`, `Dispose`, and exit-build-mode fallback.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- Updated Agent E, Agent A, and main Phase 7 progress trackers.

## Behavior Preserved

- Road build startup initialization still wires config, camera, runtime root, visual variants, read model, runtime actions, command context, GUI context, and placement outline creation.
- Dependency binding still forwards building interaction, main menu UI, runtime grid blocker, and minimap event wiring.
- Disposal still exits build mode through `EntityManager` when available, falls back to direct session cleanup when unavailable, resets skip-frame state, and delegates road build disposal.
- No new manager/controller/facade and no new updating `MonoBehaviour` loop were introduced.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-road-build-composition-lifecycle-helper-fold-road-build-command.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Editor assembly compile passed.
- Inventory regeneration passed.
- `git diff --check` passed.
- Road build command validation passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- Phase 7 architecture guard passed with `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Inventory Accounting

- Total ECS declarations: `207`
- Production non-UI rows: `199`
- Production UI rows: `8`
- Production `SystemBase`/legacy declarations: `73`
- Production `ISystem` declarations: `134`
- Production `ISystem` share: `64.7%`

## Follow-Up

- Continue Agent E with the remaining `25` open split/managed-exception candidates.
