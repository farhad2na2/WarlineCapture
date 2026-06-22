# Phase 7 Agent E Handoff - P7-0148 Runtime City Bulk Building Spawn Routine Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0148` - `RuntimeCityBulkBuildingSpawnRoutineSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeCityBulkBuildingSpawnRoutineSystem` from a disabled `SystemBase` wrapper into a plain runtime-city bulk building spawn routine helper.
- Preserved nested contracts: `GenerationRandomState`, `PlaceHouseYardWallsAction`, and `PlaceCityDecorationBuildingsAction`.
- Preserved spawn coroutine sequencing, entry/roadside/rural/decorative placement calls, yard-wall/decor callback delegates, and runtime-city generation callers.
- Updated `RuntimeCityCompositionSystem` to direct-own the helper instead of resolving it through `GetOrCreateSystemManaged`.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityBulkBuildingSpawnRoutineSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result components changed.
- Public helper API, nested delegate types, generation random state, and runtime-city generation call shape stayed unchanged.

Counts after regeneration:
- Total ECS declarations: `222`.
- Production `SystemBase`/legacy declarations: `88`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `60.4%`.
- Production non-UI rows: `214`.
- Production UI rows: `8`.
- Agent E remaining rows: `40`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `55`.
- Open rows: `66`.
- MonoBehaviour loop baseline: `41` existing loop keys.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-bulk-building-spawn-routine-helper-fold-city-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This slice preserved the existing coroutine helper behavior and Unity-object placement boundaries; it did not split the remaining active runtime-city composition or visual/prefab ownership.
- Remaining Agent E open rows still include managed runtime-city, road, citizen, and environment boundaries that need lane-scoped follow-up.
