# Phase 7 Agent F Handoff - Combat VFX Boundary

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:

- `P7-0283` - `CombatGameObjectVfxPlaybackSystem` - `ManagedException`
- `P7-0284` - `UnitAttackVfxRequestSystem` - `ManagedException`
- `P7-0338` - `UnitAttackSystem` - `Converted`

Files changed:

- `Assets/Game/Scripts/Systems/UnitAttackSystem.cs`
- `Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs`
- `Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs.meta`
- `Tools/Architecture/generate_systembase_to_isystem_inventory.py`
- `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Visual split:

- Request/result data:
  - `UnitAttackSystem` remains an unmanaged `ISystem` and emits `UnitAttackVfxRequest` entities for normal muzzle/impact playback.
  - Ground and air missile systems continue to emit `CombatGameObjectVfxRequest` entities through `CombatGameObjectVfxRequests.Enqueue`.
- Managed presentation exceptions:
  - `UnitAttackVfxRequestSystem` consumes `UnitAttackVfxRequest` entities and unwraps authored muzzle/impact `UnityObjectRef<GameObject>` values only at the playback boundary.
  - `CombatGameObjectVfxPlaybackSystem` consumes `CombatGameObjectVfxRequest` entities and performs authored pooled GameObject VFX playback.
- Converted gameplay:
  - `UnitAttackSystem` no longer shares a file with the managed VFX playback classes and is inventoried as `Converted` with no GameObject blocker.

Counts for this slice:

- Converted to `ISystem`: `1`
- Split passive/managed boundaries: `0`
- Managed presentation `SystemBase` exceptions: `2`
- Retired/folded: `0`

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `git diff --check`: passed.
- `/private/tmp/warline-phase7-agent-f-vfx-boundary-unit-combat.log`: `[UnitCombatFocusedEditModeValidation] result=Passed tests=1`.
- `/private/tmp/warline-phase7-agent-f-vfx-boundary-ground-missile-visual.log`: `[GroundMissileVisualValidation] result=Passed tests=1`.
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- Visual QA: not captured; presentation behavior was preserved by moving code without changing playback paths, then validated by focused combat and missile visual smoke tests.

Guardrail note:

- `P7-0283` and `P7-0284` contain combat vocabulary because they consume VFX request entities named for combat systems, but they do not own attack validation, damage, command execution, simulation, or gameplay ECS mutation policy. `NonUiSystemBaseMigrationArchitectureTests` now has a narrow reviewed-row allowlist for these two ids only.

Risks:

- These rows remain managed because authored pooled GameObject VFX playback requires `GameObject` and `UnityEngine` APIs. Converting them to `ISystem` would violate the architecture contract and risk changing visual quality.
