# Skirmish mission 5 (Desert Base Base Assault expanded)

Catalog identity **S003** is named once here: Desert Base · Base Assault · Air Mobile · Field Base.
Handoff ordinal 5. First visit: Regular / Standard. Seeds: `104732`, `130366`, `155924`.

Started from `main` at `a9cea343d` (S002 Playable, PR #23). This slice does not edit Operations trees, shared `SaveDataModel`, `MatchSceneView`, `V3UiLocalizationCatalog`, or Campaign/Gridlock.

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
- Publication row is **InProgress**. Complete evidence cannot flip S003 to Playable in this slice.

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

## Publication
- Status: **InProgress**. Compiler, Field roster, AA/pad economy, public air controls, and registry spawn are in. Flight, refuel, Game View, and the ARIA matrix are not.
- Not Playable. S002 on the checked-in manifest stays Playable.

## Remaining gaps

| Area | Gap |
|---|---|
| Air motion | No attack pass, return, landing, or refuel. Fuel return is not simulated |
| Helipad yard | The Helipad/Airport prefab keys attach when the registry already has them. There is still no authored Skirmish pad yard, taxi path, or landing reservation |
| Field backbone | Matrix reports 7 starting structures. The compiler still spawns the designated Barracks and Ground Staging only |
| Game View | No Regular Standard Playing capture for seed `104732` |
| Playable | Row stays InProgress until a human win and the evidence files exist |
| ARIA matrix | Seeds `104732` / `130366` / `155924` × EN/FA are not run. `runs.csv` is not opened for S003. The shell does not yet present the AA or pad controls |
| Localization | Keys below are listed for Game Design. `V3UiLocalizationCatalog` was not edited |
| Library / HUD | S002 copy projection is unchanged. S003 is not a Quick Custom card |
| Combat certification | Air overlay health, damage, and range are uncertified |

### Localization keys for Game Design (EN/FA)

Do not treat this list as catalog copy. `V3UiLocalizationCatalog` was left unchanged.

- `skirmish.s003.title`
- `skirmish.s003.brief`
- `skirmish.s003.objective`
- `skirmish.s003.warning.missing_helipad`
- `skirmish.s003.warning.missing_airport`
- `skirmish.s003.warning.pad_not_ready`
- `skirmish.s003.warning.aa_before_air`
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

S002 Playable tests in the same suites stay in the run. This environment has no Unity Editor, so the new markers were not executed here. Windows Programmer 1 already passed definition, catalog, and objective markers on the previous tip.
