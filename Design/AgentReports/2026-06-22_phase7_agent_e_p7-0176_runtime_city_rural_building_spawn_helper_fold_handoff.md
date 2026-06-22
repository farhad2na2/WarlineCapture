# Phase 7 Agent E Handoff - P7-0176 RuntimeCityRuralBuildingSpawnSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0176` - `RuntimeCityRuralBuildingSpawnSystem` - `Retired/Folded`

Contracts changed:
- `RuntimeCityRuralBuildingSpawnSystem` is no longer an ECS `SystemBase` declaration. It is a plain runtime-city helper owned directly by `RuntimeCityCompositionSystem`.
- `RuntimeCityRuralBuildingSpawnState`, rural plot sampling, distance and road rejection, prefab selection, spawn-and-reserve behavior, used-plot tracking, placement anchors, and runtime-city generation callers stayed unchanged.

Counts after inventory regeneration:
- Total ECS declarations: `213`.
- Production SystemBase/legacy declarations: `79`.
- Production ISystem declarations: `134`.
- Production ISystem share: `62.9%`.
- Production non-UI rows: `205`.
- Production UI rows: `8`.
- Agent E remaining rows: `31`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `46`.
- Open rows: `57`.
- MonoBehaviour loop baseline: `41`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- Inventory regeneration: passed, updated `Design/Architecture/systembase_to_isystem_inventory.md`.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-rural-building-spawn-helper-fold-city-generation.log` (`[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`).
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log` (`[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`).

Risks:
- This was a disabled wrapper fold, not an unmanaged `ISystem` conversion. The ISystem count remains `134`; the percentage increased because the production ECS declaration denominator shrank by one.
