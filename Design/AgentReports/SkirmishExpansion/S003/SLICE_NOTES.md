# Skirmish mission 5 (Desert Base Base Assault expanded)

Catalog identity **S003** is named once here: Desert Base · Base Assault · Air Mobile · Field Base.
Handoff ordinal 5. First visit: Regular / Standard. Seeds: `104732`, `130366`, `155924`.

Started from `main` at `a9cea343d` (S002 Playable, PR #23). This lane does not edit Operations trees, shared `SaveDataModel`, `MatchSceneView`, or Campaign/Gridlock. EN/FA catalog copy was merged from Game Design and was not re-authored here.

S002 publication on the checked-in manifest stays **Playable**. Rebuild of expanded definitions restores that status if the asset was already Playable.

## What landed

### Definition, setup, and layout
- `SkirmishScenario_S003.asset` binds `skirmish.s003` / `scenario.skirmish.s003` / `opmap.skirmish.desert_base_01` / `layout.skirmish.db.ba`.
- Army profile **A** (`SkirmishArmy_AirMobile.asset`) and start **F** (existing Field package, readiness 1, seven reported structures).
- Recommended later size is War. Certified first visit stays Regular Standard.
- Required features: `ground`, `intel`, `transport`, `offensive_air`, `advanced_air`, `objective_ba`.
- `ScenarioSetup_S003.asset` reuses the Desert Base deployment anchors. Deterministic seed is `104732`.
- `SkirmishLayout_S003.asset` is the existing Desert Base Base Assault envelope (600 × 420 m, player staging −192, 0, Barracks −228, 0). No new map geometry.

### Compiler and catalog
- `SkirmishExpansionCatalogFactory` authors S003 beside S002. Catalog walk still compiles S002 when the manifest only lists S002 features. S003 compiles when the manifest includes its air features.
- Regular Standard seeds `104732`, `130366`, `155924` compile against `INITIAL_SETUP_MATRIX.csv`: 8 rifle, 4 rocketeer, 1 car, 1 armored APC, 0 tank, 0 air, 450/120/350, deadline 1080 s, readiness 1.
- War (`393244`) and Large War (`458882`) also match the matrix. Measured pad binding stays Regular Standard only, same pads as S002.
- Publication row is **InProgress**. The full evidence bundle still cannot flip S003. The guarded Game View flip can write Playable only when `confirmWrite=true` and the Regular Standard files exist. This tip does not run that write.

### Air Mobile gates (army A / start F)
- Ground Maneuver still excludes attack helicopters, jets, and the transport plane. Its production test still returns `UnsupportedRole`.
- Air Mobile allows R1 AA from Ground Staging (220 Materials) and rejects tank, heavy APC, and ground siege.
- Attack helicopters need Established readiness and a living Helipad. Jets and the transport plane need Full Arsenal and a living Airport. Field start therefore cannot queue offensive or advanced air until those are paid for.
- Compiled S003 overlays carry those roles with `CapabilityCertified=false`. Materials follow MATCH_SETUP. Health and damage are uncertified placeholders.
- Role catalog bindings point at the existing air prefab configs.

### Air Mobile economy (SK-03)
- Field AA (`role.aa`) buys from a living Ground Staging for 220 Materials and 6 Supply. It uses the ground cap, not the tactical-air cap, and does not require a Helipad.
- Attack helicopters still need Established readiness and a living Helipad. Jets still need Full Arsenal and a living Airport. A missing pad or readiness rejects the queue and leaves Materials, Fuel, and reservations unchanged.
- Starting or dispatching a Helipad or Airport reservation whose pad or readiness is gone refunds the full Materials paid and cancels the reservation. No air unit is spawned. That is the failed-dispatch receipt. `NotifyProducerDestroyed` is unchanged: a Producing reservation reported through that path is still Lost without a refund, and a Reserved one is still refunded. Tank, heavy APC, and siege stay unsupported on army A.
- Produced roles that are absent from the starting force resolve their runtime prefab key from the existing role catalog (`Unit_Veh_Missle_Launcher_Air`, helicopter, and jet keys).

### Public ARIA and enemy hooks (SK-05)
- Shared Base Assault scoring recruits `role.aa` when hostile air is visible and army A can afford it. Army G never takes that branch, so the S002 rocketeer counter stays in place.
- The enemy executes that recruit only through `SkirmishProductionService.TryProduce` on faction 2. Player Materials are not touched.
- `AriaSkirmishPlanSystem.StepExpandedBaseAssault` targets the presented `RecruitAntiAir` control, or the presented `AirPad` control when an air queue is offered and the pad is not ready. The planner still has no gameplay mutation API.
- `SkirmishAriaSkillPolicy` uses the same public flags (`recruit.aa`, `pad.not_ready`). Shell HUD does not yet publish those touch targets.

### Registry air spawn (SK-02)
- Living Helipad and Airport structures map to the existing producers and to prefab keys `Building_Helipad` and `Building_Airport`. No new art files were added.
- When the unit registry already contains those keys, produced AA spawns at Ground Staging and a produced light attack helicopter spawns with the Helipad visual. Editor coverage uses stand-ins named with those existing keys.

### Regular Standard Game View and guarded flip
- `SkirmishS003GameViewCapture.RunFocusedLaunchRegularStandard` queues catalog **S003** / Regular / Standard / seed `104732`, dumps the Playing frame, and stays in Play Mode. It does not inject an army or force Victory.
- Playing PNG and sidecar are written under project `_Evidence/` and copied to `Design/AgentReports/SkirmishExpansion/S003/_Evidence/`. Names follow the S002 scaffold pattern: `s003-regular-standard-104732-playing.png` and `s003-regular-standard-104732-gameview.json`.
- `SkirmishPublicationFlipMenu.RunFocusedFlipS003DryRun` only evaluates. `RunFocusedFlipS003Confirm` writes **S003** to Playable when those files exist and the compiled content/setup hashes match. It does not change the S002 row. The checked-in manifest stays **InProgress** until that confirm runs.
- Rebuild of expanded definitions still restores S002 when that row is already Playable, and now does the same for S003. An InProgress row is left InProgress.

## Publication
- Status: **InProgress**. Compiler, Field roster, AA/pad economy, public air controls, registry spawn, and the Game View / flip entry points are in. The Playing PNG is not captured on this tip. Flight, refuel, and the ARIA matrix are not.
- Not Playable until Programmer 1 confirms the flip. S002 on the checked-in manifest stays Playable.

## Remaining gaps

| Area | Gap |
|---|---|
| Air motion | No attack pass, return, landing, or refuel. Fuel return is not simulated |
| Helipad yard | The Helipad/Airport prefab keys attach when the registry already has them. There is still no authored Skirmish pad yard, taxi path, or landing reservation |
| Field backbone | Matrix reports 7 starting structures. The compiler still spawns the designated Barracks and Ground Staging only |
| Game View | Launch entry point is in. The Playing PNG for seed `104732` is not captured yet |
| Playable | Row stays InProgress until the evidence files exist and Programmer 1 runs the confirm flip |
| ARIA matrix | The Windows watch harness and a header-only `runs.csv` are in. Seeds `104732` / `130366` / `155924` × EN/FA are not run. No Victory row is stamped. The shell does not yet present the AA or pad controls |
| Localization | EN/FA keys are merged from Game Design (`c01a4772b`, PR #28). ARIA matrix runs in both locales are still open |
| Library / HUD | S002 copy projection is unchanged. S003 is not a Quick Custom card |
| Combat certification | Air overlay health, damage, and range are uncertified |

### Localization

EN/FA keys are merged from `cursor/skirmish-s003-localization-f9f7` at `c01a4772b` (PR #28). The merge kept Game Design’s `V3UiLocalizationCatalog.asset` and `Assets/Game/Configs/Localization/SkirmishS003UiStrings.json`. Keys were not re-authored on this branch.

- `skirmish.s003.title`
- `skirmish.s003.brief`
- `skirmish.s003.objective`
- `skirmish.s003.warning.offensive_air`
- `skirmish.s003.warning.replacement_base`
- `skirmish.s003.result.victory`
- `skirmish.s003.result.defeat`
- `skirmish.s003.result.draw_bases`
- `skirmish.s003.result.draw_deadline`
- `skirmish.s003.result.surrender`

## How to validate in Editor

Use the Skirmish shadow project. Do not lock the shared checkout. Keep Unity Hub open and signed in, and run the Windows wrapper (or `Tools/CI/invoke_unity_macos.sh` on macOS, without `-batchmode`).

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-definitions.log" `
  -RequiredPassMarker "[SkirmishExpandedDefinitionTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedCatalogTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-catalog.log" `
  -RequiredPassMarker "[SkirmishExpandedCatalogTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedEconomyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-economy.log" `
  -RequiredPassMarker "[SkirmishExpandedEconomyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-aria.log" `
  -RequiredPassMarker "[SkirmishExpandedAriaTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedVisualTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-visual.log" `
  -RequiredPassMarker "[SkirmishExpandedVisualTests] result=Passed"
```

Markers:

- `S003RegularStandardCompilesFirstVisitSeeds`
- `S003FieldSizesMatchMatrixWithoutStartingAir`
- `S003AirMobileRejectsArmorAndGatesOffensiveAir`
- `S003AssetsReuseDesertBaseLayoutAndStayInProgress`
- `S003PublicationStaysInProgressAndS002AssetStaysPlayable`
- `S003AirMobileAaGatePadReadinessAndMissingPadRefund`
- `S003AirMobilePublicControlsDoNotMutateGameplay`
- `S003RegistrySpawnsAaAndLightHelicopterFromExistingKeys`

S002 Playable tests in the same suites stay in the run. This environment has no Unity Editor, so the new markers were not executed here. Windows Programmer 1 already passed definition, catalog, objective, economy, aria, and visual on tip `4eb1bbd74`.

### Game View capture and guarded flip

Use the Skirmish shadow project. Keep Unity Hub open and signed in. The launch stays in Play Mode (`stayInPlayMode=1`), same as S002.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishS003GameViewCapture.RunFocusedLaunchRegularStandard `
  -LogFile "$env:TEMP\skirmish-s003-gameview.log" `
  -RequiredPassMarker "[SkirmishS003GameView] result=Passed"
```

Required evidence, either directory is enough for the flip:

| File | Directory |
|---|---|
| `s003-regular-standard-104732-playing.png` | `_Evidence/` and `Design/AgentReports/SkirmishExpansion/S003/_Evidence/` |
| `s003-regular-standard-104732-gameview.json` | same two directories (`forcedVictory=0`) |

Dry-run does not write the manifest. Before the files exist it logs `result=Failed catalog=S003 playable=0 confirm=0`, and the Windows wrapper treats that as a failed validation. After the files exist and the hashes match, the same method logs `wouldFlip=1` and still leaves the asset InProgress.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishPublicationFlipMenu.RunFocusedFlipS003DryRun `
  -LogFile "$env:TEMP\skirmish-s003-flip-dry.log" `
  -RequiredPassMarker "[SkirmishPublicationFlip] result=Passed catalog=S003 wouldFlip=1 playable=0 confirm=0"
```

Confirm writes only the S003 row. Run it after the dry-run reports `wouldFlip=1`. S002 stays Playable.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "<shadow project>" `
  -ExecuteMethod Game.Editor.SkirmishPublicationFlipMenu.RunFocusedFlipS003Confirm `
  -LogFile "$env:TEMP\skirmish-s003-flip-confirm.log" `
  -RequiredPassMarker "[SkirmishPublicationFlip] result=Passed catalog=S003 playable=1 confirm=1"
```

Menu names: `Tools/Warline/Skirmish/Launch S003 Regular Standard Game View` and `Tools/Warline/Skirmish/Flip S003 Playable If Evidence Ready`.

## ARIA Windows harness (not a live win)

The recorder is `Game.Editor.SkirmishS003AriaRunHarness`. It uses the same watch driver as S002 (`SkirmishExpandedAriaWatchDriver`): shipping touch ARIA, `normal_speed=1`, and an abort of `simulationNotAdvancing` about 45 seconds after Playing when simulation is inactive or match elapsed stays at 0. It never stamps Victory unless the match has finished with that outcome. This section does not mark the mission or the AriaWon matrix complete. Live S003 AriaWon waits until after the S002 Windows proof and until the Skirmish Editor lock is free. Do not start `Launch*` while that lock is held.

Focused proof (no live match; the wrapper's `-quit` is fine here):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishS003AriaHarnessTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-aria-harness.log" `
  -RequiredPassMarker "[SkirmishS003AriaHarnessTests] result=Passed" `
  -GuiLicensing
```

Live watch, only after the lock is free: run the menu or executeMethod from the open Editor. Do **not** pass `-quit`. `Tools/CI/InvokeUnityExecuteMethodValidation.ps1` always passes `-quit` and will return when Play Mode is scheduled, before any terminal row.

- Menu: `Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 en`
- Execute method: `Game.Editor.SkirmishS003AriaRunHarness.Launch104732En`

Keep Unity Hub signed in, focus the Game View, and keep `timeScale` at 1. Expect trace lines under `Design/AgentReports/SkirmishExpansion/S003/_Evidence/` with rising `elapsed` / `clockElapsed` and `simulationActive=1` within about 45 seconds of Playing. A stuck-zero clock must Abort with `simulationNotAdvancing`, not sit until the 1500 second timeout. Fill rules and the other seed menus are in `acceptance.md`.
