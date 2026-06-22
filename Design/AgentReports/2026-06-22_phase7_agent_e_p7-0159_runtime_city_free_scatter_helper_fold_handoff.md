# Phase 7 Agent E Handoff - P7-0159 RuntimeCityFreeScatterDecorationSystem

Branch:
`codex/phase7-agent-e-road-city-citizen`

Rows completed:
- `P7-0159` - `RuntimeCityFreeScatterDecorationSystem` - `Retired/Folded`

Contracts changed:
- `RuntimeCityFreeScatterDecorationSystem` is no longer an ECS `SystemBase` declaration. It is a plain runtime-city helper owned directly by `RuntimeCityCompositionSystem`.
- `RuntimeCityFreeScatterDecorationState`, scatter plot sampling, road/spacing rejection, random prefab selection, spawn-and-reserve request behavior, used-plot tracking, and decoration building callers stayed unchanged.

Counts after inventory regeneration:
- Total ECS declarations: `217`.
- Production SystemBase/legacy declarations: `83`.
- Production ISystem declarations: `134`.
- Production ISystem share: `61.8%`.
- Production non-UI rows: `209`.
- Production UI rows: `8`.
- Agent E remaining rows: `35`.
- DirectConvert rows: `8`.
- SplitThenConvert rows: `50`.
- Open rows: `61`.
- MonoBehaviour loop baseline: `41`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- Inventory regeneration: passed, updated `Design/Architecture/systembase_to_isystem_inventory.md`.
- `git diff --check`: passed.
- Runtime city generation focused validation: passed, `/private/tmp/warline-phase7-agent-e-runtime-city-free-scatter-helper-fold-city-generation.log` (`[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`).
- Phase 7 architecture guard: passed, `/private/tmp/warline-phase7-agent-a-architecture.log` (`[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`).

Risks:
- This was a disabled wrapper fold, not an unmanaged `ISystem` conversion. The ISystem count remains `134`; the percentage increased because the production ECS declaration denominator shrank by one.
