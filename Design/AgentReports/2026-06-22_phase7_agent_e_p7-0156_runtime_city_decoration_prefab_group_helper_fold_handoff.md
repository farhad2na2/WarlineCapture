# Phase 7 Agent E Handoff - P7-0156 Runtime City Decoration Prefab Group Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0156` - `RuntimeCityDecorationPrefabGroupSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeCityDecorationPrefabGroupSystem` from a disabled `SystemBase` wrapper into a plain runtime-city decoration prefab grouping helper.
- Preserved `RuntimeCityDecorationPrefabGroupState` as the owner of decoration prefab classification.
- Preserved cloth-cover, archway, and free-scatter grouping rules, null-prefab handling, group contract, and decoration building callers.
- Updated `RuntimeCityCompositionSystem` to direct-own the helper instead of resolving it through `GetOrCreateSystemManaged`.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityDecorationPrefabGroupSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result components changed.
- `RuntimeCityDecorationPrefabGroupSystem.Groups` and `RuntimeCityDecorationPrefabGroupState.CreateGroups` call shape stayed unchanged.
- Runtime-city decoration building placement continues to consume the same grouping helper.

Counts after regeneration:
- Total ECS declarations: `218`.
- Production `SystemBase`/legacy declarations: `84`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `61.5%`.
- Production non-UI rows: `210`.
- Production UI rows: `8`.
- Agent E remaining rows: `36`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `51`.
- Open rows: `62`.
- MonoBehaviour loop baseline: `41` existing loop keys.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-decoration-prefab-group-helper-fold-city-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This slice preserved managed prefab references in the existing runtime-city managed boundary; it did not split remaining active city composition, startup, spawn, or visual ownership.
- `P7-0152 RuntimeCityCompositionSystem` remains the broad active managed composition owner and needs a later split rather than a simple fold.
