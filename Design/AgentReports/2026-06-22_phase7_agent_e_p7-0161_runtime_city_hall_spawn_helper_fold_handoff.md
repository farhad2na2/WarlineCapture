# Phase 7 Agent E Handoff - P7-0161 RuntimeCityHallSpawnSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0161` - `RuntimeCityHallSpawnSystem` - `Retired/Folded`

Contracts changed:
- `RuntimeCityHallSpawnSystem` is no longer an ECS `SystemBase` declaration. It is a plain runtime-city helper owned directly by `RuntimeCityCompositionSystem`.
- `RuntimeCityHallSpawnState`, civic-center placement, hall-prefab shuffling, landmark offset checks, spawn-and-reserve behavior, diagnostic failure logging, and runtime-city generation callers stayed unchanged.

Counts after inventory regeneration:
- Total ECS declarations: `216`.
- Production SystemBase/legacy declarations: `82`.
- Production ISystem declarations: `134`.
- Production ISystem share: `62.0%`.
- Production non-UI rows: `208`.
- Production UI rows: `8`.
- Agent E remaining rows: `34`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `49`.
- Open rows: `60`.
- MonoBehaviour loop baseline: `41`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- Inventory regeneration: passed, updated `Design/Architecture/systembase_to_isystem_inventory.md`.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-hall-spawn-helper-fold-city-generation.log` (`[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`).
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log` (`[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`).

Risks:
- This was a disabled wrapper fold, not an unmanaged `ISystem` conversion. The ISystem count remains `134`; the percentage increased because the production ECS declaration denominator shrank by one.
