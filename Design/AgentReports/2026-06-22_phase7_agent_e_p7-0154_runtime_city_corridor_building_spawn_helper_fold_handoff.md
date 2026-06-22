# Phase 7 Agent E Handoff - P7-0154 RuntimeCityCorridorBuildingSpawnSystem Helper Fold

Date: 2026-06-22

## Scope

Folded `P7-0154 RuntimeCityCorridorBuildingSpawnSystem` from a disabled `SystemBase` wrapper into a plain Agent E runtime-city corridor building spawn helper.

## Code Changes

- `Assets/Game/Scripts/Environment/RuntimeCityCorridorBuildingSpawnSystem.cs`
  - Removed `SystemBase` inheritance and empty disabled ECS lifecycle methods.
  - Kept `RuntimeCityCorridorBuildingSpawnState`, corridor roadside plot collection, corridor shop placement, and corridor house placement behavior unchanged.
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
  - Replaced `GetOrCreateSystemManaged<RuntimeCityCorridorBuildingSpawnSystem>()` with direct helper construction.

## Architecture Notes

- This row was inventoried as `DirectConvert`, but the implementation had no active ECS update responsibility. It was a disabled wrapper around a plain state object, so folding it out of ECS reduces the denominator without creating a broad replacement `ISystem`.
- No Unity object presentation, UI, prefab, camera, scene, or visual ownership was introduced.
- Runtime-city generation still consumes the same corridor building spawn state API through composition.

## Inventory After Regeneration

- Total ECS system declarations: `244`
- Production `SystemBase`/legacy declarations: `110`
- Production `ISystem` declarations: `134`
- Current production `ISystem` share: `54.9%`
- Production non-UI rows: `236`
- Production UI rows: `8`
- Open rows: `88`

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-e-runtime-city-corridor-building-spawn-helper-fold-city.log -quit
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log -quit
git diff --check
```

Results:

- Editor compile passed with `0 Warning(s), 0 Error(s)`.
- Runtime city focused validation passed: `/private/tmp/warline-phase7-agent-e-runtime-city-corridor-building-spawn-helper-fold-city.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

## Next Candidate

Continue Agent E with another remaining runtime-city direct-convert helper row such as `P7-0158 RuntimeCityEntryBuildingSpawnSystem`, or inspect the remaining open rows before choosing the next narrow slice.
