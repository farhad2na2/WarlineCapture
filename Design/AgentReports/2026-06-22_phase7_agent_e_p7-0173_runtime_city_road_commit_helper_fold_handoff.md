# Phase 7 Agent E Handoff - P7-0173 RuntimeCityRoadCommitSystem Helper Fold

Date: 2026-06-22

## Scope

Folded `P7-0173 RuntimeCityRoadCommitSystem` from a disabled `SystemBase` wrapper into a plain Agent E runtime-city road commit helper.

## Code Changes

- `Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs`
  - Removed `SystemBase` inheritance and empty disabled ECS lifecycle methods.
  - Kept `RuntimeCityRoadCommitState`, `Context`, road-network commit, source-exit road commit, autobahn commit, standalone connector handoff, and occupied-road-cell mutation APIs unchanged.
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
  - Replaced `GetOrCreateSystemManaged<RuntimeCityRoadCommitSystem>()` with direct helper construction.

## Architecture Notes

- This row was inventoried as `DirectConvert`, but the implementation had no active ECS update responsibility. It was a disabled wrapper around a plain state object, so folding it out of ECS reduces the denominator without creating a broad replacement `ISystem`.
- No Unity object presentation, UI, prefab, camera, scene, or visual ownership was introduced.
- Runtime-city composition and generation still call the same state/context APIs.

## Inventory After Regeneration

- Total ECS system declarations: `248`
- Production `SystemBase`/legacy declarations: `114`
- Production `ISystem` declarations: `134`
- Current production `ISystem` share: `54.0%`
- Production non-UI rows: `240`
- Production UI rows: `8`
- Open rows: `92`

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-road-commit-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
git diff --check
```

Results:

- Editor compile passed with `0 Warning(s), 0 Error(s)`.
- Runtime city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-road-commit-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with a nearby runtime-city data/helper row such as `P7-0174 RuntimeCityRoadLayoutSystem`, or inspect the remaining direct-convert road/city rows before choosing the next narrow slice.
