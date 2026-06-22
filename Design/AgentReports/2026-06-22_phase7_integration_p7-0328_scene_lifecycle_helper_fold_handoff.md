# Phase 7 Integration Handoff - P7-0328 SceneLifecycleSystem

Date: 2026-06-22

Slice:
`P7-0328` `SceneLifecycleSystem`

## Summary

- Folded `SceneLifecycleSystem` in `Assets/Game/Scripts/Systems/SceneLifecycleSystem.cs` out of ECS by removing its disabled `SystemBase` wrapper.
- Kept the existing direct-owned managed scene lifecycle helper API used by menu/bootstrap and launch composition.
- Added `Assets/Game/Scripts/Editor/SceneLifecycleValidationRunner.cs` to validate lifecycle boundary creation and duplicate request behavior without triggering scene load/unload.

## Rationale

The inventory row was labeled `DirectConvert`, but the implementation was not a safe unmanaged ECS processor:

- `OnCreate` only set `Enabled = false`.
- `OnUpdate` was empty.
- Runtime work was invoked directly through `QueueLoadMatch`, `QueueUnloadMatch`, `TryEnqueue`, `Update`, and `EnsureLifecycleEntity`.
- The helper owns managed `SceneManager`/`AsyncOperation` state, which belongs in a managed direct-owned boundary, not an unmanaged `ISystem`.

Folding the disabled wrapper removes the legacy ECS declaration without creating a fake broad `ISystem` shell.

## Behavior Preserved

- `MenuBootstrapSystem` and `MatchLaunchCommand` still own and invoke the helper directly.
- `QueueLoadMatch` still queues an additive Match scene load request with activation enabled.
- `QueueUnloadMatch` still queues Match scene unload requests when appropriate.
- `EnsureLifecycleEntity` still creates/reuses the scene lifecycle boundary entity and request/result buffers.
- Duplicate load requests and ignored unload requests remain idempotent in the unloaded/no-busy state.
- Managed scene load/unload ownership remains in the helper through `SceneManager` and `AsyncOperation`.

## Inventory Impact

- Total ECS declarations: `173`.
- Production `SystemBase`/legacy declarations: `37`.
- Production `ISystem` declarations: `136`.
- Current production `ISystem` share: `78.6%`.
- Inventory rows: `173 total`, `165 ProductionNonUI`, `8 ProductionUI`.
- Owner lanes: `AgentB 19`, `AgentC 12`, `AgentD 9`, `AgentE 10`, `AgentF 34`, `Integration 89`.
- Dispositions: `Converted 129`, `DirectConvert 1`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 9`, `UIOutOfScope 8`.
- Statuses: `Converted 129`, `Deferred 8`, `ManagedException 24`, `Open 12`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SceneLifecycleValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-scene-lifecycle-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Scene lifecycle focused validation passed: `/private/tmp/warline-phase7-integration-scene-lifecycle-helper-fold.log` marker `[SceneLifecycleValidation] result=Passed tests=1`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
