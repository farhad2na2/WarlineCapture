# Phase 7 Agent E Handoff - P7-0151 Runtime City Cloth Cover Helper Fold

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0151` - `RuntimeCityClothCoverSpawnSystem` - `Retired/Folded`

Scope:
- Folded `RuntimeCityClothCoverSpawnSystem` from a disabled `SystemBase` wrapper into a plain runtime-city cloth-cover spawn helper.
- Preserved `RuntimeCityClothCoverSpawnState` as the owner of the cloth-cover placement algorithm.
- Preserved prefab shuffling, adjacent-origin search, spawn-and-reserve request behavior, reserved-footprint use, and decoration building callers.
- Updated `RuntimeCityCompositionSystem` to direct-own the helper instead of resolving it through `GetOrCreateSystemManaged`.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityClothCoverSpawnSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Contracts changed:
- No request/result components changed.
- `RuntimeCityClothCoverSpawnState.PlaceClothCoverBuildings` call shape stayed unchanged.
- Runtime-city decoration building callers continue to receive the same state helper through `RuntimeCityBuildingSpawnContextSystem.Context`.

Counts after regeneration:
- Total ECS declarations: `221`.
- Production `SystemBase`/legacy declarations: `87`.
- Production `ISystem` declarations: `134`.
- Current production `ISystem` share: `60.6%`.
- Production non-UI rows: `213`.
- Production UI rows: `8`.
- Agent E remaining rows: `39`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `54`.
- Open rows: `65`.
- MonoBehaviour loop baseline: `41` existing loop keys.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md`: passed.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-cloth-cover-helper-fold-city-generation.log`, marker `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Risks:
- This slice preserved managed prefab placement in the existing runtime-city managed boundary; it did not split remaining active city composition or visual/prefab ownership.
- Remaining Agent E open rows still include managed runtime-city, road, citizen, and environment boundaries that need lane-scoped follow-up.
