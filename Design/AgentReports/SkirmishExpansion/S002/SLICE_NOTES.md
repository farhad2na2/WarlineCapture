# Skirmish mission 4 (Desert Base Base Assault expanded)

Catalog identity **S002** is named once here: Desert Base · Base Assault · Ground Maneuver · Established Base.
Handoff ordinal 4. First visit: Regular / Standard. Seed sample: `104731`.

Operations package 0 landed on `main` as `54b64e906` and is merged here with a
merge commit. That tree was not edited in this slice.

## What landed

### Shared contracts ticket (SK-00)
- New `Game.Skirmish.Contracts` assembly (`noEngineReferences`, auto-referenced).
  Consumer assemblies also list it explicitly (see `ASMDEF_REFERENCES.md`).
- Typed IDs/enums for catalog/definition/setup, size, difficulty, army, start,
  objective, roles, outcomes, checkpoint header, and reason codes.
- Legacy prototype map helper (0/1/3 + reserved stress 2).

### Compiler and session ticket (SK-01)
- Twelve config types under `Assets/Game/Scripts/Configs/Skirmish/`.
- `SkirmishResolvedSetup`, `SkirmishLaunchPayload`, session/ownership/objective
  components, and composition launch helper.
- Shared authored assets under `Assets/Game/Configs/SkirmishExpansion/Shared/`.
- `SkirmishSetupCompiler.TryCompile` compares army/start/logic/size vectors to
  `INITIAL_SETUP_MATRIX.csv` with field-specific reasons.
- `SkirmishDefinitionBuilder` + `SkirmishSetupCompilerValidation`.
- Session init / spawn / cleanup systems. Skirmish mission 4 Standard Regular
  starting forces/structures bind `UnitSourcePrefabKey` from the role catalog, so
  `SpawnVisualPending=0` without a live ECS prefab registry. Instantiating
  GameObjects from that registry remains later ground-roster certification.
- Catalog entry gained optional `DefinitionId` / `ContentVersion` /
  `ReadinessManifestId` fields. Original `SCENARIO_CATALOG.csv` column order is
  unchanged. Publication lives in `SkirmishPublicationManifest.asset`.

### Ground roster and production visual ticket (SK-02)
- Typed `SkirmishRoleOverlay` + `SkirmishRoleOverlayCatalog` for infantry and
  Skirmish mission 4 ground vehicles (rifle/gunner/rocketeer/car/APC/tank plus
  Ground Maneuver-legal support). Overlays bake into the compiled snapshot with
  MATCH_SETUP Materials costs. `CapabilityCertified=false`.
- `SkirmishRosterProjectionSystem` applies overlays once (health/damage/range/
  producer/target domains). Blanket prototype rifle tuning is not used on
  expanded sessions.
- Infantry production accepts **4 members per squad**. Vehicles require Ground
  Staging. Ground Maneuver still rejects offensive air.
- Ground Staging is a typed producer + compiled starting structure (visual key
  `Building_GroundStaging`). Not a new art prefab.
- Spawn expands force quantity, binds `Faction` + `UnitSourcePrefabKey` for
  every Skirmish mission 4 starting member. `SpawnVisualPending=0` for Standard
  Regular.
- `SkirmishProductionService.TryProduce` / `TryReleaseDeath` is the
  production-to-death path for the Skirmish mission 4 ground set (ledger +
  overlay + cap).

### Ground capacity reservation ticket (SK-03)
- `SkirmishEconomyStockComponent` seeds Materials/Oil/Fuel from the compiled
  snapshot (Standard Established: 900/240/700).
- `SkirmishCapacityLedger` + reservation buffer: Reserved → Live → Released.
  Instant Skirmish mission 4 produce visits Reserved then Live in the same call.
  Starting grants count as Live. Death releases once.
- Shared eligibility now also checks Materials/Fuel/caps when
  `EnforceStocks=true`. No research tree in this slice.

### Army control, fog, and movement ticket (SK-04)
- Starting Skirmish mission 4 Standard Regular forces form persistent tactical
  groups: four-person infantry squads and singleton ground vehicles. Group IDs
  stay stable through losses. A produced tank or rifle squad opens a new group.
- Legal player selection is one group or an explicit multi-group union. Enemy
  and empty groups are rejected. Shared `SelectedUnitTag` is the selection
  truth so existing command owners can see the same set. Clearing a selection
  writes group records first, then removes tags, so the army-group buffer is
  never held across that structural change.
- Paging is independent of group IDs (Standard page size 4). Page 0 holds the
  first four player groups; page 1 slot 3 is the starting tank for this roster.
- Shared fog seeds from the intel config (`SharedFog=true`,
  `DevelopmentFullVision=true` for this ground slice). Precision Attack is
  legal only against **Visible** contacts. Turning off development full vision
  ages a previously seen enemy to LastSeen and rejects Attack
  (`HiddenContact`). Reveal restores Visible.
- Move / Hold / Attack stamp the selected groups. Ground Move excludes air
  members instead of silently forcing them onto a ground destination. This is
  **not** live path/world movement or an Army drawer HUD.

### Base Assault objective depth (SK-06)
- `SkirmishBaseAssaultFacts` + `SkirmishObjectiveFactProjectionSystem`.
  Terminal evaluation uses **original designated** `base.player` / `base.enemy`
  identities only. Replacement Barracks do not count as extra lives.
- Field-army wipe is non-terminal. Pause freezes the deadline clock.
  Surrender is accepted only while Playing. Same-tick both-designated-dead is
  Draw / BothBasesDestroyed.
- `SkirmishOutcomeSystem` remains the only terminal writer.

### Launch path
- `SkirmishExpandedLaunchResolver.TryCompileAndQueue` compiles Skirmish
  mission 4 (size/difficulty/seed) and queues the immutable snapshot. No new
  `MatchSceneView` catalog-identity switch. Map hint remains Desert Base
  (index 0) for scene load only.

### Enemy strategy and public ARIA skills (SK-05)
- `SkirmishEnemyStrategySystem` scores Base Assault with shared
  `SkirmishStrategyScoring` (objective 40 / prevent-loss 100 / counter 20,
  hysteresis 15, ~25% Supply reserve). It issues only
  `SkirmishProductionService.TryProduce` (faction 2) and
  `SkirmishArmyCommandService.TryIssueGroupOrder` — no private producer or
  designated-base mutation. Enemy Materials/capacity live on
  `SkirmishEnemyStockComponent` / `SkirmishEnemyCapacityComponent`
  (`NextReservationId` starts at 1000) so player stocks stay untouched.
- Perception is fog-honest: visible player combat only
  (`SkirmishFogService.IsVisible`). Hostile Materials stay unknown unless
  `KnowsHostileMaterials` is set. Full-vision Skirmish mission 4 therefore
  sees the starting tank and recruits a rocketeer (120 Materials) when
  affordable, then attacks the designated player Barracks when that contact
  is Visible.
- Live `OnUpdate` resolves Ground Maneuver through a cached army profile
  (no per-tick `CreateInMemory`). Tests pass the authored
  `ArmyGround` ScriptableObject.
- `AriaSkirmishPlanSystem.Step` routes expanded sessions through
  `StepExpandedBaseAssault`. It only targets presented `AriaTouchTarget`
  controls (Recruit / SelectSquad / Attack / Hold / Inspect / Handback).
  Three failed attempts at the same action choose an alternative or
  Handback. The ARIA assembly still has no gameplay mutation API.
- Shadow tip `bb30f8ce3` failed
  `ExpandedAriaPlanTargetsPublicControlsWithoutGameplayMutation`:
  default `AriaPlayPhase.Manual` returned before the planner copied
  Recruit `Id=41` into `AriaPlayObservationComponent.TargetId` (Intent
  stayed the default Recruit = 0). Expanded planning now publishes the
  presented control while idle/Manual; Blocked, Starting, and in-flight
  Touching still skip. Prototype consent (`Manual` until Observing) is
  unchanged.
- Shared eligibility (`SkirmishProductionEligibility` with
  `EnforceStocks=true`) is the affordability boundary for UI, enemy, and
  ARIA. `SkirmishAriaPublicProjection` reads the player wallet only.
- Not yet: Frontline Control / Breakthrough / Convoy Escort policies,
  four difficulty behaviour profiles, structures/research/transport/scout
  camera skills, EN/FA teaching copy, or a counted normal-speed ARIA win.

### Publication
- Status: **InProgress**. Closer to a playable ground slice (compiler +
  overlays + designated Base Assault facts + capacity + legal army groups +
  legal enemy/ARIA BA skills) but **not Playable**, not ARIA/War certified,
  not Accepted.

## Remaining ticket gaps

| Ticket | Gap |
|---|---|
| Ground roster and production visual ticket (SK-02) | Registry GameObject instantiate, Ground Staging prefab/pads, air roles, combat certification |
| Ground capacity reservation ticket (SK-03) | Research tree, fabrication/refinery profiles, physical haul, cancel/refund 75% mid-produce. Recruitment FuelCost stays 0 (MATCH_SETUP treats fuel as operation, not a second buy currency) |
| Army control, fog, and movement ticket (SK-04) | Live path/world movement, transport boarding, formation/columns, Army drawer HUD, terrain-blocked sight |
| Enemy strategy ticket (SK-05) | Frontline Control / Breakthrough / Convoy Escort policies; four difficulty profiles; structures/research/transport/camera skills; EN/FA teaching; counted full-speed ARIA wins |
| Base Assault objective depth (SK-06) | Hidden-health HUD, world-damage fixtures, pause/result replay through live match |
| Checkpoint ticket (SK-10) | Checkpoint/result/replay DTOs and atomic restore |
| Measured layout ticket (SK-11) | Measured Desert Base layout, legal pads, route times |
| Publication and localization ticket (SK-12) | Quick Custom briefing/HUD, EN/FA catalog wiring, publication validator |
| Acceptance evidence ticket (SK-13) | Manual + ARIA win matrix, device/recovery evidence |

## How to validate in Editor

Use Programmer 1’s **shadow project only**: `D:\Projects\WarlineCapture-Skirmish`
(git worktree, own `Library`). Check out `cursor/skirmish-shadow` or this
short-lived `cursor/skirmish-s002-…` branch there. Do **not** open or lock the
shared `D:\Projects\WarlineCapture` checkout.

1. Keep Unity Hub open and signed in.
2. Compile/check the new assemblies. `Game.Skirmish.Contracts` stays its own
   assembly. Consumers that `using Game.Skirmish.Contracts` reference it
   explicitly: `Game.Components`, `Game.Configs`, `Game.Runtime`,
   `Game.Composition`, `Game.Editor`, and `Game.Tests.Editor`.
3. From the shadow worktree, run the Windows wrapper (preferred on Programmer 1):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-definitions.log" `
  -RequiredPassMarker "[SkirmishExpandedDefinitionTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedObjectiveTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-objectives.log" `
  -RequiredPassMarker "[SkirmishExpandedObjectiveTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedEconomyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-economy.log" `
  -RequiredPassMarker "[SkirmishExpandedEconomyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedArmyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-army.log" `
  -RequiredPassMarker "[SkirmishExpandedArmyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-aria.log" `
  -RequiredPassMarker "[SkirmishExpandedAriaTests] result=Passed"
```

Required markers:

- `[SkirmishExpandedDefinitionTests] result=Passed`
- `[SkirmishExpandedObjectiveTests] result=Passed`
- `[SkirmishExpandedEconomyTests] result=Passed`
- `[SkirmishExpandedArmyTests] result=Passed`
- `[SkirmishExpandedAriaTests] result=Passed`

Optional compiler rebuild:

```
Tools/Warline/Skirmish/Rebuild Expanded Definitions
```

4. Legacy smoke: Quick Custom still offers Skirmish 1 Desert Base prototype
   (index 0), Skirmish 2 City Crossroads (index 1), and Farhad’s Skirmish 3
   Industrial Basin (index 3). Editor stress remains index 2.
   Do not expect Skirmish mission 4 on the old playable catalog override.
5. Expanded seed path: Regular Standard `104731` (also `130365`, `155923`).

## Localization follow-up

Shared `V3UiLocalizationCatalog` was not edited. Required keys:

| Key | EN | FA |
|---|---|---|
| `skirmish.s002.title` | Desert Base · Base Assault · Established | پایگاه صحرا · حمله به پایگاه · پایگاه برقرار |
| `skirmish.s002.brief` | Use the starting tank and APC to contest the highway. Keep one infantry squad at home. Press now or scout the south sweep before siege. | با تانک و نفربر شروع‌شده بزرگراه را بگیر. یک دسته پیاده در خانه بماند. یا سریع فشار بده یا جنوب را شناسایی کن. |
| `skirmish.s002.objective` | Destroy the original enemy Barracks while your original Barracks survives. | سربازخانه اصلی دشمن را نابود کن، در حالی که سربازخانه اصلی خودت سالم است. |
| `skirmish.s002.warning.offensive_air` | Ground Maneuver cannot queue attack helicopters or jets. | پروفایل زمینی حمله هوایی تهاجمی را صف نمی‌کند. |
| `skirmish.s002.warning.replacement_base` | A replacement Barracks is not the designated victory base. | سربازخانه جایگزین پایگاه پیروزی تعیین‌شده نیست. |
| `skirmish.s002.result.victory` | Enemy main base destroyed. | پایگاه اصلی دشمن نابود شد. |
| `skirmish.s002.result.defeat` | Your main base was destroyed. | پایگاه اصلی تو نابود شد. |
| `skirmish.s002.result.draw_bases` | Both original bases were destroyed. | هر دو پایگاه اصلی نابود شدند. |
| `skirmish.s002.result.draw_deadline` | Time expired with both original bases standing. | زمان تمام شد و هر دو پایگاه اصلی سالم ماندند. |
| `skirmish.s002.result.surrender` | Surrender accepted. | تسلیم پذیرفته شد. |

Owner: publication and localization ticket (SK-12) / localization catalog
builder. Do not mark Skirmish mission 4 Playable until those keys exist in
both EN and FA tables.
