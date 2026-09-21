# SK-00 baseline — S002 ground slice

Recorded 2026-09-21 from `main` tip `1e0eb9da2120d31e620d1022c6cf2b9eda80c651`
(`docs: add technical handoff for all 120 skirmish scenarios`).

Working tree ownership for this slice: new `Game.Skirmish.Contracts` plus
`Assets/Game/Configs/SkirmishExpansion/` and the new SK-01 compiler/session
types. Existing Operations trees, `SaveDataModel`, `MatchSceneView`, existing
asmdefs, and the shared localization catalog were not edited.

## Prototype mapping (preserved)

| Runtime `ScenarioIndex` | Catalog ID | Meaning |
|---:|---|---|
| 0 | S001 | Desert Base small Base Assault prototype |
| 1 | S025 | City Crossroads small Base Assault prototype |
| 2 | none | Editor-only E0.3 stress probe. Never migrate to S003. |
| 3 | S073 | Industrial Basin prototype compatibility mapping |

Legacy `QuickGameConfig.NormalizeForBaseAssault()` still maps unknown indices to
0 and still preserves 1 and 2. Expanded S002 must not synthesize
`ScenarioIndex=4`. Desert Base map reuse for expanded S002 uses index 0 only as
a scene-load hint, with `SkirmishExpandedSessionComponent.IsLegacy=0`.

## Roster dispositions

`ROSTER_SOURCE_AUDIT.csv` remains the inventory (51 UnitGrid + 23 BuildingDefinition).
This slice binds only the canonical source-config candidates named in
`ROSTER_AND_ECONOMY_IMPLEMENTATION.md`. No abilities were inferred from names.
Capability certification is still Pending (SK-02).

## Compiler / architecture status

This cloud environment does not run Unity Editor. Contracts, configs, compiler,
session systems, and Editor validation suites are source-complete. Focused
validation entry points:

- `Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation`
  marker `[SkirmishExpandedDefinitionTests] result=Passed`
- `Game.Editor.SkirmishSetupCompilerValidation.RunFocusedValidation`
  marker `[SkirmishSetupCompilerValidation] result=Passed`

Legacy 0/1/3 + stress 2 dispatch remains in existing `SkirmishLaunchProjection`,
`SkirmishPresetConfig.Load`, and `QuickGameConfig.NormalizeForBaseAssault`.
`SkirmishRulesSystem` now skips when an expanded (non-legacy) session exists.

## Device / performance

No device probe or X01–X33 rerun was executed in this environment. Do not treat
this report as ARIA/War/device certification.
