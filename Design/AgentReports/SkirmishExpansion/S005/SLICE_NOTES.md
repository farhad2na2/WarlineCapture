# Skirmish mission 7 (Desert Base Base Assault expanded)

Catalog identity **S005** is named once here: Desert Base · Base Assault · Combined Arms · Field Base.
Handoff ordinal 7. First visit: Regular / Standard. Seeds: `104734`, `130368`, `155926`.

Started from `main` at `737ae3372` (S004 Playable, PR #29). This lane does not edit Operations trees, shared `SaveDataModel`, or `MatchSceneView`. EN/FA catalog copy was merged from Game Design and was not re-authored here.

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
- Publication row is **InProgress**. Complete evidence cannot flip S005 to Playable. Asset existence cannot set Playable. The guarded Game View flip can write Playable only when `confirmWrite=true` and the Regular Standard files exist. This tip does not run that write.

### Combined Arms gates (army C / start F)
- Ground Maneuver still excludes attack helicopters, jets, and the transport plane.
- Air Mobile still rejects tank, heavy APC, and ground siege, and still allows R1 AA.
- Combined Arms allows the certified ground, infantry, recon, transport, offensive-air, anti-air, and siege roles together, including tank, heavy APC, siege, R1 AA, and offensive air.
- Field start does not grant a Helipad or an Airport. R1 AA queues from Ground Staging (220 Materials). Tank and heavy APC need Established readiness. Siege needs Full Arsenal. Attack helicopters need Established readiness and a living Helipad. Jets need Full Arsenal and a living Airport.
- Compiled S005 overlays carry those roles with `CapabilityCertified=false`. Heavy APC materials are 280 and siege materials are 420, from MATCH_SETUP. Health, damage, and range on the new overlays are uncertified placeholders.

### Combined Arms economy (SK-03)
- Field AA (`role.aa`) buys from a living Ground Staging for 220 Materials and 6 Supply. It uses the ground cap, not the tactical-air cap, and does not require a Helipad.
- At Field readiness, tank, heavy APC, siege, attack helicopter, and fighter queues are rejected with `MissingReadiness`. Materials, Fuel, and reservations stay unchanged.
- Established readiness opens tank (360 Materials) and heavy APC (280 Materials) from Ground Staging. The Field purse of 450 cannot pay both; the second purchase returns `InsufficientMaterials` and does not debit.
- Siege stays closed until Full Arsenal, then buys from Ground Staging for 420 Materials. A dead Helipad does not block that ground queue.
- Attack helicopters still need Established readiness and a living Helipad. Queueing one debits 420 Materials. Destroying the pad before dispatch refunds that payment and does not spawn the aircraft.
- Jets need Full Arsenal and a living Airport. The 450 Field purse rejects the 480 Materials fighter with `InsufficientMaterials` until the purse can cover it. Paying that cost debits once and uses the air cap.
- Simultaneous armor, AA, and air queues still do not share a category ledger beyond the existing per-request capacity check.

### Public ARIA and enemy hooks (SK-05)
- Shared Base Assault scoring recruits `role.aa` when hostile air is visible and army C can afford it. Visible tanks without hostile air still recruit the rocketeer counter.
- The enemy executes that AA recruit only through `SkirmishProductionService.TryProduce` on faction 2. Player Materials stay 450.
- Field projection reports `CanAffordAntiAir` and not `CanAffordTank`. Paying readiness to Established flips `CanAffordTank` without touching stocks. `PadReady` and `AirQueueOffered` stay clear until a living Helipad exists at Established readiness. Destroying that pad clears both flags. S003 Field and S004 Established behavior is unchanged.
- `AriaSkirmishPlanSystem.StepExpandedBaseAssault` targets the presented `RecruitAntiAir` control, or the presented `AirPad` control when an air queue is offered and the pad is not ready. With the pad ready it does not inspect the pad; it continues to the legal attack control. The planner still has no gameplay mutation API.

### Registry spawn (SK-02)
- Living Helipad and Airport structures map to the existing producers and to prefab keys `Building_Helipad` and `Building_Airport`. Tank, heavy APC, siege, AA, attack helicopter, and fighter use `Unit_Veh_Tank_USA`, `Unit_Veh_APC_Heavy`, `Unit_Veh_Missle_Launcher_Ground`, `Unit_Veh_Missle_Launcher_Air`, `Unit_Veh_Helicopter_Attack`, and `Unit_Veh_Jet_02`. No new art files were added.
- Field start does not spawn those visuals. AA spawns at Ground Staging without touching the air cap. Tank and heavy APC spawn only after Established readiness. Siege, the attack helicopter, and the fighter spawn only after their readiness and producer gates pass. Filling the Standard air cap rejects another attack helicopter with `InsufficientCapacity` and does not add another visual. Enemy Materials stay 450.

## Publication
- Status: **InProgress**. Compiler, Field roster, AA/armor/siege/air economy, public pad and tank flags, registry spawn, and the Game View / flip entry points are in. The Playing PNG is not captured on this tip.
- `SkirmishS005GameViewCapture.RunFocusedLaunchRegularStandard` queues catalog **S005** / Regular / Standard / seed `104734`, dumps the Playing frame, and stays in Play Mode. It does not inject an army or force Victory.
- Playing PNG and sidecar are written under project `_Evidence/` and copied to `Design/AgentReports/SkirmishExpansion/S005/_Evidence/`. Names: `s005-regular-standard-104734-playing.png` and `s005-regular-standard-104734-gameview.json`.
- `SkirmishPublicationFlipMenu.RunFocusedFlipS005DryRun` only evaluates. `RunFocusedFlipS005Confirm` writes **S005** to Playable when those files exist and the compiled content/setup hashes match. It does not change the S002, S003, or S004 rows. The checked-in manifest stays **InProgress** until that confirm runs.
- S002, S003, and S004 on the checked-in manifest stay Playable. The S005 full-evidence validator decision stays InProgress until Combined Arms flight/refuel and the ARIA matrix land.

## Remaining gaps

| Area | Gap |
|---|---|
| Structure census | Matrix reports 7 starting structures on Standard. The compiler spawns the designated Barracks and Ground Staging only |
| Field producers | Helipad and Airport stay unbuilt at start. Offensive air and jets stay gated until readiness and the producer are paid for |
| Shared reservations | Simultaneous armor, AA, and air queues do not yet share a category ledger beyond per-request capacity |
| Air and armor motion | No attack pass, return, landing, or refuel. No tank or siege motion |
| Game View | Launch and guarded flip entry points are in. The Playing PNG for seed `104734` is not captured yet |
| Playable | Row stays InProgress until the evidence files exist and Programmer 1 runs the confirm flip |
| ARIA matrix | Seeds `104734` / `130368` / `155926` × EN/FA are not run. `runs.csv` is not opened for S005 |
| Localization | EN/FA keys are merged from Game Design (`333c665ff`, PR #32). ARIA matrix runs in both locales are still open |
| Library / HUD | S005 is not a Quick Custom card |
| Combat certification | Combined overlay health, damage, and range are uncertified. Fuel return is not simulated |

### Localization

EN/FA keys are merged from `cursor/skirmish-s005-localization-26fc` at `333c665ff` (PR #32). The merge kept Game Design’s `V3UiLocalizationCatalog.asset` and `Assets/Game/Configs/Localization/SkirmishS005UiStrings.json`, including `skirmish.s005.warning.shared_reservation`. Keys were not re-authored on this branch.

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

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedEconomyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s005-economy.log" `
  -RequiredPassMarker "[SkirmishExpandedEconomyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s005-aria.log" `
  -RequiredPassMarker "[SkirmishExpandedAriaTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedVisualTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s005-visual.log" `
  -RequiredPassMarker "[SkirmishExpandedVisualTests] result=Passed"
```

Markers:

- `S005RegularStandardCompilesFirstVisitSeeds`
- `S005FieldSizesMatchMatrixWithoutStartingAir`
- `S005CombinedArmsKeepsArmorAndGatesFieldAir`
- `S005AssetsReuseDesertBaseLayoutAndStayInProgress`
- `S005CatalogWalkCompilesCombinedRowsOnFullManifest`
- `S005PublicationStaysInProgressAndPriorRowsStayPlayable`
- `S005CombinedArmsFieldAaGateAndPaidReadiness`
- `S005CombinedArmsFieldPublicControlsDoNotMutateGameplay`
- `S005RegistrySpawnsPaidArmorAaAndAirFromExistingKeys`
- `S005GameViewNamesAndDryFlipDoNotPublish`

S002, S003, and S004 tests in the same suites stay in the run. The checked-in S002–S004 rows are Playable. The S005 full-evidence validator decision stays InProgress.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAcceptanceTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s005-acceptance.log" `
  -RequiredPassMarker "[SkirmishExpandedAcceptanceTests] result=Passed"
```

### Game View capture and guarded flip

Use the Skirmish shadow project. Keep Unity Hub open and signed in. The launch stays in Play Mode (`stayInPlayMode=1`), same as S002, S003, and S004.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishS005GameViewCapture.RunFocusedLaunchRegularStandard `
  -LogFile "$env:TEMP\skirmish-s005-gameview.log" `
  -RequiredPassMarker "[SkirmishS005GameView] result=Passed"
```

Required evidence, either directory is enough for the flip:

| File | Directory |
|---|---|
| `s005-regular-standard-104734-playing.png` | `_Evidence/` and `Design/AgentReports/SkirmishExpansion/S005/_Evidence/` |
| `s005-regular-standard-104734-gameview.json` | same two directories (`forcedVictory=0`) |

Dry-run does not write the manifest. Before the files exist it logs `result=Failed catalog=S005 playable=0 confirm=0`, and the Windows wrapper treats that as a failed validation. After the files exist and the hashes match, the same method logs `wouldFlip=1` and still leaves the asset InProgress.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishPublicationFlipMenu.RunFocusedFlipS005DryRun `
  -LogFile "$env:TEMP\skirmish-s005-flip-dry.log" `
  -RequiredPassMarker "[SkirmishPublicationFlip] result=Passed catalog=S005 wouldFlip=1 playable=0 confirm=0"
```

Confirm writes only the S005 row. Run it after the dry-run reports `wouldFlip=1`. S002, S003, and S004 stay Playable.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishPublicationFlipMenu.RunFocusedFlipS005Confirm `
  -LogFile "$env:TEMP\skirmish-s005-flip-confirm.log" `
  -RequiredPassMarker "[SkirmishPublicationFlip] result=Passed catalog=S005 playable=1 confirm=1"
```

Menu names: `Tools/Warline/Skirmish/Launch S005 Regular Standard Game View` and `Tools/Warline/Skirmish/Flip S005 Playable If Evidence Ready`.
