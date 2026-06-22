# Phase 7 Agent E Handoff - P7-0186 CitizenDangerSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0186` - `CitizenDangerSystem` - folded from a disabled `SystemBase` wrapper into a plain citizen danger helper.

Files changed:
- `Assets/Game/Scripts/Systems/CitizenDangerSystem.cs`
- `Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Behavior preserved:
- Danger-source registration, unregistration, periodic source-position refresh, safe-building checks, nearest-safe-building selection, and flee-target selection remain in `CitizenDangerSystem`.
- `CitizenPopulationCompositionSystem` now constructs the helper directly instead of resolving a disabled managed ECS wrapper from the default world.
- No citizen movement, visible citizen, resource, schedule, or population contracts changed.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`
- Inventory after regeneration: `209` total ECS declarations, `75` production SystemBase/legacy declarations, `134` production ISystem declarations, `64.1%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Citizen movement focused validation: passed, `/private/tmp/warline-phase7-agent-e-citizen-danger-helper-fold-citizen-movement.log`, marker `[CitizenMovementCommandFocusedValidation] result=Passed tests=2`.
- Visible citizen focused validation: passed, `/private/tmp/warline-phase7-agent-e-citizen-danger-helper-fold-visible-unit.log`, marker `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This remains a managed helper because danger sources are registered as `Transform` references. It is no longer an ECS system declaration and does not add a gameplay update loop.

Coordination notes:
- Agent E ownership only. No Agent C/D/F contract changes were required.
