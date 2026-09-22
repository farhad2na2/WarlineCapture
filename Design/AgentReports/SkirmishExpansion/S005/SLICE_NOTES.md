# Skirmish mission 7 (Desert Base Base Assault expanded)

Catalog identity **S005** is named once here: Desert Base · Base Assault · Combined Arms · Field Base.
Handoff ordinal 7. First visit: Regular / Standard. Seeds: `104734`, `130368`, `155926`.

Started from `main` at `737ae3372` (S004 Playable, PR #29). This slice does not edit Operations trees, shared `SaveDataModel`, `MatchSceneView`, or `V3UiLocalizationCatalog`.

S002, S003, and S004 publication on the checked-in manifest stay **Playable**. Rebuild of expanded definitions restores any of those rows when it is already Playable. The in-memory factory still authors those rows as InProgress; the asset is the published status.

## What landed

### Definition, setup, and layout
- `SkirmishScenario_S005.asset` binds `skirmish.s005` / `scenario.skirmish.s005` / `opmap.skirmish.desert_base_01` / `layout.skirmish.db.ba`.
- Army profile **C** (`SkirmishArmy_CombinedArms.asset`) and start **F** (existing Field package, readiness 1, seven reported structures).
- Recommended later size is War. Certified first visit stays Regular Standard.
- Required features: `ground`, `intel`, `transport`, `offensive_air`, `advanced_ground`, `advanced_air`, `objective_ba`.
- `ScenarioSetup_S005.asset` reuses the Desert Base deployment anchors. Deterministic seed is `104734`.
- `SkirmishLayout_S005.asset` is the existing Desert Base Base Assault envelope (600 × 420 m, player staging −192, 0, Barracks −228, 0). No new map geometry.

### Compiler and catalog
- `SkirmishExpansionCatalogFactory` authors S005 beside S002, S003, and S004. A manifest that only lists S002 features still compiles the three S002 sizes and leaves S005 uncompiled until `offensive_air` and `advanced_air` are present. An S004 air manifest still compiles S003 and S004 and leaves S002 and S005 uncompiled until `advanced_ground` is present. The S005 manifest compiles all four rows, three sizes each.
- Regular Standard seeds `104734`, `130368`, `155926` compile against `INITIAL_SETUP_MATRIX.csv`: 8 rifle, 4 rocketeer, 1 car, 1 armored APC, 0 tank, 0 air, 450/120/350, deadline 1080 s, readiness 1, reported structures 7.
- War (`393246`) and Large War (`458884`) also match the matrix. Measured pad binding stays Regular Standard only.
- Publication row is **InProgress**. Complete evidence cannot flip S005 to Playable in this slice. Asset existence cannot set Playable.

### Combined Arms gates (army C / start F)
- Ground Maneuver still excludes attack helicopters, jets, and the transport plane.
- Air Mobile still rejects tank, heavy APC, and ground siege, and still allows R1 AA.
- Combined Arms allows the certified ground, infantry, recon, transport, offensive-air, anti-air, and siege roles together, including tank, heavy APC, siege, R1 AA, and offensive air.
- Field start does not grant a Helipad or an Airport. R1 AA queues from Ground Staging (220 Materials). Tank and heavy APC need Established readiness. Siege needs Full Arsenal. Attack helicopters need Established readiness and a living Helipad. Jets need Full Arsenal and a living Airport.
- Compiled S005 overlays carry those roles with `CapabilityCertified=false`. Heavy APC materials are 280 and siege materials are 420, from MATCH_SETUP. Health, damage, and range on the new overlays are uncertified placeholders. This slice does not spawn Combined Arms visuals and does not install the shared armor/AA/air reservation ledger.

## Publication
- Status: **InProgress**. Compiler, definition, Field roster, and Combined Arms production gates only.
- Not Playable, not Game View captured, not ARIA/War certified, not Accepted.

## Remaining gaps

| Area | Gap |
|---|---|
| Structure census | Matrix reports 7 starting structures on Standard. The compiler spawns the designated Barracks and Ground Staging only |
| Field producers | Helipad and Airport stay unbuilt. Offensive air, tanks, heavy APC, and siege are gated until readiness and the producer are paid for |
| Shared reservations | Simultaneous armor, AA, and air queues do not yet share category and Supply reservations. Eligibility is per request only |
| Air and armor motion | No attack pass, return, landing, or refuel. No tank or siege motion |
| Game View | No Regular Standard Playing capture for seed `104734` |
| Playable | Row stays InProgress until visuals, a human win, and the evidence files exist |
| ARIA matrix | Seeds `104734` / `130368` / `155926` × EN/FA are not run. `runs.csv` is not opened for S005 |
| Localization | Keys below are listed for Game Design. `V3UiLocalizationCatalog` was not edited |
| Library / HUD | S005 is not a Quick Custom card |
| Combat certification | Combined overlay health, damage, and range are uncertified. Fuel return is not simulated |

### Localization

Game Design authors these EN/FA keys. This slice does not edit the localization catalog or add `SkirmishS005UiStrings.json`.

- `skirmish.s005.title`
- `skirmish.s005.brief`
- `skirmish.s005.objective`
- `skirmish.s005.warning.offensive_air`
- `skirmish.s005.warning.shared_reservation`
- `skirmish.s005.warning.replacement_base`
- `skirmish.s005.result.victory`
- `skirmish.s005.result.defeat`
- `skirmish.s005.result.draw_bases`
- `skirmish.s005.result.draw_deadline`
- `skirmish.s005.result.surrender`

The definition asset already stores `title`, `brief`, `objective`, `result.victory`, and `result.defeat`. The warning and remaining result keys are catalog copy only.

## How to validate in Editor

Use the Skirmish shadow project. Do not lock the shared checkout. Keep Unity Hub open and signed in, and run the Windows wrapper (or `Tools/CI/invoke_unity_macos.sh` on macOS, without `-batchmode`). This environment has no Unity Editor, so the markers below were not executed here.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s005-definitions.log" `
  -RequiredPassMarker "[SkirmishExpandedDefinitionTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedCatalogTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s005-catalog.log" `
  -RequiredPassMarker "[SkirmishExpandedCatalogTests] result=Passed"
```

New markers inside those suites:

- `S005RegularStandardCompilesFirstVisitSeeds`
- `S005FieldSizesMatchMatrixWithoutStartingAir`
- `S005CombinedArmsKeepsArmorAndGatesFieldAir`
- `S005AssetsReuseDesertBaseLayoutAndStayInProgress`
- `S005CatalogWalkCompilesCombinedRowsOnFullManifest`
- `S005PublicationStaysInProgressAndPriorRowsStayPlayable`

S002, S003, and S004 tests in the same suites stay in the run. The checked-in S004 row is Playable, so the S004 asset assertions now expect Playable. The S004 full-evidence validator decision stays InProgress.
