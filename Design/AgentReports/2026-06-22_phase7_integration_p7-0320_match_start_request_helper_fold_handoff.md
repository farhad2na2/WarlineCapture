# Phase 7 Integration Handoff - P7-0320 MatchStartRequestSystem

Date: 2026-06-22

Slice:
`P7-0320` `MatchStartRequestSystem`

## Summary

- Folded `MatchStartRequestSystem` in `Assets/Game/Scripts/Systems/MatchStartRequestSystem.cs` out of ECS by removing its disabled `SystemBase` wrapper.
- Kept the existing direct-owned helper API used by `MatchLaunchCommand`.
- Added `Assets/Game/Scripts/Editor/MatchStartRequestValidationRunner.cs` to validate queue helper behavior.

## Rationale

The inventory row was labeled `DirectConvert`, but the implementation was not a recurring ECS processor:

- `OnCreate` only set `Enabled = false`.
- `OnUpdate` was empty.
- The only behavior was the public `QueueStartAfterMatchLoaded(EntityManager)` helper called directly by composition code.
- The helper carries cached `World`/`Entity` state for direct-call reuse.

Creating an `ISystem` shell for this would improve the inheritance count in the wrong way. The architecture contract prefers retiring disabled helper wrappers over introducing fake ECS processors.

## Behavior Preserved

- `MatchLaunchCommand` still calls `QueueStartAfterMatchLoaded(EntityManager)`.
- The helper still creates or reuses the `MatchStartBoundaryComponent` entity.
- The helper still ensures `MatchStartQueueComponent`, `MatchStartRequestElement`, `MatchStartResultElement`, and `MatchStartProgressComponent`.
- The helper still queues a single `RequireMatchLoaded` request and remains idempotent while a request is already pending.

## Inventory Impact

- Total ECS declarations: `175`.
- Production `SystemBase`/legacy declarations: `39`.
- Production `ISystem` declarations: `136`.
- Current production `ISystem` share: `77.7%`.
- Inventory rows: `175 total`, `167 ProductionNonUI`, `8 ProductionUI`.
- Owner lanes: `AgentB 19`, `AgentC 12`, `AgentD 9`, `AgentE 10`, `AgentF 34`, `Integration 91`.
- Dispositions: `Converted 129`, `DirectConvert 3`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 9`, `UIOutOfScope 8`.
- Statuses: `Converted 129`, `Deferred 8`, `ManagedException 24`, `Open 14`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchStartRequestValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-match-start-request-helper-fold.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Match start request focused validation passed: `/private/tmp/warline-phase7-integration-match-start-request-helper-fold.log` marker `[MatchStartRequestValidation] result=Passed tests=1`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
