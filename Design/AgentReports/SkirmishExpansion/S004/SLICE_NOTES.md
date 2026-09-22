# Skirmish mission 6 (Desert Base Base Assault expanded)

Catalog identity **S004** is named once here: Desert Base · Base Assault · Air Mobile · Established Base.
Handoff ordinal 6. First visit: Regular / Standard. Seeds: `104733`, `130367`, `155925`.

Started from `main` at `9304e097f` (S003 Playable, PR #27). This lane does not edit Operations trees, shared `SaveDataModel`, or `MatchSceneView`. EN/FA catalog copy was merged from Game Design and was not re-authored here.

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
- Starting AA and the transport helicopter are ledger grants: reservation id 0, empty production buffer, Materials 900, Fuel 700, Oil 240. Standard air cap is 2 with 1 air already live. Filling that cap rejects another attack helicopter with `InsufficientCapacity` and no debit.
- While an attack helicopter reservation is `Producing` on the granted Helipad, a second air `TryProduce` returns `QueueLocked` and does not debit. AA still buys from Ground Staging. `NotifyProducerDestroyed` on that producing reservation marks it `Lost` and does not refund the 420. A later reserved transport helicopter refunds its 240 and returns to `Cancelled`.
- Aircraft efficiency researches from the living Helipad for 200 Materials. A dead pad rejects it with `producer.air` and leaves stocks alone. Completion stays at Established readiness, leaves infantry and vehicle levels at zero, and scales the granted transport's fuel rates to 90 percent. A second queue is `AlreadyCompleted`.
- Releasing the granted transport on death frees its air and supply once and does not refund Materials. Enemy Materials stay 900 and enemy air live stays 1.

### Public ARIA and enemy hooks
- Shared Base Assault scoring still recruits `role.aa` when hostile air is visible. On S004 the starting transport helicopter is that visible air, so the enemy buys AA through `SkirmishProductionService.TryProduce` on faction 2. Player Materials stay 900.
- The public projection reports `PadReady` because the Helipad is already alive and readiness is Established. It also reports `AirQueueOffered` when that pad can afford an attack helicopter. The enemy transport helicopter is visible hostile air, so the first skill is still `recruit.aa`. When AA is not affordable, the same view recruits `recruit.attack_heli` instead of `pad.not_ready`.
- The planner targets the presented `RecruitAntiAir` control first. With the pad ready and an air queue offered, it does not inspect the pad; it continues to the legal attack control. After the granted pad is destroyed, `PadReady` and `AirQueueOffered` clear and the Field `pad.not_ready` path returns. S003 Field behavior is unchanged.
- The planner still has no gameplay mutation API. Player Materials stay 900. Enemy air live stays 1.

### Registry spawn
- Living Helipad uses the existing `Building_Helipad` key. Starting AA and the transport helicopter use `Unit_Veh_Missle_Launcher_Air` and `Unit_Veh_Helicopter_Transport`. No new art files were added.
- Attaching those keys does not debit Materials. Player and enemy grants both spawn, so the transport, AA, and Helipad keys each have two registry instances. A later attack-helicopter produce uses `Unit_Veh_Helicopter_Attack`, debits 420, and fills the Standard air cap. A further transport helicopter then fails `InsufficientCapacity` and does not add another visual. Enemy Materials stay 900. Airport is still absent.

## Publication
- Status: **InProgress**. Compiler, Established roster, grant ledger, Helipad research, public pad-ready and air-queue flags, registry spawn of the grants, and the Game View / flip entry points are in. The Playing PNG is not captured on this tip.
- `SkirmishS004GameViewCapture.RunFocusedLaunchRegularStandard` queues catalog **S004** / Regular / Standard / seed `104733`, dumps the Playing frame, and stays in Play Mode. It does not inject an army or force Victory.
- Playing PNG and sidecar are written under project `_Evidence/` and copied to `Design/AgentReports/SkirmishExpansion/S004/_Evidence/`. Names: `s004-regular-standard-104733-playing.png` and `s004-regular-standard-104733-gameview.json`.
- `SkirmishPublicationFlipMenu.RunFocusedFlipS004DryRun` only evaluates. `RunFocusedFlipS004Confirm` writes **S004** to Playable when those files exist and the compiled content/setup hashes match. It does not change the S002 or S003 rows. The checked-in manifest stays **InProgress** until that confirm runs.
- S002 and S003 on the checked-in manifest stay Playable. The S004 full-evidence validator decision stays InProgress until Established air flight/refuel and the ARIA matrix land.

## Remaining gaps

| Area | Gap |
|---|---|
| Structure census | Matrix reports 10 starting structures on Standard. The compiler spawns the designated Barracks, Ground Staging, and the granted Helipad. Intel, refinery, and the rest of that census are not spawned |
| Airport | Remains unbuilt, as the packet requires. Jets and the transport plane stay blocked |
| Air motion | No attack pass, return, landing, or refuel. The granted transport helicopter is a placed unit, not a flying one |
| Game View | Launch and guarded flip entry points are in. The Playing PNG for seed `104733` is not captured yet |
| Playable | Row stays InProgress until the evidence files exist and Programmer 1 runs the confirm flip |
| ARIA matrix | The Windows watch harness and a header-only `runs.csv` are in. Seeds `104733` / `130367` / `155925` × EN/FA are not run. No Victory row is stamped |
| Localization | EN/FA keys are merged from Game Design (`ca4c5f0ee`, PR #30). ARIA matrix runs in both locales are still open |
| Library / HUD | S004 is not a Quick Custom card |
| Combat certification | Air overlay health, damage, and range are uncertified. Helipad health 500 is an uncertified placeholder |

### Localization

EN/FA keys are merged from `cursor/skirmish-s004-localization-4634` at `ca4c5f0ee` (PR #30). The merge kept Game Design’s `V3UiLocalizationCatalog.asset` and `Assets/Game/Configs/Localization/SkirmishS004UiStrings.json`. Keys were not re-authored on this branch.

- `skirmish.s004.title`
- `skirmish.s004.brief`
- `skirmish.s004.objective`
- `skirmish.s004.warning.offensive_air`
- `skirmish.s004.warning.replacement_base`
- `skirmish.s004.result.victory`
- `skirmish.s004.result.defeat`
- `skirmish.s004.result.draw_bases`
- `skirmish.s004.result.draw_deadline`
- `skirmish.s004.result.surrender`

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
- `S004EstablishedGrantLedgerAirCapAndHelipadResearch`
- `S004EstablishedPadReadyDoesNotMutatePlayerStocks`
- `S004RegistrySpawnsGrantedTransportAaAndHelipad`
- `S004GameViewNamesAndDryFlipDoNotPublish`

S002 and S003 tests in the same suites stay in the run.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAcceptanceTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-acceptance.log" `
  -RequiredPassMarker "[SkirmishExpandedAcceptanceTests] result=Passed"
```

### Game View capture and guarded flip

Use the Skirmish shadow project. Keep Unity Hub open and signed in. The launch stays in Play Mode (`stayInPlayMode=1`), same as S002 and S003.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishS004GameViewCapture.RunFocusedLaunchRegularStandard `
  -LogFile "$env:TEMP\skirmish-s004-gameview.log" `
  -RequiredPassMarker "[SkirmishS004GameView] result=Passed"
```

Required evidence, either directory is enough for the flip:

| File | Directory |
|---|---|
| `s004-regular-standard-104733-playing.png` | `_Evidence/` and `Design/AgentReports/SkirmishExpansion/S004/_Evidence/` |
| `s004-regular-standard-104733-gameview.json` | same two directories (`forcedVictory=0`) |

Dry-run does not write the manifest. Before the files exist it logs `result=Failed catalog=S004 playable=0 confirm=0`, and the Windows wrapper treats that as a failed validation. After the files exist and the hashes match, the same method logs `wouldFlip=1` and still leaves the asset InProgress.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishPublicationFlipMenu.RunFocusedFlipS004DryRun `
  -LogFile "$env:TEMP\skirmish-s004-flip-dry.log" `
  -RequiredPassMarker "[SkirmishPublicationFlip] result=Passed catalog=S004 wouldFlip=1 playable=0 confirm=0"
```

Confirm writes only the S004 row. Run it after the dry-run reports `wouldFlip=1`. S002 and S003 stay Playable.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishPublicationFlipMenu.RunFocusedFlipS004Confirm `
  -LogFile "$env:TEMP\skirmish-s004-flip-confirm.log" `
  -RequiredPassMarker "[SkirmishPublicationFlip] result=Passed catalog=S004 playable=1 confirm=1"
```

Menu names: `Tools/Warline/Skirmish/Launch S004 Regular Standard Game View` and `Tools/Warline/Skirmish/Flip S004 Playable If Evidence Ready`.

## ARIA Windows harness (not a live win)

The recorder is `Game.Editor.SkirmishS004AriaRunHarness`. It uses the same watch driver as S002 (`SkirmishExpandedAriaWatchDriver`): shipping touch ARIA, `normal_speed=1`, and an abort of `simulationNotAdvancing` about 45 seconds after Playing when simulation is inactive or match elapsed stays at 0. It never stamps Victory unless the match has finished with that outcome. This section does not mark the mission or the AriaWon matrix complete. Live S004 AriaWon waits until after the S002 Windows proof and until the Skirmish Editor lock is free. Do not start `Launch*` while that lock is held.

Focused proof (no live match; the wrapper's `-quit` is fine here):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishS004AriaHarnessTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s004-aria-harness.log" `
  -RequiredPassMarker "[SkirmishS004AriaHarnessTests] result=Passed" `
  -GuiLicensing
```

Live watch, only after the lock is free: run the menu or executeMethod from the open Editor. Do **not** pass `-quit`. `Tools/CI/InvokeUnityExecuteMethodValidation.ps1` always passes `-quit` and will return when Play Mode is scheduled, before any terminal row.

- Menu: `Tools/Warline/Skirmish/Launch S004 ARIA Watch 104733 en`
- Execute method: `Game.Editor.SkirmishS004AriaRunHarness.Launch104733En`

Keep Unity Hub signed in, focus the Game View, and keep `timeScale` at 1. Expect trace lines under `Design/AgentReports/SkirmishExpansion/S004/_Evidence/` with rising `elapsed` / `clockElapsed` and `simulationActive=1` within about 45 seconds of Playing. A stuck-zero clock must Abort with `simulationNotAdvancing`, not sit until the 1500 second timeout. Fill rules and the other seed menus are in `acceptance.md`.
