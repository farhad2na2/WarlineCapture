# Phase 7 Agent F Handoff - 2026-06-22 - P7-0245 Unit Attached Light Managed Exception

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:
- `P7-0245` - `UnitAttachedLightSystem` - `ManagedPresentationSystemBaseException`

Files changed:
- `Tools/Architecture/generate_systembase_to_isystem_inventory.py`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- None.

Shared components/contracts/asmdefs/tests touched:
- None.

Generated inventory touched:
- Regenerated `Design/Architecture/systembase_to_isystem_inventory.md`.
- JSON sidecar emitted to `/private/tmp/warline-phase7-systembase-inventory.json`.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions confirmed: `1`
- Retired/folded: `0`
- Current inventory: `51` production SystemBase/legacy declarations, `134` production ISystem declarations, `72.4%` production ISystem share.
- Current dispositions: `23` managed presentation exceptions, `19` split candidates, `28` open rows.

Implementation notes:
- Reclassified `UnitAttachedLightSystem` from `SplitThenConvert` to `ManagedPresentationSystemBaseException` in the authoritative inventory generator.
- The system consumes ECS `UnitAttachedLightSetupElement` and `UnitAttachedLightCleanupRequest` data, but owns Unity `Light` GameObject creation, destruction, transform updates, and managed instance tracking.
- No gameplay policy moved into the managed exception. Unit death still emits cleanup data; the light system remains the presentation consumer.
- No runtime source behavior changed in this slice.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed with `0 Warning(s), 0 Error(s)`.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CombatDeathValidationTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-f-unit-attached-light-managed-exception-combat-death.log` passed with marker `[CombatDeathFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log` passed with marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check` passed.

Blockers:
- None.

Deferred validation:
- None.

Coordination notes:
- `P7-0245` is an intentional counted managed exception. Forcing Unity `Light` GameObject ownership into an unmanaged `ISystem` would violate the Phase 7 visual/presentation boundary contract.
- This slice changes open-row accounting but does not change the SystemBase/ISystem numerator or denominator.
