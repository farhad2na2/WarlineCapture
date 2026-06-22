# Phase 7 Agent E Handoff - P7-0153 Runtime City Config Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0153` - `RuntimeCityConfigSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeCityConfigSystem` from a disabled `SystemBase` wrapper into a plain runtime-city config helper.
- Preserved `RuntimeCityConfigSystem.Snapshot` as the runtime-city config contract.
- Preserved snapshot defaults, config projection, prefab-list fallback behavior, current snapshot state, and runtime-city composition callers.
- Updated `RuntimeCityCompositionSystem` to direct-own the helper instead of resolving it through `GetOrCreateSystemManaged`.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityConfigSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result components changed.
- `RuntimeCityConfigSystem.Snapshot` construction, `Default`, `From`, `Current`, and `Apply` call shape stayed unchanged.
- Runtime-city generation and building spawn context continue to consume `RuntimeCityConfigSystem.Snapshot`.

Counts after regeneration:
- Total ECS declarations: `220`.
- Production `SystemBase`/legacy declarations: `86`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `60.9%`.
- Production non-UI rows: `212`.
- Production UI rows: `8`.
- Agent E remaining rows: `38`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `53`.
- Open rows: `64`.
- MonoBehaviour loop baseline: `41` existing loop keys.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-config-helper-fold-city-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This slice preserved managed prefab/config references in the existing runtime-city managed boundary; it did not split remaining active city composition, startup, spawn, or visual ownership.
- `P7-0152 RuntimeCityCompositionSystem` remains the broad active managed composition owner and needs a later split rather than a simple fold.
