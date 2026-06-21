# Phase 7 Agent F Handoff - P7-0281 Selection Screen Marker

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:

- `P7-0281` - `SelectionScreenMarkerSystem` - `Retired/Folded`

Files changed:

- `Assets/Game/Scripts/Systems/SelectionScreenMarkerSystem.cs`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Visual split:

- Request/result data: existing screen-space move, attack, and hide marker events remain in `SelectionScreenMarkerSystem`.
- Managed presentation exceptions: none added.
- Folded helper: `SelectionScreenMarkerSystem` is now a plain direct-owned request relay. `SelectionGameplayStartupSystem` instantiates it directly instead of registering a disabled `SystemBase` in the ECS World.

Counts:

- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed presentation SystemBase exceptions: `0`
- Retired/folded: `1`

Inventory impact:

- Total ECS declarations: `360`
- Production SystemBase/legacy declarations: `227`
- Production ISystem declarations: `133`
- Production non-UI rows: `353`
- Agent F rows: `50`

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed, `0 Warning(s), 0 Error(s)`.
- `SelectionOrderMarkerSystemTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-f-selection-screen-marker-order-marker.log`, marker `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
- `SelectionCommandRequestResultContractTests.RunBatchValidation`: passed, `/private/tmp/warline-phase7-agent-f-selection-screen-marker-request-result.log`, marker `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
- `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation`: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Risks:

- No visual presentation code changed. Risk is limited to startup ownership: marker request subscribers now use a directly owned relay instead of a disabled ECS-managed object.
