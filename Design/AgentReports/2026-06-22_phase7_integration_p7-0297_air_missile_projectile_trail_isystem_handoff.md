# Phase 7 Integration Handoff - P7-0297 AirMissileProjectileTrailSystem

Date: 2026-06-22

Slice:
`P7-0297` `AirMissileProjectileTrailSystem`

## Summary

- Converted `AirMissileProjectileTrailSystem` in `Assets/Game/Scripts/Systems/AirMissileLauncherSystems.cs` from `SystemBase` to unmanaged `ISystem`.
- Kept the slice direct and behavior-preserving: no manager/controller/facade, no new `MonoBehaviour` update loop, no UI Toolkit/Canvas work.
- Regenerated the authoritative inventory after the conversion.

## Behavior Preserved

- `[UpdateAfter(typeof(AirMissileHomingProjectileSystem))]` and `[UpdateBefore(typeof(AirMissileImpactSystem))]` ordering stayed unchanged.
- `RequireForUpdate<AirMissileProjectileTrailComponent>()` stayed unchanged via `SystemState`.
- The query still reads `AirMissileProjectileComponent` and `LocalTransform`, filters with `AirMissileProjectileTrailComponent`, and uses entity access.
- `MissileTrailVfxView.Sync(entity, position, direction)` still receives the same projectile entity, transform position, and normalized velocity/fallback direction.

## Inventory Impact

- Production `SystemBase`/legacy declarations: `41`.
- Production `ISystem` declarations: `135`.
- Current production `ISystem` share: `76.7%`.
- Inventory rows: `176 total`, `168 ProductionNonUI`, `8 ProductionUI`.
- Dispositions: `Converted 128`, `DirectConvert 5`, `ManagedPresentationSystemBaseException 24`, `RetireFold 2`, `SplitThenConvert 9`, `UIOutOfScope 8`.
- Statuses: `Converted 128`, `Deferred 8`, `ManagedException 24`, `Open 16`.

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod AirMissileLauncherValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-air-missile-projectile-trail-isystem.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
git diff --check
```

Results:

- Compile passed with `0 Warning(s), 0 Error(s)`.
- Air missile launcher focused validation passed: `/private/tmp/warline-phase7-integration-air-missile-projectile-trail-isystem.log` marker `[AirMissileLauncherValidation] PASS`.
- Phase 7 architecture guard passed: `/private/tmp/warline-phase7-agent-a-architecture.log` marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

## Follow-Up

- Continue remaining Integration rows one slice at a time.
- `P7-0003` and `P7-0019` remain held until the Phase 7 guardrail/model explicitly supports those managed-reference boundary helper classifications.
