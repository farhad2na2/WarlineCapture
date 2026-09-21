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
- Role catalog bindings point at the existing air prefab configs. This slice does not spawn air visuals.

## Publication
- Status: **InProgress**. Compiler, definition, Field roster, and air production gates only.
- Not Playable, not Game View captured, not ARIA/War certified, not Accepted.

## Remaining gaps

| Area | Gap |
|---|---|
| Air visuals | No Helipad/Airport yard, no attack-helicopter or jet GameObjects, no air return/refuel motion |
| Field backbone | Matrix reports 7 starting structures. The compiler still spawns the designated Barracks and Ground Staging only, same as the S002 structure list versus its reported count of 10 |
| Game View | No Regular Standard Playing capture for seed `104732` |
| Playable | Row stays InProgress until visuals, a human win, and the evidence files exist |
| ARIA matrix | Seeds `104732` / `130366` / `155924` × EN/FA are not run. `runs.csv` is not opened for S003 |
| Localization | `skirmish.s003.*` keys are on the definition only. `V3UiLocalizationCatalog` was not edited |
| Library / HUD | S002 copy projection is unchanged. S003 is not a Quick Custom card |
| Combat certification | Air overlay health, damage, and range are uncertified. Fuel return is not simulated |

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

New markers inside those suites:

- `S003RegularStandardCompilesFirstVisitSeeds`
- `S003FieldSizesMatchMatrixWithoutStartingAir`
- `S003AirMobileRejectsArmorAndGatesOffensiveAir`
- `S003AssetsReuseDesertBaseLayoutAndStayInProgress`
- `S003PublicationStaysInProgressAndS002AssetStaysPlayable`

S002 Playable tests in the same suites stay in the run. This environment has no Unity Editor, so those markers were not executed here.
