# Phase 7 Agent B Handoff - P7-0002 MapSurfaceRuntimeBootstrapSystem

Date: `2026-06-21`
Lane: `AgentB`
Inventory row: `P7-0002`
System: `MapSurfaceRuntimeBootstrapSystem`

## Result

- Retired/folded `MapSurfaceRuntimeBootstrapSystem` out of ECS inheritance.
- Preserved the explicit composition helper API used by `MatchBootstrapSystem`.
- Preserved runtime map-surface blob installation, stale subscene replacement, map-surface entity cleanup, and owned blob disposal.
- Preserved managed scene-overlay extraction from `MapSurfaceAuthoring`, `MapBakeGroupAuthoring`, `MeshFilter`, and `Renderer` as an explicit composition boundary rather than an unmanaged ECS system.
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`; the type no longer appears in the ECS system denominator.

## Changed Files

- `Assets/Game/Scripts/Composition/MapSurfaceRuntimeBootstrapSystem.cs`
- `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
- `Assets/Tests/Editor/MapSurfaceRuntimeBootstrapSystemTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- Phase 7 tracker documents

## Validation

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
```

Result: passed with `0 Warning(s), 0 Error(s)`.

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MapSurfaceRuntimeBootstrapSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-b-map-surface-runtime-bootstrap.log
```

Result: passed, marker `[MapSurfaceRuntimeBootstrapValidation] result=Passed tests=2`.

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
```

Result: passed.

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Result: passed.

```bash
git diff --check
```

Result: passed.

## Residual Risk

- This remains a managed composition helper because scene overlay extraction needs Unity authoring objects and renderer bounds. Runtime ECS data writes stay explicit and method-scoped.

## Next Target

- Continue Agent B with `P7-0005 AICombatOrderSystem` false-positive blocker cleanup.
