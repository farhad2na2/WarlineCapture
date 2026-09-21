# Skirmish mission 6 (Desert Base Base Assault expanded)

Catalog identity **S004** is named once here: Desert Base · Base Assault · Air Mobile · Established Base.
Handoff ordinal 6. First visit: Regular / Standard. Seeds: `104733`, `130367`, `155925`.

Started from `main` at `9304e097f` (S003 Playable, PR #27). This lane does not edit Operations trees, shared `SaveDataModel`, `MatchSceneView`, or the localization catalog.

S002 and S003 publication on the checked-in manifest stay **Playable**. Rebuild of expanded definitions restores either row when it is already Playable. The in-memory factory still authors those rows as InProgress; the asset is the published status.

## What landed

### Definition, setup, and layout
- `SkirmishScenario_S004.asset` binds `skirmish.s004` / `scenario.skirmish.s004` / `opmap.skirmish.desert_base_01` / `layout.skirmish.db.ba`.
- Army profile **A** (existing `SkirmishArmy_AirMobile.asset`) and start **E** (existing Established package, readiness 2).
- Recommended later size is War. Certified first visit stays Regular Standard.
- Required features: `ground`, `intel`, `transport`, `offensive_air`, `advanced_air`, `objective_ba`.
- `ScenarioSetup_S004.asset` reuses the Desert Base deployment anchors. Deterministic seed is `104733`.
- `SkirmishLayout_S004.asset` is the existing Desert Base Base Assault envelope (600 × 420 m, player staging −192, 0, Barracks −228, 0, air pad −258, 117.6). No new map geometry.

### Compiler and catalog
- `SkirmishExpansionCatalogFactory` authors S004 beside S002 and S003. A manifest that only lists S002 features still compiles the three S002 sizes. An S004 air manifest compiles S003 and S004 (three sizes each) and leaves S002 uncompiled until `advanced_ground` is present.
- Regular Standard seeds `104733`, `130367`, `155925` compile against `INITIAL_SETUP_MATRIX.csv`: 12 rifle, 4 gunner, 4 rocketeer, 1 car, 1 armored APC, 1 AA, 1 transport helicopter, 0 tank, 900/240/700, deadline 1080 s, readiness 2, reported structures 10.
- War (`393245`) and Large War (`458883`) also match the matrix, including 2 and 3 starting AA and 2 transport helicopters. Measured pad binding stays Regular Standard only.
- Publication row is **InProgress**. Complete evidence still cannot flip S004. Asset existence cannot set Playable.

### Established Air Mobile grants (army A / start E)
- Ground Maneuver still excludes attack helicopters, jets, and the transport plane.
- Air Mobile still rejects tank, heavy APC, and ground siege, and still allows R1 AA from Ground Staging (220 Materials, ground cap).
- Established start seeds readiness 2. Category upgrades stay level zero. The research queue is empty. Starting Materials stay 900; the starting AA, transport helicopter, and Helipad are grants.
- Established plus offensive air grants one Helipad per side on the existing air-return pad. Airport is not granted. Field Air Mobile (S003) and Established Ground Maneuver (S002) do not receive that pad.
- With the granted pad and Established readiness, an attack helicopter queue debits 420 Materials once. Destroying the pad before dispatch refunds that payment and does not spawn the aircraft. A further transport helicopter queue then fails `MissingProducer`. AA production still uses Ground Staging and does not touch the air cap.
- Jets still need Full Arsenal and a living Airport. Bumping readiness without an Airport rejects the fighter and leaves stocks unchanged.

### Public ARIA and enemy hooks
- Shared Base Assault scoring still recruits `role.aa` when hostile air is visible. On S004 the starting transport helicopter is that visible air, so the enemy buys AA through `SkirmishProductionService.TryProduce` on faction 2. Player Materials stay 900.
- The public projection reports `PadReady` because the Helipad is already alive and readiness is Established. The `pad.not_ready` skill is the Field path. S003 Field behavior is unchanged.
- The planner still has no gameplay mutation API.

### Registry spawn
- Living Helipad uses the existing `Building_Helipad` key. Starting AA and the transport helicopter use `Unit_Veh_Missle_Launcher_Air` and `Unit_Veh_Helicopter_Transport`. No new art files were added.
- Attaching those keys does not debit Materials. A later attack-helicopter produce uses `Unit_Veh_Helicopter_Attack` and debits 420.

## Publication
- Status: **InProgress**. Compiler, Established roster, granted Helipad, AA/pad economy, public pad-ready flag, and registry spawn of the grant are in.
- S002 and S003 on the checked-in manifest stay Playable. Focused suites that still expected the S003 asset to be InProgress now expect Playable, matching the flip on `80d9335ce`. The S003 full-evidence validator decision stays InProgress.

## Remaining gaps

| Area | Gap |
|---|---|
| Structure census | Matrix reports 10 starting structures on Standard. The compiler spawns the designated Barracks, Ground Staging, and the granted Helipad. Intel, refinery, and the rest of that census are not spawned |
| Airport | Remains unbuilt, as the packet requires. Jets and the transport plane stay blocked |
| Air motion | No attack pass, return, landing, or refuel. The granted transport helicopter is a placed unit, not a flying one |
| Game View | No S004 launch or Playing PNG. No guarded Playable flip |
| Playable | Row stays InProgress until evidence exists and a later confirm flip is added |
| ARIA matrix | Seeds `104733` / `130367` / `155925` × EN/FA are not run. `runs.csv` is not opened |
| Localization | Keys below are listed for Game Design. `V3UiLocalizationCatalog` was not edited |
| Library / HUD | S004 is not a Quick Custom card |
| Combat certification | Air overlay health, damage, and range are uncertified. Helipad health 500 is an uncertified placeholder |

### Localization keys for Game Design

Do not treat this list as merged copy. Suggested English gloss only:

- `skirmish.s004.title` — Desert Base · Base Assault · Air Mobile · Established Base
- `skirmish.s004.brief` — Scout with infantry and the starting transport helicopter. AA covers the staging area. Lift infantry toward the north ruins, or build offensive air while a ground reserve stays home.
- `skirmish.s004.objective` — Destroy the original enemy Barracks while your original Barracks survives.
- `skirmish.s004.warning.offensive_air` — Established Air Mobile can queue attack helicopters from the granted Helipad. Jets and the transport plane still need an Airport and Full Arsenal. Tanks, heavy APCs, and ground siege are not in this army.
- `skirmish.s004.warning.replacement_base` — A replacement Barracks does not replace the original main base.
- `skirmish.s004.result.victory` — The original enemy Barracks is destroyed.
- `skirmish.s004.result.defeat` — The original player Barracks is destroyed.
- `skirmish.s004.result.draw_bases` — Both original Barracks were destroyed on the same tick.
- `skirmish.s004.result.draw_deadline` — Both original Barracks were still standing at the deadline.
- `skirmish.s004.result.surrender` — Surrender accepted.

## How to validate in Editor

Use the Skirmish shadow project. Do not lock the shared checkout. Keep Unity Hub open and signed in, and run the Windows wrapper (or `Tools/CI/invoke_unity_macos.sh` on macOS, without `-batchmode`). This environment has no Unity Editor, so the markers below were not executed here.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-definitions.log" `
  -RequiredPassMarker "[SkirmishExpandedDefinitionTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedCatalogTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-catalog.log" `
  -RequiredPassMarker "[SkirmishExpandedCatalogTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedEconomyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-economy.log" `
  -RequiredPassMarker "[SkirmishExpandedEconomyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-aria.log" `
  -RequiredPassMarker "[SkirmishExpandedAriaTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedVisualTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-visual.log" `
  -RequiredPassMarker "[SkirmishExpandedVisualTests] result=Passed"
```

Markers:

- `S004RegularStandardCompilesFirstVisitSeeds`
- `S004EstablishedSizesMatchMatrixWithStartingAir`
- `S004EstablishedAirRejectsArmorAndKeepsGrantedPad`
- `S004AssetsReuseDesertBaseLayoutAndStayInProgress`
- `S004CatalogWalkCompilesAirRowsAndLeavesS002OnItsOwnManifest`
- `S004PublicationStaysInProgressAndPriorRowsStayPlayable`
- `S004EstablishedGrantsDoNotDebitAndPadStartsLive`
- `S004EstablishedPadReadyDoesNotMutatePlayerStocks`
- `S004RegistrySpawnsGrantedTransportAaAndHelipad`

S002 and S003 tests in the same suites stay in the run.
