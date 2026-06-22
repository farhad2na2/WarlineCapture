# Phase 7 Agent B Handoff - P7-0001 GameplaySceneBindingSystem Helper Fold

Date: 2026-06-22

Lane: Agent B - direct ECS data, startup, config, and diagnostics

## Summary

Folded `P7-0001 GameplaySceneBindingSystem` out of ECS into a plain direct-owned helper. The old type was a disabled `SystemBase` with empty `OnUpdate`; `MatchBootstrapSystem` already owned it directly with `new GameplaySceneBindingSystem()`.

This was intentionally not converted to unmanaged `ISystem` because the remaining behavior reads scene-authoring `GridAuthoring.Instances` and `grid.gameObject.scene` to bind runtime grid blocker debug views. That is a composition/scene binding edge, not hot ECS gameplay work.

## Behavior Preserved

- `GameplayFeatureStartupSystem.Initialize(...)` still invokes `sceneBindingSystem.BindRuntimeGridBlockerDebugViews(runtimeGridBlockers)`.
- Runtime grid blocker debug-view binding still iterates existing `GridAuthoring.Instances`.
- Scene validity filtering through `grid.gameObject.scene.IsValid()` stayed unchanged.
- No startup order, runtime grid blocker, or debug-view binding behavior was changed.

## Architecture Notes

- No new manager, controller, facade, or broad replacement `ISystem` shell was introduced.
- No new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, or coroutine loop was introduced.
- No UI Toolkit or Canvas migration work was touched.
- The slice reduces the production ECS denominator by one instead of creating an unmanaged wrapper around scene-authoring work.

## Inventory Accounting

- Production `SystemBase`/legacy declarations: `42`.
- Production `ISystem` declarations: `134`.
- Production `ISystem` share: `76.1%`.
- Authoritative inventory rows: `176 total`, `168 ProductionNonUI`, `8 ProductionUI`.
- Open rows: `17`.
- Managed presentation/config/camera exceptions: `24`.

## Validation

```text
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
```

Result: passed with `0 Warning(s), 0 Error(s)`.

```text
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md
```

Result: regenerated authoritative inventory with `42` production `SystemBase`/legacy declarations, `134` production `ISystem` declarations, and `76.1%` production `ISystem` share.

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Result marker: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

```text
git diff --check
```

Result: passed.

Focused validation note:

- `/private/tmp/warline-phase7-agent-b-gameplay-scene-binding-helper-fold-bootstrap.log` failed on existing UI Toolkit lookup debt: `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs:1809 uses HierarchyFind`.
- `/private/tmp/warline-phase7-agent-b-gameplay-scene-binding-helper-fold-assembly-boundary.log` failed on existing `Game.UI.Runtime` ECS API boundary debt.
- Those failures are outside this slice and were not changed; UI Toolkit/Canvas migration remains out of scope for Phase 7 unless explicitly assigned.
