# Phase 7 Integration Handoff - P7-0300 CustomGameStartupSystem

Date: 2026-06-22

Slice:
`P7-0300` `CustomGameStartupSystem`

## Summary

- Folded `CustomGameStartupSystem` in `Assets/Game/Scripts/Systems/CustomGameStartupSystem.cs` out of ECS by removing its disabled `SystemBase` wrapper.
- Kept it as a direct-owned startup helper that projects managed startup configs and legacy prefab references into ECS startup buffers.
- Updated `MatchBootstrapSystem` and focused tests to construct the helper with the active `EntityManager`.
- Introduced no manager/controller/facade, no new `MonoBehaviour` update loop, and no UI Toolkit/Canvas work.

## Rationale

The inventory row was labeled `SplitThenConvert`, but the implementation had no ECS update behavior to convert directly:

- `OnCreate` only set `Enabled = false`.
- `OnUpdate` was empty.
- Runtime work was invoked directly through `Initialize` and `InitializeFromLegacyConfigs`.
- The helper reads managed `ScriptableObject` configs and `GameObject` prefab references, then writes ECS startup components/buffers.

Folding the disabled wrapper removes the legacy ECS declaration without forcing managed config/prefab projection into a fake broad `ISystem`.

## Behavior Preserved

- `MatchBootstrapSystem` still invokes `InitializeFromLegacyConfigs` during custom-game startup.
- Legacy initial-spawn configs still create/reuse the startup entity and buffers.
- Initial spawn lifecycle and stale building runtime spawn requests are still reset for the active plan.
- Faction 2 tent lookup-key behavior is unchanged.
- Source-key startup config projection still writes startup state, faction spawn, unit source, initial unit, initial building, and unit source registry buffers.

## Inventory Impact

- Total ECS declarations: `172`.
- Production `SystemBase`/legacy declarations: `35`.
- Production `ISystem` declarations: `137`.
- Current production `ISystem` share: `79.7%`.
- Inventory rows: `172 total`, `164 ProductionNonUI`, `8 ProductionUI`.
- Owner lanes: `AgentB 19`, `AgentC 12`, `AgentD 9`, `AgentE 10`, `AgentF 34`, `Integration 88`.
- Dispositions: `Converted 130`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 8`, `UIOutOfScope 8`.
- Statuses: `Converted 130`, `Deferred 8`, `ManagedException 24`, `Open 10`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CustomGameStartupSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-custom-game-startup-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Custom game startup focused validation passed: `/private/tmp/warline-phase7-integration-custom-game-startup-helper-fold.log` marker `[CustomGameStartupFocusedValidation] result=Passed tests=4`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration split-first rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
