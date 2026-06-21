# Phase 7 Agent F Handoff - P7-0249/P7-0251 Camera Snapshot Boundary

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:

- `P7-0249` - `UnitModelSpawnSystem` - `Converted`
- `P7-0251` - `UnitRenderBudgetSystem` - `Converted`

Files changed:

- `Assets/Game/Scripts/Components/RuntimeCameraReferenceComponent.cs`
- `Assets/Game/Scripts/Rendering/Systems/RuntimeCameraReferenceSystem.cs`
- `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs`
- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetCameraMotionSystem.cs`
- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetDistanceSystem.cs`
- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs`
- `Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs`
- `Assets/Tests/Editor/UnitRenderBudgetPerformanceValidation.cs`
- `Tools/Architecture/generate_systembase_to_isystem_inventory.py`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Visual split:

- Request/result data: `RuntimeCameraSnapshotComponent` now carries value-type camera position, rotation, world-to-camera, projection, and view-projection data.
- Managed presentation exception: `RuntimeCameraReferenceSystem` remains the managed `Camera` owner and publishes the value snapshot.
- Converted consumers: `UnitModelSpawnSystem`, `UnitRenderBudgetSystem`, `UnitRenderBudgetCameraMotion`, and `UnitRenderBudgetDistance` now use the camera snapshot instead of direct `Camera` reads.

Counts:

- Converted to ISystem: `2`
- Split passive/managed boundaries: `1`
- Managed presentation SystemBase exceptions: `0` new
- Retired/folded: `0`

Inventory impact:

- Total ECS declarations: `359`
- Production SystemBase/legacy declarations: `226`
- Production ISystem declarations: `133`
- Production non-UI rows: `352`
- Agent F rows: `49`
- Dispositions: `Converted 125`, `SplitThenConvert 118`

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `RuntimeCameraReferenceSystemTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-f-camera-snapshot-reference.log`, marker `[RuntimeCameraReferenceFocusedValidation] result=Passed tests=3`.
- `UnitRenderBudgetSystemTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-f-camera-snapshot-render-budget.log`, marker `[UnitRenderBudgetFocusedValidation] result=Passed tests=31`.
- `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Risks:

- The snapshot is refreshed by the managed camera boundary when render systems request it, so the hot `ISystem` rows avoid direct managed `Camera` access while still using current camera matrices.
