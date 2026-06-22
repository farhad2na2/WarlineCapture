# Phase 7 Agent F Handoff - P7-0280 SelectionOrderMarkerSystem Helper Fold

Date: 2026-06-22

Lane: Agent F - rendering, presentation, VFX, camera, and visual boundaries

## Summary

Folded `P7-0280 SelectionOrderMarkerSystem` out of ECS into a plain direct-owned helper. The old type was a disabled `SystemBase` shell with empty `OnUpdate`; `SelectionGameplayStartupSystem` already created it directly, so the slice removed inheritance and lifecycle overrides without changing call ownership.

## Behavior Preserved

- Move order markers.
- Attack order markers.
- Scan marker line renderers.
- Attack-target selection marker and ring.
- Attack and board preview marker pools.
- Command result marker consumption.
- Visibility expiry and HUD world-marker toggles.
- Disposal of marker instances, line renderers, materials, and helper-owned pools.

## Architecture Notes

- No new manager, controller, facade, or broad replacement `ISystem` shell was introduced.
- No new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, or coroutine loop was introduced.
- Unity object ownership remains in a narrow direct-owned presentation helper instead of pretending to be unmanaged ECS work.
- No UI Toolkit or Canvas migration work was touched.

## Inventory Accounting

- Production `SystemBase`/legacy declarations: `43`.
- Production `ISystem` declarations: `134`.
- Production `ISystem` share: `75.7%`.
- Authoritative inventory rows: `177 total`, `169 ProductionNonUI`, `8 ProductionUI`.
- Open rows: `18`.
- Managed presentation/config/camera exceptions: `24`.

## Validation

```text
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
```

Result: passed with `0 Warning(s), 0 Error(s)`.

```text
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
```

Result: regenerated authoritative inventory with `43` production `SystemBase`/legacy declarations, `134` production `ISystem` declarations, and `75.7%` production `ISystem` share.

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-selection-order-marker-helper-fold.log
```

Result marker: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

```text
git diff --check
```

Result: passed.
