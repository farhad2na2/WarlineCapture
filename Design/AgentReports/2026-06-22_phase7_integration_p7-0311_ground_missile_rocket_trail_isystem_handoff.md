# Phase 7 Integration Handoff - P7-0311 GroundMissileRocketTrailSystem

Date: 2026-06-22

Slice:
`P7-0311` `GroundMissileRocketTrailSystem`

## Summary

- Converted `GroundMissileRocketTrailSystem` in `Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs` from `SystemBase` to unmanaged `ISystem`.
- Kept the conversion behavior-preserving and lane-scoped.
- Introduced no manager/controller/facade, no new `MonoBehaviour` update loop, and no UI Toolkit/Canvas work.

## Behavior Preserved

- `[UpdateAfter(typeof(GroundMissileFlyingRocketVisualSystem))]` and `[UpdateBefore(typeof(GroundMissileProjectileFlightSystem))]` ordering stayed unchanged.
- `RequireForUpdate<GroundMissileFlyingRocketVisualComponent>()` stayed unchanged via `SystemState`.
- The query still reads `LocalTransform`, filters with `GroundMissileFlyingRocketVisualComponent`, and uses entity access.
- `MissileTrailVfxView.Sync(entity, position, direction)` still receives the same rocket entity, transform position, and transform-forward direction.

## Inventory Impact

- Production `SystemBase`/legacy declarations: `40`.
- Production `ISystem` declarations: `136`.
- Current production `ISystem` share: `77.3%`.
- Inventory rows: `176 total`, `168 ProductionNonUI`, `8 ProductionUI`.
- Dispositions: `Converted 129`, `DirectConvert 4`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 9`, `UIOutOfScope 8`.
- Statuses: `Converted 129`, `Deferred 8`, `ManagedException 24`, `Open 15`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod GroundMissileLauncherRuntimeTests.RunMissileVisualValidation -logFile /private/tmp/warline-phase7-integration-ground-missile-rocket-trail-isystem.log
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Ground missile visual focused validation passed: `/private/tmp/warline-phase7-integration-ground-missile-rocket-trail-isystem.log` marker `[GroundMissileVisualValidation] result=Passed tests=1`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
