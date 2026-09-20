# E0.3 — reproducible stress scene/preset

Status: authored on `cursor/e03-stress-preset-b6c7`. Edit-mode checks are source-authored. Play-mode **actual counts** and screenshots were **not** produced in the cloud VM (no Unity Editor). Do not invent PNG evidence. Do not treat requested recipe counts as spawned/alive/destroyed.

Handoff: **Programmer 2** owns Unity self-validation and screenshots. There is no Game PM merge gate. Draft PR is a bookmark only. After Programmer 2 has census + PNGs, Game PM forwards that packet to Farhad. Do **not** push/fast-forward into `codex/m03-radar-warning` until Farhad has seen the shots.

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
2. Player-setup two-battle shot: `Tools/Warline/Skirmish/Launch E0.3 Player Setup Capture`
3. Launch the default spread sequence: `Tools/Warline/Skirmish/Launch E0.3 Stress Sequence`
4. Concentrated visibility sequence: `Tools/Warline/Skirmish/Launch E0.3 Stress/Concentrated Sequence P100`
5. Single-phase menus under `Tools/Warline/Skirmish/Launch E0.3 Stress/…`
6. Mid-run snapshot: `Tools/Warline/Skirmish/Capture E0.3 Census Now`
7. Manual Game-view shots: `Tools/Warline/Skirmish/Capture E0.3 Screenshot/Player Setup Two Battles` and `…/Current Phase`

Or from the C# console / a one-off executeMethod:

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

macOS:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-e03-stress.log -- \
  -quit -executeMethod SkirmishStressPresetTests.RunFocusedValidation
```

Windows (checked wrapper only; do not invoke Unity.exe directly):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor>\Unity.exe" `
  -ProjectPath (Get-Location).Path `
  -ExecuteMethod SkirmishStressPresetTests.RunFocusedValidation `
  -LogFile "$env:TEMP\warline-e03-stress.log" `
  -RequiredPassMarker "[SkirmishStressPreset] result=Passed"
```

Required markers: `[SkirmishStressPreset] result=Passed` and `[SkirmishStressValidation] result=Passed`.

### Play-mode census capture (actual counts)

After the probe finishes — or after `Capture E0.3 Census Now` — copy **measured** files. Never paste the requested table into the actual-count section.

| Artifact | Default path |
|---|---|
| Census markdown | `$TMPDIR/warline-e03-stress/E0_3_STRESS_CENSUS.md` (macOS) or `%TEMP%\warline-e03-stress\E0_3_STRESS_CENSUS.md` (Windows) |
| Override directory | env `WARLINE_SKIRMISH_STRESS_REPORT_DIR` |
| Console filter | `[SkirmishStress]` and `[SkirmishStressProbe]` |

The census table columns `Spawned` / `Alive` / `Destroyed` / `Missing spawn` are measured. `Requested` is authoring input only. `spawn-stalled` in the log means initial spawn stopped making progress for 8 seconds (typically `no-free-cell`); warmup still records whatever entities exist.

## Phase definitions

Each phase can run alone (`sequence: false`) or as the default sequence (`warmup → idle → mass move → dense combat → air/transport → destruction`).

| Phase | What it does | Force composition |
|---|---|---|
| warmup | Wait until initial spawn finishes, or 8s of frozen spawn progress. No orders. | Ground + support. Sequence also pre-spawns air. |
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

**Not measured in this delivery.** The cloud agent could not enter Unity play mode.

Programmer 2: paste the probe census table here after a real Editor run. Leave cells as `—` until then.

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

## Editor self-test (Programmer 2)

Do this on a machine with Unity Hub signed in and the Editor version in `ProjectSettings/ProjectVersion.txt`. Idle Editor, no dirty scenes. Keep Hub open. Use the macOS wrapper (no `-batchmode`) or the checked Windows PowerShell wrappers. Do not invoke Unity directly.

### A. Compile / Edit-mode

1. Confirm no other Editor owns this worktree.
2. Optional: `Tools/Warline/Skirmish/Rebuild E0.3 Stress Preset`, then save assets.
3. Run `SkirmishStressPresetTests.RunFocusedValidation` through the platform wrapper above.
4. Expect `[SkirmishStressPreset] result=Passed` and `[SkirmishStressValidation] result=Passed`.

### B. Player setup still has two battles

1. `Tools/Warline/Skirmish/Launch E0.3 Player Setup Capture`.
2. Confirm only `1 · DESERT BASE` and `2 · CITY CROSSROADS`. No third player button.
3. The probe sets Game view to 1920×1080 and writes `$TMPDIR/warline-e03-stress/e03-player-setup-two-battles.png` (or `%TEMP%\warline-e03-stress\`).
4. If the file is empty, stay in play on setup and use `Capture E0.3 Screenshot/Player Setup Two Battles`.
5. Exit play mode.

### C. Default spread sequence + actual counts

1. `Tools/Warline/Skirmish/Launch E0.3 Stress Sequence`.
2. Console filter: `[SkirmishStress]`.
3. Wait until `[SkirmishStressProbe] Census samples=` (sequence writes a Destruction row). Timeout is 420s.
4. If spawn cannot place every unit, look for `[SkirmishStress] spawn-stalled` and a positive `missingSpawnCombat`. That is a measured shortfall, not a hang.
5. Optional mid-phase: `Tools/Warline/Skirmish/Capture E0.3 Census Now`.
6. Copy `$TMPDIR/warline-e03-stress/E0_3_STRESS_CENSUS.md` into the Actual counts table above.
7. Collect PNGs the probe queued (end-of-frame `ScreenCapture`; wait one frame after the log line if a file is still empty):

| File | What it must show |
|---|---|
| `e03-warmup-spread-p100.png` | Living armies on Desert Base, no orders yet |
| `e03-idle-spread-p100.png` | Hold / idle |
| `e03-massmove-spread-p100.png` | Ground units moving toward the midline |
| `e03-densecombat-spread-p100.png` | Contact + attack traces if combat has started |
| `e03-airtransport-spread-p100.png` | Helicopters moving |
| `e03-destruction-spread-p100.png` | At least one wreck/loss if `destroyedCombat` > 0 |

### D. Concentrated visibility

1. Exit play mode.
2. `Tools/Warline/Skirmish/Launch E0.3 Stress/Concentrated Sequence P100`.
3. Collect `e03-*-concentrated-p100.png`, especially `e03-densecombat-concentrated-p100.png` with both armies in one camera.
4. Keep the second census (rename the first file before this run if you need both).

### E. Optional scale spots

P50 warmup spread, P200 concentrated dense combat, P100 air-only. 350/500 only if the machine survives. Same census rule: report spawned/alive/destroyed, not requested.

### F. After capture

1. Restore the Editor scene/profile if a reversible QA snapshot was taken.
2. Programmer 2 owns the Unity evidence packet (census + PNGs). Game PM forwards it to Farhad. There is no Game PM merge gate.
3. Do not push/fast-forward this branch into `codex/m03-radar-warning` until Farhad has seen the shots.

## Screenshots

No Editor screenshots were captured in this VM. Filenames above are the contract for Programmer 2. Do not invent or reuse unrelated screenshots.

If `ScreenCapture` writes an empty file, recapture with `Capture E0.3 Screenshot/Current Phase` after the Game view has painted.

## Known blockers

- Play-mode census is Editor-only until Programmer 2 runs the probe. This report is not a device or 500-entity performance pass.
- `SkirmishPresetConfig.InfantryLimitPerFaction` is still 24. Stress scale uses initial spawn, not recruitment. Recruiting past 24 during a stress match is expected to refuse.
- `SkirmishCombatPolicy.ApplyRoster` still retunes only soldier / light armored car / guard tower. Ghillie, APC, tank, and helicopters keep their prefab combat stats.
- Spatial index 2048-entry bound, path queue, and render budget are unmeasured at 200/350/500. That is E0.4 work.
- Large radii can still fail `no-free-cell` on Desert Base. Warmup now completes after 8s of frozen spawn progress so census can record `missingSpawnCombat` instead of hanging forever.
- City Crossroads is not the stress host. The known CC floating-shelf defect is unchanged (E0.1).
- Helipad placement may fail the existing Skirmish slope/height audit; air units still spawn from the initial-unit path.
- Scenario index 2 is hidden. `QuickCustomScreenView.Bind` still clamps player setup to 0/1.

## Evidence paths

| Path | Role |
|---|---|
| `Assets/Game/Resources/SkirmishStressScaleProbe.asset` | Hidden Scenario 2 preset |
| `Assets/Game/Configs/Skirmish/Stress/` | Construction, UnitRegistry, InitialForces |
| `Assets/Game/Scripts/Configs/SkirmishStressRecipe.cs` | Seeds, phases, requested tables |
| `Assets/Game/Scripts/Systems/SkirmishStressDirector.cs` | Projection, orders, phase machine, 8s spawn-stall |
| `Assets/Game/Scripts/Systems/SkirmishStressCensus.cs` | Measured counts |
| `Assets/Game/Scripts/Editor/SkirmishStressEditorProbe.cs` | Play-mode launcher + census/screenshot capture |
| `Assets/Tests/Editor/SkirmishStressPresetTests.cs` | Focused Edit-mode validation |
| `Assets/Game/Resources/SkirmishBaseAssault.asset` | Unchanged player preset 0 |
| `Assets/Game/Resources/SkirmishCityCrossroads.asset` | Unchanged player preset 1 |

## Next step

Programmer 2: run A–D on this branch, paste **actual** census rows, attach the PNG paths listed above. Game PM forwards that packet to Farhad. Landing (push/fast-forward into `codex/m03-radar-warning`) waits until Farhad has seen the shots. E0.1 (CC float), E1 roster expansion, and PR #18/#19/#20 stay out of this package.
