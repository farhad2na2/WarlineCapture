# Phase 7 Agent E Handoff - P7-0169 RuntimeCityPrefabSelectionSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0169` - `RuntimeCityPrefabSelectionSystem` - folded from a disabled `SystemBase` wrapper into a plain runtime-city prefab selection helper.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityPrefabSelectionSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Behavior preserved:
- Prefab membership checks, random prefab selection, generic shuffling, major/minor footprint access, and renderer-based footprint caching remain in `RuntimeCityPrefabSelectionState`.
- `RuntimeCityCompositionSystem` now constructs the helper directly instead of resolving a disabled managed ECS wrapper from the default world.
- No request/result contracts, prefab presentation behavior, or runtime city generation sequencing changed.

Counts:
- Converted to ISystem: `0`
- Split passive/managed boundaries: `0`
- Managed SystemBase exceptions: `0`
- Retired/folded: `1`
- Inventory after regeneration: `211` total ECS declarations, `77` production SystemBase/legacy declarations, `134` production ISystem declarations, `63.5%` production ISystem share.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Runtime city focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-prefab-selection-helper-fold-city-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This remains a managed Unity-object helper because it reads `GameObject` renderers to estimate prefab footprints. It is no longer an ECS system declaration and does not add a gameplay update loop.

Coordination notes:
- Agent E ownership only. No Agent C/D/F contract changes were required.
