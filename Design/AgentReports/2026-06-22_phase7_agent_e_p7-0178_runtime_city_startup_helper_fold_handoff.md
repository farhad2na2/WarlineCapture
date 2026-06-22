# Phase 7 Agent E Handoff - P7-0178 RuntimeCityStartupSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0178` - `RuntimeCityStartupSystem` - folded from a disabled `SystemBase` wrapper into a plain runtime-city startup helper.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Behavior preserved:
- Startup readiness evaluation, manual generation evaluation, startup blocker descriptions, initial-spawn wait diagnostics, and required-prefab checks remain in `RuntimeCityStartupState`.
- `RuntimeCityCompositionSystem` now constructs the helper directly instead of resolving a disabled managed ECS wrapper from the default world.
- No runtime city request/result contracts, prefab presentation behavior, or generation sequencing changed.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`
- Inventory after regeneration: `210` total ECS declarations, `76` production SystemBase/legacy declarations, `134` production ISystem declarations, `63.8%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Runtime city focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-startup-helper-fold-city-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This remains a managed helper because startup readiness uses configured `GameObject` prefab collections supplied by runtime-city composition. It is no longer an ECS system declaration and does not add a gameplay update loop.

Coordination notes:
- Agent E ownership only. No Agent C/D/F contract changes were required.
