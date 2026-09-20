# E0.3 — reproducible stress scene/preset

Status: authored and Editor-validated as additive Skirmish content. Play-mode entity counts were **not** measured in the cloud agent VM (no Unity Editor play mode). Do not treat requested recipe counts as spawned/alive/destroyed.

Host: Desert Base geometry, real unit prefabs, existing Skirmish AI / projectiles / pathing. Player-facing Base Assault and City Crossroads are unchanged.

## How to launch

Deterministic inputs:

| Input | Value |
|---|---|
| Scenario index | `2` (`SkirmishPresetConfig.StressScaleProbeScenarioIndex`) |
| Seed | `104729` (`SkirmishStressRecipe.FixedSeed`) |
| Default scale | `100` combat units across both sides, plus 6 aircraft in sequence mode |
| Default layout | spread-out armies on the existing west/east Desert Base lots |
| Player setup UI | still only Desert Base / City Crossroads; stress is Editor/QA only |

### Editor menus

1. Optional rebuild after pulling: `Tools/Warline/Skirmish/Rebuild E0.3 Stress Preset`
2. Launch the default sequence: `Tools/Warline/Skirmish/Launch E0.3 Stress Sequence`
3. Or from the C# console / a one-off executeMethod:

```csharp
Game.Editor.SkirmishStressEditorProbe.Launch(
    Game.Configs.SkirmishStressScale.P100,
    Game.Configs.SkirmishStressPhase.Warmup,
    Game.Configs.SkirmishStressLayout.Spread,
    sequence: true);
```

Single-phase example (dense combat only, concentrated visibility, 200-entity probe):

```csharp
Game.Editor.SkirmishStressEditorProbe.Launch(
    Game.Configs.SkirmishStressScale.P200,
    Game.Configs.SkirmishStressPhase.DenseCombat,
    Game.Configs.SkirmishStressLayout.Concentrated,
    sequence: false);
```

The probe opens `Assets/Game/Scenes/Menu.unity`, queues Scenario 2 with seed `104729`, and enters the match route. It does not click the two player scenario buttons.

### Focused Edit-mode check (no play mode)

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-e03-stress.log -- \
  -quit -executeMethod SkirmishStressPresetTests.RunFocusedValidation
```

Required marker: `[SkirmishStressPreset] result=Passed`. The same batch also prints `[SkirmishStressValidation] result=Passed`.

### Play-mode census capture

After the probe finishes, copy:

- Console lines starting with `[SkirmishStress]`
- `$TMPDIR/warline-e03-stress/E0_3_STRESS_CENSUS.md` (or `WARLINE_SKIRMISH_STRESS_REPORT_DIR`)

Those files contain **measured** spawned/alive/destroyed columns. Paste the table into the “Actual counts” section below. Do not copy the requested table into that section.

## Phase definitions

Each phase can run alone (`sequence: false`) or as the default sequence (`warmup → idle → mass move → dense combat → air/transport → destruction`).

| Phase | What it does | Force composition |
|---|---|---|
| warmup | Wait until initial spawn finishes. No orders. | Ground + support. Sequence also pre-spawns air. |
| idle | `Hold` on combat units. | Same as warmup. |
| mass move | Immediate Move toward the map midline / opposite approach. | Ground + support. |
| dense combat | Attack-move into contact so rifles, cars, APCs, tanks and traces run. | Ground + support. Prefer concentrated layout for all-units-visible. |
| air/transport | Attack-move attack/transport helicopters. Solo launch uses a rifle/APC escort. | Air + escort (solo) or the sequence union. |
| destruction | Keep attack-move and record destroyed/lost entities. | Ground + support. |

Layouts:

- **Spread:** existing Desert Base cells `(835,610)` / `(1210,580)` with local offsets.
- **Concentrated:** same bases/buildings; combat offsets shift both armies toward `(1022,595)` so they share one camera.

## Requested counts (authoring only)

These are recipe inputs used to fill spawn buffers. They are **not** measured entity counts.

Ground combat per side (solo ground phases):

| Scale | Rifle | Ghillie | Car | APC Fast | APC Heavy | Tank | Combat both sides |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 50 | 16 | 4 | 2 | 2 | 0 | 1 | 50 |
| 100 | 30 | 8 | 4 | 4 | 2 | 2 | 100 |
| 200 | 60 | 16 | 8 | 8 | 4 | 4 | 200 |
| 350 | 110 | 28 | 14 | 12 | 6 | 5 | 350 |
| 500 | 160 | 40 | 20 | 16 | 8 | 6 | 500 |

Air (sequence, or the air/transport phase) per side: P50 `1/0/1`, P100 `1/1/1`, P200 `2/1/2`, P350 `3/2/2`, P500 `4/2/3` (attack / small attack / transport). Support is always 1 tanker + 2 tray per side. Buildings are the five Base Assault structures plus one helipad per side.

Default sequence at P100 therefore **requests** 106 combat (100 ground + 6 air), 6 support, 12 buildings.

Real prefabs: `Unit_Chr_Soldier_Male_02_Alt_04`, `Unit_Chr_Ghillie_Male_01`, `Unit_Veh_Light_Armored_Car`, `Unit_Veh_APC_Fast`, `Unit_Veh_APC_Heavy`, `Unit_Veh_Tank_USA`, `Unit_Veh_Helicopter_Attack`, `Unit_Veh_Helicopter_Attack_Small`, `Unit_Veh_Helicopter_Transport`, `Unit_Veh_Truck_Tanker`, `Unit_Veh_Truck_Tray`. No stub cubes.

## Actual counts

**Not measured in this delivery.** The cloud agent could not enter Unity play mode or screenshot the Editor.

Paste the probe census table here after a Game PM / QA Editor run:

| Phase | Requested combat | Spawned combat | Alive combat | Destroyed combat | Missing spawn | Spawned air | Alive air | Spawned support | Spawned buildings | Live traces | Elapsed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| warmup | — | — | — | — | — | — | — | — | — | — | — |
| idle | — | — | — | — | — | — | — | — | — | — | — |
| mass move | — | — | — | — | — | — | — | — | — | — | — |
| dense combat | — | — | — | — | — | — | — | — | — | — | — |
| air/transport | — | — | — | — | — | — | — | — | — | — | — |
| destruction | — | — | — | — | — | — | — | — | — | — | — |

`Missing spawn` is `requestedCombat - spawnedCombat`. A positive number means cells/prefab resolution failed. Destroyed uses remaining `UnitHealth` plus `SkirmishMatchState` loss counters when entities are removed.

Repeat at least once with `SkirmishStressLayout.Concentrated` so both spread-out and concentrated-visibility cases have measured rows.

## Editor self-test / Game PM Unity gate

1. Confirm `ProjectSettings/ProjectVersion.txt` matches the machine Editor.
2. Idle Editor, no dirty scenes. Unity Hub signed in.
3. `Tools/Warline/Skirmish/Rebuild E0.3 Stress Preset` once, then save assets.
4. Run `SkirmishStressPresetTests.RunFocusedValidation` through `invoke_unity_macos.sh`. Expect `[SkirmishStressPreset] result=Passed` and `[SkirmishStressValidation] result=Passed`.
5. Confirm setup still shows only `1 · DESERT BASE` and `2 · CITY CROSSROADS`.
6. Launch the default sequence. Wait until `[SkirmishStressProbe] Census samples=` appears.
7. Record the census markdown and console lines. Compare spawned vs requested; never close the gate on requested counts alone.
8. Optional: P50 warmup spread, P200 concentrated dense combat, P100 air-only, then a 350/500 engineering probe if the machine survives.
9. Restore the Editor scene/profile if a reversible QA snapshot was taken.

Windows: use the checked PowerShell wrappers and the same executeMethod / pass marker. Do not invoke Unity directly.

## Screenshots

No Editor screenshots were captured in this VM.

Placeholder shots for Game PM / QA (1920×1080 Game view):

1. Skirmish setup still showing two player battles.
2. Stress warmup, spread, all living units, census line visible in console.
3. Concentrated dense combat, both armies on screen, attack traces visible.
4. Air/transport phase with helicopters moving.
5. Destruction phase with at least one wreck/loss and a non-zero `destroyedCombat`.

Do not invent or reuse unrelated screenshots.

## Known blockers

- Play-mode census is Editor-only until someone runs the probe. This report must not be read as a device or 500-entity performance pass.
- `SkirmishPresetConfig.InfantryLimitPerFaction` is still 24. Stress scale uses initial spawn, not recruitment. Recruiting past 24 during a stress match is expected to refuse.
- `SkirmishCombatPolicy.ApplyRoster` still retunes only soldier / light armored car / guard tower. Ghillie, APC, tank, and helicopters keep their prefab combat stats.
- Spatial index 2048-entry bound, path queue, and render budget are unmeasured at 200/350/500. That is E0.4 work.
- Large radii can still fail `no-free-cell` on Desert Base. Treat `missingSpawnCombat` as a real spawn failure, not a logging bug.
- City Crossroads is not the stress host. The known CC floating-shelf defect is unchanged (E0.1).
- Helipad placement may fail the existing Skirmish slope/height audit; air units still spawn from the initial-unit path.
- Scenario index 2 is hidden. `QuickCustomScreenView.Bind` still clamps player setup to 0/1.

## Evidence paths

| Path | Role |
|---|---|
| `Assets/Game/Resources/SkirmishStressScaleProbe.asset` | Hidden Scenario 2 preset |
| `Assets/Game/Configs/Skirmish/Stress/` | Construction, UnitRegistry, InitialForces |
| `Assets/Game/Scripts/Configs/SkirmishStressRecipe.cs` | Seeds, phases, requested tables |
| `Assets/Game/Scripts/Systems/SkirmishStressDirector.cs` | Projection, orders, phase machine |
| `Assets/Game/Scripts/Systems/SkirmishStressCensus.cs` | Measured counts |
| `Assets/Game/Scripts/Editor/SkirmishStressEditorProbe.cs` | Play-mode launcher |
| `Assets/Tests/Editor/SkirmishStressPresetTests.cs` | Focused Edit-mode validation |
| `Assets/Game/Resources/SkirmishBaseAssault.asset` | Unchanged player preset 0 |
| `Assets/Game/Resources/SkirmishCityCrossroads.asset` | Unchanged player preset 1 |

## Next step for Game PM

Run the Unity gate above on the `codex/m03-radar-warning` integration Editor, paste **actual** census rows into this file, and only then schedule E0.4 device profiling. E0.1 (CC float), E1 roster expansion, and other PR branches stay out of this package.
