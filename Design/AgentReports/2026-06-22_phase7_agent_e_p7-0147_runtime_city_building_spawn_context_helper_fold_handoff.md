# Phase 7 Agent E Handoff - P7-0147 RuntimeCityBuildingSpawnContextSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

## Summary

Folded `P7-0147 RuntimeCityBuildingSpawnContextSystem` from a disabled `SystemBase` wrapper into a plain direct-owned runtime-city spawn context helper.

Runtime behavior stayed with `RuntimeCityCompositionSystem`. Context creation, fallback context creation, building spawn system package data, and runtime city composition ownership stayed unchanged. `RuntimeCityCompositionSystem` now caches the plain helper directly instead of resolving it from the ECS world.

## Rows Completed

- `P7-0147 RuntimeCityBuildingSpawnContextSystem`

## Contracts Changed

- Request/result components: none.
- Runtime city context data shape: unchanged.
- Managed presentation boundaries: unchanged.

## Counts

- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Managed `SystemBase` exceptions: `0`.
- Retired/folded wrappers: `1`.

## Validation

- Compile and inventory regeneration passed:
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
  - `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`
- Runtime city focused validation passed:
  - `/private/tmp/warline-phase7-agent-e-runtime-city-building-spawn-context-helper-fold-city.log`
  - `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`
- Phase 7 architecture guard passed:
  - `/private/tmp/warline-phase7-agent-a-architecture.log`
  - `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`
- Whitespace validation passed:
  - `git diff --check`

## Inventory Impact

- Total ECS system declarations: `277`.
- Production `SystemBase`/legacy declarations: `144`.
- Production `ISystem` declarations: `133`.
- Current production `ISystem` share: `48.0%`.
- Production non-UI rows: `270`.
- Production UI rows: `7`.
- Agent E remaining open rows: `96`.
- Open rows: `122`.

This slice does not increase the `ISystem` count because it is a retire/fold cleanup of a disabled wrapper, not a conversion into a new runtime processor.

## Risks

- Low. The helper had no updating lifecycle and only packaged runtime-city context structs.
